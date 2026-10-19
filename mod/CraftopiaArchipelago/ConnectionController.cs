using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx.Configuration;

namespace CraftopiaArchipelago
{
    // Process callbacks only enqueue text; all game/profile work happens in Pump.
    internal sealed class ConnectionController : IDisposable
    {
        private readonly ConfigFile config;
        private readonly ConfigEntry<string> serverSetting, slotSetting, storedPassword, pythonSetting, starterSetting;
        private readonly ConfigEntry<bool> rememberSetting;
        private readonly string directory;
        private readonly ConcurrentQueue<Tuple<int,string>> messages = new ConcurrentQueue<Tuple<int,string>>();
        private Process process;
        private int generation, port;
        private string requestedServer, requestedSlot, requestedPassword, lastServer, lastSlot;
        private bool prepared, disposed;
        public string Server { get; set; }
        public string Slot { get; set; }
        public string Password { get; set; }
        public string Seed { get; private set; } = "";
        public string StarterChoice { get => starterSetting.Value; set { if(starterSetting.Value==value)return;starterSetting.Value=value;config.Save(); } }
        private bool rememberPassword;
        public bool RememberPassword { get => rememberPassword; set { if(rememberPassword==value)return; rememberPassword=value; if(!value && storedPassword != null) { storedPassword.Value=""; rememberSetting.Value=false; config.Save(); } } }
        public string Status { get; private set; } = "Enter your server and slot, then connect.";
        public bool IsRunning => process != null;
        public bool IsConnected { get; private set; }

        public ConnectionController(ConfigFile config, string pluginDirectory)
        {
            this.config = config; directory = pluginDirectory;
            serverSetting = config.Bind("Connection", "Server", "localhost:38281", "Archipelago server hostname and port.");
            slotSetting = config.Bind("Connection", "Slot", "Player1", "Archipelago slot name.");
            rememberSetting = config.Bind("Connection", "RememberPassword", false, "Remember password protected for this Windows user.");
            storedPassword = config.Bind("Connection", "ProtectedPassword", "", "Windows-protected password; never paste plaintext here.");
            pythonSetting = config.Bind("Connection", "PythonExecutable", "", "Optional absolute Python executable path; blank uses the bundled runtime.");
            starterSetting = config.Bind("Connection", "StarterChoice", "Sword", "Preferred starting combat choice for player-choice sessions.");
            Server = serverSetting.Value; Slot = slotSetting.Value; RememberPassword = rememberSetting.Value;
            Password = RememberPassword ? PasswordStore.Unprotect(storedPassword.Value) : "";
            if (!RememberPassword || (Password.Length == 0 && storedPassword.Value.Length != 0)) { storedPassword.Value = ""; config.Save(); }
        }

