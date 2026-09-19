using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(-200), DisallowMultipleComponent]
    public sealed class PhasebreakChatHub : MonoBehaviour
    {
        private const int MaxMessages = 60;
        private const int MaxHistory = 40;
        private static readonly Color Backdrop = new(.012f, .019f, .032f, .9f);
        private static readonly Color Raised = new(.035f, .053f, .075f, .96f);
        private static readonly Color Accent = new(.16f, .67f, .76f, 1f);
        private static readonly Color Body = new(.85f, .9f, .93f, 1f);

        private readonly Queue<TextMeshProUGUI> messages = new();
        private readonly List<string> history = new();
        private readonly List<string> commandSuggestions = new();
        private InputAction enterAction;
        private InputAction submitAction;
        private InputAction escapeAction;
        private InputAction upAction;
        private InputAction downAction;
        private InputAction tabAction;
        private RectTransform panel;
        private CanvasGroup panelGroup;
        private RectTransform messageContent;
        private ScrollRect scroll;
        private TMP_InputField input;
        private TextMeshProUGUI suggestion;
        private int historyIndex;
        private int openedFrame;
        private string draft = string.Empty;
        private DeveloperCommandRegistry commands;
        private RectTransform hudRoot;
        private TextMeshProUGUI debugOverlay;
        private readonly List<GameObject> hiddenHud = new();
        private float nextOverlayUpdate;

        public bool ShowCoords { get; set; }
        public bool ShowFps { get; set; }
        public bool HudVisible { get; private set; } = true;

        public bool IsOpen => GameplayInputFocus.ChatFocused;

        private void Awake()
        {
            enterAction = PhasebreakSettings.Button("chat", "Chat Enter");
            enterAction.AddBinding("<Keyboard>/numpadEnter");
            submitAction = new InputAction("Chat Submit", InputActionType.Button);
            submitAction.AddBinding("<Keyboard>/enter");
            submitAction.AddBinding("<Keyboard>/numpadEnter");
            escapeAction = new InputAction("Chat Cancel", InputActionType.Button, "<Keyboard>/escape");
            upAction = new InputAction("Chat History Previous", InputActionType.Button, "<Keyboard>/upArrow");
            downAction = new InputAction("Chat History Next", InputActionType.Button, "<Keyboard>/downArrow");
            tabAction = new InputAction("Chat Suggestion", InputActionType.Button, "<Keyboard>/tab");
        }

        private void OnEnable()
        {
            enterAction.Enable(); submitAction.Enable(); escapeAction.Enable(); upAction.Enable(); downAction.Enable(); tabAction.Enable();
        }

        private void OnDisable()
        {
            Close();
            SetHudVisible(true);
            ShowCoords = false;
            ShowFps = false;
            enterAction.Disable(); submitAction.Disable(); escapeAction.Disable(); upAction.Disable(); downAction.Disable(); tabAction.Disable();
        }

        private void OnDestroy()
        {
            PhasebreakSettings.Unregister(enterAction);
            enterAction.Dispose(); submitAction.Dispose(); escapeAction.Dispose(); upAction.Dispose(); downAction.Dispose(); tabAction.Dispose();
        }

        public void Initialize(RectTransform hudCanvas)
        {
            if (panel != null || hudCanvas == null)
                return;
            EnsureEventSystem();
            hudRoot = hudCanvas;
            BuildUi(hudCanvas);
            commands = new DeveloperCommandRegistry(this);
            SetCommandSuggestions(commands.Names);
            AppendMessage($"Local chat  •  {PhasebreakSettings.Display("chat")} to talk", Accent);
            panelGroup.alpha = .78f;
            panelGroup.blocksRaycasts = false;
            input.interactable = false;
        }

        public void SetCommandSuggestions(IEnumerable<string> names)
        {
            commandSuggestions.Clear();
            if (names != null)
                foreach (string name in names)
                    if (!string.IsNullOrWhiteSpace(name)) commandSuggestions.Add(name.TrimStart('/'));
            RefreshSuggestion(input != null ? input.text : string.Empty);
        }

        private void Update()
        {
            UpdateOverlay();
            if (input == null)
                return;
            if (!IsOpen)
            {
                if (enterAction.WasPressedThisFrame() && !GameplayInputFocus.GameplayInputBlocked &&
                    !PhasebreakInventoryHud.IsMajorMenuOpen && !WorldQuestHud.IsWorldMenuOpen)
                    Open();
                return;
            }

            if (escapeAction.WasPressedThisFrame()) { Close(); return; }
            if (Time.frameCount == openedFrame) return;
            if (submitAction.WasPressedThisFrame()) { Submit(); return; }
            if (upAction.WasPressedThisFrame()) Recall(-1);
            else if (downAction.WasPressedThisFrame()) Recall(1);
            else if (tabAction.WasPressedThisFrame()) CompleteSuggestion();
        }

        private void Open()
        {
            if (input.placeholder is TextMeshProUGUI placeholder)
                placeholder.text = $"Press {PhasebreakSettings.Display("chat")} to speak locally…";
            GameplayInputFocus.SetChatFocused(true);
            panelGroup.alpha = 1f;
            panelGroup.blocksRaycasts = true;
            input.interactable = true;
            openedFrame = Time.frameCount;
            historyIndex = history.Count;
            draft = string.Empty;
            input.text = string.Empty;
            StartCoroutine(FocusNextFrame());
        }

        private IEnumerator FocusNextFrame()
        {
            // Let the opening key event pass before the Input System UI module sees the field.
            yield return null;
            if (!IsOpen || input == null) yield break;
            input.ActivateInputField();
            input.Select();
            input.caretPosition = input.text.Length;
        }

        private void Close()
        {
            if (!IsOpen) return;
            GameplayInputFocus.SetChatFocused(false);
            if (input == null) return;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == input.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
            input.DeactivateInputField();
            input.text = string.Empty;
            input.interactable = false;
            panelGroup.alpha = .78f;
            panelGroup.blocksRaycasts = false;
        }

        private void Submit()
        {
            string message = input.text.Trim();
            if (message.Length > 0)
            {
                if (history.Count == MaxHistory) history.RemoveAt(0);
                history.Add(message);
                if (message.StartsWith("/"))
                {
                    string result = commands.Run(message);
                    if (!string.IsNullOrEmpty(result)) AppendMessage(result, Accent);
                }
                else
                    AppendMessage("YOU  " + message, Body);
            }
            Close();
        }

        private void Recall(int direction)
        {
            if (history.Count == 0) return;
            if (historyIndex == history.Count && direction < 0) draft = input.text;
            historyIndex = Mathf.Clamp(historyIndex + direction, 0, history.Count);
            input.text = historyIndex == history.Count ? draft : history[historyIndex];
            input.caretPosition = input.text.Length;
        }

        private void CompleteSuggestion()
        {
            string match = FindSuggestion(input.text);
            if (match == null) return;
            input.text = "/" + match + " ";
            input.caretPosition = input.text.Length;
        }

        private string FindSuggestion(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] != '/') return null;
            string prefix = value.Substring(1).Split(' ')[0];
            foreach (string name in commandSuggestions)
                if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) return name;
            return null;
        }

        private void RefreshSuggestion(string value)
        {
            if (suggestion == null) return;
            string match = FindSuggestion(value);
            suggestion.text = match != null ? "/" + match + "    TAB TO COMPLETE" :
                !string.IsNullOrEmpty(value) && value[0] == '/' ? "UNKNOWN COMMAND  •  /help" : string.Empty;
            suggestion.gameObject.SetActive(suggestion.text.Length > 0);
        }

        private void AppendMessage(string value, Color color)
        {
            TextMeshProUGUI line = AddText("Chat Line", messageContent, value, 17f, color);
            line.textWrappingMode = TextWrappingModes.Normal;
            LayoutElement layout = line.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 22f;
            messages.Enqueue(line);
            if (messages.Count > MaxMessages)
                Destroy(messages.Dequeue().gameObject);
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        public void ClearMessages()
        {
            while (messages.Count > 0) Destroy(messages.Dequeue().gameObject);
        }

        public void SetHudVisible(bool visible)
        {
            if (hudRoot == null || HudVisible == visible) return;
            HudVisible = visible;
            if (!visible)
            {
                hiddenHud.Clear();
                foreach (Transform child in hudRoot)
                {
                    if (child == panel || child == debugOverlay.transform || !child.gameObject.activeSelf) continue;
                    hiddenHud.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
            else
            {
                foreach (GameObject item in hiddenHud)
                    if (item != null) item.SetActive(true);
                hiddenHud.Clear();
            }
        }

        private void UpdateOverlay()
        {
            if (debugOverlay == null || Time.unscaledTime < nextOverlayUpdate) return;
            nextOverlayUpdate = Time.unscaledTime + .25f;
            var player = FindAnyObjectByType<PhasebreakPlayerMovement>();
            Vector3 p = player != null ? player.transform.position : Vector3.zero;
            debugOverlay.text = (ShowFps ? $"FPS {1f / Mathf.Max(.001f, Time.unscaledDeltaTime):0}\n" : "") +
                (ShowCoords && player != null ? $"XYZ {p.x:0.#}, {p.y:0.#}, {p.z:0.#}" : "");
            debugOverlay.gameObject.SetActive(debugOverlay.text.Length > 0);
        }

        private void BuildUi(RectTransform canvas)
        {
            panel = Rect("Local Chat", canvas);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0f, 0f);
            panel.anchoredPosition = new Vector2(26f, 30f);
            panel.sizeDelta = new Vector2(520f, 254f);
            AddImage(panel, Backdrop, true);
            panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            Outline outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.12f, .48f, .55f, .65f);
            outline.effectDistance = new Vector2(1f, -1f);

            TextMeshProUGUI heading = AddText("Chat Heading", panel, "LOCAL  /  PHASEBREAK", 13f, Accent);
            Place(heading.rectTransform, new Vector2(14f, 222f), new Vector2(492f, 20f));

            RectTransform scrollRoot = Rect("Chat Scroll", panel);
            Place(scrollRoot, new Vector2(12f, 70f), new Vector2(496f, 148f));
            AddImage(scrollRoot, new Color(.008f, .015f, .026f, .6f), true);
            scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 22f;

            RectTransform viewport = Rect("Viewport", scrollRoot);
            Stretch(viewport, 4f);
            AddImage(viewport, new Color(1f, 1f, 1f, .01f), true);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            messageContent = Rect("Content", viewport);
            messageContent.anchorMin = new Vector2(0f, 1f);
            messageContent.anchorMax = Vector2.one;
            messageContent.pivot = new Vector2(.5f, 1f);
            messageContent.sizeDelta = Vector2.zero;
            VerticalLayoutGroup group = messageContent.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(8, 8, 5, 5);
            group.spacing = 4f;
            group.childControlHeight = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = messageContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = messageContent;

            RectTransform fieldRoot = Rect("Chat Input", panel);
            Place(fieldRoot, new Vector2(12f, 14f), new Vector2(496f, 43f));
            AddImage(fieldRoot, Raised, true);
            input = fieldRoot.gameObject.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 240;
            RectTransform textViewport = Rect("Text Area", fieldRoot);
            Stretch(textViewport, 12f);
            textViewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI value = AddText("Text", textViewport, string.Empty, 18f, Body);
            Stretch(value.rectTransform, 0f);
            TextMeshProUGUI placeholder = AddText("Placeholder", textViewport, "Press Enter to speak locally…", 18f,
                new Color(.48f, .56f, .62f, 1f));
            Stretch(placeholder.rectTransform, 0f);
            input.textViewport = textViewport;
            input.textComponent = value;
            input.placeholder = placeholder;
            input.onValueChanged.AddListener(RefreshSuggestion);

            suggestion = AddText("Command Suggestion", panel, string.Empty, 13f, Accent);
            Place(suggestion.rectTransform, new Vector2(16f, 58f), new Vector2(486f, 16f));
            suggestion.gameObject.SetActive(false);

            debugOverlay = AddText("Debug Overlay", canvas, string.Empty, 15f, Accent);
            debugOverlay.alignment = TextAlignmentOptions.TopRight;
            debugOverlay.rectTransform.anchorMin = debugOverlay.rectTransform.anchorMax = new Vector2(1f, 1f);
            debugOverlay.rectTransform.pivot = new Vector2(1f, 1f);
            debugOverlay.rectTransform.anchoredPosition = new Vector2(-22f, -22f);
            debugOverlay.rectTransform.sizeDelta = new Vector2(340f, 54f);
            debugOverlay.gameObject.SetActive(false);
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static Image AddImage(RectTransform rect, Color color, bool raycast)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TextMeshProUGUI AddText(string name, Transform parent, string value, float size, Color color)
        {
            TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
