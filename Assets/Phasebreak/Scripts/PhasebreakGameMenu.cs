using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(500), DisallowMultipleComponent]
    public sealed class PhasebreakGameMenu : MonoBehaviour
    {
        private readonly struct SettingRow
        {
            public readonly string Category;
            public readonly string SearchText;
            public readonly GameObject Object;
            public SettingRow(string category, string searchText, GameObject item)
            { Category = category; SearchText = searchText; Object = item; }
        }

        private static readonly Color Back = PhasebreakUiTheme.Window;
        private static readonly Color Raised = PhasebreakUiTheme.Raised;
        private static readonly Color Accent = PhasebreakUiTheme.Accent;
        private static readonly Color Light = PhasebreakUiTheme.Text;
        private static readonly Color Muted = PhasebreakUiTheme.MutedText;

        private readonly List<SettingRow> rows = new();
        private readonly List<(int startRow, GameObject item)> headers = new();
        private string lastHeaderCategory;
        private readonly Dictionary<string, TextMeshProUGUI> bindingLabels = new();
        private RectTransform root;
        private RectTransform menuPanel;
        private RectTransform settingsPanel;
        private RectTransform content;
        private TMP_InputField search;
        private TextMeshProUGUI status;
        private RectTransform conflictRoot;
        private TextMeshProUGUI conflictHeading;
        private TextMeshProUGUI conflictBody;
        private PhasebreakSettings.PendingBindingChange pendingBindingChange;
        private bool hasPendingBindingChange;
        private PhasebreakFollowCamera followCamera;
        private string rebindingId;
        private bool open;
        private float previousTimeScale = 1f;
        private float nextScaleRefresh;
        private int resolutionIndex;
        private readonly List<Vector2Int> resolutions = new();

        private void Awake()
        {
            followCamera = FindAnyObjectByType<PhasebreakFollowCamera>();
            ApplyVideoPrefs();
            CollectResolutions();
        }

        public void Initialize()
        {
            if (root != null) return;
            GameObject canvasObject = new("Phasebreak Game Menu", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.matchWidthOrHeight = .5f;
            root = canvasObject.GetComponent<RectTransform>();
            BuildMenu();
            BuildSettings();
            root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextScaleRefresh)
            {
                nextScaleRefresh = Time.unscaledTime + 2f;
                ApplyUiScale();
            }
            Keyboard keyboard = Keyboard.current;
            if (open)
            {
                if (hasPendingBindingChange)
                {
                    if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CancelPendingBinding();
                    return;
                }
                if (rebindingId != null) { CaptureBinding(keyboard, Mouse.current); return; }
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    if (settingsPanel.gameObject.activeSelf) ShowMain();
                    else Close();
                }
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !GameplayInputFocus.GameplayInputBlocked &&
                !PhasebreakInventoryHud.IsMajorMenuOpen && !WorldQuestHud.IsWorldMenuOpen)
                Open();
        }

        private void OnDisable()
        {
            if (open) Close();
        }

        private void Open()
        {
            open = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            root.gameObject.SetActive(true);
            ApplyUiScale();
            ShowMain();
            GameplayInputFocus.SetMenuFocused(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Close()
        {
            open = false;
            Time.timeScale = previousTimeScale;
            rebindingId = null;
            HideConflict();
            if (root != null) root.gameObject.SetActive(false);
            if (search != null) search.text = string.Empty;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            GameplayInputFocus.SetMenuFocused(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ShowMain()
        {
            rebindingId = null;
            HideConflict();
            menuPanel.gameObject.SetActive(true);
            settingsPanel.gameObject.SetActive(false);
        }

        private void ShowSettings()
        {
            menuPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(true);
            status.text = "Select a control to change its key.";
            RefreshBindingLabels();
            RefreshSearch(search.text);
        }

        private void BuildMenu()
        {
            RectTransform backdrop = Rect("Menu Backdrop", root, new Color(.015f, .014f, .012f, .72f), true);
            Stretch(backdrop, 0f);
            menuPanel = Rect("Game Menu", root, Back, true);
            PhasebreakUiTheme.StyleSurface(menuPanel.GetComponent<UnityEngine.UI.Image>(), Back);
            menuPanel.anchorMin = menuPanel.anchorMax = menuPanel.pivot = new Vector2(.5f, .5f);
            menuPanel.sizeDelta = new Vector2(560f, 480f);
            Label("Title", menuPanel, "PHASEBREAK", 39f, Light, new Vector2(.08f, .77f), new Vector2(.92f, .93f));
            Label("Subtitle", menuPanel, "GAME MENU", 16f, Accent, new Vector2(.08f, .68f), new Vector2(.92f, .77f));
            Button("Resume", menuPanel, "RESUME", new Vector2(.14f, .51f), new Vector2(.86f, .63f), Close);
            Button("Settings", menuPanel, "SETTINGS", new Vector2(.14f, .35f), new Vector2(.86f, .47f), ShowSettings);
            Button("Quit", menuPanel, "QUIT GAME", new Vector2(.14f, .19f), new Vector2(.86f, .31f), QuitGame);
        }

        private void BuildSettings()
        {
            settingsPanel = Rect("Settings", root, Back, true);
            PhasebreakUiTheme.StyleSurface(settingsPanel.GetComponent<UnityEngine.UI.Image>(), Back);
            settingsPanel.anchorMin = new Vector2(.12f, .08f);
            settingsPanel.anchorMax = new Vector2(.88f, .92f);
            settingsPanel.offsetMin = settingsPanel.offsetMax = Vector2.zero;
            Label("Heading", settingsPanel, "SETTINGS", 31f, Light, new Vector2(.035f, .89f), new Vector2(.47f, .97f));
            Button("Back", settingsPanel, "BACK", new Vector2(.84f, .895f), new Vector2(.965f, .965f), ShowMain);

            RectTransform searchRoot = Rect("Search Settings", settingsPanel, Raised, true);
            PhasebreakUiTheme.StyleSurface(searchRoot.GetComponent<UnityEngine.UI.Image>(), Raised);
            Anchors(searchRoot, new Vector2(.035f, .79f), new Vector2(.965f, .875f));
            search = searchRoot.gameObject.AddComponent<TMP_InputField>();
            search.lineType = TMP_InputField.LineType.SingleLine;
            search.characterLimit = 80;
            RectTransform searchViewport = Rect("Search Text Area", searchRoot, Color.clear, false);
            Stretch(searchViewport, 14f);
            // Reserve the right edge for CLEAR so long search text never paints over it.
            searchViewport.offsetMax = new Vector2(-112f, -14f);
            searchViewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            TextMeshProUGUI value = Label("Search Value", searchViewport, string.Empty, 23f, Light,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
            TextMeshProUGUI placeholder = Label("Search Placeholder", searchViewport,
                "SEARCH SETTINGS OR KEYBINDINGS", 22f, Muted, Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
            search.textViewport = searchViewport;
            search.textComponent = value;
            search.placeholder = placeholder;
            search.onValueChanged.AddListener(RefreshSearch);
            Button("Clear Search", settingsPanel, "CLEAR", new Vector2(.84f, .793f), new Vector2(.959f, .869f),
                () => { search.text = string.Empty; search.Select(); });

            RectTransform scrollRoot = Rect("Settings Scroll", settingsPanel, Raised, true);
            PhasebreakUiTheme.StyleSurface(scrollRoot.GetComponent<UnityEngine.UI.Image>(), Raised);
            Anchors(scrollRoot, new Vector2(.035f, .13f), new Vector2(.965f, .775f));
            UnityEngine.UI.ScrollRect scroll = scrollRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            RectTransform viewport = Rect("Viewport", scrollRoot, new Color(1f, 1f, 1f, .01f), true);
            Stretch(viewport, 5f);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            content = Rect("Content", viewport, Color.clear, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.sizeDelta = Vector2.zero;
            UnityEngine.UI.VerticalLayoutGroup layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;

            BuildRows();
            status = Label("Settings Status", settingsPanel, string.Empty, 17f, Accent,
                new Vector2(.04f, .035f), new Vector2(.96f, .115f), TextAlignmentOptions.Left);
            BuildConflictModal();
            settingsPanel.gameObject.SetActive(false);
        }

        private void BuildRows()
        {
            foreach (PhasebreakSettings.Binding binding in PhasebreakSettings.Bindings)
            {
                Header(binding.Category);
                RectTransform row = Row(binding.Category, binding.Label + " " + binding.Id);
                Label("Action", row, binding.Label, 18f, Light, new Vector2(.02f, .08f), new Vector2(.60f, .92f), TextAlignmentOptions.Left);
                string id = binding.Id;
                TextMeshProUGUI key = Button("Key", row, PhasebreakSettings.Display(id),
                    new Vector2(.61f, .1f), new Vector2(.875f, .9f), () => BeginRebind(id));
                Button("Clear Binding", row, "CLEAR", new Vector2(.885f, .1f), new Vector2(.985f, .9f),
                    () => ClearBinding(id));
                bindingLabels[id] = key;
            }

            Header("Controls");
            AddButtonRow("Controls", "Reset all keybindings", "RESET TO DEFAULTS", () =>
            { PhasebreakSettings.ResetBindings(); RefreshBindingLabels(); status.text = "Default keys restored."; });

            Header("Camera");
            AddStepper("Camera", "Mouse sensitivity", () => followCamera != null ? followCamera.MouseSensitivity : .12f,
                value => { if (followCamera != null) followCamera.MouseSensitivity = value; }, .01f, .01f, 1f, "0.00");
            AddStepper("Camera", "Zoom speed", () => followCamera != null ? followCamera.ZoomSensitivity : 3.5f,
                value => { if (followCamera != null) followCamera.ZoomSensitivity = value; }, .5f, .5f, 10f, "0.0");
            TextMeshProUGUI invertLabel = null;
            invertLabel = AddButtonRow("Camera", "Invert Y", followCamera != null && followCamera.InvertY ? "ON" : "OFF", () =>
            { if (followCamera != null) { followCamera.InvertY = !followCamera.InvertY;
              invertLabel.text = followCamera.InvertY ? "ON" : "OFF"; status.text = "Invert Y: " + invertLabel.text; } });

            Header("Interface");
            AddStepper("Interface", "UI scale", () => PhasebreakSettings.GetFloat("uiScale", 1f),
                value => { PhasebreakSettings.SetFloat("uiScale", value); ApplyUiScale(); }, .1f, .7f, 1.5f, "0.0");
            AddButtonRow("Interface", "Reset HUD positions", "RESET POSITIONS", () =>
            { HudFrameDragHandle.ResetAllPositions(); status.text = "HUD positions reset."; });

            Header("Gameplay");
            AddStepper("Gameplay", "Screen shake", () => followCamera != null ? followCamera.ScreenShakeAmount : 1f,
                value => { if (followCamera != null) followCamera.ScreenShakeAmount = value; }, .25f, 0f, 1f, "0.00");

            Header("Video");
            TextMeshProUGUI resolutionLabel = null;
            resolutionLabel = AddButtonRow("Video", "Resolution", $"{Screen.width} × {Screen.height}", () =>
            { Vector2Int size = CycleResolution(); resolutionLabel.text = $"{size.x} × {size.y}"; });
            TextMeshProUGUI displayModeLabel = null;
            displayModeLabel = AddButtonRow("Video", "Display mode", Screen.fullScreenMode.ToString(), () =>
            { displayModeLabel.text = CycleDisplayMode().ToString(); });
            TextMeshProUGUI vsyncLabel = null;
            vsyncLabel = AddButtonRow("Video", "VSync", QualitySettings.vSyncCount == 0 ? "OFF" : "ON", () =>
            { QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
              PhasebreakSettings.SetInt("vsync", QualitySettings.vSyncCount);
              vsyncLabel.text = QualitySettings.vSyncCount == 0 ? "OFF" : "ON";
              status.text = "VSync: " + vsyncLabel.text; });
            if (QualitySettings.names.Length > 1)
            {
                TextMeshProUGUI qualityLabel = null;
                qualityLabel = AddButtonRow("Video", "Quality preset", QualitySettings.names[QualitySettings.GetQualityLevel()], () =>
                { int index = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
                  QualitySettings.SetQualityLevel(index, true); PhasebreakSettings.SetInt("quality", index);
                  qualityLabel.text = QualitySettings.names[index]; status.text = "Quality: " + qualityLabel.text; });
            }
        }

        private void Header(string category)
        {
            if (lastHeaderCategory == category) return;
            RectTransform header = Rect(category + " Header", content, Color.clear, false);
            header.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 42f;
            Label("Category", header, category.ToUpperInvariant(), 18f, Accent,
                new Vector2(.02f, .04f), new Vector2(.98f, .96f), TextAlignmentOptions.Left);
            headers.Add((rows.Count, header.gameObject));
            lastHeaderCategory = category;
        }

        private RectTransform Row(string category, string searchText)
        {
            RectTransform row = Rect(searchText, content, PhasebreakUiTheme.Panel, false);
            row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 58f;
            rows.Add(new SettingRow(category, searchText.ToLowerInvariant() + " " + category.ToLowerInvariant(), row.gameObject));
            return row;
        }

        private TextMeshProUGUI AddButtonRow(string category, string name, string action, Action callback)
        {
            RectTransform row = Row(category, name);
            Label("Setting", row, name, 18f, Light, new Vector2(.02f, .08f), new Vector2(.66f, .92f), TextAlignmentOptions.Left);
            return Button("Action", row, action, new Vector2(.68f, .1f), new Vector2(.985f, .9f), callback);
        }

        private void AddStepper(string category, string name, Func<float> read, Action<float> write,
            float step, float minimum, float maximum, string format)
        {
            RectTransform row = Row(category, name);
            Label("Setting", row, name, 18f, Light, new Vector2(.02f, .08f), new Vector2(.55f, .92f), TextAlignmentOptions.Left);
            TextMeshProUGUI value = Label("Value", row, read().ToString(format), 18f, Accent,
                new Vector2(.65f, .08f), new Vector2(.81f, .92f));
            void Change(float direction)
            { float next = Mathf.Clamp(read() + direction * step, minimum, maximum);
              write(next); value.text = read().ToString(format); status.text = name + ": " + value.text; }
            Button("Decrease", row, "−", new Vector2(.57f, .1f), new Vector2(.65f, .9f), () => Change(-1f));
            Button("Increase", row, "+", new Vector2(.83f, .1f), new Vector2(.97f, .9f), () => Change(1f));
        }

        private void RefreshSearch(string query)
        {
            string needle = (query ?? string.Empty).Trim().ToLowerInvariant();
            int visible = 0;
            foreach (SettingRow row in rows)
            {
                bool match = needle.Length == 0 || row.SearchText.Contains(needle);
                row.Object.SetActive(match);
                if (match) visible++;
            }
            for (int i = 0; i < headers.Count; i++)
            {
                bool any = false;
                int end = i + 1 < headers.Count ? headers[i + 1].startRow : rows.Count;
                for (int row = headers[i].startRow; row < end; row++)
                    if (rows[row].Object.activeSelf) { any = true; break; }
                headers[i].item.SetActive(any);
            }
            if (status != null) status.text = visible == 0 ? "No settings found. Clear search to see all options." : $"{visible} settings shown";
        }

        private void BeginRebind(string id)
        {
            rebindingId = id;
            status.text = "Hold Shift, Ctrl or Alt if desired, then press a key or mouse button. Escape cancels.";
        }

        private void CaptureBinding(Keyboard keyboard, Mouse mouse)
        {
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            { rebindingId = null; status.text = "Rebinding cancelled."; return; }
            if (keyboard != null) foreach (KeyControl key in keyboard.allKeys)
            {
                if (!key.wasPressedThisFrame || key.keyCode == Key.LeftShift || key.keyCode == Key.RightShift ||
                    key.keyCode == Key.LeftCtrl || key.keyCode == Key.RightCtrl ||
                    key.keyCode == Key.LeftAlt || key.keyCode == Key.RightAlt) continue;
                CapturePrimary("<Keyboard>/" + key.name, keyboard);
                return;
            }
            if (mouse == null) return;
            string mousePath = mouse.leftButton.wasPressedThisFrame ? "<Mouse>/leftButton" :
                mouse.rightButton.wasPressedThisFrame ? "<Mouse>/rightButton" :
                mouse.middleButton.wasPressedThisFrame ? "<Mouse>/middleButton" :
                mouse.backButton.wasPressedThisFrame ? "<Mouse>/backButton" :
                mouse.forwardButton.wasPressedThisFrame ? "<Mouse>/forwardButton" : null;
            if (mousePath == null) return;
            CapturePrimary(mousePath, keyboard);
        }

        private void CapturePrimary(string primaryPath, Keyboard keyboard)
        {
            bool shift = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            bool ctrl = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
            bool alt = keyboard != null && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed);
            int modifierCount = (shift ? 1 : 0) + (ctrl ? 1 : 0) + (alt ? 1 : 0);
            if (modifierCount > 1)
            { status.text = "Use at most one modifier: Shift, Ctrl or Alt."; return; }
            PhasebreakSettings.BindingModifier modifier = shift ? PhasebreakSettings.BindingModifier.Shift :
                ctrl ? PhasebreakSettings.BindingModifier.Ctrl : alt ? PhasebreakSettings.BindingModifier.Alt :
                PhasebreakSettings.BindingModifier.None;
            string id = rebindingId;
            if (!PhasebreakSettings.TryPrepareRebind(id, primaryPath, modifier,
                    out PhasebreakSettings.PendingBindingChange change, out string message))
            {
                status.text = message;
                if (message == "Binding unchanged.") rebindingId = null;
                return;
            }
            rebindingId = null;
            if (change.HasConflicts) { ShowConflict(change); return; }
            PhasebreakSettings.Apply(change, false, out message);
            status.text = message;
            RefreshBindingLabels();
        }

        private void ClearBinding(string id)
        {
            rebindingId = null;
            PhasebreakSettings.Clear(id, out string message);
            status.text = message;
            RefreshBindingLabels();
        }

        private void BuildConflictModal()
        {
            conflictRoot = Rect("Binding Conflict Backdrop", root, new Color(.015f, .014f, .012f, .82f), true);
            Stretch(conflictRoot, 0f);
            RectTransform panel = Rect("Binding Conflict", conflictRoot, Back, true);
            PhasebreakUiTheme.StyleSurface(panel.GetComponent<UnityEngine.UI.Image>(), Back);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.sizeDelta = new Vector2(650f, 380f);
            conflictHeading = Label("Heading", panel, string.Empty, 25f, Accent,
                new Vector2(.07f, .76f), new Vector2(.93f, .92f));
            conflictBody = Label("Details", panel, string.Empty, 19f, Light,
                new Vector2(.08f, .30f), new Vector2(.92f, .74f));
            conflictBody.textWrappingMode = TextWrappingModes.Normal;
            Button("Replace", panel, "REPLACE", new Vector2(.10f, .09f), new Vector2(.47f, .23f), ReplacePendingBinding);
            Button("Cancel", panel, "CANCEL", new Vector2(.53f, .09f), new Vector2(.90f, .23f), CancelPendingBinding);
            conflictRoot.gameObject.SetActive(false);
        }

        private void ShowConflict(PhasebreakSettings.PendingBindingChange change)
        {
            pendingBindingChange = change;
            hasPendingBindingChange = true;
            conflictHeading.text = change.ProposedDisplay.ToUpperInvariant() + " IS ALREADY BOUND";
            conflictBody.text = change.ProposedDisplay + " is currently assigned to:\n" +
                change.ExistingOwnersLabel + "\n\nBinding it to:\n" + change.DestinationLabel +
                "\n\nwill leave the previous action unbound.";
            conflictRoot.gameObject.SetActive(true);
        }

        private void ReplacePendingBinding()
        {
            if (!hasPendingBindingChange) return;
            PhasebreakSettings.Apply(pendingBindingChange, true, out string message);
            HideConflict();
            status.text = message;
            RefreshBindingLabels();
        }

        private void CancelPendingBinding()
        {
            HideConflict();
            status.text = "Binding change cancelled.";
            RefreshBindingLabels();
        }

        private void HideConflict()
        {
            hasPendingBindingChange = false;
            pendingBindingChange = default;
            if (conflictRoot != null) conflictRoot.gameObject.SetActive(false);
        }

        private void RefreshBindingLabels()
        {
            foreach (KeyValuePair<string, TextMeshProUGUI> pair in bindingLabels)
                pair.Value.text = PhasebreakSettings.Display(pair.Key);
        }

        private void CollectResolutions()
        {
            foreach (Resolution resolution in Screen.resolutions)
            {
                Vector2Int size = new(resolution.width, resolution.height);
                if (!resolutions.Contains(size)) resolutions.Add(size);
            }
            if (resolutions.Count == 0) resolutions.Add(new Vector2Int(Screen.width, Screen.height));
            resolutions.Sort((left, right) =>
            {
                int area = ((long)left.x * left.y).CompareTo((long)right.x * right.y);
                return area != 0 ? area : left.x.CompareTo(right.x);
            });
            resolutionIndex = resolutions.IndexOf(new Vector2Int(Screen.width, Screen.height));
            if (resolutionIndex < 0)
            {
                resolutionIndex = -1;
                long currentArea = (long)Screen.width * Screen.height;
                for (int i = 0; i < resolutions.Count; i++)
                    if ((long)resolutions[i].x * resolutions[i].y <= currentArea) resolutionIndex = i;
            }
        }

        private Vector2Int CycleResolution()
        {
            resolutionIndex = (resolutionIndex + 1) % resolutions.Count;
            Vector2Int size = resolutions[resolutionIndex];
            Screen.SetResolution(size.x, size.y, Screen.fullScreenMode);
            PhasebreakSettings.SetInt("width", size.x);
            PhasebreakSettings.SetInt("height", size.y);
            status.text = $"Resolution: {size.x} × {size.y}";
            return size;
        }

        private FullScreenMode CycleDisplayMode()
        {
            FullScreenMode mode = Screen.fullScreenMode switch
            {
                FullScreenMode.Windowed => FullScreenMode.FullScreenWindow,
                FullScreenMode.FullScreenWindow => FullScreenMode.ExclusiveFullScreen,
                _ => FullScreenMode.Windowed
            };
            Screen.SetResolution(Screen.width, Screen.height, mode);
            PhasebreakSettings.SetInt("displayMode", (int)mode);
            status.text = "Display mode: " + mode;
            return mode;
        }

        private static void ApplyVideoPrefs()
        {
            int width = PhasebreakSettings.GetInt("width", Screen.width);
            int height = PhasebreakSettings.GetInt("height", Screen.height);
            FullScreenMode mode = (FullScreenMode)PhasebreakSettings.GetInt("displayMode", (int)Screen.fullScreenMode);
            if (PhasebreakSettings.Has("width") && PhasebreakSettings.Has("height") && width > 0 && height > 0)
                Screen.SetResolution(width, height, mode);
            QualitySettings.vSyncCount = PhasebreakSettings.GetInt("vsync", QualitySettings.vSyncCount);
            int quality = PhasebreakSettings.GetInt("quality", QualitySettings.GetQualityLevel());
            if (quality >= 0 && quality < QualitySettings.names.Length) QualitySettings.SetQualityLevel(quality, true);
        }

        private static void ApplyUiScale()
        {
            float scale = Mathf.Clamp(PhasebreakSettings.GetFloat("uiScale", 1f), .7f, 1.5f);
            foreach (UnityEngine.UI.CanvasScaler scaler in FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsSortMode.None))
            {
                string name = scaler.gameObject.name;
                if (name != "Phasebreak HUD Canvas" && name != "World and Quest UI" &&
                    name != "Phasebreak Menus" && name != "Phasebreak Game Menu") continue;
                if (scaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
                    scaler.referenceResolution = new Vector2(1920f / scale, 1080f / scale);
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static RectTransform Rect(string name, Transform parent, Color color, bool raycast)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value, float size, Color color,
            Vector2 min, Vector2 max, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = size; label.color = color; label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap; label.raycastTarget = false;
            Anchors(label.rectTransform, min, max);
            return label;
        }

        private static TextMeshProUGUI Button(string name, Transform parent, string text, Vector2 min, Vector2 max, Action click)
        {
            RectTransform rect = Rect(name, parent, Raised, true);
            Anchors(rect, min, max);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            PhasebreakUiTheme.StyleButton(button);
            button.onClick.AddListener(() => click());
            return Label("Label", rect, text, 17f, Light, Vector2.zero, Vector2.one);
        }
    }
}
