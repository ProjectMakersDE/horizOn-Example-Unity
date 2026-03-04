using UnityEngine;
using UnityEngine.UI;

namespace SeagullStorm
{
    /// <summary>
    /// XP bar fill component.
    /// </summary>
    public class XPBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        public void SetValue(float current, float toNext)
        {
            if (fillImage != null && toNext > 0)
                fillImage.fillAmount = Mathf.Clamp01(current / toNext);
        }
    }
}
