#if UNITY_EDITOR
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class BuildPhase675CombatPresentation
    {
        private const string AbilityRoot = "Assets/Phasebreak/Data/Abilities";
        private const string GearRoot = "Assets/Phasebreak/Prefabs/Gear";
        private const string GearMaterialPath = "Assets/Phasebreak/Materials/PlayerGear.mat";
        private const string ScenePath = "Assets/dashlash.unity";

        [MenuItem("Phasebreak/Build Phase 6.75 Combat Presentation")]
        public static void Build()
        {
            CombatAbilityDefinition strike = Load("strike");
            CombatAbilityDefinition crushing = Load("crushing-blow");
            CombatAbilityDefinition lunge = Load("phase-lunge");
            CombatAbilityDefinition dash = LoadOrCreate("phase-dash");
            CombatAbilityDefinition charge = LoadOrCreate("rift-charge");

            Configure(strike, "strike", "Strike", "1", AbilityExecutionType.Melee,
                5f, 2.4f, 0f, 0f, 0f, 1, 0.16f, 0.14f, 1.7f);
            Configure(crushing, "crushing-blow", "Crushing Blow", "2", AbilityExecutionType.Melee,
                10f, 2.65f, 0f, 5f, 30f, 1, 0.3f, 0.3f, 2.8f);
            crushing.criticalBonus = 0.1f;
            Configure(lunge, "phase-lunge", "Phase Lunge", "3", AbilityExecutionType.PhaseLunge,
                6f, 7f, 0f, 8f, 20f, 1, 0.12f, 0.16f, 8.5f);
            lunge.criticalBonus = 0.05f;
            Configure(dash, "phase-dash", "Phase Dash", "4", AbilityExecutionType.PhaseDash,
                0f, 0f, 0f, 6f, 0f, 2, 0f, 0.04f, 0f);
            dash.requiresTarget = false;
            dash.usesGlobalCooldown = false;
            dash.movementDistance = 4.5f;
            dash.movementDuration = 0.16f;
            Configure(charge, "rift-charge", "Rift Charge", "5", AbilityExecutionType.Charge,
                7f, 14f, 4f, 10f, 15f, 1, 0f, 0.22f, 0f);
            charge.movementDuration = 0.42f;
            charge.stopDistance = 1.35f;

            GameObject weapon = CreateWeaponPrefab();
            GameObject chest = CreateChestPrefab();
            AssignVisual("Assets/Phasebreak/Data/Items/weapon_rift-iron.asset", weapon);
            AssignVisual("Assets/Phasebreak/Data/Items/chest_bulwark.asset", chest);

            InstallQuaterniusPlayerVisual.Install();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerCombat combat = Object.FindAnyObjectByType<PlayerCombat>();
            SerializedObject serializedCombat = new(combat);
            SerializedProperty definitions = serializedCombat.FindProperty("abilityDefinitions");
            definitions.arraySize = 5;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = strike;
            definitions.GetArrayElementAtIndex(1).objectReferenceValue = crushing;
            definitions.GetArrayElementAtIndex(2).objectReferenceValue = lunge;
            definitions.GetArrayElementAtIndex(3).objectReferenceValue = dash;
            definitions.GetArrayElementAtIndex(4).objectReferenceValue = charge;
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combat);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_PHASE_675_BUILT: five abilities, combat presentation and test gear installed.");
        }

        private static CombatAbilityDefinition Load(string name) =>
            AssetDatabase.LoadAssetAtPath<CombatAbilityDefinition>($"{AbilityRoot}/{name}.asset");

        private static CombatAbilityDefinition LoadOrCreate(string name)
        {
            CombatAbilityDefinition ability = Load(name);
            if (ability != null)
                return ability;
            ability = ScriptableObject.CreateInstance<CombatAbilityDefinition>();
            AssetDatabase.CreateAsset(ability, $"{AbilityRoot}/{name}.asset");
            return ability;
        }

        private static void Configure(CombatAbilityDefinition ability, string id, string displayName,
            string key, AbilityExecutionType type, float damage, float range, float minimumRange,
            float cooldown, float cost, int charges, float windup, float recovery, float impulse)
        {
            ability.id = id;
            ability.displayName = displayName;
            ability.key = key;
            ability.executionType = type;
            ability.damage = damage;
            ability.range = Mathf.Max(0.1f, range);
            ability.minimumRange = minimumRange;
            ability.cooldown = cooldown;
            ability.resourceCost = cost;
            ability.maximumCharges = charges;
            ability.windup = windup;
            ability.recovery = recovery;
            ability.impulse = impulse;
            ability.requiresTarget = type != AbilityExecutionType.PhaseDash;
            ability.usesGlobalCooldown = type != AbilityExecutionType.PhaseDash;
            EditorUtility.SetDirty(ability);
        }

        private static GameObject CreateWeaponPrefab()
        {
            EnsureFolder(GearRoot);
            Material material = GetGearMaterial();
            GameObject root = new("Rift-Iron Edge Visual");
            try
            {
                AddPrimitive(root.transform, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.42f, 0f),
                    new Vector3(0.08f, 0.72f, 0.035f), material);
                AddPrimitive(root.transform, "Guard", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f),
                    new Vector3(0.34f, 0.06f, 0.08f), material);
                AddPrimitive(root.transform, "Grip", PrimitiveType.Cylinder, new Vector3(0f, -0.14f, 0f),
                    new Vector3(0.045f, 0.18f, 0.045f), material);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{GearRoot}/RiftIronEdgeVisual.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CreateChestPrefab()
        {
            EnsureFolder(GearRoot);
            Material material = GetGearMaterial();
            GameObject root = new("Bulwark Chest Visual");
            try
            {
                AddPrimitive(root.transform, "Chest Plate", PrimitiveType.Cube, new Vector3(0f, 0.03f, 0.08f),
                    new Vector3(0.38f, 0.28f, 0.14f), material);
                AddPrimitive(root.transform, "Core Plate", PrimitiveType.Sphere, new Vector3(0f, 0.03f, 0.17f),
                    new Vector3(0.11f, 0.11f, 0.04f), material);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{GearRoot}/BulwarkChestVisual.prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AddPrimitive(Transform parent, string name, PrimitiveType type,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material GetGearMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GearMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                color = new Color(0.08f, 0.2f, 0.31f, 1f)
            };
            material.SetFloat("_Metallic", 0.72f);
            material.SetFloat("_Smoothness", 0.68f);
            AssetDatabase.CreateAsset(material, GearMaterialPath);
            return material;
        }

        private static void AssignVisual(string itemPath, GameObject visual)
        {
            PhasebreakItemDefinition item = AssetDatabase.LoadAssetAtPath<PhasebreakItemDefinition>(itemPath);
            if (item == null)
                return;
            item.visualPrefab = visual;
            EditorUtility.SetDirty(item);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
#endif
