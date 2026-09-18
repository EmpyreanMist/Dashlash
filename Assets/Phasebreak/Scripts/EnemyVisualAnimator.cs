using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EnemyVisualAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        [SerializeField] private MeleeEnemy enemy;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0.01f)] private float poseBlendSpeed = 10f;

        private Transform spine;
        private Transform chest;
        private Transform rightUpperArm;
        private Transform leftUpperArm;
        private Vector3 baseRootPosition;
        private Quaternion baseRootRotation;
        private float attackPose;
        private float staggerPose;

        public Animator Animator => animator;

        public void Configure(MeleeEnemy owner, CharacterController controller, Animator visualAnimator,
            Transform modelRoot)
        {
            enemy = owner;
            characterController = controller;
            animator = visualAnimator;
            visualRoot = modelRoot;
        }

        private void Awake()
        {
            enemy ??= GetComponent<MeleeEnemy>();
            characterController ??= GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            if (visualRoot == null && animator != null)
                visualRoot = animator.transform;
            if (visualRoot != null)
            {
                baseRootPosition = visualRoot.localPosition;
                baseRootRotation = visualRoot.localRotation;
            }
            if (animator != null)
            {
                animator.applyRootMotion = false;
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                leftUpperArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            }
        }

        private void Update()
        {
            if (enemy == null || animator == null)
                return;
            Vector3 velocity = characterController != null ? characterController.velocity : Vector3.zero;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            animator.SetFloat(SpeedId, enemy.IsMoving ? planarSpeed : 0f, .1f, Time.deltaTime);
            animator.speed = enemy.IsDead ? 0f : 1f;

            float attackTarget = enemy.IsCasting ? -Mathf.Lerp(.15f, 1f, enemy.CastProgress) :
                enemy.IsAttackActive ? 1f : 0f;
            attackPose = Mathf.MoveTowards(attackPose, attackTarget, poseBlendSpeed * Time.deltaTime);
            staggerPose = Mathf.MoveTowards(staggerPose, enemy.IsStaggered ? 1f : 0f,
                poseBlendSpeed * 1.4f * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (visualRoot == null)
                return;
            visualRoot.localPosition = baseRootPosition + new Vector3(0f, 0f,
                Mathf.Max(0f, attackPose) * .14f - staggerPose * .1f);
            visualRoot.localRotation = baseRootRotation * Quaternion.Euler(
                staggerPose * -12f, attackPose * 8f, Mathf.Max(0f, attackPose) * -4f);

            float windup = Mathf.Max(0f, -attackPose);
            float strike = Mathf.Max(0f, attackPose);
            AddRotation(spine, new Vector3(windup * -15f + strike * 18f, strike * 8f, 0f));
            AddRotation(chest, new Vector3(windup * -10f + strike * 12f, 0f, staggerPose * 10f));
            AddRotation(rightUpperArm, new Vector3(windup * -55f + strike * 65f, 0f, windup * -25f));
            AddRotation(leftUpperArm, new Vector3(windup * -30f + strike * 42f, 0f, windup * 18f));
        }

        private static void AddRotation(Transform bone, Vector3 euler)
        {
            if (bone != null)
                bone.localRotation *= Quaternion.Euler(euler);
        }
    }
}
