using System;
using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [RequireComponent(typeof(PhasebreakPlayerMovement))]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealth = 5;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.55f;

        [Header("Hit Feedback")]
        [SerializeField] private Color hitColor = Color.white;
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.09f;
        [SerializeField, Min(0f)] private float cameraImpulse = 0.24f;

        private PhasebreakPlayerMovement movement;
        private PlayerBuildSystem build;
        private PhasebreakFollowCamera followCamera;
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private float invulnerableUntil;
        private Coroutine flashRoutine;
        private int progressionBonusHealth;
        private int equipmentBonusHealth;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth + progressionBonusHealth + equipmentBonusHealth;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public int HitCount { get; private set; }
        public event Action Died;
        public event Action<Vector3, int> HitReceived;
        public event Action ResetPerformed;

        public bool TakeHit(int damage, Vector3 direction) => TakeHit(damage, direction, 0f);

        private void Awake()
        {
            movement = GetComponent<PhasebreakPlayerMovement>();
            build = GetComponent<PlayerBuildSystem>();
            followCamera = FindAnyObjectByType<PhasebreakFollowCamera>();
            renderers = GetComponentsInChildren<Renderer>(true);
            propertyBlock = new MaterialPropertyBlock();
            CurrentHealth = MaxHealth;
        }

        public bool TakeHit(int damage, Vector3 direction, float knockback)
        {
            if (!IsAlive || IsInvulnerable)
                return false;

            HitCount++;
            int mitigated = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - (build != null ? Mathf.Clamp(build.Defense, 0f, .75f) : 0f))));
            CurrentHealth = Mathf.Max(0, CurrentHealth - mitigated);
            invulnerableUntil = Time.time + invulnerabilityDuration;
            if (knockback > 0f)
                movement.AddCombatImpulse(direction.normalized * knockback);
            followCamera?.AddImpulse(cameraImpulse);
            HitReceived?.Invoke(direction.normalized, mitigated);

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());

            if (CurrentHealth == 0)
                Died?.Invoke();
            return true;
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
            invulnerableUntil = Time.time + 0.2f;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            ClearColor();
            ResetPerformed?.Invoke();
        }

        public void ApplyProgressionBonus(int bonusHealth)
        {
            int previousMaximum = MaxHealth;
            progressionBonusHealth = Mathf.Max(0, bonusHealth);
            int gainedHealth = Mathf.Max(0, MaxHealth - previousMaximum);
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + gainedHealth);
        }

        public void ApplyEquipmentBonus(int bonusHealth)
        {
            int previousMaximum = MaxHealth;
            equipmentBonusHealth = Mathf.Max(0, bonusHealth);
            int gainedHealth = Mathf.Max(0, MaxHealth - previousMaximum);
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + gainedHealth);
        }

        private IEnumerator Flash()
        {
            SetColor(hitColor);
            yield return new WaitForSecondsRealtime(hitFlashDuration);
            ClearColor();
            flashRoutine = null;
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
            if (propertyBlock == null || renderers == null)
                return;
            propertyBlock.Clear();
            foreach (Renderer item in renderers)
                item.SetPropertyBlock(propertyBlock);
        }
    }
}
