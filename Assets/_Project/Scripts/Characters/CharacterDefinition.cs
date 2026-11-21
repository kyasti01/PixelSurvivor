using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>A playable character: look, base stats and starting weapons.</summary>
    [CreateAssetMenu(menuName = "PixelSurvivor/Character", fileName = "Character")]
    public class CharacterDefinition : ScriptableObject
    {
        public string displayName = "Character";
        [TextArea(2, 5)] public string description;

        [Header("Animation frames")]
        public Sprite[] standFrames;
        public Sprite[] runFrames;
        public Sprite[] deadFrames;

        [Header("Stats")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0.5f)] public float moveSpeed = 3f;
        [Min(0.1f)] public float pickupRadius = 2.5f;

        [Header("Starting weapons")]
        public bool gun = true;
        [Min(1)] public int gunProjectiles = 1;
        public bool ring = true;
        [Min(1)] public int ringCount = 8;
        [Min(0.1f)] public float ringInterval = 1.4f;
        [Min(0)] public int orbitBlades;

        [Header("Unlock")]
        [Tooltip("Gold needed to unlock in the character select (0 = free).")]
        [Min(0)] public int unlockCost;
        [Tooltip("Unlocked by beating the final boss once (ignores Unlock Cost).")]
        public bool requiresVictory;
    }
}
