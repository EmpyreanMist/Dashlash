# PHASEBREAK project state

Authoritative branch: main
Gameplay baseline verified: 2026-09-19
Initial verified gameplay baseline: c847a176524787ca5cf9db3fb1218357abfefac9

PHASEBREAK is a single-player Unity 6000.6.1f1 vertical slice. This document records implemented capability and verification limits; GitHub Issues hold actionable work.

## Playable world

- `Assets/Phasebreak/Scenes/StarterZone_V2.unity` is first in build settings. Its four fixed MapMagic terrain tiles cover a 2 km square with a southwest spawn, Northgate, Westmere, Eastwatch, Ancient Ruins, Rift Crypt entrance, six quest NPCs, and 26 encounter zones. `Assets/dashlash.unity` remains the older fallback scene.
- V2 activates nearby zombie encounter zones and respawns them on timers. Ordinary zombies, Brutes, and Skirmishers use the existing enemy, XP, and corpse-loot systems. `docs/reference/STARTER_ZONE_V2.md` describes world ownership, required assets, and rebuilding.

## Implemented systems and ownership

- `Assets/Phasebreak/Scripts` owns third-person movement, jump/air control, camera, targeting, interaction, five combat abilities, Energy, health/death, XP/levels 1–10, inventory/equipment, set effects, specialization, talents, quests, enemy encounters, and Rift Crypt checkpoint recovery. UI and debug tools should use these owners rather than duplicate gameplay state.
- The UI includes player/target frames, action bar, Inventory, Character, Talents, Journal, quest tracker, minimap, schematic world map, a local chat hub, and an Escape game menu with searchable Settings. A reusable dark forged-metal uGUI style foundation now supplies rounded charcoal surfaces, subtle metal edges, muted gold accents, and button states to initial inventory, chat, and action-bar surfaces. The compact inventory and full panel/HUD restyle remain open issues. Settings persists keybinding overrides, camera controls, UI scale, screen shake, and video choices through PlayerPrefs. The chat has a metadata-driven developer command registry with generated help, autocomplete, movement/recovery/world/encounter/progression commands, and transient debug overlays. Godmode enables a rebindable cursor-teleport key plus left click; fly and noclip use the configured jump and strafe-left keys to rise and descend, or Ctrl to descend. Gameplay-changing commands run in the Editor or Development Builds. `docs/reference/UI_FUNCTIONALITY_FOLLOWUPS.md` records remaining interface and regression work.
- PlayerPrefs stores progression, inventory/equipment, specialization, talents, quest progress, and selected UI positions. This is local prototype persistence, not an authoritative multiplayer save service.
- Editor authoring tools live under `Assets/Phasebreak/Editor`; MapMagic 2.1.19 is under `Assets/MapMagic`. Third-party attribution and license files remain with their assets.

## Verification and current limits

- Unity compilation completed without current errors. V2 reopened with the Player, four terrains, MapMagic, and 26 encounter zones; Play Mode started with five nearby enemies and no new runtime errors. Its 195 dependency paths and scene scripts resolved, the required `.meta` audit found no gaps, and `git lfs fsck` passed.
- The shared UI style foundation compiled in Unity 6000.6.1f1 and was visually checked in StarterZone_V2 Play Mode at 1280×720 on the chat, action bar, and inventory window. Full UI restyling and multi-resolution layout verification remain open.
- There is no meaningful automated Unity test suite or packaged-build regression pass yet. Full quest, loot, talent, dungeon, persistence, and performance regression remains open.
- Settlements and world art are prototype quality; the world map is schematic. Local chat and developer commands have no networking or server authority. There is no Spellbook or multiplayer service. `docs/reference/CHAT_COMMAND_REGISTRY_PROPOSAL.md` is the historical design proposal.
