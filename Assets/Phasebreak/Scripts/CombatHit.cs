using UnityEngine;

namespace Phasebreak.Gameplay
{
    public readonly struct CombatHit
    {
        public readonly Vector3 Point;
        public readonly Vector3 Direction;
        public readonly float Power;
        public readonly float Knockback;
        public readonly bool DashEnhanced;
        public readonly bool IsCritical;
        public readonly string AbilityName;

        public CombatHit(Vector3 point, Vector3 direction, float power, float knockback, bool dashEnhanced,
            bool isCritical = false, string abilityName = null)
        {
            Point = point;
            Direction = direction;
            Power = power;
            Knockback = knockback;
            DashEnhanced = dashEnhanced;
            IsCritical = isCritical;
            AbilityName = abilityName;
        }
    }

    public interface ICombatTarget
    {
        void ReceiveHit(CombatHit hit);
    }
}
