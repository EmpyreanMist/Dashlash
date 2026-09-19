using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerTargeting : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private PhasebreakFollowCamera followCamera;

        [Header("Selection")]
        [SerializeField, Min(1f)] private float maximumTargetDistance = 45f;
        [SerializeField, Range(20f, 180f)] private float tabTargetCone = 120f;
        [SerializeField] private LayerMask clickLayers = ~0;

        private readonly List<Targetable> candidateBuffer = new List<Targetable>();
        private InputAction nextTargetAction;
        private InputAction clearTargetAction;

        public Targetable CurrentTarget { get; private set; }
        public event Action<Targetable> TargetChanged;

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
            if (followCamera == null && worldCamera != null)
                followCamera = worldCamera.GetComponent<PhasebreakFollowCamera>();
            nextTargetAction = new InputAction("Next Target", InputActionType.Button, "<Keyboard>/tab");
            clearTargetAction = new InputAction("Clear Target", InputActionType.Button, "<Keyboard>/escape");
        }

        private void OnEnable()
        {
            nextTargetAction.Enable();
            clearTargetAction.Enable();
        }

        private void OnDisable()
        {
            nextTargetAction.Disable();
            clearTargetAction.Disable();
            SetTarget(null);
        }

        private void OnDestroy()
        {
            nextTargetAction.Dispose();
            clearTargetAction.Dispose();
        }

        private void Update()
        {
            if (CurrentTarget != null && (!CurrentTarget.IsAlive || !IsWithinRange(CurrentTarget)))
                SetTarget(null);

            if (GameplayInputFocus.GameplayInputBlocked || PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen)
                return;

            if (clearTargetAction.WasPressedThisFrame())
                SetTarget(null);

            if (nextTargetAction.WasPressedThisFrame())
            {
                bool reverse = Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                SelectNextTarget(reverse);
            }

            if (followCamera != null && followCamera.LeftClickReleasedThisFrame)
                SelectFromScreenPoint(followCamera.LeftClickPosition);
        }

        public void SetTarget(Targetable newTarget)
        {
            if (newTarget == CurrentTarget)
                return;
            if (newTarget != null && (!newTarget.IsHostile || !newTarget.IsAlive || !IsWithinRange(newTarget)))
                return;

            CurrentTarget?.SetSelected(false);
            CurrentTarget = newTarget;
            CurrentTarget?.SetSelected(true);
            TargetChanged?.Invoke(CurrentTarget);
        }

        public Targetable TrySelectNearestInFront(float range, float coneAngle)
        {
            float maximumSqrDistance = Mathf.Max(0.1f, range) * Mathf.Max(0.1f, range);
            float halfCone = Mathf.Clamp(coneAngle, 1f, 180f) * 0.5f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Targetable nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;
            foreach (Targetable candidate in Targetable.ActiveTargets)
            {
                if (candidate == null || !candidate.IsHostile || !candidate.IsAlive)
                    continue;

                Vector3 toCandidate = candidate.transform.position - transform.position;
                toCandidate.y = 0f;
                float sqrDistance = toCandidate.sqrMagnitude;
                if (sqrDistance < 0.01f || sqrDistance > maximumSqrDistance ||
                    Vector3.Angle(forward, toCandidate) > halfCone || sqrDistance >= nearestSqrDistance)
                    continue;

                nearest = candidate;
                nearestSqrDistance = sqrDistance;
            }

            if (nearest != null)
                SetTarget(nearest);
            return nearest;
        }

        private void SelectFromScreenPoint(Vector2 screenPoint)
        {
            if (worldCamera == null)
                return;

            Ray ray = worldCamera.ScreenPointToRay(screenPoint);
            RaycastHit[] hits = Physics.RaycastAll(ray, maximumTargetDistance, clickLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                Targetable targetable = hit.collider.GetComponentInParent<Targetable>();
                if (targetable == null || !targetable.IsHostile || !targetable.IsAlive)
                    continue;
                SetTarget(targetable);
                return;
            }

            SetTarget(null);
        }

        private void SelectNextTarget(bool reverse)
        {
            candidateBuffer.Clear();
            foreach (Targetable targetable in Targetable.ActiveTargets)
            {
                if (targetable == null || !targetable.IsHostile || !targetable.IsAlive || !IsWithinRange(targetable))
                    continue;

                Vector3 direction = targetable.transform.position - transform.position;
                direction.y = 0f;
                Vector3 viewForward = worldCamera != null ? worldCamera.transform.forward : transform.forward;
                viewForward.y = 0f;
                if (direction.sqrMagnitude < 0.01f || Vector3.Angle(viewForward, direction) > tabTargetCone * 0.5f)
                    continue;
                candidateBuffer.Add(targetable);
            }

            if (candidateBuffer.Count == 0)
            {
                SetTarget(null);
                return;
            }

            candidateBuffer.Sort((a, b) => GetTargetScore(a).CompareTo(GetTargetScore(b)));
            int currentIndex = candidateBuffer.IndexOf(CurrentTarget);
            int directionStep = reverse ? -1 : 1;
            int nextIndex = currentIndex < 0
                ? (reverse ? candidateBuffer.Count - 1 : 0)
                : (currentIndex + directionStep + candidateBuffer.Count) % candidateBuffer.Count;
            SetTarget(candidateBuffer[nextIndex]);
        }

        private float GetTargetScore(Targetable targetable)
        {
            Vector3 viewport = worldCamera != null
                ? worldCamera.WorldToViewportPoint(targetable.NameplateWorldPosition)
                : new Vector3(0.5f, 0.5f, 1f);
            float screenDistance = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f).sqrMagnitude;
            float worldDistance = Vector3.Distance(transform.position, targetable.transform.position);
            return screenDistance * 100f + worldDistance * 0.05f;
        }

        private bool IsWithinRange(Targetable targetable) =>
            targetable != null &&
            (targetable.transform.position - transform.position).sqrMagnitude <=
            maximumTargetDistance * maximumTargetDistance;
    }
}
