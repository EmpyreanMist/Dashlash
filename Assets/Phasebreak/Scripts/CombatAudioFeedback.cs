using UnityEngine;

namespace Phasebreak.Gameplay
{
    [RequireComponent(typeof(AudioSource)), DisallowMultipleComponent]
    public sealed class CombatAudioFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        [SerializeField, Range(0f, 1f)] private float volume = 0.28f;

        private AudioSource source;
        private AudioClip swing;
        private AudioClip impact;
        private AudioClip mobility;
        private AudioClip denied;

        private void Awake()
        {
            combat ??= GetComponentInParent<PlayerCombat>();
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0.15f;
            swing = CreateNoiseClip("Combat Swing", 0.14f, 0.62f, 120f);
            impact = CreateNoiseClip("Combat Impact", 0.11f, 0.38f, 72f);
            mobility = CreateNoiseClip("Rift Mobility", 0.2f, 0.52f, 190f);
            denied = CreateToneClip("Ability Denied", 0.08f, 180f);
        }

        private void OnEnable()
        {
            if (combat == null)
                return;
            combat.AbilityStarted += HandleStarted;
            combat.AbilityImpact += HandleImpact;
            combat.AbilityFailed += HandleFailed;
            combat.FlickerDeparted += HandleDeparture;
            combat.FlickerArrived += HandleArrival;
        }

        private void OnDisable()
        {
            if (combat == null)
                return;
            combat.AbilityStarted -= HandleStarted;
            combat.AbilityImpact -= HandleImpact;
            combat.AbilityFailed -= HandleFailed;
            combat.FlickerDeparted -= HandleDeparture;
            combat.FlickerArrived -= HandleArrival;
        }

        private void OnDestroy()
        {
            if (swing != null) Destroy(swing);
            if (impact != null) Destroy(impact);
            if (mobility != null) Destroy(mobility);
            if (denied != null) Destroy(denied);
        }

        private void HandleDeparture(AbilityPresentationEvent value)
        {
            source.pitch = 1.65f;
            source.PlayOneShot(mobility, volume * .5f);
        }
        private void HandleArrival(AbilityPresentationEvent value)
        {
            source.pitch = value.Finisher ? .9f : 1.4f;
            source.PlayOneShot(swing, volume * .7f);
        }
        private void HandleStarted(AbilityPresentationEvent value)
        {
            if (value.Type == AbilityExecutionType.FlickerStrike) return;
            source.pitch = value.Type switch
            {
                AbilityExecutionType.PhaseDash => 1.35f,
                AbilityExecutionType.Charge => 0.8f,
                AbilityExecutionType.PhaseLunge => 1.15f,
                _ => value.Index == 1 ? 0.72f : 1f
            };
            source.PlayOneShot(value.Type is AbilityExecutionType.PhaseDash or AbilityExecutionType.Charge
                ? mobility : swing, volume);
        }

        private void HandleImpact(AbilityPresentationEvent value)
        {
            if (value.Type == AbilityExecutionType.FlickerStrike)
            {
                source.pitch = value.Finisher ? .7f : 1.2f;
                source.PlayOneShot(impact, volume * (value.Finisher ? 1.3f : .65f));
                return;
            }
            source.pitch = value.Critical ? 0.72f : value.Index == 1 ? 0.82f : 1f;
            if (value.Type is AbilityExecutionType.PhaseLunge or AbilityExecutionType.Charge && combat.RiftChainCount > 0)
                source.pitch = 1f + Mathf.Min(combat.RiftChainCount, 5) * .07f;
            source.PlayOneShot(impact, value.Critical ? volume * 1.5f : volume);
        }

        private void HandleFailed(string message)
        {
            source.pitch = 1f;
            source.PlayOneShot(denied, volume * 0.45f);
        }

        private static AudioClip CreateNoiseClip(string name, float duration, float decay, float frequency)
        {
            const int rate = 44100;
            int samples = Mathf.CeilToInt(duration * rate);
            float[] data = new float[samples];
            var random = new System.Random(name.GetHashCode());
            float previous = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                previous = Mathf.Lerp(previous, noise, Mathf.Clamp01(frequency / rate));
                data[i] = previous * Mathf.Pow(1f - t, 1f + decay * 4f);
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateToneClip(string name, float duration, float frequency)
        {
            const int rate = 44100;
            int samples = Mathf.CeilToInt(duration * rate);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * (1f - i / (float)samples) * 0.35f;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
