using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class CorpseLootContainer : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float corpseLifetime = 60f;
        [SerializeField, Min(0f)] private float emptyDespawnDelay = 2f;
        private readonly List<PhasebreakItemDefinition> items = new();
        private MeleeEnemy owner; private Renderer[] renderers; private MaterialPropertyBlock block;
        private CapsuleCollider interactionCollider;
        private LootRarityPresentation rarityPresentation;
        private float expiresAt, emptyAt = float.PositiveInfinity; private bool hovered;
        public IReadOnlyList<PhasebreakItemDefinition> Items => items;
        public bool IsEmpty => items.Count == 0;
        public string DisplayName => owner != null ? owner.gameObject.name : gameObject.name;
        public float CorpseLifetime => corpseLifetime;

        public void Initialize(MeleeEnemy enemy, IEnumerable<PhasebreakItemDefinition> loot)
        {
            owner = enemy; items.Clear(); if (loot != null) items.AddRange(loot);
            renderers = GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock();
            CharacterController character = GetComponent<CharacterController>();
            interactionCollider = gameObject.AddComponent<CapsuleCollider>(); interactionCollider.isTrigger = true;
            if (character != null) { interactionCollider.center = character.center; interactionCollider.radius = character.radius * 1.15f; interactionCollider.height = character.height; }
            expiresAt = Time.unscaledTime + corpseLifetime; emptyAt = items.Count == 0 ? Time.unscaledTime + emptyDespawnDelay : float.PositiveInfinity;
            RefreshPresentation();
        }
        public bool Take(PhasebreakItemDefinition item, PlayerBuildSystem inventory)
        {
            int index = items.IndexOf(item); if (index < 0 || inventory == null || !inventory.AddToInventory(item)) return false;
            items.RemoveAt(index); if (items.Count == 0) emptyAt = Time.unscaledTime + emptyDespawnDelay;
            RefreshPresentation(); return true;
        }
        public int TakeAll(PlayerBuildSystem inventory)
        {
            int count = 0; for (int i = items.Count - 1; i >= 0; i--) if (inventory.AddToInventory(items[i])) { items.RemoveAt(i); count++; }
            if (items.Count == 0) emptyAt = Time.unscaledTime + emptyDespawnDelay;
            RefreshPresentation(); return count;
        }
        public void SetHovered(bool value)
        {
            if (hovered == value) return; hovered = value;
            if (renderers == null) return;
            block ??= new MaterialPropertyBlock();
            foreach (Renderer r in renderers) { if (r == null) continue; r.GetPropertyBlock(block); block.SetColor("_EmissionColor", value ? new Color(.08f,.38f,.55f) : Color.black); r.SetPropertyBlock(block); }
        }
        private void Update()
        {
            if ((Time.unscaledTime >= expiresAt || Time.unscaledTime >= emptyAt) && !PhasebreakInventoryHud.IsLootWindowOpenFor(this))
                Despawn();
        }
        public void Despawn()
        {
            items.Clear(); RefreshPresentation(); SetHovered(false); PhasebreakInventoryHud.NotifyCorpseDespawned(this); gameObject.SetActive(false);
        }
        public void ClearForReset() { items.Clear(); RefreshPresentation(); SetHovered(false); if (interactionCollider != null) Destroy(interactionCollider); enabled = false; Destroy(this); }

        private void RefreshPresentation()
        {
            if (items.Count == 0) { rarityPresentation?.Show(null); return; }
            rarityPresentation ??= GetComponent<LootRarityPresentation>() ?? gameObject.AddComponent<LootRarityPresentation>();
            ItemRarity highest = ItemRarity.Common;
            foreach (PhasebreakItemDefinition item in items)
                if (item != null && item.rarity > highest) highest = item.rarity;
            rarityPresentation.Show(highest);
        }
    }
}
