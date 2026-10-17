using System;

namespace CraftopiaArchipelago
{
    public static class CurrencyInventoryVerifier
    {
        // insertWithoutDrop returns the stack remainder. The adapter must NEVER
        // use the normal TryTakeItem overload, which can spill overflow to ground.
        public static CurrencyMutation Apply(int amount,Func<int> count,Func<int,int> insertWithoutDrop) {
            if(amount<1 || amount>999 || count==null || insertWithoutDrop==null) return CurrencyMutation.NoMutation;
            int before;
            try { before=count(); if(before<0 || before>int.MaxValue-amount) return CurrencyMutation.NoMutation; }
            catch { return CurrencyMutation.NoMutation; }
            try {
                int remaining=insertWithoutDrop(amount), after=count();
                if(remaining==amount && after==before) return CurrencyMutation.NoMutation;
                if(remaining==0 && after==before+amount) return CurrencyMutation.Applied;
                return CurrencyMutation.Ambiguous;
            } catch { return CurrencyMutation.Ambiguous; }
        }
    }
}
