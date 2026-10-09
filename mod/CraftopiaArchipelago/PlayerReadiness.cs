using System;
using System.Collections.Generic;
using System.Reflection;
using Oc;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Abstraction over the live player for skill-point grants. The real
    /// implementation re-evaluates OcPlMng.LocalPl on every call and never
    /// caches the player object; tests use a fake. This makes the readiness
    /// gate and the apply/verify decision unit-testable without Unity.
    /// </summary>
    internal interface IPlayerSkillGateway
    {
        /// <summary>True when a grant may be applied right now.</summary>
        bool IsReady(out string whyNot);
        int PlayerInstanceId { get; }
        string SceneName { get; }
        int GetSkillPoints();
        void AddSkillPoints(int n);
    }

    /// <summary>
    /// Real gateway. Readiness requires: a valid local player from
    /// OcPlMng.LocalPl (re-evaluated on every call, never cached),
    /// isLocalPlayer, the private OcPlMaster._isLoaded flag set (meaning
    /// LoadSaveData has finished restoring the saved skill points), and a
    /// valid SkillCtrl. The _isLoaded field is resolved once via reflection,
    /// type-verified, and fails closed with a clear diagnostic if unavailable.
    /// No fixed delays are used for readiness.
    /// </summary>
    internal sealed class OcPlayerSkillGateway : IPlayerSkillGateway
    {
        private static FieldInfo isLoadedField;
        private static bool isLoadedFieldResolved;
        private static string isLoadedFieldError;

        private static bool ResolveIsLoadedField()
        {
            if (isLoadedFieldResolved) return isLoadedField != null;
            isLoadedFieldResolved = true;
            try
            {
                var f = typeof(OcPlMaster).GetField("_isLoaded",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (f == null)
                    isLoadedFieldError = "_isLoaded field not found on OcPlMaster";
                else if (f.FieldType != typeof(bool))
                    isLoadedFieldError = "_isLoaded is " + f.FieldType.FullName + ", expected System.Boolean";
                else
                    isLoadedField = f;
            }
            catch (Exception e)
            {
                isLoadedFieldError = "reflection failed: " + e.GetType().Name + ": " + e.Message;
            }
            return isLoadedField != null;
        }

        private static OcPlMaster CurrentPlayer()
        {
            try { return OcPlMng.LocalPl; }
            catch { return null; }
        }

        public bool IsReady(out string whyNot)
        {
            whyNot = null;
            if (!ResolveIsLoadedField())
            {
                whyNot = "player readiness unavailable: " + isLoadedFieldError;
                return false;
            }
            var p = CurrentPlayer();
            if (p == null) { whyNot = "OcPlMng.LocalPl is null"; return false; }
            bool isLocal;
            try { isLocal = p.isLocalPlayer; }
            catch (Exception e) { whyNot = "isLocalPlayer read failed: " + e.GetType().Name; return false; }
            if (!isLocal) { whyNot = "player is not the local player"; return false; }
            bool loaded;
            try { loaded = (bool)isLoadedField.GetValue(p); }
            catch (Exception e) { whyNot = "_isLoaded read failed: " + e.GetType().Name; return false; }
            if (!loaded) { whyNot = "_isLoaded=false (save data still loading)"; return false; }
            OcPlSkillCtrl sc;
            try { sc = p.SkillCtrl; }
            catch (Exception e) { whyNot = "SkillCtrl read failed: " + e.GetType().Name; return false; }
            if (sc == null) { whyNot = "SkillCtrl is null"; return false; }
            return true;
        }

        public int PlayerInstanceId
        {
            get
            {
                var p = CurrentPlayer();
                return p != null ? p.GetInstanceID() : 0;
            }
        }

        public string SceneName
        {
            get
            {
                try
                {
                    var p = CurrentPlayer();
                    return p != null ? p.gameObject.scene.name : "?";
                }
                catch { return "?"; }
            }
        }

        public int GetSkillPoints() { return CurrentPlayer().SkillCtrl.SkillPoint; }
        public void AddSkillPoints(int n) { CurrentPlayer().SkillCtrl.AddSkillPoint(n); }
    }

    /// <summary>
    /// Applies queued skill-point grants with player-readiness gating and
    /// before/after balance verification. Readiness transitions are logged
    /// once (no per-frame spam). A confirmed point increase returns Confirmed;
    /// anything else is Ambiguous (never blindly retried — duplication risk)
    /// or Retryable (not applied: player not ready or call failed).
    /// </summary>
    internal sealed class PlayerGrantApplicator
    {
        private readonly IPlayerSkillGateway gateway;
        private readonly Action<string> logInfo;
        private readonly Action<string> logError;
        private bool lastReady;
        private bool hasLastReady;

        public PlayerGrantApplicator(IPlayerSkillGateway gateway,
            Action<string> logInfo, Action<string> logError)
        {
            this.gateway = gateway;
            this.logInfo = logInfo;
            this.logError = logError;
        }

        private static string Idx(List<long> indices)
        {
            return string.Join(",", indices.ConvertAll(x => x.ToString()).ToArray());
        }

        /// <summary>
        /// Checks readiness and logs transitions (once per transition, never
        /// per frame). Shared by PollReadiness and TryApply.
        /// </summary>
        private bool CheckReadiness(out string whyNot)
        {
            bool ready = false;
            whyNot = null;
            bool threw = false;
            string throwDetail = null;
            try { ready = gateway.IsReady(out whyNot); }
            catch (Exception e)
            {
                threw = true;
                throwDetail = e.GetType().Name + ": " + e.Message;
                whyNot = "readiness check threw: " + throwDetail;
            }

            if (!hasLastReady || ready != lastReady)
            {
                if (threw)
                    logError("[AP] player readiness check threw: " + throwDetail + "; treating as NOT READY");
                logInfo("[AP] player readiness -> " + (ready ? "READY" : "NOT READY")
                    + ((ready || whyNot == null) ? "" : ": " + whyNot));
                lastReady = ready;
                hasLastReady = true;
            }
            return ready;
        }

        /// <summary>
        /// Checks readiness and logs transitions without applying anything.
        /// Called every frame so the NOT READY / READY lines are guaranteed
        /// even with no queued grants (e.g. at the main menu, where TryApply
        /// would otherwise never run).
        /// </summary>
        public void PollReadiness()
        {
            string whyNot;
            CheckReadiness(out whyNot);
        }

        public GrantApplyResult TryApply(List<long> indices)
        {
            string whyNot;
            if (!CheckReadiness(out whyNot)) return GrantApplyResult.Retryable;

            int n = indices.Count;
            int pidBefore, before;
            string scene;
            try
            {
                pidBefore = gateway.PlayerInstanceId;
                scene = gateway.SceneName;
                before = gateway.GetSkillPoints();
            }
            catch (Exception e)
            {
                logError("[AP] grant pre-read failed for indices [" + Idx(indices) + "]: "
                    + e.GetType().Name + ": " + e.Message + "; kept queued, will retry");
                return GrantApplyResult.Retryable;
            }

            try { gateway.AddSkillPoints(n); }
            catch (Exception e)
            {
                // The mutation call was entered: AddSkillPoint changes the
                // balance before subsequent skill-manager/UI operations that
                // can throw. The balance may already have changed, so this is
                // Ambiguous — never retry blindly (duplication risk).
                logError("[AP] AMBIGUOUS grant for indices [" + Idx(indices) + "]: AddSkillPoints threw ("
                    + e.GetType().Name + ": " + e.Message + ") after the mutation call was entered; "
                    + "NOT retrying (duplication risk); delivery stopped for review");
                return GrantApplyResult.Ambiguous;
            }

            int after, pidAfter;
            try
            {
                after = gateway.GetSkillPoints();
                pidAfter = gateway.PlayerInstanceId;
            }
            catch (Exception e)
            {
                logError("[AP] AMBIGUOUS grant for indices [" + Idx(indices) + "]: post-read failed ("
                    + e.GetType().Name + "); mutation may have occurred; NOT retrying (duplication risk); delivery stopped for review");
                return GrantApplyResult.Ambiguous;
            }

            if (pidAfter != pidBefore)
            {
                logError("[AP] AMBIGUOUS grant for indices [" + Idx(indices) + "]: player instance changed "
                    + "mid-application (" + pidBefore + " -> " + pidAfter + "); NOT retrying; delivery stopped for review");
                return GrantApplyResult.Ambiguous;
            }

            if (after - before == n)
            {
                logInfo("[AP] applied " + n + " skill point(s) for item indices [" + Idx(indices) + "]"
                    + " (player " + pidBefore + ", scene '" + scene + "', balance " + before + " -> " + after + ")");
                return GrantApplyResult.Confirmed;
            }

            logError("[AP] AMBIGUOUS grant for indices [" + Idx(indices) + "]: balance " + before + " -> " + after
                + " (expected +" + n + "; player " + pidBefore + ", scene '" + scene + "'); NOT retrying (duplication risk); delivery stopped for review");
            return GrantApplyResult.Ambiguous;
        }
    }
}
