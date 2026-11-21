using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace PixelSurvivor
{
    /// <summary>
    /// Sonsuz zemine dekor serper (mezar taşı, kaya, fıçı...). Dik duran dekorlar katı engeldir;
    /// yerde yatanların (kafatası, pentagram) üzerinden geçilebilir. Dünya parçalara (chunk) bölünür;
    /// her parçanın yerleşimi bir seed'den gelir, yani geri dönünce aynı dekorlar görünür. Kameradan
    /// uzaktaki parçalar geri dönüştürülür.
    /// </summary>
    public class DecorScatter : MonoBehaviour
    {
        [Serializable]
        public class Item
        {
            public Sprite sprite;
            [Min(0f)] public float weight = 1f;
            [Tooltip("Yerde yatar (karakterlerin altında çizilir, üzerinden geçilir), örn. pentagram.")]
            public bool flat;
        }

        [Header("Obstacles")]
        [Tooltip("Collider yarıçapı, sprite'ın kısa kenarının bu oranı kadar. Yerde yatmayan dekorlar hareketi engeller.")]
        [SerializeField, Range(0.1f, 0.6f)] private float colliderSize = 0.38f;
        [Tooltip("Başlangıç noktasına bu mesafeden yakın katı dekor konmaz; oyuncu sıkışmış başlamasın diye.")]
        [SerializeField, Min(0f)] private float clearRadiusAtStart = 3f;

        [SerializeField] private Item[] items;
        [SerializeField, Min(2f)] private float chunkSize = 8f;
        [SerializeField, Min(0)] private int minPerChunk;
        [SerializeField, Min(0)] private int maxPerChunk = 3;
        [Tooltip("Kameranın etrafında her yönde tutulan parça (chunk) sayısı.")]
        [SerializeField, Min(1)] private int viewChunks = 2;
        [Tooltip("Sprite'lar bu pixels-per-unit değerine göre ölçeklenir; farklı paketlerin çizimleri aynı boyutta görünsün diye.")]
        [SerializeField, Min(1f)] private float referencePpu = 18f;
        [SerializeField] private int seed = 1337;

        private readonly Dictionary<Vector2Int, List<SpriteRenderer>> chunks = new();
        private readonly Stack<SpriteRenderer> pool = new();
        private readonly Dictionary<SpriteRenderer, CircleCollider2D> colliders = new();
        private readonly List<Vector2Int> toRemove = new();
        private Camera cam;
        private Vector2Int lastCenter = new(int.MinValue, int.MinValue);
        private float totalWeight;

        private void Awake()
        {
            cam = Camera.main;
            foreach (Item item in items) totalWeight += item.weight;
        }

        private void LateUpdate()
        {
            if (cam == null || items.Length == 0 || totalWeight <= 0f) return;

            Vector3 p = cam.transform.position;
            var center = new Vector2Int(Mathf.FloorToInt(p.x / chunkSize), Mathf.FloorToInt(p.y / chunkSize));
            if (center == lastCenter) return;
            lastCenter = center;

            for (int x = -viewChunks; x <= viewChunks; x++)
            for (int y = -viewChunks; y <= viewChunks; y++)
            {
                var c = new Vector2Int(center.x + x, center.y + y);
                if (!chunks.ContainsKey(c)) chunks[c] = Populate(c);
            }

            toRemove.Clear();
            foreach (var kv in chunks)
            {
                Vector2Int d = kv.Key - center;
                if (Mathf.Abs(d.x) > viewChunks + 1 || Mathf.Abs(d.y) > viewChunks + 1) toRemove.Add(kv.Key);
            }
            foreach (Vector2Int c in toRemove)
            {
                foreach (SpriteRenderer sr in chunks[c])
                {
                    sr.gameObject.SetActive(false);
                    pool.Push(sr);
                }
                chunks.Remove(c);
            }
        }

        private List<SpriteRenderer> Populate(Vector2Int chunk)
        {
            var list = new List<SpriteRenderer>();
            var rng = new Random(Hash(chunk));
            int count = rng.Next(minPerChunk, maxPerChunk + 1);
            for (int i = 0; i < count; i++)
            {
                Item item = Pick(rng.NextDouble() * totalWeight);
                var pos = new Vector2(
                    (chunk.x + (float)rng.NextDouble()) * chunkSize,
                    (chunk.y + (float)rng.NextDouble()) * chunkSize);
                if (!item.flat && pos.sqrMagnitude < clearRadiusAtStart * clearRadiusAtStart) continue;

                SpriteRenderer sr = pool.Count > 0 ? pool.Pop() : Create();
                sr.sprite = item.sprite;
                sr.sortingOrder = item.flat ? -50 : 0;
                sr.transform.position = pos;
                sr.transform.localScale = Vector3.one * (item.sprite.pixelsPerUnit / referencePpu);

                // Tabana yakın yuvarlak collider: karakterler takılmak yerine kenarından kayar.
                CircleCollider2D col = colliders[sr];
                col.enabled = !item.flat;
                Vector2 size = item.sprite.bounds.size;
                col.radius = Mathf.Min(size.x, size.y) * colliderSize;
                col.offset = new Vector2(0f, -size.y * 0.15f);

                sr.gameObject.SetActive(true);
                list.Add(sr);
            }
            return list;
        }

        private Item Pick(double roll)
        {
            foreach (Item item in items)
            {
                roll -= item.weight;
                if (roll <= 0) return item;
            }
            return items[^1];
        }

        private SpriteRenderer Create()
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            colliders[sr] = go.AddComponent<CircleCollider2D>(); // statik: Rigidbody2D yok
            return sr;
        }

        private int Hash(Vector2Int c) => unchecked(seed * 73856093 ^ c.x * 19349663 ^ c.y * 83492791);
    }
}
