# Proposed next step: local chat and developer commands

This proposal follows the implemented baseline in `PROJECT_STATE.md`. It is future work and does not change the current controls.

Status: proposal only. No chat, command registry, or developer command from this document is implemented yet.

## Recommendation

This is a useful, contained next milestone for the current single-player vertical slice. It gives testers a way to travel, inspect, and recover while exercising the existing world. Keep ordinary messages explicitly **local**: there is no network chat service or other player to receive them. Developer commands should be unavailable in a release build unless an explicit development entitlement enables them.

Build the shared input-focus foundation first, then the registry and safe commands. Treat flight, noclip, forced level changes, and mass enemy actions as a second pass because they alter core movement, progression, or encounter state.

## What the current game provides

| Existing system | Reuse / required adapter |
| --- | --- |
| `PhasebreakPlayerMovement` and `CharacterController` | Movement reads its own `InputAction`s. Add a transient debug speed multiplier and a controlled movement mode with reliable restoration; do not change equipment movement bonuses. |
| `PhasebreakFollowCamera`, `PlayerCombat`, `PlayerTargeting`, `PhasebreakInventoryHud`, `WorldQuestHud`, `QuestJournal`, `RiftDungeonController`, `CombatArenaReset` | Each reads input independently. All need a shared gameplay-input gate while chat has focus. Camera mouse steering and Escape priority also need checking. |
| `PlayerHealth` | `TakeHit` handles mitigation, brief invulnerability, and `Died`; `ResetHealth` fully heals. Add an explicit transient debug invulnerability flag. A forced `/kill` path must still invoke the real death event, even if godmode is on. |
| `PlayerCombat` | Energy is `currentResource` with a public read-only property. Add a bounded debug setter on this owner for `/energy`; do not keep a duplicate chat-side value. |
| `PlayerProgression` | `GrantExperience` is the real XP and save path; current scene supports levels 1–10. `/level` needs a dedicated validated debug setter that reapplies level benefits, emits progression events, and uses the same persistence path. |
| `MeleeEnemy` and `FrontierEncounterZone` | Enemy death currently grants XP and loot. `/killall` therefore needs a reward-free death/reset path rather than calling `ReceiveHit` as-is. Encounter reset must own its spawn slots and timers; merely destroying active objects can cause immediate respawns. |
| `RiftDungeonController` and `CombatArenaReset` | Dungeon death resets at the active checkpoint; the arena has its own reset flow. `/respawn` must select the current context rather than assuming one world spawn. |
| `QuestLocation` and V2 landmarks | Use stable landmark IDs or a small authored location catalog for `/tp <location>`. Include `spawn`, `northgate`, `westmere`, `eastwatch`, `ancientruins`, and `riftcrypt`; validate arrival on terrain outside the dungeon. |

## Chat and input contract

- Enter focuses the input. Enter submits; Escape cancels and returns focus to gameplay. Empty input closes without adding a message. Plain text adds a local message. A leading `/` is parsed as a command; show a clear result or usage error in chat.
- Use one central `GameplayInputBlocked`/focus authority, with the chat input owning focus until it closes. Every gameplay consumer above must respect it. Clear held movement, buffered jumps, and mouse steering when focus changes so a key held before opening chat does not keep moving the player.
- Define Escape priority: chat first, then an open gameplay panel, then target clearing. Chat submission must not fire an ability, interaction, menu shortcut, target cycle, jump, or arena reset on the same key event.
- Keep a bounded local display log and separate session-only input history. Up/Down browses history only when the chat field is focused; editing an entry should not mutate stored history. `/clear` clears displayed lines, including local command output, without altering game state.
- A compact bottom-left panel with a small scrollback and unobtrusive input fits the present HUD. The text field should remain readable at 1280×720 and while the world map or inventory is open. Command autocomplete can filter registry names and aliases after `/`; Tab accepts a suggestion only while chat owns focus.
- Keep local messages visually distinct from command feedback and errors. Never describe a local message as sent to other players.

## Registry and command behavior

Each command defines a canonical name, aliases, description, usage, category, `developerOnly`, and an execute callback. Register names case-insensitively, reject duplicate names/aliases at startup, and parse quoted arguments for multiword locations. The callback returns success/failure text; exceptions become a brief error in chat and a full development log entry. `/help` is generated from visible registrations; `/help godmode` shows that command's usage, aliases, description, and access level. Unknown names and malformed numbers return usage without changing state. Clamp or reject NaN, infinity, negative XP, extreme speed, unsafe coordinates, and out-of-range levels.

| Category | Commands | Important behavior |
| --- | --- | --- |
| General | `/help`, `/clear` | Available to all local players. |
| Player | `/godmode [on\|off]`, `/heal`, `/energy [amount]`, `/damage <amount>`, `/kill`, `/respawn`, `/unstuck` | Decide whether omitted optional values toggle, show current state, or refill; document that choice in generated help. `/damage` should use normal damage rules unless explicitly named otherwise. `/unstuck` should use a tracked recent safe grounded position, then a known safe spawn fallback. |
| Movement | `/speed [multiplier\|reset]`, `/fly [on\|off]`, `/noclip [on\|off]` | Debug-only state. Reset transient modes on scene change and Play Mode restart. Noclip and fly must preserve and restore CharacterController collision/gravity and clear movement velocity. |
| World | `/coords [on\|off]`, `/where`, `/tp <x> <y> <z>`, `/tp <location>` | `/where` reports scene, XYZ, and nearest landmark; `/coords` is a tiny overlay. Teleport through one service that temporarily disables the CharacterController and resets motion. Reject coordinates outside the current playable bounds unless an explicit dungeon target is used. |
| Progression | `/xp <amount>`, `/level <level>` | Use `GrantExperience`; `/level` requires a controlled setter. Treat these as save-affecting developer operations and show the resulting level/XP. |
| Debug | `/fps [on\|off]`, `/hud [on\|off]`, `/enemies`, `/killall`, `/resetencounters` | `/enemies` should distinguish active living enemies from authored encounter capacity. Keep chat accessible when `/hud off`; command feedback needs an escape hatch. Default `/killall` to the active world area, exclude the dungeon unless explicitly requested, and grant no XP or loot. |

Mark gameplay-changing and debug commands `developerOnly` (`/godmode` through `/resetencounters`, except ordinary `/where` if desired). This is a proposed access policy, not an existing authentication system. Do not use a client-only flag as security once multiplayer exists; authoritative commands would require server permission later.

## Suggested implementation order and acceptance checks

1. Shared focus gate and chat panel: type `wasd 12345 becjt r`, press Space/Tab, use mouse buttons, and verify no gameplay action occurs while focused. Verify Escape and Enter do not leak to other consumers.
2. Registry, generated help, errors, aliases, autocomplete, and bounded history. Verify ordinary local text, `/help`, `/help godmode`, `/clear`, unknown command, quoted and malformed arguments.
3. Read-only/debug display and safe recovery commands: `/where`, `/coords`, `/fps`, `/enemies`, `/heal`, `/energy`, `/unstuck`, and validated teleport. Test both `StarterZone_V2` and the preserved old scene.
4. Mutating commands: godmode, damage, kill/respawn, XP/level, speed, then fly/noclip and reward-free encounter controls. Test toggles repeatedly, scene transition, dungeon checkpoint, save/reload, and that normal collision and movement return after disabling a mode.

Useful additions not in the draft: a small `LastSafeGroundedPosition` tracker for `/unstuck`; scene/context-aware teleport and respawn; a command access policy; a no-reward enemy defeat API; and a defined input priority across chat, menus, targeting, and the dungeon. These are prerequisites for reliable behavior rather than extra gameplay features.
