# Enemy placement and renewal audit — 2026-10-16

The installed open-world scene contains actual, enabled placements for the previously verified material-drop families. This closes much of the gap between enemy drop templates and supply routes. Bone/Dust skeletons are explicitly night-conditioned, while the early ordinary Wizard placements have no time condition. Sheep, low-level cows, chickens and goblins have repeat-spawn configurations. A level80 Reaper placement exists in Volcano with a 600-second repeat interval. These are configuration-plus-code findings, not completed gameplay accessibility tests.

## Provenance and scope

`spawns.json` retains 2,143 selected spawner records from `level10`, including broader goblin/skeleton variants for comparison. 1,772 have enabled components and fully active serialized ancestor chains; 371 have inactive ancestry and must not be counted as ordinary current placements. All selected full spawner reads passed byte-consumption checks. Twelve other level10 objects failed byte validation and were excluded; their path IDs and errors remain in JSON, so absence claims are inappropriate.

Unity BuildSettings identifies level10 as `Assets/_Overcraft/Scene/OpenWorld/OcScene_OpenworldMap_Main_Spawner_Convert.unity` (shared extraction evidence: `artifacts/remaining-merchants/buildsettings.json`). This is a scene placement inventory, not a prefab catalogue. JSON contains the level10 SHA-256 and installed assembly SHA-256. The merchant agent's extraction supplied full spawner objects and transform ancestry; this audit independently reread real MonoBehaviour enabled flags and each ancestor GameObject active flag. Generated synthetic MonoBehaviour headers were excluded from spawner data.

World positions are calculated by composing local translation, quaternion rotation and scale through all ancestors. Fields are joined using `OcOpenWorldAreaManager.GetFieldID`: strict cylinder containment at field center minus5 y plus zone offset, choosing the highest nonnegative weight. They are **spawner-center** joins, not every possible randomized spawn coordinate. Internal Tutorial/GrassField01 names and localization IDs100/110 are retained; their localized Geezah/Yarden labels have not been independently joined here. Hierarchy folder labels can disagree with weighted field boundaries (for example a `2nd_biome` spawner center can join Tutorial), so folder names were not substituted for the algorithm.

## Concrete active placements

All examples below are level10, Dev_Status1, enabled, active through ancestors, IslandLv0–99, no manual-spawn requirement, no linked StaticObj, no quest-clear save flag. Quantities are simultaneous spawn capacity, not guaranteed loot quantities. The previous `2026-10-16-followup/drops.md` establishes the exact family-to-material tables.

| Source / material | Path ID | Center xyz (rounded) | Field / fixed level | Repeat configuration |
|---|---:|---|---|---|
| Skeleton_1HAxeShield / Bone695 + Dust2460 | 51245 | -2766.75,66.64,-1674.47 | Tutorial /4 | night only; 120s, capacity2 |
| Skeleton_1HAxeShield / Bone695 + Dust2460 | 51209 | -1747.93,13.84,-1169.32 | Tutorial /12 | night only; 120s, capacity2 |
| Wizard / Crystal2453 + Dust2460 | 60023 | -1798.27,82.01,-272.66 | GrassField01 /18 | no time condition; 300s, capacity1 |
| Goblin_Sword / Fang680 | 51065 | -2580.40,167.16,-1410.15 | Tutorial /7 | no time condition; 120s, capacity1 |
| Sheep / Leather683 | 58038 | -1895.43,120.49,-1408.41 | Tutorial /2 | no time condition; 60s, capacity2 |
| Cow / Leather683 | 50198 | -2544.50,173.84,-1331.17 | Tutorial /3 | no time condition; 60s, capacity2 |
| Chicken / Feather692 | 56157 | -2167.59,127.01,-1292.12 | Tutorial /2 | no time condition; 60s, capacity2 |
| ChickPhoenix / Feather692, chance Dust2460 | 59809 | -1511.41,40.96,-841.75 | GrassField01 /12 | no time condition; 120s, capacity2 |
| Reaper / Scale3355 and Bone695 | 50201 | -398.85,27.60,3194.93 | Volcano /80 | no time condition; initial180–480s, repeat600s, capacity1 |

