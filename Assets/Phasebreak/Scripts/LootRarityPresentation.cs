using UnityEngine;
using UnityEngine.Rendering;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LootRarityPresentation : MonoBehaviour
    {
        private GameObject effectRoot;
        private Material effectMaterial;
        private ItemRarity? displayedRarity;

        public ItemRarity? DisplayedRarity => displayedRarity;

        public void Show(ItemRarity? rarity)
        {
            if (displayedRarity == rarity) return;
            displayedRarity = rarity;
            if (effectRoot != null)
            {
                effectRoot.SetActive(false);
                Destroy(effectRoot);
                effectRoot = null;
            }
            if (!rarity.HasValue) return;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) return;
            effectMaterial ??= new Material(shader);
            effectRoot = new GameObject("Loot Rarity Effect");
            effectRoot.transform.SetParent(transform, false);
            effectRoot.transform.localPosition = Vector3.up * .12f;

            Color color = Tint(rarity.Value);
            Ring("Ground Glint", .57f, .035f, new Color(color.r, color.g, color.b, .38f));
            if (rarity == ItemRarity.Common) return;

            int tier = (int)rarity.Value;
            Particles(color, tier);
            if (tier >= (int)ItemRarity.Rare)
                Beam("Loot Beam", tier == (int)ItemRarity.Rare ? 1.1f : tier == (int)ItemRarity.Epic ? 1.65f : 2.05f,
                    tier == (int)ItemRarity.Rare ? .105f : .16f, new Color(color.r, color.g, color.b, .68f));
            if (rarity == ItemRarity.Mythic)
            {
                Ring("Outer Glint", .83f, .025f, new Color(1f, .79f, .5f, .55f));
                Beam("Inner Beam", 1.7f, .055f, new Color(1f, .89f, .64f, .8f));
            }
        }

        private void Ring(string name, float radius, float width, Color color)
        {
            LineRenderer line = Line(name, width, color);
            line.positionCount = 33;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .025f, Mathf.Sin(angle) * radius));
            }
        }

        private void Beam(string name, float height, float width, Color color)
        {
            LineRenderer line = Line(name, width, color);
            line.positionCount = 2;
            line.SetPosition(0, Vector3.up * .05f);
            line.SetPosition(1, Vector3.up * height);
            line.endWidth = width * .18f;
            line.endColor = new Color(color.r, color.g, color.b, .08f);
        }

        private LineRenderer Line(string name, float width, Color color)
        {
            GameObject child = new(name);
            child.transform.SetParent(effectRoot.transform, false);
            LineRenderer line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = effectMaterial;
            line.useWorldSpace = false;
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Particles(Color color, int tier)
        {
            GameObject child = new("Loot Sparks");
            child.transform.SetParent(effectRoot.transform, false);
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = tier >= (int)ItemRarity.Epic ? 1.1f : .8f;
            main.startSpeed = tier >= (int)ItemRarity.Epic ? .7f : .45f;
            main.startSize = tier >= (int)ItemRarity.Epic ? .085f : .06f;
            main.startColor = new Color(color.r, color.g, color.b, .8f);
            main.maxParticles = 28;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var emission = particles.emission;
            emission.rateOverTime = tier == (int)ItemRarity.Uncommon ? 3f : tier == (int)ItemRarity.Rare ? 6f : tier == (int)ItemRarity.Epic ? 10f : 14f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = .28f;
            shape.angle = 8f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = effectMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
        }

        private static Color Tint(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Mythic => new Color(1f, .43f, .25f),
            ItemRarity.Epic => new Color(.72f, .52f, 1f),
            ItemRarity.Rare => new Color(.32f, .65f, 1f),
            ItemRarity.Uncommon => new Color(.42f, .88f, .55f),
            _ => new Color(.78f, .82f, .78f)
        };

        private void OnDestroy()
        {
            if (effectMaterial != null) Destroy(effectMaterial);
        }
    }
}
