#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(600), DisallowMultipleComponent]
    public sealed class PhasebreakDeveloperTools : MonoBehaviour
    {
        private readonly struct ActionSpec
        {
            public readonly string Label;
            public readonly Action Action;
            public ActionSpec(string label, Action action) { Label = label; Action = action; }
        }

        private enum PlacementMode { None, SpawnEnemy, SpawnDummy, TeleportPlayer, TeleportTarget }

        private static readonly string[] TabNames =
        {
            "QUICK", "PLAYER", "COMBAT", "TARGET", "SPAWN", "PROGRESSION",
            "ITEMS", "WORLD", "DUNGEON", "VISUAL", "SAVE"
        };

        private readonly Dictionary<string, GameObject> tabPages = new(StringComparer.Ordinal);
        private readonly List<Action> liveRefresh = new();
        private InputAction toggleAction;
        private RectTransform root;
        private RectTransform window;
        private TextMeshProUGUI statusStrip;
        private TextMeshProUGUI feedback;
        private RectTransform modal;
        private TextMeshProUGUI modalHeading;
        private TextMeshProUGUI modalBody;
        private UnityEngine.UI.Button modalConfirm;
        private TextMeshProUGUI modalConfirmLabel;
        private RectTransform placementOverlay;
        private TextMeshProUGUI placementLabel;
        private bool open;
        private float nextRefresh;
        private Action pendingConfirmation;
        private PlacementMode placementMode;
        private int placementStartedFrame;
        private int pendingDummyHealth;
        private Targetable pendingTarget;
        private Vector3 bookmark;
        private Quaternion bookmarkRotation;
        private bool hasBookmark;

        private PlayerHealth health;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PhasebreakPlayerMovement movement;
        private PlayerTargeting targeting;
        private PlayerBuildSystem build;
        private TalentSystem talents;
        private QuestJournal journal;
        private PhasebreakFollowCamera followCamera;
        private PhasebreakChatHub chat;

        private int selectedAbility;
        private TMP_InputField cooldownOverrideInput;
        private TMP_InputField costOverrideInput;
        private TMP_InputField flickerCountOverrideInput;
        private TMP_InputField flickerContinuingCostOverrideInput;
        private TMP_InputField flickerIntervalOverrideInput;
        private int selectedItem;
        private EnemyRole spawnRole = EnemyRole.Brute;
        private EnemyRank spawnRank = EnemyRank.Normal;
        private int spawnCount = 1;
        private float spawnRadius = 6f;
        private float spawnHealthMultiplier = 1f;
        private float spawnScaleMultiplier = 1f;
        private float spawnDamageMultiplier = 1f;

        private void Awake() => toggleAction = PhasebreakSettings.Button("developer.tools", "Developer Tools");

        private void OnEnable() => toggleAction?.Enable();

        private void OnDisable()
        {
            if (open) Close();
            toggleAction?.Disable();
        }

        private void OnDestroy()
        {
            PhasebreakSettings.Unregister(toggleAction);
            toggleAction?.Dispose();
        }

        public void Initialize()
        {
            if (root != null) return;
            FindSystems();
            BuildUi();
            root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (root == null) return;
            if (!open)
            {
                if (toggleAction.WasPressedThisFrame() && !GameplayInputFocus.GameplayInputBlocked &&
                    !PhasebreakInventoryHud.IsMajorMenuOpen && !WorldQuestHud.IsWorldMenuOpen)
                    Open();
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (modal.gameObject.activeSelf)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HideModal();
                return;
            }
            if (placementMode != PlacementMode.None)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) CancelPlacement();
                else HandlePlacementClick();
                return;
            }
            if (toggleAction.WasPressedThisFrame() || keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .1f;
                RefreshLiveViews();
            }
        }

        private void FindSystems()
        {
            health ??= FindAnyObjectByType<PlayerHealth>();
            combat ??= FindAnyObjectByType<PlayerCombat>();
            progression ??= FindAnyObjectByType<PlayerProgression>();
            movement ??= FindAnyObjectByType<PhasebreakPlayerMovement>();
            targeting ??= FindAnyObjectByType<PlayerTargeting>();
            build ??= FindAnyObjectByType<PlayerBuildSystem>();
            talents ??= FindAnyObjectByType<TalentSystem>();
            journal ??= FindAnyObjectByType<QuestJournal>();
            followCamera ??= FindAnyObjectByType<PhasebreakFollowCamera>();
            chat ??= GetComponent<PhasebreakChatHub>() ?? FindAnyObjectByType<PhasebreakChatHub>();
        }

        private void Open()
        {
            FindSystems();
            open = true;
            root.gameObject.SetActive(true);
            window.gameObject.SetActive(true);
            placementOverlay.gameObject.SetActive(false);
            modal.gameObject.SetActive(false);
            GameplayInputFocus.SetMenuFocused(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshLiveViews();
        }

        private void Close()
        {
            placementMode = PlacementMode.None;
            pendingConfirmation = null;
            open = false;
            root.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            GameplayInputFocus.SetMenuFocused(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RefreshLiveViews()
        {
            FindSystems();
            string god = health != null && health.DebugGodMode ? "ON" : "OFF";
            string hp = health != null ? $"{health.CurrentHealth}/{health.MaxHealth}" : "--";
            string energy = combat != null ? $"{combat.CurrentResource:0.#}/{combat.MaximumResource:0.#}" : "--";
            string level = progression != null ? progression.Level.ToString(CultureInfo.InvariantCulture) : "--";
            string spec = build != null ? build.Specialization.ToString().ToUpperInvariant() : "--";
            string target = targeting != null && targeting.CurrentTarget != null ? targeting.CurrentTarget.DisplayName : "NONE";
            statusStrip.text = $"GOD: {god}  |  HP {hp}  |  ENERGY {energy}  |  LVL {level}  |  {spec}  |  TARGET: {target}  |  {Time.timeScale:0.##}x";
            foreach (Action refresh in liveRefresh) refresh();
        }

        private void BuildUi()
        {
            GameObject canvasObject = new("Phasebreak Developer Tools", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 180;
            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            float uiScale = Mathf.Clamp(PhasebreakSettings.GetFloat("uiScale", 1f), .7f, 1.5f);
            scaler.referenceResolution = new Vector2(1920f / uiScale, 1080f / uiScale);
            scaler.matchWidthOrHeight = .5f;
            root = canvasObject.GetComponent<RectTransform>();

            RectTransform backdrop = Rect("Backdrop", root, new Color(.008f, .008f, .01f, .78f), true);
            Stretch(backdrop, 0f);
            window = Rect("Developer Tools Window", root, PhasebreakUiTheme.Window, true);
            PhasebreakUiTheme.StyleSurface(window.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Window);
            Anchors(window, new Vector2(.035f, .04f), new Vector2(.965f, .96f));

            Label("Title", window, "DEVELOPER TOOLS", 30f, PhasebreakUiTheme.Text,
                new Vector2(.025f, .925f), new Vector2(.45f, .985f), TextAlignmentOptions.Left, FontStyles.Bold);
            Label("Subtitle", window, "LOCAL DEBUG / DEVELOPMENT BUILD", 14f, PhasebreakUiTheme.Accent,
                new Vector2(.46f, .935f), new Vector2(.78f, .978f), TextAlignmentOptions.Left, FontStyles.Bold);
            Button("Close", window, "CLOSE  [ESC]", new Vector2(.82f, .934f), new Vector2(.975f, .982f), Close);

            RectTransform strip = Rect("Live Status", window, PhasebreakUiTheme.Panel, true);
            PhasebreakUiTheme.StyleSurface(strip.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Panel);
            Anchors(strip, new Vector2(.025f, .865f), new Vector2(.975f, .922f));
            statusStrip = Label("Status", strip, string.Empty, 15f, PhasebreakUiTheme.Text,
                new Vector2(.012f, .05f), new Vector2(.988f, .95f), TextAlignmentOptions.Left, FontStyles.Bold);

            RectTransform nav = Rect("Navigation", window, PhasebreakUiTheme.Panel, true);
            PhasebreakUiTheme.StyleSurface(nav.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Panel);
            Anchors(nav, new Vector2(.025f, .085f), new Vector2(.145f, .852f));
            UnityEngine.UI.VerticalLayoutGroup navLayout = nav.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            navLayout.padding = new RectOffset(8, 8, 8, 8);
            navLayout.spacing = 5f;
            navLayout.childControlHeight = true;
            navLayout.childForceExpandHeight = true;
            navLayout.childControlWidth = true;
            navLayout.childForceExpandWidth = true;

            RectTransform tabHost = Rect("Tab Host", window, PhasebreakUiTheme.Panel, true);
            PhasebreakUiTheme.StyleSurface(tabHost.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Panel);
            Anchors(tabHost, new Vector2(.155f, .085f), new Vector2(.975f, .852f));

            foreach (string tab in TabNames)
            {
                string selectedTab = tab;
                ButtonInLayout(tab, nav, tab, () => ShowTab(selectedTab));
                RectTransform content = CreateScrollPage(tab, tabHost);
                tabPages[tab] = content.parent.parent.gameObject;
                BuildTab(tab, content);
            }

            RectTransform feedbackRoot = Rect("Feedback", window, PhasebreakUiTheme.Raised, true);
            PhasebreakUiTheme.StyleSurface(feedbackRoot.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Raised);
            Anchors(feedbackRoot, new Vector2(.025f, .018f), new Vector2(.975f, .073f));
            feedback = Label("Feedback Text", feedbackRoot, "Ready.", 15f, PhasebreakUiTheme.Accent,
                new Vector2(.012f, .05f), new Vector2(.988f, .95f), TextAlignmentOptions.Left, FontStyles.Bold);

            BuildModal();
            BuildPlacementOverlay();
            ShowTab("QUICK");
        }

        private void BuildTab(string tab, RectTransform content)
        {
            switch (tab)
            {
                case "QUICK": BuildQuick(content); break;
                case "PLAYER": BuildPlayer(content); break;
                case "COMBAT": BuildCombat(content); break;
                case "TARGET": BuildTarget(content); break;
                case "SPAWN": BuildSpawn(content); break;
                case "PROGRESSION": BuildProgression(content); break;
                case "ITEMS": BuildItems(content); break;
                case "WORLD": BuildWorld(content); break;
                case "DUNGEON": BuildDungeon(content); break;
                case "VISUAL": BuildVisual(content); break;
                case "SAVE": BuildSave(content); break;
            }
        }

        private void BuildQuick(RectTransform content)
        {
            Section(content, "PLAYER");
            ButtonRow(content,
                new("GOD MODE", ToggleGodMode), new("HEAL FULL", HealFull), new("SET 1 HP", () => SetPlayerHealth(1)),
                new("KILL", () => RunCommand("/kill")), new("RESPAWN", () => RunCommand("/respawn")),
                new("UNSTUCK", () => RunCommand("/unstuck")));
            Section(content, "COMBAT");
            ButtonRow(content,
                new("FILL ENERGY", () => SetEnergy(combat?.MaximumResource ?? 0f)), new("ZERO ENERGY", () => SetEnergy(0f)),
                new("RESET COOLDOWNS", ResetCooldowns), new("REFILL CHARGES", RefillCharges),
                new("CLEAR TARGET", ClearTarget), new("RESET COMBAT", ResetCombat));
            Section(content, "WORLD");
            ButtonRow(content, new("TELEPORT TO MOUSE", () => BeginPlacement(PlacementMode.TeleportPlayer)),
                new("RESET ENCOUNTERS", () => RunCommand("/resetencounters")), new("CLEAR DEBUG SPAWNS", ClearDebugSpawns));
            Section(content, "SIMULATION");
            TimeScaleRow(content, .25f, .5f, 1f, 2f);
            Section(content, "PRESETS");
            ButtonRow(content, new("FLICKER 15", () => ApplyFlickerPreset(15)), new("FLICKER 23", () => ApplyFlickerPreset(23)),
                new("FLICKER 31", () => ApplyFlickerPreset(31)), new("RIFTBLADE BUILD", () => RunCommand("/riftbuild apply")),
                new("PRACTICE PACK", () => RunCommand("/riftpack")), new("DEATH TEST", DeathTest));
        }

        private void BuildPlayer(RectTransform content)
        {
            Section(content, "HEALTH");
            TextMeshProUGUI healthInfo = Info(content, string.Empty);
            liveRefresh.Add(() => healthInfo.text = health != null
                ? $"CURRENT {health.CurrentHealth}    MAXIMUM {health.MaxHealth}    ALIVE {(health.IsAlive ? "YES" : "NO")}" : "PLAYER HEALTH UNAVAILABLE");
            NumberAction(content, "SET HP  (0 uses real death flow)", "1", "SET", text =>
            {
                if (TryInt(text, 0, health?.MaxHealth ?? 0, out int value, out string note))
                { SetPlayerHealth(value); AppendNote(note); }
            });
            NumberAction(content, "DAMAGE SELF  (requested damage; mitigation applies)", "5", "TAKE HIT", text =>
            {
                if (TryInt(text, 1, 1000000, out int value, out string note) && health != null)
                { int before = health.CurrentHealth; bool hit = health.TakeHit(value, Vector3.zero); SetFeedback(hit ? $"Requested {value} damage; {before - health.CurrentHealth} applied." : "Damage blocked."); AppendNote(note); }
            });
            ButtonRow(content, new("HEAL FULL", HealFull), new("SET 1 HP", () => SetPlayerHealth(1)),
                new("KILL PLAYER", () => RunCommand("/kill")), new("RESPAWN", () => RunCommand("/respawn")));
            Section(content, "GOD MODE");
            ToggleButton(content, "GOD MODE", () => health != null && health.DebugGodMode, ToggleGodMode);
            Section(content, "ENERGY");
            TextMeshProUGUI energyInfo = Info(content, string.Empty);
            liveRefresh.Add(() => energyInfo.text = combat != null ? $"CURRENT {combat.CurrentResource:0.#}    MAXIMUM {combat.MaximumResource:0.#}" : "COMBAT UNAVAILABLE");
            NumberAction(content, "SET ENERGY", "100", "SET", text =>
            {
                if (TryFloat(text, 0f, combat?.MaximumResource ?? 0f, out float value, out string note))
                { SetEnergy(value); AppendNote(note); }
            });
            ButtonRow(content, new("FILL", () => SetEnergy(combat?.MaximumResource ?? 0f)), new("ZERO", () => SetEnergy(0f)));
            Section(content, "MOVEMENT");
            ButtonRow(content, new("0.25x", () => SetSpeed(.25f)), new("0.5x", () => SetSpeed(.5f)), new("1x", () => SetSpeed(1f)),
                new("2x", () => SetSpeed(2f)), new("3x", () => SetSpeed(3f)), new("5x", () => SetSpeed(5f)));
            ToggleButton(content, "FLY", () => movement != null && movement.DebugFly, () =>
            { if (movement != null) { movement.SetDebugFly(!movement.DebugFly); SetFeedback($"Fly {(movement.DebugFly ? "enabled" : "disabled")}."); } });
            ToggleButton(content, "NOCLIP", () => movement != null && movement.DebugNoClip, () =>
            { if (movement != null) { movement.SetDebugNoClip(!movement.DebugNoClip); SetFeedback($"Noclip {(movement.DebugNoClip ? "enabled" : "disabled")}."); } });
            ButtonRow(content, new("UNSTUCK", () => RunCommand("/unstuck")), new("RESET MOVEMENT", ResetMovement));
            Section(content, "POSITION");
            TextMeshProUGUI position = Info(content, string.Empty);
            liveRefresh.Add(() => position.text = movement != null
                ? $"X {movement.transform.position.x:0.00}    Y {movement.transform.position.y:0.00}    Z {movement.transform.position.z:0.00}    ROT Y {movement.transform.eulerAngles.y:0.0}" : "POSITION UNAVAILABLE");
            ButtonRow(content, new("COPY COORDINATES", CopyCoordinates), new("SAVE DEBUG BOOKMARK", SaveBookmark), new("RETURN TO BOOKMARK", ReturnBookmark));
        }

        private void BuildCombat(RectTransform content)
        {
            Section(content, "RESOURCES & ABILITY STATE");
            NumberAction(content, "SET ENERGY", "31", "SET", text =>
            {
                if (TryFloat(text, 0f, combat?.MaximumResource ?? 0f, out float value, out string note))
                { SetEnergy(value); AppendNote(note); }
            });
            ButtonRow(content, new("FILL ENERGY", () => SetEnergy(combat?.MaximumResource ?? 0f)), new("ZERO ENERGY", () => SetEnergy(0f)),
                new("RESET ALL COOLDOWNS", ResetCooldowns), new("REFILL ALL CHARGES", RefillCharges),
                new("CLEAR GLOBAL COOLDOWN", ClearGlobalCooldown), new("RESET COMBAT", ResetCombat));
            ToggleButton(content, "AUTO-ATTACK", () => combat != null && combat.AutoAttackArmed, () =>
            { if (combat != null) SetFeedback(combat.SetAutoAttackArmed(!combat.AutoAttackArmed) ? "Auto-attack state changed." : "Auto-attack is blocked."); });

            Section(content, "ABILITY INSPECTOR");
            TextMeshProUGUI abilityName = Info(content, string.Empty, 38f);
            TextMeshProUGUI abilityDetails = Info(content, string.Empty, 172f);
            liveRefresh.Add(() => RefreshAbilityInspector(abilityName, abilityDetails));
            ButtonRow(content, new("PREVIOUS", () => StepAbility(-1)), new("NEXT", () => StepAbility(1)),
                new("RESET SELECTED COOLDOWN", ResetSelectedCooldown), new("REFILL SELECTED CHARGES", RefillSelectedCharges));
            cooldownOverrideInput = NumberAction(content, "COOLDOWN OVERRIDE SECONDS  •  BLANK = AUTHORED", string.Empty, "APPLY", ApplyCooldownOverride);
            costOverrideInput = NumberAction(content, "ENERGY COST OVERRIDE  •  BLANK = AUTHORED", string.Empty, "APPLY", ApplyCostOverride);
            ButtonRow(content, new ActionSpec("RESET SELECTED OVERRIDES", ResetSelectedOverrides));

            TextMeshProUGUI flickerHeading = Section(content, "FLICKER STRIKE TEST OVERRIDES  •  DEVELOPMENT ONLY");
            flickerCountOverrideInput = NumberAction(content, "MAX FLICKER STRIKES  1–50  •  BLANK = AUTHORED", string.Empty, "APPLY", ApplyFlickerCountOverride);
            flickerContinuingCostOverrideInput = NumberAction(content, "CONTINUING ENERGY COST  •  BLANK = AUTHORED", string.Empty, "APPLY", ApplyFlickerContinuingCostOverride);
            flickerIntervalOverrideInput = NumberAction(content, "FLICKER INTERVAL SECONDS  •  BLANK = AUTHORED", string.Empty, "APPLY", ApplyFlickerIntervalOverride);
            RectTransform sustainRow = ToggleButton(content, "SUSTAIN UNTIL ENERGY EMPTY  •  DEBUG TEST OVERRIDE",
                () => combat != null && combat.DebugSustainFlicker, () =>
                {
                    if (combat == null) return;
                    combat.DebugSetSustainFlicker(!combat.DebugSustainFlicker);
                    SetFeedback($"Sustained Flicker testing {(combat.DebugSustainFlicker ? "enabled" : "disabled")}.");
                });
            Transform[] flickerOnly =
            {
                flickerHeading.transform,
                flickerCountOverrideInput.transform.parent,
                flickerContinuingCostOverrideInput.transform.parent,
                flickerIntervalOverrideInput.transform.parent,
                sustainRow
            };
            liveRefresh.Add(() =>
            {
                bool show = IsSelectedFlicker();
                foreach (Transform item in flickerOnly)
                    if (item != null && item.gameObject.activeSelf != show) item.gameObject.SetActive(show);
            });
            RefreshTuningInputs();

            Section(content, "CRITICAL TESTING");
            ButtonRow(content, new ActionSpec("FORCE NEXT HIT CRITICAL", ForceNextCritical));
            ToggleButton(content, "FORCE CRITS  •  DEBUG OVERRIDE", () => combat != null && combat.DebugForceCriticals, () =>
            { if (combat != null) { combat.DebugSetForceCriticals(!combat.DebugForceCriticals); SetFeedback($"Force Crits {(combat.DebugForceCriticals ? "enabled" : "disabled")}."); } });
            Section(content, "INCOMING DAMAGE  •  REQUESTED VALUE, NORMAL MITIGATION");
            ButtonRow(content, new("TAKE 1 DAMAGE", () => TakeDamage(1)), new("TAKE 5 DAMAGE", () => TakeDamage(5)), new("TAKE 10 DAMAGE", () => TakeDamage(10)));
        }

        private void BuildTarget(RectTransform content)
        {
            Section(content, "CURRENT HOSTILE TARGET");
            TextMeshProUGUI targetInfo = Info(content, string.Empty, 130f);
            liveRefresh.Add(() => targetInfo.text = TargetDescription());
            NumberAction(content, "SET TARGET HP", "1", "SET", text =>
            {
                MeleeEnemy enemy = CurrentEnemy();
                if (enemy == null) { SetFeedback("No enemy target selected."); return; }
                if (TryInt(text, 0, enemy.MaxHealth, out int value, out string note))
                { enemy.DebugSetHealth(value); SetFeedback($"Target health set to {enemy.CurrentHealth}/{enemy.MaxHealth}."); AppendNote(note); }
            });
            ButtonRow(content, new("HEAL TARGET", () => WithEnemy(enemy => { enemy.DebugHeal(); SetFeedback("Target healed."); })),
                new("SET TARGET TO 1 HP", () => WithEnemy(enemy => { enemy.DebugSetHealth(1); SetFeedback("Target set to 1 HP."); })),
                new("KILL TARGET", () => WithEnemy(enemy => { enemy.DebugSetHealth(0); SetFeedback("Target defeated without rewards."); })),
                new("CLEAR TARGET", ClearTarget));
            ButtonRow(content, new("TELEPORT TARGET TO PLAYER", TeleportTargetToPlayer),
                new("TELEPORT TARGET TO MOUSE", () => BeginPlacement(PlacementMode.TeleportTarget)));
            ToggleButton(content, "FREEZE AI", () => CurrentEnemy() != null && CurrentEnemy().DebugAiFrozen, () =>
            { WithEnemy(enemy => { enemy.DebugSetAiFrozen(!enemy.DebugAiFrozen); SetFeedback(enemy.DebugAiFrozen ? "Target AI frozen." : "Target AI resumed."); }); });
        }

        private void BuildSpawn(RectTransform content)
        {
            Section(content, "ENEMY CONFIGURATION");
            ChoiceRow(content, "ENEMY TYPE", () => RoleLabel(spawnRole), () =>
                spawnRole = (EnemyRole)(((int)spawnRole + 4) % 5), () => spawnRole = (EnemyRole)(((int)spawnRole + 1) % 5));
            ChoiceRow(content, "RANK", () => spawnRank.ToString(), () => spawnRank = (EnemyRank)(((int)spawnRank + 2) % 3),
                () => spawnRank = (EnemyRank)(((int)spawnRank + 1) % 3));
            NumberAction(content, "COUNT  1–20", "1", "APPLY", text =>
            { if (TryInt(text, 1, 20, out int value, out string note)) { spawnCount = value; SetFeedback($"Spawn count set to {value}."); AppendNote(note); } });
            NumberAction(content, "SPAWN RADIUS  2–40", "6", "APPLY", text =>
            { if (TryFloat(text, 2f, 40f, out float value, out string note)) { spawnRadius = value; SetFeedback($"Spawn radius set to {value:0.#}."); AppendNote(note); } });
            NumberAction(content, "HP MULTIPLIER  0.1–10", "1", "APPLY", text =>
            { if (TryFloat(text, .1f, 10f, out float value, out string note)) { spawnHealthMultiplier = value; SetFeedback($"HP multiplier set to {value:0.##}x."); AppendNote(note); } });
            NumberAction(content, "SCALE MULTIPLIER  0.5–3", "1", "APPLY", text =>
            { if (TryFloat(text, .5f, 3f, out float value, out string note)) { spawnScaleMultiplier = value; SetFeedback($"Scale multiplier set to {value:0.##}x."); AppendNote(note); } });
            NumberAction(content, "DAMAGE MULTIPLIER  0.1–10", "1", "APPLY", text =>
            { if (TryFloat(text, .1f, 10f, out float value, out string note)) { spawnDamageMultiplier = value; SetFeedback($"Damage multiplier set to {value:0.##}x."); AppendNote(note); } });
            ButtonRow(content, new("SPAWN AT MOUSE", () => BeginPlacement(PlacementMode.SpawnEnemy)),
                new("SPAWN AROUND PLAYER", SpawnAroundPlayer), new("CLEAR DEBUG SPAWNS", ClearDebugSpawns));
            Section(content, "COMBAT DUMMY  •  STATIONARY / REWARDLESS");
            ButtonRow(content, new("SPAWN 100 HP AT MOUSE", () => BeginDummyPlacement(100)),
                new("SPAWN 1,000 HP AT MOUSE", () => BeginDummyPlacement(1000)),
                new("SPAWN 100,000 HP AT MOUSE", () => BeginDummyPlacement(100000)));
            ButtonRow(content, new("RESET DUMMY HP", ResetDummies), new("CLEAR DUMMIES", ClearDummies));
        }

        private void BuildProgression(RectTransform content)
        {
            Section(content, "LEVEL & EXPERIENCE");
            TextMeshProUGUI progressInfo = Info(content, string.Empty);
            liveRefresh.Add(() => progressInfo.text = progression != null
                ? $"LEVEL {progression.Level}/{progression.MaximumLevel}    XP {progression.CurrentExperience}    NEXT {progression.ExperienceToNextLevel}" : "PROGRESSION UNAVAILABLE");
            NumberAction(content, "SET LEVEL", "10", "SET", text =>
            { if (TryInt(text, 1, progression?.MaximumLevel ?? 1, out int value, out string note) && progression != null) { progression.SetDebugLevel(value); SetFeedback($"Level set to {value}."); AppendNote(note); } });
            NumberAction(content, "SET CURRENT-LEVEL XP", "0", "SET", text =>
            { if (TryInt(text, 0, Math.Max(0, (progression?.ExperienceToNextLevel ?? 1) - 1), out int value, out string note) && progression != null) { progression.SetDebugExperience(value); SetFeedback($"XP set to {value}."); AppendNote(note); } });
            NumberAction(content, "ADD XP", "10", "ADD", text =>
            { if (TryInt(text, 0, 1000000, out int value, out string note) && progression != null) { progression.GrantExperience(value); SetFeedback($"Added {value} XP."); AppendNote(note); } });
            ButtonRow(content, new ActionSpec("ADD ENOUGH FOR NEXT LEVEL", () =>
            { if (progression != null && !progression.IsMaximumLevel) { int amount = progression.ExperienceToNextLevel - progression.CurrentExperience; progression.GrantExperience(amount); SetFeedback($"Added {amount} XP."); } }));
            Section(content, "SPECIALIZATION");
            ButtonRow(content, new("BERSERKER", () => SetSpecialization(Specialization.Berserker)),
                new("BULWARK", () => SetSpecialization(Specialization.Bulwark)), new("RIFTBLADE", () => SetSpecialization(Specialization.Riftblade)));
            Section(content, "TALENT STATE");
            TextMeshProUGUI talentInfo = Info(content, string.Empty);
            liveRefresh.Add(() => talentInfo.text = talents != null && build != null
                ? $"ACTIVE {build.Specialization.ToString().ToUpperInvariant()}    TOTAL {talents.TotalPoints}    SPENT {talents.SpentPoints}    AVAILABLE {talents.AvailablePoints}" : "TALENTS UNAVAILABLE");
            ButtonRow(content, new("RESET CURRENT TREE", () => ResetTalentTree(build != null ? build.Specialization : Specialization.Unchosen)),
                new("RESET BERSERKER", () => ResetTalentTree(Specialization.Berserker)),
                new("RESET BULWARK", () => ResetTalentTree(Specialization.Bulwark)),
                new("RESET RIFTBLADE", () => ResetTalentTree(Specialization.Riftblade)));
            Section(content, "EXISTING TEST PRESETS");
            ButtonRow(content, new("APPLY RIFTBLADE TEST BUILD", () => RunCommand("/riftbuild apply")),
                new("RIFTBLADE PRACTICE PACK", () => RunCommand("/riftpack")));
        }

        private void BuildItems(RectTransform content)
        {
            Section(content, "AUTHORED ITEM CATALOG");
            TextMeshProUGUI itemInfo = Info(content, string.Empty, 72f);
            liveRefresh.Add(() => RefreshItemInspector(itemInfo));
            TMP_InputField search = InputRow(content, "SEARCH NAME OR ID", "rift");
            ButtonRow(content, new("SEARCH", () => SearchItems(search.text)), new("PREVIOUS", () => StepItem(-1)), new("NEXT", () => StepItem(1)),
                new("GRANT ITEM", GrantSelectedItem), new("GRANT AND EQUIP", GrantAndEquipSelectedItem));
            Section(content, "PRESETS");
            ButtonRow(content, new("GRANT RIFTSTALKER TEST SET", GrantRiftstalkerSet), new("GRANT ALL AUTHORED TEST ITEMS", GrantAllItems));
        }

        private void BuildWorld(RectTransform content)
        {
            Section(content, "NAMED DESTINATIONS  •  SCENE-OWNED TRANSFORMS");
            ButtonRow(content, new("FRONTIER TRAILHEAD", () => TeleportNamed("trailhead")), new("NORTHGATE", () => TeleportNamed("northgate")),
                new("WESTMERE", () => TeleportNamed("westmere")), new("EASTWATCH", () => TeleportNamed("eastwatch")));
            ButtonRow(content, new("ANCIENT RUINS", () => TeleportNamed("ancient")), new("RIFT CRYPT ENTRANCE", () => TeleportNamed("rift crypt")),
                new("RIFTBLADE PRACTICE LANE", () => TeleportNamed("riftblade-practice")));
            Section(content, "POSITION & OVERLAYS");
            TextMeshProUGUI worldInfo = Info(content, string.Empty, 56f);
            liveRefresh.Add(() => worldInfo.text = WorldDescription());
            ButtonRow(content, new("TELEPORT TO MOUSE", () => BeginPlacement(PlacementMode.TeleportPlayer)),
                new("TOGGLE COORDINATES", () => RunCommand("/coords " + (chat != null && chat.ShowCoords ? "off" : "on"))));
            Section(content, "ENCOUNTERS");
            TextMeshProUGUI enemyCount = Info(content, string.Empty);
            liveRefresh.Add(() => enemyCount.text = EnemyCountDescription());
            ButtonRow(content, new("RESET WORLD ENCOUNTERS", () => RunCommand("/resetencounters")),
                new("DEFEAT ACTIVE WITHOUT REWARDS", () => RunCommand("/killall")), new("CLEAR DEBUG SPAWNS", ClearDebugSpawns));
        }

        private void BuildDungeon(RectTransform content)
        {
            Section(content, "RIFT CRYPT");
            TextMeshProUGUI dungeonInfo = Info(content, string.Empty, 90f);
            liveRefresh.Add(() => dungeonInfo.text = DungeonDescription());
            ButtonRow(content, new("TELEPORT TO RIFT CRYPT ENTRANCE", () => TeleportNamed("rift crypt")),
                new("RESET CURRENT ENCOUNTER", () => RunCommand("/resetencounters")),
                new("RECOVER TO CHECKPOINT", () => RunCommand("/respawn")));
            Info(content, "V1 intentionally does not expose reward duplication, arbitrary boss-room skips, or quest completion.", 45f);
        }

        private void BuildVisual(RectTransform content)
        {
            Section(content, "TIME SCALE");
            TextMeshProUGUI time = Info(content, string.Empty);
            liveRefresh.Add(() => time.text = $"CURRENT TIME SCALE  {Time.timeScale:0.##}x");
            TimeScaleRow(content, .1f, .25f, .5f, 1f, 2f);
            ButtonRow(content, new ActionSpec("RESET TO 1.0x", () =>
            {
                Time.timeScale = 1f;
                SetFeedback("Time scale reset to 1.0x.");
            }));
            Section(content, "EXISTING OVERLAYS");
            ButtonRow(content, new("TOGGLE FPS", () => RunCommand("/fps " + (chat != null && chat.ShowFps ? "off" : "on"))),
                new("TOGGLE COORDINATES", () => RunCommand("/coords " + (chat != null && chat.ShowCoords ? "off" : "on"))),
                new("TOGGLE HUD", () => RunCommand("/hud " + (chat != null && chat.HudVisible ? "off" : "on"))));
            Section(content, "COMBAT PRESENTATION");
            NumberAction(content, "SCREEN SHAKE  0–1", "0.5", "SET", text =>
            { if (TryFloat(text, 0f, 1f, out float value, out string note) && followCamera != null) { followCamera.ScreenShakeAmount = value; SetFeedback($"Screen shake set to {value:0.##}."); AppendNote(note); } });
            Info(content, "Flicker candidate visualization and presentation-disable toggles are deferred; V1 keeps the existing presentation owners unchanged.", 48f);
        }

        private void BuildSave(RectTransform content)
        {
            Section(content, "DANGER ZONE");
            Info(content, "RESET CHARACTER removes progression, talents, specialization, inventory/equipment, quests/discovery, one-time character rewards, and action-bar assignments. Settings, keybindings, video preferences, UI scale, and HUD positions are preserved. The active scene reloads immediately.", 92f);
            ButtonRow(content, new ActionSpec("RESET CHARACTER", ConfirmResetCharacter));
            Info(content, "RESET ALL LOCAL DATA removes the entire local PHASEBREAK profile, including character state, Settings, keybindings, video/UI preferences, HUD positions, and action assignments. The active scene reloads immediately.", 82f);
            ButtonRow(content, new ActionSpec("RESET ALL LOCAL DATA", ConfirmResetAll));
        }

        private void BuildModal()
        {
            modal = Rect("Confirmation Modal", root, new Color(.008f, .008f, .01f, .86f), true);
            Stretch(modal, 0f);
            RectTransform dialog = Rect("Dialog", modal, PhasebreakUiTheme.Window, true);
            PhasebreakUiTheme.StyleSurface(dialog.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Window);
            dialog.anchorMin = dialog.anchorMax = dialog.pivot = new Vector2(.5f, .5f);
            dialog.sizeDelta = new Vector2(720f, 360f);
            modalHeading = Label("Heading", dialog, string.Empty, 28f, PhasebreakUiTheme.Accent,
                new Vector2(.07f, .73f), new Vector2(.93f, .91f), TextAlignmentOptions.Center, FontStyles.Bold);
            modalBody = Label("Body", dialog, string.Empty, 18f, PhasebreakUiTheme.Text,
                new Vector2(.08f, .29f), new Vector2(.92f, .72f), TextAlignmentOptions.Center);
            modalConfirm = Button("Confirm", dialog, "CONFIRM", new Vector2(.08f, .08f), new Vector2(.55f, .23f), ConfirmModal);
            modalConfirmLabel = modalConfirm.GetComponentInChildren<TextMeshProUGUI>();
            Button("Cancel", dialog, "CANCEL", new Vector2(.6f, .08f), new Vector2(.92f, .23f), HideModal);
            modal.gameObject.SetActive(false);
        }

        private void BuildPlacementOverlay()
        {
            placementOverlay = Rect("Placement Mode", root, new Color(.01f, .01f, .012f, .42f), false);
            Stretch(placementOverlay, 0f);
            RectTransform prompt = Rect("Placement Prompt", placementOverlay, PhasebreakUiTheme.Window, true);
            PhasebreakUiTheme.StyleSurface(prompt.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Window);
            prompt.anchorMin = new Vector2(.25f, .84f);
            prompt.anchorMax = new Vector2(.75f, .96f);
            prompt.offsetMin = prompt.offsetMax = Vector2.zero;
            placementLabel = Label("Prompt", prompt, string.Empty, 20f, PhasebreakUiTheme.Text,
                new Vector2(.04f, .08f), new Vector2(.96f, .92f), TextAlignmentOptions.Center, FontStyles.Bold);
            placementOverlay.gameObject.SetActive(false);
        }

        private RectTransform CreateScrollPage(string name, RectTransform parent)
        {
            RectTransform page = Rect(name + " Page", parent, Color.clear, false);
            Stretch(page, 8f);
            UnityEngine.UI.ScrollRect scroll = page.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 30f;
            RectTransform viewport = Rect("Viewport", page, new Color(1f, 1f, 1f, .01f), true);
            Stretch(viewport, 2f);
            viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            RectTransform content = Rect("Content", viewport, Color.clear, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.sizeDelta = Vector2.zero;
            UnityEngine.UI.VerticalLayoutGroup layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 18);
            layout.spacing = 7f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit =
                UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        private void ShowTab(string name)
        {
            foreach (KeyValuePair<string, GameObject> pair in tabPages) pair.Value.SetActive(pair.Key == name);
        }

        private TextMeshProUGUI Section(RectTransform parent, string text)
        {
            TextMeshProUGUI label = Label("Section " + text, parent, text, 18f, PhasebreakUiTheme.Accent,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            Layout(label.gameObject, 32f);
            return label;
        }

        private TextMeshProUGUI Info(RectTransform parent, string text, float height = 36f)
        {
            TextMeshProUGUI label = Label("Info", parent, text, 15f, PhasebreakUiTheme.Text,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
            label.textWrappingMode = TextWrappingModes.Normal;
            Layout(label.gameObject, height);
            return label;
        }

        private void ButtonRow(RectTransform parent, params ActionSpec[] actions)
        {
            RectTransform row = Rect("Action Row", parent, Color.clear, false);
            Layout(row.gameObject, 38f);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = actions.Length > 1;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            float singleWidth = actions.Length == 1 ? 420f : 0f;
            foreach (ActionSpec spec in actions) ButtonInLayout(spec.Label, row, spec.Label, spec.Action, singleWidth);
        }

        private RectTransform ToggleButton(RectTransform parent, string label, Func<bool> state, Action action)
        {
            RectTransform row = Rect(label + " Row", parent, Color.clear, false);
            Layout(row.gameObject, 38f);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            UnityEngine.UI.Button button = ButtonInLayout(label, row, string.Empty, action, 520f);
            TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
            liveRefresh.Add(() => text.text = $"{label}: {(state() ? "ON" : "OFF")}");
            return row;
        }

        private void ChoiceRow(RectTransform parent, string label, Func<string> value, Action previous, Action next)
        {
            RectTransform row = Rect(label + " Choice", parent, Color.clear, false);
            Layout(row.gameObject, 38f);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 7f; layout.childControlHeight = true; layout.childForceExpandHeight = true;
            TextMeshProUGUI heading = Label("Heading", row, label, 15f, PhasebreakUiTheme.MutedText, Vector2.zero, Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            UnityEngine.UI.LayoutElement headingLayout = Layout(heading.gameObject, 38f); headingLayout.preferredWidth = 170f;
            ButtonInLayout("Previous", row, "<", previous, 46f);
            TextMeshProUGUI current = Label("Value", row, string.Empty, 16f, PhasebreakUiTheme.Text, Vector2.zero, Vector2.one, TextAlignmentOptions.Center, FontStyles.Bold);
            UnityEngine.UI.LayoutElement valueLayout = Layout(current.gameObject, 38f); valueLayout.flexibleWidth = 1f;
            liveRefresh.Add(() => current.text = value());
            ButtonInLayout("Next", row, ">", next, 46f);
        }

        private TMP_InputField NumberAction(RectTransform parent, string label, string initial, string actionLabel, Action<string> action)
        {
            RectTransform row = Rect(label + " Input Row", parent, Color.clear, false);
            Layout(row.gameObject, 40f);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 7f; layout.childControlHeight = true; layout.childForceExpandHeight = true;
            TextMeshProUGUI heading = Label("Heading", row, label, 15f, PhasebreakUiTheme.MutedText, Vector2.zero, Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            UnityEngine.UI.LayoutElement headingLayout = Layout(heading.gameObject, 40f); headingLayout.flexibleWidth = 1f;
            TMP_InputField input = InputInLayout(row, initial, 180f);
            ButtonInLayout("Apply", row, actionLabel, () => action(input.text), 150f);
            return input;
        }

        private TMP_InputField InputRow(RectTransform parent, string label, string initial)
        {
            RectTransform row = Rect(label + " Input Row", parent, Color.clear, false);
            Layout(row.gameObject, 40f);
            UnityEngine.UI.HorizontalLayoutGroup layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 7f; layout.childControlHeight = true; layout.childForceExpandHeight = true;
            TextMeshProUGUI heading = Label("Heading", row, label, 15f, PhasebreakUiTheme.MutedText, Vector2.zero, Vector2.one, TextAlignmentOptions.Left, FontStyles.Bold);
            UnityEngine.UI.LayoutElement headingLayout = Layout(heading.gameObject, 40f); headingLayout.preferredWidth = 210f;
            TMP_InputField input = InputInLayout(row, initial, 0f);
            input.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1f;
            return input;
        }

        private TMP_InputField InputInLayout(RectTransform parent, string initial, float width)
        {
            RectTransform box = Rect("Input", parent, PhasebreakUiTheme.Raised, true);
            PhasebreakUiTheme.StyleSurface(box.GetComponent<UnityEngine.UI.Image>(), PhasebreakUiTheme.Raised);
            UnityEngine.UI.LayoutElement element = Layout(box.gameObject, 40f); element.preferredWidth = width;
            if (width <= 0f) element.flexibleWidth = 1f;
            TMP_InputField input = box.gameObject.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 80;
            RectTransform viewport = Rect("Text Area", box, Color.clear, false);
            Stretch(viewport, 9f);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            TextMeshProUGUI value = Label("Value", viewport, initial, 16f, PhasebreakUiTheme.Text, Vector2.zero, Vector2.one, TextAlignmentOptions.Left);
            input.textViewport = viewport;
            input.textComponent = value;
            input.text = initial;
            return input;
        }

        private void TimeScaleRow(RectTransform content, params float[] values)
        {
            ActionSpec[] specs = values.Select(value => new ActionSpec($"{value:0.##}x", () =>
            { Time.timeScale = value; SetFeedback($"Time scale set to {value:0.##}x."); })).ToArray();
            ButtonRow(content, specs);
        }

        private void SetFeedback(string message) => feedback.text = message;
        private void AppendNote(string note) { if (!string.IsNullOrEmpty(note)) feedback.text += " " + note; }

        private void RunCommand(string command)
        {
            SetFeedback(chat != null ? chat.RunDeveloperCommand(command) : "Developer command registry unavailable.");
        }

        private void ToggleGodMode()
        {
            if (health == null) return;
            health.SetDebugGodMode(!health.DebugGodMode);
            SetFeedback($"God Mode {(health.DebugGodMode ? "enabled" : "disabled")}.");
        }

        private void HealFull()
        {
            if (health == null) return;
            health.ResetHealth();
            SetFeedback($"Health restored to {health.CurrentHealth}/{health.MaxHealth}.");
        }

        private void SetPlayerHealth(int value)
        {
            if (health == null) return;
            int result = health.DebugSetHealth(value);
            SetFeedback(value <= 0 ? "Player killed; normal death flow started." : $"Health set to {result}/{health.MaxHealth}.");
        }

        private void SetEnergy(float value)
        {
            if (combat == null) return;
            combat.SetDebugResource(value);
            SetFeedback($"Energy set to {combat.CurrentResource:0.#}.");
        }

        private void SetSpeed(float value)
        {
            if (movement == null) return;
            movement.SetDebugSpeed(value);
            SetFeedback($"Movement speed set to {movement.DebugSpeedMultiplier:0.##}x.");
        }

        private void ResetMovement()
        {
            if (movement == null) return;
            movement.SetDebugSpeed(1f); movement.SetDebugFly(false); movement.SetDebugNoClip(false); movement.ResetMotion();
            SetFeedback("Movement debug state reset.");
        }

        private void ResetCooldowns() { combat?.DebugResetAllCooldowns(); SetFeedback("All ability cooldowns reset."); }
        private void RefillCharges() { combat?.DebugRefillAllCharges(); SetFeedback("All ability charges refilled."); }
        private void ClearGlobalCooldown() { combat?.DebugClearGlobalCooldown(); SetFeedback("Global cooldown cleared."); }
        private void ResetCombat() { combat?.ResetCombat(); SetFeedback("Combat state reset."); }
        private void ClearTarget() { targeting?.SetTarget(null); SetFeedback("Target cleared."); }

        private void TakeDamage(int amount)
        {
            if (health == null) return;
            int before = health.CurrentHealth;
            bool applied = health.TakeHit(amount, Vector3.zero);
            SetFeedback(applied ? $"Requested {amount} damage; {before - health.CurrentHealth} applied." : "Damage blocked.");
        }

        private void ForceNextCritical()
        {
            combat?.DebugForceNextHitCritical();
            SetFeedback("Next eligible player damage hit will be critical.");
        }

        private void StepAbility(int delta)
        {
            int count = Math.Max(1, combat?.CatalogCount ?? 1);
            selectedAbility = (selectedAbility + delta + count) % count;
            RefreshTuningInputs();
            RefreshLiveViews();
        }

        private void RefreshAbilityInspector(TextMeshProUGUI heading, TextMeshProUGUI details)
        {
            if (combat == null || combat.CatalogCount == 0) { heading.text = "NO AUTHORED ABILITIES"; details.text = string.Empty; return; }
            selectedAbility = Mathf.Clamp(selectedAbility, 0, combat.CatalogCount - 1);
            CombatAbilityDefinition definition = combat.GetAbilityDefinition(selectedAbility);
            AbilityState state = combat.GetAbilityState(selectedAbility);
            if (definition == null) { heading.text = "MISSING ABILITY DEFINITION"; details.text = string.Empty; return; }
            string slots = string.Join(", ", Enumerable.Range(0, combat.AbilityCount)
                .Where(slot => combat.GetAssignedAbilityIndex(slot) == selectedAbility).Select(slot => (slot + 1).ToString()));
            string cooldownOverride = combat.DebugHasCooldownOverride(selectedAbility)
                ? $"OVERRIDE {combat.DebugCooldownOverride(selectedAbility):0.##}s" : "AUTHORED";
            string costOverride = combat.DebugHasCostOverride(selectedAbility)
                ? $"OVERRIDE {combat.DebugCostOverride(selectedAbility):0.##}" : "AUTHORED";
            heading.text = $"{definition.displayName}    [{selectedAbility + 1}/{combat.CatalogCount}]";
            details.text = $"ID  {definition.id}\nUNLOCKED  {(combat.IsAbilityUnlocked(selectedAbility) ? "YES" : "NO")}  •  SOURCE  {combat.GetAbilitySourceText(selectedAbility)}\n" +
                $"SLOTS  {(slots.Length == 0 ? "NONE" : slots)}  •  CHARGES  {state.Charges}/{state.MaximumCharges}  •  COOLDOWN  {state.CooldownRemaining:0.00}s\n" +
                $"COOLDOWN  AUTHORED {definition.cooldown:0.##}s  •  EFFECTIVE {combat.DebugGetEffectiveCooldown(selectedAbility):0.##}s  •  {cooldownOverride}\n" +
                $"ENERGY  AUTHORED {definition.resourceCost:0.##}  •  EFFECTIVE {combat.DebugGetEffectiveCost(selectedAbility):0.##}  •  {costOverride}  •  EXECUTION  {definition.executionType}";
            if (definition.executionType == AbilityExecutionType.FlickerStrike)
            {
                string count = combat.DebugHasFlickerCountOverride ? combat.DebugFlickerCountOverride.ToString() : $"AUTHORED {definition.flickerCount}";
                string continuing = combat.DebugHasFlickerContinuingCostOverride ? combat.DebugFlickerContinuingCostOverride.ToString("0.##") : $"AUTHORED {definition.flickerContinuingCost:0.##}";
                string interval = combat.DebugHasFlickerIntervalOverride ? $"{combat.DebugFlickerIntervalOverride:0.###}s" : $"AUTHORED {definition.flickerInterval:0.###}s";
                details.text += $"\nFLICKER  MAX {count}  •  CONTINUING {continuing}  •  INTERVAL {interval}  •  SUSTAIN {(combat.DebugSustainFlicker ? "ON" : "OFF")}" +
                    $"\nLAST RUN  {combat.DebugLastFlickerStrikeCount} STRIKES  •  {combat.DebugLastFlickerStopReason}";
            }
        }

        private bool IsSelectedFlicker() => combat != null && selectedAbility >= 0 && selectedAbility < combat.CatalogCount &&
            combat.GetAbilityDefinition(selectedAbility)?.executionType == AbilityExecutionType.FlickerStrike;

        private void RefreshTuningInputs()
        {
            if (combat == null) return;
            cooldownOverrideInput?.SetTextWithoutNotify(combat.DebugHasCooldownOverride(selectedAbility)
                ? combat.DebugCooldownOverride(selectedAbility).ToString("0.##", CultureInfo.InvariantCulture) : string.Empty);
            costOverrideInput?.SetTextWithoutNotify(combat.DebugHasCostOverride(selectedAbility)
                ? combat.DebugCostOverride(selectedAbility).ToString("0.##", CultureInfo.InvariantCulture) : string.Empty);
            flickerCountOverrideInput?.SetTextWithoutNotify(combat.DebugHasFlickerCountOverride
                ? combat.DebugFlickerCountOverride.ToString(CultureInfo.InvariantCulture) : string.Empty);
            flickerContinuingCostOverrideInput?.SetTextWithoutNotify(combat.DebugHasFlickerContinuingCostOverride
                ? combat.DebugFlickerContinuingCostOverride.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty);
            flickerIntervalOverrideInput?.SetTextWithoutNotify(combat.DebugHasFlickerIntervalOverride
                ? combat.DebugFlickerIntervalOverride.ToString("0.###", CultureInfo.InvariantCulture) : string.Empty);
        }

        private void ApplyCooldownOverride(string text)
        {
            if (combat == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                combat.DebugClearCooldownOverride(selectedAbility);
                SetFeedback("Selected ability uses its authored cooldown.");
                RefreshTuningInputs();
                return;
            }
            if (TryFloat(text, 0f, 300f, out float value, out string note) && combat.DebugSetCooldownOverride(selectedAbility, value))
            {
                SetFeedback($"Cooldown override set to {value:0.##} seconds.");
                AppendNote(note);
                RefreshTuningInputs();
            }
        }

        private void ApplyCostOverride(string text)
        {
            if (combat == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                combat.DebugClearCostOverride(selectedAbility);
                SetFeedback("Selected ability uses its authored Energy cost.");
                RefreshTuningInputs();
                return;
            }
            if (TryFloat(text, 0f, combat.MaximumResource, out float value, out string note) && combat.DebugSetCostOverride(selectedAbility, value))
            {
                SetFeedback($"Energy cost override set to {value:0.##}.");
                AppendNote(note);
                RefreshTuningInputs();
            }
        }

        private void ApplyFlickerCountOverride(string text)
        {
            if (combat == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                combat.DebugClearFlickerCountOverride();
                SetFeedback("Flicker uses its authored strike count.");
                RefreshTuningInputs();
                return;
            }
            if (TryInt(text, 1, 50, out int value, out string note))
            {
                combat.DebugSetFlickerCountOverride(value);
                SetFeedback($"Flicker strike limit set to {value}.");
                AppendNote(note);
                RefreshTuningInputs();
            }
        }

        private void ApplyFlickerContinuingCostOverride(string text)
        {
            if (combat == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                combat.DebugClearFlickerContinuingCostOverride();
                SetFeedback("Flicker uses its authored continuing Energy cost.");
                RefreshTuningInputs();
                return;
            }
            if (TryFloat(text, 0f, combat.MaximumResource, out float value, out string note))
            {
                combat.DebugSetFlickerContinuingCostOverride(value);
                SetFeedback($"Flicker continuing Energy cost set to {value:0.##}.");
                AppendNote(note);
                RefreshTuningInputs();
            }
        }

        private void ApplyFlickerIntervalOverride(string text)
        {
            if (combat == null) return;
            if (string.IsNullOrWhiteSpace(text))
            {
                combat.DebugClearFlickerIntervalOverride();
                SetFeedback("Flicker uses its authored interval.");
                RefreshTuningInputs();
                return;
            }
            if (TryFloat(text, .04f, 5f, out float value, out string note))
            {
                combat.DebugSetFlickerIntervalOverride(value);
                SetFeedback($"Flicker interval set to {value:0.###} seconds.");
                AppendNote(note);
                RefreshTuningInputs();
            }
        }

        private void ResetSelectedOverrides()
        {
            if (combat == null || !combat.DebugResetAbilityTuningOverrides(selectedAbility))
            {
                SetFeedback("No ability selected.");
                return;
            }
            RefreshTuningInputs();
            SetFeedback("Selected ability tuning overrides reset to authored values.");
        }

        private void ResetSelectedCooldown()
        {
            SetFeedback(combat != null && combat.DebugResetAbilityCooldown(selectedAbility)
                ? $"{combat.GetAbilityDefinition(selectedAbility)?.displayName} cooldown reset." : "No ability selected.");
        }

        private void RefillSelectedCharges()
        {
            SetFeedback(combat != null && combat.DebugRefillAbilityCharges(selectedAbility)
                ? $"{combat.GetAbilityDefinition(selectedAbility)?.displayName} charges refilled." : "No ability selected.");
        }

        private int FlickerIndex()
        {
            if (combat == null) return -1;
            for (int i = 0; i < combat.CatalogCount; i++)
                if (combat.GetAbilityDefinition(i)?.executionType == AbilityExecutionType.FlickerStrike) return i;
            return -1;
        }

        private void ApplyFlickerPreset(int energy)
        {
            if (combat == null || build == null || talents == null)
            {
                SetFeedback("Flicker preset failed: combat, build, or talent owner is unavailable.");
                return;
            }
            int index = FlickerIndex();
            if (index < 0)
            {
                SetFeedback("Flicker preset failed: Flicker Strike is missing from the authored combat catalog.");
                return;
            }
            SpecializationChangeResult specialization = build.SetSpecialization(Specialization.Riftblade);
            if (specialization is SpecializationChangeResult.AbilityInProgress or SpecializationChangeResult.Defeated)
            {
                SetFeedback($"Flicker preset failed: {specialization}.");
                return;
            }
            if (specialization is not (SpecializationChangeResult.Changed or SpecializationChangeResult.AlreadyActive))
            {
                SetFeedback("Flicker preset failed: Riftblade specialization is unavailable.");
                return;
            }
            if (!talents.DebugActivateAbilityGrant("flicker-strike") || !combat.IsAbilityUnlocked(index))
            {
                SetFeedback("Flicker preset failed: the development-only Flicker talent override could not be activated.");
                return;
            }
            if (!combat.DebugResetAbilityCooldown(index) || !combat.DebugRefillAbilityCharges(index))
            {
                SetFeedback("Flicker preset failed: cooldown or charge state could not be reset.");
                return;
            }
            combat.DebugClearGlobalCooldown();
            combat.SetDebugResource(energy);
            AbilityState state = combat.GetAbilityState(index);
            if (build.Specialization != Specialization.Riftblade || !combat.IsAbilityUnlocked(index) ||
                state.Charges < 1 || !Mathf.Approximately(combat.CurrentResource, energy))
            {
                SetFeedback("Flicker preset failed validation after setup.");
                return;
            }
            selectedAbility = index;
            RefreshTuningInputs();
            SetFeedback($"Flicker test state ready: Riftblade active, Flicker unlocked, charge ready, cooldown reset, Energy {energy}.");
        }

        private void DeathTest()
        {
            if (health == null) return;
            health.SetDebugGodMode(false); health.DebugSetHealth(1); combat?.ResetCombat(); combat?.SetDebugResource(0f);
            SetFeedback("Death test ready: God Mode OFF, HP 1, combat state reset.");
        }

        private MeleeEnemy CurrentEnemy() => targeting != null && targeting.CurrentTarget != null
            ? targeting.CurrentTarget.GetComponent<MeleeEnemy>() : null;

        private void WithEnemy(Action<MeleeEnemy> action)
        {
            MeleeEnemy enemy = CurrentEnemy();
            if (enemy == null) { SetFeedback("No enemy target selected."); return; }
            action(enemy);
        }

        private string TargetDescription()
        {
            Targetable target = targeting?.CurrentTarget;
            if (target == null) return "NO TARGET SELECTED";
            MeleeEnemy enemy = target.GetComponent<MeleeEnemy>();
            float distance = movement != null ? Vector3.Distance(movement.transform.position, target.transform.position) : 0f;
            Vector3 p = target.transform.position;
            Vector3 f = target.transform.forward;
            return $"{target.DisplayName}  •  {(enemy != null ? enemy.Role.ToString() : target.Faction.ToString())}  •  RANK {(enemy != null ? enemy.Rank.ToString() : target.Rank.ToString())}\n" +
                $"HP {target.CurrentHealth}/{target.MaxHealth}  •  DISTANCE {distance:0.0}m  •  {(target.IsAlive ? "ALIVE" : "DEAD")}  •  STATE {(enemy != null ? enemy.StateName : "N/A")}\n" +
                $"POSITION {p.x:0.0}, {p.y:0.0}, {p.z:0.0}  •  FORWARD {f.x:0.00}, {f.y:0.00}, {f.z:0.00}";
        }

        private void TeleportTargetToPlayer()
        {
            WithEnemy(enemy =>
            {
                if (movement == null) return;
                Vector3 point = movement.transform.position + movement.transform.forward * 2.2f;
                if (TryGround(point, out Vector3 ground)) point = ground;
                enemy.DebugTeleport(point);
                SetFeedback("Target teleported to player.");
            });
        }

        private void BeginDummyPlacement(int healthValue)
        {
            pendingDummyHealth = healthValue;
            BeginPlacement(PlacementMode.SpawnDummy);
        }

        private void BeginPlacement(PlacementMode mode)
        {
            if (mode == PlacementMode.TeleportTarget && targeting?.CurrentTarget == null)
            { SetFeedback("No target selected."); return; }
            pendingTarget = mode == PlacementMode.TeleportTarget ? targeting.CurrentTarget : null;
            placementMode = mode;
            placementStartedFrame = Time.frameCount;
            window.gameObject.SetActive(false);
            placementOverlay.gameObject.SetActive(true);
            placementLabel.text = mode switch
            {
                PlacementMode.SpawnEnemy => "CLICK VALID GROUND TO SPAWN  •  ESC TO CANCEL",
                PlacementMode.SpawnDummy => $"CLICK VALID GROUND TO SPAWN {pendingDummyHealth:N0} HP DUMMY  •  ESC TO CANCEL",
                PlacementMode.TeleportTarget => "CLICK VALID GROUND TO TELEPORT TARGET  •  ESC TO CANCEL",
                _ => "CLICK VALID GROUND TO TELEPORT  •  ESC TO CANCEL"
            };
        }

        private void CancelPlacement()
        {
            placementMode = PlacementMode.None;
            pendingTarget = null;
            placementOverlay.gameObject.SetActive(false);
            window.gameObject.SetActive(true);
            SetFeedback("Placement cancelled.");
            GameplayInputFocus.ConsumeFrame();
        }

        private void HandlePlacementClick()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Time.frameCount <= placementStartedFrame || !mouse.leftButton.wasPressedThisFrame) return;
            if (!TryGroundFromScreen(mouse.position.ReadValue(), out Vector3 point, out string reason))
            { placementLabel.text = reason + "  •  CLICK ANOTHER POINT  •  ESC TO CANCEL"; return; }
            string result = placementMode switch
            {
                PlacementMode.SpawnEnemy => SpawnConfiguredAt(point),
                PlacementMode.SpawnDummy => SpawnDummyAt(point, pendingDummyHealth),
                PlacementMode.TeleportPlayer => TeleportPlayer(point),
                PlacementMode.TeleportTarget => TeleportPendingTarget(point),
                _ => "Placement cancelled."
            };
            placementMode = PlacementMode.None;
            pendingTarget = null;
            placementOverlay.gameObject.SetActive(false);
            window.gameObject.SetActive(true);
            SetFeedback(result);
            GameplayInputFocus.ConsumeFrame();
        }

        private string SpawnConfiguredAt(Vector3 point)
        {
            int spawned = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 offset = spawnCount == 1 ? Vector2.zero : UnityEngine.Random.insideUnitCircle * Math.Max(1f, spawnRadius * .35f);
                Vector3 candidate = point + new Vector3(offset.x, 0f, offset.y);
                if (TryGround(candidate, out Vector3 grounded) && IsSafePlacement(grounded) && SpawnEnemy(grounded, false, 0) != null) spawned++;
            }
            return $"Spawned {spawned} {spawnRank} {RoleLabel(spawnRole)} at cursor.";
        }

        private void SpawnAroundPlayer()
        {
            if (movement == null) return;
            int spawned = 0;
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = i * Mathf.PI * 2f / spawnCount + UnityEngine.Random.Range(-.22f, .22f);
                float distance = UnityEngine.Random.Range(Mathf.Max(2.5f, spawnRadius * .55f), spawnRadius);
                Vector3 candidate = movement.transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (TryGround(candidate, out Vector3 grounded) && IsSafePlacement(grounded) && SpawnEnemy(grounded, false, 0) != null) spawned++;
            }
            SetFeedback($"Spawned {spawned}/{spawnCount} enemies around player.");
        }

        private string SpawnDummyAt(Vector3 point, int healthValue)
        {
            MeleeEnemy enemy = SpawnEnemy(point, true, healthValue);
            return enemy != null ? $"Spawned {healthValue:N0} HP combat dummy." : "Combat dummy spawn failed.";
        }

        private MeleeEnemy SpawnEnemy(Vector3 point, bool dummy, int dummyHealth)
        {
            GameObject prefab = FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None)
                .Select(zone => zone.DebugEnemyPrefab).FirstOrDefault(candidate => candidate != null);
            if (prefab == null) { SetFeedback("No authored enemy prefab is available in this scene."); return null; }
            GameObject created = Instantiate(prefab, point, Quaternion.identity);
            created.name = dummy ? $"Debug Combat Dummy {dummyHealth}" : $"Debug {spawnRank} {spawnRole}";
            MeleeEnemy enemy = created.GetComponent<MeleeEnemy>();
            if (enemy == null) { Destroy(created); return null; }
            enemy.ConfigureRole(dummy ? EnemyRole.Zombie : spawnRole, dummy ? EnemyRank.Normal : spawnRank);
            if (dummy)
            {
                enemy.DebugSetMaximumHealth(dummyHealth);
                enemy.DebugConfigure(1f, 1f, 1f, true, true);
                created.GetComponent<Targetable>()?.Configure("Combat Dummy", TargetFaction.Hostile, 1);
            }
            else enemy.DebugConfigure(spawnHealthMultiplier, spawnScaleMultiplier, spawnDamageMultiplier, true, false);
            DebugSpawnedEnemy marker = created.AddComponent<DebugSpawnedEnemy>();
            marker.Configure(dummy, dummyHealth);
            return enemy;
        }

        private void ClearDebugSpawns()
        {
            DebugSpawnedEnemy[] markers = FindObjectsByType<DebugSpawnedEnemy>(FindObjectsSortMode.None);
            foreach (DebugSpawnedEnemy marker in markers) if (marker != null) Destroy(marker.gameObject);
            SetFeedback($"Cleared {markers.Length} debug-spawned enemies.");
        }

        private void ResetDummies()
        {
            int count = 0;
            foreach (DebugSpawnedEnemy marker in FindObjectsByType<DebugSpawnedEnemy>(FindObjectsSortMode.None))
                if (marker != null && marker.IsCombatDummy && marker.TryGetComponent(out MeleeEnemy enemy))
                { enemy.DebugSetMaximumHealth(marker.DummyMaximumHealth); count++; }
            SetFeedback($"Reset {count} combat dummies.");
        }

        private void ClearDummies()
        {
            DebugSpawnedEnemy[] markers = FindObjectsByType<DebugSpawnedEnemy>(FindObjectsSortMode.None)
                .Where(marker => marker != null && marker.IsCombatDummy).ToArray();
            foreach (DebugSpawnedEnemy marker in markers) Destroy(marker.gameObject);
            SetFeedback($"Cleared {markers.Length} combat dummies.");
        }

        private void SetSpecialization(Specialization specialization)
        {
            if (build == null) return;
            SpecializationChangeResult result = build.SetSpecialization(specialization);
            SetFeedback(result is SpecializationChangeResult.Changed or SpecializationChangeResult.AlreadyActive
                ? $"Specialization set to {specialization}." : $"Specialization change blocked: {result}.");
        }

        private void ResetTalentTree(Specialization specialization)
        {
            if (talents == null || specialization == Specialization.Unchosen) { SetFeedback("No specialization tree selected."); return; }
            talents.ResetTree(specialization);
            SetFeedback($"{specialization} talent tree reset.");
        }

        private IReadOnlyList<PhasebreakItemDefinition> Items => build?.ItemCatalog ?? Array.Empty<PhasebreakItemDefinition>();

        private void RefreshItemInspector(TextMeshProUGUI label)
        {
            if (Items.Count == 0) { label.text = "NO AUTHORED ITEMS AVAILABLE"; return; }
            selectedItem = Mathf.Clamp(selectedItem, 0, Items.Count - 1);
            PhasebreakItemDefinition item = Items[selectedItem];
            label.text = item == null ? "MISSING ITEM" : $"{item.displayName}    [{selectedItem + 1}/{Items.Count}]\nID  {item.id}  •  RARITY  {item.rarity}  •  SLOT  {item.slot}";
        }

        private void StepItem(int delta)
        {
            if (Items.Count == 0) return;
            selectedItem = (selectedItem + delta + Items.Count) % Items.Count;
            RefreshLiveViews();
        }

        private void SearchItems(string query)
        {
            int found = -1;
            for (int index = 0; index < Items.Count; index++)
                if (Items[index] != null &&
                    (Items[index].displayName.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase) ||
                     Items[index].id.Contains(query ?? string.Empty, StringComparison.OrdinalIgnoreCase)))
                { found = index; break; }
            if (found < 0) SetFeedback("No authored item matched the search.");
            else { selectedItem = found; SetFeedback($"Selected {Items[found].displayName}."); RefreshLiveViews(); }
        }

        private void GrantSelectedItem()
        {
            PhasebreakItemDefinition item = Items.Count > 0 ? Items[Mathf.Clamp(selectedItem, 0, Items.Count - 1)] : null;
            SetFeedback(item != null && build.GrantRewardById(item.id) ? $"Granted {item.displayName}." : "Item grant failed.");
        }

        private void GrantAndEquipSelectedItem()
        {
            PhasebreakItemDefinition item = Items.Count > 0 ? Items[Mathf.Clamp(selectedItem, 0, Items.Count - 1)] : null;
            SetFeedback(item != null && build.DebugGrantAndEquipById(item.id) ? $"Granted and equipped {item.displayName}." : "Grant and equip failed.");
        }

        private void GrantRiftstalkerSet()
        {
            string[] ids = { "weapon.rift-iron", "hands.phasegrip", "boots.wake", "core.riftheart", "relic.blinkwake", "sigil.emberglass" };
            int granted = ids.Count(id => build != null && build.GrantRewardById(id));
            SetFeedback($"Granted {granted}/{ids.Length} Riftstalker test items.");
        }

        private void GrantAllItems()
        {
            int granted = Items.Count(item => item != null && build != null && build.GrantRewardById(item.id));
            SetFeedback($"Granted {granted} authored items.");
        }

        private void TeleportNamed(string search)
        {
            if (movement == null) return;
            Transform destination = null;
            if (search == "riftblade-practice")
                destination = FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None)
                    .FirstOrDefault(zone => zone.ZoneId == search)?.transform;
            else
            {
                destination = FindObjectsByType<QuestLocation>(FindObjectsSortMode.None)
                    .FirstOrDefault(location => Contains(location.Id, search) || Contains(location.DisplayName, search))?.transform;
                if (destination == null)
                    destination = FindObjectsByType<OpenWorldCheckpoint>(FindObjectsSortMode.None)
                        .FirstOrDefault(checkpoint => Contains(checkpoint.DisplayName, search))?.transform;
            }
            if (destination == null) { SetFeedback($"Scene-owned destination '{search}' was not found."); return; }
            Vector3 point = destination.position;
            if (TryGround(point, out Vector3 grounded)) point = grounded;
            movement.DebugTeleport(point);
            targeting?.SetTarget(null); combat?.ResetCombat();
            SetFeedback($"Teleported to {destination.name}.");
        }

        private string WorldDescription()
        {
            if (movement == null) return "WORLD POSITION UNAVAILABLE";
            Vector3 p = movement.transform.position;
            QuestLocation nearest = FindObjectsByType<QuestLocation>(FindObjectsSortMode.None)
                .OrderBy(location => Vector3.Distance(p, location.transform.position)).FirstOrDefault();
            string landmark = nearest != null ? $"{nearest.DisplayName} ({Vector3.Distance(p, nearest.transform.position):0}m)" : "NONE";
            return $"XYZ  {p.x:0.0}, {p.y:0.0}, {p.z:0.0}    NEAREST  {landmark}";
        }

        private string EnemyCountDescription()
        {
            MeleeEnemy[] enemies = FindObjectsByType<MeleeEnemy>(FindObjectsSortMode.None);
            int debug = enemies.Count(enemy => enemy != null && enemy.GetComponent<DebugSpawnedEnemy>() != null && enemy.IsAlive);
            int world = enemies.Count(enemy => enemy != null && enemy.IsAlive && enemy.GetComponentInParent<RiftDungeonEncounter>() == null);
            return $"LIVING WORLD ENEMIES  {world}    DEBUG-SPAWNED  {debug}";
        }

        private string DungeonDescription()
        {
            RiftDungeonController dungeon = FindAnyObjectByType<RiftDungeonController>();
            if (dungeon == null) return "RIFT DUNGEON OWNER UNAVAILABLE";
            RiftDungeonEncounter encounter = dungeon.CurrentEncounter;
            return $"IN DUNGEON  {(dungeon.IsInDungeon ? "YES" : "NO")}  •  STATE  {dungeon.State}\n" +
                $"ENCOUNTER  {(encounter != null ? (dungeon.CurrentEncounterIndex + 1).ToString() : "NONE")}  •  REMAINING  {(encounter != null ? encounter.RemainingEnemies : 0)}  •  OBJECTIVE  {dungeon.ObjectiveText}";
        }

        private void CopyCoordinates()
        {
            if (movement == null) return;
            Vector3 p = movement.transform.position;
            GUIUtility.systemCopyBuffer = string.Format(CultureInfo.InvariantCulture, "{0:0.###}, {1:0.###}, {2:0.###}  rotY {3:0.###}", p.x, p.y, p.z, movement.transform.eulerAngles.y);
            SetFeedback("Coordinates copied to clipboard.");
        }

        private void SaveBookmark()
        {
            if (movement == null) return;
            bookmark = movement.transform.position; bookmarkRotation = movement.transform.rotation; hasBookmark = true;
            SetFeedback("Runtime debug bookmark saved.");
        }

        private void ReturnBookmark()
        {
            if (!hasBookmark || movement == null) { SetFeedback("No debug bookmark has been saved this run."); return; }
            movement.DebugTeleport(bookmark, bookmarkRotation);
            SetFeedback("Returned to debug bookmark.");
        }

        private string TeleportPlayer(Vector3 point)
        {
            if (movement == null) return "Player movement unavailable.";
            movement.DebugTeleport(point); targeting?.SetTarget(null); return "Teleported player to mouse.";
        }

        private string TeleportPendingTarget(Vector3 point)
        {
            MeleeEnemy enemy = pendingTarget != null ? pendingTarget.GetComponent<MeleeEnemy>() : null;
            if (enemy == null) return "Selected target is unavailable.";
            enemy.DebugTeleport(point); return "Teleported target to mouse.";
        }

        private bool TryGroundFromScreen(Vector2 screenPoint, out Vector3 point, out string reason)
        {
            point = default;
            Camera camera = Camera.main;
            if (camera == null) { reason = "WORLD CAMERA UNAVAILABLE"; return false; }
            RaycastHit[] hits = Physics.RaycastAll(camera.ScreenPointToRay(screenPoint), 2000f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<PlayerHealth>() != null || hit.collider.GetComponentInParent<MeleeEnemy>() != null) continue;
                if (hit.normal.y < .55f) continue;
                point = hit.point + Vector3.up * .15f;
                if (!IsSafePlacement(point)) { reason = "PLACEMENT BLOCKED"; return false; }
                reason = string.Empty; return true;
            }
            reason = "NO VALID GROUND"; return false;
        }

        private bool TryGround(Vector3 candidate, out Vector3 point)
        {
            if (Physics.Raycast(candidate + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 300f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y >= .55f)
            { point = hit.point + Vector3.up * .15f; return true; }
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 origin = terrain.transform.position; Vector3 size = terrain.terrainData.size;
                if (candidate.x < origin.x || candidate.x > origin.x + size.x || candidate.z < origin.z || candidate.z > origin.z + size.z) continue;
                point = candidate; point.y = terrain.SampleHeight(candidate) + origin.y + .15f; return true;
            }
            point = candidate; return false;
        }

        private bool IsSafePlacement(Vector3 point)
        {
            if (movement != null && Vector3.Distance(point, movement.transform.position) < 1.6f) return false;
            return !Physics.CheckCapsule(point + Vector3.up * .55f, point + Vector3.up * 1.65f, .35f, ~0, QueryTriggerInteraction.Ignore);
        }

        private void ConfirmResetCharacter()
        {
            ShowModal("RESET CHARACTER?", "This resets progression, talents, quests, inventory/equipment, world discovery and action-bar assignments.\n\nSettings and keybindings will be preserved.", "RESET CHARACTER", ResetCharacter);
        }

        private void ConfirmResetAll()
        {
            ShowModal("DELETE ALL LOCAL DATA?", "This resets the entire local PHASEBREAK profile, including character progression, keybindings and Settings.\n\nThis cannot be undone.", "DELETE EVERYTHING", ResetAllLocalData);
        }

        private void ShowModal(string heading, string body, string confirm, Action action)
        {
            modalHeading.text = heading; modalBody.text = body; modalConfirmLabel.text = confirm;
            pendingConfirmation = action; modal.gameObject.SetActive(true);
        }

        private void HideModal() { pendingConfirmation = null; modal.gameObject.SetActive(false); }

        private void ConfirmModal()
        {
            Action action = pendingConfirmation; pendingConfirmation = null; modal.gameObject.SetActive(false); action?.Invoke();
        }

        private void ResetCharacter()
        {
            PlayerProgression.DebugDeleteSavedState(); TalentSystem.DebugDeleteSavedState(); PlayerBuildSystem.DebugDeleteSavedState();
            QuestJournal.DebugDeleteSavedState(); PlayerCombat.DebugDeleteSavedAssignments(); PlayerPrefs.Save();
            SetFeedback("Character data reset. Settings preserved. Reloading scene…");
            ReloadScene();
        }

        private void ResetAllLocalData()
        {
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save(); PhasebreakSettings.DebugClearRuntimeCache();
            SetFeedback("All local data reset. Reloading scene…");
            ReloadScene();
        }

        private void ReloadScene()
        {
            Time.timeScale = 1f;
            GameplayInputFocus.SetMenuFocused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private bool TryInt(string text, int minimum, int maximum, out int value, out string note)
        {
            note = string.Empty;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                value = 0;
                SetFeedback("Enter a valid whole number.");
                return false;
            }
            value = Mathf.Clamp(parsed, minimum, maximum);
            if (value != parsed) note = $"Clamped to {value}.";
            return true;
        }

        private bool TryFloat(string text, float minimum, float maximum, out float value, out string note)
        {
            note = string.Empty;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) || float.IsNaN(parsed) || float.IsInfinity(parsed))
            {
                value = 0f;
                SetFeedback("Enter a valid number using a decimal point.");
                return false;
            }
            value = Mathf.Clamp(parsed, minimum, maximum);
            if (!Mathf.Approximately(value, parsed)) note = $"Clamped to {value:0.##}.";
            return true;
        }

        private static bool Contains(string value, string search) => !string.IsNullOrWhiteSpace(value) &&
            value.Replace("-", " ").Contains(search, StringComparison.OrdinalIgnoreCase);

        private static string RoleLabel(EnemyRole role) => role switch
        {
            EnemyRole.Zombie => "Risen Zombie", EnemyRole.Brute => "Puglin Brute",
            EnemyRole.Skirmisher => "Imp Skirmisher", EnemyRole.Caster => "Rift Caster",
            EnemyRole.Charger => "Puglin Charger", _ => role.ToString()
        };

        private static RectTransform Rect(string name, Transform parent, Color color, bool raycast)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            item.transform.SetParent(parent, false);
            UnityEngine.UI.Image image = item.GetComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = raycast;
            return item.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string text, float size, Color color,
            Vector2 min, Vector2 max, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            GameObject item = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            item.transform.SetParent(parent, false);
            TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
            label.text = text; label.fontSize = size; label.color = color; label.alignment = alignment;
            label.fontStyle = style; label.raycastTarget = false; label.textWrappingMode = TextWrappingModes.Normal;
            Anchors(label.rectTransform, min, max);
            return label;
        }

        private static UnityEngine.UI.Button Button(string name, Transform parent, string text, Vector2 min, Vector2 max, Action action)
        {
            RectTransform rect = Rect(name, parent, PhasebreakUiTheme.Raised, true);
            Anchors(rect, min, max);
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            PhasebreakUiTheme.StyleButton(button);
            TextMeshProUGUI label = Label("Label", rect, text, 15f, PhasebreakUiTheme.Text, new Vector2(.03f, .05f), new Vector2(.97f, .95f), TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private static UnityEngine.UI.Button ButtonInLayout(string name, Transform parent, string text, Action action, float preferredWidth = 0f)
        {
            RectTransform rect = Rect(name, parent, PhasebreakUiTheme.Raised, true);
            UnityEngine.UI.LayoutElement element = Layout(rect.gameObject, 36f);
            if (preferredWidth > 0f) element.preferredWidth = preferredWidth; else element.flexibleWidth = 1f;
            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            PhasebreakUiTheme.StyleButton(button);
            TextMeshProUGUI label = Label("Label", rect, text, 14f, PhasebreakUiTheme.Text, new Vector2(.03f, .04f), new Vector2(.97f, .96f), TextAlignmentOptions.Center, FontStyles.Bold);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        private static UnityEngine.UI.LayoutElement Layout(GameObject item, float height)
        {
            UnityEngine.UI.LayoutElement element = item.GetComponent<UnityEngine.UI.LayoutElement>() ?? item.AddComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = height; element.preferredHeight = height;
            return element;
        }

        private static void Anchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
        }
    }
}
#endif
