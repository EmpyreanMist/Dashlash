using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Phasebreak.Gameplay
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    public sealed class PhasebreakFollowCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;

        [Header("Third Person Orbit")]
        [SerializeField, Min(0.5f)] private float distance = 10f;
        [SerializeField, Min(0.01f)] private float minimumDistance = 0.05f;
        [SerializeField, Min(0.5f)] private float maximumDistance = 18f;
        [SerializeField, Range(-20f, 85f)] private float pitch = 32f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;
        [Tooltip("World-space zoom distance per mouse-wheel notch. Scroll input is normalized across common Unity input backends.")]
        [SerializeField, Min(0.01f)] private float zoomSensitivity = 3.5f;
        [SerializeField, Min(1f)] private float clickDragThreshold = 8f;
        [SerializeField, Range(-20f, 85f)] private float minimumPitch = -10f;
        [SerializeField, Range(-20f, 85f)] private float maximumPitch = 72f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.09f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.35f, 0f);
        [FormerlySerializedAs("isometricFieldOfView")]
        [SerializeField, Range(35f, 100f)] private float fieldOfView = 60f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.28f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.12f;
        [SerializeField, Min(0.01f)] private float collisionReleaseSmoothTime = 0.12f;

        private readonly RaycastHit[] collisionHits = new RaycastHit[16];
        private Camera controlledCamera;
        private InputAction lookAction;
        private InputAction leftMouseAction;
        private InputAction rightMouseAction;
        private InputAction zoomAction;
        private Vector3 trackedPivot;
        private Vector3 followVelocity;
        private Vector3 impulseOffset;
        private Vector2 savedCursorPosition;
        private float impulseStrength;
        private float currentDistance;
        private float collisionDistanceVelocity;
        private float leftDragDistance;
        private bool pointerCaptured;
        private bool leftDragging;
        private bool leftClickCandidate;
        private bool invertY;
        private float screenShakeAmount = 1f;
        private Transform flickerTarget;
        private float flickerBlend;
        public void BeginFlicker(Transform focus) => flickerTarget = focus;
        public void EndFlicker() => flickerTarget = null;

        public bool IsLeftMouseHeld => !GameplayInputFocus.GameplayInputBlocked && leftMouseAction != null && leftMouseAction.IsPressed();
        public bool IsRightMouseHeld => !GameplayInputFocus.GameplayInputBlocked && rightMouseAction != null && rightMouseAction.IsPressed();
        public bool MoveForwardRequested => IsLeftMouseHeld && IsRightMouseHeld;
        public bool LeftClickReleasedThisFrame { get; private set; }
        public Vector2 LeftClickPosition { get; private set; }
        public bool RightClickStartedThisFrame { get; private set; }
        public Vector2 RightClickPosition { get; private set; }
        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        public void AddImpulse(float strength) => impulseStrength = Mathf.Max(impulseStrength, strength * screenShakeAmount);
        public void SnapAfterTeleport() => SnapToTarget();
        public float MouseSensitivity { get => mouseSensitivity; set { mouseSensitivity = Mathf.Clamp(value, .01f, 1f); PhasebreakSettings.SetFloat("mouseSensitivity", mouseSensitivity); } }
        public float ZoomSensitivity { get => zoomSensitivity; set { zoomSensitivity = Mathf.Clamp(value, .1f, 10f); PhasebreakSettings.SetFloat("zoomSensitivity", zoomSensitivity); } }
        public bool InvertY { get => invertY; set { invertY = value; PhasebreakSettings.SetInt("invertY", value ? 1 : 0); } }
        public float ScreenShakeAmount { get => screenShakeAmount; set { screenShakeAmount = Mathf.Clamp01(value); PhasebreakSettings.SetFloat("screenShake", screenShakeAmount); } }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        private void Awake()
        {
            mouseSensitivity = Mathf.Clamp(PhasebreakSettings.GetFloat("mouseSensitivity", mouseSensitivity), .01f, 1f);
            zoomSensitivity = Mathf.Clamp(PhasebreakSettings.GetFloat("zoomSensitivity", zoomSensitivity), .1f, 10f);
            invertY = PhasebreakSettings.GetInt("invertY", 0) != 0;
            screenShakeAmount = Mathf.Clamp01(PhasebreakSettings.GetFloat("screenShake", 1f));
            controlledCamera = GetComponent<Camera>();
            lookAction = new InputAction("Orbit Camera", InputActionType.Value, "<Mouse>/delta");
            leftMouseAction = new InputAction("Orbit Without Turning", InputActionType.Button, "<Mouse>/leftButton");
            rightMouseAction = new InputAction("Orbit And Turn", InputActionType.Button, "<Mouse>/rightButton");
            zoomAction = new InputAction("Camera Zoom", InputActionType.Value, "<Mouse>/scroll");
        }

        private void OnEnable()
        {
            lookAction.Enable();
            leftMouseAction.Enable();
            rightMouseAction.Enable();
            zoomAction.Enable();
        }

        private void Start()
        {
            distance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
            SnapToTarget();
        }

        private void Update()
        {
            LeftClickReleasedThisFrame = false;
            RightClickStartedThisFrame = false;
            if (GameplayInputFocus.GameplayInputBlocked || PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen)
            {
                leftClickCandidate = false;
                leftDragging = false;
                SetPointerCaptured(false);
                return;
            }
            bool framePointerInput = IsLeftMouseHeld || IsRightMouseHeld ||
                                     leftMouseAction.WasReleasedThisFrame() || rightMouseAction.WasReleasedThisFrame();
            if (HudFrameDragHandle.IsDraggingAny ||
                (framePointerInput && HudFrameDragHandle.IsPointerOverFrame()))
            {
                leftClickCandidate = false;
                leftDragging = false;
                SetPointerCaptured(false);
                return;
            }
            if (rightMouseAction.WasPressedThisFrame())
            {
                RightClickStartedThisFrame = true;
                RightClickPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            }
            Vector2 lookDelta = lookAction.ReadValue<Vector2>();

            if (leftMouseAction.WasPressedThisFrame())
            {
                LeftClickPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
                leftDragDistance = 0f;
                leftDragging = false;
                leftClickCandidate = true;
            }

            if (IsLeftMouseHeld)
            {
                leftDragDistance += lookDelta.magnitude;
                if (leftDragDistance >= clickDragThreshold)
                {
                    leftDragging = true;
                    leftClickCandidate = false;
                }
                if (IsRightMouseHeld)
                    leftClickCandidate = false;
            }

            if (leftMouseAction.WasReleasedThisFrame())
            {
                LeftClickReleasedThisFrame = leftClickCandidate;
                leftClickCandidate = false;
                leftDragging = false;
            }

            bool orbiting = IsRightMouseHeld || (IsLeftMouseHeld && leftDragging);
            SetPointerCaptured(orbiting);

            if (orbiting)
            {
                yaw += lookDelta.x * mouseSensitivity;
                pitch = Mathf.Clamp(pitch + lookDelta.y * mouseSensitivity * (invertY ? 1f : -1f), minimumPitch, maximumPitch);
            }

            Vector2 scroll = zoomAction.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) > 0.01f)
            {
                // Windows commonly reports 120 per wheel notch while some devices/backends report 1.
                // Normalize both conventions while retaining fractional high-resolution trackpad input.
                float scrollSteps = Mathf.Abs(scroll.y) >= 10f ? scroll.y / 120f : scroll.y;
                distance = Mathf.Clamp(distance - scrollSteps * zoomSensitivity, minimumDistance, maximumDistance);
            }

            if (IsRightMouseHeld && target != null && flickerTarget == null)
                target.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desiredPivot = target.position + lookOffset;
            flickerBlend = Mathf.MoveTowards(flickerBlend, flickerTarget != null ? 1f : 0f, Time.deltaTime * 5f);
            if (flickerTarget != null) desiredPivot = Vector3.Lerp(desiredPivot,
                flickerTarget.position + lookOffset, .85f);
            trackedPivot = Vector3.SmoothDamp(trackedPivot, desiredPivot, ref followVelocity,
                Mathf.Lerp(followSmoothTime, .2f, flickerBlend));

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            float collisionDistance = FindCollisionDistance(trackedPivot, orbitRotation);
            if (collisionDistance < currentDistance)
            {
                currentDistance = collisionDistance;
                collisionDistanceVelocity = 0f;
            }
            else
            {
                currentDistance = Mathf.SmoothDamp(currentDistance, collisionDistance,
                    ref collisionDistanceVelocity, collisionReleaseSmoothTime);
            }

            UpdateImpulse();
            transform.SetPositionAndRotation(
                trackedPivot - orbitRotation * Vector3.forward * currentDistance + impulseOffset,
                orbitRotation);

            if (controlledCamera != null)
                controlledCamera.fieldOfView = Mathf.Lerp(controlledCamera.fieldOfView, fieldOfView,
                    12f * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            EndFlicker();
            flickerBlend = 0f;
            lookAction.Disable();
            leftMouseAction.Disable();
            rightMouseAction.Disable();
            zoomAction.Disable();
            SetPointerCaptured(false);
        }

        private void OnDestroy()
        {
            lookAction.Dispose();
            leftMouseAction.Dispose();
            rightMouseAction.Dispose();
            zoomAction.Dispose();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SetPointerCaptured(false);
        }

        private float FindCollisionDistance(Vector3 pivot, Quaternion orbitRotation)
        {
            Vector3 direction = -(orbitRotation * Vector3.forward);
            int count = Physics.SphereCastNonAlloc(pivot, collisionRadius, direction, collisionHits, distance,
                collisionLayers, QueryTriggerInteraction.Ignore);
            float nearestDistance = distance;

            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = collisionHits[i].collider;
                if (hitCollider == null)
                    continue;
                if (target != null && (hitCollider.transform == target || hitCollider.transform.IsChildOf(target)))
                    continue;
                if (flickerTarget != null && hitCollider.transform.IsChildOf(flickerTarget)) continue;

                nearestDistance = Mathf.Min(nearestDistance,
                    Mathf.Max(0.2f, collisionHits[i].distance - collisionPadding));
            }

            return nearestDistance;
        }

        private void UpdateImpulse()
        {
            impulseOffset = Vector3.Lerp(impulseOffset, Random.insideUnitSphere * impulseStrength,
                18f * Time.unscaledDeltaTime);
            impulseStrength = Mathf.MoveTowards(impulseStrength, 0f, 8f * Time.unscaledDeltaTime);
        }

        private void SnapToTarget()
        {
            if (target == null)
                return;

            trackedPivot = target.position + lookOffset;
            followVelocity = Vector3.zero;
            impulseOffset = Vector3.zero;
            currentDistance = Mathf.Clamp(distance, minimumDistance, maximumDistance);
            collisionDistanceVelocity = 0f;
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(
                trackedPivot - orbitRotation * Vector3.forward * currentDistance,
                orbitRotation);
        }

        private void SetPointerCaptured(bool captured)
        {
            if (pointerCaptured == captured)
                return;

            pointerCaptured = captured;
            if (captured)
            {
                if (Mouse.current != null)
                    savedCursorPosition = Mouse.current.position.ReadValue();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                return;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (Mouse.current != null)
                Mouse.current.WarpCursorPosition(savedCursorPosition);
        }
    }
}
