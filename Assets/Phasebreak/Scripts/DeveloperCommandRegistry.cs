using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Phasebreak.Gameplay
{
    // Local developer tooling. It does not confer multiplayer authority.
    public sealed class DeveloperCommandRegistry
    {
        public sealed class Command
        {
            public readonly string Name, Usage, Description, Category;
            public readonly string[] Aliases;
            public readonly bool DeveloperOnly;
            public readonly Func<string[], string> Execute;

            public Command(string name, string usage, string description, string category,
                bool developerOnly, Func<string[], string> execute, params string[] aliases)
            {
                Name = name; Usage = usage; Description = description; Category = category;
                DeveloperOnly = developerOnly; Execute = execute; Aliases = aliases;
            }
        }

        private readonly Dictionary<string, Command> lookup = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Command> commands = new();
        private readonly PhasebreakChatHub chat;
        private PlayerHealth Health => UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();
        private PhasebreakPlayerMovement Movement => UnityEngine.Object.FindAnyObjectByType<PhasebreakPlayerMovement>();
        private PlayerCombat Combat => UnityEngine.Object.FindAnyObjectByType<PlayerCombat>();
        private PlayerProgression Progression => UnityEngine.Object.FindAnyObjectByType<PlayerProgression>();
        private RiftDungeonController Dungeon => UnityEngine.Object.FindAnyObjectByType<RiftDungeonController>();
        public IEnumerable<string> Names => lookup.Where(pair => Visible(pair.Value)).Select(pair => pair.Key).OrderBy(x => x);

        private static bool Visible(Command command)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return !command.DeveloperOnly;
#endif
        }

        public DeveloperCommandRegistry(PhasebreakChatHub owner)
        {
            chat = owner;
            Register(new("help", "/help [command]", "List commands or explain one command.", "General", false, Help, "?"));
            Register(new("clear", "/clear", "Clear local chat scrollback.", "General", false, Clear));
            Register(new("godmode", "/godmode [on|off]", "Set invulnerability; G + left click teleports to the cursor hit while enabled.", "Player", true, GodMode, "god"));
            Register(new("heal", "/heal", "Restore full health.", "Player", true, Heal));
            Register(new("energy", "/energy [amount]", "Set Energy; omit amount to refill.", "Player", true, Energy));
            Register(new("damage", "/damage <amount>", "Apply ordinary mitigated damage.", "Player", true, Damage));
            Register(new("kill", "/kill", "Invoke the real death flow, including in godmode.", "Player", true, Kill));
            Register(new("respawn", "/respawn", "Recover at the current checkpoint or world spawn.", "Player", true, Respawn));
            Register(new("unstuck", "/unstuck", "Return to the last grounded point or spawn.", "Player", true, Unstuck));
            Register(new("speed", "/speed [multiplier|reset]", "Set movement speed from 0.25 to 5; omit value to show status.", "Movement", true, Speed));
            Register(new("fly", "/fly [on|off]", "Fly with Space up and Q or Ctrl down; omit value to show status.", "Movement", true, Fly));
            Register(new("noclip", "/noclip [on|off]", "Pass through collision; Space moves up, Q or Ctrl down.", "Movement", true, NoClip));
            Register(new("where", "/where", "Show scene, position and nearest landmark.", "World", false, Where));
            Register(new("coords", "/coords [on|off]", "Show a coordinate overlay; omit value to show status.", "World", true, Coords));
            Register(new("tp", "/tp <location> | /tp <x> <y> <z>", "Teleport to a named landmark or safe world coordinates.", "World", true, Teleport, "teleport"));
            Register(new("fps", "/fps [on|off]", "Show an FPS overlay; omit value to show status.", "Debug", true, Fps));
            Register(new("hud", "/hud [on|off]", "Show or hide the game HUD while keeping chat accessible.", "Debug", true, Hud));
            Register(new("enemies", "/enemies", "Count living enemies and authored encounter capacity.", "Debug", true, Enemies));
            Register(new("killall", "/killall", "Defeat active world enemies without XP or loot.", "Debug", true, KillAll));
            Register(new("resetencounters", "/resetencounters", "Reset encounters through their owners.", "Debug", true, ResetEncounters));
            Register(new("xp", "/xp <amount>", "Grant nonnegative XP through progression.", "Progression", true, Xp));
            Register(new("level", "/level <level>", "Set a level within the current progression range.", "Progression", true, Level));
            Register(new("riftbuild", "/riftbuild apply", "Save a level-10 Riftblade test loadout with existing talents and six Circuit items; preserve other specialization talents.", "Progression", true, RiftBuild));
            Register(new("riftpack", "/riftpack", "Reset the Riftblade practice pack and travel to its starting point.", "Debug", true, RiftPack));
        }

        private string RiftBuild(string[] args)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Application.isPlaying) return "Enter Play Mode to apply the Riftblade loadout.";
            if (args.Length != 1 || args[0] != "apply") return "Usage: /riftbuild apply — saves level, talents and equipment to this character.";
            PlayerBuildSystem build = Combat != null ? Combat.GetComponent<PlayerBuildSystem>() : null;
            TalentSystem talents = Combat != null ? Combat.GetComponent<TalentSystem>() : null;
            if (build == null || talents == null || Progression == null) return Missing;
            SpecializationChangeResult specialization = build.SetSpecialization(Specialization.Riftblade);
            if (specialization == SpecializationChangeResult.AbilityInProgress)
                return "Finish the current ability before applying the Riftblade loadout.";
            if (specialization == SpecializationChangeResult.Defeated)
                return "Recover before applying the Riftblade loadout.";
            if (specialization is not (SpecializationChangeResult.Changed or SpecializationChangeResult.AlreadyActive))
                return "Riftblade specialization is unavailable.";
            Progression.SetDebugLevel(Progression.MaximumLevel);
            string[] path = { "phase-efficiency", "lunge-mastery", "rift-momentum", "echo-step", "rift-execution", "void-circuit" };
            foreach (string id in path)
            {
                TalentNodeDefinition node = talents.GetNode("riftblade-" + id);
                if (node == null) return "Missing Riftblade talent: " + id;
                while (talents.GetRank(node.id) < node.maximumRank)
                {
                    TalentPurchaseResult result = talents.Purchase(node);
                    if (result != TalentPurchaseResult.Purchased) return "Talent setup stopped: " + result;
                }
            }
            string[] items = { "weapon.rift-iron", "hands.phasegrip", "boots.wake", "core.riftheart", "relic.blinkwake", "sigil.emberglass" };
            foreach (string id in items)
            {
                if (Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Any(slot => build.GetEquipped(slot)?.id == id)) continue;
                PhasebreakItemDefinition item = build.Inventory.FirstOrDefault(i => i.id == id);
                if (item == null)
                {
                    if (!build.GrantRewardById(id)) return "Missing test equipment: " + id;
                    item = build.Inventory.First(i => i.id == id);
                }
                build.Equip(item);
            }
            Combat.ResetCombat();
            Health?.ResetHealth();
            return "Riftblade loadout saved. /riftpack to practice. Phase Dash primes Momentum; Phase Lunge kills return Energy and one charge. No godmode enabled.";
