namespace CraftopiaArchipelago
{
    // Main-thread state. Test approval is read once from persistent configuration
    // at startup; loss of a socket never disables enforcement. A conflicting
    // contract latches a hold until an explicit configuration review/restart.
    public sealed class EquipmentSession
    {
        private readonly bool approved;
        private readonly int guid;
        private readonly long epoch;
        private readonly string slot;
        private bool accepted, rejected;
        public bool Required { get; private set; }
        public bool ContractReady => !Required || (accepted && !rejected);

        public EquipmentSession(bool approved, int guid, long epoch, string slot)
        {
            this.approved = approved; this.guid = guid; this.epoch = epoch; this.slot = slot;
            Required = approved;
        }

        public bool Bind(long sessionEpoch, string sessionSlot, string catalog, int protocol, int multiplayer)
        {
            if (catalog != null || protocol != 0) Required = true;
            if (!Required) return true;
            accepted = false;
            if (rejected || !approved || guid == 0 || epoch <= 0 || string.IsNullOrWhiteSpace(slot)
                || sessionEpoch != epoch || sessionSlot != slot || catalog != EquipmentCatalog.CatalogId
                || protocol != 1 || multiplayer != 0)
            {
                rejected = true;
                return false;
            }
            accepted = true;
            return true;
        }

        public bool Ready(int currentGuid, long currentEpoch, string currentSlot, bool loadValidated)
            => Required && accepted && !rejected && loadValidated && currentGuid == guid
                && currentEpoch == epoch && currentSlot == slot;

        public bool Admit(bool identityReady, System.Func<bool> equipmentAllowed)
        {
            if (!Required) return true;
            if (!ContractReady || !identityReady) return false;
            try { return equipmentAllowed(); } catch { return false; }
        }
    }
}
