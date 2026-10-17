using System;
using System.IO;
namespace CraftopiaArchipelago
{
    // Even a partial/corrupt marker requires session negotiation after restart.
    public static class SoulOrbCharacterBinding
    {
        public static bool RequiresSession(string path) {
            try { File.GetAttributes(path); return true; }
            catch(FileNotFoundException) { return false; }
            catch { return true; }
        }
        public static bool Matches(string path,string identity) {
            try { return !string.IsNullOrEmpty(identity) && File.ReadAllText(path)==identity; }
            catch { return false; }
        }
        public static bool Mark(string path,string identity) {
            try {
                if(string.IsNullOrEmpty(identity)) return false;
                if(RequiresSession(path)) return Matches(path,identity);
                using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    var bytes=System.Text.Encoding.UTF8.GetBytes(identity);
                    stream.Write(bytes,0,bytes.Length); stream.Flush(true);
                }
                return true;
            } catch { return false; }
        }
    }
}