        internal static string NormalizeServer(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Enter a server hostname and port.");
            value = value.Trim();
            foreach(char c in value) if(char.IsWhiteSpace(c) || char.IsControl(c)) throw new ArgumentException("Use a server hostname and port without spaces.");
            if (!Uri.TryCreate(value.Contains("://") ? value : "ws://" + value, UriKind.Absolute, out var uri) || uri.Scheme != "ws" ||
                uri.Host.Length == 0 || uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0)
                throw new ArgumentException("Use a server hostname and port without credentials or a path.");
            // Uri assigns ws its standard port 80 when no explicit port was supplied.
            string authority = value.Contains("://") ? value.Substring(value.IndexOf("://",StringComparison.Ordinal)+3) : value;
            authority = authority.TrimEnd('/');
            bool explicitPort = authority.StartsWith("[") ? authority.Contains("]:") : authority.Contains(":");
            int p = explicitPort ? uri.Port : 38281;
            if(p < 1 || p > 65535) throw new ArgumentException("Enter a server port from 1 to 65535.");
            return uri.Host.ToLowerInvariant() + ":" + p;
        }
        internal static bool IsValidProfileKey(string key)
        {
            if(key == null || key.Length != 72 || !key.StartsWith("managed-",StringComparison.Ordinal))return false;
            for(int i=8;i<key.Length;i++)if(!((key[i]>='0'&&key[i]<='9')||(key[i]>='a'&&key[i]<='f')))return false;
            return true;
        }
        internal static ProcessStartInfo CreateStartInfo(string executable,string script)
        {
            var info = new ProcessStartInfo { FileName=executable, WorkingDirectory=Path.GetDirectoryName(script), UseShellExecute=false,
                CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, RedirectStandardInput=true, RedirectStandardOutput=true, RedirectStandardError=true };
            // Unity Mono does not consistently implement ArgumentList. Only this fixed local path is quoted; credentials travel through stdin.
            info.Arguments = "-u " + QuoteArgument(script); return info;
        }
        private static string QuoteArgument(string value)
        {
            var result=new StringBuilder("\"");int slashes=0;
            foreach(char c in value) {
                if(c == '\\') { slashes++; continue; }
                if(c == '"') { result.Append('\\',slashes*2+1);result.Append(c);slashes=0;continue; }
                result.Append('\\',slashes);slashes=0;result.Append(c);
            }
            result.Append('\\',slashes*2);return result.Append('"').ToString();
        }
        public void Connect()
        {
            if(disposed || IsRunning)return;
            Seed="";
            try {
                requestedServer = NormalizeServer(Server);
                requestedSlot = Slot; requestedPassword=Password??"";
                if(string.IsNullOrWhiteSpace(Slot) || Slot.Length>256)throw new ArgumentException("Enter your slot name (at most 256 characters).");
                foreach(char c in Slot)if(char.IsControl(c))throw new ArgumentException("The slot name cannot contain control characters.");
                if(!Plugin.CanChangeConnection && (requestedServer!=lastServer || requestedSlot!=lastSlot || !IsValidProfileKey(Plugin.ManagedProfileKey)))
                    throw new ArgumentException("Return to the main menu before connecting a different server or slot.");
                string python = pythonSetting.Value;
                if(string.IsNullOrWhiteSpace(python))python=Path.Combine(directory,"bridge","python","python.exe");
                if(!Path.IsPathRooted(python) || !File.Exists(python))throw new ArgumentException("Bridge Python is missing. Install the complete mod bundle or set Connection.PythonExecutable to an absolute Python path.");
                string script=Path.Combine(directory,"bridge","client","connection_host.py");
                if(!File.Exists(script))throw new ArgumentException("Bridge files are missing. Install the complete mod bundle including its bridge folder.");
                var listener=new TcpListener(IPAddress.Loopback,0);
                try { listener.Start(); port=((IPEndPoint)listener.LocalEndpoint).Port; } finally { listener.Stop(); }
                serverSetting.Value=Server=requestedServer; slotSetting.Value=Slot; rememberSetting.Value=RememberPassword;
                storedPassword.Value=RememberPassword ? PasswordStore.Protect(Password??"") : ""; config.Save();
                int attempt=++generation; prepared=false; IsConnected=false;
                var child=new Process { StartInfo=CreateStartInfo(python,script), EnableRaisingEvents=true };
                child.OutputDataReceived+=(s,e)=>{if(e.Data!=null)messages.Enqueue(Tuple.Create(attempt,e.Data));};
                // Never relay stderr: third-party libraries may echo connection credentials.
                child.ErrorDataReceived+=(s,e)=>{};
                child.Exited+=(s,e)=>messages.Enqueue(Tuple.Create(attempt,"{\"state\":\"exited\"}"));
                process=child;
                if(!child.Start())throw new InvalidOperationException();
                child.BeginOutputReadLine(); child.BeginErrorReadLine();
                Send(new Dictionary<string,object>{{"server",requestedServer},{"slot",requestedSlot},{"password",requestedPassword},{"port",port},{"starter_choice",StarterChoice}});
                Status="Connecting to Archipelago…";
            } catch(ArgumentException e) { Stop(); Status=e.Message; }
              catch { Stop(); Status="Could not start the bridge. Check the bundled Python runtime and bridge files, then reconnect."; }
        }
        private void Send(Dictionary<string,object> value) { process.StandardInput.WriteLine(MiniJson.Stringify(value)); process.StandardInput.Flush(); }
        public void Pump()
        {
            while(messages.TryDequeue(out var message)) {
                if(message.Item1!=generation || process==null)continue;
                try { HandleStatus(MiniJson.ParseObject(message.Item2)); }
                catch { Stop(); Status="The bridge returned an invalid response. Check the installation and reconnect."; }
            }
        }
        private void HandleStatus(Dictionary<string,object> value)
        {
            string state=MiniJson.GetString(value,"state");
            if(state=="prepared") {
                string key=MiniJson.GetString(value,"session");
                string seed=MiniJson.GetString(value,"seed");
                if(prepared || string.IsNullOrWhiteSpace(seed) || !IsValidProfileKey(key) || MiniJson.GetString(value,"slot")!=requestedSlot || MiniJson.GetString(value,"server")!=requestedServer)
                    throw new InvalidDataException();
                if(!Plugin.CanChangeConnection && key!=Plugin.ManagedProfileKey) { Stop(); Status="This server now has a different seed. Return to the main menu before connecting."; return; }
                if(!Plugin.PrepareManagedSession(requestedSlot,key,port)) { Stop(); Status="Could not prepare this session. Return to the main menu and reconnect."; return; }
                lastServer=requestedServer;lastSlot=requestedSlot;prepared=true;Seed=seed;
                Send(new Dictionary<string,object>{{"cmd","start"}});Status="Session prepared. Authenticating…";
            } else if(state=="connected") {
                if(!prepared)throw new InvalidDataException();
                IsConnected=true;Status="Connected to Archipelago.";
            } else if(state=="reconnecting") {
                if(!prepared)throw new InvalidDataException();
                IsConnected=false;Status="Server connection lost. Reconnecting…";
            } else if(state=="error") {
                string detail=MiniJson.GetString(value,"message");
                if(!string.IsNullOrEmpty(requestedPassword))detail=detail.Replace(requestedPassword,"[redacted]");
                Stop(); Status=string.IsNullOrWhiteSpace(detail)?"Connection failed. Check server, slot, and password, then reconnect.":detail;
            } else if(state=="disconnected" || state=="exited") {
                Stop(); Status="Disconnected. Check the server and reconnect when ready.";
            }
        }
        private void Stop()
        {
            ++generation; IsConnected=false; prepared=false;
            var child=process; process=null;
            if(child==null)return;
            try { if(!child.HasExited) { child.StandardInput.Close(); if(!child.WaitForExit(250))child.Kill(); } } catch { }
            child.Dispose();
        }
        public void Disconnect() { Stop(); Status="Disconnected."; }
        public void Dispose() { if(disposed)return;disposed=true;Stop();Password=""; }
    }

