using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(Collider2D))]
    public class Enemy : MonoBehaviour
    {
        /// <summary>Every enemy currently in play (alive or playing its death animation).</summary>
        public static readonly List<Enemy> Active = new();
        /// <summary>Raised once when an enemy's health reaches zero.</summary>
        public static event Action<Enemy> Killed;

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Animator animator;
        [Tooltip("Tick if the source sprite looks to the right when not flipped.")]
        [SerializeField] private bool spriteFacesRight = true;
        [SerializeField, Min(0f)] private float deathDuration = 0.6f;
        [Tooltip("How long a knockback overrides chasing.")]
        [SerializeField, Min(0f)] private float knockbackTime = 0.08f;
        [Tooltip("Enemies left this far behind are moved back in front of the player.")]
        [SerializeField, Min(5f)] private float recycleDistance = 22f;
        [SerializeField, Min(1f)] private float recycleSpawnDistance = 15f;
        [Tooltip("Random per-enemy offset (degrees) from the straight line to the player.")]
        [SerializeField, Range(0f, 45f)] private float maxApproachAngle = 15f;
        [SerializeField] private Color hitFlashColor = new(1f, 0.35f, 0.35f);
        [SerializeField, Min(0f)] private float hitFlashTime = 0.08f;
        [Tooltip("Hits within this window are summed into one damage number.")]
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
        private float moveSpeed;  // stats scaled by run time, set in Init
        private float contactDps;
        private float projectileDamage;
        private float attackTimer;
        private float pendingNumber;
        private float numberTimer;
        private Color baseColor = Color.white;

        public bool IsAlive => !dying;
        /// <summary>Tougher, golden variant with better drops (see MakeElite).</summary>
        public bool IsElite { get; private set; }
        public EnemyStats Stats => stats;
        public Health Health => health;
        /// <summary>Damage per second dealt while touching the player (already scaled by run time).</summary>
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
            attackTimer = stats.attackInterval * UnityEngine.Random.Range(0.3f, 1f); // don't all fire at once
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

        /// <summary>Call right after Init: bigger, golden, more health (drops are handled by DropManager).</summary>
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
                // Left far behind: reappear on the side the player is heading towards.
                Vector2 p = (Vector2)target.position + toTarget.normalized * recycleSpawnDistance;
                rb.position = p;
                transform.position = p;
                return;
            }

            // Ranged enemies stop at a comfortable distance and shoot instead of closing in.
            if (stats.IsRanged && toTarget.sqrMagnitude < Sq(stats.attackRange * 0.7f))
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            // Each enemy approaches at a slight personal angle, so the crowd spreads out and
            // enemies slide past obstacles instead of pushing into their exact centre.
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
                // Fade the corpse out over the death animation.
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

            // Damage numbers are batched per enemy (see FlushDamageNumber) so crowds stay readable.
            pendingNumber += damage;
            if (numberTimer <= 0f) FlushDamageNumber();
            health.TakeDamage(damage);
            if (dying) return; // killed by this hit

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

        /// <summary>Shows the damage gathered since the last number as one number, then waits a moment.</summary>
        private void FlushDamageNumber()
        {
            if (pendingNumber <= 0f) return;
            DamageNumbers.Show(transform.position + Vector3.up * (0.6f * transform.localScale.y), pendingNumber);
            pendingNumber = 0f;
            numberTimer = damageNumberInterval;
        }

        private void OnDied()
        {
            FlushDamageNumber(); // always show the killing blow
            dying = true;
            col.enabled = false;
            rb.linearVelocity = Vector2.zero;
            body.sortingOrder = -1; // corpses render under living enemies
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
