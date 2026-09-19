# PHASEBREAK roadmap

`PROJECT_STATE.md` is the factual implementation snapshot. This roadmap records direction and milestones; GitHub Issues define actionable work. The non-authoritative design catalog is in `docs/reference/PHASEBREAK_MASTER_FEATURE_CATALOG.md`.

## Vision

"You start as a fighter. You end as an anomaly." PHASEBREAK is a third-person action RPG where talents, abilities, equipment, and sets increasingly change combat rules, not just numbers. Strong mechanical synergies should let sufficiently invested builds feel intentionally powerful, even borderline broken; discovering those interactions should reward replay. Berserker aims toward self-sustaining offensive pressure through crits and momentum, Bulwark toward turning defense and incoming pressure into offense, and Riftblade toward movement-driven damage and controlled teleport chains. These are design directions, not claims that all three transformations exist today.

The Riftblade chain-kill slice is the first mechanical proof. Berserker and Bulwark still need similarly transformative paths. Specializations are freely switchable, with independent saved talent allocations, so players can experiment without permanently committing a character. Core, Relics, Sigils, item tags, cross-slot sets, and a Wildcard Artifact support that build discovery. Keep a future path to authoritative multiplayer without building MMO services for this single-player prototype.

Judge major additions by combat feel, build discovery and transformation, meaningful loot, escalating power, and replayable encounters. Borrow useful MMO/ARPG structure without chasing MMO content volume.

## Completed major milestones

- Third-person movement and camera, targeting, player/target frames, and ability combat with Energy, critical hits, cooldowns, charges, and presentation feedback.
- Rift Crypt encounter, boss, checkpoint, and reward loop.
- Inventory, corpse loot, 16 equipment slots, comparison, sets, specialization, first talent trees, and local persistence.
- Five-slot action bar, Phase Dash, Rift Charge, and first equipment visuals.
- Living starter-world foundation: quests and NPCs, zombie variants, world/map HUD, and build-first `StarterZone_V2` with MapMagic terrain and local encounter zones.
- First Riftblade mobility chain prototype, with an authored 12-enemy practice lane and build-dependent Energy/charge recovery; free specialization switching with independent saved talent trees.
- Rebuildable outdoor map foundation: Editor-baked terrain and road layers shared by the world map and player-centered minimap, with scene-owned landmarks and live player/objective markers.

## Current milestone

**Core Build Identity — Fighter to Anomaly.** Validate whether the Riftblade chain-kill prototype feels fun with real keyboard and mouse input, clear combat feedback, audible escalation, and satisfying progression from ordinary attacks to invested flicker chains. Free specialization switching supports comparison and experimentation. Prove that build investment changes how combat plays before adding a global Phasebreak meter or world-wide Phasebreach system. The existing API-driven 12-kill chain establishes mechanical feasibility; it does not establish play-feel or final balance.

## Near-term milestones

- Finish hands-on Riftblade validation: keyboard/mouse feel, audio listening, balance, and readability in the practice lane; adjust only where the chain is less fun or clear than intended.
- Regress Berserker, Bulwark, and Riftblade talent effects, ability behavior, persistence, and repeated switching, including inactive-tree isolation.
- Continue forged-metal UI cohesion as supporting presentation work: remaining major panels, utility cluster, action bar/HUD, tooltips, and combat readability.
- Develop readable ranged, caster, and bruiser enemies with camp/leash behavior so builds face more varied decisions.
- Improve authored points of interest and world presentation where prototype geometry or animation obscures encounters.
- Improve Rift Crypt pacing, boss phases, and reward distinction. Revisit the Rift Marks vendor/supply sink and map marker interaction as separate scoped work.
- Add focused automated checks for stable gameplay contracts and input-focus transitions as those systems change; verify broader quest, loot, talent, dungeon, and save flows in a packaged build.

## Long-term direction

Give Berserker and Bulwark transformative build paths after the core loop is proven. Expand active talents, status effects, item instances and affixes, crafting/economy, world activities, and endgame content as separate milestones. Group play and online services follow only after the local combat and persistence boundaries are ready.

## Multiplayer architecture guardrails

Keep combat outcomes, inventory changes, loot grants, and saved character state behind replaceable ownership boundaries. Local PlayerPrefs are suitable for this prototype; an online version must move validation and authoritative state to the server.
