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

        public AbilityState(string name, string key, float cooldownRemaining, float cooldownDuration,
            float resourceCost, bool isUsable)
        {
            Name = name;
            Key = key;
            CooldownRemaining = cooldownRemaining;
            CooldownDuration = cooldownDuration;
            ResourceCost = resourceCost;
            IsUsable = isUsable;
        }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        private const int AbilityCountValue = 3;

        [Header("References")]
        [SerializeField] private PhasebreakPlayerMovement movement;
        [SerializeField] private PhasebreakFollowCamera followCamera;
        [SerializeField] private PlayerTargeting targeting;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private PlayerBuildSystem build;
        [SerializeField] private Transform slashVisual;
        [SerializeField] private CombatAbilityDefinition[] abilityDefinitions;

        [Header("Combat Stats")]
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.2f;
        [SerializeField, Min(1f)] private float criticalDamageMultiplier = 2f;
        [SerializeField, Min(0.1f)] private float globalCooldown = 0.8f;
        [SerializeField, Min(1f)] private float maximumResource = 100f;
        [SerializeField, Min(0f)] private float resourceRegeneration = 14f;

        [Header("1 - Strike")]
        [SerializeField, Min(1f)] private float strikeDamage = 5f;
        [SerializeField, Min(0.1f)] private float strikeRange = 2.4f;
        [SerializeField, Min(0f)] private float strikeCooldown;
        [SerializeField, Min(0f)] private float strikeCost;

        [Header("2 - Crushing Blow")]
        [SerializeField, Min(1f)] private float crushingDamage = 10f;
        [SerializeField, Min(0.1f)] private float crushingRange = 2.65f;
        [SerializeField, Min(0f)] private float crushingCooldown = 5f;
        [SerializeField, Min(0f)] private float crushingCost = 30f;
        [SerializeField, Range(0f, 1f)] private float crushingCriticalBonus = 0.1f;

        [Header("3 - Phase Lunge")]
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

        private readonly float[] readyAt = new float[AbilityCountValue];
        private readonly InputAction[] abilityActions = new InputAction[AbilityCountValue];
        private float globalReadyAt;
        private float bonusLungeReadyAt;
        private float currentResource;
        private Coroutine attackRoutine;
        private Coroutine hitStopRoutine;

        public int AbilityCount => AbilityCountValue;
        public bool IsAttacking => attackRoutine != null;
        public float CurrentResource => currentResource;
        public float MaximumResource => maximumResource;
        public float ResourceFraction => maximumResource <= 0f ? 0f : currentResource / maximumResource;
        public float CriticalChance => Mathf.Clamp01(criticalChance +
            (progression != null ? progression.CriticalChanceBonus : 0f) +
            (build != null ? build.CriticalChanceBonus : 0f));
        public float CriticalDamageMultiplier => criticalDamageMultiplier +
            (progression != null ? progression.CriticalDamageBonus : 0f) +
            (build != null ? build.CriticalDamageBonus : 0f);

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
            if (movement == null)
                movement = GetComponent<PhasebreakPlayerMovement>();
            if (followCamera == null)
                followCamera = FindAnyObjectByType<PhasebreakFollowCamera>();
            if (targeting == null)
                targeting = GetComponent<PlayerTargeting>();
            if (progression == null)
                progression = GetComponent<PlayerProgression>();
            if (build == null)
                build = GetComponent<PlayerBuildSystem>() ?? gameObject.AddComponent<PlayerBuildSystem>();

            CreateInputActions();
            currentResource = maximumResource;
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
                action?.Dispose();
        }

        private void CreateInputActions()
        {
            for (int i = 0; i < abilityActions.Length; i++) abilityActions[i]?.Dispose();
            abilityActions[0] = new InputAction("Strike", InputActionType.Button, "<Keyboard>/1");
            abilityActions[1] = new InputAction("Crushing Blow", InputActionType.Button, "<Keyboard>/2");
            abilityActions[2] = new InputAction("Phase Lunge", InputActionType.Button, "<Keyboard>/3");
        }

        private void Update()
        {
            currentResource = Mathf.MoveTowards(currentResource, maximumResource,
                resourceRegeneration * Time.deltaTime);

            for (int i = 0; i < abilityActions.Length; i++)
            {
                if (abilityActions[i].WasPressedThisFrame())
                    TryUseAbility(i);
            }
        }

        public AbilityState GetAbilityState(int index)
        {
            if (index < 0 || index >= AbilityCountValue)
                return default;

            float abilityRemaining = Mathf.Max(0f, readyAt[index] - Time.time);
            if (index == 2 && build != null && build.PhaseLungeExtraCharges > 0)
                abilityRemaining = Mathf.Min(abilityRemaining, Mathf.Max(0f, bonusLungeReadyAt - Time.time));
            float globalRemaining = Mathf.Max(0f, globalReadyAt - Time.time);
            bool globalIsLonger = globalRemaining > abilityRemaining;
            float cooldownDuration = globalIsLonger ? globalCooldown : GetCooldown(index);
            float cooldownRemaining = globalIsLonger ? globalRemaining : abilityRemaining;
            return new AbilityState(GetAbilityName(index), (index + 1).ToString(), cooldownRemaining,
                cooldownDuration, GetCost(index), CanUseAbility(index));
        }

        public bool TryUseAbility(int index)
        {
            if (!CanBeginAbility(index))
                return false;

            Targetable target = targeting.CurrentTarget;
            if (target == null || !target.IsHostile || !target.IsAlive ||
                !IsTargetValid(target, GetRange(index)))
            {
                target = targeting.TrySelectNearestInFront(GetRange(index), autoTargetCone);
            }
            if (target == null || !IsTargetValid(target, GetRange(index)))
                return false;

            float damage = GetDamage(index) * (progression != null ? progression.PowerMultiplier : 1f) *
                           (build != null ? build.PowerMultiplier : 1f);
            if (build != null && target.GetComponent<RiftWardenBoss>() != null)
                damage *= build.BossDamageMultiplier;
            float critChance = Mathf.Clamp01(CriticalChance + GetCriticalBonus(index));
            bool critical = UnityEngine.Random.value < critChance;
            if (critical)
                damage *= CriticalDamageMultiplier;

            currentResource = Mathf.Max(0f, currentResource - GetCost(index));
            float cooldown = GetCooldown(index);
            if (index == 2 && build != null && build.PhaseLungeExtraCharges > 0 && Time.time < readyAt[index])
                bonusLungeReadyAt = Time.time + cooldown;
            else
                readyAt[index] = Time.time + cooldown;
            globalReadyAt = Time.time + globalCooldown / (build != null ? build.AttackSpeedMultiplier : 1f);
            attackRoutine = StartCoroutine(PerformAbility(index, target, damage, critical));
            return true;
        }

        public void ResetCombat()
        {
            StopCombatRoutines();
            Array.Clear(readyAt, 0, readyAt.Length);
            bonusLungeReadyAt = 0f;
            globalReadyAt = 0f;
            currentResource = maximumResource;
        }

        private bool CanUseAbility(int index)
        {
            if (!CanBeginAbility(index))
                return false;

            Targetable target = targeting.CurrentTarget;
            return target != null && target.IsHostile && target.IsAlive &&
                   IsTargetValid(target, GetRange(index));
        }

        private bool CanBeginAbility(int index) =>
            index >= 0 && index < AbilityCountValue && !IsAttacking && Time.time >= globalReadyAt &&
            (Time.time >= readyAt[index] || (index == 2 && build != null && build.PhaseLungeExtraCharges > 0 && Time.time >= bonusLungeReadyAt)) &&
            currentResource >= GetCost(index) && targeting != null;

        private bool IsTargetValid(Targetable target, float range)
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 destination = target.NameplateWorldPosition - Vector3.up * 0.65f;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;
            if (distance > range || distance < 0.01f)
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

        private IEnumerator PerformAbility(int index, Targetable target, float damage, bool critical)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

            movement?.CancelDashForAttack();
            float windup = (Ability(index) != null ? Ability(index).windup : index switch { 1 => 0.18f, 2 => 0.08f, _ => 0.1f }) /
                           (build != null ? build.AttackSpeedMultiplier : 1f);
            if (index == 2 && toTarget.sqrMagnitude > 0.01f)
                movement?.AddCombatImpulse(toTarget.normalized * (Ability(index) != null ? Ability(index).impulse : lungeImpulse));
            else
                movement?.AddCombatImpulse(transform.forward * (Ability(index) != null ? Ability(index).impulse : index == 1 ? 2.8f : 1.7f));

            yield return new WaitForSeconds(windup);
            SetSlashVisible(true);
            if (slashVisual != null)
                slashVisual.localScale = Vector3.one * (critical ? 1.55f : index == 1 ? 1.3f : 1f);

            if (target != null && target.IsAlive &&
                Vector3.Distance(transform.position, target.transform.position) <= GetRange(index) + 1f)
            {
                ICombatTarget combatTarget = target.GetComponent<ICombatTarget>();
                if (combatTarget != null)
                {
                    Vector3 direction = target.transform.position - transform.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude < 0.01f)
                        direction = transform.forward;
                    direction.Normalize();
                    Vector3 hitPoint = target.transform.position + Vector3.up * 1.15f;
                    combatTarget.ReceiveHit(new CombatHit(hitPoint, direction, damage,
                        knockback * (index == 1 ? 1.5f : 1f), index == 2, critical,
                        GetAbilityName(index)));

                    if (critical && build != null && build.CritEnergyRestore > 0f)
                        currentResource = Mathf.Min(maximumResource, currentResource + build.CritEnergyRestore);
                    if (index == 1 && build != null && build.CrushingBlowCleave)
                        PerformCleave(target, damage * .6f, critical);
                    if (index == 2 && !target.IsAlive && build != null && build.TeleportKillRecovery > 0f)
                    {
                        currentResource = Mathf.Min(maximumResource, currentResource + build.TeleportKillRecovery);
                        readyAt[2] = bonusLungeReadyAt = Time.time;
                    }

                    followCamera?.AddImpulse(critical ? criticalCameraImpulse : normalCameraImpulse);
                    float hitStop = critical ? criticalHitStop : normalHitStop;
                    if (hitStop > 0f)
                        hitStopRoutine = StartCoroutine(HitStop(hitStop));
                }
            }

            yield return new WaitForSeconds(0.12f);
            SetSlashVisible(false);
            yield return new WaitForSeconds(index == 1 ? 0.2f : 0.1f);
            attackRoutine = null;
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
                if (other == null || other == primary || !other.IsHostile || !other.IsAlive || Vector3.Distance(transform.position, other.transform.position) > GetRange(1) + 1.2f) continue;
                ICombatTarget victim = other.GetComponent<ICombatTarget>(); if (victim == null) continue;
                Vector3 direction = (other.transform.position - transform.position).normalized;
                victim.ReceiveHit(new CombatHit(other.transform.position + Vector3.up, direction, damage, knockback, false, critical, "Crushing Blow: Fracture"));
            }
        }

        private string GetAbilityName(int index) => Ability(index) != null ? Ability(index).displayName : index switch
        {
            1 => "Crushing Blow",
            2 => "Phase Lunge",
            _ => "Strike"
        };

        private float GetDamage(int index) => Ability(index) != null ? Ability(index).damage : index switch
        {
            1 => crushingDamage,
            2 => lungeDamage,
            _ => strikeDamage
        };

        private float GetRange(int index) => Ability(index) != null ? Ability(index).range : index switch
        {
            1 => crushingRange,
            2 => lungeRange,
            _ => strikeRange
        };

        private float GetCooldown(int index)
        {
            float value = Ability(index) != null ? Ability(index).cooldown : index switch
        {
            1 => crushingCooldown,
            2 => lungeCooldown,
            _ => strikeCooldown
        };
            return index == 2 && build != null ? value * build.PhaseLungeCooldownMultiplier : value;
        }

        private float GetCost(int index) => Ability(index) != null ? Ability(index).resourceCost : index switch
        {
            1 => crushingCost,
            2 => lungeCost,
            _ => strikeCost
        };

        private float GetCriticalBonus(int index) => Ability(index) != null ? Ability(index).criticalBonus : index switch
        {
            1 => crushingCriticalBonus,
            2 => lungeCriticalBonus,
            _ => 0f
        };

        private CombatAbilityDefinition Ability(int index) => abilityDefinitions != null && index >= 0 && index < abilityDefinitions.Length ? abilityDefinitions[index] : null;
    }
}
