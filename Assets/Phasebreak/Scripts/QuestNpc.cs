using TMPro;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
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
        }

        private void Awake()
        {
            label = GetComponentInChildren<TextMeshPro>(true);
            journal = FindAnyObjectByType<QuestJournal>();
            RefreshLabel();
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
            string marker = turnIn ? "<color=#7FFFE1>?</color>\n" : available ? "<color=#FFD66B>!</color>\n" : string.Empty;
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
