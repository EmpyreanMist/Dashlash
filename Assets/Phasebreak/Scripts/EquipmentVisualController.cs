using System;
using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum EquipmentVisualSlot
    {
        PrimaryWeapon,
        Secondary,
        Head,
        Shoulders,
        Chest,
        Hands,
        Legs,
        Boots
    }

    [Serializable]
    public sealed class EquipmentVisualAnchor
    {
        public EquipmentVisualSlot slot;
        public Transform anchor;
    }

    [DisallowMultipleComponent]
    public sealed class EquipmentVisualController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rightHandAttachment;
        [SerializeField] private Transform leftHandAttachment;
        [SerializeField] private EquipmentVisualAnchor[] visualAnchors = Array.Empty<EquipmentVisualAnchor>();

        private readonly Dictionary<EquipmentVisualSlot, GameObject> equippedVisuals = new();

        public Transform RightHandAttachment => rightHandAttachment;
        public Transform LeftHandAttachment => leftHandAttachment;

        public void Configure(Animator characterAnimator, Transform rightHand, Transform leftHand,
            EquipmentVisualAnchor[] anchors)
        {
            animator = characterAnimator;
            rightHandAttachment = rightHand;
            leftHandAttachment = leftHand;
            visualAnchors = anchors ?? Array.Empty<EquipmentVisualAnchor>();
        }

        public GameObject SetVisual(EquipmentVisualSlot slot, GameObject visualPrefab)
        {
            ClearVisual(slot);
            if (visualPrefab == null)
                return null;

            Transform anchor = GetAnchor(slot);
            if (anchor == null)
            {
                Debug.LogWarning($"No visual anchor configured for {slot}.", this);
                return null;
            }

            GameObject instance = Instantiate(visualPrefab, anchor, false);
            instance.name = $"{slot} Visual";
            equippedVisuals[slot] = instance;
            return instance;
        }

        public void ClearVisual(EquipmentVisualSlot slot)
        {
            if (!equippedVisuals.TryGetValue(slot, out GameObject visual) || visual == null)
                return;

            if (Application.isPlaying)
                Destroy(visual);
            else
                DestroyImmediate(visual);
            equippedVisuals.Remove(slot);
        }

        public Transform GetAnchor(EquipmentVisualSlot slot)
        {
            if (slot == EquipmentVisualSlot.PrimaryWeapon && rightHandAttachment != null)
                return rightHandAttachment;
            if (slot == EquipmentVisualSlot.Secondary && leftHandAttachment != null)
                return leftHandAttachment;

            foreach (EquipmentVisualAnchor entry in visualAnchors)
            {
                if (entry != null && entry.slot == slot)
                    return entry.anchor;
            }

            return null;
        }

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }
    }
}
