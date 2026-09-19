# StarterZone_V2 reference

`StarterZone_V2` is PHASEBREAK's build-first starter world. `../../PROJECT_STATE.md` records the current verified baseline; this document covers long-lived scene ownership and maintenance.

## Architecture and ownership

- Scene: `Assets/Phasebreak/Scenes/StarterZone_V2.unity`. The older `Assets/dashlash.unity` remains available as the builder source and fallback.
- MapMagic 2.1.19 is vendored under `Assets/MapMagic`. The V2 graph is `Assets/Phasebreak/Data/StarterZoneV2/ShatteredFrontier.asset`; the editor builder is `Assets/Phasebreak/Editor/BuildStarterZoneV2.cs`.
- `FrontierTerrainNode` feeds `HeightOutput200`; fixed-seed `Noise200` and `TexturesOutput200` supply the terrain surface. Seed: `PHASEBREAK_STARTER_V2_SEED = 271828`. Four pinned 1 km tiles cover a 2 km square, with 513-sample heightmaps and a 320 m height scale. MapMagic generates terrain in the Editor; the fixed runtime scene retains the graph for regeneration.
- `Assets/Phasebreak/Scripts/FrontierEncounterZone.cs` owns local encounter activation and respawn. Normal zones derive a stable Zombie/Puglin/Imp mix from their danger tier and spawn slot; the Riftblade practice lane stays zombies. `MeleeEnemy` owns combat state for all three roles, and `EnemyRoleDefinition` assets hold Brute/Skirmisher values and rank tuning. V2 reuses the existing Player, camera, combat, inventory, XP, quests, NPCs, zombie prefab, and Rift Crypt systems. `WorldQuestHud` reads the V2 `WorldMapDefinition`; terrain bounds come from the bake, and landmark markers follow scene QuestLocation transforms.

## Layout and encounters

- The southwest spawn near (145, 155) leads through a conifer trail and introductory pack to a ridge view of Northgate. The Old King's Road branches toward Westmere, Eastwatch, Ancient Ruins, the Rift basin and Rift Crypt, the Old Fort, the Old Mine hook, and future exit routes.
- Road corridors and settlement terraces are fixed by the terrain function. Northgate is the main defended settlement; Westmere is western lowland and Eastwatch an eastern ridge. Grassland, forest belts, northern highlands, and the sparse Rift basin provide distinct travel spaces.
- There are 26 stable encounter centers and a designed capacity of 99 enemies. Nearby zones instantiate within 230 m and unload beyond 300 m. Ordinary respawns use 180 seconds; larger clearing respawns use 240 seconds and wait while the player is within 48 m. Enemy defeat, XP, corpse loot, and leash behavior remain owned by the existing enemy systems.

## Required asset families

- MapMagic terrain generation, `Assets/MapMagic/Demo/LandTextures`, cloudy sky material, and pine prefabs/textures.
- `Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures` for settlement surfaces; `Assets/Phasebreak/Art/Characters/QuaterniusRegularMale` for existing NPC visuals.
- `Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab` is the stable enemy gameplay prefab and the existing Rift Crypt scene object copied from `dashlash.unity`. It also supplies the common enemy components for Puglin/Imp roles. On the asset owner's machine, use **Phasebreak > Enemies > Import Local Bestiary** and select `C:\Users\chris\Documents\Bestiary - Dungeon Monsters Kit[Standard]` to import the two FBX models, six color textures, normal maps, and license into `Assets/Phasebreak/LocalMonsters`. This folder and its `.meta` files are Git-ignored because the pack license restricts redistribution of raw assets. A fresh checkout without the local import shows simple fallback shapes, while combat roles still work. The imported FBX models contain no animation clips, so `EnemyRoleVisual` supplies procedural idle/move/attack poses.
- V2-specific graph, terrain layers, and materials under `Assets/Phasebreak/Data/StarterZoneV2`. Preserve all referenced assets and their `.meta` GUIDs. License and attribution files remain with imported assets.

## Rebuild and limits

- Use Unity's **Phasebreak > Rebuild Complete Starter Zone V2** editor menu only when regeneration is needed. It copies `dashlash.unity`, builds the V2 graph and authored layout, waits for terrain generation, applies surface/art passes, and saves the V2 scene. It replaces the generated scene, so verify the saved scene before committing: Player, four terrains, MapMagic graph, 26 zones, opening route, Northgate interaction, and Rift entrance.
- After terrain, splat, or vegetation changes, open V2 and run **Phasebreak > Maps > Bake Current World Map**. The selected `WorldMapDefinition` (or the one matching the open scene) updates `Assets/Phasebreak/Generated/Maps/StarterZone_V2_Terrain.png` and `_Roads.png`. Commit the generated sprites with their `.meta` files and definition. The world map and minimap both read these assets; player and quest markers stay live. The road layer uses high Earth splat weights because the final V2 surface pass removes temporary road meshes.
- Settlement architecture, ruins, and distant slopes are prototype art. Westmere has no modeled water; Old Mine and onward routes have no destination scenes. Full quest, boss, loot, level-grind, and packaged-build performance regression remains open.
