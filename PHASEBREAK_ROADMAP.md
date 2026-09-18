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

## Phase 6 content

- Inventory opens with `B` or `I`; loot is stored rather than auto-equipped.
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
- Item definitions now accept an optional Sprite icon; slot glyphs provide dependency-free placeholders until final item art exists.

## Recommended next milestone — Phase 7

Build the first scalable content pipeline: weighted loot tables per dungeon/boss, affix pools with slot/tag rules, item-instance rolls and a small character-sheet test suite. Then add dismantling/crafting so duplicate drops have value before expanding the open world.

## Multiplayer guardrails

Keep authoritative combat results, inventory mutations and loot grants behind replaceable service boundaries. Local saves are suitable for the prototype only; an MMO version must move character state, drops and validation to the server.
