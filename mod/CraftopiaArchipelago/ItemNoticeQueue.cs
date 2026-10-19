using System;
using System.Collections.Generic;

namespace CraftopiaArchipelago
{
    // Cosmetic only. Time advances once per rendered frame, never from OnGUI.
    public sealed class ItemNoticeQueue
    {
        public sealed class Card
        {
            public string Id, Title, Location;
            public bool Sent;
            public double Remaining = 6;
        }
        private readonly HashSet<string> seen = new HashSet<string>();
        private readonly Queue<Card> pending = new Queue<Card>();
        private readonly List<Card> visible = new List<Card>();
        public IReadOnlyList<Card> Visible => visible;
        public int PendingCount => pending.Count;
        public void Clear() { seen.Clear(); pending.Clear(); visible.Clear(); }
        public void Add(string id, string title, string location, bool sent, bool silent = false)
        {
            if (string.IsNullOrEmpty(id) || !seen.Add(id) || silent) return;
            pending.Enqueue(new Card { Id=id, Title=Plain(title), Location=Plain(location), Sent=sent });
        }
        private static string Plain(string text)
        {
            var result = new System.Text.StringBuilder();
            foreach(char c in text ?? "") if (!char.IsControl(c)) result.Append(c);
            return result.ToString(); // Rendered with richText=false; preserve long names for wrapping.
        }
        public void Advance(double seconds, bool paused)
        {
            if (paused) return;
            foreach (var card in visible) card.Remaining -= Math.Max(0, seconds);
            visible.RemoveAll(card => card.Remaining <= 0);
            while (visible.Count < 3 && pending.Count > 0) visible.Add(pending.Dequeue());
        }
    }
}
