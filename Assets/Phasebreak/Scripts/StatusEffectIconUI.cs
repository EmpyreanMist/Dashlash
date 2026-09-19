using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class StatusEffectIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI durationLabel;
        [SerializeField] private TextMeshProUGUI stackLabel;
        [SerializeField] private GameObject tooltipRoot;
        [SerializeField] private TextMeshProUGUI tooltipLabel;

        private StatusEffectDefinition definition;
        private float remainingDuration;
        private int stacks;

        public void Bind(StatusEffectDefinition status, float duration, int stackCount = 1)
        {
            definition = status;
            remainingDuration = Mathf.Max(0f, duration);
            stacks = Mathf.Max(1, stackCount);
            if (icon != null)
            {
                icon.sprite = definition != null ? definition.ResolveIcon() : null;
                icon.color = icon.sprite != null ? Color.white : Color.clear;
                icon.preserveAspect = true;
            }
            Refresh();
        }

        public void SetState(float duration, int stackCount)
        {
            remainingDuration = Mathf.Max(0f, duration);
            stacks = Mathf.Max(1, stackCount);
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipRoot == null || tooltipLabel == null || definition == null)
                return;
            tooltipLabel.text = $"<b>{definition.displayName}</b>\n{definition.description}";
            tooltipRoot.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }

        private void Refresh()
        {
            if (durationLabel != null)
                durationLabel.text = remainingDuration > 0f ? Mathf.CeilToInt(remainingDuration).ToString() : string.Empty;
            if (stackLabel != null)
                stackLabel.text = stacks > 1 ? stacks.ToString() : string.Empty;
            if (tooltipRoot != null)
                tooltipRoot.SetActive(false);
        }
    }
}
