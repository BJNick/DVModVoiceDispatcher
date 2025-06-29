using System.Collections.Generic;

namespace VoiceDispatcherMod {
    public class NarratorLineQueue {
        private readonly Queue<QueueItem> _queue = new Queue<QueueItem>();
        private readonly HashSet<QueueItem> _inQueue = new HashSet<QueueItem>();
        public int Count => _queue.Count;

        public void Enqueue(List<string> lines) {
            var queueItem = new QueueItem(lines);
            Enqueue(queueItem);
        }
        
        private void Enqueue(QueueItem queueItem) {
            if (queueItem == null || queueItem.lines == null || queueItem.lines.Count == 0) {
                return; // Ignore empty items
            }
            if (_inQueue.Contains(queueItem)) {
                return; // Already in queue
            }
            _queue.Enqueue(queueItem);
            _inQueue.Add(queueItem);
        }
        
        public List<string> Dequeue() {
            if (_queue.Count == 0) {
                return null; // No items to dequeue
            }
            var item = _queue.Dequeue();
            _inQueue.Remove(item);
            return item.lines;
        }
        
        public bool HasLines() {
            return _queue.Count != 0;
        }

        public void Clear() {
            _queue.Clear();
            _inQueue.Clear();
        }
        
    }

    public class QueueItem {
        public readonly List<string> lines;
        public string key => string.Join(",", lines);
        
        public QueueItem(List<string> lines) {
            this.lines = lines;
        }

        private bool Equals(QueueItem other) {
            return Equals(key, other.key);
        }

        public override bool Equals(object obj) {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((QueueItem)obj);
        }

        public override int GetHashCode() {
            return (key != null ? key.GetHashCode() : 0);
        }
    }
}