using UnityEngine;
using System.Collections.Generic;

namespace Dashlash.World.Encounters
{
    public static class ExclusionValidator
    {
        private static List<Collider> exclusionZones = new List<Collider>();

        public static void Initialize()
        {
            exclusionZones = new List<Collider>(GameObject.FindGameObjectsWithTag("ExclusionZone"));
        }

        public static bool IsInExclusionZone(Vector3 position)
        {
            foreach (var zone in exclusionZones)
            {
                if (zone.bounds.Contains(position))
                {
                    return true;
                }
            }
            return false;
        }
    }
}