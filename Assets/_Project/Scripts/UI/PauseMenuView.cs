using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Esc menu: resume, volume sliders, back to main menu.</summary>
    public class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [Tooltip("The pause menu doesn't open over the level-up screen.")]
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
            // With the non-pausing level-up strip the menu may open over it; only a full-screen
            // (pausing) level-up panel blocks it.
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
