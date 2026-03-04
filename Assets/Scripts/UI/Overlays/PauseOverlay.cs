using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeagullStorm.Managers;
using SeagullStorm.SDK;

namespace SeagullStorm.UI.Overlays
{
    public class PauseOverlay : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newsButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button quitButton;

        [Header("News Panel")]
        [SerializeField] private GameObject newsPanel;
        [SerializeField] private Transform newsContainer;
        [SerializeField] private GameObject newsEntryPrefab;
        [SerializeField] private Button closeNewsButton;

        [Header("Feedback Panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMP_InputField feedbackTitleInput;
        [SerializeField] private TMP_InputField feedbackMessageInput;
        [SerializeField] private TMP_Dropdown feedbackCategoryDropdown;
        [SerializeField] private Button submitFeedbackButton;
        [SerializeField] private Button closeFeedbackButton;
        [SerializeField] private TMP_Text feedbackStatusText;

        private void OnEnable()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (newsButton != null) newsButton.onClick.AddListener(OnShowNews);
            if (feedbackButton != null) feedbackButton.onClick.AddListener(OnShowFeedback);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuit);
            if (closeNewsButton != null) closeNewsButton.onClick.AddListener(() => newsPanel?.SetActive(false));
            if (closeFeedbackButton != null) closeFeedbackButton.onClick.AddListener(() => feedbackPanel?.SetActive(false));
            if (submitFeedbackButton != null) submitFeedbackButton.onClick.AddListener(OnSubmitFeedback);
        }

        private void OnDisable()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveAllListeners();
            if (newsButton != null) newsButton.onClick.RemoveAllListeners();
            if (feedbackButton != null) feedbackButton.onClick.RemoveAllListeners();
            if (quitButton != null) quitButton.onClick.RemoveAllListeners();
            if (closeNewsButton != null) closeNewsButton.onClick.RemoveAllListeners();
            if (closeFeedbackButton != null) closeFeedbackButton.onClick.RemoveAllListeners();
            if (submitFeedbackButton != null) submitFeedbackButton.onClick.RemoveAllListeners();
        }

        private void OnResume()
        {
            RunManager.Instance?.ResumeRun();
            gameObject.SetActive(false);
        }

        private async void OnShowNews()
        {
            if (newsPanel != null) newsPanel.SetActive(true);
            if (feedbackPanel != null) feedbackPanel.SetActive(false);

            var news = await HorizonSDKIntegration.Instance.LoadNews(5, "en");
            if (news == null || newsContainer == null || newsEntryPrefab == null) return;

            foreach (Transform child in newsContainer) Destroy(child.gameObject);

            foreach (var item in news)
            {
                var obj = Instantiate(newsEntryPrefab, newsContainer);
                var text = obj.GetComponent<TMP_Text>();
                if (text != null) text.text = $"* {item.title}";
            }
        }

        private void OnShowFeedback()
        {
            if (feedbackPanel != null) feedbackPanel.SetActive(true);
            if (newsPanel != null) newsPanel.SetActive(false);
        }

        private async void OnSubmitFeedback()
        {
            string title = feedbackTitleInput != null ? feedbackTitleInput.text : "";
            string message = feedbackMessageInput != null ? feedbackMessageInput.text : "";
            string category = "GENERAL";

            if (feedbackCategoryDropdown != null)
            {
                switch (feedbackCategoryDropdown.value)
                {
                    case 0: category = "BUG"; break;
                    case 1: category = "FEATURE"; break;
                    case 2: category = "GENERAL"; break;
                }
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                if (feedbackStatusText != null) feedbackStatusText.text = "Fill in both fields.";
                return;
            }

            if (feedbackStatusText != null) feedbackStatusText.text = "Sending...";
            bool success = await HorizonSDKIntegration.Instance.SubmitFeedback(title, message, category);

            if (feedbackStatusText != null)
                feedbackStatusText.text = success ? "Sent! Thank you." : "Failed to send.";
        }

        private void OnQuit()
        {
            RunManager.Instance?.QuitRun();
        }
    }
}
