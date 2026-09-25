using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent, RequireComponent(typeof(Targetable))]
    public sealed class QuestNpc : MonoBehaviour
    {
        [SerializeField] private string npcId;
        [SerializeField] private string displayName;
        [SerializeField] private string role;
        [TextArea, SerializeField] private string greeting;
        private TextMeshPro label;
        private QuestJournal journal;
        private float nextStatusRefresh;

        public string Id => npcId;
        public string DisplayName => displayName;
        public string Role => role;
        public string Greeting => greeting;

        public void Configure(string id, string name, string job, string dialogue)
        {
            npcId = id; displayName = name; role = job; greeting = dialogue;
            ConfigureTargeting();
        }

        private void Awake()
        {
            ConfigureTargeting();
            label = GetComponentInChildren<TextMeshPro>(true);
            if (label != null)
            {
                label.fontSize = 4.2f;
                label.transform.localScale = Vector3.one * .45f;
            }
            journal = FindAnyObjectByType<QuestJournal>();
            RefreshLabel();
        }

        // Used by world authoring and by older scenes when loaded; no extra selection owner.
        private void ConfigureTargeting()
        {
            Targetable target = GetComponent<Targetable>();
            if (target == null) target = gameObject.AddComponent<Targetable>();
            target.Configure(displayName, TargetFaction.Friendly);
            if (GetComponent<Collider>() == null)
            {
                CapsuleCollider body = gameObject.AddComponent<CapsuleCollider>();
                body.center = Vector3.up * .9f;
                body.height = 1.8f;
                body.radius = .4f;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextStatusRefresh) return;
            nextStatusRefresh = Time.unscaledTime + .3f;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            if (journal == null) journal = FindAnyObjectByType<QuestJournal>();
            QuestDefinition active = journal?.ActiveQuest;
            bool turnIn = active != null && journal.ObjectiveReady && active.turnInNpcId == npcId;
            bool available = journal != null && journal.OffersQuest(npcId);
            string marker = turnIn ? "<size=160%><color=#7FFFE1>?</color></size>\n" :
                available ? "<size=160%><color=#FFD66B>!</color></size>\n" : string.Empty;
            label.text = $"{marker}{displayName}\n<color=#77D9F0>{role}</color>";
        }

        private void LateUpdate()
        {
            if (label == null || Camera.main == null) return;
            Vector3 towardCamera = label.transform.position - Camera.main.transform.position;
            if (towardCamera.sqrMagnitude > .001f) label.transform.rotation = Quaternion.LookRotation(towardCamera);
        }
    }
}
