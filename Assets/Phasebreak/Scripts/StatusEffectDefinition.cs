using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum StatusEffectKind { Buff, Debuff, Neutral }

    [CreateAssetMenu(menuName = "Phasebreak/Status Effect", fileName = "StatusEffect")]
    public sealed class StatusEffectDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public StatusEffectKind kind;
        public Sprite icon;
        [Min(0f)] public float defaultDuration;
        [Min(1)] public int maximumStacks = 1;

        public Sprite ResolveIcon()
        {
            if (icon != null)
                return icon;
            PhasebreakIconCatalog catalog = PhasebreakIconCatalog.Current;
            if (catalog == null)
                return null;
            return kind == StatusEffectKind.Debuff ? catalog.fallbackDebuff : catalog.fallbackBuff;
        }
    }
}
