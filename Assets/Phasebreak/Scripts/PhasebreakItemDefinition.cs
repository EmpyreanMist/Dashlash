using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum EquipmentSlot
    {
        PrimaryWeapon, Secondary, Head, Shoulders, Chest, Hands, Legs, Boots, Core,
        MobilityRelic, PowerRelic, UtilityRelic, Sigil1, Sigil2, Sigil3, WildcardArtifact
    }

    public enum ItemRarity { Common, Uncommon, Rare, Epic, Mythic }
    public enum Specialization { Unchosen, Berserker, Bulwark, Riftblade }

    [Flags]
    public enum ItemTag
    {
        None = 0, Void = 1 << 0, Crit = 1 << 1, Mobility = 1 << 2, Bleed = 1 << 3,
        Fire = 1 << 4, Defense = 1 << 5, Energy = 1 << 6, Boss = 1 << 7
    }

    [Flags]
    public enum BuildEffect
    {
        None = 0, CritRestoresEnergy = 1 << 0, PhaseLungeCooldown = 1 << 1,
        PhaseLungeExtraCharge = 1 << 2, CrushingBlowCleave = 1 << 3,
        TeleportKillRecovery = 1 << 4, BossDamage = 1 << 5
    }

    [Serializable]
    public struct BuildStats
    {
        public float power;
        public int maxHealth;
        public float defense;
        public float criticalChance;
        public float criticalDamage;
        public float attackSpeed;
        public float movementSpeed;
        public float bossDamage;

        public static BuildStats operator +(BuildStats a, BuildStats b) => new()
        {
            power = a.power + b.power, maxHealth = a.maxHealth + b.maxHealth,
            defense = a.defense + b.defense, criticalChance = a.criticalChance + b.criticalChance,
            criticalDamage = a.criticalDamage + b.criticalDamage, attackSpeed = a.attackSpeed + b.attackSpeed,
            movementSpeed = a.movementSpeed + b.movementSpeed, bossDamage = a.bossDamage + b.bossDamage
        };
    }

    [CreateAssetMenu(menuName = "Phasebreak/Item", fileName = "Item")]
    public sealed class PhasebreakItemDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Tooltip("Optional item artwork. UI uses a generated slot glyph when empty.")]
        public Sprite icon;
        public EquipmentSlot slot;
        public ItemRarity rarity;
        [Min(1)] public int itemLevel = 1;
        public ItemTag tags;
        public PhasebreakItemSetDefinition itemSet;
        public BuildStats stats;
        public BuildEffect effects;
        [Min(0f)] public float effectValue;

        public bool CanEquipIn(EquipmentSlot target) => slot == target ||
            (slot == EquipmentSlot.Sigil1 && target is EquipmentSlot.Sigil1 or EquipmentSlot.Sigil2 or EquipmentSlot.Sigil3);
    }

    [Serializable]
    public struct SetBonusDefinition
    {
        [Range(2, 6)] public int pieces;
        public string description;
        public BuildStats stats;
        public BuildEffect effects;
        public float effectValue;
    }

}
