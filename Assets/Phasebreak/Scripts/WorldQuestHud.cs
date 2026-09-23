using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private WorldMapDefinition mapDefinition;
        private QuestLocation[] mapLocations;
        private QuestNpc[] mapNpcs;
        private FrontierEncounterZone[] mapEncounters;
        private readonly List<GameObject> journalRows = new();
        private QuestJournal journal;
        private PlayerBuildSystem build;
        private Transform player;
        private InputAction mapAction;
        private InputAction journalAction;
        private InputAction closeAction;
        private RectTransform canvas;
        private RectTransform mapWindow;
        private RectTransform journalWindow;
        private RectTransform vendorWindow;
        private RectTransform modalBackdrop;
        private RectTransform trackerPanel;
        private RectTransform miniPanel;
        private RectTransform mapSurface;
        private RectTransform toastPanel;
        private RectTransform journalList;
        private RectTransform minimap;
        private UnityEngine.UI.RawImage minimapTerrain;
        private UnityEngine.UI.RawImage minimapRoads;
        private Rect minimapWorldView;
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
        private TextMeshProUGUI mapDetails;
        private TextMeshProUGUI vendorBalance;
        private TextMeshProUGUI vendorFeedback;
        private string selectedMapName;
        private string selectedMapId;
        private Vector2 selectedMapWorld;
        private CanvasGroup toastGroup;
        private string selectedJournalId;
        private float toastUntil;
        private bool mapOpen;
        private bool journalOpen;
        private bool vendorOpen;
        public static bool IsWorldMenuOpen { get; private set; }
        public static bool IsMapOpen => instance != null && instance.mapOpen;
        public static bool IsJournalOpen => instance != null && instance.journalOpen;
        public static void ToggleMapMenu() => instance?.ToggleMap();
        public static void ToggleJournalMenu() => instance?.ToggleJournal();
        public static void CloseWorldMenus()
        {
            WorldQuestHud hud = instance != null ? instance : FindAnyObjectByType<WorldQuestHud>();
            if (hud != null && (hud.mapOpen || hud.journalOpen || hud.vendorOpen)) hud.CloseWindows();
        }

        private static readonly Color Back = PhasebreakUiTheme.Window;
        private static readonly Color Raised = PhasebreakUiTheme.Raised;
        private static readonly Color Text = PhasebreakUiTheme.Text;
        private static readonly Color Muted = PhasebreakUiTheme.MutedText;
        private static readonly Color Cyan = PhasebreakUiTheme.Accent;
        private static readonly Color Purple = new(.55f, .35f, .84f, 1f);

        private void Awake()
        {
            instance = this;
            journal = FindAnyObjectByType<QuestJournal>();
            mapDefinition = Resources.LoadAll<WorldMapDefinition>("Maps")
                .FirstOrDefault(map => map.sceneName == SceneManager.GetActiveScene().name);
            mapLocations = FindObjectsByType<QuestLocation>();
            mapNpcs = FindObjectsByType<QuestNpc>();
            mapEncounters = FindObjectsByType<FrontierEncounterZone>();
            player = journal != null ? journal.transform : FindAnyObjectByType<PlayerProgression>()?.transform;
            build = player != null ? player.GetComponent<PlayerBuildSystem>() : null;
            mapAction = PhasebreakSettings.Button("worldmap", "World Map");
            journalAction = PhasebreakSettings.Button("journal", "Quest Journal");
            closeAction = new InputAction("Close World UI", InputActionType.Button, "<Keyboard>/escape");
            Build();
            Refresh();
        }

        private void OnEnable()
        {
            instance = this;
            PhasebreakSettings.BindingsChanged += RefreshBindingHints;
            mapAction?.Enable(); journalAction?.Enable(); closeAction?.Enable();
            if (journal != null) { journal.Changed += Refresh; journal.Message += ShowMessage; journal.QuartermasterRequested += OpenVendor; }
        }

        private void OnDisable()
        {
            IsWorldMenuOpen = false;
            PhasebreakSettings.BindingsChanged -= RefreshBindingHints;
            mapAction?.Disable(); journalAction?.Disable(); closeAction?.Disable();
            if (journal != null) { journal.Changed -= Refresh; journal.Message -= ShowMessage; journal.QuartermasterRequested -= OpenVendor; }
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            PhasebreakSettings.Unregister(mapAction); PhasebreakSettings.Unregister(journalAction);
            mapAction?.Dispose(); journalAction?.Dispose(); closeAction?.Dispose();
        }

        private void Update()
        {
            if (!GameplayInputFocus.GameplayInputBlocked)
            {
                if (mapAction.WasPressedThisFrame()) ToggleMap();
                else if (journalAction.WasPressedThisFrame()) ToggleJournal();
                else if (closeAction.WasPressedThisFrame() && (mapOpen || journalOpen || vendorOpen)) { CloseWindows(); GameplayInputFocus.ConsumeFrame(); }
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
                if (minimapTerrain != null) UpdateMinimapView(playerXZ);
                PositionMarker(minimapPlayer, minimapTerrain != null ? MinimapPosition(playerXZ) : new Vector2(.5f, .5f));
                PositionMarker(mapPlayer, WorldToMap(playerXZ));
                foreach ((RectTransform marker, Vector2 world) in miniLocations)
                    PositionMarker(marker, minimapTerrain != null ? MinimapPosition(world) : MiniPosition(playerXZ, world));
                QuestDefinition active = journal?.ActiveQuest;
                QuestDefinition next = journal?.NextAvailableQuest;
                if (active != null || next != null)
                {
                    Vector2 objective = active != null ? ObjectivePosition(active) : NpcPosition(next.giverNpcId, next.mapPosition);
                    PositionMarker(minimapObjective, minimapTerrain != null ? MinimapPosition(objective) : MiniPosition(playerXZ, objective));
                }
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

            miniPanel = Block("Zone Compass", canvas, Back);
            Pin(miniPanel, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(360f, 240f));
            PhasebreakUiTheme.StyleSurface(miniPanel.GetComponent<UnityEngine.UI.Image>(), Back);
            Label("Mini Title", miniPanel, mapDefinition != null ? mapDefinition.displayName.ToUpperInvariant() : "NORTHGATE FRONTIER", 11f, TextAlignmentOptions.Center,
                new Vector2(.06f, .86f), new Vector2(.94f, .97f), Text);
            Label("North Cue", miniPanel, "N", 11f, TextAlignmentOptions.Center,
                new Vector2(.46f, .75f), new Vector2(.54f, .84f), Cyan);
            minimap = Block("Mini Map", miniPanel, Raised);
            Stretch(minimap, new Vector2(12f, 12f), new Vector2(-12f, -34f));
            PhasebreakUiTheme.StyleSurface(minimap.GetComponent<UnityEngine.UI.Image>(), Raised, false);
            if (mapDefinition != null && mapDefinition.generatedTerrain != null)
            {
                minimap.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                minimapTerrain = MiniImage("Terrain", mapDefinition.generatedTerrain);
                if (mapDefinition.generatedRoads != null)
                    minimapRoads = MiniImage("Roads", mapDefinition.generatedRoads);
            }
            if (mapDefinition != null)
            {
                foreach (QuestLocation location in mapLocations)
                    MiniLandmark(location.DisplayName, new Vector2(location.transform.position.x, location.transform.position.z), Cyan);
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

            trackerPanel = Block("Quest Tracker", canvas, Back);
            Pin(trackerPanel, new Vector2(1f, 1f), new Vector2(-24f, -278f), new Vector2(360f, 130f));
            PhasebreakUiTheme.StyleSurface(trackerPanel.GetComponent<UnityEngine.UI.Image>(), Back);
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

            mapWindow = Window("World Map", mapDefinition != null ? mapDefinition.displayName.ToUpperInvariant() : "NORTHGATE FRONTIER", new Vector2(.12f, .09f), new Vector2(.88f, .91f));
            mapSurface = Block("Map Surface", mapWindow, new Color(.046f, .068f, .071f, 1f));
            Place(mapSurface, mapDefinition != null ? new Vector2(.265f, .095f) : new Vector2(.035f, .095f),
                mapDefinition != null ? new Vector2(.735f, .87f) : new Vector2(.965f, .87f));
            if (mapDefinition != null)
            {
                mapSurface.anchorMin = new Vector2(.5f, .095f);
                mapSurface.anchorMax = new Vector2(.5f, .87f);
                var aspect = mapSurface.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.HeightControlsWidth;
                aspect.aspectRatio = mapDefinition.worldBounds.width / mapDefinition.worldBounds.height;
            }
            if (mapDefinition != null && mapDefinition.generatedTerrain != null)
            {
                MapImage("Terrain", mapDefinition.generatedTerrain);
                if (mapDefinition.generatedRoads != null) MapImage("Roads", mapDefinition.generatedRoads);
                foreach (WorldMapRegion region in mapDefinition.regions ?? Array.Empty<WorldMapRegion>())
                    MapRegion(region.name, region.worldPosition);
                foreach (QuestLocation location in mapLocations)
                    MapPoint(mapSurface, location.DisplayName.ToUpperInvariant(),
                        new Vector2(location.transform.position.x, location.transform.position.z), Cyan, location.Id);
            }
            else
            {
                Label("Map Region", mapSurface, "THE FRONTIER", 34f, TextAlignmentOptions.Center,
                    new Vector2(.28f, .7f), new Vector2(.72f, .82f), new Color(.18f, .29f, .28f, 1f));
                Label("Map South", mapSurface, "LOWLANDS", 26f, TextAlignmentOptions.Center,
                    new Vector2(.34f, .16f), new Vector2(.66f, .27f), new Color(.18f, .29f, .28f, 1f));
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
            AddMapInteraction(mapObjective, () => ShowObjectiveDetails(), () => RestoreMapDetails(), () => SelectObjective());
            mapPlayer = Diamond("Player Marker", mapSurface, new Color(.75f, .95f, 1f), 18f);
            RectTransform detailPanel = Block("Map Details", mapWindow, Back);
            Place(detailPanel, new Vector2(.75f, .15f), new Vector2(.965f, .85f));
            PhasebreakUiTheme.StyleSurface(detailPanel.GetComponent<UnityEngine.UI.Image>(), Back);
            mapDetails = Label("Map Detail Text", detailPanel, string.Empty, 17f, TextAlignmentOptions.TopLeft,
                new Vector2(.07f, .06f), new Vector2(.93f, .94f), Text);
            RestoreMapDetails();
            mapFooter = Label("Map Footer", mapWindow, $"LIGHT MARKER: YOU     GOLD MARKER: OBJECTIVE     [{PhasebreakSettings.Display("worldmap")}] CLOSE", 13f, TextAlignmentOptions.Center,
                new Vector2(.1f, .02f), new Vector2(.9f, .075f), Muted);
            mapWindow.gameObject.SetActive(false);

            vendorWindow = Window("Quartermaster", "QUARTERMASTER ORIN", new Vector2(.27f, .17f), new Vector2(.73f, .83f));
            Label("Vendor Introduction", vendorWindow, "Spend Rift Marks on supplies for the road and the crypt.", 17f,
                TextAlignmentOptions.Left, new Vector2(.05f, .79f), new Vector2(.95f, .86f), Muted);
            vendorBalance = Label("Rift Marks Balance", vendorWindow, string.Empty, 20f,
                TextAlignmentOptions.Left, new Vector2(.05f, .72f), new Vector2(.95f, .79f), Text);
            if (journal != null)
            {
                for (int i = 0; i < journal.QuartermasterStock.Count; i++)
                {
                    QuartermasterOffer offer = journal.QuartermasterStock[i];
                    PhasebreakItemDefinition item = build?.FindItemById(offer.ItemId);
                    float bottom = .62f - i * .11f;
                    RectTransform row = Block("Supply " + offer.ItemId, vendorWindow, Raised);
                    Place(row, new Vector2(.05f, bottom), new Vector2(.95f, bottom + .09f));
                    Label("Supply Name", row, item != null ? item.displayName : offer.ItemId, 17f,
                        TextAlignmentOptions.Left, new Vector2(.025f, .48f), new Vector2(.76f, .98f), Text);
                    Label("Supply Effect", row, item != null ? item.description : "Unavailable", 13f,
                        TextAlignmentOptions.Left, new Vector2(.025f, .04f), new Vector2(.76f, .52f), Muted);
                    RectTransform buyRect = Block("Buy", row, Back);
                    Place(buyRect, new Vector2(.78f, .14f), new Vector2(.98f, .86f));
                    buyRect.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                    UnityEngine.UI.Button buy = buyRect.gameObject.AddComponent<UnityEngine.UI.Button>();
                    Label("Price", buyRect, $"{offer.Cost} MARKS", 14f, TextAlignmentOptions.Center,
                        Vector2.zero, Vector2.one, Text);
                    string itemId = offer.ItemId;
                    buy.onClick.AddListener(() => TryBuy(itemId));
                }
            }
            vendorFeedback = Label("Vendor Feedback", vendorWindow, "Choose a supply to purchase.", 16f,
                TextAlignmentOptions.Left, new Vector2(.05f, .07f), new Vector2(.95f, .16f), Muted);
            vendorWindow.gameObject.SetActive(false);

            journalWindow = Window("Quest Journal", "FIELD JOURNAL", new Vector2(.17f, .22f), new Vector2(.83f, .78f));
            journalList = Block("Quest List", journalWindow, Raised);
            Place(journalList, new Vector2(.035f, .08f), new Vector2(.36f, .85f));
            Label("Quest List Heading", journalList, "QUESTS", 14f, TextAlignmentOptions.Left,
                new Vector2(.06f, .89f), new Vector2(.94f, .98f), Cyan);
            RectTransform journalDetailPanel = Block("Quest Details", journalWindow, Raised);
            Place(journalDetailPanel, new Vector2(.38f, .08f), new Vector2(.965f, .85f));
            journalDetails = Label("Quest Detail Text", journalDetailPanel, string.Empty, 20f, TextAlignmentOptions.TopLeft,
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

        private void MapPoint(RectTransform parent, string name, Vector2 world, Color color, string id = null)
        {
            RectTransform marker = Diamond(name + " Marker", parent, color, 13f);
            PositionMarker(marker, WorldToMap(world));
            AddMapInteraction(marker, () => ShowMapDetails(name, id, world), RestoreMapDetails,
                () => { selectedMapName = name; selectedMapId = id; selectedMapWorld = world; ShowMapDetails(name, id, world); });
            TextMeshProUGUI label = Label(name + " Label", parent, name, 12f, TextAlignmentOptions.Center,
                WorldToMap(world), WorldToMap(world), Text);
            label.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            label.rectTransform.sizeDelta = new Vector2(150f, 20f);
        }

        private void MapRegion(string name, Vector2 world)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            Vector2 position = WorldToMap(world);
            TextMeshProUGUI label = Label(name + " Region", mapSurface, name.ToUpperInvariant(), 16f,
                TextAlignmentOptions.Center, position, position, new Color(.86f, .79f, .63f, .95f));
            label.rectTransform.sizeDelta = new Vector2(210f, 30f);
        }

        private static void AddMapInteraction(RectTransform marker, Action enter, Action exit, Action click)
        {
            RectTransform hitArea = Block("Marker Hit Area", marker, Color.clear);
            hitArea.anchorMin = hitArea.anchorMax = new Vector2(.5f, .5f);
            hitArea.sizeDelta = new Vector2(34f, 34f);
            UnityEngine.UI.Image image = hitArea.GetComponent<UnityEngine.UI.Image>();
            image.raycastTarget = true;
            EventTrigger trigger = hitArea.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerEnter, enter);
            AddEvent(trigger, EventTriggerType.PointerExit, exit);
            AddEvent(trigger, EventTriggerType.PointerClick, click);
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType type, Action action)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void ShowMapDetails(string name, string id, Vector2 world)
        {
            if (mapDetails == null) return;
            bool discovered = !string.IsNullOrEmpty(id) && journal != null && journal.IsDiscovered(id);
            QuestDefinition active = journal?.ActiveQuest;
            bool objectiveHere = active != null && !journal.ObjectiveReady &&
                string.Equals(active.targetId, id, StringComparison.OrdinalIgnoreCase);
            bool returnHere = active != null && journal.ObjectiveReady &&
                string.Equals(active.turnInNpcId, id, StringComparison.OrdinalIgnoreCase);
            string purpose = returnHere ? "Return here to complete: " + active.title
                : objectiveHere ? "Quest destination: " + active.title
                : discovered ? "Discovered landmark" : "Landmark";
            string distance = player == null ? string.Empty :
                $"\n\n{Mathf.RoundToInt(Vector2.Distance(new Vector2(player.position.x, player.position.z), world))} m from you";
            mapDetails.text = $"<color=#C9A86C><size=21><b>{name}</b></size></color>\n\n{purpose}{distance}\n\n<color=#8998AA>Hover or select another marker for details.</color>";
        }

        private void ShowObjectiveDetails()
        {
            if (mapDetails == null) return;
            QuestDefinition active = journal?.ActiveQuest;
            if (active == null)
            {
                QuestDefinition next = journal?.NextAvailableQuest;
                mapDetails.text = next == null
                    ? "<color=#C9A86C><size=21><b>THE FRONTIER</b></size></color>\n\nSelect a landmark for details. The light marker shows your location."
                    : $"<color=#C9A86C><size=21><b>NEXT QUEST</b></size></color>\n\n{next.title}\n\nSpeak with {NpcName(next.giverNpcId)} to begin.";
                return;
            }
            string purpose = journal.ObjectiveReady ? $"Return to {active.turnInNpcId.Replace('-', ' ')}" : active.objectiveText;
            mapDetails.text = $"<color=#C9A86C><size=21><b>CURRENT OBJECTIVE</b></size></color>\n\n{active.title}\n\n{purpose}\n\n{journal.Progress}/{active.requiredCount}";
        }

        private void SelectObjective()
        {
            selectedMapName = null;
            selectedMapId = null;
            ShowObjectiveDetails();
        }

        private void RestoreMapDetails()
        {
            if (mapDetails == null) return;
            if (!string.IsNullOrEmpty(selectedMapName))
                ShowMapDetails(selectedMapName, selectedMapId, selectedMapWorld);
            else if (journal?.ActiveQuest != null || journal?.NextAvailableQuest != null) ShowObjectiveDetails();
            else mapDetails.text = "<color=#C9A86C><size=21><b>THE FRONTIER</b></size></color>\n\nSelect a landmark to see its name and distance.\n\nThe light marker shows your location.";
        }

        private void MapImage(string name, Sprite sprite)
        {
            RectTransform layer = Block(name, mapSurface, Color.white);
            Place(layer, Vector2.zero, Vector2.one);
            layer.GetComponent<UnityEngine.UI.Image>().sprite = sprite;
        }

        private UnityEngine.UI.RawImage MiniImage(string name, Sprite sprite)
        {
            GameObject layer = new(name, typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            layer.transform.SetParent(minimap, false);
            RectTransform rect = layer.GetComponent<RectTransform>();
            Place(rect, Vector2.zero, Vector2.one);
            UnityEngine.UI.RawImage image = layer.GetComponent<UnityEngine.UI.RawImage>();
            image.texture = sprite.texture;
            image.raycastTarget = false;
            return image;
        }

        private void UpdateMinimapView(Vector2 playerWorld)
        {
            Rect bounds = mapDefinition.worldBounds;
            float aspect = Mathf.Max(.01f, minimap.rect.width / Mathf.Max(1f, minimap.rect.height));
            float height = Mathf.Min(mapDefinition.minimapViewHeight, bounds.height, bounds.width / aspect);
            float width = height * aspect;
            float x = Mathf.Clamp(playerWorld.x - width * .5f, bounds.xMin, bounds.xMax - width);
            float z = Mathf.Clamp(playerWorld.y - height * .5f, bounds.yMin, bounds.yMax - height);
            minimapWorldView = new Rect(x, z, width, height);
            Rect uv = new((x - bounds.xMin) / bounds.width, (z - bounds.yMin) / bounds.height,
                width / bounds.width, height / bounds.height);
            minimapTerrain.uvRect = uv;
            if (minimapRoads != null) minimapRoads.uvRect = uv;
        }

        private Vector2 MinimapPosition(Vector2 world) => new(
            Mathf.Clamp((world.x - minimapWorldView.xMin) / minimapWorldView.width, .08f, .92f),
            Mathf.Clamp((world.y - minimapWorldView.yMin) / minimapWorldView.height, .08f, .92f));

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
            QuestDefinition next = journal?.NextAvailableQuest;
            bool hasObjective = active != null || next != null;
            minimapObjective.gameObject.SetActive(hasObjective);
            mapObjective.gameObject.SetActive(hasObjective);
            if (hasObjective)
            {
                PositionMarker(mapObjective, WorldToMap(active != null ? ObjectivePosition(active) : NpcPosition(next.giverNpcId, next.mapPosition)));
            }
            RestoreMapDetails();
            RefreshVendor();
            trackerHeading.text = active != null ? active.title.ToUpperInvariant() :
                next != null ? "NEXT: " + next.title.ToUpperInvariant() : "NO ACTIVE QUEST";
            string journalKey = PhasebreakSettings.Display("journal");
            if (active != null)
                tracker.text = $"{(journal.ObjectiveReady ? $"Return to {active.turnInNpcId.Replace('-', ' ')}" : active.objectiveText)}\n<color=#6BE7FF>{journal.Progress}/{active.requiredCount}</color>  <color=#8998AA>•  [{journalKey}] JOURNAL</color>";
            else if (next != null)
                tracker.text = $"Speak with {NpcName(next.giverNpcId)}.\n<color=#8998AA>[{journalKey}] FIELD JOURNAL</color>";
            else
                tracker.text = $"Frontier duties complete.\n<color=#8998AA>[{journalKey}] FIELD JOURNAL</color>";
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

        private void RefreshVendor()
        {
            if (vendorBalance != null)
                vendorBalance.text = $"RIFT MARKS   <color=#C9A86C>{journal?.Marks ?? 0}</color>";
        }

        private void TryBuy(string itemId)
        {
            if (journal == null || vendorFeedback == null) return;
            QuartermasterPurchaseResult result = journal.TryPurchase(itemId);
            PhasebreakItemDefinition item = build?.FindItemById(itemId);
            vendorFeedback.text = result switch
            {
                QuartermasterPurchaseResult.Purchased => $"Purchased {item?.displayName ?? itemId}. Find it in Inventory.",
                QuartermasterPurchaseResult.InsufficientMarks => "Not enough Rift Marks. Complete quests to earn more.",
                _ => "This supply is unavailable."
            };
            RefreshVendor();
        }

        private void OpenVendor()
        {
            PhasebreakInventoryHud.CloseMajorMenu();
            mapOpen = journalOpen = false;
            vendorOpen = true;
            mapWindow.gameObject.SetActive(false);
            journalWindow.gameObject.SetActive(false);
            vendorWindow.gameObject.SetActive(true);
            vendorFeedback.text = "Choose a supply to purchase.";
            modalBackdrop.gameObject.SetActive(true);
            RefreshVendor();
            SetCursor();
        }

        private void ToggleMap()
        {
            bool opening = !mapOpen;
            if (opening) PhasebreakInventoryHud.CloseMajorMenu();
            mapOpen = opening; journalOpen = vendorOpen = false;
            mapWindow.gameObject.SetActive(mapOpen); journalWindow.gameObject.SetActive(false);
            vendorWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(mapOpen); SetCursor();
        }

        private void ToggleJournal()
        {
            bool opening = !journalOpen;
            if (opening) PhasebreakInventoryHud.CloseMajorMenu();
            journalOpen = opening; mapOpen = vendorOpen = false;
            journalWindow.gameObject.SetActive(journalOpen); mapWindow.gameObject.SetActive(false);
            vendorWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(journalOpen); Refresh(); SetCursor();
        }

        private void CloseWindows()
        {
            mapOpen = journalOpen = vendorOpen = false;
            mapWindow.gameObject.SetActive(false); journalWindow.gameObject.SetActive(false);
            vendorWindow.gameObject.SetActive(false);
            modalBackdrop.gameObject.SetActive(false); SetCursor();
        }
        private void SetCursor()
        {
            IsWorldMenuOpen = mapOpen || journalOpen || vendorOpen;
            if (IsWorldMenuOpen) { Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
            PhasebreakInventoryHud.RefreshNavigation();
        }

        private void RefreshBindingHints()
        {
            if (mapFooter != null)
                mapFooter.text = $"LIGHT MARKER: YOU     GOLD MARKER: OBJECTIVE     [{PhasebreakSettings.Display("worldmap")}] CLOSE";
            Refresh();
        }

        private Vector2 ObjectivePosition(QuestDefinition quest)
        {
            if (quest == null) return Vector2.zero;
            if (mapDefinition == null) return quest.mapPosition;
            string target = journal != null && journal.ObjectiveReady ? quest.turnInNpcId : quest.targetId;
            foreach (QuestLocation location in mapLocations)
                if (location.Id == target) return new Vector2(location.transform.position.x, location.transform.position.z);
            foreach (QuestNpc npc in mapNpcs)
                if (npc.Id == target) return new Vector2(npc.transform.position.x, npc.transform.position.z);
            if (quest.objectiveKind == QuestObjectiveKind.CompleteDungeon)
            {
                QuestLocation nearest = mapLocations
                    .FirstOrDefault(location => location.Id.IndexOf("rift", StringComparison.OrdinalIgnoreCase) >= 0);
                if (nearest != null) return new Vector2(nearest.transform.position.x, nearest.transform.position.z);
            }
            if (quest.objectiveKind == QuestObjectiveKind.Defeat && player != null)
            {
                if (!string.IsNullOrWhiteSpace(quest.mapEncounterZoneId))
                {
                    FrontierEncounterZone authored = mapEncounters.FirstOrDefault(zone => zone != null &&
                        string.Equals(zone.ZoneId, quest.mapEncounterZoneId, StringComparison.OrdinalIgnoreCase));
                    if (authored != null)
                        return new Vector2(authored.transform.position.x, authored.transform.position.z);
                }
                FrontierEncounterZone nearest = null;
                float distance = float.MaxValue;
                foreach (FrontierEncounterZone zone in mapEncounters)
                {
                    if (zone == null) continue;
                    float next = (zone.transform.position - player.position).sqrMagnitude;
                    if (next >= distance) continue;
                    nearest = zone;
                    distance = next;
                }
                if (nearest != null) return new Vector2(nearest.transform.position.x, nearest.transform.position.z);
            }
            return quest.mapPosition;
        }

        private Vector2 NpcPosition(string npcId, Vector2 fallback)
        {
            QuestNpc npc = mapNpcs.FirstOrDefault(candidate => candidate != null &&
                string.Equals(candidate.Id, npcId, StringComparison.OrdinalIgnoreCase));
            return npc != null ? new Vector2(npc.transform.position.x, npc.transform.position.z) : fallback;
        }

        private string NpcName(string npcId) => mapNpcs.FirstOrDefault(candidate => candidate != null &&
            string.Equals(candidate.Id, npcId, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? npcId.Replace('-', ' ');

        private Vector2 WorldToMap(Vector2 world) => mapDefinition != null
            ? mapDefinition.WorldToMap(new Vector3(world.x, 0f, world.y))
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
