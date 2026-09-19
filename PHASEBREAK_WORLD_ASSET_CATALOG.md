# World asset catalog for StarterZone_V2

| Use | Asset/source | Notes |
| --- | --- | --- |
| Terrain generation | `Assets/MapMagic` version 2.1.19 | Imported package and demo graphs inspected; V2 uses a separate graph and scene. |
| Grass, dirt, cliff and sky | `Assets/MapMagic/Demo/LandTextures` and `Assets/MapMagic/Demo/Materials/SkyCloudyEvening.mat` | URP terrain layers and cloudy cubemap sky in V2. |
| Forest meshes | `Assets/MapMagic/Demo/Trees/Pine/Prefabs` and its `Textures/Pine.tif` | Three pine shapes, a V2 URP unlit cutout material, deterministic woodland patches. |
| Settlement surfaces | `Assets/Phasebreak/Art/World/MedievalVillageMegaKit/Textures` | Plaster, brick, tile roof, wood, and stone trim textures on V2 cottages and gates. |
| Existing NPC visuals | `Assets/Phasebreak/Art/Characters/QuaterniusRegularMale` | Existing six NPC GameObjects copied into V2 at new positions. |
| Enemies | `Assets/Phasebreak/Art/Enemies/SZombie/SZombieEnemy.prefab` | Existing enemy AI, combat, loot, and XP reused by `FrontierEncounterZone`. |
| Dungeon | Existing `Rift Crypt` scene object in `dashlash.unity` | Dungeon instance copied; world entrance moved to the Rift basin and interior moved outside the terrain bounds. |

V2 generation creates its scene under `Assets/Phasebreak/Scenes` and graph, materials, and terrain layers under `Assets/Phasebreak/Data/StarterZoneV2`. The original `Assets/dashlash.unity` and source prefabs remain available.
