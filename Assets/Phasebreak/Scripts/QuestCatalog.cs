using UnityEngine;

namespace Phasebreak.Gameplay
{
    [CreateAssetMenu(menuName = "Phasebreak/World/Quest Catalog", fileName = "QuestCatalog")]
    public sealed class QuestCatalog : ScriptableObject
    {
        public int saveVersion = 1;
        public QuestDefinition[] quests;
    }
}
