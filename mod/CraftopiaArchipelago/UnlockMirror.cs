using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // Main-thread-only mirror of the BRIDGE's durable slot/epoch unlock history.
    // An ack confirms this mirror accepted a receipt. It never deletes ownership
    // history at the bridge. A fresh socket receives the entire history again.
    internal sealed class UnlockMirror
    {
        private readonly PermissionManager permissions;
        private readonly Dictionary<long, Dictionary<string, object>> records = new Dictionary<long, Dictionary<string, object>>();
        private readonly HashSet<long> pending = new HashSet<long>();
        private string slot, token;
        private long epoch;
        private int connection;
        private bool held, visible, exposed;

        internal UnlockMirror(PermissionManager permissions) { this.permissions = permissions; }
        internal bool BeginSession(string slot, long epoch, string token, int connection)
        {
            if (!Text(slot) || !Text(token) || epoch <= 0 || connection <= 0) return false;
            if (this.slot == slot && this.epoch == epoch && this.token == token && this.connection == connection)
                return !held;
            Suspend(); records.Clear(); pending.Clear(); held = false;
            this.slot = slot; this.epoch = epoch; this.token = token; this.connection = connection;
            return true;
        }
        internal void Suspend()
        {
            visible = false;
            if (!exposed) return;
            exposed = false;
            try { permissions.Clear(); } catch { }
        }
        private static bool Text(string value) { return !string.IsNullOrWhiteSpace(value) && value == value.Trim(); }
        private static bool Long(Dictionary<string,object> d, string key, out long value)
        {
            object raw; value=0;
            if (!d.TryGetValue(key,out raw) || !(raw is long)) return false;
            value=(long)raw; return true;
        }
        internal bool Accept(Dictionary<string,object> msg, int sourceConnection)
        {
            long e, receipt;
            if (held || token == null || sourceConnection != connection ||
                !Long(msg,"epoch",out e) || e != epoch ||
                MiniJson.GetString(msg,"slot") != slot || MiniJson.GetString(msg,"connection_token") != token ||
                !Long(msg,"receipt_id",out receipt) || receipt < -7) return false;
            string cmd=MiniJson.GetString(msg,"cmd");
            var record=new Dictionary<string,object> { ["cmd"]=cmd, ["receipt_id"]=receipt };
            if (cmd=="unlock_equipment")
            {
                long tier; string category=MiniJson.GetString(msg,"category");
                if (!Text(category) || !Long(msg,"tier",out tier) || tier < 1 || tier > int.MaxValue) return false;
                record["category"]=category; record["tier"]=tier;
            }
            else if (cmd=="unlock_production" || cmd=="unlock_skill_tree")
            {
                string key=cmd=="unlock_production" ? "station_id" : "tree_id";
                string value=MiniJson.GetString(msg,key);
                if (!Text(value) || receipt < 0) return false;
                record[key]=value;
            }
            else return false;
            Dictionary<string,object> previous;
            if (records.TryGetValue(receipt,out previous))
            {
                if (MiniJson.Stringify(previous) != MiniJson.Stringify(record))
                { held=true; Suspend(); return false; }
            }
            else { records.Add(receipt,record); visible=false; }
            pending.Add(receipt);
            return true;
        }
        internal void Flush(bool eligible, int currentConnection, Action<Dictionary<string,object>> ack)
        {
            if (!eligible || held || currentConnection != connection || token == null)
            { Suspend(); return; }
            if (!visible)
            {
                try
                {
                    exposed=true;
                    permissions.Clear();
                    foreach (var record in records.Values)
                    {
                        switch ((string)record["cmd"])
                        {
                            case "unlock_equipment": permissions.UnlockEquipmentTier((string)record["category"],(int)(long)record["tier"]); break;
                            case "unlock_production": permissions.UnlockProduction((string)record["station_id"]); break;
                            case "unlock_skill_tree": permissions.UnlockSkillTree((string)record["tree_id"]); break;
                        }
                    }
                    visible=true;
                }
                catch { Suspend(); return; }
            }
            foreach (long receipt in new List<long>(pending))
            {
                try
                {
                    ack(new Dictionary<string,object> { ["cmd"]="unlock_ack", ["receipt_id"]=receipt,
                        ["epoch"]=epoch, ["slot"]=slot, ["connection_token"]=token });
                    pending.Remove(receipt);
                }
                catch { return; }
            }
        }
    }
}
