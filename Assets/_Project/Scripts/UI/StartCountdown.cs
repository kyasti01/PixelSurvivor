using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Oyun başında "3, 2, 1, HAYATTA KAL!" geri sayımı. Animasyonun kendisi bir Animation Clip'tir
    /// (Data/Countdown/StartCountdown.anim) ve Unity'nin Animation penceresinden elle düzenlenebilir.
    /// Bu script sadece oyunu geri sayım boyunca durdurur ve klipteki Animation Event'lere cevap verir:
    ///   Tick()     → her sayıda "bip" sesi
    ///   Go()       → "HAYATTA KAL!" sesi; oyun başlar
    ///   Finished() → geri sayım ekranı kapanır
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class StartCountdown : MonoBehaviour
    {
        [SerializeField] private AudioClip tickSfx;
        [SerializeField] private AudioClip goSfx;

        private Animator animator;
        private bool started;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            // Oyun geri sayım boyunca durduğu (timeScale = 0) için animasyon gerçek zamanla oynar.
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        private void Start()
        {
            // Klip yoksa oyunu kilitlemek yerine geri sayımı atla.
            if (animator.runtimeAnimatorController == null)
            {
                gameObject.SetActive(false);
                return;
            }

            started = true;
            if (GameManager.Instance != null) GameManager.Instance.SetIntroPaused(true);
        }

        private void Update()
        {
            // Esc menüsü açıkken geri sayım da bekler.
            GameManager gm = GameManager.Instance;
            animator.speed = gm != null && gm.IsMenuPaused ? 0f : 1f;
        }

        private void OnDisable() => ReleaseGame();

        // ---------- Animation Event'ler (klipten çağrılır) ----------

        public void Tick() => SfxPlayer.Play(tickSfx);

        public void Go()
        {
            SfxPlayer.Play(goSfx);
            ReleaseGame();
        }

        public void Finished() => gameObject.SetActive(false);

        private void ReleaseGame()
        {
            if (!started) return;
            started = false;
            if (GameManager.Instance != null) GameManager.Instance.SetIntroPaused(false);
        }
    }
}
