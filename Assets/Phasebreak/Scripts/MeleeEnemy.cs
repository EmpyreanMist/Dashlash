using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum EnemyRole { Zombie, Brute, Skirmisher }
    public enum EnemyRank { Normal, Veteran, Elite }

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
        private Vector3 telegraphOriginalScale = Vector3.one;
        private bool attackFired;
        private bool defeatRewardGranted;
        private Coroutine reactionRoutine;
        private EnemyRole role;
        private EnemyRank enemyRank;
        private float nextSidestepAt;
        private int sidestepDirection = 1;
        private EnemyRoleDefinition roleDefinition;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public int AttackDamage => attackDamage;
        public EnemyRole Role => role;
        public EnemyRank Rank => enemyRank;
        public bool IsAlive => state != EnemyState.Dead;
        public string StateName => state.ToString();
        public int AttackCount { get; private set; }
        public bool IsCasting => state == EnemyState.Windup;
        public string CastName => role == EnemyRole.Brute ? "Crushing Swing" :
            role == EnemyRole.Skirmisher ? "Ember Bolt" : "Savage Strike";
        public float CastProgress => !IsCasting || windupDuration <= 0f
            ? 0f : 1f - Mathf.Clamp01(stateRemaining / windupDuration);
        public bool IsCastInterruptible => true;
        public bool IsMoving => state == EnemyState.Chase || state == EnemyState.Return;
        public bool IsAttackActive => state == EnemyState.Active;
        public bool IsRecovering => state == EnemyState.Recovery;
        public bool IsStaggered => state == EnemyState.Stagger;
        public bool IsDead => state == EnemyState.Dead;

        public void SetRespawnEnabled(bool enabled) => respawnEnabled = enabled;

        public void ConfigureRole(EnemyRole newRole, EnemyRank newRank)
        {
            role = newRole;
            enemyRank = newRank;
            roleDefinition = newRole == EnemyRole.Zombie ? null :
                Resources.Load<EnemyRoleDefinition>("EnemyRoles/" + newRole);
            if (roleDefinition != null)
            {
                maxHealth = roleDefinition.health;
                attackDamage = roleDefinition.damage;
                experienceReward = roleDefinition.experience;
                moveSpeed = roleDefinition.moveSpeed;
                awarenessRange = roleDefinition.awarenessRange;
                leashRange = roleDefinition.leashRange;
                attackRange = roleDefinition.attackRange;
                windupDuration = roleDefinition.windup;
                activeDuration = roleDefinition.active;
                recoveryDuration = roleDefinition.recovery;
            }
            EnemyRoleDefinition.RankTuning tuning = roleDefinition != null ? roleDefinition.Tuning(newRank) :
                new EnemyRoleDefinition.RankTuning { healthMultiplier = 1f, damageMultiplier = 1f,
                    experienceMultiplier = 1f };
            // Keep the gameplay capsule aligned with the role visual without scaling world movement.
            controller ??= GetComponent<CharacterController>();
            if (newRole != EnemyRole.Zombie)
            {
                float rankScale = Mathf.Lerp(1f, Mathf.Max(1f, tuning.scaleMultiplier), .75f);
                controller.height = (newRole == EnemyRole.Brute ? 2.65f : 1.7f) * rankScale;
                controller.radius = (newRole == EnemyRole.Brute ? .75f : .34f) * rankScale;
                controller.center = Vector3.up * (controller.height * .5f);
            }
            maxHealth = Mathf.RoundToInt(maxHealth * tuning.healthMultiplier);
            attackDamage = Mathf.Max(1, Mathf.CeilToInt(attackDamage * tuning.damageMultiplier));
            experienceReward = Mathf.RoundToInt(experienceReward * tuning.experienceMultiplier);
            CurrentHealth = maxHealth;
            telegraphBaseScale = telegraphOriginalScale * (role == EnemyRole.Brute ? 1.45f : 1f);
            Targetable identity = GetComponent<Targetable>();
            string name = roleDefinition != null ? roleDefinition.displayName : "Risen Zombie";
            identity?.Configure((newRank == EnemyRank.Normal ? "" : newRank + " ") + name,
                TargetFaction.Hostile, newRank == EnemyRank.Elite ? 3 : newRank == EnemyRank.Veteran ? 2 : 1);
            identity?.SetRank(newRank == EnemyRank.Elite ? UnitRank.Elite : newRank == EnemyRank.Veteran ? UnitRank.Rare : UnitRank.Normal);
            if (newRole != EnemyRole.Zombie)
            {
                (GetComponent<EnemyRoleVisual>() ?? gameObject.AddComponent<EnemyRoleVisual>()).Configure(newRole, newRank, roleDefinition);
                renderers = GetComponentsInChildren<Renderer>(true);
            }
        }

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
                telegraphOriginalScale = telegraphBaseScale = telegraphVisual.localScale;
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
            propertyBlock ??= new MaterialPropertyBlock();
            renderers ??= GetComponentsInChildren<Renderer>(true);
            controller ??= GetComponent<CharacterController>();
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

        public void DebugDefeatWithoutRewards()
        {
            if (!IsAlive) return;
            defeatRewardGranted = true;
            CurrentHealth = 0;
            SetState(EnemyState.Dead, 0f);
            knockbackVelocity = Vector3.zero;
            if (reactionRoutine != null) StopCoroutine(reactionRoutine);
            GetComponent<CorpseLootContainer>()?.ClearForReset();
            controller.enabled = false;
        }

        private void TickChase(Vector3 toTarget, float distance)
        {
            if (role == EnemyRole.Skirmisher)
            {
                RotateToward(toTarget);
                Vector3 direction = Vector3.zero;
                if (distance < 5f) direction = -toTarget.normalized;
                else if (distance > 9f) direction = toTarget.normalized;
                else if (Time.time >= nextSidestepAt)
                { nextSidestepAt = Time.time + 1.6f; sidestepDirection = -sidestepDirection; }
                if (direction == Vector3.zero)
                    direction = Vector3.Cross(Vector3.up, toTarget.normalized) * sidestepDirection * .55f;
                controller.Move(direction * (moveSpeed * Time.deltaTime));
                if (distance >= 5f && distance <= attackRange && HasLineOfSight())
                    SetState(EnemyState.Windup, windupDuration);
                return;
            }
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

            if (role == EnemyRole.Skirmisher)
            {
                Vector3 origin = transform.position + Vector3.up * 1.25f + transform.forward * .7f;
                Vector3 direction = (target.position + Vector3.up * .45f - origin).normalized;
                EnemyProjectile.Spawn(origin, direction, this, attackDamage);
                AttackCount++;
                return;
            }

            Vector3 toPlayer = target.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > attackRange + 0.35f || Vector3.Angle(transform.forward, toPlayer) > attackArc * 0.5f)
                return;

            if (playerHealth.TakeHit(attackDamage, toPlayer.normalized))
                AttackCount++;
        }

        private bool HasLineOfSight()
        {
            if (target == null) return false;
            Vector3 origin = transform.position + Vector3.up * 1.25f;
            Vector3 direction = target.position + Vector3.up - origin;
            if (!Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude,
                ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.transform == target || hit.transform.IsChildOf(target);
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
                CombatEvents.RaiseEnemyDefeated(experienceReward,
                    GetComponent<Targetable>()?.DisplayName ?? gameObject.name, transform.position);
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
            propertyBlock ??= new MaterialPropertyBlock();
            renderers ??= GetComponentsInChildren<Renderer>(true);
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
            propertyBlock ??= new MaterialPropertyBlock();
            renderers ??= GetComponentsInChildren<Renderer>(true);
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
