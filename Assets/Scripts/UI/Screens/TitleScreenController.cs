using System.Threading.Tasks;
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
        [SerializeField] private Button appleButton;
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
            if (appleButton != null) appleButton.onClick.AddListener(OnAppleClicked);
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
                // In production, obtain the authorization code from a platform-specific
                // Google Sign-In plugin (e.g. Google Sign-In for Unity).
                // For this example we attempt the SDK call so the full flow is visible.
                string authCode = await GetGoogleAuthorizationCode();

                bool success = await HorizonManager.Instance.SignInGoogle(authCode);
                if (success)
                {
                    SceneManager.LoadScene("GameScene");
                }
                else
                {
                    SetStatus("Google Sign-In failed. Try Email or Guest instead.");
                }
            }
            catch (System.Exception ex)
            {
                // Expected on platforms without Google Sign-In SDK configured.
                Debug.Log($"Google Sign-In unavailable: {ex.Message}");
                SetStatus("Google Sign-In requires platform-specific setup. Use Email or Guest instead.");
            }
        }

        /// <summary>
        /// Placeholder for platform-specific Google Sign-In.
        /// Replace this with your actual Google Sign-In plugin call that returns
        /// an authorization code (e.g. GoogleSignIn.DefaultInstance.SignIn()).
        /// </summary>
        private Task<string> GetGoogleAuthorizationCode()
        {
            throw new System.PlatformNotSupportedException(
                "Google Sign-In plugin is not configured. " +
                "Integrate a platform-specific Google Sign-In SDK and return the authorization code here.");
        }

        private async void OnAppleClicked()
        {
            SetStatus("Starting Apple Sign-In...");

            // SignInWithApple opens the native ASAuthorizationController on iOS or a
            // system-browser OAuth redirect on other platforms (configured via
            // HorizonConfig.AppleServicesId / AppleRedirectUri). On USER_NOT_FOUND
            // it transparently falls through to SignUpApple.
            bool success = await HorizonManager.Instance.SignInWithApple();

            if (success)
            {
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                SetStatus("Apple Sign-In failed. Try Email or Guest instead.");
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
