// Enrollment attestation: durable proof of verified fresh-character enrollment.
//
// Design:
// - An attestation binds (character GUID, epoch, slot) to an explicit user
//   approval at a point in time. It is the "future attestation provider"
//   referenced in EntitlementStore.
// - Attestations survive restart (durable JSON file, atomic writes).
// - A valid attestation for (guid, epoch) allows EntitlementStore.Read()
//   to return NoRecordConfirmed (verified fresh, zero points) instead of
//   UncertainHistory.
// - Attestations are NEVER auto-created. They require explicit user approval
//   through the enrollment UI/command. Missing/corrupt history never becomes
//   an attestation.
// - Attestations are bound to slot; a different slot requires re-attestation.
// - Character switch invalidates: attestation is for a specific GUID.
//
// File format:
// {
//   "schema": 1,
//   "attestations": {
//     "123": {
//       "epoch": 456,
//       "slot": "TestPlayer",
//       "character_name": "DisposableTest",
//       "attested_at": "2026-10-07T21:00:00Z",
//       "approved_by": "user",
//       "note": "Fresh character for test seed"
//     }
//   }
// }

using System;
using System.Collections.Generic;
using System.IO;

namespace CraftopiaArchipelago
{
    public class EnrollmentAttestation
    {
        private readonly string path;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;

        private Dictionary<int, AttestationRecord> attestations;
        private bool loaded;
        private bool loadFailed;

        public struct AttestationRecord
        {
            public long Epoch;
            public string Slot;
            public string CharacterName;
            public string AttestedAt;
            public string Note;
            // Consumed: attestation is a ONE-TIME authorization to initialize.
            // Once consumed (first entitlement committed), it can never again
            // authorize freshness. Lost history after consumption -> HOLD.
            public string ConsumedAt; // null if not yet consumed
            public bool IsConsumed => !string.IsNullOrEmpty(ConsumedAt);
        }

