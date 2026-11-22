using System;
using UnityEngine;

namespace PixelSurvivor
{
    [Serializable]
    public class EnemyStats
    {
        public string name = "Zombie";
        public RuntimeAnimatorController animator;
        [Min(1f)] public float maxHealth = 20f;
        [Min(0f)] public float moveSpeed = 1.5f;
        // Oyuncuya değerken verilen saniyelik hasar.
        [Min(0f)] public float contactDps = 10f;
        // Ölünce düşen XP.
        [Min(1)] public int xpValue = 1;
        // Bu türün doğmaya başladığı saniye.
        [Min(0f)] public float unlockTime;
        // Bu türün artık doğmadığı saniye. 0 = hiçbir zaman.
        [Min(0f)] public float retireTime;
        [Min(0f)] public float spawnWeight = 1f;

        // 0 = tam geri itilme, 1 = hiç kıpırdamaz.
        [Range(0f, 1f)] public float knockbackResistance;
        // Ölünce hazine sandığı düşürür.
        public bool dropsChest;

        [Header("Ranged attack (Attack Range 0 = melee only)")]
        // Bu mesafenin içinde ateş etmeye başlar, %70'inde yürümeyi bırakır.
        [Min(0f)] public float attackRange;
        [Min(0.2f)] public float attackInterval = 2.5f;
        [Min(0f)] public float projectileDamage = 10f;
        [Min(0.5f)] public float projectileSpeed = 6f;

        public bool IsRanged => attackRange > 0f;

        [Header("Look")]
        // Düşmanın tamamını ölçekler, collider dahil.
        [Min(0.3f)] public float scale = 1f;
        // Sadece sprite'ı ölçekler; farklı boyutta çizilmiş paketler için.
        [Min(0.1f)] public float visualScale = 1f;
        // Sadece sprite'ı kaydırır; örn. karakterin ayakları gölgesinin üstüne gelsin diye.
        public Vector2 visualOffset;
        public Color tint = Color.white;

        public bool IsSpawnableAt(float time) => time >= unlockTime && (retireTime <= 0f || time < retireTime);
    }
}
