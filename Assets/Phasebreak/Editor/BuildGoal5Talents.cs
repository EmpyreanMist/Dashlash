using System;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildGoal5Talents
    {
        private const string IconRoot = "Assets/Phasebreak/Art/UI/Icons/Talents";
        private const string DataRoot = "Assets/Phasebreak/Data/Talents";
        private const string ResourceRoot = "Assets/Phasebreak/Resources/Talents";

        [MenuItem("Phasebreak/Build Goal 5 Talents")]
        public static void Build()
        {
            EnsureFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureIcons();

            TalentTreeDefinition berserker = Tree("berserker", "Berserker", "Aggressive melee pressure: critical hits build momentum and drive Crushing Blow into a relentless finishing rhythm.", Specialization.Berserker,
                Node("berserker-keen-edge", "Keen Edge", "Sharpen the existing critical-hit foundation.", "keen_edge.png", Specialization.Berserker, TalentNodeType.Passive, 2, TalentEffect.CriticalChance, .025f, new(.5f, .84f)),
                Node("berserker-blood-momentum", "Blood Momentum", "Critical hits ignite a short burst of attack speed.", "blood_momentum.png", Specialization.Berserker, TalentNodeType.Passive, 2, TalentEffect.CriticalMomentum, .06f, new(.28f, .62f), "berserker-keen-edge"),
                Node("berserker-crushing-force", "Crushing Force", "Drive more force through Crushing Blow.", "crushing_force.png", Specialization.Berserker, TalentNodeType.AbilityModifier, 2, TalentEffect.AbilityDamage, .15f, new(.72f, .62f), "berserker-keen-edge", target: "crushing-blow"),
                Node("berserker-executioners-rhythm", "Executioner's Rhythm", "Chaining offensive abilities builds a rising damage cadence.", "executioners_rhythm.png", Specialization.Berserker, TalentNodeType.Passive, 1, TalentEffect.OffensiveRhythm, .18f, new(.28f, .38f), "berserker-blood-momentum"),
                Node("berserker-fracture", "Fracture", "Crushing Blow tears through nearby enemies.", "fracture.png", Specialization.Berserker, TalentNodeType.AbilityModifier, 1, TalentEffect.CrushingCleave, 1f, new(.72f, .38f), "berserker-crushing-force", target: "crushing-blow"),
                Node("berserker-relentless", "Relentless", "Critical pressure feeds Energy while Crushing Blow recovers faster.", "relentless_keystone.png", Specialization.Berserker, TalentNodeType.Keystone, 1, TalentEffect.CriticalEnergy, 10f, new(.5f, .13f), new[] { "berserker-executioners-rhythm", "berserker-fracture" }, "crushing-blow", TalentEffect.AbilityCooldown, .15f));

            TalentNodeDefinition resolute = Node("bulwark-resolute", "Resolute", "Pressure focuses your reserves when Health is low.", "resolute.png", Specialization.Bulwark, TalentNodeType.Choice, 1, TalentEffect.ResourceUnderPressure, .45f, new(.28f, .38f), "bulwark-brace");
            resolute.choiceGroup = "bulwark-pressure"; EditorUtility.SetDirty(resolute);
            TalentNodeDefinition heavyImpact = Node("bulwark-heavy-impact", "Heavy Impact", "After taking damage, empower your next Crushing Blow.", "heavy_impact.png", Specialization.Bulwark, TalentNodeType.Choice, 1, TalentEffect.HeavyImpact, .45f, new(.72f, .38f), "bulwark-fortified", target: "crushing-blow");
            heavyImpact.choiceGroup = "bulwark-pressure"; EditorUtility.SetDirty(heavyImpact);
            TalentTreeDefinition bulwark = Tree("bulwark", "Bulwark", "Stability under pressure: Health and Defense turn incoming force into a controlled counterattack.", Specialization.Bulwark,
                Node("bulwark-iron-constitution", "Iron Constitution", "Reinforce the body against the rift's punishment.", "iron_constitution.png", Specialization.Bulwark, TalentNodeType.Passive, 2, TalentEffect.MaximumHealth, 4f, new(.5f, .84f)),
                Node("bulwark-brace", "Brace", "Taking damage briefly hardens your defenses.", "brace.png", Specialization.Bulwark, TalentNodeType.Passive, 2, TalentEffect.BraceAfterHit, .08f, new(.28f, .62f), "bulwark-iron-constitution"),
                Node("bulwark-fortified", "Fortified", "Build a permanent layer of mitigation.", "fortified.png", Specialization.Bulwark, TalentNodeType.Passive, 2, TalentEffect.Defense, .05f, new(.72f, .62f), "bulwark-iron-constitution"),
                resolute,
                heavyImpact,
                Node("bulwark-last-bastion", "Last Bastion", "At critical Health, become dramatically harder to break.", "last_bastion.png", Specialization.Bulwark, TalentNodeType.Keystone, 1, TalentEffect.LastBastion, .22f, new(.5f, .13f), new[] { "bulwark-brace", "bulwark-fortified" }));

            TalentTreeDefinition riftblade = Tree("riftblade", "Riftblade", "Teleport-driven combat: movement creates momentum, kills sustain mobility, and mastery grows toward a high-investment flicker style.", Specialization.Riftblade,
                Node("riftblade-phase-efficiency", "Phase Efficiency", "Phase Lunge spends less Energy.", "phase_efficiency.png", Specialization.Riftblade, TalentNodeType.AbilityModifier, 2, TalentEffect.AbilityCost, .1f, new(.5f, .84f), target: "phase-lunge"),
                Node("riftblade-lunge-mastery", "Lunge Mastery", "Phase Lunge recharges faster.", "lunge_mastery.png", Specialization.Riftblade, TalentNodeType.AbilityModifier, 2, TalentEffect.AbilityCooldown, .08f, new(.28f, .62f), "riftblade-phase-efficiency", target: "phase-lunge"),
                Node("riftblade-rift-momentum", "Rift Momentum", "Movement abilities leave a short offensive wake.", "rift_momentum.png", Specialization.Riftblade, TalentNodeType.Passive, 2, TalentEffect.MobilityMomentum, .08f, new(.72f, .62f), "riftblade-phase-efficiency"),
                Node("riftblade-echo-step", "Echo Step", "Phase Dash gains an additional charge.", "echo_step.png", Specialization.Riftblade, TalentNodeType.AbilityModifier, 1, TalentEffect.AbilityExtraCharge, 1f, new(.28f, .38f), "riftblade-lunge-mastery", target: "phase-dash"),
                Node("riftblade-rift-execution", "Rift Execution", "Mobility kills feed Energy back into the sequence.", "rift_execution.png", Specialization.Riftblade, TalentNodeType.Passive, 1, TalentEffect.TeleportKillRecovery, 22f, new(.72f, .38f), "riftblade-rift-momentum"),
                Node("riftblade-void-circuit", "Void Circuit", "Mobility kills restore the movement charge that delivered the finishing blow.", "void_circuit.png", Specialization.Riftblade, TalentNodeType.Keystone, 1, TalentEffect.MobilityKillCircuit, 1f, new(.5f, .13f), new[] { "riftblade-echo-step", "riftblade-rift-execution" }));

            TalentNodeDefinition circuit = Array.Find(riftblade.nodes, node => node.id == "riftblade-void-circuit");
            circuit.secondaryEffect = TalentEffect.AbilityDamage;
            circuit.secondaryValuePerRank = .35f;
            circuit.targetAbilityId = "phase-lunge";
            EditorUtility.SetDirty(circuit);

            TalentCatalog catalog = AssetAt<TalentCatalog>($"{ResourceRoot}/TalentCatalog.asset");
            catalog.saveVersion = 1;
            catalog.prototypeBasePoints = 6;
            catalog.pointsPerLevelAfterFirst = 1;
            catalog.trees = new[] { berserker, bulwark, riftblade };
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_GOAL5_TALENTS_BUILT trees=3 nodes=18 basePoints=6");
        }

        private static TalentNodeDefinition Node(string id, string name, string description, string icon, Specialization specialization,
            TalentNodeType type, int ranks, TalentEffect effect, float value, Vector2 position, string prerequisite = null,
            string target = null, TalentEffect secondary = TalentEffect.None, float secondaryValue = 0f) =>
            Node(id, name, description, icon, specialization, type, ranks, effect, value, position,
                string.IsNullOrWhiteSpace(prerequisite) ? Array.Empty<string>() : new[] { prerequisite }, target, secondary, secondaryValue);

        private static TalentNodeDefinition Node(string id, string name, string description, string icon, Specialization specialization,
            TalentNodeType type, int ranks, TalentEffect effect, float value, Vector2 position, string[] prerequisites,
            string target = null, TalentEffect secondary = TalentEffect.None, float secondaryValue = 0f)
        {
            TalentNodeDefinition node = AssetAt<TalentNodeDefinition>($"{DataRoot}/{id}.asset");
            node.id = id; node.displayName = name; node.description = description; node.icon = Icon(icon);
            node.specialization = specialization; node.nodeType = type; node.maximumRank = ranks; node.pointCost = 1;
            node.requiredPlayerLevel = 1; node.prerequisiteIds = prerequisites ?? Array.Empty<string>();
            node.effect = effect; node.effectValuePerRank = value; node.targetAbilityId = target;
            node.secondaryEffect = secondary; node.secondaryValuePerRank = secondaryValue; node.presentationPosition = position;
            EditorUtility.SetDirty(node); return node;
        }

        private static TalentTreeDefinition Tree(string id, string name, string identity, Specialization specialization, params TalentNodeDefinition[] nodes)
        {
            TalentTreeDefinition tree = AssetAt<TalentTreeDefinition>($"{ResourceRoot}/{name}.asset");
            tree.id = id; tree.displayName = name; tree.identity = identity; tree.specialization = specialization;
            tree.icon = PhasebreakIconCatalog.Current?.GetSpecializationIcon(specialization); tree.nodes = nodes;
            EditorUtility.SetDirty(tree); return tree;
        }

        private static Sprite Icon(string file) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconRoot}/{file}");

        private static T AssetAt<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        private static void ConfigureIcons()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear; importer.maxTextureSize = 128; importer.SaveAndReimport();
            }
        }

        private static void EnsureFolders()
        {
            Folder("Assets/Phasebreak/Data", "Talents");
            Folder("Assets/Phasebreak/Resources", "Talents");
        }

        private static void Folder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grand = System.IO.Path.GetDirectoryName(parent)?.Replace('\\', '/');
                string leaf = System.IO.Path.GetFileName(parent);
                if (!string.IsNullOrEmpty(grand)) Folder(grand, leaf);
            }
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
