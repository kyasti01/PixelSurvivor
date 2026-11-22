using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Oyun sonu paneli. Kazanınca yeşilimsi arka plan, büyük KAZANDIN! yazısı ve konfeti;
    /// ölünce kırmızı arka plan ve ÖLDÜN... yazısı. Altında istatistikler ve butonlar.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private Text headline;
        [SerializeField] private Image title;
        [SerializeField] private Sprite defeatTitle;
        [SerializeField] private Sprite victoryTitle;
        [SerializeField] private Text stats;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Confetti confetti;

        [Header("Kazanma / kaybetme görünümü")]
        [SerializeField] private string victoryText = "KAZANDIN!";
        [SerializeField] private string defeatText = "ÖLDÜN...";
        [SerializeField] private Color victoryColor = new(1f, 0.85f, 0.25f);
        [SerializeField] private Color defeatColor = new(1f, 0.3f, 0.25f);
        [SerializeField] private Color victoryBackground = new(0.04f, 0.16f, 0.06f, 0.85f);
        [SerializeField] private Color defeatBackground = new(0.18f, 0.02f, 0.02f, 0.8f);

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
            if (headline != null)
            {
                headline.text = victory ? victoryText : defeatText;
                headline.color = victory ? victoryColor : defeatColor;
            }
            if (background != null) background.color = victory ? victoryBackground : defeatBackground;

            title.sprite = victory ? victoryTitle : defeatTitle;
            title.rectTransform.sizeDelta = title.sprite.rect.size * 5f; // her sanat pikseli için 5 ekran pikseli

            int s = (int)elapsed;
            stats.text = $"{DifficultySettings.DisplayName(DifficultySettings.Current)}     " +
                         $"Süre {s / 60:00}:{s % 60:00}     Kill {kills}     Seviye {level}\n" +
                         $"Altın +{gold}   (Toplam: {MetaProgression.Gold})";
            root.SetActive(true);

            if (victory && confetti != null) confetti.Play();
        }
    }
}
