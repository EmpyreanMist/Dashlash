using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    public sealed class PhasebreakFollowCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Renderer[] firstPersonHiddenRenderers;

        [Header("Isometric View")]
        [SerializeField, Min(1f)] private float distance = 13f;
        [SerializeField, Range(20f, 80f)] private float pitch = 52f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;
        [SerializeField, Min(0.01f)] private float verticalFollowSmoothTime = 0.28f;
        [SerializeField, Min(0f)] private float verticalDeadZone = 1.25f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.85f, 0f);
        [SerializeField, Range(30f, 100f)] private float isometricFieldOfView = 48f;

        [Header("First Person View")]
        [SerializeField] private bool startInFirstPerson;
        [SerializeField] private Vector3 firstPersonEyeOffset = new Vector3(0f, 0.72f, 0.08f);
        [SerializeField, Range(40f, 110f)] private float firstPersonFieldOfView = 75f;
        [SerializeField, Min(0.001f)] private float mouseSensitivity = 0.08f;
        [SerializeField, Range(-89f, 0f)] private float minimumLookPitch = -75f;
        [SerializeField, Range(0f, 89f)] private float maximumLookPitch = 75f;

        private Camera controlledCamera;
        private InputAction toggleViewAction;
        private InputAction lookAction;
        private Vector3 followVelocity;
        private Vector3 impulseOffset;
        private float impulseStrength;
        private float trackedTargetHeight;
        private float verticalVelocity;
        private float firstPersonYaw;
        private float firstPersonPitch;
        private bool firstPerson;

        public bool IsFirstPerson => firstPerson;

        public void AddImpulse(float strength) => impulseStrength = Mathf.Max(impulseStrength, strength);

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        public void SetFirstPersonHiddenRenderers(params Renderer[] renderers)
        {
            firstPersonHiddenRenderers = renderers;
            ApplyVisibility();
        }

        public void ToggleView() => SetFirstPerson(!firstPerson);

        public void SetFirstPerson(bool enabled)
        {
            firstPerson = enabled;
            if (target != null && enabled)
            {
                firstPersonYaw = target.eulerAngles.y;
                firstPersonPitch = 0f;
            }
            ApplyVisibility();
            ApplyCursorState();
            SnapToTarget();
        }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            toggleViewAction = new InputAction("Toggle View", InputActionType.Button, "<Keyboard>/v");
            lookAction = new InputAction("First Person Look", InputActionType.Value, "<Mouse>/delta");
        }

        private void OnEnable()
        {
            toggleViewAction.Enable();
            lookAction.Enable();
        }

        private void Start()
        {
            firstPerson = startInFirstPerson;
            if (target != null)
                firstPersonYaw = target.eulerAngles.y;
            ApplyVisibility();
            ApplyCursorState();
            SnapToTarget();
        }

        private void Update()
        {
            if (toggleViewAction.WasPressedThisFrame())
                ToggleView();

            if (!firstPerson)
                return;

            Vector2 lookDelta = lookAction.ReadValue<Vector2>();
            firstPersonYaw += lookDelta.x * mouseSensitivity;
            firstPersonPitch = Mathf.Clamp(firstPersonPitch - lookDelta.y * mouseSensitivity,
                minimumLookPitch, maximumLookPitch);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            UpdateImpulse();
            if (firstPerson)
                UpdateFirstPerson();
            else
                UpdateIsometric();

            if (controlledCamera != null)
            {
                float desiredFov = firstPerson ? firstPersonFieldOfView : isometricFieldOfView;
                controlledCamera.fieldOfView = Mathf.Lerp(controlledCamera.fieldOfView, desiredFov,
                    12f * Time.unscaledDeltaTime);
            }
        }

        private void OnDisable()
        {
            toggleViewAction.Disable();
            lookAction.Disable();
            SetRenderersVisible(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            toggleViewAction.Dispose();
            lookAction.Dispose();
        }

        private void UpdateIsometric()
        {
            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            float heightDifference = target.position.y - trackedTargetHeight;
            float desiredHeight = trackedTargetHeight;
            if (heightDifference > verticalDeadZone)
                desiredHeight = target.position.y - verticalDeadZone;
            else if (heightDifference < -verticalDeadZone)
                desiredHeight = target.position.y + verticalDeadZone;

            trackedTargetHeight = Mathf.SmoothDamp(trackedTargetHeight, desiredHeight, ref verticalVelocity,
                verticalFollowSmoothTime);
            Vector3 trackedTarget = new Vector3(target.position.x, trackedTargetHeight, target.position.z);
            Vector3 desiredPosition = trackedTarget + lookOffset - orbitRotation * Vector3.forward * distance;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity,
                followSmoothTime) + impulseOffset;
            transform.rotation = orbitRotation;
        }

        private void UpdateFirstPerson()
        {
            Quaternion viewRotation = Quaternion.Euler(firstPersonPitch, firstPersonYaw, 0f);
            transform.position = target.position + target.TransformVector(firstPersonEyeOffset) + impulseOffset * 0.35f;
            transform.rotation = viewRotation;
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

            trackedTargetHeight = target.position.y;
            followVelocity = Vector3.zero;
            verticalVelocity = 0f;
            impulseOffset = Vector3.zero;
            if (firstPerson)
            {
                transform.position = target.position + target.TransformVector(firstPersonEyeOffset);
                transform.rotation = Quaternion.Euler(firstPersonPitch, firstPersonYaw, 0f);
            }
            else
            {
                Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
                transform.position = target.position + lookOffset - orbitRotation * Vector3.forward * distance;
                transform.rotation = orbitRotation;
            }
        }

        private void ApplyVisibility() => SetRenderersVisible(!firstPerson);

        private void SetRenderersVisible(bool visible)
        {
            if (firstPersonHiddenRenderers == null)
                return;
            foreach (Renderer item in firstPersonHiddenRenderers)
            {
                if (item != null)
                    item.enabled = visible;
            }
        }

        private void ApplyCursorState()
        {
            Cursor.lockState = firstPerson ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !firstPerson;
        }
    }
}
