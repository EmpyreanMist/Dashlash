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
            if (portraitInitial != null && portraitImage != null && portraitImage.sprite == null)
                portraitInitial.text = Initial(target.DisplayName);
            if (rankLabel != null)
            {
                bool boss = target.GetComponent<RiftWardenBoss>() != null || target.Rank == UnitRank.Boss;
                rankLabel.text = boss ? "BOSS" : target.Rank == UnitRank.Normal ? string.Empty : target.Rank.ToString().ToUpperInvariant();
                rankLabel.gameObject.SetActive(!string.IsNullOrEmpty(rankLabel.text));
            }
            healthBar?.SetValue(target.CurrentHealth, target.MaxHealth);
            bool casting = castSource != null && castSource.IsCasting;
            castBar?.SetCast(casting ? castSource.CastName : string.Empty,
                casting ? castSource.CastProgress : 0f, casting,
                casting && castSource.IsCastInterruptible);
        }

        private static string Initial(string value) => string.IsNullOrWhiteSpace(value)
            ? "?" : value.Trim()[0].ToString().ToUpperInvariant();
    }
}
