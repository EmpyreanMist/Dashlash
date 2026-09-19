using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/Talents/Catalog", fileName = "TalentCatalog")]
    public sealed class TalentCatalog : ScriptableObject
    {
        public int saveVersion = 1;
        [Min(0)] public int prototypeBasePoints = 6;
        [Min(0)] public int pointsPerLevelAfterFirst = 1;
        public TalentTreeDefinition[] trees = Array.Empty<TalentTreeDefinition>();

        public int PointsForLevel(int level) => prototypeBasePoints + Mathf.Max(0, level - 1) * pointsPerLevelAfterFirst;
    }
}
