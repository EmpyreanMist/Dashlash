using UnityEngine;

namespace Phasebreak.Gameplay
{
    public enum QuestObjectiveKind { Speak, Defeat, Discover, CompleteDungeon }

    [CreateAssetMenu(menuName = "Phasebreak/World/Quest", fileName = "Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(2, 5)] public string description;
        public string giverNpcId;
        public string turnInNpcId;
        public string prerequisiteQuestId;
        public QuestObjectiveKind objectiveKind;
        public string targetId;
        [Tooltip("Optional authored encounter site for the world-map objective marker.")]
        public string mapEncounterZoneId;
        public string objectiveText;
        [Min(1)] public int requiredCount = 1;
        [Min(0)] public int experienceReward;
        [Min(0)] public int marksReward;
        public string itemRewardId;
        public Vector2 mapPosition;
    }
}
