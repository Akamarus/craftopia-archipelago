using System;
namespace CraftopiaArchipelago
{
    public static class SoulOrbReplacement
    {
        // Return whether the original game's reward method may run.
        public static bool Collect(bool bound,bool selected,bool historical,bool taken,bool eligible,Func<bool> persist,Action finish) {
            if(!bound || !selected || historical) return true;
            if(taken || !eligible) return false;
            try {
                if(persist==null || finish==null || !persist()) return false;
                finish();
            } catch { /* Once selected, an error must never restore vanilla currency. */ }
            return false;
        }
    }
}
