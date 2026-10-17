namespace CraftopiaArchipelago
{
    public static class MilestoneDecision
    {
        public static bool Accept(string family,int before,int after,bool successfulAction,bool loadContext,bool sameWorld)
        {
            if(!successfulAction || loadContext || !sameWorld || before<0 || before>=int.MaxValue || after!=before+1) return false;
            if(family=="category" || family=="dungeon") return before==0 && after==1;
            return family=="tower" || family=="age";
        }
    }
}
