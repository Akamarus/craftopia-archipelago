using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;

namespace CraftopiaDiagnostic
{
    [BepInPlugin("com.craftopia.diagnostic", "Craftopia Diagnostic Collector", "1.3.0")]
    public class DiagnosticCollector : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("[DIAG] Diagnostic collector loaded. Press F9 in-game to run.");
            // Startup diagnostics: verify reflection lookups.
            try
            {
                var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
                if (inputType == null)
                    Logger.LogWarning("[DIAG] UnityEngine.Input not found");
                else
                {
                    // Correct overloads: GetKeyDown(KeyCode) and GetKeyDown(string)
                    var keyCodeType = Type.GetType("UnityEngine.KeyCode, UnityEngine.CoreModule");
                    var m1 = keyCodeType != null ? inputType.GetMethod("GetKeyDown", new[] { keyCodeType }) : null;
                    var m2 = inputType.GetMethod("GetKeyDown", new[] { typeof(string) });
                    Logger.LogInfo($"[DIAG] GetKeyDown(KeyCode): {(m1 != null ? "OK" : "MISSING")}");
                    Logger.LogInfo($"[DIAG] GetKeyDown(string): {(m2 != null ? "OK" : "MISSING")}");
                }
            }
            catch (Exception e) { Logger.LogWarning("[DIAG] startup check: " + e.GetType().Name); }
        }

        private void Update()
        {
            try
            {
                var inputType = Type.GetType("UnityEngine.Input, UnityEngine.InputLegacyModule");
                if (inputType == null) return;
                var keyCodeType = Type.GetType("UnityEngine.KeyCode, UnityEngine.CoreModule");
                if (keyCodeType == null) return;
                // Correct: GetKeyDown(KeyCode), F9 = 290
                var m = inputType.GetMethod("GetKeyDown", new[] { keyCodeType });
                if (m == null) return;
                object f9 = Enum.ToObject(keyCodeType, 290);
                if ((bool)m.Invoke(null, new[] { f9 }))
                {
                    RunDiagnostic();
                }
            }
            catch { }
        }

        private static string Hash(string path)
        {
            try { using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var file = System.IO.File.OpenRead(path))
                    return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant(); }
            catch { return null; }
        }

        private void RunDiagnostic()
        {
            Logger.LogInfo("[DIAG] Starting diagnostic collection...");
            var output = new Dictionary<string, object>();
            output["timestamp"] = DateTime.UtcNow.ToString("o");
            output["schema_version"] = 4;
            output["collector_version"] = "1.3.0";
            output["game_assembly_sha256"] = Hash(typeof(Oc.OcPlMaster).Assembly.Location);
            output["collector_sha256"] = Hash(Assembly.GetExecutingAssembly().Location);
            ProgressionCapture.Collect(output);

            try
            {
                var asm = typeof(Oc.OcPlMaster).Assembly;
                output["assembly_name"] = asm.GetName().Name;
                output["assembly_version"] = asm.GetName().Version.ToString();
            }
            catch (Exception e) { output["assembly_error"] = e.GetType().Name; }

            // Skills: OcSkillManager.skillList.all
            // Metadata: OcSkillManager has field Oc.SoSkillDataList skillList.
            // Oc.SoSkillDataList has field Oc.Skills.OcSkill[] all.
            // OcSkill has ID, Category, Tier, SkillName, IsImplemented.
            var skills = new List<Dictionary<string, object>>();
            string skillsStatus = "not_collected";
            try
            {
                var mgrType = Type.GetType("Oc.Skills.OcSkillManager, AD__Overcraft");
                if (mgrType == null)
                {
                    skillsStatus = "type_not_found";
                }
                else
                {
                    // Find instance: try static Inst property, then player SkillCtrl.
                    object mgrInst = null;
                    var instProp = mgrType.GetProperty("Inst",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (instProp != null) mgrInst = instProp.GetValue(null);
                    // Fallback: try via OcPlMaster.Inst.SkillCtrl or similar.
                    // (Documented as fallback; primary is Inst.)
                    if (mgrInst == null)
                    {
                        skillsStatus = "instance_not_found";
                        output["skills_note"] = "OcSkillManager instance not found; " +
                            "try with character loaded and skills UI opened";
                    }
                    else
                    {
                        var skillListField = mgrType.GetField("skillList",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (skillListField == null)
                        {
                            skillsStatus = "field_not_found";
                        }
                        else
                        {
                            var skillList = skillListField.GetValue(mgrInst);
                            if (skillList == null)
                            {
                                skillsStatus = "skilllist_null";
                            }
                            else
                            {
                                var allField = skillList.GetType().GetField("all",
                                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                var all = allField?.GetValue(skillList) as Array;
                                if (all == null)
                                {
                                    skillsStatus = "all_null";
                                }
                                else
                                {
                                    foreach (var sk in all)
                                    {
                                        if (sk == null) continue;
                                        var d = new Dictionary<string, object>();
                                        try
                                        {
                                            var t = sk.GetType();
                                            d["id"] = t.GetProperty("ID")?.GetValue(sk);
                                            d["skill_name"] = t.GetProperty("SkillName")?.GetValue(sk);
                                            // Category/Tier as both enum and raw int.
                                            var catProp = t.GetProperty("Category");
                                            var tierProp = t.GetProperty("Tier");
                                            d["category"] = catProp?.GetValue(sk)?.ToString();
                                            d["tier"] = tierProp?.GetValue(sk)?.ToString();
                                            // Raw fields for verification.
                                            var catField = t.GetField("category",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            var tierField = t.GetField("tier",
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                            d["raw_category"] = catField?.GetValue(sk);
                                            d["raw_tier"] = tierField?.GetValue(sk);
                                            d["is_implemented"] = t.GetProperty("IsImplemented")?.GetValue(sk);
                                            d["max_level"] = t.GetProperty("MaxLevel")?.GetValue(sk);
                                            d["observed"] = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            d["observed"] = false;
                                            d["error"] = ex.GetType().Name;
                                        }
                                        skills.Add(d);
                                    }
                                    skillsStatus = "collected";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                output["skills_error"] = e.GetType().Name;
                skillsStatus = "error";
            }
            output["skills_status"] = skillsStatus;
            output["skills"] = skills;
            output["skill_count"] = skills.Count;

            // Missions: OcMissionManager.Inst with FlattenHierarchy
            var missions = new List<Dictionary<string, object>>();
            string missionsStatus = "not_collected";
            try
            {
                var mgrType = Type.GetType("Oc.Missions.OcMissionManager, AD__Overcraft");
                if (mgrType != null)
                {
                    var instProp = mgrType.GetProperty("Inst",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    if (instProp == null)
                    {
                        missionsStatus = "inst_not_found";
                        output["missions_note"] = "Inst property not found even with FlattenHierarchy";
                    }
                    else
                    {
                        var inst = instProp.GetValue(null);
                        if (inst == null)
                        {
                            missionsStatus = "inst_null";
                        }
                        else
                        {
                            var getMissions = mgrType.GetMethod("GetMissionLists");
                            var arr = getMissions?.Invoke(inst, null) as Array;
                            if (arr != null)
                            {
                                foreach (var m in arr)
                                {
                                    if (m == null) continue;
                                    var d = new Dictionary<string, object>();
                                    try
                                    {
                                        var t = m.GetType();
                                        d["class"] = t.FullName;
                                        d["requirements_and_rewards"] = CatalogFields.Read(m, new[] {
                                            "worldLevel", "targetValue", "status", "achievementID",
                                            "rewardItemID_A", "rewardItemCount_A", "rewardItemID_B", "rewardItemCount_B",
                                            "rewardGold", "skillPoint", "rewardExp", "lithograph"
                                        });
                                        d["id"] = t.GetProperty("ID")?.GetValue(m);
                                        d["title"] = t.GetProperty("Title")?.GetValue(m);
                                        d["category"] = t.GetProperty("Category")?.GetValue(m);
                                        d["observed"] = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        d["observed"] = false;
                                        d["error"] = ex.GetType().Name;
                                    }
                                    missions.Add(d);
                                }
                                missionsStatus = "collected";
                            }
                            else missionsStatus = "getmissionlists_null";
                        }
                    }
                }
                else missionsStatus = "type_not_found";
            }
            catch (Exception e)
            {
                output["missions_error"] = e.GetType().Name;
                missionsStatus = "error";
            }
            output["missions_status"] = missionsStatus;
            output["missions"] = missions;
            output["mission_count"] = missions.Count;

            // Write JSON with proper escaping
            try
            {
                string dir = System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location);
                string path = System.IO.Path.Combine(dir,
                    $"craftopia_diag_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                System.IO.File.WriteAllText(path, JsonWriter.Stringify(output));
                Logger.LogInfo($"[DIAG] Complete: {missions.Count} missions, status={missionsStatus}. Wrote {path}");
            }
            catch (Exception e)
            {
                Logger.LogError("[DIAG] Write failed: " + e.GetType().Name);
            }
        }
    }

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
            if (obj is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (obj is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
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
            return Escape(obj.ToString());
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
