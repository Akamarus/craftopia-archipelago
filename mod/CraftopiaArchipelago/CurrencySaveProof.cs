using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // Immutable snapshot from a character-save invocation that began AFTER mutation.
    // The caller supplies the exact character path to the read-only database matcher.
    public sealed class CurrencySaveProof
    {
        public static bool SameSavePath(string first,string second) {
            try {
                if(string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second) ||
                    !System.IO.Path.IsPathRooted(first) || !System.IO.Path.IsPathRooted(second)) return false;
                return string.Equals(System.IO.Path.GetFullPath(first),System.IO.Path.GetFullPath(second),StringComparison.OrdinalIgnoreCase);
            } catch { return false; }
        }
        readonly string token, player, inventory;
        readonly int guid;
        CurrencySaveProof(string token,int guid,string player,string inventory) {
            this.token=token; this.guid=guid; this.player=player; this.inventory=inventory;
        }
        public static CurrencySaveProof Create(string token,int guid,int expectedCount,string player,string inventory) {
            try {
                if(string.IsNullOrWhiteSpace(token) || guid==0 || expectedCount<1) return null;
                var p=MiniJson.ParseObject(player);
                if(p==null || !p.TryGetValue("GUID",out var g) || !(g is long id) || id!=guid) return null;
                var d=MiniJson.ParseObject(inventory);
                if(d==null || !d.TryGetValue("inventorySaveData",out var inv) || !(inv is Dictionary<string,object> data) ||
                    !data.TryGetValue("importantList",out var items) || !(items is List<object> boxes)) return null;
                long count=0;
                foreach(var box in boxes) {
                    if(!(box is Dictionary<string,object> b) || !b.TryGetValue("itemInBox",out var rows) || !(rows is List<object> list)) return null;
                    foreach(var row in list) {
                        if(!(row is Dictionary<string,object> r) || !r.TryGetValue("item",out var item) || !(item is Dictionary<string,object> detail) ||
                            !detail.TryGetValue("itemId",out var rawId) || !(rawId is long itemId) ||
                            !r.TryGetValue("count",out var rawCount) || !(rawCount is long n) || n<0 || n>int.MaxValue) return null;
                        if(itemId==3840) count=checked(count+n);
                    }
                }
                if(count!=expectedCount) return null;
                return new CurrencySaveProof(token,guid,player,inventory);
            } catch { return null; }
        }
        public bool Matches(string activeToken,int activeGuid,Func<string,string,bool> readOnlyMatch) {
            if(token!=activeToken || guid!=activeGuid || readOnlyMatch==null) return false;
            try { return readOnlyMatch(player,inventory); } catch { return false; }
        }
    }
}
