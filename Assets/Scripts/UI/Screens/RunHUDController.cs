using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// In-run HUD: HP bar, XP bar, wave, timer, score, level display.
    /// </summary>
    public class RunHUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private HPBar hpBar;
        [SerializeField] private XPBar xpBar;
        [SerializeField] private Button pauseButton;

        private void OnEnable()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(OnPause);
        }

        private void OnDisable()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPause);
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            if (Input.GetKeyDown(KeyCode.Escape) && GameManager.Instance.CurrentState == GameState.Run)
            {
                GameManager.Instance.ChangeState(GameState.Paused);
                return;
            }

            var run = GameManager.Instance.RunState;

            if (waveText != null) waveText.text = $"Wave {run.wave}";
            if (scoreText != null) scoreText.text = $"Score: {run.score:N0}";
            if (levelText != null) levelText.text = $"Lv. {run.level}";

            if (timerText != null)
            {
                int m = Mathf.FloorToInt(Mathf.Max(0, run.timeRemaining) / 60f);
                int s = Mathf.FloorToInt(Mathf.Max(0, run.timeRemaining) % 60f);
                timerText.text = $"{m}:{s:D2}";
            }

            if (hpBar != null) hpBar.SetValue(run.playerHP, run.playerMaxHP);
            if (xpBar != null) xpBar.SetValue(run.xpCurrent, run.xpToNextLevel);
        }

        private void OnPause()
        {
            GameManager.Instance?.ChangeState(GameState.Paused);
        }
    }
}
