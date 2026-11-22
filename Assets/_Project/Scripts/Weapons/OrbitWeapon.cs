using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Oyuncunun etrafında dönen kürekler; içinden geçtikleri her düşmana vururlar.
    /// Sıfır kürekle başlar; yükseltmeler kürek ekler.
    /// </summary>
    public class OrbitWeapon : MonoBehaviour
    {
        [SerializeField] private OrbitBlade bladePrefab;
        [SerializeField] private AudioClip hitSfx;

        [Header("Stats")]
        [SerializeField, Min(0)] private int count;
        [SerializeField, Min(0.3f)] private float radius = 1.6f;
        [SerializeField] private float degreesPerSecond = 200f;
        [SerializeField, Min(0f)] private float damage = 15f;
        [SerializeField, Min(0f)] private float knockback = 2f;
        // Sprite dışarı baksın diye eklenen dönüş (yukarı bakacak çizilmiş sprite'lar için -90).
        [SerializeField] private float spriteAngleOffset = -90f;

        private readonly List<OrbitBlade> blades = new();
        private float angle;

        public int Count
        {
            get => count;
            set
            {
                count = Mathf.Max(0, value);
                SyncBlades();
            }
        }

        public float DegreesPerSecond { get => degreesPerSecond; set => degreesPerSecond = value; }
        public float Radius { get => radius; set => radius = Mathf.Max(0.3f, value); }
        public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }

        private void Start() => SyncBlades();

        private void SyncBlades()
        {
            while (blades.Count < count)
            {
                OrbitBlade blade = Instantiate(bladePrefab, transform);
                blade.Init(this);
                blades.Add(blade);
            }
            while (blades.Count > count)
            {
                Destroy(blades[^1].gameObject);
                blades.RemoveAt(blades.Count - 1);
            }
        }

        private void Update()
        {
            if (blades.Count == 0) return;

            angle = (angle + degreesPerSecond * Time.deltaTime) % 360f;
            float step = 360f / blades.Count;

            for (int i = 0; i < blades.Count; i++)
            {
                float a = angle + i * step;
                float rad = a * Mathf.Deg2Rad;
                blades[i].transform.SetLocalPositionAndRotation(
                    new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius,
                    Quaternion.Euler(0f, 0f, a + spriteAngleOffset));
            }
        }

        internal void HandleHit(Enemy enemy)
        {
            Vector2 away = ((Vector2)enemy.transform.position - (Vector2)transform.position).normalized;
            enemy.TakeHit(damage * PlayerController.CurrentDamageMultiplier, away * knockback);
            SfxPlayer.Play(hitSfx, 0.3f);
        }
    }
}
