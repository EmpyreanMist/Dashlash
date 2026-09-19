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
- The UI includes player/target frames, action bar, Inventory, Character, Talents, Journal, quest tracker, minimap, and schematic world map. `docs/reference/UI_FUNCTIONALITY_FOLLOWUPS.md` records remaining interface and regression work.
- PlayerPrefs stores progression, inventory/equipment, specialization, talents, quest progress, and selected UI positions. This is local prototype persistence, not an authoritative multiplayer save service.
- Editor authoring tools live under `Assets/Phasebreak/Editor`; MapMagic 2.1.19 is under `Assets/MapMagic`. Third-party attribution and license files remain with their assets.

## Verification and current limits

- Unity compilation completed without current errors. V2 reopened with the Player, four terrains, MapMagic, and 26 encounter zones; Play Mode started with five nearby enemies and no new runtime errors. Its 195 dependency paths and scene scripts resolved, the required `.meta` audit found no gaps, and `git lfs fsck` passed.
- There is no meaningful automated Unity test suite or packaged-build regression pass yet. Full quest, loot, talent, dungeon, persistence, and performance regression remains open.
- Settlements and world art are prototype quality; the world map is schematic. There is no local chat/command registry, unified Settings menu, Spellbook, or multiplayer service. Chat and developer commands remain a proposal in `docs/reference/CHAT_COMMAND_REGISTRY_PROPOSAL.md`.
