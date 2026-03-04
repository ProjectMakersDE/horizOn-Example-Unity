using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeagullStorm.Managers;

namespace SeagullStorm.UI.Screens
{
    public class RunHUD : MonoBehaviour
    {
        [Header("Top Bar")]
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text levelText;

        [Header("HP Bar")]
        [SerializeField] private Image hpBarFill;

        [Header("XP Bar")]
        [SerializeField] private Image xpBarFill;
        [SerializeField] private TMP_Text xpPercentText;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        private void OnEnable()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunTimerTick += Refresh;
                RunManager.Instance.OnScoreChanged += Refresh;
                RunManager.Instance.OnHPChanged += Refresh;
                RunManager.Instance.OnXPChanged += Refresh;
            }

            if (pauseButton != null) pauseButton.onClick.AddListener(OnPause);
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunTimerTick -= Refresh;
                RunManager.Instance.OnScoreChanged -= Refresh;
                RunManager.Instance.OnHPChanged -= Refresh;
                RunManager.Instance.OnXPChanged -= Refresh;
            }

            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPause);
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            var run = GameManager.Instance.CurrentRun;

            if (waveText != null) waveText.text = $"Wave {run.currentWave}";
            if (scoreText != null) scoreText.text = $"Score: {run.currentScore:N0}";
            if (levelText != null) levelText.text = $"Lv. {run.currentLevel}";

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(Mathf.Max(0, run.timeRemaining) / 60f);
                int seconds = Mathf.FloorToInt(Mathf.Max(0, run.timeRemaining) % 60f);
                timerText.text = $"{minutes}:{seconds:D2}";
            }

            if (hpBarFill != null && run.playerMaxHP > 0)
                hpBarFill.fillAmount = run.playerHP / run.playerMaxHP;

            if (xpBarFill != null && run.xpToNextLevel > 0)
                xpBarFill.fillAmount = run.xpCurrent / run.xpToNextLevel;

            if (xpPercentText != null && run.xpToNextLevel > 0)
                xpPercentText.text = $"{Mathf.RoundToInt(run.xpCurrent / run.xpToNextLevel * 100f)}%";
        }

        private void OnPause()
        {
            RunManager.Instance?.PauseRun();
        }
    }
}
