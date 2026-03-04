using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeagullStorm.Managers;
using SeagullStorm.SDK;

namespace SeagullStorm.UI.Screens
{
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Buttons")]
        [SerializeField] private Button playAsGuestButton;
        [SerializeField] private Button signInEmailButton;
        [SerializeField] private Button createAccountButton;

        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject emailPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private bool _isEmailMode;
        private bool _isCreateMode;

        private async void Start()
        {
            AudioManager.Instance?.PlayMenuMusic();

            ShowLoading("Connecting...");

            bool sdkReady = await HorizonSDKIntegration.Instance.InitializeSDK();
            if (!sdkReady)
            {
                ShowStatus("Failed to connect. Check internet connection.");
                ShowMain();
                return;
            }

            ShowLoading("Restoring session...");
            bool restored = await HorizonSDKIntegration.Instance.RestoreSession();
            if (restored)
            {
                GoToHub();
                return;
            }

            ShowMain();
        }

        private void OnEnable()
        {
            if (playAsGuestButton != null)
                playAsGuestButton.onClick.AddListener(OnPlayAsGuest);
            if (signInEmailButton != null)
                signInEmailButton.onClick.AddListener(OnSignInEmail);
            if (createAccountButton != null)
                createAccountButton.onClick.AddListener(OnCreateAccount);
        }

        private void OnDisable()
        {
            if (playAsGuestButton != null)
                playAsGuestButton.onClick.RemoveListener(OnPlayAsGuest);
            if (signInEmailButton != null)
                signInEmailButton.onClick.RemoveListener(OnSignInEmail);
            if (createAccountButton != null)
                createAccountButton.onClick.RemoveListener(OnCreateAccount);
        }

        private async void OnPlayAsGuest()
        {
            string displayName = nameInput != null ? nameInput.text : "";
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "Seagull" + Random.Range(1000, 9999);

            ShowLoading("Signing in...");
            bool success = await HorizonSDKIntegration.Instance.SignUpAnonymous(displayName);

            if (success)
            {
                GoToHub();
            }
            else
            {
                ShowStatus("Sign up failed. Try again.");
                ShowMain();
            }
        }

        private async void OnSignInEmail()
        {
            if (emailPanel != null && !_isEmailMode)
            {
                _isEmailMode = true;
                _isCreateMode = false;
                emailPanel.SetActive(true);
                if (signInEmailButton != null)
                {
                    var txt = signInEmailButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "Sign In";
                }
                return;
            }

            string email = emailInput != null ? emailInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Enter email and password.");
                return;
            }

            ShowLoading("Signing in...");
            bool success = await HorizonSDKIntegration.Instance.SignInEmail(email, password);

            if (success)
            {
                GoToHub();
            }
            else
            {
                ShowStatus("Sign in failed. Check credentials.");
                ShowMain();
            }
        }

        private async void OnCreateAccount()
        {
            if (emailPanel != null && !_isCreateMode)
            {
                _isCreateMode = true;
                _isEmailMode = false;
                emailPanel.SetActive(true);
                if (createAccountButton != null)
                {
                    var txt = createAccountButton.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.text = "Create";
                }
                return;
            }

            string email = emailInput != null ? emailInput.text : "";
            string password = passwordInput != null ? passwordInput.text : "";
            string displayName = nameInput != null ? nameInput.text : "";
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "Seagull" + Random.Range(1000, 9999);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ShowStatus("Enter email and password.");
                return;
            }

            ShowLoading("Creating account...");
            bool success = await HorizonSDKIntegration.Instance.SignUpEmail(email, password, displayName);

            if (success)
            {
                GoToHub();
            }
            else
            {
                ShowStatus("Account creation failed. Email may be taken.");
                ShowMain();
            }
        }

        private void GoToHub()
        {
            GameManager.Instance.GoToHub();
        }

        private void ShowMain()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        private void ShowLoading(string message)
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(true);
            ShowStatus(message);
        }

        private void ShowStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }
    }
}
