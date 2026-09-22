#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Editor
{
    public static class ConfigureOpenWorldRecovery
    {
        public static void Apply()
        {
            if (Application.isPlaying || SceneManager.GetActiveScene().path != "Assets/Phasebreak/Scenes/StarterZone_V2.unity")
                throw new InvalidOperationException("Open StarterZone_V2 outside Play Mode.");
            PlayerHealth player = UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
            var points = new List<OpenWorldCheckpoint>();
            Transform root = GameObject.Find("Open World Recovery Points")?.transform;
            if (root == null) root = new GameObject("Open World Recovery Points").transform;
            OpenWorldCheckpoint start = Point("Frontier Trailhead", player.transform.position, player.transform.rotation, null);
            foreach (QuestLocation location in UnityEngine.Object.FindObjectsByType<QuestLocation>().OrderBy(x => x.Id))
            {
                if (location.Id != "northgate" && location.Id != "westmere" && location.Id != "eastwatch") continue;
                // Clear ground beside the settlement center, away from Eastwatch's central tower.
                Vector3 arrival = location.transform.position + new Vector3(-12f, 0f, -12f);
                foreach (Terrain terrain in Terrain.activeTerrains)
                {
                    Vector3 local = arrival - terrain.transform.position;
                    Vector3 size = terrain.terrainData.size;
                    if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z)
                        arrival.y = terrain.SampleHeight(arrival) + terrain.transform.position.y + 1.5f;
                }
                points.Add(Point(location.DisplayName + " Recovery", arrival, Quaternion.identity, location));
            }
            OpenWorldRecovery recovery = player.GetComponent<OpenWorldRecovery>();
            if (recovery == null) recovery = player.gameObject.AddComponent<OpenWorldRecovery>();
            recovery.Configure(start, points.ToArray(), UnityEngine.Object.FindAnyObjectByType<RiftDungeonController>());
            EditorUtility.SetDirty(recovery);

            OpenWorldCheckpoint Point(string name, Vector3 position, Quaternion rotation, QuestLocation location)
            {
                Transform point = root.Find(name);
                if (point == null) { point = new GameObject(name).transform; point.SetParent(root); }
                point.SetPositionAndRotation(position, rotation);
                OpenWorldCheckpoint checkpoint = point.GetComponent<OpenWorldCheckpoint>();
                if (checkpoint == null) checkpoint = point.gameObject.AddComponent<OpenWorldCheckpoint>();
                checkpoint.Configure(location);
                EditorUtility.SetDirty(checkpoint);
                return checkpoint;
            }
        }
    }
}
#endif
