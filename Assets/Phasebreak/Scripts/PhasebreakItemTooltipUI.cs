using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PhasebreakItemTooltipUI : MonoBehaviour
    {
        private RectTransform canvasRect;
        private RectTransform root;
        private RectTransform comparisonPanel;
        private TextMeshProUGUI itemText;
        private TextMeshProUGUI comparisonText;
        private Image itemIcon;
        private Image comparisonIcon;
        private Outline rootOutline;
        private PlayerBuildSystem build;

        public void Initialize(RectTransform canvas)
        {
            canvasRect = canvas;
            build = FindAnyObjectByType<PlayerBuildSystem>();
            root = GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
            root.pivot = new Vector2(0f, 1f);
            root.sizeDelta = new Vector2(386f, 462f);
            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(.012f, .019f, .032f, .99f);
            rootOutline = gameObject.AddComponent<Outline>();
            rootOutline.effectColor = new Color(.2f, .72f, .9f, .9f);
            rootOutline.effectDistance = new Vector2(2f, -2f);
            CanvasGroup group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform accent = Block("Arcane Accent", root, new Color(.1f, .68f, .86f, 1f));
            Place(accent, new Vector2(0f, .984f), Vector2.one, 0f);

            RectTransform iconFrame = Block("Item Icon Frame", root, new Color(.06f, .1f, .16f, 1f));
            iconFrame.anchorMin = iconFrame.anchorMax = new Vector2(0f, 1f);
            iconFrame.pivot = new Vector2(0f, 1f);
            iconFrame.anchoredPosition = new Vector2(18f, -24f);
            iconFrame.sizeDelta = new Vector2(72f, 72f);
            itemIcon = ImageChild("Item Icon", iconFrame, new Vector2(.12f, .12f), new Vector2(.88f, .88f));
            itemIcon.preserveAspect = true;

            itemText = Text("Item Details", root, string.Empty, 14f, TextAlignmentOptions.TopLeft, new Vector2(.05f, .035f), new Vector2(.95f, .94f));
            itemText.margin = new Vector4(0f, 0f, 0f, 0f);

            comparisonPanel = Block("Equipped Comparison", root.parent, new Color(.012f, .019f, .032f, .99f));
            comparisonPanel.sizeDelta = root.sizeDelta;
            comparisonPanel.anchorMin = comparisonPanel.anchorMax = new Vector2(.5f, .5f);
            comparisonPanel.pivot = new Vector2(0f, 1f);
            Outline compareOutline = comparisonPanel.gameObject.AddComponent<Outline>();
            compareOutline.effectColor = new Color(.39f, .28f, .68f, .9f);
            compareOutline.effectDistance = new Vector2(2f, -2f);
            CanvasGroup compareGroup = comparisonPanel.gameObject.AddComponent<CanvasGroup>();
            compareGroup.blocksRaycasts = false;
            compareGroup.interactable = false;
            RectTransform compareAccent = Block("Void Accent", comparisonPanel, new Color(.48f, .28f, .86f, 1f));
            Place(compareAccent, new Vector2(0f, .984f), Vector2.one, 0f);
            RectTransform compareIconFrame = Block("Equipped Icon Frame", comparisonPanel, new Color(.06f, .1f, .16f, 1f));
            compareIconFrame.anchorMin = compareIconFrame.anchorMax = new Vector2(0f, 1f);
            compareIconFrame.pivot = new Vector2(0f, 1f);
            compareIconFrame.anchoredPosition = new Vector2(18f, -24f);
            compareIconFrame.sizeDelta = new Vector2(72f, 72f);
            comparisonIcon = ImageChild("Equipped Icon", compareIconFrame, new Vector2(.12f, .12f), new Vector2(.88f, .88f));
            comparisonIcon.preserveAspect = true;
            comparisonText = Text("Equipped Details", comparisonPanel, string.Empty, 14f, TextAlignmentOptions.TopLeft, new Vector2(.05f, .035f), new Vector2(.95f, .94f));
            comparisonPanel.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Show(PhasebreakItemDefinition item, PhasebreakItemDefinition equipped, RectTransform source, GearComparisonResult comparison = default)
        {
            if (item == null || canvasRect == null)
                return;

            itemIcon.sprite = PhasebreakItemIconLibrary.Resolve(item);
            itemIcon.color = item.icon != null ? Color.white : RarityTint(item.rarity);
            itemText.text = BuildItemText(item, equipped, false, comparison);
            rootOutline.effectColor = comparison.IsUpgrade
                ? new Color(.25f, 1f, .49f, .9f)
                : new Color(.2f, .72f, .9f, .9f);
            gameObject.SetActive(true);
            root.SetAsLastSibling();

            bool compare = equipped != null;
            comparisonPanel.gameObject.SetActive(compare);
            if (compare)
            {
                comparisonIcon.sprite = PhasebreakItemIconLibrary.Resolve(equipped);
                comparisonIcon.color = equipped.icon != null ? Color.white : RarityTint(equipped.rarity);
                comparisonText.text = BuildItemText(equipped, null, true, default);
                comparisonPanel.SetAsLastSibling();
            }

            Position(source, compare);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            if (comparisonPanel != null)
                comparisonPanel.gameObject.SetActive(false);
        }

        private void Position(RectTransform source, bool hasComparison)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, source.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local);
            Rect bounds = canvasRect.rect;
            float combinedWidth = root.sizeDelta.x + (hasComparison ? comparisonPanel.sizeDelta.x + 10f : 0f);
            float x = local.x + 58f;
            if (x + combinedWidth > bounds.xMax - 12f)
                x = local.x - combinedWidth - 58f;
            x = Mathf.Clamp(x, bounds.xMin + 12f, bounds.xMax - combinedWidth - 12f);
            float y = Mathf.Clamp(local.y + 50f, bounds.yMin + root.sizeDelta.y + 12f, bounds.yMax - 12f);
            root.anchoredPosition = new Vector2(x, y);
            if (hasComparison)
                comparisonPanel.anchoredPosition = new Vector2(x + root.sizeDelta.x + 10f, y);
        }

        private string BuildItemText(PhasebreakItemDefinition item, PhasebreakItemDefinition compare, bool equipped, GearComparisonResult comparison)
        {
            StringBuilder text = new();
            text.Append($"<indent=86><size=12><color=#7E90A8>{(equipped ? "CURRENTLY EQUIPPED" : "ITEM")}</color></size>\n");
            text.Append($"<size=20><b><color={RarityHex(item.rarity)}>{item.displayName}</color></b></size>\n");
            text.Append($"<size=13><color=#A9B7CA>{item.rarity}  •  Item Level {item.itemLevel}</color></size></indent>\n\n");
            text.Append($"<b>{Pretty(item.slot)}</b>\n");
            if (item.tags != ItemTag.None)
                text.Append($"<color=#73CAE7>Tags</color>  {PrettyFlags(item.tags.ToString())}\n");

            if (!equipped)
            {
                if (comparison.IsUpgrade)
                {
                    string reason = comparison.HasEquippedComparison
                        ? $"+{comparison.Delta:0.0} estimated build score"
                        : $"Empty {Pretty(comparison.ComparedSlot)} slot";
                    text.Append($"\n<color=#5EFF91><b>UPGRADE</b>  {reason}</color>\n");
                }
                else if (comparison.IsWorse)
                {
                    text.Append($"\n<color=#A17880>Lower estimated build score  {comparison.Delta:0.0}</color>\n");
                }
                else if (comparison.Kind == GearComparisonKind.Equivalent)
                {
                    text.Append("\n<color=#8998AA>Equivalent estimated build score</color>\n");
                }
            }

            AppendStat(text, "Power", item.stats.power, compare?.stats.power ?? 0f, true, compare != null);
            AppendStat(text, "Health", item.stats.maxHealth, compare?.stats.maxHealth ?? 0f, false, compare != null);
            AppendStat(text, "Defense", item.stats.defense, compare?.stats.defense ?? 0f, true, compare != null);
            AppendStat(text, "Critical chance", item.stats.criticalChance, compare?.stats.criticalChance ?? 0f, true, compare != null);
            AppendStat(text, "Critical damage", item.stats.criticalDamage, compare?.stats.criticalDamage ?? 0f, true, compare != null);
            AppendStat(text, "Attack speed", item.stats.attackSpeed, compare?.stats.attackSpeed ?? 0f, true, compare != null);
            AppendStat(text, "Movement speed", item.stats.movementSpeed, compare?.stats.movementSpeed ?? 0f, true, compare != null);
            AppendStat(text, "Boss damage", item.stats.bossDamage, compare?.stats.bossDamage ?? 0f, true, compare != null);

            if (item.itemSet != null)
            {
                int equippedPieces = CountEquippedSetPieces(item.itemSet);
                int maximumPieces = 0;
                foreach (SetBonusDefinition bonus in item.itemSet.bonuses ?? System.Array.Empty<SetBonusDefinition>())
                    maximumPieces = Mathf.Max(maximumPieces, bonus.pieces);
                int displayedPieces = maximumPieces > 0 ? Mathf.Min(equippedPieces, maximumPieces) : equippedPieces;
                text.Append($"\n<color=#66D6F1><b>{item.itemSet.displayName}</b>  {displayedPieces}/{maximumPieces}</color>\n");
                text.Append($"<color=#8698AE>{item.itemSet.fantasy}</color>\n");
                foreach (SetBonusDefinition bonus in item.itemSet.bonuses ?? System.Array.Empty<SetBonusDefinition>())
                {
                    bool active = equippedPieces >= bonus.pieces;
                    string color = active ? "#5EE58C" : "#68778A";
                    string state = active ? "ACTIVE" : "LOCKED";
                    text.Append($"<color={color}><b>{bonus.pieces}-PIECE · {state}</b>  {bonus.description}</color>\n");
                }
            }
            if (item.effects != BuildEffect.None)
            {
                string value = item.effectValue > 0f ? $"  {item.effectValue:0.##}" : string.Empty;
                text.Append($"\n<color=#C59AFF><b>{PrettyFlags(item.effects.ToString())}</b>{value}</color>");
            }
            if (!string.IsNullOrWhiteSpace(item.description))
                text.Append($"\n\n<i><color=#AEB8C8>{item.description}</color></i>");
            return text.ToString();
        }

        private int CountEquippedSetPieces(PhasebreakItemSetDefinition set)
        {
            if (set == null || build == null)
                return 0;
            int count = 0;
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                if (build.GetEquipped(slot)?.itemSet == set)
                    count++;
            return count;
        }

        private static void AppendStat(StringBuilder text, string label, float value, float oldValue, bool percent, bool comparing)
        {
            if (Mathf.Approximately(value, 0f) && (!comparing || Mathf.Approximately(oldValue, 0f)))
                return;
            float delta = value - oldValue;
            string color = !comparing || Mathf.Abs(delta) < .001f ? "#DEE7F2" : delta > 0f ? "#5EE58C" : "#FF6874";
            string formatted = percent ? value.ToString("+0%;-0%;0%") : value.ToString("+0;-0;0");
            string difference = comparing && Mathf.Abs(delta) >= .001f
                ? $"  <size=12>({(percent ? delta.ToString("+0%;-0%") : delta.ToString("+0;-0"))})</size>"
                : string.Empty;
            text.Append($"\n<color={color}>{label}  <b>{formatted}</b>{difference}</color>");
        }

        private static string Pretty(EquipmentSlot slot) => System.Text.RegularExpressions.Regex.Replace(slot.ToString(), "([a-z])([A-Z0-9])", "$1 $2");
        private static string PrettyFlags(string value) => value.Replace(",", "  •");
        private static Color RarityTint(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Mythic => new Color(1f, .47f, .3f),
            ItemRarity.Epic => new Color(.72f, .55f, 1f),
            ItemRarity.Rare => new Color(.3f, .68f, 1f),
            ItemRarity.Uncommon => new Color(.4f, .88f, .55f),
            _ => new Color(.82f, .86f, .92f)
        };
        private static string RarityHex(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Mythic => "#FF784E",
            ItemRarity.Epic => "#B58CFF",
            ItemRarity.Rare => "#4BA3FF",
            ItemRarity.Uncommon => "#62D77B",
            _ => "#D6D9DE"
        };

        private static RectTransform Block(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }

        private static Image ImageChild(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = Block(name, parent, Color.white);
            Place(rect, min, max, 0f);
            return rect.GetComponent<Image>();
        }

        private static TextMeshProUGUI Text(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Vector2 min, Vector2 max)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(.88f, .93f, 1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            Place(text.rectTransform, min, max, 0f);
            return text;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max, float inset)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
