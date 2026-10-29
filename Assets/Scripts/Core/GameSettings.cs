using System;
using UnityEngine;

namespace BustAKlud
{
    /// <summary>Player preferences and the high score, persisted through PlayerPrefs (IndexedDB on WebGL).</summary>
    public static class GameSettings
    {
        const string MusicKey = "bak.music";
        const string SfxKey = "bak.sfx";
        const string GuideKey = "bak.guide";
        const string BestKey = "bak.best";

        public static event Action Changed;

        public static bool MusicOn
        {
            get => GetFlag(MusicKey, true);
            set => SetFlag(MusicKey, value);
        }

        public static bool SfxOn
        {
            get => GetFlag(SfxKey, true);
            set => SetFlag(SfxKey, value);
        }

        public static bool AimGuide
        {
            get => GetFlag(GuideKey, true);
            set => SetFlag(GuideKey, value);
        }

        public static int HighScore
        {
            get => PlayerPrefs.GetInt(BestKey, 0);
            set
            {
                PlayerPrefs.SetInt(BestKey, value);
                PlayerPrefs.Save();
            }
        }

        static bool GetFlag(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;

        static void SetFlag(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
