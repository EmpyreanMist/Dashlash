using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum TargetFaction
    {
        Player,
        Friendly,
        Neutral,
        Hostile
    }

    public enum UnitRank
    {
        Normal,
        Rare,
        Elite,
        Boss
    }

    [DisallowMultipleComponent]
    public sealed class Targetable : MonoBehaviour
    {
        private static readonly List<Targetable> activeTargets = new List<Targetable>();

        [Header("Identity")]
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField] private TargetFaction faction = TargetFaction.Hostile;
        [SerializeField] private UnitRank rank = UnitRank.Normal;
        [SerializeField, Min(0f)] private float nameplateHeight = 2.25f;

        [Header("Selection")]
        [SerializeField] private Color selectionColor = new Color(1f, 0.58f, 0.12f, 1f);
        [SerializeField, Min(0.01f)] private float selectionRingRadius = 0.72f;

        private const int RingSegments = 48;
        private MeleeEnemy enemy;
        private PlayerHealth playerHealth;
        private LineRenderer selectionRing;
        private Material selectionMaterial;

        public static IReadOnlyList<Targetable> ActiveTargets => activeTargets;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
        public int Level => level;
        public TargetFaction Faction => faction;
        public UnitRank Rank => rank;
        public Vector3 NameplateWorldPosition => transform.position + Vector3.up * nameplateHeight;
        public bool IsHostile => faction == TargetFaction.Hostile;
        public bool IsAlive => enemy != null ? enemy.IsAlive : playerHealth == null || playerHealth.IsAlive;
        public int CurrentHealth => enemy != null ? enemy.CurrentHealth : playerHealth != null ? playerHealth.CurrentHealth : 1;
        public int MaxHealth => enemy != null ? enemy.MaxHealth : playerHealth != null ? playerHealth.MaxHealth : 1;

        public void Configure(string newDisplayName, TargetFaction newFaction, int newLevel = 1)
        {
            displayName = newDisplayName;
            faction = newFaction;
            level = Mathf.Max(1, newLevel);
            selectionColor = faction == TargetFaction.Friendly ? new Color(.43f, .78f, .59f) :
                faction == TargetFaction.Neutral ? new Color(.85f, .75f, .48f) : new Color(1f, .58f, .12f);
            if (selectionRing != null)
                selectionRing.startColor = selectionRing.endColor = selectionColor;
            if (selectionMaterial != null) selectionMaterial.color = selectionColor;
        }

        public void SetLevel(int newLevel) => level = Mathf.Max(1, newLevel);

        public void SetRank(UnitRank newRank) => rank = newRank;

        public void SetSelected(bool selected)
        {
            EnsureSelectionRing();
            selectionRing.gameObject.SetActive(selected && IsAlive);
        }

        private void Awake()
        {
            enemy = GetComponent<MeleeEnemy>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (!activeTargets.Contains(this))
                activeTargets.Add(this);
        }

        private void OnDisable()
        {
            activeTargets.Remove(this);
            if (selectionRing != null)
                selectionRing.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            activeTargets.Remove(this);
            if (selectionMaterial != null)
                Destroy(selectionMaterial);
        }

        private void EnsureSelectionRing()
        {
            if (selectionRing != null)
                return;

            GameObject ringObject = new GameObject("Target Selection Ring");
            ringObject.transform.SetParent(transform, false);
            CharacterController characterController = GetComponent<CharacterController>();
            float localHeight = characterController != null
                ? characterController.center.y - characterController.height * 0.5f + 0.06f
                : 0.06f;
            ringObject.transform.localPosition = Vector3.up * localHeight;

            selectionRing = ringObject.AddComponent<LineRenderer>();
            selectionRing.useWorldSpace = false;
            selectionRing.loop = true;
            selectionRing.positionCount = RingSegments;
            selectionRing.widthMultiplier = 0.075f;
            selectionRing.numCornerVertices = 2;
            selectionRing.numCapVertices = 2;
            selectionRing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            selectionRing.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                selectionMaterial = new Material(shader) { color = selectionColor };
                selectionRing.sharedMaterial = selectionMaterial;
            }
            selectionRing.startColor = selectionColor;
            selectionRing.endColor = selectionColor;

            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                selectionRing.SetPosition(i,
                    new Vector3(Mathf.Cos(angle) * selectionRingRadius, 0f, Mathf.Sin(angle) * selectionRingRadius));
            }

            ringObject.SetActive(false);
        }
    }
}
