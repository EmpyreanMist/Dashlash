using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    [Serializable] internal sealed class QuestSaveData
    {
        public int version = 1;
        public string activeId;
        public int progress;
        public int marks;
        public List<string> completed = new();
        public List<string> discovered = new();
    }

    [DisallowMultipleComponent]
    public sealed class QuestJournal : MonoBehaviour
    {
        private const string SaveKey = "Phasebreak.World.v1";
        private readonly HashSet<string> completed = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> discovered = new(StringComparer.OrdinalIgnoreCase);
        private QuestCatalog catalog;
        private PlayerProgression progression;
        private PlayerBuildSystem build;
        private RiftDungeonController dungeon;
        private InputAction interact;
        private QuestNpc[] npcs;
        private float nextNpcRefresh;
        private string activeId;
        private int progress;
        private int marks;

        public QuestDefinition ActiveQuest => FindQuest(activeId);
        public int Progress => progress;
        public int Marks => marks;
        public bool ObjectiveReady => ActiveQuest != null && progress >= Mathf.Max(1, ActiveQuest.requiredCount);
        public IReadOnlyCollection<string> CompletedIds => completed;
        public IReadOnlyCollection<string> DiscoveredIds => discovered;
        public event Action Changed;
        public event Action<string> Message;

        private void Awake()
        {
            catalog = Resources.Load<QuestCatalog>("World/QuestCatalog");
            progression = GetComponent<PlayerProgression>();
            build = GetComponent<PlayerBuildSystem>();
            dungeon = FindAnyObjectByType<RiftDungeonController>();
            interact = PhasebreakSettings.Button("interact", "Talk to NPC");
            npcs = FindObjectsByType<QuestNpc>(FindObjectsSortMode.None);
            Load();
        }

        private void OnEnable()
        {
            interact?.Enable();
            CombatEvents.EnemyDefeatedDetailed += OnEnemyDefeated;
            if (dungeon == null) dungeon = FindAnyObjectByType<RiftDungeonController>();
            if (dungeon != null) dungeon.StateChanged += OnDungeonStateChanged;
        }

        private void OnDisable()
        {
            interact?.Disable();
            CombatEvents.EnemyDefeatedDetailed -= OnEnemyDefeated;
            if (dungeon != null) dungeon.StateChanged -= OnDungeonStateChanged;
        }

        private void OnDestroy() { PhasebreakSettings.Unregister(interact); interact?.Dispose(); }

        private void Update()
        {
            if (Time.unscaledTime >= nextNpcRefresh)
            {
                nextNpcRefresh = Time.unscaledTime + 2f;
                npcs = FindObjectsByType<QuestNpc>(FindObjectsSortMode.None);
            }
            if (!GameplayInputFocus.GameplayInputBlocked && !WorldQuestHud.IsWorldMenuOpen && !PhasebreakInventoryHud.IsMajorMenuOpen &&
                interact != null && interact.WasPressedThisFrame())
            {
                QuestNpc nearby = NearbyNpc();
                if (nearby != null) Interact(nearby);
            }
        }

        public QuestNpc NearbyNpc()
        {
            QuestNpc nearest = null;
            float best = 10.24f;
            foreach (QuestNpc npc in npcs ?? Array.Empty<QuestNpc>())
            {
                if (npc == null || !npc.gameObject.activeInHierarchy) continue;
                Vector3 delta = npc.transform.position - transform.position; delta.y = 0f;
                float distance = delta.sqrMagnitude;
                if (distance >= best) continue;
                best = distance; nearest = npc;
            }
            return nearest;
        }

        public string InteractionPrompt
        {
            get
            {
                QuestNpc npc = NearbyNpc();
                return npc == null ? string.Empty : $"[E] Speak with {npc.DisplayName}";
            }
        }

        public bool Interact(QuestNpc npc)
        {
            if (npc == null) return false;
            QuestDefinition active = ActiveQuest;
            if (active != null)
            {
                if (ObjectiveReady && string.Equals(active.turnInNpcId, npc.Id, StringComparison.OrdinalIgnoreCase))
                {
                    CompleteActive();
                    return true;
                }
                if (active.objectiveKind == QuestObjectiveKind.Speak &&
                    string.Equals(active.targetId, npc.Id, StringComparison.OrdinalIgnoreCase))
                {
                    Advance(1);
                    Message?.Invoke($"{npc.DisplayName}: {npc.Greeting}");
                    return true;
                }
                Message?.Invoke($"{npc.DisplayName}: {npc.Greeting}");
                return true;
            }

            QuestDefinition next = catalog?.quests?.FirstOrDefault(q => q != null && !completed.Contains(q.id) &&
                string.Equals(q.giverNpcId, npc.Id, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(q.prerequisiteQuestId) || completed.Contains(q.prerequisiteQuestId)));
            if (next == null) { Message?.Invoke($"{npc.DisplayName}: {npc.Greeting}"); return false; }
            activeId = next.id;
            progress = next.objectiveKind == QuestObjectiveKind.Discover && discovered.Contains(next.targetId) ? next.requiredCount : 0;
            Save(); Changed?.Invoke();
            Message?.Invoke($"QUEST ACCEPTED  •  {next.title}\n{next.description}");
            return true;
        }

        public void Discover(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || !discovered.Add(id)) return;
            QuestDefinition quest = ActiveQuest;
            if (quest != null && quest.objectiveKind == QuestObjectiveKind.Discover &&
                string.Equals(quest.targetId, id, StringComparison.OrdinalIgnoreCase)) Advance(1);
            Save(); Changed?.Invoke();
            Message?.Invoke($"LOCATION DISCOVERED  •  {name}");
        }

        public bool IsDiscovered(string id) => discovered.Contains(id);

        public QuestDefinition FindQuest(string id) => string.IsNullOrWhiteSpace(id)
            ? null : catalog?.quests?.FirstOrDefault(q => q != null && string.Equals(q.id, id, StringComparison.OrdinalIgnoreCase));

        public bool IsCompleted(string id) => !string.IsNullOrWhiteSpace(id) && completed.Contains(id);

        public bool OffersQuest(string npcId) => ActiveQuest == null && catalog?.quests?.Any(q => q != null &&
            !completed.Contains(q.id) && string.Equals(q.giverNpcId, npcId, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(q.prerequisiteQuestId) || completed.Contains(q.prerequisiteQuestId))) == true;

        private void OnEnemyDefeated(EnemyDefeatedEvent defeated)
        {
            QuestDefinition quest = ActiveQuest;
            if (quest == null || quest.objectiveKind != QuestObjectiveKind.Defeat) return;
            if (string.IsNullOrWhiteSpace(quest.targetId) ||
                defeated.EnemyName.IndexOf(quest.targetId, StringComparison.OrdinalIgnoreCase) >= 0) Advance(1);
        }

        private void OnDungeonStateChanged()
        {
            QuestDefinition quest = ActiveQuest;
            if (quest == null || quest.objectiveKind != QuestObjectiveKind.CompleteDungeon || dungeon == null) return;
            if (dungeon.State == RiftDungeonState.Completed && dungeon.RewardClaimed) Advance(1);
        }

        private void Advance(int amount)
        {
            QuestDefinition quest = ActiveQuest;
            if (quest == null || progress >= quest.requiredCount) return;
            progress = Mathf.Min(quest.requiredCount, progress + Mathf.Max(0, amount));
            Save(); Changed?.Invoke();
            Message?.Invoke(ObjectiveReady ? $"OBJECTIVE COMPLETE  •  Return to {NpcName(quest.turnInNpcId)}" :
                $"{quest.objectiveText}  {progress}/{quest.requiredCount}");
        }

        private void CompleteActive()
        {
            QuestDefinition quest = ActiveQuest;
            if (quest == null || !ObjectiveReady) return;
            completed.Add(quest.id);
            marks += quest.marksReward;
            if (quest.experienceReward > 0) progression?.GrantExperience(quest.experienceReward);
            if (!string.IsNullOrWhiteSpace(quest.itemRewardId)) build?.GrantRewardById(quest.itemRewardId);
            activeId = null; progress = 0;
            Save(); Changed?.Invoke();
            Message?.Invoke($"QUEST COMPLETE  •  {quest.title}\n+{quest.experienceReward} XP   +{quest.marksReward} Rift Marks");
        }

        private string NpcName(string id) => (npcs ?? Array.Empty<QuestNpc>()).FirstOrDefault(n => n != null && n.Id == id)?.DisplayName ?? id;

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            QuestSaveData saved = JsonUtility.FromJson<QuestSaveData>(PlayerPrefs.GetString(SaveKey));
            if (saved == null) return;
            foreach (string id in saved.completed ?? new List<string>()) if (!string.IsNullOrWhiteSpace(id)) completed.Add(id);
            foreach (string id in saved.discovered ?? new List<string>()) if (!string.IsNullOrWhiteSpace(id)) discovered.Add(id);
            activeId = saved.activeId;
            progress = Mathf.Max(0, saved.progress);
            marks = Mathf.Max(0, saved.marks);
            if (FindQuest(activeId) == null) { activeId = null; progress = 0; }
        }

        private void Save()
        {
            QuestSaveData data = new() { activeId = activeId, progress = progress, marks = marks,
                completed = completed.OrderBy(x => x).ToList(), discovered = discovered.OrderBy(x => x).ToList() };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
