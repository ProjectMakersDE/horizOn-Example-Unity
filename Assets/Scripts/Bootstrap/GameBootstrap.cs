using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PM.horizOn.Cloud.Core;
namespace SeagullStorm
{
    public class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Application.targetFrameRate = 60;
            if (HorizonManager.Instance == null)
                new GameObject("[HorizonManager]").AddComponent<HorizonManager>();
            SceneManager.LoadScene("TitleScene");
            bool featureFixture = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            featureFixture = SeagullFeatureFixtureSmoke.Enabled;
#endif
            if (!SeagullSmoke.Enabled && !featureFixture) PrepareOnline();
        }
        private async void PrepareOnline()
        {
            try
            {
                if (Resources.Load<ScriptableObject>("horizOn/HorizonConfig") != null && HorizonApp.Initialize())
                    HorizonManager.Instance.Configured = await new HorizonServer().Connect();
            }
            catch (Exception ex) { Debug.LogWarning("[SeagullStorm] Online services unavailable: " + ex.Message); }
        }
    }
}
