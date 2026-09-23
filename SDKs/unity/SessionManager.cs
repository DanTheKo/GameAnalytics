using System;
using System.Collections;
using UnityEngine;

namespace GameAnalytics
{
    internal class SessionManager
    {
        private readonly GameAnalyticsClient _client;
        private readonly MonoBehaviour       _runner;

        // Guid.Empty means no active session — events must not be sent until this is set
        public Guid CurrentSessionId { get; private set; } = Guid.Empty;
        public bool IsActive         => CurrentSessionId != Guid.Empty;
        public bool IsStarting       { get; private set; }

        public SessionManager(GameAnalyticsClient client, MonoBehaviour runner)
        {
            _client = client;
            _runner = runner;
        }

        public void StartSession(Action onReady = null)
        {
            if (IsActive || IsStarting) { onReady?.Invoke(); return; }
            IsStarting = true;
            _runner.StartCoroutine(DoStart(onReady));
        }

        private IEnumerator DoStart(Action onReady)
        {
            yield return _client.StartSession(sid =>
            {
                CurrentSessionId = sid;
                IsStarting       = false;

                if (sid == Guid.Empty)
                    Debug.LogWarning("[GameAnalytics] Session start failed — events will be sent without a session ID.");
                else
                    Debug.Log($"[GameAnalytics] Session started: {sid}");

                onReady?.Invoke();
            });
        }

        public void EndSession()
        {
            if (!IsActive) return;
            var sid = CurrentSessionId;
            CurrentSessionId = Guid.Empty;
            _runner.StartCoroutine(_client.EndSession(sid));
        }

        public void OnPause(bool paused)
        {
            if (paused) EndSession();
            else        StartSession();
        }
    }
}
