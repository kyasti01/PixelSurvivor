using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Tek seferlik sprite animasyonlarını (patlama vb.) bir renderer havuzundan oynatır.</summary>
    public class EffectPlayer : MonoBehaviour
    {
        private class Effect
        {
            public SpriteRenderer renderer;
            public Sprite[] frames;
            public float fps;
            public float time;
        }

        private static EffectPlayer instance;

        private readonly List<Effect> playing = new();
        private readonly Stack<Effect> free = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake() => instance = this;

        /// <param name="scale">Efekt nesnesinin dünya ölçeği.</param>
        /// <param name="rotation">Z ekseni etrafında derece.</param>
        public static void Play(Sprite[] frames, float fps, Vector2 position, float scale, int sortingOrder = 30,
                                float rotation = 0f)
        {
            if (instance == null || frames == null || frames.Length == 0) return;

            Effect fx = instance.free.Count > 0 ? instance.free.Pop() : instance.Create();
            fx.frames = frames;
            fx.fps = Mathf.Max(1f, fps);
            fx.time = 0f;
            fx.renderer.sprite = frames[0];
            fx.renderer.sortingOrder = sortingOrder;
            fx.renderer.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, rotation));
            fx.renderer.transform.localScale = Vector3.one * scale;
            fx.renderer.gameObject.SetActive(true);
            instance.playing.Add(fx);
        }

        private Effect Create()
        {
            var go = new GameObject("Effect");
            go.transform.SetParent(transform, false);
            return new Effect { renderer = go.AddComponent<SpriteRenderer>() };
        }

        private void Update()
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                Effect fx = playing[i];
                fx.time += Time.deltaTime;
                int frame = (int)(fx.time * fx.fps);
                if (frame < fx.frames.Length)
                {
                    fx.renderer.sprite = fx.frames[frame];
                    continue;
                }

                fx.renderer.gameObject.SetActive(false);
                playing.RemoveAt(i);
                free.Push(fx);
            }
        }
    }
}
