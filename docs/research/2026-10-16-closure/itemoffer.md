# ItemOffer runtime closure — 2026-10-16

**Sting (1723), Mjolnir (1724), and Sharp Shooter (6162) must remain outside proven normal acquisition.** Their previously decoded ItemOffer high-table reward chains are real, but newly generated quests in the installed normal runtime cannot select those tables. NPC placement remains unproved as a separate issue. This closes the particular normal ItemOffer route more strongly than the earlier placement-only uncertainty; it does not prove global item unobtainability or historical legacy-world behavior.

## The decisive runtime gate

`OcEm_NPC_ItemOffer.RenewQuest` reads `OcWorldManager.IslandLevel` (`all.cs:518723–518782`). Its weights at island level 0 are `[1,0,0]`; the target-bearing table index 2 is positive only at island levels 6 and 7 (`518345–518355`). The installed `OcWorldManager.IslandLevel` getter returns **0** unless `SROptions_Sub.Current.DummyIslandWorldLevel >= 0` (`431161–431176`). The override defaults to -1 (`20554`), and normal world-setting reset passes -1 (`431861–431864`). World age/`WorldLevel` is a separate property and does not drive this getter.

This revises the earlier statement that the high rewards first become available at “vanilla IslandLevel 6”: that describes the abstract table input, **not a normally reachable current-runtime stage**. No age should be assigned from it.

The override is not exclusively developer-only: creative world settings pass `creativeSetting.islandLevel` to `ApplyWorldSetting`, which sets the override (`431866–431891`). Debug combat presets, a debug treasure test and aging-test tasks also write it. Consequently the precise result is **fresh normal-current-runtime high-table route blocked; creative/debug conditional route still requires a working NPC and placement**. No gameplay acquisition was tested.

## Registration does not create an encounter

All five NPC types remain registered in `sharedassets6.assets`: `SoEnemyArray` path ID 42274 has addressable prefab GUIDs, and `EmTypeAssetIdPairAsset` path ID 39990 has network IDs. Exact pointers/GUIDs are in `itemoffer.json` and the full source decoding is in `artifacts/closure-itemoffer/registration.json`.

| NPC | Enum | Network asset ID |
|---|---:|---:|
| BossDrop | 159 | 3812666041 |
| Enchant | 160 | 1322740069 |
| Fish | 161 | 1393196979 |
| NaturalResource | 162 | 3712213119 |
| Treasure | 163 | 1480331540 |

`OcEmMng.Start` registers spawn callbacks **only on multiplayer clients** (`159640–159663`). Actual server creation needs `EmSpawnInfo`, then `createEmInst` loads the prefab through `OcEnemyAssetMng` and `SoEnemyArray.GetAssetRef`. Registration establishes loadability and replication, not a world spawn rule. Exact named `NPC_Quest_*` searches found only enum declarations, not a dedicated code spawn call; numeric/data-driven routes are not excluded by that search.

The prior 5,628 loose and 181 bundled spawners had no enum 159–163. The parallel closure scan also decoded all 436 prefabs referenced by the level13 static-placement manager, including descendants, without finding NPC or `OcSpawner` components. Its `OcStaticObj_Spawner` components spawn static objects and must not be mistaken for enemy/NPC spawners. See the bundle closure report and `artifacts/closure-bundles/prefabs.json` for that independent evidence. This covers the decoded prefab hierarchies; 619 `OcDelayCreate` components reference further addressables by name. Their loader (`all.cs:199567–199817`) instantiates a child GameObject and manages LODs, but does not restrict child contents to decoration. Those delayed children remain a placement-coverage caveat until decoded; the island-level gate above is independent of it.

## Save, import, and replacement checks

`IsDuringWork` requires an `OwnerSpawner`, or a matching guest authorization GUID (`518415–518433`). `canTalk` rejects NPCs outside that state (`518676–518686`), and normal host quest initialization occurs only during work. Thus merely restoring, capturing, importing, or otherwise summoning the NPC model does not establish a working quest source. `OcEmMng.loadSaveData` uses spawner GUID 65535 (`159943–159974`); the general eligibility function permits charmed enemies, vehicles and a short explicit list, rather than giving these NPCs a dedicated restore route (`160032–160060`).

`LoadQuestData` can reuse an unexpired in-memory quest's table index without re-rolling. That is a real exception to a statement about all possible quest state, so the conclusion is deliberately about **fresh normal quests**. `WorldInGameData.questNpcDataDic` is marked `IgnoreMember` and initialized empty (`432548–432549`, `432607`); inspected other writers are quest acceptance/clear and multiplayer synchronization, not a legacy world import. Existing high-table quest state produced under an override can be shared within that session; this does not establish ordinary generation.

The old-character importer collects equipment/building/consumption/material inventory and pet records (`296288–296367`). This may carry an already-owned weapon into a new character, but is not a renewable current-world source and does not import the NPC world quest dictionary. Historical legacy-build placement and historical acquisition were not proven from the current installed assembly.

The `questRequiredItemTables` field has only its declaration and indexed reads in the installed decompile; no runtime replacement assignment or alternative consumer was found. The earlier full prefab/table chain remains the evidence for reward identity.

## Reproduction and limits

Code evidence with one-based line ranges is preserved in `artifacts/closure-itemoffer/code-evidence.json`; `itemoffer.json` embeds those excerpts and the decompile SHA-256. `registration.py` decoded both registration assets using `check_read=True`. One unrelated unidentified `resources.assets` object (path ID 421193, null script pointer) is retained as a scan error, not silently counted as inspected. The report is research-only: no game, save, mod, workbook or tier changes.
