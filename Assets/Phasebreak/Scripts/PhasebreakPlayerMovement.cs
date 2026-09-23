using System;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private PlayerHealth health;
        private PlayerBuildSystem build;
        private InputAction forwardAction;
        private InputAction backwardAction;
        private InputAction turnLeftAction;
        private InputAction turnRightAction;
        private InputAction strafeLeftAction;
        private InputAction strafeRightAction;
        private InputAction jumpAction;
        private InputAction autoRunAction;
        private bool autoRun;
        public bool AutoRun => autoRun;
        private InputAction teleportAction;
        private Vector3 planarVelocity;
        private Vector3 desiredMoveDirection;
        private Vector3 dashDirection;
        private float verticalVelocity;
        private float rotationVelocity;
        private float dashElapsed;
        private float dashDistanceTravelled;
        private float activeDashDistance;
        private float activeDashDuration;
        private float activeDashExitSpeedFraction;
        private float lastGroundedAt = float.NegativeInfinity;
        private float jumpBufferedUntil = float.NegativeInfinity;
        private int airDashesRemaining;
        private bool dashStartedAirborne;
        private bool isDashing;
        private bool isAbilityDriven;
        private bool inputWasBlocked;
        private float debugSpeedMultiplier = 1f;
        private bool debugFly;
        private bool debugNoClip;
        private bool debugTeleportClickConsumed;
        private bool debugLeftMouseWasPressed;
        private int debugTeleportReleaseFrame = -1;
        private Vector3 lastSafeGroundedPosition;
        private bool hasSafeGroundedPosition;

        public bool IsDashing => isDashing;
        public bool CanDashNow => !isDashing && !isAbilityDriven && CanDash(controller != null && controller.isGrounded);
        public bool CanQuickstepNow => !isDashing && !isAbilityDriven && controller != null && controller.isGrounded;
        public bool IsGrounded => controller != null && controller.isGrounded;
        public float VerticalSpeed => verticalVelocity;
        public float DebugSpeedMultiplier => debugSpeedMultiplier;
        public bool DebugFly => debugFly;
        public bool DebugNoClip => debugNoClip;
        public bool DebugTeleportClickConsumed => debugTeleportClickConsumed;
        public bool HasSafeGroundedPosition => hasSafeGroundedPosition;
        public Vector3 LastSafeGroundedPosition => lastSafeGroundedPosition;
        public event Action DashStarted;

        public void SetDebugSpeed(float multiplier) => debugSpeedMultiplier = Mathf.Clamp(multiplier, .25f, 5f);

        public void SetDebugFly(bool enabled)
        {
            debugFly = enabled;
            ResetMotion();
        }

        public void SetDebugNoClip(bool enabled)
        {
            if (debugNoClip == enabled) return;
            debugNoClip = enabled;
            ResetMotion();
            controller.enabled = !enabled;
        }

        public void DebugTeleport(Vector3 position, Quaternion? rotation = null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            if (rotation.HasValue) transform.rotation = rotation.Value;
            ResetMotion();
            controller.enabled = wasEnabled;
            followCamera?.SnapAfterTeleport();
        }

        public bool TryDebugCursorTeleport(Camera camera, Vector2 screenPoint)
        {
            if (health == null || !health.DebugGodMode || camera == null) return false;
            Ray ray = camera.ScreenPointToRay(screenPoint);
            RaycastHit[] hits = Physics.RaycastAll(ray, 2000f, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null || hit.collider.transform == transform ||
                    hit.collider.transform.IsChildOf(transform)) continue;
                Vector3 arrival = hit.point + hit.normal * (controller.radius + .15f) +
                    Vector3.up * (controller.height * .5f + .1f);
                DebugTeleport(arrival);
                return true;
            }
            return false;
        }

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

            BeginDash(grounded, Mathf.Max(0.1f, distance), Mathf.Max(0.01f, duration), 0.55f);
            return true;
        }

        public bool TryQuickstep(float distance, float duration)
        {
            if (!CanQuickstepNow)
                return false;
            BeginDash(true, Mathf.Max(0.1f, distance), Mathf.Max(0.01f, duration), 0f);
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
            if (!isAbilityDriven || GameplayInputFocus.GameplayInputBlocked)
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
            health = GetComponent<PlayerHealth>();
            build = GetComponent<PlayerBuildSystem>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (followCamera == null && cameraTransform != null)
                followCamera = cameraTransform.GetComponent<PhasebreakFollowCamera>();

            forwardAction = PhasebreakSettings.Button("move.forward", "Move forward");
            backwardAction = PhasebreakSettings.Button("move.backward", "Move backward");
            turnLeftAction = PhasebreakSettings.Button("move.left", "Turn left");
            turnRightAction = PhasebreakSettings.Button("move.right", "Turn right");
            strafeLeftAction = PhasebreakSettings.Button("strafe.left", "Strafe left");
            strafeRightAction = PhasebreakSettings.Button("strafe.right", "Strafe right");
            jumpAction = PhasebreakSettings.Button("jump", "Jump");
            autoRunAction = PhasebreakSettings.Button("autorun", "Toggle auto run");
            teleportAction = PhasebreakSettings.Button("teleport", "Godmode cursor teleport");
            airDashesRemaining = airDashesPerJump;
        }

        private void OnEnable()
        {
            forwardAction.Enable();
            backwardAction.Enable();
            turnLeftAction.Enable();
            turnRightAction.Enable();
            strafeLeftAction.Enable();
            strafeRightAction.Enable();
            jumpAction.Enable();
            autoRunAction.Enable();
            teleportAction.Enable();
        }

        private void OnDisable()
        {
            debugFly = false;
            debugNoClip = false;
            debugSpeedMultiplier = 1f;
            debugTeleportClickConsumed = false;
            debugLeftMouseWasPressed = false;
            debugTeleportReleaseFrame = -1;
            if (controller != null) controller.enabled = true;
            forwardAction.Disable();
            backwardAction.Disable();
            turnLeftAction.Disable();
            turnRightAction.Disable();
            strafeLeftAction.Disable();
            strafeRightAction.Disable();
            jumpAction.Disable();
            autoRunAction.Disable();
            autoRun = false;
            teleportAction.Disable();
        }

        private void OnDestroy()
        {
            PhasebreakSettings.Unregister(forwardAction);
            PhasebreakSettings.Unregister(backwardAction);
            PhasebreakSettings.Unregister(turnLeftAction);
            PhasebreakSettings.Unregister(turnRightAction);
            PhasebreakSettings.Unregister(strafeLeftAction);
            PhasebreakSettings.Unregister(strafeRightAction);
            PhasebreakSettings.Unregister(jumpAction);
            PhasebreakSettings.Unregister(autoRunAction);
            PhasebreakSettings.Unregister(teleportAction);
            forwardAction.Dispose();
            backwardAction.Dispose();
            turnLeftAction.Dispose();
            turnRightAction.Dispose();
            strafeLeftAction.Dispose();
            strafeRightAction.Dispose();
            jumpAction.Dispose();
            autoRunAction.Dispose();
            teleportAction.Dispose();
        }

        private void Update()
        {
            if (health != null && !health.IsAlive)
            {
                autoRun = false;
                ResetMotion();
                return;
            }
            bool blocked = GameplayInputFocus.GameplayInputBlocked;
            bool leftMouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool leftMousePressed = leftMouseHeld && !debugLeftMouseWasPressed;
            debugLeftMouseWasPressed = leftMouseHeld;
            if (debugTeleportClickConsumed && !leftMouseHeld)
            {
                if (debugTeleportReleaseFrame < 0) debugTeleportReleaseFrame = Time.frameCount;
                else if (Time.frameCount > debugTeleportReleaseFrame)
                {
                    debugTeleportClickConsumed = false;
                    debugTeleportReleaseFrame = -1;
                }
            }
            if (!blocked && health != null && health.DebugGodMode && Keyboard.current != null &&
                teleportAction.IsPressed() && Mouse.current != null &&
                leftMousePressed &&
                !PhasebreakInventoryHud.IsMajorMenuOpen && !WorldQuestHud.IsWorldMenuOpen &&
                !HudFrameDragHandle.IsPointerOverFrame() &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                debugTeleportClickConsumed = true;
                debugTeleportReleaseFrame = -1;
                Camera camera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : Camera.main;
                if (TryDebugCursorTeleport(camera, Mouse.current.position.ReadValue())) return;
            }
            if (blocked && !inputWasBlocked)
                ResetMotion();
            inputWasBlocked = blocked;
            if (!blocked && autoRunAction.WasPressedThisFrame()) autoRun = !autoRun;
            Vector2 input = blocked ? Vector2.zero : new Vector2(
                (turnRightAction.IsPressed() ? 1f : 0f) - (turnLeftAction.IsPressed() ? 1f : 0f),
                (forwardAction.IsPressed() ? 1f : 0f) - (backwardAction.IsPressed() ? 1f : 0f));
            if (!blocked && autoRun)
            {
                if (input.y < -.01f) autoRun = false;
                else input.y = 1f;
            }
            float strafeInput = blocked ? 0f : (strafeRightAction.IsPressed() ? 1f : 0f) -
                (strafeLeftAction.IsPressed() ? 1f : 0f);
            if ((debugFly || debugNoClip) && strafeLeftAction.IsPressed())
                strafeInput = Mathf.Max(0f, strafeInput);
            bool mouseSteering = !blocked && followCamera != null && followCamera.IsRightMouseHeld;
            if (!mouseSteering && Mathf.Abs(input.x) > 0.001f)
                transform.Rotate(0f, input.x * keyboardTurnSpeed * Time.deltaTime, 0f);

            desiredMoveDirection = GetMovementDirection(input, strafeInput, mouseSteering);
            float inputMagnitude = GetMovementMagnitude(input, strafeInput, mouseSteering);
            bool grounded = controller.isGrounded;

            if (grounded)
            {
                if (!debugFly && !debugNoClip)
                {
                    lastSafeGroundedPosition = transform.position;
                    hasSafeGroundedPosition = true;
                }
                lastGroundedAt = Time.time;
                airDashesRemaining = airDashesPerJump;
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
            }

            if (!blocked && jumpAction.WasPressedThisFrame())
                QueueJump();

            if (!debugFly && !debugNoClip && Time.time <= jumpBufferedUntil)
                TryJump();

            if (!debugFly && !debugNoClip) UpdateVerticalVelocity(grounded);
            else verticalVelocity = 0f;

            if (debugFly || debugNoClip)
            {
                float rise = blocked || Keyboard.current == null ? 0f : (jumpAction.IsPressed() ? 1f : 0f) -
                    (strafeLeftAction.IsPressed() || Keyboard.current.leftCtrlKey.isPressed ||
                     Keyboard.current.rightCtrlKey.isPressed ? 1f : 0f);
                Vector3 motion = (desiredMoveDirection * inputMagnitude + Vector3.up * rise) *
                    (moveSpeed * debugSpeedMultiplier * Time.deltaTime);
                if (debugNoClip) transform.position += motion;
                else controller.Move(motion);
                UpdateRotation(mouseSteering);
                return;
            }

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
            Vector3 targetVelocity = desiredMoveDirection * (moveSpeed * buildSpeed * debugSpeedMultiplier * inputMagnitude);
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

        private void BeginDash(bool grounded, float distance, float duration, float exitSpeedFraction)
        {
            isDashing = true;
            activeDashDistance = distance;
            activeDashDuration = duration;
            activeDashExitSpeedFraction = Mathf.Max(0f, exitSpeedFraction);
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
                planarVelocity = dashDirection * moveSpeed * activeDashExitSpeedFraction;
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
