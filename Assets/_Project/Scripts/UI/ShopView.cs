using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Ana menü dükkanı: altınla kalıcı yükseltmeler al.</summary>
    public class ShopView : MonoBehaviour
    {
        [Serializable]
        public class Row
        {
            public MetaStat stat;
            public Button buyButton;
            public Text title;
            public Text description;
            public Text cost;
        }

        [SerializeField] private Row[] rows;
        [SerializeField] private Text goldText;
        [SerializeField] private AudioClip buySfx;
        [SerializeField] private Color affordableColor = new(1f, 0.82f, 0.35f);
        [SerializeField] private Color unaffordableColor = new(0.55f, 0.5f, 0.45f);

        private void Awake()
        {
            foreach (Row row in rows)
            {
                Row r = row;
                r.buyButton.onClick.AddListener(() => Buy(r));
            }
        }

        private void OnEnable() => Refresh();

        private void Buy(Row row)
        {
            if (!MetaProgression.TryBuy(row.stat)) return;
            SfxPlayer.Play(buySfx);
            Refresh();
        }

        private void Refresh()
        {
            goldText.text = $"Altın: {MetaProgression.Gold}";
            foreach (Row row in rows)
            {
                int level = MetaProgression.Level(row.stat);
                bool maxed = MetaProgression.IsMaxed(row.stat);
                bool canBuy = MetaProgression.CanBuy(row.stat);

                row.title.text = $"{MetaProgression.DisplayName(row.stat)}  {level}/{MetaProgression.MaxLevel}";
                row.description.text = MetaProgression.Description(row.stat);
                row.cost.text = maxed ? "MAKS" : $"{MetaProgression.Cost(row.stat)} altın";
                row.cost.color = canBuy || maxed ? affordableColor : unaffordableColor;
                row.buyButton.interactable = canBuy;
            }
        }
    }
}
