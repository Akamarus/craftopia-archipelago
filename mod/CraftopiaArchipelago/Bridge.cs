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
        private readonly CharacterIdentitySync identitySync = new CharacterIdentitySync();
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
                if (!IsConnected || epoch != Interlocked.Read(ref knownEpoch)) return;
                if (identitySync.TryQueue(ConnectionId, guid, epoch, slot, () => outbox.Enqueue(MiniJson.Stringify(msg))))
                    Plugin.Log.LogInfo($"[AP] queued identity update: guid={guid}, epoch={epoch}");
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[AP] identity update failed: " + e.GetType().Name);
            }
        }

        internal void SendCharacterEnrollment(int guid,long epoch,string seed,string slot) {
            if(guid==0 || !IsConnected || epoch!=Interlocked.Read(ref knownEpoch) || string.IsNullOrEmpty(knownToken))return;
            outbox.Enqueue(MiniJson.Stringify(new Dictionary<string,object> {
                {"cmd","character_enrollment"},{"character_guid",guid.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {"epoch",epoch},{"seed_name",seed},{"slot",slot},{"connection_token",knownToken},{"local_enrollment_verified",true}
            }));
        }
        internal void SendMilestoneEnrollment(int guid,long epoch,string seed,string slot) {
            if(!IsConnected || epoch!=Interlocked.Read(ref knownEpoch) || string.IsNullOrEmpty(knownToken))return;
            outbox.Enqueue(MiniJson.Stringify(new Dictionary<string,object> {
                {"cmd","milestone_enrollment"},{"character_guid",guid.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {"epoch",epoch},{"seed_name",seed},{"slot",slot},{"connection_token",knownToken},{"local_enrollment_verified",true}
            }));
        }
        private readonly CancellationTokenSource stopping = new CancellationTokenSource();
        private ClientWebSocket activeSocket;
        private static int nextConnectionId;
        private Thread thread;
        private volatile bool running;
        private volatile bool connected;
        private int connectionId;
        private long knownEpoch;
        private readonly bool fixedEpoch;
        internal Bridge(long expectedEpoch = 0, bool fixedEpoch = false) {
            knownEpoch = expectedEpoch; this.fixedEpoch = fixedEpoch;
        }
        internal int ConnectionId { get { return Volatile.Read(ref connectionId); } }
        private readonly ConcurrentQueue<string> outbox = new ConcurrentQueue<string>();
        private ObservationJournal observations;
        private string knownToken;

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
            stopping.Cancel();
            try { activeSocket?.Abort(); } catch { }
            if (thread != null && thread != Thread.CurrentThread && !thread.Join(5000))
                throw new InvalidOperationException("Previous bridge is still stopping; session change refused");
            connected = false;
        }

        public void InitDurableOutbox(string path)
        {
            observations = new ObservationJournal(path);
            // Replay only after the socket negotiates the matching epoch/slot.
        }

        private readonly System.Collections.Concurrent.ConcurrentQueue<Dictionary<string, object>> notifications =
            new System.Collections.Concurrent.ConcurrentQueue<Dictionary<string, object>>();
        // Recheck binding on the game thread, after pending session changes.
        public void PumpNotifications()
        {
            while (notifications.TryDequeue(out var msg)) {
                if (!IsConnected || Plugin.Validator == null || MiniJson.GetLong(msg, "epoch") != Interlocked.Read(ref knownEpoch) ||
                    MiniJson.GetLong(msg, "epoch") != Plugin.Validator.CurrentEpoch ||
                    MiniJson.GetString(msg, "slot") != Plugin.SlotName.Value ||
                    string.IsNullOrEmpty(knownToken) || MiniJson.GetString(msg, "connection_token") != knownToken) continue;
                if (MiniJson.GetString(msg, "cmd") == "orb_map_snapshot") { OrbMapTracker.Snapshot(msg); continue; }
                if (MiniJson.GetString(msg, "cmd") == "item_notice") {
                    msg.TryGetValue("silent", out var silent);
                    Plugin.StatusDisplay?.ItemNotices.Add(MiniJson.GetString(msg,"id"),
                        MiniJson.GetString(msg,"title"), MiniJson.GetString(msg,"location"),
                        MiniJson.GetString(msg,"kind") == "sent", silent is bool quiet && quiet);
                    continue;
                }
                if (MiniJson.GetString(msg, "cmd") == "activity_snapshot") {
                    msg.TryGetValue("received", out var received);
                    msg.TryGetValue("sent", out var sent);
                    msg.TryGetValue("goal_complete", out var complete);
                    if(Plugin.StatusDisplay != null) Plugin.StatusDisplay.TotalChecks = MiniJson.GetLong(msg, "total_check_count", -1);
                    Plugin.StatusDisplay?.SetActivity(received as List<object>, sent as List<object>,
                        MiniJson.GetLong(msg, "checked_count"), complete is bool won && won);
                    if (Plugin.StatusDisplay != null)
                        Plugin.StatusDisplay.HistoryLimited = msg.TryGetValue("history_limited", out var limited) && limited is bool isLimited && isLimited;
                    continue;
                }
                // Item notices carry the reward and source. Check totals update in the snapshot.
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
                ["observation_id"] = "mission:" + missionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["mission_title"] = missionTitle ?? "",
                ["mission_class"] = missionClass ?? ""
            });
            // Durable: persist before queueing so a game exit doesn't lose it.
            if (observations != null && observations.Append(msg)) {
                outbox.Enqueue(msg);
            }
            else Plugin.SafeError("[AP] observation not queued: durable journal unavailable; preserve files for review");
        }

        internal bool SendMilestoneObservation(Dictionary<string,object> message)
        {
            if(MiniJson.GetLong(message,"epoch")!=Interlocked.Read(ref knownEpoch) || MiniJson.GetString(message,"slot")!=Plugin.SlotName.Value) return false;
            var text=MiniJson.Stringify(message);
            if(observations==null || !observations.Append(text)) return false;
            outbox.Enqueue(text); return true;
        }

        internal void RestoreOrbMapPending() {
            foreach(var line in observations?.Snapshot(Interlocked.Read(ref knownEpoch),Plugin.SlotName.Value) ?? new List<string>()) {
                var m=MiniJson.ParseObject(line);
                if(MiniJson.GetString(m,"check_category")!="soul_orb")continue;
                long guid=MiniJson.GetLong(m,"orb_guid");
                if(guid>=int.MinValue&&guid<=int.MaxValue)OrbMapTracker.State.MarkCollected(OrbMapTracker.Identity(m),(int)guid);
            }
        }
        internal bool SendOrbCheck(int orb,string seed,string slot,long epoch,int guid)
        {
            if(guid==0 || epoch!=Interlocked.Read(ref knownEpoch) || slot!=Plugin.SlotName.Value) return false;
            var message=MiniJson.Stringify(new Dictionary<string,object> {
                {"cmd","check"},{"check_category","soul_orb"},{"orb_guid",(long)orb},
                {"observation_id","orb:"+orb.ToString(System.Globalization.CultureInfo.InvariantCulture)},
                {"seed_name",seed},{"slot",slot},{"epoch",epoch},
                {"character_guid",guid.ToString(System.Globalization.CultureInfo.InvariantCulture)}
            });
            if(observations==null || !observations.Append(message)) return false;
            outbox.Enqueue(message);
            OrbMapTracker.State.MarkCollected(OrbMapTracker.Identity(MiniJson.ParseObject(message)),orb);
            // Server confirmation produces the collection popup; do not show a second queued popup.
            return true;
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
                        activeSocket = ws;
                        var uri = new Uri("ws://127.0.0.1:" + Plugin.LocalPort.Value + "/");
                        await ws.ConnectAsync(uri, stopping.Token);
                        if (!running) break;
                        Volatile.Write(ref connectionId, Interlocked.Increment(ref nextConnectionId));
                        connected = true;
                        Plugin.Log.LogInfo("[AP] connected to local bridge at " + uri);

                        var handshake = new Dictionary<string, object>
                        {
                            ["cmd"] = "connect",
                            ["game"] = "Craftopia",
                            ["name"] = Plugin.SlotName.Value,
                            ["unlock_protocol"] = 2L,
                            ["equipment_protocol"] = 1L,
                            ["equipment_protocol_max"] = 2L,
                            ["currency_protocol"] = 1L,
                            ["soul_orb_protocol"] = 1L
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
                finally { connected = false; activeSocket = null; }

                if (running)
                    try { await Task.Delay(2000, stopping.Token); } catch (OperationCanceledException) { }
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
                case "check_confirmed":
                case "orb_map_snapshot":
                case "activity_snapshot":
                case "item_notice":
                    notifications.Enqueue(msg);
                    break;
                case "observation_ack":
                    if (MiniJson.GetLong(msg, "epoch") == Interlocked.Read(ref knownEpoch) &&
                        MiniJson.GetString(msg, "slot") == Plugin.SlotName.Value &&
                        MiniJson.GetString(msg, "connection_token") == knownToken)
                        observations?.Ack(MiniJson.GetString(msg, "observation_id"),
                            MiniJson.GetLong(msg, "epoch"), MiniJson.GetString(msg, "slot"));
                    break;
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
                case "grant_currency":
                    CurrencyRuntime.QueueGrant(msg, ConnectionId);
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
                if (fixedEpoch && Interlocked.Read(ref knownEpoch) > 0 && epoch != Interlocked.Read(ref knownEpoch)) {
                    Plugin.SafeError("[AP] conflicting managed session epoch refused; preserve existing session files");
                    return;
                }
                Interlocked.Exchange(ref knownEpoch, epoch);
                knownToken = MiniJson.GetString(msg, "connection_token");
                if (observations != null)
                    foreach (var pending in observations.Snapshot(epoch, Plugin.SlotName.Value)) outbox.Enqueue(pending);
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
                // Malformed optional fields become a rejected contract, never legacy mode.
                string catalog = null;
                if (msg.TryGetValue("equipment_catalog",out var catalogRaw)) catalog = catalogRaw as string ?? "INVALID";
                int equipmentProtocol = 0, multiplayer = 0;
                if (msg.TryGetValue("equipment_protocol",out var ep)) equipmentProtocol = ep is long epn && epn >= 0 && epn <= int.MaxValue ? (int)epn : -1;
                if (msg.TryGetValue("multiplayer_mode",out var mp)) multiplayer = mp is long mpn && mpn >= 0 && mpn <= int.MaxValue ? (int)mpn : -1;
                Plugin.OnSlotData(epoch, enrolled, MiniJson.GetString(msg,"connection_token"), ConnectionId, catalog, equipmentProtocol, multiplayer, msg);
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
            if (epoch <= 0 || epoch != Interlocked.Read(ref knownEpoch)) return;
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
