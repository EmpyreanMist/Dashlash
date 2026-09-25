#if UNITY_EDITOR
using System;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Phasebreak.Editor
{
    // Safe to rerun: existing mesh, material and prefab assets retain their GUIDs.
    public static class BuildVisibleEquipment
    {
        private const string Source = "Assets/Phasebreak/Art/KayKitAdventurers/Prefabs";
        private const string Output = "Assets/Phasebreak/Prefabs/Gear/VisibleEquipment";
        private const string Items = "Assets/Phasebreak/Data/Items";
        private const string PlayerVisual = "Assets/Phasebreak/Prefabs/Player/RegularMaleVisual.prefab";

        [MenuItem("Phasebreak/Build Issue 144 Visible Gear")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play Mode before authoring visible equipment.");

            EnsureFolder(Output);
            GameObject frontierBlade = CreateWeapon("FrontierBlade", "sword_1handed", .65f);
            GameObject trailCleaver = CreateWeapon("TrailCleaver", "axe_1handed", .68f, handRoll: 15f);
            GameObject watchsteel = CreateWeapon("WatchsteelSaber", "sword_1handed", .72f,
                new Color(.71f, .85f, 1f, 1f));
            GameObject bloodwake = CreateWeapon("BloodwakeEdge", "axe_2handed", .68f, handRoll: 25f);
            GameObject riftIron = CreateWeapon("RiftIronEdge", "sword_2handed", .68f);

            GameObject trailwardenHelm = CreateHead("TrailwardenHelm", "Knight", -.12f, .15f);
            GameObject bloodcrest = CreateHead("BloodcrestVisor", "Knight", -.12f, .15f,
                new Color(.72f, .42f, .43f, 1f));

            GameObject playerInstance = UnityEngine.Object.Instantiate(Load<GameObject>(PlayerVisual));
            GameObject frontierJerkin;
            GameObject wardenPlate;
            GameObject bastionAegis;
            try
            {
                Animator player = playerInstance.GetComponentInChildren<Animator>(true);
                // The imported bind pose faces backward. Sample the project's idle Humanoid
                // animation so the saved bindposes match the pose used when gameplay starts.
                AnimationClip idle = Array.Find(player.runtimeAnimatorController.animationClips,
                    clip => clip.name == "Idle");
                if (idle == null) throw new InvalidOperationException("Player idle clip is missing.");
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(player.gameObject, idle, 0f);
                frontierJerkin = CreateChest("FrontierJerkin", "Barbarian", .55f, .84f, player);
                wardenPlate = CreateChest("WardenPlate", "Knight", .65f, .78f, player);
                bastionAegis = CreateChest("BastionAegis", "Knight", .72f, .72f, player,
                    new Color(.62f, .71f, .77f, 1f));
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(playerInstance);
            }

            Assign("weapon_frontier-blade", frontierBlade);
            Assign("weapon_trail-cleaver", trailCleaver);
            Assign("weapon_watchsteel", watchsteel);
            Assign("weapon_bloodwake", bloodwake);
            Assign("weapon_rift-iron", riftIron);
            Assign("head_trailwarden-helm", trailwardenHelm);
            Assign("head_bloodcrest", bloodcrest);
            Assign("chest_frontier-jerkin", frontierJerkin);
            Assign("chest_warden-plate", wardenPlate);
            Assign("chest_bastion-aegis", bastionAegis);

            AssetDatabase.SaveAssets();
            Debug.Log("PHASEBREAK_VISIBLE_GEAR_BUILT: five weapons, two heads, three chest visuals.");
        }

        private static GameObject CreateWeapon(string name, string sourceName, float size,
            Color? tint = null, float handRoll = 0f)
        {
            GameObject source = Load<GameObject>($"{Source}/Weapons/WPN_{sourceName}.prefab");
            MeshRenderer sourceRenderer = source.GetComponentInChildren<MeshRenderer>(true);
            GameObject root = new(name);
            try
            {
                CopyRigidMesh(sourceRenderer, root.transform, size, Vector3.zero,
                    tint.HasValue ? Tint(name, sourceRenderer.sharedMaterial, tint.Value) : null);
                root.AddComponent<EquipmentWeaponPlacement>().Configure(Vector3.zero,
                    new Vector3(180f, 0f, handRoll), Vector3.zero, Vector3.zero);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Output}/{name}.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateHead(string name, string family, float y, float size,
            Color? tint = null)
        {
            GameObject source = Load<GameObject>($"{Source}/Armor/ARM_{family}_Head.prefab");
            MeshRenderer sourceRenderer = null;
            foreach (MeshRenderer candidate in source.GetComponentsInChildren<MeshRenderer>(true))
                if (candidate.enabled) { sourceRenderer = candidate; break; }
            if (sourceRenderer == null) throw new InvalidOperationException($"No visible head mesh in {source.name}");
            GameObject root = new(name);
            try
            {
                CopyRigidMesh(sourceRenderer, root.transform, size, new Vector3(0f, y, 0f),
                    tint.HasValue ? Tint(name, sourceRenderer.sharedMaterial, tint.Value) : null);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Output}/{name}.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CopyRigidMesh(MeshRenderer source, Transform destination, float size,
            Vector3 offset, Material replacement)
        {
            GameObject meshObject = new(source.name);
            meshObject.transform.SetParent(destination, false);
            meshObject.transform.localPosition = offset + source.transform.position * size;
            meshObject.transform.localRotation = source.transform.rotation;
            meshObject.transform.localScale = source.transform.lossyScale * size;
            meshObject.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = replacement != null ? new[] { replacement } : source.sharedMaterials;
        }

        private static GameObject CreateChest(string name, string family, float size, float y,
            Animator player, Color? tint = null)
        {
            GameObject source = Load<GameObject>($"{Source}/Armor/ARM_{family}_Chest.prefab");
            GameObject sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            GameObject root = new(name);
            try
            {
                sourceInstance.transform.SetParent(root.transform, false);
                sourceInstance.transform.localPosition = new Vector3(0f, y, 0f);
                sourceInstance.transform.localScale = Vector3.one * size;
                SkinnedMeshRenderer sourceSkin = null;
                foreach (SkinnedMeshRenderer candidate in sourceInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (candidate.enabled) { sourceSkin = candidate; break; }
                if (sourceSkin == null) throw new InvalidOperationException($"No visible chest skin in {source.name}");

                Mesh mesh = UnityEngine.Object.Instantiate(sourceSkin.sharedMesh);
                mesh.name = name + " Bound Mesh";
                Matrix4x4[] bindposes = mesh.bindposes;
                Transform[] sourceBones = sourceSkin.bones;
                EquipmentSkinBone[] mapping = new EquipmentSkinBone[3];
                int found = 0;
                for (int i = 0; i < sourceBones.Length; i++)
                {
                    HumanBodyBones target;
                    switch (sourceBones[i].name)
                    {
                        case "hips": target = HumanBodyBones.Hips; break;
                        case "spine": target = HumanBodyBones.Spine; break;
                        case "chest": target = HumanBodyBones.UpperChest; break;
                        default: continue;
                    }
                    Transform playerBone = player.GetBoneTransform(target);
                    if (playerBone == null) throw new InvalidOperationException($"Missing player bone {target}");
                    bindposes[i] = playerBone.worldToLocalMatrix * sourceBones[i].localToWorldMatrix * bindposes[i];
                    mapping[found++] = new EquipmentSkinBone(i, target);
                }
                if (found != mapping.Length)
                    throw new InvalidOperationException($"Expected hips, spine and chest in {source.name}, found {found}");
                mesh.bindposes = bindposes;
                mesh = SaveMesh(name, mesh);

                GameObject meshObject = new(sourceSkin.name);
                meshObject.transform.SetParent(root.transform, false);
                meshObject.transform.position = sourceSkin.transform.position;
                meshObject.transform.rotation = sourceSkin.transform.rotation;
                meshObject.transform.localScale = sourceSkin.transform.lossyScale;
                SkinnedMeshRenderer skin = meshObject.AddComponent<SkinnedMeshRenderer>();
                skin.sharedMesh = mesh;
                skin.sharedMaterials = tint.HasValue
                    ? new[] { Tint(name, sourceSkin.sharedMaterial, tint.Value) }
                    : sourceSkin.sharedMaterials;
                skin.localBounds = sourceSkin.localBounds;
                skin.updateWhenOffscreen = true;
                root.AddComponent<EquipmentSkinnedMeshBinding>().Configure(skin, mapping);

                UnityEngine.Object.DestroyImmediate(sourceInstance);
                return PrefabUtility.SaveAsPrefabAsset(root, $"{Output}/{name}.prefab");
            }
            finally
            {
                if (sourceInstance != null) UnityEngine.Object.DestroyImmediate(sourceInstance);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = $"{Output}/{name}BoundMesh.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            return existing;
        }

        private static Material Tint(string name, Material source, Color color)
        {
            string path = $"{Output}/{name}Material.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            else EditorUtility.CopySerialized(source, material);
            material.name = name + " Material";
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void Assign(string fileName, GameObject visual)
        {
            PhasebreakItemDefinition item = Load<PhasebreakItemDefinition>($"{Items}/{fileName}.asset");
            if (item.visualPrefab == visual) return;
            item.visualPrefab = visual;
            EditorUtility.SetDirty(item);
        }

        private static T Load<T>(string path) where T : UnityEngine.Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"Missing asset: {path}");

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path[..slash]);
            AssetDatabase.CreateFolder(path[..slash], path[(slash + 1)..]);
        }
    }
}
#endif
