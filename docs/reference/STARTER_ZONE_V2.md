# StarterZone_V2 reference

`StarterZone_V2` is PHASEBREAK's build-first starter world. `../../PROJECT_STATE.md` records the current verified baseline; this document covers long-lived scene ownership and maintenance.

## Architecture and ownership

- Scene: `Assets/Phasebreak/Scenes/StarterZone_V2.unity`. The older `Assets/dashlash.unity` remains available as the builder source and fallback.
- MapMagic 2.1.19 is vendored under `Assets/MapMagic`. The V2 graph is `Assets/Phasebreak/Data/StarterZoneV2/ShatteredFrontier.asset`; the editor builder is `Assets/Phasebreak/Editor/BuildStarterZoneV2.cs`.
- `FrontierTerrainNode` feeds `HeightOutput200`; fixed-seed `Noise200` and `TexturesOutput200` supply the terrain surface. Seed: `PHASEBREAK_STARTER_V2_SEED = 271828`. Four pinned 1 km tiles cover a 2 km square, with 513-sample heightmaps and a 320 m height scale. MapMagic generates terrain in the Editor; the fixed runtime scene retains the graph for regeneration.
- `Assets/Phasebreak/Scripts/FrontierEncounterZone.cs` owns local encounter activation and respawn. V2 reuses the existing Player, camera, combat, inventory, XP, quests, NPCs, zombie prefab, and Rift Crypt systems. `WorldQuestHud` uses V2-specific map bounds and markers while this scene is active.

## Layout and encounters

- The southwest spawn near (145, 155) leads through a conifer trail and introductory pack to a ridge view of Northgate. The Old King's Road branches toward Westmere, Eastwatch, Ancient Ruins, the Rift basin and Rift Crypt, the Old Fort, the Old Mine hook, and future exit routes.
- Road corridors and settlement terraces are fixed by the terrain function. Northgate is the main defended settlement; Westmere is western lowland and Eastwatch an eastern ridge. Grassland, forest belts, northern highlands, and the sparse Rift basin provide distinct travel spaces.
- There are 26 stable encounter centers and a designed capacity of 99 enemies. Nearby zones instantiate within 230 m and unload beyond 300 m. Ordinary respawns use 180 seconds; larger clearing respawns use 240 seconds and wait while the player is within 48 m. Enemy defeat, XP, corpse loot, and leash behavior remain owned by the existing enemy systems.

## Required asset families

- MapMagic terrain generation, `Assets/MapMagic/Demo/LandTextures`, cloudy sky material, and pine prefabs/textures.
- `Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures` for settlement surfaces; `Assets/Phasebreak/Art/Characters/QuaterniusRegularMale` for existing NPC visuals.
- `Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab` for encounter enemies and the existing Rift Crypt scene object copied from `dashlash.unity`.
- V2-specific graph, terrain layers, and materials under `Assets/Phasebreak/Data/StarterZoneV2`. Preserve all referenced assets and their `.meta` GUIDs. License and attribution files remain with imported assets.

## Rebuild and limits

- Use Unity's **Phasebreak > Rebuild Complete Starter Zone V2** editor menu only when regeneration is needed. It copies `dashlash.unity`, builds the V2 graph and authored layout, waits for terrain generation, applies surface/art passes, and saves the V2 scene. It replaces the generated scene, so verify the saved scene before committing: Player, four terrains, MapMagic graph, 26 zones, opening route, Northgate interaction, and Rift entrance.
- Settlement architecture, ruins, and distant slopes are prototype art. Westmere has no modeled water; Old Mine and onward routes have no destination scenes. Full quest, boss, loot, level-grind, and packaged-build performance regression remains open.
