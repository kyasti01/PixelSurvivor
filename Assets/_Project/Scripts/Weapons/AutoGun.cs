using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Menzildeki en yakın canlı düşmana otomatik ateş eder.</summary>
    public class AutoGun : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private AudioClip fireSfx;

        [Header("Stats")]
        [SerializeField, Min(0.02f)] private float fireInterval = 0.5f;
        [SerializeField, Min(0.5f)] private float range = 8f;
        [SerializeField, Min(0f)] private float damage = 13f;
        [SerializeField, Min(0.5f)] private float projectileSpeed = 10f;
        [Tooltip("Bir merminin delip geçtiği ek düşman sayısı.")]
        [SerializeField, Min(0)] private int pierce = 1;
        [SerializeField, Min(0f)] private float knockback = 1.2f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [Tooltip("Aynı anda birden fazla mermi atılırken mermiler arası açı (derece).")]
        [SerializeField, Min(0f)] private float spreadAngle = 12f;

        private ProjectilePool pool;
        private float cooldown;

        public float FireInterval { get => fireInterval; set => fireInterval = Mathf.Max(0.02f, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
        public int Pierce { get => pierce; set => pierce = Mathf.Max(0, value); }
        public int ProjectileCount { get => projectileCount; set => projectileCount = Mathf.Max(1, value); }

        private void Awake() => pool = new ProjectilePool(projectilePrefab, $"{name} Projectiles");
        private void OnDestroy() => pool?.Dispose();

        private void Update()
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Health.IsDead) return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            Vector2 origin = transform.position;
            Enemy target = ProjectilePool.FindNearestEnemy(origin, range);
            if (target == null) return;

            cooldown = fireInterval;
            Vector2 dir = ((Vector2)target.transform.position - origin).normalized;
            float hit = damage * PlayerController.CurrentDamageMultiplier;

            for (int i = 0; i < projectileCount; i++)
            {
                float offset = (i - (projectileCount - 1) * 0.5f) * spreadAngle;
                pool.Fire(origin, Quaternion.Euler(0f, 0f, offset) * dir, projectileSpeed, hit, pierce, knockback);
            }
            SfxPlayer.Play(fireSfx, 0.35f);
        }
    }
}
