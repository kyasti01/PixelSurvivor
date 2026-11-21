using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Drops fire bombs on random enemies near the player; each blast damages everything in
    /// its radius. Starts with zero bombs; upgrades add them.
    /// </summary>
    public class BombThrower : MonoBehaviour
    {
        [SerializeField] private Sprite[] explosionFrames;
        [SerializeField, Min(1f)] private float explosionFps = 14f;
        [Tooltip("World width of the last explosion frame at scale 1; used to match the sprite to the blast radius.")]
        [SerializeField, Min(0.01f)] private float explosionSpriteWidth = 1.5f;
        [SerializeField] private AudioClip explosionSfx;

        [Header("Stats")]
        [SerializeField, Min(0)] private int count;
        [SerializeField, Min(0.1f)] private float fireInterval = 2.2f;
        [SerializeField, Min(0.2f)] private float radius = 1.5f;
        [SerializeField, Min(0f)] private float damage = 30f;
        [SerializeField, Min(0f)] private float knockback = 3f;
        [SerializeField, Min(1f)] private float range = 9f;

        private readonly List<Collider2D> hits = new();
        private ContactFilter2D enemyFilter;
        private float cooldown;

        public int Count { get => count; set => count = Mathf.Max(0, value); }
        public float Radius { get => radius; set => radius = Mathf.Max(0.2f, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }

        private void Awake()
        {
            enemyFilter = new ContactFilter2D { useTriggers = false };
            enemyFilter.SetLayerMask(LayerMask.GetMask(GameLayers.Enemy));
        }

        private void Update()
        {
            if (count == 0) return;
            if (PlayerController.Instance != null && PlayerController.Instance.Health.IsDead) return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            bool fired = false;
            for (int i = 0; i < count; i++)
            {
                Enemy target = RandomEnemyInRange();
                if (target == null) break;
                Explode(target.transform.position);
                fired = true;
            }
            if (fired) cooldown = fireInterval;
        }

        private Enemy RandomEnemyInRange()
        {
            if (Enemy.Active.Count == 0) return null;
            Vector2 origin = transform.position;
            float sqrRange = range * range;

            // A few random samples is plenty with a crowd on screen, and costs O(1).
            for (int tries = 0; tries < 12; tries++)
            {
                Enemy e = Enemy.Active[Random.Range(0, Enemy.Active.Count)];
                if (e.IsAlive && ((Vector2)e.transform.position - origin).sqrMagnitude <= sqrRange) return e;
            }
            return null;
        }

        private void Explode(Vector2 center)
        {
            EffectPlayer.Play(explosionFrames, explosionFps, center, radius * 2f / explosionSpriteWidth);
            SfxPlayer.Play(explosionSfx, 0.35f);
            CameraFollow.Shake(0.06f, 0.1f);

            float hit = damage * PlayerController.CurrentDamageMultiplier;
            Physics2D.OverlapCircle(center, radius, enemyFilter, hits);
            foreach (Collider2D c in hits)
            {
                if (!c.TryGetComponent(out Enemy enemy) || !enemy.IsAlive) continue;
                Vector2 away = ((Vector2)enemy.transform.position - center).normalized;
                enemy.TakeHit(hit, away * knockback);
            }
        }
    }
}
