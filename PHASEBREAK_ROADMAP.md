# Phasebreak Roadmap

Den fullständiga valbara funktions- och statuskatalogen finns i `PHASEBREAK_MASTER_FEATURE_CATALOG.md`. Roadmapen används för den korta fasöversikten; masterkatalogen är projektets detaljerade backlog och beslutslista.

For the verified implementation baseline and test limits, see `PROJECT_STATE.md`. Earlier milestone descriptions below are historical snapshots when a newer section supersedes them.

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
- `T` — open the implemented first-pass specialization talent trees. Balance and further talent content remain open.
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
- The earlier prototype arena was replaced visually by a 420 × 420 metre medieval starter realm built from the CC0 Quaternius Medieval Village MegaKit Standard. That scene remains as `dashlash.unity`; the build-first scene is now `StarterZone_V2.unity`.
- Ordinary melee-enemy hits retain flash, animation event and camera feedback but no longer displace the player. Explicit special/boss knockback remains supported.
- `Risen Zombie` is the first imported production-style monster visual. One reusable prefab powers 36 ground-aligned, individually rotated instances grouped around ruins, roads, forests, fields and outer-region encounter pockets; the player start and settlement interiors remain quiet.
- Zombie AI supports idle, aggro, chase, readable windup/attack/recovery, stagger, immediate death, corpse loot, leash and return-to-spawn with health reset.
- The supplied zombie FBX contains a Humanoid rig but no authored action clips beyond its bind/T-pose. Existing compatible idle/run clips and a procedural presentation layer currently provide locomotion, melee, hit and death feedback; authored zombie clips remain a future art upgrade.

## Goal 5 — Talents

The first data-driven talent trees, point spending and respec flow are now implemented for the existing specializations. Production balance, full spellbook/action-bar integration and content tuning remain open.

## Goal 6 — Living Starter Zone foundation

- In the older `dashlash.unity` scene the player starts in Northgate. In build-first `StarterZone_V2.unity`, the player starts in the southwest opening pocket and travels to Northgate. Six named NPCs connect Northgate, Westmere and Eastwatch.
- The existing zombie population now includes five heavier Brutes and five faster Skirmishers, with distinct health, speed, attack cadence, rewards and target rank.
- Six sequential quests now cover NPC dialogue, overworld zombie combat, ruin discovery and the Rift Crypt completion/reward loop. Objectives, discovered locations, completed quests and Rift Marks persist locally.
- `E` interacts with nearby NPCs; `J` opens the field journal and `M` opens a schematic world map. The HUD shows the active objective, NPC interaction prompt and map/compass markers.
- The level cap is 10 in the active scene, with locally saved level/experience, making quest rewards and further overworld combat meaningful beyond level 3.
- This is a single-player vertical slice, not an online MMO. Rift Marks have no vendor sink yet, the map is schematic, NPCs are stationary, and all three zombie types still use melee AI. Ranged/caster roles are explicit next tasks, not completed features.

## Recommended next milestone — Phase 7

Make the world loop deeper: give Rift Marks a vendor/supply sink, add ranged/caster/bruiser enemy roles with readable telegraphs and camp behavior, and turn the schematic map into a readable authored zone map. Then polish Rift Crypt into a 10–15 minute dungeon with boss phases and a unique reward. Keep authoritative multiplayer services as a separate later milestone.

## Proposed focused next step — local chat and developer commands

The current single-player slice would benefit from a compact local chat/command console before more content work. This is a proposal, not an implemented milestone. `CHAT_COMMAND_REGISTRY_PROPOSAL.md` records the requested command set, shared input-focus gate, registry contract, existing gameplay systems to reuse, developer access policy, and regression checks. Build the focus gate and registry first; flight, noclip, forced level changes, and reward-free mass enemy actions need explicit system adapters. Plain chat text remains local until multiplayer chat is built.

## Multiplayer guardrails

Keep authoritative combat results, inventory mutations and loot grants behind replaceable service boundaries. Local saves are suitable for the prototype only; an MMO version must move character state, drops and validation to the server.
