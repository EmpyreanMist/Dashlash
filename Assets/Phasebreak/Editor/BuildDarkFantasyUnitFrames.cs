using System.IO;
using Phasebreak.Gameplay;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildDarkFantasyUnitFrames
    {
        private const string Folder = "Assets/Phasebreak/Resources/UI";
        private static readonly Color Ink = new(.012f, .014f, .025f, .98f);
        private static readonly Color Inner = new(.028f, .032f, .052f, .98f);
        private static readonly Color Muted = new(.43f, .48f, .6f, 1f);
        private static readonly Color PlayerAccent = new(.18f, .72f, .9f, .82f);
        private static readonly Color PlayerGlow = new(.43f, .26f, .82f, .48f);
        private static readonly Color HostileAccent = new(.87f, .19f, .14f, .88f);
        private static readonly Color HostileGlow = new(1f, .42f, .12f, .42f);

        [MenuItem("Phasebreak/Build Dark Fantasy Unit Frames")]
        public static void Build()
        {
            EnsureFolder(Folder);
            SavePrefab(BuildPlayerFrame(), $"{Folder}/PlayerFrame.prefab");
            SavePrefab(BuildTargetFrame(), $"{Folder}/TargetFrame.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PHASEBREAK_UNIT_FRAMES_BUILT: player and target frame prefabs created.");
        }

        private static GameObject BuildPlayerFrame()
        {
            GameObject root = Root("Player Frame", new Vector2(400f, 132f), PlayerAccent, PlayerGlow,
                out UnityEngine.UI.Image accent, out TextMeshProUGUI moveMode);
            RectTransform portraitRoot = Portrait(root.transform, new Vector2(14f, -21f), new Vector2(86f, 86f),
                PlayerAccent, PlayerGlow, out UnityEngine.UI.Image portrait, out TextMeshProUGUI initial);

            TextMeshProUGUI name = Label("Player Name", root.transform, "PHASEBREAKER", 19f,
                TextAlignmentOptions.Left, new Vector2(116f, -13f), new Vector2(215f, 28f), Color.white, FontStyles.Bold);
            Label("Role", root.transform, "VANGUARD", 10f, TextAlignmentOptions.Left,
                new Vector2(117f, -37f), new Vector2(120f, 16f), new Color(.47f, .74f, .9f, 1f), FontStyles.Bold);
            TextMeshProUGUI level = Badge(root.transform, new Vector2(-18f, -15f), PlayerAccent, "1");

            HealthBarUI health = HealthBar(root.transform, new Vector2(116f, -57f), new Vector2(264f, 25f),
                new Color(.08f, .48f, .48f, 1f), new Color(.18f, .86f, .75f, 1f));
            ResourceBarUI resource = ResourceBar(root.transform, new Vector2(116f, -88f), new Vector2(264f, 18f),
                new Color(.17f, .19f, .48f, 1f), new Color(.34f, .5f, 1f, 1f));

            UnityEngine.UI.Image[] pipImages = new UnityEngine.UI.Image[3];
            RectTransform pipsRoot = Rect("Class Resource", root.transform, new Vector2(116f, -113f),
                new Vector2(122f, 10f));
            for (int i = 0; i < pipImages.Length; i++)
            {
                RectTransform pip = Rect($"Pip {i + 1}", pipsRoot, new Vector2(i * 40f, 0f), new Vector2(34f, 8f));
                pipImages[i] = Image(pip, new Color(.08f, .11f, .18f, .9f));
                AddOutline(pip.gameObject, new Color(.2f, .5f, .75f, .65f), new Vector2(1f, -1f));
            }
            ClassResourcePipsUI pips = pipsRoot.gameObject.AddComponent<ClassResourcePipsUI>();
            pips.Configure(pipImages, new Color(.28f, .85f, 1f, 1f), new Color(.08f, .11f, .18f, .9f));
            Label("Pip Label", root.transform, "PHASE", 9f, TextAlignmentOptions.Right,
                new Vector2(247f, -116f), new Vector2(65f, 14f), Muted, FontStyles.Bold);

            PlayerFrameUI frame = root.AddComponent<PlayerFrameUI>();
            frame.Configure(name, level, initial, portrait, health, resource, pips);
            HudFrameDragHandle drag = root.AddComponent<HudFrameDragHandle>();
            drag.Configure("Phasebreak.Hud.PlayerFrame", moveMode, accent, PlayerAccent,
                new Color(.42f, .94f, 1f, 1f));
            return root;
        }

        private static GameObject BuildTargetFrame()
        {
            GameObject root = Root("Target Frame", new Vector2(420f, 154f), HostileAccent, HostileGlow,
                out UnityEngine.UI.Image accent, out TextMeshProUGUI moveMode);
            Portrait(root.transform, new Vector2(14f, -23f), new Vector2(90f, 90f), HostileAccent, HostileGlow,
                out UnityEngine.UI.Image portrait, out TextMeshProUGUI initial);

            TextMeshProUGUI name = Label("Target Name", root.transform, "HOSTILE TARGET", 19f,
                TextAlignmentOptions.Left, new Vector2(120f, -14f), new Vector2(218f, 28f), Color.white, FontStyles.Bold);
            Label("Disposition", root.transform, "HOSTILE", 10f, TextAlignmentOptions.Left,
                new Vector2(121f, -38f), new Vector2(90f, 16f), new Color(1f, .42f, .3f, 1f), FontStyles.Bold);
            TextMeshProUGUI rank = Label("Rank", root.transform, string.Empty, 10f, TextAlignmentOptions.Right,
                new Vector2(276f, -39f), new Vector2(72f, 16f), new Color(1f, .68f, .22f, 1f), FontStyles.Bold);
            TextMeshProUGUI level = Badge(root.transform, new Vector2(-18f, -15f), HostileAccent, "1");

            HealthBarUI health = HealthBar(root.transform, new Vector2(120f, -59f), new Vector2(280f, 27f),
                new Color(.45f, .045f, .07f, 1f), new Color(.93f, .18f, .14f, 1f));
            CastBarUI cast = CastBar(root.transform, new Vector2(120f, -93f), new Vector2(280f, 21f));

            UnityEngine.UI.Image[] statuses = new UnityEngine.UI.Image[6];
            RectTransform statusRoot = Rect("Status Slots", root.transform, new Vector2(120f, -121f),
                new Vector2(210f, 23f));
            for (int i = 0; i < statuses.Length; i++)
            {
                RectTransform slot = Rect($"Status {i + 1}", statusRoot, new Vector2(i * 34f, 0f), new Vector2(26f, 22f));
                statuses[i] = Image(slot, new Color(.18f, .1f, .13f, .78f));
                AddOutline(slot.gameObject, new Color(.52f, .16f, .15f, .68f), new Vector2(1f, -1f));
            }

            TargetFrameUI frame = root.AddComponent<TargetFrameUI>();
            frame.Configure(name, level, rank, initial, portrait, health, cast, statuses);
            HudFrameDragHandle drag = root.AddComponent<HudFrameDragHandle>();
            drag.Configure("Phasebreak.Hud.TargetFrame", moveMode, accent, HostileAccent,
                new Color(1f, .5f, .18f, 1f));
            return root;
        }

        private static GameObject Root(string name, Vector2 size, Color accentColor, Color glowColor,
            out UnityEngine.UI.Image accent, out TextMeshProUGUI moveMode)
        {
            GameObject root = new(name, typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            UnityEngine.UI.Image hitSurface = root.GetComponent<UnityEngine.UI.Image>();
            hitSurface.color = accentColor;
            hitSurface.raycastTarget = true;
            var shadow = root.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, .8f);
            shadow.effectDistance = new Vector2(7f, -7f);

            RectTransform panel = StretchRect("Obsidian Panel", root.transform, 3f);
            Image(panel, Ink);
            RectTransform inner = StretchRect("Inner Panel", panel, 4f);
            Image(inner, Inner);

            RectTransform topAccent = Rect("Arcane Edge", root.transform, new Vector2(12f, -3f),
                new Vector2(size.x - 24f, 4f));
            accent = Image(topAccent, accentColor);
            RectTransform glow = Rect("Arcane Glow", root.transform, new Vector2(54f, -8f),
                new Vector2(size.x - 108f, 2f));
            Image(glow, glowColor);

            Corner(root.transform, new Vector2(2f, -2f), 1f, accentColor);
            Corner(root.transform, new Vector2(-2f, -2f), -1f, accentColor, true);
            moveMode = Label("Move Mode", root.transform, string.Empty, 10f, TextAlignmentOptions.Right,
                new Vector2(size.x - 145f, -size.y + 17f), new Vector2(128f, 14f),
                new Color(.75f, .92f, 1f, 1f), FontStyles.Bold);
            moveMode.gameObject.SetActive(false);
            return root;
        }

        private static RectTransform Portrait(Transform parent, Vector2 position, Vector2 size, Color accentColor,
            Color glowColor, out UnityEngine.UI.Image portrait, out TextMeshProUGUI initial)
        {
            RectTransform border = Rect("Portrait Frame", parent, position, size);
            Image(border, accentColor);
            RectTransform inset = StretchRect("Portrait Well", border, 4f);
            Image(inset, new Color(.035f, .04f, .065f, 1f));
            RectTransform portraitRect = StretchRect("Portrait", inset, 6f);
            portrait = Image(portraitRect, new Color(glowColor.r * .42f, glowColor.g * .42f, glowColor.b * .55f, 1f));
            initial = LabelStretch("Portrait Initial", portraitRect, "P", 35f, TextAlignmentOptions.Center,
                new Color(.88f, .92f, 1f, 1f), FontStyles.Bold);
            RectTransform rune = Rect("Portrait Rune", border, new Vector2(size.x - 13f, -size.y + 13f),
                new Vector2(10f, 10f));
            rune.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image(rune, glowColor);
            return border;
        }

        private static TextMeshProUGUI Badge(Transform parent, Vector2 position, Color color, string text)
        {
            RectTransform badge = Rect("Level Badge", parent, position, new Vector2(42f, 34f),
                new Vector2(1f, 1f), new Vector2(1f, 1f));
            Image(badge, new Color(color.r * .38f, color.g * .38f, color.b * .38f, 1f));
            AddOutline(badge.gameObject, color, new Vector2(1.2f, -1.2f));
            return LabelStretch("Level", badge, text, 16f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        }

        private static HealthBarUI HealthBar(Transform parent, Vector2 position, Vector2 size,
            Color fillColor, Color shineColor)
        {
            RectTransform bar = Rect("Health Bar", parent, position, size);
            Image(bar, new Color(.035f, .03f, .045f, 1f));
            AddOutline(bar.gameObject, new Color(.28f, .24f, .31f, .9f), new Vector2(1f, -1f));
            RectTransform fill = StretchRect("Health Fill", bar, 3f);
            fill.anchorMax = new Vector2(1f, 1f);
            Image(fill, fillColor);
            RectTransform shine = StretchRect("Health Shine", fill, 0f);
            shine.anchorMin = new Vector2(0f, .78f);
            shine.anchorMax = Vector2.one;
            shine.offsetMin = Vector2.zero;
            shine.offsetMax = Vector2.zero;
            Image(shine, new Color(shineColor.r, shineColor.g, shineColor.b, .46f));
            TextMeshProUGUI label = LabelStretch("Health Value", bar, "100 / 100", 12f,
                TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            HealthBarUI result = bar.gameObject.AddComponent<HealthBarUI>();
            result.Configure(fill, label);
            return result;
        }

        private static ResourceBarUI ResourceBar(Transform parent, Vector2 position, Vector2 size,
            Color fillColor, Color shineColor)
        {
            RectTransform bar = Rect("Resource Bar", parent, position, size);
            Image(bar, new Color(.025f, .025f, .055f, 1f));
            RectTransform fill = StretchRect("Resource Fill", bar, 2f);
            Image(fill, fillColor);
            RectTransform shine = StretchRect("Resource Shine", fill, 0f);
            shine.anchorMin = new Vector2(0f, .72f);
            shine.anchorMax = Vector2.one;
            shine.offsetMin = Vector2.zero;
            shine.offsetMax = Vector2.zero;
            Image(shine, new Color(shineColor.r, shineColor.g, shineColor.b, .42f));
            TextMeshProUGUI label = LabelStretch("Resource Value", bar, "ENERGY 100", 10f,
                TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            ResourceBarUI result = bar.gameObject.AddComponent<ResourceBarUI>();
            result.Configure(fill, label);
            return result;
        }

        private static CastBarUI CastBar(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform bar = Rect("Cast Bar", parent, position, size);
            Image(bar, new Color(.045f, .025f, .05f, 1f));
            RectTransform fill = StretchRect("Cast Fill", bar, 2f);
            Image(fill, new Color(.72f, .22f, .56f, 1f));
            TextMeshProUGUI label = LabelStretch("Cast Name", bar, "SAVAGE STRIKE", 10f,
                TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            RectTransform markerRect = Rect("Interruptible", bar, new Vector2(-5f, -5f), new Vector2(8f, 8f),
                new Vector2(1f, 1f), new Vector2(.5f, .5f));
            markerRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            UnityEngine.UI.Image marker = Image(markerRect, new Color(1f, .72f, .2f, 1f));
            CastBarUI result = bar.gameObject.AddComponent<CastBarUI>();
            result.Configure(bar, fill, label, marker);
            return result;
        }

        private static void Corner(Transform parent, Vector2 position, float direction, Color color,
            bool right = false)
        {
            RectTransform shard = Rect(right ? "Right Fang" : "Left Fang", parent, position,
                new Vector2(24f, 6f), new Vector2(right ? 1f : 0f, 1f), new Vector2(right ? 1f : 0f, 1f));
            shard.localRotation = Quaternion.Euler(0f, 0f, 24f * direction);
            Image(shard, color);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size,
            Vector2? anchor = null, Vector2? pivot = null)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0f, 1f);
            rect.pivot = pivot ?? new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform StretchRect(string name, Transform parent, float inset)
        {
            RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            return rect;
        }

        private static UnityEngine.UI.Image Image(RectTransform rect, Color color)
        {
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            var outline = target.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, string value, float size,
            TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions, Color color, FontStyles style)
        {
            RectTransform rect = Rect(name, parent, position, dimensions);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static TextMeshProUGUI LabelStretch(string name, Transform parent, string value, float size,
            TextAlignmentOptions alignment, Color color, FontStyles style)
        {
            RectTransform rect = StretchRect(name, parent, 0f);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
