using System;

namespace CraftopiaArchipelago.Diagnostics
{
    internal sealed class ProbeSession
    {
        private Guid pending;
        public void Arm(Guid nonce)
        {
            if (nonce == Guid.Empty) throw new ArgumentException("Nonce must not be empty.");
            pending = nonce;
        }
        public void Reset() => pending = Guid.Empty;
        public bool AcceptEcho(Guid nonce)
        {
            if (pending == Guid.Empty || nonce != pending) return false;
            Reset();
            return true;
        }
    }
}
