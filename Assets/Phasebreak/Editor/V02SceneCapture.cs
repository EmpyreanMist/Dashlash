#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class V02SceneCapture
    {
        public static void Before() => Capture("before");
        public static void After() => Capture("after");

        private static void Capture(string phase)
        {
            EditorSceneManager.OpenScene("Assets/Phasebreak/Scenes/StarterZone_V2.unity");
            string output = Path.Combine(Path.GetTempPath(), "phasebreak-v02-captures");
            Directory.CreateDirectory(output);
            View(output, phase, "northgate", new Vector2(850, 765), 48, new Vector2(850, 870));
            View(output, phase, "road", new Vector2(430, 465), 42, new Vector2(480, 555));
            View(output, phase, "eastwatch", new Vector2(1270, 860), 58, new Vector2(1390, 960));
            Debug.Log("V02_SCENE_CAPTURE_COMPLETE: " + output);
        }

        private static void View(string output, string phase, string name, Vector2 from, float height, Vector2 at)
        {
            var go = new GameObject("Temporary scene capture camera");
            var camera = go.AddComponent<Camera>();
            camera.transform.position = Ground(from.x, from.y) + Vector3.up * height;
            camera.transform.LookAt(Ground(at.x, at.y) + Vector3.up * 6);
            camera.fieldOfView = 62;
            camera.farClipPlane = 650;
            camera.allowHDR = false;
            var target = new RenderTexture(1280, 720, 24);
            camera.targetTexture = target;
            camera.Render();
            var old = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(output, phase + "-" + name + ".png"), image.EncodeToPNG());
            RenderTexture.active = old;
            camera.targetTexture = null;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(go);
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
    }
}
#endif
