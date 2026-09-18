using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum AbilityExecutionType
    {
        Melee,
        PhaseLunge,
        PhaseDash,
        Charge
    }

    [CreateAssetMenu(menuName = "Phasebreak/Combat Ability", fileName = "Ability")]
    public sealed class CombatAbilityDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public string key = "1";
        [Min(0f)] public float damage = 5f;
        [Min(0.1f)] public float range = 2.5f;
        [Min(0f)] public float cooldown;
        [Min(0f)] public float resourceCost;
        [Range(0f, 1f)] public float criticalBonus;
        [Min(0f)] public float impulse = 1.7f;
        [Min(0f)] public float windup = 0.1f;
        [Min(0f)] public float recovery = 0.1f;
        public AbilityExecutionType executionType = AbilityExecutionType.Melee;
        public bool requiresTarget = true;
        public bool usesGlobalCooldown = true;
        [Min(0f)] public float minimumRange;
        [Range(1, 3)] public int maximumCharges = 1;
        [Min(0f)] public float movementDistance;
        [Min(0.01f)] public float movementDuration = 0.16f;
        [Min(0.1f)] public float stopDistance = 1.35f;
        public Sprite icon;
    }
}
