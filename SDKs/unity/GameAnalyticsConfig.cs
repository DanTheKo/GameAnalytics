using System;
using UnityEngine;

namespace GameAnalytics
{
    internal class GameAnalyticsConfig
    {
        public string ApiUrl     { get; }
        public string ApiKey     { get; }
        public Guid   PlayerId   { get; }
        public string Country    { get; }
        public string Platform   { get; }
        public string DeviceInfo { get; }

        private const string PlayerIdKey = "ga_player_id";

        public GameAnalyticsConfig(string apiUrl, string apiKey)
        {
            ApiUrl  = apiUrl.TrimEnd('/');
            ApiKey  = apiKey;

            var stored = PlayerPrefs.GetString(PlayerIdKey, "");
            if (!Guid.TryParse(stored, out var pid))
            {
                pid = Guid.NewGuid();
                PlayerPrefs.SetString(PlayerIdKey, pid.ToString());
                PlayerPrefs.Save();
            }
            PlayerId = pid;

            Country    = RegionInfo();
            Platform   = Application.platform.ToString();
            DeviceInfo = $"{SystemInfo.deviceModel} / {SystemInfo.operatingSystem}";
        }

        private static string RegionInfo()
        {
            try   { return System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName; }
            catch { return ""; }
        }
    }
}
