#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class InstallQuaterniusPlayerVisual
    {
        private const string ArtRoot = "Assets/Phasebreak/Art/Characters/QuaterniusRegularMale";
        private const string ModelPath = ArtRoot + "/RegularMale.fbx";
        private const string BodyMaterialPath = ArtRoot + "/RegularMale.mat";
        private const string EyeMaterialPath = ArtRoot + "/RegularMaleEyes.mat";
        private const string PrefabFolder = "Assets/Phasebreak/Prefabs/Player";
        private const string PrefabPath = PrefabFolder + "/RegularMaleVisual.prefab";
        private const string ScenePath = "Assets/dashlash.unity";
        private const string ControllerPath =
            "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";

        [MenuItem("Phasebreak/Install Quaternius Regular Male")]
        public static void Install()
        {
            ConfigureTextures();
            ConfigureModel();
            CreateMaterials();
            GameObject prefab = CreateVisualPrefab();
            InstallInScene(prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_PLAYER_VISUAL_INSTALLED: Quaternius Regular Male is configured and installed.");
        }

        private static void ConfigureTextures()
        {
            ConfigureNormalTexture(ArtRoot + "/RegularMale_Normal.png");
            ConfigureNormalTexture(ArtRoot + "/Eye_Normal.png");
        }

        private static void ConfigureNormalTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer ||
                importer.textureType == TextureImporterType.NormalMap)
                return;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static void ConfigureModel()
        {
            if (AssetImporter.GetAtPath(ModelPath) is not ModelImporter importer)
                throw new InvalidOperationException($"Could not load model importer at {ModelPath}.");

            bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                           importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                           importer.importAnimation || importer.importCameras || importer.importLights;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.addCollider = false;
            importer.optimizeGameObjects = false;
            if (changed)
                importer.SaveAndReimport();

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Avatar avatar = model != null ? model.GetComponent<Animator>()?.avatar : null;
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Quaternius model did not produce a valid Humanoid avatar.");
        }

        private static void CreateMaterials()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material body = LoadOrCreateMaterial(BodyMaterialPath, shader);
            body.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/RegularMale_BaseColor.png"));
            body.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/RegularMale_Normal.png"));
            body.EnableKeyword("_NORMALMAP");
            body.SetFloat("_Smoothness", 0.25f);
            EditorUtility.SetDirty(body);

            Material eyes = LoadOrCreateMaterial(EyeMaterialPath, shader);
            eyes.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/Eye_Brown.png"));
            eyes.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/Eye_Normal.png"));
            eyes.EnableKeyword("_NORMALMAP");
            eyes.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(eyes);
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject CreateVisualPrefab()
        {
            EnsureFolder("Assets/Phasebreak/Prefabs");
            EnsureFolder(PrefabFolder);

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (source == null || controller == null)
                throw new InvalidOperationException("Player model or locomotion controller is missing.");

            GameObject wrapper = new("Regular Male Visual");
            try
            {
                GameObject rig = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (rig == null)
                    throw new InvalidOperationException("Could not instantiate the Quaternius FBX.");
                rig.name = "Rig";
                rig.transform.SetParent(wrapper.transform, false);

                Animator animator = rig.GetComponent<Animator>() ?? rig.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;

                FitRigToPlayerCapsule(rig.transform);
                AssignMaterials(rig);

                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (rightHand == null || leftHand == null)
                    throw new InvalidOperationException("Humanoid hand bones were not mapped.");

                Transform primary = CreateAnchor(rightHand, "Primary Weapon Attachment");
                Transform secondary = CreateAnchor(leftHand, "Secondary Attachment");
                var anchors = new[]
                {
                    Entry(EquipmentVisualSlot.Head, animator, HumanBodyBones.Head, "Head Visual Slot"),
                    Entry(EquipmentVisualSlot.Shoulders, animator, HumanBodyBones.UpperChest, "Shoulders Visual Slot"),
                    Entry(EquipmentVisualSlot.Chest, animator, HumanBodyBones.Chest, "Chest Visual Slot"),
                    Entry(EquipmentVisualSlot.Hands, animator, HumanBodyBones.RightHand, "Hands Visual Slot"),
                    Entry(EquipmentVisualSlot.Legs, animator, HumanBodyBones.Hips, "Legs Visual Slot"),
                    Entry(EquipmentVisualSlot.Boots, animator, HumanBodyBones.RightFoot, "Boots Visual Slot")
                };

                EquipmentVisualController equipment = wrapper.AddComponent<EquipmentVisualController>();
                equipment.Configure(animator, primary, secondary, anchors);
                PlayerVisualAnimator visualAnimator = rig.AddComponent<PlayerVisualAnimator>();
                visualAnimator.Configure(null, null, animator);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabPath);
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wrapper);
            }
        }

        private static void FitRigToPlayerCapsule(Transform rig)
        {
            Renderer[] renderers = rig.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = GetBounds(renderers);
            const float targetHeight = 1.82f;
            float scale = bounds.size.y > 0.001f ? targetHeight / bounds.size.y : 1f;
            rig.localScale = Vector3.one * scale;

            bounds = GetBounds(renderers);
            rig.position += Vector3.up * -bounds.min.y;
            bounds = GetBounds(renderers);
            rig.position += new Vector3(-bounds.center.x, 0f, -bounds.center.z);
        }

        private static Bounds GetBounds(IReadOnlyList<Renderer> renderers)
        {
            if (renderers.Count == 0)
                return new Bounds(Vector3.zero, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void AssignMaterials(GameObject rig)
        {
            Material body = AssetDatabase.LoadAssetAtPath<Material>(BodyMaterialPath);
            Material eyes = AssetDatabase.LoadAssetAtPath<Material>(EyeMaterialPath);
            foreach (Renderer renderer in rig.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string materialName = materials[i] != null ? materials[i].name : string.Empty;
                    materials[i] = materialName.IndexOf("eye", StringComparison.OrdinalIgnoreCase) >= 0 ? eyes : body;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static EquipmentVisualAnchor Entry(EquipmentVisualSlot slot, Animator animator,
            HumanBodyBones bone, string name)
        {
            Transform boneTransform = animator.GetBoneTransform(bone);
            return new EquipmentVisualAnchor
            {
                slot = slot,
                anchor = boneTransform != null ? CreateAnchor(boneTransform, name) : null
            };
        }

        private static Transform CreateAnchor(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;
            GameObject anchor = new(name);
            anchor.transform.SetParent(parent, false);
            return anchor.transform;
        }

        private static void InstallInScene(GameObject visualPrefab)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null || player.GetComponent<PhasebreakPlayerMovement>() == null)
                throw new InvalidOperationException("Phasebreak Player gameplay root was not found in dashlash scene.");

            Transform oldVisual = player.transform.Find("Regular Male Visual");
            if (oldVisual != null)
                UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

            GameObject instance = PrefabUtility.InstantiatePrefab(visualPrefab, scene) as GameObject;
            instance.name = "Regular Male Visual";
            instance.transform.SetParent(player.transform, false);
            instance.transform.localPosition = new Vector3(0f, -1f, 0f);
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            DisablePrototypeRenderer(player.transform.Find("Body"));
            DisablePrototypeRenderer(player.transform.Find("Facing Marker"));

            CharacterController capsule = player.GetComponent<CharacterController>();
            if (capsule == null || Mathf.Abs(capsule.height - 2f) > 0.01f || Mathf.Abs(capsule.radius - 0.45f) > 0.01f)
                Debug.LogWarning("Player CharacterController differs from the verified 2.0 x 0.45 capsule.", player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
        }

        private static void DisablePrototypeRenderer(Transform visual)
        {
            if (visual == null)
                return;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
#endif