    internal static class PasswordStore
    {
        [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Size; public IntPtr Data; }
        [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)] private static extern bool CryptProtectData(ref Blob input,string description,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,out Blob output);
        [DllImport("crypt32.dll",SetLastError=true)] private static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,int flags,out Blob output);
        [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
        private static byte[] Transform(byte[] data,bool encrypt)
        {
            var input=new Blob { Size=data.Length,Data=Marshal.AllocHGlobal(data.Length) }; Blob output=default;
            try {
                Marshal.Copy(data,0,input.Data,data.Length);
                bool ok=encrypt?CryptProtectData(ref input,"Craftopia Archipelago",IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);
                if(!ok)throw new InvalidOperationException("Windows password protection failed.");
                var result=new byte[output.Size];Marshal.Copy(output.Data,result,0,result.Length);return result;
            } finally { for(int i=0;i<input.Size;i++)Marshal.WriteByte(input.Data,i,0);Marshal.FreeHGlobal(input.Data);if(output.Data!=IntPtr.Zero)LocalFree(output.Data); }
        }
        public static string Protect(string value) => string.IsNullOrEmpty(value)?"":Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(value),true));
        public static string Unprotect(string value) { try { return string.IsNullOrEmpty(value)?"":Encoding.UTF8.GetString(Transform(Convert.FromBase64String(value),false)); } catch { return ""; } }
    }
}
