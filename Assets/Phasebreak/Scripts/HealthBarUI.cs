using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private TextMeshProUGUI valueLabel;

        public void Configure(RectTransform fillRect, TextMeshProUGUI label)
        {
            fill = fillRect;
            valueLabel = label;
        }

        public void SetValue(int current, int maximum)
        {
            int safeMaximum = Mathf.Max(1, maximum);
            int safeCurrent = Mathf.Clamp(current, 0, safeMaximum);
            if (fill != null)
            {
                fill.anchorMax = new Vector2((float)safeCurrent / safeMaximum, 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            if (valueLabel != null)
                valueLabel.text = $"{safeCurrent:N0} / {safeMaximum:N0}";
        }
    }
}
