using System;
using System.Collections.Generic;
using System.Linq;

namespace Phasebreak.Gameplay
{
    public enum GearComparisonKind
    {
        NotComparable,
        Worse,
        Equivalent,
        Upgrade
    }

    public readonly struct GearComparisonResult
    {
        public GearComparisonResult(
            GearComparisonKind kind,
            EquipmentSlot comparedSlot,
            PhasebreakItemDefinition equippedItem,
            float currentScore,
            float candidateScore)
        {
            Kind = kind;
            ComparedSlot = comparedSlot;
            EquippedItem = equippedItem;
            CurrentScore = currentScore;
            CandidateScore = candidateScore;
        }

        public GearComparisonKind Kind { get; }
        public EquipmentSlot ComparedSlot { get; }
        public PhasebreakItemDefinition EquippedItem { get; }
        public float CurrentScore { get; }
        public float CandidateScore { get; }
        public float Delta => CandidateScore - CurrentScore;
        public bool IsUpgrade => Kind == GearComparisonKind.Upgrade;
        public bool IsWorse => Kind == GearComparisonKind.Worse;
        public bool HasEquippedComparison => EquippedItem != null;
    }

    /// <summary>
    /// Deterministic first-pass gear heuristic. It compares complete equipped loadouts before and
    /// after a candidate swap, so activating or breaking item-set thresholds is included. The
    /// weights intentionally live outside the UI and can later move to a balance definition.
    /// </summary>
    public static class GearUpgradeEvaluator
    {
        private const float EqualityTolerance = .05f;
        private static readonly EquipmentSlot[] Slots = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

        public static GearComparisonResult Evaluate(PhasebreakItemDefinition candidate, PlayerBuildSystem build)
        {
            if (candidate == null || build == null)
                return default;

            EquipmentSlot targetSlot = ResolveTargetSlot(candidate, build);
            if (!candidate.CanEquipIn(targetSlot))
                return new GearComparisonResult(GearComparisonKind.NotComparable, targetSlot, null, 0f, 0f);

            Dictionary<EquipmentSlot, PhasebreakItemDefinition> before = Slots
                .Select(slot => new KeyValuePair<EquipmentSlot, PhasebreakItemDefinition>(slot, build.GetEquipped(slot)))
                .Where(pair => pair.Value != null)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            before.TryGetValue(targetSlot, out PhasebreakItemDefinition equipped);

            float currentScore = ScoreLoadout(before.Values);
            Dictionary<EquipmentSlot, PhasebreakItemDefinition> after = new(before) { [targetSlot] = candidate };
            float candidateScore = ScoreLoadout(after.Values);
            float delta = candidateScore - currentScore;

            GearComparisonKind kind;
            if (equipped == null)
                kind = GearComparisonKind.Upgrade;
            else if (delta > EqualityTolerance)
                kind = GearComparisonKind.Upgrade;
            else if (delta < -EqualityTolerance)
                kind = GearComparisonKind.Worse;
            else
                kind = GearComparisonKind.Equivalent;

            return new GearComparisonResult(kind, targetSlot, equipped, currentScore, candidateScore);
        }

        public static EquipmentSlot ResolveTargetSlot(PhasebreakItemDefinition candidate, PlayerBuildSystem build)
        {
            if (candidate == null || candidate.slot != EquipmentSlot.Sigil1 || build == null)
                return candidate != null ? candidate.slot : EquipmentSlot.Sigil1;
            if (build.GetEquipped(EquipmentSlot.Sigil1) == null)
                return EquipmentSlot.Sigil1;
            if (build.GetEquipped(EquipmentSlot.Sigil2) == null)
                return EquipmentSlot.Sigil2;
            return EquipmentSlot.Sigil3;
        }

        private static float ScoreLoadout(IEnumerable<PhasebreakItemDefinition> items)
        {
            PhasebreakItemDefinition[] equipped = items.Where(item => item != null).ToArray();
            BuildStats stats = default;
            float score = 0f;

            foreach (PhasebreakItemDefinition item in equipped)
            {
                stats += item.stats;
                score += item.itemLevel * .35f;
                score += ScoreEffect(item.effects, item.effectValue);
            }

            foreach (IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition> group in equipped
                         .Where(item => item.itemSet != null)
                         .GroupBy(item => item.itemSet))
            {
                int count = group.Count();
                foreach (SetBonusDefinition bonus in group.Key.bonuses ?? Array.Empty<SetBonusDefinition>())
                {
                    if (count < bonus.pieces)
                        continue;
                    stats += bonus.stats;
                    score += ScoreEffect(bonus.effects, bonus.effectValue);
                }
            }

            score += stats.power * 100f;
            score += stats.maxHealth * 1.25f;
            score += stats.defense * 90f;
            score += stats.criticalChance * 110f;
            score += stats.criticalDamage * 65f;
            score += stats.attackSpeed * 75f;
            score += stats.movementSpeed * 60f;
            score += stats.bossDamage * 55f;
            return score;
        }

        private static float ScoreEffect(BuildEffect effects, float value)
        {
            float score = 0f;
            if ((effects & BuildEffect.CritRestoresEnergy) != 0) score += 14f + Math.Min(value, 30f) * .45f;
            if ((effects & BuildEffect.PhaseLungeCooldown) != 0) score += 22f + Math.Min(value, 1f) * 45f;
            if ((effects & BuildEffect.PhaseLungeExtraCharge) != 0) score += 55f;
            if ((effects & BuildEffect.CrushingBlowCleave) != 0) score += 42f;
            if ((effects & BuildEffect.TeleportKillRecovery) != 0) score += 32f + Math.Min(value, 100f) * .35f;
            if ((effects & BuildEffect.BossDamage) != 0) score += 25f + Math.Min(value, 100f) * .4f;
            return score;
        }
    }
}
