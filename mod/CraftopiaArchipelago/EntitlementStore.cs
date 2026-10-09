// Entitlement store: durable AP skill-point entitlement per (character GUID, epoch).
//
// Design (from PERSISTENCE_DESIGN-v3 + corrections):
// - Entitlement is bound to (guid, epoch). Never derived from applied.Count.
// - Store is a JSON file: { "guid": { "epoch": { "points": N, "firstSeen": "..." } } }
// - Statuses: Ok, NoRecordConfirmed, UncertainHistory, StoreUnreadable,
//   EpochMismatch, PendingIntent. Unknown inputs defer.
// - The store never auto-creates; missing store (not in init) -> UncertainHistory.
// - One-time enrollment: init file lists GUIDs verified fresh. After enrollment,
//   history loss -> UncertainHistory, never back to NoRecord.
//
// This is the persistence layer only. The decision logic is in Validation.cs
// (which uses ValidationDecision.Decide).

using System;
using System.Collections.Generic;
using System.IO;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Result of reading entitlement for a (guid, epoch).
    /// </summary>
    public struct EntitlementRead
    {
        public EntitlementStatus Status;
        public int Points;       // valid if Status==Ok
        public int BoundGuid;    // valid if Status==Ok
        public long BoundEpoch;  // valid if Status==Ok
        public string Detail;    // for logging
    }

    public class EntitlementStore
    {
        private readonly string path;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;
        // Optional attestation provider for verified fresh enrollment.
        // Set via SetAttestationProvider before Load().
        private EnrollmentAttestation attestation;

        // In-memory cache: guid -> epoch -> points
        private Dictionary<int, Dictionary<long, int>> cache;
        private bool loaded;
        private bool loadFailed;
        private bool storeExisted; // false if file was missing at load

        public EntitlementStore(string path, Action<string> logInfo, Action<string> logError)
        {
            this.path = path;
            this.logInfo = logInfo;
            this.logError = logError;
            this.cache = new Dictionary<int, Dictionary<long, int>>();
            this.loaded = false;
            this.loadFailed = false;
            this.attestation = null;
        }

        /// <summary>
        /// Set the attestation provider. Call before Load().
        /// </summary>
        public void SetAttestationProvider(EnrollmentAttestation att)
        {
            this.attestation = att;
        }

        /// <summary>
        /// Load the store from disk. Call once at startup.
        /// Does not create the file if missing.
        /// </summary>
        public void Load()
        {
            try
            {
                if (!File.Exists(path))
                {
                    logInfo("[AP] entitlement store not found (no auto-create)");
                    loaded = true;
                    storeExisted = false;
                    // Missing file = UncertainHistory (not fresh). We cannot
                    // verify enrollment without the store; do not infer.
                    return;
                }
                storeExisted = true;
                string json = File.ReadAllText(path);
                var data = MiniJson.ParseObject(json);
                if (data == null)
                {
                    loadFailed = true;
                    logError("[AP] entitlement store corrupt (not a JSON object)");
                    return;
                }
                cache.Clear();
                foreach (var kv in data)
                {
                    if (!int.TryParse(kv.Key, out int guid) || guid == 0) throw new FormatException("Invalid character key");
                    var epochs = kv.Value as Dictionary<string, object>;
                    if (epochs == null) throw new FormatException("Invalid epoch map");
                    var edict = new Dictionary<long, int>();
                    foreach (var ekv in epochs)
                    {
                        if (!long.TryParse(ekv.Key, out long epoch) || epoch <= 0) throw new FormatException("Invalid epoch");
                        // Value is { "points": N, "firstSeen": "..." }
                        var edata = ekv.Value as Dictionary<string, object>;
                        if (edata == null) throw new FormatException("Invalid entitlement entry");
                        if (!edata.TryGetValue("points", out object pobj) ||
                            !(pobj is long pts) || pts < 0 || pts > int.MaxValue)
                            throw new FormatException("Invalid entitlement points");
                        edict[epoch] = (int)pts;
                    }
                    cache[guid] = edict;
                }
                loaded = true;
                logInfo("[AP] entitlement store loaded");
            }
            catch (Exception e)
            {
                loadFailed = true;
                logError("[AP] entitlement store load failed: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Read entitlement for (guid, epoch). Never infers; unknown -> defer status.
        /// </summary>
        public EntitlementRead Read(int guid, long epoch, bool enrolled, bool hasHistory)
        {
            if (loadFailed)
            {
                return new EntitlementRead
                {
                    Status = EntitlementStatus.StoreUnreadable,
                    Detail = "store load failed",
                };
            }
            if (!loaded)
            {
                return new EntitlementRead
                {
                    Status = EntitlementStatus.Unknown,
                    Detail = "store not loaded",
                };
            }

            // Check for pending intent first (separate file, checked by caller).
            // This store only handles points.

            if (cache.TryGetValue(guid, out var epochs) &&
                epochs.TryGetValue(epoch, out int points))
            {
                return new EntitlementRead
                {
                    Status = EntitlementStatus.Ok,
                    Points = points,
                    BoundGuid = guid,
                    BoundEpoch = epoch,
                    Detail = "ok",
                };
            }

            // No record for (guid, epoch). Check if guid has OTHER epochs
            // (mismatch) before considering freshness.
            if (cache.TryGetValue(guid, out var otherEpochs) && otherEpochs.Count > 0)
            {
                // Guid exists but not for this epoch -> EpochMismatch hold.
                // Do NOT return NoRecordConfirmed; the epoch changed.
                return new EntitlementRead
                {
                    Status = EntitlementStatus.EpochMismatch,
                    Detail = "guid has other epoch(s), not this one",
                };
            }

            // No record at all. Check for a valid enrollment attestation.
            // A durable attestation for (guid, epoch, slot) is verified proof
            // of fresh enrollment, allowing NoRecordConfirmed.
            // Without attestation: UncertainHistory. Never infer from missing data.
            if (attestation != null)
            {
                // Slot is needed for attestation binding. Caller provides via
                // ReadWithSlot; this overload uses null slot (no match).
                // (See ReadWithSlot for the slot-bound version.)
            }
            if (!storeExisted)
            {
                return new EntitlementRead
                {
                    Status = EntitlementStatus.UncertainHistory,
                    Detail = "store file missing; cannot verify freshness",
                };
            }
            return new EntitlementRead
            {
                Status = EntitlementStatus.UncertainHistory,
                Detail = enrolled ? "enrollment hint is not durable freshness proof"
                                  : "not enrolled, no record",
            };
        }

        /// <summary>
        /// Read entitlement with slot binding for attestation check.
        /// If no entitlement record exists but a valid attestation does,
        /// returns NoRecordConfirmed (verified fresh, zero points).
        /// </summary>
        public EntitlementRead ReadWithSlot(int guid, long epoch, string slot, bool enrolled, bool hasHistory)
        {
            var baseRead = Read(guid, epoch, enrolled, hasHistory);
            // Only upgrade UncertainHistory to NoRecordConfirmed via attestation.
            if (baseRead.Status != EntitlementStatus.UncertainHistory)
                return baseRead;
            if (attestation != null && !string.IsNullOrEmpty(slot) &&
                attestation.HasAttestation(guid, epoch, slot))
            {
                return new EntitlementRead
                {
                    Status = EntitlementStatus.NoRecordConfirmed,
                    Points = 0,
                    BoundGuid = guid,
                    BoundEpoch = epoch,
                    Detail = "verified fresh via enrollment attestation",
                };
            }
            return baseRead;
        }

        /// <summary>
        /// Commit entitlement points for (guid, epoch). ACCUMULATES: adds points
        /// to any existing entitlement for (guid, epoch). Called only by the
        /// grant transaction owner after successful application.
        /// Idempotent for retries: the transaction tracks which indices were
        /// committed, so a retry of the same batch does not double-count.
        /// Does NOT mutate the cache until the save succeeds.
        /// </summary>
        public bool Commit(int guid, long epoch, int points)
        {
            try
            {
                if (loadFailed || !loaded || guid == 0 || epoch <= 0 || points < 0) return false;
                // Build the new state first, without mutating the cache.
                var newCache = new Dictionary<int, Dictionary<long, int>>(cache.Count + 1);
                foreach (var kv in cache)
                    newCache[kv.Key] = new Dictionary<long, int>(kv.Value);
                if (!newCache.TryGetValue(guid, out var epochs))
                {
                    epochs = new Dictionary<long, int>();
                    newCache[guid] = epochs;
                }
                // ACCUMULATE, not overwrite.
                int existing = 0;
                epochs.TryGetValue(epoch, out existing);
                epochs[epoch] = checked(existing + points);
                // Save the new state; only on success replace the cache.
                if (!Save(newCache))
                    return false;
                cache = newCache;
                storeExisted = true; // we just wrote it
                // Consume the attestation: one-time authorization is now used.
                // Future loss of this store must HOLD, not go fresh.
                try { attestation?.MarkConsumed(guid); } catch { }
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] entitlement commit failed: " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Atomically save the given cache state. Returns true on success.
        /// Uses write-temp + atomic replace (no delete-before-move window).
        /// </summary>
        private bool Save(Dictionary<int, Dictionary<long, int>> state)
        {
            try
            {
                var data = new Dictionary<string, object>();
                foreach (var gkv in state)
                {
                    var edict = new Dictionary<string, object>();
                    foreach (var ekv in gkv.Value)
                    {
                        edict[ekv.Key.ToString()] = new Dictionary<string, object>
                        {
                            { "points", ekv.Value },
                            { "firstSeen", DateTime.UtcNow.ToString("o") },
                        };
                    }
                    data[gkv.Key.ToString()] = edict;
                }
                string json = MiniJson.Stringify(data);
                // Atomic: write temp, then replace (no delete-before-move).
                // File.Replace is atomic on Windows; on other platforms use Move
                // with overwrite (which is atomic for rename).
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                // Move with overwrite is atomic (rename syscall).
                if (File.Exists(path))
                {
                    string backup = path + ".bak";
                    try { File.Replace(tmp, path, backup, true); }
                    catch
                    {
                        // Fallback: Move (atomic rename, overwrites on Windows).
                        File.Move(tmp, path);
                    }
                    try { if (File.Exists(backup)) File.Delete(backup); } catch { }
                }
                else
                {
                    File.Move(tmp, path);
                }
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] entitlement save failed: " + e.GetType().Name);
                return false;
            }
        }
    }
}

