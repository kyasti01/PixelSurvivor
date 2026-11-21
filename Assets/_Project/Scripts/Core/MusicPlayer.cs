using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Müzik ses ayarına uyan ve parça değiştirebilen arka plan müziği.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float baseVolume = 0.35f;

        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            ApplyVolume();
            GameSettings.Changed += ApplyVolume;
        }

        private void OnDestroy() => GameSettings.Changed -= ApplyVolume;

        private void ApplyVolume() => source.volume = baseVolume * GameSettings.MusicVolume;

        public void Play(AudioClip clip)
        {
            if (clip == null || source.clip == clip) return;
            source.clip = clip;
            source.loop = true;
            source.Play();
        }
    }
}
