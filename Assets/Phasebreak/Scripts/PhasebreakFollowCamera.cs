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
        [SerializeField, Min(0.5f)] private float minimumDistance = 2.5f;
        [SerializeField, Min(0.5f)] private float maximumDistance = 18f;
        [SerializeField, Range(-20f, 85f)] private float pitch = 32f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Min(0.001f)] private float zoomSensitivity = 0.01f;
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

        public bool IsLeftMouseHeld => leftMouseAction != null && leftMouseAction.IsPressed();
        public bool IsRightMouseHeld => rightMouseAction != null && rightMouseAction.IsPressed();
        public bool MoveForwardRequested => IsLeftMouseHeld && IsRightMouseHeld;
        public bool LeftClickReleasedThisFrame { get; private set; }
        public Vector2 LeftClickPosition { get; private set; }
        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        public void AddImpulse(float strength) => impulseStrength = Mathf.Max(impulseStrength, strength);

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        private void Awake()
        {
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
                pitch = Mathf.Clamp(pitch - lookDelta.y * mouseSensitivity, minimumPitch, maximumPitch);
            }

            Vector2 scroll = zoomAction.ReadValue<Vector2>();
            if (Mathf.Abs(scroll.y) > 0.01f)
                distance = Mathf.Clamp(distance - scroll.y * zoomSensitivity, minimumDistance, maximumDistance);

            if (IsRightMouseHeld && target != null)
                target.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 desiredPivot = target.position + lookOffset;
            trackedPivot = Vector3.SmoothDamp(trackedPivot, desiredPivot, ref followVelocity, followSmoothTime);

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
