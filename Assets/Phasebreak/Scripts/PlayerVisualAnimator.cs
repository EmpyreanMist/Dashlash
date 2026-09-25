using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FreeFallId = Animator.StringToHash("FreeFall");
        private static readonly int MoveXId = Animator.StringToHash("MoveX");
        private static readonly int MoveZId = Animator.StringToHash("MoveZ");

        [SerializeField] private PhasebreakPlayerMovement movement;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float directionDampTime = 0.08f;
        [SerializeField, Min(0.01f)] private float stopDampTime = 0.05f;
        [SerializeField, Min(0.01f)] private float directionReferenceSpeed = 8f;
        [Header("Authored stride speed in metres per second")]
        [SerializeField, Min(0.1f)] private float forwardReferenceSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float backwardReferenceSpeed = 2.4f;
        [SerializeField, Min(0.1f)] private float strafeReferenceSpeed = 2.2f;
        [SerializeField, Min(0f)] private float jumpVelocityThreshold = 0.1f;
        [SerializeField] private float fallVelocityThreshold = -0.1f;

        private PlayerHealth health;
        private PlayerCombat combat;
        private PlayerCombatPresentation combatPresentation;
        private Avatar cachedAvatar;
        private Transform spine, chest, head, leftShoulder, rightShoulder;
        private Quaternion spineBase, chestBase, headBase, leftBase, rightBase;
        private float idleWeight;
        private bool idleApplied;

        public Animator Animator => animator;

        public void Configure(PhasebreakPlayerMovement playerMovement, CharacterController controller,
            Animator visualAnimator)
        {
            movement = playerMovement;
            characterController = controller;
            animator = visualAnimator;
            if (animator != null)
                animator.applyRootMotion = false;
        }

        private void Awake()
        {
            if (movement == null)
                movement = GetComponentInParent<PhasebreakPlayerMovement>();
            if (characterController == null && movement != null)
                characterController = movement.GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponent<Animator>();

            if (animator != null)
                animator.applyRootMotion = false;
            health = GetComponentInParent<PlayerHealth>();
            combat = GetComponentInParent<PlayerCombat>();
            combatPresentation = GetComponent<PlayerCombatPresentation>();
        }

        private void Update()
        {
            RestoreIdle();
            if (animator == null || movement == null || characterController == null)
                return;

            Vector3 velocity = characterController.velocity;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            bool grounded = movement.IsGrounded;
            float verticalSpeed = movement.VerticalSpeed;

            // Use actual displacement after collision, in the gameplay owner's facing frame.
            // Neither the input vector nor the visual rig's imported axes own movement.
            velocity.y = 0f;
            Vector3 local = movement.transform.InverseTransformDirection(velocity);
            Vector2 blend = Vector2.ClampMagnitude(new Vector2(local.x, local.z) / directionReferenceSpeed, 1f);
            float damp = planarSpeed < .05f ? stopDampTime : directionDampTime;
            animator.SetFloat(MoveXId, blend.x, damp, Time.deltaTime);
            animator.SetFloat(MoveZId, blend.y, damp, Time.deltaTime);
            animator.SetFloat(SpeedId, Mathf.Clamp01(planarSpeed / directionReferenceSpeed), damp, Time.deltaTime);
            float lateral = Mathf.Abs(local.x);
            float longitudinal = Mathf.Abs(local.z);
            float reference = (lateral * strafeReferenceSpeed + longitudinal *
                (local.z < 0f ? backwardReferenceSpeed : forwardReferenceSpeed)) / Mathf.Max(.001f, lateral + longitudinal);
            float playbackSpeed = planarSpeed > .05f ? Mathf.Clamp(planarSpeed / Mathf.Max(.1f, reference), .65f, 3f) : 1f;
            animator.SetFloat(MotionSpeedId, playbackSpeed, .08f, Time.deltaTime);
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(JumpId, !grounded && verticalSpeed > jumpVelocityThreshold);
            animator.SetBool(FreeFallId, !grounded && verticalSpeed < fallVelocityThreshold);
        }

        private void LateUpdate()
        {
            if (animator == null || !animator.isHuman || movement == null || characterController == null) return;
            if (cachedAvatar != animator.avatar)
            {
                cachedAvatar = animator.avatar;
                spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ?? animator.GetBoneTransform(HumanBodyBones.Chest);
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            }
            Vector3 v = characterController.velocity;
            bool idle = movement.IsGrounded && !movement.IsDashing && v.x*v.x + v.z*v.z < .0025f &&
                (health == null || health.IsAlive) && (combat == null || !combat.IsAttacking) &&
                (combatPresentation == null || !combatPresentation.IsPresentingAbility);
            idleWeight = Mathf.MoveTowards(idleWeight, idle ? 1f : 0f, Time.deltaTime * (idle ? 2f : 10f));
            // Combat owns the final pose. Do not layer breath over an attack/death even while fading out.
            if (!idle || idleWeight < .001f || spine == null || chest == null || head == null) return;
            float breath = Mathf.Sin(Time.time * (2f * Mathf.PI / 3.8f)) * idleWeight;
            float settle = Mathf.Sin(Time.time * (2f * Mathf.PI / 9.7f)) * idleWeight;
            spineBase=spine.localRotation; chestBase=chest.localRotation; headBase=head.localRotation;
            spine.localRotation *= Quaternion.Euler(.5f*breath, .2f*settle, .35f*settle);
            chest.localRotation *= Quaternion.Euler(.9f*breath, 0f, -.2f*settle);
            head.localRotation *= Quaternion.Euler(-.35f*breath, .3f*settle, -.15f*settle);
            if(leftShoulder!=null) { leftBase=leftShoulder.localRotation; leftShoulder.localRotation *= Quaternion.Euler(0f,0f,.35f*breath); }
            if(rightShoulder!=null) { rightBase=rightShoulder.localRotation; rightShoulder.localRotation *= Quaternion.Euler(0f,0f,-.35f*breath); }
            idleApplied=true;
        }

        private void RestoreIdle()
        {
            if(!idleApplied) return;
            if(spine!=null) spine.localRotation=spineBase;
            if(chest!=null) chest.localRotation=chestBase;
            if(head!=null) head.localRotation=headBase;
            if(leftShoulder!=null) leftShoulder.localRotation=leftBase;
            if(rightShoulder!=null) rightShoulder.localRotation=rightBase;
            idleApplied=false;
        }

        private void OnDisable() { RestoreIdle(); idleWeight=0f; }

        // Starter Assets locomotion clips contain these events. They intentionally remain visual-only
        // hooks until Phasebreak has a dedicated footsteps/landing audio and VFX system.
        private void OnFootstep(AnimationEvent animationEvent)
        {
        }

        private void OnLand(AnimationEvent animationEvent)
        {
        }
    }
}
