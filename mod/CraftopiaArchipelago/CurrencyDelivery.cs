using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    public interface ICurrencyReceiptStore {
        string Read();
        bool CompareExchange(string expected, string next);
    }
    public enum CurrencyMutation { Ambiguous=0, NoMutation=1, Applied=2 }
    public enum CurrencyResult { Hold=0, Deferred=1, WaitingForSave=2, Acknowledge=3 }

    // Soul Orb currency only. Independent of SP entitlement. Main-thread owner.
    // Live acknowledgments require the post-mutation character-save proof.
    public sealed class CurrencyDelivery
    {
        readonly ICurrencyReceiptStore store;
        readonly string slot;
        readonly long epoch;
        readonly int guid;
        readonly int itemId;
        readonly Dictionary<long,int> committed = new Dictionary<long,int>();
        string persisted;
        long? pending;
        int pendingAmount;
        bool mutated;
        public bool Held { get; private set; }
        public string MutationToken { get; private set; }

        static bool Identity(string slot,long epoch,int guid) =>
            !string.IsNullOrWhiteSpace(slot) && slot==slot.Trim() && epoch>0 && guid!=0;
        static bool Exact(Dictionary<string,object> d,string key,long expected) =>
            d.TryGetValue(key,out var value) && value is long n && n==expected;
        static Dictionary<string,object> Document(string slot,long epoch,int guid,
            Dictionary<long,int> receipts,long? pending,int amount,int itemId)
        {
            var rows=new List<object>();
            foreach(var pair in receipts) rows.Add(new Dictionary<string,object>{{"index",pair.Key},{"amount",(long)pair.Value}});
            return new Dictionary<string,object> {
                {"schema",1L},{"slot",slot},{"epoch",epoch},{"guid",(long)guid},{"item_id",(long)itemId},
                {"receipts",rows},{"pending",pending.HasValue ? (object)new Dictionary<string,object>
                    {{"index",pending.Value},{"amount",(long)amount}} : null}
            };
        }
        static bool Receipt(Dictionary<string,object> row,out long index,out int amount) {
            index=-1; amount=0;
            if(row==null || !row.TryGetValue("index",out var i) || !(i is long n) || n<0 ||
                !row.TryGetValue("amount",out var a) || !(a is long count) || count<1 || count>999) return false;
            index=n; amount=(int)count; return true;
        }
        public static bool InitializeFresh(ICurrencyReceiptStore store,string slot,long epoch,int guid,int itemId=3840) {
            // Caller must establish explicit fresh enrollment; never called on a missing-file fallback.
            if(!Identity(slot,epoch,guid)) return false;
            try { return store.Read()==null && store.CompareExchange(null,
                MiniJson.Stringify(Document(slot,epoch,guid,new Dictionary<long,int>(),null,0,itemId))); }
            catch { return false; }
        }
        public CurrencyDelivery(ICurrencyReceiptStore store,string slot,long epoch,int guid,int itemId=3840) {
            this.store=store; this.slot=slot; this.epoch=epoch; this.guid=guid; this.itemId=itemId;
            try {
                if(!Identity(slot,epoch,guid)) throw new FormatException();
                persisted=store.Read();
                var d=MiniJson.ParseObject(persisted);
                if(d==null || !d.TryGetValue("schema",out var schema) || !(schema is long ver) || ver!=1 ||
                    MiniJson.GetString(d,"slot")!=slot || !Exact(d,"epoch",epoch) ||
                    !Exact(d,"guid",guid) || !Exact(d,"item_id",itemId) ||
                    !d.TryGetValue("receipts",out var rows) || !(rows is List<object> list) ||
                    !d.ContainsKey("pending")) throw new FormatException();
                foreach(var row in list) {
                    if(!Receipt(row as Dictionary<string,object>,out var index,out var amount) || committed.ContainsKey(index))
                        throw new FormatException();
                    committed.Add(index,amount);
                }
                // Any unresolved intent after restart is ambiguous, even if inventory happens to match.
                if(d["pending"]!=null) Held=true;
            } catch { Held=true; }
        }
        bool Save(Dictionary<long,int> receipts,long? nextPending,int amount) {
            try {
                string next=MiniJson.Stringify(Document(slot,epoch,guid,receipts,nextPending,amount,itemId));
                if(!store.CompareExchange(persisted,next)) return false;
                persisted=next; return true;
            } catch { return false; }
        }
        bool Unchanged() {
            try { if(store.Read()==persisted) return true; } catch { }
            Held=true; return false;
        }
        public CurrencyResult Deliver(long index,int amount,bool eligible,Func<int,CurrencyMutation> apply) {
            if(Held || !Unchanged()) return CurrencyResult.Hold;
            if(index<0 || amount<1 || amount>999 || apply==null) { Held=true; return CurrencyResult.Hold; }
            if(!eligible) return CurrencyResult.Deferred; // Includes duplicate acknowledgments.
            if(committed.TryGetValue(index,out var prior)) {
                if(prior==amount) return CurrencyResult.Acknowledge;
                Held=true; return CurrencyResult.Hold;
            }
            if(pending.HasValue) {
                if(pending.Value!=index) return CurrencyResult.Deferred;
                if(pendingAmount!=amount) { Held=true; return CurrencyResult.Hold; }
                return mutated ? CurrencyResult.WaitingForSave : CurrencyResult.Hold;
            }
            if(!Save(committed,index,amount)) { Held=true; return CurrencyResult.Hold; }
            pending=index; pendingAmount=amount;
            CurrencyMutation outcome;
            try { outcome=apply(amount); } catch { outcome=CurrencyMutation.Ambiguous; }
            if(outcome==CurrencyMutation.NoMutation) {
                if(!Save(committed,null,0)) { Held=true; return CurrencyResult.Hold; }
                pending=null; pendingAmount=0; return CurrencyResult.Deferred;
            }
            if(outcome!=CurrencyMutation.Applied) { Held=true; return CurrencyResult.Hold; }
            mutated=true; MutationToken=Guid.NewGuid().ToString("N");
            return CurrencyResult.WaitingForSave;
        }
        public bool ConfirmSaved(long index,string mutationToken,string savedSlot,long savedEpoch,int savedGuid,bool verified) {
            // Integration must capture this token when a save STARTS, and supply it
            // only after verifying that same character's inventory save completed.
            // A generic autosave notification or a save started earlier is not proof.
            if(Held || !Unchanged() || !verified || !mutated || pending!=index || string.IsNullOrEmpty(MutationToken) ||
                MutationToken!=mutationToken || slot!=savedSlot || epoch!=savedEpoch || guid!=savedGuid) return false;
            var next=new Dictionary<long,int>(committed) {{index,pendingAmount}};
            if(!Save(next,null,0)) return false; // Persist-only retry; no second mutation.
            committed.Add(index,pendingAmount); pending=null; pendingAmount=0; mutated=false; MutationToken=null;
            return true;
        }
    }
}
