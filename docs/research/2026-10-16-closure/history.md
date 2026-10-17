# Equipment history and import boundary — 2026-10-16

**Project decision:** At the user’s direction, all 48 exact IDs below are labeled **Legacy (presumed carryover)**, removed from active research, and excluded from required progression planning. This is a working classification, not proof that importing is their only source. Reopen only on new direct acquisition evidence or an explicit user request. Historical findings below are preserved.

Research only. **No one of the 48 unresolved current equipment IDs can be closed as import-only from the historical evidence collected here.** Every exact ID matches a captured catalog row with `isOldData=false`. Historical availability of a same-named item is useful for finding old recipes, but does not identify the current item or prove current reachability. No AP tier or minimum age is assigned.

## Exact-ID result

Input: `../2026-10-16-deep/equipment.json`, filtering the 48 `source_unresolved_after_expanded_audit` entries. Joined numerically to `../../../worlds/craftopia/equipment_catalog_20261010_r1.json`; 48/48 matches, 48/48 non-old rows. The catalog was captured 2026-10-10 and identifies assembly SHA-256 `88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea`.

The companion JSON records all 48 exact ID/name/category matches and local status fields. Web sources below have **no numeric item IDs**; their candidate ID links are research targets, never identity proof.

## Version boundary and useful primary evidence

- [Pocketpair release announcement, 2023-06-28](https://www.pocketpair.jp/en/game-news/craftopias-largest-update-ever-seamless-world-update-available-nowentire-world-remade-into-a-new-gigantic-map-combat-system-revamped/) dates the Seamless World release. Earlier recipes must be treated as Legacy evidence.
- [Official transfer instructions, 2023-06-27, and release announcement, 2023-06-28](https://store.steampowered.com/news/posts/?appids=1307550&enddate=1688204792&feed=steam_community_announcements) describe bringing inventory items from the Legacy branch through Transfer Maid Fam. They also describe gradual restoration of old crafting content after launch. This establishes an import route in general; it supplies neither a whitelist of these 48 IDs nor proof that any current definition is obtainable only by importing.
- [Official patch v20210930.1502, 2021-09-30](https://store.steampowered.com/news/posts/?appids=1307550&enddate=1633323907&feed=steam_community_announcements) adds Wizard Cloak in two styles and three colors at Blacksmith, three Classical Witch Hats at Blacksmith/Hatter, and Straw Hat at Hatter. Candidate targets: 2539–2542, 5516, 5518, 1795. The patch does not give color-specific IDs. It cannot distinguish duplicate Straw Hats.
- [Official Desert update v20231218.1856, published 2023-12-20](https://store.steampowered.com/news/posts/?appids=1307550&enddate=1705910587&feed=steam_community_announcements) changes body alignment for Dragon Mail, Wizard Cloak and Farmer's Overalls among other clothing. Candidate targets: 2511, 2512, 2517, 2518, 2539–2542, 2552, 2553. This is post-Seamless maintenance of these model families, **not** a source/unlock announcement, and does not establish any color or item ID.

## Dated secondary leads, deliberately not promoted into sources

[Armor guide dated 2022-08-26](https://fantasyroleplayinggames.com/craftopia-armor-guide/) records Legacy red/blue Dragon Mail and red/blue Wizard Cloak recipes, including both sex variants. Candidate IDs are 2511, 2512, 2517, 2518 and 2539–2542. Its historical material combinations help distinguish old recipe assets: red mail uses platinum and Dragon Scales; blue uses platinum and Reaper's Spirit Scale; the two wizard colors use linen/elemental ingots with amber or glass. No numeric IDs are supplied and the current definition/source chain still needs proof.

[Hatter guide explicitly describing 2021-10-26](https://fantasyroleplayinggames.com/craftopia-hatter-guide/) lists red/blue Dragon Helms and multiple Straw Hat rows. Candidate helmets: 5501, 5502, 5505, 5506. The duplicate hats make it especially unsafe to assign its ordinary cheap hat recipe to ID 1795, whose captured `workbenchLv` is 99. The full page fetch failed in this pass; indexed text supplied the dated scope and duplicate-hat warning. Treat this as a lead to inspect, not verified recipe identity.

Searches for Handaxe/Bardiche/Battle Mace (6014/6016/6018), Fimbulwinter (1704) and Sohaya no Tsuruki (6468) did not yield version-specific primary acquisition evidence. This is a search limitation, not evidence of absence. Secondary Moon Piercer chest claims omit exact IDs, build and chest table, so they do not resolve 6166. Generic Fandom weapon lists mix versions and lack exact IDs; neither those lists nor their omissions justify a current-source conclusion.

## Executable import semantics

Local decompile `artifacts/harvesting-research/all.cs` provides a stronger identity constraint than names:

- Lines 555146–555171: `GetItemData_OrNull(id, isOld)` explicitly chooses old definitions for `isOld=true`; with unspecified version it can fall back to `GetOldItemData` if the current lookup fails.
- Lines 555175–555182: current and old enabled arrays are built separately, and the current appearing-in-game subset is separate again.
- Lines 555680–555701: `CreateOldItem` creates from old data and assigns data version 0; network reconstruction dispatches on that version.
- Lines 555733–555764: versioned and saved-item reconstruction preserve the old/current choice.
- Line 558991: saved item `IsOldData` is `dataVersion == 0`.

Consequently the safe identity key for imported equipment includes data version, not just ID or display name. Importing an old item with ID X does not establish a normal acquisition route for current ID X. Nor does the mere existence of a current row establish such a route.

## Archived research leads (not an active backlog)

Compare exact current and old ScriptableObject membership and recipes for the named armor groups, retaining data-version provenance. Inspect status meaning before using it as an exclusion. For the newer 6000-range weapons, prefer ID references in current loot/quest/merchant/blueprint assets and reachable placements; historical same-name searches are weak. For Straw Hat 1795 and Silver Bow 6161, explicitly compare all same-name local definitions before associating any web recipe. The underlying source evidence remains unresolved, but the user has closed active investigation of these 48 as presumed legacy carryovers.
