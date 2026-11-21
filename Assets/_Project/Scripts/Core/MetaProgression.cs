using UnityEngine;

namespace PixelSurvivor
{
    public enum MetaStat { MaxHealth, Damage, MoveSpeed, PickupRadius, Greed }

    /// <summary>
    /// Gold and permanent upgrades bought in the main-menu shop. Stored in PlayerPrefs,
    /// so they survive between runs and game sessions.
    /// </summary>
    public static class MetaProgression
    {
        public const int MaxLevel = 5;
        private const string GoldKey = "meta.gold";

        public static int Gold
        {
            get => PlayerPrefs.GetInt(GoldKey, 0);
            private set
            {
                PlayerPrefs.SetInt(GoldKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static void AddGold(int amount)
        {
            if (amount > 0) Gold += amount;
        }

        public static int Level(MetaStat stat) => PlayerPrefs.GetInt(LevelKey(stat), 0);
        public static bool IsMaxed(MetaStat stat) => Level(stat) >= MaxLevel;
        public static int Cost(MetaStat stat) => 25 * (Level(stat) + 1);
        public static bool CanBuy(MetaStat stat) => !IsMaxed(stat) && Gold >= Cost(stat);

        public static bool TryBuy(MetaStat stat)
        {
            if (!CanBuy(stat)) return false;
            Gold -= Cost(stat);
            PlayerPrefs.SetInt(LevelKey(stat), Level(stat) + 1);
            PlayerPrefs.Save();
            return true;
        }

        // Bonuses applied at the start of every run.
        public static float HealthBonus => 10f * Level(MetaStat.MaxHealth);
        public static float DamageMultiplier => 1f + 0.06f * Level(MetaStat.Damage);
        public static float SpeedMultiplier => 1f + 0.04f * Level(MetaStat.MoveSpeed);
        public static float PickupMultiplier => 1f + 0.1f * Level(MetaStat.PickupRadius);
        public static float GoldMultiplier => 1f + 0.15f * Level(MetaStat.Greed);

        public static string DisplayName(MetaStat stat) => stat switch
        {
            MetaStat.MaxHealth => "Sağlamlık",
            MetaStat.Damage => "Güç",
            MetaStat.MoveSpeed => "Çeviklik",
            MetaStat.PickupRadius => "Mıknatıs",
            MetaStat.Greed => "Açgözlülük",
            _ => stat.ToString(),
        };

        public static string Description(MetaStat stat) => stat switch
        {
            MetaStat.MaxHealth => "Seviye başına +10 maksimum can",
            MetaStat.Damage => "Seviye başına +%6 hasar",
            MetaStat.MoveSpeed => "Seviye başına +%4 hareket hızı",
            MetaStat.PickupRadius => "Seviye başına +%10 toplama alanı",
            MetaStat.Greed => "Seviye başına +%15 altın",
            _ => "",
        };

        private static string LevelKey(MetaStat stat) => "meta.level." + stat;

        // ---------------------------------------------------------------- victories & characters

        /// <summary>True once the final boss has been beaten at least once.</summary>
        public static bool HasWon => PlayerPrefs.GetInt("meta.won", 0) == 1;

        public static void RecordVictory()
        {
            PlayerPrefs.SetInt("meta.won", 1);
            PlayerPrefs.Save();
        }

        public static bool IsUnlocked(CharacterDefinition c)
        {
            if (c.unlockCost <= 0 && !c.requiresVictory) return true;
            if (c.requiresVictory) return HasWon;
            return PlayerPrefs.GetInt(CharacterKey(c), 0) == 1;
        }

        /// <summary>Buys a gold-locked character. Victory-locked ones unlock by winning instead.</summary>
        public static bool TryUnlock(CharacterDefinition c)
        {
            if (IsUnlocked(c)) return true;
            if (c.requiresVictory || Gold < c.unlockCost) return false;
            Gold -= c.unlockCost;
            PlayerPrefs.SetInt(CharacterKey(c), 1);
            PlayerPrefs.Save();
            return true;
        }

        private static string CharacterKey(CharacterDefinition c) => "meta.character." + c.name;
    }
}
