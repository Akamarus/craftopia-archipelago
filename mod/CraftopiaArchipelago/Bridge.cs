using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CraftopiaArchipelago
{
    /// <summary>
    /// Local websocket client. Connects to the Python bridge (localhost),
    /// sends check events, applies incoming item grants. Runs on its own thread;
    /// game interaction happens via queues drained on Unity's main thread.
    /// </summary>
    internal class Bridge
    {
        /// <summary>
        /// Send character identity update to the bridge.
        /// Called on character load (menu-first connect) and character switch.
        /// Must be called on the Unity main thread.
        /// </summary>
        public void SendIdentityUpdate(int guid, long epoch, string slot)
        {
            try
            {
                var msg = new Dictionary<string, object>
                {
                    ["cmd"] = "identity_update",
                    ["character_guid"] = (long)guid,
                    ["epoch"] = epoch,
                    ["slot"] = slot,
                };
                outbox.Enqueue(MiniJson.Stringify(msg));
                Plugin.Log.LogInfo($"[AP] queued identity update: guid={guid}, epoch={epoch}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] identity update failed: " + e.GetType().Name);
            }
        }

        private Thread thread;
        private volatile bool running;
        private volatile bool connected;
        private int connectionId;
        private long knownEpoch;
        internal int ConnectionId { get { return Volatile.Read(ref connectionId); } }
        private readonly ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        private string outboxPath;
        private readonly object outboxFileLock = new object();

        public bool IsConnected { get { return connected; } }

        public void Start()
        {
            running = true;
            thread = new Thread(RunThread);
            thread.IsBackground = true;
            thread.Name = "CraftopiaArchipelago-Bridge";
            thread.Start();
        }

        public void Stop()
        {
            running = false;
        }

        /// <summary>Called from the Harmony patch (Unity main thread).</summary>
        /// <summary>
        /// Initialize the durable observation outbox. Call once at startup.
        /// Replays any observations that were persisted but not yet sent.
        /// </summary>
        public void InitDurableOutbox(string path)
        {
            outboxPath = path;
            // Replay unsent observations from previous session.
            try
            {
                if (System.IO.File.Exists(path))
                {
                    var lines = System.IO.File.ReadAllLines(path);
                    int replayed = 0;
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            outbox.Enqueue(line.Trim());
                            replayed++;
                        }
                    }
                    if (replayed > 0)
                        Plugin.Log.LogInfo($"[AP] replayed {replayed} unsent observation(s) from durable outbox");
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] durable outbox replay failed: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Persist an observation to the durable outbox file before queueing.
        /// Uses atomic write (temp file + replace) to avoid corruption.
        /// </summary>
        private void PersistObservation(string msg)
        {
            if (string.IsNullOrEmpty(outboxPath)) return;
            lock (outboxFileLock)
            {
                try
                {
                    // Append to the file. Each line is one JSON message.
                    System.IO.File.AppendAllText(outboxPath, msg + "\n");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[AP] failed to persist observation: " + e.GetType().Name);
                }
            }
        }

        /// <summary>
        /// Remove a sent observation from the durable outbox.
        /// Called after the bridge acknowledges receipt.
        /// </summary>
        public void AckObservation(string msg)
        {
            if (string.IsNullOrEmpty(outboxPath)) return;
            lock (outboxFileLock)
            {
                try
                {
                    if (!System.IO.File.Exists(outboxPath)) return;
                    var lines = System.IO.File.ReadAllLines(outboxPath);
                    var remaining = new System.Collections.Generic.List<string>();
                    bool removed = false;
                    foreach (var line in lines)
                    {
                        if (!removed && line.Trim() == msg.Trim())
                        {
                            removed = true; // Remove only the first match.
                        }
                        else if (!string.IsNullOrWhiteSpace(line))
                        {
                            remaining.Add(line);
                        }
                    }
                    if (removed)
                    {
                        var tmp = outboxPath + ".tmp";
                        System.IO.File.WriteAllLines(tmp, remaining);
                        System.IO.File.Replace(tmp, outboxPath, null);
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[AP] failed to ack observation: " + e.GetType().Name);
                }
            }
        }

        public void SendCheck(int missionId, string missionTitle, string missionClass)
        {
            long epoch = Interlocked.Read(ref knownEpoch);
            if (epoch <= 0) return; // No seed binding yet; never attribute it to a future session.
            var msg = MiniJson.Stringify(new Dictionary<string, object>
            {
                ["cmd"] = "check",
                ["epoch"] = epoch,
                ["slot"] = Plugin.SlotName.Value,
                ["mission_id"] = (long)missionId,
                ["mission_title"] = missionTitle ?? "",
                ["mission_class"] = missionClass ?? ""
            });
            // Durable: persist before queueing so a game exit doesn't lose it.
            PersistObservation(msg);
            outbox.Enqueue(msg);
        }

        /// <summary>
        /// Acks applied item grants back to the bridge client. Called on Unity's
        /// main thread only after the points were applied AND durably persisted.
        /// The epoch lets the client ignore acks from a stale session.
        /// </summary>
        public void SendAck(List<long> indices, long epoch)
        {
            var msg = MiniJson.Stringify(new Dictionary<string, object>
            {
                ["cmd"] = "grant_ack",
                ["epoch"] = epoch,
                ["indices"] = indices
            });
            outbox.Enqueue(msg);
        }

        internal void SendUnlockAck(Dictionary<string, object> message)
        {
            outbox.Enqueue(MiniJson.Stringify(message));
        }

        private void RunThread()
        {
            try { RunAsync().GetAwaiter().GetResult(); }
            catch (Exception e) { Plugin.Log.LogError("[AP] bridge thread died: " + e); }
        }

        private async Task RunAsync()
        {
            while (running)
            {
                try
                {
                    using (var ws = new ClientWebSocket())
                    {
                        var uri = new Uri("ws://127.0.0.1:" + Plugin.LocalPort.Value + "/");
                        await ws.ConnectAsync(uri, CancellationToken.None);
                        Interlocked.Increment(ref connectionId);
                        connected = true;
                        Plugin.Log.LogInfo("[AP] connected to local bridge at " + uri);

                        var handshake = new Dictionary<string, object>
                        {
                            ["cmd"] = "connect",
                            ["game"] = "Craftopia",
                            ["name"] = Plugin.SlotName.Value,
                            ["unlock_protocol"] = 2L
                        };
                        // Include character GUID if a character is loaded.
                        // The bridge uses this for enrollment (explicit, not automatic).
                        int guid = Plugin.GetCharacterGuid();
                        if (guid != 0)
                            handshake["character_guid"] = (long)guid;
                        await SendRaw(ws, MiniJson.Stringify(handshake));

                        var buffer = new byte[65536];
                        // One receive stays pending for the life of the connection
                        // and is NEVER cancelled: cancelling a ClientWebSocket
                        // receive transitions the socket to Aborted and kills
                        // the connection (observed as "no close frame" on the
                        // bridge). The 200ms delay below only bounds how long
                        // we wait before flushing the outbox again.
                        Task<WebSocketReceiveResult> receiveTask = null;
                        while (running && ws.State == WebSocketState.Open)
                        {
                            while (outbox.TryDequeue(out string json))
                                await SendRaw(ws, json);

                            if (receiveTask == null)
                                receiveTask = ws.ReceiveAsync(
                                    new ArraySegment<byte>(buffer), CancellationToken.None);

                            var completed = await Task.WhenAny(receiveTask, Task.Delay(200));
                            if (completed != receiveTask)
                                continue; // poll interval elapsed; receive stays pending

                            WebSocketReceiveResult res;
                            try { res = await receiveTask; }
                            finally { receiveTask = null; }

                            if (res.MessageType == WebSocketMessageType.Close)
                            {
                                Plugin.Log.LogInfo("[AP] bridge closed the connection");
                                break;
                            }
                            if (res.MessageType != WebSocketMessageType.Text || !res.EndOfMessage)
                            {
                                Plugin.Log.LogWarning("[AP] unexpected bridge frame, ignoring");
                                continue;
                            }

                            // PoC: messages are small, assume a single frame each.
                            string text = Encoding.UTF8.GetString(buffer, 0, res.Count);
                            HandleMessage(text);
                        }
                        Plugin.Log.LogInfo("[AP] bridge receive loop ended (running="
                            + running + ", state=" + ws.State + ")");
                    }
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("[AP] bridge connection failed, retrying: "
                        + e.GetType().Name + ": " + e.Message);
                }
                finally { connected = false; }

                if (running)
                    await Task.Delay(2000);
            }
        }

        private static Task SendRaw(ClientWebSocket ws, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            return ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void HandleMessage(string text)
        {
            Dictionary<string, object> msg;
            try { msg = MiniJson.ParseObject(text); }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("[AP] bad bridge message: " + e.Message);
                return;
            }

            string cmd = MiniJson.GetString(msg, "cmd");
            switch (cmd)
            {
                case "connected":
                    Plugin.Log.LogInfo("[AP] bridge link is live (server connected)");
                    break;
                case "session":
                    HandleSession(msg);
                    break;
                case "grant_items":
                    HandleGrantItems(msg);
                    break;
                case "print":
                    Plugin.Log.LogInfo("[AP] " + MiniJson.GetString(msg, "text"));
                    break;
                case "unlock_equipment":
                    Plugin.QueueUnlock(msg, ConnectionId);
                    break;
                case "unlock_production":
                    Plugin.QueueUnlock(msg, ConnectionId);
                    break;
                case "unlock_skill_tree":
                    Plugin.QueueUnlock(msg, ConnectionId);
                    break;
                default:
                    Plugin.Log.LogWarning("[AP] unknown bridge cmd: " + cmd);
                    break;
            }
        }

        private void HandleSession(Dictionary<string, object> msg)
        {
            try
            {
                object epochRaw, protocolRaw;
                if (!msg.TryGetValue("epoch",out epochRaw) || !(epochRaw is long) || (long)epochRaw <= 0 ||
                    !msg.TryGetValue("unlock_protocol",out protocolRaw) || !(protocolRaw is long) || (long)protocolRaw != 2 ||
                    MiniJson.GetString(msg,"slot") != Plugin.SlotName.Value ||
                    string.IsNullOrWhiteSpace(MiniJson.GetString(msg,"connection_token"))) return;
                long epoch = (long)epochRaw;
                Interlocked.Exchange(ref knownEpoch, epoch);
                var enrolledList = MiniJson.GetLongList(msg, "enrolled_guids");
                var enrolled = new System.Collections.Generic.HashSet<int>();
                foreach (long g in enrolledList)
                {
                    // GUIDs are int32; skip out-of-range.
                    if (g >= int.MinValue && g <= int.MaxValue)
                        enrolled.Add((int)g);
                }
                Plugin.Log.LogInfo("[AP] session: epoch=" + epoch +
                    ", enrolled=" + enrolled.Count + " guid(s)");
                Plugin.OnSlotData(epoch, enrolled, MiniJson.GetString(msg,"connection_token"), ConnectionId);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] session handle error: " + e.GetType().Name);
            }
        }

        /// <summary>
        /// Delivery semantics: indices the bridge client sends are AP
        /// ReceivedItems positions. Already-applied indices are skipped but
        /// still acked, so a re-send after a lost ack cannot double-grant.
        /// A newer epoch means a new seed: rotate the applied set first.
        /// </summary>
        /// <summary>
        /// Delivery entry point. Runs on the network thread: it ONLY enqueues
        /// the parsed message into the pipeline's thread-safe inbound queue.
        /// All classification, dedup, epoch rotation, and ack decisions happen
        /// on Unity's main thread when it drains the queue.
        /// </summary>
        private void HandleGrantItems(Dictionary<string, object> msg)
        {
            var indices = MiniJson.GetLongList(msg, "indices");
            if (indices.Count == 0) return;

            long epoch = MiniJson.GetLong(msg, "epoch");
            try
            {
                Plugin.Pipeline.EnqueueInbound(epoch, indices);
                Plugin.Log.LogInfo("[AP] grant_items received (" + indices.Count
                    + " indice(s), epoch " + epoch + ")");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] grant inbound queue error: " + e);
            }
        }
    }
}
