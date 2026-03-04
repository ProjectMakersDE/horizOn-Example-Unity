using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Game over screen: score display, rank fetch, play again/hub buttons.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text wavesText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinsEarnedText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text bestScoreText;
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

            // Wait for score submission to complete before fetching rank
            while (GameManager.Instance != null && !GameManager.Instance.ScoreSubmitted)
                await Task.Yield();

            // Fetch rank
            try
            {
                var rank = await HorizonManager.Instance.GetRank();
                if (rank != null && rankText != null)
                    rankText.text = $"Your Rank: #{rank.position}";
            }
            catch
            {
                if (rankText != null) rankText.text = "Rank: --";
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
