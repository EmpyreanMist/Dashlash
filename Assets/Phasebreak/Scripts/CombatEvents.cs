using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public readonly struct DamageNumberEvent
    {
        public readonly Vector3 WorldPosition;
        public readonly int Amount;
        public readonly bool IsCritical;
        public readonly string AbilityName;

        public DamageNumberEvent(Vector3 worldPosition, int amount, bool isCritical, string abilityName)
        {
            WorldPosition = worldPosition;
            Amount = amount;
            IsCritical = isCritical;
            AbilityName = abilityName;
        }
    }

    public readonly struct EnemyDefeatedEvent
    {
        public readonly int ExperienceReward;
        public readonly string EnemyName;
        public readonly Vector3 WorldPosition;

        public EnemyDefeatedEvent(int experienceReward, string enemyName, Vector3 worldPosition)
        {
            ExperienceReward = Mathf.Max(0, experienceReward);
            EnemyName = string.IsNullOrWhiteSpace(enemyName) ? "Enemy" : enemyName;
            WorldPosition = worldPosition;
        }
    }

    public static class CombatEvents
    {
        public static event Action<DamageNumberEvent> DamageNumberRequested;
        public static event Action<int> EnemyDefeated;
        public static event Action<EnemyDefeatedEvent> EnemyDefeatedDetailed;

        public static void RaiseDamageNumber(Vector3 worldPosition, int amount, bool isCritical,
            string abilityName)
        {
            DamageNumberRequested?.Invoke(new DamageNumberEvent(worldPosition, Mathf.Max(1, amount),
                isCritical, abilityName));
        }

        public static void RaiseEnemyDefeated(int experienceReward, string enemyName = "Enemy",
            Vector3 worldPosition = default)
        {
            int reward = Mathf.Max(0, experienceReward);
            EnemyDefeated?.Invoke(reward);
            EnemyDefeatedDetailed?.Invoke(new EnemyDefeatedEvent(reward, enemyName, worldPosition));
        }
    }
}
