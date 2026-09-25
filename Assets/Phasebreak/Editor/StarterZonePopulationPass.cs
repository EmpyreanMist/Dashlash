#if UNITY_EDITOR
using System;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    // Scene authoring only. Runtime population remains entirely with FrontierEncounterZone.
    public static class StarterZonePopulationPass
    {
        private const string ScenePath = "Assets/Phasebreak/Scenes/StarterZone_V2.unity";
        private const string RockPath = "Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Generated/Realm Rock.prefab";
        private const string EnemyPath = "Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab";
        private const string MaterialRoot = "Assets/Phasebreak/Data/StarterZoneV2/";

        private static Material timber;
        private static Material stone;
        private static Material riftStone;
        private static GameObject rock;
        private static GameObject enemy;

        [MenuItem("Phasebreak/Author Starter Zone V2 Population")]
        public static void ApplyToOpenScene()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open StarterZone_V2 in Edit Mode first.");
            Transform world = GameObject.Find("The Shattered Frontier")?.transform;
            Transform root = world?.Find("Encounter ecology - 26 respawning pockets") ??
                world?.Find("Encounter ecology - authored population");
            if (root == null) throw new InvalidOperationException("Starter Zone encounter root was not found.");
            Author(root);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("STARTER_ZONE_V2_POPULATION_AUTHORED: eight world sites plus untouched practice lane");
        }

        public static void Author(Transform root)
        {
            timber = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "Frontier Timber.mat");
            stone = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "Frontier Stone.mat");
            riftStone = AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot + "Rift Stone.mat");
            rock = AssetDatabase.LoadAssetAtPath<GameObject>(RockPath);
            enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath);
            if (timber == null || stone == null || riftStone == null || rock == null || enemy == null)
                throw new InvalidOperationException("Starter Zone population assets are missing.");

            // Preserve the existing 12-zombie mechanical test, including its transform and serialized formation.
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<FrontierEncounterZone>()?.ZoneId == "riftblade-practice") continue;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            root.name = "Encounter ecology - authored population";

            Transform opening = Site(root, "02 - Starting road stragglers", "opening-stragglers",
                new Vector2(195, 215), 1,
                new[] { O(-8, 5), O(5, -6), O(12, 8) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Zombie), R(EnemyRole.Zombie) });
            Crate(opening, 184, 221, 1.3f);
            Fence(opening, 207, 207, 35);

            Transform ambush = Site(root, "03 - Broken cart road ambush", "broken-cart-ambush",
                new Vector2(480, 555), 1,
                new[] { O(-12, -6), O(7, 4), O(14, -8) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Zombie), R(EnemyRole.Charger) });
            Cart(ambush, 482, 551, 25);
            Crate(ambush, 470, 559, 1.7f);
            Boulder(ambush, 493, 563, 1.2f);

            Transform camp = Site(root, "04 - Abandoned road camp", "abandoned-road-camp",
                new Vector2(625, 675), 1,
                new[] { O(-12, 9), O(8, 11), O(15, -5), O(-4, -10) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Charger), R(EnemyRole.Zombie), R(EnemyRole.Brute) });
            Campfire(camp, 625, 677);
            Crate(camp, 610, 681, 2);
            Barrel(camp, 638, 667);
            Fence(camp, 616, 662, -18);

            Transform guard = Site(root, "05 - East road brute guard", "east-road-brute-guard",
                new Vector2(1040, 830), 2,
                new[] { O(-16, -7), O(10, 9), O(3, -12) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Caster), R(EnemyRole.Brute, EnemyRank.Veteran) });
            Fence(guard, 1033, 816, 45);
            Crate(guard, 1051, 840, 2);
            Boulder(guard, 1024, 838, 1.5f);

            Vector2 overlookAt = new Vector2(1120, 1030);
            Vector3[] overlookOffsets = OverlookOffsets(overlookAt);
            Transform overlook = Site(root, "06 - King's Road imp overlook", "kings-road-imp-overlook",
                overlookAt, 2, overlookOffsets,
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Charger), R(EnemyRole.Skirmisher), R(EnemyRole.Caster) });
            Boulder(overlook, overlookAt.x + overlookOffsets[2].x + 5, overlookAt.y + overlookOffsets[2].z + 3, 1.8f);
            Boulder(overlook, overlookAt.x + overlookOffsets[3].x - 5, overlookAt.y + overlookOffsets[3].z + 2, 1.5f);

            Transform shrine = Site(root, "07 - Ruined shrine", "ruined-shrine",
                new Vector2(940, 1220), 2,
                new[] { O(-14, -9), O(8, 12), O(15, -4), O(-4, 4) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Charger), R(EnemyRole.Brute), R(EnemyRole.Caster, EnemyRank.Veteran) });
            Shrine(shrine, 940, 1220);
            Boulder(shrine, 923, 1234, 1.4f);

            Transform elite = Site(root, "08 - Old Fort elite watch", "old-fort-elite-watch",
                new Vector2(1535, 1553), 3,
                new[] { O(-13, -7), O(10, -8), O(1, 10) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Charger), R(EnemyRole.Caster, EnemyRank.Elite, "Veyra the Rift Seer") });
            Shrine(elite, 1535, 1553);
            Barrel(elite, 1550, 1545);

            Transform crypt = Site(root, "09 - Rift Crypt approach", "rift-crypt-approach",
                new Vector2(1320, 590), 3,
                new[] { O(-18, -9), O(14, -10), O(12, 12), O(-3, 8) },
                new[] { R(EnemyRole.Zombie), R(EnemyRole.Charger), R(EnemyRole.Skirmisher), R(EnemyRole.Brute, EnemyRank.Veteran, "Mordek the Gatebreaker") });
            BrokenWall(crypt, 1305, 580, 35);
            BrokenWall(crypt, 1336, 603, -22);
            Boulder(crypt, 1311, 610, 1.6f);
        }

        private static Transform Site(Transform parent, string name, string id, Vector2 center, int tier,
            Vector3[] offsets, FrontierEncounterZone.SpawnRole[] roles)
        {
            GameObject site = new GameObject(name);
            site.transform.SetParent(parent);
            site.transform.position = Ground(center.x, center.y);
            FrontierEncounterZone zone = site.AddComponent<FrontierEncounterZone>();
            zone.Configure(id, enemy, offsets.Length, 18, tier == 3 ? 240 : 180, tier);
            zone.ConfigureFormation(offsets);
            zone.ConfigureRoles(roles);
            return site.transform;
        }

        private static FrontierEncounterZone.SpawnRole R(EnemyRole role, EnemyRank rank = EnemyRank.Normal,
            string uniqueName = null) =>
            new FrontierEncounterZone.SpawnRole { role = role, rank = rank, uniqueName = uniqueName };
        private static Vector3 O(float x, float z) => new Vector3(x, 0, z);

        private static Vector3[] OverlookOffsets(Vector2 center)
        {
            // Pick two actual higher terrain positions, so Imps fire down toward the road approach.
            Vector3[] candidates = { O(-25, 19), O(-13, 27), O(3, 27), O(18, 19), O(26, 5), O(-27, 3) };
            Array.Sort(candidates, (a, b) => Ground(center.x + b.x, center.y + b.z).y
                .CompareTo(Ground(center.x + a.x, center.y + a.z).y));
            return new[] { O(-10, -9), O(11, -7), candidates[0], candidates[1] };
        }

        private static Vector3 Ground(float x, float z)
        {
            Vector3 point = new Vector3(x, 0, z);
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 p = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (x >= p.x && x <= p.x + size.x && z >= p.z && z <= p.z + size.z)
                    return new Vector3(x, terrain.SampleHeight(point) + p.y, z);
            }
            return new Vector3(x, FrontierTerrainNode.Height(x, z), z);
        }

        private static GameObject Block(Transform parent, string name, float x, float z, float rise,
            Vector3 size, float yaw, Material material, PrimitiveType type = PrimitiveType.Cube)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.SetPositionAndRotation(Ground(x, z) + Vector3.up * rise, Quaternion.Euler(0, yaw, 0));
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        private static void Boulder(Transform parent, float x, float z, float scale)
        {
            GameObject obj = PrefabUtility.InstantiatePrefab(rock) as GameObject;
            obj.name = "Encounter fieldstone";
            obj.transform.SetParent(parent);
            obj.transform.position = Ground(x, z);
            obj.transform.localScale *= scale;
        }

        private static void Crate(Transform parent, float x, float z, float size) =>
            Block(parent, "Abandoned crate", x, z, size * .5f, Vector3.one * size, 17, timber);

        private static void Barrel(Transform parent, float x, float z)
        {
            Block(parent, "Abandoned barrel", x, z, .9f, new Vector3(1.25f, .9f, 1.25f), 0, timber, PrimitiveType.Cylinder);
            Block(parent, "Barrel rim", x, z, 1.65f, new Vector3(1.35f, .065f, 1.35f), 0, stone, PrimitiveType.Cylinder);
        }

        private static void Fence(Transform parent, float x, float z, float yaw)
        {
            GameObject rail = Block(parent, "Broken fence rail", x, z, 1.1f, new Vector3(8, .3f, .35f), yaw, timber);
            Block(parent, "Fence post", x - 3, z, 1.1f, new Vector3(.45f, 2.2f, .45f), yaw, timber);
            StarterZonePlacement.BrokenFence(rail.transform);
        }

        private static void Cart(Transform parent, float x, float z, float yaw)
        {
            Transform cart = new GameObject("Broken supply cart").transform;
            cart.SetParent(parent);
            Block(cart, "Cart bed", x, z, .9f, new Vector3(4.8f, .45f, 2.7f), yaw, timber);
            Block(cart, "Cart side", x - 1.8f, z, 1.45f, new Vector3(.35f, 1.15f, 2.7f), yaw, timber);
            Block(cart, "Splintered cart side", x + 1.8f, z, 1.35f, new Vector3(.35f, .95f, 2.2f), yaw + 12, timber);
            Block(cart, "Cart rear", x, z - 1.1f, 1.4f, new Vector3(3.7f, 1.1f, .3f), yaw, timber);
            Block(cart, "Broken axle", x + 1.3f, z + 1.6f, .55f, new Vector3(2.9f, .28f, .28f), yaw + 24, timber);
            GameObject wheel = Block(cart, "Loose wheel", x - 2.6f, z - 2.2f, .85f,
                new Vector3(1.5f, .2f, 1.5f), 0, timber, PrimitiveType.Cylinder);
            wheel.transform.rotation = Quaternion.Euler(78, yaw + 28, 0);
            Block(cart, "Cart shaft", x + 3.7f, z + .3f, .72f, new Vector3(4, .28f, .28f), yaw - 8, timber);
            StarterZonePlacement.Cart(cart, x, z, yaw);
        }

        private static void Campfire(Transform parent, float x, float z)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5;
                Block(parent, "Fire ring stone", x + Mathf.Cos(a) * 1.7f, z + Mathf.Sin(a) * 1.7f,
                    .3f, new Vector3(1.2f, .6f, .8f), i * 72, stone);
            }
            Block(parent, "Cold firewood", x, z, .22f, new Vector3(2.5f, .3f, .4f), 35, timber);
            Block(parent, "Cold firewood", x, z, .3f, new Vector3(2.2f, .3f, .4f), -40, timber);
        }

        private static void Shrine(Transform parent, float x, float z)
        {
            Block(parent, "Broken shrine plinth", x, z, .6f, new Vector3(6, 1.2f, 5), 12, stone);
            Block(parent, "Fallen shrine pillar", x + 5, z - 4, .55f, new Vector3(7, 1.1f, 1.1f), 32, riftStone);
            Block(parent, "Standing shrine fragment", x - 6, z + 4, 2.2f, new Vector3(1.7f, 4.4f, 1.7f), -12, riftStone);
        }

        private static void BrokenWall(Transform parent, float x, float z, float yaw) =>
            Block(parent, "Collapsed approach wall", x, z, 1.2f, new Vector3(8, 2.4f, 1.6f), yaw, riftStone);
    }
}
#endif
