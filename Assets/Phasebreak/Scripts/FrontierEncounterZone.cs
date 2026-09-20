using System.Collections.Generic;
using UnityEngine;

namespace Phasebreak.Gameplay
{
    public sealed class FrontierEncounterZone : MonoBehaviour
    {
        [System.Serializable]
        public struct SpawnRole
        {
            public EnemyRole role;
            public EnemyRank rank;
        }
        [SerializeField] private string zoneId;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int count = 3;
        [SerializeField] private float radius = 10;
        [SerializeField] private float respawnDelay = 180;
        [SerializeField] private float safeRespawnDistance = 48;
        [SerializeField] private float activationDistance = 230;
        [SerializeField] private float unloadDistance = 300;
        [SerializeField] private int dangerTier = 1;
        [SerializeField] private Vector3[] authoredOffsets;
        [SerializeField] private SpawnRole[] authoredRoles;
        public string ZoneId => zoneId;
        public void ConfigureFormation(Vector3[] offsets) { authoredOffsets = offsets; count = offsets.Length; }
        public void ConfigureRoles(SpawnRole[] roles) { authoredRoles = roles; }
        private readonly List<MeleeEnemy> enemies = new List<MeleeEnemy>();
        private readonly List<float> deaths = new List<float>();
        private Transform player;
        public int Capacity => count;
        public int LivingCount
        {
            get
            {
                int living = 0;
                foreach (MeleeEnemy enemy in enemies)
                    if (enemy != null && enemy.IsAlive) living++;
                return living;
            }
        }

        public int DebugDefeatActive()
        {
            int defeated = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || !enemies[i].IsAlive) continue;
                enemies[i].DebugDefeatWithoutRewards();
                deaths[i] = Time.time;
                defeated++;
            }
            return defeated;
        }

        public void DebugReset()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null)
                {
                    enemies[i].gameObject.SetActive(false);
                    Destroy(enemies[i].gameObject);
                }
                enemies[i] = null;
                deaths[i] = -1f;
            }
        }

        public void Configure(string id, GameObject prefab, int population, float spread, float delay, int tier)
        {
            zoneId = id; enemyPrefab = prefab; count = population; radius = spread;
            respawnDelay = delay; dangerTier = tier;
        }

        private void Start()
        {
            player = FindAnyObjectByType<PlayerHealth>()?.transform;
            for (int i = 0; i < count; i++) { enemies.Add(null); deaths.Add(-1); }
        }

        private void Update()
        {
            if (player == null) { player = FindAnyObjectByType<PlayerHealth>()?.transform; return; }
            float distance = Vector3.Distance(player.position, transform.position);
            if (distance > unloadDistance)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null) continue;
                    if (!enemies[i].IsAlive && deaths[i] < 0) deaths[i] = Time.time;
                    Destroy(enemies[i].gameObject);
                    enemies[i] = null;
                }
                return;
            }
            if (distance > activationDistance) return;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive) continue;
                if (enemies[i] != null && deaths[i] < 0) deaths[i] = Time.time;
                if (deaths[i] >= 0 && Time.time - deaths[i] < respawnDelay) continue;
                if (deaths[i] >= 0 && distance < safeRespawnDistance) continue;
                if (enemies[i] != null) Destroy(enemies[i].gameObject);
                Spawn(i);
                deaths[i] = -1;
            }
        }

        private void Spawn(int index)
        {
            if (enemyPrefab == null) return;
            int hash = 0;
            foreach (char c in zoneId) hash = (hash * 31 + c) % 997;
            float angle = index * 2.39996f + hash * .11f;
            float distance = radius * (.38f + .52f * ((index % 3) / 2f));
            Vector3 point = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
            if (authoredOffsets != null && index < authoredOffsets.Length)
                point = transform.TransformPoint(authoredOffsets[index]);
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                Terrain[] terrains = Terrain.activeTerrains;
                foreach (Terrain candidate in terrains)
                {
                    Vector3 p = candidate.transform.position;
                    Vector3 s = candidate.terrainData.size;
                    if (point.x >= p.x && point.x <= p.x + s.x && point.z >= p.z && point.z <= p.z + s.z)
                    { terrain = candidate; break; }
                }
                point.y = terrain.SampleHeight(point) + terrain.transform.position.y + .15f;
            }
            GameObject enemy = Instantiate(enemyPrefab, point, Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0), transform);
            SpawnRole spawnRole = GetRole(index);
            enemies[index] = enemy.GetComponent<MeleeEnemy>();
            enemies[index].ConfigureRole(spawnRole.role, spawnRole.rank);
            enemy.name = spawnRole.role == EnemyRole.Zombie ? enemyPrefab.name :
                spawnRole.rank + " " + spawnRole.role;
        }

        private SpawnRole GetRole(int index)
        {
            if (authoredRoles != null && index < authoredRoles.Length) return authoredRoles[index];
            if (zoneId == "riftblade-practice") return default;
            if (index == count - 1)
                return new SpawnRole { role = EnemyRole.Skirmisher,
                    rank = dangerTier >= 3 ? EnemyRank.Elite : dangerTier >= 2 ? EnemyRank.Veteran : EnemyRank.Normal };
            if (dangerTier >= 2 && index == count - 2)
                return new SpawnRole { role = EnemyRole.Brute,
                    rank = dangerTier >= 3 ? EnemyRank.Veteran : EnemyRank.Normal };
            return default;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = dangerTier > 2 ? new Color(.6f, .2f, .8f) : new Color(.9f, .6f, .2f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
