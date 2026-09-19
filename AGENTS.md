# PHASEBREAK project guidance

- Work on one scoped request at a time. Do not refactor unrelated systems or implement future tickets during a verification or documentation task.
- Unity project: open with Unity 6000.6.1f1. The playable starter world is `Assets/Phasebreak/Scenes/StarterZone_V2.unity`; `Assets/dashlash.unity` is the older fallback scene.
- Follow the documentation and ticket context policy below before changing systems.
- Preserve the existing gameplay owners in `Assets/Phasebreak/Scripts`: movement, health, combat, progression, build, talents, quests, enemies, and encounters. Extend those owners instead of storing duplicate gameplay state in UI or debug tools.
- Keep scene, prefab, ScriptableObject, and `.meta` files together. Preserve stable GUIDs. Do not commit `Library/`, `Temp/`, `Logs/`, local QA captures, or generated build output.
- Avoid extra packages unless the scoped task requires one. Keep interfaces between systems clear enough for a future authoritative multiplayer service without building one for this single-player prototype.
- MapMagic 2 lives in `Assets/MapMagic`. The V2 scene and graph are paired. If a scene genuinely fails to deserialize, use the checked-in `Phasebreak > Rebuild Complete Starter Zone V2` editor workflow and verify the saved scene afterward.
- Let Unity finish import and compile, then run focused Play Mode checks for the changed flow. Report what was observed and what remains untested.
- When finishing work, report files changed, tests run, manual checks, and remaining risks.
- The chat and command registry in `docs/reference/CHAT_COMMAND_REGISTRY_PROPOSAL.md` is a proposal, not an existing feature.

## Documentation policy

- Do not create new Markdown planning, audit, report, proposal, handoff, or status files unless explicitly requested.
- `PROJECT_STATE.md` is the factual current implementation snapshot. Update it only when implemented capability materially changes.
- `PHASEBREAK_ROADMAP.md` records development direction and milestones. Update it only when milestone status or development direction changes.
- GitHub Issues are the actionable backlog. `docs/reference/` holds specialist context, not parallel task lists.
- Do not create per-ticket completion report Markdown files. Report completion in the Codex response and/or PR.

## Normal implementation context

Before implementing a ticket, read:

1. `AGENTS.md`.
2. `PROJECT_STATE.md`.
3. The active GitHub Issue.
Read `PHASEBREAK_ROADMAP.md` only when broader milestone context is needed. Read `docs/reference/` only when the active issue explicitly requires it or repository investigation shows it is necessary. Do not automatically read `docs/reference/PHASEBREAK_MASTER_FEATURE_CATALOG.md` or every Markdown file in the repository.
