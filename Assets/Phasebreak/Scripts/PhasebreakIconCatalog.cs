using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/UI/Icon Catalog", fileName = "PhasebreakIconCatalog")]
    public sealed class PhasebreakIconCatalog : ScriptableObject
    {
        [Serializable]
        public struct SlotIcon
        {
            public EquipmentSlot slot;
            public Sprite icon;
        }

        [Serializable]
        public struct SpecializationIcon
        {
            public Specialization specialization;
            public Sprite icon;
        }

        [Serializable]
        public struct NamedIcon
        {
            public string id;
            public Sprite icon;
        }

        private static PhasebreakIconCatalog current;

        [Header("Safe fallbacks")]
        public Sprite fallbackItem;
        public Sprite fallbackAbility;
        public Sprite fallbackBuff;
        public Sprite fallbackDebuff;

        [Header("Data-driven mappings")]
        public SlotIcon[] equipmentSlots;
        public SpecializationIcon[] specializations;
        public NamedIcon[] passives;

        public static PhasebreakIconCatalog Current
        {
            get
            {
                if (current == null)
                    current = Resources.Load<PhasebreakIconCatalog>("UI/PhasebreakIconCatalog");
                return current;
            }
        }

        public Sprite GetSlotIcon(EquipmentSlot slot)
        {
            foreach (SlotIcon entry in equipmentSlots ?? Array.Empty<SlotIcon>())
                if (entry.slot == slot && entry.icon != null)
                    return entry.icon;
            return fallbackItem;
        }

        public Sprite GetSpecializationIcon(Specialization specialization)
        {
            foreach (SpecializationIcon entry in specializations ?? Array.Empty<SpecializationIcon>())
                if (entry.specialization == specialization && entry.icon != null)
                    return entry.icon;
            return fallbackBuff;
        }

        public Sprite GetPassiveIcon(string id)
        {
            foreach (NamedIcon entry in passives ?? Array.Empty<NamedIcon>())
                if (string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase) && entry.icon != null)
                    return entry.icon;
            return fallbackBuff;
        }

        public static void ClearRuntimeCache() => current = null;
    }
}
