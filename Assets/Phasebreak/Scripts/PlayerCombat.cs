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

        public AbilityState(string name, string key, float cooldownRemaining, float cooldownDuration,
            float resourceCost, bool isUsable, int charges, int maximumCharges, Sprite icon)
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
        }
    }

    public readonly struct AbilityPresentationEvent
    {
        public readonly int Index;
        public readonly string Name;
        public readonly AbilityExecutionType Type;
        public readonly float Duration;
        public readonly bool Critical;

        public AbilityPresentationEvent(int index, string name, AbilityExecutionType type,
            float duration, bool critical)
        {
            Index = index;
            Name = name;
            Type = type;
            Duration = duration;
            Critical = critical;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const int AbilityCountValue = 5;

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

        private readonly int[] charges = new int[AbilityCountValue];
        private readonly float[] nextChargeReadyAt = new float[AbilityCountValue];
        private readonly InputAction[] abilityActions = new InputAction[AbilityCountValue];
        private readonly int[] slotAbilities = new int[AbilityCountValue];
        private const string AssignmentPrefix = "Phasebreak.ActionBar.v1.slot.";
        private float globalReadyAt;
        private float currentResource;
        private Coroutine attackRoutine;
        private Coroutine hitStopRoutine;
        private int riftChainCount;
        private float riftChainUntil;
        public int RiftChainCount => Time.time < riftChainUntil && (health == null || health.IsAlive) ? riftChainCount : 0;
        public float RiftChainRemaining => RiftChainCount > 0 ? Mathf.Max(0f, riftChainUntil - Time.time) : 0f;
        public float LastChainEnergyRestored { get; private set; }
        public bool LastChainChargeRestored { get; private set; }
        public event Action<int> RiftChainKill;

        public int AbilityCount => AbilityCountValue;
        public int CatalogCount => abilityDefinitions != null && abilityDefinitions.Length > 0
            ? Mathf.Min(abilityDefinitions.Length, AbilityCountValue) : AbilityCountValue;
        public event Action AssignmentsChanged;
        public bool IsAttacking => attackRoutine != null;
        public float CurrentResource => currentResource;
        public void SetDebugResource(float amount) => currentResource = Mathf.Clamp(amount, 0f, maximumResource);
        public float MaximumResource => maximumResource;
        public float ResourceFraction => maximumResource <= 0f ? 0f : currentResource / maximumResource;
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
            LoadAssignments();
            CreateInputActions();
            currentResource = maximumResource;
            FillCharges();
            SetSlashVisible(false);
        }

        private void OnEnable()
        {
            if (abilityActions[0] == null)
                CreateInputActions();
            foreach (InputAction action in abilityActions)
                action.Enable();
        }

        private void OnDisable()
        {
            foreach (InputAction action in abilityActions)
                action?.Disable();
            StopCombatRoutines();
        }

        private void OnDestroy()
        {
            foreach (InputAction action in abilityActions)
            {
                PhasebreakSettings.Unregister(action);
                action?.Dispose();
            }
        }

        private void CreateInputActions()
        {
            for (int i = 0; i < abilityActions.Length; i++)
            {
                abilityActions[i]?.Dispose();
                abilityActions[i] = PhasebreakSettings.Button($"ability.{i + 1}", GetAbilityName(i));
            }
        }

        private void Update()
        {
            float pressureRegeneration = talents != null && health != null && health.MaxHealth > 0 && health.CurrentHealth < health.MaxHealth * .5f
                ? 1f + talents.GetEffect(TalentEffect.ResourceUnderPressure) : 1f;
            currentResource = Mathf.MoveTowards(currentResource, maximumResource,
                resourceRegeneration * pressureRegeneration * Time.deltaTime);
            UpdateCharges();
            if (GameplayInputFocus.GameplayInputBlocked || PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen)
                return;
            for (int i = 0; i < abilityActions.Length; i++)
                if (abilityActions[i].WasPressedThisFrame())
                    TryUseAssignedAbility(i);
        }

        public CombatAbilityDefinition GetAbilityDefinition(int index) =>
            IsValidIndex(index) ? Ability(index) : null;

        public int GetAssignedAbilityIndex(int slot) => IsValidIndex(slot) ? slotAbilities[slot] : -1;

        public bool AssignAbilityToSlot(int slot, string abilityId)
        {
            if (!IsValidIndex(slot) || string.IsNullOrWhiteSpace(abilityId)) return false;
            for (int index = 0; index < CatalogCount; index++)
            {
                if (!string.Equals(GetAbilityId(index), abilityId, StringComparison.Ordinal)) continue;
                slotAbilities[slot] = index;
                PlayerPrefs.SetString(AssignmentPrefix + slot, abilityId);
                PlayerPrefs.Save();
                AssignmentsChanged?.Invoke();
                return true;
            }
            return false;
        }

        public AbilityState GetAssignedAbilityState(int slot)
        {
            if (!IsValidIndex(slot)) return default;
            AbilityState ability = GetAbilityState(slotAbilities[slot]);
            return new AbilityState(ability.Name, GetKey(slot), ability.CooldownRemaining,
                ability.CooldownDuration, ability.ResourceCost, ability.IsUsable,
                ability.Charges, ability.MaximumCharges, ability.Icon);
        }

        public bool TryUseAssignedAbility(int slot) => IsValidIndex(slot) && TryUseAbility(slotAbilities[slot]);

        private void LoadAssignments()
        {
            for (int slot = 0; slot < slotAbilities.Length; slot++)
            {
                slotAbilities[slot] = Mathf.Min(slot, CatalogCount - 1);
                string savedId = PlayerPrefs.GetString(AssignmentPrefix + slot, string.Empty);
                if (string.IsNullOrEmpty(savedId)) continue;
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
            bool globalIsLonger = globalRemaining > abilityRemaining;
            return new AbilityState(GetAbilityName(index), GetKey(index),
                globalIsLonger ? globalRemaining : abilityRemaining,
                globalIsLonger ? globalCooldown : GetCooldown(index), GetCost(index),
                CanUseAbility(index), charges[index], maximumCharges, GetAbilityIcon(index));
        }

        public bool TryUseAbility(int index)
        {
            if (!ValidateCommonRequirements(index, true))
                return false;
            AbilityExecutionType type = GetExecutionType(index);
            if (type == AbilityExecutionType.PhaseDash)
            {
                if (movement == null || !movement.CanDashNow)
                {
                    Fail("Phase Dash cannot be used right now");
                    return false;
                }
                ConsumeAbility(index);
                if (build != null && build.Specialization == Specialization.Riftblade)
                    talents?.NotifyAbilityUsed(true);
                attackRoutine = StartCoroutine(PerformDash(index));
                return true;
            }

            Targetable target = ResolveTarget(index);
            if (target == null)
            {
                Fail(type == AbilityExecutionType.Charge ? "No charge target in range" : "No valid target in range");
                return false;
            }

            float talentDamage = talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityDamage, GetAbilityId(index)) + talents.RhythmDamageBonus : 0f;
            if (index == 1 && talents != null) talentDamage += talents.HeavyImpactBonus;
            float damage = GetDamage(index) * (1f + talentDamage) * (progression != null ? progression.PowerMultiplier : 1f) *
                           (build != null ? build.PowerMultiplier : 1f);
            if (build != null && target.GetComponent<RiftWardenBoss>() != null)
                damage *= build.BossDamageMultiplier;
            bool critical = UnityEngine.Random.value < Mathf.Clamp01(CriticalChance + GetCriticalBonus(index));
            if (critical)
                damage *= CriticalDamageMultiplier;

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
            Array.Clear(nextChargeReadyAt, 0, nextChargeReadyAt.Length);
            globalReadyAt = 0f;
            currentResource = maximumResource;
            FillCharges();
        }

        public int[] CaptureMaximumCharges()
        {
            int[] maximum = new int[AbilityCountValue];
            for (int i = 0; i < maximum.Length; i++) maximum[i] = GetMaximumCharges(i);
            return maximum;
        }

        public void ReconcileAbilityModifiers(int[] previousMaximumCharges)
        {
            if (previousMaximumCharges == null || previousMaximumCharges.Length != AbilityCountValue) return;
            for (int i = 0; i < AbilityCountValue; i++)
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
            if (IsAttacking)
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
            if (GetExecutionType(index) == AbilityExecutionType.PhaseDash)
                return movement != null && movement.CanDashNow;
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
            if (target == null || !target.IsHostile || !target.IsAlive)
                return false;
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 destination = target.NameplateWorldPosition - Vector3.up * 0.65f;
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
            int chainBefore = RiftChainCount;
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            movement?.CancelDashForAttack();
            float attackSpeed = build != null ? build.AttackSpeedMultiplier : 1f;
            float windup = GetWindup(index) / attackSpeed;
            float recovery = GetRecovery(index) / attackSpeed;
            AbilityPresentationEvent presentation = Event(index, windup + recovery + 0.12f, critical);
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
            else if (!riftLunge)
                movement?.AddCombatImpulse(transform.forward * GetImpulse(index));

            yield return new WaitForSeconds(windup);
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
            attackRoutine = null;
        }

        private void ApplyHit(int index, Targetable target, float damage, bool critical, bool mobilityHit)
        {
            ICombatTarget combatTarget = target.GetComponent<ICombatTarget>();
            if (combatTarget == null)
                return;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;
            direction.Normalize();
            combatTarget.ReceiveHit(new CombatHit(target.transform.position + Vector3.up * 1.15f,
                direction, damage, knockback * (index == 1 ? 1.5f : 1f), mobilityHit, critical,
                GetAbilityName(index)));

            if (critical && build != null && build.CritEnergyRestore > 0f)
                currentResource = Mathf.Min(maximumResource, currentResource + build.CritEnergyRestore);
            if (critical) talents?.NotifyCriticalHit();
            if (index == 1 && build != null && build.CrushingBlowCleave)
                PerformCleave(target, damage * 0.6f, critical);
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
            followCamera?.AddImpulse(critical ? criticalCameraImpulse : normalCameraImpulse);
            float hitStop = critical ? criticalHitStop : normalHitStop;
            if (hitStop > 0f)
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
            riftChainCount = 0;
            riftChainUntil = 0f;
            LastChainEnergyRestored = 0f;
            LastChainChargeRestored = false;
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

        private void PerformCleave(Targetable primary, float damage, bool critical)
        {
            foreach (Targetable other in Targetable.ActiveTargets)
            {
                if (other == null || other == primary || !other.IsHostile || !other.IsAlive ||
                    FlatDistance(transform.position, other.transform.position) > GetRange(1) + 1.2f)
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
            for (int i = 0; i < AbilityCountValue; i++)
                charges[i] = GetMaximumCharges(i);
        }

        private void UpdateCharges()
        {
            for (int i = 0; i < AbilityCountValue; i++)
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
        private bool IsValidIndex(int index) => index >= 0 && index < AbilityCountValue;
        private CombatAbilityDefinition Ability(int index) =>
            abilityDefinitions != null && index >= 0 && index < abilityDefinitions.Length
                ? abilityDefinitions[index] : null;
        private string GetAbilityId(int index) => !string.IsNullOrWhiteSpace(Ability(index)?.id) ? Ability(index).id : index switch
        {
            1 => "crushing-blow", 2 => "phase-lunge", 3 => "phase-dash", 4 => "rift-charge", _ => "strike"
        };
        private string GetAbilityName(int index) => Ability(index) != null ? Ability(index).displayName : index switch
        {
            1 => "Crushing Blow", 2 => "Phase Lunge", 3 => "Phase Dash", 4 => "Rift Charge", _ => "Strike"
        };
        private Sprite GetAbilityIcon(int index)
        {
            Sprite icon = Ability(index)?.icon;
            return icon != null ? icon : PhasebreakIconCatalog.Current?.fallbackAbility;
        }
        private string GetKey(int index) => PhasebreakSettings.Display($"ability.{index + 1}");
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
            if (index == 2 && build != null) value *= build.PhaseLungeCooldownMultiplier;
            else if (talents != null) value *= Mathf.Clamp01(1f - talents.GetAbilityEffect(TalentEffect.AbilityCooldown, GetAbilityId(index)));
            return value;
        }
        private float GetCost(int index)
        {
            float value = Ability(index) != null ? Ability(index).resourceCost : index switch { 1 => crushingCost, 2 => lungeCost, 4 => 15f, _ => 0f };
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
