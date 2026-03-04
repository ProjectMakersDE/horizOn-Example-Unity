using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PM.horizOn.Cloud.Objects.Network.Responses;
using SeagullStorm.Data;
using SeagullStorm.Managers;
using SeagullStorm.SDK;

namespace SeagullStorm.UI.Screens
{
    public class HubScreenUI : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private TMP_Text playerNameText;

        [Header("Upgrade Buttons")]
        [SerializeField] private Button speedUpgradeButton;
        [SerializeField] private TMP_Text speedUpgradeText;
        [SerializeField] private Button damageUpgradeButton;
        [SerializeField] private TMP_Text damageUpgradeText;
        [SerializeField] private Button hpUpgradeButton;
        [SerializeField] private TMP_Text hpUpgradeText;
        [SerializeField] private Button magnetUpgradeButton;
        [SerializeField] private TMP_Text magnetUpgradeText;

        [Header("Actions")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button giftCodeButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button settingsButton;

        [Header("Leaderboard")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardEntryPrefab;

        [Header("News")]
        [SerializeField] private Transform newsContainer;
        [SerializeField] private GameObject newsEntryPrefab;

        [Header("Panels")]
        [SerializeField] private GameObject giftCodePanel;
        [SerializeField] private TMP_InputField giftCodeInput;
        [SerializeField] private Button redeemButton;
        [SerializeField] private TMP_Text giftCodeStatus;

        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMP_InputField feedbackTitleInput;
        [SerializeField] private TMP_InputField feedbackMessageInput;
        [SerializeField] private TMP_Dropdown feedbackCategoryDropdown;
        [SerializeField] private Button submitFeedbackButton;
        [SerializeField] private TMP_Text feedbackStatus;

        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button closeSettingsButton;

        [Header("Loading")]
        [SerializeField] private GameObject loadingOverlay;

        private async void Start()
        {
            AudioManager.Instance?.PlayMenuMusic();

            SetupButtons();

            if (loadingOverlay != null) loadingOverlay.SetActive(true);

            await GameManager.Instance.LoadHubData();

            RefreshUI();
            await LoadLeaderboard();
            await LoadNews();

            if (loadingOverlay != null) loadingOverlay.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDataChanged += RefreshUI;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayerDataChanged -= RefreshUI;
        }

        private void SetupButtons()
        {
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (speedUpgradeButton != null) speedUpgradeButton.onClick.AddListener(() => OnUpgrade("speed"));
            if (damageUpgradeButton != null) damageUpgradeButton.onClick.AddListener(() => OnUpgrade("damage"));
            if (hpUpgradeButton != null) hpUpgradeButton.onClick.AddListener(() => OnUpgrade("hp"));
            if (magnetUpgradeButton != null) magnetUpgradeButton.onClick.AddListener(() => OnUpgrade("magnet"));
            if (giftCodeButton != null) giftCodeButton.onClick.AddListener(OnToggleGiftCode);
            if (feedbackButton != null) feedbackButton.onClick.AddListener(OnToggleFeedback);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnToggleSettings);
            if (redeemButton != null) redeemButton.onClick.AddListener(OnRedeemGiftCode);
            if (submitFeedbackButton != null) submitFeedbackButton.onClick.AddListener(OnSubmitFeedback);
            if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOut);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(() => settingsPanel?.SetActive(false));
        }

        private void RefreshUI()
        {
            var data = GameManager.Instance.PlayerData;
            var config = GameManager.Instance.Config;

            if (coinsText != null) coinsText.text = $"Coins: {data.coins:N0}";
            if (bestScoreText != null) bestScoreText.text = $"Best: {data.highscore:N0}";
            if (playerNameText != null) playerNameText.text = HorizonSDKIntegration.Instance.DisplayName;

            RefreshUpgradeButton(speedUpgradeText, "Speed", data.upgrades.speed, config.upgradeSpeedMax, config.upgradeSpeedCosts);
            RefreshUpgradeButton(damageUpgradeText, "Damage", data.upgrades.damage, config.upgradeDamageMax, config.upgradeDamageCosts);
            RefreshUpgradeButton(hpUpgradeText, "HP", data.upgrades.hp, config.upgradeHpMax, config.upgradeHpCosts);
            RefreshUpgradeButton(magnetUpgradeText, "Magnet", data.upgrades.magnet, config.upgradeMagnetMax, config.upgradeMagnetCosts);
        }

        private void RefreshUpgradeButton(TMP_Text text, string name, int level, int max, int[] costs)
        {
            if (text == null) return;

            if (level >= max)
            {
                text.text = $"{name} Lv.{level}\nMAX";
            }
            else
            {
                int cost = level < costs.Length ? costs[level] : 999;
                text.text = $"{name} Lv.{level}\n[+] {cost}";
            }
        }

