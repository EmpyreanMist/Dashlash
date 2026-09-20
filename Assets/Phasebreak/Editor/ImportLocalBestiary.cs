#if UNITY_EDITOR
using System;
using System.IO;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class ImportLocalBestiary
    {
        private const string Destination = "Assets/Phasebreak/LocalMonsters/Resources/LocalMonsters";

        [MenuItem("Phasebreak/Enemies/Import Local Bestiary")]
        private static void ChooseFolder()
        {
            string suggested = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Bestiary - Dungeon Monsters Kit[Standard]");
            string folder = EditorUtility.OpenFolderPanel("Select local Bestiary - Dungeon Monsters Kit", suggested, "");
            if (string.IsNullOrEmpty(folder)) return;
            try { ImportFrom(folder); }
            catch (Exception exception) { Debug.LogError("Bestiary import failed: " + exception.Message); }
        }

        public static void ImportFrom(string sourceRoot)
        {
            string modelRoot = Path.Combine(sourceRoot, "Exports", "FBX (Unity)");
            string textureRoot = Path.Combine(sourceRoot, "Textures");
            string license = Path.Combine(sourceRoot, "License_Standard.txt");
            string[] species = { "Imp", "Puglin" };
            foreach (string name in species)
            {
                Require(Path.Combine(modelRoot, name + ".fbx"));
                for (int tier = 1; tier <= 3; tier++)
                    Require(Path.Combine(textureRoot, $"T_{name}_BaseColor_{tier}.png"));
                Require(Path.Combine(textureRoot, $"T_{name}_Normal.png"));
            }
            Require(license);
            Directory.CreateDirectory(Destination);
            File.Copy(license, Path.Combine(Destination, "License_Standard.txt"), true);
            foreach (string name in species)
            {
                File.Copy(Path.Combine(modelRoot, name + ".fbx"), Path.Combine(Destination, name + ".fbx"), true);
                for (int tier = 1; tier <= 3; tier++)
                {
                    string file = $"T_{name}_BaseColor_{tier}.png";
                    File.Copy(Path.Combine(textureRoot, file), Path.Combine(Destination, file), true);
                }
                string normal = $"T_{name}_Normal.png";
                File.Copy(Path.Combine(textureRoot, normal), Path.Combine(Destination, normal), true);
            }
            AssetDatabase.Refresh();
            foreach (string name in species)
            {
                string modelPath = Destination + "/" + name + ".fbx";
                if (AssetImporter.GetAtPath(modelPath) is ModelImporter model)
                {
                    model.animationType = ModelImporterAnimationType.Generic;
                    model.importAnimation = true;
                    model.SaveAndReimport();
                }
                string normalPath = Destination + $"/T_{name}_Normal.png";
                if (AssetImporter.GetAtPath(normalPath) is TextureImporter texture)
                {
                    texture.textureType = TextureImporterType.NormalMap;
                    texture.SaveAndReimport();
                }
            }
            EnsureProfiles();
            Debug.Log("Local Bestiary imported for this checkout. Raw assets are excluded from Git.");
        }

        public static void EnsureProfiles()
        {
            const string folder = "Assets/Phasebreak/Resources/EnemyRoles";
            Directory.CreateDirectory(folder);
            CreateRole(folder + "/Brute.asset", EnemyRole.Brute, "Puglin", 85, 3, 28,
                2.25f, 10f, 18f, 2.25f, 1.15f, .2f, 1.35f);
            CreateRole(folder + "/Skirmisher.asset", EnemyRole.Skirmisher, "Imp", 24, 2, 17,
                5.2f, 16f, 25f, 11f, .8f, .1f, 1.3f);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateRole(string path, EnemyRole role, string model, int health, int damage,
            int experience, float moveSpeed, float awareness, float leash, float range, float windup,
            float active, float recovery)
        {
            if (AssetDatabase.LoadAssetAtPath<EnemyRoleDefinition>(path) != null) return;
            EnemyRoleDefinition profile = ScriptableObject.CreateInstance<EnemyRoleDefinition>();
            profile.role = role;
            profile.displayName = model;
            profile.localModelName = model;
            profile.health = health;
            profile.damage = damage;
            profile.experience = experience;
            profile.moveSpeed = moveSpeed;
            profile.awarenessRange = awareness;
            profile.leashRange = leash;
            profile.attackRange = range;
            profile.windup = windup;
            profile.active = active;
            profile.recovery = recovery;
            profile.ranks = new[]
            {
                new EnemyRoleDefinition.RankTuning { rank = EnemyRank.Normal, healthMultiplier = 1f,
                    damageMultiplier = 1f, experienceMultiplier = 1f, scaleMultiplier = 1f, colorVariant = 1 },
                new EnemyRoleDefinition.RankTuning { rank = EnemyRank.Veteran, healthMultiplier = 1.4f,
                    damageMultiplier = 1.2f, experienceMultiplier = 1.2f, scaleMultiplier = 1.07f, colorVariant = 2 },
                new EnemyRoleDefinition.RankTuning { rank = EnemyRank.Elite, healthMultiplier = 2f,
                    damageMultiplier = 1.4f, experienceMultiplier = 1.5f, scaleMultiplier = 1.16f, colorVariant = 3 }
            };
            AssetDatabase.CreateAsset(profile, path);
        }

        private static void Require(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Required local Bestiary file missing", path);
        }
    }
}
#endif
