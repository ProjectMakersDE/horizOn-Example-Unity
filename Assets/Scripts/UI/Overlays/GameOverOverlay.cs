using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeagullStorm.Managers;
using SeagullStorm.SDK;

namespace SeagullStorm.UI.Overlays
{
    public class GameOverOverlay : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text wavesText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text bestText;

        [Header("Buttons")]
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button hubButton;

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnGameOverTriggered += ShowGameOver;
            if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
            if (hubButton != null) hubButton.onClick.AddListener(OnHub);
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnGameOverTriggered -= ShowGameOver;
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
            if (hubButton != null) hubButton.onClick.RemoveListener(OnHub);
        }

        private async void ShowGameOver()
        {
            gameObject.SetActive(true);

            var run = GameManager.Instance.CurrentRun;
            int coinsEarned = run.currentScore / Mathf.Max(1, GameManager.Instance.Config.coinDivisor);

            if (scoreText != null) scoreText.text = $"Score:    {run.currentScore:N0}";
            if (wavesText != null) wavesText.text = $"Waves:    {run.currentWave}";
            if (levelText != null) levelText.text = $"Level:    {run.currentLevel}";
            if (coinsText != null) coinsText.text = $"Coins:    +{coinsEarned}";

            // Process end-of-run server calls
            await GameManager.Instance.EndRun(run.playerHP > 0);

            // Fetch rank
            var rankData = await HorizonSDKIntegration.Instance.GetPlayerRank();
            if (rankData != null && rankText != null)
            {
                rankText.text = $"Your Rank: #{rankData.position}";
            }

            if (bestText != null)
            {
                bestText.text = $"Best:      {GameManager.Instance.PlayerData.highscore:N0}";
                if (rankData != null)
                    bestText.text += $" (#{rankData.position})";
            }
        }

        private void OnPlayAgain()
        {
            gameObject.SetActive(false);
            GameManager.Instance.StartRun();
        }

        private void OnHub()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance.GoToHub();
        }
    }
}
