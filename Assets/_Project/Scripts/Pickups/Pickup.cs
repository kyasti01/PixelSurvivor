using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    public enum PickupKind { Experience, Health, Magnet, Chest, Gold }

    /// <summary>
    /// Yerde duran bir şey. Oyuncu toplama alanına girince ona doğru uçar ve toplanır.
    /// Fizik yerine mesafe kontrolü kullanır; etrafta yüzlerce taş varken bu daha ucuzdur.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        public static readonly List<Pickup> Active = new();

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.05f)] private float collectDistance = 0.4f;
        [SerializeField, Min(1f)] private float acceleration = 40f;
        [SerializeField, Min(1f)] private float maxSpeed = 16f;
        [Tooltip("Sprite'lar bu pixels-per-unit değerine göre ölçeklenir; farklı paketlerin çizimleri aynı boyutta görünsün diye.")]
        [SerializeField, Min(1f)] private float referencePpu = 18f;
        [SerializeField, Min(0.1f)] private float baseScale = 1.3f;

        [Header("Sounds")]
        [SerializeField] private AudioClip goldSfx;
        [SerializeField] private AudioClip healthSfx;
        [SerializeField] private AudioClip magnetSfx;
        [SerializeField] private AudioClip chestSfx;

        public PickupKind Kind { get; private set; }
        public int Amount { get; private set; }

        private Action<Pickup> release;
        private bool magnetized;
        private float speed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        public void Init(PickupKind kind, int amount, Sprite sprite, Vector2 position, Action<Pickup> releaseToPool,
                         float scale = 1f)
        {
            Kind = kind;
            Amount = amount;
            spriteRenderer.sprite = sprite;
            float ppuScale = sprite != null ? sprite.pixelsPerUnit / referencePpu : 1f;
            transform.localScale = Vector3.one * (baseScale * scale * ppuScale);
            transform.position = position;
            release = releaseToPool;
            magnetized = false;
            speed = 0f;
        }

        public void AddAmount(int amount) => Amount += amount;
        public void SetSprite(Sprite sprite) => spriteRenderer.sprite = sprite;
        public void Magnetize() => magnetized = true;

        private void Update()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null || player.Health.IsDead) return;

            Vector2 toPlayer = player.transform.position - transform.position;
            float sqr = toPlayer.sqrMagnitude;

            if (!magnetized)
            {
                float r = player.Experience.PickupRadius;
                if (sqr > r * r) return;
                magnetized = true;
            }

            if (sqr <= collectDistance * collectDistance)
            {
                Collect(player);
                return;
            }

            speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
            float step = Mathf.Min(speed * Time.deltaTime, Mathf.Sqrt(sqr));
            transform.position += (Vector3)(toPlayer.normalized * step);
        }

        private void Collect(PlayerController player)
        {
            switch (Kind)
            {
                case PickupKind.Experience:
                    player.Experience.AddXp(Amount);
                    break;
                case PickupKind.Health:
                    player.Health.Heal(Amount);
                    SfxPlayer.Play(healthSfx, 0.7f);
                    break;
                case PickupKind.Magnet:
                    foreach (Pickup p in Active)
                        if (p.Kind is PickupKind.Experience or PickupKind.Gold) p.Magnetize();
                    SfxPlayer.Play(magnetSfx, 0.7f);
                    break;
                case PickupKind.Chest:
                    SfxPlayer.Play(chestSfx);
                    if (UpgradeManager.Instance != null) UpgradeManager.Instance.GrantChest(Amount);
                    break;
                case PickupKind.Gold:
                    SfxPlayer.Play(goldSfx, 0.35f);
                    if (GameManager.Instance != null) GameManager.Instance.AddGold(Amount);
                    break;
            }

            if (release != null) release(this);
            else Destroy(gameObject);
        }
    }
}
