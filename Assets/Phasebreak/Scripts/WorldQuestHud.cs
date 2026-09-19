using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(220), DisallowMultipleComponent]
    public sealed class WorldQuestHud : MonoBehaviour
    {
        private static WorldQuestHud instance;
        private readonly List<(RectTransform marker, Vector2 world)> miniLocations = new();
        private readonly List<(RectTransform line, Vector2 from, Vector2 to)> mapRoads = new();
        private readonly List<GameObject> journalRows = new();
        private QuestJournal journal;
        private Transform player;
        private InputAction mapAction;
        private InputAction journalAction;
        private InputAction closeAction;
        private RectTransform canvas;
        private RectTransform mapWindow;
        private RectTransform journalWindow;
        private RectTransform modalBackdrop;
        private RectTransform trackerPanel;
        private RectTransform miniPanel;
        private RectTransform mapSurface;
        private RectTransform toastPanel;
        private RectTransform journalList;
        private RectTransform minimap;
        private RectTransform minimapPlayer;
        private RectTransform minimapObjective;
        private RectTransform mapPlayer;
        private RectTransform mapObjective;
        private TextMeshProUGUI tracker;
        private TextMeshProUGUI trackerHeading;
        private TextMeshProUGUI prompt;
        private TextMeshProUGUI toast;
        private TextMeshProUGUI journalDetails;
        private TextMeshProUGUI mapFooter;
        private CanvasGroup toastGroup;
        private string selectedJournalId;
        private float toastUntil;
        private bool mapOpen;
        private bool journalOpen;
        public static bool IsWorldMenuOpen { get; private set; }
        public static void CloseWorldMenus()
        {
            WorldQuestHud hud = instance != null ? instance : FindAnyObjectByType<WorldQuestHud>();
            if (hud != null && (hud.mapOpen || hud.journalOpen)) hud.CloseWindows();
        }

        private static readonly Color Back = new(.014f, .022f, .038f, .96f);
        private static readonly Color Raised = new(.031f, .049f, .077f, .96f);
        private static readonly Color Text = new(.9f, .94f, 1f, 1f);
        private static readonly Color Muted = new(.55f, .64f, .74f, 1f);
        private static readonly Color Cyan = new(.12f, .74f, .89f, 1f);
        private static readonly Color Purple = new(.55f, .35f, .84f, 1f);

        private void Awake()
        {
            instance = this;
            journal = FindAnyObjectByType<QuestJournal>();
            player = journal != null ? journal.transform : FindAnyObjectByType<PlayerProgression>()?.transform;
            mapAction = new InputAction("World Map", InputActionType.Button, "<Keyboard>/m");
            journalAction = new InputAction("Quest Journal", InputActionType.Button, "<Keyboard>/j");
            closeAction = new InputAction("Close World UI", InputActionType.Button, "<Keyboard>/escape");
            Build();
            Refresh();
        }

        private void OnEnable()
        {
            instance = this;
            mapAction?.Enable(); journalAction?.Enable(); closeAction?.Enable();
            if (journal != null) { journal.Changed += Refresh; journal.Message += ShowMessage; }
        }

        private void OnDisable()
        {
            IsWorldMenuOpen = false;
            mapAction?.Disable(); journalAction?.Disable(); closeAction?.Disable();
            if (journal != null) { journal.Changed -= Refresh; journal.Message -= ShowMessage; }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            mapAction?.Dispose(); journalAction?.Dispose(); closeAction?.Dispose();
        }

        private void Update()
        {
            if (!GameplayInputFocus.GameplayInputBlocked)
            {
                if (mapAction.WasPressedThisFrame()) ToggleMap();
                else if (journalAction.WasPressedThisFrame()) ToggleJournal();
                else if (closeAction.WasPressedThisFrame() && (mapOpen || journalOpen)) CloseWindows();
            }
            bool modal = IsWorldMenuOpen || PhasebreakInventoryHud.IsMajorMenuOpen;
            if (trackerPanel != null) trackerPanel.gameObject.SetActive(!modal);
            if (miniPanel != null) miniPanel.gameObject.SetActive(!modal);
            if (prompt != null)
            {
                prompt.text = journal?.InteractionPrompt ?? string.Empty;
                prompt.gameObject.SetActive(!modal && !string.IsNullOrEmpty(prompt.text));
            }
            if (toastPanel != null)
            {
                float remaining = toastUntil - Time.unscaledTime;
                toastPanel.gameObject.SetActive(!modal && remaining > 0f);
                if (toastGroup != null) toastGroup.alpha = Mathf.Clamp01(remaining / .7f);
            }
            if (player != null && minimapPlayer != null)
            {
                Vector2 playerXZ = new(player.position.x, player.position.z);
                PositionMarker(minimapPlayer, new Vector2(.5f, .5f));
                PositionMarker(mapPlayer, WorldToMap(playerXZ));
                foreach ((RectTransform marker, Vector2 world) in miniLocations)
                    PositionMarker(marker, MiniPosition(playerXZ, world));
                QuestDefinition active = journal?.ActiveQuest;
                if (active != null) PositionMarker(minimapObjective, MiniPosition(playerXZ, V2Objective(active)));
                minimapPlayer.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            }
            if (mapOpen && mapSurface != null)
            {
                foreach ((RectTransform line, Vector2 from, Vector2 to) in mapRoads)
                    PositionRoad(line, mapSurface.rect, WorldToMap(from), WorldToMap(to));
            }
        }

        private void Build()
        {
            GameObject root = new("World and Quest UI", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false);
            UnityEngine.UI.CanvasScaler scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            Canvas uiCanvas = root.GetComponent<Canvas>(); uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay; uiCanvas.sortingOrder = 32;
            canvas = root.GetComponent<RectTransform>();

            miniPanel = Block("Zone Compass", canvas, new Color(.012f, .018f, .03f, .9f));
            Pin(miniPanel, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(224f, 224f));
            Label("Mini Title", miniPanel, "NORTHGATE FRONTIER", 11f, TextAlignmentOptions.Center,
                new Vector2(.06f, .86f), new Vector2(.94f, .97f), Text);
            Label("North Cue", miniPanel, "N", 11f, TextAlignmentOptions.Center,
                new Vector2(.46f, .75f), new Vector2(.54f, .84f), Cyan);
            minimap = Block("Mini Map", miniPanel, new Color(.04f, .058f, .066f, .96f));
            Stretch(minimap, new Vector2(12f, 12f), new Vector2(-12f, -34f));
            if (IsFrontierV2)
            {
                MiniLandmark("Northgate", new Vector2(850, 870), new Color(.36f, .75f, .77f));
                MiniLandmark("Westmere", new Vector2(470, 985), new Color(.36f, .75f, .77f));
                MiniLandmark("Eastwatch", new Vector2(1390, 1010), new Color(.36f, .75f, .77f));
                MiniLandmark("Rift Crypt", new Vector2(1450, 480), new Color(.65f, .43f, .82f));
                MiniLandmark("Ancient Ruins", new Vector2(1020, 1420), new Color(.75f, .52f, .36f));
            }
            else
            {
            MiniLandmark("Northgate", new Vector2(-55f, 75f), new Color(.36f, .75f, .77f));
            MiniLandmark("Westmere", new Vector2(-103f, -47f), new Color(.36f, .75f, .77f));
            MiniLandmark("Eastwatch", new Vector2(105f, 53f), new Color(.36f, .75f, .77f));
            MiniLandmark("Rift Crypt", new Vector2(-8f, 8f), new Color(.65f, .43f, .82f));
            MiniLandmark("Northwest Ruins", new Vector2(-120f, 101f), new Color(.75f, .52f, .36f));
            }
            minimapObjective = Diamond("Objective Marker", minimap, new Color(1f, .72f, .28f), 12f);
            minimapPlayer = Diamond("Player Marker", minimap, new Color(.76f, .95f, 1f), 12f);

            trackerPanel = Block("Quest Tracker", canvas, new Color(.014f, .022f, .038f, .86f));
            Pin(trackerPanel, new Vector2(1f, 1f), new Vector2(-24f, -262f), new Vector2(310f, 108f));
            Stripe(trackerPanel, Cyan, new Vector2(0f, 0f), new Vector2(.012f, 1f));
            trackerHeading = Label("Tracker Heading", trackerPanel, string.Empty, 17f, TextAlignmentOptions.Left,
                new Vector2(.055f, .67f), new Vector2(.95f, .94f), Text);
            tracker = Label("Tracker Body", trackerPanel, string.Empty, 15f, TextAlignmentOptions.TopLeft,
                new Vector2(.055f, .1f), new Vector2(.95f, .66f), Muted);

            prompt = Label("NPC Prompt", canvas, string.Empty, 20f, TextAlignmentOptions.Center,
                new Vector2(.3f, .19f), new Vector2(.7f, .25f), Text);
            toastPanel = Block("World Toast", canvas, new Color(.016f, .027f, .043f, .96f));
            Place(toastPanel, new Vector2(.315f, .8f), new Vector2(.685f, .9f));
            Stripe(toastPanel, Purple, new Vector2(0f, 0f), new Vector2(.008f, 1f));
            toast = Label("Toast Text", toastPanel, string.Empty, 18f, TextAlignmentOptions.Center,
                new Vector2(.04f, .08f), new Vector2(.96f, .92f), Text);
            toastGroup = toastPanel.gameObject.AddComponent<CanvasGroup>();
            toastPanel.gameObject.SetActive(false);

            modalBackdrop = Block("World Menu Backdrop", canvas, new Color(0f, .008f, .018f, .7f));
            Place(modalBackdrop, Vector2.zero, Vector2.one);
            modalBackdrop.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            modalBackdrop.gameObject.SetActive(false);

            mapWindow = Window("World Map", "NORTHGATE FRONTIER", new Vector2(.12f, .09f), new Vector2(.88f, .91f));
            mapSurface = Block("Map Surface", mapWindow, new Color(.046f, .068f, .071f, 1f));
            Place(mapSurface, new Vector2(.035f, .095f), new Vector2(.965f, .87f));
            Label("Map Region", mapSurface, "THE FRONTIER", 34f, TextAlignmentOptions.Center,
                new Vector2(.28f, .7f), new Vector2(.72f, .82f), new Color(.18f, .29f, .28f, 1f));
            Label("Map South", mapSurface, "LOWLANDS", 26f, TextAlignmentOptions.Center,
                new Vector2(.34f, .16f), new Vector2(.66f, .27f), new Color(.18f, .29f, .28f, 1f));
            if (IsFrontierV2)
            {
                MapRoad(new Vector2(145, 155), new Vector2(435, 522));
                MapRoad(new Vector2(435, 522), new Vector2(850, 870));
                MapRoad(new Vector2(850, 870), new Vector2(470, 985));
                MapRoad(new Vector2(850, 870), new Vector2(1390, 1010));
                MapRoad(new Vector2(850, 870), new Vector2(1020, 1420));
                MapRoad(new Vector2(1040, 830), new Vector2(1450, 480));
                MapPoint(mapSurface, "NORTHGATE", new Vector2(850, 870), Cyan);
                MapPoint(mapSurface, "WESTMERE", new Vector2(470, 985), Cyan);
                MapPoint(mapSurface, "EASTWATCH", new Vector2(1390, 1010), Cyan);
                MapPoint(mapSurface, "ANCIENT RUINS", new Vector2(1020, 1420), new Color(.93f, .55f, .31f));
                MapPoint(mapSurface, "RIFT CRYPT", new Vector2(1450, 480), new Color(.7f, .4f, .92f));
            }
            else
            {
            MapRoad(new Vector2(-55f, 75f), new Vector2(-103f, -47f));
            MapRoad(new Vector2(-55f, 75f), new Vector2(105f, 53f));
            MapRoad(new Vector2(-55f, 75f), new Vector2(-120f, 101f));
            MapRoad(new Vector2(-103f, -47f), new Vector2(-78f, -108f));
            MapRoad(new Vector2(105f, 53f), new Vector2(117f, -94f));
            MapRoad(new Vector2(-55f, 75f), new Vector2(-8f, 8f));
            MapPoint(mapSurface, "NORTHGATE", new Vector2(-55f, 75f), Cyan);
            MapPoint(mapSurface, "WESTMERE", new Vector2(-103f, -47f), Cyan);
            MapPoint(mapSurface, "EASTWATCH", new Vector2(105f, 53f), Cyan);
            MapPoint(mapSurface, "NORTHWEST RUINS", new Vector2(-120f, 101f), new Color(.93f, .55f, .31f));
            MapPoint(mapSurface, "SOUTHERN WATCH", new Vector2(-78f, -108f), new Color(.93f, .55f, .31f));
            MapPoint(mapSurface, "SOUTHEAST RUINS", new Vector2(117f, -94f), new Color(.93f, .55f, .31f));
            MapPoint(mapSurface, "RIFT CRYPT", new Vector2(-8f, 8f), new Color(.7f, .4f, .92f));
            }
            mapObjective = Diamond("Objective Marker", mapSurface, new Color(1f, .74f, .21f), 20f);
            mapPlayer = Diamond("Player Marker", mapSurface, new Color(.75f, .95f, 1f), 18f);
            mapFooter = Label("Map Footer", mapWindow, "LIGHT MARKER: YOU     GOLD MARKER: OBJECTIVE     [M] CLOSE", 13f, TextAlignmentOptions.Center,
                new Vector2(.1f, .02f), new Vector2(.9f, .075f), Muted);
            mapWindow.gameObject.SetActive(false);

            journalWindow = Window("Quest Journal", "FIELD JOURNAL", new Vector2(.17f, .22f), new Vector2(.83f, .78f));
            journalList = Block("Quest List", journalWindow, Raised);
            Place(journalList, new Vector2(.035f, .08f), new Vector2(.36f, .85f));
            Label("Quest List Heading", journalList, "QUESTS", 14f, TextAlignmentOptions.Left,
                new Vector2(.06f, .89f), new Vector2(.94f, .98f), Cyan);
            RectTransform detailPanel = Block("Quest Details", journalWindow, Raised);
            Place(detailPanel, new Vector2(.38f, .08f), new Vector2(.965f, .85f));
            journalDetails = Label("Quest Detail Text", detailPanel, string.Empty, 20f, TextAlignmentOptions.TopLeft,
                new Vector2(.045f, .04f), new Vector2(.955f, .96f), Text);
            journalWindow.gameObject.SetActive(false);
        }

        private RectTransform Window(string name, string title, Vector2 min, Vector2 max)
        {
            RectTransform window = Block(name, canvas, Back); Place(window, min, max);
            window.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Label("Title", window, title, 26f, TextAlignmentOptions.Left, new Vector2(.045f, .89f), new Vector2(.83f, .98f), Text);
            UnityEngine.UI.Button close = Block("Close", window, Raised).gameObject.AddComponent<UnityEngine.UI.Button>();
            close.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Place(close.GetComponent<RectTransform>(), new Vector2(.91f, .9f), new Vector2(.975f, .975f));
            Label("Close Label", close.transform, "×", 22f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Text);
            close.onClick.AddListener(CloseWindows);
            Stripe(window, Cyan, new Vector2(.025f, .875f), new Vector2(.975f, .881f));
            return window;
        }

        private void MapPoint(RectTransform parent, string name, Vector2 world, Color color)
        {
            RectTransform marker = Diamond(name + " Marker", parent, color, 13f);
            PositionMarker(marker, WorldToMap(world));
            TextMeshProUGUI label = Label(name + " Label", parent, name, 12f, TextAlignmentOptions.Center,
                WorldToMap(world), WorldToMap(world), Text);
            label.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            label.rectTransform.sizeDelta = new Vector2(150f, 20f);
        }

        private void MiniLandmark(string name, Vector2 world, Color color)
        {
            RectTransform marker = Diamond(name + " Mini Marker", minimap, color, 7f);
            miniLocations.Add((marker, world));
        }

        private void MapRoad(Vector2 from, Vector2 to)
        {
            RectTransform line = Block("Old Road", mapSurface, new Color(.22f, .33f, .31f, .72f));
            line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
            line.pivot = new Vector2(.5f, .5f);
            mapRoads.Add((line, from, to));
        }

        private static void PositionRoad(RectTransform line, Rect rect, Vector2 from, Vector2 to)
        {
            Vector2 start = new((from.x - .5f) * rect.width, (from.y - .5f) * rect.height);
            Vector2 end = new((to.x - .5f) * rect.width, (to.y - .5f) * rect.height);
            Vector2 delta = end - start;
            line.anchoredPosition = (start + end) * .5f;
            line.sizeDelta = new Vector2(delta.magnitude, 3f);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static RectTransform Diamond(string name, Transform parent, Color color, float size)
        {
            RectTransform marker = Block(name, parent, color);
            marker.sizeDelta = new Vector2(size, size);
            marker.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return marker;
        }

        private void Refresh()
        {
            if (tracker == null || journalDetails == null) return;
            QuestDefinition active = journal?.ActiveQuest;
            bool hasObjective = active != null;
            minimapObjective.gameObject.SetActive(hasObjective);
            mapObjective.gameObject.SetActive(hasObjective);
            if (hasObjective)
            {
                PositionMarker(mapObjective, WorldToMap(V2Objective(active)));
            }
            trackerHeading.text = active == null ? "NO ACTIVE QUEST" : active.title.ToUpperInvariant();
            tracker.text = active == null ? "Speak with a questgiver.\n<color=#8998AA>[J] FIELD JOURNAL</color>" :
                $"{(journal.ObjectiveReady ? $"Return to {active.turnInNpcId.Replace('-', ' ')}" : active.objectiveText)}\n<color=#6BE7FF>{journal.Progress}/{active.requiredCount}</color>  <color=#8998AA>•  [J] JOURNAL</color>";
            RefreshJournal();
        }

        private void RefreshJournal()
        {
            foreach (GameObject row in journalRows) if (row != null) Destroy(row);
            journalRows.Clear();
            QuestDefinition active = journal?.ActiveQuest;
            List<QuestDefinition> entries = new();
            if (active != null) entries.Add(active);
            if (journal != null)
                entries.AddRange(journal.CompletedIds.Select(journal.FindQuest).Where(q => q != null).OrderBy(q => q.title));
            if (entries.Count == 0)
            {
                selectedJournalId = null;
                journalDetails.text = "<color=#6BE7FF>NO QUESTS YET</color>\n\nMeet Warden Elira in Northgate to begin your journey.";
                return;
            }
            if (string.IsNullOrEmpty(selectedJournalId) || entries.All(q => q.id != selectedJournalId))
                selectedJournalId = active != null ? active.id : entries[0].id;
            for (int i = 0; i < entries.Count && i < 7; i++)
            {
                QuestDefinition quest = entries[i];
                bool selected = quest.id == selectedJournalId;
                RectTransform row = Block("Quest " + quest.id, journalList,
                    selected ? new Color(.09f, .19f, .23f, 1f) : new Color(.035f, .06f, .09f, 1f));
                Place(row, new Vector2(.04f, .77f - i * .105f), new Vector2(.96f, .855f - i * .105f));
                row.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                UnityEngine.UI.Button button = row.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => { selectedJournalId = quest.id; RefreshJournal(); });
                Label("Quest Name", row, (quest == active ? "ACTIVE  " : "DONE  ") + quest.title, 17f,
                    TextAlignmentOptions.Left, new Vector2(.04f, 0f), new Vector2(.97f, 1f), selected ? Text : Muted);
                journalRows.Add(row.gameObject);
            }
            QuestDefinition chosen = entries.First(q => q.id == selectedJournalId);
            bool completed = chosen != active;
            string status = completed ? "COMPLETED" : journal.ObjectiveReady ? "READY TO TURN IN" : "ACTIVE QUEST";
            string objective = completed ? "Objective complete" :
                $"{chosen.objectiveText}  <color=#6BE7FF>{journal.Progress}/{chosen.requiredCount}</color>";
            journalDetails.text = $"<color=#6BE7FF><size=22><b>{chosen.title}</b></size></color>\n" +
                $"<color=#8998AA>{status}</color>\n\n{chosen.description}\n\n" +
                $"<color=#8998AA>OBJECTIVE</color>\n{objective}\n\n" +
                $"<color=#8998AA>REWARDS</color>\n{chosen.experienceReward} XP  •  {chosen.marksReward} Rift Marks\n\n" +
                $"<color=#8998AA>CURRENCY</color>\n{journal.Marks} Rift Marks";
        }

        private void ShowMessage(string value)
        {
            int separator = value.IndexOf("  •  ", StringComparison.Ordinal);
            toast.text = separator < 0 ? value :
                $"<color=#6BE7FF><size=15>{value[..separator]}</size></color>\n{value[(separator + 5)..]}";
            toastUntil = Time.unscaledTime + 4.5f;
            toastPanel.gameObject.SetActive(true);
        }

        private void ToggleMap()
        {
            bool opening = !mapOpen;
            if (opening) PhasebreakInventoryHud.CloseMajorMenu();
            mapOpen = opening; journalOpen = false;
            mapWindow.gameObject.SetActive(mapOpen); journalWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(mapOpen); SetCursor();
        }

        private void ToggleJournal()
        {
            bool opening = !journalOpen;
            if (opening) PhasebreakInventoryHud.CloseMajorMenu();
            journalOpen = opening; mapOpen = false;
            journalWindow.gameObject.SetActive(journalOpen); mapWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(journalOpen); Refresh(); SetCursor();
        }

        private void CloseWindows()
        {
            mapOpen = journalOpen = false;
            mapWindow.gameObject.SetActive(false); journalWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(false); SetCursor();
        }
        private void SetCursor()
        {
            IsWorldMenuOpen = mapOpen || journalOpen;
            if (IsWorldMenuOpen) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
        }

        private static bool IsFrontierV2 => SceneManager.GetActiveScene().name == "StarterZone_V2";

        private static Vector2 V2Objective(QuestDefinition quest)
        {
            if (!IsFrontierV2 || quest == null) return quest != null ? quest.mapPosition : Vector2.zero;
            switch (quest.id)
            {
                case "frontier-01": return new Vector2(850, 870);
                case "frontier-02": return new Vector2(600, 740);
                case "frontier-03": return new Vector2(1020, 1420);
                case "frontier-04": return new Vector2(1110, 1480);
                case "frontier-05": return new Vector2(1390, 1010);
                case "frontier-06": return new Vector2(1450, 480);
                default: return quest.mapPosition;
            }
        }

        private static Vector2 WorldToMap(Vector2 world) => IsFrontierV2
            ? new Vector2(Mathf.Clamp01(world.x / 2000f), Mathf.Clamp01(world.y / 2000f))
            : new Vector2(Mathf.Clamp01((world.x + 210f) / 420f), Mathf.Clamp01((world.y + 210f) / 420f));

        private static Vector2 MiniPosition(Vector2 player, Vector2 world) => new(
            Mathf.Clamp(.5f + (world.x - player.x) / 240f, .08f, .92f),
            Mathf.Clamp(.5f + (world.y - player.y) / 240f, .08f, .92f));

        private static void PositionMarker(RectTransform marker, Vector2 normalized)
        {
            if (marker == null) return;
            marker.anchorMin = marker.anchorMax = normalized;
            marker.anchoredPosition = Vector2.zero;
        }

        private static RectTransform Block(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>(); image.color = color;
            image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>(); label.text = value; label.fontSize = size;
            label.alignment = alignment; label.color = color; label.textWrappingMode = TextWrappingModes.Normal; label.raycastTarget = false;
            Place(label.rectTransform, min, max); return label;
        }

        private static void Stripe(RectTransform parent, Color color, Vector2 min, Vector2 max) { RectTransform line = Block("Accent", parent, color); Place(line, min, max); line.GetComponent<UnityEngine.UI.Image>().raycastTarget = false; }
        private static void Place(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
        private static void Pin(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; }
    }
}
