using Den.Tools.Matrices;
using MapMagic.Core;
using MapMagic.Nodes;
using MapMagic.Products;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [System.Serializable]
    [GeneratorMenu(menu = "Map/Initial", name = "Shattered Frontier", disengageable = true)]
    public sealed class FrontierTerrainNode : Generator, IOutlet<MatrixWorld>
    {
        public const int PHASEBREAK_STARTER_V2_SEED = 271828;
        private static readonly Vector3[] PrimaryRoad =
        {
            new Vector3(145, 56, 155), new Vector3(194, 58, 210), new Vector3(232, 60, 292),
            new Vector3(312, 63, 355), new Vector3(380, 67, 435), new Vector3(435, 70, 522),
            new Vector3(512, 66, 590), new Vector3(630, 61, 670), new Vector3(742, 68, 790),
            new Vector3(850, 71, 870), new Vector3(960, 76, 950), new Vector3(1110, 82, 1040),
            new Vector3(1240, 89, 1150), new Vector3(1430, 102, 1280), new Vector3(1640, 143, 1450),
            new Vector3(1820, 210, 1840)
        };
        private static readonly Vector3[] WestRoad =
        {
            new Vector3(650, 62, 690), new Vector3(565, 57, 770),
            new Vector3(470, 49, 985), new Vector3(350, 56, 1110), new Vector3(250, 70, 1270)
        };
        private static readonly Vector3[] EastRoad =
        {
            new Vector3(850, 71, 870), new Vector3(1040, 74, 830),
            new Vector3(1220, 76, 910), new Vector3(1390, 76, 1010),
            new Vector3(1590, 91, 1040), new Vector3(1880, 120, 1160)
        };
        private static readonly Vector3[] RiftRoad =
        {
            new Vector3(1040, 74, 830), new Vector3(1180, 55, 710),
            new Vector3(1320, 30, 590), new Vector3(1450, 10, 480),
            new Vector3(1690, 55, 350)
        };
        private static readonly Vector3[] RuinsRoad =
        {
            new Vector3(850, 71, 870), new Vector3(910, 83, 1050),
            new Vector3(940, 105, 1220), new Vector3(1020, 126, 1420)
        };
        private static readonly Vector3[] SouthRoad =
        {
            new Vector3(435, 70, 522), new Vector3(610, 65, 400),
            new Vector3(775, 59, 290), new Vector3(850, 56, 40)
        };

        public override void Generate(TileData data, StopToken stop)
        {
            MatrixWorld map = new MatrixWorld(data.area.full.rect, data.area.full.worldPos,
                data.area.full.worldSize, data.globals.height);
            for (int z = map.rect.offset.z; z < map.rect.offset.z + map.rect.size.z; z++)
            {
                if (stop != null && stop.stop) return;
                for (int x = map.rect.offset.x; x < map.rect.offset.x + map.rect.size.x; x++)
                {
                    float wx = map.worldPos.x + (x - map.rect.offset.x) * map.worldSize.x / (map.rect.size.x - 1);
                    float wz = map.worldPos.z + (z - map.rect.offset.z) * map.worldSize.z / (map.rect.size.z - 1);
                    map[x, z] = Height(wx, wz) / data.globals.height;
                }
            }
            data.StoreProduct(this, map);
        }

        public static float Height(float x, float z)
        {
            float broad = Mathf.PerlinNoise(x / 510f + 37.1f, z / 510f + 18.3f);
            float middle = Mathf.PerlinNoise(x / 155f + 8.7f, z / 155f + 41.2f);
            float fine = Mathf.PerlinNoise(x / 43f + 73.4f, z / 43f + 11.8f);
            float north = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1050, 1920, z));
            float west = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(160, 570, x));
            float east = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1350, 1960, x));
            float starter = 1f - Mathf.SmoothStep(0, 1, Vector2.Distance(new Vector2(x, z), new Vector2(160, 160)) / 260f);
            float rift = 1f - Mathf.SmoothStep(0, 1, Vector2.Distance(new Vector2(x, z), new Vector2(1450, 480)) / 410f);
            float h = 30 + broad * 32 + (middle - .5f) * 18 + (fine - .5f) * 4;
            h += north * (70 + broad * 125) + west * (25 + middle * 48) + east * (35 + broad * 70);
            h -= starter * 30 + rift * 40;
            // Traversable clearings are deliberate flats within the larger natural forms.
            h = Flatten(h, x, z, 850, 870, 150, 71);
            h = Flatten(h, x, z, 470, 985, 80, 49);
            h = Flatten(h, x, z, 1390, 1010, 75, 76);
            h = Flatten(h, x, z, 1020, 1420, 90, 126);
            h = SmoothRoad(h, x, z, PrimaryRoad, true);
            h = SmoothRoad(h, x, z, WestRoad, false);
            h = SmoothRoad(h, x, z, EastRoad, false);
            h = SmoothRoad(h, x, z, RiftRoad, false);
            h = SmoothRoad(h, x, z, RuinsRoad, false);
            h = SmoothRoad(h, x, z, SouthRoad, false);
            return Mathf.Clamp(h, 8, 290);
        }

        private static float SmoothRoad(float height, float x, float z, Vector3[] road, bool opening)
        {
            Vector2 point = new Vector2(x, z);
            float nearest = float.MaxValue;
            float target = height;
            int segment = 0;
            for (int i = 0; i < road.Length - 1; i++)
            {
                Vector2 a = new Vector2(road[i].x, road[i].z);
                Vector2 b = new Vector2(road[i + 1].x, road[i + 1].z);
                Vector2 delta = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude);
                float distance = Vector2.Distance(point, a + t * delta);
                if (distance >= nearest) continue;
                nearest = distance;
                target = Mathf.Lerp(road[i].y, road[i + 1].y, t);
                segment = i;
            }
            float shoulder = opening && segment < 5 ? 48 : 115;
            float blend = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, shoulder, nearest));
            return Mathf.Lerp(height, target, blend);
        }

        private static float Flatten(float height, float x, float z, float cx, float cz, float radius, float level)
        {
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz));
            float t = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(radius * .7f, radius * 1.35f, d));
            return Mathf.Lerp(height, level, t);
        }
    }
}
