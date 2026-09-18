using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [Serializable] internal sealed class BuildSaveData { public List<string> inventory = new(); public List<string> slots = new(); public List<string> gear = new(); public Specialization specialization; }

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
        private PlayerHealth health; private PlayerProgression progression; private BuildStats stats;
        private BuildEffect effects; private float critEnergy, lungeReduction, teleportRecovery; private int enemiesDefeated;

        public IReadOnlyList<PhasebreakItemDefinition> Inventory => inventory;
        public Specialization Specialization { get; private set; }
        public int EnemiesDefeated => enemiesDefeated;
        public float PowerMultiplier => 1f + stats.power + (Specialization == Specialization.Berserker ? .12f : 0f);
        public float CriticalChanceBonus => stats.criticalChance + (Specialization == Specialization.Berserker ? .06f : 0f);
        public float CriticalDamageBonus => .1f + stats.criticalDamage;
        public int BonusHealth => stats.maxHealth + (Specialization == Specialization.Bulwark ? 8 : 0);
        public float Defense => stats.defense + (Specialization == Specialization.Bulwark ? .12f : 0f);
        public float AttackSpeedMultiplier => 1f + stats.attackSpeed;
        public float MovementSpeedMultiplier => 1f + stats.movementSpeed;
        public float BossDamageMultiplier => 1f + stats.bossDamage;
        public float CritEnergyRestore => Has(BuildEffect.CritRestoresEnergy) ? Mathf.Max(6f, critEnergy) : 0f;
        public float PhaseLungeCooldownMultiplier => Has(BuildEffect.PhaseLungeCooldown) ? Mathf.Clamp01(1f - Mathf.Max(.15f, lungeReduction)) : (Specialization == Specialization.Riftblade ? .82f : 1f);
        public int PhaseLungeExtraCharges => Has(BuildEffect.PhaseLungeExtraCharge) ? 1 : 0;
        public bool CrushingBlowCleave => Has(BuildEffect.CrushingBlowCleave);
        public float TeleportKillRecovery => Has(BuildEffect.TeleportKillRecovery) ? Mathf.Max(20f, teleportRecovery) : 0f;
        public event Action BuildChanged;
        public event Action<PhasebreakItemDefinition> LootAcquired;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>(); progression = GetComponent<PlayerProgression>();
            foreach (PhasebreakItemDefinition item in itemCatalog) if (item != null && !string.IsNullOrWhiteSpace(item.id)) byId[item.id] = item;
            LoadOrSeed(); Recalculate();
        }
        public PhasebreakItemDefinition GetEquipped(EquipmentSlot slot) => equipped.GetValueOrDefault(slot);

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
        public bool ChooseSpecialization(Specialization choice)
        {
            if (choice == Specialization.Unchosen || Specialization != Specialization.Unchosen || progression == null || progression.Level < 3) return false;
            Specialization = choice; Changed(); return true;
        }
        public bool GrantRewardById(string id)
        {
            if (!byId.TryGetValue(id, out PhasebreakItemDefinition item)) return false;
            inventory.Add(item); Save(); LootAcquired?.Invoke(item); BuildChanged?.Invoke(); return true;
        }
        public bool AddToInventory(PhasebreakItemDefinition item)
        {
            if (item == null || !byId.ContainsKey(item.id)) return false;
            inventory.Add(item); Save(); LootAcquired?.Invoke(item); BuildChanged?.Invoke(); return true;
        }
        public List<PhasebreakItemDefinition> GenerateCorpseLoot()
        {
            List<PhasebreakItemDefinition> drops = new(); enemiesDefeated++;
            if (itemCatalog == null || itemCatalog.Length == 0 ||
                (enemiesDefeated > 3 && UnityEngine.Random.value > dropChanceAfterFirstThree)) return drops;
            drops.Add(itemCatalog[(enemiesDefeated - 1) % itemCatalog.Length]);
            if (enemiesDefeated % 5 == 0 && itemCatalog.Length > 1)
                drops.Add(itemCatalog[enemiesDefeated % itemCatalog.Length]);
            return drops;
        }
        public int GetTagCount(ItemTag tag) => equipped.Values.Count(i => i != null && (i.tags & tag) != 0);
        public string GetBuildSummary()
        {
            string spec = Specialization == Specialization.Unchosen ? "Choose at level 3" : Specialization.ToString();
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
            foreach (IGrouping<PhasebreakItemSetDefinition, PhasebreakItemDefinition> group in equipped.Values.Where(i => i != null && i.itemSet != null).GroupBy(i => i.itemSet)) yield return $"{group.Key.displayName}  {group.Count()}/6";
        }
        private void Changed() { Recalculate(); Save(); BuildChanged?.Invoke(); }
        private void LoadOrSeed()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) { inventory.AddRange(startingInventory.Where(i => i != null)); Save(); return; }
            BuildSaveData data = JsonUtility.FromJson<BuildSaveData>(PlayerPrefs.GetString(SaveKey)) ?? new BuildSaveData();
            if (data.inventory.Count == 0 && data.gear.Count == 0) { inventory.AddRange(startingInventory.Where(i => i != null)); Save(); return; }
            Specialization = data.specialization;
            foreach (string id in data.inventory) if (byId.TryGetValue(id, out PhasebreakItemDefinition item)) inventory.Add(item);
            for (int i = 0; i < Mathf.Min(data.slots.Count, data.gear.Count); i++) if (Enum.TryParse(data.slots[i], out EquipmentSlot slot) && byId.TryGetValue(data.gear[i], out PhasebreakItemDefinition item)) equipped[slot] = item;
        }
        private void Save()
        {
            BuildSaveData data = new() { specialization = Specialization }; data.inventory.AddRange(inventory.Where(i => i != null).Select(i => i.id));
            foreach (KeyValuePair<EquipmentSlot, PhasebreakItemDefinition> pair in equipped) { data.slots.Add(pair.Key.ToString()); data.gear.Add(pair.Value.id); }
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data)); PlayerPrefs.Save();
        }
    }
}
