# Geezah pilot and progression milestones

Status: implements the direction approved in conversation; detailed implementation review pending. This supplements `2026-10-15-regional-progression-design.md`.

## Player-facing outcome

Create a new, short, single-player seed containing only explicitly reviewed Geezah checks and eligible early missions. The pilot ends when Ruin of Beginning is cleared. Later region presets require both the selected region's final tower repair and its main dungeon clear, and include checks only from that region and earlier explicitly included regions. Region order is data, not nearest-tower distance. The current CraftopiaOrbs seed and character remain intact for diagnostics.

Add distinct checks for successful tower repair stages, age advancement, first dungeon clears, and mission-category reward claims. Individual mission checks remain separate. Preserve native tower advancement, age changes, dungeon rewards and category rewards. Tamer logic uses vanilla capabilities only. Respawning chests remain excluded.

## Evidence and boundaries

- Installed game assembly SHA-256: `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.
- Dungeon ID 1 (`So--OcDungeonOpenWorld_001_Skydungeon`) was identified by the user as Ruin of Beginning. `AddDungeonClear(1)` changed the clear list from empty to `[1]` on October 16. Evidence: workspace `evidence/dungeon-20261016`.
- Millewind GUID -1329457748, definition 3807, stage 0 to 1 was observed inside `TryProgressConstruction`. Reload also produces an apparent 0 to 1 sampled transition. Therefore sampled stage changes and `TrySetProgress` alone never award checks. Evidence: `evidence/tower-age-20261016`.
- `EvoAndCloseUI` nested `AddWorldLevel(1)` changed world level 1 to 2 (Frontier Age). Count this as one advancement. Loading a world or assigning its level is not advancement.
- Assembly inspection confirms `Oc.Missions.OcMissionManager.TryTakeCategoryReward(int)` returns false for already-taken/incomplete categories. `IsCategoryCompelete(int)` requires every member's `IsRewardTaken`. Success appends the category ID to `categoryTookList` after native rewards. Observe false-to-true taken state plus successful return, not a menu/popup.
- The active Anubis exchange class is `Oc.Item.UI.OcUI_StatusUpPageSoulOrb`. `OnExchangeButtonPressed()` spends five orbs, delivers/unlocks the native reward, increments `_exchangeCount[0]`, and calls `SetSoulOrbExchangeCount`. Its `StatusUpConfirm()` is empty. The older `OcUI_SoulOrbUI` is not the menu shown in user screenshots.
- Anubis screenshots show next reward 12, two orbs held, five required; Growth upgrades cost four slates and can be reset. Existing captures missed this active UI. Reward catalog length 61 is not proof of 61 usable checks. Evidence: `evidence/anubis-20261016`.

## Runtime contract

New families are opt-in through versioned slot data and a frozen catalog. No change to old seed behavior. Require an enrolled character, selected-world binding, matching seed/slot/epoch, installed hooks, and a catalog allowlist. Unsupported multiplayer fails readiness for the new pilot without blocking unrelated vanilla saves.

Use event-local before/after snapshots. Only accepted completion boundaries create durable outbox entries. Duplicates, failed native actions, load callbacks, wrong-world actions, unknown IDs and unexplained multi-level jumps produce no checks. A changing world reference invalidates in-flight snapshots. A comparison fingerprint is not a globally unique world identity; copied/ambiguous world enrollment must not silently establish a new binding.

New observations use `milestone:<family>:<source_id>:<stage>` with families `tower`, `age`, `dungeon`, `category`; stage is zero for dungeon/category, destination stage for tower, destination world level for age (source ID zero). The bridge resolves locations only through the frozen catalog and selected slot map, persists before acknowledging, retries delivery, and waits for AP confirmation before showing completion or victory.

If native completion succeeds but the observation cannot be persisted, retain a bounded retry entry and visibly hold further check processing. Do not pretend it was submitted. Document the crash window; do not retro-award arbitrary save state to conceal it. Historical-state reconciliation needs explicit enrollment evidence.

## Catalog and generation

Preserve existing IDs. Reserve disjoint ranges after checking all existing tables: tower 3000–3999, age 4000–4099, dungeon 4100–4199, category 4200–4999. Assign authored IDs, never derive them from enumeration order. Unknown/unverified entries may appear in research output but cannot be activated by a reviewed profile.

Mission categories have explicit source IDs, display names and full member lists. Include a category only when all its members have supported, within-profile completion requirements. Do not treat an empty, truncated or unknown membership as free access. A category's native reward must be claimed to send its AP check.

Every pilot location must have explicit region membership and requirements. The 55 nearest-Geezah orb candidates are not automatically approved. Omit ambiguous candidates and explain omissions. Required production and equipment unlocks must exist in the pool and be reachable without themselves. Smelting must be placed on the same player's reviewed opening missions or reviewed Geezah orbs; exclusions/plando that eliminate all legal destinations fail generation.

Pilot goal is an explicit required location list containing dungeon ID 1's location. Future regional goals use an AND list containing the region's final tower stage and main dungeon. Do not use generic boss events or a broad mission count as substitutes. Build total checks, route maps and item counts from actual selected locations. Preserve the compact draggable HUD and its completed/total count; no completed-check list.

## Anubis scope

Fix diagnostic discovery and observe the actual exchange method now. Record bounded reward counter snapshots and category membership/claimed state through F9; no recurring whole-scene scans. Anubis AP checks remain disabled until a successful exchange, its persisted counter and reset/reload behavior are verified. Do not request the user farm currency just to finish this batch. Never turn repeatable stat respecs into repeatable AP checks.

## Release and live validation

Build and test locally before installation. Check the actual host process before replacing DLLs. Back up installed binaries and configuration, verify installed hashes, and preserve the existing seed and saves. A new location set requires a new seed. No reset or migration of the current character is implicit authorization.

Live pilot acceptance: initial category claim sends one check; reopening/reclaiming/reload does not duplicate it; dungeon first clear sends one check and one victory; repeats/reconnect/reload do not duplicate either. Tower and age probes can use the current world's next uncompleted milestones; do not require a restart of progression for each probe. Later-region preset activation waits for its actual catalog and dependency coverage.
