using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Minimal JSON writer/parser, just enough for our fixed bridge protocol.
    /// Values: string, long, bool, List&lt;object&gt;, Dictionary&lt;string, object&gt;, null.
    /// </summary>
    internal static class MiniJson
    {
        public static string Stringify(Dictionary<string, object> obj)
        {
            var sb = new StringBuilder();
            WriteValue(sb, obj);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is string s) { WriteString(sb, s); return; }
            if (v is bool b) { sb.Append(b ? "true" : "false"); return; }
            if (v is int i) { sb.Append(i.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is long l) { sb.Append(l.ToString(CultureInfo.InvariantCulture)); return; }
            if (v is Dictionary<string, object> d)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in d)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    WriteValue(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }
            if (v is System.Collections.IEnumerable e && !(v is string))
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in e)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteValue(sb, item);
                }
                sb.Append(']');
                return;
            }
            WriteString(sb, v.ToString());
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) { sb.Append("\\u"); sb.Append(((int)c).ToString("x4")); }
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        public static Dictionary<string, object> ParseObject(string json)
        {
            var p = new Parser(json);
            p.SkipWs();
            var v = p.ParseValue();
            p.RequireEnd();
            var d = v as Dictionary<string, object>;
            if (d == null) throw new FormatException("top-level JSON is not an object");
            return d;
        }

        public static string GetString(Dictionary<string, object> d, string key, string def = "")
        {
            object v;
            if (d.TryGetValue(key, out v) && v is string s) return s;
            return def;
        }

        public static long GetLong(Dictionary<string, object> d, string key, long def = 0)
        {
            object v;
            if (d.TryGetValue(key, out v))
            {
                if (v is long l) return l;
                if (v is int i) return i;
                if (v is double f) return (long)f;
            }
            return def;
        }

        public static List<long> GetLongList(Dictionary<string, object> d, string key)
        {
            var result = new List<long>();
            object v;
            if (d.TryGetValue(key, out v) && v is List<object> l)
            {
                foreach (var item in l)
                {
                    if (item is long ll) result.Add(ll);
                    else if (item is int ii) result.Add(ii);
                    else if (item is double dd) result.Add((long)dd);
                }
            }
            return result;
        }

        private sealed class Parser
        {
            private readonly string s;
            private int i;
            public Parser(string s) { this.s = s; }
            public void RequireEnd() { SkipWs(); if (i != s.Length) throw new FormatException("trailing JSON"); }

            public void SkipWs()
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            }

            public object ParseValue()
            {
                SkipWs();
                if (i >= s.Length) throw new FormatException("unexpected end of JSON");
                char c = s[i];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ParseNumber();
            }

            private Dictionary<string, object> ParseObject()
            {
                var d = new Dictionary<string, object>();
                i++; // {
                SkipWs();
                if (i < s.Length && s[i] == '}') { i++; return d; }
                while (true)
                {
                    SkipWs();
                    string key = ParseString();
                    SkipWs();
                    if (i >= s.Length || s[i] != ':') throw new FormatException("expected ':'");
                    i++;
                    if (d.ContainsKey(key)) throw new FormatException("duplicate key");
                    d[key] = ParseValue();
                    SkipWs();
                    if (i >= s.Length) throw new FormatException("unterminated object");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; return d; }
                    throw new FormatException("expected ',' or '}'");
                }
            }

            private List<object> ParseArray()
            {
                var l = new List<object>();
                i++; // [
                SkipWs();
                if (i < s.Length && s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(ParseValue());
                    SkipWs();
                    if (i >= s.Length) throw new FormatException("unterminated array");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; return l; }
                    throw new FormatException("expected ',' or ']'");
                }
            }

            private string ParseString()
            {
                if (i >= s.Length || s[i] != '"') throw new FormatException("expected string");
                var sb = new StringBuilder();
                i++; // opening "
                while (i < s.Length)
                {
                    char c = s[i++];
                    if (c == '"') return sb.ToString();
                    if (c == '\\')
                    {
                        if (i >= s.Length) break;
                        char e = s[i++];
                        switch (e)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (i + 4 > s.Length) throw new FormatException("bad \\u escape");
                                sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                                i += 4;
                                break;
                            case 'b': sb.Append((char)8); break;
                            case 'f': sb.Append((char)12); break;
                            default: throw new FormatException("invalid escape");
                        }
                    }
                    else { if (c < 32) throw new FormatException("control character"); sb.Append(c); }
                }
                throw new FormatException("unterminated string");
            }

            private object ParseNumber()
            {
                int start = i;
                while (i < s.Length && "-+0123456789.eE".IndexOf(s[i]) >= 0) i++;
                string num = s.Substring(start, i - start);
                if (!System.Text.RegularExpressions.Regex.IsMatch(num, @"^-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?$")) throw new FormatException("invalid number");
                long l;
                if (long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
                double f;
                if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) return f;
                throw new FormatException("bad number: " + num);
            }

            private void Expect(string word)
            {
                if (s.Length - i < word.Length || s.Substring(i, word.Length) != word)
                    throw new FormatException("expected '" + word + "'");
                i += word.Length;
            }
        }
    }
}
