using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Kısa ses efektleri için tek bir ortak AudioSource. Aynı ses kısa aralıklarla sınırlanır;
    /// böylece aynı karede ölen yüz düşman hoparlörü patlatmaz.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minRepeatInterval = 0.05f;

        private static SfxPlayer instance;
        private AudioSource source;
        private readonly Dictionary<AudioClip, float> lastPlayed = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public static void Play(AudioClip clip, float volume = 1f)
        {
            if (instance == null || clip == null) return;

            float now = Time.unscaledTime;
            if (instance.lastPlayed.TryGetValue(clip, out float last) && now - last < instance.minRepeatInterval)
                return;

            instance.lastPlayed[clip] = now;
            instance.source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }
    }
}
