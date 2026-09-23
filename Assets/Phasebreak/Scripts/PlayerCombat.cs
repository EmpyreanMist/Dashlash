using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    public readonly struct AbilityState
    {
        public readonly string Name;
        public readonly string Key;
        public readonly float CooldownRemaining;
        public readonly float CooldownDuration;
        public readonly float ResourceCost;
        public readonly bool IsUsable;
        public readonly int Charges;
        public readonly int MaximumCharges;
        public readonly Sprite Icon;
        public readonly bool HasAbility;
        public readonly float RechargeRemaining;

        public AbilityState(string name, string key, float cooldownRemaining, float cooldownDuration,
            float resourceCost, bool isUsable, int charges, int maximumCharges, Sprite icon,
            bool hasAbility = true, float rechargeRemaining = 0f)
        {
            Name = name;
            Key = key;
            CooldownRemaining = cooldownRemaining;
            CooldownDuration = cooldownDuration;
            ResourceCost = resourceCost;
            IsUsable = isUsable;
            Charges = charges;
            MaximumCharges = maximumCharges;
            Icon = icon;
            HasAbility = hasAbility;
            RechargeRemaining = rechargeRemaining;
        }
    }

    public readonly struct AbilityPresentationEvent
    {
        public readonly int Index;
        public readonly string Name;
        public readonly AbilityExecutionType Type;
        public readonly float Duration;
        public readonly bool Critical;
        public readonly int Strike;
        public readonly bool Finisher;

        public AbilityPresentationEvent(int index, string name, AbilityExecutionType type,
            float duration, bool critical, int strike = 0, bool finisher = false)
        {
            Index = index;
            Name = name;
            Type = type;
            Duration = duration;
            Critical = critical;
            Strike = strike;
            Finisher = finisher;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const int AbilityCountValue = 24;
        private const int LegacyAssignedSlotCount = 5;
        private const int MaximumCatalogSize = 64;
        // Presentation-only index; basic attacks never occupy an action-bar/catalog slot.
        private const int BasicAttackIndex = -1;

        [Header("Basic Attack")]
        [SerializeField, Min(0.1f)] private float basicAttackInterval = 1.8f;
        [SerializeField, Min(0.1f)] private float basicAttackRange = 2.4f;
        [SerializeField, Min(1f)] private float basicAttackDamage = 3f;

        [Header("References")]
        [SerializeField] private PhasebreakPlayerMovement movement;
        [SerializeField] private PhasebreakFollowCamera followCamera;
        [SerializeField] private PlayerTargeting targeting;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerBuildSystem build;
        [SerializeField] private TalentSystem talents;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Transform slashVisual;
        [SerializeField] private CombatAbilityDefinition[] abilityDefinitions;

        [Header("Combat Stats")]
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.2f;
        [SerializeField, Min(1f)] private float criticalDamageMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float globalCooldown = 0.8f;
        [SerializeField, Min(1f)] private float maximumResource = 100f;
        [SerializeField, Min(0f)] private float resourceRegeneration = 14f;

        [Header("Legacy Fallback Values")]
        [SerializeField, Min(1f)] private float strikeDamage = 5f;
        [SerializeField, Min(0.1f)] private float strikeRange = 2.4f;
        [SerializeField, Min(1f)] private float crushingDamage = 10f;
        [SerializeField, Min(0.1f)] private float crushingRange = 2.65f;
        [SerializeField, Min(0f)] private float crushingCooldown = 5f;
        [SerializeField, Min(0f)] private float crushingCost = 30f;
        [SerializeField, Range(0f, 1f)] private float crushingCriticalBonus = 0.1f;
        [SerializeField, Min(1f)] private float lungeDamage = 6f;
        [SerializeField, Min(0.1f)] private float lungeRange = 7f;
        [SerializeField, Min(0f)] private float lungeCooldown = 8f;
        [SerializeField, Min(0f)] private float lungeCost = 20f;
        [SerializeField, Min(0f)] private float lungeImpulse = 8.5f;
        [SerializeField, Range(0f, 1f)] private float lungeCriticalBonus = 0.05f;

        [Header("Impact")]
        [SerializeField, Min(0f)] private float knockback = 0.75f;
        [SerializeField, Range(0f, 0.15f)] private float normalHitStop = 0.035f;
        [SerializeField, Range(0f, 0.2f)] private float criticalHitStop = 0.075f;
        [SerializeField, Range(0f, 1f)] private float normalCameraImpulse = 0.12f;
        [SerializeField, Range(0f, 1f)] private float criticalCameraImpulse = 0.3f;
        [SerializeField] private LayerMask lineOfSightLayers = ~0;

        [Header("Soft Auto Target")]
        [SerializeField, Range(10f, 180f)] private float autoTargetCone = 110f;

        private readonly int[] charges = new int[MaximumCatalogSize];
        private readonly float[] nextChargeReadyAt = new float[MaximumCatalogSize];
        private readonly InputAction[] abilityActions = new InputAction[AbilityCountValue];
        private readonly int[] slotAbilities = new int[AbilityCountValue];
        private const string AssignmentPrefix = "Phasebreak.ActionBar.v1.slot.";
        private float globalReadyAt;
        private float currentResource;
        private Coroutine attackRoutine;
        private bool flickerActive;
        private AbilityPresentationEvent flickerPresentation;
        public bool IsFlickering => flickerActive;
        internal Transform SlashVisual => slashVisual;
        public event Action<AbilityPresentationEvent> FlickerDeparted;
        public event Action<AbilityPresentationEvent> FlickerArrived;
        private InputAction autoAttackAction;
        private bool basicAttackActive;
        private float basicAttackReadyAt;
        private AbilityPresentationEvent basicPresentation;
        public bool AutoAttackArmed { get; private set; }
        public float BasicAttackInterval => Mathf.Max(.5f, basicAttackInterval /
            Mathf.Max(.1f, build != null ? build.AttackSpeedMultiplier : 1f));
        private bool CombatInputBlocked => GameplayInputFocus.GameplayInputBlocked ||
            PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen;
        private Coroutine hitStopRoutine;
        private int riftChainCount;
        private float riftChainUntil;
        private float guardUntil;
        private float storedGuardDamage;
        private Targetable markedTarget;
        private float markUntil;
        private GameObject markVisual;
        private Material markMaterial;
        public int RiftChainCount => Time.time < riftChainUntil && (health == null || health.IsAlive) ? riftChainCount : 0;
        public float RiftChainRemaining => RiftChainCount > 0 ? Mathf.Max(0f, riftChainUntil - Time.time) : 0f;
        public float LastChainEnergyRestored { get; private set; }
        public bool LastChainChargeRestored { get; private set; }
        public event Action<int> RiftChainKill;

        public int AbilityCount => AbilityCountValue;
        public int CatalogCount => abilityDefinitions != null && abilityDefinitions.Length > 0
            ? Mathf.Min(abilityDefinitions.Length, MaximumCatalogSize) : AbilityCountValue;
        public event Action AssignmentsChanged;
        public bool IsAttacking => attackRoutine != null;
        public float CurrentResource => currentResource;
        public void SetDebugResource(float amount) => currentResource = Mathf.Clamp(amount, 0f, maximumResource);
        public float MaximumResource => maximumResource;
        public float ResourceFraction => maximumResource <= 0f ? 0f : currentResource / maximumResource;
        public float ActiveGuardReduction => Time.time < guardUntil ? .6f : 0f;
        public float CriticalChance => Mathf.Clamp01(criticalChance +
            (progression != null ? progression.CriticalChanceBonus : 0f) +
            (build != null ? build.CriticalChanceBonus : 0f));
        public float CriticalDamageMultiplier => criticalDamageMultiplier +
            (progression != null ? progression.CriticalDamageBonus : 0f) +
            (build != null ? build.CriticalDamageBonus : 0f);

        public event Action<AbilityPresentationEvent> AbilityStarted;
        public event Action<AbilityPresentationEvent> AbilityImpact;
        public event Action<AbilityPresentationEvent> AbilityCompleted;
        public event Action<string> AbilityFailed;

        public void Configure(PhasebreakPlayerMovement playerMovement, PhasebreakFollowCamera camera,
            Transform visual)
        {
            movement = playerMovement;
            followCamera = camera;
            slashVisual = visual;
            SetSlashVisible(false);
        }

        private void Awake()
        {
            movement ??= GetComponent<PhasebreakPlayerMovement>();
            followCamera ??= FindAnyObjectByType<PhasebreakFollowCamera>();
            targeting ??= GetComponent<PlayerTargeting>();
            progression ??= GetComponent<PlayerProgression>();
            build ??= GetComponent<PlayerBuildSystem>() ?? gameObject.AddComponent<PlayerBuildSystem>();
            talents ??= GetComponent<TalentSystem>() ?? gameObject.AddComponent<TalentSystem>();
            health ??= GetComponent<PlayerHealth>();
            CombatAbilityDefinition[] additions = Resources.LoadAll<CombatAbilityDefinition>("Abilities");
            if (additions.Length > 0)
            {
                Array.Sort(additions, (a, b) => string.CompareOrdinal(a?.id, b?.id));
                var catalog = new System.Collections.Generic.List<CombatAbilityDefinition>(abilityDefinitions ?? Array.Empty<CombatAbilityDefinition>());
                foreach (CombatAbilityDefinition addition in additions)
                    if (addition != null && !catalog.Exists(existing => existing != null && existing.id == addition.id))
                        catalog.Add(addition);
                abilityDefinitions = catalog.ToArray();
            }
            if (health != null) health.HitReceived += OnPlayerHit;
            LoadAssignments();
            CreateInputActions();
            currentResource = maximumResource;
            FillCharges();
            SetSlashVisible(false);
        }

        private void OnEnable()
        {
            if (health != null) health.Died += ResetCombat;
            if (abilityActions[0] == null)
                CreateInputActions();
            foreach (InputAction action in abilityActions)
                action.Enable();
            autoAttackAction.Enable();
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= ResetCombat;
            foreach (InputAction action in abilityActions)
                action?.Disable();
            autoAttackAction?.Disable();
            StopCombatRoutines();
        }

        private void OnDestroy()
        {
            if (health != null) health.HitReceived -= OnPlayerHit;
            ClearMark();
            if (markMaterial != null) Destroy(markMaterial);
            PhasebreakSettings.Unregister(autoAttackAction);
            autoAttackAction?.Dispose();
            foreach (InputAction action in abilityActions)
            {
                PhasebreakSettings.Unregister(action);
                action?.Dispose();
            }
        }

        private void CreateInputActions()
        {
            PhasebreakSettings.Unregister(autoAttackAction);
            autoAttackAction?.Dispose();
            autoAttackAction = PhasebreakSettings.Button("combat.autoAttack", "Toggle auto-attack");
            for (int i = 0; i < abilityActions.Length; i++)
            {
                PhasebreakSettings.Unregister(abilityActions[i]);
                abilityActions[i]?.Dispose();
                abilityActions[i] = PhasebreakSettings.Button($"ability.{i + 1}", GetAbilityName(i));
            }
        }

        private void Update()
        {
            if (markedTarget != null && (!markedTarget.IsAlive || Time.time >= markUntil)) ClearMark();
            float pressureRegeneration = talents != null && health != null && health.MaxHealth > 0 && health.CurrentHealth < health.MaxHealth * .5f
                ? 1f + talents.GetEffect(TalentEffect.ResourceUnderPressure) : 1f;
            if (!flickerActive) currentResource = Mathf.MoveTowards(currentResource, maximumResource,
                resourceRegeneration * pressureRegeneration * Time.deltaTime);
            UpdateCharges();
            if (GameplayInputFocus.GameplayInputBlocked || PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen)
                return;
            for (int i = 0; i < abilityActions.Length; i++)
                if (abilityActions[i].WasPressedThisFrame())
                    TryUseAssignedAbility(i);
        }

        // Run after menu/target/input Updates so an opening menu or active ability wins this frame.
        private void LateUpdate()
        {
            if (health != null && !health.IsAlive)
            {
                SetAutoAttackArmed(false);
                return;
            }
            if (CombatInputBlocked)
            {
                CancelBasicAttack();
                return;
            }
            if (autoAttackAction.WasPressedThisFrame()) SetAutoAttackArmed(!AutoAttackArmed);
            Targetable target = targeting != null ? targeting.CurrentTarget : null;
            if (!AutoAttackArmed || IsAttacking || Time.time < basicAttackReadyAt ||
                Time.time < globalReadyAt || Time.timeScale <= 0f ||
                (movement != null && movement.IsDashing) || !IsTargetValid(target, 0f, basicAttackRange)) return;

            float damage = CalculateWeaponDamage(basicAttackDamage, target, 0f, out bool critical);
            basicAttackReadyAt = Time.time + BasicAttackInterval;
            basicAttackActive = true;
            attackRoutine = StartCoroutine(PerformAttack(BasicAttackIndex, target, damage, critical));
        }

        public bool SetAutoAttackArmed(bool armed)
        {
            if (armed && (!isActiveAndEnabled || CombatInputBlocked || health != null && !health.IsAlive)) return false;
            AutoAttackArmed = armed;
            if (!armed) CancelBasicAttack();
            return true;
        }

        private void CancelBasicAttack()
        {
            if (!basicAttackActive) return;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            basicAttackActive = false;
            SetSlashVisible(false);
            AbilityCompleted?.Invoke(basicPresentation);
        }

        private float CalculateWeaponDamage(float baseDamage, Targetable target, float criticalBonus, out bool critical)
        {
            float damage = baseDamage * (progression != null ? progression.PowerMultiplier : 1f) *
                (build != null ? build.PowerMultiplier : 1f);
            if (build != null && target.GetComponent<RiftWardenBoss>() != null) damage *= build.BossDamageMultiplier;
            critical = UnityEngine.Random.value < Mathf.Clamp01(CriticalChance + criticalBonus);
            return critical ? damage * CriticalDamageMultiplier : damage;
        }

        public CombatAbilityDefinition GetAbilityDefinition(int index) =>
            IsValidIndex(index) ? Ability(index) : null;

        public string GetAbilitySourceText(int index)
        {
            CombatAbilityDefinition definition = IsValidIndex(index) ? Ability(index) : null;
            if (definition == null || definition.unlockType == AbilityUnlockType.Baseline) return "General";
            if (definition.unlockType == AbilityUnlockType.CharacterLevel)
                return $"Level {Mathf.Max(1, definition.requiredLevel)}";
            TalentNodeDefinition node = talents?.GetAbilityGrantNode(GetAbilityId(index));
            return node != null ? node.specialization.ToString() : "Specialization talent tree";
        }

        public int GetAssignedAbilityIndex(int slot) => IsValidSlot(slot) ? slotAbilities[slot] : -1;

        public bool AssignAbilityToSlot(int slot, string abilityId)
        {
            if (!IsValidSlot(slot) || string.IsNullOrWhiteSpace(abilityId)) return false;
            for (int index = 0; index < CatalogCount; index++)
            {
                if (!string.Equals(GetAbilityId(index), abilityId, StringComparison.Ordinal)) continue;
                if (!IsAbilityUnlocked(index)) return false;
                slotAbilities[slot] = index;
                PlayerPrefs.SetString(AssignmentPrefix + slot, abilityId);
                PlayerPrefs.Save();
                AssignmentsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool ClearAbilitySlot(int slot)
        {
            if (!IsValidSlot(slot)) return false;
            slotAbilities[slot] = -1;
            SaveAssignment(slot);
            PlayerPrefs.Save();
            AssignmentsChanged?.Invoke();
            return true;
        }

        public bool MoveAbilitySlot(int sourceSlot, int destinationSlot)
        {
            if (!IsValidSlot(sourceSlot) || !IsValidSlot(destinationSlot) || sourceSlot == destinationSlot)
                return false;
            int source = slotAbilities[sourceSlot];
            if (!IsValidIndex(source)) return false;
            int destination = slotAbilities[destinationSlot];
            slotAbilities[destinationSlot] = source;
            slotAbilities[sourceSlot] = destination;
            SaveAssignment(sourceSlot);
            SaveAssignment(destinationSlot);
            PlayerPrefs.Save();
            AssignmentsChanged?.Invoke();
            return true;
        }

        private void SaveAssignment(int slot)
        {
            string id = IsValidIndex(slotAbilities[slot]) ? GetAbilityId(slotAbilities[slot]) : string.Empty;
            PlayerPrefs.SetString(AssignmentPrefix + slot, id);
        }

        public AbilityState GetAssignedAbilityState(int slot)
        {
            if (!IsValidSlot(slot)) return default;
            if (!IsValidIndex(slotAbilities[slot]))
                return new AbilityState(string.Empty, GetKey(slot), 0f, 0f, 0f, false, 0, 0, null, false);
            AbilityState ability = GetAbilityState(slotAbilities[slot]);
            return new AbilityState(ability.Name, GetKey(slot), ability.CooldownRemaining,
                ability.CooldownDuration, ability.ResourceCost, ability.IsUsable,
                ability.Charges, ability.MaximumCharges, ability.Icon, true, ability.RechargeRemaining);
        }

        public bool TryUseAssignedAbility(int slot) => IsValidSlot(slot) && TryUseAbility(slotAbilities[slot]);

        private void LoadAssignments()
        {
            for (int slot = 0; slot < slotAbilities.Length; slot++)
            {
                slotAbilities[slot] = slot < LegacyAssignedSlotCount ? Mathf.Min(slot, CatalogCount - 1) : -1;
                string key = AssignmentPrefix + slot;
                if (!PlayerPrefs.HasKey(key)) continue;
                string savedId = PlayerPrefs.GetString(key, string.Empty);
                if (string.IsNullOrEmpty(savedId)) { slotAbilities[slot] = -1; continue; }
                for (int index = 0; index < CatalogCount; index++)
                    if (string.Equals(GetAbilityId(index), savedId, StringComparison.Ordinal))
                    { slotAbilities[slot] = index; break; }
            }
        }

        public AbilityState GetAbilityState(int index)
        {
            if (!IsValidIndex(index))
                return default;
            int maximumCharges = GetMaximumCharges(index);
            float abilityRemaining = charges[index] < maximumCharges
                ? Mathf.Max(0f, nextChargeReadyAt[index] - Time.time) : 0f;
            float globalRemaining = UsesGlobalCooldown(index)
                ? Mathf.Max(0f, globalReadyAt - Time.time) : 0f;
            float blockingRecharge = charges[index] <= 0 ? abilityRemaining : 0f;
            bool globalIsLonger = globalRemaining > blockingRecharge;
            return new AbilityState(GetAbilityName(index), GetKey(index),
                globalIsLonger ? globalRemaining : blockingRecharge,
                globalIsLonger ? globalCooldown : GetCooldown(index), GetCost(index),
                CanUseAbility(index), charges[index], maximumCharges, GetAbilityIcon(index), true,
                abilityRemaining);
        }

        public bool TryUseAbility(int index)
        {
            if (!ValidateCommonRequirements(index, true))
                return false;
            AbilityExecutionType type = GetExecutionType(index);
            if (type == AbilityExecutionType.Guard || type == AbilityExecutionType.Area)
            {
                ConsumeAbility(index);
                attackRoutine = StartCoroutine(type == AbilityExecutionType.Guard
                    ? PerformGuard(index) : PerformArea(index));
                return true;
            }
            if (type is AbilityExecutionType.PhaseDash or AbilityExecutionType.Quickstep)
            {
                bool canMove = movement != null && (type == AbilityExecutionType.Quickstep
                    ? movement.CanQuickstepNow : movement.CanDashNow);
                if (!canMove)
                {
                    Fail(type == AbilityExecutionType.Quickstep
                        ? "Quickstep requires solid ground" : "Phase Dash cannot be used right now");
                    return false;
                }
                ConsumeAbility(index);
                if (type == AbilityExecutionType.PhaseDash && build != null &&
                    build.Specialization == Specialization.Riftblade)
                    talents?.NotifyAbilityUsed(true);
                attackRoutine = StartCoroutine(type == AbilityExecutionType.Quickstep
                    ? PerformQuickstep(index) : PerformDash(index));
                return true;
            }

            Targetable target = type == AbilityExecutionType.FlickerStrike
                ? targeting?.CurrentTarget : ResolveTarget(index);
            if (type == AbilityExecutionType.FlickerStrike && target != null &&
                !IsTargetValid(target, 0f, GetRange(index))) target = null;
            if (target == null)
            {
                Fail(type == AbilityExecutionType.Charge ? "No charge target in range" : "No valid target in range");
                return false;
            }

            if (type == AbilityExecutionType.FlickerStrike)
            {
                if (movement == null || !movement.TryFindFlickerPosition(target, 180f, out _) &&
                    !TryFlickerDestination(target, 0, out _)) return Reject(true, "No safe Flicker destination");
                if (!movement.BeginAbilityMovement()) return Reject(true, "Movement ability is active");
                ConsumeAbility(index);
                talents?.NotifyAbilityUsed(false);
                attackRoutine = StartCoroutine(PerformFlicker(index, target));
                return true;
            }

            float talentDamage = talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityDamage, GetAbilityId(index)) + talents.RhythmDamageBonus : 0f;
            if (index == 1 && talents != null) talentDamage += talents.HeavyImpactBonus;
            float damage = CalculateWeaponDamage(GetDamage(index) * (1f + talentDamage), target,
                GetCriticalBonus(index), out bool critical);

            ConsumeAbility(index);
            talents?.NotifyAbilityUsed(type is AbilityExecutionType.PhaseDash or AbilityExecutionType.PhaseLunge or AbilityExecutionType.Charge);
            if (index == 1) talents?.ConsumeHeavyImpact();
            attackRoutine = StartCoroutine(type == AbilityExecutionType.Charge
                ? PerformCharge(index, target, damage, critical)
                : PerformAttack(index, target, damage, critical));
            return true;
        }

        public void ResetCombat()
        {
            StopCombatRoutines();
            talents?.ResetTransientEffects();
            Array.Clear(nextChargeReadyAt, 0, nextChargeReadyAt.Length);
            globalReadyAt = 0f;
            currentResource = maximumResource;
            FillCharges();
        }

        public int[] CaptureMaximumCharges()
        {
            int[] maximum = new int[CatalogCount];
            for (int i = 0; i < maximum.Length; i++) maximum[i] = GetMaximumCharges(i);
            return maximum;
        }

        public void ReconcileAbilityModifiers(int[] previousMaximumCharges)
        {
            if (previousMaximumCharges == null || previousMaximumCharges.Length != CatalogCount) return;
            for (int i = 0; i < CatalogCount; i++)
            {
                int maximum = GetMaximumCharges(i);
                charges[i] = Mathf.Min(charges[i], maximum);
                if (charges[i] >= maximum) nextChargeReadyAt[i] = 0f;
                else if (maximum != previousMaximumCharges[i] && nextChargeReadyAt[i] <= Time.time)
                    nextChargeReadyAt[i] = Time.time + GetCooldown(i);
            }
            riftChainCount = 0;
            riftChainUntil = 0f;
            LastChainEnergyRestored = 0f;
            LastChainChargeRestored = false;
        }

        private bool ValidateCommonRequirements(int index, bool report)
        {
            if (health != null && !health.IsAlive)
                return Reject(report, "Cannot attack while defeated");
            if (!IsValidIndex(index))
                return false;
            if (!IsAbilityUnlocked(index))
                return Reject(report, "Unlock this ability in its talent tree");
            if (IsAttacking && !basicAttackActive)
                return Reject(report, "Another ability is already active");
            if (UsesGlobalCooldown(index) && Time.time < globalReadyAt)
                return Reject(report, "Global cooldown");
            if (charges[index] <= 0)
                return Reject(report, $"{GetAbilityName(index)} is recharging");
            if (currentResource < GetCost(index))
                return Reject(report, "Not enough Energy");
            return true;
        }

        private bool CanUseAbility(int index)
        {
            if (!ValidateCommonRequirements(index, false))
                return false;
            if (GetExecutionType(index) is AbilityExecutionType.Guard or AbilityExecutionType.Area)
                return true;
            if (GetExecutionType(index) == AbilityExecutionType.PhaseDash)
                return movement != null && movement.CanDashNow;
            if (GetExecutionType(index) == AbilityExecutionType.Quickstep)
                return movement != null && movement.CanQuickstepNow;
            Targetable target = targeting != null ? targeting.CurrentTarget : null;
            return target != null && IsTargetValid(target, GetMinimumRange(index), GetRange(index));
        }

        private bool Reject(bool report, string message)
        {
            if (report)
                Fail(message);
            return false;
        }

        private void Fail(string message) => AbilityFailed?.Invoke(message);

        private Targetable ResolveTarget(int index)
        {
            if (targeting == null)
                return null;
            Targetable target = targeting.CurrentTarget;
            if (target != null && IsTargetValid(target, GetMinimumRange(index), GetRange(index)))
                return target;
            target = targeting.TrySelectNearestInFront(GetRange(index), autoTargetCone);
            return target != null && IsTargetValid(target, GetMinimumRange(index), GetRange(index)) ? target : null;
        }

        private bool IsTargetValid(Targetable target, float minimumRange, float maximumRange)
        {
            if (target == null || !target.isActiveAndEnabled || !target.IsHostile || !target.IsAlive ||
                target.GetComponent<ICombatTarget>() == null)
                return false;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            CharacterController targetController = target.GetComponent<CharacterController>();
            Vector3 destination = targetController != null ? targetController.bounds.center :
                target.NameplateWorldPosition - Vector3.up * 0.65f;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance > maximumRange || FlatDistance(transform.position, target.transform.position) < minimumRange || distance < 0.01f)
                return false;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction / distance, distance + 0.2f,
                lineOfSightLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform))
                    continue;
                return hit.collider.GetComponentInParent<Targetable>() == target;
            }
            return true;
        }

        private bool TryFlickerDestination(Targetable target, int strike, out Vector3 position)
        {
            position = transform.position;
            float preferred = strike % 3 == 0 ? 180f : strike % 3 == 1 ? 35f : -125f;
            float[] offsets = { 0f, 35f, -35f, 65f, -65f };
            foreach (float offset in offsets)
                if (movement.TryFindFlickerPosition(target, preferred + offset, out position) &&
                    (strike == 0 || FlatDistance(position, transform.position) > .8f)) return true;
            return false;
        }

        private IEnumerator PerformFlicker(int index, Targetable target)
        {
            CombatAbilityDefinition definition = Ability(index);
            flickerActive = true;
            flickerPresentation = Event(index, .6f, false);
            try
            {
                // Establish coroutine ownership before callbacks can disable or kill the player.
                yield return null;
                AbilityStarted?.Invoke(flickerPresentation);
                if (!flickerActive || !isActiveAndEnabled || target == null) yield break;
                followCamera?.BeginFlicker(target.transform);
                for (int strike = 0; strike < Mathf.Max(1, definition.flickerCount); strike++)
                {
                    if (!IsTargetValid(target, 0f, GetRange(index)) || health != null && !health.IsAlive ||
                        !TryFlickerDestination(target, strike, out Vector3 destination)) break;
                    float continuingCost = Mathf.Max(0f, definition.flickerContinuingCost);
                    if (strike > 0 && currentResource < continuingCost) break;
                    bool final = strike == Mathf.Max(1, definition.flickerCount) - 1;
                    float transit = Mathf.Max(.02f, definition.flickerTransit);
                    float interval = Mathf.Max(.04f, definition.flickerInterval - transit) +
                        (final ? Mathf.Max(0f, definition.recovery) : 0f);
                    float modifier = 1f + (talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityDamage,
                        GetAbilityId(index)) + talents.RhythmDamageBonus : 0f);
                    float damage = CalculateWeaponDamage(definition.damage * modifier *
                        (final ? definition.flickerFinisherMultiplier : 1f), target, GetCriticalBonus(index), out bool critical);
                    var beat = new AbilityPresentationEvent(index, definition.displayName, AbilityExecutionType.FlickerStrike,
                        interval, critical, strike, final);
                    health?.GrantInvulnerability(transit + interval + .04f);
                    FlickerDeparted?.Invoke(beat);
                    if (!flickerActive || !isActiveAndEnabled) yield break;
                    float until = Time.time + transit;
                    while (Time.time < until && target != null && target.isActiveAndEnabled && target.IsAlive) yield return null;
                    if (!IsTargetValid(target, 0f, GetRange(index)) || !TryFlickerDestination(target, strike, out destination)) break;
                    if (!movement.TeleportAbility(destination, target.transform.position - destination)) break;
                    if (strike > 0) currentResource = Mathf.Max(0f, currentResource - continuingCost);
                    FlickerArrived?.Invoke(beat);
                    if (!flickerActive || !isActiveAndEnabled) yield break;
                    // Allow the weapon windup to read before contact; no movement occurs during this beat.
                    until = Time.time + interval * .4f;
                    while (Time.time < until && target != null && target.isActiveAndEnabled && target.IsAlive) yield return null;
                    if (!IsTargetValid(target, 0f, GetRange(index)) || !IsFlickerInMeleeReach(target)) break;
                    Vector3 facing = target.transform.position - transform.position;
                    facing.y = 0f;
                    if (facing.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(facing);
                    SetSlashVisible(true);
                    if (slashVisual != null) slashVisual.localScale = Vector3.one * (final ? 1.45f : .9f);
                    ApplyHit(index, target, damage, critical, false, final);
                    AbilityImpact?.Invoke(beat);
                    if (target == null || !target.isActiveAndEnabled || !target.IsAlive)
                    {
                        // Finish this contact's visual beat, without another relocation or attack.
                        yield return new WaitForSeconds(.06f);
                        break;
                    }
                    until = Time.time + interval * .6f;
                    while (Time.time < until && target != null && target.isActiveAndEnabled && target.IsAlive) yield return null;
                    SetSlashVisible(false);
                }
            }
            finally { EndFlicker(); attackRoutine = null; }
        }

        private void EndFlicker()
        {
            if (!flickerActive) return;
            flickerActive = false;
            movement?.EndAbilityMovement();
            followCamera?.EndFlicker();
            SetSlashVisible(false);
            AbilityCompleted?.Invoke(flickerPresentation);
        }

        private bool IsFlickerInMeleeReach(Targetable target)
        {
            foreach (Collider body in target.GetComponentsInChildren<Collider>())
                if (body.enabled && !body.isTrigger &&
                    Vector3.Distance(transform.position, body.ClosestPoint(transform.position)) <= 1.65f) return true;
            return false;
        }

        private IEnumerator PerformDash(int index)
        {
            AbilityPresentationEvent presentation = Event(index, GetMovementDuration(index), false);
            AbilityStarted?.Invoke(presentation);
            if (!movement.TryDash(GetMovementDistance(index), GetMovementDuration(index)))
            {
                RefundCharge(index);
                AbilityCompleted?.Invoke(presentation);
                attackRoutine = null;
                yield break;
            }
            while (movement.IsDashing)
                yield return null;
            AbilityCompleted?.Invoke(presentation);
            attackRoutine = null;
        }

        private IEnumerator PerformQuickstep(int index)
        {
            AbilityPresentationEvent presentation = Event(index, GetMovementDuration(index), false);
            AbilityStarted?.Invoke(presentation);
            if (!movement.TryQuickstep(GetMovementDistance(index), GetMovementDuration(index)))
            {
                RefundCharge(index);
                AbilityCompleted?.Invoke(presentation);
                attackRoutine = null;
                yield break;
            }
            health?.GrantInvulnerability(0.15f);
            followCamera?.AddImpulse(0.07f);
            while (movement.IsDashing)
                yield return null;
            AbilityCompleted?.Invoke(presentation);
            attackRoutine = null;
        }

        private IEnumerator PerformGuard(int index)
        {
            AbilityPresentationEvent presentation = Event(index, 2.5f, false);
            AbilityStarted?.Invoke(presentation);
            guardUntil = Time.time + 2.5f;
            yield return new WaitForSeconds(.2f);
            AbilityImpact?.Invoke(presentation);
            AbilityCompleted?.Invoke(presentation);
            attackRoutine = null;
        }

        private IEnumerator PerformArea(int index)
        {
            AbilityPresentationEvent presentation = Event(index, GetWindup(index) + GetRecovery(index), false);
            AbilityStarted?.Invoke(presentation);
            yield return new WaitForSeconds(GetWindup(index));
            float power = (GetDamage(index) + storedGuardDamage * .8f) *
                (progression != null ? progression.PowerMultiplier : 1f) *
                (build != null ? build.PowerMultiplier : 1f) *
                (1f + (talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityDamage, GetAbilityId(index)) : 0f));
            storedGuardDamage = 0f;
            foreach (Targetable other in Targetable.ActiveTargets)
            {
                if (other == null || !other.IsHostile || !other.IsAlive ||
                    FlatDistance(transform.position, other.transform.position) > GetRange(index)) continue;
                other.GetComponent<ICombatTarget>()?.ReceiveHit(new CombatHit(other.transform.position + Vector3.up,
                    (other.transform.position - transform.position).normalized, power, knockback * 2f,
                    false, false, GetAbilityName(index)));
            }
            AbilityImpact?.Invoke(presentation);
            yield return new WaitForSeconds(GetRecovery(index));
            AbilityCompleted?.Invoke(presentation);
            attackRoutine = null;
        }

        private IEnumerator PerformCharge(int index, Targetable target, float damage, bool critical)
        {
            float duration = GetMovementDuration(index);
            AbilityPresentationEvent presentation = Event(index, duration + GetRecovery(index), critical);
            AbilityStarted?.Invoke(presentation);
            if (movement == null || !movement.BeginAbilityMovement())
            {
                RefundCharge(index);
                AbilityCompleted?.Invoke(presentation);
                attackRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && target != null && target.IsAlive)
            {
                Vector3 delta = target.transform.position - transform.position;
                delta.y = 0f;
                if (delta.magnitude <= GetStopDistance(index))
                    break;
                Vector3 direction = delta.normalized;
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                float speed = Mathf.Max(8f, GetRange(index) / duration);
                CollisionFlags flags = movement.MoveAbility(direction * (speed * Time.deltaTime));
                if ((flags & CollisionFlags.Sides) != 0)
                    break;
                elapsed += Time.deltaTime;
                yield return null;
            }
            movement.EndAbilityMovement();

            if (target != null && target.IsAlive &&
                FlatDistance(transform.position, target.transform.position) <= GetStopDistance(index) + 1.2f)
            {
                ApplyHit(index, target, damage, critical, true);
                AbilityImpact?.Invoke(presentation);
            }
            else
                Fail("Charge was blocked");

            yield return new WaitForSeconds(GetRecovery(index));
            AbilityCompleted?.Invoke(presentation);
            attackRoutine = null;
        }

        private IEnumerator PerformAttack(int index, Targetable target, float damage, bool critical)
        {
            bool basic = index == BasicAttackIndex;
            int chainBefore = RiftChainCount;
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            if (!basic) movement?.CancelDashForAttack();
            float attackSpeed = build != null ? build.AttackSpeedMultiplier : 1f;
            float windup = basic ? .3f : GetWindup(index) / attackSpeed;
            float recovery = basic ? .35f : GetRecovery(index) / attackSpeed;
            AbilityPresentationEvent presentation = Event(index, windup + recovery + 0.12f, critical);
            if (basic) basicPresentation = presentation;
            AbilityStarted?.Invoke(presentation);

            bool riftLunge = GetExecutionType(index) == AbilityExecutionType.PhaseLunge &&
                build != null && build.Specialization == Specialization.Riftblade;
            if (riftLunge && movement != null && movement.BeginAbilityMovement())
            {
                float elapsed = 0f;
                float duration = Mathf.Max(.08f, GetMovementDuration(index));
                while (target != null && target.IsAlive && elapsed < duration)
                {
                    Vector3 delta = target.transform.position - transform.position;
                    delta.y = 0f;
                    float remaining = delta.magnitude - GetStopDistance(index);
                    if (remaining <= .05f) break;
                    CollisionFlags flags = movement.MoveAbility(delta.normalized *
                        Mathf.Min(remaining, GetRange(index) / duration * Time.deltaTime));
                    elapsed += Time.deltaTime;
                    if ((flags & CollisionFlags.Sides) != 0) break;
                    yield return null;
                }
                movement.EndAbilityMovement();
            }
            else if (!riftLunge && GetExecutionType(index) == AbilityExecutionType.PhaseLunge && toTarget.sqrMagnitude > 0.01f)
                movement?.AddCombatImpulse(toTarget.normalized * GetImpulse(index));
            else if (!riftLunge && !basic)
                movement?.AddCombatImpulse(transform.forward * GetImpulse(index));

            yield return new WaitForSeconds(windup);
            if (basic && (!AutoAttackArmed || CombatInputBlocked || health != null && !health.IsAlive ||
                targeting == null || targeting.CurrentTarget != target || !IsTargetValid(target, 0f, basicAttackRange)))
            {
                basicAttackActive = false;
                attackRoutine = null;
                AbilityCompleted?.Invoke(presentation);
                yield break;
            }
            SetSlashVisible(true);
            if (slashVisual != null)
                slashVisual.localScale = Vector3.one * (critical ? 1.55f : index == 1 ? 1.3f : 1f);
            if (target != null && target.IsAlive &&
                FlatDistance(transform.position, target.transform.position) <= (riftLunge ? GetStopDistance(index) + .6f : GetRange(index) + 1f) &&
                (!riftLunge || IsTargetValid(target, 0f, GetRange(index))))
            {
                ApplyHit(index, target, damage, critical,
                    GetExecutionType(index) == AbilityExecutionType.PhaseLunge);
                AbilityImpact?.Invoke(presentation);
            }
            yield return new WaitForSeconds(0.12f);
            SetSlashVisible(false);
            yield return new WaitForSeconds(riftLunge && RiftChainCount > chainBefore && LastChainChargeRestored ? Mathf.Min(recovery, .04f) : recovery);
            AbilityCompleted?.Invoke(presentation);
            if (basic) basicAttackActive = false;
            attackRoutine = null;
        }

        private void ApplyHit(int index, Targetable target, float damage, bool critical, bool mobilityHit, bool finisher = false)
        {
            ICombatTarget combatTarget = target.GetComponent<ICombatTarget>();
            if (combatTarget == null)
                return;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;
            direction.Normalize();
            AbilitySpecialEffect special = Ability(index)?.specialEffect ?? AbilitySpecialEffect.None;
            if (special == AbilitySpecialEffect.ExecuteHeal && target.GetComponent<MeleeEnemy>() is MeleeEnemy enemy &&
                enemy.MaxHealth > 0 && enemy.CurrentHealth <= enemy.MaxHealth * .35f)
                damage *= 1.75f;
            if (special == AbilitySpecialEffect.MarkDetonate && target == markedTarget && Time.time < markUntil)
            {
                damage *= 1.5f;
                ClearMark();
                PerformCleave(target, damage * .5f, critical, 4f);
            }
            combatTarget.ReceiveHit(new CombatHit(target.transform.position + Vector3.up * 1.15f,
                direction, damage, knockback * (index == 1 ? 1.5f : 1f), mobilityHit, critical,
                GetAbilityName(index)));
            if (special == AbilitySpecialEffect.Mark) ShowMark(target);
            if (special == AbilitySpecialEffect.ExecuteHeal && !target.IsAlive) health?.Heal(2);
            if (special == AbilitySpecialEffect.Cleave) PerformCleave(target, damage * .55f, critical, GetRange(index) + 1f);

            if (!flickerActive && critical && build != null && build.CritEnergyRestore > 0f)
                currentResource = Mathf.Min(maximumResource, currentResource + build.CritEnergyRestore);
            if (!flickerActive && critical) talents?.NotifyCriticalHit();
            if (index == 1 && build != null && build.CrushingBlowCleave)
                PerformCleave(target, damage * 0.6f, critical, GetRange(index) + 1.2f);
            bool riftblade = build != null && build.Specialization == Specialization.Riftblade;
            if (riftblade && mobilityHit && !target.IsAlive)
            {
                float before = currentResource;
                currentResource = Mathf.Min(maximumResource, currentResource + build.TeleportKillRecovery);
                LastChainEnergyRestored = currentResource - before;
                bool restore = (GetExecutionType(index) == AbilityExecutionType.PhaseLunge && build.TeleportKillRestoresCharge) ||
                    (talents != null && talents.HasEffect(TalentEffect.MobilityKillCircuit));
                int previousCharges = charges[index];
                if (restore) RefundCharge(index);
                LastChainChargeRestored = charges[index] > previousCharges;
                if (LastChainChargeRestored) globalReadyAt = Time.time;
                riftChainCount = RiftChainCount + 1;
                riftChainUntil = Time.time + 3f;
                RiftChainKill?.Invoke(riftChainCount);
                followCamera?.AddImpulse(.04f * Mathf.Min(riftChainCount, 4));
            }
            else if (!riftblade && GetExecutionType(index) == AbilityExecutionType.PhaseLunge && !target.IsAlive &&
                build != null && build.TeleportKillRecovery > 0f)
            {
                currentResource = Mathf.Min(maximumResource, currentResource + build.TeleportKillRecovery);
                charges[index] = GetMaximumCharges(index);
                nextChargeReadyAt[index] = 0f;
            }
            if (!riftblade && mobilityHit && !target.IsAlive && talents != null && talents.HasEffect(TalentEffect.MobilityKillCircuit))
            {
                charges[index] = GetMaximumCharges(index);
                nextChargeReadyAt[index] = 0f;
            }
            followCamera?.AddImpulse(flickerActive ? (finisher ? .16f : .06f) : critical ? criticalCameraImpulse : normalCameraImpulse);
            float hitStop = flickerActive ? (finisher ? .025f : .012f) : critical ? criticalHitStop : normalHitStop;
            if (hitStop > 0f && hitStopRoutine == null)
                hitStopRoutine = StartCoroutine(HitStop(hitStop));
        }

        private IEnumerator HitStop(float duration)
        {
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = previousScale;
            hitStopRoutine = null;
        }

        private void StopCombatRoutines()
        {
            EndFlicker();
            SetAutoAttackArmed(false);
            basicAttackReadyAt = 0f;
            riftChainCount = 0;
            riftChainUntil = 0f;
            LastChainEnergyRestored = 0f;
            LastChainChargeRestored = false;
            guardUntil = 0f;
            storedGuardDamage = 0f;
            ClearMark();
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
                Time.timeScale = 1f;
            }
            movement?.EndAbilityMovement();
            SetSlashVisible(false);
        }

        private void OnPlayerHit(Vector3 direction, int damage)
        {
            if (Time.time < guardUntil) storedGuardDamage = Mathf.Min(30f, storedGuardDamage + damage);
        }

        private void ShowMark(Targetable target)
        {
            ClearMark();
            markedTarget = target;
            markUntil = Time.time + 6f;
            markVisual = new GameObject("Rift Mark Ring");
            markVisual.transform.SetParent(target.transform, false);
            markVisual.transform.localPosition = Vector3.up * .08f;
            LineRenderer line = markVisual.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 25;
            line.startWidth = line.endWidth = .045f;
            line.startColor = line.endColor = new Color(.64f, .35f, .9f);
            if (markMaterial == null)
                markMaterial = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            line.sharedMaterial = markMaterial;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * .85f, 0f, Mathf.Sin(angle) * .85f));
            }
        }

        private void ClearMark()
        {
            markedTarget = null;
            if (markVisual != null) Destroy(markVisual);
            markVisual = null;
        }

        private void SetSlashVisible(bool visible)
        {
            if (slashVisual == null)
                return;
            slashVisual.gameObject.SetActive(visible);
            if (!visible)
            {
                slashVisual.localRotation = Quaternion.identity;
                slashVisual.localScale = Vector3.one;
            }
        }

        private void PerformCleave(Targetable primary, float damage, bool critical, float radius)
        {
            foreach (Targetable other in Targetable.ActiveTargets)
            {
                if (other == null || other == primary || !other.IsHostile || !other.IsAlive ||
                    FlatDistance(transform.position, other.transform.position) > radius)
                    continue;
                ICombatTarget victim = other.GetComponent<ICombatTarget>();
                if (victim == null)
                    continue;
                Vector3 direction = (other.transform.position - transform.position).normalized;
                victim.ReceiveHit(new CombatHit(other.transform.position + Vector3.up, direction, damage,
                    knockback, false, critical, "Crushing Blow: Fracture"));
            }
        }

        private void FillCharges()
        {
            for (int i = 0; i < CatalogCount; i++)
                charges[i] = GetMaximumCharges(i);
        }

        private void UpdateCharges()
        {
            for (int i = 0; i < CatalogCount; i++)
            {
                int maximum = GetMaximumCharges(i);
                charges[i] = Mathf.Min(charges[i], maximum);
                if (charges[i] >= maximum || Time.time < nextChargeReadyAt[i])
                    continue;
                charges[i]++;
                nextChargeReadyAt[i] = charges[i] < maximum ? Time.time + GetCooldown(i) : 0f;
            }
        }

        private void ConsumeAbility(int index)
        {
            // A validated active ability preempts the filler swing before starting its routine.
            CancelBasicAttack();
            currentResource = Mathf.Max(0f, currentResource - GetCost(index));
            int maximum = GetMaximumCharges(index);
            if (charges[index] == maximum)
                nextChargeReadyAt[index] = Time.time + GetCooldown(index);
            charges[index] = Mathf.Max(0, charges[index] - 1);
            if (UsesGlobalCooldown(index))
                globalReadyAt = Time.time + globalCooldown /
                    (build != null ? build.AttackSpeedMultiplier : 1f);
        }

        private void RefundCharge(int index)
        {
            charges[index] = Mathf.Min(GetMaximumCharges(index), charges[index] + 1);
            if (charges[index] >= GetMaximumCharges(index))
                nextChargeReadyAt[index] = 0f;
        }

        private AbilityPresentationEvent Event(int index, float duration, bool critical) =>
            new(index, GetAbilityName(index), GetExecutionType(index), duration, critical);
        private bool IsValidSlot(int slot) => slot >= 0 && slot < AbilityCountValue;
        private bool IsValidIndex(int index) => index >= 0 && index < CatalogCount;
        public bool IsAbilityUnlocked(int index)
        {
            if (!IsValidIndex(index)) return false;
            CombatAbilityDefinition definition = Ability(index);
            if (definition == null) return index < AbilityCountValue;
            return definition.unlockType switch
            {
                AbilityUnlockType.CharacterLevel => progression != null &&
                    progression.Level >= Mathf.Max(1, definition.requiredLevel),
                AbilityUnlockType.Talent => talents != null &&
                    talents.IsAbilityUnlocked(GetAbilityId(index), false),
                _ => true
            };
        }

        public string GetAbilityUnlockText(int index)
        {
            CombatAbilityDefinition definition = IsValidIndex(index) ? Ability(index) : null;
            if (definition == null || IsAbilityUnlocked(index)) return string.Empty;
            return definition.unlockType == AbilityUnlockType.CharacterLevel
                ? $"Unlocks at level {Mathf.Max(1, definition.requiredLevel)}"
                : "Unlock in its specialization tree";
        }
        private CombatAbilityDefinition Ability(int index) =>
            abilityDefinitions != null && index >= 0 && index < abilityDefinitions.Length
                ? abilityDefinitions[index] : null;
        private string GetAbilityId(int index) => !string.IsNullOrWhiteSpace(Ability(index)?.id) ? Ability(index).id : index switch
        {
            1 => "crushing-blow", 2 => "phase-lunge", 3 => "phase-dash", 4 => "rift-charge", _ => "strike"
        };
        private string GetAbilityName(int index) => index == BasicAttackIndex ? "Basic Attack" : Ability(index) != null ? Ability(index).displayName : index switch
        {
            1 => "Crushing Blow", 2 => "Phase Lunge", 3 => "Phase Dash", 4 => "Rift Charge", _ => "Strike"
        };
        private Sprite GetAbilityIcon(int index)
        {
            Sprite icon = Ability(index)?.icon;
            return icon != null ? icon : PhasebreakIconCatalog.Current?.fallbackAbility;
        }
        private string GetKey(int index) => index >= 0 && index < AbilityCountValue
            ? PhasebreakSettings.DisplayCompact($"ability.{index + 1}") : string.Empty;
        private AbilityExecutionType GetExecutionType(int index) => Ability(index) != null
            ? Ability(index).executionType : index switch
            {
                2 => AbilityExecutionType.PhaseLunge,
                3 => AbilityExecutionType.PhaseDash,
                4 => AbilityExecutionType.Charge,
                _ => AbilityExecutionType.Melee
            };
        private float GetDamage(int index) => Ability(index) != null ? Ability(index).damage : index switch
        {
            1 => crushingDamage, 2 => lungeDamage, 3 => 0f, 4 => 7f, _ => strikeDamage
        };
        private float GetRange(int index) => Ability(index) != null ? Ability(index).range : index switch
        {
            1 => crushingRange, 2 => lungeRange, 3 => 0f, 4 => 14f, _ => strikeRange
        };
        private float GetMinimumRange(int index) => Ability(index) != null ? Ability(index).minimumRange : index == 4 ? 4f : 0f;
        private float GetCooldown(int index)
        {
            float value = Ability(index) != null ? Ability(index).cooldown : index switch
            {
                1 => crushingCooldown, 2 => lungeCooldown, 3 => 6f, 4 => 10f, _ => 0f
            };
            if (GetExecutionType(index) == AbilityExecutionType.Quickstep) return value;
            if (index == 2 && build != null) value *= build.PhaseLungeCooldownMultiplier;
            else if (talents != null) value *= Mathf.Clamp01(1f - talents.GetAbilityEffect(TalentEffect.AbilityCooldown, GetAbilityId(index)));
            return value;
        }
        private float GetCost(int index)
        {
            float value = Ability(index) != null ? Ability(index).resourceCost : index switch { 1 => crushingCost, 2 => lungeCost, 4 => 15f, _ => 0f };
            if (GetExecutionType(index) == AbilityExecutionType.Quickstep) return value;
            return value * Mathf.Clamp01(1f - (talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityCost, GetAbilityId(index)) : 0f));
        }
        private float GetCriticalBonus(int index) => Ability(index) != null ? Ability(index).criticalBonus : index switch
        {
            1 => crushingCriticalBonus, 2 => lungeCriticalBonus, _ => 0f
        };
        private float GetImpulse(int index) => Ability(index) != null ? Ability(index).impulse : index switch
        {
            1 => 2.8f, 2 => lungeImpulse, _ => 1.7f
        };
        private float GetWindup(int index) => Ability(index) != null ? Ability(index).windup : index switch
        {
            1 => 0.28f, 2 => 0.12f, _ => 0.18f
        };
        private float GetRecovery(int index) => Ability(index) != null ? Ability(index).recovery : index switch
        {
            1 => 0.3f, 2 => 0.16f, 4 => 0.22f, _ => 0.14f
        };
        private float GetMovementDuration(int index) => Ability(index) != null ? Ability(index).movementDuration : index == 4 ? 0.42f : 0.16f;
        private float GetMovementDistance(int index) => Ability(index) != null && Ability(index).movementDistance > 0f
            ? Ability(index).movementDistance : 4.5f;
        private float GetStopDistance(int index) => Ability(index) != null ? Ability(index).stopDistance : 1.35f;
        private int GetMaximumCharges(int index)
        {
            int value = Ability(index) != null ? Ability(index).maximumCharges : index == 3 ? 2 : 1;
            if (index == 2 && build != null)
                value += build.PhaseLungeExtraCharges;
            if (index != 2 && talents != null)
                value += Mathf.RoundToInt(talents.GetAbilityEffect(TalentEffect.AbilityExtraCharge, GetAbilityId(index)));
            return Mathf.Clamp(value, 1, 3);
        }
        private bool UsesGlobalCooldown(int index) => Ability(index) != null
            ? Ability(index).usesGlobalCooldown : index != 3;
        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
