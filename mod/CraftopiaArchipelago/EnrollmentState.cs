// Atomic enrollment state: single-file durable enrollment authority.
//
// Replaces the two-file (attestation + entitlement) design which had a
// non-atomic transition: Commit() could succeed while MarkConsumed() failed,
// leaving a usable attestation after entitlement existed.
//
// This design:
// - Single file: one atomic write per state transition.
// - Phases: None -> Attested -> EntitlementCommitted (consumed).
// - Attested: valid attestation, no entitlement yet. Read -> NoRecordConfirmed.
// - EntitlementCommitted: entitlement exists, attestation consumed.
//   Read -> Ok(points). Lost file -> UncertainHistory (HOLD), never fresh.
// - No separate consumption write; it's part of the commit.
//
// File format:
// {
//   "schema": 2,
//   "enrollments": {
//     "123": {
//       "epoch": 456,
//       "slot": "TestPlayer",
//       "character_name": "...",
//       "attested_at": "...",
//       "phase": "attested" | "committed",
//       "points": 0,
//       "consumed_at": "..." | null
//     }
//   }
// }

using System;
using System.Collections.Generic;
using System.IO;

namespace CraftopiaArchipelago
{
    public enum EnrollmentPhase
    {
        None,
        Attested,      // Valid attestation, no entitlement yet
        Committed,     // Entitlement committed, attestation consumed
    }

    public struct EnrollmentRecord
    {
        public long Epoch;
        public string Slot;
        public string CharacterName;
        public string AttestedAt;
        public EnrollmentPhase Phase;
        public int Points;
        public string ConsumedAt;
        // Durable per-operation deduplication: grant indices already committed.
        // A retry of the same index does not re-increment; a new index does.
        public List<long> CommittedIndices;
    }

    public class EnrollmentState
    {
        private readonly string path;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;

        private Dictionary<int, EnrollmentRecord> enrollments;
        private bool loaded;
        private bool loadFailed;

        public EnrollmentState(string path, Action<string> logInfo, Action<string> logError)
        {
            this.path = path;
            this.logInfo = logInfo;
            this.logError = logError;
            this.enrollments = new Dictionary<int, EnrollmentRecord>();
            this.loaded = false;
            this.loadFailed = false;
        }

        public void Load()
        {
            loaded = false;
            loadFailed = false;
            enrollments = new Dictionary<int, EnrollmentRecord>();
            try
            {
                if (!File.Exists(path))
                {
                    logInfo("[AP] no enrollment state file (no enrollments yet)");
                    loaded = true;
                    return;
                }
                string json = File.ReadAllText(path);
                var data = MiniJson.ParseObject(json);
                if (data == null) throw new FormatException("not a JSON object");
                if (!data.TryGetValue("schema", out object s) || !(s is long schema) || schema != 2)
                    throw new FormatException("unsupported schema");
                var enr = data.TryGetValue("enrollments", out object e) ?
                    e as Dictionary<string, object> : null;
                if (enr == null) throw new FormatException("missing enrollments");
                enrollments.Clear();
                foreach (var kv in enr)
                {
                    if (!int.TryParse(kv.Key, out int guid) || guid == 0)
                        throw new FormatException("invalid guid");
                    var rec = kv.Value as Dictionary<string, object>;
                    if (rec == null) throw new FormatException("invalid record");
                    var r = new EnrollmentRecord();
                    if (!rec.TryGetValue("epoch", out object ep) || !(ep is long epoch) || epoch <= 0)
                        throw new FormatException("invalid epoch");
                    r.Epoch = epoch;
                    r.Slot = rec.TryGetValue("slot", out object sl) ? sl as string : null;
                    if (string.IsNullOrWhiteSpace(r.Slot)) throw new FormatException("invalid slot");
                    r.CharacterName = rec.TryGetValue("character_name", out object cn) ? cn as string : null;
                    r.AttestedAt = rec.TryGetValue("attested_at", out object at) ? at as string : null;
                    r.ConsumedAt = rec.TryGetValue("consumed_at", out object ca) ? ca as string : null;
                    string phaseStr = rec.TryGetValue("phase", out object ph) ? ph as string : null;
                    if (phaseStr == "committed") r.Phase = EnrollmentPhase.Committed;
                    else if (phaseStr == "attested") r.Phase = EnrollmentPhase.Attested;
                    else throw new FormatException("invalid phase: " + phaseStr);
                    // Points: must be present, must be long, must be 0 <= p <= int.MaxValue.
                    // Malformed -> throw, do NOT default to zero.
                    if (!rec.TryGetValue("points", out object p))
                        throw new FormatException("missing points");
                    if (!(p is long pts))
                        throw new FormatException("points not an integer");
                    if (pts < 0 || pts > int.MaxValue)
                        throw new FormatException("points out of range");
                    r.Points = (int)pts;
                    // Phase-consistent invariants.
                    if (r.Phase == EnrollmentPhase.Attested && r.Points != 0)
                        throw new FormatException("attested phase must have zero points");
                    // Committed indices: must be present if committed.
                    if (r.Phase == EnrollmentPhase.Committed)
                    {
                        var idxList = rec.TryGetValue("committed_indices", out object ci) ?
                            ci as System.Collections.IList : null;
                        if (idxList == null) throw new FormatException("missing committed_indices");
                        r.CommittedIndices = new List<long>();
                        foreach (var idx in idxList)
                        {
                            if (!(idx is long l) || l < 0 || r.CommittedIndices.Contains(l))
                                throw new FormatException("invalid or duplicate index");
                            r.CommittedIndices.Add(l);
                        }
                        if (r.CommittedIndices.Count == 0) throw new FormatException("committed record has no indices");
                    }
                    else if (rec.TryGetValue("committed_indices", out object pendingIndices) &&
                        (!(pendingIndices is System.Collections.IList pendingList) || pendingList.Count != 0))
                        throw new FormatException("attested record has grant history");
                    enrollments[guid] = r;
                }
                loaded = true;
                logInfo($"[AP] loaded {enrollments.Count} enrollment record(s)");
            }
            catch (Exception e)
            {
                loadFailed = true;
                logError("[AP] enrollment state load failed: " + e.GetType().Name);
            }
        }

