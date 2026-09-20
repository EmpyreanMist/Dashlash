#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    // Re-runnable scene dressing. Encounter zones, NPCs, terrain graph and gameplay owners are untouched.
    public static class StarterZoneWorldQualityPass
    {
        private const string ScenePath = "Assets/Phasebreak/Scenes/StarterZone_V2.unity";
        private const string Kit = "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/FBX/";
        private const string Data = "Assets/Phasebreak/Data/StarterZoneV2/";
        private static Material wood;
        private static Material stone;
        private static Material canvas;
        private static Material iron;
        private static Material slateRoof;

        [MenuItem("Phasebreak/Author V0.2 Starter Zone World Quality")]
        public static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open StarterZone_V2 in Edit Mode first.");
            Transform world = GameObject.Find("The Shattered Frontier")?.transform;
            if (world == null) throw new InvalidOperationException("The Shattered Frontier root is missing.");
            wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Generated/MI_WoodTrim.mat");
            stone = AssetDatabase.LoadAssetAtPath<Material>(Data + "Frontier Stone.mat");
            iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Generated/MI_MetalOrnaments.mat");
            if (wood == null || stone == null || iron == null) throw new InvalidOperationException("Existing world materials are missing.");
            canvas = CanvasMaterial();
            slateRoof = SlateRoofMaterial();

            Transform old = world.Find("V0.2 authored world detail");
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            Transform root = Group(world, "V0.2 authored world detail");

            ArrangeHouses(world.Find("Northgate - frontier settlement"), new[]
            {
                new Vector3(796, 0, 823), new Vector3(824, 0, 810), new Vector3(881, 0, 811),
                new Vector3(913, 0, 829), new Vector3(782, 0, 859), new Vector3(917, 0, 871),
                new Vector3(787, 0, 909), new Vector3(815, 0, 934), new Vector3(885, 0, 935),
                new Vector3(920, 0, 913), new Vector3(832, 0, 884), new Vector3(883, 0, 884),
                new Vector3(850, 0, 948)
            }, new[] { 30f, -12f, 18f, -35f, 74f, -68f, 105f, 20f, -15f, -96f, 160f, -155f, 5f });
            ArrangeHouses(world.Find("Westmere wetland"), new[]
            {
                new Vector3(440, 0, 965), new Vector3(468, 0, 948), new Vector3(494, 0, 974),
                new Vector3(450, 0, 1001), new Vector3(487, 0, 1014)
            }, new[] { 42f, -19f, -51f, 95f, -8f });
            ArrangeHouses(world.Find("Eastwatch ridge"), new[]
            {
                new Vector3(1380, 0, 1002), new Vector3(1413, 0, 1018), new Vector3(1400, 0, 973)
            }, new[] { 25f, -28f, 108f });

            Northgate(root);
            Westmere(root);
            Eastwatch(root);
            Roadside(root);
            Woodlands(root);
            PaintSettlementPaths();
            Atmosphere();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("V02_WORLD_QUALITY_AUTHORED: settlements, roadsides, landmarks and restrained atmosphere");
        }

        public static void ApplyBatch()
        {
            EditorSceneManager.OpenScene(ScenePath);
            ApplyToOpenScene();
        }

        private static void ArrangeHouses(Transform settlement, Vector3[] positions, float[] yaws)
        {
            if (settlement == null) throw new InvalidOperationException("Settlement root is missing.");
            int index = 0;
            foreach (Transform child in settlement)
            {
                if (child.name != "Brick House" && child.name != "Plaster House" &&
                    child.name != "Westmere home" && child.name != "Eastwatch outpost" &&
                    !child.name.Contains("frontier cottage")) continue;
                if (index >= positions.Length) break;
                Vector3 p = positions[index];
                child.SetPositionAndRotation(Ground(p.x, p.z), Quaternion.Euler(0, yaws[index], 0));
                if (index % 3 == 0)
                    foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>())
                        if (renderer.name.StartsWith("Roof ")) renderer.sharedMaterial = slateRoof;
                Transform oldVisual = child.Find("V0.2 kit house visual");
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>()) renderer.enabled = true;
                index++;
            }
            if (index != positions.Length) throw new InvalidOperationException("Expected settlement houses were not found: " + settlement.name);
        }

        private static void Northgate(Transform parent)
        {
            Transform town = Group(parent, "Northgate supply square and gate");
            // The south entrance frames the road; the open center remains available to NPCs and the player.
            KitProp(town, "Supply wagon", "Prop_Wagon.fbx", 822, 837, 28, 6.4f, wood);
            KitProp(town, "Merchant wagon", "Prop_Wagon.fbx", 895, 839, -35, 5.8f, wood);
            Stall(town, "Provision stall", 815, 863, 20);
            Stall(town, "Repair stall", 890, 864, -23);
            for (int i = 0; i < 5; i++) KitProp(town, "Stacked provisions", "Prop_Crate.fbx",
                810 + (i % 3) * 2.3f, 849 + (i / 3) * 2.5f, i * 31, 1.5f, wood);
            for (int i = 0; i < 4; i++) KitProp(town, "Quartermaster crates", "Prop_Crate.fbx",
                893 + (i % 2) * 2.2f, 851 + (i / 2) * 2.2f, i * 48, 1.6f, wood);
            FenceRun(town, 788, 831, 788, 887, 9);
            FenceRun(town, 914, 828, 914, 883, 9);
            FenceRun(town, 800, 805, 834, 805, 6);
            FenceRun(town, 876, 805, 902, 805, 5);
            Gate(town, 850, 786);
            Banner(town, "Gate banner west", 829, 787, 7);
            Banner(town, "Gate banner east", 872, 787, 7);
            FireRing(town, 850, 920, 2.6f);
            StoneScatter(town, 783, 806, 8, 8);
            StoneScatter(town, 916, 805, 8, 11);
        }

        private static void Westmere(Transform parent)
        {
            Transform village = Group(parent, "Westmere fishing and repair yard");
            KitProp(village, "Trade wagon", "Prop_Wagon.fbx", 471, 988, 78, 5.7f, wood);
            for (int i = 0; i < 5; i++) KitProp(village, "Dry goods", "Prop_Crate.fbx",
                464 + (i % 3) * 2, 994 + (i / 3) * 2, i * 29, 1.5f, wood);
            FenceRun(village, 429, 982, 430, 1015, 6);
            FenceRun(village, 511, 977, 509, 1009, 6);
            FireRing(village, 473, 970, 2.2f);
            StoneScatter(village, 430, 951, 6, 19);
        }

        private static void Eastwatch(Transform parent)
        {
            Transform watch = Group(parent, "Eastwatch defensive stores");
            FenceRun(watch, 1371, 961, 1423, 961, 10);
            FenceRun(watch, 1424, 962, 1424, 1010, 9);
            KitProp(watch, "Watch supplies", "Prop_Wagon.fbx", 1369, 986, 60, 5.5f, wood);
            for (int i = 0; i < 5; i++) KitProp(watch, "Watch crates", "Prop_Crate.fbx",
                1365 + (i % 3) * 2.1f, 995 + (i / 3) * 2.3f, i * 34, 1.5f, wood);
            Banner(watch, "Ridge standard", 1353, 1008, 10);
            StoneScatter(watch, 1432, 972, 8, 23);
        }

        private static void Roadside(Transform parent)
        {
            Transform road = Group(parent, "Old King's Road story details");
            // Alternating visible anchors replace long empty stretches without increasing active enemy counts.
            KitProp(road, "Lost caravan wagon", "Prop_Wagon.fbx", 530, 607, 120, 6.2f, wood);
            KitProp(road, "Lost cargo", "Prop_Crate.fbx", 535, 604, 35, 1.8f, wood);
            KitProp(road, "Lost cargo", "Prop_Crate.fbx", 527, 613, -17, 1.5f, wood);
            FenceRun(road, 555, 614, 575, 636, 5);
            FireRing(road, 616, 693, 2.3f);
            KitProp(road, "Ruined road wagon", "Prop_Wagon.fbx", 1027, 838, -36, 5.8f, wood);
            StoneScatter(road, 1030, 842, 9, 29);
            Banner(road, "Crypt waymarker", 1300, 588, 8);
            StoneScatter(road, 1302, 584, 9, 31);
            Banner(road, "Mine waymarker", 1571, 684, 7);
            StoneScatter(road, 1577, 680, 7, 37);
        }

        private static void Woodlands(Transform parent)
        {
            Transform forest = Group(parent, "Forest edges and settlement clearings");
            Grove(forest, 739, 887, 41, 46, 101);
            Grove(forest, 957, 898, 43, 48, 102);
            Grove(forest, 845, 1014, 48, 42, 103);
            Grove(forest, 374, 966, 30, 30, 104);
            Grove(forest, 544, 1023, 32, 30, 105);
            Grove(forest, 1324, 1049, 35, 32, 106);
            Grove(forest, 1480, 1030, 35, 34, 107);
            Grove(forest, 559, 735, 28, 30, 108);
            Grove(forest, 1119, 1115, 35, 30, 109);
        }

        private static void Grove(Transform parent, float x, float z, float radius, int count, int seed)
        {
            string pineRoot = "Assets/MapMagic/Demo/Trees/Pine/Prefabs/";
            GameObject[] pines = {
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineBig1.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineMed2.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>(pineRoot + "PineSmall1.prefab")
            };
            foreach (GameObject pine in pines)
                if (pine == null) throw new InvalidOperationException("MapMagic pine prefab is missing.");
            Material foliage = AssetDatabase.LoadAssetAtPath<Material>(Data + "Frontier Pine Foliage.mat");
            if (foliage == null) throw new InvalidOperationException("Frontier Pine Foliage material is missing.");
            var random = new System.Random(seed);
            Transform grove = Group(parent, "Pine and stone grove");
            for (int i = 0; i < count; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
                float px = x + Mathf.Cos(angle) * distance;
                float pz = z + Mathf.Sin(angle) * distance;
                GameObject tree = PrefabUtility.InstantiatePrefab(pines[i % pines.Length]) as GameObject;
                tree.name = "Grove pine";
                tree.transform.SetParent(grove);
                tree.transform.SetPositionAndRotation(Ground(px, pz), Quaternion.Euler(0, (float)random.NextDouble() * 360, 0));
                Renderer[] parts = tree.GetComponentsInChildren<Renderer>();
                if (parts.Length == 0) throw new InvalidOperationException("Pine prefab has no renderer.");
                foreach (Renderer part in parts) part.sharedMaterial = foliage;
                Bounds bounds = parts[0].bounds;
                for (int j = 1; j < parts.Length; j++) bounds.Encapsulate(parts[j].bounds);
                float targetHeight = 13 + (float)random.NextDouble() * 9;
                if (bounds.size.y > .001f) tree.transform.localScale *= targetHeight / bounds.size.y;
                tree.isStatic = true;
            }
        }

        private static void Stall(Transform parent, string name, float x, float z, float yaw)
        {
            Transform stall = Group(parent, name);
            stall.SetPositionAndRotation(Ground(x, z), Quaternion.Euler(0, yaw, 0));
            Cube(stall, "Timber counter", new Vector3(0, 1, 0), new Vector3(7, .3f, 2.4f), wood);
            for (int side = -1; side <= 1; side += 2)
                for (int end = -1; end <= 1; end += 2)
                    Cube(stall, "Canvas support", new Vector3(side * 3.2f, 2.4f, end * 1.6f), new Vector3(.2f, 4.8f, .2f), wood);
            Cube(stall, "Weathered canvas awning", new Vector3(0, 4.9f, 0), new Vector3(7.6f, .18f, 4.2f), canvas);
        }

        private static void Gate(Transform parent, float x, float z)
        {
            Transform gate = Group(parent, "Northgate southern fortified entry");
            gate.position = Ground(x, z);
            Cube(gate, "West stone pier", new Vector3(-10, 5, 0), new Vector3(4.5f, 10, 5), stone);
            Cube(gate, "East stone pier", new Vector3(10, 5, 0), new Vector3(4.5f, 10, 5), stone);
            Cube(gate, "Heavy crossbeam", new Vector3(0, 10.8f, 0), new Vector3(24, 1.5f, 5), wood);
            Cube(gate, "Guard roof", new Vector3(0, 12.6f, 0), new Vector3(27, 1.4f, 7), slateRoof);
            for (int side = -1; side <= 1; side += 2)
                Cube(gate, "Gate lamp bracket", new Vector3(side * 7.4f, 7.8f, -2.8f),
                    new Vector3(1.3f, .25f, .25f), iron);
        }

        private static void Banner(Transform parent, string name, float x, float z, float height)
        {
            Transform banner = Group(parent, name);
            banner.position = Ground(x, z);
            Cube(banner, "Iron bound post", new Vector3(0, height * .5f, 0), new Vector3(.28f, height, .28f), wood);
            Cube(banner, "Crossbar", new Vector3(1.2f, height - .65f, 0), new Vector3(2.6f, .18f, .18f), iron);
            Cube(banner, "Faded frontier cloth", new Vector3(1.2f, height - 1.7f, 0), new Vector3(2.3f, 1.9f, .08f), canvas);
        }

        private static void FireRing(Transform parent, float x, float z, float radius)
        {
            Transform ring = Group(parent, "Cold campfire and gathering place");
            ring.position = Ground(x, z);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Cube(ring, "Fire ring stone", new Vector3(Mathf.Cos(a) * radius, .25f, Mathf.Sin(a) * radius),
                    new Vector3(1.2f, .5f, .8f), stone).transform.localRotation = Quaternion.Euler(0, i * 45, 0);
            }
            Cube(ring, "Spent timber", new Vector3(0, .2f, 0), new Vector3(3.4f, .35f, .4f), wood).transform.localRotation = Quaternion.Euler(0, 30, 0);
            Cube(ring, "Spent timber", new Vector3(0, .25f, 0), new Vector3(3.4f, .35f, .4f), wood).transform.localRotation = Quaternion.Euler(0, -35, 0);
        }

        private static void FenceRun(Transform parent, float ax, float az, float bx, float bz, int count)
        {
            float segmentLength = Vector2.Distance(new Vector2(ax, az), new Vector2(bx, bz)) / count;
            float yaw = Mathf.Atan2(bx - ax, bz - az) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float t = (i + .5f) / count;
                float x = Mathf.Lerp(ax, bx, t), z = Mathf.Lerp(az, bz, t);
                Transform segment = Group(parent, "Timber boundary fence");
                segment.SetPositionAndRotation(Ground(x, z), Quaternion.Euler(0, yaw, 0));
                Cube(segment, "Lower rail", new Vector3(0, 1.15f, 0), new Vector3(.19f, .22f, segmentLength + .15f), wood);
                Cube(segment, "Upper rail", new Vector3(0, 2.15f, 0), new Vector3(.19f, .22f, segmentLength + .15f), wood);
                Cube(segment, "Fence post", new Vector3(0, 1.3f, -segmentLength * .5f), new Vector3(.34f, 2.6f, .34f), wood);
            }
        }

        private static void StoneScatter(Transform parent, float x, float z, int count, int seed)
        {
            var random = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float px = x + (float)(random.NextDouble() - .5) * 15;
                float pz = z + (float)(random.NextDouble() - .5) * 15;
                Cube(parent, "Roadside rubble", Ground(px, pz) + Vector3.up * .25f,
                    new Vector3(1 + (float)random.NextDouble(), .4f, .7f + (float)random.NextDouble()), stone)
                    .transform.rotation = Quaternion.Euler(0, (float)random.NextDouble() * 360, 0);
            }
        }

        private static void KitProp(Transform parent, string name, string file, float x, float z,
            float yaw, float width, Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + file);
            if (prefab == null) throw new InvalidOperationException("Missing kit prop: " + file);
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(Ground(x, z), Quaternion.Euler(0, yaw, 0));
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Kit prop has no renderer: " + file);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float horizontal = Mathf.Max(bounds.size.x, bounds.size.z);
            if (horizontal > .001f) obj.transform.localScale *= width / horizontal;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            obj.transform.position += Vector3.up * (Ground(x, z).y - bounds.min.y);
            foreach (Renderer renderer in renderers) renderer.sharedMaterial = material;
            obj.isStatic = true;
        }

        private static GameObject Cube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.localPosition = position;
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(block.GetComponent<Collider>());
            block.isStatic = true;
            return block;
        }

        private static Transform Group(Transform parent, string name)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent);
            return group;
        }

        private static Vector3 Ground(float x, float z)
        {
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 at = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (x >= at.x && x <= at.x + size.x && z >= at.z && z <= at.z + size.z)
                    return new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + at.y, z);
            }
            return new Vector3(x, Phasebreak.Gameplay.FrontierTerrainNode.Height(x, z), z);
        }

        private static Material CanvasMaterial()
        {
            string path = Data + "Frontier Market Canvas.mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                result = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(result, path);
            }
            result.SetColor("_BaseColor", new Color(.43f, .33f, .22f));
            result.SetFloat("_Smoothness", .05f);
            EditorUtility.SetDirty(result);
            return result;
        }

        private static Material SlateRoofMaterial()
        {
            string path = Data + "Frontier Slate Roof.mat";
            Material result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(Data + "Frontier Tile Roof.mat");
                if (source == null) throw new InvalidOperationException("Frontier Tile Roof material is missing.");
                result = new Material(source);
                AssetDatabase.CreateAsset(result, path);
            }
            result.SetColor("_BaseColor", new Color(.39f, .41f, .43f));
            EditorUtility.SetDirty(result);
            return result;
        }

        private static void PaintSettlementPaths()
        {
            // Existing terrain layers stay in place. A soft earth blend follows pedestrian routes only.
            Vector2[][] paths = {
                new[] { new Vector2(850, 785), new Vector2(850, 852), new Vector2(850, 925), new Vector2(850, 950) },
                new[] { new Vector2(850, 850), new Vector2(815, 850), new Vector2(790, 860) },
                new[] { new Vector2(850, 850), new Vector2(891, 850), new Vector2(915, 870) },
                new[] { new Vector2(850, 906), new Vector2(818, 925), new Vector2(792, 916) },
                new[] { new Vector2(850, 906), new Vector2(886, 922), new Vector2(913, 913) },
                new[] { new Vector2(448, 979), new Vector2(473, 984), new Vector2(497, 990) },
                new[] { new Vector2(1371, 1005), new Vector2(1391, 1008), new Vector2(1415, 1000) }
            };
            Vector2[] yards = { new Vector2(850, 870), new Vector2(473, 985), new Vector2(1390, 1008) };
            float[] radii = { 18, 12, 12 };
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                TerrainData data = terrain.terrainData;
                if (data.terrainLayers.Length < 2) throw new InvalidOperationException("V2 grass/earth terrain layers are missing.");
                int width = data.alphamapWidth, height = data.alphamapHeight;
                float[,,] weights = data.GetAlphamaps(0, 0, width, height);
                Vector3 origin = terrain.transform.position;
                for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
                {
                    Vector2 at = new Vector2(origin.x + (x + .5f) * data.size.x / width,
                        origin.z + (z + .5f) * data.size.z / height);
                    float mask = 0;
                    foreach (Vector2[] path in paths)
                        for (int i = 1; i < path.Length; i++)
                            mask = Mathf.Max(mask, 1 - Mathf.SmoothStep(0, 1,
                                Mathf.InverseLerp(2.5f, 6.5f, DistanceToSegment(at, path[i - 1], path[i]))));
                    for (int i = 0; i < yards.Length; i++)
                        mask = Mathf.Max(mask, (1 - Mathf.SmoothStep(0, 1,
                            Mathf.InverseLerp(radii[i] * .6f, radii[i], Vector2.Distance(at, yards[i])))) * .65f);
                    if (mask < .001f) continue;
                    float earth = Mathf.Max(weights[z, x, 1], mask * .88f);
                    float remain = Mathf.Max(.001f, weights[z, x, 0] + weights[z, x, 2]);
                    float rock = weights[z, x, 2] / remain * (1 - earth);
                    weights[z, x, 0] = 1 - earth - rock;
                    weights[z, x, 1] = earth;
                    weights[z, x, 2] = rock;
                }
                data.SetAlphamaps(0, 0, weights);
                EditorUtility.SetDirty(data);
            }
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(.001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + t * ab);
        }

        private static void Atmosphere()
        {
            Material roof = AssetDatabase.LoadAssetAtPath<Material>(Data + "Frontier Tile Roof.mat");
            if (roof != null)
            {
                roof.SetColor("_BaseColor", new Color(.55f, .49f, .43f));
                EditorUtility.SetDirty(roof);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.47f, .52f, .51f);
            RenderSettings.fogStartDistance = 360;
            RenderSettings.fogEndDistance = 1250;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.52f, .52f, .49f);
        }
    }
}
#endif
