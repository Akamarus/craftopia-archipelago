# Production and progression evidence batch

Development source v1.9.0 was deployed for controlled testing on October 10 after backing up the prior installation and test state. Collector v1.2.0 also captured successfully. No enrollment reset, historical recovery, save edit, or new grant injection was performed.

Runtime checkpoint: all 22 production hooks and nine existing hooks registered without logged errors. The existing character passed AP-aware validation and restored its permissions. A campfire cooking attempt without the cooking unlock was denied; the tester explicitly confirmed the raw ingredient count stayed unchanged. Basic crafting successfully produced a stone pickaxe. Making and using a glider delivered the real Recycling and Smelting AP rewards; durable ownership and acknowledgments were inspected. After building the first tower and advancing normally to Agricultural Age, the tester confirmed a furnace batch completed with expected material consumption and output. Furnace save/reload behavior subsequently passed (see below); connector behavior and the other production paths remain unverified.

## Production repair

**Subsequent evidence correction:** The positive furnace tests above prove ordinary operation after age advancement and after reload. They do not prove a locked furnace is blocked by Smelting permission. Inspection found the old `Refiner → smelting` class mapping actually targets weapon refinement, and generic crafting benches select recipes by their installed item's family ID. That mapping remains an identified defect in the installed development build pending station-family evidence; do not count furnace enforcement as verified. See [equipment/prerequisite work](EQUIPMENT_PROGRESSION_20261010.md).

The old `StartProcessReq` guard was too late for several callers: the installed game consumes items or currency before invoking it. The new guard set targets 22 inspected method boundaries in UI crafting and automatic production. All target names/signatures were verified against the installed assembly. Bound unknown stations and evaluation failures deny; opening crafting and explicitly excluded power retain vanilla behavior.

The connector uses a narrow transpiler at the host crafting branch. Denial branches to the existing discovery/cleanup section, before consumption. It does not skip the entire Update. The transpiler rejects unfamiliar method layouts; patch-install failure removes this mod's partial patches. A synthetic IL regression executes locked target → discovery → unlocked target without consuming on the denied frame. An independent source/IL review found no remaining concrete blocker after this correction.

Actual Harmony registration, locked campfire ingredient preservation, basic crafting and one unlocked furnace batch are now verified as described above. The other production paths remain unverified. Automated world machinery currently uses the local permission mirror; separate-player ownership/authority is not established by this patch. Multiplayer production is not a supported claim.

### Production reload checkpoint

After the requested save/full close/reload, the tester confirmed the furnace still completed smelting correctly. The new startup log shows the same verified character, AP-aware totals matching, eligibility granted, and Smelting permission restored. The bridge recorded a disconnect and fresh session handshake. This completes the tested furnace persistence path; connector behavior, other station types and multiplayer remain unverified.

## Mission observation custody

The mod flushes an observation journal before queueing a check. It replays only the active epoch/slot after session negotiation. The bridge acknowledges custody only after its pending/deferred state commits. The mod removes an entry only on a matching epoch, slot and connection token acknowledgment, with atomic file replacement. Failed writes retain or block evidence rather than allowing an unpersisted send.

This acknowledgment transfers durable custody to the bridge; it does not claim the AP server accepted the location. The bridge retains its own pending checks until server confirmation. Older journal lines without observation IDs remain preserved/replayable and cannot be removed by the new ID acknowledgment. Corrupt/partial journals block new observations and require review.

## F9 evidence

Capture timestamp: `2026-10-10T03:53:43.5746911Z`.

| Data | Observed count |
| --- | ---: |
| Item rows | 7,053 |
| Unique positive item IDs | 7,052 |
| Zero-ID sentinel rows excluded | 1 |
| Items with material fields | 3,213 |
| Bench groups | 21 |
| Age records | 9 |
| Raw age-new-item array groups | 10 |
| Mission rows | 125 |
| Skill rows, including non-player candidates | 360 |

All material references, bench item references and age requirement item references resolve to captured positive IDs. Bench-map assignments agree with the item rows. No malformed material pairs or duplicate positive IDs were found. All `isOldData` flags were false; eight different raw status values are present and require interpretation before deciding player availability.

The audit summary contains provenance hashes and counts without character identity or save state. Reproduce it against a private capture with:

```sh
python tools/audit_progression_capture.py path/to/capture.json --output audit.json
```

Raw IDs, rank, rarity and array positions are not automatically AP tiers. We still need verified station-to-bench bindings, meaning of age unlock arrays, gathering/drop sources, and mission subclass requirements. Existing mission IDs and generation logic remain unchanged. Equipment, boss/age goals, reward replacement and material/money delivery are not enabled by this capture.

## Verification

- All 11 C# programs passed: GrantPipeline, RepairRegression, ValidationDecision, TransactionRegression, UnlockMirror, SkillCatalog, EnrollmentRegression, Production, Collector, Observation and Connector.
- All 70 client tests passed, including durable observation acknowledgment, numeric GUID multiplayer fixtures and Windows lock behavior.
- All 14 world tests passed, including real AP 0.6.8 legacy generation: 53 locations, 53 pool items and objective completion events.
- Gameplay build: zero errors/warnings. Collector build/capture succeeded independently.
- Neither synthetic IL tests nor fake-client multiplayer tests substitute for game runtime verification.

## Next controlled runtime test

Close the game before replacing the gameplay DLL. Retain the tested character/slot/session and create a file backup before deployment. Choose an unowned production group and record the exact input stack and currency. Attempt one recipe: denial must preserve both. Deliver that group's actual AP unlock through the test session, repeat and verify vanilla consumption/output once. Check reconnect/reload ownership, then connector target switching if the required objects are available. Stop on partial hook installation, material loss, identity mismatch or a recovery hold; do not reset persistence to bypass a failure.

The wider approved batch remains open: equipment progression, prerequisite logic, additional check adapters, replacement rewards/filler and live multiplayer still need implementation/evidence.
