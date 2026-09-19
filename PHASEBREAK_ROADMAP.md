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
- Rebuildable outdoor map foundation: Editor-baked terrain and road layers shared by the world map and player-centered minimap, with scene-owned landmarks and live player/objective markers.

## Current milestone

The current UI milestone moves the prototype toward a compact dark fantasy action RPG presentation: charcoal and translucent black surfaces, forged-metal framing, rounded corners, and restrained accents. Inventory and Character lead the layout pass, followed by a lower-right utility cluster and a consistent treatment for the player frame, action bar, quest tracker, chat, minimap, and other major panels. Keep existing gameplay behavior and system ownership intact. GitHub Issues define the scoped work and verification for each part.

## Near-term milestones

- Validate the Riftblade "fighter to anomaly" direction through the implemented chain-kill lane: build investment sustains Energy, then charges, then short flicker sequences. The mechanical prototype supports a 12-kill chain; hands-on feel, audio and balance validation should precede any global Phasebreak meter or world-wide Phasebreach expansion.
- Add a useful Rift Marks vendor/supply sink and more readable ranged, caster, and bruiser enemy roles with camp behavior.
- Improve Rift Crypt pacing toward a 10–15 minute dungeon with boss phases and a distinctive reward. Map marker interaction and visual polish remain separate scoped work.
- Replace prototype world geometry and fallback animation where they limit readability; validate quest, loot, talent, dungeon, and save flows in Play Mode and a packaged build.
- Add focused automated tests for stable gameplay contracts and input-focus transitions as those systems change.

## Long-term direction

Expand class identity, active talents, status effects, item instances and affixes, crafting/economy, world activities, and endgame content as separate milestones. Group play and online services follow only after the local combat and persistence boundaries are ready.

## Multiplayer architecture guardrails

Keep combat outcomes, inventory changes, loot grants, and saved character state behind replaceable ownership boundaries. Local PlayerPrefs are suitable for this prototype; an online version must move validation and authoritative state to the server.
