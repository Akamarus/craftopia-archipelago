// Pure, headless validation decision function for Craftopia Archipelago
// persistence.
//
// This decides, from explicit input snapshots, whether the skill-point
// validator may proceed: PASS (AP-aware totals match), RUN_VANILLA
// (explicitly verified no-history; delegate to original), or DEFER
// (preserve loaded values, block grants, require recovery).
//
// Rules:
// - Unknown inputs must defer. Freshness is never inferred from missing data.
// - AP-aware: allowed = (level-1)*K + mission + entitlement. Both discrepancy
//   directions (actual > allowed, actual < allowed) defer.
// - Every uncertain/held status defers: UncertainHistory, StoreUnreadable,
//   EpochMismatch, PendingIntent, Unknown.
// - Only an explicitly verified no-history state (NoRecordConfirmed) runs vanilla.
// - Identity is a separate status; GUID is signed Int32 (negatives valid).
//
// No Harmony, no Unity, no IO, no enrollment, no recovery, no bridge,
// no grant-pipeline integration. Pure function of its input.

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Outcome of the validation decision.
    /// </summary>
    public enum ValidationOutcome
    {
        /// <summary>
        /// Preserve loaded values, block grants/acks, require reviewed
        /// recovery. Do not run the validator.
        /// This is value 0 so default(ValidationResult) is safe (fail-closed).
        /// </summary>
        Defer = 0,

        /// <summary>
        /// AP-aware totals match. Skip the validator; grants may proceed
        /// (subject to other gates). No recovery needed.
        /// </summary>
        Pass,

        /// <summary>
        /// Explicitly verified no AP history. Run the original vanilla
        /// validator (it passes or resets per vanilla rules; both correct).
        /// </summary>
        RunVanilla,
    }

    /// <summary>
    /// Entitlement status from the store. Must be explicitly verified;
    /// never inferred from missing data.
    /// </summary>
    public enum EntitlementStatus
    {
        Unknown = 0,       // not determined → DEFER
        Ok,                // verified entitlement for (BoundGuid, BoundEpoch)
        NoRecordConfirmed, // explicitly verified fresh, no history → RUN_VANILLA
        UncertainHistory,  // → DEFER
        StoreUnreadable,   // → DEFER
        EpochMismatch,     // → DEFER
        PendingIntent,     // → DEFER
    }

    /// <summary>
    /// Identity status from read-only observation. Separate from entitlement.
    /// GUID is signed Int32; negative values are valid; 0 means unissued.
    /// </summary>
    public enum IdentityStatus
    {
        Unknown = 0,  // not observed → DEFER
        Verified,     // observed via pure reads (OcPl.GUID, _selectedCharacter.GUID)
    }

    /// <summary>
    /// Explicit input snapshot. All fields must be populated by the caller
    /// from verified observations; nothing is inferred here.
    /// </summary>
    public struct ValidationInput
    {
        // Game-observed totals (validator prefix).
        public int Level;
        public int K;               // INCREASE_SKILLPOINT_BY_LEVEL_UP, read live
        public int MissionPoints;    // GetTotalSkillPointTaken()
        public int AssignedPoints;   // CurrentAssignedSP
        public int UnspentPoints;    // SkillCtrl.SkillPoint
        // True only if ALL totals were read successfully. If false, the
        // numeric fields are unreliable and the decision must Defer.
        public bool TotalsKnown;

        // Identity (read-only observation).
        public IdentityStatus Identity;
        public int Guid;             // valid if Identity==Verified; signed
        public long Epoch;            // valid if Identity==Verified

        // Entitlement (store).
        public EntitlementStatus Entitlement;
        public int EntitlementPoints; // valid if Entitlement==Ok
        public int BoundGuid;         // valid if Entitlement==Ok
        public long BoundEpoch;        // valid if Entitlement==Ok
    }

    public struct ValidationResult
    {
        public ValidationOutcome Outcome;
        public string Reason;
        public int VanillaAllowed; // (level-1)*K + mission
        public int Actual;         // assigned + unspent
        public int ApAllowed;      // vanillaAllowed + entitlement (if Ok & bound)
        public bool ApAllowedValid;
    }

    public static class ValidationDecision
    {
        public static ValidationResult Decide(ValidationInput input)
        {
            int vanillaAllowed = (input.Level - 1) * input.K + input.MissionPoints;
            int actual = input.AssignedPoints + input.UnspentPoints;

            // Unknown totals must defer. Do not compute with substituted defaults.
            if (!input.TotalsKnown)
            {
                return new ValidationResult
                {
                    Outcome = ValidationOutcome.Defer,
                    Reason = "totals not known",
                    VanillaAllowed = vanillaAllowed,
                    Actual = actual,
                    ApAllowedValid = false,
                };
            }

            // Unknown inputs must defer. Do not infer freshness from missing data.
            if (input.Identity != IdentityStatus.Verified)
            {
                return new ValidationResult
                {
                    Outcome = ValidationOutcome.Defer,
                    Reason = "identity not verified",
                    VanillaAllowed = vanillaAllowed,
                    Actual = actual,
                    ApAllowedValid = false,
                };
            }

            switch (input.Entitlement)
            {
                case EntitlementStatus.Ok:
                    // Binding: entitlement must be for this (guid, epoch).
                    if (input.Guid != input.BoundGuid || input.Epoch != input.BoundEpoch)
                    {
                        return new ValidationResult
                        {
                            Outcome = ValidationOutcome.Defer,
                            Reason = "entitlement binding mismatch",
                            VanillaAllowed = vanillaAllowed,
                            Actual = actual,
                            ApAllowed = vanillaAllowed + input.EntitlementPoints,
                            ApAllowedValid = true,
                        };
                    }
                    int apAllowed = vanillaAllowed + input.EntitlementPoints;
                    if (actual == apAllowed)
                    {
                        return new ValidationResult
                        {
                            Outcome = ValidationOutcome.Pass,
                            Reason = "AP-aware totals match",
                            VanillaAllowed = vanillaAllowed,
                            Actual = actual,
                            ApAllowed = apAllowed,
                            ApAllowedValid = true,
                        };
                    }
                    // Both discrepancy directions defer.
                    return new ValidationResult
                    {
                        Outcome = ValidationOutcome.Defer,
                        Reason = "AP discrepancy: actual=" + actual + " apAllowed=" + apAllowed,
                        VanillaAllowed = vanillaAllowed,
                        Actual = actual,
                        ApAllowed = apAllowed,
                        ApAllowedValid = true,
                    };

                case EntitlementStatus.NoRecordConfirmed:
                    return new ValidationResult
                    {
                        Outcome = ValidationOutcome.RunVanilla,
                        Reason = "verified no-history",
                        VanillaAllowed = vanillaAllowed,
                        Actual = actual,
                        ApAllowedValid = false,
                    };

                case EntitlementStatus.UncertainHistory:
                case EntitlementStatus.StoreUnreadable:
                case EntitlementStatus.EpochMismatch:
                case EntitlementStatus.PendingIntent:
                case EntitlementStatus.Unknown:
                default:
                    return new ValidationResult
                    {
                        Outcome = ValidationOutcome.Defer,
                        Reason = "entitlement status=" + input.Entitlement,
                        VanillaAllowed = vanillaAllowed,
                        Actual = actual,
                        ApAllowedValid = false,
                    };
            }
        }
    }
}
