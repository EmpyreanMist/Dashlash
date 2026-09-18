using System;
using System.Collections.Generic;
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
        private static readonly EquipmentSlot[] Slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));
        private PlayerBuildSystem build; private PlayerProgression progression; private InputAction toggle;
        private RectTransform panel, inventoryRoot, gearRoot; private TextMeshProUGUI tooltip, summary, title;
        private readonly List<GameObject> dynamicRows = new(); private bool open;

        private void Awake()
        {
            build = FindAnyObjectByType<PlayerBuildSystem>(); progression = FindAnyObjectByType<PlayerProgression>();
            toggle = new InputAction("Build Vault", InputActionType.Button); toggle.AddBinding("<Keyboard>/b"); toggle.AddBinding("<Keyboard>/i");
            EnsureEventSystem(); BuildUi(); SetOpen(false);
        }
        private void OnEnable() { toggle.Enable(); if (build != null) build.BuildChanged += Refresh; }
        private void OnDisable() { toggle.Disable(); if (build != null) build.BuildChanged -= Refresh; if (open) SetOpen(false); }
        private void OnDestroy() => toggle.Dispose();
        private void Update() { if (toggle.WasPressedThisFrame()) SetOpen(!open); }

        private void SetOpen(bool value)
        {
            open = value; if (panel != null) panel.gameObject.SetActive(value);
            Cursor.visible = value; Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            if (value) Refresh();
        }

        private void BuildUi()
        {
            Canvas canvas = FindAnyObjectByType<PhasebreakHud>()?.GetComponentInChildren<Canvas>();
            if (canvas == null) { GameObject go = new("Phasebreak Build Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; }
            panel = Block("Build Vault", canvas.transform, new Color(.018f, .025f, .045f, .97f)); panel.anchorMin = new Vector2(.06f, .07f); panel.anchorMax = new Vector2(.94f, .93f); panel.offsetMin = panel.offsetMax = Vector2.zero;
            title = Text("Title", panel, "CONSTELLATION VAULT   •   B / I TO CLOSE", 24, TextAlignmentOptions.Center); Place(title.rectTransform, new Vector2(0, .92f), new Vector2(1, 1), 12);
            inventoryRoot = Block("Inventory", panel, new Color(.04f, .055f, .085f, .95f)); Place(inventoryRoot, new Vector2(.02f, .05f), new Vector2(.35f, .9f), 0);
            gearRoot = Block("Equipment", panel, new Color(.035f, .05f, .075f, .95f)); Place(gearRoot, new Vector2(.37f, .05f), new Vector2(.68f, .9f), 0);
            RectTransform right = Block("Analysis", panel, new Color(.03f, .04f, .065f, .95f)); Place(right, new Vector2(.7f, .05f), new Vector2(.98f, .9f), 0);
            summary = Text("Summary", right, "", 15, TextAlignmentOptions.TopLeft); Place(summary.rectTransform, new Vector2(.04f, .37f), new Vector2(.96f, .96f), 0);
            tooltip = Text("Tooltip", right, "Hover an item to inspect it.", 14, TextAlignmentOptions.TopLeft); Place(tooltip.rectTransform, new Vector2(.04f, .02f), new Vector2(.96f, .17f), 0);
            AddSpecializationButton(right, Specialization.Berserker, .31f);
            AddSpecializationButton(right, Specialization.Bulwark, .25f);
            AddSpecializationButton(right, Specialization.Riftblade, .19f);
        }

        private void Refresh()
        {
            foreach (GameObject row in dynamicRows) Destroy(row); dynamicRows.Clear(); if (build == null) return;
            AddHeader(inventoryRoot, "INVENTORY — click to equip", 0);
            int index = 1; foreach (PhasebreakItemDefinition item in build.Inventory) AddItemButton(inventoryRoot, item, index++, () => build.Equip(item), build.GetEquipped(item.slot));
            AddHeader(gearRoot, "EQUIPMENT — click to unequip", 0);
            for (int i = 0; i < Slots.Length; i++)
            {
                EquipmentSlot slot = Slots[i]; PhasebreakItemDefinition item = build.GetEquipped(slot);
                AddSlotButton(gearRoot, slot, item, i + 1);
            }
            summary.text = "<b>BUILD SUMMARY</b>\n" + build.GetBuildSummary() + SpecializationText();
        }

        private string SpecializationText()
        {
            if (progression == null || progression.Level < 3 || build.Specialization != Specialization.Unchosen) return "";
            return "\n\n<b>SPECIALIZATION READY</b>\nChoose below. This choice is saved locally.";
        }

        private void AddSpecializationButton(RectTransform root, Specialization choice, float y)
        {
            RectTransform rt = Block(choice.ToString(), root, new Color(.13f, .16f, .24f, .98f));
            rt.anchorMin = new Vector2(.08f, y); rt.anchorMax = new Vector2(.92f, y + .05f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            Button button = rt.gameObject.AddComponent<Button>(); TextMeshProUGUI label = Text("Label", rt, choice.ToString(), 13, TextAlignmentOptions.Center); Place(label.rectTransform, Vector2.zero, Vector2.one, 0);
            button.onClick.AddListener(() => { if (build != null && build.ChooseSpecialization(choice)) Refresh(); });
        }

        private void AddHeader(RectTransform root, string value, int row)
        {
            TextMeshProUGUI label = Text("Header", root, value, 15, TextAlignmentOptions.Center); Row(label.rectTransform, row, 34); dynamicRows.Add(label.gameObject);
        }
        private void AddItemButton(RectTransform root, PhasebreakItemDefinition item, int row, Action action, PhasebreakItemDefinition compare)
        {
            Button button = MakeButton(root, $"<color={Rarity(item.rarity)}>{item.displayName}</color>  <size=12>iLvl {item.itemLevel} • {Pretty(item.slot)}</size>", row);
            button.onClick.AddListener(() => { action(); Refresh(); }); AddHover(button.gameObject, () => ShowTooltip(item, compare));
        }
        private void AddSlotButton(RectTransform root, EquipmentSlot slot, PhasebreakItemDefinition item, int row)
        {
            string value = item == null ? $"<color=#7890A8>{Pretty(slot)}</color>  — empty" : $"<color=#87DBFF>{Pretty(slot)}</color>  <b>{item.displayName}</b>";
            Button button = MakeButton(root, value, row); button.interactable = item != null;
            if (item != null) { button.onClick.AddListener(() => { build.Unequip(slot); Refresh(); }); AddHover(button.gameObject, () => ShowTooltip(item, null)); }
        }
        private void ShowTooltip(PhasebreakItemDefinition item, PhasebreakItemDefinition compare)
        {
            StringBuilder s = new(); s.Append($"<size=18><b><color={Rarity(item.rarity)}>{item.displayName}</color></b></size>\n{item.rarity} • iLvl {item.itemLevel} • {Pretty(item.slot)}\nTags: {item.tags}\n");
            AppendStat(s, "Power", item.stats.power, compare?.stats.power ?? 0, true); AppendStat(s, "Health", item.stats.maxHealth, compare?.stats.maxHealth ?? 0, false);
            AppendStat(s, "Crit", item.stats.criticalChance, compare?.stats.criticalChance ?? 0, true); AppendStat(s, "Crit Damage", item.stats.criticalDamage, compare?.stats.criticalDamage ?? 0, true);
            AppendStat(s, "Attack Speed", item.stats.attackSpeed, compare?.stats.attackSpeed ?? 0, true); AppendStat(s, "Mobility", item.stats.movementSpeed, compare?.stats.movementSpeed ?? 0, true);
            if (item.effects != BuildEffect.None) s.Append($"\n<color=#B58CFF>{item.effects}</color>\n"); if (item.itemSet != null) s.Append($"Set: {item.itemSet.displayName}\n"); s.Append($"\n{item.description}"); tooltip.text = s.ToString();
        }
        private static void AppendStat(StringBuilder s, string name, float value, float old, bool percent)
        {
            if (Mathf.Approximately(value, 0f)) return; float delta = value - old; string color = delta > .001f ? "#62E68A" : delta < -.001f ? "#FF6670" : "#C9D2E3"; s.Append($"\n<color={color}>{name}: +{(percent ? value.ToString("P0") : value.ToString("0"))}</color>");
        }
        private static void AddHover(GameObject go, Action action) { EventTrigger trigger = go.AddComponent<EventTrigger>(); EventTrigger.Entry e = new() { eventID = EventTriggerType.PointerEnter }; e.callback.AddListener(_ => action()); trigger.triggers.Add(e); }
        private Button MakeButton(Transform root, string value, int row)
        {
            RectTransform rt = Block("Row", root, row % 2 == 0 ? new Color(.07f, .09f, .13f, .95f) : new Color(.055f, .07f, .105f, .95f)); Row(rt, row, 38); Button b = rt.gameObject.AddComponent<Button>(); TextMeshProUGUI label = Text("Label", rt, value, 13, TextAlignmentOptions.MidlineLeft); Place(label.rectTransform, Vector2.zero, Vector2.one, 8); dynamicRows.Add(rt.gameObject); return b;
        }
        private static void EnsureEventSystem() { if (EventSystem.current != null) return; GameObject go = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); }
        private static RectTransform Block(string name, Transform parent, Color color) { GameObject go = new(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>(); }
        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment) { GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false); TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>(); t.text = value; t.fontSize = size; t.color = new Color(.88f, .93f, 1f); t.alignment = alignment; t.textWrappingMode = TextWrappingModes.Normal; return t; }
        private static void Place(RectTransform rt, Vector2 min, Vector2 max, float inset) { rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset); }
        private static void Row(RectTransform rt, int row, float height) { rt.anchorMin = new Vector2(.03f, 1); rt.anchorMax = new Vector2(.97f, 1); rt.pivot = new Vector2(.5f, 1); rt.anchoredPosition = new Vector2(0, -10 - row * (height + 3)); rt.sizeDelta = new Vector2(0, height); }
        private static string Pretty(EquipmentSlot slot) => System.Text.RegularExpressions.Regex.Replace(slot.ToString(), "([a-z])([A-Z0-9])", "$1 $2");
        private static string Rarity(ItemRarity rarity) => rarity switch { ItemRarity.Mythic => "#FF784E", ItemRarity.Epic => "#B58CFF", ItemRarity.Rare => "#4BA3FF", ItemRarity.Uncommon => "#62D77B", _ => "#D6D9DE" };
    }
}