        // Validation must use this status-bearing read, never GetPhase(None) as
        // permission to consult another authority. Missing/corrupt/mismatched state holds.
        public EntitlementRead Read(int guid, long epoch, string slot, bool hasHistory)
        {
            if (loadFailed) return Result(EntitlementStatus.StoreUnreadable, "enrollment load failed");
            if (!loaded || guid == 0 || epoch <= 0 || string.IsNullOrWhiteSpace(slot))
                return Result(EntitlementStatus.Unknown, "enrollment or identity not ready");
            if (!enrollments.TryGetValue(guid, out var rec))
                return Result(EntitlementStatus.UncertainHistory, "enrollment authority has no record");
            if (rec.Epoch != epoch || rec.Slot != slot)
                return Result(EntitlementStatus.EpochMismatch, "enrollment session/slot mismatch");
            if (rec.Phase == EnrollmentPhase.Attested && hasHistory)
                return Result(EntitlementStatus.UncertainHistory, "attested enrollment conflicts with grant history");
            if (rec.Phase != EnrollmentPhase.Attested && rec.Phase != EnrollmentPhase.Committed)
                return Result(EntitlementStatus.StoreUnreadable, "invalid enrollment phase");
            return new EntitlementRead {
                Status = rec.Phase == EnrollmentPhase.Attested ? EntitlementStatus.NoRecordConfirmed : EntitlementStatus.Ok,
                Points = rec.Points, BoundGuid = guid, BoundEpoch = epoch,
                Detail = rec.Phase == EnrollmentPhase.Attested ? "verified fresh via atomic enrollment" : "enrolled and committed"
            };
        }

        private static EntitlementRead Result(EntitlementStatus status, string detail)
            => new EntitlementRead { Status = status, Detail = detail };

        // Legacy-only operation is explicit: once an atomic authority is configured,
        // its failure cannot be masked by old state. Migration is a separate procedure.
        public static EntitlementRead Resolve(EnrollmentState authority, EntitlementStore legacy,
            int guid, long epoch, string slot, bool enrolled, bool hasHistory)
        {
            if (authority != null) return authority.Read(guid, epoch, slot, hasHistory);
            return legacy != null ? legacy.ReadWithSlot(guid, epoch, slot, enrolled, hasHistory)
                : Result(EntitlementStatus.Unknown, "no entitlement authority configured");
        }

        /// <summary>
        /// Get the enrollment phase for (guid, epoch, slot).
        /// Returns None if no record, wrong epoch/slot, or load failed.
        /// </summary>
        public EnrollmentPhase GetPhase(int guid, long epoch, string slot)
        {
            if (!loaded || loadFailed) return EnrollmentPhase.None;
            if (enrollments.TryGetValue(guid, out var rec))
            {
                if (rec.Epoch == epoch && rec.Slot == slot)
                    return rec.Phase;
            }
            return EnrollmentPhase.None;
        }

        /// <summary>
        /// Get points for a committed enrollment. Returns -1 if not committed.
        /// </summary>
        public int GetPoints(int guid, long epoch, string slot)
        {
            if (GetPhase(guid, epoch, slot) == EnrollmentPhase.Committed &&
                enrollments.TryGetValue(guid, out var rec))
                return rec.Points;
            return -1;
        }

