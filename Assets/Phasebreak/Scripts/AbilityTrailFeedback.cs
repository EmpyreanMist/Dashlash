using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AbilityTrailFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private Color dashColor = new(0.18f, 0.72f, 1f, 0.82f);
        [SerializeField] private Color chargeColor = new(1f, 0.32f, 0.08f, 0.88f);

        private TrailRenderer trail;
        private Material trailMaterial;
        private Coroutine stopRoutine;

        private void Awake()
        {
            combat ??= GetComponentInParent<PlayerCombat>();
            GameObject trailObject = new("Ability Motion Trail");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = Vector3.up * 0.9f;
            trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.minVertexDistance = 0.04f;
            trail.startWidth = 0.62f;
            trail.endWidth = 0f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                trailMaterial = new Material(shader);
                trail.material = trailMaterial;
            }
            trail.emitting = false;
        }

        private void OnEnable()
        {
            if (combat != null)
                combat.AbilityStarted += HandleAbilityStarted;
        }

        private void OnDisable()
        {
            if (combat != null)
                combat.AbilityStarted -= HandleAbilityStarted;
            if (stopRoutine != null)
                StopCoroutine(stopRoutine);
        }

        private void OnDestroy()
        {
            if (trailMaterial != null)
                Destroy(trailMaterial);
        }

        private void HandleAbilityStarted(AbilityPresentationEvent value)
        {
            if (value.Type is not (AbilityExecutionType.PhaseDash or AbilityExecutionType.PhaseLunge or AbilityExecutionType.Charge))
                return;
            if (stopRoutine != null)
                StopCoroutine(stopRoutine);
            Color color = value.Type == AbilityExecutionType.Charge ? chargeColor : dashColor;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.Clear();
            trail.emitting = true;
            stopRoutine = StartCoroutine(StopAfter(Mathf.Max(0.08f, value.Duration)));
        }

        private IEnumerator StopAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            trail.emitting = false;
            stopRoutine = null;
        }
    }
}
