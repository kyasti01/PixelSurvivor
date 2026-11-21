using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>
    /// Level-up choices: pick one upgrade with the mouse or keys 1-3. Shown as a strip at the
    /// bottom of the screen while the game keeps running (see UpgradeManager.pauseWhileChoosing).
    /// </summary>
    public class LevelUpView : MonoBehaviour
    {
        [Serializable]
        public class Option
        {
            public Button button;
            public Image icon;
            public Text title;
            public Text description;
            [Tooltip("Small tag such as YENİ! / Sv.2 / EVRİM! (optional).")]
            public Text badge;
        }

        [SerializeField] private Color newColor = new(0.45f, 1f, 0.45f);
        [SerializeField] private Color levelColor = new(0.55f, 0.85f, 1f);
        [SerializeField] private Color evolutionColor = new(1f, 0.45f, 0.9f);

        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Option[] options;
        [Tooltip("Ignore input briefly after opening, so a held key or click doesn't pick by accident.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.3f;

        private readonly List<UpgradeDefinition> current = new();
        private Action<UpgradeDefinition> onPick;
        private float openedAt;

        public bool IsOpen => root.activeSelf;

        private void Awake()
        {
            root.SetActive(false);
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                options[i].button.onClick.AddListener(() => Pick(index));
            }
        }

        public void SetTitle(string title)
        {
            if (titleText != null) titleText.text = title;
        }

        public void Show(string title, IReadOnlyList<UpgradeDefinition> choices, Func<UpgradeDefinition, int> levelOf,
                         Action<UpgradeDefinition> pick)
        {
            if (titleText != null) titleText.text = title;
            current.Clear();
            current.AddRange(choices);
            onPick = pick;
            openedAt = Time.unscaledTime;
            root.SetActive(true);

            for (int i = 0; i < options.Length; i++)
            {
                Option o = options[i];
                bool used = i < current.Count;
                o.button.gameObject.SetActive(used);
                if (!used) continue;

                UpgradeDefinition def = current[i];
                int level = levelOf(def);
                o.icon.sprite = def.icon;
                o.icon.enabled = def.icon != null;
                string badge = def.isEvolution ? "EVRİM!"
                    : def.maxLevel == 0 ? ""
                    : level == 0 ? "YENİ!"
                    : $"Sv.{level + 1}";
                Color badgeColor = def.isEvolution ? evolutionColor : level == 0 ? newColor : levelColor;

                if (o.badge != null)
                {
                    o.title.text = def.displayName;
                    o.badge.text = badge;
                    o.badge.color = badgeColor;
                }
                else
                {
                    o.title.text = $"[{i + 1}] {def.displayName}  {badge}";
                }
                o.description.text = def.description;
            }

            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (!IsOpen) return;

            GameManager gm = GameManager.Instance;
            if (gm != null && gm.IsGameOver)
            {
                root.SetActive(false); // the run ended while choices were still on screen
                return;
            }
            if (gm != null && gm.IsMenuPaused) return; // Esc menu is open on top

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Pick(0);
            else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Pick(1);
            else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Pick(2);
        }

        private void Pick(int index)
        {
            if (!IsOpen || index >= current.Count) return;
            if (Time.unscaledTime - openedAt < inputDelay) return;

            root.SetActive(false);
            Action<UpgradeDefinition> callback = onPick;
            onPick = null;
            callback?.Invoke(current[index]); // may immediately Show() again for the next level
        }
    }
}
