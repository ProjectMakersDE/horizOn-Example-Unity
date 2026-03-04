using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;
using SeagullStorm.Data;

namespace SeagullStorm.SDK
{
    public class HorizonSDKIntegration : MonoBehaviour
    {
        public static HorizonSDKIntegration Instance { get; private set; }

        private HorizonServer _server;

        public bool IsAuthenticated => UserManager.Instance.IsSignedIn;
        public string UserId => UserManager.Instance.CurrentUser?.UserId ?? "";
        public string DisplayName => UserManager.Instance.CurrentUser?.DisplayName ?? "Guest";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task<bool> InitializeSDK()
        {
            bool initialized = HorizonApp.Initialize();
            if (!initialized)
            {
                Debug.LogError("[SeagullStorm] Failed to initialize horizOn SDK");
                return false;
            }

            _server = new HorizonServer();
            bool connected = await _server.Connect();
            if (!connected)
            {
                Debug.LogError("[SeagullStorm] Failed to connect to horizOn server");
                return false;
            }

            Debug.Log("[SeagullStorm] horizOn SDK initialized and connected");
            return true;
        }

        // ===== AUTH =====

        public async Task<bool> SignUpAnonymous(string displayName)
        {
            bool result = await UserManager.Instance.SignUpAnonymous(displayName);
            if (result)
            {
                CrashManager.Instance.SetUserId(UserId);
            }
            return result;
        }

        public async Task<bool> SignInEmail(string email, string password)
        {
            bool result = await UserManager.Instance.SignInEmail(email, password);
            if (result)
            {
                CrashManager.Instance.SetUserId(UserId);
            }
            return result;
        }

        public async Task<bool> SignUpEmail(string email, string password, string displayName)
        {
            bool result = await UserManager.Instance.SignUpEmail(email, password, displayName);
            if (result)
            {
                CrashManager.Instance.SetUserId(UserId);
            }
            return result;
        }

        public async Task<bool> RestoreSession()
        {
            if (UserManager.Instance.IsSignedIn)
            {
                bool valid = await UserManager.Instance.CheckAuth();
                if (valid)
                {
                    CrashManager.Instance.SetUserId(UserId);
                    return true;
                }
            }

            bool restored = await UserManager.Instance.RestoreAnonymousSession();
            if (restored)
            {
                CrashManager.Instance.SetUserId(UserId);
            }
            return restored;
        }

        public void SignOut()
        {
            UserManager.Instance.SignOut();
        }

        // ===== REMOTE CONFIG =====

        public async Task<Dictionary<string, string>> LoadAllConfigs()
        {
            return await RemoteConfigManager.Instance.GetAllConfigs(useCache: false);
        }

        // ===== CLOUD SAVE =====

        public async Task<SaveData> LoadCloudSave()
        {
            string json = await CloudSaveManager.Instance.Load();
            if (string.IsNullOrEmpty(json))
            {
                return new SaveData();
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SeagullStorm] Failed to parse cloud save: {e.Message}");
                return new SaveData();
            }
        }

        public async Task<bool> SaveCloudData(SaveData data)
        {
            string json = JsonUtility.ToJson(data);
            return await CloudSaveManager.Instance.Save(json);
        }

        // ===== LEADERBOARD =====

        public async Task<List<SimpleLeaderboardEntry>> GetLeaderboardTop(int limit = 10)
        {
            return await LeaderboardManager.Instance.GetTop(limit, useCache: false);
        }

        public async Task<bool> SubmitScore(long score)
        {
            return await LeaderboardManager.Instance.SubmitScore(score);
        }

        public async Task<AppUserRankResponse> GetPlayerRank()
        {
            return await LeaderboardManager.Instance.GetRank();
        }

        // ===== NEWS =====

        public async Task<List<UserNewsResponse>> LoadNews(int limit = 5, string lang = "en")
        {
            return await NewsManager.Instance.LoadNews(limit, lang, useCache: false);
        }

        // ===== GIFT CODES =====

        public async Task<bool?> ValidateGiftCode(string code)
        {
            return await GiftCodeManager.Instance.Validate(code);
        }

        public async Task<RedeemGiftCodeResponse> RedeemGiftCode(string code)
        {
            return await GiftCodeManager.Instance.Redeem(code);
        }

        // ===== FEEDBACK =====

        public async Task<bool> SubmitFeedback(string title, string message, string category)
        {
            return await FeedbackManager.Instance.Submit(title, category, message);
        }

        // ===== USER LOG =====

        public async Task LogRunEnd(int waves, int level, int score, float duration, UpgradeData upgrades, int coinsEarned)
        {
            string msg = $"Run ended | Waves: {waves} | Level: {level} | Score: {score} | " +
                         $"Duration: {Mathf.FloorToInt(duration / 60)}m{Mathf.FloorToInt(duration % 60):D2}s | " +
                         $"Upgrades: speed:{upgrades.speed},dmg:{upgrades.damage},hp:{upgrades.hp} | " +
                         $"Coins earned: {coinsEarned}";
            await UserLogManager.Instance.Info(msg);
        }

        public async Task LogEarlyDeathWarning(int consecutiveWave1Deaths)
        {
            if (consecutiveWave1Deaths >= 3)
            {
                await UserLogManager.Instance.Warn(
                    $"Player died in wave 1 three consecutive times (total: {consecutiveWave1Deaths}). Possible balancing issue.");
            }
        }

        // ===== CRASH REPORTING =====

        public void StartCrashCapture()
        {
            CrashManager.Instance.StartCapture();
        }

        public void RecordBreadcrumb(string type, string message)
        {
            CrashManager.Instance.RecordBreadcrumb(type, message);
        }

        public void SetCrashCustomKey(string key, string value)
        {
            CrashManager.Instance.SetCustomKey(key, value);
        }

        public void RecordException(Exception exception)
        {
            CrashManager.Instance.RecordException(exception);
        }
    }
}
