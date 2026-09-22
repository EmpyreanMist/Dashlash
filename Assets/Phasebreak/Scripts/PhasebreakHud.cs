using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PhasebreakHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerTargeting targeting;
        [SerializeField] private Targetable player;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerBuildSystem build;
        [SerializeField] private RiftDungeonController dungeon;

        [Header("Nameplates")]
        [SerializeField, Min(1f)] private float nameplateRange = 35f;

        private readonly Dictionary<Targetable, WorldNameplateUI> nameplates =
            new Dictionary<Targetable, WorldNameplateUI>();
        private readonly List<FloatingDamageView> floatingDamage = new List<FloatingDamageView>();
        private RectTransform canvasRect;
        private RectTransform nameplateLayer;
        private RectTransform combatTextLayer;
        private PlayerFrameUI playerFrame;
        private TargetFrameUI targetFrame;
        private AbilitySlotView[] abilitySlots;
        private RectTransform resourceFill;
        private TextMeshProUGUI resourceLabel;
        private TextMeshProUGUI autoAttackLabel;
        private RectTransform experienceFill;
        private TextMeshProUGUI experienceLabel;
        private TextMeshProUGUI levelUpBanner;
        private TextMeshProUGUI buildLabel;
        private RectTransform buildPanel;
        private InputAction debugHudAction;
        private bool debugHudVisible;
        private TextMeshProUGUI abilityTooltip;
        private TextMeshProUGUI lootToast;
        private TextMeshProUGUI abilityFeedback;
        private TextMeshProUGUI riftChain;
        private RectTransform riftChainPanel;
        private RectTransform dungeonPanel;
        private TextMeshProUGUI dungeonLabel;
        private TextMeshProUGUI interactionPrompt;
        private RectTransform dungeonCompletePanel;
        private TextMeshProUGUI dungeonCompleteLabel;
        private float levelUpBannerStartedAt = float.NegativeInfinity;
        private float lootToastStartedAt = float.NegativeInfinity;
        private float abilityFeedbackUntil = float.NegativeInfinity;
        private float nextRefreshAt;

        private static readonly Color PanelColor = PhasebreakUiTheme.Panel;
        private static readonly Color BarBackgroundColor = new Color(0.08f, 0.09f, 0.12f, 0.98f);
        private static readonly Color PlayerHealthColor = new Color(0.16f, 0.72f, 0.3f, 1f);
        private static readonly Color EnemyHealthColor = new Color(0.78f, 0.13f, 0.12f, 1f);
        private static readonly Color SelectedColor = new Color(1f, 0.58f, 0.12f, 1f);
        private static readonly Color ResourceColor = new Color(.34f, .46f, .62f, 1f);
        private static readonly Color NormalDamageColor = new Color(1f, 0.9f, 0.68f, 1f);
        private static readonly Color CriticalDamageColor = new Color(1f, 0.48f, 0.05f, 1f);

        private void Awake()
        {
            if (targeting == null)
                targeting = FindAnyObjectByType<PlayerTargeting>();
            if (worldCamera == null)
                worldCamera = Camera.main;
            if (combat == null)
                combat = FindAnyObjectByType<PlayerCombat>();
            if (progression == null)
                progression = FindAnyObjectByType<PlayerProgression>();
            if (build == null)
                build = FindAnyObjectByType<PlayerBuildSystem>();
            if (dungeon == null)
                dungeon = FindAnyObjectByType<RiftDungeonController>();
            if (player == null)
            {
                PlayerHealth health = FindAnyObjectByType<PlayerHealth>();
                if (health != null)
                    player = health.GetComponent<Targetable>();
            }
            BuildCanvas();
            debugHudAction = PhasebreakSettings.Button("debug.hud", "Debug Build Snapshot");
        }

        private void Start() => RefreshNameplates();

        private void OnEnable()
        {
            debugHudAction?.Enable();
            if (targeting != null)
                targeting.TargetChanged += HandleTargetChanged;
            CombatEvents.DamageNumberRequested += HandleDamageNumber;
            if (progression != null)
                progression.LevelGained += HandleLevelGained;
            if (build != null)
                build.LootAcquired += HandleLootAcquired;
            if (combat != null)
                combat.AbilityFailed += HandleAbilityFailed;
        }

        private void OnDisable()
        {
            debugHudAction?.Disable();
            if (targeting != null)
                targeting.TargetChanged -= HandleTargetChanged;
            CombatEvents.DamageNumberRequested -= HandleDamageNumber;
            if (progression != null)
                progression.LevelGained -= HandleLevelGained;
            if (build != null)
                build.LootAcquired -= HandleLootAcquired;
            if (combat != null)
                combat.AbilityFailed -= HandleAbilityFailed;
        }

        private void Update()
        {
            if (!GameplayInputFocus.GameplayInputBlocked && debugHudAction != null && debugHudAction.WasPressedThisFrame())
            {
                debugHudVisible = !debugHudVisible;
                if (buildPanel != null) buildPanel.gameObject.SetActive(debugHudVisible);
            }
            if (Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + 1f;
                RefreshNameplates();
            }

            Targetable currentTarget = targeting != null ? targeting.CurrentTarget : null;
            if (targetFrame != null && targetFrame.Target != currentTarget)
                targetFrame.Bind(currentTarget);

            foreach (WorldNameplateUI view in nameplates.Values)
                view.Refresh(worldCamera, player, currentTarget, canvasRect, nameplateRange);
            UpdateActionBar();
            if (riftChain != null && combat != null)
            {
                int count = combat.RiftChainCount;
                riftChainPanel.gameObject.SetActive(count >= 2);
                if (count >= 2)
                {
                    string recovery = combat.LastChainEnergyRestored > .05f ? $"+{combat.LastChainEnergyRestored:0} Energy" : "";
                    if (combat.LastChainChargeRestored) recovery += (recovery.Length > 0 ? "  /  " : "") + "+1 movement charge";
                    riftChain.text = $"RIFT CHAIN x{count}\n<size=14>{recovery}</size>";
                    riftChain.alpha = Mathf.Clamp01(combat.RiftChainRemaining / .6f);
                }
            }
            UpdateProgressionBar();
            UpdateLevelUpBanner();
            UpdateBuildDisplay();
            UpdateLootToast();
            UpdateDungeonDisplay();
            UpdateFloatingDamage();
            if (abilityFeedback != null)
                abilityFeedback.gameObject.SetActive(Time.unscaledTime < abilityFeedbackUntil);
            if (PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen)
                HideAbilityTooltip();
        }

        private void OnDestroy() { PhasebreakSettings.Unregister(debugHudAction); debugHudAction?.Dispose(); }

        private void HandleTargetChanged(Targetable currentTarget) => targetFrame?.Bind(currentTarget);

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("Phasebreak HUD Canvas", typeof(RectTransform),
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();
            nameplateLayer = CreateRect("Nameplates", canvasRect);
            Stretch(nameplateLayer);

            playerFrame = InstantiateFrame<PlayerFrameUI>("UI/PlayerFrame", "Player Frame", canvasRect,
                new Vector2(0f, 1f), new Vector2(30f, -30f));
            playerFrame?.Bind(player, combat, progression);
            targetFrame = InstantiateFrame<TargetFrameUI>("UI/TargetFrame", "Target Frame", canvasRect,
                new Vector2(0f, 1f), new Vector2(446f, -30f));
            targetFrame?.Bind(null);
            CreateActionBar();
            CreateProgressionDisplay();
            CreateBuildDisplay();
            CreateDungeonDisplay();

            abilityFeedback = AddText("Ability Feedback", canvasRect, 22f, TextAlignmentOptions.Center);
            abilityFeedback.fontStyle = FontStyles.Bold;
            abilityFeedback.color = new Color(1f, 0.38f, 0.26f, 1f);
            abilityFeedback.rectTransform.anchorMin = abilityFeedback.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            abilityFeedback.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            abilityFeedback.rectTransform.anchoredPosition = new Vector2(0f, -210f);
            abilityFeedback.rectTransform.sizeDelta = new Vector2(520f, 42f);
            abilityFeedback.gameObject.SetActive(false);
            riftChainPanel = CreateRect("Rift Chain Toast", canvasRect);
            riftChainPanel.anchorMin = riftChainPanel.anchorMax = new Vector2(.5f, 0f);
            riftChainPanel.anchoredPosition = new Vector2(0f, 200f);
            riftChainPanel.sizeDelta = new Vector2(330f, 58f);
            UnityEngine.UI.Image chainSurface = riftChainPanel.gameObject.AddComponent<UnityEngine.UI.Image>();
            PhasebreakUiTheme.StyleSurface(chainSurface, PhasebreakUiTheme.Panel);
            chainSurface.raycastTarget = false;
            riftChain = AddText("Rift Chain", riftChainPanel, 21f, TextAlignmentOptions.Center);
            riftChain.color = PhasebreakUiTheme.Accent;
            riftChain.fontStyle = FontStyles.Bold;
            riftChain.raycastTarget = false;
            riftChain.rectTransform.anchorMin = riftChain.rectTransform.anchorMax = new Vector2(.5f, .5f);
            riftChain.rectTransform.anchoredPosition = Vector2.zero;
            riftChain.rectTransform.sizeDelta = new Vector2(330f, 58f);
            riftChainPanel.gameObject.SetActive(false);

            combatTextLayer = CreateRect("Combat Text", canvasRect);
            Stretch(combatTextLayer);

            PhasebreakChatHub chat = GetComponent<PhasebreakChatHub>();
            if (chat == null) chat = gameObject.AddComponent<PhasebreakChatHub>();
            chat.Initialize(canvasRect);
            PhasebreakGameMenu gameMenu = GetComponent<PhasebreakGameMenu>();
            if (gameMenu == null) gameMenu = gameObject.AddComponent<PhasebreakGameMenu>();
            gameMenu.Initialize();
        }

        private void HandleAbilityFailed(string message)
        {
            if (abilityFeedback == null)
                return;
            abilityFeedback.text = message;
            abilityFeedbackUntil = Time.unscaledTime + 1.15f;
            abilityFeedback.gameObject.SetActive(true);
        }

        private void CreateDungeonDisplay()
        {
            dungeonPanel = CreateRect("Dungeon Status", canvasRect);
            dungeonPanel.anchorMin = dungeonPanel.anchorMax = dungeonPanel.pivot = new Vector2(1f, 1f);
            dungeonPanel.anchoredPosition = new Vector2(-28f, -28f);
            dungeonPanel.sizeDelta = new Vector2(390f, 126f);
            AddImage(dungeonPanel, PanelColor);

            dungeonLabel = AddText("Dungeon Status Label", dungeonPanel, 17f,
                TextAlignmentOptions.TopLeft);
            dungeonLabel.rectTransform.anchorMin = Vector2.zero;
            dungeonLabel.rectTransform.anchorMax = Vector2.one;
            dungeonLabel.rectTransform.offsetMin = new Vector2(16f, 12f);
            dungeonLabel.rectTransform.offsetMax = new Vector2(-16f, -12f);
            dungeonLabel.textWrappingMode = TextWrappingModes.Normal;

            interactionPrompt = AddText("Interaction Prompt", canvasRect, 23f,
                TextAlignmentOptions.Center);
            interactionPrompt.fontStyle = FontStyles.Bold;
            interactionPrompt.outlineWidth = 0.2f;
            interactionPrompt.outlineColor = new Color32(5, 8, 14, 255);
            interactionPrompt.rectTransform.anchorMin = interactionPrompt.rectTransform.anchorMax =
                new Vector2(0.5f, 0f);
            interactionPrompt.rectTransform.pivot = new Vector2(0.5f, 0f);
            interactionPrompt.rectTransform.anchoredPosition = new Vector2(0f, 172f);
            interactionPrompt.rectTransform.sizeDelta = new Vector2(720f, 48f);

            dungeonCompletePanel = CreateRect("Dungeon Complete", canvasRect);
            dungeonCompletePanel.anchorMin = dungeonCompletePanel.anchorMax = dungeonCompletePanel.pivot =
                new Vector2(0.5f, 0.5f);
            dungeonCompletePanel.sizeDelta = new Vector2(620f, 300f);
            AddImage(dungeonCompletePanel, new Color(0.018f, 0.028f, 0.05f, 0.97f));

            dungeonCompleteLabel = AddText("Dungeon Complete Label", dungeonCompletePanel, 25f,
                TextAlignmentOptions.Center);
            Stretch(dungeonCompleteLabel.rectTransform);
            dungeonCompleteLabel.rectTransform.offsetMin = new Vector2(28f, 24f);
            dungeonCompleteLabel.rectTransform.offsetMax = new Vector2(-28f, -24f);
            dungeonCompletePanel.gameObject.SetActive(false);
        }

        private void UpdateDungeonDisplay()
        {
            if (dungeon == null)
            {
                if (dungeonPanel != null)
                    dungeonPanel.gameObject.SetActive(false);
                return;
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.text = dungeon.InteractionPrompt;
                interactionPrompt.gameObject.SetActive(!string.IsNullOrEmpty(interactionPrompt.text));
            }

            bool showStatus = dungeon.IsInDungeon;
            if (dungeonPanel != null)
                dungeonPanel.gameObject.SetActive(showStatus);
            if (showStatus && dungeonLabel != null)
            {
                RiftDungeonEncounter encounter = dungeon.CurrentEncounter;
                int remaining = encounter != null ? encounter.RemainingEnemies : 0;
                string mechanic = string.Empty;
                if (encounter != null && encounter.IsBossEncounter && encounter.Enemies != null &&
                    encounter.Enemies.Length > 0 && encounter.Enemies[0] != null)
                {
                    RiftWardenBoss boss = encounter.Enemies[0].GetComponent<RiftWardenBoss>();
                    if (boss != null)
                        mechanic = $"\nWarden phase: <b>{boss.PhaseName}</b>";
                    if (boss != null && !string.IsNullOrEmpty(boss.CurrentMechanic))
                        mechanic += $"\n<color=#FF673D><b>{boss.CurrentMechanic}</b></color>";
                }
                dungeonLabel.text =
                    $"<b>{dungeon.DungeonName.ToUpperInvariant()}</b>  •  {dungeon.DifficultyName}\n" +
                    $"{dungeon.ObjectiveText}\n" +
                    $"Enemies remaining: {remaining}   Time: {FormatTime(dungeon.ElapsedTime)}{mechanic}";
            }

            bool showComplete = dungeon.State == RiftDungeonState.Completed;
            if (dungeonCompletePanel != null)
                dungeonCompletePanel.gameObject.SetActive(showComplete);
            if (showComplete && dungeonCompleteLabel != null)
            {
                dungeonCompleteLabel.text =
                    "<size=38><color=#F7A23B><b>RIFT CRYPT CLEARED</b></color></size>\n\n" +
                    $"Time  <b>{FormatTime(dungeon.ElapsedTime)}</b>\n" +
                    $"Enemies defeated  <b>{dungeon.KillCount}</b>\n" +
                    (dungeon.RewardGrantedThisRun
                        ? "Reward  <color=#B56CFF><b>Heart of the Rift Warden</b></color>\n\n"
                        : "Cache  <b>Previously claimed</b>\n\n") +
                    "Use the exit rift to return";
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private void CreateBuildDisplay()
        {
            RectTransform panel = CreateRect("Build Panel", canvasRect);
            buildPanel = panel;
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f);
            panel.anchoredPosition = new Vector2(-28f, -28f);
            panel.sizeDelta = new Vector2(360f, 190f);
            AddImage(panel, PanelColor);

            buildLabel = AddText("Build Summary", panel, 16f, TextAlignmentOptions.TopLeft);
            buildLabel.rectTransform.anchorMin = Vector2.zero;
            buildLabel.rectTransform.anchorMax = Vector2.one;
            buildLabel.rectTransform.offsetMin = new Vector2(16f, 12f);
            buildLabel.rectTransform.offsetMax = new Vector2(-16f, -12f);
            buildLabel.textWrappingMode = TextWrappingModes.Normal;
            buildPanel.gameObject.SetActive(false);

            lootToast = AddText("Loot Toast", canvasRect, 25f, TextAlignmentOptions.Center);
            lootToast.fontStyle = FontStyles.Bold;
            lootToast.outlineWidth = 0.2f;
            lootToast.outlineColor = new Color32(8, 12, 20, 255);
            lootToast.rectTransform.anchorMin = lootToast.rectTransform.anchorMax =
                new Vector2(0.5f, 0.5f);
            lootToast.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lootToast.rectTransform.anchoredPosition = new Vector2(0f, 155f);
            lootToast.rectTransform.sizeDelta = new Vector2(760f, 80f);
            lootToast.gameObject.SetActive(false);
        }

        private void UpdateBuildDisplay()
        {
            if (!debugHudVisible || buildLabel == null || build == null)
                return;

            buildLabel.text = "<b>BUILD SNAPSHOT</b>\n" + build.GetBuildSummary();
        }

        private void HandleLootAcquired(PhasebreakItemDefinition item)
        {
            if (lootToast == null)
                return;
            string color = item.rarity switch
            {
                ItemRarity.Epic => "#B56CFF",
                ItemRarity.Rare => "#4BA3FF",
                _ => "#E7E7E7"
            };
            lootToast.text = $"LOOT SECURED  <color={color}>{item.displayName}</color>";
            lootToastStartedAt = Time.unscaledTime;
            lootToast.gameObject.SetActive(true);
        }

        private void UpdateLootToast()
        {
            if (lootToast == null || !lootToast.gameObject.activeSelf)
                return;
            float t = Mathf.Clamp01((Time.unscaledTime - lootToastStartedAt) / 2.8f);
            lootToast.rectTransform.localScale = Vector3.one *
                (t < 0.12f ? Mathf.Lerp(0.72f, 1.08f, t / 0.12f) : Mathf.Lerp(1.08f, 1f, 0.25f));
            Color color = lootToast.color;
            color.a = t < 0.72f ? 1f : 1f - (t - 0.72f) / 0.28f;
            lootToast.color = color;
            if (t >= 1f)
                lootToast.gameObject.SetActive(false);
        }

        private void CreateActionBar()
        {
            RectTransform root = CreateRect("ActionBar", canvasRect);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, 25f);
            root.sizeDelta = new Vector2(414f, 119f);
            PhasebreakUiTheme.StyleSurface(AddImage(root, PhasebreakUiTheme.Window), PhasebreakUiTheme.Window);

            RectTransform energy = CreateRect("Energy", root);
            energy.anchorMin = energy.anchorMax = new Vector2(0.5f, 0f);
            energy.pivot = new Vector2(0.5f, 0f);
            energy.anchoredPosition = new Vector2(0f, 79f);
            energy.sizeDelta = new Vector2(388f, 12f);
            AddImage(energy, BarBackgroundColor);

            resourceFill = CreateRect("Fill", energy);
            resourceFill.anchorMin = Vector2.zero;
            resourceFill.anchorMax = Vector2.one;
            resourceFill.offsetMin = resourceFill.offsetMax = Vector2.zero;
            AddImage(resourceFill, ResourceColor);

            resourceLabel = AddText("Resource Label", energy, 11f, TextAlignmentOptions.Center);
            Stretch(resourceLabel.rectTransform);
            resourceLabel.fontStyle = FontStyles.Bold;

            autoAttackLabel = AddText("Auto Attack Status", root, 12f, TextAlignmentOptions.Center);
            autoAttackLabel.rectTransform.anchorMin = autoAttackLabel.rectTransform.anchorMax = new Vector2(.5f, 0f);
            autoAttackLabel.rectTransform.anchoredPosition = new Vector2(0f, 108f);
            autoAttackLabel.rectTransform.sizeDelta = new Vector2(388f, 20f);
            autoAttackLabel.raycastTarget = false;

            int abilityCount = combat != null ? combat.AbilityCount : 5;
            abilitySlots = new AbilitySlotView[abilityCount];
            for (int i = 0; i < abilitySlots.Length; i++)
            {
                RectTransform slot = CreateRect($"Ability {i + 1}", root);
                slot.anchorMin = slot.anchorMax = new Vector2(0.5f, 0f);
                slot.pivot = new Vector2(0.5f, 0f);
                slot.anchoredPosition = new Vector2((i - (abilitySlots.Length - 1) * 0.5f) * 72f, 10f);
                slot.sizeDelta = new Vector2(64f, 64f);
                UnityEngine.UI.Image panel = AddImage(slot, PhasebreakUiTheme.Raised);
                PhasebreakUiTheme.StyleSurface(panel, PhasebreakUiTheme.Raised);
                panel.raycastTarget = true;

                RectTransform iconRect = CreateRect("Ability Icon", slot);
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(3f, 3f);
                iconRect.offsetMax = new Vector2(-3f, -3f);
                UnityEngine.UI.Image abilityIcon = AddImage(iconRect, Color.white);
                abilityIcon.preserveAspect = true;

                TextMeshProUGUI name = AddText("Name", slot, 16f, TextAlignmentOptions.Center);
                name.fontStyle = FontStyles.Bold;
                name.fontSize = 11f;
                name.outlineWidth = .2f;
                name.outlineColor = new Color32(0, 0, 0, 255);
                name.rectTransform.anchorMin = Vector2.zero;
                name.rectTransform.anchorMax = new Vector2(1f, .3f);
                name.rectTransform.offsetMin = new Vector2(3f, 2f);
                name.rectTransform.offsetMax = new Vector2(-3f, 0f);
                name.gameObject.SetActive(false);

                TextMeshProUGUI key = AddText("Key", slot, 13f, TextAlignmentOptions.Center);
                key.fontStyle = FontStyles.Bold;
                key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0f, 1f);
                key.rectTransform.pivot = new Vector2(0f, 1f);
                key.rectTransform.anchoredPosition = new Vector2(5f, -5f);
                key.rectTransform.sizeDelta = new Vector2(18f, 18f);
                key.fontStyle = FontStyles.Bold;
                key.outlineWidth = .18f;
                key.outlineColor = Color.black;

                TextMeshProUGUI cost = AddText("Cost", slot, 10f, TextAlignmentOptions.BottomRight);
                cost.rectTransform.anchorMin = Vector2.zero;
                cost.rectTransform.anchorMax = Vector2.one;
                cost.rectTransform.offsetMin = new Vector2(4f, 4f);
                cost.rectTransform.offsetMax = new Vector2(-6f, -4f);
                cost.fontStyle = FontStyles.Bold;
                cost.outlineWidth = .18f;
                cost.outlineColor = Color.black;

                TextMeshProUGUI charges = AddText("Charges", slot, 12f, TextAlignmentOptions.TopRight);
                charges.rectTransform.anchorMin = Vector2.zero;
                charges.rectTransform.anchorMax = Vector2.one;
                charges.rectTransform.offsetMin = new Vector2(4f, 4f);
                charges.rectTransform.offsetMax = new Vector2(-6f, -4f);
                charges.fontStyle = FontStyles.Bold;
                charges.outlineWidth = .18f;
                charges.outlineColor = Color.black;

                RectTransform cooldown = CreateRect("Cooldown", slot);
                cooldown.anchorMin = Vector2.zero;
                cooldown.anchorMax = Vector2.one;
                cooldown.offsetMin = cooldown.offsetMax = Vector2.zero;
                AddImage(cooldown, new Color(.02f, .02f, .02f, .82f));

                TextMeshProUGUI cooldownText = AddText("Cooldown Text", slot, 25f,
                    TextAlignmentOptions.Center);
                Stretch(cooldownText.rectTransform);
                cooldownText.fontStyle = FontStyles.Bold;
                cooldownText.color = PhasebreakUiTheme.Text;
                cooldownText.outlineWidth = .2f;
                cooldownText.outlineColor = Color.black;
                key.transform.SetAsLastSibling();
                cost.transform.SetAsLastSibling();
                charges.transform.SetAsLastSibling();
                abilitySlots[i] = new AbilitySlotView(panel, abilityIcon, cooldown, name, key, cost, charges, cooldownText);
                int slotIndex = i;
                ItemSlotUI hover = slot.gameObject.AddComponent<ItemSlotUI>();
                hover.Configure(() => ShowAbilityTooltip(slotIndex), HideAbilityTooltip, null);
            }
            RectTransform tooltipSurface = CreateRect("Ability Tooltip Surface", canvasRect);
            tooltipSurface.anchorMin = tooltipSurface.anchorMax = new Vector2(.5f, 0f);
            tooltipSurface.pivot = new Vector2(.5f, 0f);
            tooltipSurface.anchoredPosition = new Vector2(0f, 138f);
            tooltipSurface.sizeDelta = new Vector2(400f, 80f);
            PhasebreakUiTheme.StyleSurface(AddImage(tooltipSurface, PhasebreakUiTheme.Window),
                PhasebreakUiTheme.Window);
            abilityTooltip = AddText("Ability Tooltip", tooltipSurface, 15f, TextAlignmentOptions.TopLeft);
            abilityTooltip.rectTransform.anchorMin = Vector2.zero;
            abilityTooltip.rectTransform.anchorMax = Vector2.one;
            abilityTooltip.rectTransform.offsetMin = new Vector2(14f, 10f);
            abilityTooltip.rectTransform.offsetMax = new Vector2(-14f, -10f);
            abilityTooltip.color = PhasebreakUiTheme.Text;
            tooltipSurface.gameObject.SetActive(false);
        }

        private void ShowAbilityTooltip(int index)
        {
            if (combat == null || abilityTooltip == null || PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen) return;
            AbilityState state = combat.GetAssignedAbilityState(index);
            abilityTooltip.text = $"<color=#C9A86C><b>{state.Name}</b></color>  <color=#AAA69B>[{state.Key}]</color>\n" +
                $"<color=#AAA69B>Energy {Mathf.CeilToInt(state.ResourceCost)}  •  Cooldown {state.CooldownDuration:0.#}s" +
                (state.MaximumCharges > 1 ? $"  •  Charges {state.MaximumCharges}" : string.Empty) + "</color>";
            abilityTooltip.transform.parent.gameObject.SetActive(true);
        }

        private void HideAbilityTooltip()
        {
            if (abilityTooltip != null) abilityTooltip.transform.parent.gameObject.SetActive(false);
        }

        private void UpdateActionBar()
        {
            if (combat == null || abilitySlots == null)
                return;

            SetFill(resourceFill, combat.ResourceFraction);
            if (autoAttackLabel != null)
            {
                autoAttackLabel.text = $"[{PhasebreakSettings.Display("combat.autoAttack")}] AUTO-ATTACK  {(combat.AutoAttackArmed ? "ARMED" : "OFF")}";
                autoAttackLabel.color = combat.AutoAttackArmed ? PhasebreakUiTheme.Accent : PhasebreakUiTheme.Text;
            }
            if (resourceLabel != null)
                resourceLabel.text = $"ENERGY  {Mathf.CeilToInt(combat.CurrentResource)}/{Mathf.CeilToInt(combat.MaximumResource)}";
            for (int i = 0; i < abilitySlots.Length; i++)
                abilitySlots[i].UpdateView(combat.GetAssignedAbilityState(i), combat.CurrentResource);
        }

        private void CreateProgressionDisplay()
        {
            RectTransform experience = CreateRect("Experience", canvasRect);
            experience.anchorMin = experience.anchorMax = new Vector2(0.5f, 0f);
            experience.pivot = new Vector2(0.5f, 0f);
            experience.anchoredPosition = new Vector2(0f, 8f);
            experience.sizeDelta = new Vector2(390f, 11f);
            AddImage(experience, BarBackgroundColor);

            experienceFill = CreateRect("Fill", experience);
            experienceFill.anchorMin = Vector2.zero;
            experienceFill.anchorMax = Vector2.one;
            experienceFill.offsetMin = experienceFill.offsetMax = Vector2.zero;
            AddImage(experienceFill, new Color(0.62f, 0.28f, 0.94f, 1f));

            experienceLabel = AddText("Experience Label", experience, 12f, TextAlignmentOptions.Center);
            Stretch(experienceLabel.rectTransform);
            experienceLabel.fontStyle = FontStyles.Bold;

            levelUpBanner = AddText("Level Up Banner", canvasRect, 42f, TextAlignmentOptions.Center);
            levelUpBanner.fontStyle = FontStyles.Bold;
            levelUpBanner.color = new Color(1f, 0.72f, 0.12f, 1f);
            levelUpBanner.outlineWidth = 0.25f;
            levelUpBanner.outlineColor = new Color32(35, 10, 0, 255);
            levelUpBanner.rectTransform.anchorMin = levelUpBanner.rectTransform.anchorMax =
                new Vector2(0.5f, 1f);
            levelUpBanner.rectTransform.pivot = new Vector2(0.5f, 1f);
            levelUpBanner.rectTransform.anchoredPosition = new Vector2(0f, -145f);
            levelUpBanner.rectTransform.sizeDelta = new Vector2(760f, 120f);
            levelUpBanner.gameObject.SetActive(false);
        }

        private void UpdateProgressionBar()
        {
            if (progression == null)
                return;

            SetFill(experienceFill, progression.ExperienceFraction);
            if (experienceLabel == null)
                return;
            experienceLabel.text = progression.IsMaximumLevel
                ? $"LEVEL {progression.Level}  •  MAX"
                : $"LEVEL {progression.Level}  •  XP {progression.CurrentExperience}/{progression.ExperienceToNextLevel}";
        }

        private void HandleLevelGained(int level, string reward)
        {
            if (levelUpBanner == null)
                return;
            levelUpBanner.text = $"LEVEL {level}!\n{reward.ToUpperInvariant()} UNLOCKED";
            levelUpBannerStartedAt = Time.unscaledTime;
            levelUpBanner.gameObject.SetActive(true);
        }

        private void UpdateLevelUpBanner()
        {
            if (levelUpBanner == null || !levelUpBanner.gameObject.activeSelf)
                return;

            const float duration = 2.4f;
            float t = Mathf.Clamp01((Time.unscaledTime - levelUpBannerStartedAt) / duration);
            float scale = t < 0.16f
                ? Mathf.Lerp(0.55f, 1.28f, 1f - Mathf.Pow(1f - t / 0.16f, 3f))
                : Mathf.Lerp(1.28f, 1f, Mathf.Clamp01((t - 0.16f) / 0.3f));
            levelUpBanner.rectTransform.localScale = Vector3.one * scale;
            float alpha = t < 0.72f ? 1f : 1f - (t - 0.72f) / 0.28f;
            levelUpBanner.color = new Color(1f, 0.72f, 0.12f, Mathf.Clamp01(alpha));
            if (t >= 1f)
                levelUpBanner.gameObject.SetActive(false);
        }

        private void HandleDamageNumber(DamageNumberEvent damageEvent)
        {
            if (combatTextLayer == null || canvasRect == null || worldCamera == null)
                return;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(damageEvent.WorldPosition);
            if (screenPoint.z <= 0f)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null,
                out Vector2 localPoint);

            RectTransform root = CreateRect(damageEvent.IsCritical ? "Critical Damage" : "Damage",
                combatTextLayer);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(320f, 90f);
            localPoint += new Vector2(Random.Range(-20f, 20f), Random.Range(4f, 20f));
            root.anchoredPosition = localPoint;

            TextMeshProUGUI label = root.gameObject.AddComponent<TextMeshProUGUI>();
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = damageEvent.IsCritical ? 3f : 1f;
            label.fontSize = damageEvent.IsCritical ? 50f : 34f;
            label.text = damageEvent.IsCritical ? $"CRIT!  {damageEvent.Amount}" : damageEvent.Amount.ToString();
            label.color = damageEvent.IsCritical ? CriticalDamageColor : NormalDamageColor;
            label.outlineWidth = damageEvent.IsCritical ? 0.28f : 0.18f;
            label.outlineColor = new Color32(20, 5, 0, 255);

            float drift = Random.Range(-34f, 34f);
            floatingDamage.Add(new FloatingDamageView(label, localPoint, drift,
                damageEvent.IsCritical, Time.unscaledTime));
        }

        private void UpdateFloatingDamage()
        {
            for (int i = floatingDamage.Count - 1; i >= 0; i--)
            {
                FloatingDamageView view = floatingDamage[i];
                if (!view.UpdateView(Time.unscaledTime))
                    continue;
                if (view.Label != null)
                    Destroy(view.Label.gameObject);
                floatingDamage.RemoveAt(i);
            }
        }

        private void RefreshNameplates()
        {
            List<Targetable> stale = new List<Targetable>(nameplates.Keys);
            foreach (Targetable targetable in Targetable.ActiveTargets)
            {
                if (targetable == null || !targetable.IsHostile)
                    continue;
                stale.Remove(targetable);
                if (!nameplates.ContainsKey(targetable))
                    nameplates.Add(targetable, CreateNameplate(targetable));
            }

            foreach (Targetable targetable in stale)
            {
                if (nameplates.TryGetValue(targetable, out WorldNameplateUI view) && view.Root != null)
                    Destroy(view.Root.gameObject);
                nameplates.Remove(targetable);
            }
        }

        private WorldNameplateUI CreateNameplate(Targetable targetable)
        {
            RectTransform root = CreateRect($"{targetable.DisplayName} Nameplate", nameplateLayer);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(170f, 43f);

            UnityEngine.UI.Image panel = AddImage(root, PhasebreakUiTheme.Window);
            PhasebreakUiTheme.StyleSurface(panel, PhasebreakUiTheme.Window);
            RectTransform bar = CreateRect("Health", root);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = new Vector2(5f, 5f);
            bar.offsetMax = new Vector2(-5f, 18f);
            PhasebreakUiTheme.StyleSurface(AddImage(bar, BarBackgroundColor), BarBackgroundColor, false);

            RectTransform fill = CreateRect("Fill", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            UnityEngine.UI.Image fillImage = AddImage(fill, EnemyHealthColor);

            TextMeshProUGUI label = AddText("Name", root, 14f, TextAlignmentOptions.Center);
            label.color = PhasebreakUiTheme.Text;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(6f, 18f);
            labelRect.offsetMax = new Vector2(-6f, -2f);

            WorldNameplateUI result = root.gameObject.AddComponent<WorldNameplateUI>();
            result.Configure(targetable, panel, fill, fillImage, label, PhasebreakUiTheme.Window, EnemyHealthColor,
                PhasebreakUiTheme.Active, EnemyHealthColor);
            return result;
        }

        private static T InstantiateFrame<T>(string resourcePath, string instanceName, RectTransform parent,
            Vector2 anchor, Vector2 position) where T : Component
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"Missing HUD frame prefab at Resources/{resourcePath}.");
                return null;
            }
            GameObject instance = Instantiate(prefab, parent, false);
            instance.name = instanceName;
            RectTransform root = instance.GetComponent<RectTransform>();
            if (root == null)
            {
                Debug.LogError($"HUD frame prefab {resourcePath} has no RectTransform.");
                Destroy(instance);
                return null;
            }
            root.anchorMin = root.anchorMax = anchor;
            root.pivot = anchor;
            root.anchoredPosition = position;
            RestyleUnitFrame(root, typeof(T) == typeof(TargetFrameUI));
            HudFrameDragHandle dragHandle = root.GetComponent<HudFrameDragHandle>();
            dragHandle?.SetAccentPalette(PhasebreakUiTheme.MetalEdge, PhasebreakUiTheme.Accent);
            dragHandle?.CaptureDefaultPosition();
            return instance.GetComponent<T>();
        }

        private static void RestyleUnitFrame(RectTransform root, bool hostile)
        {
            PhasebreakUiTheme.StyleSurface(root.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Window);
            root.GetComponentInChildren<ClassResourcePipsUI>(true)?.SetPalette(
                PhasebreakUiTheme.Accent, PhasebreakUiTheme.Raised);
            foreach (UnityEngine.UI.Image image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                switch (image.gameObject.name)
                {
                    case "Inner Panel":
                        PhasebreakUiTheme.StyleSurface(image, PhasebreakUiTheme.Panel, false);
                        break;
                    case "Obsidian Panel":
                    case "Portrait Well":
                        image.color = PhasebreakUiTheme.Window;
                        break;
                    case "Portrait Frame":
                    case "Level Badge":
                        PhasebreakUiTheme.StyleSurface(image, PhasebreakUiTheme.Raised);
                        break;
                    case "Portrait":
                        image.color = new Color(.18f, .17f, .16f, 1f);
                        break;
                    case "Arcane Edge":
                    case "Left Fang":
                    case "Right Fang":
                    case "Portrait Rune":
                        image.color = PhasebreakUiTheme.MetalEdge;
                        break;
                    case "Arcane Glow":
                        image.color = new Color(.4f, .35f, .27f, .12f);
                        break;
                    case "Health Bar":
                    case "Resource Bar":
                    case "Cast Bar":
                        PhasebreakUiTheme.StyleSurface(image, PhasebreakUiTheme.Raised, false);
                        break;
                    case "Health Fill":
                        image.color = hostile ? new Color(.57f, .23f, .22f, 1f) : new Color(.27f, .53f, .36f, 1f);
                        break;
                    case "Resource Fill":
                        image.color = new Color(.33f, .43f, .55f, 1f);
                        break;
                    case "Health Shine":
                    case "Resource Shine":
                        image.color = new Color(.8f, .78f, .73f, .13f);
                        break;
                }
            }
            foreach (TextMeshProUGUI label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.gameObject.name is "Player Name" or "Target Name" or "Role" or "Disposition" or "Rank")
                    label.color = label.gameObject.name is "Player Name" or "Target Name"
                        ? PhasebreakUiTheme.Text : PhasebreakUiTheme.MutedText;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.GetComponent<RectTransform>();
        }

        private static UnityEngine.UI.Image AddImage(RectTransform target, Color color)
        {
            UnityEngine.UI.Image image = target.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI AddText(string name, Transform parent, float fontSize,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static float GetHealthFraction(Targetable targetable) =>
            targetable == null || targetable.MaxHealth <= 0
                ? 0f
                : Mathf.Clamp01((float)targetable.CurrentHealth / targetable.MaxHealth);

        private static void SetFill(RectTransform fill, float amount)
        {
            if (fill == null)
                return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        private sealed class AbilitySlotView
        {
            private static readonly Color ReadyColor = PhasebreakUiTheme.Active;
            private static readonly Color BlockedColor = PhasebreakUiTheme.Panel;

            private readonly UnityEngine.UI.Image panel;
            private readonly UnityEngine.UI.Image icon;
            private readonly RectTransform cooldown;
            private readonly TextMeshProUGUI name;
            private readonly TextMeshProUGUI key;
            private readonly TextMeshProUGUI cost;
            private readonly TextMeshProUGUI charges;
            private readonly TextMeshProUGUI cooldownText;

            public AbilitySlotView(UnityEngine.UI.Image panel, UnityEngine.UI.Image icon, RectTransform cooldown,
                TextMeshProUGUI name, TextMeshProUGUI key, TextMeshProUGUI cost, TextMeshProUGUI charges,
                TextMeshProUGUI cooldownText)
            {
                this.panel = panel;
                this.icon = icon;
                this.cooldown = cooldown;
                this.name = name;
                this.key = key;
                this.cost = cost;
                this.charges = charges;
                this.cooldownText = cooldownText;
            }

            public void UpdateView(AbilityState state, float availableResource)
            {
                name.text = state.Name;
                icon.sprite = state.Icon;
                icon.color = state.Icon == null ? Color.clear : state.IsUsable ? Color.white : PhasebreakUiTheme.MutedText;
                key.text = state.Key;
                cost.text = state.ResourceCost > 0f ? Mathf.CeilToInt(state.ResourceCost).ToString() : string.Empty;
                cost.color = availableResource < state.ResourceCost
                    ? new Color(.9f, .49f, .39f, 1f) : PhasebreakUiTheme.Text;
                charges.text = state.MaximumCharges > 0
                    ? $"{state.Charges}/{state.MaximumCharges}" : string.Empty;
                charges.color = state.Charges == 0
                    ? new Color(.9f, .49f, .39f, 1f) : PhasebreakUiTheme.Text;
                panel.color = state.IsUsable ? ReadyColor : BlockedColor;

                bool coolingDown = state.CooldownRemaining > 0.01f;
                cooldown.gameObject.SetActive(coolingDown);
                cooldownText.gameObject.SetActive(coolingDown);
                if (!coolingDown)
                    return;

                float fraction = state.CooldownDuration <= 0f
                    ? 0f
                    : Mathf.Clamp01(state.CooldownRemaining / state.CooldownDuration);
                cooldown.anchorMax = new Vector2(1f, fraction);
                cooldown.offsetMin = cooldown.offsetMax = Vector2.zero;
                cooldownText.text = state.CooldownRemaining >= 1f
                    ? Mathf.CeilToInt(state.CooldownRemaining).ToString()
                    : state.CooldownRemaining.ToString("0.0");
            }
        }

        private sealed class FloatingDamageView
        {
            private const float NormalDuration = 0.9f;
            private const float CriticalDuration = 1.15f;

            public readonly TextMeshProUGUI Label;
            private readonly Vector2 startPosition;
            private readonly float drift;
            private readonly bool critical;
            private readonly float startedAt;
            private readonly Color baseColor;

            public FloatingDamageView(TextMeshProUGUI label, Vector2 startPosition, float drift,
                bool critical, float startedAt)
            {
                Label = label;
                this.startPosition = startPosition;
                this.drift = drift;
                this.critical = critical;
                this.startedAt = startedAt;
                baseColor = label.color;
                label.rectTransform.localScale = Vector3.one * (critical ? 0.55f : 0.75f);
            }

            public bool UpdateView(float now)
            {
                float duration = critical ? CriticalDuration : NormalDuration;
                float t = Mathf.Clamp01((now - startedAt) / duration);
                float easedRise = 1f - Mathf.Pow(1f - t, 2.4f);
                float rise = critical ? 175f : 105f;
                float shake = critical ? Mathf.Sin(t * 52f) * 8f * (1f - t) : 0f;
                Label.rectTransform.anchoredPosition = startPosition +
                    new Vector2(drift * t + shake, rise * easedRise);

                float scale;
                if (critical)
                {
                    if (t < 0.13f)
                        scale = Mathf.Lerp(0.55f, 2.05f, 1f - Mathf.Pow(1f - t / 0.13f, 3f));
                    else if (t < 0.34f)
                        scale = Mathf.Lerp(2.05f, 1.12f, (t - 0.13f) / 0.21f);
                    else
                        scale = Mathf.Lerp(1.12f, 0.95f, (t - 0.34f) / 0.66f);
                }
                else
                {
                    scale = t < 0.16f
                        ? Mathf.Lerp(0.75f, 1.25f, t / 0.16f)
                        : Mathf.Lerp(1.25f, 0.92f, (t - 0.16f) / 0.84f);
                }
                Label.rectTransform.localScale = Vector3.one * scale;

                float alpha = t < 0.62f ? 1f : 1f - (t - 0.62f) / 0.38f;
                Label.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));
                return t >= 1f;
            }
        }

    }
}
