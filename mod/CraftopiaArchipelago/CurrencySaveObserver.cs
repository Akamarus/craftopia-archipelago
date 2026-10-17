using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    // Observes serialization; never writes a game save. Active only for a pending
    // currency mutation. Saved data are compared later through CurrencySaveReader.
    internal sealed class CurrencySaveObserver
    {
        static CurrencySaveObserver current;
        static string lastStage;
        static void Report(string stage) {
            if(lastStage==stage) return;
            lastStage=stage;
            try { Plugin.Log.LogInfo("[AP] Currency save verification: "+stage); } catch { }
        }
        [ThreadStatic] static Capture capture;
        readonly string token,path;
        readonly int guid,count;
        readonly object player;
        CurrencySaveProof proof;
        internal static bool Installed { get; private set; }
        static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        sealed class Capture {
            internal CurrencySaveObserver Owner;
            internal string Player,Inventory;
        }
        CurrencySaveObserver(string token,int guid,int count,string path,object player) {
            this.token=token; this.guid=guid; this.count=count; this.path=path; this.player=player;
        }
        internal static void Install(Harmony harmony) {
            var save=typeof(Oc.OcPlCharacterManager).GetMethod("SaveCharacter",flags,null,Type.EmptyTypes,null);
            var cacheType=typeof(Oc.OcSaveManager).GetNestedType("FileCache",BindingFlags.Public|BindingFlags.NonPublic);
            var add=typeof(Oc.OcSaveManager).GetMethod("AddCacheData",flags,null,new[]{cacheType,typeof(string),typeof(string)},null);
            if(save==null || add==null) throw new MissingMethodException("Currency save observer");
            var type=typeof(CurrencySaveObserver);
            harmony.Patch(save,prefix:new HarmonyMethod(type,nameof(Start)),finalizer:new HarmonyMethod(type,nameof(Finish)));
            harmony.Patch(add,postfix:new HarmonyMethod(type,nameof(Cache)));
            Installed=Has(save,nameof(Start),true) && Has(save,nameof(Finish),false) &&
                Harmony.GetPatchInfo(add).Postfixes.Any(p=>p.owner==Plugin.GUID && p.PatchMethod.Name==nameof(Cache));
            if(!Installed) throw new InvalidOperationException("Currency save hooks missing");
        }
        static bool Has(MethodBase method,string name,bool prefix) {
            var info=Harmony.GetPatchInfo(method);
            if(info==null) return false;
            foreach(var p in prefix ? info.Prefixes : info.Finalizers)
                if(p.owner==Plugin.GUID && p.PatchMethod.Name==name) return true;
            return false;
        }
        internal static CurrencySaveObserver Track(string token,int guid,int count) {
            Cancel();
            if(!Installed || string.IsNullOrEmpty(token) || count<1) return null;
            try {
                var manager=Oc.OcPlCharacterManager.Inst;
                var selected=typeof(Oc.OcPlCharacterManager).GetField("_selectedCharacter",flags).GetValue(manager) as Oc.OcPlCharaSaveData;
                var local=Oc.OcPlMng.LocalPl;
                if(selected==null || local==null || selected.GUID!=guid || local.GUID!=guid) return null;
                var path=System.IO.Path.GetFullPath(manager.GetCharaSaveFullPath(selected));
                Report("tracking post-delivery save");
                return current=new CurrencySaveObserver(token,guid,count,path,local);
            } catch(Exception e) { Report("tracking failed: "+e.GetType().Name); return null; }
        }
        internal static void Cancel() { current=null; capture=null; }
        bool IdentityMatches() {
            try {
                var local=Oc.OcPlMng.LocalPl;
                var manager=Oc.OcPlCharacterManager.Inst;
                var selected=typeof(Oc.OcPlCharacterManager).GetField("_selectedCharacter",flags).GetValue(manager) as Oc.OcPlCharaSaveData;
                return ReferenceEquals(local,player) && local!=null && local.GUID==guid && selected!=null && selected.GUID==guid &&
                    string.Equals(System.IO.Path.GetFullPath(manager.GetCharaSaveFullPath(selected)),path,StringComparison.OrdinalIgnoreCase);
            } catch { return false; }
        }
        internal bool Verify(string activeToken,int activeGuid) {
            if(!ReferenceEquals(current,this) || !IdentityMatches() || proof==null) return false;
            bool saved=proof.Matches(activeToken,activeGuid,(p,i)=>CurrencySaveReader.Matches(path,p,i));
            Report(saved ? "saved inventory verified" : "waiting for matching saved rows");
            return saved;
        }
        static void Start(out object __state) {
            __state=null;
            try {
                if(current==null || !current.IdentityMatches()) return;
                // A nested invocation cannot prove which snapshot completed.
                if(capture!=null) { capture=null; return; }
                __state=capture=new Capture{Owner=current};
            } catch { capture=null; }
        }
        static void Cache(object __0,string __1,string __2) {
            try {
                var c=capture;
                if(c==null || !ReferenceEquals(c.Owner,current) || !c.Owner.IdentityMatches()) return;
                var t=__0.GetType();
                if((bool)t.GetProperty("Locked",flags).GetValue(__0)) return;
                var path=(string)t.GetProperty("FilePath",flags).GetValue(__0);
                if(!CurrencySaveProof.SameSavePath(path,c.Owner.path)) { Report("save path mismatch"); return; }
                if(__1=="PlayerSave") c.Player=__2;
                if(__1=="InGame") c.Inventory=__2;
            } catch(Exception e) { Report("cache capture failed: "+e.GetType().Name); capture=null; }
        }
        static Exception Finish(Exception __exception,object __state) {
            try {
                var c=__state as Capture;
                if(__exception==null && c!=null && ReferenceEquals(c,capture) && ReferenceEquals(c.Owner,current) && c.Owner.IdentityMatches())
                {
                    c.Owner.proof=CurrencySaveProof.Create(c.Owner.token,c.Owner.guid,c.Owner.count,c.Player,c.Inventory);
                    Report(c.Owner.proof!=null ? "post-delivery snapshot captured" : "snapshot missing or invalid");
                }
            } catch { }
            finally { capture=null; }
            return __exception;
        }
    }
}
