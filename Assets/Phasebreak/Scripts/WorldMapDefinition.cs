using System;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum WorldMapProviderKind { Terrain, Dungeon, Custom }

    [Serializable]
    public struct WorldMapRegion
    {
        public string name;
        public Vector2 worldPosition;
    }

    [CreateAssetMenu(menuName = "Phasebreak/World/Map Definition", fileName = "WorldMap")]
    public sealed class WorldMapDefinition : ScriptableObject
    {
        public string sceneName;
        public string displayName;
        public WorldMapProviderKind provider = WorldMapProviderKind.Terrain;
        [Min(256)] public int resolution = 1024;
        public Rect worldBounds;
        public Sprite generatedTerrain;
        public Sprite generatedRoads;
        public WorldMapRegion[] regions;
        [Min(50f)] public float minimapViewHeight = 240f;
        [Header("Terrain layer names")]
        public string grassLayer = "Grass";
        public string earthLayer = "Earth";
        public string rockLayer = "Rock";
        [Tooltip("A terrain splat layer containing painted roads. Leave empty if this zone has no roads.")]
        public string roadLayer = "Earth";
        [Range(0f, 1f)] public float roadWeightStart = .7f;
        [Range(0f, 1f)] public float roadWeightFull = .92f;
        public Color lowland = new(.16f, .24f, .21f);
        public Color highland = new(.39f, .38f, .32f);
        public Color forest = new(.08f, .17f, .14f);
        public Color earth = new(.34f, .27f, .21f);
        public Color rock = new(.43f, .42f, .39f);
        public Color road = new(.68f, .51f, .30f);

        public Vector2 WorldToMap(Vector3 position) => new(
            Mathf.Clamp01((position.x - worldBounds.xMin) / worldBounds.width),
            Mathf.Clamp01((position.z - worldBounds.yMin) / worldBounds.height));
    }
}
