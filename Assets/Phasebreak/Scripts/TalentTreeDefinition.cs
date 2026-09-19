using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/Talents/Tree", fileName = "TalentTree")]
    public sealed class TalentTreeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string identity;
        public Specialization specialization;
        public Sprite icon;
        public TalentNodeDefinition[] nodes = Array.Empty<TalentNodeDefinition>();
    }
}
