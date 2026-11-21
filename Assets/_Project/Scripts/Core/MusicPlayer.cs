using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Background music that follows the music volume setting and can switch tracks.</summary>
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
