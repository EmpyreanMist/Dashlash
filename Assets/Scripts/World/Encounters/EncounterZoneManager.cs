using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Dashlash.World.Encounters
{
    public class EncounterZoneManager : MonoBehaviour
    {
        public static EncounterZoneManager Instance { get; private set; }
        public List<AmbientWildernessZone> ambientZones = new List<AmbientWildernessZone>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RegisterAmbientZones();
        }

        private void RegisterAmbientZones()
        {
            ambientZones = FindObjectsOfType<AmbientWildernessZone>().ToList();
        }
    }
}