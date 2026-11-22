using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Kazanma ekranı için konfeti: ekranın iki alt köşesinden yukarı fışkıran renkli kâğıt parçaları.
    /// Oyun bitince Time.timeScale 0 olduğu için zamandan bağımsız (unscaled) çalışır.
    /// Parçalar baştan oluşturulan sabit bir havuzdan gelir; oynarken yeni nesne üretilmez.
    /// </summary>
    public class Confetti : MonoBehaviour
    {
        [SerializeField, Min(10)] private int maxPieces = 260;
        [SerializeField] private Vector2 pieceSize = new(14f, 24f);
        [Tooltip("Aşağı doğru çekim (UI pikseli / saniye²).")]
        [SerializeField, Min(0f)] private float gravity = 1100f;
        [SerializeField] private Vector2 launchSpeed = new(1100f, 1700f);
        [SerializeField] private Color[] colors =
        {
            new(1f, 0.82f, 0.2f), new(1f, 0.3f, 0.3f), new(0.3f, 0.85f, 1f),
            new(0.45f, 1f, 0.45f), new(1f, 0.45f, 0.9f), Color.white,
        };

        [Header("Dalgalar")]
        [SerializeField, Min(1)] private int firstBurst = 140;
        [SerializeField, Min(0)] private int laterBurst = 40;
        [Tooltip("İlk patlamadan sonra bu süre boyunca küçük dalgalar devam eder.")]
        [SerializeField, Min(0f)] private float burstDuration = 3f;
        [SerializeField, Min(0.1f)] private float burstInterval = 0.6f;

        private class Piece
        {
            public RectTransform rect;
            public Image image;
            public Vector2 velocity;
            public float spin;
            public float flip;
            public bool active;
        }

        private Piece[] pieces;
        private RectTransform area;
        private int next;
        private float playTime = -1f;
        private float nextBurst;

        private void Awake()
        {
            area = (RectTransform)transform;
            pieces = new Piece[maxPieces];
            for (int i = 0; i < maxPieces; i++)
            {
                var go = new GameObject("Piece", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = pieceSize;
                go.SetActive(false);
                pieces[i] = new Piece { rect = rect, image = image };
            }
        }

        /// <summary>Konfetiyi başlatır: büyük bir ilk patlama, ardından birkaç saniye küçük dalgalar.</summary>
        public void Play()
        {
            playTime = 0f;
            nextBurst = burstInterval;
            Burst(firstBurst);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (playTime >= 0f)
            {
                playTime += dt;
                if (playTime < burstDuration && playTime >= nextBurst)
                {
                    Burst(laterBurst);
                    nextBurst += burstInterval;
                }
            }

            float bottom = -area.rect.height * 0.5f - 60f;
            foreach (Piece p in pieces)
            {
                if (!p.active) continue;

                p.velocity.y -= gravity * dt;
                p.velocity.x *= 1f - 0.8f * dt; // hava direnci: yatay hız yavaşça azalır
                p.rect.anchoredPosition += p.velocity * dt;
                p.rect.Rotate(0f, 0f, p.spin * dt);
                // Kâğıt dönüyormuş gibi görünsün diye genişlik sürekli değişir.
                p.flip += dt * 8f;
                p.rect.localScale = new Vector3(Mathf.Cos(p.flip), 1f, 1f);

                if (p.velocity.y < 0f && p.rect.anchoredPosition.y < bottom)
                {
                    p.active = false;
                    p.rect.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>İki alt köşeden yukarı ve içeri doğru <paramref name="count"/> parça fırlatır.</summary>
        private void Burst(int count)
        {
            float halfW = area.rect.width * 0.5f;
            float halfH = area.rect.height * 0.5f;

            for (int i = 0; i < count; i++)
            {
                Piece p = pieces[next];
                next = (next + 1) % pieces.Length; // havuz doluysa en eski parça yeniden kullanılır

                bool left = i % 2 == 0;
                float angle = Random.Range(58f, 82f) * Mathf.Deg2Rad;
                float speed = Random.Range(launchSpeed.x, launchSpeed.y);
                var dir = new Vector2(Mathf.Cos(angle) * (left ? 1f : -1f), Mathf.Sin(angle));

                p.rect.anchoredPosition = new Vector2(left ? -halfW + 40f : halfW - 40f, -halfH);
                p.rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                p.velocity = dir * speed;
                p.spin = Random.Range(-540f, 540f);
                p.flip = Random.Range(0f, Mathf.PI * 2f);
                p.image.color = colors[Random.Range(0, colors.Length)];
                p.active = true;
                p.rect.gameObject.SetActive(true);
            }
        }
    }
}
