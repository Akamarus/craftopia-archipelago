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
