using System;
using System.Collections.Generic;
namespace CraftopiaArchipelago {
    public sealed class FreshWorldBaseline {
        readonly object world;
        readonly long created;
        readonly int worldSeed, character;
        public FreshWorldBaseline(object world,long created,int worldSeed,int character) {
            this.world=world;this.created=created;this.worldSeed=worldSeed;this.character=character;
        }
        public bool Matches(object current,long currentCreated,int currentSeed,int currentCharacter,int age,bool milestonesFresh)
            => world!=null && created>0 && character!=0 && ReferenceEquals(world,current) &&
                created==currentCreated && worldSeed==currentSeed && character==currentCharacter && age==0 && milestonesFresh;
    }
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
