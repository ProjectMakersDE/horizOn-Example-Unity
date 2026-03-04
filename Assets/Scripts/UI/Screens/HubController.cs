using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PM.horizOn.Cloud.Objects.Network.Responses;

namespace SeagullStorm
{
    /// <summary>
    /// Hub UI: remote config load, cloud save load, leaderboard, news, upgrades, gift codes, feedback, settings.
    /// </summary>
    public class HubController : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text highscoreText;

        [Header("Play")]
        [SerializeField] private Button playButton;

        [Header("Upgrades")]
        [SerializeField] private Transform upgradeContainer;
        [SerializeField] private GameObject upgradeSlotPrefab;

        [Header("Leaderboard")]
        [SerializeField] private Transform leaderboardContainer;
        [SerializeField] private GameObject leaderboardEntryPrefab;

        [Header("News")]
        [SerializeField] private Transform newsContainer;
        [SerializeField] private GameObject newsEntryPrefab;

        [Header("Sub-Panels")]
        [SerializeField] private GiftCodeForm giftCodeForm;
        [SerializeField] private FeedbackForm feedbackForm;
        [SerializeField] private SettingsPanel settingsPanel;

        [Header("Settings Button")]
        [SerializeField] private Button settingsButton;

        public static List<UserNewsResponse> CachedNews { get; private set; }

        private async void OnEnable()
        {
            AudioManager.Instance?.PlayMenuMusic();

            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSaveDataChanged += RefreshUI;

            // Load remote config
            var configs = await HorizonManager.Instance.LoadAllConfigs(useCache: true);
            if (configs != null)
                GameManager.Instance.Config = GameConfig.Parse(configs);

            // Load cloud save
            string json = await HorizonManager.Instance.LoadCloudData();
            if (!string.IsNullOrEmpty(json))
                GameManager.Instance.Save = JsonUtility.FromJson<SaveData>(json);
            else
                GameManager.Instance.Save = SaveData.CreateDefault();

            HorizonManager.Instance.RecordBreadcrumb("navigation", "hub_loaded");

            RefreshUI();
            await LoadLeaderboard();
            await LoadNews();
        }

        private void OnDisable()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlay);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);

            if (GameManager.Instance != null)
                GameManager.Instance.OnSaveDataChanged -= RefreshUI;
        }

        private void RefreshUI()
        {
            var save = GameManager.Instance?.Save;
            if (save == null) return;

            if (coinsText != null) coinsText.text = $"Coins: {save.coins:N0}";
            if (highscoreText != null) highscoreText.text = $"Best: {save.highscore:N0}";

            RefreshUpgrades();
        }

        private void RefreshUpgrades()
        {
            if (upgradeContainer == null || upgradeSlotPrefab == null) return;

            foreach (Transform child in upgradeContainer) Destroy(child.gameObject);

            var save = GameManager.Instance.Save;
            var config = GameManager.Instance.Config;

            CreateUpgradeSlot("speed", "Speed", save.upgrades.speed, config.UpgradeSpeedCosts);
            CreateUpgradeSlot("damage", "Damage", save.upgrades.damage, config.UpgradeDamageCosts);
            CreateUpgradeSlot("hp", "HP", save.upgrades.hp, config.UpgradeHpCosts);
            CreateUpgradeSlot("magnet", "Magnet", save.upgrades.magnet, config.UpgradeMagnetCosts);
        }

        private void CreateUpgradeSlot(string type, string label, int level, int[] costs)
        {
            var obj = Instantiate(upgradeSlotPrefab, upgradeContainer);
            var slot = obj.GetComponent<UpgradeSlot>();
            if (slot != null)
                slot.Setup(type, label, level, costs);
        }

        private async System.Threading.Tasks.Task LoadLeaderboard()
        {
            var entries = await HorizonManager.Instance.GetTop10();
            if (entries == null || leaderboardContainer == null || leaderboardEntryPrefab == null) return;

            foreach (Transform child in leaderboardContainer) Destroy(child.gameObject);

            foreach (var entry in entries)
            {
                var obj = Instantiate(leaderboardEntryPrefab, leaderboardContainer);
                var comp = obj.GetComponent<LeaderboardEntry>();
                if (comp != null)
                    comp.Setup(entry.position, entry.username, entry.score);
            }
        }

        private async System.Threading.Tasks.Task LoadNews()
        {
            var news = await HorizonManager.Instance.LoadNews();
            CachedNews = news;
            if (news == null || newsContainer == null || newsEntryPrefab == null) return;

            foreach (Transform child in newsContainer) Destroy(child.gameObject);

            foreach (var item in news)
            {
                var obj = Instantiate(newsEntryPrefab, newsContainer);
                var comp = obj.GetComponent<NewsEntry>();
                if (comp != null)
                    comp.Setup(item.title, item.releaseDate);
            }
        }

        private void OnPlay()
        {
            GameManager.Instance?.StartRun();
        }

        private void OnSettings()
        {
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
        }
    }
}
