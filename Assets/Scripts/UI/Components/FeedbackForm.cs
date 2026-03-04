using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Reusable feedback form (used in hub + pause).
    /// </summary>
    public class FeedbackForm : MonoBehaviour
    {
        [SerializeField] private TMP_InputField titleInput;
        [SerializeField] private TMP_InputField messageInput;
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text statusText;

        private void OnEnable()
        {
            if (submitButton != null) submitButton.onClick.AddListener(OnSubmit);
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
        }

        private async void OnSubmit()
        {
            string title = titleInput != null ? titleInput.text : "";
            string msg = messageInput != null ? messageInput.text : "";
            string category = "GENERAL";

            if (categoryDropdown != null)
            {
                var options = categoryDropdown.options;
                if (categoryDropdown.value < options.Count)
                    category = options[categoryDropdown.value].text;
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(msg))
            {
                SetStatus("Fill in title and message.");
                return;
            }

            SetStatus("Sending...");
            bool success = await HorizonManager.Instance.SubmitFeedback(title, msg, category);

            if (success)
            {
                SetStatus("Sent! Thank you.");
                if (titleInput != null) titleInput.text = "";
                if (messageInput != null) messageInput.text = "";
            }
            else
            {
                SetStatus("Failed to send.");
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }
}
