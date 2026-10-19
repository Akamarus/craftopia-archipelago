using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using HarmonyLib;

namespace CraftopiaArchipelago
{
    internal static class SoulOrbHooks
    {
        const string Catalog="soul_orbs_20261015_r1";
        static readonly Dictionary<int,long> frozen=new Dictionary<int,long>();
        static readonly HashSet<int> selected=new HashSet<int>();
        static bool requested,valid;
        static string seed,slot;
        static long epoch;
        static string BindingPath(int character) => Path.Combine(BepInEx.Paths.ConfigPath,
            "craftopia-orb-character-"+character.ToString(System.Globalization.CultureInfo.InvariantCulture)+".bound");
        static string BindingIdentity => MiniJson.Stringify(new Dictionary<string,object>{{"seed",seed},{"slot",slot},{"epoch",epoch}});
        internal static bool MarkCharacter(int character) => character!=0 && requested && valid && SoulOrbCharacterBinding.Mark(BindingPath(character),BindingIdentity);
        internal static bool HasCharacterBinding(int character) => character!=0 && SoulOrbCharacterBinding.RequiresSession(BindingPath(character));
        internal static bool MatchesCharacterBinding(int character) => character!=0 && requested && valid && SoulOrbCharacterBinding.Matches(BindingPath(character),BindingIdentity);
        static readonly MethodInfo hide=typeof(Oc.OcSoulOrb).GetMethod("Hide",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        internal static void Install(Harmony harmony) {
            frozen.Clear();
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(Catalog+".json"))
            using(var reader=new StreamReader(stream)) {
                var data=MiniJson.ParseObject(reader.ReadToEnd());
                if(MiniJson.GetString(data,"catalog_id")!=Catalog || !(data["orbs"] is List<object> rows)) throw new FormatException("Orb catalog");
                foreach(var row in rows) {
                    var r=(Dictionary<string,object>)row;
                    frozen.Add(checked((int)(long)r["guid"]),(long)r["ap_location_id"]);
                }
            }
            var target=AccessTools.Method(typeof(Oc.OcSoulOrb),"TryGetReward",Type.EmptyTypes);
            if(hide==null || target==null) throw new MissingMethodException("Orb reward boundary");
            harmony.Patch(target,prefix:new HarmonyMethod(typeof(SoulOrbHooks),nameof(Reward)));
            if(!Harmony.GetPatchInfo(target).Prefixes.Any(p=>p.owner==Plugin.GUID && p.PatchMethod.Name==nameof(Reward)))
                throw new InvalidOperationException("Orb hook missing");
        }
        internal static void Bind(Dictionary<string,object> message) {
            selected.Clear(); valid=false;
            requested=message.ContainsKey("soul_orb_catalog");
            if(!requested) {OrbMapTracker.State.Clear();return;}
            try {
                if(MiniJson.GetString(message,"soul_orb_catalog")!=Catalog || !(message["soul_orb_schema"] is long schema) || schema!=1 ||
                    !(message["soul_orb_locations"] is Dictionary<string,object> map) || map.Count==0) return;
                foreach(var pair in map) {
                    if(!int.TryParse(pair.Key,out var guid) || pair.Key!=guid.ToString(System.Globalization.CultureInfo.InvariantCulture) ||
                        !(pair.Value is long location) || !frozen.TryGetValue(guid,out var id) || location!=id) return;
                    selected.Add(guid);
                }
                seed=MiniJson.GetString(message,"seed_name"); slot=MiniJson.GetString(message,"slot"); epoch=MiniJson.GetLong(message,"epoch");
                valid=!string.IsNullOrWhiteSpace(seed) && epoch>0;
            } catch { valid=false; }
            finally { if(valid) OrbMapTracker.Bind(message,selected); else OrbMapTracker.State.Clear(); }
        }
        static bool Reward(Oc.OcSoulOrb __instance) {
            try {
                int character=Oc.OcPlMng.LocalPl==null ? 0 : Oc.OcPlMng.LocalPl.GUID;
                if(HasCharacterBinding(character) && (!Plugin.IsSessionBound() || !MatchesCharacterBinding(character))) {
                    Plugin.StatusDisplay?.Transition("orb","Soul Orb collection waiting for Archipelago session.");
                    return false;
                }
                if(!Plugin.IsSessionBound()) return true;
                if(!requested) return true;
                if(!valid) return false;
                var owner=__instance.Owner;
                if(owner==null) return false;
                int guid=owner.GUID;
                if(selected.Contains(guid) && !owner.FirstTook && !owner.IsTaken && !CurrencyRuntime.ReadyForChecks)
                    Plugin.StatusDisplay?.Transition("orb","Soul Orb collection waiting for verified currency delivery readiness.");
                else Plugin.StatusDisplay?.Transition("orb",null);
                return SoulOrbReplacement.Collect(true,selected.Contains(guid),owner.FirstTook,owner.IsTaken,
                    CurrencyRuntime.ReadyForChecks,
                    ()=>Plugin.Bridge!=null && Plugin.Bridge.SendOrbCheck(guid,seed,slot,epoch,Plugin.GetCharacterGuid()),
                    ()=>{owner.SetTaken(); hide.Invoke(__instance,null);});
            } catch { return false; }
        }
    }
}