The low-level fixed Cow3 placement is materially stronger evidence than the broad assertion that cows supply leather: higher cow tables lack or reduce that drop. Fixed Wizard18 selects the first ordinary Wizard table and supports its strong Crystal/Dust route without relying on elemental-wizard probabilities. Reaper80 selects the highest normal Reaper table under the prior family-level rule; that table still has Scale3355 at configured rate1. This does not remove combat or regional traversal requirements.

Among active level10 placements, Tutorial contains22 axe/shield skeleton and2 bow skeleton spawners, all with night conditions and120-second intervals;11 sheep,4 cow and4 chicken spawners have60-second intervals. GrassField01 has3 ordinary Wizard spawners with300-second intervals and no time conditions, and6 ChickPhoenix spawners with120-second intervals and no time conditions. These counts are scene-component counts, not simultaneous enemy totals. Other regions and inactive records are preserved in JSON.

## Exact night and renewal semantics

`OcSpawner.Start` initializes all managed-reference `_SpawnConditions`; legacy `_IsNightOnly`/`_IsDayOnly` flags are only migrated when the condition array is empty. Therefore looking only at legacy flags would incorrectly classify the modern skeleton placements as unrestricted. JSON explicitly resolves each condition rid to its class and payload. `SpawnCondition_Time.CanSpawn` gives night precedence if both flags are true; flags false/false impose no time restriction.

`OcDayMng.isNight()` returns true in Hell or forced-night mode; normally it tests `CurHour > 15` or `< 0` in an internal20-hour cycle. **15 is not a literal displayed15:00 clock reading.** `OcSpawner.move` checks conditions before timers; when a condition fails it deactivates existing spawns and resets timers if enemies were present. Night supply is recurring across eligible periods, not an always-present daytime population.

`ResetSpawnTimer` uses3 seconds for ordinary initial delay when `_UseInitSpawnInterval` is false. When true, initial delay is interval plus uniform jitter with a minimum3 seconds. `recalcSpawnTimer` uses `_SpawnInterval + Random.value * _SpawnInterval_Jitter`. The listed examples have zero repeat jitter; Reaper explicitly uses initial180 plus up to300 seconds jitter.

After an initial spawn, positive repeat intervals advance while capacity is available. Out of immediate spawn range, the move method can accumulate reserved spawns and deliver them on return at1–5-second spacing. `OcSpawnerMng` schedules both nearby and outer-chunk spawners, subject to host, load/join and static-object-manager readiness checks. Thus an inspector comment about distant timers pausing is not a sufficient description of the implemented reserve behavior. Timing is game-update timing, not a guaranteed wall-clock delivery deadline.

For negative intervals, `checkNetActiveRange` clears initial-spawn completion on leaving range. On return, initial spawns can recur **unless** `InitSpawnOnly` is effective: then `doStartSpawn` subtracts KillCount from the initial quota and stops when exhausted. A negative interval alone is not proof of one-time access. Explicit `ResetSpawner` clears kill count and initial state, but this audit does not prove when every dungeon/event reset is available. Persistent `SpawnCondition_Once` is another independent restriction; do not infer renewability from a timer while ignoring condition records.

`notifySpawneeKilled` frees capacity. Actual spawn attempts remain subject to development status, SoEnemy.CanSpawn, quest-clear state, anti-spawn banners, enemy pool capacity, geometry/water checks, manual/event controls and scene loading. Serialized active ancestry is strong placement evidence but does not prove no runtime controller later disables that object. Spawner-center region membership also does not prove a safe walking route, tower prerequisite, dungeon entrance, or early AP reachability.

## Practical conclusion

There are concrete active-scene, recurring configurations for all requested material families, including an early-region night-skeleton supply, low-level animal leather/feathers, second-region ordinary Wizard Crystal/Dust, and Volcano field Reaper scale. Use these as evidence-backed route candidates with their stated conditions. Do not encode unlimited supply or exact progression gates until the relevant route and runtime availability are checked. No installed game, save, mod or APWorld code was changed.
