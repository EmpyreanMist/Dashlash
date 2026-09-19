# PHASEBREAK project state

Baseline reviewed: 2026-09-19 on `codex/phasebreak-ui-quality-rescue`, starting from the complete safety snapshot `42e7f6d` (`backup/pre-workflow-reconciliation`). This is a single-player Unity vertical slice. Status below describes the working project, not future MMO scope.

## Current playable content

- `Assets/Phasebreak/Scenes/StarterZone_V2.unity` is first in build settings. It contains the southwest starting pocket, four fixed MapMagic terrain tiles across a 2 km square, Northgate, Westmere, Eastwatch, Ancient Ruins, the Rift Crypt entrance, six quest NPCs, and 26 encounter zones. `Assets/dashlash.unity` is the older 420 m starter realm and remains available.
- The player has third-person movement and camera, targeting, five combat abilities, health/death, Energy, XP and levels 1–10. Inventory/equipment, set bonuses, specialization, talents, quests, Rift Crypt encounters, and local PlayerPrefs persistence exist.
- Combat includes cooldowns, charges, critical hits, damage feedback, corpse loot, and enemy XP rewards. Movement includes jump, air control, camera steering, and ability-driven dash. Existing debug and editor builders support local iteration; there is no general developer command console.
- The current UI has player/target frames, an action bar, quest tracker, minimap, Inventory, Character, Talents, Journal, and schematic Map. The UI quality pass and outstanding interface issues are recorded in `UI_AUDIT.md` and `UI_FUNCTIONALITY_FOLLOWUPS.md`.
- The zombie population includes ordinary enemies, Brutes, and Skirmishers. V2 activates nearby encounter zones and respawns them on timers. This is not a networked or server-authoritative game.
- PlayerPrefs stores progression, inventory/equipment, specialization, talents, quest progress, and selected UI placement. The current save model is local prototype state.

## Project assets and ownership

- MapMagic 2.1.19 is vendored under `Assets/MapMagic`. V2 uses `Assets/Phasebreak/Data/StarterZoneV2/ShatteredFrontier.asset` and the builder in `Assets/Phasebreak/Editor/BuildStarterZoneV2.cs`. Keep the scene, graph, referenced art, and `.meta` GUIDs together.
- The obsolete one-off `Assets/Editor/HubForceResolve.cs` bootstrapper and its `.meta` files are absent from the complete snapshot. The active MapMagic package imports and compiles without that forced package resolve script.
- The project uses URP and the Unity Input System. Source art and licenses are described in `PHASEBREAK_WORLD_ASSET_CATALOG.md` and the scoped `THIRD_PARTY_ASSETS.md` note.
- Runtime gameplay code is under `Assets/Phasebreak/Scripts`. The editor builders under `Assets/Phasebreak/Editor` are authoring tools, not duplicate runtime systems.

## Verification and limits

- The checked-in V2 scene initially failed to deserialize and opened empty in the connected Editor. The existing complete V2 builder regenerated it; the resulting scene had eight roots, the Player, four terrains, MapMagic, and 26 encounter zones. Play Mode entered with five nearby enemies and no new console errors. The repaired scene and paired graph are part of this baseline.
- Unity compilation completed without errors. Third-party MapMagic API warnings and some obsolete Unity API warnings remain. Focused Play Mode smoke checks establish scene startup and nearby encounter activation; full quest, loot, talent, dungeon, persistence, and packaged-build regression runs remain outstanding.
- Unity found 195 V2 scene dependencies, no missing dependency paths, and no missing scripts in the scene hierarchy. An Assets file audit found no missing `.meta` files outside normal plugin bundle internals. `git lfs fsck` passed. Unity Test Runner has no meaningful automated tests in this project.
- V2 architecture and settlements still use prototype geometry. The world map is schematic. There is no local chat/command registry, unified Settings menu, Spellbook, or multiplayer service.

## Next work

`CHAT_COMMAND_REGISTRY_PROPOSAL.md` specifies a useful focused next step: local chat plus developer commands, with shared input-focus blocking and adapters to existing systems. It has not been implemented. Other future work is tracked in `PHASEBREAK_ROADMAP.md`, `PHASEBREAK_MASTER_FEATURE_CATALOG.md`, and `UI_FUNCTIONALITY_FOLLOWUPS.md`.
