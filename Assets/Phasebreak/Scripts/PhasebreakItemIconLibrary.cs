using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public static class PhasebreakItemIconLibrary
    {
        private const int Size = 64;
        private static readonly Dictionary<EquipmentSlot, Sprite> Cache = new();
        private static Sprite silhouette;

        public static Sprite Resolve(PhasebreakItemDefinition item)
            => item != null && item.icon != null ? item.icon : Get(item != null ? item.slot : EquipmentSlot.Core);

        public static Sprite Get(EquipmentSlot slot)
        {
            if (Cache.TryGetValue(slot, out Sprite sprite))
                return sprite;

            PhasebreakIconCatalog catalog = PhasebreakIconCatalog.Current;
            Sprite configured = catalog != null ? catalog.GetSlotIcon(slot) : null;
            if (configured != null)
            {
                Cache[slot] = configured;
                return configured;
            }

            Texture2D texture = NewTexture(Size, Size, $"Phasebreak {slot} Icon");
            DrawSlot(texture, slot);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(.5f, .5f), Size);
            sprite.name = $"Generated {slot} Icon";
            Cache[slot] = sprite;
            return sprite;
        }

        public static Sprite GetCharacterSilhouette()
        {
            if (silhouette != null)
                return silhouette;

            const int width = 220;
            const int height = 360;
            Texture2D texture = NewTexture(width, height, "Phasebreak Character Silhouette");
            Color body = new(.22f, .19f, .16f, .9f);
            Color edge = new(.69f, .48f, .25f, .72f);
            FillCircle(texture, 110, 310, 30, body);
            FillPolygon(texture, new[] { new Vector2Int(73, 272), new Vector2Int(147, 272), new Vector2Int(165, 170), new Vector2Int(135, 138), new Vector2Int(85, 138), new Vector2Int(55, 170) }, body);
            FillPolygon(texture, new[] { new Vector2Int(68, 260), new Vector2Int(48, 247), new Vector2Int(20, 145), new Vector2Int(45, 135), new Vector2Int(88, 232) }, body);
            FillPolygon(texture, new[] { new Vector2Int(152, 260), new Vector2Int(172, 247), new Vector2Int(200, 145), new Vector2Int(175, 135), new Vector2Int(132, 232) }, body);
            FillPolygon(texture, new[] { new Vector2Int(88, 145), new Vector2Int(108, 145), new Vector2Int(101, 18), new Vector2Int(68, 18) }, body);
            FillPolygon(texture, new[] { new Vector2Int(112, 145), new Vector2Int(132, 145), new Vector2Int(152, 18), new Vector2Int(119, 18) }, body);
            DrawCircle(texture, 110, 310, 31, edge, 3);
            DrawLine(texture, 73, 272, 55, 170, edge, 3);
            DrawLine(texture, 147, 272, 165, 170, edge, 3);
            DrawLine(texture, 55, 170, 85, 138, edge, 3);
            DrawLine(texture, 165, 170, 135, 138, edge, 3);
            DrawLine(texture, 85, 138, 68, 18, edge, 3);
            DrawLine(texture, 135, 138, 152, 18, edge, 3);
            texture.Apply(false, true);
            silhouette = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100f);
            silhouette.name = "Generated Character Silhouette";
            return silhouette;
        }

        private static Texture2D NewTexture(int width, int height, string name)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color32[] pixels = new Color32[width * height];
            texture.SetPixels32(pixels);
            return texture;
        }

        private static void DrawSlot(Texture2D texture, EquipmentSlot slot)
        {
            Color c = new(.78f, .93f, 1f, 1f);
            Color fill = new(.14f, .48f, .62f, .38f);
            switch (slot)
            {
                case EquipmentSlot.PrimaryWeapon:
                    DrawLine(texture, 17, 13, 47, 51, c, 5); DrawLine(texture, 13, 10, 24, 16, c, 4); DrawLine(texture, 39, 45, 50, 54, c, 3); break;
                case EquipmentSlot.Secondary:
                    FillPolygon(texture, new[] { new Vector2Int(15, 49), new Vector2Int(49, 49), new Vector2Int(45, 24), new Vector2Int(32, 11), new Vector2Int(19, 24) }, fill);
                    DrawLine(texture, 15, 49, 49, 49, c, 3); DrawLine(texture, 15, 49, 19, 24, c, 3); DrawLine(texture, 49, 49, 45, 24, c, 3); DrawLine(texture, 19, 24, 32, 11, c, 3); DrawLine(texture, 45, 24, 32, 11, c, 3); break;
                case EquipmentSlot.Head:
                    DrawArc(texture, 32, 32, 19, 15, 165, c, 4); DrawLine(texture, 13, 33, 18, 18, c, 4); DrawLine(texture, 51, 33, 46, 18, c, 4); DrawLine(texture, 18, 18, 27, 15, c, 3); DrawLine(texture, 46, 18, 37, 15, c, 3); break;
                case EquipmentSlot.Shoulders:
                    DrawArc(texture, 17, 28, 13, 15, 165, c, 4); DrawArc(texture, 47, 28, 13, 15, 165, c, 4); DrawLine(texture, 27, 36, 37, 36, c, 3); break;
                case EquipmentSlot.Chest:
                    FillPolygon(texture, new[] { new Vector2Int(18, 49), new Vector2Int(46, 49), new Vector2Int(51, 36), new Vector2Int(42, 12), new Vector2Int(22, 12), new Vector2Int(13, 36) }, fill);
                    DrawLine(texture, 18, 49, 46, 49, c, 3); DrawLine(texture, 18, 49, 13, 36, c, 3); DrawLine(texture, 46, 49, 51, 36, c, 3); DrawLine(texture, 13, 36, 22, 12, c, 3); DrawLine(texture, 51, 36, 42, 12, c, 3); DrawLine(texture, 22, 12, 42, 12, c, 3); break;
                case EquipmentSlot.Hands:
                    DrawLine(texture, 20, 13, 20, 43, c, 5); DrawLine(texture, 29, 14, 29, 47, c, 5); DrawLine(texture, 38, 14, 38, 45, c, 5); DrawLine(texture, 47, 17, 47, 40, c, 5); DrawLine(texture, 18, 15, 48, 15, c, 5); break;
                case EquipmentSlot.Legs:
                    DrawLine(texture, 20, 50, 44, 50, c, 5); DrawLine(texture, 22, 48, 27, 13, c, 7); DrawLine(texture, 42, 48, 37, 13, c, 7); break;
                case EquipmentSlot.Boots:
                    DrawLine(texture, 17, 48, 17, 19, c, 7); DrawLine(texture, 17, 17, 31, 12, c, 7); DrawLine(texture, 40, 48, 40, 19, c, 7); DrawLine(texture, 40, 17, 54, 12, c, 7); break;
                case EquipmentSlot.Core:
                    DrawDiamond(texture, 32, 32, 21, c, 4); DrawDiamond(texture, 32, 32, 10, c, 3); break;
                case EquipmentSlot.MobilityRelic:
                    DrawLine(texture, 10, 35, 30, 44, c, 4); DrawLine(texture, 10, 35, 30, 26, c, 4); DrawLine(texture, 25, 35, 54, 35, c, 5); DrawLine(texture, 42, 46, 54, 35, c, 4); DrawLine(texture, 42, 24, 54, 35, c, 4); break;
                case EquipmentSlot.PowerRelic:
                    DrawLine(texture, 35, 55, 18, 31, c, 5); DrawLine(texture, 18, 31, 31, 31, c, 5); DrawLine(texture, 31, 31, 27, 9, c, 5); DrawLine(texture, 27, 9, 48, 38, c, 5); DrawLine(texture, 48, 38, 35, 38, c, 5); break;
                case EquipmentSlot.UtilityRelic:
                    DrawCircle(texture, 32, 32, 20, c, 3); DrawLine(texture, 32, 12, 32, 52, c, 3); DrawLine(texture, 12, 32, 52, 32, c, 3); DrawCircle(texture, 32, 32, 6, c, 3); break;
                case EquipmentSlot.Sigil1:
                case EquipmentSlot.Sigil2:
                case EquipmentSlot.Sigil3:
                    DrawCircle(texture, 32, 32, 21, c, 3); DrawDiamond(texture, 32, 32, 14, c, 3); DrawCircle(texture, 32, 32, 4, c, 2); break;
                case EquipmentSlot.WildcardArtifact:
                    DrawStar(texture, 32, 32, 23, 10, c, 3); break;
            }
        }

        private static void DrawDiamond(Texture2D t, int x, int y, int r, Color c, int thick)
        {
            DrawLine(t, x, y + r, x + r, y, c, thick); DrawLine(t, x + r, y, x, y - r, c, thick);
            DrawLine(t, x, y - r, x - r, y, c, thick); DrawLine(t, x - r, y, x, y + r, c, thick);
        }

        private static void DrawStar(Texture2D t, int x, int y, int outer, int inner, Color c, int thick)
        {
            Vector2Int[] points = new Vector2Int[10];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = Mathf.Deg2Rad * (90f + i * 36f);
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = new Vector2Int(Mathf.RoundToInt(x + Mathf.Cos(angle) * radius), Mathf.RoundToInt(y + Mathf.Sin(angle) * radius));
            }
            for (int i = 0; i < points.Length; i++) DrawLine(t, points[i].x, points[i].y, points[(i + 1) % points.Length].x, points[(i + 1) % points.Length].y, c, thick);
        }

        private static void DrawArc(Texture2D t, int x, int y, int radius, float from, float to, Color c, int thick)
        {
            Vector2Int previous = default;
            for (int i = 0; i <= 32; i++)
            {
                float angle = Mathf.Lerp(from, to, i / 32f) * Mathf.Deg2Rad;
                Vector2Int point = new(Mathf.RoundToInt(x + Mathf.Cos(angle) * radius), Mathf.RoundToInt(y + Mathf.Sin(angle) * radius));
                if (i > 0) DrawLine(t, previous.x, previous.y, point.x, point.y, c, thick);
                previous = point;
            }
        }

        private static void DrawCircle(Texture2D t, int x, int y, int radius, Color c, int thick) => DrawArc(t, x, y, radius, 0, 360, c, thick);

        private static void FillCircle(Texture2D t, int x, int y, int radius, Color c)
        {
            for (int py = -radius; py <= radius; py++)
                for (int px = -radius; px <= radius; px++)
                    if (px * px + py * py <= radius * radius) Set(t, x + px, y + py, c);
        }

        private static void FillPolygon(Texture2D t, Vector2Int[] points, Color c)
        {
            int minY = t.height - 1, maxY = 0;
            foreach (Vector2Int p in points) { minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y); }
            for (int y = minY; y <= maxY; y++)
            {
                List<int> nodes = new();
                for (int i = 0, j = points.Length - 1; i < points.Length; j = i++)
                    if ((points[i].y < y && points[j].y >= y) || (points[j].y < y && points[i].y >= y))
                        nodes.Add(points[i].x + (y - points[i].y) * (points[j].x - points[i].x) / (points[j].y - points[i].y));
                nodes.Sort();
                for (int i = 0; i + 1 < nodes.Count; i += 2)
                    for (int x = nodes[i]; x <= nodes[i + 1]; x++) Set(t, x, y, c);
            }
        }

        private static void DrawLine(Texture2D t, int x0, int y0, int x1, int y1, Color c, int thick)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, error = dx + dy;
            while (true)
            {
                for (int oy = -thick / 2; oy <= thick / 2; oy++) for (int ox = -thick / 2; ox <= thick / 2; ox++) Set(t, x0 + ox, y0 + oy, c);
                if (x0 == x1 && y0 == y1) break;
                int doubled = 2 * error;
                if (doubled >= dy) { error += dy; x0 += sx; }
                if (doubled <= dx) { error += dx; y0 += sy; }
            }
        }

        private static void Set(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                texture.SetPixel(x, y, color);
        }
    }
}
