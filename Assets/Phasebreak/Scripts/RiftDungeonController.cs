using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    public enum RiftDungeonState
    {
        Outside,
        InProgress,
        RewardReady,
        Completed
    }

    [DisallowMultipleComponent]
    public sealed class RiftDungeonController : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private PlayerTargeting targeting;
        [SerializeField] private PlayerBuildSystem build;
        [SerializeField] private CombatArenaReset worldArenaReset;

        [Header("Dungeon")]
        [SerializeField] private Transform worldEntrance;
        [SerializeField] private Transform dungeonSpawn;
        [SerializeField] private Transform rewardChest;
        [SerializeField] private Transform exitPortal;
        [SerializeField] private RiftDungeonEncounter[] encounters;
        [SerializeField, Min(1f)] private float interactionRange = 3f;
        [SerializeField, Min(0.2f)] private float deathResetDelay = 1.15f;

        private InputAction interactAction;
        private Vector3 worldReturnPosition;
        private Quaternion worldReturnRotation;
        private Transform activeCheckpoint;
        private float startedAt;
        private float completedAt;
        private Coroutine deathRoutine;

        public RiftDungeonState State { get; private set; } = RiftDungeonState.Outside;
        public bool IsInDungeon => State != RiftDungeonState.Outside;
        public int CurrentEncounterIndex { get; private set; } = -1;
        public int KillCount { get; private set; }
        public bool RewardClaimed { get; private set; }
        public string DungeonName => "Rift Crypt";
        public string DifficultyName => "Normal";
        public float ElapsedTime => State == RiftDungeonState.Outside
            ? 0f
            : (State == RiftDungeonState.InProgress || State == RiftDungeonState.RewardReady
                ? Time.unscaledTime - startedAt
                : completedAt - startedAt);
        public RiftDungeonEncounter CurrentEncounter => encounters != null &&
            CurrentEncounterIndex >= 0 && CurrentEncounterIndex < encounters.Length
                ? encounters[CurrentEncounterIndex]
                : null;
        public string ObjectiveText
        {
            get
            {
                if (State == RiftDungeonState.RewardReady)
                    return "Claim the Warden's Cache";
                if (State == RiftDungeonState.Completed)
                    return "Use the exit rift";
                RiftDungeonEncounter encounter = CurrentEncounter;
                return encounter != null ? $"Defeat {encounter.EncounterName}" : "Enter the crypt";
            }
        }
        public string InteractionPrompt
        {
            get
            {
                if (player == null)
                    return string.Empty;
                if (State == RiftDungeonState.Outside && IsNear(worldEntrance))
                    return "[E] Enter Rift Crypt";
                if (State == RiftDungeonState.RewardReady && IsNear(rewardChest))
                    return "[E] Open Warden's Cache";
                if (State == RiftDungeonState.Completed && IsNear(exitPortal))
                    return "[E] Return to the open world";
                return string.Empty;
            }
        }

        public event Action StateChanged;

        public void Configure(Transform playerTransform, Transform entrance, Transform spawn,
            Transform chest, Transform exit, RiftDungeonEncounter[] dungeonEncounters,
            CombatArenaReset arenaReset)
        {
            player = playerTransform;
            playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
            playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
            targeting = player != null ? player.GetComponent<PlayerTargeting>() : null;
            build = player != null ? player.GetComponent<PlayerBuildSystem>() : null;
            worldEntrance = entrance;
            dungeonSpawn = spawn;
            rewardChest = chest;
            exitPortal = exit;
            encounters = dungeonEncounters;
            worldArenaReset = arenaReset;
        }

        private void Awake()
        {
            if (player == null)
            {
                PlayerHealth found = FindAnyObjectByType<PlayerHealth>();
                if (found != null)
                    player = found.transform;
            }
            playerHealth ??= player != null ? player.GetComponent<PlayerHealth>() : null;
            playerCombat ??= player != null ? player.GetComponent<PlayerCombat>() : null;
            targeting ??= player != null ? player.GetComponent<PlayerTargeting>() : null;
            build ??= player != null ? player.GetComponent<PlayerBuildSystem>() : null;
            interactAction = new InputAction("Dungeon Interact", InputActionType.Button, "<Keyboard>/e");
            PrepareDungeon();
        }

        private void OnEnable()
        {
            interactAction.Enable();
            if (playerHealth != null)
                playerHealth.Died += HandlePlayerDied;
            CombatEvents.EnemyDefeatedDetailed += HandleEnemyDefeated;
        }

        private void OnDisable()
        {
            interactAction.Disable();
            if (playerHealth != null)
                playerHealth.Died -= HandlePlayerDied;
            CombatEvents.EnemyDefeatedDetailed -= HandleEnemyDefeated;
        }

        private void OnDestroy() => interactAction.Dispose();

        private void Update()
        {
            if (interactAction.WasPressedThisFrame())
                TryInteract();

            if (State != RiftDungeonState.InProgress)
                return;
            RiftDungeonEncounter encounter = CurrentEncounter;
            if (encounter != null && encounter.IsComplete)
                AdvanceEncounter();
        }

        public bool TryInteract()
        {
            if (State == RiftDungeonState.Outside && IsNear(worldEntrance))
            {
                EnterDungeon();
                return true;
            }
            if (State == RiftDungeonState.RewardReady && IsNear(rewardChest))
            {
                ClaimReward();
                return true;
            }
            if (State == RiftDungeonState.Completed && IsNear(exitPortal))
            {
                ExitDungeon();
                return true;
            }
            return false;
        }

        public void EnterDungeon()
        {
            if (player == null || dungeonSpawn == null)
                return;
            worldReturnPosition = player.position;
            worldReturnRotation = player.rotation;
            if (worldArenaReset != null)
                worldArenaReset.enabled = false;
            PrepareDungeon();
            KillCount = 0;
            RewardClaimed = false;
            startedAt = Time.unscaledTime;
            completedAt = 0f;
            State = RiftDungeonState.InProgress;
            CurrentEncounterIndex = 0;
            activeCheckpoint = dungeonSpawn;
            TeleportPlayer(dungeonSpawn.position, dungeonSpawn.rotation);
            CurrentEncounter?.Begin();
            StateChanged?.Invoke();
        }

        public void ClaimReward()
        {
            if (State != RiftDungeonState.RewardReady || RewardClaimed)
                return;
            RewardClaimed = true;
            build?.GrantRewardById("artifact.riftwarden-heart");
            completedAt = Time.unscaledTime;
            State = RiftDungeonState.Completed;
            if (rewardChest != null)
                rewardChest.gameObject.SetActive(false);
            if (exitPortal != null)
                exitPortal.gameObject.SetActive(true);
            StateChanged?.Invoke();
        }

        public void ExitDungeon()
        {
            if (State != RiftDungeonState.Completed)
                return;
            TeleportPlayer(worldReturnPosition, worldReturnRotation);
            targeting?.SetTarget(null);
            PrepareDungeon();
            State = RiftDungeonState.Outside;
            CurrentEncounterIndex = -1;
            activeCheckpoint = null;
            if (worldArenaReset != null)
                worldArenaReset.enabled = true;
            StateChanged?.Invoke();
        }

        private void AdvanceEncounter()
        {
            RiftDungeonEncounter completed = CurrentEncounter;
            completed?.Complete();
            if (encounters == null || CurrentEncounterIndex >= encounters.Length - 1)
            {
                completedAt = Time.unscaledTime;
                State = RiftDungeonState.RewardReady;
                if (rewardChest != null)
                    rewardChest.gameObject.SetActive(true);
                StateChanged?.Invoke();
                return;
            }

            CurrentEncounterIndex++;
            RiftDungeonEncounter next = CurrentEncounter;
            activeCheckpoint = next != null && next.Checkpoint != null ? next.Checkpoint : activeCheckpoint;
            next?.Begin();
            StateChanged?.Invoke();
        }

        private void PrepareDungeon()
        {
            if (encounters != null)
            {
                foreach (RiftDungeonEncounter encounter in encounters)
                    encounter?.Prepare();
            }
            if (rewardChest != null)
                rewardChest.gameObject.SetActive(false);
            if (exitPortal != null)
                exitPortal.gameObject.SetActive(false);
        }

        private void HandleEnemyDefeated(EnemyDefeatedEvent defeated)
        {
            if (State == RiftDungeonState.InProgress)
                KillCount++;
        }

        private void HandlePlayerDied()
        {
            if (State == RiftDungeonState.Outside || deathRoutine != null)
                return;
            deathRoutine = StartCoroutine(ResetAfterDeath());
        }

        private IEnumerator ResetAfterDeath()
        {
            yield return new WaitForSecondsRealtime(deathResetDelay);
            CurrentEncounter?.ResetEncounter();
            Transform checkpoint = activeCheckpoint != null ? activeCheckpoint : dungeonSpawn;
            if (checkpoint != null)
                TeleportPlayer(checkpoint.position, checkpoint.rotation);
            playerHealth?.ResetHealth();
            deathRoutine = null;
        }

        private void TeleportPlayer(Vector3 position, Quaternion rotation)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.SetPositionAndRotation(position, rotation);
            if (controller != null)
                controller.enabled = true;
            player.GetComponent<PhasebreakPlayerMovement>()?.ResetMotion();
            playerCombat?.ResetCombat();
            targeting?.SetTarget(null);
        }

        private bool IsNear(Transform point)
        {
            if (point == null || player == null || !point.gameObject.activeInHierarchy)
                return false;
            Vector3 delta = point.position - player.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= interactionRange * interactionRange;
        }
    }
}
