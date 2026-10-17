using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CraftopiaArchipelago
{
    // Worker threads enqueue only; all game/store operations run in Plugin.Update.
    internal static class CurrencyRuntime
    {
        struct Inbound { internal Dictionary<string,object> Message; internal int Connection; }
        static readonly ConcurrentQueue<Inbound> grants=new ConcurrentQueue<Inbound>();
        static readonly SortedDictionary<long,Dictionary<string,object>> pending=new SortedDictionary<long,Dictionary<string,object>>();
        static string seed,slot,token;
        static long epoch;
        static int connection,guid;
        static CurrencyReceiptFile file;
        static CurrencyDelivery delivery;
        static CurrencySaveObserver observer;
        static long activeIndex=-1;
        static double nextSave;
        static bool enabled;
        internal static bool ReadyForChecks { get; private set; }
        static bool Number(Dictionary<string,object> d,string k,long n) => d.TryGetValue(k,out var v) && v is long x && x==n;
        static string Text(Dictionary<string,object> d,string k) => d.TryGetValue(k,out var v) ? v as string : null;
        internal static void QueueGrant(Dictionary<string,object> msg,int c) {
            if(grants.Count<4096) grants.Enqueue(new Inbound{Message=msg,Connection=c});
        }
        internal static void Suspend() {
            ReadyForChecks=false;
            CurrencySaveObserver.Cancel(); observer=null; delivery=null;
            file?.Dispose(); file=null; guid=0; activeIndex=-1; pending.Clear();
        }
        // Called synchronously by the one main-thread session application, before enrollment.
        internal static void ApplySession(Dictionary<string,object> m,int c) {
            if(m==null) { enabled=false; Suspend(); return; }
            if(Plugin.Bridge==null || c!=Plugin.Bridge.ConnectionId || Plugin.Validator==null ||
                !Number(m,"epoch",Plugin.Validator.CurrentEpoch)) { enabled=false; Suspend(); return; }
            var newSeed=Text(m,"seed_name"); var newSlot=Text(m,"slot"); var newToken=Text(m,"connection_token");
            bool valid=Number(m,"currency_protocol",1) && Number(m,"multiplayer_mode",0) &&
                !string.IsNullOrWhiteSpace(newSeed) && newSeed==newSeed.Trim() && newSlot==Plugin.SlotName.Value &&
                !string.IsNullOrWhiteSpace(newToken);
            if(!valid || seed!=newSeed || slot!=newSlot || epoch!=Plugin.Validator.CurrentEpoch) Suspend();
            enabled=valid;
            SoulOrbHooks.Bind(m);
            seed=newSeed; slot=newSlot; token=newToken; epoch=Plugin.Validator.CurrentEpoch; connection=c;
            // Never retain packets from the old connection, even on same-epoch reconnect.
            pending.Clear();
        }
        static string StatePath(int character) {
            string identity=MiniJson.Stringify(new Dictionary<string,object>{{"seed",seed},{"slot",slot},{"epoch",epoch},{"guid",(long)character}});
            using(var hash=SHA256.Create()) {
                string key=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-","").ToLowerInvariant();
                return Path.Combine(BepInEx.Paths.ConfigPath,"craftopia-currency-"+key+".json");
            }
        }
        internal static void AttestNew(int character) {
            // Called ONLY after the explicit new-character enrollment transition.
            // Never run this on a missing-file fallback or an existing enrollment.
            if(!enabled) return;
            try {
                if(!SoulOrbHooks.MarkCharacter(character)) throw new IOException("Orb binding not durable");
                using(var fresh=new CurrencyReceiptFile(StatePath(character)))
                    if(!CurrencyDelivery.InitializeFresh(fresh,slot,epoch,character))
                        Notice("Currency enrollment could not be created; delivery held for review.");
            } catch { Notice("Currency enrollment write failed; delivery held for review."); }
        }
        static void Notice(string text) { try { Plugin.StatusDisplay?.Transition("currency",text); } catch { } }
        static bool Valid(Dictionary<string,object> m,int c,int character) {
            return c==connection && Number(m,"currency_protocol",1) && Number(m,"epoch",epoch) &&
                Number(m,"item_id",3840) && Number(m,"amount",1) && Text(m,"seed_name")==seed &&
                Text(m,"slot")==slot && Text(m,"connection_token")==token &&
                Text(m,"character_guid")==character.ToString(System.Globalization.CultureInfo.InvariantCulture) &&
                m.TryGetValue("receipt_id",out var id) && id is long n && n>=0;
        }
        internal static void Pump(bool eligible) {
            ReadyForChecks=false;
            try {
                if(!enabled || Plugin.Bridge==null || !Plugin.Bridge.IsConnected || connection!=Plugin.Bridge.ConnectionId ||
                    Plugin.Validator==null || epoch!=Plugin.Validator.CurrentEpoch) return;
                var pl=Oc.OcPlMng.LocalPl;
                if(pl==null || !eligible) return;
                int character=pl.GUID;
                if(character==0 || Plugin.Validator.IsEnrolled==null || !Plugin.Validator.IsEnrolled(character)) return;
                if(!SoulOrbHooks.MatchesCharacterBinding(character)) { Notice("Currency character binding missing or mismatched; delivery held for review."); return; }
                if(guid!=character) { Suspend(); guid=character; }
                if(file==null) {
                    file=new CurrencyReceiptFile(StatePath(character));
                    delivery=new CurrencyDelivery(file,slot,epoch,character);
                }
                while(grants.TryDequeue(out var g))
                    if(Valid(g.Message,g.Connection,character)) pending[(long)g.Message["receipt_id"]]=g.Message;
                if(delivery.Held) { Notice("Currency delivery held: preserve receipt files for review."); return; }
                ReadyForChecks=true;
                // Prefer the live mutation until its save/commit completes.
                long index=activeIndex;
                if(index<0) foreach(var pair in pending) { index=pair.Key; break; }
                if(index<0 || !pending.TryGetValue(index,out var message)) return;
                bool completedNow=false;
                var outcome=delivery.Deliver(index,1,true,SoulOrbInventoryAdapter.TryApply);
                if(outcome==CurrencyResult.WaitingForSave) {
                    activeIndex=index;
                    if(observer==null) observer=CurrencySaveObserver.Track(delivery.MutationToken,character,SoulOrbInventoryAdapter.Count());
                    double now=PlayerStatusDisplay.Now;
                    if(observer!=null && observer.Verify(delivery.MutationToken,character)) {
                        if(delivery.ConfirmSaved(index,delivery.MutationToken,slot,epoch,character,true)) {
                            outcome=CurrencyResult.Acknowledge; completedNow=true;
                            observer=null; CurrencySaveObserver.Cancel(); activeIndex=-1;
                        }
                    } else if(now>=nextSave) {
                        nextSave=now+5;
                        Oc.OcSaveManager.Inst.SaveGameData(false,false);
                    }
                }
                if(outcome==CurrencyResult.Acknowledge) {
                    var ack=new Dictionary<string,object>(message) {{"saved",true}};
                    ack["cmd"]="currency_ack";
                    Plugin.Bridge.SendUnlockAck(ack); pending.Remove(index);
                    Notice(null);
                    if(completedNow) Plugin.StatusDisplay?.Notify("currency:"+epoch+":"+index,"Received: Soul Orb x1",PlayerStatusDisplay.Now);
                } else if(outcome==CurrencyResult.Deferred) Notice("Soul Orb currency waiting for inventory space.");
                else if(outcome==CurrencyResult.Hold) { ReadyForChecks=false; Notice("Currency delivery held: preserve receipt files for review."); }
            } catch { ReadyForChecks=false; Notice("Currency delivery could not be verified; no acknowledgment sent."); }
        }
    }
}
