using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameAnalytics
{
    [Serializable]
    internal class QueuedEvent
    {
        public string eventName;
        public string eventTimestamp;
        public string eventData;
        public string sessionId;
        public string userId;
    }

    [Serializable]
    internal class QueuedEventList
    {
        public List<QueuedEvent> events = new();
    }

    internal class EventQueue
    {
        private readonly List<QueuedEvent> _queue = new();
        private readonly object            _lock  = new();
        private readonly string            _persistPath;
        private const int MaxQueueSize = 500;

        public EventQueue()
        {
            _persistPath = Path.Combine(Application.persistentDataPath, "ga_queue.json");
            Load();
        }

        public void Enqueue(QueuedEvent ev)
        {
            lock (_lock)
            {
                if (_queue.Count >= MaxQueueSize) _queue.RemoveAt(0);
                _queue.Add(ev);
            }
            Save();
        }

        public List<QueuedEvent> Flush(int maxBatch = 50)
        {
            lock (_lock)
            {
                var batch = _queue.GetRange(0, Math.Min(maxBatch, _queue.Count));
                _queue.RemoveRange(0, batch.Count);
                Save();
                return new List<QueuedEvent>(batch);
            }
        }

        public int Count { get { lock (_lock) return _queue.Count; } }

        private void Save()
        {
            try
            {
                lock (_lock)
                {
                    var wrapper = new QueuedEventList { events = new List<QueuedEvent>(_queue) };
                    File.WriteAllText(_persistPath, JsonUtility.ToJson(wrapper));
                }
            }
            catch (Exception e) { Debug.LogWarning($"[GameAnalytics] Queue save failed: {e.Message}"); }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_persistPath)) return;
                var wrapper = JsonUtility.FromJson<QueuedEventList>(File.ReadAllText(_persistPath));
                if (wrapper?.events != null) _queue.AddRange(wrapper.events);
            }
            catch (Exception e) { Debug.LogWarning($"[GameAnalytics] Queue load failed: {e.Message}"); }
        }
    }
}
