using UnityEngine;

namespace Phasebreak.Gameplay
{
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
    }
}
