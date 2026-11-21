using UnityEngine;

namespace PixelSurvivor
{
    /// <summary><see cref="OrbitWeapon"/> silahının bir küreği; her geçişte her düşmana bir kez vurur.</summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class OrbitBlade : MonoBehaviour
    {
        private OrbitWeapon owner;

        public void Init(OrbitWeapon weapon) => owner = weapon;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (owner != null && other.TryGetComponent(out Enemy enemy) && enemy.IsAlive)
                owner.HandleHit(enemy);
        }
    }
}
