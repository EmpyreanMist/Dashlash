# PHASEBREAK UI and functionality follow-ups — 2026-09-19

`PROJECT_STATE.md` is the current implementation baseline. This list tracks work and regression coverage still open after the UI pass.

This is the honest remainder after the UI quality pass, not a claim that the systems below failed.

## Missing interfaces

- A local chat/command console is proposed but not implemented. `CHAT_COMMAND_REGISTRY_PROPOSAL.md` defines its input-focus behavior, command registry, access policy and system adapters. Focus testing must cover every independently bound gameplay key, including F8 and arena reset R, plus camera mouse steering.
- No unified Escape/game menu or applied Settings screen. Esc currently closes an open major panel; when none is open it does not open a game menu. Camera sensitivity, zoom speed, invert Y, UI scale, screen shake, keybindings and resolution/fullscreen therefore have no in-game settings QA.
- No Spellbook or action-bar assignment UI. The current combat/action bar has five bound abilities; do not imply a 20-slot assignment system exists.
- No unified HUD edit mode, reset-position/reset-all workflow or HUD-scale control. Existing frame dragging/persistence should be regression-tested when this is built.

## Presentation still worth improving

- The minimap and world map are readable schematics but still lack authored terrain art and per-marker hover/selection information. Use a coherent, explicitly licensed map art family if later added.
- Character analysis becomes small at 1280×720 because many stats share a narrow panel. A compact two-column stat layout would improve legibility without changing equipment logic.
- World nameplates at distance and the strong cyan Player/Target frame outlines deserve a focused visual pass in a real gameplay run.
- Journal currently presents at most seven rows. The six-quest authored campaign fits; add a ScrollRect before expanding quest count.
- Ability tooltip is local to the action bar; item/talent/map tooltip styles are not yet one shared component.

## Regression coverage to complete

- Create automated Edit/Play Mode tests; Unity Test Runner currently discovers zero tests.
- Manually exercise W/S, A/D, Q/E, jump and air control; camera orbit/RMB steering/zoom/collision/sensitivity; mouse and Tab/Shift+Tab targeting; dead-target handling; all five abilities, costs, cooldowns, charges, range, damage, crit, death and weapon presentation. Only W movement was directly exercised in this pass.
- Verify inventory select/tooltip/comparison/equip/unequip/save, interactions in all 16 Character equipment slots and sets/modifiers, Talent purchase/prerequisites/ranks/effects/respec/save-load. Filter counts, three tree loads and the 16-channel enum were checked without mutation; changing the user's existing build/save for a destructive test was intentionally avoided.
- Play through quest accept/update/turn-in and NPC range checks, temporary notifications, loot-one/Take All/no duplicate loot, Rift Crypt encounter/boss/cache, and save/reload for inventory/equipment/specialization/talents/quests. The current PlayerPrefs data was not reset.
- Test HUD/tooltip hit areas with a pointer, Escape hierarchy using physical keys, and every menu at 1280×720, 1920×1080, 2560×1440 and ultrawide in a packaged build. Camera-based captures were inspected here; no packaged build QA was performed.
