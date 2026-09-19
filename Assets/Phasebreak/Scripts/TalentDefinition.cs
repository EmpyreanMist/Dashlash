using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum TalentNodeType { Passive, AbilityUnlock, AbilityModifier, Choice, Keystone }

    public enum TalentEffect
    {
        None, Power, MaximumHealth, Defense, CriticalChance, CriticalDamage, AttackSpeed,
        MovementSpeed, AbilityDamage, AbilityCooldown, AbilityCost, AbilityExtraCharge,
        CriticalEnergy, CrushingCleave, CriticalMomentum, OffensiveRhythm,
        BraceAfterHit, ResourceUnderPressure, HeavyImpact, LastBastion,
        MobilityMomentum, TeleportKillRecovery, MobilityKillCircuit
    }

    public static class TalentText
    {
        public static string Describe(TalentEffect effect, float value, string ability) => effect switch
        {
            TalentEffect.Power => $"Increase Power by {value:P0}.",
            TalentEffect.MaximumHealth => $"Increase maximum Health by {Mathf.RoundToInt(value)}.",
            TalentEffect.Defense => $"Increase Defense by {value:P0}.",
            TalentEffect.CriticalChance => $"Increase critical chance by {value:P0}.",
            TalentEffect.CriticalDamage => $"Increase critical damage by {value:P0}.",
            TalentEffect.AttackSpeed => $"Increase attack speed by {value:P0}.",
            TalentEffect.MovementSpeed => $"Increase movement speed by {value:P0}.",
            TalentEffect.AbilityDamage => $"Increase {PrettyAbility(ability)} damage by {value:P0}.",
            TalentEffect.AbilityCooldown => $"Reduce {PrettyAbility(ability)} cooldown by {value:P0}.",
            TalentEffect.AbilityCost => $"Reduce {PrettyAbility(ability)} Energy cost by {value:P0}.",
            TalentEffect.AbilityExtraCharge => $"Grant {PrettyAbility(ability)} {Mathf.RoundToInt(value)} additional charge.",
            TalentEffect.CriticalEnergy => $"Critical hits restore {value:0} Energy.",
            TalentEffect.CrushingCleave => "Crushing Blow cleaves nearby enemies for 60% damage.",
            TalentEffect.CriticalMomentum => $"Critical hits grant {value:P0} attack speed for 4 seconds.",
            TalentEffect.OffensiveRhythm => $"Repeated offensive abilities build Rhythm, up to {value:P0} bonus damage.",
            TalentEffect.BraceAfterHit => $"Taking damage grants {value:P0} Defense for 4 seconds.",
            TalentEffect.ResourceUnderPressure => $"Below half Health, Energy regeneration is increased by {value:P0}.",
            TalentEffect.HeavyImpact => $"After taking damage, your next Crushing Blow deals {value:P0} more damage.",
            TalentEffect.LastBastion => $"At low Health, gain {value:P0} Defense and offensive stability.",
            TalentEffect.MobilityMomentum => $"Movement abilities grant {value:P0} Power for 4 seconds.",
            TalentEffect.TeleportKillRecovery => $"Mobility kills restore {value:0} Energy and refresh movement pressure.",
            TalentEffect.MobilityKillCircuit => "Mobility kills immediately restore a movement charge.",
            _ => string.Empty
        };

        private static string PrettyAbility(string id) => string.IsNullOrWhiteSpace(id)
            ? "the linked ability"
            : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('-', ' '));
    }
}