        private void OnPlay()
        {
            GameManager.Instance.StartRun();
        }

        private void OnUpgrade(string type)
        {
            bool success = GameManager.Instance.TryPurchaseUpgrade(type);
            if (success)
            {
                AudioManager.Instance?.PlayUpgradeSelect();
            }
        }

        private async System.Threading.Tasks.Task LoadLeaderboard()
        {
            var entries = await HorizonSDKIntegration.Instance.GetLeaderboardTop(10);
            if (entries == null || leaderboardContainer == null || leaderboardEntryPrefab == null) return;

            foreach (Transform child in leaderboardContainer) Destroy(child.gameObject);

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var obj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                var text = obj.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = $"#{entry.position} {entry.username}  {entry.score:N0}";
                }
            }
        }

        private async System.Threading.Tasks.Task LoadNews()
        {
            var news = await HorizonSDKIntegration.Instance.LoadNews(5, "en");
            if (news == null || newsContainer == null || newsEntryPrefab == null) return;

            foreach (Transform child in newsContainer) Destroy(child.gameObject);

            foreach (var item in news)
            {
                var obj = Instantiate(newsEntryPrefab, newsContainer);
                var text = obj.GetComponent<TMP_Text>();
                if (text != null)
                {
                    text.text = $"* {item.title}";
                }
            }
        }

        private void OnToggleGiftCode()
        {
            if (giftCodePanel != null) giftCodePanel.SetActive(!giftCodePanel.activeSelf);
            if (feedbackPanel != null) feedbackPanel.SetActive(false);
        }

        private void OnToggleFeedback()
        {
            if (feedbackPanel != null) feedbackPanel.SetActive(!feedbackPanel.activeSelf);
            if (giftCodePanel != null) giftCodePanel.SetActive(false);
        }

        private void OnToggleSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private async void OnRedeemGiftCode()
        {
            string code = giftCodeInput != null ? giftCodeInput.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                if (giftCodeStatus != null) giftCodeStatus.text = "Enter a code.";
                return;
            }

            // Check if already redeemed locally
            if (GameManager.Instance.PlayerData.giftCodesRedeemed.Contains(code))
            {
                if (giftCodeStatus != null) giftCodeStatus.text = "Already redeemed!";
                return;
            }

            if (giftCodeStatus != null) giftCodeStatus.text = "Validating...";

            bool? valid = await HorizonSDKIntegration.Instance.ValidateGiftCode(code);
            if (valid != true)
            {
                if (giftCodeStatus != null) giftCodeStatus.text = "Invalid code.";
                return;
            }

            var result = await HorizonSDKIntegration.Instance.RedeemGiftCode(code);
            if (result != null)
            {
                GameManager.Instance.PlayerData.giftCodesRedeemed.Add(code);
                // Parse gift data for coin reward
                if (!string.IsNullOrEmpty(result.giftData))
                {
                    try
                    {
                        var giftData = JsonUtility.FromJson<GiftReward>(result.giftData);
                        if (giftData != null)
                        {
                            GameManager.Instance.PlayerData.coins += giftData.coins;
                        }
                    }
                    catch { }
                }
                if (giftCodeStatus != null) giftCodeStatus.text = "Redeemed!";
                RefreshUI();
            }
            else
            {
                if (giftCodeStatus != null) giftCodeStatus.text = "Redemption failed.";
            }
        }

        private async void OnSubmitFeedback()
        {
            string title = feedbackTitleInput != null ? feedbackTitleInput.text : "";
            string message = feedbackMessageInput != null ? feedbackMessageInput.text : "";
            string category = "GENERAL";

            if (feedbackCategoryDropdown != null)
            {
                switch (feedbackCategoryDropdown.value)
                {
                    case 0: category = "BUG"; break;
                    case 1: category = "FEATURE"; break;
                    case 2: category = "GENERAL"; break;
                }
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                if (feedbackStatus != null) feedbackStatus.text = "Fill in title and message.";
                return;
            }

            if (feedbackStatus != null) feedbackStatus.text = "Sending...";
            bool success = await HorizonSDKIntegration.Instance.SubmitFeedback(title, message, category);

            if (success)
            {
                if (feedbackStatus != null) feedbackStatus.text = "Sent! Thank you.";
                if (feedbackTitleInput != null) feedbackTitleInput.text = "";
                if (feedbackMessageInput != null) feedbackMessageInput.text = "";
            }
            else
            {
                if (feedbackStatus != null) feedbackStatus.text = "Failed to send.";
            }
        }

        private void OnSignOut()
        {
            HorizonSDKIntegration.Instance.SignOut();
            GameManager.Instance.GoToTitle();
        }

        [System.Serializable]
        private class GiftReward
        {
            public int coins;
        }
    }
}
