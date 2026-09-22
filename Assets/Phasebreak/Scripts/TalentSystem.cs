using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [Serializable] public sealed class TalentRankSaveData { public string id; public int rank; }
    [Serializable] public sealed class TalentAllocationSaveData
    {
        public int version = 1;
        public List<TalentRankSaveData> allocations = new();
    }

    public enum TalentPurchaseResult { Purchased, MissingDefinition, WrongSpecialization, MaximumRank, InsufficientPoints, LevelRequired, PrerequisiteRequired, ChoiceConflict }

    [DefaultExecutionOrder(-80), DisallowMultipleComponent]
    public sealed class TalentSystem : MonoBehaviour
    {
        private const string SaveKey = "Phasebreak.Talents.v1";
        private readonly Dictionary<string, int> ranks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, TalentNodeDefinition> nodes = new(StringComparer.OrdinalIgnoreCase);
        private TalentCatalog catalog;
        private PlayerProgression progression;
        private PlayerBuildSystem build;
        private PlayerHealth health;
        private PlayerCombat combat;
        private float criticalMomentumUntil;
        private float braceUntil;
        private float mobilityMomentumUntil;
        private float heavyImpactUntil;
        private int rhythmStacks;
        private float rhythmUntil;

        public TalentCatalog Catalog => catalog;
        public int AvailablePoints => GetAvailablePoints(build != null ? build.Specialization : Specialization.Unchosen);
        public int TotalPoints => catalog != null ? catalog.PointsForLevel(progression != null ? progression.Level : 1) : 0;
        public int SpentPoints => GetSpentPoints(build != null ? build.Specialization : Specialization.Unchosen);
        public int GetAvailablePoints(Specialization specialization) => Mathf.Max(0, TotalPoints - GetSpentPoints(specialization));
        public int GetSpentPoints(Specialization specialization) => nodes.Values
            .Where(n => n.specialization == specialization).Sum(n => GetRank(n.id) * Mathf.Max(1, n.pointCost));
        public event Action Changed;
        public event Action AbilityAvailabilityChanged;

        private void Awake()
        {
            catalog = Resources.Load<TalentCatalog>("Talents/TalentCatalog");
            progression = GetComponent<PlayerProgression>();
            build = GetComponent<PlayerBuildSystem>();
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombat>();
            IndexCatalog();
            Load();
        }

        private void OnEnable()
        {
            if (progression != null) progression.ProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            if (progression != null) progression.ProgressChanged -= HandleProgressChanged;
        }

        public TalentTreeDefinition GetTree(Specialization specialization) =>
            catalog?.trees?.FirstOrDefault(t => t != null && t.specialization == specialization);

        public TalentNodeDefinition GetNode(string id) => !string.IsNullOrWhiteSpace(id) && nodes.TryGetValue(id, out TalentNodeDefinition node) ? node : null;
        public int GetRank(string id) => !string.IsNullOrWhiteSpace(id) && ranks.TryGetValue(id, out int rank) ? rank : 0;
        private bool IsActive(TalentNodeDefinition node) => build != null && node.specialization == build.Specialization;
        public bool HasEffect(TalentEffect effect) => nodes.Values.Any(n => IsActive(n) && GetRank(n.id) > 0 &&
            (n.effect == effect || n.secondaryEffect == effect));

        public float GetEffect(TalentEffect effect)
        {
            float total = 0f;
            foreach (TalentNodeDefinition node in nodes.Values)
            {
                if (!IsActive(node)) continue;
                int rank = GetRank(node.id);
                if (node.effect == effect) total += node.effectValuePerRank * rank;
                if (node.secondaryEffect == effect) total += node.secondaryValuePerRank * rank;
            }
            return total;
        }

        public float GetAbilityEffect(TalentEffect effect, string abilityId)
        {
            float total = 0f;
            foreach (TalentNodeDefinition node in nodes.Values)
            {
                if (!IsActive(node) || GetRank(node.id) <= 0 || !string.Equals(node.targetAbilityId, abilityId, StringComparison.OrdinalIgnoreCase)) continue;
                if (node.effect == effect) total += node.effectValuePerRank * GetRank(node.id);
                if (node.secondaryEffect == effect) total += node.secondaryValuePerRank * GetRank(node.id);
            }
            return total;
        }

        public float DynamicPowerBonus => (Time.time < mobilityMomentumUntil ? GetEffect(TalentEffect.MobilityMomentum) : 0f);
        public float DynamicAttackSpeedBonus => Time.time < criticalMomentumUntil ? GetEffect(TalentEffect.CriticalMomentum) : 0f;
        public float DynamicDefenseBonus => (Time.time < braceUntil ? GetEffect(TalentEffect.BraceAfterHit) : 0f) +
            (health != null && health.MaxHealth > 0 && health.CurrentHealth <= health.MaxHealth * .35f ? GetEffect(TalentEffect.LastBastion) : 0f);
        public float RhythmDamageBonus => Time.time < rhythmUntil ? GetEffect(TalentEffect.OffensiveRhythm) * Mathf.Clamp01(rhythmStacks / 3f) : 0f;
        public float HeavyImpactBonus => Time.time < heavyImpactUntil ? GetEffect(TalentEffect.HeavyImpact) : 0f;

        public TalentPurchaseResult CanPurchase(TalentNodeDefinition node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.id)) return TalentPurchaseResult.MissingDefinition;
            if (!IsActive(node)) return TalentPurchaseResult.WrongSpecialization;
            if (GetRank(node.id) >= node.maximumRank) return TalentPurchaseResult.MaximumRank;
            if (GetAvailablePoints(node.specialization) < Mathf.Max(1, node.pointCost)) return TalentPurchaseResult.InsufficientPoints;
            if (progression != null && progression.Level < node.requiredPlayerLevel) return TalentPurchaseResult.LevelRequired;
            if ((node.prerequisiteIds ?? Array.Empty<string>()).Any(id => GetRank(id) <= 0)) return TalentPurchaseResult.PrerequisiteRequired;
            if (!string.IsNullOrWhiteSpace(node.choiceGroup) && nodes.Values.Any(other => other != node &&
                other.specialization == node.specialization && other.choiceGroup == node.choiceGroup && GetRank(other.id) > 0))
                return TalentPurchaseResult.ChoiceConflict;
            return TalentPurchaseResult.Purchased;
        }

        public TalentPurchaseResult Purchase(TalentNodeDefinition node)
        {
            TalentPurchaseResult result = CanPurchase(node);
            if (result != TalentPurchaseResult.Purchased) return result;
            int[] previousMaximumCharges = combat?.CaptureMaximumCharges();
            ranks[node.id] = GetRank(node.id) + 1;
            PersistAndNotify(!string.IsNullOrWhiteSpace(node.grantedAbilityId), previousMaximumCharges);
            return TalentPurchaseResult.Purchased;
        }

        public void ResetTree(Specialization specialization)
        {
            TalentTreeDefinition tree = GetTree(specialization);
            if (tree == null) return;
            bool active = build != null && build.Specialization == specialization;
            int[] previousMaximumCharges = active ? combat?.CaptureMaximumCharges() : null;
            bool abilitiesChanged = active && tree.nodes.Any(n => n != null && GetRank(n.id) > 0 && !string.IsNullOrWhiteSpace(n.grantedAbilityId));
            foreach (TalentNodeDefinition node in tree.nodes) if (node != null) ranks.Remove(node.id);
            if (active) ResetTransientEffects();
            PersistAndNotify(abilitiesChanged, previousMaximumCharges);
        }

        public bool IsAbilityUnlocked(string abilityId, bool isBaseAbility = true) => isBaseAbility ||
            nodes.Values.Any(n => IsActive(n) && GetRank(n.id) > 0 && string.Equals(n.grantedAbilityId, abilityId, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyList<string> GetTalentGrantedAbilities() => nodes.Values
            .Where(n => IsActive(n) && GetRank(n.id) > 0 && !string.IsNullOrWhiteSpace(n.grantedAbilityId))
            .Select(n => n.grantedAbilityId).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        public void NotifyCriticalHit()
        {
            if (HasEffect(TalentEffect.CriticalMomentum)) criticalMomentumUntil = Time.time + 4f;
        }

        public void NotifyDamageTaken()
        {
            if (HasEffect(TalentEffect.BraceAfterHit)) braceUntil = Time.time + 4f;
            if (HasEffect(TalentEffect.HeavyImpact)) heavyImpactUntil = Time.time + 6f;
        }

        public void NotifyAbilityUsed(bool isMovement)
        {
            if (HasEffect(TalentEffect.OffensiveRhythm))
            {
                rhythmStacks = Time.time < rhythmUntil ? Mathf.Min(3, rhythmStacks + 1) : 1;
                rhythmUntil = Time.time + 5f;
            }
            if (isMovement && HasEffect(TalentEffect.MobilityMomentum)) mobilityMomentumUntil = Time.time + 4f;
        }

        public void ConsumeHeavyImpact() => heavyImpactUntil = 0f;

        public void OnSpecializationChanged()
        {
            ResetTransientEffects();
            Changed?.Invoke();
            AbilityAvailabilityChanged?.Invoke();
        }

        private void HandleProgressChanged() => Changed?.Invoke();

        private void IndexCatalog()
        {
            nodes.Clear();
            foreach (TalentTreeDefinition tree in catalog?.trees ?? Array.Empty<TalentTreeDefinition>())
                foreach (TalentNodeDefinition node in tree?.nodes ?? Array.Empty<TalentNodeDefinition>())
                    if (node != null && !string.IsNullOrWhiteSpace(node.id)) nodes[node.id] = node;
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            TalentAllocationSaveData data = JsonUtility.FromJson<TalentAllocationSaveData>(PlayerPrefs.GetString(SaveKey));
            foreach (TalentRankSaveData entry in data?.allocations ?? new List<TalentRankSaveData>())
                if (entry != null && nodes.TryGetValue(entry.id, out TalentNodeDefinition node)) ranks[entry.id] = Mathf.Clamp(entry.rank, 0, node.maximumRank);
        }

        private void Save()
        {
            TalentAllocationSaveData data = new() { version = catalog != null ? catalog.saveVersion : 1 };
            foreach (KeyValuePair<string, int> pair in ranks.Where(p => p.Value > 0)) data.allocations.Add(new TalentRankSaveData { id = pair.Key, rank = pair.Value });
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void PersistAndNotify(bool abilitiesChanged, int[] previousMaximumCharges)
        {
            Save();
            build?.RefreshTalentModifiers();
            combat?.ReconcileAbilityModifiers(previousMaximumCharges);
            Changed?.Invoke();
            if (abilitiesChanged) AbilityAvailabilityChanged?.Invoke();
        }

        public void ResetTransientEffects()
        {
            criticalMomentumUntil = braceUntil = mobilityMomentumUntil = heavyImpactUntil = rhythmUntil = 0f;
            rhythmStacks = 0;
        }
    }
}
