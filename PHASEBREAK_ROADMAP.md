# Phasebreak Roadmap

Den fullständiga valbara funktions- och statuskatalogen finns i `PHASEBREAK_MASTER_FEATURE_CATALOG.md`. Roadmapen används för den korta fasöversikten; masterkatalogen är projektets detaljerade backlog och beslutslista.

## Vision

Third-person open-world action RPG with target-based MMO readability, instanced dungeons, expressive class builds and a future multiplayer path. Core, Relics, Sigils, tags, cross-slot sets and a Wildcard Artifact form Phasebreak's own build identity.

## Completed milestones

- Phase 1 — third-person movement and MMO-style camera controls.
- Phase 2 — targeting, player/target frames and nameplates.
- Phase 3 — ability combat, critical hits, satisfying floating damage and progression to level 3.
- Phase 4 — combat resource, soft auto-targeting and initial loot hooks.
- Phase 5 — Rift Crypt dungeon loop, encounters, boss, checkpoint and reward chest.
- Phase 6 — player-controlled inventory/equipment, comparison tooltips, build summary, persistent specialization, data-driven items/sets/abilities and build-changing effects.
- Phase 6.5 — bag-style inventory, separate character sheet, HUD menu navigation and per-corpse right-click looting.
- Phase 6.75 — five-slot combat bar, ability-driven Phase Dash, target-based Rift Charge, procedural combat presentation and the first equipment visuals.

## Phase 6 content

- Inventory opens with `B`; loot is stored rather than auto-equipped.
- Sixteen equipment sockets: Primary Weapon, Secondary, six armor locations, Core, three Relics, three Sigils and Wildcard Artifact.
- Riftstalker Circuit demonstrates cross-slot 2/3/4/6-piece effects.
- Item tags currently include Void, Crit, Mobility, Bleed, Fire, Defense, Energy and Boss.
- Implemented effects: crit Energy recovery, Phase Lunge cooldown/extra charge, Crushing Blow cleave, teleport-kill recovery and boss damage.
- Level 3 specialization choice: Berserker, Bulwark or Riftblade.
- Inventory, equipped items and specialization persist locally through `PlayerPrefs` JSON using stable item IDs.

## Phase 6.5 controls and flow

- `B` — Inventory bag.
- `C` — Character, equipment, final stats, specialization, active sets and build modifiers.
- `T` — Talents placeholder; the full talent tree is intentionally deferred.
- `Escape` — close the active gameplay menu.
- Right mouse button on a corpse — open that corpse's private loot container.
- Corpses stop combat participation immediately, retain their own generated drops and remain for 60 seconds by default. Items or Take All transfer through the existing inventory/save path.
- Item definitions use authored Sprite icons when available. A central icon catalog supplies slot-specific art fallbacks, with procedural silhouettes retained only as the final safety net.

## Current UI update — inventory and character presentation

- Inventory now uses a nine-column scrollable bag grid, rarity borders, icon-first items, visible empty capacity and category filters for weapons, armor, cores/relics and sigils.
- Every current item now has unique authored icon art. Missing art falls back through a data-driven weapon, armor, relic, sigil or artifact icon before the procedural safety net; cryptic letter placeholders are not used.
- Left-click selects and inspects an item. Right-click or double-click equips it through the existing `PlayerBuildSystem`; the Character screen uses the same pattern to unequip.
- Hover tooltips are screen-clamped and can show a side-by-side currently-equipped item with green/red stat differences.
- A reusable loadout-aware evaluator marks genuine upgrades with a restrained green outline in Inventory and Corpse Loot. It compares the complete before/after build, including item stats, item level, gameplay modifiers and set-threshold changes; the bag header reports how many visible items are upgrades.
- Upgrade tooltips and the selected-item inspection explain whether the candidate fills an empty slot or improves the estimated build score. Existing authored item Sprites remain the first choice, with recognizable slot-specific procedural icons as the fallback.
- Character now presents all sixteen equipment channels around a Phasebound paper-doll silhouette, with final stats, specialization, passive, active set thresholds and build modifiers in a separate analysis panel.
- Inventory, equipment calculations, build effects, item sets and local save ownership remain unchanged; this update only replaces and modularizes the runtime presentation layer.

## Current icon integration update

