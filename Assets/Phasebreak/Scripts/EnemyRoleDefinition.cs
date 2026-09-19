using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/Enemy Role", fileName = "EnemyRole")]
    public sealed class EnemyRoleDefinition : ScriptableObject
    {
        [Serializable]
        public struct RankTuning
        {
            public EnemyRank rank;
            public float healthMultiplier;
            public float damageMultiplier;
            public float experienceMultiplier;
            public float scaleMultiplier;
            public int colorVariant;
        }

        public EnemyRole role;
        public string displayName;
        public string localModelName;
        [Min(1)] public int health;
        [Min(1)] public int damage;
        [Min(0)] public int experience;
        [Min(0f)] public float moveSpeed;
        [Min(0f)] public float awarenessRange;
        [Min(1f)] public float leashRange;
        [Min(.1f)] public float attackRange;
        [Min(0f)] public float windup;
        [Min(.01f)] public float active;
        [Min(0f)] public float recovery;
        public RankTuning[] ranks;

        public RankTuning Tuning(EnemyRank rank)
        {
            if (ranks != null)
                foreach (RankTuning tuning in ranks)
                    if (tuning.rank == rank) return tuning;
            return new RankTuning { rank = rank, healthMultiplier = 1f, damageMultiplier = 1f,
                experienceMultiplier = 1f, scaleMultiplier = 1f, colorVariant = 1 };
        }
    }
}
