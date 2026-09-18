#if UNITY_EDITOR
using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class BuildMedievalStarterRealm
    {
        private const string ScenePath = "Assets/dashlash.unity";
        private const string KitRoot = "Assets/Phasebreak/Art/World/MedievalVillageMegaKit";
        private const string ModelRoot = KitRoot + "/FBX";
        private const string TextureRoot = KitRoot + "/Textures";
        private const string GeneratedRoot = KitRoot + "/Generated";
        private const string WorldRootName = "Medieval Starter Realm";
        private const string ZombiePrefabPath = "Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab";
        private const float WorldSize = 420f;

        private static readonly Vector2[] DistrictCenters =
        {
            new(-58f, 76f), new(-104f, -48f), new(105f, 55f)
        };

        private static readonly Vector3[] ZombieSpawns =
        {
            new(-126f, 0f, 104f), new(-116f, 0f, 96f), new(-105f, 0f, 112f),
            new(126f, 0f, -92f), new(116f, 0f, -101f), new(105f, 0f, -88f),
            new(-87f, 0f, -109f), new(-76f, 0f, -102f), new(-69f, 0f, -116f),
            new(78f, 0f, -104f), new(88f, 0f, -113f), new(96f, 0f, -98f),
            new(-142f, 0f, 8f), new(-134f, 0f, -3f),
            new(145f, 0f, 12f), new(136f, 0f, 23f)
        };

        [MenuItem("Phasebreak/Build Medieval Starter Realm")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
                throw new System.InvalidOperationException("Stop Play Mode before rebuilding the starter realm.");

            EnsureFolder(GeneratedRoot);
            Dictionary<string, Material> materials = BuildMaterials();
            GameObject plasterHouse = BuildHousePrefab("Plaster House", false, materials);
            GameObject brickHouse = BuildHousePrefab("Brick House", true, materials);
            GameObject pine = BuildPinePrefab(materials);
            GameObject rock = BuildRockPrefab(materials);
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject oldWorld = GameObject.Find(WorldRootName);
            if (oldWorld != null)
                Object.DestroyImmediate(oldWorld);
            DisablePrototypeBoundaries();

            GameObject world = new(WorldRootName);
            CreateTerrain(world.transform, materials["Realm Ground"]);
            CreateRoadNetwork(world.transform, materials["Village Road"]);
            CreateDistrict(world.transform, "Northgate Village", DistrictCenters[0], plasterHouse, brickHouse,
                new[] { 12f, -18f, 28f, -35f, 8f, 42f });
            CreateDistrict(world.transform, "Westmere Hamlet", DistrictCenters[1], plasterHouse, brickHouse,
                new[] { -12f, 31f, -40f, 18f, 48f });
            CreateDistrict(world.transform, "Eastwatch Hamlet", DistrictCenters[2], plasterHouse, brickHouse,
                new[] { 22f, -28f, 40f, -5f, -44f });
            CreateRuins(world.transform, materials);
            CreateLandmarks(world.transform, materials);
            ScatterNature(world.transform, pine, rock);
            InstallZombiePopulation(world.transform);
            ConfigureAtmosphere();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_REALM_BUILT: 420m medieval starter realm, three settlements, ruins and sixteen zombie spawns installed.");
        }

        private static Dictionary<string, Material> BuildMaterials()
        {
            Dictionary<string, Material> result = new();
            result["MI_Brick"] = CreateMaterial("MI_Brick", "T_Brick_BaseColor.png", new Color(.72f, .65f, .55f), .26f, "Normals Godot-Unity/T_Brick_Normal.png");
            result["MI_RedBrick"] = CreateMaterial("MI_RedBrick", "T_RedBrick_BaseColor.png", Color.white, .24f);
            result["MI_Plaster"] = CreateMaterial("MI_Plaster", "T_Plaster_BaseColor.png", new Color(.92f, .88f, .75f), .18f, "Normals Godot-Unity/T_Plaster_Normal.png");
            result["MI_RockTrim"] = CreateMaterial("MI_RockTrim", "T_RockTrim_BaseColor.png", Color.white, .2f, "Normals Godot-Unity/T_RockTrim_Normal.png");
            result["MI_RoundTiles"] = CreateMaterial("MI_RoundTiles", "T_RoundTiles_BaseColor.png", new Color(.72f, .48f, .34f), .2f, "Normals Godot-Unity/T_RoundTiles_Normal.png");
            result["MI_UnevenBrick"] = CreateMaterial("MI_UnevenBrick", "T_UnevenBrick_BaseColor.png", Color.white, .18f, "Normals Godot-Unity/T_UnevenBrick_Normal.png");
            result["MI_WoodTrim"] = CreateMaterial("MI_WoodTrim", "T_WoodTrim_BaseColor.png", new Color(.64f, .51f, .38f), .22f, "Normals Godot-Unity/T_WoodTrim_Normal.png");
            result["MI_WoodTrim_Wear"] = result["MI_WoodTrim"];
            result["MI_MetalOrnaments"] = CreateSolidMaterial("MI_MetalOrnaments", new Color(.17f, .19f, .2f), .55f, .65f);
            result["MI_WindowGlass"] = CreateMaterial("MI_WindowGlass", "T_WindowGradient.png", new Color(.5f, .78f, .9f), .72f);
            result["MI_Vine"] = CreateMaterial("MI_Vine", "T_VineLeaf.png", new Color(.58f, .75f, .42f), .08f);
            result["Realm Ground"] = CreateMaterial("Realm Ground", "T_Noise_Terrain.png", new Color(.31f, .42f, .23f), .08f);
            result["Village Road"] = CreateMaterial("Village Road", "T_UnevenBrick_BaseColor.png", new Color(.54f, .48f, .39f), .12f, "Normals Godot-Unity/T_UnevenBrick_Normal.png");
            result["Pine Bark"] = CreateSolidMaterial("Pine Bark", new Color(.19f, .12f, .075f), .12f, 0f);
            result["Pine Needles"] = CreateSolidMaterial("Pine Needles", new Color(.075f, .19f, .13f), .06f, 0f);
            result["Field Stone"] = CreateSolidMaterial("Field Stone", new Color(.29f, .32f, .31f), .18f, 0f);
            result["Realm Ground"].mainTextureScale = new Vector2(42f, 42f);
            result["Village Road"].mainTextureScale = new Vector2(2f, 8f);
            return result;
        }

        private static Material CreateMaterial(string name, string textureFile, Color tint, float smoothness,
            string normalFile = null)
        {
            string path = $"{GeneratedRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureRoot}/{textureFile}");
            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            if (!string.IsNullOrEmpty(normalFile))
            {
                string normalPath = $"{TextureRoot}/{normalFile}";
                TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateSolidMaterial(string name, Color color, float smoothness, float metallic)
        {
            string path = $"{GeneratedRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildHousePrefab(string name, bool brick, Dictionary<string, Material> materials)
        {
            string path = $"{GeneratedRoot}/{name}.prefab";
            GameObject root = new(name);
            try
            {
                string straight = brick ? "Wall_UnevenBrick_Straight.fbx" : "Wall_Plaster_Straight.fbx";
                string window = brick ? "Wall_UnevenBrick_Window_Wide_Round.fbx" : "Wall_Plaster_Window_Wide_Round.fbx";
                string door = brick ? "Wall_UnevenBrick_Door_Round.fbx" : "Wall_Plaster_Door_Round.fbx";
                for (int x = -4; x <= 4; x += 2)
                {
                    AddModel(x == 0 ? door : window, root.transform, new Vector3(x, 0f, -7f), 0f, materials, true);
                    AddModel(x % 4 == 0 ? window : straight, root.transform, new Vector3(-x, 0f, 7f), 180f, materials, true);
                }
                for (int z = -6; z <= 6; z += 2)
                {
                    AddModel(z % 4 == 0 ? window : straight, root.transform, new Vector3(-5f, 0f, z), 90f, materials, true);
                    AddModel(z % 4 == 0 ? window : straight, root.transform, new Vector3(5f, 0f, -z), -90f, materials, true);
                }
                AddModel("Roof_RoundTiles_8x14.fbx", root.transform, new Vector3(0f, 3.9f, 0f), 0f, materials, false);
                AddModel("Prop_Chimney.fbx", root.transform, new Vector3(2.8f, 6.2f, 1.5f), 0f, materials, false);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildPinePrefab(Dictionary<string, Material> materials)
        {
            string path = $"{GeneratedRoot}/Realm Pine.prefab";
            GameObject root = new("Realm Pine");
            try
            {
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(root.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 1.65f, 0f);
                trunk.transform.localScale = new Vector3(.32f, 1.65f, .32f);
                trunk.GetComponent<Renderer>().sharedMaterial = materials["Pine Bark"];
                for (int i = 0; i < 3; i++)
                {
                    GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    crown.name = $"Needles {i + 1}";
                    crown.transform.SetParent(root.transform, false);
                    crown.transform.localPosition = new Vector3(0f, 3.1f + i * 1.15f, 0f);
                    float width = 2.35f - i * .48f;
                    crown.transform.localScale = new Vector3(width, 1.45f, width);
                    crown.GetComponent<Renderer>().sharedMaterial = materials["Pine Needles"];
                    Object.DestroyImmediate(crown.GetComponent<Collider>());
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject BuildRockPrefab(Dictionary<string, Material> materials)
        {
            string path = $"{GeneratedRoot}/Realm Rock.prefab";
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "Realm Rock";
            root.transform.localScale = new Vector3(1.6f, .75f, 1.15f);
            root.GetComponent<Renderer>().sharedMaterial = materials["Field Stone"];
            try { return PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { Object.DestroyImmediate(root); }
        }

        private static void CreateTerrain(Transform parent, Material material)
        {
            const int resolution = 65;
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                float worldX = -WorldSize * .5f + WorldSize * x / (resolution - 1f);
                float worldZ = -WorldSize * .5f + WorldSize * z / (resolution - 1f);
                int index = z * resolution + x;
                vertices[index] = new Vector3(worldX, GroundHeight(worldX, worldZ), worldZ);
                uv[index] = new Vector2(x / (resolution - 1f), z / (resolution - 1f));
            }
            int t = 0;
            for (int z = 0; z < resolution - 1; z++)
            for (int x = 0; x < resolution - 1; x++)
            {
                int i = z * resolution + x;
                triangles[t++] = i; triangles[t++] = i + resolution; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + resolution; triangles[t++] = i + resolution + 1;
            }
            Mesh mesh = new() { name = "Medieval Starter Realm Terrain" };
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string meshPath = $"{GeneratedRoot}/Medieval Starter Realm Terrain.asset";
            AssetDatabase.DeleteAsset(meshPath);
            AssetDatabase.CreateAsset(mesh, meshPath);

            GameObject terrain = new("Realm Terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            terrain.transform.SetParent(parent, false);
            terrain.GetComponent<MeshFilter>().sharedMesh = mesh;
            terrain.GetComponent<MeshRenderer>().sharedMaterial = material;
            terrain.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static float GroundHeight(float x, float z)
        {
            float distance = new Vector2(x, z).magnitude;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(58f, 155f, distance));
            float rolling = (Mathf.PerlinNoise((x + 400f) * .012f, (z + 500f) * .012f) - .48f) * 8f;
            float height = -.08f + rolling * blend;
            foreach (Vector2 center in DistrictCenters)
            {
                float districtDistance = Vector2.Distance(new Vector2(x, z), center);
                height = Mathf.Lerp(-.02f, height, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 34f, districtDistance)));
            }
            return height;
        }

        private static void CreateRoadNetwork(Transform parent, Material material)
        {
            Transform roads = new GameObject("Road Network").transform;
            roads.SetParent(parent, false);
            CreateRoad(roads, Vector2.zero, DistrictCenters[0], 6f, material);
            CreateRoad(roads, Vector2.zero, DistrictCenters[1], 5f, material);
            CreateRoad(roads, Vector2.zero, DistrictCenters[2], 5f, material);
            CreateRoad(roads, DistrictCenters[1], new Vector2(-126f, 104f), 4f, material);
            CreateRoad(roads, DistrictCenters[2], new Vector2(126f, -92f), 4f, material);
        }

        private static void CreateRoad(Transform parent, Vector2 start, Vector2 end, float width, Material material)
        {
            float distance = Vector2.Distance(start, end);
            int segments = Mathf.CeilToInt(distance / 6f);
            for (int i = 0; i < segments; i++)
            {
                float a = (i + .5f) / segments;
                Vector2 point = Vector2.Lerp(start, end, a);
                Vector2 direction = (end - start).normalized;
                GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
                road.name = "Road Segment";
                road.transform.SetParent(parent, false);
                road.transform.position = new Vector3(point.x, GroundHeight(point.x, point.y) + .015f, point.y);
                road.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, 0f);
                road.transform.localScale = new Vector3(width, .035f, distance / segments + .3f);
                road.GetComponent<Renderer>().sharedMaterial = material;
                Object.DestroyImmediate(road.GetComponent<Collider>());
            }
        }

        private static void CreateDistrict(Transform parent, string name, Vector2 center, GameObject plasterHouse,
            GameObject brickHouse, float[] rotations)
        {
            Transform district = new GameObject(name).transform;
            district.SetParent(parent, false);
            Vector2[] offsets =
            {
                new(-19f, -12f), new(0f, -17f), new(20f, -10f), new(-20f, 12f), new(2f, 18f), new(21f, 13f)
            };
            for (int i = 0; i < rotations.Length; i++)
            {
                Vector2 p = center + offsets[i];
                GameObject prefab = i % 3 == 1 ? brickHouse : plasterHouse;
                GameObject house = PrefabUtility.InstantiatePrefab(prefab, district) as GameObject;
                house.name = $"{prefab.name} {i + 1:00}";
                house.transform.SetPositionAndRotation(new Vector3(p.x, GroundHeight(p.x, p.y), p.y),
                    Quaternion.Euler(0f, rotations[i], 0f));
            }
        }

        private static void CreateRuins(Transform parent, Dictionary<string, Material> materials)
        {
            CreateRuin(parent, "Northwest Ruins", new Vector2(-120f, 101f), 12f, materials);
            CreateRuin(parent, "Southeast Ruins", new Vector2(117f, -94f), -24f, materials);
            CreateRuin(parent, "Southern Watch Ruins", new Vector2(-78f, -108f), 38f, materials);
        }

        private static void CreateRuin(Transform parent, string name, Vector2 center, float yaw,
            Dictionary<string, Material> materials)
        {
            Transform ruin = new GameObject(name).transform;
            ruin.SetParent(parent, false);
            ruin.SetPositionAndRotation(new Vector3(center.x, GroundHeight(center.x, center.y), center.y), Quaternion.Euler(0f, yaw, 0f));
            AddModel("Wall_Arch.fbx", ruin, new Vector3(0f, 0f, -6f), 0f, materials, true);
            for (int i = -2; i <= 2; i++)
            {
                if (i != 0) AddModel("Wall_UnevenBrick_Straight.fbx", ruin, new Vector3(i * 2f, 0f, -6f), 0f, materials, true);
                AddModel("Wall_UnevenBrick_Straight.fbx", ruin, new Vector3(-6f, 0f, i * 2f), 90f, materials, true);
            }
            AddModel("Prop_Crate.fbx", ruin, new Vector3(2f, 0f, 1f), 31f, materials, true);
            AddModel("Prop_Wagon.fbx", ruin, new Vector3(-1f, 0f, 5f), -18f, materials, true);
        }

        private static void CreateLandmarks(Transform parent, Dictionary<string, Material> materials)
        {
            Transform landmarks = new GameObject("Landmarks and Boundaries").transform;
            landmarks.SetParent(parent, false);
            Vector2[] wagonPoints = { new(18f, 43f), new(-55f, -25f), new(61f, 26f), new(-122f, -61f) };
            for (int i = 0; i < wagonPoints.Length; i++)
            {
                Vector2 p = wagonPoints[i];
                AddModel("Prop_Wagon.fbx", landmarks, new Vector3(p.x, GroundHeight(p.x, p.y), p.y), i * 67f, materials, true);
            }
            for (int i = -3; i <= 3; i++)
            {
                AddModel("Prop_WoodenFence_Extension2.fbx", landmarks,
                    new Vector3(-34f + i * 4f, GroundHeight(-34f + i * 4f, 47f), 47f), 0f, materials, true);
            }
        }

        private static void ScatterNature(Transform parent, GameObject pine, GameObject rock)
        {
            Transform nature = new GameObject("Woodland and Field Stones").transform;
            nature.SetParent(parent, false);
            System.Random random = new(675420);
            int trees = 0;
            int attempts = 0;
            while (trees < 180 && attempts++ < 2200)
            {
                float x = Mathf.Lerp(-198f, 198f, (float)random.NextDouble());
                float z = Mathf.Lerp(-198f, 198f, (float)random.NextDouble());
                Vector2 point = new(x, z);
                if (point.magnitude < 48f || IsNearDistrict(point, 38f) || Mathf.Abs(x) > 195f || Mathf.Abs(z) > 195f)
                    continue;
                GameObject instance = PrefabUtility.InstantiatePrefab(pine, nature) as GameObject;
                instance.name = $"Realm Pine {trees + 1:000}";
                float scale = Mathf.Lerp(.72f, 1.45f, (float)random.NextDouble());
                instance.transform.SetPositionAndRotation(new Vector3(x, GroundHeight(x, z), z),
                    Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                instance.transform.localScale = Vector3.one * scale;
                trees++;
            }
            for (int i = 0; i < 48; i++)
            {
                float x = Mathf.Lerp(-190f, 190f, (float)random.NextDouble());
                float z = Mathf.Lerp(-190f, 190f, (float)random.NextDouble());
                if (new Vector2(x, z).magnitude < 35f) { i--; continue; }
                GameObject instance = PrefabUtility.InstantiatePrefab(rock, nature) as GameObject;
                instance.name = $"Field Stone {i + 1:00}";
                float scale = Mathf.Lerp(.45f, 1.5f, (float)random.NextDouble());
                instance.transform.SetPositionAndRotation(new Vector3(x, GroundHeight(x, z) + .15f, z),
                    Quaternion.Euler((float)random.NextDouble() * 18f, (float)random.NextDouble() * 360f, (float)random.NextDouble() * 12f));
                instance.transform.localScale = Vector3.one * scale;
            }
        }

        private static bool IsNearDistrict(Vector2 point, float radius)
        {
            foreach (Vector2 center in DistrictCenters)
                if (Vector2.Distance(point, center) < radius) return true;
            return false;
        }

        private static void ConfigureAtmosphere()
        {
            string path = $"{GeneratedRoot}/Realm Skybox.mat";
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                Shader shader = Shader.Find("Skybox/Procedural");
                if (shader != null)
                {
                    sky = new Material(shader) { name = "Realm Skybox" };
                    AssetDatabase.CreateAsset(sky, path);
                }
            }
            if (sky != null)
            {
                sky.SetColor("_SkyTint", new Color(.17f, .27f, .38f));
                sky.SetColor("_GroundColor", new Color(.12f, .13f, .12f));
                sky.SetFloat("_AtmosphereThickness", .85f);
                sky.SetFloat("_Exposure", .9f);
                RenderSettings.skybox = sky;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientIntensity = .9f;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.19f, .25f, .27f);
            RenderSettings.fogDensity = .0027f;
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                mainCamera.clearFlags = CameraClearFlags.Skybox;
            GameObject sunObject = GameObject.Find("Directional Light");
            if (sunObject != null && sunObject.TryGetComponent(out Light sun))
            {
                sun.intensity = 1.15f;
                sun.color = new Color(1f, .86f, .68f);
                sunObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            }
        }

        private static void InstallZombiePopulation(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            if (prefab == null)
                throw new MissingReferenceException($"Zombie prefab missing at {ZombiePrefabPath}.");
            GameObject existing = GameObject.Find("Zombie Population");
            if (existing != null)
                Object.DestroyImmediate(existing);

            Transform population = new GameObject("Zombie Population").transform;
            population.SetParent(parent, false);
            PlayerHealth playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
            List<MeleeEnemy> enemies = new();
            for (int i = 0; i < ZombieSpawns.Length; i++)
            {
                Vector3 spawn = ZombieSpawns[i];
                spawn.y = GroundHeight(spawn.x, spawn.z) + .15f;
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, population) as GameObject;
                instance.name = $"Risen Zombie {i + 1:00}";
                instance.transform.SetPositionAndRotation(spawn, Quaternion.Euler(0f, (i * 137f) % 360f, 0f));
                MeleeEnemy enemy = instance.GetComponent<MeleeEnemy>();
                enemy.Configure(playerHealth != null ? playerHealth.transform : null, instance.transform.Find("Attack Telegraph"));
                enemies.Add(enemy);
            }
            CombatArenaReset resetter = Object.FindAnyObjectByType<CombatArenaReset>();
            if (resetter != null && playerHealth != null)
                resetter.Configure(playerHealth.transform, playerHealth, enemies.ToArray());
        }

        private static GameObject AddModel(string fileName, Transform parent, Vector3 localPosition, float localYaw,
            Dictionary<string, Material> materials, bool addCollider)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelRoot}/{fileName}");
            if (source == null)
                throw new MissingReferenceException($"Missing medieval model {fileName}.");
            GameObject instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            instance.name = System.IO.Path.GetFileNameWithoutExtension(fileName);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, localYaw, 0f);
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            foreach (Material sourceMaterial in renderer.sharedMaterials)
                if (sourceMaterial != null && materials.TryGetValue(sourceMaterial.name, out Material replacement))
                {
                    Material[] assigned = renderer.sharedMaterials;
                    for (int i = 0; i < assigned.Length; i++)
                        if (assigned[i] != null && materials.TryGetValue(assigned[i].name, out Material mapped)) assigned[i] = mapped;
                    renderer.sharedMaterials = assigned;
                    break;
                }
            if (addCollider)
            foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null) continue;
                MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
            return instance;
        }

        private static void DisablePrototypeBoundaries()
        {
            GameObject room = GameObject.Find("Test Room");
            if (room == null) return;
            string[] boundaries =
            {
                "Floor", "Wall North", "Wall South", "Wall East", "Wall West",
                "Obstacles", "Jump Course", "Starting Zone Terrain"
            };
            foreach (string childName in boundaries)
            {
                Transform child = room.transform.Find(childName);
                if (child != null) child.gameObject.SetActive(false);
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
