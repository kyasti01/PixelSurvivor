using UnityEngine;
using UnityEngine.Pool;

namespace PixelSurvivor
{
    /// <summary>
    /// Pool of projectiles parented to a scene-root object, so shots don't move with the shooter.
    /// </summary>
    public sealed class ProjectilePool
    {
        private readonly ObjectPool<Projectile> pool;
        private readonly Transform root;

        public ProjectilePool(Projectile prefab, string name)
        {
            root = new GameObject(name).transform;
            pool = new ObjectPool<Projectile>(
                createFunc: () => Object.Instantiate(prefab, root),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Object.Destroy(p.gameObject),
                collectionCheck: false,
                defaultCapacity: 32,
                maxSize: 512);
        }

        public void Fire(Vector2 origin, Vector2 direction, float speed, float damage, int pierce, float knockback)
        {
            pool.Get().Launch(origin, direction, speed, damage, pierce, knockback, pool.Release);
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }

        /// <summary>Nearest living enemy within <paramref name="range"/> of <paramref name="origin"/>, or null.</summary>
        public static Enemy FindNearestEnemy(Vector2 origin, float range)
        {
            float bestSqr = range * range;
            Enemy best = null;
            foreach (Enemy e in Enemy.Active)
            {
                if (!e.IsAlive) continue;
                float sqr = ((Vector2)e.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }
    }
}
