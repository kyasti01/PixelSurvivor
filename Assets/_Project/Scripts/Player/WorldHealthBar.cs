using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Small sprite health bar that follows a character (fill shrinks towards the left).</summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private SpriteRenderer fill;

        private Vector3 fullScale;
        private float fullWidth;
        private float lastValue = -1f;

        private void Awake()
        {
            fullScale = fill.transform.localScale;
            fullWidth = fill.sprite.bounds.size.x * fullScale.x;
        }

        private void LateUpdate()
        {
            float t = health.Normalized;
            if (Mathf.Approximately(t, lastValue)) return;
            lastValue = t;

            fill.transform.localScale = new Vector3(fullScale.x * t, fullScale.y, fullScale.z);
            fill.transform.localPosition = new Vector3(-(1f - t) * fullWidth * 0.5f, 0f, 0f);
        }
    }
}
