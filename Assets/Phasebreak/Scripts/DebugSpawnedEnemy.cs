#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Phasebreak.Gameplay
{
    // Ownership marker only. Gameplay state remains on MeleeEnemy/Targetable.
    [DisallowMultipleComponent]
    public sealed class DebugSpawnedEnemy : MonoBehaviour
    {
        public bool IsCombatDummy { get; private set; }
        public int DummyMaximumHealth { get; private set; }

        public void Configure(bool combatDummy, int dummyMaximumHealth = 0)
        {
            IsCombatDummy = combatDummy;
            DummyMaximumHealth = combatDummy ? Mathf.Max(1, dummyMaximumHealth) : 0;
        }
    }
}
#endif
