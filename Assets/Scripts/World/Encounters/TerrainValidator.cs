using UnityEngine;

namespace Dashlash.World.Encounters
{
    public static class TerrainValidator
    {
        public static bool IsSafeSpawn(Vector3 position)
        {
            // Terrain height check
            if (position.y < Terrain.activeTerrain.terrainData.size.y * 0.1f)
            {
                return false; // Below terrain
            }

            // Slope check
            float slope = Terrain.activeTerrain.terrainData.GetSteepness(position);
            if (slope > 45f) // >45 degrees
            {
                return false;
            }

            // Water check (assuming Water layer exists)
            if (Physics.CheckSphere(position, 1f, LayerMask.GetMask("Water")))
            {
                return false;
            }

            return true;
        }
    }
}