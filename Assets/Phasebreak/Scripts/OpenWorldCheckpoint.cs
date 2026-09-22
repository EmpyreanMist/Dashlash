using UnityEngine;

namespace Phasebreak.Gameplay
{
    // Scene-authored safe arrival. Discovery remains owned and saved by QuestJournal.
    [DisallowMultipleComponent]
    public sealed class OpenWorldCheckpoint : MonoBehaviour
    {
        [SerializeField] private QuestLocation location;
        public string DisplayName => location != null ? location.DisplayName : "Frontier Trailhead";
        public void Configure(QuestLocation discoveredLocation) => location = discoveredLocation;

        public bool IsAvailable(QuestJournal journal) => isActiveAndEnabled &&
            (location == null || (location.isActiveAndEnabled && journal != null && journal.IsDiscovered(location.Id)));
    }
}
