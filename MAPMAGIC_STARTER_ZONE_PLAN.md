# The Shattered Frontier: starter zone V2

Status: implemented in the build-first scene. `PROJECT_STATE.md` records current verification and remaining test limits; this file describes the intended layout and rebuild workflow.

## World layout

- Four fixed 1 km MapMagic tiles form a 2 km by 2 km region, with southwest spawn at approximately (145, 155). The accepted seed is `PHASEBREAK_STARTER_V2_SEED = 271828`.
- A narrow conifer trail leads through one introductory enemy pocket to a ridge gate and the first view of Northgate. The Old King's Road then branches toward Westmere, Eastwatch, Ancient Ruins, the Rift basin, and northern pass.
- Civilization occupies a small defensible hill at Northgate. Forest patches, open grassland, western lowlands, northern highlands, and the lower Rift basin provide contrasting travel and combat spaces.

## MapMagic and authored content

- MapMagic 2.1.19 generates the heightfield with `FrontierTerrainNode`, `HeightOutput200`, a fixed-seed `Noise200` earth mask, and `TexturesOutput200`. Road corridors and settlement terraces are fixed so regeneration cannot move gameplay landmarks.
- An editor finishing pass paints three terrain layers, including slope rock and road masks. It then keeps the four generated tiles fixed for runtime. Authored cottages, landmarks, routes, props, and encounter centers are placed from the same terrain height function.
- Rebuild from Unity's **Phasebreak > Rebuild Complete Starter Zone V2** menu. The command copies `dashlash.unity` to `StarterZone_V2.unity`, preserves the old scene, waits for MapMagic generation, applies surface and art passes, and saves V2.

## Gameplay and verification

- Reuse the existing Player, camera, combat, quests, six NPCs, inventory, XP, and Rift Crypt logic. V2 only changes scene positions and adds local encounter activation and timed respawn.
- Check the opening trail, route grades, first vista, Northgate interaction, first pack combat, repeatable XP, Rift entrance, map bounds, and Play Mode rendering after each rebuild.
