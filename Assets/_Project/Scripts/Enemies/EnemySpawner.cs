using UnityEngine;
using UnityEngine.Pool;

namespace PixelSurvivor
{
    /// <summary>
    /// Spawns enemies just outside the camera view. Pressure grows three ways over time:
    /// shorter interval, more enemies per tick, and periodic ring waves that surround the player.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Enemy enemyPrefab;
        [SerializeField] private EnemyStats[] enemyTypes;

        [Header("Steady stream")]
        // Tuned so Normal reaches ~4-6 min on a first try and the 8:00 final boss with good builds
        // (at 2:20 the old values spawned ~14 enemies/s, which killed players around then).
        [SerializeField, Min(0.01f)] private float initialInterval = 0.9f;
        [SerializeField, Min(0.01f)] private float minInterval = 0.08f;
        [Tooltip("The spawn interval halves every this many seconds.")]
        [SerializeField, Min(1f)] private float intervalHalvingTime = 150f;
        [Tooltip("One more enemy per spawn tick every this many seconds.")]
        [SerializeField, Min(1f)] private float extraEnemyEvery = 90f;

        [Header("Ring waves")]
        [SerializeField, Min(1f)] private float waveEvery = 60f;
        [SerializeField, Min(1)] private int waveSize = 14;
        [SerializeField, Min(0)] private int waveGrowthPerWave = 8;

        [Header("Scaling over time (m = minutes into the run)")]
        [Tooltip("Health x (1 + perMinute*m + quadratic*m²): slow at first, steep later.")]
        [SerializeField, Min(0f)] private float healthGrowthPerMinute = 0.35f;
        [Tooltip("Raised when the enemy cap was lowered: fewer but tougher enemies keep the challenge with less clutter.")]
        [SerializeField, Min(0f)] private float healthGrowthQuadratic = 0.14f;
        [Tooltip("Contact damage x (1 + this*m).")]
        [SerializeField, Min(0f)] private float damageGrowthPerMinute = 0.12f;
        [Tooltip("Move speed x (1 + this*m), capped below.")]
        [SerializeField, Min(0f)] private float speedGrowthPerMinute = 0.05f;
        [SerializeField, Min(1f)] private float maxSpeedMultiplier = 1.4f;

        [Header("Elites")]
        [Tooltip("Chance that a normal spawn is an elite: base + perMinute*m, capped.")]
        [SerializeField, Range(0f, 1f)] private float eliteBaseChance = 0.02f;
        [SerializeField, Range(0f, 1f)] private float eliteChancePerMinute = 0.01f;
        [SerializeField, Range(0f, 1f)] private float maxEliteChance = 0.1f;
        [SerializeField, Min(1f)] private float eliteHealthMultiplier = 4f;
        [SerializeField, Min(1f)] private float eliteScale = 1.3f;
        [SerializeField] private Color eliteTint = new(1f, 0.8f, 0.25f);

        [Header("Limits")]
        [SerializeField, Min(1)] private int maxAlive = 180;
        [Tooltip("Distance beyond the screen edge where enemies appear.")]
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

        /// <summary>Spawns one specific enemy (e.g. a boss) just off-screen, ignoring the alive cap.</summary>
        public Enemy SpawnSpecial(EnemyStats stats, float healthMultiplier)
        {
            if (pool == null || player == null || cam == null || stats == null) return null;

            // Bosses get the time scaling for health and damage, but keep their designed speed.
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
            EnemyStats waveType = PickType(); // one type per wave, so each wave reads as an event

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

        /// <summary>Weighted random pick among the types active at the current time.</summary>
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

            // Distance from the camera centre to the screen rectangle along dir.
            float toEdge = Mathf.Min(
                Mathf.Abs(dir.x) > 0.0001f ? halfW / Mathf.Abs(dir.x) : float.MaxValue,
                Mathf.Abs(dir.y) > 0.0001f ? halfH / Mathf.Abs(dir.y) : float.MaxValue);

            return (Vector2)cam.transform.position + dir * (toEdge + spawnPadding);
        }
    }
}
