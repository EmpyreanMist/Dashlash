using UnityEngine;

namespace Phasebreak.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class RiftDungeonEncounter : MonoBehaviour
    {
        [SerializeField] private string encounterName = "Crypt Guardians";
        [SerializeField] private MeleeEnemy[] enemies;
        [SerializeField] private EnemyRole[] enemyRoles;
        [SerializeField] private GameObject gate;
        [SerializeField] private Transform checkpoint;
        [SerializeField] private bool bossEncounter;
        private bool rolesApplied;

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
            Transform respawnCheckpoint, bool isBoss, EnemyRole[] roles = null)
        {
            encounterName = title;
            enemies = encounterEnemies;
            enemyRoles = roles;
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
            for (int i = 0; i < enemies.Length; i++)
            {
                MeleeEnemy enemy = enemies[i];
                if (enemy == null)
                    continue;
                enemy.gameObject.SetActive(true);
                if (!rolesApplied && enemyRoles != null && i < enemyRoles.Length &&
                    enemyRoles[i] != EnemyRole.Zombie)
                {
                    enemy.ConfigureRole(enemyRoles[i], EnemyRank.Normal);
                    Transform body = enemy.transform.Find("Body");
                    Transform blade = enemy.transform.Find("Blade");
                    if (body != null) body.gameObject.SetActive(false);
                    if (blade != null) blade.gameObject.SetActive(false);
                }
                enemy.ResetEnemy();
                enemy.GetComponent<RiftWardenBoss>()?.ResetForEncounter();
            }
            rolesApplied = true;
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
                enemy.GetComponent<RiftWardenBoss>()?.ResetForEncounter();
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
