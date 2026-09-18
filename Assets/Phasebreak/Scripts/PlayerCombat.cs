using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        private enum AttackPhase { Ready, Windup, Active, Recovery }

        [Header("References")]
        [SerializeField] private PhasebreakPlayerMovement movement;
        [SerializeField] private PhasebreakFollowCamera followCamera;
        [SerializeField] private Transform slashVisual;

        [Header("Combo Timing")]
        [SerializeField, Min(0f)] private float firstWindup = 0.045f;
        [SerializeField, Min(0f)] private float secondWindup = 0.04f;
        [SerializeField, Min(0f)] private float finisherWindup = 0.065f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.075f;
        [SerializeField, Min(0f)] private float firstRecovery = 0.105f;
        [SerializeField, Min(0f)] private float secondRecovery = 0.115f;
        [SerializeField, Min(0f)] private float finisherRecovery = 0.2f;
        [SerializeField, Min(0f)] private float inputBufferDuration = 0.22f;
        [SerializeField, Min(0f)] private float comboGraceDuration = 0.75f;

        [Header("Aerial Attack")]
        [SerializeField, Min(0f)] private float aerialWindup = 0.035f;
        [SerializeField, Min(0.01f)] private float aerialActiveDuration = 0.1f;
        [SerializeField, Min(0f)] private float aerialRecovery = 0.11f;
        [SerializeField, Min(1f)] private float aerialPowerMultiplier = 1.25f;

        [Header("Hit Shape")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.4f;
        [SerializeField, Min(0.1f)] private float hitRadius = 1.2f;
        [SerializeField, Range(10f, 180f)] private float attackArc = 130f;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Attack Motion")]
        [SerializeField, Min(0f)] private float firstLunge = 3.2f;
        [SerializeField, Min(0f)] private float secondLunge = 4f;
        [SerializeField, Min(0f)] private float finisherLunge = 6.2f;
        [SerializeField, Min(0f)] private float aerialLunge = 2.5f;

        [Header("Soft Targeting")]
        [SerializeField, Min(0f)] private float targetingRadius = 5.5f;
        [SerializeField, Range(10f, 180f)] private float targetingArc = 100f;
        [SerializeField, Range(0f, 90f)] private float attackSteeringDegrees = 28f;

        [Header("Impact")]
        [SerializeField, Min(0f)] private float attackPower = 1f;
        [SerializeField, Min(0f)] private float knockback = 1.05f;
        [SerializeField, Min(1f)] private float secondHitMultiplier = 1.15f;
        [SerializeField, Min(1f)] private float finisherMultiplier = 1.85f;
        [SerializeField, Min(1f)] private float dashAttackMultiplier = 1.35f;
        [SerializeField, Range(0f, 0.15f)] private float hitStopDuration = 0.04f;
        [SerializeField, Range(0f, 1f)] private float cameraImpulse = 0.16f;

        private readonly Collider[] hitBuffer = new Collider[24];
        private readonly Collider[] targetingBuffer = new Collider[32];
        private readonly HashSet<ICombatTarget> hitTargets = new HashSet<ICombatTarget>();
        private InputAction attackAction;
        private AttackPhase phase;
        private float phaseRemaining;
        private float phaseDuration;
        private float bufferedUntil = float.NegativeInfinity;
        private float comboExpiresAt = float.NegativeInfinity;
        private int comboStep;
        private int currentAttackStep;
        private bool currentAttackIsAerial;
        private bool aerialBridgeUsed;
        private bool attackStartedFromDash;
        private bool hitboxFired;
        private Coroutine hitStopRoutine;
        private Transform softTarget;

        public bool IsAttacking => phase != AttackPhase.Ready;
        public bool IsAerialAttack => IsAttacking && currentAttackIsAerial;
        public int ComboStep => comboStep;
        public Transform SoftTarget => softTarget;

        public void QueueAttack() => bufferedUntil = Time.time + inputBufferDuration;

        public void ResetCombat()
        {
            phase = AttackPhase.Ready;
            bufferedUntil = float.NegativeInfinity;
            comboExpiresAt = float.NegativeInfinity;
            comboStep = 0;
            currentAttackStep = 0;
            currentAttackIsAerial = false;
            aerialBridgeUsed = false;
            softTarget = null;
            hitTargets.Clear();
            SetSlashVisible(false);
        }

        public void Configure(PhasebreakPlayerMovement playerMovement, PhasebreakFollowCamera camera, Transform visual)
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
            attackAction = new InputAction("Slash", InputActionType.Button, "<Mouse>/leftButton");
        }

        private void OnEnable()
        {
            attackAction.Enable();
            if (movement != null)
                movement.DashStarted += HandleDashStarted;
        }

        private void OnDisable()
        {
            attackAction.Disable();
            if (movement != null)
                movement.DashStarted -= HandleDashStarted;
            SetSlashVisible(false);
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
                Time.timeScale = 1f;
            }
        }

        private void OnDestroy() => attackAction.Dispose();

        private void Update()
        {
            if (attackAction.WasPressedThisFrame())
                QueueAttack();

            if (movement != null && movement.IsGrounded)
                aerialBridgeUsed = false;

            if (phase == AttackPhase.Ready)
            {
                if (comboStep > 0 && Time.time > comboExpiresAt)
                    comboStep = 0;
                if (Time.time <= bufferedUntil)
                    BeginBufferedAttack();
                return;
            }

            AnimateSlash();
            phaseRemaining -= Time.deltaTime;
            if (phaseRemaining > 0f)
                return;

            switch (phase)
            {
                case AttackPhase.Windup:
                    phase = AttackPhase.Active;
                    phaseDuration = currentAttackIsAerial ? aerialActiveDuration : activeDuration;
                    phaseRemaining = phaseDuration;
                    SetSlashVisible(true);
                    FireHitbox();
                    break;
                case AttackPhase.Active:
                    phase = AttackPhase.Recovery;
                    phaseDuration = GetRecoveryDuration();
                    phaseRemaining = phaseDuration;
                    SetSlashVisible(false);
                    break;
                case AttackPhase.Recovery:
                    FinishAttack();
                    break;
            }
        }

        private void BeginBufferedAttack()
        {
            bool airborne = movement != null && !movement.IsGrounded;
            if (airborne && !aerialBridgeUsed)
            {
                aerialBridgeUsed = true;
                BeginAttack(0, true);
                return;
            }

            int nextStep = comboStep + 1;
            if (nextStep > 3)
                nextStep = 1;
            BeginAttack(nextStep, false);
        }

        private void BeginAttack(int step, bool aerial)
        {
            bufferedUntil = float.NegativeInfinity;
            currentAttackStep = step;
            currentAttackIsAerial = aerial;
            attackStartedFromDash = movement != null && movement.IsDashing;
            movement?.CancelDashForAttack();

            if (!aerial)
            {
                comboStep = step;
                comboExpiresAt = Time.time + comboGraceDuration;
            }

            ApplyAttackSteering();

            phase = AttackPhase.Windup;
            phaseDuration = GetWindupDuration();
            phaseRemaining = phaseDuration;
            hitboxFired = false;
            hitTargets.Clear();

            float lunge = aerial ? aerialLunge : step switch
            {
                2 => secondLunge,
                3 => finisherLunge,
                _ => firstLunge
            };
            movement?.AddCombatImpulse(transform.forward * lunge);
        }

        private void ApplyAttackSteering()
        {
            softTarget = null;
            if (targetingRadius <= 0f || attackSteeringDegrees <= 0f)
                return;

            int count = Physics.OverlapSphereNonAlloc(transform.position, targetingRadius, targetingBuffer,
                targetLayers, QueryTriggerInteraction.Collide);
            float bestScore = float.PositiveInfinity;
            Vector3 bestDirection = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = targetingBuffer[i];
                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                ICombatTarget combatTarget = candidate.GetComponentInParent<ICombatTarget>();
                Component targetComponent = combatTarget as Component;
                if (targetComponent == null)
                    continue;
                if (combatTarget is MeleeEnemy enemy && !enemy.IsAlive)
                    continue;

                Vector3 direction = targetComponent.transform.position - transform.position;
                direction.y = 0f;
                float distance = direction.magnitude;
                if (distance < 0.01f)
                    continue;

                float angle = Vector3.Angle(transform.forward, direction);
                if (angle > targetingArc * 0.5f)
                    continue;

                float score = distance + angle * 0.035f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestDirection = direction.normalized;
                softTarget = targetComponent.transform;
            }

            if (softTarget == null)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(bestDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, attackSteeringDegrees);
        }

        private void FinishAttack()
        {
            phase = AttackPhase.Ready;
            SetSlashVisible(false);
            if (!currentAttackIsAerial && currentAttackStep == 3)
            {
                comboStep = 0;
                comboExpiresAt = float.NegativeInfinity;
            }

            if (Time.time <= bufferedUntil)
                BeginBufferedAttack();
        }

        private void HandleDashStarted()
        {
            if (!IsAttacking)
                return;

            bool finishedCombo = !currentAttackIsAerial && currentAttackStep == 3;
            bufferedUntil = float.NegativeInfinity;
            phase = AttackPhase.Ready;
            SetSlashVisible(false);
            hitTargets.Clear();
            if (finishedCombo)
                comboStep = 0;
            else
                comboExpiresAt = Time.time + comboGraceDuration;
        }

        private float GetWindupDuration()
        {
            if (currentAttackIsAerial)
                return aerialWindup;
            return currentAttackStep switch { 2 => secondWindup, 3 => finisherWindup, _ => firstWindup };
        }

        private float GetRecoveryDuration()
        {
            if (currentAttackIsAerial)
                return aerialRecovery;
            return currentAttackStep switch { 2 => secondRecovery, 3 => finisherRecovery, _ => firstRecovery };
        }

        private void FireHitbox()
        {
            if (hitboxFired)
                return;

            hitboxFired = true;
            Vector3 center = transform.position + Vector3.up * 0.75f + transform.forward * attackRange;
            int count = Physics.OverlapSphereNonAlloc(center, hitRadius, hitBuffer, targetLayers, QueryTriggerInteraction.Collide);
            float multiplier = currentAttackStep switch { 2 => secondHitMultiplier, 3 => finisherMultiplier, _ => 1f };
            if (currentAttackIsAerial)
                multiplier *= aerialPowerMultiplier;
            if (attackStartedFromDash)
                multiplier *= dashAttackMultiplier;

            bool connected = false;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = hitBuffer[i];
                if (candidate == null || candidate.transform.IsChildOf(transform))
                    continue;

                Vector3 toTarget = candidate.bounds.center - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, toTarget) > attackArc * 0.5f)
                    continue;

                ICombatTarget target = candidate.GetComponentInParent<ICombatTarget>();
                if (target == null || !hitTargets.Add(target))
                    continue;

                Vector3 direction = toTarget.normalized;
                target.ReceiveHit(new CombatHit(candidate.ClosestPoint(center), direction,
                    attackPower * multiplier, knockback * multiplier, attackStartedFromDash));
                connected = true;
            }

            if (!connected)
                return;

            followCamera?.AddImpulse(cameraImpulse * multiplier);
            if (hitStopDuration > 0f)
            {
                if (hitStopRoutine != null)
                    StopCoroutine(hitStopRoutine);
                hitStopRoutine = StartCoroutine(HitStop());
            }
        }

        private IEnumerator HitStop()
        {
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = previousScale;
            hitStopRoutine = null;
        }

        private void AnimateSlash()
        {
            if (slashVisual == null || phase != AttackPhase.Active || phaseDuration <= 0f)
                return;

            float progress = 1f - phaseRemaining / phaseDuration;
            float direction = currentAttackStep == 2 ? -1f : 1f;
            slashVisual.localRotation = Quaternion.Euler(0f,
                Mathf.Lerp(-55f * direction, 55f * direction, progress), 0f);
            float punch = 0.9f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
            slashVisual.localScale = Vector3.one * punch;
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

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position + Vector3.up * 0.75f;
            Vector3 center = origin + transform.forward * attackRange;
            Gizmos.color = new Color(1f, 0.25f, 0.05f, 0.45f);
            Gizmos.DrawWireSphere(center, hitRadius);
            Vector3 left = Quaternion.Euler(0f, -attackArc * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, attackArc * 0.5f, 0f) * transform.forward;
            Gizmos.DrawLine(origin, origin + left * (attackRange + hitRadius));
            Gizmos.DrawLine(origin, origin + right * (attackRange + hitRadius));
        }
    }
}
