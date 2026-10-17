using System;
using System.Collections.Generic;
namespace CraftopiaArchipelago {
    public static class MilestoneBaseline {
        public static bool Accept(IEnumerable<string> selected,Func<string,int?> read) {
            try {
                bool any=false;
                foreach(string key in selected) { any=true;if(read(key)!=0)return false; }
                return any;
            } catch { return false; }
        }
    }
}