#else
            return "Developer commands require an Editor or Development Build.";
#endif
        }

        private string RiftPack(string[] args)
        {
            if (!Application.isPlaying) return "Enter Play Mode to practice the encounter.";
            if (!NoArgs(args)) return "Usage: /riftpack";
            FrontierEncounterZone zone = UnityEngine.Object.FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None)
                .FirstOrDefault(z => z.ZoneId == "riftblade-practice");
            if (zone == null || Movement == null) return "Riftblade practice pack is available in StarterZone_V2.";
            Combat?.GetComponent<PlayerTargeting>()?.SetTarget(null);
            zone.DebugReset();
            Vector3 point = zone.transform.position + Vector3.back * 5f;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 local = point - terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (local.x >= 0 && local.z >= 0 && local.x <= size.x && local.z <= size.z)
                    point.y = terrain.SampleHeight(point) + terrain.transform.position.y + .2f;
            }
            Movement.DebugTeleport(point, Quaternion.identity);
            Combat?.ResetCombat();
            return "Rift practice: 12 zombies in a winding lane. Dash to prime Momentum, then Lunge between kills; soften targets if undergeared. Three seconds between kills.";
        }

        private void Register(Command command)
        {
            foreach (string name in new[] { command.Name }.Concat(command.Aliases))
            {
                if (lookup.ContainsKey(name)) throw new InvalidOperationException("Duplicate command: " + name);
                lookup.Add(name, command);
            }
            commands.Add(command);
        }

        public string Run(string raw)
        {
            if (!TryTokenize(raw.TrimStart('/'), out List<string> tokens))
                return "Unclosed quote. Use /help for command usage.";
            if (tokens.Count == 0) return "Enter a command after /.";
            if (!lookup.TryGetValue(tokens[0], out Command command))
                return $"Unknown command /{tokens[0]}. Try /help.";
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (command.DeveloperOnly) return "Developer commands require an Editor or Development Build.";
#endif
            try
            {
                return command.Execute(tokens.Skip(1).ToArray());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return $"/{command.Name} failed. See the development log.";
            }
        }

        private static bool TryTokenize(string raw, out List<string> tokens)
        {
            tokens = new List<string>();
            StringBuilder word = new();
            bool quoted = false;
            foreach (char c in raw)
            {
                if (c == '"') { quoted = !quoted; continue; }
                if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (word.Length > 0) { tokens.Add(word.ToString()); word.Clear(); }
                }
                else word.Append(c);
            }
            if (quoted) return false;
            if (word.Length > 0) tokens.Add(word.ToString());
            return true;
        }

        private static bool NoArgs(string[] args) => args.Length == 0;
        private static bool Number(string text, out float value) =>
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Integer(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        private static bool Toggle(string[] args, bool current, out bool value)
        {
            value = current;
            if (args.Length == 0) return true;
            if (args.Length != 1) return false;
            if (args[0].Equals("on", StringComparison.OrdinalIgnoreCase)) { value = true; return true; }
            if (args[0].Equals("off", StringComparison.OrdinalIgnoreCase)) { value = false; return true; }
            return false;
        }
        private static string State(bool enabled) => enabled ? "on" : "off";
        private static string Missing => "Player systems are unavailable in this scene.";

        private string Help(string[] args)
        {
            if (args.Length > 1) return "Usage: /help [command]";
            if (args.Length == 1)
            {
                if (!lookup.TryGetValue(args[0].TrimStart('/'), out Command command) || !Visible(command))
                    return "Unknown command. Try /help.";
                return $"{command.Usage} — {command.Description}" +
                    (command.Aliases.Length > 0 ? " Aliases: " + string.Join(", ", command.Aliases) + "." : "") +
                    (command.DeveloperOnly ? " Editor/Development Build only." : "");
            }
            return string.Join("\n", commands.Where(Visible).GroupBy(c => c.Category).Select(group =>
                group.Key + ": " + string.Join(", ", group.Select(c => "/" + c.Name))));
        }
        private string Clear(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /clear";
            chat.ClearMessages();
            return string.Empty;
        }
        private string GodMode(string[] args)
        {
            PlayerHealth health = Health;
            if (health == null) return Missing;
            if (!Toggle(args, health.DebugGodMode, out bool value)) return "Usage: /godmode [on|off]";
            if (args.Length == 1) health.SetDebugGodMode(value);
            return "Godmode " + State(health.DebugGodMode) + ".";
        }
        private string Heal(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /heal";
            PlayerHealth health = Health;
            if (health == null) return Missing;
            health.ResetHealth();
            return $"Health {health.CurrentHealth}/{health.MaxHealth}.";
        }
        private string Energy(string[] args)
        {
            if (args.Length > 1 || (args.Length == 1 && (!Number(args[0], out float value) || value < 0f)))
                return "Usage: /energy [nonnegative amount]";
            PlayerCombat combat = Combat;
            if (combat == null) return Missing;
            combat.SetDebugResource(args.Length == 0 ? combat.MaximumResource : float.Parse(args[0], CultureInfo.InvariantCulture));
            return $"Energy {combat.CurrentResource:0.#}/{combat.MaximumResource:0.#}.";
        }
        private string Damage(string[] args)
        {
            if (args.Length != 1 || !Integer(args[0], out int value) || value <= 0 || value > 1000000)
                return "Usage: /damage <1..1000000>";
            PlayerHealth health = Health;
            if (health == null) return Missing;
            return health.TakeHit(value, Vector3.zero) ? $"Health {health.CurrentHealth}/{health.MaxHealth}." : "Damage blocked by invulnerability or death.";
        }
        private string Kill(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /kill";
            PlayerHealth health = Health;
            if (health == null) return Missing;
            if (!health.IsAlive) return "Player is already dead.";
            health.DebugKill();
            return "Player killed; normal death flow started.";
        }
        private string Respawn(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /respawn";
            PlayerHealth health = Health;
            if (health == null || Movement == null) return Missing;
            if (Dungeon != null && Dungeon.IsInDungeon) { Dungeon.DebugRespawn(); return "Respawned at dungeon checkpoint."; }
            CombatArenaReset arena = UnityEngine.Object.FindAnyObjectByType<CombatArenaReset>();
            if (arena != null && arena.enabled) { arena.ResetArena(); return "Respawned at world spawn."; }
            if (!TeleportTo(WorldSpawn, true)) return "World spawn unavailable.";
            health.ResetHealth();
            Combat?.ResetCombat();
            UnityEngine.Object.FindAnyObjectByType<PlayerTargeting>()?.SetTarget(null);
            return "Respawned at world spawn.";
        }
        private string Unstuck(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /unstuck";
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return Missing;
            if (Dungeon != null && Dungeon.IsInDungeon) { Dungeon.DebugRespawn(); return "Moved to dungeon checkpoint."; }
            if (movement.HasSafeGroundedPosition)
                return TeleportTo(movement.LastSafeGroundedPosition, false) ? "Moved to last grounded position." : Missing;
            return TeleportTo(WorldSpawn, true) ? "Moved to world spawn." : "No safe position is available.";
        }
        private string Speed(string[] args)
        {
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return Missing;
            if (args.Length > 1) return "Usage: /speed [0.25..5|reset]";
            if (args.Length == 1)
            {
                if (args[0].Equals("reset", StringComparison.OrdinalIgnoreCase)) movement.SetDebugSpeed(1f);
                else if (Number(args[0], out float value) && value >= .25f && value <= 5f) movement.SetDebugSpeed(value);
                else return "Usage: /speed [0.25..5|reset]";
            }
            return $"Speed {movement.DebugSpeedMultiplier:0.##}x.";
        }
        private string Fly(string[] args)
        {
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return Missing;
            if (!Toggle(args, movement.DebugFly, out bool value)) return "Usage: /fly [on|off]";
            if (args.Length == 1) movement.SetDebugFly(value);
            return "Fly " + State(movement.DebugFly) + ".";
        }
        private string NoClip(string[] args)
        {
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return Missing;
            if (!Toggle(args, movement.DebugNoClip, out bool value)) return "Usage: /noclip [on|off]";
            if (args.Length == 1) movement.SetDebugNoClip(value);
            return "Noclip " + State(movement.DebugNoClip) + ".";
        }
        private static readonly Vector3 WorldSpawn = new(145f, 0f, 155f);
        private static readonly (string name, Vector2 point)[] Landmarks =
        {
            ("spawn", new Vector2(145, 155)), ("northgate", new Vector2(850, 870)),
            ("westmere", new Vector2(470, 985)), ("eastwatch", new Vector2(1390, 1010)),
            ("ancientruins", new Vector2(1020, 1420)), ("riftcrypt", new Vector2(1450, 480))
        };
        private static bool TryGround(Vector3 point, out Vector3 ground)
        {
            ground = point;
            foreach (Terrain terrain in Terrain.activeTerrains)
            {
                Vector3 basePoint = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (point.x < basePoint.x || point.x > basePoint.x + size.x ||
                    point.z < basePoint.z || point.z > basePoint.z + size.z) continue;
                ground.y = terrain.SampleHeight(point) + basePoint.y + 1.5f;
                return true;
            }
            return false;
        }
        private bool TeleportTo(Vector3 point, bool ground)
        {
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return false;
            if (ground && !TryGround(point, out point)) return false;
            movement.DebugTeleport(point);
            return true;
        }
        private string Teleport(string[] args)
        {
            if (Movement == null) return Missing;
            if (Dungeon != null && Dungeon.IsInDungeon) return "Exit Rift Crypt before using /tp; use /respawn for the checkpoint.";
            if (args.Length == 1)
            {
                string key = args[0].Replace(" ", "").Replace("-", "");
                foreach (var landmark in Landmarks)
                    if (landmark.name.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return TeleportTo(new Vector3(landmark.point.x, 0f, landmark.point.y), true)
                            ? $"Teleported to {landmark.name}." : "Landmark terrain is unavailable.";
            }
            if (args.Length == 3 && Number(args[0], out float x) && Number(args[1], out float y) && Number(args[2], out float z) &&
                x >= -10000f && x <= 10000f && z >= -10000f && z <= 10000f && y >= -1000f && y <= 1000f &&
                TryGround(new Vector3(x, y, z), out Vector3 ground) && y >= ground.y - 2f && y <= ground.y + 500f)
                return TeleportTo(new Vector3(x, y, z), false) ? $"Teleported to {x:0.#}, {y:0.#}, {z:0.#}." : Missing;
            return "Usage: /tp <spawn|northgate|westmere|eastwatch|ancientruins|riftcrypt> or /tp <x> <y> <z> on terrain.";
        }
        private string Where(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /where";
            PhasebreakPlayerMovement movement = Movement;
            if (movement == null) return Missing;
            Vector3 p = movement.transform.position;
            if (Dungeon != null && Dungeon.IsInDungeon)
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} / Rift Crypt: ({1:0.#}, {2:0.#}, {3:0.#}).",
                    SceneManager.GetActiveScene().name, p.x, p.y, p.z);
            var nearest = Landmarks.OrderBy(l => Vector2.Distance(new Vector2(p.x, p.z), l.point)).First();
            return string.Format(CultureInfo.InvariantCulture,
                "{0}: ({1:0.#}, {2:0.#}, {3:0.#}); nearest {4} ({5:0}m).",
                SceneManager.GetActiveScene().name, p.x, p.y, p.z, nearest.name,
                Vector2.Distance(new Vector2(p.x, p.z), nearest.point));
        }
        private string Coords(string[] args)
        {
            if (!Toggle(args, chat.ShowCoords, out bool value)) return "Usage: /coords [on|off]";
            if (args.Length == 1) chat.ShowCoords = value;
            return "Coordinates " + State(chat.ShowCoords) + ".";
        }
        private string Fps(string[] args)
        {
            if (!Toggle(args, chat.ShowFps, out bool value)) return "Usage: /fps [on|off]";
            if (args.Length == 1) chat.ShowFps = value;
            return "FPS " + State(chat.ShowFps) + ".";
        }
        private string Hud(string[] args)
        {
            if (!Toggle(args, chat.HudVisible, out bool value)) return "Usage: /hud [on|off]";
            if (args.Length == 1) chat.SetHudVisible(value);
            return "HUD " + State(chat.HudVisible) + ".";
        }
        private string Enemies(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /enemies";
            FrontierEncounterZone[] zones = UnityEngine.Object.FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None);
            int living = UnityEngine.Object.FindObjectsByType<MeleeEnemy>(FindObjectsSortMode.None)
                .Count(enemy => enemy.IsAlive && enemy.GetComponentInParent<RiftDungeonEncounter>() == null);
            int capacity = zones.Sum(z => z.Capacity) + (UnityEngine.Object.FindAnyObjectByType<CombatArenaReset>()?.EnemyCapacity ?? 0);
            RiftDungeonController dungeon = Dungeon;
            string dungeonInfo = dungeon != null && dungeon.IsInDungeon && dungeon.CurrentEncounter != null
                ? $" Dungeon encounter: {dungeon.CurrentEncounter.RemainingEnemies}/{dungeon.CurrentEncounter.TotalEnemies}." : "";
            return $"World enemies: {living} living; {capacity} authored slots in {zones.Length} zones." + dungeonInfo;
        }
        private string KillAll(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /killall";
            if (Dungeon != null && Dungeon.IsInDungeon) return "World killall is unavailable inside Rift Crypt.";
            int count = 0;
            foreach (FrontierEncounterZone zone in UnityEngine.Object.FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None))
                count += zone.DebugDefeatActive();
            foreach (MeleeEnemy enemy in UnityEngine.Object.FindObjectsByType<MeleeEnemy>(FindObjectsSortMode.None))
            {
                if (!enemy.IsAlive || enemy.GetComponentInParent<FrontierEncounterZone>() != null ||
                    enemy.GetComponentInParent<RiftDungeonEncounter>() != null) continue;
                enemy.DebugDefeatWithoutRewards();
                count++;
            }
            return $"Defeated {count} active world enemies without rewards.";
        }
        private string ResetEncounters(string[] args)
        {
            if (!NoArgs(args)) return "Usage: /resetencounters";
            if (Dungeon != null && Dungeon.IsInDungeon) { Dungeon.DebugResetEncounters(); return "Current dungeon encounter reset."; }
            FrontierEncounterZone[] zones = UnityEngine.Object.FindObjectsByType<FrontierEncounterZone>(FindObjectsSortMode.None);
            foreach (FrontierEncounterZone zone in zones) zone.DebugReset();
            CombatArenaReset arena = UnityEngine.Object.FindAnyObjectByType<CombatArenaReset>();
            arena?.DebugResetEnemies();
            return $"Reset {zones.Length} world encounter zones" + (arena != null ? " and the arena enemy set." : ".");
        }
        private string Xp(string[] args)
        {
            if (args.Length != 1 || !Integer(args[0], out int amount) || amount < 0 || amount > 1000000)
                return "Usage: /xp <0..1000000>";
            PlayerProgression progression = Progression;
            if (progression == null) return Missing;
            progression.GrantExperience(amount);
            return $"Level {progression.Level}; XP {progression.CurrentExperience}/{progression.ExperienceToNextLevel}.";
        }
        private string Level(string[] args)
        {
            if (args.Length != 1 || !Integer(args[0], out int level)) return "Usage: /level <level>";
            PlayerProgression progression = Progression;
            if (progression == null) return Missing;
            if (!progression.SetDebugLevel(level)) return $"Level must be between 1 and {progression.MaximumLevel}.";
            return $"Level set to {progression.Level}.";
        }
    }
}
