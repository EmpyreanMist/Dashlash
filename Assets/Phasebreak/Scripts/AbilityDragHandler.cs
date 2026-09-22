using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    public enum AbilityDragOrigin { Spellbook, ActionBar }

    public readonly struct AbilityDragPayload
    {
        public readonly AbilityDragOrigin Origin;
        public readonly int Index;
        public readonly string AbilityId;

        public AbilityDragPayload(AbilityDragOrigin origin, int index, string abilityId)
        { Origin = origin; Index = index; AbilityId = abilityId; }
    }

    // Lightweight uGUI drag bridge shared by the existing Spellbook and action-bar owners.
    public sealed class AbilityDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static AbilityDragPayload? Current { get; private set; }
        public static bool DropAccepted { get; set; }

        private Func<bool> canDrag;
        private Func<Sprite> icon;
        private Func<AbilityDragPayload> payload;
        private Action outsideDrop;
        private Action<bool> setMuted;
        private RectTransform ghost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            Current = null;
            DropAccepted = false;
        }

        public void Configure(Func<bool> canDrag, Func<Sprite> icon, Func<AbilityDragPayload> payload,
            Action outsideDrop, Action<bool> setMuted)
        {
            this.canDrag = canDrag;
            this.icon = icon;
            this.payload = payload;
            this.outsideDrop = outsideDrop;
            this.setMuted = setMuted;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canDrag == null || !canDrag()) return;
            Current = payload();
            DropAccepted = false;
            setMuted?.Invoke(true);
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            GameObject go = new("Ability Drag Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghost = go.GetComponent<RectTransform>();
            ghost.SetParent(canvas.transform, false);
            ghost.sizeDelta = new Vector2(52f, 52f);
            Image image = go.GetComponent<Image>();
            image.sprite = icon?.Invoke();
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, .82f);
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghost.SetAsLastSibling();
            ghost.position = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null) ghost.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Current.HasValue && !DropAccepted) outsideDrop?.Invoke();
            setMuted?.Invoke(false);
            if (ghost != null) Destroy(ghost.gameObject);
            ghost = null;
            Current = null;
            DropAccepted = false;
        }
    }

    public sealed class AbilityDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Action<AbilityDragPayload> drop;
        private Outline highlight;

        public void Configure(Action<AbilityDragPayload> drop, Outline highlight)
        { this.drop = drop; this.highlight = highlight; }

        public void OnDrop(PointerEventData eventData)
        {
            if (!AbilityDragSource.Current.HasValue) return;
            AbilityDragSource.DropAccepted = true;
            drop?.Invoke(AbilityDragSource.Current.Value);
            SetHighlighted(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        { if (AbilityDragSource.Current.HasValue) SetHighlighted(true); }

        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);

        private void SetHighlighted(bool active)
        {
            if (highlight == null) return;
            highlight.enabled = active;
            highlight.effectColor = new Color(.93f, .72f, .32f, 1f);
        }
    }
}
