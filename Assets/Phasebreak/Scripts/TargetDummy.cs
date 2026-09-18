using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public sealed class TargetDummy : MonoBehaviour, ICombatTarget
    {
        [SerializeField, Min(0.01f)] private float recoilDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float returnDuration = 0.22f;
        [SerializeField] private Color hitColor = Color.white;

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Coroutine reactionRoutine;
        private bool initialized;

        public int HitCount { get; private set; }

        private void Awake() => Initialize();

        private void Initialize()
        {
            renderers = GetComponentsInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            restPosition = transform.position;
            restRotation = transform.rotation;
            restScale = transform.localScale;
            initialized = true;
        }

        public void ReceiveHit(CombatHit hit)
        {
            if (!initialized || propertyBlock == null || renderers == null)
                Initialize();

            HitCount++;
            CombatEvents.RaiseDamageNumber(hit.Point + Vector3.up * 0.35f,
                Mathf.Max(1, Mathf.RoundToInt(hit.Power)), hit.IsCritical, hit.AbilityName);
            if (reactionRoutine != null)
                StopCoroutine(reactionRoutine);
            reactionRoutine = StartCoroutine(React(hit));
        }

        private IEnumerator React(CombatHit hit)
        {
            SetColor(hitColor);
            Vector3 start = transform.position;
            Vector3 recoilTarget = restPosition + hit.Direction * hit.Knockback;
            Quaternion startRotation = transform.rotation;
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, hit.Direction).normalized;
            Quaternion recoilRotation = Quaternion.AngleAxis(12f * Mathf.Clamp(hit.Power, 0.75f, 2.5f), tiltAxis) * restRotation;
            Vector3 startScale = transform.localScale;
            float squash = Mathf.Clamp01(0.16f * hit.Power);
            Vector3 recoilScale = Vector3.Scale(restScale, new Vector3(1f + squash, 1f - squash, 1f + squash));
            float elapsed = 0f;

            while (elapsed < recoilDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / recoilDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(start, recoilTarget, eased);
                transform.rotation = Quaternion.Slerp(startRotation, recoilRotation, eased);
                transform.localScale = Vector3.Lerp(startScale, recoilScale, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            ClearColor();
            elapsed = 0f;
            start = transform.position;
            startRotation = transform.rotation;
            startScale = transform.localScale;
            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                float eased = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(start, restPosition, eased);
                transform.rotation = Quaternion.Slerp(startRotation, restRotation, eased);
                transform.localScale = Vector3.Lerp(startScale, restScale, eased);
                yield return null;
            }

            transform.position = restPosition;
            transform.rotation = restRotation;
            transform.localScale = restScale;
            reactionRoutine = null;
        }

        private void SetColor(Color color)
        {
            foreach (Renderer item in renderers)
            {
                item.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                item.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearColor()
        {
            propertyBlock.Clear();
            foreach (Renderer item in renderers)
                item.SetPropertyBlock(propertyBlock);
        }
    }
}
