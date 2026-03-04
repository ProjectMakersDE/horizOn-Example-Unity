using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Single upgrade display in hub: level, cost, buy button.
    /// </summary>
    public class UpgradeSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button buyButton;

        private string _upgradeType;

        public void Setup(string type, string label, int level, int[] costs)
        {
            _upgradeType = type;

            if (labelText != null) labelText.text = label;
            if (levelText != null) levelText.text = $"Lv.{level}";

            if (level >= costs.Length)
            {
                if (costText != null) costText.text = "MAX";
                if (buyButton != null) buyButton.interactable = false;
            }
            else
            {
                if (costText != null) costText.text = $"{costs[level]}";
                if (buyButton != null)
                {
                    buyButton.interactable = true;
                    buyButton.onClick.RemoveAllListeners();
                    buyButton.onClick.AddListener(OnBuy);
                }
            }
        }

        private void OnBuy()
        {
            bool success = GameManager.Instance?.TryPurchaseUpgrade(_upgradeType) ?? false;
            if (success)
                AudioManager.Instance?.PlayUpgradeSelect();
        }
    }
}