        public EnrollmentAttestation(string path, Action<string> logInfo, Action<string> logError)
        {
            this.path = path;
            this.logInfo = logInfo;
            this.logError = logError;
            this.attestations = new Dictionary<int, AttestationRecord>();
            this.loaded = false;
            this.loadFailed = false;
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(path))
                {
                    logInfo("[AP] no attestation file (no enrollments yet)");
                    loaded = true;
                    return;
                }
                string json = File.ReadAllText(path);
                var data = MiniJson.ParseObject(json);
                if (data == null) throw new FormatException("not a JSON object");
                if (!data.TryGetValue("schema", out object s) || !(s is long schema) || schema != 1)
                    throw new FormatException("unsupported schema");
                var att = data.TryGetValue("attestations", out object a) ?
                    a as Dictionary<string, object> : null;
                if (att == null) throw new FormatException("missing attestations");
                attestations.Clear();
                foreach (var kv in att)
                {
                    if (!int.TryParse(kv.Key, out int guid) || guid == 0)
                        throw new FormatException("invalid guid key");
                    var rec = kv.Value as Dictionary<string, object>;
                    if (rec == null) throw new FormatException("invalid record");
                    var record = new AttestationRecord();
                    if (!rec.TryGetValue("epoch", out object e) || !(e is long epoch) || epoch <= 0)
                        throw new FormatException("invalid epoch");
                    record.Epoch = epoch;
                    record.Slot = rec.TryGetValue("slot", out object sl) ? sl as string : null;
                    record.CharacterName = rec.TryGetValue("character_name", out object cn) ? cn as string : null;
                    record.AttestedAt = rec.TryGetValue("attested_at", out object at) ? at as string : null;
                    record.Note = rec.TryGetValue("note", out object n) ? n as string : null;
                    record.ConsumedAt = rec.TryGetValue("consumed_at", out object ca) ? ca as string : null;
                    attestations[guid] = record;
                }
                loaded = true;
                logInfo($"[AP] loaded {attestations.Count} enrollment attestation(s)");
            }
            catch (Exception e)
            {
                loadFailed = true;
                logError("[AP] attestation load failed: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Check if a valid, UNCONSUMED attestation exists for (guid, epoch, slot).
        /// A consumed attestation (already used to initialize) never authorizes
        /// freshness again. Lost history after consumption -> HOLD, not fresh.
        /// </summary>
        public bool HasAttestation(int guid, long epoch, string slot)
        {
            if (!loaded || loadFailed) return false;
            if (attestations.TryGetValue(guid, out var rec))
            {
                // Bound to epoch and slot. Must not be consumed.
                if (rec.IsConsumed) return false;
                return rec.Epoch == epoch && rec.Slot == slot;
            }
            return false;
        }

        /// <summary>
        /// Mark an attestation as consumed. Called when the first entitlement
        /// is committed for (guid, epoch). After this, the attestation can
        /// never authorize freshness again.
        /// </summary>
        public bool MarkConsumed(int guid)
        {
            if (!loaded || loadFailed) return false;
            if (!attestations.TryGetValue(guid, out var rec)) return false;
            if (rec.IsConsumed) return true; // already consumed
            try
            {
                var newAtt = new Dictionary<int, AttestationRecord>(attestations);
                rec.ConsumedAt = DateTime.UtcNow.ToString("o");
                newAtt[guid] = rec;
                if (!Save(newAtt)) return false;
                attestations = newAtt;
                logInfo($"[AP] attestation consumed for guid={guid} (one-time use)");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] mark consumed failed: " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Create an attestation. Requires explicit user approval (caller must
        /// have obtained it). Never auto-called. Returns false on failure.
        /// </summary>
        public bool Attest(int guid, long epoch, string slot, string characterName, string note)
        {
            if (!loaded || loadFailed || guid == 0 || epoch <= 0 || string.IsNullOrEmpty(slot))
                return false;
            try
            {
                var newAtt = new Dictionary<int, AttestationRecord>(attestations);
                newAtt[guid] = new AttestationRecord
                {
                    Epoch = epoch,
                    Slot = slot,
                    CharacterName = characterName,
                    AttestedAt = DateTime.UtcNow.ToString("o"),
                    Note = note,
                };
                if (!Save(newAtt)) return false;
                attestations = newAtt;
                logInfo($"[AP] attested enrollment: guid={guid}, epoch={epoch}, slot={slot}");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] attestation failed: " + e.GetType().Name);
                return false;
            }
        }

        /// <summary>
        /// Revoke an attestation (e.g., character switch, user request).
        /// </summary>
        public bool Revoke(int guid)
        {
            if (!loaded || loadFailed) return false;
            try
            {
                var newAtt = new Dictionary<int, AttestationRecord>(attestations);
                if (!newAtt.Remove(guid)) return true; // already absent
                if (!Save(newAtt)) return false;
                attestations = newAtt;
                logInfo($"[AP] revoked attestation for guid={guid}");
                return true;
            }
            catch (Exception e)
            {
                logError("[AP] revoke failed: " + e.GetType().Name);
                return false;
            }
        }

        private bool Save(Dictionary<int, AttestationRecord> state)
        {
            try
            {
                var data = new Dictionary<string, object> { { "schema", 1L } };
                var att = new Dictionary<string, object>();
                foreach (var kv in state)
                {
                    att[kv.Key.ToString()] = new Dictionary<string, object>
                    {
                        { "epoch", kv.Value.Epoch },
                        { "slot", kv.Value.Slot },
                        { "character_name", kv.Value.CharacterName },
                        { "attested_at", kv.Value.AttestedAt },
                        { "note", kv.Value.Note },
                        { "consumed_at", kv.Value.ConsumedAt },
                    };
                }
                data["attestations"] = att;
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
                logError("[AP] attestation save failed: " + e.GetType().Name);
                return false;
            }
        }
    }
}
