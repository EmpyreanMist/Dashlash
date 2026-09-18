using System.Collections.Generic;
using Phasebreak.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Phasebreak.Editor
{
    public static class BuildPhase6Inventory
    {
        private const string Root = "Assets/Phasebreak/Data";

        [MenuItem("Phasebreak/Build Phase 6 Inventory")]
        public static void Build()
        {
            EnsureFolders();
            PhasebreakItemSetDefinition set = Asset<PhasebreakItemSetDefinition>($"{Root}/Sets/RiftstalkerCircuit.asset");
            set.id = "set.riftstalker"; set.displayName = "Riftstalker Circuit"; set.fantasy = "A cross-slot circuit that turns precision into phase momentum.";
            set.bonuses = new[] {
                Bonus(2, "Critical hits return 8 Energy.", BuildEffect.CritRestoresEnergy, 8f),
                Bonus(3, "Phase Lunge recharges 25% faster.", BuildEffect.PhaseLungeCooldown, .25f),
                Bonus(4, "Crushing Blow fractures nearby enemies.", BuildEffect.CrushingBlowCleave, 0f),
                Bonus(6, "Teleport kills restore Energy and Phase Lunge.", BuildEffect.TeleportKillRecovery, 30f)
            }; EditorUtility.SetDirty(set);

            List<PhasebreakItemDefinition> items = new();
            items.Add(Item("weapon.rift-iron", "Rift-Iron Edge", EquipmentSlot.PrimaryWeapon, ItemRarity.Rare, 5, ItemTag.Void | ItemTag.Crit, set, new BuildStats { power=.12f, criticalChance=.04f }, BuildEffect.None, 0, "A blade whose edge briefly vanishes between impacts."));
            items.Add(Item("hands.phasegrip", "Phasegrip Gauntlets", EquipmentSlot.Hands, ItemRarity.Rare, 5, ItemTag.Crit | ItemTag.Mobility, set, new BuildStats { attackSpeed=.09f, criticalChance=.03f }, BuildEffect.None, 0, "Hands focus attack speed and precise critical timing."));
            items.Add(Item("boots.wake", "Wake-Runner Boots", EquipmentSlot.Boots, ItemRarity.Rare, 5, ItemTag.Mobility, set, new BuildStats { movementSpeed=.1f }, BuildEffect.None, 0, "Boots tuned for aggressive movement."));
            items.Add(Item("core.riftheart", "Riftheart Core", EquipmentSlot.Core, ItemRarity.Epic, 7, ItemTag.Void | ItemTag.Energy, set, new BuildStats { power=.06f }, BuildEffect.PhaseLungeExtraCharge, 1, "Build defining: Phase Lunge gains a second charge."));
            items.Add(Item("relic.blinkwake", "Blinkwake Drive", EquipmentSlot.MobilityRelic, ItemRarity.Epic, 7, ItemTag.Mobility | ItemTag.Void, set, new BuildStats { movementSpeed=.06f }, BuildEffect.TeleportKillRecovery, 25, "Teleport kills restore Energy and refresh Phase Lunge."));
            items.Add(Item("sigil.emberglass", "Emberglass Sigil", EquipmentSlot.Sigil1, ItemRarity.Rare, 5, ItemTag.Fire | ItemTag.Crit, set, new BuildStats { criticalDamage=.18f }, BuildEffect.None, 0, "A volatile sigil; can occupy any Sigil socket."));
            items.Add(Item("artifact.riftwarden-heart", "Heart of the Rift Warden", EquipmentSlot.WildcardArtifact, ItemRarity.Mythic, 9, ItemTag.Void | ItemTag.Boss, null, new BuildStats { power=.1f, bossDamage=.12f }, BuildEffect.BossDamage | BuildEffect.CrushingBlowCleave, .12f, "Build defining: tag diversity adds boss damage and Crushing Blow fractures nearby enemies."));
            items.Add(Item("relic.keen-cell", "Keen Current Cell", EquipmentSlot.PowerRelic, ItemRarity.Epic, 6, ItemTag.Crit | ItemTag.Energy, null, new BuildStats { criticalChance=.05f }, BuildEffect.CritRestoresEnergy, 10, "Critical hits restore 10 Energy."));
            items.Add(Item("chest.bulwark", "Ashen Bulwark Shell", EquipmentSlot.Chest, ItemRarity.Rare, 5, ItemTag.Defense | ItemTag.Fire, null, new BuildStats { maxHealth=4, defense=.1f }, BuildEffect.None, 0, "A defensive shell that resists the first pressure spike."));
            items.Add(Item("relic.fracture", "Fracture Prism", EquipmentSlot.UtilityRelic, ItemRarity.Epic, 6, ItemTag.Bleed, null, new BuildStats { power=.05f }, BuildEffect.CrushingBlowCleave, 0, "Crushing Blow splinters into nearby enemies."));

            CombatAbilityDefinition[] abilities = {
                Ability("strike", "Strike", "1", 5, 2.4f, 0, 0, 0, 1.7f, .1f),
                Ability("crushing-blow", "Crushing Blow", "2", 10, 2.65f, 5, 30, .1f, 2.8f, .18f),
                Ability("phase-lunge", "Phase Lunge", "3", 6, 7, 8, 20, .05f, 8.5f, .08f)
            };

            GameObject player = GameObject.Find("Player"); PhasebreakHud hud = Object.FindAnyObjectByType<PhasebreakHud>();
            if (player == null || hud == null) { Debug.LogError("Phase 6 requires the active dashlash gameplay scene."); return; }
            PlayerBuildSystem build = player.GetComponent<PlayerBuildSystem>() ?? player.AddComponent<PlayerBuildSystem>();
            SerializedObject buildSo = new(build); buildSo.FindProperty("itemCatalog").arraySize = items.Count; buildSo.FindProperty("startingInventory").arraySize = items.Count;
            for (int i=0;i<items.Count;i++) { buildSo.FindProperty("itemCatalog").GetArrayElementAtIndex(i).objectReferenceValue=items[i]; buildSo.FindProperty("startingInventory").GetArrayElementAtIndex(i).objectReferenceValue=items[i]; } buildSo.ApplyModifiedPropertiesWithoutUndo();
            PlayerCombat combat = player.GetComponent<PlayerCombat>(); SerializedObject combatSo = new(combat); combatSo.FindProperty("abilityDefinitions").arraySize=abilities.Length;
            for(int i=0;i<abilities.Length;i++) combatSo.FindProperty("abilityDefinitions").GetArrayElementAtIndex(i).objectReferenceValue=abilities[i]; combatSo.ApplyModifiedPropertiesWithoutUndo();
            if (hud.GetComponent<PhasebreakInventoryHud>() == null) hud.gameObject.AddComponent<PhasebreakInventoryHud>();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes(); AssetDatabase.SaveAssets();
            Debug.Log($"PHASE6_BUILT items={items.Count} abilities={abilities.Length} setBonuses={set.bonuses.Length}");
        }

        private static SetBonusDefinition Bonus(int pieces, string description, BuildEffect effect, float value) => new() { pieces=pieces, description=description, effects=effect, effectValue=value };
        private static PhasebreakItemDefinition Item(string id,string name,EquipmentSlot slot,ItemRarity rarity,int level,ItemTag tags,PhasebreakItemSetDefinition set,BuildStats stats,BuildEffect effect,float value,string description)
        { PhasebreakItemDefinition item=Asset<PhasebreakItemDefinition>($"{Root}/Items/{id.Replace('.','_')}.asset"); item.id=id;item.displayName=name;item.slot=slot;item.rarity=rarity;item.itemLevel=level;item.tags=tags;item.itemSet=set;item.stats=stats;item.effects=effect;item.effectValue=value;item.description=description;EditorUtility.SetDirty(item);return item; }
        private static CombatAbilityDefinition Ability(string id,string name,string key,float damage,float range,float cooldown,float cost,float crit,float impulse,float windup)
        { CombatAbilityDefinition a=Asset<CombatAbilityDefinition>($"{Root}/Abilities/{id}.asset");a.id=id;a.displayName=name;a.key=key;a.damage=damage;a.range=range;a.cooldown=cooldown;a.resourceCost=cost;a.criticalBonus=crit;a.impulse=impulse;a.windup=windup;EditorUtility.SetDirty(a);return a; }
        private static T Asset<T>(string path) where T:ScriptableObject { T value=AssetDatabase.LoadAssetAtPath<T>(path); if(value!=null)return value; if(AssetDatabase.LoadMainAssetAtPath(path)!=null) AssetDatabase.DeleteAsset(path); value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);return value; }
        private static void EnsureFolders() { Folder("Assets/Phasebreak","Data");Folder(Root,"Items");Folder(Root,"Sets");Folder(Root,"Abilities"); }
        private static void Folder(string parent,string name) { if(!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent,name); }
    }
}
