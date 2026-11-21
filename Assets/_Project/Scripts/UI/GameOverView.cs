using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Oyun sonu paneli: DEAD... ya da SURVIVED!, oyun istatistikleri ve yeniden başlat butonu.</summary>
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
            title.rectTransform.sizeDelta = title.sprite.rect.size * 6f; // her sanat pikseli için 6 ekran pikseli

            int s = (int)elapsed;
            stats.text = $"{DifficultySettings.DisplayName(DifficultySettings.Current)}     " +
                         $"Süre {s / 60:00}:{s % 60:00}     Kill {kills}     Seviye {level}\n" +
                         $"Altın +{gold}   (Toplam: {MetaProgression.Gold})";
            root.SetActive(true);
        }
    }
}
