using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/Talents/Node", fileName = "TalentNode")]
    public sealed class TalentNodeDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;
        public Specialization specialization;
        public TalentNodeType nodeType;
        [Range(1, 5)] public int maximumRank = 1;
        [Min(1)] public int pointCost = 1;
        [Min(0)] public int requiredPlayerLevel;
        public string[] prerequisiteIds = Array.Empty<string>();
        public string choiceGroup;
        public TalentEffect effect;
        public TalentEffect secondaryEffect;
        public string targetAbilityId;
        public string grantedAbilityId;
        public ItemTag synergyTag;
        public float effectValuePerRank;
        public float secondaryValuePerRank;
        [Tooltip("Normalized position in the tree presentation area.")]
        public Vector2 presentationPosition = new(.5f, .5f);

        public string RankEffect(int rank)
        {
            rank = Mathf.Clamp(rank, 0, maximumRank);
            if (!string.IsNullOrWhiteSpace(grantedAbilityId))
                return "Unlock " + System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                    grantedAbilityId.Replace('-', ' ')) + " in the Spellbook.";
            float primary = effectValuePerRank * rank;
            float secondary = secondaryValuePerRank * rank;
            return TalentText.Describe(effect, primary, targetAbilityId) +
                   (secondaryEffect == TalentEffect.None ? string.Empty : $"\n{TalentText.Describe(secondaryEffect, secondary, targetAbilityId)}");
        }
    }
}
