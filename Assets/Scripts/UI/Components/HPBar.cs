using UnityEngine;
using UnityEngine.UI;

namespace SeagullStorm
{
    /// <summary>
    /// HP bar fill component.
    /// </summary>
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        public void SetValue(float current, float max)
        {
            if (fillImage != null && max > 0)
                fillImage.fillAmount = Mathf.Clamp01(current / max);
        }
    }
}
