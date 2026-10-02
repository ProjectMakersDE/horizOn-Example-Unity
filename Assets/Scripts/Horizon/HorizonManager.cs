using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Requests;
using PM.horizOn.Cloud.Objects.Network.Responses;
namespace SeagullStorm
{
    /// <summary>Session-scoped SDK facade. Late responses never replace another identity or practice session.</summary>
    public class HorizonManager : MonoBehaviour
    {
        public static HorizonManager Instance { get; private set; }
        public bool Configured { get; set; }
        private bool _practiceMode = true;
        public bool PracticeMode => _practiceMode;
        public int Generation { get; private set; }
        public bool CloudReady { get; private set; }
        public bool IsSignedIn => Configured && UserManager.Instance.IsSignedIn;
        public bool Online => IsSignedIn && !_practiceMode;
        public string ConnectionStatus => Online ? "Connected to horizOn" : Configured ? "Practice • sign in for online features" : "Offline practice • no SDK config";
        public string GetUserId() => Online ? UserManager.Instance.CurrentUser?.UserId : null;
        public string GetDisplayName() => Online ? UserManager.Instance.CurrentUser?.DisplayName ?? "Seagull" : "Practice pilot";
        public string ActiveBoard { get; set; } = "default";
        public string LastLeaderboardError => Online ? LeaderboardManager.Instance.LastErrorCode : "OFFLINE_PRACTICE";
        public string ProfileError => Online ? PlayerProfileManager.Instance.LastErrorCode : "OFFLINE_PRACTICE";
        private Dictionary<string, string> _cachedConfigs;
        private readonly SemaphoreSlim _authLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _saveLock = new SemaphoreSlim(1, 1);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        public void EnterPractice()
        {
            Generation++;
            _practiceMode = true;
            CloudReady = false;
            _cachedConfigs = null;
        }
        public bool IsCurrent(int generation, string owner) => generation == Generation && Online && owner == GetUserId();
        private async Task<T> SessionCall<T>(Func<Task<T>> operation)
        {
            if (!Online) return default;
            int generation = Generation;
            string owner = GetUserId();
            T result = await operation();
            return IsCurrent(generation, owner) ? result : default;
        }
        private async Task<bool> Auth(Func<Task<bool>> operation)
        {
            if (!Configured) return false;
            EnterPractice();
            int generation = Generation;
            // SDK auth updates a shared CurrentUser. Serialize attempts so a cancelled older
            // response cannot replace the current user after a newer sign-in has completed.
            await _authLock.WaitAsync();
            try
            {
                if (generation != Generation) return false;
                bool success = await operation();
                if (generation != Generation) return false;
                if (success && UserManager.Instance.IsSignedIn)
                {
                    _practiceMode = false;
                    CrashManager.Instance.SetUserId(GetUserId());
                    StartCrashCapture();
                    return true;
                }
                return false;
            }
            finally { _authLock.Release(); }
        }
        public Task<bool> SignUpGuest(string name) => Auth(() => UserManager.Instance.SignUpAnonymous(name));
        public Task<bool> SignInEmail(string email, string pw) => Auth(() => UserManager.Instance.SignInEmail(email, pw));
        public Task<bool> SignUpEmail(string email, string pw, string name) => Auth(() => UserManager.Instance.SignUpEmail(email, pw, name));
        public Task<bool> SignInGoogle(string code, string redirectUri = "") => Auth(() => UserManager.Instance.SignInGoogle(code, redirectUri));
        public Task<bool> SignUpGoogle(string code, string redirectUri = "", string name = null) => Auth(() => UserManager.Instance.SignUpGoogle(code, redirectUri, name));
        public Task<bool> SignInWithApple() => Auth(() => UserManager.Instance.SignInWithApple());
        public Task<bool> RestoreSession() => Auth(async () =>
        {
            int generation = Generation;
            if (await UserManager.Instance.CheckAuth()) return true;
            if (generation != Generation) return false;
            return await UserManager.Instance.RestoreAnonymousSession();
        });
        public void SignOut()
        {
            EnterPractice();
            if (Configured) UserManager.Instance.SignOut();
        }
        public async Task<Dictionary<string, string>> LoadAllConfigs(bool useCache = true)
        {
            if (!Online) return null;
            if (useCache && _cachedConfigs != null) return _cachedConfigs;
            int generation = Generation;
            string owner = GetUserId();
            var value = await SessionCall(() => RemoteConfigManager.Instance.GetAllConfigs(useCache: false));
            if (!IsCurrent(generation, owner)) return null;
            if (value != null) _cachedConfigs = value;
            return value;
        }
        public async Task<string> LoadCloudData()
        {
            if (!Online) return null;
            int generation = Generation;
            string owner = GetUserId();
            CloudReady = false;
            string loaded = await CloudSaveManager.Instance.Load();
            if (!IsCurrent(generation, owner)) return null;
            CloudReady = loaded != null;
            if (loaded == null)
            {
                // Load() conflates absence and failure. Confirm absence via the same typed SDK contract.
                var response = await HorizonApp.Network.PostAsync<LoadCloudSaveResponse>(
                    "/api/v1/app/cloud-save/load", new LoadCloudDataRequest { userId = owner }, useSessionToken: true);
                if (!IsCurrent(generation, owner)) return null;
                CloudReady = response.IsSuccess && response.Data != null && !response.Data.found;
            }
            return loaded;
        }
        public async Task<bool> SaveCloudData(string json)
        {
            if (!Online || !CloudReady) return false;
            int generation = Generation;
            string owner = GetUserId();
            await _saveLock.WaitAsync();
            try
            {
                if (!IsCurrent(generation, owner) || !CloudReady) return false;
                bool saved = await CloudSaveManager.Instance.Save(json);
                return saved && IsCurrent(generation, owner);
            }
            finally { _saveLock.Release(); }
        }
        public Task<List<SimpleLeaderboardEntry>> GetTop10() => SessionCall(() => LeaderboardManager.Instance.GetTop(10, false, ActiveBoard));
        public Task<List<SimpleLeaderboardEntry>> GetAround() => SessionCall(() => LeaderboardManager.Instance.GetAround(5, false, ActiveBoard));
        public Task<List<LeaderboardBoardResponse>> ListBoards() => SessionCall(() => LeaderboardManager.Instance.ListBoards());
        public Task<bool> SubmitScore(long score) => SessionCall(() => LeaderboardManager.Instance.SubmitScore(score, boardKey: ActiveBoard));
        public Task<AppUserRankResponse> GetRank() => SessionCall(() => LeaderboardManager.Instance.GetRank(ActiveBoard));
        public Task<PlayerProfileResponse> GetProfile() => SessionCall(() => PlayerProfileManager.Instance.GetProfile());
        public Task<PlayerProfileResponse> SetProfile(string avatar, string frame, IList<string> badges) => SessionCall(() => PlayerProfileManager.Instance.SetProfile(avatar, frame, badges));
        public Task<Dictionary<string, string>> Localize(string lang) => SessionCall(() =>
        {
            LocalizationManager.Instance.SetLanguage(lang);
            return LocalizationManager.Instance.GetAllLocalizations(lang);
        });
        public Task<PlayerState> GetValidatedState() => SessionCall(() => ValidatedActionsManager.Instance.GetState());
        public bool ValidatedActionsSupported => ValidatedRunService.IsSupported;
        public string LastValidatedErrorCode => ValidatedRunService.LastErrorCode;
        public Task<int?> StartValidatedRun(string board) => SessionCall(() => ValidatedRunService.StartRun(board));
        public Task<ValidatedRunOutcome> SubmitValidatedRun(long score, byte[] evidence, string stage, string coinsKey, long earned) => SessionCall(() => ValidatedRunService.Submit(score, evidence, stage, coinsKey, earned));
        public Task<List<UserNewsResponse>> LoadNews() => SessionCall(() => NewsManager.Instance.LoadNews(5, "en"));
        public Task<bool?> ValidateGiftCode(string code) => SessionCall(() => GiftCodeManager.Instance.Validate(code));
        public Task<RedeemGiftCodeResponse> RedeemGiftCode(string code) => SessionCall(() => GiftCodeManager.Instance.Redeem(code));
        public Task<bool> SubmitFeedback(string title, string message, string category, string email = null) => SessionCall(() => FeedbackManager.Instance.Submit(title, category, message, email, includeDeviceInfo: true));
        public Task<SendEmailResponse> SendEmail(string recipient, string template, string language) => SessionCall(() => EmailSendingManager.Instance.SendEmail(recipient, template, new Dictionary<string, string>(), language));
        public async Task LogRunEnd(string message) { if (Online) await UserLogManager.Instance.Info(message); }
        public async Task LogWarning(string message) { if (Online) await UserLogManager.Instance.Warn(message); }
        public void StartCrashCapture() { if (Online) CrashManager.Instance.StartCapture(); }
        public void RecordBreadcrumb(string type, string message) { if (Online) CrashManager.Instance.RecordBreadcrumb(type, message); }
        public void SetCrashCustomKey(string key, string value) { if (Online) CrashManager.Instance.SetCustomKey(key, value); }
        public void RecordException(Exception ex) { if (Online) CrashManager.Instance.RecordException(ex); }
    }
}
