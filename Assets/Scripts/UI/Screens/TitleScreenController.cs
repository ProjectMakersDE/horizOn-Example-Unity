using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Title screen auth UI. Handles guest, email sign-in, create account, and Google placeholder.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Buttons")]
        [SerializeField] private Button guestButton;
        [SerializeField] private Button googleButton;
        [SerializeField] private Button emailSignInButton;
        [SerializeField] private Button createAccountButton;

        [Header("Panels")]
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject emailPanel;
        [SerializeField] private TMP_Text statusText;

        private bool _isEmailSignIn;

        private void Start()
        {
            AudioManager.Instance?.PlayMenuMusic();

            if (emailPanel != null) emailPanel.SetActive(false);
            SetStatus("");

            if (guestButton != null) guestButton.onClick.AddListener(OnGuestClicked);
            if (googleButton != null) googleButton.onClick.AddListener(OnGoogleClicked);
            if (emailSignInButton != null) emailSignInButton.onClick.AddListener(OnEmailSignInClicked);
            if (createAccountButton != null) createAccountButton.onClick.AddListener(OnCreateAccountClicked);
        }

        private async void OnGuestClicked()
        {
            string name = nameInput != null ? nameInput.text : "";
            if (string.IsNullOrWhiteSpace(name))
                name = "Seagull" + Random.Range(1000, 9999);

            SetStatus("Signing in...");
            bool success = await HorizonManager.Instance.SignUpGuest(name);

            if (success)
                SceneManager.LoadScene("GameScene");
            else
                SetStatus("Sign up failed. Try again.");
        }

        private async void OnGoogleClicked()
        {
            SetStatus("Starting Google Sign-In...");
            try
            {
                // Google Sign-In requires platform-specific setup (Google Sign-In SDK).
                // This example shows the SDK call — actual auth code comes from the platform plugin.
                SetStatus("Google Sign-In requires platform-specific setup. Use Email or Guest instead.");
            }
            catch (System.Exception ex)
            {
                SetStatus($"Google Sign-In failed: {ex.Message}");
            }
        }

        private async void OnEmailSignInClicked()
        {
            if (emailPanel != null && !emailPanel.activeSelf)
            {
                _isEmailSignIn = true;
                emailPanel.SetActive(true);
                SetStatus("Enter email and password to sign in.");
                return;
            }

            if (!_isEmailSignIn)
            {
                _isEmailSignIn = true;
                SetStatus("Enter email and password to sign in.");
                return;
            }

            string email = emailInput != null ? emailInput.text : "";
            string pw = passwordInput != null ? passwordInput.text : "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pw))
            {
                SetStatus("Enter email and password.");
                return;
            }

            SetStatus("Signing in...");
            bool success = await HorizonManager.Instance.SignInEmail(email, pw);

            if (success)
                SceneManager.LoadScene("GameScene");
            else
                SetStatus("Sign in failed. Check credentials.");
        }

        private async void OnCreateAccountClicked()
        {
            if (emailPanel != null && !emailPanel.activeSelf)
            {
                _isEmailSignIn = false;
                emailPanel.SetActive(true);
                SetStatus("Enter email and password to create account.");
                return;
            }

            if (_isEmailSignIn)
            {
                _isEmailSignIn = false;
                SetStatus("Enter email and password to create account.");
                return;
            }

            string email = emailInput != null ? emailInput.text : "";
            string pw = passwordInput != null ? passwordInput.text : "";
            string name = nameInput != null ? nameInput.text : "";
            if (string.IsNullOrWhiteSpace(name))
                name = "Seagull" + Random.Range(1000, 9999);

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pw))
            {
                SetStatus("Enter email and password.");
                return;
            }

            SetStatus("Creating account...");
            bool success = await HorizonManager.Instance.SignUpEmail(email, pw, name);

            if (success)
                SceneManager.LoadScene("GameScene");
            else
                SetStatus("Account creation failed. Email may be taken.");
        }

        private void SetStatus(string msg)
        {
            if (statusText != null) statusText.text = msg;
        }
    }
}
