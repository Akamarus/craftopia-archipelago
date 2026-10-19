using System;
using System.Collections.Generic;
using UnityEngine;

namespace CraftopiaArchipelago
{
    // Presentation and explicit setup actions; grants remain behind the plugin's validation gates.
    internal sealed class FieldGuideView
    {
        public bool IsOpen { get; private set; }
        private readonly FieldGuidePause pause = new FieldGuidePause();
        private bool ownsCursor, previousVisible;
        private CursorLockMode previousLock;
        private int tab;
        private readonly Vector2[] scroll = new Vector2[4];
        private bool confirmingEnrollment;
        private string setupMessage = "";
        private GUIStyle input;
        private GUIStyle title, label, muted, light, heading, button, selected, badgeText, small;
        private GUIStyle sentItem, foundItem, itemLocation;
        private Texture2D paper, teal, edge, inset, active;
        private readonly Color ink = new Color(.17f, .25f, .20f);
        private readonly Color quiet = new Color(.37f, .43f, .36f);
        private List<string> unlockRows = new List<string>();
        private double nextUnlockRefresh;
        private IReadOnlyList<string> previousItems;
        private readonly IReadOnlyList<string>[] measuredRows = new IReadOnlyList<string>[4];
        private readonly float[][] rowHeights = new float[4][];
        private readonly float[] measuredWidths = new float[4];
        private readonly float[] measuredHeights = new float[4];

