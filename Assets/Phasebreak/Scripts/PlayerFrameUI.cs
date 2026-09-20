using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class PlayerFrameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI portraitInitial;
        [SerializeField] private UnityEngine.UI.Image portraitImage;
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private ResourceBarUI resourceBar;
        [SerializeField] private ClassResourcePipsUI classResource;

        private Targetable player;
        private PlayerCombat combat;
        private PlayerProgression progression;
        private PlayerBuildSystem build;

        public void Configure(TextMeshProUGUI unitName, TextMeshProUGUI level, TextMeshProUGUI initial,
            UnityEngine.UI.Image portrait, HealthBarUI health, ResourceBarUI resource,
            ClassResourcePipsUI pips)
        {
            nameLabel = unitName;
            levelLabel = level;
            portraitInitial = initial;
            portraitImage = portrait;
            healthBar = health;
            resourceBar = resource;
            classResource = pips;
        }

        public void Bind(Targetable playerTarget, PlayerCombat playerCombat, PlayerProgression playerProgression)
        {
            player = playerTarget;
            combat = playerCombat;
            progression = playerProgression;
            build = playerTarget != null ? playerTarget.GetComponent<PlayerBuildSystem>() : null;
            Refresh();
        }

        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage == null)
                return;
            portraitImage.sprite = sprite;
            portraitImage.color = sprite != null ? Color.white : new Color(.18f, .17f, .16f, 1f);
            portraitImage.preserveAspect = true;
            if (portraitInitial != null)
                portraitInitial.gameObject.SetActive(sprite == null);
        }

        private void Update() => Refresh();

        public void Refresh()
        {
            if (player == null)
                return;
            if (nameLabel != null)
                nameLabel.text = player.DisplayName.ToUpperInvariant();
            if (levelLabel != null)
                levelLabel.text = (progression != null ? progression.Level : player.Level).ToString();
            if (portraitImage != null)
            {
                PhasebreakIconCatalog icons = PhasebreakIconCatalog.Current;
                Sprite portrait = build != null && build.Specialization != Specialization.Unchosen
                    ? icons?.GetSpecializationIcon(build.Specialization) : icons?.GetSlotIcon(EquipmentSlot.Head);
                if (portraitImage.sprite != portrait) SetPortrait(portrait);
            }
            if (portraitInitial != null && portraitImage != null && portraitImage.sprite == null)
                portraitInitial.text = Initial(player.DisplayName);
            healthBar?.SetValue(player.CurrentHealth, player.MaxHealth);
            if (combat == null)
                return;
            resourceBar?.SetValue(combat.CurrentResource, combat.MaximumResource);
            AbilityState dash = combat.AbilityCount > 3 ? combat.GetAbilityState(3) : default;
            classResource?.SetPips(dash.Charges, dash.MaximumCharges);
        }

        private static string Initial(string value) => string.IsNullOrWhiteSpace(value)
            ? "?" : value.Trim()[0].ToString().ToUpperInvariant();
    }
}
