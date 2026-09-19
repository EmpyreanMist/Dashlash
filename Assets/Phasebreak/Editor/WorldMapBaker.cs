#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class WorldMapBaker
    {
        private const string OutputRoot = "Assets/Phasebreak/Generated/Maps";

        [MenuItem("Phasebreak/Maps/Bake Current World Map")]
        public static void BakeCurrent()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before baking a map.");
            string sceneName = SceneManager.GetActiveScene().name;
            WorldMapDefinition definition = Selection.activeObject as WorldMapDefinition;
            if (definition == null)
                definition = AssetDatabase.FindAssets("t:WorldMapDefinition")
                    .Select(guid => AssetDatabase.LoadAssetAtPath<WorldMapDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                    .FirstOrDefault(map => map.sceneName == sceneName);
            if (definition == null || definition.sceneName != sceneName)
                throw new InvalidOperationException("Select a WorldMapDefinition for the open scene.");
            if (definition.provider != WorldMapProviderKind.Terrain)
                throw new NotSupportedException($"{definition.provider} map baking has no provider yet.");
            Bake(definition);
        }

        public static void Bake(WorldMapDefinition definition)
        {
            Terrain[] terrains = UnityEngine.Object.FindObjectsByType<Terrain>()
                .Where(t => t.terrainData != null && t.gameObject.scene == SceneManager.GetActiveScene())
                .OrderBy(t => t.transform.position.z).ThenBy(t => t.transform.position.x).ToArray();
            if (terrains.Length == 0) throw new InvalidOperationException("No terrain in the open scene.");
            float x0 = terrains.Min(t => t.transform.position.x), z0 = terrains.Min(t => t.transform.position.z);
            float x1 = terrains.Max(t => t.transform.position.x + t.terrainData.size.x);
            float z1 = terrains.Max(t => t.transform.position.z + t.terrainData.size.z);
            definition.worldBounds = Rect.MinMaxRect(x0, z0, x1, z1);
            int size = Mathf.Clamp(definition.resolution, 256, 2048);
            // Authored vegetation lives in scene objects in this zone. Quantize it into broad
            // density cells so the map depicts woods rather than individual prefab locations.
            const int vegetationGrid = 64;
            float[,] vegetation = new float[vegetationGrid, vegetationGrid];
            foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (item.gameObject.scene != SceneManager.GetActiveScene()) continue;
                if (item.name.IndexOf("pine", StringComparison.OrdinalIgnoreCase) < 0 &&
                    item.name.IndexOf("tree", StringComparison.OrdinalIgnoreCase) < 0) continue;
                int gx = Mathf.FloorToInt((item.position.x - x0) / (x1 - x0) * vegetationGrid);
                int gz = Mathf.FloorToInt((item.position.z - z0) / (z1 - z0) * vegetationGrid);
                if (gx >= 0 && gx < vegetationGrid && gz >= 0 && gz < vegetationGrid) vegetation[gx, gz] += 1f;
            }
            float[,] forestDensity = new float[vegetationGrid, vegetationGrid];
            for (int gz = 0; gz < vegetationGrid; gz++) for (int gx = 0; gx < vegetationGrid; gx++)
            {
                float count = 0f;
                for (int oz = -2; oz <= 2; oz++) for (int ox = -2; ox <= 2; ox++)
                    count += vegetation[Mathf.Clamp(gx + ox, 0, vegetationGrid - 1),
                        Mathf.Clamp(gz + oz, 0, vegetationGrid - 1)];
                forestDensity[gx, gz] = Mathf.Clamp01(count / 12f);
            }
            Color32[] pixels = new Color32[size * size], roads = new Color32[size * size];
            float[] heights = new float[size * size], earthWeights = new float[size * size];
            float[] grassWeights = new float[size * size], rockWeights = new float[size * size];
            float[] roadWeights = new float[size * size];
            foreach (Terrain terrain in terrains)
            {
                TerrainData data = terrain.terrainData;
                int ax = data.alphamapWidth, az = data.alphamapHeight;
                float[,,] alpha = data.alphamapLayers > 0 ? data.GetAlphamaps(0, 0, ax, az) : null;
                int grass = LayerIndex(data, definition.grassLayer), earth = LayerIndex(data, definition.earthLayer);
                int rock = LayerIndex(data, definition.rockLayer), road = LayerIndex(data, definition.roadLayer);
                int px0 = Mathf.Max(0, Mathf.FloorToInt((terrain.transform.position.x - x0) / (x1 - x0) * size));
                int px1 = Mathf.Min(size, Mathf.CeilToInt((terrain.transform.position.x + data.size.x - x0) / (x1 - x0) * size));
                int pz0 = Mathf.Max(0, Mathf.FloorToInt((terrain.transform.position.z - z0) / (z1 - z0) * size));
                int pz1 = Mathf.Min(size, Mathf.CeilToInt((terrain.transform.position.z + data.size.z - z0) / (z1 - z0) * size));
                for (int z = pz0; z < pz1; z++) for (int x = px0; x < px1; x++)
                {
                    float u = Mathf.Clamp01((x0 + (x + .5f) * (x1 - x0) / size - terrain.transform.position.x) / data.size.x);
                    float v = Mathf.Clamp01((z0 + (z + .5f) * (z1 - z0) / size - terrain.transform.position.z) / data.size.z);
                    int i = z * size + x;
                    heights[i] = terrain.transform.position.y + data.GetInterpolatedHeight(u, v);
                    int sx = Mathf.Clamp(Mathf.FloorToInt(u * ax), 0, ax - 1), sz = Mathf.Clamp(Mathf.FloorToInt(v * az), 0, az - 1);
                    if (alpha != null)
                    {
                        if (grass >= 0) grassWeights[i] = alpha[sz, sx, grass];
                        if (earth >= 0) earthWeights[i] = alpha[sz, sx, earth];
                        if (rock >= 0) rockWeights[i] = alpha[sz, sx, rock];
                        if (road >= 0) roadWeights[i] = alpha[sz, sx, road];
                    }
                }
            }
            // Terrain texture is a neutral relief; roads remain a separate transparent layer.
            float minHeight = heights.Min(), maxHeight = heights.Max();
            for (int z = 0; z < size; z++) for (int x = 0; x < size; x++)
            {
                int i = z * size + x;
                float elevation = Mathf.InverseLerp(minHeight, maxHeight, heights[i]);
                float dx = heights[z * size + Mathf.Min(size - 1, x + 1)] - heights[z * size + Mathf.Max(0, x - 1)];
                float dz = heights[Mathf.Min(size - 1, z + 1) * size + x] - heights[Mathf.Max(0, z - 1) * size + x];
                float slope = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dz * dz) / 12f);
                float light = Mathf.Clamp(.84f + (dz - dx) * .017f, .48f, 1.12f);
                float contour = Mathf.Abs(Mathf.Repeat(heights[i] / 18f, 1f) - .5f) < .025f ? .88f : 1f;
                Color baseColor = Color.Lerp(definition.lowland, definition.highland, elevation);
                baseColor = Color.Lerp(baseColor, definition.rock, Mathf.Max(rockWeights[i] * .78f, slope * .58f));
                float cellX = x * (vegetationGrid - 1f) / (size - 1f);
                float cellZ = z * (vegetationGrid - 1f) / (size - 1f);
                int gx = Mathf.FloorToInt(cellX), gz = Mathf.FloorToInt(cellZ);
                float tx = cellX - gx, tz = cellZ - gz;
                float forest = grassWeights[i] * Mathf.Lerp(
                    Mathf.Lerp(forestDensity[gx, gz], forestDensity[Mathf.Min(gx + 1, vegetationGrid - 1), gz], tx),
                    Mathf.Lerp(forestDensity[gx, Mathf.Min(gz + 1, vegetationGrid - 1)],
                        forestDensity[Mathf.Min(gx + 1, vegetationGrid - 1), Mathf.Min(gz + 1, vegetationGrid - 1)], tx), tz);
                baseColor = Color.Lerp(baseColor, definition.forest, forest * .65f);
                baseColor = Color.Lerp(baseColor, definition.earth, earthWeights[i] * .42f);
                pixels[i] = baseColor * (light * contour);
                // The V2 surface pass paints road cores into its Earth splatmap. A high, narrow
                // earth weight threshold avoids converting the broad dry biome into a road.
                float roadCore = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(definition.roadWeightStart, definition.roadWeightFull, roadWeights[i]))
                    * (1f - rockWeights[i]);
                roads[i] = new Color(definition.road.r, definition.road.g, definition.road.b, roadCore * .86f);
            }
            EnsureFolder(OutputRoot);
            string stem = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(definition));
            definition.generatedTerrain = WriteSprite($"{OutputRoot}/{stem}_Terrain.png", pixels, size);
            definition.generatedRoads = WriteSprite($"{OutputRoot}/{stem}_Roads.png", roads, size);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"WORLD_MAP_BAKED {definition.name}: {terrains.Length} terrains, {size}x{size}, bounds {definition.worldBounds}");
        }

        private static int LayerIndex(TerrainData data, string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return -1;
            for (int i = 0; i < data.terrainLayers.Length; i++)
                if (data.terrainLayers[i] != null &&
                    data.terrainLayers[i].name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return i;
            return -1;
        }

        private static Sprite WriteSprite(string path, Color32[] pixels, int size)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false, true);
            texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
