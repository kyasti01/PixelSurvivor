using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Başlık ekranı (Başla / Nasıl Oynanır / Dükkan / Çıkış), ardından zorluk seçimiyle karakter seçimi.
    /// Kilitli karakterler altınla satın alınır ya da oyunu kazanınca açılır.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        [Serializable]
        public class CharacterCard
        {
            public Button button;
            public Image portrait;
            public Text title;
            public Text description;
        }

        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject selectPanel;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject howToPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button howToButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button shopBackButton;
        [SerializeField] private Button howToBackButton;
        [SerializeField] private CharacterDefinition[] characters;
        [SerializeField] private CharacterCard[] cards;
        [SerializeField] private string gameScene = "Game";
        [SerializeField, Min(1f)] private float portraitFps = 8f;

        [Header("Difficulty (Easy, Normal, Hard)")]
        [SerializeField] private Button[] difficultyButtons = new Button[3];
        [SerializeField] private Text[] difficultyLabels = new Text[3];
        [SerializeField] private Color selectedColor = new(1f, 0.82f, 0.35f);
        [SerializeField] private Color unselectedColor = new(0.55f, 0.5f, 0.45f);

        [Header("Locks")]
        [SerializeField] private Text goldText;
        [SerializeField] private Color lockedPortraitColor = new(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private AudioClip unlockSfx;

        private void Awake()
        {
            startButton.onClick.AddListener(() => Show(selectPanel));
            shopButton.onClick.AddListener(() => Show(shopPanel));
            howToButton.onClick.AddListener(() => Show(howToPanel));
            backButton.onClick.AddListener(() => Show(mainPanel));
            shopBackButton.onClick.AddListener(() => Show(mainPanel));
            howToBackButton.onClick.AddListener(() => Show(mainPanel));
            quitButton.onClick.AddListener(Quit);

            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < characters.Length && characters[i] != null;
                cards[i].button.gameObject.SetActive(used);
                if (!used) continue;

                int index = i;
                cards[i].button.onClick.AddListener(() => OnCardClicked(index));
            }

            for (int i = 0; i < difficultyButtons.Length; i++)
            {
                var d = (Difficulty)i;
                difficultyButtons[i].onClick.AddListener(() =>
                {
                    DifficultySettings.Current = d;
                    RefreshDifficulty();
                });
            }

            Time.timeScale = 1f;
            Show(mainPanel);
        }

        private void Update()
        {
            // Açık karakterlerin portrelerini kendi koşu animasyonlarıyla oynat.
            for (int i = 0; i < cards.Length && i < characters.Length; i++)
            {
                CharacterDefinition c = characters[i];
                if (c == null || c.runFrames == null || c.runFrames.Length == 0 || !MetaProgression.IsUnlocked(c)) continue;
                cards[i].portrait.sprite = c.runFrames[(int)(Time.unscaledTime * portraitFps) % c.runFrames.Length];
            }

            if (!mainPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Show(mainPanel);
        }

        private void Show(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            selectPanel.SetActive(panel == selectPanel);
            shopPanel.SetActive(panel == shopPanel);
            howToPanel.SetActive(panel == howToPanel);
            if (panel == selectPanel)
            {
                RefreshCards();
                RefreshDifficulty();
            }
        }

        private void OnCardClicked(int index)
        {
            CharacterDefinition c = characters[index];
            if (MetaProgression.IsUnlocked(c))
            {
                RunConfig.SelectedCharacter = c;
                SceneManager.LoadScene(gameScene);
                return;
            }

            if (MetaProgression.TryUnlock(c))
            {
                SfxPlayer.Play(unlockSfx);
                RefreshCards();
            }
        }

        private void RefreshCards()
        {
            if (goldText != null) goldText.text = $"Altın: {MetaProgression.Gold}";

            for (int i = 0; i < cards.Length && i < characters.Length; i++)
            {
                CharacterDefinition c = characters[i];
                if (c == null) continue;
                CharacterCard card = cards[i];
                bool unlocked = MetaProgression.IsUnlocked(c);

                card.title.text = c.displayName;
                card.portrait.sprite = c.standFrames.Length > 0 ? c.standFrames[0] : null;
                card.portrait.color = unlocked ? Color.white : lockedPortraitColor;

                if (unlocked) card.description.text = c.description;
                else if (c.requiresVictory) card.description.text = $"{c.description}\n\nKİLİTLİ\nSon boss'u bir kez yen";
                else card.description.text = $"{c.description}\n\nKİLİTLİ\n{c.unlockCost} altın (tıkla)";
            }
        }

        private void RefreshDifficulty()
        {
            for (int i = 0; i < difficultyLabels.Length; i++)
            {
                bool selected = (int)DifficultySettings.Current == i;
                string name = DifficultySettings.DisplayName((Difficulty)i);
                difficultyLabels[i].text = selected ? $"> {name} <" : name;
                difficultyLabels[i].color = selected ? selectedColor : unselectedColor;
            }
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
