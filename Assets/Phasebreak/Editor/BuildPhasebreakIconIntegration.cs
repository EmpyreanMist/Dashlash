using System;
using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildPhasebreakIconIntegration
    {
        private const string IconRoot = "Assets/Phasebreak/Art/UI/Icons";
        private const string DataRoot = "Assets/Phasebreak/Data";
        private const string CatalogPath = "Assets/Phasebreak/Resources/UI/PhasebreakIconCatalog.asset";

        [MenuItem("Phasebreak/Integrate Curated UI Icons")]
        public static void Build()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureIconImports();

            Dictionary<string, string> itemIcons = new()
            {
                ["artifact_riftwarden-heart.asset"] = "Items/heart_of_rift_warden.png",
                ["boots_wake.asset"] = "Items/wake_runner_boots.png",
                ["chest_bulwark.asset"] = "Items/ashen_bulwark_shell.png",
                ["core_riftheart.asset"] = "Items/riftheart_core.png",
                ["hands_phasegrip.asset"] = "Items/phasegrip_gauntlets.png",
                ["relic_blinkwake.asset"] = "Items/blinkwake_drive.png",
                ["relic_fracture.asset"] = "Items/fracture_prism.png",
                ["relic_keen-cell.asset"] = "Items/keen_current_cell.png",
                ["sigil_emberglass.asset"] = "Items/emberglass_sigil.png",
                ["weapon_rift-iron.asset"] = "Items/rift_iron_edge.png"
            };
            foreach (KeyValuePair<string, string> pair in itemIcons)
            {
                PhasebreakItemDefinition item = AssetDatabase.LoadAssetAtPath<PhasebreakItemDefinition>($"{DataRoot}/Items/{pair.Key}");
                AssignIcon(item, Icon(pair.Value), $"item {pair.Key}");
            }

            Dictionary<string, string> abilityIcons = new()
            {
                ["strike.asset"] = "Abilities/strike.png",
                ["crushing-blow.asset"] = "Abilities/crushing_blow.png",
                ["phase-lunge.asset"] = "Abilities/phase_lunge.png",
                ["phase-dash.asset"] = "Abilities/phase_dash.png",
                ["rift-charge.asset"] = "Abilities/rift_charge.png"
            };
            foreach (KeyValuePair<string, string> pair in abilityIcons)
            {
                CombatAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<CombatAbilityDefinition>($"{DataRoot}/Abilities/{pair.Key}");
                AssignIcon(ability, Icon(pair.Value), $"ability {pair.Key}");
            }

            PhasebreakItemSetDefinition set = AssetDatabase.LoadAssetAtPath<PhasebreakItemSetDefinition>($"{DataRoot}/Sets/RiftstalkerCircuit.asset");
            if (set != null)
            {
                set.icon = Icon("Sets/riftstalker_circuit.png");
                EditorUtility.SetDirty(set);
            }

            CreateStatus("crit-surge", "Crit Surge", "Critical precision is temporarily heightened.", StatusEffectKind.Buff, 8f, 1, "Statuses/crit_surge.png");
            CreateStatus("swift-momentum", "Swift Momentum", "Attack speed is increased while momentum holds.", StatusEffectKind.Buff, 10f, 5, "Statuses/swift_momentum.png");
            CreateStatus("poison", "Poison", "Takes periodic poison damage.", StatusEffectKind.Debuff, 8f, 5, "Statuses/poison.png");
            CreateStatus("slow", "Slowed", "Movement speed is reduced.", StatusEffectKind.Debuff, 5f, 1, "Statuses/slow.png");
            CreateStatus("rift-empowerment", "Rift Empowerment", "Void energy empowers the next phase technique.", StatusEffectKind.Buff, 12f, 3, "Statuses/rift_empowerment.png");

            PhasebreakIconCatalog catalog = AssetAt<PhasebreakIconCatalog>(CatalogPath);
            catalog.fallbackItem = Icon("Fallbacks/core.png");
            catalog.fallbackAbility = Icon("Statuses/generic_status.png");
            catalog.fallbackBuff = Icon("Statuses/swift_momentum.png");
            catalog.fallbackDebuff = Icon("Statuses/generic_debuff.png");
            catalog.equipmentSlots = new[]
            {
                Slot(EquipmentSlot.PrimaryWeapon, "Fallbacks/primary_weapon.png"),
                Slot(EquipmentSlot.Secondary, "Fallbacks/secondary.png"),
                Slot(EquipmentSlot.Head, "Fallbacks/head.png"),
                Slot(EquipmentSlot.Shoulders, "Fallbacks/shoulders.png"),
                Slot(EquipmentSlot.Chest, "Fallbacks/chest.png"),
                Slot(EquipmentSlot.Hands, "Fallbacks/hands.png"),
                Slot(EquipmentSlot.Legs, "Fallbacks/legs.png"),
                Slot(EquipmentSlot.Boots, "Fallbacks/boots.png"),
                Slot(EquipmentSlot.Core, "Fallbacks/core.png"),
                Slot(EquipmentSlot.MobilityRelic, "Fallbacks/mobility_relic.png"),
                Slot(EquipmentSlot.PowerRelic, "Fallbacks/power_relic.png"),
                Slot(EquipmentSlot.UtilityRelic, "Fallbacks/utility_relic.png"),
                Slot(EquipmentSlot.Sigil1, "Fallbacks/sigil.png"),
                Slot(EquipmentSlot.Sigil2, "Fallbacks/sigil.png"),
                Slot(EquipmentSlot.Sigil3, "Fallbacks/sigil.png"),
                Slot(EquipmentSlot.WildcardArtifact, "Fallbacks/artifact.png")
            };
            catalog.specializations = new[]
            {
                Spec(Specialization.Berserker, "Specializations/berserker.png"),
                Spec(Specialization.Bulwark, "Specializations/bulwark.png"),
                Spec(Specialization.Riftblade, "Specializations/riftblade.png")
            };
            catalog.passives = new[]
            {
                new PhasebreakIconCatalog.NamedIcon { id = "Keen Edge", icon = Icon("Passives/keen_edge.png") }
            };
            EditorUtility.SetDirty(catalog);
            PhasebreakIconCatalog.ClearRuntimeCache();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"PHASEBREAK_ICONS_INTEGRATED items={itemIcons.Count} abilities={abilityIcons.Count} statuses=5 slotFallbacks={catalog.equipmentSlots.Length}");
        }

        private static void ConfigureIconImports()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 128;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void CreateStatus(string id, string name, string description, StatusEffectKind kind,
            float duration, int stacks, string iconPath)
        {
            string filename = id.Replace('-', '_');
            StatusEffectDefinition status = AssetAt<StatusEffectDefinition>($"{DataRoot}/Statuses/{filename}.asset");
            status.id = id;
            status.displayName = name;
            status.description = description;
            status.kind = kind;
            status.defaultDuration = duration;
            status.maximumStacks = stacks;
            status.icon = Icon(iconPath);
            EditorUtility.SetDirty(status);
        }

        private static PhasebreakIconCatalog.SlotIcon Slot(EquipmentSlot slot, string path) => new() { slot = slot, icon = Icon(path) };
        private static PhasebreakIconCatalog.SpecializationIcon Spec(Specialization specialization, string path) => new() { specialization = specialization, icon = Icon(path) };
        private static Sprite Icon(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconRoot}/{path}");

        private static void AssignIcon(PhasebreakItemDefinition item, Sprite icon, string label)
        {
            if (item == null || icon == null)
                throw new InvalidOperationException($"Cannot assign {label}: data or sprite is missing.");
            item.icon = icon;
            EditorUtility.SetDirty(item);
        }

        private static void AssignIcon(CombatAbilityDefinition ability, Sprite icon, string label)
        {
            if (ability == null || icon == null)
                throw new InvalidOperationException($"Cannot assign {label}: data or sprite is missing.");
            ability.icon = icon;
            EditorUtility.SetDirty(ability);
        }

        private static T AssetAt<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            Folder("Assets/Phasebreak", "Resources");
            Folder("Assets/Phasebreak/Resources", "UI");
            Folder(DataRoot, "Statuses");
        }

        private static void Folder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
