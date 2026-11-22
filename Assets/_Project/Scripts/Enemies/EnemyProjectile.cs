using System;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Menzilli bir düşmanın attığı mermi; değerse oyuncuya hasar verir.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float fps = 10f;
        [SerializeField, Min(0.5f)] private float lifetime = 4f;
        // Sprite uçuş yönünü göstersin diye eklenen dönüş (yukarı bakacak çizilmiş görseller için -90).
        [SerializeField] private float spriteAngleOffset = -90f;

        private Rigidbody2D rb;
        private Action<EnemyProjectile> release;
        private float damage;
        private float age;
        private bool released;

        private void Awake() => rb = GetComponent<Rigidbody2D>();

        public void Launch(Vector2 position, Vector2 direction, float speed, float hitDamage, Action<EnemyProjectile> releaseToPool)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            rb.position = position;
            rb.rotation = angle;
            rb.linearVelocity = direction.normalized * speed;
            damage = hitDamage;
            release = releaseToPool;
            age = 0f;
            released = false;
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Release();
                return;
            }
            if (frames != null && frames.Length > 0)
                spriteRenderer.sprite = frames[(int)(age * fps) % frames.Length];
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (released || other.attachedRigidbody == null) return;
            if (!other.attachedRigidbody.TryGetComponent(out PlayerController player)) return;

            player.Health.TakeDamage(damage); // atılma sırasında yok sayılır
            Release();
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
