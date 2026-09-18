using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class MeleeEnemy : MonoBehaviour, ICombatTarget, ICombatCastSource
    {
        private enum EnemyState { Idle, Chase, Windup, Active, Recovery, Stagger, Return, Dead }

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Transform telegraphVisual;

        [Header("Vitals")]
        [SerializeField, Min(1)] private int maxHealth = 30;
        [SerializeField, Min(0f)] private float staggerDuration = 0.18f;
        [SerializeField, Min(0)] private int experienceReward = 12;
        [SerializeField] private bool respawnEnabled = true;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float awarenessRange = 10f;
        [SerializeField, Min(0f)] private float moveSpeed = 3.6f;
        [SerializeField, Min(0f)] private float rotationSpeed = 540f;
        [SerializeField, Min(1f)] private float leashRange = 18f;
        [SerializeField, Min(0.05f)] private float returnStopDistance = 0.2f;
        [SerializeField] private float gravity = -24f;

        [Header("Attack")]
        [SerializeField, Min(0.1f)] private float attackRange = 1.75f;
        [SerializeField, Range(10f, 180f)] private float attackArc = 85f;
        [SerializeField, Min(0f)] private float windupDuration = 0.55f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.65f;
        [SerializeField, Min(1)] private int attackDamage = 1;

        [Header("Hit Reaction")]
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;
        [SerializeField, Min(0f)] private float knockbackScale = 2.3f;

        private CharacterController controller;
        private PlayerHealth playerHealth;
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private EnemyState state;
        private float stateRemaining;
        private float verticalVelocity;
        private Vector3 knockbackVelocity;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 spawnScale;
        private Vector3 telegraphBaseScale = Vector3.one;
        private bool attackFired;
        private bool defeatRewardGranted;
        private Coroutine reactionRoutine;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsAlive => state != EnemyState.Dead;
        public string StateName => state.ToString();
        public int AttackCount { get; private set; }
        public bool IsCasting => state == EnemyState.Windup;
        public string CastName => "Savage Strike";
        public float CastProgress => !IsCasting || windupDuration <= 0f
            ? 0f : 1f - Mathf.Clamp01(stateRemaining / windupDuration);
        public bool IsCastInterruptible => true;
        public bool IsMoving => state == EnemyState.Chase || state == EnemyState.Return;
        public bool IsAttackActive => state == EnemyState.Active;
        public bool IsRecovering => state == EnemyState.Recovery;
        public bool IsStaggered => state == EnemyState.Stagger;
        public bool IsDead => state == EnemyState.Dead;

        public void SetRespawnEnabled(bool enabled) => respawnEnabled = enabled;

        public void Configure(Transform newTarget, Transform telegraph)
        {
            target = newTarget;
            telegraphVisual = telegraph;
            if (telegraphVisual != null)
                telegraphVisual.gameObject.SetActive(false);
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            renderers = GetComponentsInChildren<Renderer>(true);
            propertyBlock = new MaterialPropertyBlock();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            spawnScale = transform.localScale;
            if (telegraphVisual != null)
                telegraphBaseScale = telegraphVisual.localScale;
            CurrentHealth = maxHealth;
            if (target == null)
            {
                PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
                if (player != null)
                    target = player.transform;
            }
            playerHealth = target != null ? target.GetComponent<PlayerHealth>() : null;
            SetState(EnemyState.Idle, 0f);
        }

        private void Update()
        {
            if (state == EnemyState.Dead || target == null)
                return;

            ApplyGravityAndKnockback();
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            Vector3 fromSpawn = transform.position - spawnPosition;
            fromSpawn.y = 0f;
            if (state != EnemyState.Idle && state != EnemyState.Return &&
                (fromSpawn.magnitude > leashRange || distance > awarenessRange * 1.75f))
            {
                SetState(EnemyState.Return, 0f);
            }

            switch (state)
            {
                case EnemyState.Idle:
                    if (distance <= awarenessRange)
                        SetState(EnemyState.Chase, 0f);
                    break;
                case EnemyState.Chase:
                    TickChase(toTarget, distance);
                    break;
                case EnemyState.Windup:
                    TickWindup(toTarget);
                    break;
                case EnemyState.Active:
                    TickActive();
                    break;
                case EnemyState.Recovery:
                    TickTimer(EnemyState.Chase);
                    break;
                case EnemyState.Stagger:
                    TickTimer(EnemyState.Chase);
                    break;
                case EnemyState.Return:
                    TickReturn();
                    break;
            }
        }

        public void ReceiveHit(CombatHit hit)
        {
            if (!IsAlive)
                return;

            int damage = Mathf.Max(1, Mathf.RoundToInt(hit.Power));
            int appliedDamage = Mathf.Min(CurrentHealth, damage);
            CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
            knockbackVelocity = hit.Direction.normalized * (hit.Knockback * knockbackScale);
            CombatEvents.RaiseDamageNumber(hit.Point + Vector3.up * 0.35f, appliedDamage,
                hit.IsCritical, hit.AbilityName);

            if (reactionRoutine != null)
                StopCoroutine(reactionRoutine);
            reactionRoutine = StartCoroutine(Flash());

            if (CurrentHealth == 0)
                Die(hit.Direction);
            else
                SetState(EnemyState.Stagger, staggerDuration);
        }

        public void ResetEnemy()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            GetComponent<CorpseLootContainer>()?.ClearForReset();
            if (reactionRoutine != null)
            {
                StopCoroutine(reactionRoutine);
                reactionRoutine = null;
            }
            ClearColor();
            foreach (Renderer item in renderers)
                item.enabled = true;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            transform.localScale = spawnScale;
            controller.enabled = true;
            CurrentHealth = maxHealth;
            AttackCount = 0;
            defeatRewardGranted = false;
            verticalVelocity = -2f;
            knockbackVelocity = Vector3.zero;
            SetState(EnemyState.Idle, 0f);
        }

        private void TickChase(Vector3 toTarget, float distance)
        {
            if (distance <= attackRange)
            {
                SetState(EnemyState.Windup, windupDuration);
                return;
            }

            RotateToward(toTarget);
            controller.Move(toTarget.normalized * (moveSpeed * Time.deltaTime));
        }

        private void TickReturn()
        {
            Vector3 toSpawn = spawnPosition - transform.position;
            toSpawn.y = 0f;
            if (toSpawn.magnitude <= returnStopDistance)
            {
                CurrentHealth = maxHealth;
                knockbackVelocity = Vector3.zero;
                SetState(EnemyState.Idle, 0f);
                return;
            }

            RotateToward(toSpawn);
            controller.Move(toSpawn.normalized * (moveSpeed * Time.deltaTime));
        }

        private void TickWindup(Vector3 toTarget)
        {
            RotateToward(toTarget);
            stateRemaining -= Time.deltaTime;
            if (telegraphVisual != null && windupDuration > 0f)
            {
                float progress = 1f - Mathf.Clamp01(stateRemaining / windupDuration);
                telegraphVisual.localScale = telegraphBaseScale * Mathf.Lerp(0.35f, 1f, progress);
            }

            if (stateRemaining <= 0f)
                SetState(EnemyState.Active, activeDuration);
        }

        private void TickActive()
        {
            if (!attackFired)
            {
                attackFired = true;
                TryDamagePlayer();
            }
            TickTimer(EnemyState.Recovery);
        }

        private void TryDamagePlayer()
        {
            if (playerHealth == null || !playerHealth.IsAlive)
                return;

            Vector3 toPlayer = target.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > attackRange + 0.35f || Vector3.Angle(transform.forward, toPlayer) > attackArc * 0.5f)
                return;

            if (playerHealth.TakeHit(attackDamage, toPlayer.normalized))
                AttackCount++;
        }

        private void TickTimer(EnemyState nextState)
        {
            stateRemaining -= Time.deltaTime;
            if (stateRemaining <= 0f)
                SetState(nextState, nextState == EnemyState.Recovery ? recoveryDuration : 0f);
        }

        private void SetState(EnemyState nextState, float duration)
        {
            state = nextState;
            stateRemaining = duration;
            attackFired = false;
            if (telegraphVisual != null)
            {
                telegraphVisual.gameObject.SetActive(nextState == EnemyState.Windup);
                telegraphVisual.localScale = telegraphBaseScale;
            }
        }

        private void RotateToward(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                return;
            Quaternion desired = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, rotationSpeed * Time.deltaTime);
        }

        private void ApplyGravityAndKnockback()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = knockbackVelocity + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
            knockbackVelocity = Vector3.MoveTowards(knockbackVelocity, Vector3.zero, 18f * Time.deltaTime);
        }

        private void Die(Vector3 hitDirection)
        {
            SetState(EnemyState.Dead, 0f);
            if (!defeatRewardGranted)
            {
                defeatRewardGranted = true;
                CombatEvents.RaiseEnemyDefeated(experienceReward, gameObject.name, transform.position);
                PlayerBuildSystem build = FindAnyObjectByType<PlayerBuildSystem>();
                CorpseLootContainer corpse = GetComponent<CorpseLootContainer>() ?? gameObject.AddComponent<CorpseLootContainer>();
                corpse.Initialize(this, build != null ? build.GenerateCorpseLoot() : null);
            }
            knockbackVelocity = Vector3.zero;
            controller.enabled = false;
            if (reactionRoutine != null)
                StopCoroutine(reactionRoutine);
            reactionRoutine = StartCoroutine(DeathReaction(hitDirection));
        }

        private IEnumerator Flash()
        {
            SetColor(hitColor);
            yield return new WaitForSecondsRealtime(hitFlashDuration);
            ClearColor();
            reactionRoutine = null;
        }

        private IEnumerator DeathReaction(Vector3 direction)
        {
            SetColor(hitColor);
            Vector3 startScale = transform.localScale;
            Quaternion startRotation = transform.rotation;
            Vector3 axis = Vector3.Cross(Vector3.up, direction).normalized;
            Quaternion endRotation = Quaternion.AngleAxis(80f, axis) * startRotation;
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.72f, t * t);
                yield return null;
            }

            ClearColor();
            reactionRoutine = null;
        }

        private void SetColor(Color color)
        {
            foreach (Renderer item in renderers)
            {
                item.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                item.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearColor()
        {
            propertyBlock.Clear();
            foreach (Renderer item in renderers)
                item.SetPropertyBlock(propertyBlock);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, awarenessRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
