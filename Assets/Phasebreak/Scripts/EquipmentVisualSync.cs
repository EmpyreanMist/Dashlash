using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class EquipmentVisualSync : MonoBehaviour
    {
        [SerializeField] private PlayerBuildSystem build;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private EquipmentVisualController visuals;
        [SerializeField, Min(0f)] private float sheathDelay = 5f;

        private float sheathAt;
        private bool weaponsDrawn;

        private static readonly IReadOnlyDictionary<EquipmentSlot, EquipmentVisualSlot> SlotMap =
            new Dictionary<EquipmentSlot, EquipmentVisualSlot>
            {
                [EquipmentSlot.PrimaryWeapon] = EquipmentVisualSlot.PrimaryWeapon,
                [EquipmentSlot.Secondary] = EquipmentVisualSlot.Secondary,
                [EquipmentSlot.Head] = EquipmentVisualSlot.Head,
                [EquipmentSlot.Shoulders] = EquipmentVisualSlot.Shoulders,
                [EquipmentSlot.Chest] = EquipmentVisualSlot.Chest,
                [EquipmentSlot.Hands] = EquipmentVisualSlot.Hands,
                [EquipmentSlot.Legs] = EquipmentVisualSlot.Legs,
                [EquipmentSlot.Boots] = EquipmentVisualSlot.Boots
            };

        private void Awake()
        {
            build ??= GetComponentInParent<PlayerBuildSystem>();
            combat ??= GetComponentInParent<PlayerCombat>();
            visuals ??= GetComponent<EquipmentVisualController>();
        }

        private void Start() => Refresh();

        private void OnEnable()
        {
            if (build != null)
                build.BuildChanged += Refresh;
            if (combat != null)
                combat.AbilityStarted += HandleAbilityStarted;
        }

        private void OnDisable()
        {
            if (build != null)
                build.BuildChanged -= Refresh;
            if (combat != null)
                combat.AbilityStarted -= HandleAbilityStarted;
        }

        private void Update()
        {
            if (weaponsDrawn && Time.time >= sheathAt && (combat == null || !combat.IsFlickering))
            {
                weaponsDrawn = false;
                visuals?.SetWeaponsDrawn(false);
            }
        }

        public void Refresh()
        {
            if (build == null || visuals == null)
                return;
            foreach (KeyValuePair<EquipmentSlot, EquipmentVisualSlot> pair in SlotMap)
            {
                PhasebreakItemDefinition item = build.GetEquipped(pair.Key);
                if (item != null && item.visualPrefab != null)
                    visuals.SetVisual(pair.Value, item.visualPrefab);
                else
                    visuals.ClearVisual(pair.Value);
            }
            visuals.SetWeaponsDrawn(weaponsDrawn);
        }

        private void HandleAbilityStarted(AbilityPresentationEvent value)
        {
            if (value.Type == AbilityExecutionType.PhaseDash)
                return;
            weaponsDrawn = true;
            sheathAt = Time.time + sheathDelay;
            visuals?.SetWeaponsDrawn(true);
        }
    }
}
