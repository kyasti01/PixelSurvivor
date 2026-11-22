using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Belirli zamanlarda boss gönderir; her biri sandık düşürür. Sonuncusu son boss'tur:
    /// onu yenmek oyunu kazandırır.
    /// </summary>
    public class BossDirector : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;
        // Boss Times'daki her satır için boss (daha az boss varsa sonuncusu tekrarlanır).
        [SerializeField] private EnemyStats[] bosses;
        // Her boss'un geldiği saniye. Son satır son boss'tur.
        [SerializeField] private float[] bossTimes = { 60f, 120f, 180f, 240f, 300f, 360f, 420f, 480f };
        // Sonraki her boss için ek can (0.5 = boss başına +%50), oyunun zaman çarpanına ek olarak.
        [SerializeField, Min(0f)] private float healthGrowthPerBoss;
        [SerializeField, Min(0.5f)] private float announcementDuration = 3f;
        [SerializeField] private string announcement = "BOSS GELİYOR!";
        [SerializeField] private string finalAnnouncement = "SON BOSS! YEN ONU!";
        [SerializeField] private AudioClip announceSfx;

        [Header("Final boss")]
        [SerializeField] private AudioClip finalAnnounceSfx;
        [SerializeField] private MusicPlayer music;
        [SerializeField] private AudioClip finalBossMusic;

        [Header("Boss death")]
        [SerializeField] private Sprite[] deathExplosionFrames;
        [SerializeField, Min(1f)] private float deathExplosionFps = 12f;
        // Boss ölçeğinin her birimi başına patlama ölçeği.
        [SerializeField, Min(0.1f)] private float deathExplosionScale = 1f;
        [SerializeField] private AudioClip deathSfx;

        /// <summary>Şu an hayatta olan boss, yoksa null.</summary>
        public static Enemy CurrentBoss { get; private set; }
        public static bool CurrentIsFinal { get; private set; }
        public static string Announcement { get; private set; }
        /// <summary>Uyarı yazısının gösterileceği son Time.time değeri.</summary>
        public static float AnnouncementUntil { get; private set; }
        /// <summary>Son boss'un geldiği oyun süresi (saniye); yoksa +sonsuz.</summary>
        public static float FinalBossTime { get; private set; } = float.PositiveInfinity;

        private int nextBoss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            CurrentBoss = null;
            CurrentIsFinal = false;
            Announcement = null;
            AnnouncementUntil = 0f;
            FinalBossTime = float.PositiveInfinity;
        }

        private void Awake()
        {
            FinalBossTime = bossTimes.Length > 0 ? bossTimes[^1] : float.PositiveInfinity;
        }

        private void OnEnable() => Enemy.Killed += OnEnemyKilled;

        private void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
            CurrentBoss = null;
            CurrentIsFinal = false;
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsGameOver || nextBoss >= bossTimes.Length) return;
            if (gm.Elapsed < bossTimes[nextBoss] || bosses == null || bosses.Length == 0) return;

            bool isFinal = nextBoss == bossTimes.Length - 1;
            EnemyStats stats = bosses[Mathf.Min(nextBoss, bosses.Length - 1)];
            Enemy boss = spawner.SpawnSpecial(stats, 1f + nextBoss * healthGrowthPerBoss);
            nextBoss++;
            if (boss == null) return;

            CurrentBoss = boss;
            CurrentIsFinal = isFinal;
            Announcement = isFinal ? finalAnnouncement : announcement;
            AnnouncementUntil = Time.time + announcementDuration;
            SfxPlayer.Play(isFinal && finalAnnounceSfx != null ? finalAnnounceSfx : announceSfx);
            CameraFollow.Shake(isFinal ? 0.25f : 0.12f, 0.5f);
            if (isFinal && music != null) music.Play(finalBossMusic);
        }

        // Havuzdaki düşmanlar yeniden kullanılır; o yüzden boss ölür ölmez onu unut.
        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemy != CurrentBoss) return;

            bool wasFinal = CurrentIsFinal;
            CurrentBoss = null;
            CurrentIsFinal = false;

            EffectPlayer.Play(deathExplosionFrames, deathExplosionFps, enemy.transform.position,
                deathExplosionScale * enemy.transform.localScale.x);
            SfxPlayer.Play(deathSfx);
            CameraFollow.Shake(0.35f, 0.5f);
            if (wasFinal && GameManager.Instance != null) GameManager.Instance.Victory();
        }
    }
}
