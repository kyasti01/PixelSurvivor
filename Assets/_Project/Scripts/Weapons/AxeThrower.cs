using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Havaya dönen baltalar fırlatır; kalabalığın içinden kavis çizerek düşerler ve değdikleri her şeyi
    /// delerler. Sıfır baltayla başlar; yükseltmeler balta ekler.
    /// </summary>
    public class AxeThrower : MonoBehaviour
    {
        [SerializeField] private Projectile axePrefab;
        [SerializeField] private AudioClip throwSfx;

        [Header("Stats")]
        [SerializeField, Min(0)] private int count;
        [SerializeField, Min(0.1f)] private float fireInterval = 1.5f;
        [SerializeField, Min(0f)] private float damage = 27f;
        [SerializeField, Min(1f)] private float upSpeed = 10f;
        [Tooltip("Yanlara hız aralığı; birden fazla balta bu aralığa yelpaze gibi dağılır.")]
        [SerializeField, Min(0f)] private float sideSpeed = 3f;
        [SerializeField, Min(0)] private int pierce = 50;
        [SerializeField, Min(0f)] private float knockback = 1.5f;
        [SerializeField, Min(1f)] private float activationRange = 12f;

        private ProjectilePool pool;
        private float cooldown;

        public int Count { get => count; set => count = Mathf.Max(0, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
        public float FireInterval { get => fireInterval; set => fireInterval = Mathf.Max(0.1f, value); }

        private void Awake() => pool = new ProjectilePool(axePrefab, $"{name} Projectiles");
        private void OnDestroy() => pool?.Dispose();

        private void Update()
        {
            if (count == 0) return;
            if (PlayerController.Instance != null && PlayerController.Instance.Health.IsDead) return;

            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;

            Vector2 origin = transform.position;
            if (ProjectilePool.FindNearestEnemy(origin, activationRange) == null) return;
            cooldown = fireInterval;

            float hit = damage * PlayerController.CurrentDamageMultiplier;
            for (int i = 0; i < count; i++)
            {
                float side = count == 1 ? Random.Range(-1f, 1f) : Mathf.Lerp(-1f, 1f, i / (count - 1f));
                var velocity = new Vector2(side * sideSpeed + Random.Range(-0.4f, 0.4f), upSpeed);
                pool.Fire(origin, velocity.normalized, velocity.magnitude, hit, pierce, knockback);
            }
            SfxPlayer.Play(throwSfx, 0.5f);
        }
    }
}
