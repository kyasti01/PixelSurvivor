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
        [Tooltip("Damage per second dealt while touching the player.")]
        [Min(0f)] public float contactDps = 10f;
        [Tooltip("Experience dropped on death.")]
        [Min(1)] public int xpValue = 1;
        [Tooltip("Seconds into the run before this type starts spawning.")]
        [Min(0f)] public float unlockTime;
        [Tooltip("Seconds into the run after which this type stops spawning. 0 = never.")]
        [Min(0f)] public float retireTime;
        [Min(0f)] public float spawnWeight = 1f;

        [Tooltip("0 = full knockback, 1 = immovable.")]
        [Range(0f, 1f)] public float knockbackResistance;
        [Tooltip("Drops a treasure chest on death.")]
        public bool dropsChest;

        [Header("Ranged attack (Attack Range 0 = melee only)")]
        [Tooltip("Starts shooting within this distance and stops walking at 70 % of it.")]
        [Min(0f)] public float attackRange;
        [Min(0.2f)] public float attackInterval = 2.5f;
        [Min(0f)] public float projectileDamage = 10f;
        [Min(0.5f)] public float projectileSpeed = 6f;

        public bool IsRanged => attackRange > 0f;

        [Header("Look")]
        [Tooltip("Scales the whole enemy, collider included.")]
        [Min(0.3f)] public float scale = 1f;
        [Tooltip("Scales only the sprite; for art packs drawn at a different size.")]
        [Min(0.1f)] public float visualScale = 1f;
        [Tooltip("Moves only the sprite, e.g. to put a character's feet on its shadow.")]
        public Vector2 visualOffset;
        public Color tint = Color.white;

        public bool IsSpawnableAt(float time) => time >= unlockTime && (retireTime <= 0f || time < retireTime);
    }
}
