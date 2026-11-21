using System;
using UnityEngine;

namespace PixelSurvivor
{
    public class Health : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public float Normalized => Current / maxHealth;
        /// <summary>While true, damage is ignored (e.g. during a dash).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>Raised with the amount of damage taken.</summary>
        public event Action<float> Damaged;
        public event Action Died;

        private void OnEnable() => Current = maxHealth;

        public void SetMax(float value, bool refill = true)
        {
            maxHealth = Mathf.Max(1f, value);
            if (refill) Current = maxHealth;
            else Current = Mathf.Min(Current, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || Invulnerable || amount <= 0f) return;

            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }
    }
}
