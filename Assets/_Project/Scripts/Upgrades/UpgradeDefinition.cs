using UnityEngine;

namespace PixelSurvivor
{
    public enum UpgradeKind
    {
        GunDamage,        // value = fraction, e.g. 0.25 = +25 %
        GunFireRate,      // value = fraction shorter interval
        GunProjectiles,   // value = extra projectiles
        GunPierce,        // value = extra enemies pierced
        OrbitCount,       // value = extra blades
        OrbitPower,       // value = fraction more speed and damage
        MoveSpeed,        // value = fraction
        MaxHealth,        // value = flat HP (also heals that much)
        PickupRadius,     // value = fraction
        Heal,             // value = flat HP
        // Values are stored as numbers in the upgrade assets: only ever append new kinds here.
        RingCount,        // value = extra projectiles per ring
        RingFireRate,     // value = fraction shorter interval
        RingDamage,       // value = fraction
        AxeCount,         // value = extra axes per throw (first one unlocks the weapon)
        AxePower,         // value = fraction more damage
        BombCount,        // value = extra bombs per volley (first one unlocks the weapon)
        BombRadius,       // value = fraction larger blast
        EvolveGun,        // evolutions: big one-time boosts, see UpgradeManager.Apply
        EvolveRing,
        EvolveOrbit,
    }

    /// <summary>One choice that can appear on the level-up screen.</summary>
    [CreateAssetMenu(menuName = "PixelSurvivor/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string displayName = "Upgrade";
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public UpgradeKind kind;
        public float value = 0.1f;
        [Tooltip("0 = can be taken forever.")]
        [Min(0)] public int maxLevel = 5;
        [Tooltip("Only offered after this upgrade has been taken at least once.")]
        public UpgradeDefinition requires;
        [Tooltip("Only offered once ALL of these are at their max level (weapon evolutions).")]
        public UpgradeDefinition[] requiresMaxed = System.Array.Empty<UpgradeDefinition>();
        [Tooltip("Shown with an EVOLUTION badge on the level-up screen.")]
        public bool isEvolution;
        [Tooltip("Relative chance of being offered.")]
        [Min(0f)] public float weight = 1f;
    }
}
