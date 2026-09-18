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
        private PhasebreakFollowCamera followCamera;
        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private float invulnerableUntil;
        private Coroutine flashRoutine;

        public int CurrentHealth { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsInvulnerable => Time.time < invulnerableUntil;
        public int HitCount { get; private set; }
        public event Action Died;

        private void Awake()
        {
            movement = GetComponent<PhasebreakPlayerMovement>();
            followCamera = FindAnyObjectByType<PhasebreakFollowCamera>();
            renderers = GetComponentsInChildren<Renderer>(true);
            propertyBlock = new MaterialPropertyBlock();
            CurrentHealth = maxHealth;
        }

        public bool TakeHit(int damage, Vector3 direction, float knockback)
        {
            if (!IsAlive || IsInvulnerable)
                return false;

            HitCount++;
            CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(1, damage));
            invulnerableUntil = Time.time + invulnerabilityDuration;
            movement.AddCombatImpulse(direction.normalized * knockback);
            followCamera?.AddImpulse(cameraImpulse);

            if (flashRoutine != null)
                StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());

            if (CurrentHealth == 0)
                Died?.Invoke();
            return true;
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            invulnerableUntil = Time.time + 0.2f;
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }
            ClearColor();
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
