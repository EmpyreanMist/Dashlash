using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Phasebreak.Gameplay
{
    public sealed class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Action enter, exit, click;
        public void Configure(Action onEnter, Action onExit, Action onClick) { enter = onEnter; exit = onExit; click = onClick; }
        public void OnPointerEnter(PointerEventData eventData) => enter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => exit?.Invoke();
        public void OnPointerClick(PointerEventData eventData) { if (eventData.button == PointerEventData.InputButton.Left) click?.Invoke(); }
    }
}
