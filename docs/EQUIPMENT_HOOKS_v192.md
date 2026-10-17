# v1.9.2 equipment integration — experimental

The equipment catalog now has game-facing craft, equip and loadout hooks, plus a bound test-session contract. Equipment is disabled by default and remains excluded from generated item pools. Existing seeds retain their equipment behavior.

## Implemented boundaries

Seven exact prefixes are installed and checked against Harmony metadata. Startup requires the reviewed game module MVID `62ff4b5b-7684-4c41-b5f0-f44aa19360ca`; an unsupported build or missing registration stops bridge startup.

| Boundary | Denied result | Reason for placement |
| --- | --- | --- |
| Craft manager `CanCraft(ItemData,int)` | false | Before material costs; delayed craft completion checks again |
| Enchant-selected `TryCraftItem(ItemData,int)` | null | Before its separate consumption path |
| Chest `canAutoCraftNum(ItemData)` | zero | Connector checks this before consuming ingredients |
| Equipment manager `CanEquipItem` | false | Before destination clearing/transfer |
| Equipment slot `CanEquip` | false | Covers slot/drag permission checks |
| Equipment list `TryEquipItem` | false | Defensive check before list transfer |
| Equipment loadout confirmation callback | skip whole callback | Checks every target before the first unequip; cancel remains available |

Item metadata and loadout entries are read without creating items or loading prefabs. Missing/old/duplicate/unknown loadout data denies. Native equipped slots are null when empty. The low-level `setEquip` method is not intercepted. Passing a prefix only allows normal game checks to run.

## Activation contract

Activation is intentionally unavailable through ordinary generation. A separately reviewed test requires both:

- Startup configuration `[EquipmentTest]`: `Enabled = true`, an exact approved `CharacterGuid`, and `Epoch`; the existing configured slot also binds approval.
- Session fields `equipment_catalog = equipment_20261010_r1`, integer `equipment_protocol = 1`, and `multiplayer_mode = 0`.

The mod advertises `equipment_protocol: 1`. The bridge requires matching capability on current connections before forwarding the requested equipment session or deliveries. Missing fields in historical seeds preserve legacy behavior. Malformed/unsupported requested contracts cannot become legacy mode.

Approval keeps enforcement required before connection and across disconnects. A conflicting contract latches rejection for the process; reconnect cannot clear it. Each equipment decision reads current character/load eligibility. A changed character cannot gain delivery approval through an empty or vanilla-only loadout. Recovery uncertainty supplies no combat permission.

Ownership restoration precedes inspection of currently equipped gear so missing ownership does not deadlock its own replay. Prohibited or unreadable equipped gear blocks new point grants/re-acks. It is never automatically removed, refunded or deleted. **This admission hold does not disable attacks or stats of pre-existing gear.** The feature is a controlled experiment, not complete use enforcement or a supported randomized seed.

## Verification and remaining tests

- All 12 C# executable suites pass. Equipment tests include the existing 6,843 catalog/policy assertions plus session and field/loadout adapter scenarios; row sweeps are not thousands of independent gameplay cases.
- 82 bridge tests, 22 world tests against AP 0.6.8, and eight exporter tests pass.
- Mod builds with zero warnings/errors.
- Independent review found a wrong-character admission gap; the fix adds identity gating before ownership delivery and gear admission, with actual empty/vanilla loadout regressions. Follow-up review found no blocker for a disabled registration smoke test.

First live step: install with equipment disabled, launch to menu, verify all seven equipment registrations and the existing registrations, then load the same test character/world. Verify ordinary crafting/equipping and existing production permissions remain unchanged. This step does not enable equipment randomization or change the seed.

October 11 deployment: v1.9.2 was installed while the game was closed after backing up the previous plugin, configuration, saves, client and server output. Installed SHA-256: `bb7e83508aa26ad72c8e61c73a05537f9cb7525542ff437a940c393fe3d542c2`. Equipment approval remains absent/disabled; the live bridge, seed and configuration were not changed. Registration smoke test is awaiting the next launch.

