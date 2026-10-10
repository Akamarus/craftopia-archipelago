using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // Data only. The hook must read these fields before any cost or transfer.
    public sealed class EquipmentSnapshot
    {
        public int Id, Status, WeaponCategory, Slot, Workbench, WorkbenchLevel, Prefab, InventoryTab;
        public bool OldData, CannotUse;
        internal EquipmentSnapshot Copy() => (EquipmentSnapshot)MemberwiseClone();
        internal bool Matches(EquipmentSnapshot other) => other != null && Id == other.Id
            && Status == other.Status && WeaponCategory == other.WeaponCategory && Slot == other.Slot
            && Workbench == other.Workbench && WorkbenchLevel == other.WorkbenchLevel
            && Prefab == other.Prefab && InventoryTab == other.InventoryTab
            && OldData == other.OldData && CannotUse == other.CannotUse;
    }

    public sealed class EquipmentCatalog
    {
        public const string CatalogId = "equipment_20261010_r1";
        public const string AssemblyHash = "88db99ebe9fbf1296c74465b6f32467cdbc71711235f925532d9bcf6f2ccebea";
        private sealed class Rule
        {
            internal EquipmentSnapshot Item;
            internal string Decision, Category;
            internal int Tier, Age;
        }
        private readonly Dictionary<int, Rule> rules = new Dictionary<int, Rule>();
        private EquipmentCatalog() { }
        public int Count => rules.Count;
        public EquipmentSnapshot Snapshot(int id) => rules.TryGetValue(id, out var rule) ? rule.Item.Copy() : null;
        public string Decision(int id) => rules.TryGetValue(id, out var rule) ? rule.Decision : null;

        private static int Integer(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || !(value is long number)
                || number < int.MinValue || number > int.MaxValue) throw new FormatException("Invalid " + key);
            return (int)number;
        }
        private static bool Boolean(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || !(value is bool result)) throw new FormatException("Invalid " + key);
            return result;
        }
        private static string Text(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var value) || !(value is string result)) throw new FormatException("Invalid " + key);
            return result;
        }
        private static void Hash(Dictionary<string, object> row, string key)
        {
            var value = Text(row, key);
            if (value.Length != 64) throw new FormatException("Invalid " + key);
            foreach (char c in value) if (!(c >= '0' && c <= '9') && !(c >= 'a' && c <= 'f'))
                throw new FormatException("Invalid " + key);
        }
        private static string Category(EquipmentSnapshot item)
        {
            if (item.Slot == 4) return "Head";
            if (item.Slot == 5) return "Body";
            if (item.Slot < 0 || item.Slot > 3) return null;
            switch (item.WeaponCategory)
            {
                case 5: return "Sword"; case 6: return "Bow"; case 7: return "Shield";
                case 10: return "Lance"; case 28: return "SwordTwoHand"; case 35: return "Gun";
                case 36: return "Katana"; case 37: return "MagicStaff"; case 43: return "VacuumGun";
                case 38: return "AssistEquip";
                default: return null;
            }
        }
        public static EquipmentCatalog Parse(string json)
        {
            try
            {
                var root = MiniJson.ParseObject(json);
                if (root == null || Text(root, "catalog_id") != CatalogId || Integer(root, "schema_version") != 1
                    || Text(root, "game_assembly_sha256") != AssemblyHash) throw new FormatException("Equipment catalog identity mismatch");
                Hash(root, "source_sha256"); Hash(root, "collector_sha256");
                if (!root.TryGetValue("items", out var list) || !(list is List<object> rows) || rows.Count == 0)
                    throw new FormatException("Missing equipment rows");
                var result = new EquipmentCatalog();
                foreach (var value in rows)
                {
                    if (!(value is Dictionary<string, object> row)) throw new FormatException("Invalid equipment row");
                    var item = new EquipmentSnapshot {
                        Id = Integer(row, "id"), Status = Integer(row, "status"), WeaponCategory = Integer(row, "wpCategory"),
                        Slot = Integer(row, "equipSlot"), Workbench = Integer(row, "workbenchId"),
                        WorkbenchLevel = Integer(row, "workbenchLv"), Prefab = Integer(row, "existPrefab"),
                        InventoryTab = Integer(row, "inventoryTabId"), OldData = Boolean(row, "isOldData"), CannotUse = Boolean(row, "cantUse")
                    };
                    if (item.Id < 1 || item.Id > short.MaxValue || result.rules.ContainsKey(item.Id))
                        throw new FormatException("Invalid or duplicate equipment ID");
                    if (item.WeaponCategory < 0 || item.WeaponCategory > 44 || item.Slot < 0 || item.Slot > 9
                        || item.Prefab < 0 || item.Prefab > 4 || item.InventoryTab < 0 || item.InventoryTab > 9)
                        throw new FormatException("Unrecognized equipment enum value");
                    string category = Category(item);
                    bool craftable = category != null && category != "AssistEquip" && (item.Status == 1 || item.Status == 2)
                        && !item.OldData && !item.CannotUse && (item.Prefab == 3 || item.Prefab == 4)
                        && (item.InventoryTab == 1 || item.InventoryTab == 2 || item.InventoryTab == 7 || item.InventoryTab == 9)
                        && item.Workbench > 0 && item.Workbench != 20001 && item.WorkbenchLevel >= 0 && item.WorkbenchLevel <= 9;
                    string decision = category == null ? "vanilla" : craftable ? "progressive" : "unsupported";
                    if (Text(row, "decision") != decision || (category != null && Text(row, "category") != category))
                        throw new FormatException("Inconsistent equipment classification");
                    var rule = new Rule { Item = item, Decision = decision, Category = category };
                    if (craftable)
                    {
                        rule.Tier = Integer(row, "tier"); rule.Age = Integer(row, "minimum_age");
                        if (rule.Age != item.WorkbenchLevel || rule.Tier != rule.Age + 1)
                            throw new FormatException("Inconsistent age/tier projection");
                    }
                    else if (row.ContainsKey("tier") || row.ContainsKey("minimum_age"))
                        throw new FormatException("Unsupported inferred tier");
                    result.rules.Add(item.Id, rule);
                }
                return result;
            }
            catch (FormatException) { throw; }
            catch (Exception error) { throw new FormatException("Invalid equipment catalog", error); }
        }

        internal bool Check(EquipmentSnapshot live, PermissionManager permissions, Func<int> currentAge, out string reason)
        {
            reason = "Equipment blocked: unknown item or changed game metadata.";
            if (live == null || !rules.TryGetValue(live.Id, out var rule) || !rule.Item.Matches(live)) return false;
            if (rule.Decision == "vanilla") return true;
            reason = "Equipment blocked: this combat item has no reviewed tier.";
            if (rule.Decision != "progressive") return false;
            reason = "Equipment blocked: requires " + rule.Category + " tier " + rule.Tier + ".";
            if (permissions == null || !permissions.CanUseEquipment(rule.Category, rule.Tier)) return false;
            reason = "Equipment blocked: requires world age " + rule.Age + ".";
            int age = currentAge();
            return age >= 0 && age <= 9 && age >= rule.Age;
        }
    }

    public static class EquipmentEnforcement
    {
        // Evaluate again at the confirmed loadout callback, before its first
        // unequip. Approval when opening the confirmation dialog is stale.
        public static bool AllowLoadout(bool enabled, EquipmentCatalog catalog, PermissionManager permissions,
            Func<IEnumerable<EquipmentSnapshot>> readItems, Func<int> readAge, Action<string> diagnostic)
        {
            if (!enabled) return true;
            try
            {
                var items = readItems();
                if (catalog == null || items == null) throw new InvalidOperationException();
                foreach (var item in items)
                    if (!Allow(true, catalog, permissions, () => item, readAge, diagnostic)) return false;
                return true;
            }
            catch
            {
                try { diagnostic?.Invoke("Equipment loadout blocked: evaluation failed before transfer."); } catch { }
                return false;
            }
        }

        // True means run the original checks, never force success or create equipment.
        // Feature activation is separate from connectivity and delivery eligibility.
        public static bool Allow(bool enabled, EquipmentCatalog catalog, PermissionManager permissions,
            Func<EquipmentSnapshot> readItem, Func<int> readAge, Action<string> diagnostic)
        {
            if (!enabled) return true;
            string reason = "Equipment blocked: permission evaluation unavailable.";
            try
            {
                if (catalog != null && catalog.Check(readItem(), permissions, readAge, out reason)) return true;
            }
            catch { reason = "Equipment blocked: permission evaluation failed."; }
            try { diagnostic?.Invoke(reason); } catch { }
            return false;
        }
    }
}
