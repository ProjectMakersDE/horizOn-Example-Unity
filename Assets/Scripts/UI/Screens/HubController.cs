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

            await System.Threading.Tasks.Task.Yield();
            var gm = GameManager.Instance;
            if (gm == null) return;
            int generation = HorizonManager.Instance.Generation;
            string owner = HorizonManager.Instance.GetUserId();
            bool Current() => this != null && gm != null && GameManager.Instance == gm && HorizonManager.Instance.Generation == generation && HorizonManager.Instance.GetUserId() == owner;
            if (!gm.ProgressLoaded)
            {
                gm.LoadLocalProgress();
                // Lock play while session-specific progress is loading, so a late response cannot overwrite a run.
                if (playButton != null) playButton.interactable = false;
                try
                {
                    var configs = await HorizonManager.Instance.LoadAllConfigs();
                    if (!Current()) return;
                    if (configs != null) gm.Config = GameConfig.Parse(configs);
                    HorizonManager.Instance.ActiveBoard = gm.Config.ValidatedRunsBoard;
                    string json = await HorizonManager.Instance.LoadCloudData();
                    if (!Current()) return;
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var cloud = JsonUtility.FromJson<SaveData>(json);
                        if (cloud != null && cloud.upgrades != null)
                        {
                            cloud.giftCodesRedeemed ??= new List<string>();
                            gm.Save = cloud;
                            gm.PersistLocalProgress();
                        }
                    }
                }
                catch (System.Exception ex) { Debug.LogWarning("[SeagullStorm] Hub load failed: " + ex.Message); }
                finally { if (Current()) { gm.ProgressLoaded = true; if (playButton != null) playButton.interactable = true; } }
            }
            if (!Current()) return;
            HorizonManager.Instance?.RecordBreadcrumb("navigation", "hub_loaded");

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
                    comp.Setup((int)entry.position, entry.username, entry.score);
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
