using UnityEngine;
using UnityEngine.Pool;

namespace PixelSurvivor
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private EnemyStats[] enemyTypes;

        [Header("Steady stream")]
       
        [SerializeField, Min(0.01f)] private float initialInterval = 0.9f;
        [SerializeField, Min(0.01f)] private float minInterval = 0.08f;
        // Doğma aralığı bu kadar saniyede bir yarıya iner.
        [SerializeField, Min(1f)] private float intervalHalvingTime = 150f;
        // Her bu kadar saniyede doğuş başına bir düşman daha eklenir.
        [SerializeField, Min(1f)] private float extraEnemyEvery = 90f;

        [Header("Ring waves")]
        [SerializeField, Min(1f)] private float waveEvery = 60f;
        [SerializeField, Min(1)] private int waveSize = 14;
        [SerializeField, Min(0)] private int waveGrowthPerWave = 8;

        [Header("Scaling over time (m = minutes into the run)")]
        // Can x (1 + perMinute*m + quadratic*m²): başta yavaş, sonra hızlı artar.
        [SerializeField, Min(0f)] private float healthGrowthPerMinute = 0.35f;
        // Düşman sınırı düşürülünce artırıldı: daha az ama daha dayanıklı düşman, aynı zorluk ve daha az karmaşa.
        [SerializeField, Min(0f)] private float healthGrowthQuadratic = 0.14f;
        // Temas hasarı x (1 + bu*m).
        [SerializeField, Min(0f)] private float damageGrowthPerMinute = 0.12f;
        // Hareket hızı x (1 + bu*m), alttaki değerle sınırlı.
        [SerializeField, Min(0f)] private float speedGrowthPerMinute = 0.05f;
        [SerializeField, Min(1f)] private float maxSpeedMultiplier = 1.4f;

        [Header("Elites")]
        // Normal bir doğuşun elit olma şansı: temel + perMinute*m, sınırlı.
        [SerializeField, Range(0f, 1f)] private float eliteBaseChance = 0.02f;
        [SerializeField, Range(0f, 1f)] private float eliteChancePerMinute = 0.01f;
        [SerializeField, Range(0f, 1f)] private float maxEliteChance = 0.1f;
        [SerializeField, Min(1f)] private float eliteHealthMultiplier = 4f;
        [SerializeField, Min(1f)] private float eliteScale = 1.3f;
        [SerializeField] private Color eliteTint = new(1f, 0.8f, 0.25f);

        [Header("Limits")]
        [SerializeField, Min(1)] private int maxAlive = 180;
        // Düşmanların göründüğü, ekran kenarının ötesindeki mesafe.
        [SerializeField, Min(0f)] private float spawnPadding = 1.5f;

        private ObjectPool<Enemy> pool;
        private Camera cam;
        private Transform player;
        private float timer;
        private float nextWave;
        private int wavesSpawned;

        private float Elapsed => GameManager.Instance != null ? GameManager.Instance.Elapsed : Time.timeSinceLevelLoad;

        public float CurrentInterval =>
            Mathf.Max(minInterval, initialInterval * Mathf.Pow(0.5f, Elapsed / intervalHalvingTime))
            * DifficultySettings.SpawnInterval;

        public int EnemiesPerTick => 1 + (int)(Elapsed / extraEnemyEvery);

        private float Minutes => Elapsed / 60f;
        public float HealthMultiplier =>
            (1f + healthGrowthPerMinute * Minutes + healthGrowthQuadratic * Minutes * Minutes) * DifficultySettings.EnemyHealth;
        public float DamageMultiplier => (1f + damageGrowthPerMinute * Minutes) * DifficultySettings.EnemyDamage;
        public float EliteChance => Mathf.Min(maxEliteChance, eliteBaseChance + eliteChancePerMinute * Minutes);
        public float SpeedMultiplier => Mathf.Min(maxSpeedMultiplier, 1f + speedGrowthPerMinute * Minutes);

        private void Awake()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemySpawner] Enemy Prefab is not assigned. Re-run 'PixelSurvivor > Build Prototype'.", this);
                enabled = false;
                return;
            }

            pool = new ObjectPool<Enemy>(
                createFunc: () => Instantiate(enemyPrefab, transform),
                actionOnGet: e => e.gameObject.SetActive(true),
                actionOnRelease: e => e.gameObject.SetActive(false),
                actionOnDestroy: e => Destroy(e.gameObject),
                collectionCheck: false,
                defaultCapacity: 128,
                maxSize: 1024);
            nextWave = waveEvery;
        }

        private void Start()
        {
            cam = Camera.main;
            if (PlayerController.Instance != null) player = PlayerController.Instance.transform;
        }

        private void Update()
        {
            if (pool == null || player == null || cam == null || enemyTypes == null || enemyTypes.Length == 0) return;
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

            if (Elapsed >= nextWave)
            {
                nextWave += waveEvery;
                SpawnRing(waveSize + wavesSpawned * waveGrowthPerWave);
                wavesSpawned++;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = CurrentInterval;

            int count = EnemiesPerTick;
            for (int i = 0; i < count; i++) Spawn(RandomPointOutsideView());
        }

        /// <summary>Belirli bir düşmanı (örn. boss) ekranın hemen dışında doğurur; düşman sınırını yok sayar.</summary>
        public Enemy SpawnSpecial(EnemyStats stats, float healthMultiplier)
        {
            if (pool == null || player == null || cam == null || stats == null) return null;

            // Boss'lar can ve hasarda zaman çarpanını alır, ama tasarlandıkları hızı korur.
            Enemy enemy = pool.Get();
            enemy.Init(stats, RandomPointOutsideView(), player, pool.Release,
                healthMultiplier * HealthMultiplier, DamageMultiplier, 1f);
            return enemy;
        }

        private void SpawnRing(int count)
        {
            float halfH = cam.orthographicSize;
            float radius = Mathf.Sqrt(halfH * halfH + Mathf.Pow(halfH * cam.aspect, 2f)) + spawnPadding;
            Vector2 center = player.position;
            float offset = Random.value * 360f;
            EnemyStats waveType = PickType(); // her dalga tek türden; her dalga ayrı bir olay gibi hissettirsin

            for (int i = 0; i < count; i++)
            {
                float a = (offset + i * 360f / count) * Mathf.Deg2Rad;
                Spawn(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, waveType);
            }
        }

        private void Spawn(Vector2 position, EnemyStats stats = null)
        {
            if (Enemy.Active.Count >= maxAlive) return;
            stats ??= PickType();
            if (stats == null) return;

            Enemy enemy = pool.Get();
            enemy.Init(stats, position, player, pool.Release, HealthMultiplier, DamageMultiplier, SpeedMultiplier);
            if (Random.value < EliteChance) enemy.MakeElite(eliteHealthMultiplier, eliteScale, eliteTint);
        }

        /// <summary>O anda aktif olan türler arasından ağırlıklı rastgele seçim.</summary>
        private EnemyStats PickType()
        {
            float elapsed = Elapsed;
            float total = 0f;
            foreach (var t in enemyTypes)
                if (t.IsSpawnableAt(elapsed)) total += t.spawnWeight;
            if (total <= 0f) return null;

            float roll = Random.value * total;
            EnemyStats last = null;
            foreach (var t in enemyTypes)
            {
                if (!t.IsSpawnableAt(elapsed)) continue;
                last = t;
                roll -= t.spawnWeight;
                if (roll <= 0f) return t;
            }
            return last;
        }

        private Vector2 RandomPointOutsideView()
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir == Vector2.zero) dir = Vector2.right;

            // Kamera merkezinden, dir yönünde ekran dikdörtgeninin kenarına olan mesafe.
            float toEdge = Mathf.Min(
                Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
                Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);

            return (Vector2)cam.transform.position + dir * (toEdge + spawnPadding);
        }
    }
}
