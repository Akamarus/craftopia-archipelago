using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CraftopiaArchipelago
{
    // Field-only adapter: never creates items, loads prefabs, or issues identities.
    public static class EquipmentRead
    {
        public static object Field(object value, string name)
        {
            for (var t = value?.GetType(); t != null; t = t.BaseType)
            {
                var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(value);
            }
            throw new MissingFieldException(name);
        }
        static int Number(object value, string name)
        {
            var n = Field(value, name);
            if (n is int i) return i;
            if (n != null && n.GetType().IsEnum) return Convert.ToInt32(n);
            throw new FormatException(name);
        }
        static bool Flag(object value, string name)
            => Field(value, name) is bool b ? b : throw new FormatException(name);
        public static EquipmentSnapshot Item(object value) => new EquipmentSnapshot {
            Id=Number(value,"id"), Status=Number(value,"status"), WeaponCategory=Number(value,"wpCategory"),
            Slot=Number(value,"equipSlot"), Workbench=Number(value,"workbenchId"),
            WorkbenchLevel=Number(value,"workbenchLv"), Prefab=Number(value,"existPrefab"),
            InventoryTab=Number(value,"inventoryTabId"), OldData=Flag(value,"isOldData"), CannotUse=Flag(value,"cantUse")
        };
        public static List<EquipmentSnapshot> Loadout(IEnumerable entries, IEnumerable liveItems)
        {
            if (entries == null || liveItems == null) throw new FormatException("Missing loadout data");
            var byId = new Dictionary<int, object>();
            foreach (var item in liveItems)
            {
                if (item == null) continue;
                int id = Number(item,"id");
                if (id <= 0) continue;
                if (byId.ContainsKey(id)) throw new FormatException("Duplicate live item");
                byId.Add(id,item);
            }
            var slots = new HashSet<int>();
            var result = new List<EquipmentSnapshot>();
            foreach (var entry in entries)
            {
                int slot = Number(entry,"equipSlot"), id = Number(entry,"itemId");
                if (slot < 0 || slot > 9 || !slots.Add(slot) || id < 0 || Flag(entry,"isOld"))
                    throw new FormatException("Unsupported loadout entry");
                if (id == 0) continue; // Empty slot: removal remains available.
                if (!byId.TryGetValue(id,out var item)) throw new FormatException("Unknown loadout item");
                result.Add(Item(item));
            }
            return result;
        }
    }
}
