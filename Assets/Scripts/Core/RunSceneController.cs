using UnityEngine;
using SeagullStorm.Gameplay.Pickups;
using SeagullStorm.Managers;
using SeagullStorm.UI.Overlays;

namespace SeagullStorm.Core
{
    public class RunSceneController : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject xpPickupPrefab;

        [Header("Overlays")]
        [SerializeField] private PauseOverlay pauseOverlay;
        [SerializeField] private LevelupOverlay levelupOverlay;
        [SerializeField] private GameOverOverlay gameOverOverlay;

        private void Awake()
        {
            // Register XP pickup prefab
            XPPickup.SetPrefab(xpPickupPrefab);
        }

        private void Start()
        {
            // Hide all overlays
            if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
            if (levelupOverlay != null) levelupOverlay.gameObject.SetActive(false);
            if (gameOverOverlay != null) gameOverOverlay.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (RunManager.Instance == null) return;

                if (RunManager.Instance.IsPaused)
                {
                    RunManager.Instance.ResumeRun();
                    if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(false);
                }
                else if (!RunManager.Instance.IsLevelingUp && !RunManager.Instance.IsGameOver)
                {
                    RunManager.Instance.PauseRun();
                    if (pauseOverlay != null) pauseOverlay.gameObject.SetActive(true);
                }
            }
        }
    }
}
