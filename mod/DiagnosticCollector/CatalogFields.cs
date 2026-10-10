using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace CraftopiaDiagnostic
{
    // Field-only projections: no arbitrary getters, recursion into game objects,
    // identity issuance, inventory mutation, or player/save-state traversal.
    public static class CatalogFields
    {
        public static Dictionary<string, object> StationIdentity(object station)
        {
            var item = Field(station, "<_BldgItem_InItemSlot>k__BackingField");
            var data = Field(item, "<data>k__BackingField");
            return new Dictionary<string, object> {
                ["type"] = station?.GetType().FullName,
                ["item_id"] = Primitive(Field(data, "id")),
                ["family_id"] = Primitive(Field(data, "familyId"))
            };
        }
        public static object Field(object instance, string name)
        {
            for (Type t = instance?.GetType(); t != null; t = t.BaseType)
            {
                var field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(instance);
            }
            return null;
        }
        public static Dictionary<string, object> Read(object instance, string[] names)
        {
            var row = new Dictionary<string, object>();
            foreach (string name in names) row[name] = Primitive(Field(instance, name));
            return row;
        }
        private static object Primitive(object value)
        {
            if (value == null || value is string || value is bool || value is int || value is long ||
                value is short || value is byte || value is float || value is double) return value;
            if (value.GetType().IsEnum) return Convert.ToInt32(value);
            if (value is Array array && array.Length <= 1024)
            {
                var result = new List<object>();
                foreach (var item in array) result.Add(item is Array ? null : PrimitiveScalar(item));
                return result;
            }
            return null;
        }
        private static object PrimitiveScalar(object value) => value is Array ? null :
            (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum ? Primitive(value) : null);
    }
}
