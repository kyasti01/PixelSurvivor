using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>Hedefi (oyuncuyu) takip eder ve kısa ekran sarsıntılarını destekler.</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("0 = hedefe kilitli. Daha yüksek değerler hafif bir gecikme ekler.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.05f;

        private static CameraFollow instance;

        private Vector3 basePosition;
        private Vector3 velocity;
        private float shakeStrength;
        private float shakeTime;
        private float shakeDuration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            instance = this;
            basePosition = transform.position;
        }

        /// <summary>Kamerayı sarsar; daha güçlü bir sarsıntı, devam eden zayıf olanın yerini alır.</summary>
        public static void Shake(float strength, float duration)
        {
            if (instance == null) return;
            float remaining = instance.shakeDuration > 0f ? instance.shakeStrength * instance.shakeTime / instance.shakeDuration : 0f;
            if (strength < remaining) return;
            instance.shakeStrength = strength;
            instance.shakeDuration = instance.shakeTime = duration;
        }

        private void LateUpdate()
        {
            if (target != null)
            {
                Vector3 goal = new(target.position.x, target.position.y, basePosition.z);
                basePosition = smoothTime <= 0f ? goal : Vector3.SmoothDamp(basePosition, goal, ref velocity, smoothTime);
            }

            Vector3 offset = Vector3.zero;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                offset = (Vector3)(Random.insideUnitCircle * shakeStrength * Mathf.Max(0f, shakeTime / shakeDuration));
            }
            transform.position = basePosition + offset;
        }
    }
}
