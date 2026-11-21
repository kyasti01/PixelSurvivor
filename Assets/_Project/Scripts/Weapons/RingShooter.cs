using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Her yöne bir çember mermi atar. Her atış yarım adım döndürülür;
    /// böylece arka arkaya gelen çemberler birbirinin boşluklarını kapatır.
    /// </summary>
    public class RingShooter : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private AudioClip fireSfx;

        [Header("Stats")]
        [SerializeField, Min(0.05f)] private float fireInterval = 1.4f;
        [SerializeField, Min(1)] private int count = 8;
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.5f)] private float projectileSpeed = 7f;
        [SerializeField, Min(0)] private int pierce = 1;
        [Tooltip("Düşük tut: her yöne ateş eden çember güçlü geri itmeyle oyuncuyu dokunulmaz yapar.")]
        [SerializeField, Min(0f)] private float knockback = 0.8f;
        [Tooltip("Sadece bu mesafede bir düşman varken ateş eder; boşa ateş etmesin diye.")]
        [SerializeField, Min(1f)] private float activationRange = 10f;

        private ProjectilePool pool;
        private float cooldown;
        private bool halfStep;

        public float FireInterval { get => fireInterval; set => fireInterval = Mathf.Max(0.05f, value); }
        public int Count { get => count; set => count = Mathf.Max(1, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }

        private void Awake() => pool = new ProjectilePool(projectilePrefab, $"{name} Projectiles");
        private void OnDestroy() => pool?.Dispose();

        private void Update()
        {
            if (PlayerController.Instance != null && PlayerController.Instance.Health.IsDead) return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            Vector2 origin = transform.position;
            if (ProjectilePool.FindNearestEnemy(origin, activationRange) == null) return;

            cooldown = fireInterval;
            float step = 360f / count;
            float start = halfStep ? step * 0.5f : 0f;
            halfStep = !halfStep;
            float hit = damage * PlayerController.CurrentDamageMultiplier;

            for (int i = 0; i < count; i++)
            {
                float a = (start + i * step) * Mathf.Deg2Rad;
                pool.Fire(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), projectileSpeed, hit, pierce, knockback);
            }
            SfxPlayer.Play(fireSfx, 0.45f);
        }
    }
}
