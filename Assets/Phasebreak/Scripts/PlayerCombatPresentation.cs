using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;

        private Transform spine;
        private Transform chest;
        private Transform rightUpperArm;
        private Transform rightLowerArm;
        private Transform leftUpperArm;
        private Transform leftLowerArm;
        private Transform head;
        private AbilityPresentationEvent activeAbility;
        private float abilityStartedAt;
        private float hitWeight;
        private Vector3 hitDirection;
        private bool abilityActive;
        private bool dead;

        private void Awake()
        {
            animator ??= GetComponent<Animator>();
            combat ??= GetComponentInParent<PlayerCombat>();
            health ??= GetComponentInParent<PlayerHealth>();
            if (animator == null || !animator.isHuman)
                return;
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                    animator.GetBoneTransform(HumanBodyBones.Chest);
            rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightLowerArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftLowerArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.AbilityStarted += HandleAbilityStarted;
                combat.AbilityCompleted += HandleAbilityCompleted;
            }
            if (health != null)
            {
                health.HitReceived += HandleHit;
                health.Died += HandleDeath;
                health.ResetPerformed += HandleReset;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AbilityStarted -= HandleAbilityStarted;
                combat.AbilityCompleted -= HandleAbilityCompleted;
            }
            if (health != null)
            {
                health.HitReceived -= HandleHit;
                health.Died -= HandleDeath;
                health.ResetPerformed -= HandleReset;
            }
        }

        private void HandleAbilityStarted(AbilityPresentationEvent value)
        {
            activeAbility = value;
            abilityStartedAt = Time.time;
            abilityActive = true;
        }

        private void HandleAbilityCompleted(AbilityPresentationEvent value)
        {
            if (activeAbility.Index == value.Index)
                abilityActive = false;
        }

        private void HandleHit(Vector3 direction, int damage)
        {
            hitDirection = direction;
            hitWeight = 1f;
        }

        private void HandleDeath() => dead = true;

        private void HandleReset()
        {
            dead = false;
            hitWeight = 0f;
            abilityActive = false;
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isHuman)
                return;

            if (dead)
            {
                AddRotation(spine, 0f, 0f, 62f);
                AddRotation(chest, 22f, 0f, 18f);
                AddRotation(head, -18f, 0f, -22f);
                AddRotation(rightUpperArm, 12f, -20f, 34f);
                AddRotation(leftUpperArm, -8f, 18f, -28f);
                return;
            }

            if (abilityActive)
                ApplyAbilityPose();

            if (hitWeight > 0.001f)
            {
                float side = Vector3.Dot(transform.right, hitDirection);
                AddRotation(spine, -12f * hitWeight, 0f, -side * 18f * hitWeight);
                AddRotation(chest, -18f * hitWeight, side * 12f * hitWeight, 0f);
                hitWeight = Mathf.MoveTowards(hitWeight, 0f, Time.unscaledDeltaTime * 6.5f);
            }
        }

        private void ApplyAbilityPose()
        {
            float duration = Mathf.Max(0.05f, activeAbility.Duration);
            float t = Mathf.Clamp01((Time.time - abilityStartedAt) / duration);
            float weight = Mathf.Sin(t * Mathf.PI);
            switch (activeAbility.Type)
            {
                case AbilityExecutionType.Quickstep:
                    AddRotation(spine, 18f * weight, 0f, 0f);
                    AddRotation(chest, 12f * weight, 0f, 0f);
                    AddRotation(rightUpperArm, -14f * weight, 0f, 10f * weight);
                    AddRotation(leftUpperArm, -14f * weight, 0f, -10f * weight);
                    break;
                case AbilityExecutionType.PhaseDash:
                    AddRotation(spine, 26f * weight, 0f, 0f);
                    AddRotation(chest, 18f * weight, 0f, 0f);
                    AddRotation(rightUpperArm, -24f * weight, 0f, 16f * weight);
                    AddRotation(leftUpperArm, -24f * weight, 0f, -16f * weight);
                    break;
                case AbilityExecutionType.Charge:
                    AddRotation(spine, 34f * weight, 0f, -8f * weight);
                    AddRotation(chest, 24f * weight, 0f, -12f * weight);
                    AddRotation(rightUpperArm, -42f * weight, 12f * weight, 42f * weight);
                    AddRotation(leftUpperArm, 18f * weight, -8f * weight, -28f * weight);
                    break;
                case AbilityExecutionType.PhaseLunge:
                    AddRotation(spine, 20f * weight, 18f * weight, 0f);
                    AddRotation(chest, 12f * weight, 34f * weight, 0f);
                    AddRotation(rightUpperArm, -72f * weight, 18f * weight, 58f * weight);
                    AddRotation(rightLowerArm, 0f, 0f, -32f * weight);
                    break;
                case AbilityExecutionType.Guard:
                    AddRotation(spine, -9f * weight, 0f, 0f);
                    AddRotation(leftUpperArm, -65f * weight, 0f, -38f * weight);
                    AddRotation(rightUpperArm, -55f * weight, 0f, 38f * weight);
                    break;
                case AbilityExecutionType.Area:
                    AddRotation(spine, -20f * weight, 0f, 0f);
                    AddRotation(leftUpperArm, -100f * weight, 0f, -65f * weight);
                    AddRotation(rightUpperArm, -100f * weight, 0f, 65f * weight);
                    break;
                default:
                    ApplyMeleePose(activeAbility.Index, t, weight);
                    break;
            }
        }

        private void ApplyMeleePose(int index, float t, float weight)
        {
            float swing = Mathf.Lerp(-1f, 1f, Mathf.SmoothStep(0f, 1f, t));
            if (index == 1)
            {
                AddRotation(spine, -12f * weight, 0f, 0f);
                AddRotation(chest, -18f * weight, 0f, 0f);
                AddRotation(rightUpperArm, -115f * weight, 0f, 32f * weight);
                AddRotation(leftUpperArm, -92f * weight, 0f, -28f * weight);
                AddRotation(rightLowerArm, 0f, 0f, -48f * weight);
                AddRotation(leftLowerArm, 0f, 0f, 42f * weight);
                return;
            }
            AddRotation(spine, 0f, swing * 18f * weight, swing * 8f * weight);
            AddRotation(chest, 0f, swing * 38f * weight, swing * 14f * weight);
            AddRotation(rightUpperArm, -38f * weight, swing * 62f * weight, 72f * weight);
            AddRotation(rightLowerArm, 0f, 0f, -48f * weight);
        }

        private static void AddRotation(Transform bone, float x, float y, float z)
        {
            if (bone != null)
                bone.localRotation *= Quaternion.Euler(x, y, z);
        }
    }
}
