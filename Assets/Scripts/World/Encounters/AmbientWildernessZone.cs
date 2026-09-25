using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Dashlash.World.Encounters
{
    public class AmbientWildernessZone : FrontierEncounterZone
    {
        [Header("Ambient Population Settings")]
        public int minPopulation = 3;
        public int maxPopulation = 8;
        public int desiredPopulation = 5;
        public float respawnDelayMin = 120f;
        public float respawnDelayMax = 300f;
        public float activationDistance = 200f;
        public float unloadDistance = 250f;
        public float minRespawnDistanceFromPlayer = 50f;

        [Header("Allowed Roles/Species")]
        public List<EncounterRole> allowedRoles = new List<EncounterRole>();
        public List<Species> allowedSpecies = new List<Species>();
        public Dictionary<EncounterRole, float> roleWeights = new Dictionary<EncounterRole, float>();

        private List<EncounterParticipant> activeParticipants = new List<EncounterParticipant>();
        private float nextRespawnTime = 0f;
        private bool isActive = false;

        protected override void Awake()
        {
            base.Awake();
            InitializeRoleWeights();
        }

        private void InitializeRoleWeights()
        {
            foreach (var role in allowedRoles)
            {
                roleWeights[role] = 1f; // Default weight
            }
        }

        protected override void Update()
        {
            base.Update();
            CheckActivationStatus();
            HandleRespawns();
        }

        private void CheckActivationStatus()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, PlayerManager.Instance.transform.position);
            isActive = distanceToPlayer <= activationDistance;

            if (isActive && activeParticipants.Count == 0)
            {
                SpawnPopulation();
            }
        }

        private void SpawnPopulation()
        {
            int spawnCount = Mathf.Clamp(
                Random.Range(minPopulation, maxPopulation + 1),
                minPopulation,
                maxPopulation
            );

            for (int i = 0; i < spawnCount; i++)
            {
                EncounterParticipant participant = CreateParticipant();
                if (participant != null && IsValidSpawnPosition(participant))
                {
                    activeParticipants.Add(participant);
                    participant.Spawn();
                }
            }
        }

        private EncounterParticipant CreateParticipant()
        {
            if (allowedRoles.Count == 0 || allowedSpecies.Count == 0) return null;

            // Select role based on weights
            EncounterRole selectedRole = SelectRoleByWeight();
            Species selectedSpecies = allowedSpecies[Random.Range(0, allowedSpecies.Count)];

            return new EncounterParticipant(
                selectedRole,
                selectedSpecies,
                transform.position + Random.insideUnitSphere * 10f,
                this
            );
        }

        private EncounterRole SelectRoleByWeight()
        {
            float totalWeight = roleWeights.Sum(w => w.Value);
            float randomPoint = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var role in roleWeights.Keys.ToList())
            {
                cumulativeWeight += roleWeights[role];
                if (randomPoint <= cumulativeWeight) return role;
            }

            return allowedRoles[0]; // Fallback
        }

        private bool IsValidSpawnPosition(EncounterParticipant participant)
        {
            Vector3 spawnPos = participant.transform.position;
            float distanceToPlayer = Vector3.Distance(spawnPos, PlayerManager.Instance.transform.position);

            // Check terrain/slope/water
            if (!TerrainValidator.IsSafeSpawn(spawnPos)) return false;

            // Check exclusion zones
            if (ExclusionValidator.IsInExclusionZone(spawnPos)) return false;

            // Check minimum distance from player
            if (distanceToPlayer < minRespawnDistanceFromPlayer) return false;

            // Check collision
            if (Physics.CheckSphere(spawnPos, 1.5f, LayerMask.GetMask("Obstacle"))) return false;

            return true;
        }

        private void HandleRespawns()
        {
            if (!isActive || activeParticipants.Count == 0) return;

            if (Time.time >= nextRespawnTime)
            {
                foreach (var participant in activeParticipants.ToList())
                {
                    if (!participant.IsAlive())
                    {
                        participant.Despawn();
                        activeParticipants.Remove(participant);
                    }
                }

                if (activeParticipants.Count < desiredPopulation)
                {
                    SpawnPopulation();
                    nextRespawnTime = Time.time + Random.Range(respawnDelayMin, respawnDelayMax);
                }
            }
        }

        public override void OnParticipantDeath(EncounterParticipant participant)
        {
            base.OnParticipantDeath(participant);
            activeParticipants.Remove(participant);
        }
    }
}