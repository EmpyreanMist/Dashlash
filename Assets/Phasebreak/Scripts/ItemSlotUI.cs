using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    public sealed class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Action enter, exit, leftClick, rightClick, doubleClick;
        private Image background;
        private Color normalColor;
        private Color hoverColor;
        private Color selectedColor;
        private bool selected;
        private bool hovered;

        public void Configure(Action onEnter, Action onExit, Action onClick)
            => Configure(onEnter, onExit, onClick, null, null);

        public void Configure(Action onEnter, Action onExit, Action onLeftClick, Action onRightClick, Action onDoubleClick)
        {
            enter = onEnter;
            exit = onExit;
            leftClick = onLeftClick;
            rightClick = onRightClick;
            doubleClick = onDoubleClick;
        }

        public void ConfigureVisual(Image target, Color normal, Color hover, Color selectedState)
        {
            background = target;
            normalColor = normal;
            hoverColor = hover;
            selectedColor = selectedState;
            ApplyVisual();
        }

        public void SetSelected(bool value)
        {
            selected = value;
            ApplyVisual();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            ApplyVisual();
            enter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            ApplyVisual();
            exit?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                rightClick?.Invoke();
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (eventData.clickCount >= 2 && doubleClick != null)
                doubleClick.Invoke();
            else
                leftClick?.Invoke();
        }

        private void ApplyVisual()
        {
            if (background != null)
                background.color = selected ? selectedColor : hovered ? hoverColor : normalColor;
        }
    }
}
