using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class QuestLocation : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private string displayName;
        [SerializeField, Min(1f)] private float discoveryRadius = 18f;
        private Transform player;
        private bool reported;

        public string Id => locationId;
        public string DisplayName => displayName;

        public void Configure(string id, string name, float radius)
        {
            locationId = id; displayName = name; discoveryRadius = radius;
        }

        private void Update()
        {
            if (reported) return;
            if (player == null) player = FindAnyObjectByType<PlayerProgression>()?.transform;
            if (player == null) return;
            Vector3 delta = player.position - transform.position; delta.y = 0f;
            if (delta.sqrMagnitude > discoveryRadius * discoveryRadius) return;
            reported = true;
            player.GetComponent<QuestJournal>()?.Discover(locationId, displayName);
        }
    }
}
