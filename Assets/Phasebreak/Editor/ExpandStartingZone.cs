using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class ExpandStartingZone
    {
        private const string ScenePath = "Assets/dashlash.unity";
        private const string GeneratedRootName = "Starting Zone Terrain";

        [MenuItem("Phasebreak/Expand Starting Zone")]
        public static void Build()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject room = GameObject.Find("Test Room");
            if (room == null)
                throw new MissingReferenceException("The active Phasebreak scene has no Test Room root.");

            Transform floor = RequireChild(room.transform, "Floor");
            Transform north = RequireChild(room.transform, "Wall North");
            Transform south = RequireChild(room.transform, "Wall South");
            Transform east = RequireChild(room.transform, "Wall East");
            Transform west = RequireChild(room.transform, "Wall West");
            Material floorMaterial = floor.GetComponent<Renderer>().sharedMaterial;
            Material wallMaterial = north.GetComponent<Renderer>().sharedMaterial;

            Undo.RecordObjects(new Object[] { floor, north, south, east, west }, "Expand starting zone");
            SetTransform(floor, new Vector3(0f, -.5f, -5f), new Vector3(80f, 1f, 70f));
            SetTransform(south, new Vector3(0f, 1.5f, -40f), new Vector3(80f, 4f, .75f));
            SetTransform(east, new Vector3(40f, 1.5f, -5f), new Vector3(.75f, 4f, 70f));
            SetTransform(west, new Vector3(-40f, 1.5f, -5f), new Vector3(.75f, 4f, 70f));
            SetTransform(north, new Vector3(-22.5f, 1.5f, 30f), new Vector3(35f, 4f, .75f));

            Transform previous = room.transform.Find(GeneratedRootName);
            if (previous != null)
                Undo.DestroyObjectImmediate(previous.gameObject);

            GameObject generated = new(GeneratedRootName);
            Undo.RegisterCreatedObjectUndo(generated, "Create starting zone terrain");
            generated.transform.SetParent(room.transform, false);

            CreateBlock("Wall North East", generated.transform, new Vector3(22.5f, 1.5f, 30f),
                new Vector3(35f, 4f, .75f), Vector3.zero, wallMaterial);
            CreateBlock("Dungeon Approach", generated.transform, new Vector3(0f, -.48f, 36f),
                new Vector3(10f, 1f, 12f), Vector3.zero, floorMaterial);

            CreateBlock("West Low Rise", generated.transform, new Vector3(-25f, -.27f, -18f),
                new Vector3(16f, .45f, 12f), new Vector3(0f, 8f, 0f), floorMaterial);
            CreateBlock("East Low Rise", generated.transform, new Vector3(24f, -.3f, -13f),
                new Vector3(14f, .4f, 17f), new Vector3(0f, -11f, 0f), floorMaterial);
            CreateBlock("Northwest Shelf", generated.transform, new Vector3(-28f, -.18f, 17f),
                new Vector3(17f, .65f, 8f), new Vector3(0f, -6f, 0f), floorMaterial);
            CreateBlock("Northeast Shelf", generated.transform, new Vector3(27f, -.22f, 18f),
                new Vector3(14f, .55f, 7f), new Vector3(0f, 9f, 0f), floorMaterial);

            CreateBlock("West Ridge Ramp", generated.transform, new Vector3(-20f, -.12f, 10f),
                new Vector3(9f, .5f, 6f), new Vector3(-4f, 15f, 0f), floorMaterial);
            CreateBlock("East Ridge Ramp", generated.transform, new Vector3(18f, -.15f, 8f),
                new Vector3(8f, .45f, 6f), new Vector3(4f, -18f, 0f), floorMaterial);

            Vector3[] stonePositions =
            {
                new(-34f, 1.25f, -30f), new(-35f, .9f, -3f), new(-34f, 1.5f, 24f),
                new(34f, 1.1f, -29f), new(35f, 1.4f, -1f), new(34f, .95f, 24f)
            };
            for (int i = 0; i < stonePositions.Length; i++)
            {
                float height = stonePositions[i].y * 2f;
                CreateBlock($"Boundary Stone {i + 1}", generated.transform, stonePositions[i],
                    new Vector3(1.1f + (i % 2) * .25f, height, 1.1f),
                    new Vector3(i % 2 == 0 ? 4f : -5f, i * 23f, i % 3 - 1f), wallMaterial);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Expanded starting zone to 80 x 70 metres while preserving the central test area.");
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
                throw new MissingReferenceException($"Missing {name} below {parent.name}.");
            return child;
        }

        private static void SetTransform(Transform target, Vector3 position, Vector3 scale)
        {
            target.SetPositionAndRotation(position, Quaternion.identity);
            target.localScale = scale;
        }

        private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale,
            Vector3 rotation, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
            block.transform.localScale = scale;
            block.GetComponent<Renderer>().sharedMaterial = material;
            Undo.RegisterCreatedObjectUndo(block, "Create starting zone structure");
            return block;
        }
    }
}
