namespace UnityEngine {public static class Time {public static float timeScale=1;}}
namespace Oc {public static class OcPlMng {public static object LocalPl;}public static class OcGameSceneShareSingleton {public static bool IsOpening;}}
namespace Oc.Maps {public class OcWorldManager {public static OcWorldManager Inst;public bool IsLoading;public object CurrentWorld;}}
namespace Oc.Network {public class OcNetMng {public static OcNetMng Inst;public bool IsMultiPlay;}}
namespace Oc.OcInput {public class OcInputTrack {public static OcInputTrack Inst;public int Locks;public void DisableAllInputWithKey(object k){Locks++;} public void TryEnableInputWithKey(object k,float timer=-1){Locks--;}}}
