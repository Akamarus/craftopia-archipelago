using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;

namespace CraftopiaDiagnostic
{
    [BepInPlugin("com.craftopia.diagnostic", "Craftopia Diagnostic Collector", "1.5.2")]
    public class DiagnosticCollector : BaseUnityPlugin
    {
        private ObservationHooks observations;
        private DateTime nextFlush, nextCapture;
        private MethodInfo keyDown;
        private object f9Key;
        private string evidenceDirectory;
        private void Awake()
        {
            Logger.LogInfo("[DIAG] Collector 1.5.2 loaded. Press F9 for catalogs; read-only action observations save automatically.");
            evidenceDirectory=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "diagnostics");
            try {
                observations=new ObservationHooks(evidenceDirectory, message=>Logger.LogWarning("[DIAG] "+message));
                observations.GameHash=Hash(typeof(Oc.OcPlMaster).Assembly.Location);
                observations.CollectorHash=Hash(Assembly.GetExecutingAssembly().Location);
                observations.Install();
                Logger.LogInfo("[DIAG] Observer coverage: "+JsonWriter.Stringify(observations.Status()));
            } catch(Exception e) { Logger.LogWarning("[DIAG] Observer setup incomplete: "+e.GetType().Name); }
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
                    keyDown=m1;
                    if(keyCodeType!=null) f9Key=Enum.ToObject(keyCodeType,290);
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
                observations?.PollProgression();
                if(DateTime.UtcNow>=nextFlush) {
                    nextFlush=DateTime.UtcNow.AddSeconds(2);
                    if(observations!=null) {
                        try { observations.World=WorldEvidenceCapture.FindWorld(); observations.WorldObservedAt=DateTime.UtcNow; }
                        catch { observations.World=null; observations.WorldObservedAt=default(DateTime); }
                        observations.Flush();
                    }
                }
                if (keyDown != null && DateTime.UtcNow>=nextCapture && (bool)keyDown.Invoke(null, new[] { f9Key }))
                {
                    nextCapture=DateTime.UtcNow.AddSeconds(5);
                    RunDiagnostic();
                }
            }
            catch { }
        }

        private void OnDestroy() { try { observations?.Dispose(); } catch { } }

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
            output["schema_version"] = 5;
            output["collector_version"] = "1.5.2";
            string captureId=Guid.NewGuid().ToString("N");
            output["capture_id"]=captureId;
            if(observations!=null) { observations.Capture=captureId; observations.Flush(); output["observations"]=observations.Status(); }
            output["game_assembly_sha256"] = Hash(typeof(Oc.OcPlMaster).Assembly.Location);
            output["collector_sha256"] = Hash(Assembly.GetExecutingAssembly().Location);
            ProgressionCapture.Collect(output);
            WorldEvidenceCapture.Collect(output);

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
                                            d["requirements"] = EvidenceProjection.Read(sk, new[] {
                                                "_skillLockCondition", "levelUpCost_lv1", "levelUpCost_lv2", "levelUpCost_lv3",
                                                "levelUpCost_lv4", "levelUpCost_lv5", "levelUpCost_lv6", "_preLocked"
                                            });
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
                                        d["raw_fields"] = EvidenceProjection.Read(m, null);
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
                string dir = evidenceDirectory;
                System.IO.Directory.CreateDirectory(dir);
                string path = System.IO.Path.Combine(dir,
                    $"craftopia_diag_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{captureId}.json");
                System.IO.File.WriteAllText(path+".tmp", JsonWriter.Stringify(output));
                System.IO.File.Move(path+".tmp",path);
                Logger.LogInfo($"[DIAG] Complete: {missions.Count} missions, status={missionsStatus}. Wrote {path}");
            }
            catch (Exception e)
            {
                Logger.LogError("[DIAG] Write failed: " + e.GetType().Name);
            }
        }
    }

}
