using UnityEngine;

namespace PixelSurvivor
{
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>Menüde seçilen zorluk (PlayerPrefs'te saklanır) ve neyi değiştirdiği.</summary>
    public static class DifficultySettings
    {
        private const string Key = "settings.difficulty";

        public static Difficulty Current
        {
            get => (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)Difficulty.Normal), 0, 2);
            set
            {
                PlayerPrefs.SetInt(Key, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static float EnemyHealth => Pick(0.7f, 1f, 1.4f);
        public static float EnemyDamage => Pick(0.6f, 1f, 1.4f);
        /// <summary>Düşman doğma aralığını çarpar: yüksek = daha az düşman.</summary>
        public static float SpawnInterval => Pick(1.3f, 1f, 0.8f);
        public static float GoldReward => Pick(0.75f, 1f, 1.5f);

        public static string DisplayName(Difficulty d) => d switch
        {
            Difficulty.Easy => "KOLAY",
            Difficulty.Hard => "ZOR",
            _ => "NORMAL",
        };

        private static float Pick(float easy, float normal, float hard) => Current switch
        {
            Difficulty.Easy => easy,
            Difficulty.Hard => hard,
            _ => normal,
        };
    }
}
