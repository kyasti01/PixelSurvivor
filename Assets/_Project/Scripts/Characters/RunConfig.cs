using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Choices made in the main menu that the game scene reads when a run starts.</summary>
    public static class RunConfig
    {
        /// <summary>Null when the game scene is played directly; the player then uses its default.</summary>
        public static CharacterDefinition SelectedCharacter { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => SelectedCharacter = null;
    }
}
