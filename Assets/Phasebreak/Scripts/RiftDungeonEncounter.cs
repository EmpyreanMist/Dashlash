using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RiftDungeonEncounter : MonoBehaviour
    {
        [SerializeField] private string encounterName = "Crypt Guardians";
        [SerializeField] private MeleeEnemy[] enemies;
        [SerializeField] private GameObject gate;
        [SerializeField] private Transform checkpoint;
        [SerializeField] private bool bossEncounter;

        public string EncounterName => encounterName;
        public bool IsBossEncounter => bossEncounter;
        public Transform Checkpoint => checkpoint;
        public MeleeEnemy[] Enemies => enemies;
        public int RemainingEnemies
        {
            get
            {
                int remaining = 0;
                if (enemies == null)
                    return remaining;
                foreach (MeleeEnemy enemy in enemies)
                {
                    if (enemy != null && enemy.IsAlive)
                        remaining++;
                }
                return remaining;
            }
        }
        public int TotalEnemies => enemies != null ? enemies.Length : 0;
        public bool IsComplete => RemainingEnemies == 0;

        public void Configure(string title, MeleeEnemy[] encounterEnemies, GameObject encounterGate,
            Transform respawnCheckpoint, bool isBoss)
        {
            encounterName = title;
            enemies = encounterEnemies;
            gate = encounterGate;
            checkpoint = respawnCheckpoint;
            bossEncounter = isBoss;
        }

        public void Prepare()
        {
            SetGateLocked(true);
            if (enemies == null)
                return;
            foreach (MeleeEnemy enemy in enemies)
            {
                if (enemy == null)
                    continue;
                enemy.SetRespawnEnabled(false);
                enemy.gameObject.SetActive(false);
            }
        }

        public void Begin()
        {
            SetGateLocked(true);
            if (enemies == null)
                return;
            foreach (MeleeEnemy enemy in enemies)
            {
                if (enemy == null)
                    continue;
                enemy.gameObject.SetActive(true);
                enemy.ResetEnemy();
            }
        }

        public void Complete() => SetGateLocked(false);

        public void ResetEncounter()
        {
            if (enemies == null)
                return;
            foreach (MeleeEnemy enemy in enemies)
            {
                if (enemy == null)
                    continue;
                enemy.gameObject.SetActive(true);
                enemy.ResetEnemy();
            }
            SetGateLocked(true);
        }

        private void SetGateLocked(bool locked)
        {
            if (gate != null)
                gate.SetActive(locked);
        }
    }
}
