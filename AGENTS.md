# PHASEBREAK project guidance

- Work on one scoped request at a time. Do not refactor unrelated systems or implement future tickets during a verification or documentation task.
- Unity project: open with Unity 6000.6.1f1. The playable starter world is `Assets/Phasebreak/Scenes/StarterZone_V2.unity`; `Assets/dashlash.unity` is the older fallback scene.
- Read `PROJECT_STATE.md` before changing systems. It records the verified baseline and known gaps. `PHASEBREAK_ROADMAP.md` is the short plan; `PHASEBREAK_MASTER_FEATURE_CATALOG.md` is the longer design backlog.
- Preserve the existing gameplay owners in `Assets/Phasebreak/Scripts`: movement, health, combat, progression, build, talents, quests, enemies, and encounters. Extend those owners instead of storing duplicate gameplay state in UI or debug tools.
- Keep scene, prefab, ScriptableObject, and `.meta` files together. Preserve stable GUIDs. Do not commit `Library/`, `Temp/`, `Logs/`, local QA captures, or generated build output.
- Avoid extra packages unless the scoped task requires one. Keep interfaces between systems clear enough for a future authoritative multiplayer service without building one for this single-player prototype.
- MapMagic 2 lives in `Assets/MapMagic`. The V2 scene and graph are paired. If a scene genuinely fails to deserialize, use the checked-in `Phasebreak > Rebuild Complete Starter Zone V2` editor workflow and verify the saved scene afterward.
- Let Unity finish import and compile, then run focused Play Mode checks for the changed flow. Report what was observed and what remains untested.
- When finishing work, report files changed, tests run, manual checks, and remaining risks.
- The chat and command registry in `CHAT_COMMAND_REGISTRY_PROPOSAL.md` is a proposal, not an existing feature.
