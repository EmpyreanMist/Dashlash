using System;
using System.Collections.Generic;
using System.Linq;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class AuthorStarterGearCatalog
    {
        private const string ItemRoot = "Assets/Phasebreak/Data/Items";
        private const string SetRoot = "Assets/Phasebreak/Data/Sets";
        private const string ScenePath = "Assets/Phasebreak/Scenes/StarterZone_V2.unity";

        [MenuItem("Phasebreak/Author Starter Gear Catalog")]
        public static void Apply()
        {
            PhasebreakItemSetDefinition trailwarden = Set(
                "Trailwarden Harness", "set.trailwarden", "Trailwarden Harness",
                "Reliable frontier armor for staying mobile under pressure.",
                Bonus(2, "A steadier guard adds 2 Health and 2.5% Defense.", new BuildStats { maxHealth = 2, defense = .025f }),
                Bonus(3, "Travel light: 2.5% movement speed.", new BuildStats { movementSpeed = .025f }));
            PhasebreakItemSetDefinition bloodforged = Set(
                "Bloodforged Regalia", "set.bloodforged", "Bloodforged Regalia",
                "Precision and tempo reward an aggressive fighter.",
                Bonus(2, "Pressure adds 2.5% critical chance and 3% attack speed.",
                    new BuildStats { criticalChance = .025f, attackSpeed = .03f }),
                Bonus(3, "Committed strikes gain 8% critical damage.",
                    new BuildStats { criticalDamage = .08f }));

            List<PhasebreakItemDefinition> additions = new()
            {
                Item("head.frontier-hood", "Frontier Hood", EquipmentSlot.Head, ItemRarity.Common, 1,
                    ItemTag.Defense, null, new BuildStats { maxHealth = 2 },
                    "A stitched hood that keeps dust out and softens glancing blows."),
                Item("hands.frontier-wraps", "Frontier Handwraps", EquipmentSlot.Hands, ItemRarity.Common, 1,
                    ItemTag.Crit, null, new BuildStats { attackSpeed = .02f },
                    "Light wraps leave the fingers free for quick strikes."),
                Item("weapon.trail-cleaver", "Trail Cleaver", EquipmentSlot.PrimaryWeapon, ItemRarity.Common, 2,
                    ItemTag.None, null, new BuildStats { power = .035f, maxHealth = 1 },
                    "A broad work blade that trades finesse for a little staying power."),
                Item("chest.frontier-jerkin", "Frontier Jerkin", EquipmentSlot.Chest, ItemRarity.Common, 2,
                    ItemTag.Defense, null, new BuildStats { maxHealth = 3, defense = .015f },
                    "Leather reinforced for the first dangerous road beyond Northgate."),
                Item("boots.frontier-treads", "Frontier Treads", EquipmentSlot.Boots, ItemRarity.Common, 2,
                    ItemTag.Mobility, null, new BuildStats { movementSpeed = .025f },
                    "Sturdy soles for uneven frontier paths."),
                Item("shoulders.trailwarden-pauldrons", "Trailwarden Pauldrons", EquipmentSlot.Shoulders, ItemRarity.Uncommon, 3,
                    ItemTag.Defense, trailwarden, new BuildStats { maxHealth = 2, defense = .035f },
                    "Balanced guards protect the shoulders without slowing a march."),
                Item("legs.trailwarden-greaves", "Trailwarden Greaves", EquipmentSlot.Legs, ItemRarity.Uncommon, 3,
                    ItemTag.Defense, trailwarden, new BuildStats { maxHealth = 3, defense = .025f },
                    "Flexible plates carry a fighter over broken ground."),
                Item("sigil.wayfinder", "Wayfinder Sigil", EquipmentSlot.Sigil1, ItemRarity.Uncommon, 3,
                    ItemTag.Mobility | ItemTag.Crit, null,
                    new BuildStats { movementSpeed = .02f, criticalChance = .01f },
                    "A small compass rune that rewards finding an opening."),
                Item("head.trailwarden-helm", "Trailwarden Helm", EquipmentSlot.Head, ItemRarity.Uncommon, 4,
                    ItemTag.Defense, trailwarden, new BuildStats { maxHealth = 3, defense = .03f },
                    "An open-faced helm built for watch duty."),
                Item("chest.warden-plate", "Warden Plate", EquipmentSlot.Chest, ItemRarity.Uncommon, 4,
                    ItemTag.Defense, trailwarden, new BuildStats { maxHealth = 5, defense = .045f },
                    "A practical cuirass that holds a line without becoming a burden."),
                Item("weapon.watchsteel", "Watchsteel Saber", EquipmentSlot.PrimaryWeapon, ItemRarity.Uncommon, 4,
                    ItemTag.Crit, null, new BuildStats { power = .065f, criticalChance = .015f },
                    "A narrow guard blade for precise counterstrokes."),
                Item("boots.riftstep", "Riftstep Boots", EquipmentSlot.Boots, ItemRarity.Uncommon, 4,
                    ItemTag.Mobility | ItemTag.Void, null,
                    new BuildStats { movementSpeed = .05f, power = .015f },
                    "Their light soles make a quick approach feel effortless."),
                Item("core.warded-cell", "Warded Cell", EquipmentSlot.Core, ItemRarity.Uncommon, 4,
                    ItemTag.Defense | ItemTag.Energy, null,
                    new BuildStats { maxHealth = 2, defense = .03f },
                    "A stable core for fighters who need to weather the first blow."),
                Item("relic.sparkstone", "Sparkstone Focus", EquipmentSlot.PowerRelic, ItemRarity.Uncommon, 4,
                    ItemTag.Fire | ItemTag.Energy, null,
                    new BuildStats { power = .03f, attackSpeed = .025f },
                    "A warm focus that favors quick pressure over careful defense."),
                Item("weapon.bloodwake", "Bloodwake Edge", EquipmentSlot.PrimaryWeapon, ItemRarity.Rare, 6,
                    ItemTag.Crit | ItemTag.Bleed, bloodforged,
                    new BuildStats { power = .08f, criticalChance = .035f, attackSpeed = .025f },
                    "Its thin edge favors relentless openings over heavy impacts."),
                Item("hands.bloodgrip", "Bloodgrip Gloves", EquipmentSlot.Hands, ItemRarity.Rare, 6,
                    ItemTag.Crit | ItemTag.Bleed, bloodforged,
                    new BuildStats { attackSpeed = .07f, criticalChance = .02f },
                    "A close fit lets every follow-up strike arrive sooner."),
                Item("shoulders.bastion-mantle", "Bastion Mantle", EquipmentSlot.Shoulders, ItemRarity.Rare, 7,
                    ItemTag.Defense, null, new BuildStats { maxHealth = 5, defense = .055f },
                    "Layered shoulder plates turn away crushing blows."),
                Item("head.bloodcrest", "Bloodcrest Visor", EquipmentSlot.Head, ItemRarity.Rare, 7,
                    ItemTag.Crit | ItemTag.Bleed, bloodforged,
                    new BuildStats { criticalChance = .035f, criticalDamage = .06f },
                    "A narrow visor for a hunter who commits to each target.", true),
                Item("legs.bastion", "Bastion Legguards", EquipmentSlot.Legs, ItemRarity.Rare, 8,
                    ItemTag.Defense, null, new BuildStats { maxHealth = 6, defense = .065f },
                    "Heavy greaves anchor a defender against incoming pressure."),
                Item("relic.guard-emblem", "Guard's Emblem", EquipmentSlot.UtilityRelic, ItemRarity.Rare, 6,
                    ItemTag.Defense, null, new BuildStats { maxHealth = 4, defense = .04f },
                    "A field badge awarded for holding the frontier road.", true),
                Item("relic.rift-compass", "Rift Compass", EquipmentSlot.MobilityRelic, ItemRarity.Epic, 8,
                    ItemTag.Mobility | ItemTag.Void, null,
                    new BuildStats { movementSpeed = .065f, power = .035f },
                    "Its needle points toward a path only a practiced Riftblade can see.", true,
                    BuildEffect.PhaseLungeCooldown, .15f),
                Item("chest.bastion-aegis", "Bastion Aegis", EquipmentSlot.Chest, ItemRarity.Epic, 9,
                    ItemTag.Defense | ItemTag.Boss, null,
                    new BuildStats { maxHealth = 10, defense = .085f },
                    "A Warden-grade shell that makes a final stand possible.", true)
            };

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerBuildSystem build = UnityEngine.Object.FindAnyObjectByType<PlayerBuildSystem>();
            if (build == null) throw new InvalidOperationException("StarterZone_V2 has no PlayerBuildSystem.");
            SerializedObject serialized = new(build);
            SerializedProperty catalog = serialized.FindProperty("itemCatalog");
            Dictionary<string, PhasebreakItemDefinition> byId = new(StringComparer.Ordinal);
            for (int i = 0; i < catalog.arraySize; i++)
            {
                PhasebreakItemDefinition existing = catalog.GetArrayElementAtIndex(i).objectReferenceValue as PhasebreakItemDefinition;
                if (existing != null && !string.IsNullOrWhiteSpace(existing.id)) byId.Add(existing.id, existing);
            }
            if (!byId.ContainsKey("weapon.frontier-blade") || !byId.ContainsKey("artifact.riftwarden-heart"))
                throw new InvalidOperationException("StarterZone_V2 is missing its existing starter or boss item.");
            int previousCount = byId.Count;
            foreach (PhasebreakItemDefinition addition in additions) byId[addition.id] = addition;
            catalog.arraySize = byId.Count;
            int index = 0;
            foreach (PhasebreakItemDefinition item in byId.Values)
                catalog.GetArrayElementAtIndex(index++).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(build);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"PB_GEAR_CATALOG_AUTHORED previous={previousCount} authored={additions.Count} total={byId.Count}");
        }

        private static PhasebreakItemSetDefinition Set(string fileName, string id, string displayName,
            string fantasy, params SetBonusDefinition[] bonuses)
        {
            string path = $"{SetRoot}/{fileName}.asset";
            PhasebreakItemSetDefinition set = AssetDatabase.LoadAssetAtPath<PhasebreakItemSetDefinition>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PhasebreakItemSetDefinition>();
                AssetDatabase.CreateAsset(set, path);
            }
            set.id = id;
            set.displayName = displayName;
            set.fantasy = fantasy;
            set.bonuses = bonuses;
            EditorUtility.SetDirty(set);
            return set;
        }

        private static SetBonusDefinition Bonus(int pieces, string description, BuildStats stats)
            => new() { pieces = pieces, description = description, stats = stats };

        private static PhasebreakItemDefinition Item(string id, string displayName, EquipmentSlot slot,
            ItemRarity rarity, int level, ItemTag tags, PhasebreakItemSetDefinition set,
            BuildStats stats, string description, bool reserved = false,
            BuildEffect effect = BuildEffect.None, float effectValue = 0f)
        {
            string path = $"{ItemRoot}/{id.Replace('.', '_')}.asset";
            PhasebreakItemDefinition item = AssetDatabase.LoadAssetAtPath<PhasebreakItemDefinition>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<PhasebreakItemDefinition>();
                AssetDatabase.CreateAsset(item, path);
            }
            item.id = id;
            item.displayName = displayName;
            item.description = description;
            item.slot = slot;
            item.rarity = rarity;
            item.itemLevel = level;
            item.tags = tags;
            item.itemSet = set;
            item.stats = stats;
            item.excludedFromRandomLoot = reserved;
            item.effects = effect;
            item.effectValue = effectValue;
            EditorUtility.SetDirty(item);
            return item;
        }
    }
}
