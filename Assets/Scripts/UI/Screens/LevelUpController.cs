using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Level up choice display: 3 cards from weighted pool.
    /// </summary>
    public class LevelUpController : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceCardPrefab;

        private List<LevelUpChoice> _currentChoices;

        private void OnEnable()
        {
            if (LevelUpManager.Instance != null)
                LevelUpManager.Instance.OnLevelUp += ShowChoices;
        }

        private void OnDisable()
        {
            if (LevelUpManager.Instance != null)
                LevelUpManager.Instance.OnLevelUp -= ShowChoices;
        }

        private void ShowChoices(List<LevelUpChoice> choices)
        {
            _currentChoices = choices;
            if (titleText != null) titleText.text = "LEVEL UP!";

            if (choiceContainer != null)
            {
                foreach (Transform child in choiceContainer) Destroy(child.gameObject);
            }

            var layout = choiceContainer != null ? choiceContainer.GetComponent<HorizontalLayoutGroup>() : null;
            if (layout != null) { layout.spacing = 12; layout.childControlWidth = true; layout.childForceExpandWidth = true; }
            for (int i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choiceCardPrefab == null || choiceContainer == null) continue;

                var card = Instantiate(choiceCardPrefab, choiceContainer);
                SeagullInterface.StyleLegacySubtree(card);
                var nameText = card.transform.Find("NameText")?.GetComponent<TMP_Text>();
                var descText = card.transform.Find("DescText")?.GetComponent<TMP_Text>();
                var button = card.GetComponent<Button>();

                if (nameText != null)
                {
                    nameText.text = choice.displayName;
                    nameText.fontSizeMin = 8; nameText.fontSizeMax = 12;
                    nameText.alignment = TextAlignmentOptions.Center;
                }
                if (descText != null)
                {
                    descText.text = choice.description;
                    descText.fontSizeMin = 6; descText.fontSizeMax = 9;
                    descText.fontStyle = FontStyles.Normal;
                    descText.alignment = TextAlignmentOptions.Center;
                }

                int idx = i;
                if (button != null) button.onClick.AddListener(() => OnSelect(idx));
            }
        }

        private void OnSelect(int index)
        {
            if (_currentChoices == null || index >= _currentChoices.Count) return;
            GameManager.Instance?.RecordLevelUpChoice(index);
            LevelUpManager.Instance?.SelectChoice(_currentChoices[index]);
        }
    }
}
