using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameAnalytics
{
    internal class GameAnalyticsClient
    {
        private readonly GameAnalyticsConfig _config;

        public GameAnalyticsClient(GameAnalyticsConfig config)
        {
            _config = config;
        }

        public IEnumerator UpsertUser(Action<bool> onDone = null)
        {
            // Field names must match C# record property names exactly (PascalCase)
            // because ASP.NET deserializes by property name
            var body = JsonBuilder.Object(
                ("UserId",     _config.PlayerId.ToString()),
                ("Country",    _config.Country),
                ("Platform",   _config.Platform),
                ("DeviceInfo", _config.DeviceInfo)
            );

            bool ok = false;
            yield return Post("/api/ingest/users", body, _ => ok = true, _ => { });
            onDone?.Invoke(ok);
        }

        // Returns the session ID assigned by the server, or Guid.Empty on failure
        public IEnumerator StartSession(Action<Guid> onDone)
        {
            var body = JsonBuilder.Object(
                ("UserId",    _config.PlayerId.ToString()),
                ("StartTime", DateTime.UtcNow.ToString("o"))
            );

            Guid sessionId = Guid.Empty;
            yield return Post("/api/ingest/sessions/start", body,
                json =>
                {
                    // Parse {"id":"..."} from server response
                    var parsed = SimpleJson.ParseField(json, "id");
                    if (!string.IsNullOrEmpty(parsed) && Guid.TryParse(parsed, out var sid))
                        sessionId = sid;
                    else
                        Debug.LogWarning($"[GameAnalytics] Could not parse session ID from: {json}");
                },
                err => Debug.LogWarning($"[GameAnalytics] StartSession failed: {err}"));

            onDone(sessionId);
        }

        public IEnumerator EndSession(Guid sessionId, Action<bool> onDone = null)
        {
            if (sessionId == Guid.Empty) { onDone?.Invoke(false); yield break; }

            var body = JsonBuilder.Object(("EndTime", DateTime.UtcNow.ToString("o")));

            bool ok = false;
            yield return Post($"/api/ingest/sessions/{sessionId}/end", body, _ => ok = true, _ => { });
            onDone?.Invoke(ok);
        }

        public IEnumerator SendBatch(List<QueuedEvent> events, Action<bool> onDone = null)
        {
            var sb = new StringBuilder();
            sb.Append("{\"Events\":[");
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                sb.Append(JsonBuilder.Object(
                    ("EventName",      e.eventName),
                    ("EventTimestamp", e.eventTimestamp),
                    ("EventData",      e.eventData),
                    ("SessionId",      e.sessionId),
                    ("UserId",         e.userId)
                ));
                if (i < events.Count - 1) sb.Append(',');
            }
            sb.Append("]}");

            bool ok = false;
            yield return Post("/api/ingest/events", sb.ToString(), _ => ok = true, _ => { });
            onDone?.Invoke(ok);
        }

        private IEnumerator Post(string path, string json,
            Action<string> onSuccess, Action<string> onError)
        {
            var bytes   = Encoding.UTF8.GetBytes(json);
            var request = new UnityWebRequest(_config.ApiUrl + path, "POST")
            {
                uploadHandler   = new UploadHandlerRaw(bytes),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Api-Key",    _config.ApiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onSuccess(request.downloadHandler.text);
            else
                onError(request.error + " | " + request.downloadHandler.text);

            request.Dispose();
        }
    }

    // Minimal JSON helpers to avoid Unity's JsonUtility limitations with anonymous types
    internal static class JsonBuilder
    {
        public static string Object(params (string key, string value)[] fields)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(fields[i].key).Append("\":\"")
                  .Append(Escape(fields[i].value)).Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static string Escape(string s) =>
            s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
    }

    internal static class SimpleJson
    {
        // Extracts the value of the first occurrence of "key":"value" from a JSON string
        public static string ParseField(string json, string key)
        {
            var search = $"\"{key}\":\"";
            var start  = json.IndexOf(search, StringComparison.Ordinal);
            if (start < 0) return null;
            start += search.Length;
            var end = json.IndexOf('"', start);
            return end < 0 ? null : json.Substring(start, end - start);
        }
    }
}
