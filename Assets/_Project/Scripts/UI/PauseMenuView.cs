using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Esc menüsü: devam, ses kaydırıcıları, ana menüye dönüş.</summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [Tooltip("Seviye atlama ekranı oyunu duraklatıyorsa (Pause While Choosing açıksa) menü onun üstünde açılmaz.")]
        [SerializeField] private LevelUpView levelUpView;

        public bool IsOpen => root.activeSelf;

        private void Awake()
        {
            root.SetActive(false);
            resumeButton.onClick.AddListener(Close);
            menuButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
            });

            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
            sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            if (IsOpen) Close();
            else Open();
        }

        private void Open()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || gm.IsGameOver) return;
            // Oyunu durdurmayan seviye atlama şeridinin üstünde menü açılabilir; sadece tam ekran
            // (oyunu duraklatan) seviye atlama paneli onu engeller.
            if (levelUpView != null && levelUpView.IsOpen && gm.IsPaused) return;

            root.SetActive(true);
            gm.SetMenuPaused(true);
        }

        private void Close()
        {
            root.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.SetMenuPaused(false);
        }
    }
}
