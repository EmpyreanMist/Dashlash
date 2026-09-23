using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum SpecializationChangeResult { Changed, AlreadyActive, InvalidChoice, Defeated, AbilityInProgress }

    [Serializable] internal sealed class BuildSaveData { public List<string> inventory = new(); public List<string> slots = new(); public List<string> gear = new(); public List<string> claimedRewards = new(); public Specialization specialization; }

    [RequireComponent(typeof(PlayerHealth)), DisallowMultipleComponent]
    public sealed class PlayerBuildSystem : MonoBehaviour
    {
        private const string SaveKey = "Phasebreak.Build.v2";
        [SerializeField] private PhasebreakItemDefinition[] itemCatalog;
        [SerializeField] private PhasebreakItemDefinition[] startingInventory;
        [SerializeField, Range(0f, 1f)] private float dropChanceAfterFirstThree = .45f;
        private readonly List<PhasebreakItemDefinition> inventory = new();
        private readonly Dictionary<EquipmentSlot, PhasebreakItemDefinition> equipped = new();
        private readonly Dictionary<string, PhasebreakItemDefinition> byId = new();
        private readonly HashSet<string> claimedRewards = new(StringComparer.OrdinalIgnoreCase);
        private PlayerHealth health; private PlayerProgression progression; private TalentSystem talents; private PlayerCombat combat; private BuildStats stats;
        private BuildEffect effects; private float critEnergy, lungeReduction, teleportRecovery; private int enemiesDefeated;

        public IReadOnlyList<PhasebreakItemDefinition> Inventory => inventory;
        public IReadOnlyList<PhasebreakItemDefinition> ItemCatalog => itemCatalog;
        public Specialization Specialization { get; private set; }
        public int EnemiesDefeated => enemiesDefeated;
        public float PowerMultiplier => 1f + stats.power + (Specialization == Specialization.Berserker ? .12f : 0f) + (talents != null ? talents.GetEffect(TalentEffect.Power) + talents.DynamicPowerBonus : 0f);
        public float CriticalChanceBonus => stats.criticalChance + (Specialization == Specialization.Berserker ? .06f : 0f) + (talents != null ? talents.GetEffect(TalentEffect.CriticalChance) : 0f);
        public float CriticalDamageBonus => .1f + stats.criticalDamage + (talents != null ? talents.GetEffect(TalentEffect.CriticalDamage) : 0f);
        public int BonusHealth => stats.maxHealth + (Specialization == Specialization.Bulwark ? 8 : 0) + Mathf.RoundToInt(talents != null ? talents.GetEffect(TalentEffect.MaximumHealth) : 0f);
        public float Defense => stats.defense + (Specialization == Specialization.Bulwark ? .12f : 0f) + (talents != null ? talents.GetEffect(TalentEffect.Defense) + talents.DynamicDefenseBonus : 0f);
        public float AttackSpeedMultiplier => 1f + stats.attackSpeed + (talents != null ? talents.GetEffect(TalentEffect.AttackSpeed) + talents.DynamicAttackSpeedBonus : 0f);
        public float MovementSpeedMultiplier => 1f + stats.movementSpeed + (talents != null ? talents.GetEffect(TalentEffect.MovementSpeed) : 0f);
        public float BossDamageMultiplier => 1f + stats.bossDamage;
        public float CritEnergyRestore => Mathf.Max(Has(BuildEffect.CritRestoresEnergy) ? Mathf.Max(6f, critEnergy) : 0f, talents != null ? talents.GetEffect(TalentEffect.CriticalEnergy) : 0f);
        public float PhaseLungeCooldownMultiplier => Mathf.Clamp01((Has(BuildEffect.PhaseLungeCooldown) ? 1f - Mathf.Max(.15f, lungeReduction) : (Specialization == Specialization.Riftblade ? .82f : 1f)) * (1f - (talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityCooldown, "phase-lunge") : 0f)));
        public int PhaseLungeExtraCharges => (Has(BuildEffect.PhaseLungeExtraCharge) ? 1 : 0) + Mathf.RoundToInt(talents != null ? talents.GetAbilityEffect(TalentEffect.AbilityExtraCharge, "phase-lunge") : 0f);
        public bool CrushingBlowCleave => Has(BuildEffect.CrushingBlowCleave) || (talents != null && talents.HasEffect(TalentEffect.CrushingCleave));
        public float TeleportKillRecovery => Mathf.Max(Has(BuildEffect.TeleportKillRecovery) ? Mathf.Max(20f, teleportRecovery) : 0f, talents != null ? talents.GetEffect(TalentEffect.TeleportKillRecovery) : 0f);
        public bool TeleportKillRestoresCharge => Has(BuildEffect.TeleportKillRecovery);
        public event Action BuildChanged;
        public event Action<PhasebreakItemDefinition> LootAcquired;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>(); progression = GetComponent<PlayerProgression>(); combat = GetComponent<PlayerCombat>();
            talents = GetComponent<TalentSystem>() ?? gameObject.AddComponent<TalentSystem>();
            foreach (PhasebreakItemDefinition item in itemCatalog) if (item != null && !string.IsNullOrWhiteSpace(item.id)) byId[item.id] = item;
            LoadOrSeed(); Recalculate();
        }
        public PhasebreakItemDefinition GetEquipped(EquipmentSlot slot) => equipped.GetValueOrDefault(slot);
        public PhasebreakItemDefinition FindItemById(string id) => id != null && byId.TryGetValue(id, out PhasebreakItemDefinition item) ? item : null;

        public void RefreshTalentModifiers() { Recalculate(); BuildChanged?.Invoke(); }

        public bool Equip(PhasebreakItemDefinition item)
        {
            if (item == null || !inventory.Contains(item)) return false;
            EquipmentSlot slot = ResolveSlot(item);
            if (equipped.TryGetValue(slot, out PhasebreakItemDefinition old) && old != null) inventory.Add(old);
            inventory.Remove(item); equipped[slot] = item; Changed(); return true;
        }
        public bool Unequip(EquipmentSlot slot)
        {
            if (!equipped.Remove(slot, out PhasebreakItemDefinition item) || item == null) return false;
            inventory.Add(item); Changed(); return true;
        }
        public SpecializationChangeResult SetSpecialization(Specialization choice)
        {
            if (choice is not (Specialization.Berserker or Specialization.Bulwark or Specialization.Riftblade))
                return SpecializationChangeResult.InvalidChoice;
            if (choice == Specialization) return SpecializationChangeResult.AlreadyActive;
            if (health != null && !health.IsAlive) return SpecializationChangeResult.Defeated;
            if (combat != null && combat.IsAttacking) return SpecializationChangeResult.AbilityInProgress;
            int[] previousMaximumCharges = combat?.CaptureMaximumCharges();
            Specialization = choice;
            talents?.OnSpecializationChanged();
            Recalculate();
            combat?.ReconcileAbilityModifiers(previousMaximumCharges);
            Save();
            BuildChanged?.Invoke();
            return SpecializationChangeResult.Changed;
        }
        public bool GrantRewardById(string id)
        {
            if (!byId.TryGetValue(id, out PhasebreakItemDefinition item)) return false;
            inventory.Add(item); Save(); LootAcquired?.Invoke(item); BuildChanged?.Invoke(); return true;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugGrantAndEquipById(string id)
        {
            if (!GrantRewardById(id)) return false;
            PhasebreakItemDefinition item = inventory.LastOrDefault(candidate => candidate != null && candidate.id == id);
            return item != null && Equip(item);
        }

        public static void DebugDeleteSavedState() => PlayerPrefs.DeleteKey(SaveKey);
#endif
        public bool HasClaimedUniqueReward(string id) => !string.IsNullOrWhiteSpace(id) &&
            claimedRewards.Contains(id);
        public bool ClaimUniqueRewardById(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || claimedRewards.Contains(id) ||
                !byId.TryGetValue(id, out PhasebreakItemDefinition item)) return false;
            inventory.Add(item);
            claimedRewards.Add(id);
            Save();
            LootAcquired?.Invoke(item);
            BuildChanged?.Invoke();
            return true;
        }
        public bool AddToInventory(PhasebreakItemDefinition item)
        {
            if (item == null || !byId.ContainsKey(item.id)) return false;
            inventory.Add(item); Save(); LootAcquired?.Invoke(item); BuildChanged?.Invoke(); return true;
        }
        public List<PhasebreakItemDefinition> GenerateCorpseLoot(bool guaranteed = false)
        {
            List<PhasebreakItemDefinition> drops = new(); enemiesDefeated++;
            PhasebreakItemDefinition[] pool = itemCatalog?.Where(item => item != null && !item.excludedFromRandomLoot).ToArray();
            if (pool == null || pool.Length == 0 ||
                (!guaranteed && enemiesDefeated > 3 && UnityEngine.Random.value > dropChanceAfterFirstThree)) return drops;
            drops.Add(pool[(enemiesDefeated - 1) % pool.Length]);
            if (enemiesDefeated % 5 == 0 && pool.Length > 1)
                drops.Add(pool[enemiesDefeated % pool.Length]);
            return drops;
        }
        public int GetTagCount(ItemTag tag) => equipped.Values.Count(i => i != null && (i.tags & tag) != 0);
        public string GetBuildSummary()
        {
            string spec = Specialization == Specialization.Unchosen ? "Choose in Talents" : Specialization.ToString();
            string sets = string.Join("\n", ActiveSetLines());
            float levelPower = progression != null ? progression.PowerMultiplier : 1f;
            int levelHealth = progression != null ? progression.BonusHealth : 0;
            float passiveCrit = progression != null ? progression.CriticalChanceBonus : 0f;
            string passive = progression != null && progression.HasKeenEdge ? progression.PassiveName : "Locked";
            return $"LEVEL  {(progression != null ? progression.Level : 1)}   PASSIVE  {passive}\nSPECIALIZATION  {spec}\n\nFINAL POWER  x{levelPower * PowerMultiplier:0.00}\nFINAL BONUS HEALTH  +{levelHealth + BonusHealth}\nDEFENSE  {Defense:P0}\nCRIT  +{passiveCrit + CriticalChanceBonus:P0}\nCRIT DAMAGE  +{CriticalDamageBonus:P0}\nATTACK SPEED  +{AttackSpeedMultiplier - 1f:P0}\nMOBILITY  +{MovementSpeedMultiplier - 1f:P0}\nBOSS DAMAGE  +{BossDamageMultiplier - 1f:P0}\n\nACTIVE SETS\n{(sets.Length == 0 ? "None" : sets)}";
        }
        private EquipmentSlot ResolveSlot(PhasebreakItemDefinition item)
        {
            if (item.slot != EquipmentSlot.Sigil1) return item.slot;
            if (!equipped.ContainsKey(EquipmentSlot.Sigil1)) return EquipmentSlot.Sigil1;
            if (!equipped.ContainsKey(EquipmentSlot.Sigil2)) return EquipmentSlot.Sigil2;
            return EquipmentSlot.Sigil3;
        }
        private void Recalculate()
        {
            stats = default; effects = BuildEffect.None; critEnergy = lungeReduction = teleportRecovery = 0f;
            foreach (PhasebreakItemDefinition item in equipped.Values) if (item != null) { stats += item.stats; AddEffects(item.effects, item.effectValue); }
            foreach (IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition> group in equipped.Values.Where(i => i != null && i.itemSet != null).GroupBy(i => i.itemSet))
                foreach (SetBonusDefinition bonus in group.Key.bonuses ?? Array.Empty<SetBonusDefinition>()) if (group.Count() >= bonus.pieces) { stats += bonus.stats; AddEffects(bonus.effects, bonus.effectValue); }
            if (GetEquipped(EquipmentSlot.WildcardArtifact) != null)
                stats.bossDamage += Enum.GetValues(typeof(ItemTag)).Cast<ItemTag>().Count(t => t != ItemTag.None && GetTagCount(t) > 0) * .02f;
            health?.ApplyEquipmentBonus(BonusHealth);
        }
        private bool Has(BuildEffect effect) => (effects & effect) != 0;
        private void AddEffects(BuildEffect added, float value)
        {
            effects |= added;
            if ((added & BuildEffect.CritRestoresEnergy) != 0) critEnergy = Mathf.Max(critEnergy, value);
            if ((added & BuildEffect.PhaseLungeCooldown) != 0) lungeReduction = Mathf.Max(lungeReduction, value);
            if ((added & BuildEffect.TeleportKillRecovery) != 0) teleportRecovery = Mathf.Max(teleportRecovery, value);
        }
        private IEnumerable<string> ActiveSetLines()
        {
            foreach (IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition> group in equipped.Values.Where(i => i != null && i.itemSet != null).GroupBy(i => i.itemSet))
            {
                int maximum = (group.Key.bonuses ?? Array.Empty<SetBonusDefinition>()).Select(bonus => bonus.pieces).DefaultIfEmpty(0).Max();
                int count = maximum > 0 ? Mathf.Min(group.Count(), maximum) : group.Count();
                yield return $"{group.Key.displayName}  {count}/{maximum}";
            }
        }
        private void Changed() { Recalculate(); Save(); BuildChanged?.Invoke(); }
        private void LoadOrSeed()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) { SeedFreshLoadout(); return; }
            BuildSaveData data = JsonUtility.FromJson<BuildSaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null) { SeedFreshLoadout(); return; }
            Specialization = data.specialization;
            foreach (string id in data.inventory ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(id) && byId.TryGetValue(id, out PhasebreakItemDefinition item)) inventory.Add(item);
            int savedGearCount = Mathf.Min(data.slots?.Count ?? 0, data.gear?.Count ?? 0);
            for (int i = 0; i < savedGearCount; i++)
                if (Enum.TryParse(data.slots[i], out EquipmentSlot slot) &&
                    !string.IsNullOrWhiteSpace(data.gear[i]) &&
                    byId.TryGetValue(data.gear[i], out PhasebreakItemDefinition item)) equipped[slot] = item;
            foreach (string id in data.claimedRewards ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(id)) claimedRewards.Add(id);
            // Older saves may already own the dungeon artifact without a claim record.
            if (inventory.Any(item => item.id == "artifact.riftwarden-heart") ||
                equipped.Values.Any(item => item != null && item.id == "artifact.riftwarden-heart"))
                claimedRewards.Add("artifact.riftwarden-heart");
        }
        private void SeedFreshLoadout()
        {
            foreach (PhasebreakItemDefinition item in startingInventory.Where(item => item != null))
            {
                if (item.slot == EquipmentSlot.PrimaryWeapon && !equipped.ContainsKey(EquipmentSlot.PrimaryWeapon))
                    equipped[EquipmentSlot.PrimaryWeapon] = item;
                else inventory.Add(item);
            }
            Save();
        }
        private void Save()
        {
            BuildSaveData data = new() { specialization = Specialization }; data.inventory.AddRange(inventory.Where(i => i != null).Select(i => i.id));
            foreach (KeyValuePair<EquipmentSlot, PhasebreakItemDefinition> pair in equipped) { data.slots.Add(pair.Key.ToString()); data.gear.Add(pair.Value.id); }
            data.claimedRewards.AddRange(claimedRewards.OrderBy(id => id));
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save();
        }
    }
}
