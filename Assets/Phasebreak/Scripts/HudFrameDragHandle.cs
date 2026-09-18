using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HudFrameDragHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler,
        IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform frame;
        [SerializeField] private TextMeshProUGUI moveModeLabel;
        [SerializeField] private UnityEngine.UI.Image accent;
        [SerializeField] private string positionSaveKey;
        [SerializeField] private Color lockedColor = new(.16f, .42f, .56f, .75f);
        [SerializeField] private Color unlockedColor = new(.35f, .9f, 1f, 1f);

        private Canvas canvas;
        private bool unlocked;
        private bool dragging;
        private static int activeDragCount;
        private static readonly List<RaycastResult> RaycastBuffer = new();

        public bool IsUnlocked => unlocked;
        public static bool IsDraggingAny => activeDragCount > 0;

        public void Configure(string saveKey, TextMeshProUGUI modeLabel, UnityEngine.UI.Image accentImage,
            Color normal, Color unlockedAccent)
        {
            positionSaveKey = saveKey;
            moveModeLabel = modeLabel;
            accent = accentImage;
            lockedColor = normal;
            unlockedColor = unlockedAccent;
            frame = transform as RectTransform;
            UpdateVisualState();
        }

        private void Awake()
        {
            frame ??= transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            LoadPosition();
            SetUnlocked(false);
        }

        private void OnDisable()
        {
            if (dragging)
            {
                dragging = false;
                activeDragCount = Mathf.Max(0, activeDragCount - 1);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            SetUnlocked(!unlocked);
            eventData.Use();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!unlocked || eventData.button != PointerEventData.InputButton.Left)
                return;
            dragging = true;
            activeDragCount++;
            eventData.Use();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || frame == null)
                return;
            float scale = canvas != null ? Mathf.Max(.01f, canvas.scaleFactor) : 1f;
            frame.anchoredPosition += eventData.delta / scale;
            ClampToCanvas();
            eventData.Use();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            dragging = false;
            activeDragCount = Mathf.Max(0, activeDragCount - 1);
            SavePosition();
            eventData.Use();
        }

        private void SetUnlocked(bool value)
        {
            unlocked = value;
            UpdateVisualState();
            if (!unlocked)
                SavePosition();
        }

        private void UpdateVisualState()
        {
            if (moveModeLabel != null)
            {
                moveModeLabel.text = unlocked ? "MOVE MODE  •  DRAG" : string.Empty;
                moveModeLabel.gameObject.SetActive(unlocked);
            }
            if (accent != null)
                accent.color = unlocked ? unlockedColor : lockedColor;
        }

        private void ClampToCanvas()
        {
            if (frame == null || canvas == null || canvas.transform is not RectTransform canvasRect)
                return;

            Bounds frameBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, frame);
            Rect canvasBounds = canvasRect.rect;
            Vector2 correction = Vector2.zero;

            if (frameBounds.min.x < canvasBounds.xMin)
                correction.x += canvasBounds.xMin - frameBounds.min.x;
            else if (frameBounds.max.x > canvasBounds.xMax)
                correction.x -= frameBounds.max.x - canvasBounds.xMax;

            if (frameBounds.min.y < canvasBounds.yMin)
                correction.y += canvasBounds.yMin - frameBounds.min.y;
            else if (frameBounds.max.y > canvasBounds.yMax)
                correction.y -= frameBounds.max.y - canvasBounds.yMax;

            frame.anchoredPosition += correction;
        }

        private void SavePosition()
        {
            if (frame == null || string.IsNullOrWhiteSpace(positionSaveKey))
                return;
            PlayerPrefs.SetFloat(positionSaveKey + ".x", frame.anchoredPosition.x);
            PlayerPrefs.SetFloat(positionSaveKey + ".y", frame.anchoredPosition.y);
            PlayerPrefs.Save();
        }

        private void LoadPosition()
        {
            if (frame == null || string.IsNullOrWhiteSpace(positionSaveKey) ||
                !PlayerPrefs.HasKey(positionSaveKey + ".x"))
                return;
            frame.anchoredPosition = new Vector2(
                PlayerPrefs.GetFloat(positionSaveKey + ".x"),
                PlayerPrefs.GetFloat(positionSaveKey + ".y"));
            ClampToCanvas();
        }

        public static bool IsPointerOverFrame()
        {
            if (EventSystem.current == null || Mouse.current == null)
                return false;
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };
            RaycastBuffer.Clear();
            EventSystem.current.RaycastAll(eventData, RaycastBuffer);
            foreach (RaycastResult hit in RaycastBuffer)
                if (hit.gameObject != null && hit.gameObject.GetComponentInParent<HudFrameDragHandle>() != null)
                    return true;
            return false;
        }
    }
}
