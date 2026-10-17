# Static placement and render-bundle closure audit — 2026-10-16

The bounded scan closes the previously uninspected static-placement bundle group without finding a new source for any of the 51 equipment targets or a placement for the five conditional item-offer NPCs. It does not establish global absence or change the existing 48 unresolved / 3 conditional-source dispositions.

## Actual bundle inventory

Every installed bundle in these four groups was opened and its Unity object types and MonoBehaviour classes identified. There were zero bundle-load or class-identification failures.

| Group | Bundles | MonoBehaviour inventory | Payload treatment |
|---|---:|---|---|
| staticobjplacement | 1,557 | 1,557 OcStaticObjPlacementData | All decoded with full byte-consumption checks |
| subscenehlod | 745 | 745 RendererMaterialPlaceholder | One full sample decoded; remaining render payloads excluded |
| grassrenderer | 8,533 | 981 SoAdditionalGrassChunkData | One full sample decoded; remaining grass payloads excluded |
| terrain | 56 | None | Actual object inventory contains TerrainData, TerrainLayer, textures, shaders and AssetBundle |

The HLOD sample has renderer-material key collections; the grass sample has instance data. Their exclusions follow the observed class inventory, not filenames alone. This is an object-class census of 10,891 bundles, not a claim that all serialized payloads in every bundle were decoded. The full per-bundle manifest is in `bundles.json`.

## Static placement mapping and loot

All 1,557 placement records decoded successfully. They contain 55,022 instances and every prefab index from 0 through 435. These are **static prefab indices, not enemy enums or item IDs**; numeric overlap with NPC enums159–163 is not evidence of those NPCs.

The fixed decoder reads `level13` object21063 (`OcStaticObjPlacementMng`) and its436 `_prefabs` references. The runtime lookup is `_prefabs[prefabId]` (installed decompile, `all.cs`339222–339224). All436 references resolve, and their complete GameObject descendant trees were traversed. There are4,351 GameObjects and4,387 MonoBehaviour components, with zero traversal/decode failures. Component counts are retained in JSON. They include436 static-object spawners,253 ordinary static objects,181 falling-tree objects and2 item-generating static objects, but no `OcSpawner`, item-offer NPC, quest reward or chest components.

All `_SoEnemyData` and `_itemData` references from those static-object components were resolved with the correct source serialized asset. This yields87 distinct `SoEnemyData` and1 `ItemData`, all fully decoded. No target51 item occurs in their item-bearing fields, including the drop-item fields; no positive target drop path was found. Pointer IDs and unrelated integer fields were not accepted as item evidence.

The manager registers1,553 distinct placement GUIDs. Four of the1,557 bundle GUIDs do not appear in this particular manager registry; their exact names are preserved. All four were still decoded, and their prefab indices fall within the same0–435 range. The audit does not promote unregistered bundle contents to reachable instances.

## Limits and reproduction

The trees include619 `OcDelayCreate` components carrying asset-name strings. The serialized hierarchy audit does not independently follow every such dynamically loaded asset; the runtime companion audit should be consulted for that mechanism. Registered prefab/data existence is not a gameplay test.

Scratch inputs and full payloads are under workspace `artifacts/closure-bundles`: `scan.py`, `sources.json`, `prefabs.py`, `prefabs.json`, `manager.json`, `drops.py`, `drop-data.json`, `render-samples.py`, and `render-class-samples.json`. `report.py` regenerates the JSON manifest. The earlier decoder alignment and managed-reference fixes are reused, and all payload reads use `check_read=True`. Two transient script setup failures (a local module-name collision and a script-prefix split) were corrected before the successful runs; they produced no missing source objects. Final data/decode error arrays are empty.

Research only: no game, save, production code, workbook or AP tier changes.
