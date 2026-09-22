using System;
using System.Collections;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth), typeof(PlayerProgression), typeof(PhasebreakPlayerMovement))]
    public sealed class OpenWorldRecovery : MonoBehaviour
    {
        [SerializeField] private OpenWorldCheckpoint startingCheckpoint;
        [SerializeField] private OpenWorldCheckpoint[] checkpoints = Array.Empty<OpenWorldCheckpoint>();
        [SerializeField] private RiftDungeonController dungeon;
        [SerializeField, Min(.2f)] private float recoveryDelay = 1.25f;
        private PlayerHealth health;
        private PlayerProgression progression;
        private PhasebreakPlayerMovement movement;
        private PlayerCombat combat;
        private PlayerTargeting targeting;
        private QuestJournal journal;
        private Coroutine recovery;

        public bool IsRecovering => recovery != null;
        public event Action<string> Feedback;

        public void Configure(OpenWorldCheckpoint start, OpenWorldCheckpoint[] worldCheckpoints, RiftDungeonController crypt)
        {
            startingCheckpoint = start;
            checkpoints = worldCheckpoints;
            dungeon = crypt;
        }

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            progression = GetComponent<PlayerProgression>();
            movement = GetComponent<PhasebreakPlayerMovement>();
            combat = GetComponent<PlayerCombat>();
            targeting = GetComponent<PlayerTargeting>();
            journal = GetComponent<QuestJournal>();
        }

        private void OnEnable() => health.Died += HandleDeath;
        private void OnDisable()
        {
            health.Died -= HandleDeath;
            if (recovery != null) StopCoroutine(recovery);
            recovery = null;
        }

        public OpenWorldCheckpoint FindNearestCheckpoint(Vector3 position)
        {
            OpenWorldCheckpoint nearest = null;
            float distance = float.PositiveInfinity;
            Consider(startingCheckpoint);
            foreach (OpenWorldCheckpoint checkpoint in checkpoints) Consider(checkpoint);
            return nearest;

            void Consider(OpenWorldCheckpoint checkpoint)
            {
                if (checkpoint == null || checkpoint.gameObject.scene != gameObject.scene || !checkpoint.IsAvailable(journal)) return;
                float candidate = (checkpoint.transform.position - position).sqrMagnitude;
                if (candidate >= distance) return;
                distance = candidate;
                nearest = checkpoint;
            }
        }

        private void HandleDeath()
        {
            // Dungeon state is authoritative, including RewardReady and Completed.
            if (health.IsAlive || recovery != null || (dungeon != null && dungeon.IsInDungeon)) return;
            OpenWorldCheckpoint checkpoint = FindNearestCheckpoint(transform.position);
            if (checkpoint == null)
            {
                Debug.LogError("Open-world recovery requires an available scene checkpoint.", this);
                return;
            }
            targeting?.SetTarget(null);
            combat?.ResetCombat();
            movement.ResetMotion();
            recovery = StartCoroutine(Recover(checkpoint));
        }

        private IEnumerator Recover(OpenWorldCheckpoint checkpoint)
        {
            int lost = progression.ApplyDeathPenalty();
            Feedback?.Invoke($"Defeated - Lost {lost} XP");
            yield return new WaitForSecondsRealtime(recoveryDelay);
            if (!health.IsAlive && !(dungeon != null && dungeon.IsInDungeon))
            {
                if (checkpoint == null || !checkpoint.IsAvailable(journal)) checkpoint = FindNearestCheckpoint(transform.position);
                if (checkpoint != null)
                {
                    movement.DebugTeleport(checkpoint.transform.position, checkpoint.transform.rotation);
                    targeting?.SetTarget(null);
                    combat?.ResetCombat();
                    health.ResetHealth();
                    Feedback?.Invoke($"Recovered at {checkpoint.DisplayName} - Lost {lost} XP");
                }
            }
            recovery = null;
        }
    }
}
