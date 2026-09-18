using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerHealth))]
    [DisallowMultipleComponent]
    public sealed class PlayerProgression : MonoBehaviour
    {
        [Header("Leveling")]
        [SerializeField, Min(1)] private int maximumLevel = 3;
        [SerializeField, Min(1)] private int baseExperienceToLevel = 24;
        [SerializeField, Min(0)] private int experienceGrowthPerLevel = 12;

        [Header("Growth Per Level")]
        [SerializeField, Min(0f)] private float powerPerLevel = 0.15f;
        [SerializeField, Min(0)] private int healthPerLevel = 2;

        [Header("Level 2 Reward")]
        [SerializeField] private string passiveName = "Keen Edge";
        [SerializeField, Range(0f, 1f)] private float passiveCriticalChance = 0.08f;
        [SerializeField, Min(0f)] private float passiveCriticalDamage = 0.25f;

        private PlayerHealth health;
        private Targetable targetable;

        public int Level { get; private set; } = 1;
        public int CurrentExperience { get; private set; }
        public int MaximumLevel => maximumLevel;
        public bool IsMaximumLevel => Level >= maximumLevel;
        public int ExperienceToNextLevel => IsMaximumLevel
            ? 0
            : baseExperienceToLevel + (Level - 1) * experienceGrowthPerLevel;
        public float ExperienceFraction => IsMaximumLevel
            ? 1f
            : Mathf.Clamp01((float)CurrentExperience / ExperienceToNextLevel);
        public float PowerMultiplier => 1f + (Level - 1) * powerPerLevel;
        public int BonusHealth => (Level - 1) * healthPerLevel;
        public bool HasKeenEdge => Level >= 2;
        public float CriticalChanceBonus => HasKeenEdge ? passiveCriticalChance : 0f;
        public float CriticalDamageBonus => HasKeenEdge ? passiveCriticalDamage : 0f;
        public string PassiveName => passiveName;
        public Sprite PassiveIcon => PhasebreakIconCatalog.Current?.GetPassiveIcon(passiveName);

        public event Action ProgressChanged;
        public event Action<int, string> LevelGained;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            targetable = GetComponent<Targetable>();
            ApplyLevelBenefits();
        }

        private void OnEnable() => CombatEvents.EnemyDefeated += GrantExperience;

        private void OnDisable() => CombatEvents.EnemyDefeated -= GrantExperience;

        public void GrantExperience(int amount)
        {
            if (amount <= 0 || IsMaximumLevel)
                return;

            CurrentExperience += amount;
            while (!IsMaximumLevel && CurrentExperience >= ExperienceToNextLevel)
            {
                CurrentExperience -= ExperienceToNextLevel;
                Level++;
                ApplyLevelBenefits();
                string reward = Level == 2 ? passiveName : $"+{Mathf.RoundToInt(powerPerLevel * 100f)}% Power";
                LevelGained?.Invoke(Level, reward);
            }

            if (IsMaximumLevel)
                CurrentExperience = 0;
            ProgressChanged?.Invoke();
        }

        private void ApplyLevelBenefits()
        {
            health?.ApplyProgressionBonus(BonusHealth);
            targetable?.SetLevel(Level);
        }
    }
}
