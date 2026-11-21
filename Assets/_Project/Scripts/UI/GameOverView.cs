using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>End-of-run panel: "DEAD..." or "SURVIVED!", run stats and a restart button.</summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image title;
        [SerializeField] private Sprite defeatTitle;
        [SerializeField] private Sprite victoryTitle;
        [SerializeField] private Text stats;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            root.SetActive(false);
            restartButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.Restart();
            });
            menuButton.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.GoToMenu();
            });
        }

        public void Show(bool victory, float elapsed, int kills, int level, int gold)
        {
            title.sprite = victory ? victoryTitle : defeatTitle;
            title.rectTransform.sizeDelta = title.sprite.rect.size * 6f; // 6 screen px per art px

            int s = (int)elapsed;
            stats.text = $"{DifficultySettings.DisplayName(DifficultySettings.Current)}     " +
                         $"Süre {s / 60:00}:{s % 60:00}     Kill {kills}     Seviye {level}\n" +
                         $"Altın +{gold}   (Toplam: {MetaProgression.Gold})";
            root.SetActive(true);
        }
    }
}
