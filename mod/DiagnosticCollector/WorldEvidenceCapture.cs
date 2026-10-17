using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CraftopiaDiagnostic
{
    internal static class WorldEvidenceCapture
    {
        internal static readonly string[] TreasureFields = { "_PosHash", "_prefabPosHash", "_IgnoreSave", "_resetOnDayEnd", "_IsOpenComplete", "_OpenNum", "_Count", "_Phase", "_RarityType", "_Rarity" };
        internal static readonly string[] OrbFields = { "_guid", "_firstTook", "_taken" };
        internal static readonly string[] RandomTreasureFields = { "_posHash", "_fieldID", "_oncePickup", "_isItemDropped", "_rewardDropRate" };
        internal static readonly string[] EnemyFields = { "_EmType", "_Level", "_GUID", "_SpawneeGUID", "<EmDungeonId>k__BackingField", "_OnceAddExp", "_IgnoreDropItem" };

        internal static object[] Loaded(string typeName, bool assets)
        {
            var type = typeof(Oc.OcPlMaster).Assembly.GetType(typeName, false);
            if (type == null) throw new TypeLoadException(typeName);
            // These APIs enumerate already loaded objects. They do not load assets.
            return assets ? UnityEngine.Resources.FindObjectsOfTypeAll(type) : UnityEngine.Object.FindObjectsOfType(type);
        }
        private static readonly DiscoveryCache<object> worldCache = new DiscoveryCache<object>();
        internal static object FindWorld() => worldCache.Get(DateTime.UtcNow,
            value => value is UnityEngine.Object unity && unity != null, DiscoverWorld);
        private static object DiscoverWorld()
        {
            var values = Loaded("Oc.Maps.OcWorldManager", false);
            return values.Length == 1 ? values[0] : null;
        }
        internal static Dictionary<string, object> WorldContext(object manager)
        {
            var selected = CatalogFields.Field(manager, "_selectedWorld");
            var result = EvidenceProjection.Read(selected, new[] { "worldLevel", "worldSeed", "gameMode", "dataVersion" });
            // No world/character name, path, or raw creation date. This is a
            // comparison hint, not a proven global identity (copies can match).
            var date = CatalogFields.Field(selected, "createdDate");
            var seed = CatalogFields.Field(selected, "worldSeed");
            if (date is long created && created > 0 && seed is byte s)
                using (var sha = SHA256.Create()) result["comparison_fingerprint"] =
                    BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(created.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + s))).Replace("-", "");
            result["identity_status"] = "comparison_only_not_global_id";
            return result;
        }
        private static void Section(Dictionary<string, object> output, string key, string type, bool assets, string[] fields)
        {
            try
            {
                var objects = Loaded(type, assets); var rows = new List<object>();
                foreach (var value in objects)
                {
                    if (rows.Count >= 1024) break;
                    try {
                        var row=EvidenceProjection.Read(value, fields);
                        if(assets && value is UnityEngine.Object asset) row["asset_name"]=asset.name;
                        if(!assets && value is UnityEngine.Component component &&
                            (key=="soul_orbs" || key=="treasure_boxes" || key=="random_chests" || key=="challenge_starters")) {
                            var pos=component.transform.position;
                            row["scene_position"]=new Dictionary<string,object>{["x"]=pos.x,["y"]=pos.y,["z"]=pos.z};
                        }
                        rows.Add(row);
                    }
                    catch (Exception e) { rows.Add(new Dictionary<string, object> { ["status"] = "row_error", ["error"] = e.GetType().Name }); }
                }
                output[key] = new Dictionary<string, object> { ["status"] = objects.Length == 0 ? "none_loaded" : "collected",
                    ["observed_count"] = objects.Length, ["truncated"] = rows.Count < objects.Length,
                    ["scope"] = assets ? "already_loaded_assets_only" : "active_scene_objects_only", ["rows"] = rows };
            }
            catch (Exception e) { output[key] = new Dictionary<string, object> { ["status"] = "unavailable", ["error"] = e.GetType().Name }; }
        }
        internal static void Collect(Dictionary<string, object> output)
        {
            var evidence = new Dictionary<string, object>(); output["world_evidence"] = evidence;
            try {
                var towers=Loaded("Oc.OcBldg_WorldHeritage",false); var rows=new List<object>();
                foreach(var tower in towers) {
                    if(!(CatalogFields.Field(tower,"_IsTower") is bool isTower) || !isTower) continue;
                    if(rows.Count>=128) break;
                    var row=ProgressionEvidence.Tower(tower);
                    if(tower is UnityEngine.Component component) {
                        var pos=component.transform.position;
                        row["scene_position"]=new Dictionary<string,object>{["x"]=pos.x,["y"]=pos.y,["z"]=pos.z};
                    }
                    rows.Add(row);
                }
                evidence["towers"]=new Dictionary<string,object>{["rows"]=rows,["scope"]="active_loaded_towers_only",
                    ["heritage_objects_observed"]=towers.Length,["limit"]=128,["possibly_truncated"]=rows.Count>=128};
            } catch(Exception e) { evidence["towers"]=new Dictionary<string,object>{["status"]="unavailable",["error"]=e.GetType().Name}; }
            Section(evidence, "soul_orbs", "Oc.OcSoulOrb_Spawner", false, OrbFields);
            Section(evidence, "treasure_boxes", "Oc.OcGimmick_TreasureBoxBase", false, TreasureFields);
            // Separate family: respawning overworld treasure is excluded from AP checks.
            Section(evidence, "random_chests", "Oc.OcObj_RandomTreasure", false, RandomTreasureFields);
            Section(evidence, "challenge_starters", "Oc.OcBldg_ChallengeStarter", false, new[] { "challengeData" });
            Section(evidence, "challenge_catalog", "Oc.SoChallengeData", true, null);
            Section(evidence, "quest_catalog", "Oc.Quest.SoQuestData", true, null);
            Section(evidence, "shop_catalog", "Oc.SoShopList", true, new[] { "_saleInfos", "_cashier" });
            Section(evidence, "shop_sheets", "Oc.Item.UI.OcUI_ShopItemSheet", false, new[] { "btnIndex", "<Price>k__BackingField", "_saleInfo" });
            Section(evidence, "anubis_rewards", "Oc.SoSoulOrbRewardData", true, null);
            Section(evidence, "anubis_ui", "Oc.Item.UI.OcUI_SoulOrbUI", false, new[] { "_selectedButtonId", "_exchangeCount" });
            Section(evidence, "anubis_exchange_ui", "Oc.Item.UI.OcUI_StatusUpPageSoulOrb", false, new[] { "_exchangeCount", "_lastExchange", "_totalRewardCount" });
            Section(evidence, "mission_categories", "Oc.Missions.SoMissionCategory", true,
                new[] { "id", "title", "order", "skillPoint", "rewardInventoryID", "rewardPersonalChest" });
            Section(evidence, "mission_category_state", "Oc.Missions.OcMissionManager", false,
                new[] { "categoryTookList", "_dataLoaded" });
            Section(evidence, "dungeon_catalog", "Oc.Dungeon.OcDungeonSo", true, new[] { "sourceId", "bossEmType", "bossLevel", "isCombatDungeon", "battleDungeonType", "dungeontheme" });
            Section(evidence, "dungeon_progress", "Oc.Dungeon.OcDungeonManager", false, new[] { "DungeonClearedList", "<BossRushBestFloor>k__BackingField" });
            Section(evidence, "achievements", "Oc.Achievements.OcAchievement", true, null);
            Section(evidence, "loaded_enemies", "Oc.Em.OcEm", false, EnemyFields);
            try { evidence["world_context"] = WorldContext(FindWorld()); }
            catch (Exception e) { evidence["world_context"] = new Dictionary<string, object> { ["status"] = "unavailable", ["error"] = e.GetType().Name }; }
            evidence["limitations"] = new[] {
                "Loaded/active objects only; missing rows are not proof of absence.",
                "Raw GUIDs and position hashes are candidates, not assigned AP locations.",
                "Random/respawning chests are diagnostic-only and excluded from the AP check pool.",
                "Comparison fingerprint is not a global world ID; copied worlds can match.",
                "Field-only projection; dynamic reward getters are never evaluated.",
                "Truncated and not_projected fields require follow-up; no inferred defaults.",
                "No inventory contents, character/account identifiers, player/world names or credentials are traversed. Asset names and world-object positions are included."
            };
        }
    }
}
