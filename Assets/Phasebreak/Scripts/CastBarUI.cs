using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CastBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform fill;
        [SerializeField] private TextMeshProUGUI castLabel;
        [SerializeField] private UnityEngine.UI.Image interruptMarker;

        public void Configure(RectTransform root, RectTransform fillRect, TextMeshProUGUI label,
            UnityEngine.UI.Image marker)
        {
            contentRoot = root;
            fill = fillRect;
            castLabel = label;
            interruptMarker = marker;
            SetCast(string.Empty, 0f, false, false);
        }

        public void SetCast(string castName, float progress, bool visible, bool interruptible)
        {
            if (contentRoot != null)
                contentRoot.gameObject.SetActive(visible);
            if (!visible)
                return;
            if (fill != null)
            {
                fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
            }
            if (castLabel != null)
                castLabel.text = string.IsNullOrWhiteSpace(castName) ? "CASTING" : castName.ToUpperInvariant();
            if (interruptMarker != null)
            {
                interruptMarker.gameObject.SetActive(interruptible);
                interruptMarker.color = interruptible
                    ? new Color(1f, .72f, .2f, 1f)
                    : new Color(.36f, .31f, .42f, 1f);
            }
        }
    }
}
