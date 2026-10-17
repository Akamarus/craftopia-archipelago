using System;
using System.Collections.Generic;
namespace CraftopiaDiagnostic
{
    internal static class JsonWriter
    {
        public static string Stringify(object obj)
        {
            if (obj == null) return "null";
            if (obj is string s) return Escape(s);
            if (obj is bool b) return b ? "true" : "false";
            // Culture-invariant numeric formatting
            if (obj is int i) return i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is long l) return l.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is float f) return float.IsNaN(f) || float.IsInfinity(f) ? "null" : f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is double d) return double.IsNaN(d) || double.IsInfinity(d) ? "null" : d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is byte || obj is sbyte || obj is short || obj is ushort || obj is uint || obj is ulong || obj is decimal)
                return ((IFormattable)obj).ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            if (obj is Dictionary<string, object> dict)
            {
                var parts = new List<string>();
                foreach (var kv in dict)
                    parts.Add(Escape(kv.Key) + ":" + Stringify(kv.Value));
                return "{" + string.Join(",", parts) + "}";
            }
            if (obj is System.Collections.IEnumerable e && !(obj is string))
            {
                var parts = new List<string>();
                foreach (var item in e) parts.Add(Stringify(item));
                return "[" + string.Join(",", parts) + "]";
            }
            if (obj.GetType().IsEnum) return Escape(Enum.GetName(obj.GetType(), obj) ?? "unknown_enum");
            return "null";
        }

        private static string Escape(string s)
        {
            var sb = new System.Text.StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 32) sb.Append($"\\u{(int)c:X4}");
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }
    }
}
