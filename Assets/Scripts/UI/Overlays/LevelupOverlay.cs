using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SeagullStorm.Managers;

namespace SeagullStorm.UI.Overlays
{
    public class LevelupOverlay : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionCardPrefab;

        private List<LevelupOption> _currentOptions;

        private void OnEnable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnLevelUpTriggered += ShowOptions;
        }

        private void OnDisable()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnLevelUpTriggered -= ShowOptions;
        }

        private void ShowOptions(List<LevelupOption> options)
        {
            _currentOptions = options;
            gameObject.SetActive(true);

            if (titleText != null) titleText.text = "LEVEL UP!";

            // Clear old options
            if (optionsContainer != null)
            {
                foreach (Transform child in optionsContainer) Destroy(child.gameObject);
            }

            for (int i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (optionCardPrefab == null || optionsContainer == null) continue;

                var card = Instantiate(optionCardPrefab, optionsContainer);

                var nameText = card.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var descText = card.transform.Find("DescText")?.GetComponent<TMP_Text>();
                var button = card.GetComponent<Button>();

                if (nameText != null) nameText.text = option.displayName;
                if (descText != null) descText.text = option.description;

                int index = i;
                if (button != null) button.onClick.AddListener(() => OnSelectOption(index));
            }
        }

        private void OnSelectOption(int index)
        {
            if (_currentOptions == null || index >= _currentOptions.Count) return;

            RunManager.Instance?.SelectLevelupOption(_currentOptions[index]);
            gameObject.SetActive(false);
        }
    }
}
