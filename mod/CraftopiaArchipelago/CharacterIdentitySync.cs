using System;
namespace CraftopiaArchipelago
{
    // Main-thread announcement state. Commit only after the packet is queued.
    internal sealed class CharacterIdentitySync
    {
        private int connection, guid;
        private long epoch;
        private string slot;
        internal bool TryQueue(int nextConnection,int nextGuid,long nextEpoch,string nextSlot,Action enqueue)
        {
            if(nextConnection<=0 || nextGuid==0 || nextEpoch<=0 || string.IsNullOrWhiteSpace(nextSlot))return false;
            if(connection==nextConnection && guid==nextGuid && epoch==nextEpoch && slot==nextSlot)return false;
            enqueue();
            connection=nextConnection;guid=nextGuid;epoch=nextEpoch;slot=nextSlot;
            return true;
        }
    }
}
