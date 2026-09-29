using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Game over screen: score display, rank fetch, play again/hub buttons.
    /// For validated runs it shows the verified rank or a friendly reason why the run was not ranked.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text wavesText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinsEarnedText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text bestScoreText;
        [Tooltip("Optional line for the Validated Actions result; falls back to the rank line when empty.")]
        [SerializeField] private TMP_Text validationText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button hubButton;

        private void OnEnable()
        {
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (hubButton != null) hubButton.onClick.AddListener(OnHub);

            ShowStats();
        }

        private void OnDisable()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
            if (hubButton != null) hubButton.onClick.RemoveListener(OnHub);
        }

        private async void ShowStats()
        {
            var run = GameManager.Instance?.RunState;
            var save = GameManager.Instance?.Save;
            if (run == null || save == null) return;

            if (scoreText != null) scoreText.text = $"Score: {run.score:N0}";
            if (wavesText != null) wavesText.text = $"Waves: {run.wave}";
            if (levelText != null) levelText.text = $"Level: {run.level}";
            if (coinsEarnedText != null) coinsEarnedText.text = $"Coins: +{run.coinsEarned}";

            if (bestScoreText != null)
                bestScoreText.text = $"Best: {save.highscore:N0}";
            if (validationText != null)
                validationText.text = "";

            // Wait for score submission to complete before fetching rank
            while (GameManager.Instance != null && !GameManager.Instance.ScoreSubmitted)
                await Task.Yield();

            // Validated run: the submit result already carries the rank (or the rejection reason).
            var validated = GameManager.Instance?.LastValidatedOutcome;
            if (validated != null)
            {
                ShowValidatedOutcome(validated, run, save);
                return;
            }

            // Fetch rank
            try
            {
                var rank = await HorizonManager.Instance.GetRank();
                if (rank != null)
                {
                    if (rankText != null)
                        rankText.text = $"Your Rank: #{rank.position}";
                    if (bestScoreText != null)
                        bestScoreText.text = $"Best: {save.highscore:N0} (#{rank.position})";
                }
            }
            catch
            {
                if (rankText != null) rankText.text = "Rank: --";
            }
        }

        private void ShowValidatedOutcome(ValidatedRunOutcome outcome, RunState run, SaveData save)
        {
            if (!outcome.Accepted)
            {
                string reason = FriendlyRejectionReason(outcome.ErrorCode);
                if (validationText != null)
                {
                    validationText.text = reason;
                    if (rankText != null) rankText.text = "Rank: --";
                }
                else if (rankText != null)
                {
                    rankText.text = reason;
                }
                return;
            }

            if (rankText != null)
                rankText.text = outcome.Rank > 0 ? $"Verified Rank: #{outcome.Rank}" : "Verified run";
            if (bestScoreText != null && outcome.Rank > 0)
                bestScoreText.text = $"Best: {System.Math.Max(save.highscore, outcome.BestScore):N0} (#{outcome.Rank})";
            if (coinsEarnedText != null && outcome.CoinsReported && outcome.CoinsCredited < run.coinsEarned)
                coinsEarnedText.text = $"Coins: +{run.coinsEarned} ({outcome.CoinsCredited} verified)";
            if (validationText != null)
                validationText.text = outcome.IsNewHighScore ? "New verified best!" : "Run verified by the server";
        }

        /// <summary>
        /// Maps the stable Validated Actions error codes to short, friendly lines. The code itself is
        /// logged to the console by GameManager.
        /// </summary>
        private static string FriendlyRejectionReason(string code)
        {
            switch (code)
            {
                case "DURATION_TOO_SHORT":
                    return "Not ranked: that run was too short to count.";
                case "SCORE_ABOVE_MAX":
                case "STAGE_SCORE_ABOVE_MAX":
                case "SCORE_RATE_TOO_HIGH":
                    return "Not ranked: the score could not be verified.";
                case "SCORE_BELOW_MIN":
                case "STAGE_SCORE_BELOW_MIN":
                    return "Not ranked: score too low for the leaderboard.";
                case "STAGE_REQUIRED":
                case "STAGE_UNKNOWN":
                    return "Not ranked: this wave is not allowed by the rules.";
                case "LEADERBOARD_NOT_FOUND":
                case "LEADERBOARD_MISMATCH":
                    return "Not ranked: the leaderboard is not set up.";
                case "UNKNOWN_VALUE_KEY":
                case "DUPLICATE_VALUE_KEY":
                case "EARNED_ABOVE_MAX":
                case "EARNED_BELOW_MIN":
                case "INSUFFICIENT_BALANCE":
                    return "Not ranked: the coin reward could not be verified.";
                case "TICKET_EXPIRED":
                    return "Not ranked: the run ticket expired. Play again!";
                case "TICKET_INVALID":
                case "TICKET_FOREIGN":
                case "TICKET_CONSUMED":
                case "NO_ACTIVE_RUN":
                    return "Not ranked: no valid run ticket. Play again!";
                case "RUN_RATE_LIMITED":
                case "RUN_CAPACITY_REACHED":
                case "API_RATE_LIMITED":
                    return "Not ranked: too many runs right now. Try again later.";
                case "PLAYER_BANNED":
                    return "Not ranked: this account cannot join the leaderboard.";
                case "PLAYER_NAME_REQUIRED":
                    return "Not ranked: set a display name to join the leaderboard.";
                case "SESSION_REQUIRED":
                case "SESSION_FORBIDDEN":
                    return "Not ranked: sign in again to submit verified scores.";
                case "SCORE_LIMIT_REACHED":
                    return "Not ranked: the leaderboard is full right now.";
                case "NOT_SUPPORTED":
                case "VALIDATED_ACTIONS_UNAVAILABLE":
                    return "Not ranked: run validation is not available on this server.";
                case "NETWORK_ERROR":
                    return "Not ranked: no connection to the server.";
                default:
                    return "Not ranked: the run could not be verified (offline?).";
            }
        }

        private void OnPlayAgain()
        {
            GameManager.Instance?.StartRun();
        }

        private void OnHub()
        {
            Time.timeScale = 1f;
            GameManager.Instance?.ChangeState(GameState.Hub);
        }
    }
}
