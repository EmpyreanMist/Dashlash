# PHASEBREAK roadmap

`PROJECT_STATE.md` is the factual implementation snapshot. This roadmap records direction and milestones; GitHub Issues define actionable work. The non-authoritative design catalog is in `docs/reference/PHASEBREAK_MASTER_FEATURE_CATALOG.md`.

## Vision

Build a third-person open-world action RPG with readable target-based combat, expressive class builds, meaningful loot, and instanced dungeons. Core, Relics, Sigils, item tags, cross-slot sets, and a Wildcard Artifact distinguish PHASEBREAK. Preserve a future path to authoritative multiplayer without building MMO services for the current single-player slice.

## Completed major milestones

- Third-person movement and camera, targeting, player/target frames, and ability combat with Energy, critical hits, cooldowns, charges, and presentation feedback.
- Rift Crypt encounter, boss, checkpoint, and reward loop.
- Inventory, corpse loot, 16 equipment slots, comparison, sets, specialization, first talent trees, and local persistence.
- Five-slot action bar, Phase Dash, Rift Charge, and first equipment visuals.
- Living starter-world foundation: quests and NPCs, zombie variants, world/map HUD, and build-first `StarterZone_V2` with MapMagic terrain and local encounter zones.

## Current milestone

Strengthen the single-player starter-zone vertical slice through focused GitHub Issues. Priorities are repeatable world and dungeon play, reliable input and UI behavior, and regression coverage. The local chat/developer-command design in `docs/reference/CHAT_COMMAND_REGISTRY_PROPOSAL.md` is a candidate issue, not an implemented system or automatic next task.

## Near-term milestones

- Add a useful Rift Marks vendor/supply sink and more readable ranged, caster, and bruiser enemy roles with camp behavior.
- Improve the schematic map with authored zone art and make the Rift Crypt a paced 10–15 minute dungeon with boss phases and a distinctive reward.
- Replace prototype world geometry and fallback animation where they limit readability; validate quest, loot, talent, dungeon, and save flows in Play Mode and a packaged build.
- Add focused automated tests for stable gameplay contracts and input-focus transitions as those systems change.

## Long-term direction

Expand class identity, active talents, status effects, item instances and affixes, crafting/economy, world activities, and endgame content as separate milestones. Group play and online services follow only after the local combat and persistence boundaries are ready.

## Multiplayer architecture guardrails

Keep combat outcomes, inventory changes, loot grants, and saved character state behind replaceable ownership boundaries. Local PlayerPrefs are suitable for this prototype; an online version must move validation and authoritative state to the server.
