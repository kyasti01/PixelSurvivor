using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(Collider2D))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>Şu an oyunda olan bütün düşmanlar (hayatta olanlar ya da ölüm animasyonu oynayanlar).</summary>
        public static readonly List<Enemy> Active = new();
        /// <summary>Bir düşmanın canı sıfıra indiğinde bir kez tetiklenir.</summary>
        public static event Action<Enemy> Killed;

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;
        [Tooltip("Orijinal sprite çevrilmemişken sağa bakıyorsa işaretle.")]
        [SerializeField] private bool spriteFacesRight = true;
        [SerializeField, Min(0f)] private float deathDuration = 0.6f;
        [Tooltip("Geri itilme, takibi ne kadar süre bastırır.")]
        [SerializeField, Min(0f)] private float knockbackTime = 0.08f;
        [Tooltip("Bu kadar geride kalan düşmanlar oyuncunun önüne taşınır.")]
        [SerializeField, Min(5f)] private float recycleDistance = 22f;
        [SerializeField, Min(1f)] private float recycleSpawnDistance = 15f;
        [Tooltip("Her düşmana özel, oyuncuya giden düz çizgiden rastgele sapma (derece).")]
        [SerializeField, Range(0f, 45f)] private float maxApproachAngle = 15f;
        [SerializeField] private Color hitFlashColor = new(1f, 0.35f, 0.35f);
        [SerializeField, Min(0f)] private float hitFlashTime = 0.08f;
        [Tooltip("Bu süre içindeki vuruşlar tek bir hasar sayısında toplanır.")]
        [SerializeField, Min(0f)] private float damageNumberInterval = 0.3f;
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private AudioClip deathSfx;

        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private Rigidbody2D rb;
        private Collider2D col;
        private Health health;
        private Transform target;
        private EnemyStats stats;
        private Action<Enemy> release;
        private float knockbackTimer;
        private bool dying;
        private float flashTimer;
        private float deathTimer;
        private float approachAngle;
        private float moveSpeed;  // oyun süresine göre ölçeklenmiş değerler, Init'te ayarlanır
        private float contactDps;
        private float projectileDamage;
        private float attackTimer;
        private float pendingNumber;
        private float numberTimer;
        private Color baseColor = Color.white;

        public bool IsAlive => !dying;
        /// <summary>Daha güçlü, altın renkli ve daha iyi ödül veren versiyon (bkz. MakeElite).</summary>
        public bool IsElite { get; private set; }
        public EnemyStats Stats => stats;
        public Health Health => health;
        /// <summary>Oyuncuya değerken verilen saniyelik hasar (oyun süresine göre zaten ölçeklenmiş).</summary>
        public float ContactDps => dying ? 0f : contactDps;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            Killed = null;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        public void Init(EnemyStats newStats, Vector2 position, Transform chaseTarget, Action<Enemy> releaseToPool,
                         float healthMultiplier = 1f, float damageMultiplier = 1f, float speedMultiplier = 1f)
        {
            stats = newStats;
            moveSpeed = stats.moveSpeed * speedMultiplier;
            contactDps = stats.contactDps * damageMultiplier;
            projectileDamage = stats.projectileDamage * damageMultiplier;
            attackTimer = stats.attackInterval * UnityEngine.Random.Range(0.3f, 1f); // hepsi aynı anda ateş etmesin
            IsElite = false;
            target = chaseTarget;
            release = releaseToPool;

            transform.position = position;
            rb.position = position;
            rb.linearVelocity = Vector2.zero;

            dying = false;
            knockbackTimer = 0f;
            flashTimer = 0f;
            deathTimer = 0f;
            pendingNumber = 0f;
            numberTimer = 0f;
            approachAngle = UnityEngine.Random.Range(-maxApproachAngle, maxApproachAngle);
            col.enabled = true;
            body.sortingOrder = 0;
            baseColor = stats.tint;
            body.color = baseColor;
            transform.localScale = Vector3.one * stats.scale;
            body.transform.localScale = Vector3.one * (stats.visualScale > 0f ? stats.visualScale : 1f);
            body.transform.localPosition = stats.visualOffset;

            if (stats.animator != null) animator.runtimeAnimatorController = stats.animator;
            animator.SetBool(DeadHash, false);
            health.SetMax(stats.maxHealth * healthMultiplier);
        }

        /// <summary>Init'ten hemen sonra çağır: daha büyük, altın renkli, daha çok can (ödülleri DropManager halleder).</summary>
        public void MakeElite(float healthMultiplier, float scaleMultiplier, Color tint)
        {
            IsElite = true;
            health.SetMax(health.Max * healthMultiplier);
            transform.localScale *= scaleMultiplier;
            baseColor = tint;
            body.color = tint;
        }

        private void FixedUpdate()
        {
            if (dying || target == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector2 toTarget = (Vector2)target.position - rb.position;
            if (toTarget.sqrMagnitude > recycleDistance * recycleDistance)
            {
                // Çok geride kaldı: oyuncunun gittiği tarafta yeniden ortaya çık.
                Vector2 p = (Vector2)target.position + toTarget.normalized * recycleSpawnDistance;
                rb.position = p;
                transform.position = p;
                return;
            }

            // Menzilli düşmanlar yaklaşmak yerine rahat bir mesafede durup ateş eder.
            if (stats.IsRanged && toTarget.sqrMagnitude < Sq(stats.attackRange * 0.7f))
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Her düşman hafif kişisel bir açıyla yaklaşır; böylece kalabalık yayılır ve
            // düşmanlar engellerin tam ortasına yüklenmek yerine kenarından kayar.
            Vector2 dir = Quaternion.Euler(0f, 0f, approachAngle) * toTarget.normalized;
            rb.linearVelocity = dir * moveSpeed;
        }

        private static float Sq(float v) => v * v;

        private void TryShoot()
        {
            if (!stats.IsRanged || target == null) return;

            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            if (toTarget.sqrMagnitude > Sq(stats.attackRange)) return;

            attackTimer = stats.attackInterval;
            EnemyProjectiles.Fire(transform.position, toTarget, stats.projectileSpeed, projectileDamage);
        }

        private void Update()
        {
            if (dying)
            {
                // Ölüm animasyonu boyunca cesedi soldur.
                deathTimer += Time.deltaTime;
                Color c = baseColor;
                c.a *= 1f - Mathf.Clamp01(deathTimer / deathDuration);
                body.color = c;
                return;
            }

            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                body.color = flashTimer > 0f ? hitFlashColor : baseColor;
            }

            if (numberTimer > 0f)
            {
                numberTimer -= Time.deltaTime;
                if (numberTimer <= 0f) FlushDamageNumber();
            }

            TryShoot();
        }

        private void LateUpdate()
        {
            if (dying || target == null) return;
            bool targetIsLeft = target.position.x < transform.position.x;
            body.flipX = spriteFacesRight ? targetIsLeft : !targetIsLeft;
        }

        public void TakeHit(float damage, Vector2 knockback)
        {
            if (dying) return;

            // Hasar sayıları her düşman için toplanır (bkz. FlushDamageNumber); kalabalıkta ekran okunur kalsın diye.
            pendingNumber += damage;
            if (numberTimer <= 0f) FlushDamageNumber();
            health.TakeDamage(damage);
            if (dying) return; // bu vuruşla öldü

            flashTimer = hitFlashTime;
            body.color = hitFlashColor;
            animator.SetTrigger(HitHash);
            SfxPlayer.Play(hitSfx, 0.5f);
            knockback *= 1f - stats.knockbackResistance;
            if (knockback.sqrMagnitude > 0.0001f)
            {
                rb.linearVelocity = knockback;
                knockbackTimer = knockbackTime;
            }
        }

        /// <summary>Son sayıdan bu yana birikmiş hasarı tek sayı olarak gösterir, sonra kısa bir süre bekler.</summary>
        private void FlushDamageNumber()
        {
            if (pendingNumber <= 0f) return;
            DamageNumbers.Show(transform.position + Vector3.up * (0.6f * transform.localScale.y), pendingNumber);
            pendingNumber = 0f;
            numberTimer = damageNumberInterval;
        }

        private void OnDied()
        {
            FlushDamageNumber(); // öldüren vuruşu her zaman göster
            dying = true;
            col.enabled = false;
            rb.linearVelocity = Vector2.zero;
            body.sortingOrder = -1; // cesetler yaşayan düşmanların altında çizilir
            animator.SetBool(DeadHash, true);
            SfxPlayer.Play(deathSfx, 0.6f);
            Killed?.Invoke(this);
            StartCoroutine(ReleaseAfterDeath());
        }

        private IEnumerator ReleaseAfterDeath()
        {
            yield return new WaitForSeconds(deathDuration);
            if (release != null) release(this);
            else Destroy(gameObject);
        }
    }
}
