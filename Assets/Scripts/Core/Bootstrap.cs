using UnityEngine;
using SeagullStorm.Managers;
using SeagullStorm.SDK;

namespace SeagullStorm.Core
{
    public class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // Create persistent managers
            var root = new GameObject("[SeagullStorm]");
            Object.DontDestroyOnLoad(root);

            root.AddComponent<GameManager>();
            root.AddComponent<AudioManager>();
            root.AddComponent<HorizonSDKIntegration>();
        }
    }
}
