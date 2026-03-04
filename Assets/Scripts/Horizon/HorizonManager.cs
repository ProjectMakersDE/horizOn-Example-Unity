using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Responses;

namespace SeagullStorm
{
    /// <summary>
    /// Facade for all 9 horizOn SDK features. Isolates SDK calls from gameplay code.
    /// </summary>
    public class HorizonManager : MonoBehaviour
    {
        public static HorizonManager Instance { get; private set; }

        public bool IsSignedIn => UserManager.Instance.IsSignedIn;
        public string GetUserId() => UserManager.Instance.CurrentUser?.UserId;
        public string GetDisplayName() => UserManager.Instance.CurrentUser?.DisplayName ?? "Guest";

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

        // ===== Auth =====

        public async Task<bool> SignUpGuest(string name)
        {
            bool result = await UserManager.Instance.SignUpAnonymous(name);
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public async Task<bool> SignInEmail(string email, string pw)
        {
            bool result = await UserManager.Instance.SignInEmail(email, pw);
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public async Task<bool> SignUpEmail(string email, string pw, string name)
        {
            bool result = await UserManager.Instance.SignUpEmail(email, pw, name);
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public async Task<bool> SignInGoogle(string googleAuthorizationCode, string redirectUri = "")
        {
            bool result = await UserManager.Instance.SignInGoogle(googleAuthorizationCode, redirectUri);
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public async Task<bool> SignUpGoogle(string googleAuthorizationCode, string redirectUri = "", string name = null)
        {
            bool result = await UserManager.Instance.SignUpGoogle(googleAuthorizationCode, redirectUri, name);
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public async Task<bool> RestoreSession()
        {
            bool result = await UserManager.Instance.RestoreAnonymousSession();
            if (result) CrashManager.Instance.SetUserId(GetUserId());
            return result;
        }

        public void SignOut()
        {
            UserManager.Instance.SignOut();
        }

        // ===== Remote Config =====

        private Dictionary<string, string> _cachedConfigs;

        public async Task<Dictionary<string, string>> LoadAllConfigs(bool useCache = true)
        {
            if (useCache && _cachedConfigs != null)
                return _cachedConfigs;

            var result = await RemoteConfigManager.Instance.GetAllConfigs(useCache: false);
            if (result != null)
                _cachedConfigs = result;
            return result;
        }

        // ===== Cloud Save =====

        public async Task<string> LoadCloudData()
        {
            return await CloudSaveManager.Instance.Load();
        }

        public async Task<bool> SaveCloudData(string json)
        {
            return await CloudSaveManager.Instance.Save(json);
        }

        // ===== Leaderboard =====

        public async Task<List<SimpleLeaderboardEntry>> GetTop10()
        {
            return await LeaderboardManager.Instance.GetTop(10);
        }

        public async Task<bool> SubmitScore(long score)
        {
            return await LeaderboardManager.Instance.SubmitScore(score);
        }

        public async Task<AppUserRankResponse> GetRank()
        {
            return await LeaderboardManager.Instance.GetRank();
        }

        // ===== News =====

        public async Task<List<UserNewsResponse>> LoadNews()
        {
            return await NewsManager.Instance.LoadNews(5, "en");
        }

        // ===== Gift Codes =====

        public async Task<bool?> ValidateGiftCode(string code)
        {
            return await GiftCodeManager.Instance.Validate(code);
        }

        public async Task<RedeemGiftCodeResponse> RedeemGiftCode(string code)
        {
            return await GiftCodeManager.Instance.Redeem(code);
        }

        // ===== Feedback =====

        public async Task<bool> SubmitFeedback(string title, string msg, string category, string email = null)
        {
            return await FeedbackManager.Instance.Submit(title, category, msg, email, includeDeviceInfo: true);
        }

        // ===== User Logs =====

        public async Task LogRunEnd(string message)
        {
            await UserLogManager.Instance.Info(message);
        }

        public async Task LogWarning(string message)
        {
            await UserLogManager.Instance.Warn(message);
        }

        // ===== Crash Reporting =====

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

        public void RecordException(Exception ex)
        {
            CrashManager.Instance.RecordException(ex);
        }
    }
}
