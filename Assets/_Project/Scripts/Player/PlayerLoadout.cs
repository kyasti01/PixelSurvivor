using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Applies the selected <see cref="CharacterDefinition"/> to the player before anything else runs:
    /// sprites, stats and which weapons are active.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerLoadout : MonoBehaviour
    {
        [Tooltip("Used when the game scene is started directly instead of from the menu.")]
        [SerializeField] private CharacterDefinition defaultCharacter;

        public CharacterDefinition Character { get; private set; }

        private void Awake()
        {
            Character = RunConfig.SelectedCharacter != null ? RunConfig.SelectedCharacter : defaultCharacter;
            if (Character != null) ApplyCharacter(Character);
            ApplyPermanentUpgrades();
        }

        /// <summary>Bonuses bought in the main-menu shop, on top of the character's stats.</summary>
        private void ApplyPermanentUpgrades()
        {
            var health = GetComponent<Health>();
            health.SetMax(health.Max + MetaProgression.HealthBonus);

            var controller = GetComponent<PlayerController>();
            controller.MoveSpeed *= MetaProgression.SpeedMultiplier;
            controller.DamageMultiplier = MetaProgression.DamageMultiplier;

            GetComponent<PlayerExperience>().PickupRadius *= MetaProgression.PickupMultiplier;
        }

        private void ApplyCharacter(CharacterDefinition c)
        {

            GetComponent<Health>().SetMax(c.maxHealth);
            GetComponent<PlayerController>().MoveSpeed = c.moveSpeed;
            GetComponent<PlayerExperience>().PickupRadius = c.pickupRadius;

            var frames = GetComponentInChildren<SpriteFrameAnimator>();
            frames.SetFrames("Stand", c.standFrames);
            frames.SetFrames("Run", c.runFrames);
            frames.SetFrames("Dead", c.deadFrames);

            var gun = GetComponentInChildren<AutoGun>(true);
            gun.enabled = c.gun;
            gun.ProjectileCount = c.gunProjectiles;

            var ring = GetComponentInChildren<RingShooter>(true);
            ring.enabled = c.ring;
            ring.Count = c.ringCount;
            ring.FireInterval = c.ringInterval;

            GetComponentInChildren<OrbitWeapon>(true).Count = c.orbitBlades;
        }
    }
}
