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
        [SerializeField] private PhasebreakFollowCamera followCamera;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 8f;
        [SerializeField, Min(0.01f)] private float acceleration = 42f;
        [SerializeField, Min(0.01f)] private float deceleration = 52f;
        [SerializeField, Min(0.01f)] private float rotationSmoothTime = 0.065f;
        [SerializeField, Min(1f)] private float keyboardTurnSpeed = 180f;
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
        [SerializeField] private bool allowAirDash = true;
        [SerializeField, Range(0, 3)] private int airDashesPerJump = 1;
        [SerializeField] private bool levelAirDash = true;

        private CharacterController controller;
        private PlayerBuildSystem build;
        private InputAction moveAction;
        private InputAction strafeAction;
        private InputAction jumpAction;
        private Vector3 planarVelocity;
        private Vector3 desiredMoveDirection;
        private Vector3 dashDirection;
        private float verticalVelocity;
        private float rotationVelocity;
        private float dashElapsed;
        private float dashDistanceTravelled;
        private float activeDashDistance;
        private float activeDashDuration;
        private float lastGroundedAt = float.NegativeInfinity;
        private float jumpBufferedUntil = float.NegativeInfinity;
        private int airDashesRemaining;
        private bool dashStartedAirborne;
        private bool isDashing;
        private bool isAbilityDriven;

        public bool IsDashing => isDashing;
        public bool CanDashNow => !isDashing && !isAbilityDriven && CanDash(controller != null && controller.isGrounded);
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float VerticalSpeed => verticalVelocity;
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
            return TryDash(dashDistance, dashDuration);
        }

        public bool TryDash(float distance, float duration)
        {
            if (isDashing)
                return false;

            bool grounded = controller.isGrounded;
            if (!CanDash(grounded))
                return false;

            BeginDash(grounded, Mathf.Max(0.1f, distance), Mathf.Max(0.01f, duration));
            return true;
        }

        public bool BeginAbilityMovement()
        {
            if (isDashing || isAbilityDriven)
                return false;
            isAbilityDriven = true;
            planarVelocity = Vector3.zero;
            return true;
        }

        public CollisionFlags MoveAbility(Vector3 planarDisplacement)
        {
            if (!isAbilityDriven)
                return CollisionFlags.None;
            return controller.Move(planarDisplacement + Vector3.up * (-2f * Time.deltaTime));
        }

        public void EndAbilityMovement()
        {
            isAbilityDriven = false;
            planarVelocity = Vector3.zero;
        }

        public bool CancelDashForAttack()
        {
            if (!isDashing)
                return false;

            isDashing = false;
            isAbilityDriven = false;
            planarVelocity = dashDirection * moveSpeed * 0.8f;
            return true;
        }

        public void AddCombatImpulse(Vector3 velocityChange)
        {
            velocityChange.y = 0f;
            planarVelocity = Vector3.ClampMagnitude(planarVelocity + velocityChange, moveSpeed * 2.4f);
        }

        public void ResetMotion()
        {
            isDashing = false;
            planarVelocity = Vector3.zero;
            desiredMoveDirection = Vector3.zero;
            verticalVelocity = -2f;
            jumpBufferedUntil = float.NegativeInfinity;
            airDashesRemaining = airDashesPerJump;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            build = GetComponent<PlayerBuildSystem>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (followCamera == null && cameraTransform != null)
                followCamera = cameraTransform.GetComponent<PhasebreakFollowCamera>();

            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            strafeAction = new InputAction("Strafe", InputActionType.Value);
            strafeAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q")
                .With("Positive", "<Keyboard>/e");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            airDashesRemaining = airDashesPerJump;
        }

        private void OnEnable()
        {
            moveAction.Enable();
            strafeAction.Enable();
            jumpAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            strafeAction.Disable();
            jumpAction.Disable();
        }

        private void OnDestroy()
        {
            moveAction.Dispose();
            strafeAction.Dispose();
            jumpAction.Dispose();
        }

        private void Update()
        {
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            float strafeInput = strafeAction.ReadValue<float>();
            bool mouseSteering = followCamera != null && followCamera.IsRightMouseHeld;
            if (!mouseSteering && Mathf.Abs(input.x) > 0.001f)
                transform.Rotate(0f, input.x * keyboardTurnSpeed * Time.deltaTime, 0f);

            desiredMoveDirection = GetMovementDirection(input, strafeInput, mouseSteering);
            float inputMagnitude = GetMovementMagnitude(input, strafeInput, mouseSteering);
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

            UpdateVerticalVelocity(grounded);

            if (isAbilityDriven)
                return;

            if (isDashing)
                UpdateDash();
            else
                UpdateMovement(inputMagnitude, grounded);

            UpdateRotation(mouseSteering);
        }

        private Vector3 GetMovementDirection(Vector2 input, float strafeInput, bool mouseSteering)
        {
            float forwardInput = input.y;
            float lateralInput = strafeInput;
            if (mouseSteering)
            {
                lateralInput = Mathf.Clamp(input.x + strafeInput, -1f, 1f);
                if (followCamera.MoveForwardRequested)
                    forwardInput = Mathf.Max(1f, forwardInput);

                return Vector3.ClampMagnitude(
                    followCamera.PlanarForward * forwardInput + followCamera.PlanarRight * lateralInput, 1f);
            }

            return Vector3.ClampMagnitude(
                transform.forward * forwardInput + transform.right * lateralInput, 1f);
        }

        private float GetMovementMagnitude(Vector2 input, float strafeInput, bool mouseSteering)
        {
            float forwardInput = input.y;
            float lateralInput = mouseSteering ? Mathf.Clamp(input.x + strafeInput, -1f, 1f) : strafeInput;
            if (mouseSteering && followCamera.MoveForwardRequested)
                forwardInput = Mathf.Max(1f, forwardInput);
            return Mathf.Clamp01(new Vector2(lateralInput, forwardInput).magnitude);
        }

        private void UpdateMovement(float inputMagnitude, bool grounded)
        {
            float buildSpeed = build != null ? build.MovementSpeedMultiplier : 1f;
            Vector3 targetVelocity = desiredMoveDirection * (moveSpeed * buildSpeed * inputMagnitude);
            float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
            if (!grounded)
                rate *= airControl;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);
            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private bool CanDash(bool grounded)
        {
            return grounded || (allowAirDash && airDashesRemaining > 0);
        }

        private void BeginDash(bool grounded, float distance, float duration)
        {
            isDashing = true;
            activeDashDistance = distance;
            activeDashDuration = duration;
            dashStartedAirborne = !grounded;
            if (dashStartedAirborne)
            {
                airDashesRemaining--;
                if (levelAirDash)
                    verticalVelocity = 0f;
            }
            dashElapsed = 0f;
            dashDistanceTravelled = 0f;
            dashDirection = desiredMoveDirection.sqrMagnitude > 0.001f
                ? desiredMoveDirection.normalized
                : transform.forward;
            planarVelocity = Vector3.zero;
            DashStarted?.Invoke();
        }

        private void UpdateDash()
        {
            dashElapsed = Mathf.Min(dashElapsed + Time.deltaTime, activeDashDuration);
            float normalizedTime = dashElapsed / activeDashDuration;
            float easedProgress = 1f - Mathf.Pow(1f - normalizedTime, 3f);
            float targetDistance = activeDashDistance * easedProgress;
            float frameDistance = targetDistance - dashDistanceTravelled;
            dashDistanceTravelled = targetDistance;

            // CharacterController.Move preserves collision response and naturally slides along walls.
            float verticalStep = dashStartedAirborne && levelAirDash ? 0f : verticalVelocity * Time.deltaTime;
            controller.Move(dashDirection * frameDistance + Vector3.up * verticalStep);

            if (dashElapsed >= activeDashDuration)
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

        private void UpdateRotation(bool mouseSteering)
        {
            Vector3 facing = isDashing
                ? dashDirection
                : mouseSteering && followCamera != null
                    ? followCamera.PlanarForward
                    : Vector3.zero;
            if (facing.sqrMagnitude < 0.001f)
                return;

            float targetYaw = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
