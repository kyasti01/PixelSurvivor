using System;
using UnityEngine;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float lifetime = 2f;
        [Tooltip("Sprite uçuş yönünü göstersin diye eklenen dönüş (yukarı bakacak çizilmiş sprite'lar için -90).")]
        [SerializeField] private float spriteAngleOffset = -90f;
        [Tooltip("Aşağı doğru ivme; kavis çizerek atılan silahlar için (0 = düz çizgi).")]
        [SerializeField, Min(0f)] private float gravity;
        [Tooltip("Saniyede derece. Sıfır değilse sprite uçuş yönüne hizalanmak yerine döner.")]
        [SerializeField] private float spinSpeed;

        private Rigidbody2D rb;
        private Action<Projectile> release;
        private float damage;
        private float knockback;
        private int pierceLeft;
        private float lifeLeft;
        private bool released;

        private void Awake() => rb = GetComponent<Rigidbody2D>();

        public void Launch(Vector2 position, Vector2 direction, float speed, float hitDamage,
                           int pierce, float knockbackForce, Action<Projectile> releaseToPool)
        {
            float angle = spinSpeed != 0f ? 0f : Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * speed;

            damage = hitDamage;
            pierceLeft = pierce;
            knockback = knockbackForce;
            lifeLeft = lifetime;
            release = releaseToPool;
            released = false;
        }

        private void Update()
        {
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f) Release();
        }

        private void FixedUpdate()
        {
            if (gravity > 0f) rb.linearVelocity += Vector2.down * (gravity * Time.fixedDeltaTime);
            if (spinSpeed != 0f) rb.MoveRotation(rb.rotation + spinSpeed * Time.fixedDeltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (released) return;
            if (!other.TryGetComponent(out Enemy enemy) || !enemy.IsAlive) return;

            enemy.TakeHit(damage, rb.linearVelocity.normalized * knockback);
            if (pierceLeft-- <= 0) Release();
        }

        private void Release()
        {
            if (released) return;
            released = true;
            if (release != null) release(this);
            else Destroy(gameObject);
        }
    }
}
