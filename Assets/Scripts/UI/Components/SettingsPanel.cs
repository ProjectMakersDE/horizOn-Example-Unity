using UnityEngine;
using UnityEngine.UI;

namespace SeagullStorm
{
    /// <summary>
    /// Settings panel with sign out button.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button signOutButton;
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOut);
            if (closeButton != null) closeButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            if (signOutButton != null) signOutButton.onClick.RemoveAllListeners();
            if (closeButton != null) closeButton.onClick.RemoveAllListeners();
        }

        private void OnSignOut()
        {
            GameManager.Instance?.SignOutAndReturn();
        }

        private void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
