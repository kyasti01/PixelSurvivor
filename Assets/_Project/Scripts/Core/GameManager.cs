using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PixelSurvivor
{
    /// <summary>
    /// Oyunun durumu: hayatta kalma süresi, kill sayısı, altın, duraklatma, kazanma/kaybetme ve yeniden başlatma.
    /// Oyun son boss yenilince kazanılır (bkz. <see cref="BossDirector"/>).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameOverView gameOverView;
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioClip loseSfx;
        [SerializeField] private AudioClip winSfx;
        [SerializeField] private string menuScene = "MainMenu";

        public static GameManager Instance { get; private set; }

        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        /// <summary>Bu oyunda toplanan altın (Açgözlülük yükseltmesiyle zaten çarpılmış).</summary>
        public int RunGold { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsPaused => levelUpPaused || menuPaused || introPaused;
        public bool IsMenuPaused => menuPaused;

        private Health playerHealth;
        private bool levelUpPaused;
        private bool menuPaused;
        private bool introPaused;
        private bool goldBanked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            Enemy.Killed += OnEnemyKilled;
            if (PlayerController.Instance != null)
            {
                playerHealth = PlayerController.Instance.Health;
                playerHealth.Died += OnPlayerDied;
            }
        }

        private void OnDestroy()
        {
            Enemy.Killed -= OnEnemyKilled;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (IsGameOver)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.rKey.wasPressedThisFrame) Restart();
                return;
            }

            Elapsed += Time.deltaTime;
        }

        /// <summary>Seviye atlama / sandık ekranı için duraklatma.</summary>
        public void SetPaused(bool paused)
        {
            levelUpPaused = paused;
            ApplyTimeScale();
        }

        /// <summary>Esc menüsü için duraklatma.</summary>
        public void SetMenuPaused(bool paused)
        {
            menuPaused = paused;
            ApplyTimeScale();
        }

        /// <summary>Oyun başındaki geri sayım için duraklatma (bkz. <see cref="StartCountdown"/>).</summary>
        public void SetIntroPaused(bool paused)
        {
            introPaused = paused;
            ApplyTimeScale();
        }

        private void ApplyTimeScale() => Time.timeScale = IsPaused || IsGameOver ? 0f : 1f;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            float multiplier = MetaProgression.GoldMultiplier * DifficultySettings.GoldReward;
            RunGold += Mathf.Max(1, Mathf.RoundToInt(amount * multiplier));
        }

        public void Victory() => EndRun(victory: true);

        public void Restart()
        {
            BankGold();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void GoToMenu()
        {
            BankGold();
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuScene);
        }

        /// <summary>Bu oyunda toplanan altını kalıcı kasaya aktarır (sadece bir kez).</summary>
        private void BankGold()
        {
            if (goldBanked) return;
            goldBanked = true;
            MetaProgression.AddGold(RunGold);
        }

        private void OnEnemyKilled(Enemy _) => Kills++;

        private void OnPlayerDied() => EndRun(victory: false);

        private void EndRun(bool victory)
        {
            if (IsGameOver) return;
            IsGameOver = true;
            ApplyTimeScale();
            BankGold();
            if (victory) MetaProgression.RecordVictory();

            if (music != null) music.Stop();
            SfxPlayer.Play(victory ? winSfx : loseSfx);

            int level = PlayerController.Instance != null ? PlayerController.Instance.Experience.Level : 1;
            if (gameOverView != null) gameOverView.Show(victory, Elapsed, Kills, level, RunGold);
        }
    }
}
