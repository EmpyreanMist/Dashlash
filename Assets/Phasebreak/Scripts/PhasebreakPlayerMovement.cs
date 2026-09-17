using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PhasebreakPlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0.01f)] private float acceleration = 42f;
        [SerializeField, Min(0.01f)] private float deceleration = 52f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.065f;
        [SerializeField] private float gravity = -28f;

        [Header("Jump")]
        [SerializeField, Min(0.1f)] private float jumpHeight = 2.4f;
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 1.65f;
        [SerializeField, Min(1f)] private float releasedJumpGravityMultiplier = 2.35f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.82f;

        [Header("Dash")]
        [SerializeField, Min(0.1f)] private float dashDistance = 4.5f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.45f;
        [SerializeField] private bool allowAirDash = true;
        [SerializeField, Range(0, 3)] private int airDashesPerJump = 1;
        [SerializeField] private bool levelAirDash = true;

        private CharacterController controller;
        private InputAction moveAction;
        private InputAction dashAction;
        private InputAction jumpAction;
        private Vector3 planarVelocity;
        private Vector3 desiredMoveDirection;
        private Vector3 dashDirection;
        private float verticalVelocity;
        private float rotationVelocity;
        private float dashElapsed;
        private float dashDistanceTravelled;
        private float lastDashStartedAt = float.NegativeInfinity;
        private float lastGroundedAt = float.NegativeInfinity;
        private float jumpBufferedUntil = float.NegativeInfinity;
        private int airDashesRemaining;
        private bool dashStartedAirborne;
        private bool isDashing;

        public bool IsDashing => isDashing;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float VerticalSpeed => verticalVelocity;
        public float DashCooldownRemaining => Mathf.Max(0f, dashCooldown - (Time.time - lastDashStartedAt));
        public event Action DashStarted;

        public void QueueJump()
        {
            jumpBufferedUntil = Time.time + jumpBufferTime;
        }

        public bool TryJump()
        {
            bool canJump = controller.isGrounded || Time.time <= lastGroundedAt + coyoteTime;
            if (!canJump)
                return false;

            PerformJump();
            return true;
        }

        public bool TryDash()
        {
            if (isDashing)
                return false;

            bool grounded = controller.isGrounded;
            if (!CanDash(grounded))
                return false;

            BeginDash(grounded);
            return true;
        }

        public bool CancelDashForAttack()
        {
            if (!isDashing)
                return false;

            isDashing = false;
            planarVelocity = dashDirection * moveSpeed * 0.8f;
            return true;
        }

        public void AddCombatImpulse(Vector3 velocityChange)
        {
            velocityChange.y = 0f;
            planarVelocity = Vector3.ClampMagnitude(planarVelocity + velocityChange, moveSpeed * 2.4f);
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            dashAction = new InputAction("Dash", InputActionType.Button, "<Keyboard>/leftShift");
            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            airDashesRemaining = airDashesPerJump;
        }

        private void OnEnable()
        {
            moveAction.Enable();
            dashAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            dashAction.Disable();
            jumpAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Dispose();
            dashAction.Dispose();
            jumpAction.Dispose();
        }

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            desiredMoveDirection = GetCameraRelativeDirection(input);
            bool grounded = controller.isGrounded;

            if (grounded)
            {
                lastGroundedAt = Time.time;
                airDashesRemaining = airDashesPerJump;
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
            }

            if (jumpAction.WasPressedThisFrame())
                QueueJump();

            if (Time.time <= jumpBufferedUntil)
                TryJump();

            if (dashAction.WasPressedThisFrame())
                TryDash();

            UpdateVerticalVelocity(grounded);

            if (isDashing)
                UpdateDash();
            else
                UpdateMovement(input.magnitude, grounded);

            UpdateRotation();
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private void UpdateMovement(float inputMagnitude, bool grounded)
        {
            Vector3 targetVelocity = desiredMoveDirection * (moveSpeed * inputMagnitude);
            float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
            if (!grounded)
                rate *= airControl;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private bool CanDash(bool grounded)
        {
            if (Time.time < lastDashStartedAt + dashCooldown)
                return false;

            return grounded || (allowAirDash && airDashesRemaining > 0);
        }

        private void BeginDash(bool grounded)
        {
            isDashing = true;
            dashStartedAirborne = !grounded;
            if (dashStartedAirborne)
            {
                airDashesRemaining--;
                if (levelAirDash)
                    verticalVelocity = 0f;
            }
            dashElapsed = 0f;
            dashDistanceTravelled = 0f;
            lastDashStartedAt = Time.time;
            dashDirection = desiredMoveDirection.sqrMagnitude > 0.001f
                ? desiredMoveDirection.normalized
                : transform.forward;
            planarVelocity = Vector3.zero;
            DashStarted?.Invoke();
        }

        private void UpdateDash()
        {
            dashElapsed = Mathf.Min(dashElapsed + Time.deltaTime, dashDuration);
            float normalizedTime = dashElapsed / dashDuration;
            float easedProgress = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            float targetDistance = dashDistance * easedProgress;
            float frameDistance = targetDistance - dashDistanceTravelled;
            dashDistanceTravelled = targetDistance;

            // CharacterController.Move preserves collision response and naturally slides along walls.
            float verticalStep = dashStartedAirborne && levelAirDash ? 0f : verticalVelocity * Time.deltaTime;
            controller.Move(dashDirection * frameDistance + Vector3.up * verticalStep);

            if (dashElapsed >= dashDuration)
            {
                isDashing = false;
                planarVelocity = dashDirection * moveSpeed * 0.55f;
            }
        }

        private void PerformJump()
        {
            jumpBufferedUntil = float.NegativeInfinity;
            lastGroundedAt = float.NegativeInfinity;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        private void UpdateVerticalVelocity(bool grounded)
        {
            if (grounded && verticalVelocity <= 0f)
                return;

            float multiplier = 1f;
            if (verticalVelocity < 0f)
                multiplier = fallGravityMultiplier;
            else if (!jumpAction.IsPressed())
                multiplier = releasedJumpGravityMultiplier;

            verticalVelocity += gravity * multiplier * Time.deltaTime;
        }

        private void UpdateRotation()
        {
            Vector3 facing = isDashing ? dashDirection : desiredMoveDirection;
            if (facing.sqrMagnitude < 0.001f)
                return;

            float targetYaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
