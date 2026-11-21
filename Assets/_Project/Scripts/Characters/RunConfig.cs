using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Ana menüde yapılan seçimler; oyun sahnesi oyun başlarken bunları okur.</summary>
    public static class RunConfig
    {
        /// <summary>Oyun sahnesi doğrudan açılırsa null olur; o zaman oyuncu varsayılan karakteri kullanır.</summary>
        public static CharacterDefinition SelectedCharacter { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => SelectedCharacter = null;
    }
}
