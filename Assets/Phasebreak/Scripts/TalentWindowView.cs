using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Phasebreak.Gameplay
{
    public sealed class TalentWindowView : MonoBehaviour
    {
        private readonly Dictionary<Specialization, UnityEngine.UI.Image> tabSurfaces = new();
        private readonly Dictionary<Specialization, TextMeshProUGUI> tabStates = new();
        private readonly List<GameObject> generated = new();
        private TalentSystem talents;
        private PlayerBuildSystem build;
        private PlayerProgression progression;
        private RectTransform treeArea;
        private RectTransform connectionLayer;
        private RectTransform nodeLayer;
        private TextMeshProUGUI pointsLabel;
        private TextMeshProUGUI activeSpecializationLabel;
        private UnityEngine.UI.Button activateButton;
        private TextMeshProUGUI activateLabel;
        private TextMeshProUGUI identityLabel;
        private TextMeshProUGUI detailsTitle;
        private TextMeshProUGUI detailsBody;
        private TextMeshProUGUI feedback;
        private UnityEngine.UI.Image detailsIcon;
        private UnityEngine.UI.Button purchaseButton;
        private TextMeshProUGUI purchaseLabel;
        private TalentNodeDefinition selected;
        private Specialization viewed = Specialization.Berserker;

        private static readonly Color Surface = PhasebreakUiTheme.Window;
        private static readonly Color SurfaceRaised = PhasebreakUiTheme.Raised;
        private static readonly Color Purchased = PhasebreakUiTheme.Accent;
        private static readonly Color Purple = new(.45f, .32f, .52f, 1f);
        private static readonly Color Text = PhasebreakUiTheme.Text;
        private static readonly Color Muted = PhasebreakUiTheme.MutedText;

        public void Initialize(RectTransform root)
        {
            talents = FindAnyObjectByType<TalentSystem>();
            if (talents == null)
            {
                PlayerBuildSystem playerBuild = FindAnyObjectByType<PlayerBuildSystem>();
                if (playerBuild != null) talents = playerBuild.GetComponent<TalentSystem>() ?? playerBuild.gameObject.AddComponent<TalentSystem>();
            }
            build = FindAnyObjectByType<PlayerBuildSystem>();
            progression = FindAnyObjectByType<PlayerProgression>();
            if (build != null && build.Specialization != Specialization.Unchosen) viewed = build.Specialization;
            Build(root);
            if (talents != null) talents.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (talents != null) talents.Changed -= Refresh;
        }

        public void Refresh()
        {
            if (treeArea == null) return;
            int spent = talents != null ? talents.GetSpentPoints(viewed) : 0;
            int available = talents != null ? talents.GetAvailablePoints(viewed) : 0;
            pointsLabel.text = talents == null ? "TALENT DATA UNAVAILABLE" :
                $"{viewed.ToString().ToUpperInvariant()} POINTS     AVAILABLE  <b>{available}</b>     SPENT  <b>{spent}</b> / {talents.TotalPoints}";
            Specialization active = build != null ? build.Specialization : Specialization.Unchosen;
            activeSpecializationLabel.text = $"ACTIVE SPECIALIZATION  <b>{active.ToString().ToUpperInvariant()}</b>";
            foreach (KeyValuePair<Specialization, UnityEngine.UI.Image> pair in tabSurfaces)
            {
                bool isActive = pair.Key == active;
                pair.Value.color = isActive ? PhasebreakUiTheme.Active : pair.Key == viewed ? PhasebreakUiTheme.Raised : PhasebreakUiTheme.Panel;
                tabStates[pair.Key].text = isActive ? "ACTIVE" : string.Empty;
                tabStates[pair.Key].color = isActive ? PhasebreakUiTheme.Text : PhasebreakUiTheme.MutedText;
            }
            activateButton.transform.SetParent(tabSurfaces[viewed].transform, false);
            Place(activateButton.GetComponent<RectTransform>(), new Vector2(.77f, .16f),
                new Vector2(.98f, .84f));
            activateButton.gameObject.SetActive(viewed != active);
            activateButton.interactable = build != null;
            activateLabel.text = "ACTIVATE";
            TalentTreeDefinition tree = talents?.GetTree(viewed);
            identityLabel.text = tree == null ? "Talent data is not installed." : tree.identity;
            if (tree != null && (selected == null || !tree.nodes.Contains(selected)))
                selected = tree.nodes.FirstOrDefault(node => node != null);
            RebuildTree(tree);
            RefreshDetails();
        }

        private void Build(RectTransform root)
        {
            RectTransform tabs = Block("Specialization Navigation", root, Surface);
            Place(tabs, new Vector2(.025f, .79f), new Vector2(.975f, .885f));
            activeSpecializationLabel = AddText("Active Specialization", root, string.Empty, 14f,
                TextAlignmentOptions.MidlineLeft, new Vector2(.03f, .895f), new Vector2(.68f, .96f), PhasebreakUiTheme.Text);
            Specialization[] specs = { Specialization.Berserker, Specialization.Bulwark, Specialization.Riftblade };
            for (int i = 0; i < specs.Length; i++)
            {
                Specialization spec = specs[i];
                RectTransform tab = Block(spec + " Tab", tabs, SurfaceRaised);
                Place(tab, new Vector2(.012f + i * .332f, .12f), new Vector2(.322f + i * .332f, .88f));
                tabSurfaces[spec] = tab.GetComponent<UnityEngine.UI.Image>();
                PhasebreakUiTheme.StyleSurface(tabSurfaces[spec], PhasebreakUiTheme.Panel);
                UnityEngine.UI.Button button = tab.gameObject.AddComponent<UnityEngine.UI.Button>();
                button.onClick.AddListener(() => { viewed = spec; selected = null; feedback.text = string.Empty; Refresh(); });
                UnityEngine.UI.Image icon = Image("Icon", tab, new Vector2(.035f, .16f), new Vector2(.23f, .84f));
                icon.sprite = PhasebreakIconCatalog.Current?.GetSpecializationIcon(spec);
                icon.preserveAspect = true;
                AddText("Label", tab, spec.ToString().ToUpperInvariant(), 13f, TextAlignmentOptions.MidlineLeft,
                    new Vector2(.27f, .05f), new Vector2(.76f, .95f), PhasebreakUiTheme.Text);
                tabStates[spec] = AddText("State", tab, string.Empty, 10f, TextAlignmentOptions.Center,
                    new Vector2(.76f, .1f), new Vector2(.98f, .9f), PhasebreakUiTheme.MutedText);
            }
            activateButton = Button("Activate Specialization", tabSurfaces[viewed].transform,
                "ACTIVATE", new Vector2(.77f, .16f), new Vector2(.98f, .84f),
                ActivateViewed, out activateLabel);
            PhasebreakUiTheme.StyleButton(activateButton);

            RectTransform center = Block("Talent Constellation", root, Surface);
            Place(center, new Vector2(.025f, .105f), new Vector2(.705f, .775f));
            PhasebreakUiTheme.StyleSurface(center.GetComponent<UnityEngine.UI.Image>(), Surface);
            identityLabel = AddText("Tree Identity", center, string.Empty, 14f, TextAlignmentOptions.TopLeft, new Vector2(.025f, .89f), new Vector2(.975f, .98f), Muted);
            treeArea = Rect("Tree Area", center);
            Place(treeArea, new Vector2(.025f, .035f), new Vector2(.975f, .88f));
            connectionLayer = Rect("Connections", treeArea); Stretch(connectionLayer);
            nodeLayer = Rect("Nodes", treeArea); Stretch(nodeLayer);
            AddText("State Legend", center,
                "<color=#77746E>LOCKED</color>    <color=#A98AB8>AVAILABLE</color>    <color=#C9A86C>PURCHASED</color>    <color=#E4C078>MAX RANK</color>",
                12f, TextAlignmentOptions.Center, new Vector2(.18f, .005f), new Vector2(.82f, .035f), Muted);

            RectTransform details = Block("Selected Talent Details", root, PhasebreakUiTheme.Window);
            Place(details, new Vector2(.72f, .105f), new Vector2(.975f, .775f));
            PhasebreakUiTheme.StyleSurface(details.GetComponent<UnityEngine.UI.Image>(),
                PhasebreakUiTheme.Window);
            RectTransform iconFrame = Block("Selected Icon Frame", details, Purple);
            Place(iconFrame, new Vector2(.08f, .73f), new Vector2(.36f, .9f));
            detailsIcon = Image("Selected Icon", iconFrame, new Vector2(.06f, .06f), new Vector2(.94f, .94f));
            detailsIcon.preserveAspect = true;
            detailsTitle = AddText("Selected Name", details, "SELECT A TALENT", 20f, TextAlignmentOptions.TopLeft, new Vector2(.41f, .72f), new Vector2(.94f, .91f), Text);
            detailsBody = AddText("Selected Details", details, "Choose a node in the constellation to inspect its ranks and requirements.", 15f, TextAlignmentOptions.TopLeft, new Vector2(.07f, .25f), new Vector2(.93f, .69f), Muted);
            purchaseButton = Button("Purchase", details, "INVEST POINT", new Vector2(.1f, .12f), new Vector2(.9f, .21f), BuySelected, out purchaseLabel);
            feedback = AddText("Feedback", details, string.Empty, 11f, TextAlignmentOptions.Center, new Vector2(.06f, .035f), new Vector2(.94f, .105f), Muted);

            pointsLabel = AddText("Points", root, string.Empty, 13f, TextAlignmentOptions.Left, new Vector2(.03f, .025f), new Vector2(.56f, .085f), Text);
            Button("Reset", root, "RESET CURRENT TREE", new Vector2(.76f, .025f), new Vector2(.965f, .085f), ResetViewed, out _);
        }

        private void RebuildTree(TalentTreeDefinition tree)
        {
            foreach (GameObject item in generated) if (item != null) Destroy(item);
            generated.Clear();
            if (tree == null) return;
            Dictionary<string, TalentNodeDefinition> byId = tree.nodes.Where(n => n != null).ToDictionary(n => n.id, StringComparer.OrdinalIgnoreCase);
            foreach (TalentNodeDefinition node in tree.nodes)
                foreach (string prerequisiteId in node.prerequisiteIds ?? Array.Empty<string>())
                    if (byId.TryGetValue(prerequisiteId, out TalentNodeDefinition prerequisite)) CreateConnection(prerequisite, node);
            foreach (TalentNodeDefinition node in tree.nodes) CreateNode(node);
        }

        private void CreateConnection(TalentNodeDefinition from, TalentNodeDefinition to)
        {
            RectTransform line = Block("Connection " + from.id + " to " + to.id, connectionLayer,
                talents.GetRank(from.id) > 0 ? new Color(.64f, .48f, .27f, .85f) : new Color(.22f, .21f, .2f, .8f));
            generated.Add(line.gameObject);
            Vector2 start = ToLocal(from.presentationPosition);
            Vector2 end = ToLocal(to.presentationPosition);
            Vector2 delta = end - start;
            line.anchorMin = line.anchorMax = new Vector2(.5f, .5f);
            line.pivot = new Vector2(0f, .5f);
            line.anchoredPosition = start;
            line.sizeDelta = new Vector2(delta.magnitude, 4f);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            line.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }

        private Vector2 ToLocal(Vector2 normalized)
        {
            Rect rect = treeArea.rect;
            return new Vector2((normalized.x - .5f) * rect.width, (normalized.y - .5f) * rect.height);
        }

        private void CreateNode(TalentNodeDefinition node)
        {
            int rank = talents.GetRank(node.id);
            TalentPurchaseResult state = talents.CanPurchase(node);
            bool maxed = rank >= node.maximumRank;
            Color border = maxed ? new Color(.88f, .68f, .34f, 1f) : rank > 0 ? Purchased : state == TalentPurchaseResult.Purchased ? Purple : PhasebreakUiTheme.MetalEdge;
            if (selected == node) border = PhasebreakUiTheme.Text;
            RectTransform frame = Block(node.displayName, nodeLayer, border);
            generated.Add(frame.gameObject);
            frame.anchorMin = frame.anchorMax = node.presentationPosition;
            frame.pivot = new Vector2(.5f, .5f);
            frame.sizeDelta = node.nodeType == TalentNodeType.Keystone ? new Vector2(94f, 94f) : new Vector2(82f, 82f);
            RectTransform surface = Block("Surface", frame, SurfaceRaised);
            Stretch(surface, 4f);
            UnityEngine.UI.Image icon = Image("Icon", surface, new Vector2(.08f, .08f), new Vector2(.92f, .92f));
            icon.sprite = node.icon;
            icon.preserveAspect = true;
            icon.color = state == TalentPurchaseResult.Purchased || rank > 0 ? Color.white : new Color(.65f, .68f, .73f, 1f);
            AddText("Rank", frame, $"{rank}/{node.maximumRank}", 11f, TextAlignmentOptions.Center, new Vector2(.5f, -.12f), new Vector2(1.12f, .18f), Text);
            TalentNodePointer pointer = frame.gameObject.AddComponent<TalentNodePointer>();
            pointer.Initialize(() => { selected = node; feedback.text = string.Empty; Refresh(); }, () => ShowHover(node), ClearHover);
        }

        private void RefreshDetails()
        {
            if (selected == null)
            {
                detailsIcon.sprite = null;
                detailsIcon.enabled = false;
                detailsTitle.text = "SELECT A TALENT";
                detailsBody.text = "Choose a node in the constellation to inspect its ranks and requirements.";
                purchaseButton.interactable = false;
                purchaseLabel.text = "INVEST POINT";
                return;
            }
            int rank = talents.GetRank(selected.id);
            TalentPurchaseResult state = talents.CanPurchase(selected);
            detailsIcon.sprite = selected.icon;
            detailsIcon.enabled = selected.icon != null;
            detailsTitle.text = selected.displayName.ToUpperInvariant();
            string requirements = RequirementText(selected, state);
            string next = rank < selected.maximumRank ? $"\n\n<color=#C9A86C><b>NEXT RANK</b></color>\n{selected.RankEffect(rank + 1)}" : string.Empty;
            detailsBody.text = $"<color=#A984FF>{selected.nodeType}</color>   RANK {rank}/{selected.maximumRank}\n\n{selected.description}\n\n<b>CURRENT EFFECT</b>\n{(rank > 0 ? selected.RankEffect(rank) : "Not active")}{next}\n\n<color=#8998AA>{requirements}</color>";
            purchaseButton.interactable = state == TalentPurchaseResult.Purchased;
            purchaseLabel.text = rank >= selected.maximumRank ? "MAXIMUM RANK" : $"INVEST {selected.pointCost} POINT{(selected.pointCost == 1 ? string.Empty : "S")}";
        }

        private void BuySelected()
        {
            if (selected == null) return;
            TalentPurchaseResult result = talents.Purchase(selected);
            feedback.text = result == TalentPurchaseResult.Purchased ? "<color=#C9A86C>Talent awakened.</color>" : RequirementText(selected, result);
            Refresh();
        }

        private void ActivateViewed()
        {
            if (build == null) return;
            SpecializationChangeResult result = build.SetSpecialization(viewed);
            Refresh();
            feedback.text = result switch
            {
                SpecializationChangeResult.Changed => $"{viewed} is now active. Its saved talents are in effect.",
                SpecializationChangeResult.AbilityInProgress => "Finish the current ability before switching specialization.",
                SpecializationChangeResult.Defeated => "Recover before switching specialization.",
                SpecializationChangeResult.AlreadyActive => $"{viewed} is already active.",
                _ => "This specialization cannot be activated."
            };
        }

        private void ResetViewed()
        {
            talents?.ResetTree(viewed);
            feedback.text = $"{viewed} allocation reset. Points returned.";
        }

        private void ShowHover(TalentNodeDefinition node)
        {
            if (selected == null) { detailsTitle.text = node.displayName.ToUpperInvariant(); detailsBody.text = $"RANK {talents.GetRank(node.id)}/{node.maximumRank}\n\n{node.description}\n\n{RequirementText(node, talents.CanPurchase(node))}"; detailsIcon.sprite = node.icon; detailsIcon.enabled = node.icon != null; }
        }

        private void ClearHover() { if (selected == null) RefreshDetails(); }

        private string RequirementText(TalentNodeDefinition node, TalentPurchaseResult result)
        {
            if (result == TalentPurchaseResult.Purchased) return "Available — all requirements met.";
            if (result == TalentPurchaseResult.MaximumRank) return "Maximum rank reached.";
            if (result == TalentPurchaseResult.InsufficientPoints) return $"Locked — requires {node.pointCost} available talent point(s).";
            if (result == TalentPurchaseResult.LevelRequired) return $"Locked — requires player level {node.requiredPlayerLevel}.";
            if (result == TalentPurchaseResult.WrongSpecialization) return $"Locked — active specialization is {build.Specialization}.";
            if (result == TalentPurchaseResult.ChoiceConflict) return "Locked — another talent in this choice group is active.";
            if (result == TalentPurchaseResult.PrerequisiteRequired)
            {
                string names = string.Join(", ", (node.prerequisiteIds ?? Array.Empty<string>()).Where(id => talents.GetRank(id) <= 0).Select(id => talents.GetNode(id)?.displayName ?? id));
                return $"Locked — requires {names}.";
            }
            return "Locked.";
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>();
        }

        private static RectTransform Block(string name, Transform parent, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent, false);
            go.GetComponent<UnityEngine.UI.Image>().color = color; return go.GetComponent<RectTransform>();
        }

        private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 min, Vector2 max)
        {
            RectTransform rect = Block(name, parent, Color.white); Place(rect, min, max); UnityEngine.UI.Image image = rect.GetComponent<UnityEngine.UI.Image>(); image.raycastTarget = false; return image;
        }

        private static TextMeshProUGUI AddText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>(); text.text = value; text.fontSize = size; text.alignment = alignment; text.color = color; text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false; Place(text.rectTransform, min, max); return text;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, string value, Vector2 min, Vector2 max, Action click, out TextMeshProUGUI label)
        {
            RectTransform rect = Block(name, parent, SurfaceRaised); Place(rect, min, max);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(() => click?.Invoke());
            label = AddText("Label", rect, value, 11.5f, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Text); return button;
        }

        private static void Place(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        private static void Stretch(RectTransform rect, float inset = 0f) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(inset, inset); rect.offsetMax = new Vector2(-inset, -inset); }
    }

    public sealed class TalentNodePointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private Action click;
        private Action enter;
        private Action exit;
        public void Initialize(Action onClick, Action onEnter, Action onExit) { click = onClick; enter = onEnter; exit = onExit; }
        public void OnPointerEnter(PointerEventData eventData) => enter?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => exit?.Invoke();
        public void OnPointerClick(PointerEventData eventData) => click?.Invoke();
    }
}
