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
        private enum MenuMode { None, Inventory, Character, Talents, Loot }
        private enum InventoryFilter { All, Weapons, Armor, Relics, Sigils }

        private static PhasebreakInventoryHud instance;
        private static readonly EquipmentSlot[] Slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

        private readonly List<GameObject> inventoryCells = new();
        private readonly List<GameObject> equipmentCells = new();
        private readonly List<GameObject> lootCells = new();
        private readonly Dictionary<MenuMode, Image> navImages = new();
        private readonly Dictionary<InventoryFilter, Image> filterImages = new();

        private PlayerBuildSystem build;
        private PlayerProgression progression;
        private PhasebreakFollowCamera cameraController;
        private Camera worldCamera;
        private InputAction inventoryAction;
        private InputAction characterAction;
        private InputAction talentsAction;
        private InputAction escapeAction;
        private RectTransform canvasRect;
        private RectTransform inventoryPanel;
        private RectTransform inventoryGrid;
        private RectTransform characterPanel;
        private RectTransform equipmentStage;
        private RectTransform talentsPanel;
        private RectTransform lootPanel;
        private RectTransform lootGrid;
        private TextMeshProUGUI inventoryCount;
        private TextMeshProUGUI selectedItemTitle;
        private TextMeshProUGUI selectedItemDetails;
        private TextMeshProUGUI inventoryStatus;
        private Image selectedItemIcon;
        private Button selectedEquipButton;
        private TextMeshProUGUI characterSummary;
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

        public static bool IsMajorMenuOpen => instance != null && instance.mode != MenuMode.None;
        public static bool IsLootWindowOpenFor(CorpseLootContainer corpse) => instance != null && instance.mode == MenuMode.Loot && instance.activeCorpse == corpse;
        public static void NotifyCorpseDespawned(CorpseLootContainer corpse) { if (IsLootWindowOpenFor(corpse)) instance.CloseMenu(); }

        private static readonly Color Window = new(.014f, .022f, .038f, .985f);
        private static readonly Color Panel = new(.026f, .042f, .068f, .98f);
        private static readonly Color PanelLight = new(.042f, .065f, .1f, .98f);
        private static readonly Color Cyan = new(.11f, .72f, .9f, 1f);
        private static readonly Color TextPrimary = new(.9f, .94f, 1f, 1f);
        private static readonly Color TextMuted = new(.51f, .6f, .7f, 1f);

        private void Awake()
        {
            instance = this;
            build = FindAnyObjectByType<PlayerBuildSystem>();
            progression = FindAnyObjectByType<PlayerProgression>();
            cameraController = FindAnyObjectByType<PhasebreakFollowCamera>();
            worldCamera = Camera.main;
            inventoryAction = KeyAction("Inventory", "<Keyboard>/b");
            characterAction = KeyAction("Character", "<Keyboard>/c");
            talentsAction = KeyAction("Talents", "<Keyboard>/t");
            escapeAction = KeyAction("Close Menu", "<Keyboard>/escape");
            EnsureEventSystem();
            BuildUi();
            CloseMenu();
        }

        private void OnEnable() { SetActions(true); if (build != null) build.BuildChanged += RefreshOpenPanel; }
        private void OnDisable() { SetActions(false); if (build != null) build.BuildChanged -= RefreshOpenPanel; CloseMenu(); }
        private void OnDestroy()
        {
            inventoryAction?.Dispose(); characterAction?.Dispose(); talentsAction?.Dispose(); escapeAction?.Dispose();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (inventoryAction.WasPressedThisFrame()) Toggle(MenuMode.Inventory);
            else if (characterAction.WasPressedThisFrame()) Toggle(MenuMode.Character);
            else if (talentsAction.WasPressedThisFrame()) Toggle(MenuMode.Talents);
            else if (escapeAction.WasPressedThisFrame() && mode != MenuMode.None) CloseMenu();
            UpdateCorpseInteraction();
        }

        public void OpenLoot(CorpseLootContainer corpse) { if (corpse == null) return; activeCorpse = corpse; Open(MenuMode.Loot); }
        private void Toggle(MenuMode target) { if (mode == target) CloseMenu(); else Open(target); }

        private void Open(MenuMode target)
        {
            mode = target;
            inventoryPanel.gameObject.SetActive(target == MenuMode.Inventory);
            characterPanel.gameObject.SetActive(target == MenuMode.Character);
            talentsPanel.gameObject.SetActive(target == MenuMode.Talents);
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
            activeCorpse = null;
            selectedItem = null;
            selectedInventoryIndex = -1;
            selectedEquipmentSlot = null;
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            if (characterPanel != null) characterPanel.gameObject.SetActive(false);
            if (talentsPanel != null) talentsPanel.gameObject.SetActive(false);
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
            BuildNavigation(canvasRect);
            BuildInventoryWindow(canvasRect);
            BuildCharacterWindow(canvasRect);
            BuildTalentsWindow(canvasRect);
            BuildLootWindow(canvasRect);
            GameObject tooltipObject = new("Item Tooltip", typeof(RectTransform), typeof(PhasebreakItemTooltipUI));
            tooltipObject.transform.SetParent(canvasRect, false);
            itemTooltip = tooltipObject.GetComponent<PhasebreakItemTooltipUI>();
            itemTooltip.Initialize(canvasRect);
        }

        private void BuildInventoryWindow(RectTransform root)
        {
            inventoryPanel = WindowPanel("Inventory Panel", root, new Vector2(.12f, .09f), new Vector2(.88f, .92f));
            BuildTitleBar(inventoryPanel, "RIFT SATCHEL", "INVENTORY", MenuMode.Inventory);
            RectTransform left = Section("Bag Section", inventoryPanel, new Vector2(.025f, .07f), new Vector2(.675f, .875f));
            Text("Bag Header", left, "FIELD INVENTORY", 13f, TextAlignmentOptions.Left, new Vector2(.03f, .91f), new Vector2(.5f, .985f), TextMuted);
            inventoryCount = Text("Capacity", left, string.Empty, 13f, TextAlignmentOptions.Right, new Vector2(.55f, .91f), new Vector2(.97f, .985f), TextMuted);
            BuildInventoryFilters(left);
            inventoryGrid = CreateScrollGrid(left, new Vector2(.025f, .11f), new Vector2(.975f, .82f), 9, new Vector2(82f, 82f), new Vector2(11f, 11f));
            Text("Inventory Controls", left, "LEFT-CLICK  SELECT     RIGHT-CLICK / DOUBLE-CLICK  EQUIP", 11f, TextAlignmentOptions.Center, new Vector2(.03f, .018f), new Vector2(.97f, .09f), TextMuted);

            RectTransform details = Section("Selected Item", inventoryPanel, new Vector2(.695f, .07f), new Vector2(.975f, .875f));
            Text("Inspect Label", details, "ITEM INSPECTION", 13f, TextAlignmentOptions.Left, new Vector2(.06f, .91f), new Vector2(.94f, .985f), TextMuted);
            RectTransform iconFrame = FramedIcon("Selected Icon Frame", details, new Vector2(.08f, .72f), new Vector2(.34f, .88f), Cyan);
            selectedItemIcon = IconImage("Selected Icon", iconFrame, new Vector2(.12f, .12f), new Vector2(.88f, .88f));
            selectedItemTitle = Text("Selected Name", details, "Select an item", 20f, TextAlignmentOptions.TopLeft, new Vector2(.39f, .71f), new Vector2(.94f, .88f), TextPrimary);
            selectedItemDetails = Text("Selected Details", details, "Hover an item for its full tooltip.\nSelect it to inspect actions here.", 14f, TextAlignmentOptions.TopLeft, new Vector2(.07f, .25f), new Vector2(.93f, .69f), TextMuted);
            selectedEquipButton = TextButton("Equip Button", details, "EQUIP ITEM", new Vector2(.12f, .12f), new Vector2(.88f, .22f), TryEquipSelected);
            selectedEquipButton.interactable = false;
            inventoryStatus = Text("Inventory Status", details, string.Empty, 12f, TextAlignmentOptions.Center, new Vector2(.06f, .035f), new Vector2(.94f, .105f), TextMuted);
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
                Text("Label", rect, filter == InventoryFilter.Relics ? "CORES / RELICS" : filter.ToString().ToUpperInvariant(), 10.5f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, TextPrimary);
                button.onClick.AddListener(() => SetFilter(filter));
            }
        }

        private void BuildCharacterWindow(RectTransform root)
        {
            characterPanel = WindowPanel("Character Panel", root, new Vector2(.055f, .055f), new Vector2(.945f, .945f));
            BuildTitleBar(characterPanel, "THE PHASEBOUND", "CHARACTER", MenuMode.Character);
            characterTitle = Text("Identity", characterPanel, string.Empty, 10.5f, TextAlignmentOptions.Left, new Vector2(.035f, .872f), new Vector2(.55f, .894f), TextMuted);
            equipmentStage = Section("Equipment Stage", characterPanel, new Vector2(.025f, .055f), new Vector2(.64f, .87f));
            BuildPaperDoll(equipmentStage);
            RectTransform analysis = Section("Build Analysis", characterPanel, new Vector2(.66f, .055f), new Vector2(.975f, .87f));
            Text("Analysis Header", analysis, "CHARACTER ANALYSIS", 13f, TextAlignmentOptions.Left, new Vector2(.06f, .925f), new Vector2(.94f, .985f), TextMuted);
            characterSummary = Text("Build Summary", analysis, string.Empty, 14f, TextAlignmentOptions.TopLeft, new Vector2(.06f, .13f), new Vector2(.94f, .915f), TextPrimary);
            RectTransform passiveSurface = FramedIcon("Passive Icon Frame", analysis, new Vector2(.835f, .575f), new Vector2(.925f, .66f), new Color(.36f, .22f, .58f, 1f));
            passiveIcon = IconImage("Passive Icon", passiveSurface, new Vector2(.08f, .08f), new Vector2(.92f, .92f));
            passiveIcon.preserveAspect = true;
            AddSpecButtons(analysis);
        }

        private void BuildPaperDoll(RectTransform parent)
        {
            RectTransform glow = Block("Silhouette Glow", parent, new Color(.04f, .18f, .27f, .55f));
            glow.anchorMin = glow.anchorMax = new Vector2(.5f, .52f);
            glow.pivot = new Vector2(.5f, .5f);
            glow.sizeDelta = new Vector2(276f, 488f);
            Outline glowOutline = glow.gameObject.AddComponent<Outline>();
            glowOutline.effectColor = new Color(.08f, .58f, .8f, .4f);
            glowOutline.effectDistance = new Vector2(3f, -3f);
            Image silhouette = IconImage("Character Silhouette", glow, new Vector2(.08f, .04f), new Vector2(.92f, .96f));
            silhouette.sprite = PhasebreakItemIconLibrary.GetCharacterSilhouette();
            silhouette.color = Color.white;
            silhouette.preserveAspect = true;
            Text("Identity Mark", glow, "PHASEBOUND", 12f, TextAlignmentOptions.Bottom, new Vector2(0f, .01f), new Vector2(1f, .09f), new Color(.22f, .76f, .92f, .8f));
            RectTransform line = Block("Central Arcane Line", parent, new Color(.1f, .65f, .86f, .45f));
            line.anchorMin = line.anchorMax = new Vector2(.5f, .52f);
            line.pivot = new Vector2(.5f, .5f);
            line.sizeDelta = new Vector2(2f, 520f);
        }

        private void BuildTalentsWindow(RectTransform root)
        {
            talentsPanel = WindowPanel("Talents Panel", root, new Vector2(.28f, .25f), new Vector2(.72f, .75f));
            BuildTitleBar(talentsPanel, "VOID MATRIX", "TALENTS", MenuMode.Talents);
            Image icon = IconImage("Talent Sigil", talentsPanel, new Vector2(.42f, .58f), new Vector2(.58f, .78f));
            icon.sprite = PhasebreakItemIconLibrary.Get(EquipmentSlot.WildcardArtifact);
            icon.color = new Color(.64f, .45f, 1f);
            Text("Talent Placeholder", talentsPanel, "<size=24><b>TALENT MATRIX OFFLINE</b></size>\n\nThe navigation channel is active.\nThe full talent tree belongs to a later phase.", 16f, TextAlignmentOptions.Center, new Vector2(.1f, .18f), new Vector2(.9f, .58f), TextPrimary);
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
            RectTransform nav = Block("Gameplay Navigation", root, new Color(.012f, .02f, .035f, .94f));
            nav.anchorMin = nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(1f, 0f);
            nav.anchoredPosition = new Vector2(-22f, 20f);
            nav.sizeDelta = new Vector2(208f, 70f);
            Outline outline = nav.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.08f, .45f, .62f, .8f);
            outline.effectDistance = new Vector2(1f, -1f);
            NavButton(nav, MenuMode.Inventory, EquipmentSlot.Core, 0, "Inventory  [B]");
            NavButton(nav, MenuMode.Character, EquipmentSlot.Chest, 1, "Character  [C]");
            NavButton(nav, MenuMode.Talents, EquipmentSlot.WildcardArtifact, 2, "Talents  [T]");
            navTooltip = Text("Navigation Tooltip", root, string.Empty, 13f, TextAlignmentOptions.Center, new Vector2(.77f, .105f), new Vector2(.99f, .15f), TextPrimary);
            navTooltip.gameObject.SetActive(false);
        }

        private void NavButton(RectTransform root, MenuMode target, EquipmentSlot iconSlot, int index, string hint)
        {
            RectTransform rect = Block(target.ToString(), root, PanelLight);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(9f + index * 66f, 8f);
            rect.sizeDelta = new Vector2(58f, 54f);
            Image background = rect.GetComponent<Image>();
            navImages[target] = background;
            Image icon = IconImage("Icon", rect, new Vector2(.23f, .2f), new Vector2(.77f, .8f));
            icon.sprite = PhasebreakItemIconLibrary.Get(iconSlot);
            icon.color = TextPrimary;
            ItemSlotUI relay = rect.gameObject.AddComponent<ItemSlotUI>();
            relay.Configure(() => { navTooltip.text = hint; navTooltip.gameObject.SetActive(true); }, () => navTooltip.gameObject.SetActive(false), () => Toggle(target));
            relay.ConfigureVisual(background, PanelLight, new Color(.08f, .2f, .29f, 1f), Cyan);
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
            int capacity = Mathf.Max(45, Mathf.CeilToInt(Mathf.Max(1, visibleCount) / 9f) * 9);
            for (int i = visibleCount; i < capacity; i++) CreateInventoryCell(inventoryGrid, null, i, default);
            inventoryCount.text = $"{build.Inventory.Count} ITEMS  •  {visibleCount} SHOWN  •  <color=#5EFF91>{upgradeCount} UPGRADES</color>";
            UpdateSelectedItemPanel();
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
            relay.ConfigureVisual(visual, new Color(.035f, .055f, .082f, 1f), new Color(.075f, .13f, .18f, 1f), new Color(.08f, .24f, .32f, 1f));
            relay.SetSelected(item == selectedItem && index == selectedInventoryIndex);
        }

        private void SelectInventoryItem(PhasebreakItemDefinition item, int index) { selectedItem = item; selectedInventoryIndex = index; inventoryStatus.text = "Selected for inspection"; RefreshInventory(); }
        private void TryEquipSelected() { if (selectedItem != null) TryEquip(selectedItem); }
        private void TryEquip(PhasebreakItemDefinition item)
        {
            itemTooltip.Hide();
            if (build != null && build.Equip(item)) { selectedItem = null; selectedInventoryIndex = -1; inventoryStatus.text = $"Equipped {item.displayName}"; }
            else inventoryStatus.text = "That item cannot be equipped right now";
        }

        private void UpdateSelectedItemPanel()
        {
            if (selectedItem == null)
            {
                selectedItemIcon.sprite = PhasebreakItemIconLibrary.Get(EquipmentSlot.Core);
                selectedItemIcon.color = new Color(.25f, .36f, .46f, .65f);
                selectedItemTitle.text = "Select an item";
                selectedItemDetails.text = "Hover an item for full details and comparison.\n\nSelect an item to inspect it here.";
                selectedEquipButton.interactable = false;
                return;
            }
            selectedItemIcon.sprite = PhasebreakItemIconLibrary.Resolve(selectedItem);
            selectedItemIcon.color = selectedItem.icon != null ? Color.white : RarityTint(selectedItem.rarity);
            selectedItemTitle.text = $"<color={RarityHex(selectedItem.rarity)}><b>{selectedItem.displayName}</b></color>\n<size=12><color=#8292A8>{selectedItem.rarity}  •  ITEM LEVEL {selectedItem.itemLevel}</color></size>";
            StringBuilder details = new();
            GearComparisonResult comparison = GearUpgradeEvaluator.Evaluate(selectedItem, build);
            if (comparison.IsUpgrade)
            {
                string reason = comparison.HasEquippedComparison
                    ? $"+{comparison.Delta:0.0} ESTIMATED BUILD SCORE"
                    : $"EMPTY {Pretty(comparison.ComparedSlot).ToUpperInvariant()} SLOT";
                details.Append($"<color=#5EFF91><b>UPGRADE</b>  {reason}</color>\n\n");
            }
            details.Append($"<b>{Pretty(selectedItem.slot)}</b>\n");
            if (selectedItem.tags != ItemTag.None) details.Append($"<color=#64CDEB>{selectedItem.tags.ToString().Replace(",", "  •")}</color>\n\n");
            details.Append(selectedItem.description);
            if (selectedItem.itemSet != null) details.Append($"\n\n<color=#73D6EE>{selectedItem.itemSet.displayName}</color>");
            AppendCompactStats(details, selectedItem.stats);
            selectedItemDetails.text = details.ToString();
            selectedEquipButton.interactable = true;
        }

        private void RefreshCharacter()
        {
            if (build == null) return;
            Clear(equipmentCells);
            characterTitle.text = $"LEVEL {(progression != null ? progression.Level : 1)}  •  {build.Specialization.ToString().ToUpperInvariant()}  •  16 EQUIPMENT CHANNELS";
            CreateEquipmentSlot(EquipmentSlot.Head, new Vector2(.07f, .79f), false);
            CreateEquipmentSlot(EquipmentSlot.Shoulders, new Vector2(.07f, .64f), false);
            CreateEquipmentSlot(EquipmentSlot.Chest, new Vector2(.07f, .49f), false);
            CreateEquipmentSlot(EquipmentSlot.Hands, new Vector2(.07f, .34f), false);
            CreateEquipmentSlot(EquipmentSlot.Legs, new Vector2(.07f, .19f), false);
            CreateEquipmentSlot(EquipmentSlot.Boots, new Vector2(.07f, .04f), false);
            CreateEquipmentSlot(EquipmentSlot.PrimaryWeapon, new Vector2(.78f, .79f), true);
            CreateEquipmentSlot(EquipmentSlot.Secondary, new Vector2(.78f, .64f), true);
            CreateEquipmentSlot(EquipmentSlot.Core, new Vector2(.78f, .49f), true);
            CreateEquipmentSlot(EquipmentSlot.MobilityRelic, new Vector2(.78f, .34f), true);
            CreateEquipmentSlot(EquipmentSlot.PowerRelic, new Vector2(.78f, .19f), true);
            CreateEquipmentSlot(EquipmentSlot.UtilityRelic, new Vector2(.78f, .04f), true);
            CreateEquipmentSlot(EquipmentSlot.Sigil1, new Vector2(.29f, .035f), true, true);
            CreateEquipmentSlot(EquipmentSlot.Sigil2, new Vector2(.405f, .035f), true, true);
            CreateEquipmentSlot(EquipmentSlot.Sigil3, new Vector2(.52f, .035f), true, true);
            CreateEquipmentSlot(EquipmentSlot.WildcardArtifact, new Vector2(.635f, .035f), true, true);
            passiveIcon.sprite = progression != null ? progression.PassiveIcon : PhasebreakIconCatalog.Current?.fallbackBuff;
            passiveIcon.color = passiveIcon.sprite == null ? Color.clear : progression != null && progression.HasKeenEdge ? Color.white : new Color(.35f, .4f, .48f, .65f);
            characterSummary.text = BuildCharacterSummary();
        }

        private void CreateEquipmentSlot(EquipmentSlot slot, Vector2 anchor, bool labelOnLeft, bool compact = false)
        {
            PhasebreakItemDefinition item = build.GetEquipped(slot);
            Vector2 size = compact ? new Vector2(70f, 70f) : new Vector2(78f, 78f);
            RectTransform holder = new GameObject(slot.ToString(), typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(equipmentStage, false);
            holder.anchorMin = holder.anchorMax = anchor;
            holder.pivot = Vector2.zero;
            holder.sizeDelta = compact ? new Vector2(78f, 96f) : new Vector2(205f, 82f);
            equipmentCells.Add(holder.gameObject);
            RectTransform slotRect = Block("Equipment Slot", holder, RarityColor(item != null ? item.rarity : ItemRarity.Common));
            slotRect.anchorMin = slotRect.anchorMax = labelOnLeft && !compact ? new Vector2(1f, .5f) : new Vector2(0f, .5f);
            slotRect.pivot = labelOnLeft && !compact ? new Vector2(1f, .5f) : new Vector2(0f, .5f);
            slotRect.anchoredPosition = Vector2.zero;
            slotRect.sizeDelta = size;
            RectTransform surface = Block("Slot Surface", slotRect, new Color(.035f, .055f, .082f, 1f));
            Stretch(surface, 3f);
            Image icon = IconImage("Slot Icon", surface, new Vector2(.17f, .17f), new Vector2(.83f, .83f));
            icon.sprite = item != null ? PhasebreakItemIconLibrary.Resolve(item) : PhasebreakItemIconLibrary.Get(slot);
            icon.color = item != null ? (item.icon != null ? Color.white : RarityTint(item.rarity)) : new Color(.32f, .42f, .52f, .55f);
            icon.preserveAspect = true;
            if (!compact)
            {
                Vector2 labelMin = labelOnLeft ? new Vector2(0f, .08f) : new Vector2(.42f, .08f);
                Vector2 labelMax = labelOnLeft ? new Vector2(.58f, .92f) : new Vector2(1f, .92f);
                Text("Slot Label", holder, Pretty(slot).ToUpperInvariant(), 11f, labelOnLeft ? TextAlignmentOptions.Right : TextAlignmentOptions.Left, labelMin, labelMax, item != null ? TextPrimary : TextMuted);
            }
            else Text("Slot Label", holder, CompactSlotLabel(slot), 9f, TextAlignmentOptions.Center, new Vector2(-.1f, -.2f), new Vector2(1.1f, .02f), item != null ? TextPrimary : TextMuted);
            ItemSlotUI relay = slotRect.gameObject.AddComponent<ItemSlotUI>();
            relay.Configure(
                () => { if (item != null) itemTooltip.Show(item, null, slotRect); },
                itemTooltip.Hide,
                () => SelectEquipmentSlot(slot),
                item == null ? null : () => TryUnequip(slot),
                item == null ? null : () => TryUnequip(slot));
            relay.ConfigureVisual(surface.GetComponent<Image>(), new Color(.035f, .055f, .082f, 1f), new Color(.075f, .13f, .18f, 1f), new Color(.08f, .24f, .32f, 1f));
            relay.SetSelected(selectedEquipmentSlot == slot);
        }

        private void SelectEquipmentSlot(EquipmentSlot slot) { selectedEquipmentSlot = slot; itemTooltip.Hide(); RefreshCharacter(); }
        private void TryUnequip(EquipmentSlot slot) { itemTooltip.Hide(); if (build.Unequip(slot)) selectedEquipmentSlot = null; }

        private string BuildCharacterSummary()
        {
            float levelPower = progression != null ? progression.PowerMultiplier : 1f;
            int levelHealth = progression != null ? progression.BonusHealth : 0;
            float passiveCrit = progression != null ? progression.CriticalChanceBonus : 0f;
            string passive = progression != null && progression.HasKeenEdge ? progression.PassiveName : "Locked";
            StringBuilder text = new();
            text.Append("<color=#63D9F2><b>FINAL STATS</b></color>\n");
            StatRow(text, "Power", $"x{levelPower * build.PowerMultiplier:0.00}");
            StatRow(text, "Bonus health", $"+{levelHealth + build.BonusHealth}");
            StatRow(text, "Defense", build.Defense.ToString("P0"));
            StatRow(text, "Critical chance", $"+{passiveCrit + build.CriticalChanceBonus:P0}");
            StatRow(text, "Critical damage", $"+{build.CriticalDamageBonus:P0}");
            StatRow(text, "Attack speed", $"+{build.AttackSpeedMultiplier - 1f:P0}");
            StatRow(text, "Mobility", $"+{build.MovementSpeedMultiplier - 1f:P0}");
            StatRow(text, "Boss damage", $"+{build.BossDamageMultiplier - 1f:P0}");
            text.Append($"\n<color=#A984FF><b>SPECIALIZATION</b></color>\n{(build.Specialization == Specialization.Unchosen ? "Choose at level 3" : build.Specialization)}\n");
            text.Append($"\n<color=#A984FF><b>PASSIVE</b></color>\n{passive}\n");
            text.Append("\n<color=#73D6EE><b>ACTIVE SETS</b></color>\n");
            string sets = BuildSetSummary();
            text.Append(string.IsNullOrEmpty(sets) ? "<color=#738196>None active</color>\n" : sets);
            text.Append("\n<color=#73D6EE><b>BUILD MODIFIERS</b></color>\n");
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
                result.Append($"<b>{group.Key.displayName}</b>  <color=#63D9F2>{displayedCount}/{maximum}</color>\n");
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
            RectTransform outer = Block(item == null ? "Empty Slot" : item.displayName, root, item == null ? new Color(.06f, .075f, .1f, .9f) : RarityColor(item.rarity));
            list.Add(outer.gameObject);
            RectTransform surface = Block("Slot Surface", outer, new Color(.035f, .055f, .082f, 1f));
            Stretch(surface, 3f);
            Image icon = IconImage("Item Icon", surface, new Vector2(.14f, .14f), new Vector2(.86f, .86f));
            if (item != null)
            {
                icon.sprite = PhasebreakItemIconLibrary.Resolve(item);
                icon.color = item.icon != null ? Color.white : RarityTint(item.rarity);
                icon.preserveAspect = true;
                Text("Item Level", surface, item.itemLevel.ToString(), 10f, TextAlignmentOptions.BottomRight, new Vector2(.55f, .03f), new Vector2(.95f, .28f), TextPrimary);
                ItemSlotUI relay = outer.gameObject.AddComponent<ItemSlotUI>();
                relay.ConfigureVisual(surface.GetComponent<Image>(), new Color(.035f, .055f, .082f, 1f), new Color(.075f, .13f, .18f, 1f), new Color(.08f, .24f, .32f, 1f));
                if (comparison.IsUpgrade) AddUpgradeIndicator(outer);
            }
            else
            {
                icon.sprite = PhasebreakItemIconLibrary.Get(EquipmentSlot.Core);
                icon.color = new Color(.18f, .25f, .34f, .16f);
                icon.preserveAspect = true;
            }
            if (equipped) Text("Equipped", surface, "EQUIPPED", 9f, TextAlignmentOptions.Top, new Vector2(.05f, .76f), new Vector2(.95f, .97f), new Color(.4f, .92f, .64f));
            if (!string.IsNullOrEmpty(slotLabel)) Text("Slot", surface, slotLabel, 9f, TextAlignmentOptions.Bottom, new Vector2(.02f, .01f), new Vector2(.98f, .22f), TextMuted);
            return outer.gameObject;
        }

        private static void AddUpgradeIndicator(RectTransform slot)
        {
            Outline glow = slot.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(.22f, 1f, .46f, .62f);
            glow.effectDistance = new Vector2(2.5f, -2.5f);
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

        private void UpdateFilterVisuals() { foreach (KeyValuePair<InventoryFilter, Image> pair in filterImages) pair.Value.color = pair.Key == inventoryFilter ? new Color(.08f, .38f, .5f, 1f) : PanelLight; }

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
                Button button = TextButton(spec.ToString(), root, spec.ToString().ToUpperInvariant(), new Vector2(x, .035f), new Vector2(x + .28f, .105f), () => { if (build.ChooseSpecialization(captured)) RefreshCharacter(); }, 10.5f);
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

        private void UpdateNav() { foreach (KeyValuePair<MenuMode, Image> pair in navImages) pair.Value.color = mode == pair.Key ? new Color(.08f, .4f, .54f, 1f) : PanelLight; }
        private void SetActions(bool enabled) { foreach (InputAction action in new[] { inventoryAction, characterAction, talentsAction, escapeAction }) if (enabled) action.Enable(); else action.Disable(); }

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
            Outline outline = panel.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.08f, .52f, .7f, .85f); outline.effectDistance = new Vector2(2f, -2f);
            return panel;
        }

        private static RectTransform Section(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform section = Block(name, parent, Panel); Place(section, min, max, 0f);
            Outline outline = section.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.1f, .19f, .27f, .9f); outline.effectDistance = new Vector2(1f, -1f);
            return section;
        }

        private static RectTransform FramedIcon(string name, Transform parent, Vector2 min, Vector2 max, Color border)
        {
            RectTransform outer = Block(name, parent, border); Place(outer, min, max, 0f);
            RectTransform inner = Block("Surface", outer, PanelLight); Stretch(inner, 3f); return inner;
        }

        private static Button TextButton(string name, Transform parent, string value, Vector2 min, Vector2 max, Action click, float fontSize = 12f)
        {
            RectTransform rect = Block(name, parent, new Color(.07f, .16f, .23f, 1f)); Place(rect, min, max, 0f);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(.75f, 1f, 1f); colors.pressedColor = new Color(.55f, .8f, .9f); colors.disabledColor = new Color(.35f, .4f, .46f, .65f); button.colors = colors;
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
        private static void AppendCompactStats(StringBuilder text, BuildStats stats)
        {
            StringBuilder values = new();
            if (!Mathf.Approximately(stats.power, 0f)) values.Append($"Power {stats.power:+0%;-0%}  ");
            if (stats.maxHealth != 0) values.Append($"Health {stats.maxHealth:+0;-0}  ");
            if (!Mathf.Approximately(stats.defense, 0f)) values.Append($"Defense {stats.defense:+0%;-0%}  ");
            if (!Mathf.Approximately(stats.criticalChance, 0f)) values.Append($"Crit {stats.criticalChance:+0%;-0%}  ");
            if (!Mathf.Approximately(stats.criticalDamage, 0f)) values.Append($"Crit damage {stats.criticalDamage:+0%;-0%}  ");
            if (!Mathf.Approximately(stats.attackSpeed, 0f)) values.Append($"Attack speed {stats.attackSpeed:+0%;-0%}  ");
            if (!Mathf.Approximately(stats.movementSpeed, 0f)) values.Append($"Mobility {stats.movementSpeed:+0%;-0%}  ");
            if (!Mathf.Approximately(stats.bossDamage, 0f)) values.Append($"Boss damage {stats.bossDamage:+0%;-0%}");
            if (values.Length > 0) text.Append($"\n\n<color=#DDE7F3>{values}</color>");
        }
        private static string Pretty(EquipmentSlot slot) => System.Text.RegularExpressions.Regex.Replace(slot.ToString(), "([a-z])([A-Z0-9])", "$1 $2");
        private static string CompactSlotLabel(EquipmentSlot slot) => slot switch { EquipmentSlot.Sigil1 => "SIGIL I", EquipmentSlot.Sigil2 => "SIGIL II", EquipmentSlot.Sigil3 => "SIGIL III", EquipmentSlot.WildcardArtifact => "ARTIFACT", _ => Pretty(slot).ToUpperInvariant() };
        private static InputAction KeyAction(string name, string binding) => new(name, InputActionType.Button, binding);
        private static void EnsureEventSystem() { if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); }
        private static Color RarityColor(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => new Color(.86f, .28f, .12f, 1f), ItemRarity.Epic => new Color(.47f, .25f, .78f, 1f), ItemRarity.Rare => new Color(.12f, .42f, .72f, 1f), ItemRarity.Uncommon => new Color(.1f, .52f, .29f, 1f), _ => new Color(.19f, .23f, .3f, 1f) };
        private static Color RarityTint(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => new Color(1f, .47f, .3f), ItemRarity.Epic => new Color(.72f, .55f, 1f), ItemRarity.Rare => new Color(.3f, .68f, 1f), ItemRarity.Uncommon => new Color(.4f, .88f, .55f), _ => new Color(.82f, .86f, .92f) };
        private static string RarityHex(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => "#FF784E", ItemRarity.Epic => "#B58CFF", ItemRarity.Rare => "#4BA3FF", ItemRarity.Uncommon => "#62D77B", _ => "#D6D9DE" };
    }
}
