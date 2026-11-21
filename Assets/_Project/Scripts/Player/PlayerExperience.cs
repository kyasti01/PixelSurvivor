using System;
using UnityEngine;

namespace PixelSurvivor
{
    public class PlayerExperience : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float pickupRadius = 2.5f;
        [Tooltip("Seviye 1 -> 2 için gereken XP.")]
        [SerializeField, Min(1)] private int baseXpToLevel = 3;
        [Tooltip("Sonraki her seviye için gereken ek XP.")]
        [SerializeField, Min(0)] private int xpGrowthPerLevel = 3;
        [SerializeField] private AudioClip levelUpSfx;

        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNext => baseXpToLevel + (Level - 1) * xpGrowthPerLevel;
        public float Progress => (float)Xp / XpToNext;
        /// <summary>Kazanılmış ama yükseltmesi henüz seçilmemiş seviyeler.</summary>
        public int PendingLevelUps { get; private set; }

        public float PickupRadius
        {
            get => pickupRadius;
            set => pickupRadius = Mathf.Max(0.1f, value);
        }

        /// <summary>Kazanılan her seviye için bir kez tetiklenir.</summary>
        public event Action LeveledUp;

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;

            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                PendingLevelUps++;
                SfxPlayer.Play(levelUpSfx, 0.7f);
                LeveledUp?.Invoke();
            }
        }

        public bool TryConsumeLevelUp()
        {
            if (PendingLevelUps <= 0) return false;
            PendingLevelUps--;
            return true;
        }
    }
}
