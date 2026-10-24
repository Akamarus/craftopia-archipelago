using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace CraftopiaArchipelago
{
    // Socket loss never disables enforcement; invalid contracts latch until reviewed.
    public sealed class EquipmentSession
    {
        private bool approved;
        private int guid;
        private long epoch, contractEpoch;
        private readonly string slot;
        private bool accepted, rejected, managed;
        private string approvalPath, approvedCatalog;
        public string Catalog { get; private set; }
        public bool Required { get; private set; }
        public bool ContractReady => !Required || (accepted && approved && !rejected);
        public bool CanApprove => managed && accepted && !approved && !rejected;
        public EquipmentSession(bool approved, int guid, long epoch, string slot)
        {
            this.approved=approved; this.guid=guid; this.epoch=epoch; this.slot=slot;
            Required=approved;
        }
        private static long Integer(Dictionary<string,object> value,string key) {
            if(!value.TryGetValue(key,out var raw) || !(raw is long number)) throw new FormatException();
            return number;
        }
        public static EquipmentSession Managed(string slot,string path)
        {
            var s=new EquipmentSession(false,0,0,slot) { managed=true, approvalPath=path };
            try {
                if(File.Exists(path+".tmp") || Directory.Exists(path+".tmp") || Directory.Exists(path)) throw new IOException();
                if(File.Exists(path)) {
                    var d=MiniJson.ParseObject(File.ReadAllText(path));
                    if(Integer(d,"schema")!=1 || MiniJson.GetString(d,"slot")!=slot) throw new FormatException();
                    s.guid=checked((int)Integer(d,"guid")); s.epoch=Integer(d,"epoch");
                    s.approvedCatalog=MiniJson.GetString(d,"catalog");
                    if(s.guid==0 || s.epoch<=0 || string.IsNullOrEmpty(s.approvedCatalog)) throw new FormatException();
                    s.approved=true; s.Required=true;
                }
            } catch { s.rejected=true; s.Required=true; }
            return s;
        }
        public bool Bind(long sessionEpoch, string sessionSlot, string catalog, int protocol, int multiplayer)
        {
            if(catalog!=null || protocol!=0) Required=true;
            if(!Required) return true;
            accepted=false;
            if(rejected || sessionEpoch<=0 || string.IsNullOrWhiteSpace(slot) || sessionSlot!=slot ||
                (contractEpoch!=0 && contractEpoch!=sessionEpoch) || (Catalog!=null && Catalog!=catalog) ||
                !((catalog==EquipmentCatalog.CatalogId && protocol==1) ||
                  ((catalog==EquipmentCatalog.ReviewedLootCatalogId || catalog==EquipmentCatalog.ReviewedLootR2CatalogId) && protocol==2)
                  || (catalog==EquipmentCatalog.ReviewedBalanceCatalogId && protocol==3)) ||
                multiplayer!=0 || (!managed && !approved) ||
                (approved && (guid==0 || epoch!=sessionEpoch || (approvedCatalog!=null && approvedCatalog!=catalog)))) {
                rejected=true; return false;
            }
            accepted=true; Catalog=catalog; contractEpoch=sessionEpoch; return true;
        }
        // Only called after the exact fresh-character coordinator commits successfully.
        public bool Approve(int character,long sessionEpoch,string sessionSlot)
        {
            if(!CanApprove || character==0 || contractEpoch!=sessionEpoch || slot!=sessionSlot) return false;
            try {
                string json=MiniJson.Stringify(new Dictionary<string,object> {
                    {"schema",1L},{"guid",(long)character},{"epoch",sessionEpoch},{"slot",slot},{"catalog",Catalog}
                });
                byte[] bytes=Encoding.UTF8.GetBytes(json);
                using(var f=new FileStream(approvalPath+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    f.Write(bytes,0,bytes.Length); f.Flush(true);
                }
                File.Move(approvalPath+".tmp",approvalPath);
                guid=character; epoch=sessionEpoch; approvedCatalog=Catalog; approved=true; return true;
            } catch { rejected=true; return false; }
        }
        public bool Ready(int currentGuid,long currentEpoch,string currentSlot,bool loadValidated)
            => Required && ContractReady && loadValidated && currentGuid==guid && currentEpoch==epoch && currentSlot==slot;
        public bool Admit(bool identityReady,Func<bool> equipmentAllowed)
        {
            if(!Required) return true;
            if(!ContractReady || !identityReady) return false;
            try { return equipmentAllowed(); } catch { return false; }
        }
    }
}
