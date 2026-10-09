> Historical design/review document. Some statements describe proposals or older builds. See [current status](STATUS.md) and [development testing](DEVELOPMENT.md) before using setup or recovery instructions.

# Craftopia Archipelago — Runtime Evidence Diagnostic Package

**Version:** 1.0  
**Date:** 2026-10-07  
**Purpose:** Read-only collection of game evidence needed to complete the randomizer implementation.  
**Safety:** This package NEVER modifies saves, grants items, or changes game state. It only reads and logs.

## What to do

1. Copy `DiagnosticCollector.dll` to your BepInEx plugins folder (alongside CraftopiaArchipelago.dll).
2. Launch Craftopia and load your character.
3. Press **F9** to run the full diagnostic dump. Output goes to `BepInEx/LogOutput.log` and `craftopia_diag_<timestamp>.json` in the BepInEx folder.
4. Send the JSON file back for analysis.

## Exact questions this answers

### Equipment tiers (blocks progressive equipment)
For each item in the player's inventory and storage:
- `ItemData.id`, `ItemData.name`, `ItemData.wpCategory` (int), `ItemData.rank`, `ItemData.rarity`, `ItemData.workbenchLv`, `ItemData._Refine_MaxLv`
- Groups items by `wpCategory`; within each category, sorts by `rank` to identify tier boundaries.

**Question:** Which field (`rank`, `rarity`, `workbenchLv`) actually defines crafting progression tiers per weapon category?

### Production stations (blocks production unlock groups)
For each placed building:
- Building type name, `workbenchId`, list of available recipes (`get_Recipes`)
- For each recipe: `familyId`, `workbenchLv`, material IDs and counts

**Question:** What is the actual `workbenchId` → station-type mapping? What are the real recipe dependencies?

### Skill trees (blocks skill enforcement)
Dump `OcSkillManager.skillList.all`:
- For each skill: `ID`, `Category` (int), `Tier` (int), `MaxLevel`, `IsImplemented`, lock conditions

**Question:** What is the ID → (category, tier) mapping for all 280 skills? Which Tamer skills are unimplemented?

### Check sources (blocks location adapters)
- Soul orbs: Find the class/method for orb collection. Log the type name and method signature.
- Chests: Confirm `TryTakeItem` is the right hook; log chest instance IDs for stable identifiers.
- Shops: Find the purchase method; log the signature.
- Anubis: Find the reward method; log the signature.
- Age: Confirm `TryEvoWorldLevel` parameters; log current age and requirements.

**Question:** What are the exact method signatures for each check source? Are instance IDs stable?

### Hook verification (blocks enforcement)
For each enforcement hook, verify the method exists and log its signature:
- `OcSkillManager.TryLearnSkill(int, int, bool)` — CONFIRMED
- Crafting: Find the actual craft method (search for `TryCraftItem` or similar)
- Equipment equip: Find the equip method on `OcPlEquip`
- Production: Confirm `StartProcessReq` signature on `OcBuildingBase_Craft`

## Output format

JSON file with sections:
```json
{
  "timestamp": "2026-10-07T...",
  "game_version": "...",
  "equipment": {
    "items": [{"id": 123, "name": "...", "wpCategory": 5, "rank": 1, "rarity": 2, ...}]
  },
  "production": {
    "stations": [{"type": "...", "workbenchId": 1, "recipes": [...]}]
  },
  "skills": {
    "skills": [{"id": 1, "category": 2, "tier": 1, "implemented": true}]
  },
  "hooks": {
    "TryLearnSkill": {"exists": true, "signature": "..."},
    "TryCraftItem": {"exists": false, "candidates": [...]}
  }
}
```

## Runbook

1. **Backup**: Copy your save folder before running (paranoia, not required — the tool is read-only).
2. **Install**: Drop the DLL in `BepInEx/plugins/`.
3. **Launch**: Start Craftopia normally.
4. **Load**: Load your main character.
5. **Dump**: Press F9. Wait for "Diagnostic complete" in the log.
6. **Collect**: Find `craftopia_diag_*.json` in the BepInEx folder.
7. **Remove**: Delete the DLL after collecting (optional).
8. **Send**: Upload the JSON file.

## Safety guarantees

- No Harmony patches that modify game behavior (only prefix logging, no blocking).
- No inventory changes, no skill changes, no building changes.
- No network access.
- All exceptions caught and logged; never crashes the game.

