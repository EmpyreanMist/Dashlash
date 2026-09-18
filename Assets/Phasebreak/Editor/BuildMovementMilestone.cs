using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildMovementMilestone
    {
        [MenuItem("Phasebreak/Build Movement Milestone")]
        public static void Build()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "dashlash")
            {
                Debug.LogError("Movement milestone builder only runs in the dashlash scene.");
                return;
            }

            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                Object.DestroyImmediate(root);

            Material floorMaterial = CreateMaterial("Assets/Phasebreak/Materials/Floor.mat", new Color(0.12f, 0.15f, 0.19f));
            Material wallMaterial = CreateMaterial("Assets/Phasebreak/Materials/Walls.mat", new Color(0.24f, 0.29f, 0.36f));
            Material obstacleMaterial = CreateMaterial("Assets/Phasebreak/Materials/Obstacles.mat", new Color(0.15f, 0.52f, 0.58f));
            Material playerMaterial = CreateMaterial("Assets/Phasebreak/Materials/Player.mat", new Color(1f, 0.36f, 0.12f));
            Material slashMaterial = CreateMaterial("Assets/Phasebreak/Materials/Slash.mat", new Color(1f, 0.82f, 0.18f));
            Material dummyMaterial = CreateMaterial("Assets/Phasebreak/Materials/TargetDummy.mat", new Color(0.72f, 0.18f, 0.26f));
            Material jumpMaterial = CreateMaterial("Assets/Phasebreak/Materials/JumpCourse.mat", new Color(0.42f, 0.28f, 0.72f));

            GameObject room = new GameObject("Test Room");
            CreateBlock("Floor", room.transform, new Vector3(0f, -0.5f, 0f), new Vector3(24f, 1f, 24f), floorMaterial);
            CreateBlock("Wall North", room.transform, new Vector3(0f, 1.5f, 12f), new Vector3(24f, 4f, 0.75f), wallMaterial);
            CreateBlock("Wall South", room.transform, new Vector3(0f, 1.5f, -12f), new Vector3(24f, 4f, 0.75f), wallMaterial);
            CreateBlock("Wall East", room.transform, new Vector3(12f, 1.5f, 0f), new Vector3(0.75f, 4f, 24f), wallMaterial);
            CreateBlock("Wall West", room.transform, new Vector3(-12f, 1.5f, 0f), new Vector3(0.75f, 4f, 24f), wallMaterial);

            GameObject obstacles = new GameObject("Obstacles");
            obstacles.transform.SetParent(room.transform);
            CreateBlock("Center Block", obstacles.transform, new Vector3(1.5f, 1f, 1f), new Vector3(3f, 2f, 3f), obstacleMaterial);
            CreateBlock("Dash Lane Wall", obstacles.transform, new Vector3(-5f, 1f, 2f), new Vector3(1f, 2f, 7f), obstacleMaterial);
            CreateBlock("Slide Test Wall", obstacles.transform, new Vector3(5.5f, 1f, -4f), new Vector3(7f, 2f, 0.8f), obstacleMaterial);
            CreateBlock("Corner Block", obstacles.transform, new Vector3(-7f, 0.75f, -6.5f), new Vector3(2.5f, 1.5f, 2.5f), obstacleMaterial);

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 1f, -6f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;
            controller.skinWidth = 0.06f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;
            player.AddComponent<PhasebreakPlayerMovement>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject facing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            facing.name = "Facing Marker";
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0f, 0.25f, 0.48f);
            facing.transform.localScale = new Vector3(0.28f, 0.22f, 0.32f);
            facing.GetComponent<Renderer>().sharedMaterial = wallMaterial;
            Object.DestroyImmediate(facing.GetComponent<Collider>());

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.095f);
            cameraObject.AddComponent<AudioListener>();
            PhasebreakFollowCamera followCamera = cameraObject.AddComponent<PhasebreakFollowCamera>();
            followCamera.SetTarget(player.transform);
            SerializedObject movementSettings = new SerializedObject(player.GetComponent<PhasebreakPlayerMovement>());
            movementSettings.FindProperty("cameraTransform").objectReferenceValue = cameraObject.transform;
            movementSettings.FindProperty("followCamera").objectReferenceValue = followCamera;
            movementSettings.ApplyModifiedPropertiesWithoutUndo();
            SetupCombat(player, followCamera, slashMaterial, dummyMaterial);
            SetupJumpCourse(room, jumpMaterial);
            Material enemyMaterial = CreateMaterial("Assets/Phasebreak/Materials/Enemy.mat", new Color(0.68f, 0.12f, 0.18f));
            Material telegraphMaterial = CreateMaterial("Assets/Phasebreak/Materials/EnemyTelegraph.mat", new Color(1f, 0.12f, 0.04f));
            SetupEnemyCore(player, followCamera, enemyMaterial, telegraphMaterial);
            SetupTargeting(player, followCamera);

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.94f, 0.86f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.44f);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Phasebreak movement milestone built successfully.");
        }

        [MenuItem("Phasebreak/Add Combat Sandbox")]
        public static void AddCombatSandbox()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "dashlash")
            {
                Debug.LogError("Combat sandbox setup only runs in the dashlash scene.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            PhasebreakFollowCamera followCamera = Object.FindAnyObjectByType<PhasebreakFollowCamera>();
            if (player == null || followCamera == null)
            {
                Debug.LogError("The movement milestone player and follow camera must exist first.");
                return;
            }

            Material slashMaterial = CreateMaterial("Assets/Phasebreak/Materials/Slash.mat", new Color(1f, 0.82f, 0.18f));
            Material dummyMaterial = CreateMaterial("Assets/Phasebreak/Materials/TargetDummy.mat", new Color(0.72f, 0.18f, 0.26f));
            SetupCombat(player, followCamera, slashMaterial, dummyMaterial);
            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Phasebreak combat sandbox added successfully.");
        }

        [MenuItem("Phasebreak/Add Jump Sandbox")]
        public static void AddJumpSandbox()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "dashlash")
            {
                Debug.LogError("Jump sandbox setup only runs in the dashlash scene.");
                return;
            }

            GameObject room = GameObject.Find("Test Room");
            if (room == null)
            {
                Debug.LogError("The movement milestone test room must exist first.");
                return;
            }

            Material jumpMaterial = CreateMaterial("Assets/Phasebreak/Materials/JumpCourse.mat", new Color(0.42f, 0.28f, 0.72f));
            SetupJumpCourse(room, jumpMaterial);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Phasebreak jump sandbox added successfully.");
        }

        [MenuItem("Phasebreak/Build Enemy Core")]
        public static void BuildEnemyCore()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "dashlash")
            {
                Debug.LogError("Enemy core setup only runs in the dashlash scene.");
                return;
            }

            GameObject player = GameObject.Find("Player");
            PhasebreakFollowCamera followCamera = Object.FindAnyObjectByType<PhasebreakFollowCamera>();
            if (player == null || followCamera == null)
            {
                Debug.LogError("The movement milestone player and follow camera must exist first.");
                return;
            }

            Material enemyMaterial = CreateMaterial("Assets/Phasebreak/Materials/Enemy.mat", new Color(0.68f, 0.12f, 0.18f));
            Material telegraphMaterial = CreateMaterial("Assets/Phasebreak/Materials/EnemyTelegraph.mat", new Color(1f, 0.12f, 0.04f));
            SetupEnemyCore(player, followCamera, enemyMaterial, telegraphMaterial);
            SetupTargeting(player, followCamera);
            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("Phasebreak enemy core and camera toggle built successfully.");
        }

        private static void SetupJumpCourse(GameObject room, Material material)
        {
            Transform oldCourse = room.transform.Find("Jump Course");
            if (oldCourse != null)
                Object.DestroyImmediate(oldCourse.gameObject);

            GameObject course = new GameObject("Jump Course");
            course.transform.SetParent(room.transform);

            CreateBlock("Low Step", course.transform, new Vector3(-2.5f, 0.35f, 6.5f), new Vector3(3f, 0.7f, 3f), material);
            CreateBlock("Medium Platform", course.transform, new Vector3(-6f, 0.9f, 6.5f), new Vector3(3f, 1.8f, 3f), material);
            CreateBlock("High Platform", course.transform, new Vector3(-8.5f, 1.5f, 9.2f), new Vector3(3f, 3f, 3f), material);
            CreateBlock("Gap Platform A", course.transform, new Vector3(5.2f, 0.75f, 4.5f), new Vector3(2.5f, 1.5f, 2.5f), material);
            CreateBlock("Gap Platform B", course.transform, new Vector3(8.6f, 1.25f, 7.2f), new Vector3(2.5f, 2.5f, 2.5f), material);

            GameObject ramp = CreateBlock("Ramp", course.transform, new Vector3(3.5f, 0.55f, 8.7f), new Vector3(3.5f, 0.5f, 5f), material);
            ramp.transform.rotation = Quaternion.Euler(-13f, 0f, 0f);
        }

        private static void SetupCombat(GameObject player, PhasebreakFollowCamera followCamera, Material slashMaterial, Material dummyMaterial)
        {
            Transform oldSlash = player.transform.Find("Slash Arc");
            if (oldSlash != null)
                Object.DestroyImmediate(oldSlash.gameObject);

            GameObject oldTargets = GameObject.Find("Target Dummies");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);

            PlayerCombat combat = player.GetComponent<PlayerCombat>() ?? player.AddComponent<PlayerCombat>();
            GameObject slash = new GameObject("Slash Arc");
            slash.transform.SetParent(player.transform, false);
            slash.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            for (int i = 0; i < 7; i++)
            {
                float angle = -60f + i * 20f;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"Arc Segment {i + 1}";
                segment.transform.SetParent(slash.transform, false);
                segment.transform.localPosition = direction * 1.45f;
                segment.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
                segment.transform.localScale = new Vector3(0.62f, 0.075f, 0.2f);
                segment.GetComponent<Renderer>().sharedMaterial = slashMaterial;
                Object.DestroyImmediate(segment.GetComponent<Collider>());
            }
            combat.Configure(player.GetComponent<PhasebreakPlayerMovement>(), followCamera, slash.transform);

            GameObject targets = new GameObject("Target Dummies");
            CreateDummy("Front Dummy", targets.transform, new Vector3(0f, 1.1f, -3.75f), dummyMaterial);
            CreateDummy("Left Dummy", targets.transform, new Vector3(-3.4f, 1.1f, -4.3f), dummyMaterial);
            CreateDummy("Right Dummy", targets.transform, new Vector3(4.2f, 1.1f, -1.6f), dummyMaterial);
        }

        private static void SetupEnemyCore(GameObject player, PhasebreakFollowCamera followCamera, Material enemyMaterial, Material telegraphMaterial)
        {
            GameObject oldTargets = GameObject.Find("Target Dummies");
            if (oldTargets != null)
                Object.DestroyImmediate(oldTargets);
            GameObject oldEnemies = GameObject.Find("Combat Enemies");
            if (oldEnemies != null)
                Object.DestroyImmediate(oldEnemies);
            GameObject oldArena = GameObject.Find("Combat Arena Controller");
            if (oldArena != null)
                Object.DestroyImmediate(oldArena);

            PlayerHealth health = player.GetComponent<PlayerHealth>() ?? player.AddComponent<PlayerHealth>();
            followCamera.SetTarget(player.transform);

            GameObject enemyRoot = new GameObject("Combat Enemies");
            MeleeEnemy[] enemies =
            {
                CreateEnemy("Enemy Vanguard", enemyRoot.transform, new Vector3(0f, 1f, -1.2f), player.transform, enemyMaterial, telegraphMaterial),
                CreateEnemy("Enemy Left", enemyRoot.transform, new Vector3(-5.5f, 1f, 1.5f), player.transform, enemyMaterial, telegraphMaterial),
                CreateEnemy("Enemy Right", enemyRoot.transform, new Vector3(6.5f, 1f, 5.5f), player.transform, enemyMaterial, telegraphMaterial)
            };

            GameObject arenaObject = new GameObject("Combat Arena Controller");
            CombatArenaReset resetter = arenaObject.AddComponent<CombatArenaReset>();
            resetter.Configure(player.transform, health, enemies);
        }

        private static MeleeEnemy CreateEnemy(string name, Transform parent, Vector3 position, Transform player,
            Material enemyMaterial, Material telegraphMaterial)
        {
            GameObject enemyObject = new GameObject(name);
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = position;

            CharacterController controller = enemyObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;
            controller.skinWidth = 0.06f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemyObject.transform, false);
            body.GetComponent<Renderer>().sharedMaterial = enemyMaterial;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.name = "Blade";
            blade.transform.SetParent(enemyObject.transform, false);
            blade.transform.localPosition = new Vector3(0.62f, 0.15f, 0.25f);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            blade.transform.localScale = new Vector3(0.12f, 1.15f, 0.12f);
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
            Targetable targetable = enemyObject.AddComponent<Targetable>();
            targetable.Configure(name, TargetFaction.Hostile);
            return enemy;
        }

        private static void SetupTargeting(GameObject player, PhasebreakFollowCamera followCamera)
        {
            Targetable playerTarget = player.GetComponent<Targetable>() ?? player.AddComponent<Targetable>();
            playerTarget.Configure("Player", TargetFaction.Player);
            PlayerTargeting targeting = player.GetComponent<PlayerTargeting>() ?? player.AddComponent<PlayerTargeting>();
            PlayerProgression progression = player.GetComponent<PlayerProgression>() ??
                                            player.AddComponent<PlayerProgression>();
            PlayerBuildSystem build = player.GetComponent<PlayerBuildSystem>() ??
                                      player.AddComponent<PlayerBuildSystem>();
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            Camera worldCamera = followCamera.GetComponent<Camera>();
            SerializedObject targetingSettings = new SerializedObject(targeting);
            targetingSettings.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            targetingSettings.FindProperty("followCamera").objectReferenceValue = followCamera;
            targetingSettings.ApplyModifiedPropertiesWithoutUndo();
            if (combat != null)
            {
                SerializedObject combatSettings = new SerializedObject(combat);
                combatSettings.FindProperty("targeting").objectReferenceValue = targeting;
                combatSettings.FindProperty("progression").objectReferenceValue = progression;
                combatSettings.FindProperty("build").objectReferenceValue = build;
                combatSettings.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject hudObject = GameObject.Find("Phasebreak HUD");
            if (hudObject == null)
                hudObject = new GameObject("Phasebreak HUD");
            PhasebreakHud hud = hudObject.GetComponent<PhasebreakHud>() ?? hudObject.AddComponent<PhasebreakHud>();
            SerializedObject hudSettings = new SerializedObject(hud);
            hudSettings.FindProperty("targeting").objectReferenceValue = targeting;
            hudSettings.FindProperty("player").objectReferenceValue = playerTarget;
            hudSettings.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            hudSettings.FindProperty("combat").objectReferenceValue = combat;
            hudSettings.FindProperty("progression").objectReferenceValue = progression;
            hudSettings.FindProperty("build").objectReferenceValue = build;
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDummy(string name, Transform parent, Vector3 position, Material material)
        {
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = name;
            dummy.transform.SetParent(parent);
            dummy.transform.position = position;
            dummy.transform.localScale = new Vector3(0.85f, 1.1f, 0.85f);
            dummy.GetComponent<Renderer>().sharedMaterial = material;
            dummy.AddComponent<TargetDummy>();

            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Hit Marker";
            marker.transform.SetParent(dummy.transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.25f, -0.51f);
            marker.transform.localScale = new Vector3(0.32f, 0.18f, 0.12f);
            marker.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(marker.GetComponent<Collider>());
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
