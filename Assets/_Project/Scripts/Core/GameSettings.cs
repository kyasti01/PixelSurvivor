using System;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Player settings stored in PlayerPrefs (volume levels).</summary>
    public static class GameSettings
    {
        private const string MusicKey = "settings.musicVolume";
        private const string SfxKey = "settings.sfxVolume";

        /// <summary>Raised whenever a setting changes.</summary>
        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
            set => Store(MusicKey, value);
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => Store(SfxKey, value);
        }

        private static void Store(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;
    }
}
