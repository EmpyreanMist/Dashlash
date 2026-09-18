using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class WorldNameplateUI : MonoBehaviour
    {
        [SerializeField] private Targetable target;
        [SerializeField] private UnityEngine.UI.Image panel;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private UnityEngine.UI.Image healthFillImage;
        [SerializeField] private TextMeshProUGUI label;

        private RectTransform root;
        private Color normalPanelColor;
        private Color normalHealthColor;
        private Color selectedPanelColor;
        private Color selectedHealthColor;

        public Targetable Target => target;
        public RectTransform Root => root != null ? root : root = transform as RectTransform;

        public void Configure(Targetable targetable, UnityEngine.UI.Image panelImage, RectTransform fill,
            UnityEngine.UI.Image fillImage, TextMeshProUGUI nameLabel, Color panelColor, Color healthColor,
            Color selectedPanel, Color selectedHealth)
        {
            target = targetable;
            panel = panelImage;
            healthFill = fill;
            healthFillImage = fillImage;
            label = nameLabel;
            normalPanelColor = panelColor;
            normalHealthColor = healthColor;
            selectedPanelColor = selectedPanel;
            selectedHealthColor = selectedHealth;
            root = transform as RectTransform;
        }

        public void Refresh(Camera worldCamera, Targetable player, Targetable selectedTarget,
            RectTransform canvasRect, float range)
        {
            if (target == null || worldCamera == null || player == null || canvasRect == null)
            {
                SetVisible(false);
                return;
            }

            Vector3 viewport = worldCamera.WorldToViewportPoint(target.NameplateWorldPosition);
            float distance = Vector3.Distance(player.transform.position, target.transform.position);
            bool visible = target.IsAlive && viewport.z > 0f && distance <= range &&
                           viewport.x > -.05f && viewport.x < 1.05f && viewport.y > -.05f && viewport.y < 1.05f;
            SetVisible(visible);
            if (!visible)
                return;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.NameplateWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null,
                out Vector2 localPoint);
            Root.anchoredPosition = localPoint;
            bool selected = target == selectedTarget;
            Root.localScale = selected ? Vector3.one * 1.08f : Vector3.one;
            if (panel != null)
                panel.color = selected ? selectedPanelColor : normalPanelColor;
            if (healthFillImage != null)
                healthFillImage.color = selected ? selectedHealthColor : normalHealthColor;
            if (label != null)
                label.text = $"{target.DisplayName}  •  Lv {target.Level}";
            if (healthFill != null)
            {
                float amount = target.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)target.CurrentHealth / target.MaxHealth);
                healthFill.anchorMax = new Vector2(amount, 1f);
                healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }
    }
}
