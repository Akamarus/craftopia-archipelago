using BepInEx;
using UnityEngine;

namespace CraftopiaArchipelago.Diagnostics
{
    [BepInPlugin("com.akamarus.craftopia.coopprobe", "Craftopia Co-op Diagnostic Probe", "0.1.0")]
    public sealed class ProbePlugin : BaseUnityPlugin
    {
        private ProbeTransport transport;
        private void Awake()
        {
            bool enabled=Config.Bind("Probe","Enabled",false,
                "Diagnostic only. Enable on BOTH endpoints with the same probe build. Restart required.").Value;
            bool initiate=Config.Bind("Probe","Initiate",false,
                "Guest only. Sends one hello per connection. An unmodded host may disconnect you. Restart required.").Value;
            transport=new ProbeTransport(enabled,initiate,()=>Time.realtimeSinceStartup,
                message=>Logger.LogInfo(message));
            Logger.LogInfo(enabled ? "Probe enabled; diagnostic only, no AP or save changes." : "Probe disabled; no networking hooks or traffic.");
        }
        private void Update() => transport?.Tick();
        private void OnDestroy() { transport?.Dispose(); transport=null; }
    }
}
