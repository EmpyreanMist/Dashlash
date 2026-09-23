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
        private ParticleSystem sparks;
        private LineRenderer guardRing;
        private LineRenderer pulseRing;
        private float pulseStartedAt;

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
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                trailMaterial = new Material(shader);
                trail.material = trailMaterial;
            }
            trail.emitting = false;
            GameObject burst = new("Rift Kill Sparks");
            burst.transform.SetParent(transform, false);
            burst.transform.localPosition = Vector3.up;
            sparks = burst.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = .24f;
            main.startSpeed = 2.2f;
            main.startSize = .065f;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = dashColor;
            var emission = sparks.emission;
            emission.enabled = false;
            if (trailMaterial != null) sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial = trailMaterial;
            guardRing = CreateRing("Iron Guard Ring", new Color(.88f, .61f, .3f, .75f), .045f);
            pulseRing = CreateRing("Bastion Pulse Ring", new Color(1f, .72f, .36f, .9f), .085f);
            SetRingRadius(guardRing, 1.1f);
        }

        private LineRenderer CreateRing(string name, Color color, float width)
        {
            GameObject ring = new(name);
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.up * .08f;
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.sharedMaterial = trailMaterial;
            line.enabled = false;
            return line;
        }

        private static void SetRingRadius(LineRenderer line, float radius)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void Update()
        {
            if (guardRing != null)
            {
                guardRing.enabled = combat != null && combat.ActiveGuardReduction > 0f;
            }
            if (pulseRing == null || !pulseRing.enabled) return;
            float progress = (Time.time - pulseStartedAt) / .35f;
            if (progress >= 1f) { pulseRing.enabled = false; return; }
            SetRingRadius(pulseRing, Mathf.Lerp(.8f, 4f, progress));
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.AbilityStarted += HandleAbilityStarted;
                combat.AbilityImpact += HandleAbilityImpact;
                combat.RiftChainKill += HandleChainKill;
            }
        }

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AbilityStarted -= HandleAbilityStarted;
                combat.AbilityImpact -= HandleAbilityImpact;
                combat.RiftChainKill -= HandleChainKill;
            }
            if (stopRoutine != null)
                StopCoroutine(stopRoutine);
            if (trail != null) trail.emitting = false;
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (guardRing != null) guardRing.enabled = false;
            if (pulseRing != null) pulseRing.enabled = false;
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
            int intensity = Mathf.Min(combat.RiftChainCount, 4);
            trail.time = .12f + intensity * .015f;
            trail.startWidth = .2f + intensity * .025f;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.Clear();
            trail.emitting = true;
            stopRoutine = StartCoroutine(StopAfter(Mathf.Max(0.08f, value.Duration)));
        }

        private void HandleChainKill(int count) => sparks.Emit(6 + Mathf.Min(count, 4) * 3);

        private void HandleAbilityImpact(AbilityPresentationEvent value)
        {
            if (value.Type == AbilityExecutionType.FlickerStrike)
            {
                var flickerMain = sparks.main;
                flickerMain.startColor = new Color(.42f, .64f, 1f, .8f);
                sparks.Emit(value.Finisher ? 12 : 5);
                return;
            }
            Color color = value.Name switch
            {
                "Blood Rush" or "Reaper's Arc" => new Color(.86f, .18f, .12f),
                "Iron Guard" or "Bastion Pulse" => new Color(.85f, .62f, .3f),
                "Rift Mark" or "Echo Strike" => new Color(.62f, .34f, .9f),
                _ => Color.clear
            };
            if (color == Color.clear) return;
            var main = sparks.main;
            main.startColor = color;
            sparks.Emit(value.Type == AbilityExecutionType.Area ? 24 : 12);
            if (value.Name == "Bastion Pulse" && pulseRing != null)
            {
                pulseStartedAt = Time.time;
                pulseRing.enabled = true;
            }
        }

        private IEnumerator StopAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            trail.emitting = false;
            stopRoutine = null;
        }
    }
}
