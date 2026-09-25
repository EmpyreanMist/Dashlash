#if UNITY_EDITOR
using System;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    // Editor-only placement shared by generators and the non-destructive scene repair.
    public static class StarterZonePlacement
    {
        [MenuItem("Phasebreak/Repair Starter Zone V2 Placement")]
        public static void RepairOpenScene()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/Phasebreak/Scenes/StarterZone_V2.unity")
                throw new InvalidOperationException("Open StarterZone_V2 in Edit Mode first.");
            var world = GameObject.Find("The Shattered Frontier");
            if (world == null) throw new InvalidOperationException("The Shattered Frontier is missing.");
            foreach (Transform t in world.GetComponentsInChildren<Transform>(true))
            {
                if (t.Find("Masonry shaft") != null) Tower(t);
                if (t.Find("Cart bed") != null) Cart(t, 482, 551, 25);
                if (t.Find("Stone foundation") != null)
                {
                    if (Mathf.Abs(t.position.x - 1380) < .1f && Mathf.Abs(t.position.z - 1002) < .1f)
                        t.position = Ground(1375, 1000);
                    Cottage(t);
                }
                if (t.name == "Broken fence rail") BrokenFence(t);
                if (t.name == "Abandoned barrel")
                {
                    t.localScale = new Vector3(1.25f, .9f, 1.25f);
                    t.position = Ground(t.position.x, t.position.z) + Vector3.up * .9f;
                }
                if (t.name == "Barrel rim") t.localScale = new Vector3(1.35f, .065f, 1.35f);
                float width = PropWidth(t);
                if (width > 0)
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                    if (prefab == null) throw new InvalidOperationException("Missing source for " + t.name);
                    float yaw = Mathf.Round(Mathf.Atan2(-t.right.z, t.right.x) * Mathf.Rad2Deg * 1000) / 1000;
                    KitProp(t, prefab.transform, t.position.x, t.position.z, yaw, width);
                }
            }
            Supports(world.transform);
            PlaceCrypt(GameObject.Find("Rift Crypt")?.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("STARTER_ZONE_PLACEMENT_REPAIRED: existing props and foundations; gameplay owners preserved");
        }

        public static void PlaceCrypt(Transform root)
        {
            if (root == null || SceneManager.GetActiveScene().path != "Assets/Phasebreak/Scenes/StarterZone_V2.unity") return;
            Transform entrance = root.Find("Rift Crypt Entrance");
            if (entrance == null) throw new InvalidOperationException("Rift Crypt entrance is missing.");
            // The dungeon builder also serves the fallback scene. V2 keeps its interior off-world,
            // with the existing entrance on the road side of the basin's solid light monument.
            root.position = new Vector3(2500, 0, 1000);
            entrance.position = Ground(1450, 468) + Vector3.up * .08f;
        }

        private static float PropWidth(Transform t)
        {
            switch (t.name)
            {
                case "Supply wagon": return 6.4f;
                case "Merchant wagon": return 5.8f;
                case "Trade wagon": return 5.7f;
                case "Watch supplies": return 5.5f;
                case "Lost caravan wagon": return 6.2f;
                case "Ruined road wagon": return 5.8f;
                case "Stacked provisions": case "Dry goods": case "Watch crates": return 1.5f;
                case "Quartermaster crates": return 1.6f;
                case "Lost cargo": return t.position.x > 530 ? 1.8f : 1.5f;
                default: return 0;
            }
        }

        public static Vector3 Ground(float x, float z)
        {
            var point = new Vector3(x, 0, z);
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 p = terrain.transform.position, size = terrain.terrainData.size;
                if (x >= p.x && x <= p.x + size.x && z >= p.z && z <= p.z + size.z)
                    return new Vector3(x, terrain.SampleHeight(point) + p.y, z);
            }
            return new Vector3(x, FrontierTerrainNode.Height(x, z), z);
        }

        private static Bounds Bounds(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("Missing renderer: " + t.name);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        public static void KitProp(Transform t, Transform source, float x, float z, float yaw, float width)
        {
            // Normalize before applying heading: rotated world AABBs must not determine asset scale.
            // Medieval Village FBXs carry their Z-up conversion on the root; preserve that conversion.
            t.SetPositionAndRotation(Ground(x, z), source.localRotation);
            t.localScale = source.localScale;
            var bounds = Bounds(t);
            t.localScale *= width / Mathf.Max(bounds.size.x, bounds.size.z);
            t.rotation = Quaternion.Euler(0, yaw, 0) * source.localRotation;
            bounds = Bounds(t);
            t.position += Vector3.up * (Ground(bounds.center.x, bounds.center.z).y - bounds.min.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        }

        // These authored cubes have yaw only. Extend their buried end, retaining the visible top.
        public static void Footing(Transform t)
        {
            float bottom = float.PositiveInfinity;
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                Vector3 p = t.TransformPoint(new Vector3(x * .5f, 0, z * .5f));
                bottom = Mathf.Min(bottom, Ground(p.x, p.z).y - .04f);
            }
            float top = t.position.y + t.lossyScale.y * .5f;
            float height = Mathf.Max(.05f, top - bottom);
            if (Mathf.Abs(t.lossyScale.y - height) < .001f) return;
            Vector3 scale = t.localScale;
            scale.y = height / t.parent.lossyScale.y;
            t.localScale = scale;
            t.position = new Vector3(t.position.x, top - height * .5f, t.position.z);
        }

        public static void Supports(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "Left pier" || t.name == "Right pier" || t.name == "West stone pier" ||
                    t.name == "East stone pier" || t.name == "Canvas support" || t.name == "Fence post" ||
                    t.name == "Iron bound post" || t.name == "Fire ring stone") Footing(t);
        }

        public static void Cottage(Transform root)
        {
            Transform foundation = root.Find("Stone foundation");
            root.position = Ground(root.position.x, root.position.z);
            foundation.localPosition = new Vector3(0, .45f, 0);
            foundation.localScale = new Vector3(9.2f, .9f, 8.2f);
            Footing(foundation);
        }

        public static void Tower(Transform root)
        {
            Transform shaft = root.Find("Masonry shaft");
            Vector3 at = Ground(shaft.position.x, shaft.position.z);
            int slit = 0, brace = 0;
            foreach (Transform t in root)
            {
                Vector3 offset;
                switch (t.name)
                {
                    case "Masonry shaft": offset = new Vector3(0, 9, 0); t.localScale = new Vector3(11, 18, 11); break;
                    case "Timber watch gallery": offset = new Vector3(0, 18.6f, 0); break;
                    case "Roof left slope": offset = new Vector3(-3.85f, 21.75f, 0); break;
                    case "Roof right slope": offset = new Vector3(3.85f, 21.75f, 0); break;
                    case "Recessed entrance": offset = new Vector3(0, 2.1f, -5.6f); break;
                    case "Arrow slit": offset = new Vector3(slit++ == 0 ? -5.58f : 5.58f, 11, 0); break;
                    case "Watch brace":
                        offset = new Vector3(brace < 2 ? -6.4f : 6.4f, 19.5f, brace++ % 2 == 0 ? -5 : 5);
                        t.localScale = new Vector3(.5f, 2, .5f); break;
                    default: continue;
                }
                t.position = at + offset;
                if (t.name == "Recessed entrance")
                    t.position = Ground(t.position.x, t.position.z) + Vector3.up * 2.1f;
            }
            Footing(shaft);
        }

        public static void BrokenFence(Transform rail)
        {
            Transform post = rail.parent.Find("Fence post");
            if (post == null) return;
            Vector3 p = rail.position + rail.rotation * new Vector3(-3, 0, 0);
            post.position = new Vector3(p.x, rail.position.y, p.z);
            post.localScale = new Vector3(.45f, 2.2f, .45f);
            Footing(post);
        }

        public static void Cart(Transform root, float x, float z, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0, yaw, 0);
            Vector3 at = Ground(x, z);
            foreach (Transform t in root)
            {
                Vector3 offset; float turn = 0; bool debris = false;
                switch (t.name)
                {
                    case "Cart bed": offset = new Vector3(0, .225f, 0); t.localScale = new Vector3(4.8f, .45f, 2.7f); break;
                    case "Cart side": offset = new Vector3(-2.2f, .925f, 0); break;
                    case "Splintered cart side": offset = new Vector3(2.2f, .825f, 0); turn = 12; break;
                    case "Cart rear": offset = new Vector3(0, .9f, -1.2f); break;
                    case "Cart shaft": offset = new Vector3(3.7f, .3f, .3f); turn = -8; break;
                    case "Broken axle": offset = new Vector3(1.3f, 0, 2.4f); turn = 24; debris = true; break;
                    case "Loose wheel": offset = new Vector3(-2.6f, 0, -2.2f); turn = 28; debris = true; break;
                    default: continue;
                }
                t.position = at + rotation * offset;
                t.rotation = Quaternion.Euler(t.name == "Loose wheel" ? 78 : 0, yaw + turn, 0);
                if (debris)
                    t.position += Vector3.up * (Ground(t.position.x, t.position.z).y - Bounds(t).min.y);
            }
            Footing(root.Find("Cart bed"));
        }
    }
}
#endif