- Source audit: `C:\Users\chris\Pictures\WoW Icon Pack` contains 4,314 PNG files in eight categories; all are 60 × 60, nearly all are opaque RGB images, and 12 exact duplicate groups were detected. Runtime never references this external folder.
- Only the 41 currently required sprites were copied into `Assets/Phasebreak/Art/UI/Icons`, organized as Items, Abilities, Passives, Statuses, Specializations, Sets and Fallbacks. The original collection was not imported wholesale.
- All 10 current items and all five abilities (`Strike`, `Crushing Blow`, `Phase Lunge`, `Phase Dash`, `Rift Charge`) have distinct imported UI sprites.
- The Riftstalker items share a purple/void visual family while retaining unique silhouettes. Riftstalker Circuit also has its own set emblem.
- The action bar now renders the assigned ability art while preserving keys, costs, charges, cooldown shading and usability feedback.
- Empty equipment slots use 16 mapped slot fallbacks from a central `PhasebreakIconCatalog`; item, ability, buff and debuff categories also have safe fallbacks.
- Berserker, Bulwark and Riftblade have separate specialization icons.
- The existing Keen Edge progression passive has its own icon and is presented in the Character analysis panel.
- Five initial status definitions demonstrate buff/debuff icon, duration and stack metadata: Crit Surge, Swift Momentum, Poison, Slow and Rift Empowerment. The reusable status-icon view supports duration, stacks and hover text; live combat-status ownership remains Phase 9 work.
- Item tooltips retain build-aware green/red comparisons and now list current set progress plus every 2/3/4/6-piece threshold with active/locked state.
- Imported icon textures are configured as 2D/UI Sprites, clamp-wrapped, non-mipmapped and uncompressed at a 128 maximum size for crisp 60 × 60 source art.

## Phase 6.75 controls and presentation

- `1` — Strike.
- `2` — Crushing Blow.
- `3` — Phase Lunge.
- `4` — Phase Dash. Two charges; dash is no longer bound directly to Left Shift.
- `5` — Rift Charge. Requires a hostile target at medium range, rushes to melee range and deals damage.
- Combat exposes start/impact/complete/failure events so animation, audio, trails, HUD feedback and future networking do not own damage logic.
- The Quaternius character currently uses replaceable procedural attack/hit/death poses over the locomotion controller. Authored clips and a proper upper-body Animator layer remain production work.
- Equipped test weapon and chest visuals are driven by the existing equipment state. Weapons draw into the hands during abilities and return to back/hip sheath anchors afterward.

## Phase 6.8 — Unit Frames

- New prefab-based dark-fantasy Player and Target Frames replace the former text-heavy unit panels.
- Player data includes portrait placeholder, name, level, health, Energy and class-resource/charge pips.
- Target data includes portrait placeholder, health, rank/boss marker, real enemy-windup cast bar and reserved status/debuff slots.
- Right-click either frame to toggle move mode; left-drag while unlocked and right-click again to lock. Positions persist locally and remain clamped to the HUD canvas.
- Frame visuals, data binding and reusable health/resource/cast components are separated. Enemy world nameplates now use their own component as a foundation for later floating-nameplate work.
- Production portrait art, status-effect binding, target-of-target and full HUD layout profiles remain future work.

## Current small update — medieval starter realm and first monster

- Camera zoom is normalized across common mouse-wheel input scales, moves 3.5 world units per notch and supports a 0.05–18 range so the camera can pass into the player model while retaining camera collision.
- The prototype arena has been replaced visually by a 420 × 420 metre medieval starter realm built from the CC0 Quaternius Medieval Village MegaKit Standard. It includes rolling terrain, roads, three settlement clusters, three ruins, woodland, field stones and the existing Rift Crypt connection.
- Ordinary melee-enemy hits retain flash, animation event and camera feedback but no longer displace the player. Explicit special/boss knockback remains supported.
- `Risen Zombie` is the first imported production-style monster visual. One reusable prefab powers 36 ground-aligned, individually rotated instances grouped around ruins, roads, forests, fields and outer-region encounter pockets; the player start and settlement interiors remain quiet.
- Zombie AI supports idle, aggro, chase, readable windup/attack/recovery, stagger, immediate death, corpse loot, leash and return-to-spawn with health reset.
- The supplied zombie FBX contains a Humanoid rig but no authored action clips beyond its bind/T-pose. Existing compatible idle/run clips and a procedural presentation layer currently provide locomotion, melee, hit and death feedback; authored zombie clips remain a future art upgrade.

## Recommended next milestone — Phase 7

Build the first real combat-content slice: ranged, caster and bruiser enemies; interruptible telegraphs, leash/group aggro and an elite modifier; then turn Rift Crypt into a polished 10–15 minute dungeon with an optional risk room, boss phases and a unique reward. Item-instance loot tables should follow before broad open-world expansion.

## Multiplayer guardrails

Keep authoritative combat results, inventory mutations and loot grants behind replaceable service boundaries. Local saves are suitable for the prototype only; an MMO version must move character state, drops and validation to the server.
