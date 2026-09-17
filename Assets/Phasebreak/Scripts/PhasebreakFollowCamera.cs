using UnityEngine;

namespace Phasebreak.Gameplay
{
    public sealed class PhasebreakFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(1f)] private float distance = 13f;
        [SerializeField, Range(20f, 80f)] private float pitch = 52f;
        [SerializeField] private float yaw = 45f;
        [SerializeField, Min(0.01f)] private float followSmoothTime = 0.12f;
        [SerializeField, Min(0.01f)] private float verticalFollowSmoothTime = 0.28f;
        [SerializeField, Min(0f)] private float verticalDeadZone = 1.25f;
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 0.85f, 0f);

        private Vector3 followVelocity;
        private Vector3 impulseOffset;
        private float impulseStrength;
        private float trackedTargetHeight;
        private float verticalVelocity;

        public void AddImpulse(float strength)
        {
            impulseStrength = Mathf.Max(impulseStrength, strength);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        private void Start() => SnapToTarget();

        private void LateUpdate()
        {
            if (target == null)
                return;

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            float heightDifference = target.position.y - trackedTargetHeight;
            float desiredHeight = trackedTargetHeight;
            if (heightDifference > verticalDeadZone)
                desiredHeight = target.position.y - verticalDeadZone;
            else if (heightDifference < -verticalDeadZone)
                desiredHeight = target.position.y + verticalDeadZone;

            trackedTargetHeight = Mathf.SmoothDamp(trackedTargetHeight, desiredHeight, ref verticalVelocity, verticalFollowSmoothTime);
            Vector3 trackedTarget = new Vector3(target.position.x, trackedTargetHeight, target.position.z);
            Vector3 desiredPosition = trackedTarget + lookOffset - orbitRotation * Vector3.forward * distance;
            impulseOffset = Vector3.Lerp(impulseOffset, Random.insideUnitSphere * impulseStrength, 18f * Time.unscaledDeltaTime);
            impulseStrength = Mathf.MoveTowards(impulseStrength, 0f, 8f * Time.unscaledDeltaTime);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime) + impulseOffset;
            transform.rotation = orbitRotation;
        }

        private void SnapToTarget()
        {
            if (target == null)
                return;

            Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
            trackedTargetHeight = target.position.y;
            transform.position = target.position + lookOffset - orbitRotation * Vector3.forward * distance;
            transform.rotation = orbitRotation;
            followVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }
    }
}
