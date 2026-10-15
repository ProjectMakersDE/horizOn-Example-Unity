using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeagullStorm
{
    /// <summary>
    /// Central state machine for the game. Controls which canvas is active
    /// and manages transitions between Hub, Run, Paused, LevelUp, and GameOver.
    /// Lives in GameScene (build index 2).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Canvases")]
        [SerializeField] private GameObject hubCanvas;
        [SerializeField] private GameObject runHUDCanvas;
        [SerializeField] private GameObject levelUpCanvas;
        [SerializeField] private GameObject pauseCanvas;
        [SerializeField] private GameObject gameOverCanvas;

        [Header("Gameplay Objects")]
        [SerializeField] private GameObject player;
        [SerializeField] private GameObject tilemap;

        public GameState CurrentState { get; private set; } = GameState.Hub;
        public SaveData Save { get; set; }
        public GameConfig Config { get; set; }
        public RunState RunState { get; private set; } = new RunState();

        public event Action<GameState> OnStateChanged;
        public event Action OnSaveDataChanged;

        public bool ProgressLoaded { get; set; }
        public int RunGeneration { get; private set; }
        public bool IsStartingRun => _startingRun;
        public bool RunSubmissionPending { get; private set; }
        public string SubmissionStatus { get; private set; } = "Offline practice • no score submitted";
        public bool Victory { get; private set; }
        public bool IsValidatedRun => _validatedRun;
        public string RunMode => _validatedRun ? "VALIDATED RUN" : HorizonManager.Instance?.Online == true && !Config.ValidatedRunsEnabled ? "ONLINE RUN" : "PRACTICE RUN";
        private int _consecutiveWave1Deaths;
        private bool _runEnded;
        public bool ScoreSubmitted { get; private set; }

        // Validated Actions: whether the current run has a ticket, and its input log.
        private readonly RunInputLog _inputLog = new RunInputLog();
        private bool _validatedRun;
        private string _validatedStartError;
        private bool _startingRun;
        private StatisticsIntentStore _statisticsRun;

        /// <summary>Result of the last validated submit, null when the run used the plain submit.</summary>
        public ValidatedRunOutcome LastValidatedOutcome { get; private set; }

        /// <summary>True when Remote Config enables Validated Actions and the SDK supports it.</summary>
        public bool UseValidatedActions =>
            Config.ValidatedRunsEnabled && (HorizonManager.Instance?.ValidatedActionsSupported ?? false);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Save = SaveData.CreateDefault();
            Config = new GameConfig();
        }

        private void Start()
        {
            SeagullArt.CreateArena(tilemap != null ? tilemap.transform : null);
            ChangeState(GameState.Hub);
        }

        private void FixedUpdate()
        {
            // Input log of a validated run: one physics tick per FixedUpdate while the run is playing.
            if (!_validatedRun || CurrentState != GameState.Run) return;
            _inputLog.RecordMove(PlayerController.Instance != null ? PlayerController.Instance.MoveDirection : Vector2.zero);
        }

        public void ChangeState(GameState newState)
        {
            CurrentState = newState;

            // Activate/deactivate canvases
            SetCanvasActive(hubCanvas, newState == GameState.Hub);
            SetCanvasActive(runHUDCanvas, newState == GameState.Run || newState == GameState.Paused || newState == GameState.LevelUp);
            SetCanvasActive(pauseCanvas, newState == GameState.Paused);
            SetCanvasActive(levelUpCanvas, newState == GameState.LevelUp);
            SetCanvasActive(gameOverCanvas, newState == GameState.GameOver);

            // Gameplay objects
            bool inRun = newState == GameState.Run || newState == GameState.Paused || newState == GameState.LevelUp || newState == GameState.GameOver;
            if (player != null) player.SetActive(inRun);
            if (tilemap != null) tilemap.SetActive(inRun);

            // Time scale
            switch (newState)
            {
                case GameState.Hub:
                case GameState.Run:
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                case GameState.LevelUp:
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }

            HorizonManager.Instance?.RecordBreadcrumb("navigation", $"entered_{newState}");
            OnStateChanged?.Invoke(newState);
        }

        public async void StartRun()
        {
            if (_startingRun || RunSubmissionPending) return; // the run ticket is still being requested (double click)
            _startingRun = true;
            try
            {
                // Validated Actions: get a single-use ticket and its seed before the run starts.
                LastValidatedOutcome = null;
                _runEnded = false;
                RunGeneration++;
                Victory = false;
                SubmissionStatus = "Playing";
                _validatedStartError = null;
                _validatedRun = HorizonManager.Instance?.Online == true && UseValidatedActions && await StartValidatedRun();
                if (HorizonManager.Instance?.Online != true) _validatedStartError = "OFFLINE_PRACTICE";
                else if (Config.ValidatedRunsEnabled && !_validatedRun) _validatedStartError ??= "VALIDATION_UNAVAILABLE";
                if (!_validatedRun)
                {
                    if (_validatedStartError != null && _validatedStartError != "OFFLINE_PRACTICE")
                    {
                        HorizonManager.Instance.EnterPractice();
                        Config = new GameConfig();
                        LoadLocalProgress();
                    }
                    UnityEngine.Random.InitState(Environment.TickCount);
                }
            }
            finally
            {
                _startingRun = false;
            }

            float maxHP = Config.UpgradeHpValues[
                Mathf.Clamp(Save.upgrades.hp, 0, Config.UpgradeHpValues.Length - 1)];
            RunState.Reset(maxHP, Config.RunDurationSeconds);
            _statisticsRun = HorizonManager.Instance?.Online == true ? StatisticsIntentStore.Current() : null;

            // Reset player position
            if (player != null) player.transform.position = Vector3.zero;

            // Clear old weapons and initialize default
            WeaponManager.Instance?.ClearWeapons();
            WeaponManager.Instance?.InitializeDefaultWeapon(player);

            ScoreSubmitted = false;

            EnemyPool.Instance?.ReturnAll();
            PickupPool.Instance?.ReturnAll();
            ProjectilePool.Instance?.ReturnAll();
            SpawnManager.Instance?.ResetForNewRun();

            ChangeState(GameState.Run);
            AudioManager.Instance?.PlayBattleMusic();
        }

        public async void EndRun(bool victory = false)
        {
            if (_runEnded || CurrentState != GameState.Run) return;
            _runEnded = true;
            Victory = victory;
            RunSubmissionPending = true;
            ChangeState(GameState.GameOver);
            AudioManager.Instance?.PlayGameOver();
            AudioManager.Instance?.PlayMenuMusic();
            var run = RunState;
            // Freeze one completed online session locally; the statistics panel sends it explicitly.
            // Later runs aggregate separately while an earlier immutable request is unresolved.
            try { _statisticsRun?.CompleteRun(run.kills, run.score); }
            catch (Exception error) { Debug.LogWarning("Statistics intent could not be saved: " + error.Message); }
            _statisticsRun = null;
            run.coinsEarned = run.score / Mathf.Max(1, Config.CoinDivisor);
            Save.coins += run.coinsEarned;
            Save.totalRuns++;

            if (run.score > Save.highscore)
                Save.highscore = run.score;
            PersistLocalProgress();

            // Check balance warning
            if (run.wave <= 1 && run.playerHP <= 0)
            {
                _consecutiveWave1Deaths++;
                if (_consecutiveWave1Deaths >= 3)
                {
                    try { await HorizonManager.Instance.LogWarning(
                        "Player died in wave 1 three consecutive times - possible balancing issue"); }
                    catch (System.Exception ex) { HorizonManager.Instance?.RecordException(ex); }
                    _consecutiveWave1Deaths = 0;
                }
            }
            else
            {
                _consecutiveWave1Deaths = 0;
            }

            // Submit score (validated when the run has a ticket, plain otherwise)
            try { await SubmitRunScore(run); } catch (System.Exception ex) { SubmissionStatus = "Submission failed: " + ex.Message; HorizonManager.Instance?.RecordException(ex); }
            ScoreSubmitted = true;

            // Save cloud data
            try
            {
                string json = JsonUtility.ToJson(Save);
                await HorizonManager.Instance.SaveCloudData(json);
            }
            catch (System.Exception ex) { HorizonManager.Instance?.RecordException(ex); }

            // Log run
            try
            {
                string msg = $"Run ended | Waves: {run.wave} | Level: {run.level} | Score: {run.score} | " +
                             $"Duration: {run.FormatDuration()} | " +
                             $"Upgrades: speed:{Save.upgrades.speed},dmg:{Save.upgrades.damage},hp:{Save.upgrades.hp} | " +
                             $"Coins earned: {run.coinsEarned}";
                await HorizonManager.Instance.LogRunEnd(msg);
            }
            catch (System.Exception ex) { HorizonManager.Instance?.RecordException(ex); }

            PersistLocalProgress();
            RunSubmissionPending = false;
            OnSaveDataChanged?.Invoke();
        }

        // ===== Validated Actions =====

        private async Task<bool> StartValidatedRun()
        {
            try
            {
                int? seed = await HorizonManager.Instance.StartValidatedRun(HorizonManager.Instance.ActiveBoard);
                if (seed == null)
                {
                    // Missing tickets make this a practice run; no plain score fallback.
                    _validatedStartError = HorizonManager.Instance.LastValidatedErrorCode;
                    Debug.LogWarning($"[SeagullStorm] Validated run not started: {_validatedStartError}");
                    return false;
                }

                // Seed the run; physics/input logs are evidence, not authoritative replay verification.
                UnityEngine.Random.InitState(seed.Value);
                _inputLog.Begin(seed.Value);
                return true;
            }
            catch (Exception ex)
            {
                HorizonManager.Instance?.RecordException(ex);
                return false;
            }
        }

        private async Task SubmitRunScore(RunState run)
        {
            bool validated = _validatedRun;
            _validatedRun = false;

            if (!validated)
            {
                // A missing ticket never downgrades a requested validated run to a normal score.
                if (_validatedStartError != null || HorizonManager.Instance?.Online != true)
                {
                    SubmissionStatus = _validatedStartError == "OFFLINE_PRACTICE" ? "Practice score saved on this device" : "Practice only • ticket unavailable: " + _validatedStartError;
                    LastValidatedOutcome = new ValidatedRunOutcome { Accepted = false, ErrorCode = _validatedStartError ?? "OFFLINE_PRACTICE" };
                    return;
                }
                bool submitted = await HorizonManager.Instance.SubmitScore(run.score);
                SubmissionStatus = submitted ? "Score submitted" : "Not ranked: " + HorizonManager.Instance.LastLeaderboardError;
                if (!submitted) LastValidatedOutcome = new ValidatedRunOutcome { ErrorCode = HorizonManager.Instance.LastLeaderboardError };
                return;
            }

            byte[] inputLog = _inputLog.Finish();
            if (_inputLog.Truncated)
            {
                ValidatedRunService.Discard();
                LastValidatedOutcome = new ValidatedRunOutcome { ErrorCode = "INCOMPLETE_EVIDENCE" };
                SubmissionStatus = "Not ranked: input evidence limit reached";
                return;
            }

            // A rejected validated run does not fall back to the plain submit.
            long coins = Config.ValidatedRunsSendCoins ? run.coinsEarned : 0;
            SubmissionStatus = "Waiting for server validation…";
            LastValidatedOutcome = await HorizonManager.Instance.SubmitValidatedRun(
                run.score, inputLog, $"wave_{run.wave}", GameConfig.ValidatedCoinsKey, coins);

            SubmissionStatus = LastValidatedOutcome.Accepted ? "Accepted by server" : "Rejected: " + LastValidatedOutcome.ErrorCode;
            if (!LastValidatedOutcome.Accepted)
                Debug.LogWarning($"[SeagullStorm] Validated run rejected: {LastValidatedOutcome.ErrorCode}");
        }

        /// <summary>Records the level up button (0 based) the player picked in the input log of a validated run.</summary>
        public void RecordLevelUpChoice(int buttonIndex)
        {
            if (_validatedRun) _inputLog.RecordLevelUpChoice(buttonIndex);
        }

        public bool TryPurchaseUpgrade(string upgradeType)
        {
            if (RunSubmissionPending || !ProgressLoaded || CurrentState != GameState.Hub) return false;
            int currentLevel;
            int[] costs;
            int maxLevel;

            switch (upgradeType)
            {
                case "speed":
                    currentLevel = Save.upgrades.speed;
                    costs = Config.UpgradeSpeedCosts;
                    maxLevel = costs.Length;
                    break;
                case "damage":
                    currentLevel = Save.upgrades.damage;
                    costs = Config.UpgradeDamageCosts;
                    maxLevel = costs.Length;
                    break;
                case "hp":
                    currentLevel = Save.upgrades.hp;
                    costs = Config.UpgradeHpCosts;
                    maxLevel = costs.Length;
                    break;
                case "magnet":
                    currentLevel = Save.upgrades.magnet;
                    costs = Config.UpgradeMagnetCosts;
                    maxLevel = costs.Length;
                    break;
                default:
                    return false;
            }

            if (currentLevel >= maxLevel) return false;

            int cost = costs[currentLevel];
            if (Save.coins < cost) return false;

            Save.coins -= cost;

            switch (upgradeType)
            {
                case "speed": Save.upgrades.speed++; break;
                case "damage": Save.upgrades.damage++; break;
                case "hp": Save.upgrades.hp++; break;
                case "magnet": Save.upgrades.magnet++; break;
            }

            HorizonManager.Instance?.RecordBreadcrumb("user_action", $"bought_{upgradeType}_{currentLevel + 1}");
            PersistLocalProgress();
            OnSaveDataChanged?.Invoke();
            _ = HorizonManager.Instance?.SaveCloudData(JsonUtility.ToJson(Save));
            return true;
        }

        public float GetSpeedMultiplier()
        {
            return Config.UpgradeSpeedValues[
                Mathf.Clamp(Save.upgrades.speed, 0, Config.UpgradeSpeedValues.Length - 1)];
        }

        public float GetDamageMultiplier()
        {
            return Config.UpgradeDamageValues[
                Mathf.Clamp(Save.upgrades.damage, 0, Config.UpgradeDamageValues.Length - 1)];
        }

        public float GetPlayerMaxHP()
        {
            return Config.UpgradeHpValues[
                Mathf.Clamp(Save.upgrades.hp, 0, Config.UpgradeHpValues.Length - 1)];
        }

        public float GetPickupRadius()
        {
            return Config.UpgradeMagnetValues[
                Mathf.Clamp(Save.upgrades.magnet, 0, Config.UpgradeMagnetValues.Length - 1)];
        }

        private string _progressKey;
        private string ProgressKey => _progressKey ?? "seagull-storm.progress.practice";
        public void LoadLocalProgress()
        {
            _progressKey = "seagull-storm.progress." + (SeagullSmoke.Enabled ? "qa." : "") + (HorizonManager.Instance?.GetUserId() ?? "practice");
            try
            {
                Save = PlayerPrefs.HasKey(ProgressKey) ? JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(ProgressKey)) : SaveData.CreateDefault();
                if (Save == null || Save.upgrades == null) Save = SaveData.CreateDefault();
                Save.giftCodesRedeemed ??= new System.Collections.Generic.List<string>();
            }
            catch (Exception) { Save = SaveData.CreateDefault(); }
        }
        public void PersistLocalProgress()
        {
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(Save));
            PlayerPrefs.Save();
        }
        public void NotifyProgressChanged() { PersistLocalProgress(); OnSaveDataChanged?.Invoke(); }

        public void SignOutAndReturn()
        {
            if (RunSubmissionPending || _startingRun) return;
            HorizonManager.Instance?.SignOut();
            SceneManager.LoadScene("TitleScene");
        }

        private void SetCanvasActive(GameObject canvas, bool active)
        {
            if (canvas != null) canvas.SetActive(active);
        }
    }
}