Only after that step should a separate explicitly approved equipment test be prepared. Its checklist is locked craft preserving materials; denied click/drag equip preserving both stacks; denied saved loadout preserving all current slots; allowed ownership plus native-age path; removal; reconnect; wrong character; unknown item; and pre-existing gear admission. Multiplayer, alternate mutation paths and full use enforcement remain unverified. Starter-aware progression and complete prerequisite coverage are still required before enabling equipment in generated pools.

## October 12 runtime results

The disabled-equipment smoke test passed: all seven equipment hooks and 31 existing registrations verified on Unity Mono. The restored original session loaded with verified identity, matching AP-aware totals and ownership replay. The tester confirmed normal cheap-item crafting, equipment removal/equipping, owned furnace operation, and Cooking Pot denial without ingredient consumption. Logs corroborate missing-Cooking denials and contain no Error/FATAL/recovery hold or new skill-point application during these checks. This verifies the disabled path, not active equipment enforcement.

Before character loading, a missing live bridge ledger caused startup to create an unexpected epoch. That bridge was stopped and the game closed. The original epoch and ownership were restored from a preserved backup; three later acknowledgments were reconciled against prior bridge logs, both game-side grant stores, and server replay. All 24 applied indices agreed. No game saves or point totals were edited. A local resume guard now rejects a missing, held or mismatched test ledger. The cause of the file disappearance is unknown; this operational guard is not a general bridge persistence fix.

## October 14 active equipment results

A separate diagnostic seed used an explicitly approved fresh level-1 character with zero assigned, unspent and mission points. Enrollment committed and AP-aware validation passed. Bow tier 1 was the starter; mission 50 (punch trees five times) was fixed to Sword tier 1 by a local diagnostic wrapper. Normal generation was unchanged; this is not a beatability-tested seed.

| Check | Evidence / result |
| --- | --- |
| Sword unowned | Tester confirmed starting stick equip denied; log requires Sword tier 1 |
| Sword received | Mission 50 check and Sword tier 1 unlock logged; tester confirmed stick could then equip |
| Owned crafting | Tester confirmed Stone Sword crafting and equipping |
| Ordinary tool control | Tester confirmed Stone Pickaxe crafting |
| Full game restart | Existing enrollment loaded; validation passed at five unspent points; Sword and Bow restored; tester confirmed Sword unequip/re-equip |
| Duplicate check | Prior five grant indices were not reapplied; a distinct sword-equip mission subsequently delivered the sixth point |
| Category separation | Wooden Shield crafting denied despite sufficient materials (23/3 stone, 16/2 logs); log requires Shield tier 1 |

Locked Sword crafting was not tested because gathering its materials triggered the fixed unlock mission. The shield result verifies a locked crafting decision, not independently measured before/after material preservation. Shield equipping, denied-equip stack preservation, drag/drop, denied saved loadouts, higher-tier/age gates, wrong-character switching, unknown items, and pre-existing gear admission remain unverified in this round. Private saves, logs and state snapshots were preserved; personal identifiers are not published.

An earlier bridge ledger replacement attempt reported Windows access denied. Later persisted ownership, acknowledgments and matching game records were observed; the cause of the transient failure remains undetermined. No failure-injection durability claim is made.

Follow-up in the same running game: the bridge alone was restarted using the preserved, identity-checked ledger. Seven previous point grants were restored as history without reapplication; ownership replay restored Bow/Sword. A distinct mission (check the mission screen) delivered an eighth point. The tester confirmed Sword equip remained allowed, Shield crafting remained denied, and a saved Sword loadout restored after unequipping. This verifies the positive loadout path, not rejection of a mixed/unauthorized loadout or preservation of all slots on rejection. The same character, world, seed and earned progression will be retained for further tier tests.
