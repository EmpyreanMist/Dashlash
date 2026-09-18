using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildRiftDungeon
    {
        private const string RootName = "Rift Crypt";

        [MenuItem("Phasebreak/Build Rift Crypt")]
        public static void Build()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null || player.GetComponent<PlayerHealth>() == null)
            {
                Debug.LogError("Build the Phasebreak combat milestone before building Rift Crypt.");
                return;
            }

            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            Material floor = CreateMaterial("Assets/Phasebreak/Materials/CryptFloor.mat",
                new Color(0.07f, 0.09f, 0.13f));
            Material wall = CreateMaterial("Assets/Phasebreak/Materials/CryptWall.mat",
                new Color(0.16f, 0.18f, 0.25f));
            Material accent = CreateMaterial("Assets/Phasebreak/Materials/CryptAccent.mat",
                new Color(0.22f, 0.08f, 0.38f));
            Material rift = CreateMaterial("Assets/Phasebreak/Materials/RiftPortal.mat",
                new Color(0.35f, 0.12f, 0.78f), true);
            Material gateMaterial = CreateMaterial("Assets/Phasebreak/Materials/RiftGate.mat",
                new Color(0.55f, 0.1f, 0.18f), true);
            Material enemyMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Phasebreak/Materials/Enemy.mat");
            Material telegraphMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Phasebreak/Materials/EnemyTelegraph.mat");

            GameObject root = new GameObject(RootName);
            Transform entrance = CreatePortal("Rift Crypt Entrance", root.transform,
                new Vector3(-8f, 0.08f, 8f), rift, true);

            GameObject architecture = new GameObject("Architecture");
            architecture.transform.SetParent(root.transform);
            BuildArchitecture(architecture.transform, floor, wall, accent);

            Transform spawn = CreateMarker("Dungeon Spawn", root.transform,
                new Vector3(0f, 1f, 45f));
            Transform checkpointOne = CreateMarker("Checkpoint 1", root.transform,
                new Vector3(0f, 1f, 45f));
            Transform checkpointTwo = CreateMarker("Checkpoint 2", root.transform,
                new Vector3(0f, 1f, 59f));
            Transform checkpointThree = CreateMarker("Checkpoint 3", root.transform,
                new Vector3(0f, 1f, 73f));

            GameObject gateOne = CreateBlock("Gate 1", architecture.transform,
                new Vector3(0f, 1.5f, 57f), new Vector3(4.5f, 3f, 0.35f), gateMaterial);
            GameObject gateTwo = CreateBlock("Gate 2", architecture.transform,
                new Vector3(0f, 1.5f, 71f), new Vector3(4.5f, 3f, 0.35f), gateMaterial);

            GameObject encountersRoot = new GameObject("Encounters");
            encountersRoot.transform.SetParent(root.transform);
            RiftDungeonEncounter first = CreateEncounter("1 - The Threshold", encountersRoot.transform,
                "the Threshold Guardians", checkpointOne, gateOne, false,
                new[]
                {
                    CreateEnemy("Crypt Guardian A", encountersRoot.transform, new Vector3(-2.2f, 1f, 51f),
                        player.transform, enemyMaterial, telegraphMaterial, 22, 12),
                    CreateEnemy("Crypt Guardian B", encountersRoot.transform, new Vector3(2.2f, 1f, 53.5f),
                        player.transform, enemyMaterial, telegraphMaterial, 22, 12)
                });
            RiftDungeonEncounter second = CreateEncounter("2 - The Reliquary", encountersRoot.transform,
                "the Reliquary Keepers", checkpointTwo, gateTwo, false,
                new[]
                {
                    CreateEnemy("Reliquary Keeper A", encountersRoot.transform, new Vector3(-2.5f, 1f, 64f),
                        player.transform, enemyMaterial, telegraphMaterial, 28, 14),
                    CreateEnemy("Reliquary Keeper B", encountersRoot.transform, new Vector3(2.5f, 1f, 67f),
                        player.transform, enemyMaterial, telegraphMaterial, 28, 14)
                });

            MeleeEnemy boss = CreateEnemy("Rift Warden", encountersRoot.transform,
                new Vector3(0f, 1f, 79f), player.transform, enemyMaterial, telegraphMaterial, 90, 36);
            boss.transform.Find("Body").localScale = new Vector3(1.45f, 1.45f, 1.45f);
            ConfigureEnemy(boss, "moveSpeed", 2.8f);
            ConfigureEnemy(boss, "awarenessRange", 13f);
            ConfigureEnemy(boss, "windupDuration", 0.8f);

            Transform frontal = CreateTelegraph("Frontal Slam Telegraph", boss.transform,
                PrimitiveType.Cube, telegraphMaterial);
            Transform ground = CreateTelegraph("Rift Rupture Telegraph", root.transform,
                PrimitiveType.Cylinder, telegraphMaterial);
            RiftWardenBoss bossMechanics = boss.gameObject.AddComponent<RiftWardenBoss>();
            bossMechanics.Configure(player.transform, frontal, ground);
            RiftDungeonEncounter third = CreateEncounter("3 - Rift Warden", encountersRoot.transform,
                "the Rift Warden", checkpointThree, null, true, new[] { boss });

            Transform chest = CreateChest(root.transform, new Vector3(0f, 0.65f, 83f), accent, rift);
            Transform exit = CreatePortal("Exit Rift", root.transform, new Vector3(0f, 0.08f, 85f),
                rift, false);

            RiftDungeonController controller = root.AddComponent<RiftDungeonController>();
            CombatArenaReset reset = Object.FindAnyObjectByType<CombatArenaReset>();
            controller.Configure(player.transform, entrance, spawn, chest, exit,
                new[] { first, second, third }, reset);

            PhasebreakHud hud = Object.FindAnyObjectByType<PhasebreakHud>();
            if (hud != null)
            {
                SerializedObject hudSettings = new SerializedObject(hud);
                hudSettings.FindProperty("dungeon").objectReferenceValue = controller;
                hudSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("PB_DUNGEON_BUILT Rift Crypt with 3 encounters, boss, reward and exit.");
        }

        private static void BuildArchitecture(Transform parent, Material floor, Material wall, Material accent)
        {
            CreateBlock("Crypt Floor", parent, new Vector3(0f, -0.5f, 64f),
                new Vector3(12f, 1f, 44f), floor);
            CreateBlock("West Wall", parent, new Vector3(-6.25f, 1.5f, 64f),
                new Vector3(0.5f, 4f, 44f), wall);
            CreateBlock("East Wall", parent, new Vector3(6.25f, 1.5f, 64f),
                new Vector3(0.5f, 4f, 44f), wall);
            CreateBlock("Entrance Wall", parent, new Vector3(0f, 1.5f, 42f),
                new Vector3(12f, 4f, 0.5f), wall);
            CreateBlock("Boss Wall", parent, new Vector3(0f, 1.5f, 86f),
                new Vector3(12f, 4f, 0.5f), wall);

            for (int z = 46; z <= 82; z += 6)
            {
                CreateBlock($"West Pillar {z}", parent, new Vector3(-5.35f, 1.25f, z),
                    new Vector3(0.8f, 2.5f, 0.8f), accent);
                CreateBlock($"East Pillar {z}", parent, new Vector3(5.35f, 1.25f, z),
                    new Vector3(0.8f, 2.5f, 0.8f), accent);
            }
        }

        private static RiftDungeonEncounter CreateEncounter(string objectName, Transform parent,
            string title, Transform checkpoint, GameObject gate, bool boss, MeleeEnemy[] enemies)
        {
            GameObject item = new GameObject(objectName);
            item.transform.SetParent(parent);
            RiftDungeonEncounter encounter = item.AddComponent<RiftDungeonEncounter>();
            encounter.Configure(title, enemies, gate, checkpoint, boss);
            return encounter;
        }

        private static MeleeEnemy CreateEnemy(string name, Transform parent, Vector3 position,
            Transform player, Material bodyMaterial, Material telegraphMaterial, int health, int xp)
        {
            GameObject enemyObject = new GameObject(name);
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = position;
            CharacterController controller = enemyObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.48f;
            controller.center = Vector3.zero;
            controller.skinWidth = 0.06f;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemyObject.transform, false);
            body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(enemyObject.transform, false);
            blade.transform.localPosition = new Vector3(0.65f, 0.15f, 0.25f);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            blade.transform.localScale = new Vector3(0.13f, 1.2f, 0.13f);
            blade.GetComponent<Renderer>().sharedMaterial = telegraphMaterial;
            Object.DestroyImmediate(blade.GetComponent<Collider>());

            GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            telegraph.name = "Attack Telegraph";
            telegraph.transform.SetParent(enemyObject.transform, false);
            telegraph.transform.localPosition = new Vector3(0f, -0.96f, 0f);
            telegraph.transform.localScale = new Vector3(3.5f, 0.025f, 3.5f);
            telegraph.GetComponent<Renderer>().sharedMaterial = telegraphMaterial;
            Object.DestroyImmediate(telegraph.GetComponent<Collider>());

            MeleeEnemy enemy = enemyObject.AddComponent<MeleeEnemy>();
            enemy.Configure(player, telegraph.transform);
            enemy.SetRespawnEnabled(false);
            ConfigureEnemy(enemy, "maxHealth", health);
            ConfigureEnemy(enemy, "experienceReward", xp);
            Targetable targetable = enemyObject.AddComponent<Targetable>();
            targetable.Configure(name, TargetFaction.Hostile, name == "Rift Warden" ? 3 : 2);
            return enemy;
        }

        private static Transform CreateTelegraph(string name, Transform parent, PrimitiveType type,
            Material material)
        {
            GameObject visual = GameObject.CreatePrimitive(type);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.SetActive(false);
            return visual.transform;
        }

        private static Transform CreateChest(Transform parent, Vector3 position, Material body,
            Material glow)
        {
            GameObject chest = CreateBlock("Warden's Cache", parent, position,
                new Vector3(1.5f, 0.8f, 1f), body);
            GameObject lid = CreateBlock("Rift Glow", chest.transform, position + Vector3.up * 0.55f,
                new Vector3(1.25f, 0.18f, 0.75f), glow);
            lid.transform.SetParent(chest.transform, true);
            return chest.transform;
        }

        private static Transform CreatePortal(string name, Transform parent, Vector3 position,
            Material material, bool vertical)
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = name;
            portal.transform.SetParent(parent);
            portal.transform.position = position;
            portal.transform.localScale = vertical
                ? new Vector3(1.35f, 0.08f, 1.35f)
                : new Vector3(1.5f, 0.08f, 1.5f);
            portal.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(portal.GetComponent<Collider>());
            return portal.transform;
        }

        private static Transform CreateMarker(string name, Transform parent, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent);
            marker.transform.position = position;
            marker.transform.rotation = Quaternion.identity;
            return marker.transform;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static void ConfigureEnemy(MeleeEnemy enemy, string propertyName, object value)
        {
            SerializedObject settings = new SerializedObject(enemy);
            SerializedProperty property = settings.FindProperty(propertyName);
            switch (value)
            {
                case int intValue:
                    property.intValue = intValue;
                    break;
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material CreateMaterial(string path, Color color, bool emission = false)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2.2f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
