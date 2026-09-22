#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Den.Tools;
using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Nodes.MatrixGenerators;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class BuildStarterZoneV2
    {
        private const string ScenePath = "Assets/Phasebreak/Scenes/StarterZone_V2.unity";
        private const string GraphPath = "Assets/Phasebreak/Data/StarterZoneV2/ShatteredFrontier.asset";
        private const string Kit = "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Generated/";
        private static readonly Vector2 Spawn = new Vector2(145, 155);
        private static readonly Vector2 Northgate = new Vector2(850, 870);
        private static readonly Vector2 Westmere = new Vector2(470, 985);
        private static readonly Vector2 Eastwatch = new Vector2(1390, 1010);
        private static readonly Vector2 Ruins = new Vector2(1020, 1420);
        private static readonly Vector2 Rift = new Vector2(1450, 480);

        [MenuItem("Phasebreak/Rebuild Complete Starter Zone V2")]
        public static void RebuildComplete()
        {
            Build();
            EditorApplication.update -= FinishCompleteRebuild;
            EditorApplication.update += FinishCompleteRebuild;
        }

        private static void FinishCompleteRebuild()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            { EditorApplication.update -= FinishCompleteRebuild; return; }
            MapMagicObject magic = UnityEngine.Object.FindAnyObjectByType<MapMagicObject>();
            if (magic == null || magic.IsGenerating()) return;
            foreach (var tile in magic.tiles.All()) if (!tile.Ready) return;
            EditorApplication.update -= FinishCompleteRebuild;
            try
            {
                FinishSurface();
                PolishOpening();
                UpgradeForest();
                PolishSettlements();
                PolishConnections();
                PolishForestBelts();
                ConfigureOpenWorldRecovery.Apply();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Debug.Log("STARTER_ZONE_V2_COMPLETE: full deterministic world rebuilt and saved");
            }
            catch (Exception error) { Debug.LogException(error); }
        }

        [MenuItem("Phasebreak/Build Starter Zone V2")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            EnsureFolder("Assets/Phasebreak/Scenes");
            EnsureFolder("Assets/Phasebreak/Data/StarterZoneV2");
            Scene source = EditorSceneManager.OpenScene("Assets/dashlash.unity", OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, ScenePath, true)) throw new Exception("Could not make V2 scene copy");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject oldWorld = GameObject.Find("Medieval Starter Realm");
            Dictionary<string, GameObject> npcTemplates = new Dictionary<string, GameObject>();
            if (oldWorld != null)
            {
                foreach (QuestNpc npc in oldWorld.GetComponentsInChildren<QuestNpc>(true))
                    npcTemplates[npc.name] = npc.gameObject;
            }
            GameObject world = new GameObject("The Shattered Frontier");
            Graph graph = CreateGraph();
            GameObject mapObject = new GameObject("MapMagic 2 - Shattered Frontier");
            mapObject.SetActive(false);
            mapObject.transform.SetParent(world.transform);
            MapMagicObject magic = mapObject.AddComponent<MapMagicObject>();
            magic.instantGenerate = false;
            magic.graph = graph;
            magic.tileSize = new Vector2D(1000, 1000);
            magic.tileResolution = MapMagicObject.Resolution._513;
            magic.draftResolution = MapMagicObject.Resolution._65;
            magic.globals.height = 320;
            magic.tiles.allowMove = false;
            magic.mainRange = 0;
            mapObject.SetActive(true);
            for (int x = 0; x < 2; x++) for (int z = 0; z < 2; z++)
                magic.tiles.Pin(new Coord(x, z), false, magic);

            Material road = Material("Frontier Road", new Color(.34f, .29f, .22f));
            Material stone = Textured("Frontier Stone",
                "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures/T_RockTrim_BaseColor.png",
                new Color(.69f, .68f, .63f));
            Material riftStone = Material("Rift Stone", new Color(.21f, .18f, .28f));
            Material glow = Material("Rift Glow", new Color(.33f, .16f, .65f), true);
            Material wood = Material("Frontier Timber", new Color(.28f, .19f, .11f));
            BuildRoads(world.transform, road);
            BuildLandmarks(world.transform, npcTemplates, stone, wood, riftStone, glow);
            BuildNature(world.transform);
            BuildEncounters(world.transform);
            if (oldWorld != null) UnityEngine.Object.DestroyImmediate(oldWorld);
            GameObject testRoom = GameObject.Find("Test Room");
            if (testRoom != null) testRoom.SetActive(false);
            GameObject arena = GameObject.Find("Combat Arena Controller");
            if (arena != null) arena.SetActive(false);
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.SetPositionAndRotation(Point(Spawn.x, Spawn.y, 1.5f), Quaternion.Euler(0, 35, 0));
                var camera = Camera.main != null ? Camera.main.GetComponent<PhasebreakFollowCamera>() : null;
                if (camera != null) camera.SetTarget(player.transform);
            }
            GameObject crypt = GameObject.Find("Rift Crypt");
            if (crypt != null)
            {
                Transform entrance = crypt.transform.Find("Rift Crypt Entrance");
                crypt.transform.position = new Vector3(2500, 0, 1000);
                if (entrance != null) entrance.position = Point(Rift.x, Rift.y, 1.2f);
            }
            magic.instantGenerate = true;
            magic.Refresh(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("STARTER_ZONE_V2_BUILT: four MapMagic tiles, 2 km square, eight world encounters plus practice lane, fixed seed 271828");
        }

        [MenuItem("Phasebreak/Finish Starter Zone V2 Surface")]
        public static void FinishSurface()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            string textureRoot = "Assets/MapMagic/Demo/LandTextures/";
            TerrainLayer grass = Layer("Frontier Grass", textureRoot + "GrassGreen.tif", new Color(.80f, .86f, .72f));
            TerrainLayer earth = Layer("Frontier Earth", textureRoot + "Dirt.tif", new Color(.86f, .79f, .67f));
            TerrainLayer rock = Layer("Frontier Rock", textureRoot + "CliffDark.tif", new Color(.78f, .79f, .80f));
            GameObject routeObject = GameObject.Find("Old King's Road and trails");
            if (routeObject != null)
            {
                foreach (MeshRenderer renderer in routeObject.GetComponentsInChildren<MeshRenderer>(true))
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            }
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;
                data.terrainLayers = new[] { grass, earth, rock };
                data.alphamapResolution = 512;
                float[,,] weights = new float[512, 512, 3];
                Vector3 origin = terrain.transform.position;
                for (int z = 0; z < 512; z++) for (int x = 0; x < 512; x++)
                {
                    float wx = origin.x + x * data.size.x / 511f;
                    float wz = origin.z + z * data.size.z / 511f;
                    float slope = data.GetSteepness(x / 511f, z / 511f);
                    float rocky = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(23, 49, slope));
                    rocky = Mathf.Max(rocky, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(115, 220, FrontierTerrainNode.Height(wx, wz))) * .6f);
                    float rift = 1f - Mathf.SmoothStep(0, 1, Vector2.Distance(new Vector2(wx, wz), Rift) / 360f);
                    float dry = .17f + .17f * Mathf.PerlinNoise(wx / 140f + 12, wz / 140f + 18) + rift * .45f;
                    float road = RoadMask(wx, wz);
                    dry = Mathf.Max(dry, road * .91f);
                    rocky *= 1 - road * .9f;
                    float earthWeight = Mathf.Clamp01(dry * (1 - rocky));
                    weights[z, x, 2] = rocky;
                    weights[z, x, 1] = earthWeight;
                    weights[z, x, 0] = 1 - rocky - earthWeight;
                }
                data.SetAlphamaps(0, 0, weights);
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = 8;
                terrain.basemapDistance = 600;
                EditorUtility.SetDirty(data);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.54f, .62f, .65f);
            RenderSettings.fogStartDistance = 440;
            RenderSettings.fogEndDistance = 1600;
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/MapMagic/Demo/Materials/SkyCloudyEvening.mat");
            Camera viewCamera = Camera.main;
            if (viewCamera != null) viewCamera.farClipPlane = 1800;
            MapMagicObject generated = UnityEngine.Object.FindAnyObjectByType<MapMagicObject>();
            if (generated != null)
            {
                generated.tiles.generateRange = 0;
                generated.enabled = false;
                EditorUtility.SetDirty(generated);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("STARTER_ZONE_V2_SURFACE_FINISHED");
        }

        private static float RoadMask(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float opening = PolylineDistance(p, new Vector2(145, 155), new Vector2(194, 210),
                new Vector2(232, 292), new Vector2(312, 355), new Vector2(380, 435), new Vector2(435, 522));
            float kings = PolylineDistance(p, new Vector2(435, 522), new Vector2(512, 590), new Vector2(630, 670),
                new Vector2(742, 790), Northgate, new Vector2(960, 950), new Vector2(1110, 1040),
                new Vector2(1240, 1150), new Vector2(1430, 1280), new Vector2(1640, 1450), new Vector2(1820, 1840));
            float branches = Mathf.Min(
                PolylineDistance(p, new Vector2(650, 690), new Vector2(565, 770), Westmere, new Vector2(350, 1110), new Vector2(250, 1270)),
                PolylineDistance(p, Northgate, new Vector2(1040, 830), new Vector2(1220, 910), Eastwatch, new Vector2(1590, 1040), new Vector2(1880, 1160)));
            branches = Mathf.Min(branches, PolylineDistance(p, Northgate, new Vector2(910, 1050), new Vector2(940, 1220), Ruins));
            branches = Mathf.Min(branches, PolylineDistance(p, new Vector2(1040, 830), new Vector2(1180, 710),
                new Vector2(1320, 590), Rift, new Vector2(1690, 350)));
            return Mathf.Max(1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.5f, 5f, opening)),
                Mathf.Max(1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4f, 9f, kings)),
                    1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(3f, 7f, branches))));
        }

        private static float PolylineDistance(Vector2 p, params Vector2[] points)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                float t = Mathf.Clamp01(Vector2.Dot(p - points[i], delta) / delta.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, points[i] + delta * t));
            }
            return best;
        }

        [MenuItem("Phasebreak/Polish Starter Zone V2 Opening")]
        public static void PolishOpening()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            GameObject root = GameObject.Find("The Shattered Frontier");
            Transform old = root.transform.Find("Opening woodland");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform grove = Container(root.transform, "Opening woodland");
            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Realm Pine.prefab");
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Realm Rock.prefab");
            System.Random random = new System.Random(271828);
            for (int i = 0; i < 160; i++)
            {
                float x = 80 + (float)random.NextDouble() * 390;
                float z = 100 + (float)random.NextDouble() * 480;
                if (RoadMask(x, z) > .03f || Vector2.Distance(new Vector2(x, z), Spawn) < 16) continue;
                GameObject prefab = i % 9 == 0 ? rock : pine;
                GameObject item = Place(prefab, grove, prefab == rock ? "Trail boulder" : "Trail pine", new Vector2(x, z), i * 47);
                if (item != null) item.transform.localScale *= .8f + (float)random.NextDouble() * .45f;
            }
            Vector2[] trail = { Spawn, new Vector2(194, 210), new Vector2(232, 292),
                new Vector2(312, 355), new Vector2(380, 435), new Vector2(435, 522) };
            for (int i = 0; i < trail.Length - 1; i++)
            {
                Vector2 direction = (trail[i + 1] - trail[i]).normalized;
                Vector2 side = new Vector2(-direction.y, direction.x);
                int segments = Mathf.CeilToInt(Vector2.Distance(trail[i], trail[i + 1]) / 20);
                for (int j = 0; j < segments; j++)
                {
                    Vector2 center = Vector2.Lerp(trail[i], trail[i + 1], (j + .5f) / segments);
                    foreach (int sign in new[] { -1, 1 })
                    {
                        Vector2 p = center + side * sign * (10 + (i + j) % 3 * 5);
                        Place(pine, grove, "Framing pine", p, (i * 47 + j * 81 + sign * 37) % 360);
                    }
                }
            }
            ScaleForest();
            var camera = Camera.main != null ? Camera.main.GetComponent<PhasebreakFollowCamera>() : null;
            if (camera != null)
            {
                SerializedObject so = new SerializedObject(camera);
                so.FindProperty("pitch").floatValue = 18f;
                so.FindProperty("yaw").floatValue = 35f;
                so.FindProperty("distance").floatValue = 8.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void ScaleForest()
        {
            GameObject forest = GameObject.Find("Forest and stone clusters");
            GameObject opening = GameObject.Find("Opening woodland");
            foreach (GameObject root in new[] { forest, opening })
            {
                if (root == null) continue;
                foreach (Transform child in root.transform)
                {
                    Renderer renderer = child.GetComponentInChildren<Renderer>();
                    if (renderer == null) continue;
                    if (child.name.Contains("pine") && renderer.bounds.size.y < 6) child.localScale *= 3.8f;
                    else if ((child.name.Contains("stone") || child.name.Contains("boulder")) && renderer.bounds.size.x < 3)
                        child.localScale *= 2.3f;
                }
            }
        }

        [MenuItem("Phasebreak/Upgrade Starter Zone V2 Forest")]
        public static void UpgradeForest()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            string rootPath = "Assets/MapMagic/Demo/Trees/Pine/Prefabs/";
            GameObject[] variants = {
                AssetDatabase.LoadAssetAtPath<GameObject>(rootPath + "PineBig1.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(rootPath + "PineMed2.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(rootPath + "PineSmall1.prefab")
            };
            Material foliage = Material("Frontier Pine Foliage", Color.white);
            foliage.shader = Shader.Find("Universal Render Pipeline/Unlit");
            foliage.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/MapMagic/Demo/Trees/Pine/Textures/Pine.tif"));
            foliage.SetFloat("_AlphaClip", 1);
            foliage.SetFloat("_Cutoff", .32f);
            foliage.EnableKeyword("_ALPHATEST_ON");
            foliage.SetFloat("_Cull", 0);
            EditorUtility.SetDirty(foliage);
            foreach (string group in new[] { "Forest and stone clusters", "Opening woodland" })
            {
                GameObject grove = GameObject.Find(group);
                if (grove == null) continue;
                List<Transform> originals = new List<Transform>();
                foreach (Transform child in grove.transform)
                    if (child.name.Contains("pine")) originals.Add(child);
                int index = 0;
                foreach (Transform old in originals)
                {
                    Renderer oldRenderer = old.GetComponentInChildren<Renderer>();
                    float height = oldRenderer != null ? oldRenderer.bounds.size.y : 12;
                    GameObject prefab = variants[index % variants.Length];
                    GameObject tree = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    tree.name = "Frontier Pine";
                    tree.transform.SetParent(grove.transform);
                    tree.transform.SetPositionAndRotation(old.position, old.rotation);
                    Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();
                    float sourceHeight = renderers.Length > 0 ? renderers[0].bounds.size.y : 30;
                    tree.transform.localScale = Vector3.one * Mathf.Clamp(height / Mathf.Max(1, sourceHeight), .18f, .72f);
                    foreach (Renderer renderer in renderers) renderer.sharedMaterial = foliage;
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                    index++;
                }
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Phasebreak/Polish Starter Zone V2 Settlements")]
        public static void PolishSettlements()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            string textureRoot = "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures/";
            Material plaster = Textured("Frontier Plaster", textureRoot + "T_Plaster_BaseColor.png", new Color(.87f, .82f, .69f));
            Material brick = Textured("Frontier Brick", textureRoot + "T_Brick_BaseColor.png", new Color(.75f, .68f, .55f));
            Material roof = Textured("Frontier Tile Roof", textureRoot + "T_RoundTiles_BaseColor.png", new Color(.67f, .38f, .29f));
            Material timber = Textured("Frontier Dark Timber", textureRoot + "T_WoodTrim_BaseColor.png", new Color(.41f, .30f, .19f));
            Material window = Material("Frontier Window", new Color(.13f, .20f, .23f));
            foreach (string group in new[] { "Northgate - frontier settlement", "Westmere wetland", "Eastwatch ridge" })
            {
                GameObject settlement = GameObject.Find(group);
                if (settlement == null) continue;
                List<Transform> houses = new List<Transform>();
                foreach (Transform child in settlement.transform)
                    if (child.name.Contains("House") || child.name.Contains("home") || child.name.Contains("outpost") || child.name.Contains("cottage")) houses.Add(child);
                int i = 0;
                foreach (Transform old in houses)
                {
                    Vector3 position = old.position;
                    Quaternion rotation = old.rotation;
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                    GameObject cottage = new GameObject(i % 4 == 0 ? "Stone frontier cottage" : "Plaster frontier cottage");
                    cottage.transform.SetParent(settlement.transform);
                    cottage.transform.SetPositionAndRotation(Point(position.x, position.z, 0), rotation);
                    Cottage(cottage.transform, i % 4 == 0 ? brick : plaster, roof, timber, window);
                    i++;
                }
            }
            GameObject northgate = GameObject.Find("Northgate - frontier settlement");
            if (northgate != null)
            {
                Transform oldMarket = northgate.transform.Find("Market and training square");
                if (oldMarket != null) UnityEngine.Object.DestroyImmediate(oldMarket.gameObject);
                List<Transform> remove = new List<Transform>();
                foreach (Transform child in northgate.transform)
                    if (child.name == "Palisade" && child.position.z < 780 && Mathf.Abs(child.position.x - 850) < 40) remove.Add(child);
                foreach (Transform wall in remove) UnityEngine.Object.DestroyImmediate(wall.gameObject);
                Transform market = Container(northgate.transform, "Market and training square");
                for (int i = 0; i < 3; i++)
                {
                    Vector2 p = Northgate + new Vector2(20 + i * 12, 24);
                    Transform stall = Container(market, "Supply stall");
                    stall.position = Point(p.x, p.y, 0);
                    Part(stall, "Table", new Vector3(0, 1, 0), new Vector3(5, .3f, 2.5f), Vector3.zero, timber);
                    Part(stall, "Awning", new Vector3(0, 3.5f, 0), new Vector3(6, .25f, 3), Vector3.zero, roof);
                    for (int post = -1; post <= 1; post += 2)
                        Part(stall, "Post", new Vector3(post * 2.6f, 1.8f, 0), new Vector3(.24f, 3.5f, .24f), Vector3.zero, timber);
                }
                Transform training = Container(market, "Training yard");
                training.position = Point(Northgate.x - 44, Northgate.y + 17, 0);
                for (int i = 0; i < 3; i++)
                {
                    Part(training, "Training post", new Vector3(i * 5, 1.1f, 0), new Vector3(.45f, 2.2f, .45f), Vector3.zero, timber);
                    Part(training, "Straw target", new Vector3(i * 5, 2.1f, 0), new Vector3(1.3f, 1.1f, .35f), Vector3.zero, plaster);
                }
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        private static void Cottage(Transform root, Material wall, Material roof, Material wood, Material glass)
        {
            Part(root, "Stone foundation", new Vector3(0, .45f, 0), new Vector3(9.2f, .9f, 8.2f), Vector3.zero, wood);
            Part(root, "Walls", new Vector3(0, 3.1f, 0), new Vector3(8.5f, 5.2f, 7.5f), Vector3.zero, wall);
            Part(root, "Roof west", new Vector3(-2.3f, 6.75f, 0), new Vector3(5.5f, .55f, 9.1f), new Vector3(0, 0, 31), roof);
            Part(root, "Roof east", new Vector3(2.3f, 6.75f, 0), new Vector3(5.5f, .55f, 9.1f), new Vector3(0, 0, -31), roof);
            Part(root, "Door", new Vector3(0, 1.55f, -3.82f), new Vector3(1.75f, 3.1f, .18f), Vector3.zero, wood);
            Part(root, "Window", new Vector3(-2.7f, 3.4f, -3.82f), new Vector3(1.35f, 1.2f, .16f), Vector3.zero, glass);
            Part(root, "Window", new Vector3(2.7f, 3.4f, -3.82f), new Vector3(1.35f, 1.2f, .16f), Vector3.zero, glass);
        }

        private static GameObject Part(Transform parent, string name, Vector3 localPosition, Vector3 localScale,
            Vector3 localEuler, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private static Material Textured(string name, string path, Color color)
        {
            Material material = Material(name, color);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            EditorUtility.SetDirty(material);
            return material;
        }

        [MenuItem("Phasebreak/Polish Starter Zone V2 Connections")]
        public static void PolishConnections()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            GameObject world = GameObject.Find("The Shattered Frontier");
            Transform old = world.transform.Find("Passes and future connections");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform root = Container(world.transform, "Passes and future connections");
            Material stone = Textured("Frontier Stone",
                "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures/T_RockTrim_BaseColor.png",
                new Color(.69f, .68f, .63f));
            Material timber = Material("Frontier Timber", new Color(.28f, .19f, .11f));
            Arch(root, "First vista - broken frontier gate", new Vector2(424, 510), stone, 13, 7);
            Arch(root, "Old Mine - future dungeon entrance", new Vector2(1590, 680), stone, 12, 9);
            Arch(root, "Northern Pass - future region", new Vector2(1830, 1840), stone, 18, 11);
            Arch(root, "Eastern Road - future region", new Vector2(1880, 1160), timber, 15, 8);
            Arch(root, "Southern Route - future region", new Vector2(850, 60), timber, 13, 7);
            Arch(root, "Rift Route - future region", new Vector2(1690, 350), stone, 14, 9);
            Location(root, "old-mine", "Old Mine", new Vector2(1590, 680));
            Location(root, "northern-pass", "Northern Pass", new Vector2(1830, 1840));
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void Arch(Transform parent, string name, Vector2 at, Material material, float width, float height)
        {
            Transform arch = Container(parent, name);
            arch.position = Point(at.x, at.y, 0);
            Part(arch, "Left pier", new Vector3(-width * .5f, height * .5f, 0), new Vector3(2.2f, height, 2.4f), Vector3.zero, material);
            Part(arch, "Right pier", new Vector3(width * .5f, height * .5f, 0), new Vector3(2.2f, height, 2.4f), Vector3.zero, material);
            Part(arch, "Weathered lintel", new Vector3(0, height, 0), new Vector3(width + 4, 1.5f, 2.8f), Vector3.zero, material);
        }

        [MenuItem("Phasebreak/Polish Starter Zone V2 Forest Belts")]
        public static void PolishForestBelts()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) throw new Exception("Open StarterZone_V2 first");
            GameObject world = GameObject.Find("The Shattered Frontier");
            Transform old = world.transform.Find("Frontier forest belts");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform root = Container(world.transform, "Frontier forest belts");
            string pineRoot = "Assets/MapMagic/Demo/Trees/Pine/Prefabs/";
            GameObject[] variants = {
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineBig1.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineMed2.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineSmall1.prefab")
            };
            Material foliage = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Phasebreak/Data/StarterZoneV2/Frontier Pine Foliage.mat");
            Vector3[] belts = {
                new Vector3(570, 105, 850), new Vector3(690, 110, 1040),
                new Vector3(1080, 125, 1350), new Vector3(1270, 140, 1400),
                new Vector3(1560, 125, 1190), new Vector3(1740, 130, 850)
            };
            System.Random random = new System.Random(FrontierTerrainNode.PHASEBREAK_STARTER_V2_SEED + 71);
            foreach (Vector3 belt in belts)
            {
                Transform patch = Container(root, "Woodland patch");
                for (int i = 0; i < 85; i++)
                {
                    float a = (float)random.NextDouble() * Mathf.PI * 2;
                    float r = Mathf.Sqrt((float)random.NextDouble()) * belt.y;
                    float x = belt.x + Mathf.Cos(a) * r;
                    float z = belt.z + Mathf.Sin(a) * r;
                    Vector2 p = new Vector2(x, z);
                    if (RoadMask(x, z) > .03f || Vector2.Distance(p, Northgate) < 170 ||
                        Vector2.Distance(p, Westmere) < 90 || Vector2.Distance(p, Eastwatch) < 100 ||
                        Vector2.Distance(p, Ruins) < 100 || Vector2.Distance(p, Rift) < 130) continue;
                    GameObject prefab = variants[i % variants.Length];
                    GameObject tree = Place(prefab, patch, "Forest pine", p, (float)random.NextDouble() * 360);
                    if (tree == null) continue;
                    tree.transform.localScale = Vector3.one * (.22f + (float)random.NextDouble() * .28f);
                    foreach (Renderer renderer in tree.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = foliage;
                    tree.isStatic = true;
                }
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static TerrainLayer Layer(string name, string texturePath, Color tint)
        {
            string path = $"Assets/Phasebreak/Data/StarterZoneV2/{name}.terrainlayer";
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, path); }
            layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            layer.tileSize = new Vector2(24, 24);
            layer.diffuseRemapMax = new Vector4(tint.r, tint.g, tint.b, 1);
            layer.metallic = 0;
            layer.smoothness = .08f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Graph CreateGraph()
        {
            Graph graph = AssetDatabase.LoadAssetAtPath<Graph>(GraphPath);
            if (graph != null) AssetDatabase.DeleteAsset(GraphPath);
            graph = ScriptableObject.CreateInstance<Graph>();
            graph.name = "The Shattered Frontier - Terrain";
            FrontierTerrainNode shape = (FrontierTerrainNode)Generator.Create(typeof(FrontierTerrainNode));
            HeightOutput200 output = (HeightOutput200)Generator.Create(typeof(HeightOutput200));
            shape.guiPosition = new Vector2(60, 80);
            output.guiPosition = new Vector2(360, 80);
            graph.Add(shape);
            graph.Add(output);
            graph.Link(shape, output);
            graph.Add(new Group { name = "Macro landmass / ridge / valley / settlement terraces", guiPos = new Vector2(20, 20), guiSize = new Vector2(570, 230) });
            AddTextureFoundation(graph);
            AssetDatabase.CreateAsset(graph, GraphPath);
            EditorUtility.SetDirty(graph);
            return graph;
        }

        [MenuItem("Phasebreak/Upgrade Starter Zone V2 Graph")]
        public static void UpgradeGraph()
        {
            Graph graph = AssetDatabase.LoadAssetAtPath<Graph>(GraphPath);
            if (graph == null) throw new Exception("Build V2 first");
            AddTextureFoundation(graph);
            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
        }

        private static void AddTextureFoundation(Graph graph)
        {
            foreach (Generator generator in graph.generators)
                if (generator is TexturesOutput200) return;
            string root = "Assets/MapMagic/Demo/LandTextures/";
            TerrainLayer grass = Layer("Frontier Grass", root + "GrassGreen.tif", new Color(.80f, .86f, .72f));
            TerrainLayer earth = Layer("Frontier Earth", root + "Dirt.tif", new Color(.86f, .79f, .67f));
            Noise200 patches = (Noise200)Generator.Create(typeof(Noise200));
            patches.guiPosition = new Vector2(70, 380);
            patches.seed = FrontierTerrainNode.PHASEBREAK_STARTER_V2_SEED;
            patches.size = 260;
            patches.intensity = .48f;
            patches.detail = .52f;
            TexturesOutput200 textures = (TexturesOutput200)Generator.Create(typeof(TexturesOutput200));
            textures.guiPosition = new Vector2(360, 380);
            textures.layers = new[]
            {
                new TexturesOutput200.TextureLayer { name = "Grass base", prototype = grass },
                new TexturesOutput200.TextureLayer { name = "Dry earth patches", prototype = earth }
            };
            foreach (var layer in textures.layers)
            {
                layer.SetGen(textures);
                layer.Id = Den.Tools.Id.Generate();
            }
            graph.Add(patches);
            graph.Add(textures);
            graph.Link(patches, textures.layers[1]);
            graph.Add(new Group { name = "Ground texture foundation - natural dry earth patches", guiPos = new Vector2(20, 310), guiSize = new Vector2(570, 250) });
        }

        private static void BuildRoads(Transform parent, Material material)
        {
            Transform root = Container(parent, "Old King's Road and trails");
            Path(root, "Opening trail", material, 4,
                Spawn, new Vector2(194, 210), new Vector2(232, 292), new Vector2(312, 355), new Vector2(380, 435), new Vector2(435, 522));
            Path(root, "Old King's Road", material, 10,
                new Vector2(435, 522), new Vector2(512, 590), new Vector2(630, 670), new Vector2(742, 790), Northgate,
                new Vector2(960, 950), new Vector2(1110, 1040), new Vector2(1240, 1150), new Vector2(1430, 1280), new Vector2(1640, 1450), new Vector2(1820, 1840));
            Path(root, "Westmere road", material, 7, new Vector2(650, 690), new Vector2(565, 770), Westmere,
                new Vector2(350, 1110), new Vector2(250, 1270));
            Path(root, "Eastwatch road", material, 7, Northgate, new Vector2(1040, 830), new Vector2(1220, 910), Eastwatch,
                new Vector2(1590, 1040), new Vector2(1880, 1160));
            Path(root, "Ancient ruins track", material, 5, Northgate, new Vector2(910, 1050), new Vector2(940, 1220), Ruins);
            Path(root, "Rift crypt road", material, 6, new Vector2(1040, 830), new Vector2(1180, 710),
                new Vector2(1320, 590), Rift, new Vector2(1690, 350));
            Path(root, "Southern frontier", material, 5, new Vector2(435, 522), new Vector2(610, 400),
                new Vector2(775, 290), new Vector2(850, 40));
        }

        private static void Path(Transform parent, string name, Material material, float width, params Vector2[] points)
        {
            Transform route = Container(parent, name);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 a = points[i], b = points[i + 1];
                int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 12f);
                for (int j = 0; j < n; j++)
                {
                    Vector2 p = Vector2.Lerp(a, b, (j + .5f) / n);
                    Vector2 q0 = Vector2.Lerp(a, b, (float)j / n);
                    Vector2 q1 = Vector2.Lerp(a, b, (j + 1f) / n);
                    Vector3 start = Point(q0.x, q0.y, .05f), end = Point(q1.x, q1.y, .05f);
                    GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    slab.name = "Worn road";
                    slab.transform.SetParent(route);
                    slab.transform.position = (start + end) * .5f;
                    slab.transform.rotation = Quaternion.LookRotation(end - start);
                    slab.transform.localScale = new Vector3(width, .14f, Vector3.Distance(start, end) + .3f);
                    slab.GetComponent<Renderer>().sharedMaterial = material;
                    UnityEngine.Object.DestroyImmediate(slab.GetComponent<Collider>());
                }
            }
        }

        private static void BuildLandmarks(Transform parent, Dictionary<string, GameObject> npcs,
            Material stone, Material wood, Material dark, Material glow)
        {
            GameObject house = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Plaster House.prefab");
            GameObject brick = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Brick House.prefab");
            Transform town = Container(parent, "Northgate - frontier settlement");
            for (int i = 0; i < 13; i++)
            {
                float angle = i * 2.39996f;
                float r = i < 6 ? 44 : 81;
                Vector2 p = Northgate + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                Place(i % 4 == 0 ? brick : house, town, i % 4 == 0 ? "Brick House" : "Plaster House", p, angle * Mathf.Rad2Deg);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2 / 12;
                Vector2 p = Northgate + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 112;
                Block(town, "Palisade", Point(p.x, p.y, 2), new Vector3(48, 4, 2.2f), a * Mathf.Rad2Deg + 90, wood);
            }
            Watchtower(town, "Northgate west tower", new Vector2(753, 850), stone, wood);
            Watchtower(town, "Northgate east tower", new Vector2(938, 850), stone, wood);
            MoveNpc(npcs, town, "Warden Elira", Northgate + new Vector2(-18, 5));
            MoveNpc(npcs, town, "Quartermaster Orin", Northgate + new Vector2(18, 3));
            MoveNpc(npcs, town, "Seer Nara", Northgate + new Vector2(-5, 24));
            MoveNpc(npcs, town, "Guard Vas", Northgate + new Vector2(-5, -92));
            Location(town, "northgate", "Northgate", Northgate);

            Transform west = Container(parent, "Westmere wetland");
            for (int i = 0; i < 5; i++) Place(house, west, "Westmere home", Westmere + new Vector2((i % 3) * 23 - 25, (i / 3) * 27 - 25), i * 37);
            MoveNpc(npcs, west, "Guard Maren", Westmere + new Vector2(-12, -2));
            Location(west, "westmere", "Westmere", Westmere);
            Transform east = Container(parent, "Eastwatch ridge");
            Watchtower(east, "Eastwatch", Eastwatch, stone, wood);
            for (int i = 0; i < 3; i++) Place(brick, east, "Eastwatch outpost", Eastwatch + new Vector2(22 + i * 18, -25), i * 35);
            MoveNpc(npcs, east, "Scout Ilya", Eastwatch + new Vector2(-12, 0));
            Location(east, "eastwatch", "Eastwatch", Eastwatch);

            Transform ruins = Container(parent, "Ancient Ruins");
            for (int i = 0; i < 11; i++)
            {
                float a = i * 2.39996f;
                Vector2 p = Ruins + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (22 + i * 4);
                Block(ruins, "Broken ancient wall", Point(p.x, p.y, 2), new Vector3(11 + i % 3 * 4, 4 + i % 4, 2), a * Mathf.Rad2Deg, stone);
            }
            Location(ruins, "northwest-ruins", "Ancient Ruins", Ruins);
            Watchtower(parent, "Old Fort watchtower", new Vector2(1540, 1580), stone, wood);
            Transform rift = Container(parent, "Rift basin and crypt approach");
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Vector2 p = Rift + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 47;
                Block(rift, "Rift monolith", Point(p.x, p.y, 6), new Vector3(3, 12, 3), a * Mathf.Rad2Deg, dark);
            }
            Block(rift, "Distant rift light", Point(Rift.x, Rift.y, 12), new Vector3(4, 23, 4), 0, glow);
            Location(rift, "rift-crossroads", "Rift Crossroads", Rift);
        }

        private static void BuildNature(Transform parent)
        {
            GameObject pine = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Realm Pine.prefab");
            GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "Realm Rock.prefab");
            Transform forest = Container(parent, "Forest and stone clusters");
            System.Random random = new System.Random(FrontierTerrainNode.PHASEBREAK_STARTER_V2_SEED);
            for (int i = 0; i < 620; i++)
            {
                float x = 30 + (float)random.NextDouble() * 1940;
                float z = 30 + (float)random.NextDouble() * 1940;
                Vector2 p = new Vector2(x, z);
                if (Vector2.Distance(p, Northgate) < 170 || Vector2.Distance(p, Westmere) < 85 ||
                    Vector2.Distance(p, Eastwatch) < 95 || Vector2.Distance(p, Ruins) < 95 ||
                    Vector2.Distance(p, Rift) < 135 || Vector2.Distance(p, Spawn) < 22) continue;
                float n = Mathf.PerlinNoise(x / 220f, z / 220f);
                if (n < .38f) continue;
                GameObject prefab = i % 7 == 0 ? rock : pine;
                GameObject item = Place(prefab, forest, prefab == rock ? "Fieldstone" : "Frontier pine", p, (float)random.NextDouble() * 360);
                if (item != null) item.transform.localScale *= .75f + (float)random.NextDouble() * .65f;
            }
        }

        private static void BuildEncounters(Transform parent)
        {
            GameObject zombie = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab");
            Transform root = Container(parent, "Encounter ecology - authored population");
            GameObject zone = new GameObject("01 - Riftblade practice lane");
            zone.transform.SetParent(root);
            zone.transform.position = Point(310, 335, 0);
            FrontierEncounterZone practice = zone.AddComponent<FrontierEncounterZone>();
            practice.Configure("riftblade-practice", zombie, 12, 32, 180, 1);
            Vector3[] offsets = new Vector3[12];
            for (int step = 0; step < offsets.Length; step++)
                offsets[step] = new Vector3(Mathf.Sin(step * .7f) * 3f, 0f, step * 4.8f);
            practice.ConfigureFormation(offsets);
            StarterZonePopulationPass.Author(root);
        }

        private static void MoveNpc(Dictionary<string, GameObject> templates, Transform parent, string name, Vector2 pos)
        {
            if (!templates.TryGetValue(name, out GameObject source)) return;
            GameObject copy = UnityEngine.Object.Instantiate(source, parent);
            copy.name = name;
            copy.transform.position = Point(pos.x, pos.y, 0);
            copy.SetActive(true);
        }

        private static void Location(Transform parent, string id, string label, Vector2 p)
        {
            GameObject obj = new GameObject(label + " discovery");
            obj.transform.SetParent(parent);
            obj.transform.position = Point(p.x, p.y, 0);
            obj.AddComponent<QuestLocation>().Configure(id, label, 35);
        }

        private static void Watchtower(Transform parent, string name, Vector2 p, Material stone, Material wood)
        {
            Transform root = Container(parent, name);
            Block(root, "Masonry shaft", Point(p.x, p.y, 9), new Vector3(11, 18, 11), 0, stone);
            Block(root, "Timber watch gallery", Point(p.x, p.y, 19), new Vector3(15, 1.3f, 15), 0, wood);
            Block(root, "Roof left slope", Point(p.x - 3.1f, p.y, 23.2f), new Vector3(9, .7f, 16), 0, wood).transform.rotation = Quaternion.Euler(0, 0, 31);
            Block(root, "Roof right slope", Point(p.x + 3.1f, p.y, 23.2f), new Vector3(9, .7f, 16), 0, wood).transform.rotation = Quaternion.Euler(0, 0, -31);
            Block(root, "Recessed entrance", Point(p.x, p.y - 5.6f, 2.1f), new Vector3(2.4f, 4.2f, .25f), 0, wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Block(root, "Arrow slit", Point(p.x + side * 5.58f, p.y, 11), new Vector3(.15f, 2.8f, .55f), 0, wood);
                Block(root, "Watch brace", Point(p.x + side * 6.4f, p.y - 5, 17), new Vector3(.5f, 4, .5f), 0, wood);
                Block(root, "Watch brace", Point(p.x + side * 6.4f, p.y + 5, 17), new Vector3(.5f, 4, .5f), 0, wood);
            }
        }

        private static GameObject Place(GameObject prefab, Transform parent, string name, Vector2 p, float yaw)
        {
            if (prefab == null) return null;
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            obj.name = name; obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(Point(p.x, p.y, 0), Quaternion.Euler(0, yaw, 0));
            if (name == "Trail boulder" || name == "Fieldstone")
            {
                Material stone = Textured("Frontier Stone",
                    "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures/T_RockTrim_BaseColor.png",
                    new Color(.69f, .68f, .63f));
                foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = stone;
            }
            return obj;
        }

        private static GameObject Block(Transform parent, string name, Vector3 position, Vector3 scale, float yaw, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name; block.transform.SetParent(parent);
            block.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static Vector3 Point(float x, float z, float offset) => new Vector3(x, FrontierTerrainNode.Height(x, z) + offset, z);
        private static Transform Container(Transform parent, string name)
        { GameObject obj = new GameObject(name); obj.transform.SetParent(parent); return obj.transform; }

        private static Material Material(string name, Color color, bool emission = false)
        {
            string path = $"Assets/Phasebreak/Data/StarterZoneV2/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            if (emission) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * 2.5f); }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
#endif
