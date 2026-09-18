using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ResourceBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private TextMeshProUGUI valueLabel;

        public void Configure(RectTransform fillRect, TextMeshProUGUI label)
        {
            fill = fillRect;
            valueLabel = label;
        }

        public void SetValue(float current, float maximum)
        {
            float safeMaximum = Mathf.Max(1f, maximum);
            float safeCurrent = Mathf.Clamp(current, 0f, safeMaximum);
            if (fill != null)
            {
                fill.anchorMax = new Vector2(safeCurrent / safeMaximum, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            if (valueLabel != null)
                valueLabel.text = $"ENERGY  {Mathf.CeilToInt(safeCurrent)}";
        }
    }
}
