#if UNITY_EDITOR
using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class BuildZombieEnemies
    {
        private const string ScenePath = "Assets/dashlash.unity";
        private const string Root = "Assets/Phasebreak/Art/Enemies/SZombie";
        private const string ModelPath = Root + "/SK_SZombie_Variant_1.fbx";
        private const string MaterialPath = Root + "/SZombie_Variant_1.mat";
        private const string TelegraphMaterialPath = Root + "/ZombieTelegraph.mat";
        private const string ControllerPath = Root + "/SZombie.controller";
        private const string PrefabPath = Root + "/SZombieEnemy.prefab";
        private const string PopulationName = "Zombie Population";

        private static readonly Vector3[] SpawnPositions =
        {
            new(-18f, 0f, -24f), new(-11f, 0f, -19f),
            new(17f, 0f, -23f), new(25f, 0f, -15f),
            new(-27f, 0f, 4f), new(-20f, 0f, 13f),
            new(19f, 0f, 9f), new(28f, 0f, 19f)
        };

        private static readonly float[] SpawnRotations = { 18f, 142f, -35f, 205f, 78f, -112f, 164f, 248f };

        [MenuItem("Phasebreak/Build Zombie Enemy Population")]
        public static void Build()
        {
            ConfigureImports();
            Material bodyMaterial = BuildBodyMaterial();
            Material telegraphMaterial = BuildTelegraphMaterial();
            AnimatorController animatorController = BuildAnimatorController();
            GameObject prefab = BuildPrefab(bodyMaterial, telegraphMaterial, animatorController);
            InstallPopulation(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_ZOMBIES_BUILT: reusable zombie prefab and eight-enemy starting population installed.");
        }

        private static void ConfigureImports()
        {
            ModelImporter modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (modelImporter == null)
                throw new MissingReferenceException($"Zombie model missing at {ModelPath}.");
            bool modelChanged = modelImporter.animationType != ModelImporterAnimationType.Human ||
                                modelImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.animationType = ModelImporterAnimationType.Human;
            modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            modelImporter.importAnimation = true;
            modelImporter.importBlendShapes = true;
            if (modelChanged)
                modelImporter.SaveAndReimport();

            const string normalPath = Root + "/Textures/T_SZombie_Variant_1_Normal.png";
            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
        }

        private static Material BuildBodyMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = "SZombie Variant 1" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(
                Root + "/Textures/T_SZombie_Variant_1_A_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(
                Root + "/Textures/T_SZombie_Variant_1_Normal.png");
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(
                Root + "/Textures/T_SZombie_Variant_1_Mask.png");
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normal);
            if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", mask);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", .05f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .42f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material BuildTelegraphMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(TelegraphMaterialPath);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            material = new Material(shader) { name = "Zombie Attack Telegraph" };
            Color color = new(1f, .12f, .035f, .42f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, TelegraphMaterialPath);
            return material;
        }

        private static AnimatorController BuildAnimatorController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimationClip idle = LoadClip(
                "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/Stand--Idle.anim.fbx",
                "Idle");
            AnimationClip run = LoadClip(
                "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/Locomotion--Run_N.anim.fbx",
                "Run_N");
            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = idle;
            AnimatorState moveState = stateMachine.AddState("Chase / Return");
            moveState.motion = run;
            moveState.speed = .82f;
            stateMachine.defaultState = idleState;

            AnimatorStateTransition toMove = idleState.AddTransition(moveState);
            toMove.hasExitTime = false;
            toMove.duration = .16f;
            toMove.AddCondition(AnimatorConditionMode.Greater, .1f, "Speed");
            AnimatorStateTransition toIdle = moveState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = .14f;
            toIdle.AddCondition(AnimatorConditionMode.Less, .1f, "Speed");
            return controller;
        }

        private static AnimationClip LoadClip(string path, string clipName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is AnimationClip clip && clip.name == clipName)
                    return clip;
            throw new MissingReferenceException($"Animation clip {clipName} missing at {path}.");
        }

        private static GameObject BuildPrefab(Material bodyMaterial, Material telegraphMaterial,
            RuntimeAnimatorController animatorController)
        {
            GameObject sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (sourceModel == null)
                throw new MissingReferenceException($"Zombie model missing at {ModelPath}.");

            GameObject root = new("Risen Zombie");
            try
            {
                CharacterController controller = root.AddComponent<CharacterController>();
                controller.center = new Vector3(0f, .9f, 0f);
                controller.height = 1.8f;
                controller.radius = .43f;
                controller.skinWidth = .06f;
                controller.stepOffset = .3f;
                controller.slopeLimit = 50f;

                GameObject visual = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
                visual.name = "Zombie Visual";
                visual.AddComponent<EnemyAnimationEventRelay>();
                visual.transform.SetParent(root.transform, false);
                visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterial = bodyMaterial;
                Animator animator = visual.GetComponentInChildren<Animator>(true);
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;

                GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                telegraph.name = "Attack Telegraph";
                telegraph.transform.SetParent(root.transform, false);
                telegraph.transform.localPosition = new Vector3(0f, .025f, .15f);
                telegraph.transform.localScale = new Vector3(1.65f, .018f, 1.65f);
                telegraph.GetComponent<Renderer>().sharedMaterial = telegraphMaterial;
                Object.DestroyImmediate(telegraph.GetComponent<Collider>());

                MeleeEnemy enemy = root.AddComponent<MeleeEnemy>();
                enemy.Configure(null, telegraph.transform);
                ConfigureEnemy(enemy);
                Targetable targetable = root.AddComponent<Targetable>();
                targetable.Configure("Risen Zombie", TargetFaction.Hostile, 2);
                SerializedObject targetSettings = new(targetable);
                targetSettings.FindProperty("nameplateHeight").floatValue = 2.1f;
                targetSettings.FindProperty("selectionRingRadius").floatValue = .78f;
                targetSettings.ApplyModifiedPropertiesWithoutUndo();

                EnemyVisualAnimator visualAnimator = root.AddComponent<EnemyVisualAnimator>();
                visualAnimator.Configure(enemy, controller, animator, visual.transform);
                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureEnemy(MeleeEnemy enemy)
        {
            SerializedObject settings = new(enemy);
            settings.FindProperty("maxHealth").intValue = 24;
            settings.FindProperty("experienceReward").intValue = 10;
            settings.FindProperty("respawnEnabled").boolValue = true;
            settings.FindProperty("awarenessRange").floatValue = 11f;
            settings.FindProperty("moveSpeed").floatValue = 3.1f;
            settings.FindProperty("rotationSpeed").floatValue = 480f;
            settings.FindProperty("leashRange").floatValue = 17f;
            settings.FindProperty("returnStopDistance").floatValue = .18f;
            settings.FindProperty("attackRange").floatValue = 1.65f;
            settings.FindProperty("attackArc").floatValue = 90f;
            settings.FindProperty("windupDuration").floatValue = .72f;
            settings.FindProperty("activeDuration").floatValue = .14f;
            settings.FindProperty("recoveryDuration").floatValue = .88f;
            settings.FindProperty("attackDamage").intValue = 1;
            settings.FindProperty("staggerDuration").floatValue = .2f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void InstallPopulation(GameObject prefab)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Stop Play Mode before installing zombie population.");

            GameObject oldCombatRoot = GameObject.Find("Combat Enemies");
            if (oldCombatRoot != null)
                Undo.DestroyObjectImmediate(oldCombatRoot);
            GameObject oldPopulation = GameObject.Find(PopulationName);
            if (oldPopulation != null)
                Undo.DestroyObjectImmediate(oldPopulation);

            GameObject population = new(PopulationName);
            Undo.RegisterCreatedObjectUndo(population, "Create zombie population");
            PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            List<MeleeEnemy> enemies = new();
            for (int i = 0; i < SpawnPositions.Length; i++)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, population.transform) as GameObject;
                instance.name = $"Risen Zombie {i + 1:00}";
                instance.transform.SetPositionAndRotation(SpawnPositions[i], Quaternion.Euler(0f, SpawnRotations[i], 0f));
                MeleeEnemy enemy = instance.GetComponent<MeleeEnemy>();
                enemy.Configure(playerHealth != null ? playerHealth.transform : null,
                    instance.transform.Find("Attack Telegraph"));
                enemies.Add(enemy);
            }

            CombatArenaReset resetter = Object.FindAnyObjectByType<CombatArenaReset>();
            if (resetter != null && playerHealth != null)
                resetter.Configure(playerHealth.transform, playerHealth, enemies.ToArray());
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
