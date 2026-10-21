using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Enums;
using PM.horizOn.Cloud.Manager;
using PM.horizOn.Cloud.Objects.Network.Requests;
using PM.horizOn.Cloud.Objects.Network.Responses;

namespace SeagullStorm
{
    /// <summary>
    /// Result of a validated submit in game terms, independent of the SDK version.
    /// </summary>
    public class ValidatedRunOutcome
    {
        /// <summary>True when the server accepted the run and wrote the score.</summary>
        public bool Accepted;

        /// <summary>Stable error code when not accepted, for example DURATION_TOO_SHORT. Null when accepted.</summary>
        public string ErrorCode;

        /// <summary>Rank on the leaderboard after the run (accepted runs only).</summary>
        public long Rank;

        /// <summary>Exact score returned by the accepted SDK submit.</summary>
        public long Score;

        /// <summary>Best score of the player on the leaderboard (accepted runs only).</summary>
        public long BestScore;

        /// <summary>True when the run set a new personal best (accepted runs only).</summary>
        public bool IsNewHighScore;

        /// <summary>True when the run sent earned coins and the server answered with the coin state.</summary>
        public bool CoinsReported;

        /// <summary>Coins the server actually credited (can be lower than earned because of a daily cap).</summary>
        public long CoinsCredited;

        /// <summary>Server-owned coin balance after the run.</summary>
        public long CoinBalance;

        /// <summary>True when the server asked for the input log; the SDK uploads it in the background.</summary>
        public bool EvidenceRequested;
    }

    /// <summary>
    /// Validated Actions for Seagull Storm: start a server-checked run and submit its result.
    ///
    /// This assembly targets the pinned official horizOn SDK source snapshot. The real calls are
    /// available in that snapshot; it is not a claim that every feature has been released.
    /// The first SDK release with
    /// Validated Actions is 1.9.0 (TASK-883).
    /// Requested validated runs never fall back to plain score submissions.
    /// </summary>
    public static class ValidatedRunService
    {
        /// <summary>Local code when the installed SDK has no Validated Actions yet.</summary>
        public const string SdkTooOld = "SDK_TOO_OLD";

        /// <summary>True when the installed SDK provides Validated Actions.</summary>
        public static bool IsSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>Error code of the last failed start or submit, null after a success.</summary>
        public static string LastErrorCode { get; private set; }
        private static RunEvidenceStatus _evidence = new RunEvidenceStatus();
        public static string EvidenceStatus => _evidence.Status;
        public static void Discard() { ValidatedActionsManager.Instance.DiscardRun(); }

        // Static state survives "Enter Play Mode" without a domain reload; start clean every time.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LastErrorCode = null;
            _evidenceEventsSubscribed = false;
            _evidence = new RunEvidenceStatus();
        }

        /// <summary>
        /// Starts a validated run bound to <paramref name="leaderboardKey"/>.
        /// </summary>
        /// <returns>The server seed of the run, or null when no run was started (see <see cref="LastErrorCode"/>)</returns>
        public static Task<int?> StartRun(string leaderboardKey)
        {
            return StartRunAsync(leaderboardKey);
        }

        /// <summary>
        /// Submits score, stage and input log of the current run to the board of its ticket.
        /// When <paramref name="coinsKey"/> is set
        /// and <paramref name="coinsEarned"/> is positive, the coins are sent as a server-owned value
        /// (the key must exist in the rules of the API key, otherwise the whole run is rejected).
        /// The SDK uploads the input log on its own when the server asks for it.
        /// </summary>
        public static Task<ValidatedRunOutcome> Submit(long score, byte[] inputLog, string stage, string coinsKey, long coinsEarned)
        {
            return SubmitAsync(score, inputLog, stage, coinsKey, coinsEarned);
        }

        private static bool _evidenceEventsSubscribed;

        private static async Task<int?> StartRunAsync(string leaderboardKey)
        {
            _evidence.Begin(null);
            SubscribeEvidenceEvents();

            var manager = ValidatedActionsManager.Instance;
            // Upload the input log automatically when the server asks for it (top N or flagged run).
            manager.AutoUploadEvidence = true;

            ValidatedRun run = await manager.StartRun(leaderboardKey);
            if (run == null)
            {
                // For example RUN_RATE_LIMITED, LEADERBOARD_NOT_FOUND or NOT_SUPPORTED (simpleServer).
                LastErrorCode = manager.LastErrorCode;
                return null;
            }

            LastErrorCode = null;
            _evidence.Begin(run.runId);
            return run.seed;
        }

        private static async Task<ValidatedRunOutcome> SubmitAsync(long score, byte[] inputLog, string stage, string coinsKey, long coinsEarned)
        {
            var manager = ValidatedActionsManager.Instance;

            bool sendCoins = !string.IsNullOrEmpty(coinsKey) && coinsEarned > 0;
            List<EarnedValue> earned = sendCoins
                ? new List<EarnedValue> { new EarnedValue(coinsKey, coinsEarned) }
                : null;

            ValidatedSubmitResult result = await manager.SubmitValidated(score, inputLog, stage, earned: earned);
            if (result == null)
            {
                LastErrorCode = manager.LastErrorCode;
                // Rule rejections already used up the ticket. A kept run (network error, 429,
                // PLAYER_BANNED) is not retried from the game over screen, so drop it here.
                manager.DiscardRun();
                return new ValidatedRunOutcome { Accepted = false, ErrorCode = LastErrorCode };
            }

            LastErrorCode = null;
            _evidence.Requested(result.evidence.required);
            var outcome = new ValidatedRunOutcome
            {
                Accepted = true,
                Rank = result.rank,
                Score = result.score,
                BestScore = result.bestScore,
                IsNewHighScore = result.isNewHighScore,
                EvidenceRequested = result.evidence.required
            };

            if (sendCoins)
            {
                PlayerStateValue coins = result.state.GetValue(coinsKey);
                if (coins != null)
                {
                    outcome.CoinsReported = true;
                    outcome.CoinsCredited = coins.credited;
                    outcome.CoinBalance = coins.balance;
                }
            }

            return outcome;
        }

        private static void SubscribeEvidenceEvents()
        {
            if (_evidenceEventsSubscribed) return;
            _evidenceEventsSubscribed = true;

            HorizonApp.Events.Subscribe<EvidenceUploadResult>(EventKeys.ValidatedEvidenceUploaded,
                uploaded => { _evidence.Resolve(uploaded.runId, "Evidence uploaded (" + uploaded.bytes + " bytes)"); Debug.Log("[SeagullStorm] " + EvidenceStatus); });
            HorizonApp.Events.Subscribe<ValidatedEvidenceFailure>(EventKeys.ValidatedEvidenceUploadFailed,
                failure => { _evidence.Resolve(failure.runId, "Evidence upload failed: " + failure.code + " • accepted score retained"); Debug.LogWarning("[SeagullStorm] " + EvidenceStatus); });
        }
    }
}
