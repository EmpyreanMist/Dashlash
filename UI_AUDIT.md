# Phasebreak UI audit — 2026-09-19

This is a before/after audit of the UI quality pass. The baseline classification table below describes the pre-pass UI; the outcome section describes what changed. See `PROJECT_STATE.md` and `UI_FUNCTIONALITY_FOLLOWUPS.md` for current status and remaining QA.

Baseline: `dashlash` in Play Mode at 1280×720. Screenshots and runtime hierarchy were inspected before edits; code paths were checked for screens that require combat or loot state. Classification is for the current presentation, not a claim about underlying gameplay completeness.

| Interface | Status | Evidence / action |
|---|---|---|
| Normal gameplay HUD | NEEDS REDESIGN | Giant cyan band, persistent debug stats, overlapping right-side widgets; restore world-first hierarchy. |
| Player Frame | NEEDS POLISH | Functional, readable health/resource; blue outline is too heavy. Preserve binding and drag. |
| Target Frame | NEEDS POLISH | Existing bound frame/cast/rank implementation; align and tone down framing, verify target-only visibility. |
| World nameplates | NEEDS POLISH | Existing enemy and NPC labels, but tiny at normal camera distance; inspect occlusion/readability. |
| Action Bar | NEEDS REDESIGN | Five large wide slots print ability names over art; reduce to icon-first square slots with corner metadata. |
| Energy / resource | NEEDS POLISH | Functional but sits inside an oversized bar housing. |
| XP bar | NEEDS POLISH | Functional, subordinate; keep below actions and scale consistently. |
| Interaction prompt | NEEDS POLISH | Dungeon and NPC prompts are separate; avoid duplicates and hide during menus. |
| Quest tracker | BROKEN | Incorrect accent anchors produce a ~900 px cyan rectangle at 1280×720. Separate compact tracker from transient toasts. |
| Quest notifications | NEEDS REDESIGN | Same world UI layer as tracker, no dedicated faded toast treatment. |
| Minimap | NEEDS REDESIGN | Debug-like colored squares; overlaps Build Snapshot and major menus. Preserve marker coordinates. |
| Build Snapshot | DEBUG-ONLY | Permanent gameplay overlay; default it off with a developer toggle. |
| Inventory | NEEDS POLISH | Working grid, filters, icons, inspection and tooltips; world HUD bleeds over it. Keep existing structure. |
| Character | NEEDS POLISH | 16-slot paper doll and analysis work; excessive cyan silhouette and overlay bleed. |
| Corpse Loot | NEEDS POLISH | Existing shared slot/tooltip components and Take All; visual consistency and interaction need regression QA with a corpse. |
| Talents | NEEDS REDESIGN | Small graph and mostly empty details pane at 1280×720; background HUD bleeds over window. Preserve talent data/logic. |
| Quest Journal | NEEDS REDESIGN | One text block at top of a mostly empty window; create list/details hierarchy. |
| World Map | NEEDS REDESIGN | Crosshair axes, raw coordinates and square markers read as a debug plot. Preserve POI/quest positions. |
| Escape / Settings | NOT IMPLEMENTED | No game menu or settings panel is currently present; Esc only closes an open panel. Follow-up, not a fake menu. |
| Spellbook | NOT IMPLEMENTED | No spellbook or action-bar assignment UI exists. Follow-up. |
| HUD editor | NOT IMPLEMENTED | Frame drag/persistence exists; no unified edit mode/reset workflow. Follow-up. |
| Tooltips / item inspection | NEEDS POLISH | Inventory comparison is substantial; harmonize type, padding and modal layering. |
| Specialization controls | NEEDS POLISH | Character selection remains functional; check hit targets and active state at smaller resolutions. |

Known integration defect at baseline: Inventory/Character/Talents and Journal/Map are managed by separate modal controllers, so unrelated HUD remains visible and the two menu families can overlap.

## Quality-pass outcome

- HUD: quest stripe anchor fixed; compact tracker placed below a smaller minimap; Build Snapshot hidden by default behind F8; action bar converted to icon-first 64 px slots with contextual ability tooltip and subordinate XP. World/gameplay remains the visual focus.
- Modal UI: Inventory/Character/Talents and Map/Journal now close one another; both families apply a backdrop. Tracker, minimap and NPC prompt hide under major menus. The hot-reload singleton loss found during testing was fixed, and the menu transition sequence was retested from a fresh Play Mode session.
- Modal input: ability hotkeys, target selection and dungeon interaction are ignored while a major menu is open; regeneration and encounter progression remain active.
- Talents: larger graph nodes and more legible states, with a distinct selected-node border; first node selected by default, eliminating the empty white icon/detail pane. Character silhouette/accent reduced while retaining its equipment and analysis layout.
- Journal: list/detail hierarchy, active/completed status, objective, progress and reward display. Current campaign has six quests; the list presently shows up to seven entries without scrolling.
- Map: coordinate axes and raw coordinate footer removed; settlement/ruin/crypt labels, diamond markers and restrained road connections retained. The map is a schematic region view, not terrain cartography.
- Toast: temporary centered panel with a title/body hierarchy and fade; separate from persistent tracking.

Visual checks: HUD, Inventory, Character, Talents, Journal and Map were captured in Play Mode at 1280×720; Inventory at 1920×1080; HUD at 2560×1440; Talents at 2560×1080. These screenshots exposed and drove fixes to type size, panel height, glyph fallback, anchors and modal layering. No new external artwork was imported.

Functional checks: Unity recompilation passed. A Play Mode menu transition smoke test passed for Map → Inventory → Journal (only one major menu active). W movement was exercised through Input System injection and changed the player world position; the injected key was released. Phase Dash's direct ability call succeeded and consumed one charge in transient Play Mode state. A key-4 event while Inventory was open left its charges unchanged, although the physical keybinding itself was not conclusively verified. All five ability states returned a name, key, charge state and icon. Inventory filters returned 31/6/12/12/1 items for All/Weapons/Armor/Relics/Sigils. The three specialization trees loaded six nodes each; all six authored quests had unique IDs and valid prerequisites; 16 equipment channels were present. Inventory, Character, Talents, Map and Journal opened in Play Mode without a new exception. The Unity Test Runner reports zero automated tests. Full combat, quest, loot, dungeon and persistence flows were not safely reproducible in this pass and are explicitly tracked in `UI_FUNCTIONALITY_FOLLOWUPS.md`; their appearance here is not a claim of end-to-end verification.
