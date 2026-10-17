using System;
using System.Collections.Generic;

namespace CraftopiaDiagnostic
{
    public sealed class ObservationBuffer
    {
        private readonly Queue<Dictionary<string, object>> pending = new Queue<Dictionary<string, object>>();
        private readonly int capacity;
        private readonly object gate = new object();
        private long dropped;
        public long Dropped { get { lock (gate) return dropped; } }
        public ObservationBuffer(int capacity) { if (capacity < 1) throw new ArgumentOutOfRangeException(); this.capacity = capacity; }
        public void Add(Dictionary<string, object> row)
        { lock (gate) { if (pending.Count >= capacity) { dropped++; return; } pending.Enqueue(row); } }
        public List<Dictionary<string, object>> Peek() { lock (gate) return new List<Dictionary<string, object>>(pending); }
        public void Confirm(int count) { lock (gate) { while (count-- > 0 && pending.Count > 0) pending.Dequeue(); } }
    }
}
