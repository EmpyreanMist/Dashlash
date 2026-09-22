using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MotionSpeedId = Animator.StringToHash("MotionSpeed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FreeFallId = Animator.StringToHash("FreeFall");

        [SerializeField] private PhasebreakPlayerMovement movement;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Animator animator;
        [SerializeField, Min(0.01f)] private float animationDampTime = 0.1f;
        [SerializeField, Min(0.01f)] private float animationReferenceSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpVelocityThreshold = 0.1f;
        [SerializeField, Min(0f)] private float fallVelocityThreshold = -0.1f;

        public Animator Animator => animator;

        public void Configure(PhasebreakPlayerMovement playerMovement, CharacterController controller,
            Animator visualAnimator)
        {
            movement = playerMovement;
            characterController = controller;
            animator = visualAnimator;
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
        }

        private void Update()
        {
            if (animator == null || movement == null || characterController == null)
                return;

            Vector3 velocity = characterController.velocity;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            bool grounded = movement.IsGrounded;
            float verticalSpeed = movement.VerticalSpeed;

            float locomotionSpeed = Mathf.Clamp(planarSpeed, 0f, animationReferenceSpeed);
            animator.SetFloat(SpeedId, locomotionSpeed, animationDampTime, Time.deltaTime);
            float visualSpeed = animator.GetFloat(SpeedId);
            float playbackSpeed = planarSpeed > 0.05f
                ? Mathf.Clamp(planarSpeed / Mathf.Max(0.5f, visualSpeed), 0.75f, 1.25f)
                : 1f;
            animator.SetFloat(MotionSpeedId, playbackSpeed, animationDampTime, Time.deltaTime);
            animator.SetBool(GroundedId, grounded);
            animator.SetBool(JumpId, !grounded && verticalSpeed > jumpVelocityThreshold);
            animator.SetBool(FreeFallId, !grounded && verticalSpeed < fallVelocityThreshold);
        }

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
