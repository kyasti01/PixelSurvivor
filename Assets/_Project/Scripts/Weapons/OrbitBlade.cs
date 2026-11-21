using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>One blade of an <see cref="OrbitWeapon"/>; hits each enemy once per pass.</summary>
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
