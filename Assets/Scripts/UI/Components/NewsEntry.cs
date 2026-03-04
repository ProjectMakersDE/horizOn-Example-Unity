using UnityEngine;
using TMPro;

namespace SeagullStorm
{
    /// <summary>
    /// Single news item display.
    /// </summary>
    public class NewsEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text dateText;

        public void Setup(string title, string date)
        {
            if (titleText != null) titleText.text = title ?? "";
            if (dateText != null) dateText.text = date ?? "";
        }
    }
}
