#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class V02EncounterRosterPass
    {
        private const string ScenePath = "Assets/Phasebreak/Scenes/StarterZone_V2.unity";

        [MenuItem("Phasebreak/Enemies/Author V0.2 Encounter Roster")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var formations = new Dictionary<string, FrontierEncounterZone.SpawnRole[]>
            {
                ["opening-stragglers"] = Roles(Z(), Z(), Z()),
                ["broken-cart-ambush"] = Roles(Z(), Z(), C()),
                ["abandoned-road-camp"] = Roles(Z(), C(), Z(), B()),
                ["east-road-brute-guard"] = Roles(Z(), A(), B(EnemyRank.Veteran)),
                ["kings-road-imp-overlook"] = Roles(Z(), C(), S(), A()),
                ["ruined-shrine"] = Roles(Z(), C(), B(), A(EnemyRank.Veteran)),
                ["old-fort-elite-watch"] = Roles(Z(), C(), A(EnemyRank.Elite, "Veyra the Rift Seer")),
                ["rift-crypt-approach"] = Roles(Z(), C(), S(), B(EnemyRank.Veteran, "Mordek the Gatebreaker"))
            };
            int updated = 0;
            foreach (var zone in UnityEngine.Object.FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None))
            {
                if (!formations.TryGetValue(zone.ZoneId, out var roles)) continue;
                if (zone.Capacity != roles.Length) throw new InvalidOperationException("Encounter capacity mismatch: " + zone.ZoneId);
                zone.ConfigureRoles(roles);
                EditorUtility.SetDirty(zone);
                updated++;
            }
            if (updated != formations.Count) throw new InvalidOperationException($"Updated {updated}/{formations.Count} encounter zones");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("V02_ENCOUNTER_ROSTER_AUTHORED: " + updated + " zones");
        }

        private static FrontierEncounterZone.SpawnRole[] Roles(params FrontierEncounterZone.SpawnRole[] roles) => roles;
        private static FrontierEncounterZone.SpawnRole R(EnemyRole role, EnemyRank rank = EnemyRank.Normal, string name = null) =>
            new FrontierEncounterZone.SpawnRole { role = role, rank = rank, uniqueName = name };
        private static FrontierEncounterZone.SpawnRole Z() => R(EnemyRole.Zombie);
        private static FrontierEncounterZone.SpawnRole B(EnemyRank rank = EnemyRank.Normal, string name = null) => R(EnemyRole.Brute, rank, name);
        private static FrontierEncounterZone.SpawnRole S() => R(EnemyRole.Skirmisher);
        private static FrontierEncounterZone.SpawnRole A(EnemyRank rank = EnemyRank.Normal, string name = null) => R(EnemyRole.Caster, rank, name);
        private static FrontierEncounterZone.SpawnRole C() => R(EnemyRole.Charger);
    }
}
#endif
