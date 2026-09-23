using System.Collections;
using UnityEngine;

namespace GameAnalytics
{
    internal class GameAnalyticsBehaviour : MonoBehaviour
    {
        private GameAnalyticsConfig _config;
        private GameAnalyticsClient _client;
        private SessionManager      _session;
        private EventQueue          _queue;

        private float _flushInterval = 10f;
        private float _flushTimer;
        private bool  _ready;       // true once user upsert + session start complete

        internal void Init(GameAnalyticsConfig config)
        {
            _config  = config;
            _client  = new GameAnalyticsClient(config);
            _queue   = new EventQueue();

            StartCoroutine(Startup());
        }

        private IEnumerator Startup()
        {
            // 1. Register/update the player first
            bool userOk = false;
            yield return _client.UpsertUser(ok => userOk = ok);
            if (!userOk)
                Debug.LogWarning("[GameAnalytics] User upsert failed — will retry on next launch.");

            // 2. Start session — SDK is "ready" only after server confirms session ID
            _session = new SessionManager(_client, this);
            _session.StartSession(onReady: () => _ready = true);

            // Wait up to 5s for session confirmation before giving up
            float waited = 0f;
            while (!_ready && waited < 5f)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                waited += 0.1f;
            }

            if (!_ready)
            {
                Debug.LogWarning("[GameAnalytics] Session start timed out — proceeding without session.");
                _ready = true;
            }
        }

        internal void Enqueue(string eventName, string eventDataJson)
        {
            _queue.Enqueue(new QueuedEvent
            {
                eventName      = eventName,
                eventTimestamp = System.DateTime.UtcNow.ToString("o"),
                eventData      = eventDataJson,
                // Empty string is sent as null-equivalent — backend handles missing sessionId gracefully
                sessionId      = _session?.IsActive == true ? _session.CurrentSessionId.ToString() : "",
                userId         = _config.PlayerId.ToString(),
            });
        }

        private void Update()
        {
            if (!_ready) return;
            _flushTimer += Time.unscaledDeltaTime;
            if (_flushTimer >= _flushInterval && _queue.Count > 0)
            {
                _flushTimer = 0f;
                StartCoroutine(Flush());
            }
        }

        private void OnApplicationPause(bool paused)
        {
            _session?.OnPause(paused);
            if (paused && _queue.Count > 0)
                StartCoroutine(Flush());
        }

        private void OnApplicationQuit()
        {
            _session?.EndSession();
            if (_queue.Count > 0)
                StartCoroutine(Flush());
        }

        private IEnumerator Flush()
        {
            while (_queue.Count > 0)
            {
                var batch = _queue.Flush(50);
                bool ok   = false;
                yield return _client.SendBatch(batch, success => ok = success);

                if (!ok)
                {
                    foreach (var ev in batch)
                        _queue.Enqueue(ev);
                    yield break;
                }
            }
        }
    }
}
