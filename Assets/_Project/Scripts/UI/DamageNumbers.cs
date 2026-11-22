using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Uçuşan hasar sayıları. Overlay canvas'ta sabit boyutlu bir UI metin havuzu kullanır; bütün metinler
    /// meşgulse en eskisi yeniden kullanılır, böylece yoğun savaşlarda hiç yeni nesne oluşturulmaz.
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField, Min(8)] private int poolSize = 50;
        [SerializeField, Min(8)] private int fontSize = 32;
        [SerializeField, Min(0.1f)] private float lifetime = 0.6f;
        // Sayının ömrü boyunca yukarı süzüldüğü mesafe (dünya birimi).
        [SerializeField] private float rise = 0.8f;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color bigColor = new(1f, 0.8f, 0.25f);
        // Bu değer ve üstündeki sayılar altın renkli. Sayılar her düşman için ~0.3 sn'lik toplamlardır.
        [SerializeField, Min(1f)] private float bigHit = 80f;

        private class Entry
        {
            public Text text;
            public RectTransform rect;
            public Vector3 world;
            public Color color;
            public float age;
            public bool active;
        }

        private static DamageNumbers instance;
        private static string[] numberStrings;

        private Entry[] entries;
        private int next;
        private Camera cam;
        private Canvas canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            instance = this;
            canvas = GetComponentInParent<Canvas>();
            entries = new Entry[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var go = new GameObject("Number", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var text = go.AddComponent<Text>();
                text.font = font;
                text.fontSize = fontSize;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.raycastTarget = false;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(2f, -2f);

                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = Vector2.zero;
                rect.sizeDelta = new Vector2(120f, 40f);
                go.SetActive(false);
                entries[i] = new Entry { text = text, rect = rect };
            }
        }

        public static void Show(Vector3 worldPosition, float amount)
        {
            if (instance == null || amount < 0.5f) return;
            instance.Spawn(worldPosition, amount);
        }

        private void Spawn(Vector3 world, float amount)
        {
            Entry e = entries[next];
            next = (next + 1) % entries.Length;

            int value = Mathf.RoundToInt(amount);
            e.text.text = Number(value);
            e.color = amount >= bigHit ? bigColor : normalColor;
            e.world = world + (Vector3)(Random.insideUnitCircle * 0.2f);
            e.age = 0f;
            e.active = true;
            e.text.gameObject.SetActive(true);
        }

        private static string Number(int value)
        {
            numberStrings ??= new string[1000];
            if (value < 0 || value >= numberStrings.Length) return value.ToString();
            return numberStrings[value] ??= value.ToString();
        }

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            foreach (Entry e in entries)
            {
                if (!e.active) continue;

                e.age += Time.deltaTime;
                float t = e.age / lifetime;
                if (t >= 1f)
                {
                    e.active = false;
                    e.text.gameObject.SetActive(false);
                    continue;
                }

                Vector3 screen = cam.WorldToScreenPoint(e.world + Vector3.up * (rise * t));
                e.rect.anchoredPosition = screen / scale;
                Color c = e.color;
                c.a = 1f - t * t;
                e.text.color = c;
            }
        }
    }
}
