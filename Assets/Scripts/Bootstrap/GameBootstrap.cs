using UnityEngine;
using UnityEngine.SceneManagement;
using PM.horizOn.Cloud.Core;
using PM.horizOn.Cloud.Manager;

namespace SeagullStorm
{
    /// <summary>
    /// BootScene entry point. Initializes the horizOn SDK, attempts session restore,
    /// and routes to TitleScene or GameScene.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            Application.targetFrameRate = 60;

            // Initialize horizOn SDK
            bool initialized = HorizonApp.Initialize();
            if (!initialized)
            {
                Debug.LogError("[SeagullStorm] Failed to initialize horizOn SDK");
                SceneManager.LoadScene("TitleScene");
                return;
            }

            var server = new HorizonServer();
            bool connected = await server.Connect();
            if (!connected)
            {
                Debug.LogWarning("[SeagullStorm] Failed to connect to horizOn server");
                SceneManager.LoadScene("TitleScene");
                return;
            }

            // Start crash capture
            CrashManager.Instance.StartCapture();

            // Attempt session restore
            bool restored = await UserManager.Instance.RestoreAnonymousSession();
            if (restored)
            {
                CrashManager.Instance.SetUserId(UserManager.Instance.CurrentUser?.UserId ?? "");
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                SceneManager.LoadScene("TitleScene");
            }
        }
    }
}
