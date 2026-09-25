using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum QuartermasterPurchaseResult { Purchased, InsufficientMarks, Unavailable }

    public readonly struct QuartermasterOffer
    {
        public readonly string ItemId;
        public readonly int Cost;

        public QuartermasterOffer(string itemId, int cost) { ItemId = itemId; Cost = cost; }
    }

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
        private static readonly QuartermasterOffer[] Stock =
        {
            new("sigil.emberglass", 4),
            new("boots.wake", 6),
            new("chest.bulwark", 8),
            new("relic.keen-cell", 13),
            new("relic.fracture", 20)
        };
        private readonly HashSet<string> completed = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> discovered = new(StringComparer.OrdinalIgnoreCase);
        private QuestCatalog catalog;
        private PlayerProgression progression;
        private PlayerBuildSystem build;
        private RiftDungeonController dungeon;
        private PlayerHealth health;
        public const float NpcInteractionDistance = 3.2f;
        private QuestNpc[] npcs;
        private float nextNpcRefresh;
        private string activeId;
        private int progress;
        private int marks;

        public QuestDefinition ActiveQuest => FindQuest(activeId);
        public QuestDefinition NextAvailableQuest => ActiveQuest != null ? null : catalog?.quests?.FirstOrDefault(q =>
            q != null && !completed.Contains(q.id) &&
            (string.IsNullOrWhiteSpace(q.prerequisiteQuestId) || completed.Contains(q.prerequisiteQuestId)));
        public int Progress => progress;
        public int Marks => marks;
        public bool ObjectiveReady => ActiveQuest != null && progress >= Mathf.Max(1, ActiveQuest.requiredCount);
        public IReadOnlyCollection<string> CompletedIds => completed;
        public IReadOnlyCollection<string> DiscoveredIds => discovered;
        public IReadOnlyList<QuartermasterOffer> QuartermasterStock => Stock;
        public event Action Changed;
        public event Action<string> Message;
        public event Action<QuestNpc> QuartermasterRequested;
        public event Action<QuestNpc> InteractionRequested;

        private void Awake()
        {
            catalog = Resources.Load<QuestCatalog>("World/QuestCatalog");
            progression = GetComponent<PlayerProgression>();
            build = GetComponent<PlayerBuildSystem>();
            dungeon = FindAnyObjectByType<RiftDungeonController>();
            health = GetComponent<PlayerHealth>();
            npcs = FindObjectsByType<QuestNpc>(FindObjectsSortMode.None);
            Load();
        }

        private void OnEnable()
        {
            CombatEvents.EnemyDefeatedDetailed += OnEnemyDefeated;
            if (dungeon == null) dungeon = FindAnyObjectByType<RiftDungeonController>();
            if (dungeon != null) dungeon.StateChanged += OnDungeonStateChanged;
        }

        private void OnDisable()
        {
            CombatEvents.EnemyDefeatedDetailed -= OnEnemyDefeated;
            if (dungeon != null) dungeon.StateChanged -= OnDungeonStateChanged;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextNpcRefresh)
            {
                nextNpcRefresh = Time.unscaledTime + 2f;
                npcs = FindObjectsByType<QuestNpc>(FindObjectsSortMode.None);
            }
        }

        public QuestNpc NearbyNpc()
        {
            QuestNpc nearest = null;
            float best = NpcInteractionDistance * NpcInteractionDistance;
            foreach (QuestNpc npc in npcs ?? Array.Empty<QuestNpc>())
            {
                if (!CanInteractWith(npc)) continue;
                Vector3 delta = npc.transform.position - transform.position;
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
                if (npc == null) return string.Empty;
                return $"Right-click {npc.DisplayName} to talk";
            }
        }

        public bool CanInteractWith(QuestNpc npc)
        {
            if (npc == null || !npc.isActiveAndEnabled || health != null && !health.IsAlive) return false;
            Targetable target = npc.GetComponent<Targetable>();
            if (target == null || !target.isActiveAndEnabled || target.Faction != TargetFaction.Friendly) return false;
            Vector3 delta = npc.transform.position - transform.position;
            return delta.sqrMagnitude <= NpcInteractionDistance * NpcInteractionDistance;
        }

        public QuestDefinition GetOfferedQuest(QuestNpc npc) => npc == null || ActiveQuest != null ? null :
            catalog?.quests?.FirstOrDefault(q => q != null && !completed.Contains(q.id) &&
                string.Equals(q.giverNpcId, npc.Id, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(q.prerequisiteQuestId) || completed.Contains(q.prerequisiteQuestId)));

        public bool CanTurnInAt(QuestNpc npc) => npc != null && ObjectiveReady &&
            string.Equals(ActiveQuest.turnInNpcId, npc.Id, StringComparison.OrdinalIgnoreCase);

        public bool TryAcceptQuest(QuestNpc npc, string expectedQuestId)
        {
            if (!CanInteractWith(npc)) return false;
            QuestDefinition next = GetOfferedQuest(npc);
            if (next == null || next.id != expectedQuestId) return false;
            activeId = next.id;
            progress = next.objectiveKind == QuestObjectiveKind.Discover && discovered.Contains(next.targetId) ? next.requiredCount : 0;
            Save(); Changed?.Invoke();
            Message?.Invoke($"QUEST ACCEPTED  •  {next.title}\n{next.description}");
            return true;
        }

        public bool CompleteQuestAt(QuestNpc npc, string expectedQuestId)
        {
            if (!CanInteractWith(npc) || !CanTurnInAt(npc) || ActiveQuest.id != expectedQuestId) return false;
            CompleteActive();
            return true;
        }

        public bool SpeakTo(QuestNpc npc)
        {
            if (!CanInteractWith(npc)) return false;
            QuestDefinition active = ActiveQuest;
            if (active != null && !ObjectiveReady && active.objectiveKind == QuestObjectiveKind.Speak &&
                string.Equals(active.targetId, npc.Id, StringComparison.OrdinalIgnoreCase))
                Advance(1);
            return true;
        }

        public bool Interact(QuestNpc npc)
        {
            if (npc == null || GameplayInputFocus.GameplayInputBlocked ||
                PhasebreakInventoryHud.IsMajorMenuOpen || WorldQuestHud.IsWorldMenuOpen) return false;
            if (!CanInteractWith(npc))
            {
                Message?.Invoke("Too far away");
                return false;
            }
            if (!CanTurnInAt(npc) && GetOfferedQuest(npc) == null) SpeakTo(npc);
            if (!CanTurnInAt(npc) && GetOfferedQuest(npc) == null &&
                string.Equals(npc.Id, "quartermaster-orin", StringComparison.OrdinalIgnoreCase))
                QuartermasterRequested?.Invoke(npc);
            else
                InteractionRequested?.Invoke(npc);
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

        public QuartermasterPurchaseResult TryPurchase(string itemId)
        {
            QuartermasterOffer offer = Stock.FirstOrDefault(candidate => candidate.ItemId == itemId);
            if (offer.Cost <= 0 || build == null || build.FindItemById(itemId) == null)
                return QuartermasterPurchaseResult.Unavailable;
            if (marks < offer.Cost) return QuartermasterPurchaseResult.InsufficientMarks;
            if (!build.GrantRewardById(itemId)) return QuartermasterPurchaseResult.Unavailable;
            marks -= offer.Cost;
            Save();
            Changed?.Invoke();
            return QuartermasterPurchaseResult.Purchased;
        }

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
            if (!completed.Add(quest.id)) return;
            activeId = null; progress = 0;
            marks += quest.marksReward;
            if (quest.experienceReward > 0) progression?.GrantExperience(quest.experienceReward);
            if (!string.IsNullOrWhiteSpace(quest.itemRewardId)) build?.GrantRewardById(quest.itemRewardId);
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static void DebugDeleteSavedState() => PlayerPrefs.DeleteKey(SaveKey);
#endif
    }
}
