using UnityEngine;
using UnityEngine.UI;
using PM.horizOn.Cloud.Objects.Network.Responses;

namespace SeagullStorm
{
    /// <summary>
    /// Pause menu: resume, news, feedback, quit.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newsButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private GameObject newsPanel;
        [SerializeField] private FeedbackForm feedbackForm;
        [SerializeField] private Transform newsContentContainer;
        [SerializeField] private GameObject newsEntryPrefab;

        private void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (newsButton != null) newsButton.onClick.AddListener(OnNews);
            if (feedbackButton != null) feedbackButton.onClick.AddListener(OnFeedback);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
        }

        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (newsButton != null) newsButton.onClick.RemoveAllListeners();
            if (feedbackButton != null) feedbackButton.onClick.RemoveAllListeners();
            if (quitButton != null) quitButton.onClick.RemoveAllListeners();
        }

        private void OnResume()
        {
            GameManager.Instance?.ChangeState(GameState.Run);
        }

        private async void OnNews()
        {
            if (newsPanel != null) newsPanel.SetActive(true);
            if (feedbackForm != null) feedbackForm.gameObject.SetActive(false);

            // Use cached news from hub, or fetch fresh if not available
            var news = HubController.CachedNews;
            if (news == null)
            {
                news = await HorizonManager.Instance.LoadNews();
            }

            // Populate news panel
            if (news != null && newsContentContainer != null && newsEntryPrefab != null)
            {
                foreach (Transform child in newsContentContainer) Object.Destroy(child.gameObject);

                foreach (var item in news)
                {
                    var obj = Instantiate(newsEntryPrefab, newsContentContainer);
                    var comp = obj.GetComponent<NewsEntry>();
                    if (comp != null)
                        comp.Setup(item.title, item.releaseDate);
                }
            }
        }

        private void OnFeedback()
        {
            if (feedbackForm != null) feedbackForm.gameObject.SetActive(true);
            if (newsPanel != null) newsPanel.SetActive(false);
        }

        private void OnQuit()
        {
            // Quit run = trigger game over
            PlayerController.Instance?.TakeDamage(99999);
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Paused) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnResume();
            }
        }
    }
}
