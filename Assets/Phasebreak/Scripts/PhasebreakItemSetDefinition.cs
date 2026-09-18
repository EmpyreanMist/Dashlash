using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/Item Set", fileName = "ItemSet")]
    public sealed class PhasebreakItemSetDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string fantasy;
        public Sprite icon;
        public SetBonusDefinition[] bonuses;
    }
}
