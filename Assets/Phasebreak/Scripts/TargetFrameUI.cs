using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TargetFrameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI rankLabel;
        [SerializeField] private TextMeshProUGUI portraitInitial;
        [SerializeField] private UnityEngine.UI.Image portraitImage;
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private CastBarUI castBar;
        [SerializeField] private UnityEngine.UI.Image[] statusSlots;

        private Targetable target;
        private ICombatCastSource castSource;
        private TextMeshProUGUI dispositionLabel;
        private TextMeshProUGUI roleLabel;

        private void EnsureNpcPresentation()
        {
            if (nameLabel != null)
            {
                nameLabel.enableAutoSizing = true;
                nameLabel.fontSizeMin = 13f;
                nameLabel.fontSizeMax = 19f;
                nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
                nameLabel.overflowMode = TextOverflowModes.Truncate;
            }
            if (dispositionLabel == null) dispositionLabel = transform.Find("Disposition")?.GetComponent<TextMeshProUGUI>();
            if (roleLabel != null || healthBar == null) return;
            GameObject text = new("NPC Role", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(transform, false);
            roleLabel = text.GetComponent<TextMeshProUGUI>();
            RectTransform source = healthBar.GetComponent<RectTransform>();
            RectTransform rect = roleLabel.rectTransform;
            rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition; rect.sizeDelta = new Vector2(source.sizeDelta.x, 50f);
            roleLabel.fontSize = 18f;
            roleLabel.color = PhasebreakUiTheme.Text;
            roleLabel.alignment = TextAlignmentOptions.TopLeft;
            roleLabel.raycastTarget = false;
        }

        public Targetable Target => target;

        public void Configure(TextMeshProUGUI unitName, TextMeshProUGUI level, TextMeshProUGUI rank,
            TextMeshProUGUI initial, UnityEngine.UI.Image portrait, HealthBarUI health, CastBarUI cast,
            UnityEngine.UI.Image[] statuses)
        {
            nameLabel = unitName;
            levelLabel = level;
            rankLabel = rank;
            portraitInitial = initial;
            portraitImage = portrait;
            healthBar = health;
            castBar = cast;
            statusSlots = statuses;
        }

        public void Bind(Targetable newTarget)
        {
            EnsureNpcPresentation();
            target = newTarget;
            castSource = target != null
                ? target.GetComponent(typeof(ICombatCastSource)) as ICombatCastSource
                : null;
            gameObject.SetActive(target != null);
            Refresh();
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null)
                return;
            portraitImage.sprite = sprite;
            portraitImage.preserveAspect = true;
            if (portraitInitial != null)
                portraitInitial.gameObject.SetActive(sprite == null);
        }

        public void SetStatusIcon(int index, Sprite sprite)
        {
            if (statusSlots == null || index < 0 || index >= statusSlots.Length || statusSlots[index] == null)
                return;
            statusSlots[index].sprite = sprite;
            statusSlots[index].preserveAspect = true;
            statusSlots[index].color = sprite != null ? Color.white : new Color(.18f, .1f, .13f, .78f);
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (target == null)
                return;
            if (!target.IsAlive)
            {
                Bind(null);
                return;
            }
            if (nameLabel != null)
                nameLabel.text = target.DisplayName.ToUpperInvariant();
            if (levelLabel != null)
                levelLabel.text = target.Level.ToString();
            bool hostile = target.IsHostile;
            Color accent = hostile ? new Color(.84f, .39f, .29f) : target.Faction == TargetFaction.Friendly
                ? new Color(.43f, .78f, .59f) : new Color(.85f, .75f, .48f);
            if (dispositionLabel != null)
            {
                dispositionLabel.text = target.Faction.ToString().ToUpperInvariant();
                dispositionLabel.color = accent;
            }
            if (nameLabel != null) nameLabel.color = hostile ? PhasebreakUiTheme.Text : accent;
            if (healthBar != null) healthBar.gameObject.SetActive(hostile);
            if (roleLabel != null)
            {
                roleLabel.gameObject.SetActive(!hostile);
                roleLabel.text = target.GetComponent<QuestNpc>()?.Role ?? target.Faction.ToString();
            }
            if (statusSlots != null)
                foreach (UnityEngine.UI.Image slot in statusSlots)
                    if (slot != null) slot.gameObject.SetActive(hostile);
            if (portraitInitial != null && portraitImage != null && portraitImage.sprite == null)
                portraitInitial.text = Initial(target.DisplayName);
            if (rankLabel != null)
            {
                bool boss = target.GetComponent<RiftWardenBoss>() != null || target.Rank == UnitRank.Boss;
                MeleeEnemy enemy = target.GetComponent<MeleeEnemy>();
                rankLabel.text = boss ? "BOSS" : enemy != null && enemy.Rank == EnemyRank.Veteran ? "VETERAN" :
                    target.Rank == UnitRank.Normal ? string.Empty : target.Rank.ToString().ToUpperInvariant();
                rankLabel.gameObject.SetActive(hostile && !string.IsNullOrEmpty(rankLabel.text));
            }
            healthBar?.SetValue(target.CurrentHealth, target.MaxHealth);
            bool casting = hostile && castSource != null && castSource.IsCasting;
            castBar?.SetCast(casting ? castSource.CastName : string.Empty,
                casting ? castSource.CastProgress : 0f, casting,
                casting && castSource.IsCastInterruptible);
        }

        private static string Initial(string value) => string.IsNullOrWhiteSpace(value)
            ? "?" : value.Trim()[0].ToString().ToUpperInvariant();
    }
}
