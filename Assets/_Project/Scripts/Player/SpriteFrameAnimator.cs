using System;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Minimal frame-by-frame sprite animator. Lighter than an Animator Controller
    /// for characters that only need a handful of looping clips.
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames;
            [Min(0.1f)] public float fps = 10f;
            public bool loop = true;
        }

        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Clip[] clips = Array.Empty<Clip>();

        private Clip current;
        private float time;

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (clips.Length > 0) Play(clips[0].name);
        }

        /// <summary>Replace the frames of an existing clip (e.g. to swap in another character's sprites).</summary>
        public void SetFrames(string clipName, Sprite[] frames)
        {
            Clip clip = Array.Find(clips, c => c.name == clipName);
            if (clip == null || frames == null || frames.Length == 0) return;
            clip.frames = frames;
            if (clip == current) Apply();
        }

        public void Play(string clipName, bool restart = false)
        {
            if (!restart && current != null && current.name == clipName) return;

            Clip next = Array.Find(clips, c => c.name == clipName);
            if (next == null || next.frames == null || next.frames.Length == 0)
            {
                Debug.LogWarning($"[SpriteFrameAnimator] Clip '{clipName}' not found on {name}.", this);
                return;
            }

            current = next;
            time = 0f;
            Apply();
        }

        private void Update()
        {
            if (current == null) return;
            time += Time.deltaTime;
            Apply();
        }

        private void Apply()
        {
            int frame = (int)(time * current.fps);
            int count = current.frames.Length;
            frame = current.loop ? frame % count : Mathf.Min(frame, count - 1);
            target.sprite = current.frames[frame];
        }
    }
}
