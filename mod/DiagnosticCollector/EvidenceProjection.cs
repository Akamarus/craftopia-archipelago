using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CraftopiaDiagnostic
{
    // Copies fields only. No property getters, methods, arbitrary enumerators,
    // ToString calls, Unity hierarchy walks, or writes to the supplied objects.
    public static class EvidenceProjection
    {
        public static Dictionary<string, object> Read(object value, string[] names)
        { int budget = 2048; return ReadObject(value, names, 0, ref budget); }

        private static Dictionary<string, object> Status(string status) =>
            new Dictionary<string, object> { ["status"] = status };

        private static Dictionary<string, object> ReadObject(object value, string[] names, int depth, ref int budget)
        {
            if (value == null) return Status("null");
            var result = new Dictionary<string, object> { ["type"] = value.GetType().FullName };
            var fields = new Dictionary<string, object>(); result["fields"] = fields;
            int count = 0;
            if (names != null) foreach (string name in names)
            {
                FieldInfo field = Find(value.GetType(), name);
                fields[name] = field == null ? Status("field_missing") : Project(field.GetValue(value), depth + 1, ref budget);
            }
            else for (Type type = value.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                // Stop before Unity/Mirror implementation internals.
                if (type.Namespace != null && (type.Namespace.StartsWith("UnityEngine") || type.Namespace.StartsWith("Mirror"))) break;
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (++count > 128 || budget <= 0) { result["truncated"] = true; return result; }
                    if (fields.ContainsKey(field.Name)) continue;
                    fields[field.Name] = Project(field.GetValue(value), depth + 1, ref budget);
                }
            }
            return result;
        }
        private static FieldInfo Find(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }
        private static bool Nested(Type t)
        {
            string n = t.FullName ?? "";
            return n == "Oc.Item.ItemData" || n == "Oc.OcItemID" || n == "Oc.OcDungeonID" || n == "Oc.SoChallengeData"
                || n == "Oc.NormalShopSaleInfo" || n == "Oc.RandomShopSaleInfo"
                || n == "Oc.Missions.MissionSaveData" || n == "Oc.Achievements.OcAchievementSaveData"
                || n.StartsWith("Oc.Skills.OcSkillLockCondition_")
                || n.StartsWith("Oc.SoChallengeData+") || n.StartsWith("Oc.SoSoulOrbRewardData+")
                || n.StartsWith("Oc.Quest.SoQuestData+") || n == "Oc.Quest.SoQuestData"
                || n == "Oc.SoEnemy" || n == "Oc.SoEnemyData"
                || (n.StartsWith("Oc.Quest.") && (n.EndsWith("RequiredInfo") || n.EndsWith("RequirInfo")))
                || n.StartsWith("Oc.SerializeGetter`");
        }
        private static object Project(object value, int depth, ref int budget)
        {
            if (--budget < 0) return Status("budget_limit");
            if (value == null) return null;
            if (value is string text) return text.Length <= 512 ? text : text.Substring(0, 512) + "[truncated]";
            if (value is float f) return float.IsNaN(f) || float.IsInfinity(f) ? null : (object)f;
            if (value is double d) return double.IsNaN(d) || double.IsInfinity(d) ? null : (object)d;
            Type type = value.GetType();
            if (type.IsPrimitive || value is decimal) return value;
            if (type.IsEnum) return new Dictionary<string, object> {
                ["enum_type"] = type.FullName, ["raw"] = Convert.ChangeType(value, Enum.GetUnderlyingType(type)),
                ["name"] = Enum.GetName(type, value) };
            if (depth > 3) return Status("depth_limit");
            // Only framework arrays/lists: never run a custom IEnumerable.
            if (value is Array || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var list = (IList)value; var rows = new List<object>();
                for (int i=0; i<Math.Min(list.Count, 128) && budget > 0; i++) rows.Add(Project(list[i], depth + 1, ref budget));
                return new Dictionary<string, object> { ["count"] = list.Count, ["items"] = rows, ["truncated"] = rows.Count < list.Count };
            }
            if (Nested(type)) return ReadObject(value, type.FullName == "Oc.Item.ItemData" ? new[] {"id", "familyId", "workbenchId", "workbenchLv"} : null, depth, ref budget);
            return new Dictionary<string, object> { ["status"] = "not_projected", ["type"] = type.FullName };
        }
    }
}
