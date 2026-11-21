using UnityEngine;
using UnityEngine.Pool;

namespace PixelSurvivor
{
    /// <summary>Bütün menzilli düşmanların mermileri için ortak havuz.</summary>
    public class EnemyProjectiles : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile prefab;
        [SerializeField] private AudioClip fireSfx;

        private static EnemyProjectiles instance;
        private ObjectPool<EnemyProjectile> pool;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            instance = this;
            pool = new ObjectPool<EnemyProjectile>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: false,
                defaultCapacity: 32,
                maxSize: 256);
        }

        public static void Fire(Vector2 origin, Vector2 direction, float speed, float damage)
        {
            if (instance == null || instance.prefab == null) return;
            instance.pool.Get().Launch(origin, direction, speed, damage, instance.pool.Release);
            SfxPlayer.Play(instance.fireSfx, 0.3f);
        }
    }
}
