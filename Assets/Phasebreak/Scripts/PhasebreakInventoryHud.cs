using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(150), DisallowMultipleComponent]
    public sealed class PhasebreakInventoryHud : MonoBehaviour
    {
        private enum MenuMode { None, Inventory, Character, Talents, Spellbook, Loot }
        private enum InventoryFilter { All, Weapons, Armor, Relics, Sigils }

        private static PhasebreakInventoryHud instance;
        private static readonly EquipmentSlot[] Slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

        private readonly List<GameObject> inventoryCells = new();
        private readonly List<GameObject> equipmentCells = new();
        private readonly List<GameObject> lootCells = new();
        private readonly Dictionary<MenuMode, Image> navImages = new();
        private Image journalNavImage;
        private Image mapNavImage;
        private readonly Dictionary<InventoryFilter, Image> filterImages = new();

        private PlayerBuildSystem build;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PhasebreakFollowCamera cameraController;
        private Camera worldCamera;
        private InputAction inventoryAction;
        private InputAction characterAction;
        private InputAction talentsAction;
        private InputAction spellbookAction;
        private InputAction escapeAction;
        private RectTransform canvasRect;
        private RectTransform modalBackdrop;
        private RectTransform inventoryPanel;
        private RectTransform inventoryGrid;
        private RectTransform characterPanel;
        private RectTransform equipmentStage;
        private RectTransform talentsPanel;
        private RectTransform spellbookPanel;
        private TextMeshProUGUI spellbookDetails;
        private readonly List<TextMeshProUGUI> spellbookRows = new();
        private readonly List<TextMeshProUGUI> spellbookSlots = new();
        private int selectedAbility;
        private TalentWindowView talentWindow;
        private RectTransform lootPanel;
        private RectTransform lootGrid;
        private TextMeshProUGUI inventoryCount;
        private TextMeshProUGUI inventoryStatus;
        private TextMeshProUGUI characterSummary;
        private TextMeshProUGUI characterDetails;
        private TextMeshProUGUI characterTitle;
        private Image passiveIcon;
        private TextMeshProUGUI lootTitle;
        private TextMeshProUGUI navTooltip;
        private PhasebreakItemTooltipUI itemTooltip;
        private MenuMode mode;
        private InventoryFilter inventoryFilter;
        private PhasebreakItemDefinition selectedItem;
        private int selectedInventoryIndex = -1;
        private EquipmentSlot? selectedEquipmentSlot;
        private CorpseLootContainer activeCorpse;
        private CorpseLootContainer hoveredCorpse;

        public static bool IsMajorMenuOpen
        {
            get
            {
                if (instance == null) instance = FindAnyObjectByType<PhasebreakInventoryHud>();
                return instance != null && instance.mode != MenuMode.None;
            }
        }
        public static void CloseMajorMenu()
        {
            PhasebreakInventoryHud hud = instance != null ? instance : FindAnyObjectByType<PhasebreakInventoryHud>();
            if (hud != null && hud.mode != MenuMode.None) hud.CloseMenu();
        }
        public static void RefreshNavigation() => instance?.UpdateNav();
        public static bool IsLootWindowOpenFor(CorpseLootContainer corpse) => instance != null && instance.mode == MenuMode.Loot && instance.activeCorpse == corpse;
        public static void NotifyCorpseDespawned(CorpseLootContainer corpse) { if (IsLootWindowOpenFor(corpse)) instance.CloseMenu(); }

        private static readonly Color Window = PhasebreakUiTheme.Window;
        private static readonly Color Panel = PhasebreakUiTheme.Panel;
        private static readonly Color PanelLight = PhasebreakUiTheme.Raised;
        private static readonly Color Cyan = PhasebreakUiTheme.Accent;
        private static readonly Color TextPrimary = PhasebreakUiTheme.Text;
        private static readonly Color TextMuted = PhasebreakUiTheme.MutedText;

        private void Awake()
        {
            instance = this;
            build = FindAnyObjectByType<PlayerBuildSystem>();
            combat = FindAnyObjectByType<PlayerCombat>();
            progression = FindAnyObjectByType<PlayerProgression>();
            cameraController = FindAnyObjectByType<PhasebreakFollowCamera>();
            worldCamera = Camera.main;
            inventoryAction = PhasebreakSettings.Button("inventory", "Inventory");
            characterAction = PhasebreakSettings.Button("character", "Character");
            talentsAction = PhasebreakSettings.Button("talents", "Talents");
            spellbookAction = PhasebreakSettings.Button("spellbook", "Spellbook");
            escapeAction = KeyAction("Close Menu", "<Keyboard>/escape");
            EnsureEventSystem();
            BuildUi();
            CloseMenu();
        }

        private void OnEnable() { instance = this; SetActions(true); if (build != null) build.BuildChanged += RefreshOpenPanel; }
        private void OnDisable() { SetActions(false); if (build != null) build.BuildChanged -= RefreshOpenPanel; CloseMenu(); }
        private void OnDestroy()
        {
            PhasebreakSettings.Unregister(inventoryAction); PhasebreakSettings.Unregister(characterAction); PhasebreakSettings.Unregister(talentsAction); PhasebreakSettings.Unregister(spellbookAction);
            inventoryAction?.Dispose(); characterAction?.Dispose(); talentsAction?.Dispose(); spellbookAction?.Dispose(); escapeAction?.Dispose();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (GameplayInputFocus.GameplayInputBlocked) { SetHoveredCorpse(null); return; }
            if (inventoryAction.WasPressedThisFrame()) Toggle(MenuMode.Inventory);
            else if (characterAction.WasPressedThisFrame()) Toggle(MenuMode.Character);
            else if (talentsAction.WasPressedThisFrame()) Toggle(MenuMode.Talents);
            else if (spellbookAction.WasPressedThisFrame()) Toggle(MenuMode.Spellbook);
            else if (escapeAction.WasPressedThisFrame() && mode != MenuMode.None) { CloseMenu(); GameplayInputFocus.ConsumeFrame(); }
            UpdateCorpseInteraction();
        }

        public void OpenLoot(CorpseLootContainer corpse) { if (corpse == null) return; activeCorpse = corpse; Open(MenuMode.Loot); }
        private void Toggle(MenuMode target) { if (mode == target) CloseMenu(); else Open(target); }

        private void Open(MenuMode target)
        {
            WorldQuestHud.CloseWorldMenus();
            mode = target;
            if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(target != MenuMode.Inventory);
            inventoryPanel.gameObject.SetActive(target == MenuMode.Inventory);
            characterPanel.gameObject.SetActive(target == MenuMode.Character);
            talentsPanel.gameObject.SetActive(target == MenuMode.Talents);
            spellbookPanel.gameObject.SetActive(target == MenuMode.Spellbook);
            lootPanel.gameObject.SetActive(target == MenuMode.Loot);
            itemTooltip.Hide();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshOpenPanel();
            UpdateNav();
        }

        private void CloseMenu()
        {
            mode = MenuMode.None;
            if (modalBackdrop != null) modalBackdrop.gameObject.SetActive(false);
            activeCorpse = null;
            selectedItem = null;
            selectedInventoryIndex = -1;
            selectedEquipmentSlot = null;
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            if (characterPanel != null) characterPanel.gameObject.SetActive(false);
            if (talentsPanel != null) talentsPanel.gameObject.SetActive(false);
            if (spellbookPanel != null) spellbookPanel.gameObject.SetActive(false);
            if (lootPanel != null) lootPanel.gameObject.SetActive(false);
            itemTooltip?.Hide();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UpdateNav();
        }

        private void BuildUi()
        {
            Canvas canvas = FindAnyObjectByType<PhasebreakHud>()?.GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject go = new("Phasebreak Menus", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = .5f;
            }

            canvasRect = canvas.GetComponent<RectTransform>();
            modalBackdrop = Block("Modal Backdrop", canvasRect, new Color(.005f, .009f, .018f, .56f));
            modalBackdrop.anchorMin = Vector2.zero;
            modalBackdrop.anchorMax = Vector2.one;
            modalBackdrop.offsetMin = modalBackdrop.offsetMax = Vector2.zero;
            modalBackdrop.GetComponent<Image>().raycastTarget = true;
            modalBackdrop.gameObject.SetActive(false);
            BuildNavigation(canvasRect);
            BuildInventoryWindow(canvasRect);
            BuildCharacterWindow(canvasRect);
            BuildTalentsWindow(canvasRect);
            BuildSpellbookWindow(canvasRect);
            BuildLootWindow(canvasRect);
            GameObject tooltipObject = new("Item Tooltip", typeof(RectTransform), typeof(PhasebreakItemTooltipUI));
            tooltipObject.transform.SetParent(canvasRect, false);
            itemTooltip = tooltipObject.GetComponent<PhasebreakItemTooltipUI>();
            itemTooltip.Initialize(canvasRect);
        }

        private void BuildInventoryWindow(RectTransform root)
        {
            inventoryPanel = WindowPanel("Inventory Panel", root, Vector2.zero, Vector2.zero);
            inventoryPanel.anchorMin = inventoryPanel.anchorMax = inventoryPanel.pivot = new Vector2(1f, 0f);
            inventoryPanel.anchoredPosition = new Vector2(-24f, 100f);
            inventoryPanel.sizeDelta = new Vector2(520f, 650f);
            BuildTitleBar(inventoryPanel, string.Empty, "INVENTORY", MenuMode.Inventory);
            RectTransform bag = Section("Bag Section", inventoryPanel, new Vector2(.035f, .055f), new Vector2(.965f, .88f));
            Text("Bag Header", bag, "FIELD INVENTORY", 12f, TextAlignmentOptions.Left, new Vector2(.03f, .91f), new Vector2(.47f, .985f), TextMuted);
            inventoryCount = Text("Capacity", bag, string.Empty, 12f, TextAlignmentOptions.Right, new Vector2(.46f, .91f), new Vector2(.97f, .985f), TextMuted);
            BuildInventoryFilters(bag);
            inventoryGrid = CreateScrollGrid(bag, new Vector2(.025f, .12f), new Vector2(.975f, .82f), 7, new Vector2(54f, 54f), new Vector2(6f, 6f));
            inventoryStatus = Text("Inventory Status", bag, "HOVER FOR DETAILS  •  RIGHT-CLICK TO EQUIP", 11f,
                TextAlignmentOptions.Center, new Vector2(.03f, .02f), new Vector2(.97f, .1f), TextMuted);
        }

        private void BuildInventoryFilters(RectTransform parent)
        {
            InventoryFilter[] filters = { InventoryFilter.All, InventoryFilter.Weapons, InventoryFilter.Armor, InventoryFilter.Relics, InventoryFilter.Sigils };
            for (int i = 0; i < filters.Length; i++)
            {
                InventoryFilter filter = filters[i];
                float min = .025f + i * .193f;
                RectTransform rect = Block(filter.ToString(), parent, PanelLight);
                Place(rect, new Vector2(min, .83f), new Vector2(min + .18f, .9f), 0f);
                Image image = rect.GetComponent<Image>();
                filterImages[filter] = image;
                Button button = rect.gameObject.AddComponent<Button>();
                PhasebreakUiTheme.StyleButton(button);
                Text("Label", rect, filter.ToString().ToUpperInvariant(), 10.5f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, TextPrimary);
                button.onClick.AddListener(() => SetFilter(filter));
            }
        }

        private void BuildCharacterWindow(RectTransform root)
        {
            characterPanel = WindowPanel("Character Panel", root, Vector2.zero, Vector2.zero);
            characterPanel.anchorMin = characterPanel.anchorMax = characterPanel.pivot = new Vector2(1f, 0f);
            characterPanel.anchoredPosition = new Vector2(-560f, 100f);
            characterPanel.sizeDelta = new Vector2(850f, 740f);
            BuildTitleBar(characterPanel, "THE PHASEBOUND", "CHARACTER", MenuMode.Character);
            characterTitle = Text("Identity", characterPanel, string.Empty, 14f, TextAlignmentOptions.Left, new Vector2(.035f, .865f), new Vector2(.85f, .892f), TextMuted);
            equipmentStage = Section("Equipment Stage", characterPanel, new Vector2(.025f, .055f), new Vector2(.64f, .87f));
            BuildPaperDoll(equipmentStage);
            Text("Equipment Header", equipmentStage, "EQUIPMENT", 15f, TextAlignmentOptions.Left, new Vector2(.035f, .935f), new Vector2(.5f, .985f), TextMuted);
            RectTransform analysis = Section("Build Analysis", characterPanel, new Vector2(.66f, .055f), new Vector2(.975f, .87f));
            Text("Analysis Header", analysis, "CHARACTER ANALYSIS", 15f, TextAlignmentOptions.Left, new Vector2(.06f, .93f), new Vector2(.8f, .985f), TextMuted);
            characterSummary = Text("Final Stats", analysis, string.Empty, 17f, TextAlignmentOptions.TopLeft, new Vector2(.06f, .54f), new Vector2(.94f, .925f), TextPrimary);
            RectTransform detailsScroll = Block("Build Details Scroll", analysis, PanelLight);
            Place(detailsScroll, new Vector2(.04f, .135f), new Vector2(.96f, .525f), 0f);
            ScrollRect scroll = detailsScroll.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 26f;
            RectTransform viewport = Block("Viewport", detailsScroll, Color.white);
            Stretch(viewport, 7f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, .01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            characterDetails = Text("Sets And Modifiers", content, string.Empty, 16f, TextAlignmentOptions.TopLeft, Vector2.zero, Vector2.one, TextPrimary);
            scroll.viewport = viewport;
            scroll.content = content;
            RectTransform passiveSurface = FramedIcon("Passive Icon Frame", analysis, new Vector2(.83f, .93f), new Vector2(.93f, .985f), new Color(.36f, .22f, .58f, 1f));
            passiveIcon = IconImage("Passive Icon", passiveSurface, new Vector2(.08f, .08f), new Vector2(.92f, .92f));
            passiveIcon.preserveAspect = true;
            AddSpecButtons(analysis);
        }

        private void BuildPaperDoll(RectTransform parent)
        {
            RectTransform glow = Block("Silhouette Glow", parent, new Color(.12f, .08f, .04f, .28f));
            glow.anchorMin = glow.anchorMax = new Vector2(.5f, .52f);
            glow.pivot = new Vector2(.5f, .5f);
            glow.sizeDelta = new Vector2(180f, 460f);
            Outline glowOutline = glow.gameObject.AddComponent<Outline>();
            glowOutline.effectColor = new Color(.53f, .35f, .18f, .35f);
            glowOutline.effectDistance = new Vector2(1f, -1f);
            Image silhouette = IconImage("Character Silhouette", glow, new Vector2(.08f, .04f), new Vector2(.92f, .96f));
            silhouette.sprite = PhasebreakItemIconLibrary.GetCharacterSilhouette();
            silhouette.color = Color.white;
            silhouette.preserveAspect = true;
            Text("Identity Mark", glow, "PHASEBOUND", 12f, TextAlignmentOptions.Bottom, new Vector2(0f, .01f), new Vector2(1f, .09f), new Color(.8f, .62f, .4f, .8f));
            RectTransform line = Block("Central Arcane Line", parent, new Color(.45f, .28f, .1f, .2f));
            line.anchorMin = line.anchorMax = new Vector2(.5f, .52f);
            line.pivot = new Vector2(.5f, .5f);
            line.sizeDelta = new Vector2(2f, 520f);
        }

        private void BuildTalentsWindow(RectTransform root)
        {
            talentsPanel = WindowPanel("Talents Panel", root, new Vector2(.055f, .055f), new Vector2(.945f, .945f));
            BuildTitleBar(talentsPanel, "VOID MATRIX", "TALENTS", MenuMode.Talents);
            talentWindow = talentsPanel.gameObject.AddComponent<TalentWindowView>();
            talentWindow.Initialize(talentsPanel);
        }

        private void BuildSpellbookWindow(RectTransform root)
        {
            spellbookPanel = WindowPanel("Spellbook Panel", root, new Vector2(.15f, .12f), new Vector2(.85f, .88f));
            BuildTitleBar(spellbookPanel, "FIELD ARCANUM", "SPELLBOOK", MenuMode.Spellbook);
            RectTransform list = Section("Abilities", spellbookPanel, new Vector2(.035f, .11f), new Vector2(.48f, .87f));
            Text("List Heading", list, "ABILITIES  /  SCROLL TO BROWSE", 14f, TextAlignmentOptions.Left,
                new Vector2(.04f, .91f), new Vector2(.96f, .98f), TextMuted);
            RectTransform scrollRoot = Block("Ability Scroll", list, PanelLight);
            Place(scrollRoot, new Vector2(.035f, .035f), new Vector2(.965f, .9f), 0f);
            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 28f;
            RectTransform viewport = Block("Viewport", scrollRoot, Color.white);
            Stretch(viewport, 5f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, .01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            RectTransform content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            int count = combat != null ? combat.CatalogCount : 0;
            for (int index = 0; index < count; index++)
            {
                int captured = index;
                RectTransform row = Block("Ability " + index, content, PanelLight);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
                Button button = row.gameObject.AddComponent<Button>();
                PhasebreakUiTheme.StyleButton(button);
                button.onClick.AddListener(() => { selectedAbility = captured; RefreshSpellbook(); });
                Image icon = IconImage("Icon", row, new Vector2(.025f, .12f), new Vector2(.16f, .88f));
                icon.sprite = combat.GetAbilityState(index).Icon;
                icon.preserveAspect = true;
                spellbookRows.Add(Text("Name", row, string.Empty, 16f, TextAlignmentOptions.Left,
                    new Vector2(.19f, .05f), new Vector2(.97f, .95f), TextPrimary));
            }
            RectTransform details = Section("Ability Details", spellbookPanel, new Vector2(.5f, .11f), new Vector2(.965f, .87f));
            spellbookDetails = Text("Details", details, string.Empty, 18f, TextAlignmentOptions.TopLeft,
                new Vector2(.055f, .38f), new Vector2(.945f, .95f), TextPrimary);
            Text("Assign Heading", details, "ASSIGN SELECTED ABILITY TO SLOT", 12f, TextAlignmentOptions.Left,
                new Vector2(.055f, .31f), new Vector2(.945f, .37f), TextMuted);
            for (int slot = 0; slot < (combat != null ? combat.AbilityCount : 0); slot++)
            {
                int captured = slot;
                float x = .055f + slot * .18f;
                Button button = TextButton("Slot " + (slot + 1), details, string.Empty,
                    new Vector2(x, .14f), new Vector2(x + .16f, .28f),
                    () => { CombatAbilityDefinition ability = combat.GetAbilityDefinition(selectedAbility);
                        if (ability != null) combat.AssignAbilityToSlot(captured, ability.id);
                        RefreshSpellbook(); }, 15f);
                spellbookSlots.Add(button.GetComponentInChildren<TextMeshProUGUI>());
            }
            Text("Help", details, "Select an unlocked ability, then choose an action bar slot.", 12f,
                TextAlignmentOptions.Left, new Vector2(.055f, .045f), new Vector2(.945f, .12f), TextMuted);
        }

        private void RefreshSpellbook()
        {
            if (combat == null || spellbookDetails == null || spellbookRows.Count == 0) return;
            selectedAbility = Mathf.Clamp(selectedAbility, 0, spellbookRows.Count - 1);
            for (int index = 0; index < spellbookRows.Count; index++)
                spellbookRows[index].text = (index == selectedAbility ? ">  " : "    ") +
                    (combat.IsAbilityUnlocked(index) ? "" : "[LOCKED] ") + combat.GetAbilityState(index).Name;
            AbilityState state = combat.GetAbilityState(selectedAbility);
            CombatAbilityDefinition definition = combat.GetAbilityDefinition(selectedAbility);
            string description = definition != null ? definition.description : string.Empty;
            spellbookDetails.text = $"<b>{state.Name}</b>\n{(combat.IsAbilityUnlocked(selectedAbility) ? "" : "<color=#C9A86C>Unlock in its specialization tree</color>\n")}\n{description}\n\n" +
                $"<color=#9CAABD>Energy {Mathf.CeilToInt(state.ResourceCost)}   •   Cooldown {state.CooldownDuration:0.#}s\n" +
                $"Range {(definition != null ? definition.range : 0f):0.#}   •   Charges {state.MaximumCharges}</color>";
            for (int slot = 0; slot < spellbookSlots.Count; slot++)
                spellbookSlots[slot].text = PhasebreakSettings.Display($"ability.{slot + 1}") +
                    (combat.GetAssignedAbilityIndex(slot) == selectedAbility ? " *" : string.Empty);
        }

        private void BuildLootWindow(RectTransform root)
        {
            lootPanel = WindowPanel("Loot Panel", root, new Vector2(.33f, .25f), new Vector2(.67f, .78f));
            lootTitle = Text("Loot Title", lootPanel, "CORPSE CACHE", 22f, TextAlignmentOptions.Center, new Vector2(.08f, .86f), new Vector2(.92f, .96f), TextPrimary);
            RectTransform accent = Block("Loot Accent", lootPanel, new Color(.78f, .25f, .16f, 1f));
            Place(accent, new Vector2(.08f, .845f), new Vector2(.92f, .85f), 0f);
            lootGrid = CreateScrollGrid(lootPanel, new Vector2(.06f, .22f), new Vector2(.94f, .82f), 4, new Vector2(76f, 76f), new Vector2(10f, 10f));
            TextButton("Take All", lootPanel, "TAKE ALL", new Vector2(.29f, .065f), new Vector2(.71f, .165f), TakeAll);
        }

        private void BuildNavigation(RectTransform root)
        {
            RectTransform nav = Block("Gameplay Navigation", root, Window);
            nav.anchorMin = nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(1f, 0f);
            nav.anchoredPosition = new Vector2(-22f, 20f);
            nav.sizeDelta = new Vector2(406f, 70f);
            PhasebreakUiTheme.StyleSurface(nav.GetComponent<Image>(), Window);
            NavButton(nav, MenuMode.Inventory, EquipmentSlot.Core, 0, "Inventory", "inventory");
            NavButton(nav, MenuMode.Character, EquipmentSlot.Chest, 1, "Character", "character");
            NavButton(nav, MenuMode.Talents, EquipmentSlot.WildcardArtifact, 2, "Talents", "talents");
            NavButton(nav, MenuMode.Spellbook, EquipmentSlot.Sigil1, 3, "Spellbook", "spellbook");
            journalNavImage = NavWorldButton(nav, 4, "Journal", "journal", "JOURNAL", "J",
                WorldQuestHud.ToggleJournalMenu);
            mapNavImage = NavWorldButton(nav, 5, "Map", "worldmap", "MAP", "M",
                WorldQuestHud.ToggleMapMenu);
            navTooltip = Text("Navigation Tooltip", root, string.Empty, 13f,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.zero, TextPrimary);
            navTooltip.rectTransform.anchorMin = navTooltip.rectTransform.anchorMax = new Vector2(1f, 0f);
            navTooltip.rectTransform.pivot = new Vector2(1f, 0f);
            navTooltip.rectTransform.anchoredPosition = new Vector2(-22f, 0f);
            navTooltip.rectTransform.sizeDelta = new Vector2(406f, 18f);
            navTooltip.gameObject.SetActive(false);
        }

        private void NavButton(RectTransform root, MenuMode target, EquipmentSlot iconSlot, int index, string hint, string bindingId)
        {
            RectTransform rect = Block(target.ToString(), root, PanelLight);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(9f + index * 66f, 8f);
            rect.sizeDelta = new Vector2(58f, 54f);
            Image background = rect.GetComponent<Image>();
            navImages[target] = background;
            Image icon = IconImage("Icon", rect, new Vector2(.23f, .28f), new Vector2(.77f, .86f));
            icon.sprite = PhasebreakItemIconLibrary.Get(iconSlot);
            icon.color = TextPrimary;
            Text("Label", rect, hint.ToUpperInvariant(), 9f, TextAlignmentOptions.Center,
                new Vector2(.02f, .025f), new Vector2(.98f, .26f), TextMuted);
            ItemSlotUI relay = rect.gameObject.AddComponent<ItemSlotUI>();
            relay.Configure(() => { navTooltip.text = $"{hint}  [{PhasebreakSettings.Display(bindingId)}]"; navTooltip.gameObject.SetActive(true); }, () => navTooltip.gameObject.SetActive(false), () => Toggle(target));
            PhasebreakUiTheme.StyleSurface(background, PanelLight);
            relay.ConfigureVisual(background, PanelLight, PhasebreakUiTheme.Hover, PhasebreakUiTheme.Active);
        }

        private Image NavWorldButton(RectTransform root, int index, string hint, string bindingId,
            string label, string symbol, Action onClick)
        {
            RectTransform rect = Block(hint, root, PanelLight);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(9f + index * 66f, 8f);
            rect.sizeDelta = new Vector2(58f, 54f);
            Image background = rect.GetComponent<Image>();
            Text("Symbol", rect, symbol, 29f, TextAlignmentOptions.Center,
                new Vector2(.12f, .28f), new Vector2(.88f, .88f), TextPrimary);
            Text("Label", rect, label, 9f, TextAlignmentOptions.Center,
                new Vector2(.02f, .025f), new Vector2(.98f, .26f), TextMuted);
            ItemSlotUI relay = rect.gameObject.AddComponent<ItemSlotUI>();
            relay.Configure(() => { navTooltip.text = $"{hint}  [{PhasebreakSettings.Display(bindingId)}]";
                navTooltip.gameObject.SetActive(true); },
                () => navTooltip.gameObject.SetActive(false), onClick);
            PhasebreakUiTheme.StyleSurface(background, PanelLight);
            relay.ConfigureVisual(background, PanelLight, PhasebreakUiTheme.Hover, PhasebreakUiTheme.Active);
            return background;
        }

        private void BuildTitleBar(RectTransform window, string subtitle, string title, MenuMode target)
        {
            Text("Window Title", window, title, 25f, TextAlignmentOptions.Left, new Vector2(.035f, .91f), new Vector2(.4f, .985f), TextPrimary);
            Text("Window Subtitle", window, subtitle, 11f, TextAlignmentOptions.Left, new Vector2(.24f, .918f), new Vector2(.7f, .973f), TextMuted);
            RectTransform accent = Block("Title Accent", window, Cyan);
            Place(accent, new Vector2(.025f, .895f), new Vector2(.975f, .9f), 0f);
            TextButton("Close", window, "×", new Vector2(.94f, .925f), new Vector2(.978f, .977f), () => { if (mode == target) CloseMenu(); }, 22f);
        }

        private void RefreshOpenPanel()
        {
            if (mode == MenuMode.Inventory) RefreshInventory();
            else if (mode == MenuMode.Character) RefreshCharacter();
            else if (mode == MenuMode.Talents) talentWindow?.Refresh();
            else if (mode == MenuMode.Spellbook) RefreshSpellbook();
            else if (mode == MenuMode.Loot) RefreshLoot();
        }

        private void RefreshInventory()
        {
            if (build == null) return;
            if (selectedItem != null && (selectedInventoryIndex < 0 || selectedInventoryIndex >= build.Inventory.Count || build.Inventory[selectedInventoryIndex] != selectedItem))
            {
                selectedItem = null;
                selectedInventoryIndex = -1;
            }
            Clear(inventoryCells);
            int visibleCount = 0;
            int upgradeCount = 0;
            for (int i = 0; i < build.Inventory.Count; i++)
            {
                PhasebreakItemDefinition item = build.Inventory[i];
                if (!MatchesFilter(item, inventoryFilter)) continue;
                GearComparisonResult comparison = GearUpgradeEvaluator.Evaluate(item, build);
                if (comparison.IsUpgrade) upgradeCount++;
                CreateInventoryCell(inventoryGrid, item, i, comparison);
                visibleCount++;
            }
            int capacity = Mathf.Max(42, Mathf.CeilToInt(Mathf.Max(1, visibleCount) / 7f) * 7);
            for (int i = visibleCount; i < capacity; i++) CreateInventoryCell(inventoryGrid, null, i, default);
            inventoryCount.text = $"{visibleCount} / {build.Inventory.Count}  •  {upgradeCount} UPGRADES";
            UpdateFilterVisuals();
            LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryGrid);
        }

        private void CreateInventoryCell(RectTransform root, PhasebreakItemDefinition item, int index, GearComparisonResult comparison)
        {
            GameObject cell = CreateGridItem(root, item, null, inventoryCells, false, comparison);
            if (item == null) return;
            ItemSlotUI relay = cell.GetComponent<ItemSlotUI>();
            Image visual = cell.transform.Find("Slot Surface").GetComponent<Image>();
            relay.Configure(
                () => itemTooltip.Show(item, comparison.EquippedItem, cell.GetComponent<RectTransform>(), comparison),
                itemTooltip.Hide,
                () => SelectInventoryItem(item, index),
                () => TryEquip(item),
                () => TryEquip(item));
            relay.ConfigureVisual(visual, PanelLight, PhasebreakUiTheme.Hover, PhasebreakUiTheme.Active);
            relay.SetSelected(item == selectedItem && index == selectedInventoryIndex);
        }

        private void SelectInventoryItem(PhasebreakItemDefinition item, int index) { selectedItem = item; selectedInventoryIndex = index; inventoryStatus.text = item.displayName; RefreshInventory(); }
        private void TryEquip(PhasebreakItemDefinition item)
        {
            itemTooltip.Hide();
            if (build != null && build.Equip(item)) { selectedItem = null; selectedInventoryIndex = -1; inventoryStatus.text = $"Equipped {item.displayName}"; }
            else inventoryStatus.text = "That item cannot be equipped right now";
        }

        private void RefreshCharacter()
        {
            if (build == null) return;
            Clear(equipmentCells);
            characterTitle.text = $"LEVEL {(progression != null ? progression.Level : 1)}  •  {build.Specialization.ToString().ToUpperInvariant()}  •  16 EQUIPMENT CHANNELS";
            CreateEquipmentSlot(EquipmentSlot.Head, new Vector2(.03f, .8f), false);
            CreateEquipmentSlot(EquipmentSlot.Shoulders, new Vector2(.03f, .675f), false);
            CreateEquipmentSlot(EquipmentSlot.Chest, new Vector2(.03f, .55f), false);
            CreateEquipmentSlot(EquipmentSlot.Hands, new Vector2(.03f, .425f), false);
            CreateEquipmentSlot(EquipmentSlot.Legs, new Vector2(.03f, .3f), false);
            CreateEquipmentSlot(EquipmentSlot.Boots, new Vector2(.03f, .175f), false);
            CreateEquipmentSlot(EquipmentSlot.PrimaryWeapon, new Vector2(.65f, .8f), true);
            CreateEquipmentSlot(EquipmentSlot.Secondary, new Vector2(.65f, .675f), true);
            CreateEquipmentSlot(EquipmentSlot.Core, new Vector2(.65f, .55f), true);
            CreateEquipmentSlot(EquipmentSlot.MobilityRelic, new Vector2(.65f, .425f), true);
            CreateEquipmentSlot(EquipmentSlot.PowerRelic, new Vector2(.65f, .3f), true);
            CreateEquipmentSlot(EquipmentSlot.UtilityRelic, new Vector2(.65f, .175f), true);
            CreateEquipmentSlot(EquipmentSlot.Sigil1, new Vector2(.29f, .025f), true, true);
            CreateEquipmentSlot(EquipmentSlot.Sigil2, new Vector2(.405f, .025f), true, true);
            CreateEquipmentSlot(EquipmentSlot.Sigil3, new Vector2(.52f, .025f), true, true);
            CreateEquipmentSlot(EquipmentSlot.WildcardArtifact, new Vector2(.635f, .025f), true, true);
            passiveIcon.sprite = progression != null ? progression.PassiveIcon : PhasebreakIconCatalog.Current?.fallbackBuff;
            passiveIcon.color = passiveIcon.sprite == null ? Color.clear : progression != null && progression.HasKeenEdge ? Color.white : new Color(.35f, .4f, .48f, .65f);
            characterSummary.text = BuildCharacterSummary();
            characterDetails.text = BuildCharacterDetails();
        }

        private void CreateEquipmentSlot(EquipmentSlot slot, Vector2 anchor, bool labelOnLeft, bool compact = false)
        {
            PhasebreakItemDefinition item = build.GetEquipped(slot);
            Vector2 size = compact ? new Vector2(62f, 62f) : new Vector2(68f, 68f);
            RectTransform holder = new GameObject(slot.ToString(), typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(equipmentStage, false);
            holder.anchorMin = holder.anchorMax = anchor;
            holder.pivot = Vector2.zero;
            holder.sizeDelta = compact ? new Vector2(70f, 78f) : new Vector2(160f, 72f);
            equipmentCells.Add(holder.gameObject);
            RectTransform slotRect = Block("Equipment Slot", holder, RarityColor(item != null ? item.rarity : ItemRarity.Common));
            slotRect.anchorMin = slotRect.anchorMax = labelOnLeft && !compact ? new Vector2(1f, .5f) : new Vector2(0f, .5f);
            slotRect.pivot = labelOnLeft && !compact ? new Vector2(1f, .5f) : new Vector2(0f, .5f);
            slotRect.anchoredPosition = Vector2.zero;
            slotRect.sizeDelta = size;
            RectTransform surface = Block("Slot Surface", slotRect, PanelLight);
            Stretch(surface, 3f);
            Image icon = IconImage("Slot Icon", surface, new Vector2(.17f, .17f), new Vector2(.83f, .83f));
            icon.sprite = item != null ? PhasebreakItemIconLibrary.Resolve(item) : PhasebreakItemIconLibrary.Get(slot);
            icon.color = item != null ? (item.icon != null ? Color.white : RarityTint(item.rarity)) : new Color(.32f, .42f, .52f, .55f);
            icon.preserveAspect = true;
            if (!compact)
            {
                Vector2 labelMin = labelOnLeft ? new Vector2(0f, .08f) : new Vector2(.47f, .08f);
                Vector2 labelMax = labelOnLeft ? new Vector2(.53f, .92f) : new Vector2(1f, .92f);
                Text("Slot Label", holder, Pretty(slot).ToUpperInvariant(), 12f, labelOnLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, labelMin, labelMax, item != null ? TextPrimary : TextMuted);
            }
            else Text("Slot Label", holder, CompactSlotLabel(slot), 12f, TextAlignmentOptions.Center, new Vector2(-.1f, -.2f), new Vector2(1.1f, .02f), item != null ? TextPrimary : TextMuted);
            ItemSlotUI relay = slotRect.gameObject.AddComponent<ItemSlotUI>();
            relay.Configure(
                () => { if (item != null) itemTooltip.Show(item, null, slotRect); },
                itemTooltip.Hide,
                () => SelectEquipmentSlot(slot),
                item == null ? null : () => TryUnequip(slot),
                item == null ? null : () => TryUnequip(slot));
            relay.ConfigureVisual(surface.GetComponent<Image>(), PanelLight, PhasebreakUiTheme.Hover, PhasebreakUiTheme.Active);
            relay.SetSelected(selectedEquipmentSlot == slot);
        }

        private void SelectEquipmentSlot(EquipmentSlot slot) { selectedEquipmentSlot = slot; itemTooltip.Hide(); RefreshCharacter(); }
        private void TryUnequip(EquipmentSlot slot) { itemTooltip.Hide(); if (build.Unequip(slot)) selectedEquipmentSlot = null; }

        private string BuildCharacterSummary()
        {
            float levelPower = progression != null ? progression.PowerMultiplier : 1f;
            int levelHealth = progression != null ? progression.BonusHealth : 0;
            float passiveCrit = progression != null ? progression.CriticalChanceBonus : 0f;
            StringBuilder text = new();
            text.Append("<color=#CFA66A><b>FINAL STATS</b></color>\n");
            StatRow(text, "Power", $"x{levelPower * build.PowerMultiplier:0.00}");
            StatRow(text, "Bonus health", $"+{levelHealth + build.BonusHealth}");
            StatRow(text, "Defense", build.Defense.ToString("P0"));
            StatRow(text, "Critical chance", $"+{passiveCrit + build.CriticalChanceBonus:P0}");
            StatRow(text, "Critical damage", $"+{build.CriticalDamageBonus:P0}");
            StatRow(text, "Attack speed", $"+{build.AttackSpeedMultiplier - 1f:P0}");
            StatRow(text, "Mobility", $"+{build.MovementSpeedMultiplier - 1f:P0}");
            StatRow(text, "Boss damage", $"+{build.BossDamageMultiplier - 1f:P0}");
            return text.ToString();
        }

        private string BuildCharacterDetails()
        {
            string passive = progression != null && progression.HasKeenEdge ? progression.PassiveName : "Locked";
            StringBuilder text = new();
            if (selectedEquipmentSlot is EquipmentSlot slot)
            {
                PhasebreakItemDefinition selected = build.GetEquipped(slot);
                text.Append($"<color=#CFA66A><b>{CompactSlotLabel(slot)}</b></color>\n" +
                    (selected != null ? selected.displayName : "Empty — equip from Inventory") + "\n\n");
            }
            text.Append($"<color=#CFA66A><b>SPECIALIZATION</b></color>\n{(build.Specialization == Specialization.Unchosen ? "Choose in Talents" : build.Specialization)}\n");
            text.Append($"\n<color=#CFA66A><b>PASSIVE</b></color>\n{passive}\n");
            text.Append("\n<color=#CFA66A><b>ACTIVE SETS</b></color>\n");
            string sets = BuildSetSummary();
            text.Append(string.IsNullOrEmpty(sets) ? "<color=#738196>None active</color>\n" : sets);
            text.Append("\n<color=#CFA66A><b>BUILD MODIFIERS</b></color>\n");
            text.Append(ModifierSummary(true));
            return text.ToString();
        }

        private string BuildSetSummary()
        {
            StringBuilder result = new();
            IEnumerable<IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition>> groups = Slots.Select(build.GetEquipped).Where(item => item != null && item.itemSet != null).GroupBy(item => item.itemSet);
            foreach (IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition> group in groups)
            {
                int count = group.Count();
                int maximum = (group.Key.bonuses ?? Array.Empty<SetBonusDefinition>()).Select(bonus => bonus.pieces).DefaultIfEmpty(0).Max();
                int displayedCount = maximum > 0 ? Mathf.Min(count, maximum) : count;
                result.Append($"<b>{group.Key.displayName}</b>  <color=#CFA66A>{displayedCount}/{maximum}</color>\n");
                foreach (SetBonusDefinition bonus in group.Key.bonuses ?? Array.Empty<SetBonusDefinition>())
                    result.Append($"<color={(count >= bonus.pieces ? "#5EE58C" : "#667487")}>{bonus.pieces}-piece  {bonus.description}</color>\n");
            }
            return result.ToString();
        }

        private void RefreshLoot()
        {
            Clear(lootCells);
            if (activeCorpse == null) { CloseMenu(); return; }
            lootTitle.text = activeCorpse.DisplayName.ToUpperInvariant() + (activeCorpse.IsEmpty ? "  •  EMPTY" : "  •  LOOT CACHE");
            int capacity = Mathf.Max(8, activeCorpse.Items.Count);
            for (int i = 0; i < capacity; i++)
            {
                PhasebreakItemDefinition item = i < activeCorpse.Items.Count ? activeCorpse.Items[i] : null;
                GearComparisonResult comparison = GearUpgradeEvaluator.Evaluate(item, build);
                GameObject cell = CreateGridItem(lootGrid, item, null, lootCells, false, comparison);
                if (item == null) continue;
                ItemSlotUI relay = cell.GetComponent<ItemSlotUI>();
                relay.Configure(() => itemTooltip.Show(item, comparison.EquippedItem, cell.GetComponent<RectTransform>(), comparison), itemTooltip.Hide,
                    () => { activeCorpse.Take(item, build); itemTooltip.Hide(); RefreshLoot(); }, null, null);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(lootGrid);
        }

        private GameObject CreateGridItem(RectTransform root, PhasebreakItemDefinition item, string slotLabel, List<GameObject> list, bool equipped, GearComparisonResult comparison = default)
        {
            RectTransform outer = Block(item == null ? "Empty Slot" : item.displayName, root, item == null ? Panel : RarityColor(item.rarity));
            list.Add(outer.gameObject);
            RectTransform surface = Block("Slot Surface", outer, PanelLight);
            Stretch(surface, 3f);
            Image icon = IconImage("Item Icon", surface, new Vector2(.14f, .14f), new Vector2(.86f, .86f));
            if (item != null)
            {
                icon.sprite = PhasebreakItemIconLibrary.Resolve(item);
                icon.color = item.icon != null ? Color.white : RarityTint(item.rarity);
                icon.preserveAspect = true;
                Text("Item Level", surface, item.itemLevel.ToString(), 10f, TextAlignmentOptions.BottomRight, new Vector2(.55f, .03f), new Vector2(.95f, .28f), TextPrimary);
                ItemSlotUI relay = outer.gameObject.AddComponent<ItemSlotUI>();
                relay.ConfigureVisual(surface.GetComponent<Image>(), PanelLight, PhasebreakUiTheme.Hover, PhasebreakUiTheme.Active);
                if (comparison.IsUpgrade) AddUpgradeIndicator(outer);
            }
            else
            {
                icon.sprite = PhasebreakItemIconLibrary.Get(EquipmentSlot.Core);
                icon.color = new Color(.42f, .41f, .38f, .16f);
                icon.preserveAspect = true;
            }
            if (equipped) Text("Equipped", surface, "EQUIPPED", 9f, TextAlignmentOptions.Top, new Vector2(.05f, .76f), new Vector2(.95f, .97f), new Color(.4f, .92f, .64f));
            if (!string.IsNullOrEmpty(slotLabel)) Text("Slot", surface, slotLabel, 9f, TextAlignmentOptions.Bottom, new Vector2(.02f, .01f), new Vector2(.98f, .22f), TextMuted);
            return outer.gameObject;
        }

        private static void AddUpgradeIndicator(RectTransform slot)
        {
            Outline glow = slot.gameObject.AddComponent<Outline>();
            glow.effectColor = PhasebreakUiTheme.Accent;
            glow.effectDistance = new Vector2(1f, -1f);
            glow.useGraphicAlpha = false;
        }

        private void TakeAll() { if (activeCorpse == null) return; activeCorpse.TakeAll(build); itemTooltip.Hide(); RefreshLoot(); }
        private void SetFilter(InventoryFilter filter) { inventoryFilter = filter; selectedItem = null; selectedInventoryIndex = -1; inventoryStatus.text = filter == InventoryFilter.All ? "Showing all item categories" : $"Filtered to {filter}"; RefreshInventory(); }

        private static bool MatchesFilter(PhasebreakItemDefinition item, InventoryFilter filter)
        {
            if (item == null || filter == InventoryFilter.All) return item != null;
            return filter switch
            {
                InventoryFilter.Weapons => item.slot is EquipmentSlot.PrimaryWeapon or EquipmentSlot.Secondary,
                InventoryFilter.Armor => item.slot is EquipmentSlot.Head or EquipmentSlot.Shoulders or EquipmentSlot.Chest or EquipmentSlot.Hands or EquipmentSlot.Legs or EquipmentSlot.Boots,
                InventoryFilter.Relics => item.slot is EquipmentSlot.Core or EquipmentSlot.MobilityRelic or EquipmentSlot.PowerRelic or EquipmentSlot.UtilityRelic or EquipmentSlot.WildcardArtifact,
                InventoryFilter.Sigils => item.slot is EquipmentSlot.Sigil1 or EquipmentSlot.Sigil2 or EquipmentSlot.Sigil3,
                _ => true
            };
        }

        private void UpdateFilterVisuals() { foreach (KeyValuePair<InventoryFilter, Image> pair in filterImages) pair.Value.color = pair.Key == inventoryFilter ? PhasebreakUiTheme.Active : PanelLight; }

        private void UpdateCorpseInteraction()
        {
            if (mode != MenuMode.None) { SetHoveredCorpse(null); return; }
            if (worldCamera == null || Mouse.current == null) return;
            Vector2 point = cameraController != null && cameraController.RightClickStartedThisFrame ? cameraController.RightClickPosition : Mouse.current.position.ReadValue();
            Ray ray = worldCamera.ScreenPointToRay(point);
            CorpseLootContainer found = Physics.Raycast(ray, out RaycastHit hit, 55f, ~0, QueryTriggerInteraction.Collide) ? hit.collider.GetComponentInParent<CorpseLootContainer>() : null;
            SetHoveredCorpse(found);
            if (found != null && cameraController != null && cameraController.RightClickStartedThisFrame) OpenLoot(found);
        }

        private void SetHoveredCorpse(CorpseLootContainer corpse) { if (hoveredCorpse == corpse) return; hoveredCorpse?.SetHovered(false); hoveredCorpse = corpse; hoveredCorpse?.SetHovered(true); }

        private string ModifierSummary(bool rich = false)
        {
            string active = rich ? "<color=#5EE58C>ACTIVE</color>" : "Active";
            string inactive = rich ? "<color=#68778A>Inactive</color>" : "Inactive";
            return $"Crit restores Energy  <b>{build.CritEnergyRestore:0}</b>\n" +
                   $"Phase Lunge charges  <b>{1 + build.PhaseLungeExtraCharges}</b>\n" +
                   $"Phase Lunge cooldown  <b>{build.PhaseLungeCooldownMultiplier:P0}</b>\n" +
                   $"Crushing Blow cleave  {(build.CrushingBlowCleave ? active : inactive)}\n" +
                   $"Teleport recovery  <b>{build.TeleportKillRecovery:0}</b>";
        }

        private void AddSpecButtons(RectTransform root)
        {
            float x = .06f;
            foreach (Specialization spec in new[] { Specialization.Berserker, Specialization.Bulwark, Specialization.Riftblade })
            {
                Specialization captured = spec;
                Button button = TextButton(spec.ToString(), root, spec.ToString().ToUpperInvariant(), new Vector2(x, .035f), new Vector2(x + .28f, .105f), () => { if (build.SetSpecialization(captured) == SpecializationChangeResult.Changed) RefreshCharacter(); }, 10.5f);
                Image specIcon = IconImage("Specialization Icon", button.transform, new Vector2(.035f, .13f), new Vector2(.23f, .87f));
                specIcon.sprite = PhasebreakIconCatalog.Current?.GetSpecializationIcon(spec);
                specIcon.color = specIcon.sprite != null ? Color.white : Color.clear;
                specIcon.preserveAspect = true;
                TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.rectTransform.anchorMin = new Vector2(.24f, 0f);
                button.interactable = build.Specialization == Specialization.Unchosen;
                x += .3f;
            }
        }

        private void UpdateNav()
        {
            foreach (KeyValuePair<MenuMode, Image> pair in navImages)
                pair.Value.GetComponent<ItemSlotUI>().SetSelected(mode == pair.Key);
            if (journalNavImage != null)
                journalNavImage.GetComponent<ItemSlotUI>().SetSelected(WorldQuestHud.IsJournalOpen);
            if (mapNavImage != null)
                mapNavImage.GetComponent<ItemSlotUI>().SetSelected(WorldQuestHud.IsMapOpen);
        }
        private void SetActions(bool enabled) { foreach (InputAction action in new[] { inventoryAction, characterAction, talentsAction, spellbookAction, escapeAction }) if (enabled) action.Enable(); else action.Disable(); }

        private static RectTransform CreateScrollGrid(Transform parent, Vector2 min, Vector2 max, int columns, Vector2 cellSize, Vector2 spacing)
        {
            RectTransform root = Block("Scroll View", parent, new Color(.012f, .023f, .037f, .9f));
            Place(root, min, max, 0f);
            ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 26f;
            RectTransform viewport = Block("Viewport", root, Color.white);
            Stretch(viewport, 5f);
            Image viewportImage = viewport.GetComponent<Image>(); viewportImage.color = new Color(1f, 1f, 1f, .01f);
            Mask mask = viewport.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            RectTransform content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(.5f, 1f); content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = cellSize; grid.spacing = spacing; grid.padding = new RectOffset(11, 11, 11, 11); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns; grid.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            return content;
        }

        private static RectTransform WindowPanel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform panel = Block(name, parent, Window); Place(panel, min, max, 0f);
            PhasebreakUiTheme.StyleSurface(panel.GetComponent<Image>(), Window);
            return panel;
        }

        private static RectTransform Section(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform section = Block(name, parent, Panel); Place(section, min, max, 0f);
            PhasebreakUiTheme.StyleSurface(section.GetComponent<Image>(), Panel);
            return section;
        }

        private static RectTransform FramedIcon(string name, Transform parent, Vector2 min, Vector2 max, Color border)
        {
            RectTransform outer = Block(name, parent, border); Place(outer, min, max, 0f);
            PhasebreakUiTheme.StyleSurface(outer.GetComponent<Image>(), border);
            RectTransform inner = Block("Surface", outer, PanelLight); Stretch(inner, 3f);
            PhasebreakUiTheme.StyleSurface(inner.GetComponent<Image>(), PanelLight, false);
            return inner;
        }

        private static Button TextButton(string name, Transform parent, string value, Vector2 min, Vector2 max, Action click, float fontSize = 12f)
        {
            RectTransform rect = Block(name, parent, PanelLight); Place(rect, min, max, 0f);
            Button button = rect.gameObject.AddComponent<Button>();
            PhasebreakUiTheme.StyleButton(button);
            Text("Label", rect, value, fontSize, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, TextPrimary);
            if (click != null) button.onClick.AddListener(() => click());
            return button;
        }

        private static RectTransform Block(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>();
        }

        private static Image IconImage(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = Block(name, parent, Color.white); Place(rect, min, max, 0f); Image image = rect.GetComponent<Image>(); image.raycastTarget = false; return image;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.alignment = alignment; text.color = color; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false; Place(text.rectTransform, min, max, 0f); return text;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, float inset) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset); }
        private static void Stretch(RectTransform rect, float inset) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset); }
        private static void Clear(List<GameObject> list) { foreach (GameObject go in list) if (go != null) Destroy(go); list.Clear(); }
        private static void StatRow(StringBuilder text, string label, string value) => text.Append($"<color=#8998AA>{label}</color><pos=72%><b>{value}</b>\n");
        private static string Pretty(EquipmentSlot slot) => System.Text.RegularExpressions.Regex.Replace(slot.ToString(), "([a-z])([A-Z0-9])", "$1 $2");
        private static string CompactSlotLabel(EquipmentSlot slot) => slot switch { EquipmentSlot.Sigil1 => "SIGIL I", EquipmentSlot.Sigil2 => "SIGIL II", EquipmentSlot.Sigil3 => "SIGIL III", EquipmentSlot.WildcardArtifact => "ARTIFACT", _ => Pretty(slot).ToUpperInvariant() };
        private static InputAction KeyAction(string name, string binding) => new(name, InputActionType.Button, binding);
        private static void EnsureEventSystem() { if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); }
        private static Color RarityColor(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => new Color(.67f, .38f, .24f, 1f), ItemRarity.Epic => new Color(.43f, .34f, .57f, 1f), ItemRarity.Rare => new Color(.28f, .43f, .57f, 1f), ItemRarity.Uncommon => new Color(.31f, .46f, .35f, 1f), _ => PhasebreakUiTheme.MetalEdge };
        private static Color RarityTint(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => new Color(1f, .47f, .3f), ItemRarity.Epic => new Color(.72f, .55f, 1f), ItemRarity.Rare => new Color(.3f, .68f, 1f), ItemRarity.Uncommon => new Color(.4f, .88f, .55f), _ => new Color(.82f, .86f, .92f) };
    }
}
