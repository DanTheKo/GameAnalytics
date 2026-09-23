using UnityEngine;

namespace GameAnalytics
{
    public static class GA
    {
        private static GameAnalyticsBehaviour _behaviour;
        private static bool                   _initialized;

        public static void Init(string apiUrl, string apiKey)
        {
            if (_initialized) { Debug.LogWarning("[GameAnalytics] Already initialized."); return; }

            var config = new GameAnalyticsConfig(apiUrl, apiKey);
            var go     = new GameObject("[GameAnalytics]");
            Object.DontDestroyOnLoad(go);

            _behaviour   = go.AddComponent<GameAnalyticsBehaviour>();
            _behaviour.Init(config);
            _initialized = true;

            Debug.Log($"[GameAnalytics] Initialized. Player: {config.PlayerId}");
        }

        public static void Track(string eventName, object data = null)
        {
            if (!CheckInit()) return;
            _behaviour.Enqueue(eventName, data != null ? JsonUtility.ToJson(data) : "{}");
        }

        public static void Revenue(double amount, string currency = "USD", string itemName = null)
        {
            if (!CheckInit()) return;
            var json = itemName != null
                ? $"{{\"amount\":{amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"currency\":\"{currency}\",\"item\":\"{itemName}\"}}"
                : $"{{\"amount\":{amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"currency\":\"{currency}\"}}";
            _behaviour.Enqueue("iap_purchase", json);
        }

        public static void LevelStart(int level)    => Track("level_start",    new { level_id = level });
        public static void LevelComplete(int level, int score = 0) => Track("level_complete", new { level_id = level, score });
        public static void LevelFail(int level)     => Track("level_fail",     new { level_id = level });
        public static void TutorialComplete()        => Track("tutorial_finish");

        private static bool CheckInit()
        {
            if (_initialized) return true;
            Debug.LogWarning("[GameAnalytics] Call GA.Init() before tracking events.");
            return false;
        }
    }
}