        /// <summary>
        /// Attest: create a new enrollment record in Attested phase.
        /// Requires explicit user approval (caller must have obtained it).
        /// Fails if a record already exists (never overwrite history).
        /// </summary>
        public bool Attest(int guid, long epoch, string slot, string characterName, string note)
        {
            if (!loaded || loadFailed || guid == 0 || epoch <= 0 || string.IsNullOrEmpty(slot))
                return false;
            if (enrollments.ContainsKey(guid))
            {
                logError($"[AP] attest refused: enrollment already exists for guid={guid}");
                return false; // Never overwrite existing history.
            }
            try
            {
                var newEnr = new Dictionary<int, EnrollmentRecord>(enrollments);
                newEnr[guid] = new EnrollmentRecord
                {
                    Epoch = epoch,
                    Slot = slot,
                    CharacterName = characterName,
                    AttestedAt = DateTime.UtcNow.ToString("o"),
                    Phase = EnrollmentPhase.Attested,
                    Points = 0,
                    ConsumedAt = null,
                };
                if (!Save(newEnr)) return false;
                enrollments = newEnr;
                logInfo($"[AP] enrollment attested: guid={guid}, epoch={epoch}, slot={slot}");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] attest failed: " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Commit entitlement for a specific grant index.
        /// ATOMIC: updates points, phase, and committed indices in ONE write.
        ///
        /// - If grantIndex already committed: idempotent retry, returns true
        ///   WITHOUT re-incrementing points.
        /// - If grantIndex is new: increments points, records index, returns true.
        /// - If write fails: state unchanged, returns false; caller must NOT ack.
        ///
        /// The grantIndex distinguishes a retry (same index) from a new grant
        /// (different index). Without this, all later rewards would be silently
        /// omitted after the first commit.
        /// </summary>
        public bool CommitEntitlement(int guid, long epoch, string slot, int pointsToAdd, long grantIndex)
        {
            if (!loaded || loadFailed || guid == 0 || epoch <= 0 || pointsToAdd < 0 || grantIndex < 0)
                return false;
            if (!enrollments.TryGetValue(guid, out var rec))
            {
                logError($"[AP] commit refused: no enrollment for guid={guid}");
                return false;
            }
            if (rec.Epoch != epoch || rec.Slot != slot)
            {
                logError($"[AP] commit refused: epoch/slot mismatch for guid={guid}");
                return false;
            }
            if (rec.Phase != EnrollmentPhase.Attested && rec.Phase != EnrollmentPhase.Committed)
            {
                logError($"[AP] commit refused: invalid phase for guid={guid}");
                return false;
            }
            // Check if this grant index was already committed (retry vs new).
            if (rec.CommittedIndices != null && rec.CommittedIndices.Contains(grantIndex))
            {
                logInfo($"[AP] commit retry: index {grantIndex} already committed for guid={guid}");
                return true; // Idempotent, no re-increment.
            }
            try
            {
                var newEnr = new Dictionary<int, EnrollmentRecord>(enrollments);
                // The struct and dictionary copies otherwise share this mutable list.
                // Failed writes must leave every part of the published state unchanged.
                rec.CommittedIndices = rec.CommittedIndices == null
                    ? new List<long>() : new List<long>(rec.CommittedIndices);
                rec.Points = checked(rec.Points + pointsToAdd);
                rec.CommittedIndices.Add(grantIndex);
                // First commit transitions Attested -> Committed (consumes attestation).
                if (rec.Phase == EnrollmentPhase.Attested)
                {
                    rec.Phase = EnrollmentPhase.Committed;
                    rec.ConsumedAt = DateTime.UtcNow.ToString("o");
                }
                newEnr[guid] = rec;
                // SINGLE ATOMIC WRITE.
                if (!Save(newEnr)) return false;
                enrollments = newEnr;
                logInfo($"[AP] entitlement committed: guid={guid}, index={grantIndex}, points={rec.Points}");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] commit failed: " + e.GetType().Name);
                return false;
            }
        }

        private bool Save(Dictionary<int, EnrollmentRecord> state)
        {
            try
            {
                var data = new Dictionary<string, object> { { "schema", 2L } };
                var enr = new Dictionary<string, object>();
                foreach (var kv in state)
                {
                    enr[kv.Key.ToString()] = new Dictionary<string, object>
                    {
                        { "epoch", kv.Value.Epoch },
                        { "slot", kv.Value.Slot },
                        { "character_name", kv.Value.CharacterName },
                        { "attested_at", kv.Value.AttestedAt },
                        { "phase", kv.Value.Phase == EnrollmentPhase.Committed ? "committed" : "attested" },
                        { "points", (long)kv.Value.Points },
                        { "consumed_at", kv.Value.ConsumedAt },
                        { "committed_indices", kv.Value.CommittedIndices?.ConvertAll(x => (object)x) ?? new List<object>() },
                    };
                }
                data["enrollments"] = enr;
                string json = MiniJson.Stringify(data);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path))
                {
                    string backup = path + ".bak";
                    try { File.Replace(tmp, path, backup, true); }
                    catch { File.Move(tmp, path); }
                    try { if (File.Exists(backup)) File.Delete(backup); } catch { }
                }
                else File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] enrollment save failed: " + e.GetType().Name);
                return false;
            }
        }
    }
}