        public void Toggle() { SetOpen(!IsOpen); }
        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            if(open) pause.SetOpen(true); else pause.SetOpen(false);
            IsOpen = open;
            if (!open) { confirmingEnrollment = false; Plugin.CancelEnrollment(); }
            if (open) {
                previousVisible = Cursor.visible; previousLock = Cursor.lockState;
                ownsCursor = true; KeepCursor();
            } else RestoreCursor();
        }
        public void KeepCursor()
        {
            if(IsOpen && pause.WorldChanged()){SetOpen(false);return;}
            if(IsOpen) pause.Maintain();
            if (IsOpen && Application.isFocused) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }
        private void RestoreCursor()
        {
            if (!ownsCursor) return;
            Cursor.lockState = previousLock; Cursor.visible = previousVisible; ownsCursor = false;
        }
        public void Dispose()
        {
            SetOpen(false);
            foreach (var texture in new[] { paper, teal, edge, inset, active })
                if (texture != null) UnityEngine.Object.Destroy(texture);
        }
        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }
        private GUIStyle Text(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, wordWrap = true, richText = false,
                padding = new RectOffset(0, 0, 0, 0), alignment = TextAnchor.UpperLeft };
            style.normal.textColor = color; return style;
        }
        private void EnsureStyles()
        {
            if (title != null) return;
            paper = Solid(new Color(.91f, .91f, .83f, .99f));
            teal = Solid(new Color(.075f, .19f, .17f, .98f));
            edge = Solid(new Color(.59f, .65f, .53f));
            inset = Solid(new Color(.83f, .86f, .77f));
            active = Solid(new Color(.72f, .78f, .65f));
            title = Text(28, new Color(.93f, .94f, .82f));
            label = Text(17, ink); muted = Text(14, quiet); small = Text(12, quiet);
            light = Text(14, new Color(.72f, .82f, .73f)); heading = Text(20, ink);
            badgeText = Text(15, new Color(.93f, .94f, .82f));
            button = Text(15, ink); button.alignment = TextAnchor.MiddleCenter;
            button.normal.background = inset; button.hover.background = active; button.active.background = edge;
            button.hover.textColor = ink; button.active.textColor = ink;
            selected = new GUIStyle(button); selected.normal.background = active;
            input = new GUIStyle(GUI.skin.textField) { fontSize = 17, richText = false };
            sentItem = Text(16, new Color(.65f, .84f, 1f));
            foundItem = Text(16, new Color(.70f, .95f, .73f));
            itemLocation = Text(13, Color.white);
        }
        private static string Safe(string value) => PlayerStatusDisplay.CleanText(value);
        private void Panel(Rect area, Texture2D fill)
        {
            GUI.DrawTexture(area, edge);
            GUI.DrawTexture(new Rect(area.x + 1, area.y + 1, Math.Max(0, area.width - 2), Math.Max(0, area.height - 2)), fill);
        }
        public void Draw(PlayerStatusDisplay status, PermissionManager permissions, string slot, bool connected,
            bool recovery, string enrollment, float badgeX, float badgeY, float scale)
        {
            EnsureStyles();
            var matrix = GUI.matrix; var color = GUI.color; var background = GUI.backgroundColor; var content = GUI.contentColor;
            try {
                GUI.color = GUI.backgroundColor = GUI.contentColor = Color.white;
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                float sw = Screen.width / scale, sh = Screen.height / scale;
                string count = (status?.CheckedCount ?? 0) + "/" + (status != null && status.TotalChecks >= 0 ? status.TotalChecks.ToString() : "?");
                float bx = PlayerStatusDisplay.ClampHudPosition(badgeX, sw, 244);
                float by = PlayerStatusDisplay.ClampHudPosition(badgeY, sh, 52);
                Panel(new Rect(bx, by, 244, 52), teal);
                GUI.Label(new Rect(bx + 12, by + 8, 190, 22), "AP  ·  " + count, badgeText);
                GUI.Label(new Rect(bx + 12, by + 31, 190, 17), recovery ? "Deliveries on hold  ·  F8" : !connected ? "Disconnected  ·  F8" : status?.GoalComplete == true ? "Goal complete  ·  F8" : "Connected  ·  F8", light);
                if (GUI.Button(new Rect(bx + 210, by + 10, 25, 30), IsOpen ? "−" : "+", button)) Toggle();
                if (IsOpen) DrawBook(status, permissions, slot, connected, recovery, enrollment, sw, sh, count);
                DrawNotices(status, sw, sh);
                if (!IsOpen) DrawItemNotices(status, sw);
            } finally { GUI.matrix = matrix; GUI.color = color; GUI.backgroundColor = background; GUI.contentColor = content; }
        }
        private void DrawBook(PlayerStatusDisplay status, PermissionManager permissions, string slot, bool connected,
            bool recovery, string enrollment, float sw, float sh, string count)
        {
            float w = Math.Min(940, sw - 32), h = Math.Min(650, sh - 90);
            float x = (sw - w) / 2, y = (sh - h) / 2;
            Panel(new Rect(x, y, w, h), paper);
            GUI.DrawTexture(new Rect(x + 1, y + 1, w - 2, 88), teal);
            GUI.Label(new Rect(x + 25, y + 13, w - 100, 38), "Archipelago  /  Field Guide", title);
            GUI.Label(new Rect(x + 26, y + 55, w - 100, 24), Safe(slot) + "   ·   " + (connected ? "Bridge connected" : "Bridge disconnected"), light);
            if (GUI.Button(new Rect(x + w - 49, y + 21, 30, 32), "×", button)) SetOpen(false);
            float side = w >= 650 ? 216 : 165;
            float bodyTop = y + 89, bodyHeight = h - 126;
            GUI.DrawTexture(new Rect(x + 1, bodyTop, side - 1, bodyHeight), inset);
            GUI.Label(new Rect(x + 20, bodyTop + 24, side - 38, 22), "YOUR ADVENTURE", small);
            GUI.Label(new Rect(x + 20, bodyTop + 56, side - 38, 52), status?.GoalComplete == true ? "Goal complete" : "Adventure in progress", heading);
            GUI.Label(new Rect(x + 20, bodyTop + 125, side - 38, 22), "Checks completed", muted);
            GUI.Label(new Rect(x + 20, bodyTop + 151, side - 38, 30), count, heading);
            GUI.DrawTexture(new Rect(x + 20, bodyTop + 194, side - 40, 5), edge);
            if (status != null && status.TotalChecks > 0)
                GUI.DrawTexture(new Rect(x + 20, bodyTop + 194, (side - 40) * Mathf.Clamp01((float)status.CheckedCount / status.TotalChecks), 5), teal);
            GUI.Label(new Rect(x + 20, bodyTop + 225, side - 38, 90), "Each check sends a reward through Archipelago.\n\nKeep exploring at your own pace.", muted);
            float right = x + side + 25, rw = w - side - 50;
            string[] tabs = { "Received", "Unlocks", "Session", "Connection" };
            for (int i = 0; i < tabs.Length; i++)
                if (GUI.Button(new Rect(right + i * rw / tabs.Length, bodyTop + 18, rw / tabs.Length - 4, 36), tabs[i], tab == i ? selected : button)) {
                    tab = i; confirmingEnrollment = false; Plugin.CancelEnrollment();
                }
            var viewport = new Rect(right, bodyTop + 71, rw, Math.Max(40, bodyHeight - 88));
            if (!ReferenceEquals(previousItems, status?.ReceivedItems)) {
                // Keep the reader's position across live snapshots; reset only on a session reset.
                previousItems = status?.ReceivedItems;
                if (previousItems == null || previousItems.Count == 0) scroll[0] = Vector2.zero;
            }
            if (tab == 0) DrawReceived(status, viewport);
            else if (tab == 1) {
                if (PlayerStatusDisplay.Now >= nextUnlockRefresh) {
                    unlockRows = permissions?.GetGuideRows() ?? new List<string>(); nextUnlockRefresh = PlayerStatusDisplay.Now + .5;
                }
                DrawRows(viewport, unlockRows, "Current permissions · equipment still follows game requirements", true);
            } else if (tab == 3) DrawConnection(viewport);
            else {
                var rows = new List<string> {
                    "Player slot|" + Safe(slot),
                    "Bridge connection|" + (connected ? "Connected" : "Disconnected — waiting to reconnect"),
                    "Checks completed|" + count,
                    "Goal|" + (status?.GoalComplete == true ? "Complete" : "Not complete"),
                    "Delivery status|" + (recovery ? "On hold — recovery needed" : connected ? "Active" : "Waiting for connection"),
                    "Character validation|" + Safe(string.IsNullOrWhiteSpace(enrollment) ? "Waiting for session" : enrollment),
                    "Mod version|" + Plugin.VERSION
                };
                DrawRows(viewport, rows, "Your current Archipelago session", true);
            }
            GUI.DrawTexture(new Rect(x + 1, y + h - 37, w - 2, 1), edge);
            GUI.Label(new Rect(x + 23, y + h - 25, w - 46, 20), pause.Message + "                                   F8 / Esc  ·  close", small);
        }
        private void DrawConnection(Rect viewport)
        {
            var connection = Plugin.Connection;
            if (connection == null) { GUI.Label(viewport, "Connection setup is unavailable.", label); return; }
            float width = viewport.width - 24;
            scroll[3] = GUI.BeginScrollView(viewport, scroll[3], new Rect(0, 0, width, 790));
            bool wasEnabled = GUI.enabled;
            try {
                GUI.Label(new Rect(0, 0, width, 28), "CONNECT TO ARCHIPELAGO", heading);
                GUI.Label(new Rect(0, 36, width, 22), "Server address and port", muted);
                GUI.enabled = wasEnabled && !connection.IsRunning;
                connection.Server = GUI.TextField(new Rect(0, 60, width, 32), connection.Server ?? "", 512, input);
                GUI.Label(new Rect(0, 100, width, 22), "Slot name", muted);
                connection.Slot = GUI.TextField(new Rect(0, 124, width, 32), connection.Slot ?? "", 256, input);
                GUI.Label(new Rect(0, 164, width, 22), "Password (optional)", muted);
                connection.Password = GUI.PasswordField(new Rect(0, 188, width, 32), connection.Password ?? "", '•', 256, input);
                connection.RememberPassword = GUI.Toggle(new Rect(0, 229, width, 27), connection.RememberPassword, " Remember password on this Windows account");
                GUI.Label(new Rect(0, 267, width, 24), "Starting weapon preference (when the seed allows a choice)", muted);
                string[] choices = { "Sword", "Bow", "Shield", "Lance", "SwordTwoHand", "Katana", "MagicStaff" };
                string[] names = { "Sword", "Bow", "Shield", "Spear", "Two-handed", "Katana", "Staff" };
                for (int i = 0; i < choices.Length; i++) {
                    int row = i / 4, col = i % 4;
                    if (GUI.Button(new Rect(col * width / 4, 299 + row * 34, width / 4 - 4, 30), names[i], connection.StarterChoice == choices[i] ? selected : button)) connection.StarterChoice = choices[i];
                }
                GUI.enabled = wasEnabled;
                if (GUI.Button(new Rect(0, 378, width, 36), connection.IsRunning ? "Disconnect" : "Connect", button)) {
                    confirmingEnrollment = false; Plugin.CancelEnrollment(); setupMessage = "";
                    if (connection.IsRunning) connection.Disconnect(); else connection.Connect();
                }
                GUI.Label(new Rect(0, 425, width, 42), Safe(connection.Status), label);
                GUI.Label(new Rect(0, 470, width, 24), string.IsNullOrEmpty(connection.Seed) ? "" : "Seed: " + Safe(connection.Seed), small);
                GUI.DrawTexture(new Rect(0, 498, width, 1), edge);
                GUI.Label(new Rect(0, 513, width, 28), "CHARACTER SETUP", heading);
                GUI.Label(new Rect(0, 550, width, 70), Safe(Plugin.EnrollmentSummary), muted);
                if (confirmingEnrollment) {
                    GUI.Label(new Rect(0, 625, width, 58), "Bind this fresh character and world to this seed? This choice is saved for future visits.", label);
                    if (GUI.Button(new Rect(0, 690, width * .65f - 4, 38), "Confirm and start", button)) {
                        setupMessage = Plugin.ConfirmEnrollment(); confirmingEnrollment = false;
                    }
                    if (GUI.Button(new Rect(width * .65f, 690, width * .35f, 38), "Cancel", button)) {
                        Plugin.CancelEnrollment(); confirmingEnrollment = false; setupMessage = "";
                    }
                } else {
                    GUI.enabled = wasEnabled && connection.IsConnected && Plugin.CanEnroll;
                    if (GUI.Button(new Rect(0, 625, width, 42), "Start this seed with this character", button)) {
                        setupMessage = Plugin.BeginEnrollment(); confirmingEnrollment = Plugin.HasPendingEnrollment;
                    }
                    GUI.enabled = wasEnabled;
                }
                GUI.Label(new Rect(0, 738, width, 50), Safe(setupMessage), muted);
            } finally { GUI.enabled = wasEnabled; GUI.EndScrollView(); }
        }
        private void DrawReceived(PlayerStatusDisplay status, Rect viewport)
        {
            DrawRows(viewport, status?.ReceivedItems ?? Array.Empty<string>(),
                status?.HistoryLimited == true ? "Recent items · older history omitted by the bridge" : "Items from Archipelago · newest first", false);
        }
        private void DrawRows(Rect viewport, IReadOnlyList<string> rows, string intro, bool split)
        {
            float width = viewport.width - 22;
            if (!ReferenceEquals(measuredRows[tab], rows) || measuredWidths[tab] != width) {
                measuredRows[tab] = rows; measuredWidths[tab] = width;
                rowHeights[tab] = new float[rows.Count];
                float total = rows.Count == 0 ? 136 : 46;
                for (int i = 0; i < rows.Count; i++) { rowHeights[tab][i] = RowHeight(rows[i], width, split); total += rowHeights[tab][i]; }
                measuredHeights[tab] = total;
            }
            float height = measuredHeights[tab];
            scroll[tab].y = PlayerStatusDisplay.ClampHudPosition(scroll[tab].y, height, viewport.height);
            scroll[tab] = GUI.BeginScrollView(viewport, scroll[tab], new Rect(0, 0, width, height));
            try {
                GUI.Label(new Rect(0, 0, width, 38), intro, muted);
                float y = 46;
                if (rows.Count == 0) GUI.Label(new Rect(0, y + 10, width, 80), "No items received yet.\nYour rewards will appear here as they arrive.", label);
                for (int i = 0; i < rows.Count; i++) {
                    string value = rows[i]; float rh = rowHeights[tab][i];
                    if (y + rh >= scroll[tab].y && y <= scroll[tab].y + viewport.height) {
                        int separator = split ? value.IndexOf('|') : -1;
                        if (separator >= 0) {
                            GUI.Label(new Rect(0, y + 6, width, 25), value.Substring(0, separator), label);
                            GUI.Label(new Rect(0, y + 33, width, rh - 39), value.Substring(separator + 1), muted);
                        } else GUI.Label(new Rect(0, y + 13, width, rh - 20), value, label);
                        GUI.DrawTexture(new Rect(0, y + rh - 1, width, 1), inset);
                    }
                    y += rh;
                }
            } finally { GUI.EndScrollView(); }
        }
        private float RowHeight(string value, float width, bool split)
        {
            int separator = split ? value.IndexOf('|') : -1;
            return separator < 0 ? Math.Max(58, label.CalcHeight(new GUIContent(value), width) + 27)
                : 43 + Math.Max(22, muted.CalcHeight(new GUIContent(value.Substring(separator + 1)), width));
        }
        private void DrawNotices(PlayerStatusDisplay status, float sw, float sh)
        {
            string text = status?.VisibleMessages(PlayerStatusDisplay.Now) ?? "";
            if (!string.IsNullOrEmpty(status?.RecipeNotice)) text = status.RecipeNotice + (text.Length == 0 ? "" : "\n" + text);
            if (text.Length == 0) return;
            float width = Math.Min(460, sw - 32), height = light.CalcHeight(new GUIContent(text), width - 32) + 28;
            float y = Math.Max(8, sh - height - 85);
            Panel(new Rect((sw - width) / 2, y, width, height), teal);
            GUI.Label(new Rect((sw - width) / 2 + 16, y + 14, width - 32, height - 20), text, light);
        }
        private void DrawItemNotices(PlayerStatusDisplay status, float sw)
        {
            if (status == null) return;
            float width = Math.Min(360, sw - 32), x = sw - width - 16, y = 80;
            foreach (var card in status.ItemNotices.Visible) {
                var style = card.Sent ? sentItem : foundItem;
                float first = style.CalcHeight(new GUIContent(card.Title), width - 28);
                float second = itemLocation.CalcHeight(new GUIContent(card.Location), width - 28);
                float height = first + second + 29;
                Panel(new Rect(x, y, width, height), teal);
                GUI.Label(new Rect(x + 14, y + 12, width - 28, first), card.Title, style);
                GUI.Label(new Rect(x + 14, y + 17 + first, width - 28, second), card.Location, itemLocation);
                y += height + 8;
            }
            if (status.ItemNotices.PendingCount > 0)
                GUI.Label(new Rect(x, y, width, 35), status.ItemNotices.PendingCount + " more queued", itemLocation);
        }
    }
}
