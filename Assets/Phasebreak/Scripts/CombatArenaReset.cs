using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Phasebreak.Gameplay
{
    public sealed class CombatArenaReset : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private MeleeEnemy[] enemies;
        [SerializeField, Min(0f)] private float deathResetDelay = 1.1f;

        private InputAction resetAction;
        private Vector3 playerSpawnPosition;
        private Quaternion playerSpawnRotation;
        private Coroutine pendingReset;

        public int ResetCount { get; private set; }
        public int EnemyCapacity => enemies != null ? enemies.Length : 0;

        public void Configure(Transform playerTransform, PlayerHealth health, MeleeEnemy[] arenaEnemies)
        {
            player = playerTransform;
            playerHealth = health;
            enemies = arenaEnemies;
        }

        private void Awake()
        {
            if (player == null && playerHealth != null)
                player = playerHealth.transform;
            if (playerHealth == null && player != null)
                playerHealth = player.GetComponent<PlayerHealth>();
            if (enemies == null || enemies.Length == 0)
                enemies = FindObjectsByType<MeleeEnemy>();

            if (player != null)
            {
                playerSpawnPosition = player.position;
                playerSpawnRotation = player.rotation;
            }
            resetAction = new InputAction("Reset Arena", InputActionType.Button, "<Keyboard>/r");
        }

        private void OnEnable()
        {
            resetAction.Enable();
            if (playerHealth != null)
                playerHealth.Died += HandlePlayerDied;
        }

        private void OnDisable()
        {
            resetAction.Disable();
            if (playerHealth != null)
                playerHealth.Died -= HandlePlayerDied;
            if (pendingReset != null)
            {
                StopCoroutine(pendingReset);
                pendingReset = null;
            }
        }

        private void OnDestroy() => resetAction.Dispose();

        private void Update()
        {
            if (!GameplayInputFocus.GameplayInputBlocked && resetAction.WasPressedThisFrame())
                ResetArena();
        }

        public void ResetArena()
        {
            if (pendingReset != null)
            {
                StopCoroutine(pendingReset);
                pendingReset = null;
            }

            CharacterController controller = player != null ? player.GetComponent<CharacterController>() : null;
            if (controller != null)
                controller.enabled = false;
            if (player != null)
                player.SetPositionAndRotation(playerSpawnPosition, playerSpawnRotation);
            if (controller != null)
                controller.enabled = true;

            player?.GetComponent<PhasebreakPlayerMovement>()?.ResetMotion();
            player?.GetComponent<PlayerCombat>()?.ResetCombat();
            playerHealth?.ResetHealth();
            DebugResetEnemies();
            ResetCount++;
        }

        public void DebugResetEnemies()
        {
            if (enemies == null) return;
            foreach (MeleeEnemy enemy in enemies)
                enemy?.ResetEnemy();
        }

        private void HandlePlayerDied()
        {
            if (pendingReset != null)
                StopCoroutine(pendingReset);
            pendingReset = StartCoroutine(ResetAfterDelay());
        }

        private IEnumerator ResetAfterDelay()
        {
            yield return new WaitForSecondsRealtime(deathResetDelay);
            pendingReset = null;
            ResetArena();
        }
    }
}
