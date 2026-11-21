using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelSurvivor
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(PlayerExperience))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 3f;

        [Header("Dash (Shift / Space)")]
        [SerializeField, Min(1f)] private float dashSpeed = 13f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0.1f)] private float dashCooldown = 1.2f;
        [SerializeField] private AudioClip dashSfx;
        [SerializeField] private Sprite[] dashEffectFrames;
        [SerializeField, Min(0.05f)] private float dashEffectScale = 0.7f;
        [Tooltip("Added to the dash angle so the effect sprite points the right way (180 for art drawn moving left).")]
        [SerializeField] private float dashEffectAngleOffset = 180f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteFrameAnimator frameAnimator;
        [Tooltip("Tick if the source sprite looks to the right when not flipped.")]
        [SerializeField] private bool spriteFacesRight = true;
        [SerializeField] private Color damageFlashColor = new(1f, 0.45f, 0.45f);
        [SerializeField, Min(0f)] private float damageFlashTime = 0.1f;

        [Header("Contact damage")]
        [Tooltip("Enemies whose collider overlaps this circle hurt the player.")]
        [SerializeField, Min(0.1f)] private float hurtRadius = 0.5f;
        [SerializeField] private Vector2 hurtOffset = new(0f, -0.2f);
        [Tooltip("Only the strongest N touching enemies deal damage, so being surrounded is dangerous but not instant death.")]
        [SerializeField, Min(1)] private int maxContactAttackers = 4;

        [Header("Feedback")]
        [SerializeField] private AudioClip hurtSfx;
        [SerializeField] private AudioClip deathSfx;
        [Tooltip("Minimum seconds between hurt sounds/shakes while taking continuous damage.")]
        [SerializeField, Min(0.05f)] private float hurtFeedbackInterval = 0.35f;
        [SerializeField, Min(0f)] private float hurtShake = 0.12f;

        /// <summary>Multiplies all weapon damage (permanent upgrades).</summary>
        public float DamageMultiplier { get; set; } = 1f;

        /// <summary>Damage multiplier of the current player, or 1 if there is none.</summary>
        public static float CurrentDamageMultiplier => Instance != null ? Instance.DamageMultiplier : 1f;

        public static PlayerController Instance { get; private set; }

        public Health Health { get; private set; }
        public PlayerExperience Experience { get; private set; }
        public Vector2 MoveInput { get; private set; }
        /// <summary>Last non-zero movement direction (normalized).</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.right;
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
        public bool IsDashing => dashTimer > 0f;
        /// <summary>0 right after a dash, 1 when the dash is ready again.</summary>
        public float DashReadiness => 1f - Mathf.Clamp01(dashCooldownTimer / dashCooldown);

        private readonly List<Collider2D> touching = new();
        private readonly List<float> contactDps = new();
        private ContactFilter2D enemyFilter;
        private Rigidbody2D rb;
        private InputAction moveAction;
        private InputAction dashAction;
        private float flashTimer;
        private float nextHurtFeedback;
        private float dashTimer;
        private float dashCooldownTimer;
        private Vector2 dashDirection;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            Instance = this;
            rb = GetComponent<Rigidbody2D>();
            Health = GetComponent<Health>();
            Experience = GetComponent<PlayerExperience>();
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;

            // Project-wide actions (Assets/Settings/InputSystem_Actions): WASD, arrows, gamepad stick;
            // "Sprint" is Left Shift / left stick press, used here for the dash.
            if (InputSystem.actions != null)
            {
                moveAction = InputSystem.actions.FindAction("Player/Move");
                dashAction = InputSystem.actions.FindAction("Player/Sprint");
            }
            moveAction?.Enable();
            dashAction?.Enable();

            enemyFilter = new ContactFilter2D { useTriggers = false };
            enemyFilter.SetLayerMask(LayerMask.GetMask(GameLayers.Enemy));
        }

        private void OnDisable()
        {
            if (IsDashing) EndDash();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Health.IsDead)
            {
                MoveInput = Vector2.zero;
                return;
            }

            MoveInput = Vector2.ClampMagnitude(ReadMove(), 1f);
            if (MoveInput.sqrMagnitude > 0.01f) FacingDirection = MoveInput.normalized;

            UpdateDash();

            Vector2 look = IsDashing ? dashDirection : MoveInput;
            if (Mathf.Abs(look.x) > 0.01f)
                body.flipX = spriteFacesRight ? look.x < 0f : look.x > 0f;

            frameAnimator.Play(MoveInput.sqrMagnitude > 0.01f || IsDashing ? "Run" : "Stand");

            if (flashTimer > 0f) flashTimer -= Time.deltaTime;
            Color c = flashTimer > 0f ? damageFlashColor : Color.white;
            if (IsDashing) c.a = 0.55f;
            body.color = c;
        }

        private void FixedUpdate()
        {
            rb.linearVelocity = IsDashing ? dashDirection * dashSpeed : MoveInput * moveSpeed;
            ApplyContactDamage();
        }

        // ---------------------------------------------------------------- dash

        private void UpdateDash()
        {
            if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

            if (IsDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f) EndDash();
                return;
            }

            if (dashCooldownTimer <= 0f && DashPressed()) StartDash();
        }

        private bool DashPressed()
        {
            if (Time.timeScale <= 0f) return false;
            if (dashAction != null && dashAction.WasPressedThisFrame()) return true;
            var kb = Keyboard.current;
            return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame);
        }

        /// <summary>Short burst of speed: no damage taken and enemies are passed through.</summary>
        private void StartDash()
        {
            dashDirection = MoveInput.sqrMagnitude > 0.01f ? MoveInput.normalized : FacingDirection;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            Health.Invulnerable = true;
            GameLayers.SetPlayerEnemyCollision(false);

            SfxPlayer.Play(dashSfx, 0.6f);
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg + dashEffectAngleOffset;
            EffectPlayer.Play(dashEffectFrames, 30f, rb.position, dashEffectScale, 25, angle);
        }

        private void EndDash()
        {
            dashTimer = 0f;
            Health.Invulnerable = false;
            GameLayers.SetPlayerEnemyCollision(true);
        }

        // ---------------------------------------------------------------- damage

        /// <summary>
        /// Every enemy overlapping the hurt circle deals its damage-per-second. Done with an overlap
        /// query rather than collision callbacks, which stop firing reliably once a crowd presses in.
        /// </summary>
        private void ApplyContactDamage()
        {
            if (Health.IsDead || Health.Invulnerable) return;

            Vector2 center = rb.position + hurtOffset;
            if (Physics2D.OverlapCircle(center, hurtRadius, enemyFilter, touching) == 0) return;

            contactDps.Clear();
            foreach (Collider2D c in touching)
                if (c.TryGetComponent(out Enemy enemy) && enemy.ContactDps > 0f) contactDps.Add(enemy.ContactDps);
            if (contactDps.Count == 0) return;

            contactDps.Sort((a, b) => b.CompareTo(a)); // strongest first
            float dps = 0f;
            for (int i = 0; i < contactDps.Count && i < maxContactAttackers; i++) dps += contactDps[i];

            Health.TakeDamage(dps * Time.fixedDeltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + (Vector3)hurtOffset, hurtRadius);
        }

        private Vector2 ReadMove()
        {
            if (moveAction != null) return moveAction.ReadValue<Vector2>();

            // Fallback if the project-wide action asset is missing.
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        private void OnDamaged(float _)
        {
            flashTimer = damageFlashTime;
            if (Time.time < nextHurtFeedback) return;
            nextHurtFeedback = Time.time + hurtFeedbackInterval;
            SfxPlayer.Play(hurtSfx, 0.6f);
            CameraFollow.Shake(hurtShake, 0.15f);
        }

        private void OnDied()
        {
            if (IsDashing) EndDash();
            SfxPlayer.Play(deathSfx);
            CameraFollow.Shake(0.3f, 0.4f);
            rb.linearVelocity = Vector2.zero;
            body.color = Color.white;
            frameAnimator.Play("Dead");
        }
    }
}
