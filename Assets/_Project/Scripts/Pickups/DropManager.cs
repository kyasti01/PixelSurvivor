using UnityEngine;
using UnityEngine.Pool;

namespace PixelSurvivor
{
    /// <summary>Düşmanların öldüğü yere XP taşı (ve arada bir iksir ya da mıknatıs) düşürür.</summary>
    public class DropManager : MonoBehaviour
    {
        [SerializeField] private Pickup pickupPrefab;

        [Header("Experience gems")]
        [Tooltip("Küçük, orta, büyük.")]
        [SerializeField] private Sprite[] gemSprites = new Sprite[3];
        [Tooltip("En az bu değerdeki taşlar orta / büyük sprite'ı kullanır.")]
        [SerializeField] private int mediumGemValue = 3;
        [SerializeField] private int largeGemValue = 10;
        [Tooltip("Yerde bundan fazla pickup varsa yeni XP mevcut taşlara eklenir.")]
        [SerializeField, Min(10)] private int maxPickups = 400;

        [Header("Rare drops")]
        [SerializeField] private Sprite healthSprite;
        [SerializeField] private Sprite magnetSprite;
        [SerializeField, Range(0f, 1f)] private float healthChance = 0.015f;
        [SerializeField, Range(0f, 1f)] private float magnetChance = 0.008f;
        [SerializeField, Min(1)] private int healAmount = 30;

        [Header("Gold")]
        [SerializeField] private Sprite goldSprite;
        [SerializeField, Range(0f, 1f)] private float goldChance = 0.07f;
        [Tooltip("Ölen boss'un etrafına saçılan altın sayısı.")]
        [SerializeField, Min(0)] private int bossGoldCoins = 8;
        [SerializeField, Min(1)] private int bossCoinValue = 3;
        [SerializeField, Min(0.1f)] private float goldScale = 0.55f;

        [Header("Elites")]
        [SerializeField, Min(1)] private int eliteXpMultiplier = 5;
        [SerializeField, Min(0)] private int eliteGoldCoins = 3;

        [Header("Chest (from bosses)")]
        [SerializeField] private Sprite chestSprite;
        [Tooltip("Bir sandığın verdiği bedava yükseltme seçimi sayısı.")]
        [SerializeField, Min(1)] private int chestPicks = 1;

        private ObjectPool<Pickup> pool;

        private void Awake()
        {
            pool = new ObjectPool<Pickup>(
                createFunc: () => Instantiate(pickupPrefab, transform),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: false,
                defaultCapacity: 128,
                maxSize: 1024);
        }

        private void OnEnable() => Enemy.Killed += OnEnemyKilled;
        private void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        private void OnEnemyKilled(Enemy enemy)
        {
            Vector2 pos = enemy.transform.position;
            int xp = enemy.Stats != null ? enemy.Stats.xpValue : 1;
            DropExperience(enemy.IsElite ? xp * eliteXpMultiplier : xp, pos);

            if (enemy.IsElite && goldSprite != null)
            {
                for (int i = 0; i < eliteGoldCoins; i++)
                    Spawn(PickupKind.Gold, 1, goldSprite, pos + Random.insideUnitCircle * 0.8f, goldScale);
            }

            bool boss = enemy.Stats != null && enemy.Stats.dropsChest;
            if (boss && chestSprite != null)
                Spawn(PickupKind.Chest, chestPicks, chestSprite, pos + Vector2.up * 0.5f);

            if (goldSprite != null)
            {
                if (boss)
                {
                    for (int i = 0; i < bossGoldCoins; i++)
                        Spawn(PickupKind.Gold, bossCoinValue, goldSprite, pos + Random.insideUnitCircle * 1.5f, goldScale);
                }
                else if (Random.value < goldChance)
                {
                    Spawn(PickupKind.Gold, 1, goldSprite, pos + Random.insideUnitCircle * 0.3f, goldScale);
                }
            }

            float roll = Random.value;
            Vector2 aside = pos + Random.insideUnitCircle * 0.4f;
            if (roll < magnetChance) Spawn(PickupKind.Magnet, 0, magnetSprite, aside);
            else if (roll < magnetChance + healthChance) Spawn(PickupKind.Health, healAmount, healthSprite, aside);
        }

        private void DropExperience(int value, Vector2 pos)
        {
            if (Pickup.Active.Count >= maxPickups)
            {
                // Yerde çok fazla var: onun yerine rastgele bir mevcut taşa ekle.
                for (int i = 0; i < 8; i++)
                {
                    Pickup p = Pickup.Active[Random.Range(0, Pickup.Active.Count)];
                    if (p.Kind != PickupKind.Experience) continue;
                    p.AddAmount(value);
                    p.SetSprite(GemSprite(p.Amount));
                    return;
                }
            }
            Spawn(PickupKind.Experience, value, GemSprite(value), pos);
        }

        private Sprite GemSprite(int value)
        {
            int tier = value >= largeGemValue ? 2 : value >= mediumGemValue ? 1 : 0;
            return gemSprites[Mathf.Min(tier, gemSprites.Length - 1)];
        }

        private void Spawn(PickupKind kind, int amount, Sprite sprite, Vector2 pos, float scale = 1f)
        {
            pool.Get().Init(kind, amount, sprite, pos, pool.Release, scale);
        }
    }
}
