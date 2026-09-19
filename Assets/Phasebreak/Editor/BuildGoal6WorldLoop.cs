#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Phasebreak.Gameplay;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class BuildGoal6WorldLoop
    {
        private const string ScenePath = "Assets/dashlash.unity";
        private const string DataRoot = "Assets/Phasebreak/Data/Quests";
        private const string NpcMaterialRoot = "Assets/Phasebreak/Data/NpcMaterials";
        private const string ResourceRoot = "Assets/Phasebreak/Resources/World";
        private const string ModelPath = "Assets/Phasebreak/Art/Characters/QuaterniusRegularMale/RegularMale.fbx";
        private const string BodyPath = "Assets/Phasebreak/Art/Characters/QuaterniusRegularMale/RegularMale.mat";
        private const string EyePath = "Assets/Phasebreak/Art/Characters/QuaterniusRegularMale/RegularMaleEyes.mat";

        [MenuItem("Phasebreak/Build Goal 6 World Loop")]
        public static void Build()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(NpcMaterialRoot);
            EnsureFolder(ResourceRoot);
            QuestDefinition[] quests =
            {
                Quest("frontier-01", "Northgate Muster", "Warden Elira asks you to check on the quartermaster before leaving the walls.",
                    "warden-elira", "warden-elira", null, QuestObjectiveKind.Speak, "quartermaster-orin", "Speak with Quartermaster Orin", 1, 18, 4, new(-57, 72)),
                Quest("frontier-02", "Roadside Dead", "Risen dead have reached the roads. Thin their numbers beyond Northgate.",
                    "warden-elira", "warden-elira", "frontier-01", QuestObjectiveKind.Defeat, "Risen Zombie", "Defeat Risen Zombies", 3, 28, 7, new(-105, 112)),
                Quest("frontier-03", "The Broken Signal", "A strange pulse is coming from the Northwest Ruins. Find its source.",
                    "warden-elira", "warden-elira", "frontier-02", QuestObjectiveKind.Discover, "northwest-ruins", "Discover the Northwest Ruins", 1, 24, 6, new(-120, 101)),
                Quest("frontier-04", "Hold the Line", "The rift is strengthening the dead. Clear another pocket before it spreads.",
                    "warden-elira", "warden-elira", "frontier-03", QuestObjectiveKind.Defeat, "Risen Zombie", "Defeat Risen Zombies", 5, 42, 10, new(-116, 96)),
                Quest("frontier-05", "An Eastern Warning", "The scouts at Eastwatch have seen the same energy. Speak with Scout Ilya.",
                    "warden-elira", "scout-ilya", "frontier-04", QuestObjectiveKind.Speak, "scout-ilya", "Speak with Scout Ilya", 1, 30, 8, new(105, 54)),
                Quest("frontier-06", "Rift Crypt Breach", "Enter the Rift Crypt, defeat its Warden, and claim the cache before the breach widens.",
                    "scout-ilya", "scout-ilya", "frontier-05", QuestObjectiveKind.CompleteDungeon, "rift-crypt", "Complete Rift Crypt and claim the cache", 1, 75, 20, new(-8, 8))
            };
            QuestCatalog catalog = AssetAt<QuestCatalog>($"{ResourceRoot}/QuestCatalog.asset");
            catalog.saveVersion = 1;
            catalog.quests = quests;
            EditorUtility.SetDirty(catalog);

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject player = GameObject.Find("Player");
            GameObject hud = GameObject.Find("Phasebreak HUD");
            GameObject world = GameObject.Find("Medieval Starter Realm");
            if (player == null || hud == null || world == null) throw new InvalidOperationException("Goal 6 requires Player, Phasebreak HUD, and Medieval Starter Realm in dashlash.");
            if (player.GetComponent<QuestJournal>() == null) player.AddComponent<QuestJournal>();
            if (hud.GetComponent<WorldQuestHud>() == null) hud.AddComponent<WorldQuestHud>();
            PlayerProgression progression = player.GetComponent<PlayerProgression>();
            if (progression != null)
            {
                SerializedObject serializedProgression = new(progression);
                serializedProgression.FindProperty("maximumLevel").intValue = 10;
                serializedProgression.ApplyModifiedPropertiesWithoutUndo();
            }
            player.transform.position = new Vector3(-55f, Ground(-55f, 80f) + 1.1f, 80f);
            PhasebreakFollowCamera followCamera = Camera.main != null ? Camera.main.GetComponent<PhasebreakFollowCamera>() : null;
            if (followCamera != null) followCamera.SetTarget(player.transform);

            Transform existing = world.transform.Find("Northgate World Loop");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            GameObject root = new("Northgate World Loop"); root.transform.SetParent(world.transform, false);
            GameObject hub = new("Northgate Hub"); hub.transform.SetParent(root.transform, false);
            CreateNpc(hub.transform, "warden-elira", "Warden Elira", "Frontier Warden", "The frontier is restless. We need clear eyes and a steady blade.", -54, 77, 180, new Color(.62f, .78f, .96f));
            CreateNpc(hub.transform, "quartermaster-orin", "Quartermaster Orin", "Supply Officer", "The road is dangerous. Keep your Energy and your wits about you.", -61, 76, 90, new Color(.94f, .73f, .44f));
            CreateNpc(hub.transform, "seer-nara", "Seer Nara", "Rift Scholar", "The rupture has a rhythm. Listen long enough and it answers.", -48, 73, 250, new Color(.72f, .59f, .96f));
            CreateNpc(hub.transform, "guard-vas", "Guard Vas", "Northgate Watch", "The gates are quiet. The road beyond them is not.", -43, 81, 220, new Color(.58f, .9f, .74f));
            CreateNpc(root.transform, "scout-ilya", "Scout Ilya", "Eastwatch Outrider", "The eastern line saw the same void pulse. It leads toward the crypt.", 104, 53, 180, new Color(.68f, .88f, .78f));
            CreateNpc(root.transform, "guard-maren", "Guard Maren", "Westmere Watch", "Westmere still stands, but the dead gather beyond the fields.", -103, -47, 20, new Color(.94f, .69f, .58f));
            CreateLocation(root.transform, "northgate", "Northgate Village", -56, 76, 18);
            CreateLocation(root.transform, "westmere", "Westmere Hamlet", -103, -47, 20);
            CreateLocation(root.transform, "eastwatch", "Eastwatch Hamlet", 105, 54, 20);
            CreateLocation(root.transform, "northwest-ruins", "Northwest Ruins", -120, 101, 18);
            CreateLocation(root.transform, "southern-watch", "Southern Watch Ruins", -78, -108, 18);
            CreateLocation(root.transform, "southeast-ruins", "Southeast Ruins", 117, -94, 18);
            TuneEnemyVariants();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PHASEBREAK_GOAL6_WORLD_LOOP_BUILT quests=6 npcs=6 discoveries=6");
        }

        private static QuestDefinition Quest(string id, string title, string description, string giver, string turnIn,
            string prerequisite, QuestObjectiveKind kind, string target, string objective, int count, int xp, int marks, Vector2 map)
        {
            QuestDefinition quest = AssetAt<QuestDefinition>($"{DataRoot}/{id}.asset");
            quest.id = id; quest.title = title; quest.description = description; quest.giverNpcId = giver;
            quest.turnInNpcId = turnIn; quest.prerequisiteQuestId = prerequisite;
            quest.objectiveKind = kind; quest.targetId = target; quest.objectiveText = objective;
            quest.requiredCount = count; quest.experienceReward = xp; quest.marksReward = marks; quest.mapPosition = map;
            EditorUtility.SetDirty(quest); return quest;
        }

        private static void CreateNpc(Transform parent, string id, string name, string role, string line, float x, float z, float yaw, Color tint)
        {
            GameObject npc = new(name); npc.transform.SetParent(parent, false);
            npc.transform.SetPositionAndRotation(new Vector3(x, Ground(x, z), z), Quaternion.Euler(0, yaw, 0));
            QuestNpc component = npc.AddComponent<QuestNpc>(); component.Configure(id, name, role, line);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model != null)
            {
                GameObject visual = PrefabUtility.InstantiatePrefab(model) as GameObject;
                if (visual != null)
                {
                    visual.name = "Civilian Visual"; visual.transform.SetParent(npc.transform, false);
                    Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0)
                    {
                        Bounds bounds = renderers[0].bounds;
                        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        float scale = bounds.size.y > .01f ? 1.8f / bounds.size.y : 1f;
                        visual.transform.localScale = Vector3.one * scale;
                        bounds = renderers[0].bounds;
                        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        visual.transform.position += Vector3.up * (npc.transform.position.y - bounds.min.y);
                    }
                    Material body = NpcMaterial(id, tint);
                    Material eyes = AssetDatabase.LoadAssetAtPath<Material>(EyePath);
                    foreach (Renderer renderer in renderers)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = materials[i] != null && materials[i].name.ToLowerInvariant().Contains("eye") ? eyes : body;
                        renderer.sharedMaterials = materials;
                    }
                    Animator animator = visual.GetComponent<Animator>();
                    if (animator != null) animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/SourceFiles/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller");
                }
            }
            GameObject labelObject = new("NPC Nameplate", typeof(TextMeshPro)); labelObject.transform.SetParent(npc.transform, false);
            labelObject.transform.localPosition = new Vector3(0, 2.55f, 0);
            TextMeshPro label = labelObject.GetComponent<TextMeshPro>(); label.text = name; label.fontSize = 3.2f;
            label.color = new Color(.9f, .95f, 1f); label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(5.5f, 1.8f);
            labelObject.transform.localScale = Vector3.one * .32f;
        }

        private static Material NpcMaterial(string id, Color tint)
        {
            string path = $"{NpcMaterialRoot}/{id}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(BodyPath);
                if (source == null) return null;
                material = new Material(source) { name = id + " Outfit" };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateLocation(Transform parent, string id, string name, float x, float z, float radius)
        {
            GameObject marker = new(name + " Discovery"); marker.transform.SetParent(parent, false);
            marker.transform.position = new Vector3(x, Ground(x, z), z);
            marker.AddComponent<QuestLocation>().Configure(id, name, radius);
        }

        private static void TuneEnemyVariants()
        {
            foreach (MeleeEnemy enemy in UnityEngine.Object.FindObjectsByType<MeleeEnemy>(FindObjectsSortMode.None))
            {
                if (!enemy.name.StartsWith("Risen Zombie", StringComparison.OrdinalIgnoreCase) || enemy.name.Length < 2 ||
                    !int.TryParse(enemy.name[^2..], out int index)) continue;
                bool brute = index % 8 == 2;
                bool skirmisher = index % 8 == 3;
                if (!brute && !skirmisher) continue;
                SerializedObject serialized = new(enemy);
                if (brute)
                {
                    serialized.FindProperty("maxHealth").intValue = 48;
                    serialized.FindProperty("attackDamage").intValue = 2;
                    serialized.FindProperty("experienceReward").intValue = 18;
                    serialized.FindProperty("moveSpeed").floatValue = 2.8f;
                    serialized.FindProperty("windupDuration").floatValue = .9f;
                    serialized.FindProperty("recoveryDuration").floatValue = 1f;
                }
                else
                {
                    serialized.FindProperty("maxHealth").intValue = 22;
                    serialized.FindProperty("attackDamage").intValue = 1;
                    serialized.FindProperty("experienceReward").intValue = 14;
                    serialized.FindProperty("moveSpeed").floatValue = 5.2f;
                    serialized.FindProperty("windupDuration").floatValue = .38f;
                    serialized.FindProperty("recoveryDuration").floatValue = .48f;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                enemy.name = $"Risen Zombie {(brute ? "Brute" : "Skirmisher")} {index:00}";
                Targetable targetable = enemy.GetComponent<Targetable>();
                if (targetable == null) continue;
                targetable.Configure(brute ? "Risen Zombie Brute" : "Risen Zombie Skirmisher", TargetFaction.Hostile, 2);
                targetable.SetRank(brute ? UnitRank.Rare : UnitRank.Normal);
                EditorUtility.SetDirty(targetable);
            }
        }

        private static float Ground(float x, float z)
        {
            Ray ray = new(new Vector3(x, 120f, z), Vector3.down);
            float terrainY = float.NegativeInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(ray, 240f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.name == "Realm Terrain") terrainY = Mathf.Max(terrainY, hit.point.y);
            }
            return float.IsNegativeInfinity(terrainY) ? 0f : terrainY;
        }

        private static T AssetAt<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

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
