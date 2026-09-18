# PHASEBREAK — Master Feature Catalog

> Levande funktionskatalog, designmeny och statuslista för hela projektet.  
> Senast uppdaterad: 2026-09-18. Senast committade baslinje: `5ff53dc`; zoom, 420 × 420-startregion, no-knockback och den första återanvändbara zombie-fienden är implementerade men ännu inte committade.

## Så används dokumentet

Det här är projektets centrala lista över befintliga system, planerade system och rimliga alternativ som kan väljas senare. Den beskriver både gameplay, content, UX, grafik, sprites, animation, ljud, teknik, multiplayer och produktion.

Status:

- ✅ **Klart** — implementerat och manuellt godkänt för nuvarande fas.
- 🟨 **Prototyp** — fungerar, men behöver mer content, polish eller robusthet.
- 🛠 **Pågår** — aktivt arbete, ännu inte godkänt.
- ⬜ **Kandidat** — möjlig framtida funktion, inte beslutad.
- 🔀 **Beslut krävs** — alternativen bör väljas innan implementation.
- 🧊 **Senare** — avsiktligt uppskjutet.
- ❌ **Bortvalt** — ska inte byggas om beslutet inte ändras.

Checkboxar används för val:

- `[x]` valt eller genomfört.
- `[ ]` tillgängligt alternativ.
- Flera alternativ kan väljas när de inte är ömsesidigt uteslutande.

Varje större funktion har ett stabilt ID. Använd gärna ID:t i framtida uppdrag.

---

## 0. Produktvision och designpelare

### VISION-01 — Grundfantasi ✅

- Tredjepersons action-RPG i en sammanhängande open world.
- Spelaren utforskar världen, hittar dungeons, besegrar bossar, samlar loot och skapar egna builds.
- MMO-läsbarhet med targets, nameplates, action bar, roller och gruppcontent.
- Egen identitet genom Core, Relics, Sigils, Tags, Sets och Wildcard Artifacts.
- Arkitekturen ska kunna utvecklas till ett riktigt online-MMORPG.

### VISION-02 — Spelpelare

- [x] Responsiv tredjepersonsrörelse.
- [x] Target-baserad strid med aktiv positionering.
- [x] Build-förändrande utrustning.
- [x] Dungeons med encounters och boss.
- [ ] Stor värld med tydliga regioner och hemligheter.
- [ ] Klass- och talentidentitet.
- [ ] Långsiktig lootjakt och endgame.
- [ ] Socialt multiplayer/MMO-lager.

### VISION-03 — Ton och visuell riktning 🔀

Välj primär riktning:

- [ ] **Arcane sci-fantasy** — magi, uråldrig teknologi, rifts och energikärnor. Rekommenderad utifrån nuvarande namn/design.
- [ ] **Dark heroic fantasy** — ruiner, förbannelser, mörka dungeons och tyngre rustningar.
- [ ] **Bright stylized fantasy** — mer färg, större former och bred tillgänglighet.
- [ ] **Cosmic void fantasy** — surrealistiska världar, dimensioner och extrema VFX.

Gemensamma krav:

- Silhuetter ska vara läsbara på MMO-kameraavstånd.
- Ability-färger ska signalera funktion, inte bara element.
- Player-, enemy- och danger-VFX måste vara lätta att skilja åt.
- UI ska kännas Phasebreak, inte kopiera World of Warcraft.

---

## 1. Kärnloop och sessionsstruktur

### LOOP-01 — Grundloop 🟨

1. Utforska open world.
2. Hitta aktivitet, fiende, quest eller dungeon.
3. Slåss och använd buildens styrkor.
4. Loot corpses/chests/boss rewards.
5. Jämför, utrusta, spara eller dismantla items.
6. Få XP, levels, talents och nya buildmöjligheter.
7. Klara svårare content och jaga unika rewards.

Nuvarande status:

- [x] Combat.
- [x] XP till level 3.
- [x] Loot och equipment.
- [x] En dungeonprototyp.
- [ ] Open-world exploration loop.
- [ ] Quest loop.
- [ ] Craft/dismantle loop.
- [ ] Endgame loop.

### LOOP-02 — Sessionsformat 🔀

- [ ] Sömlös värld med dungeonportaler.
- [ ] Världszoner med laddningsövergångar.
- [ ] Hub + expeditions.
- [ ] Hybrid: öppen overworld, instansierade dungeons. **Rekommenderad.**

### LOOP-03 — Failure och återhämtning ⬜

- [x] Dungeon-checkpoint och reset finns i prototyp.
- [ ] Corpse run.
- [ ] Respawn vid närmaste shrine.
- [ ] Gear durability loss.
- [ ] XP/guld-förlust.
- [ ] Ingen permanent förlust; endast encounter reset. **Rekommenderad tidigt.**

---

## 2. Input, kamera och gameplay-menyer

### INPUT-01 — Kontroller ✅

- `W/S` — framåt/bakåt.
- `A/D` — keyboard turn.
- `Q/E` — strafe.
- `Space` — jump.
- `1/2/3` — Strike, Crushing Blow och Phase Lunge.
- `4` — Phase Dash (två charges).
- `5` — Rift Charge.
- `Tab` / `Shift+Tab` — target cycling.
- Vänster musdrag — orbit camera utan character turn.
- Höger musdrag — orbit camera och character turn.
- Vänster + höger musknapp — gå framåt.
- Mushjul — zoom.
- `B` — Inventory.
- `C` — Character.
- `T` — Talents-placeholder.
- `Escape` — stäng gameplay-meny/clear target.
- Högerklick på corpse — loot.

### INPUT-02 — Inputförbättringar ⬜

- [ ] Full key rebinding.
- [ ] Separata primary/secondary bindings.
- [ ] Gamepad.
- [ ] Controller glyph switching.
- [ ] Mouse sensitivity X/Y separat.
- [ ] Invert Y.
- [ ] Hold/toggle mouse-look.
- [ ] Action-bar keybinds 1–0, modifiers och extra bars.
- [ ] Click-to-move som valbart alternativ.
- [ ] Input buffering för abilities.

### CAMERA-01 — Tredjepersonskamera ✅

- [x] Orbit, zoom och collision.
- [x] Plattformsnormaliserad snabbzoom: 3,5 world units per scrollsteg och 0,05–18 zoomgräns, så kameran kan passera in genom spelarmodellen.
- [x] RMB character steering.
- [x] LMB endast camera/target, inte attack.
- [x] Camera impulse vid hits/crits.
- [ ] Shoulder swap.
- [ ] Dynamic combat zoom.
- [ ] Lock-on camera mode.
- [ ] Camera presets per aktivitet.
- [ ] Photo mode.

### MENU-01 — Gameplay menu controller ✅

- [x] Endast en major panel öppen samtidigt.
- [x] Inventory/Character/Talents/Loot delar cursor-regler.
- [x] HUD-navigation med hover-tooltip.
- [x] Ingen gameplay-pause via menyer.
- [ ] Settings-panel.
- [ ] Quest journal-panel.
- [ ] Map-panel.
- [ ] Social-panel.
- [ ] Codex/collection-panel.

---

## 3. Player movement och traversal

### MOVE-01 — Basrörelse ✅

- [x] CharacterController-baserad movement.
- [x] Acceleration/deceleration.
- [x] Camera-relative steering.
- [x] Keyboard turning och strafe.
- [x] Gravity, jump buffering och coyote time.
- [x] Air control.
- [x] Ability-driven Phase Dash med collision, två charges och air-dash-regler.
- [x] Ingen separat movement-keybind för dash; movement exekveras genom ability-systemet.

### MOVE-02 — Fördjupning ⬜

- [ ] Sprint med stamina.
- [ ] Crouch/stealth.
- [ ] Mantling/vaulting.
- [ ] Climbing.
- [ ] Swimming/diving.
- [ ] Gliding.
- [ ] Grapple points.
- [ ] Wall run.
- [ ] Slide.
- [ ] Ledge grab.
- [ ] Knockdown/get-up states.

### MOVE-03 — World traversal 🔀

- [ ] Mounts.
  - [ ] Ground mounts.
  - [ ] Flying mounts.
  - [ ] Mount abilities.
  - [ ] Mount collection/customization.
- [ ] Fast travel shrines.
- [ ] Portals mellan regioner.
- [ ] Player hearth/return ability.
- [ ] Public transport: airship, caravan eller rift rail.

---

## 4. Targeting, nameplates och combat readability

### TARGET-01 — Targeting ✅

- [x] Mouse selection.
- [x] Tab targeting och reverse cycling.
- [x] Auto-target när ability används mot fiende framför spelaren.
- [x] Range/cone/line-of-sight validation.
- [x] Corpses är inte giltiga combat targets.
- [ ] Focus target.
- [ ] Soft target separat från hard target.
- [ ] Target-of-target.
- [ ] Friendly targets.
- [ ] Raid markers.
- [ ] Priority-target scoring.

### NAMEPLATE-01 — Unit readability 🛠

- [x] Modulär dark-fantasy Player Frame med porträttplats, namn, level, HP, Energy och class-resource-pips.
- [x] Modulär hostile Target Frame med porträttplats, namn, level, HP, cast bar, rank och statusplatser.
- [x] Högerklick för att låsa upp/låsa Player/Target Frame; vänsterdrag flyttar och sparar positionen lokalt.
- [x] Enemy world nameplates via en separat `WorldNameplateUI`, förberedd för fortsatt in-world-utbyggnad.
- [x] Selection ring.
- [x] Cast bar kopplad till nuvarande enemy windup via ett litet presentation-interface.
- [x] Rare/elite/boss-indikator och bossdetektering.
- [x] Interruptible cast styling.
- [ ] Live buff/debuff icons bound to target gameplay status state.
- [ ] Threat/aggro indicator.
- [ ] Status stacks, durations, dispel type och tooltips.
- [x] Data- och view-grund för statusikon, duration, stacks och hovertext; live statusbinding till Target Frame återstår.
- [ ] Nameplate stacking och occlusion.
- [ ] Friendly/player nameplates för multiplayer.
- [ ] Target-of-target, focus, party, raid och separat boss frame.
- [ ] HUD edit mode med reset, snap/grid, scale och namngivna layoutprofiler.
- [ ] Portrait provider för riktiga render textures eller porträtt-sprites.
- [ ] Färgblindhetsprofiler och alternativa friendly/hostile-paletter.

---

## 5. Combat system

### COMBAT-01 — Combat core ✅

- [x] Ability activation via keyboard.
- [x] Resource cost och regeneration.
- [x] Global och ability cooldowns.
- [x] Range och line-of-sight.
- [x] Hit reaction, knockback och hit stop.
- [x] Normal och critical damage.
- [x] Floating damage numbers.
- [x] Soft auto-target.
- [x] Boss damage modifier.
- [x] Build effects kopplade till combat.
- [x] Vanliga enemy-attacker ger hit-feedback utan player displacement; särskilda bossattacker kan fortfarande begära knockback explicit.

### COMBAT-02 — Nuvarande abilities ✅

- `Strike` — basic single-target attack.
- `Crushing Blow` — tyngre hit; kan få cleave via build.
- `Phase Lunge` — mobility attack; kan få cooldown reduction/extra charge/reset.
- `Phase Dash` — targetlös mobilitetsability med två charges och individuell recharge.
- `Rift Charge` — target-baserad rush från 4–14 meter som stannar vid melee-avstånd och träffar målet.

### COMBAT-02B — Presentation och feedback 🟨

- [x] Ability-events för start, impact, complete och failure är separerade från presentationen.
- [x] Procedurala, utbytbara poses för Strike, Crushing Blow, Phase Lunge, Phase Dash och Rift Charge.
- [x] Additiv spelarreaction för hit och death utan att ersätta locomotion-controllern.
- [x] Träffögonblick, slash, camera impulse, hit-stop och damage numbers drivs från samma ability-timing.
- [x] Enkel genererad prototypaudio för swing, impact, mobility och nekad ability.
- [x] HUD-feedback för cooldown, charges, resource/range/target-fel och fem action-bar-slots.
- [ ] Ersätt procedurala poses med licensierade eller egenproducerade humanoid-clips.
- [ ] Riktigt Animator upper-body/avatar-mask-lager för attacks while moving.
- [ ] Animation events eller normalized-time markers per clip för exakt hit timing.
- [ ] Besluta cancel windows, input buffering och root-motion-policy per ability.

### COMBAT-03 — Combatregler att välja 🔀

- Global cooldown:
  - [x] Finns i prototyp.
  - [ ] Behåll som klassisk MMO-GCD.
  - [ ] Kort GCD + animation locks.
  - [ ] Ingen universell GCD, endast individuella cooldowns.
- Basic attacks:
  - [x] Ingen attack på vänsterklick.
  - [ ] Aktiv basic ability på keybind.
  - [ ] Auto-attack efter target + combat start.
  - [ ] Hybrid där vissa klasser auto-attackerar.
- Aim:
  - [x] Primärt target-baserat.
  - [ ] Ground-target abilities.
  - [ ] Skillshots/projectiles.
  - [ ] Cone/line/area abilities.

### COMBAT-04 — Framtida mechanics ⬜

- [ ] Cast time/channeling.
- [ ] Interrupts och interrupt immunity.
- [ ] Dodge/parry/block.
- [ ] Armor och resistances.
- [ ] Healing och shields.
- [ ] Crowd control: stun, root, slow, silence, fear, pull, knock-up.
- [ ] Diminishing returns för PvP.
- [ ] Threat/aggro.
- [ ] Combo points eller class resources.
- [ ] Damage-over-time/heal-over-time.
- [ ] Proc system med internal cooldown.
- [ ] Snapshotting-regler.
- [ ] Combat log.
- [ ] Training dummy/stat parser.

### COMBAT-05 — Status effects ⬜

- [ ] Bleed.
- [ ] Burn/Fire.
- [ ] Void exposure.
- [ ] Chill/Freeze.
- [ ] Shock/chain lightning.
- [ ] Poison/corrosion.
- [ ] Vulnerable.
- [ ] Fortified.
- [ ] Haste/slow.
- [ ] Cleanse/dispel categories.

---

## 6. Classes, specializations och roles

### CLASS-01 — Nuvarande bas 🟨

- [x] Vanguard-prototyp.
- [x] Level 3 specialization choice.
- [x] Berserker — damage/crit.
- [x] Bulwark — HP/defense.
- [x] Riftblade — mobility/cooldown/Phase Lunge.
- [ ] Full ability kits per specialization.
- [ ] Spec-specific visuals och resource.
- [ ] Spec switching rules.

### CLASS-02 — Möjliga launch-klasser 🔀

- [ ] **Vanguard** — melee weapon master.
  - Berserker, Bulwark, Riftblade.
- [ ] **Arcanist** — ranged magic/control.
  - Pyromancer, Chronomancer, Voidcaller.
- [ ] **Ranger** — ranged weapon/traps/pet.
  - Marksman, Warden, Beastbinder.
- [ ] **Oracle** — support/healing/light-dark duality.
  - Luminary, Seer, Soulweaver.
- [ ] **Shade** — stealth/bleed/teleport.
  - Assassin, Nightblade, Trickster.
- [ ] **Artificer** — gadgets/turrets/core manipulation.
  - Mechanist, Alchemist, Rift Engineer.

Rekommenderad första vertical slice:

- [ ] Vanguard komplett först.
- [ ] Därefter en ranged DPS och en healer/support för partytest.

### CLASS-03 — MMO-roller ⬜

- [ ] Tank.
- [ ] Healer.
- [ ] Melee DPS.
- [ ] Ranged DPS.
- [ ] Support/controller.
- [ ] Alla builds solo-viable men olika gruppstyrkor.

### CLASS-04 — Character creation ⬜

- [ ] Body presets.
- [ ] Face/head presets.
- [ ] Hair och colors.
- [ ] Skin/material variants.
- [ ] Voice set.
- [ ] Pronouns/name.
- [ ] Class selection.
- [ ] Origin/background med mindre bonuses eller endast story.
- [ ] Preview animation och lighting.

---

## 7. Stats, resources och build calculation

### STAT-01 — Implementerade stats ✅

- [x] Power.
- [x] Maximum Health.
- [x] Defense.
- [x] Critical Chance.
- [x] Critical Damage.
- [x] Attack Speed.
- [x] Movement Speed/Mobility.
- [x] Boss Damage.
- [x] Energy och regeneration.
- [x] Build Summary summerar level/passive/spec/gear/sets/modifiers.

### STAT-02 — Kandidater ⬜

- [ ] Haste som gemensam cooldown/attack/cast-stat.
- [ ] Mastery unik per specialization.
- [ ] Armor penetration.
- [ ] Elemental penetration.
- [ ] Lifesteal.
- [ ] Healing power.
- [ ] Shield power.
- [ ] Resource generation.
- [ ] Cooldown recovery.
- [ ] Status chance/potency.
- [ ] Luck/item find.
- [ ] Tenacity/CC reduction.

### STAT-03 — Scalingregler 🔀

- [ ] Additiva bonusar inom samma kategori, multiplikativa mellan kategorier. **Rekommenderad.**
- [ ] Diminishing returns för crit/haste/defense.
- [ ] Hard caps.
- [ ] Soft caps.
- [ ] Item-level scaling i open world.
- [ ] Ingen world scaling; zoner har fasta nivåer.
- [ ] Hybrid scaling per region/difficulty.

---

## 8. Leveling, talents och långsiktig progression

### PROG-01 — Leveling 🟨

- [x] XP från kills.
- [x] Level 1–3 prototype.
- [x] Level-based power/health.
- [x] Passive vid level 2.
- [x] Specialization vid level 3.
- [ ] Slutlig level cap.
  - [ ] 30 för tight initial release.
  - [ ] 50.
  - [ ] 60 klassisk MMO-struktur.
- [ ] XP från quests, exploration och dungeons.
- [ ] Rested XP.
- [ ] Account-wide catch-up.

### TALENT-01 — Talent UI hook ✅

- [x] `T` och HUD-knapp.
- [x] Placeholder panel.
- [ ] Full talent tree.

### TALENT-02 — Talentmodell 🔀

- [ ] Klassiska branching trees med prerequisites.
- [ ] Talent rows där ett val görs per tier.
- [ ] Constellation/grid där noder kopplas fritt.
- [ ] Ability-mod system med separata modifiers.
- [ ] Hybrid: class tree + ability augments. **Rekommenderad.**

Möjliga nodtyper:

- [ ] Stat node.
- [ ] Ability unlock.
- [ ] Ability modifier.
- [ ] Keystone som förändrar spelstil.
- [ ] Trigger/proc.
- [ ] Tag synergy.
- [ ] Defensive utility.
- [ ] Party aura.
- [ ] Resource conversion.

### PROG-02 — Endgame progression ⬜

- [ ] Paragon/legacy levels.
- [ ] Account mastery.
- [ ] Dungeon difficulty tiers.
- [ ] Seasonal character progression.
- [ ] Horizontal unlocks.
- [ ] Collection bonuses.
- [ ] Prestige cosmetics.

---

## 9. Items, inventory och equipment

### ITEM-01 — Item definitions ✅

- [x] ScriptableObject item definitions.
- [x] Stable item ID.
- [x] Name, description, slot, rarity och item level.
- [x] Stats, tags, set och build effects.
- [x] Sprite icon; alla 10 nuvarande items har unik importerad ikon.
- [x] Optional `visualPrefab` för utrustad 3D-representation.
- [x] Datadriven slot-specifik fallback när Sprite saknas, med procedurgenererad silhuett endast som sista säkerhetsnät.

### ITEM-02 — Equipment slots ✅

- [x] Primary Weapon.
- [x] Secondary.
- [x] Head.
- [x] Shoulders.
- [x] Chest.
- [x] Hands.
- [x] Legs.
- [x] Boots.
- [x] Core.
- [x] Mobility Relic.
- [x] Power Relic.
- [x] Utility Relic.
- [x] Sigil 1/2/3.
- [x] Wildcard Artifact.

### ITEM-03 — Inventory UX ✅

- [x] Bag grid.
- [x] Icon-first slots.
- [x] Rarity visualization.
- [x] Hover tooltip.
- [x] Green/red equipped comparison.
- [x] Deterministisk, återanvändbar upgrade-bedömning som jämför hela loadouten före/efter bytet.
- [x] Upgrade-bedömningen räknar in stats, item level, effects/modifiers och aktiverade set-trösklar.
- [x] Tydlig men diskret grön upgrade-outline i Inventory och Corpse Loot, plus upgrade-count i bag-headern.
- [x] Vänsterklick för selection/inspection.
- [x] Högerklick eller dubbelklick för equip/unequip.
- [x] Separat Character screen.
- [x] 16-slot paper-doll-layout runt en karaktärssilhuett.
- [x] Scrollbar bag-grid med tomma slots och item-count.
- [x] Item inspection-panel med tydlig primary action.
- [x] Lokal save.
- [ ] Multiple bags/tabs.
- [ ] Search.
- [ ] Sort by rarity/slot/item level/tag.
- [x] Kategorifilter: All, Weapons, Armor, Cores/Relics och Sigils.
- [ ] Favorite/lock item.
- [ ] Mark as junk.
- [ ] Stackable items.
- [ ] Split stacks.
- [ ] Drag and drop.
- [ ] Context menu.
- [ ] Inventory capacity upgrades.
- [ ] Account stash/bank.

### ITEM-04 — Raritymodell 🔀

Nuvarande: Common, Uncommon, Rare, Epic, Mythic.

- [ ] Rarity styr endast affix count/roll quality.
- [ ] Rarity kan även låsa specialeffekter.
- [ ] Uniques utanför normal rarity.
- [ ] Set items som egen quality eller vanlig rarity + set flag.
- [ ] Mythic får endast en equipped åt gången.

### ITEM-05 — Item instances, rolls och affixes ⬜

- [ ] Unikt instance-ID.
- [ ] Base definition + rolled instance data.
- [ ] Random item level inom loot table.
- [ ] Prefix.
- [ ] Suffix.
- [ ] Min/max rolls.
- [ ] Seeded generation för server verification.
- [ ] Affix tiers.
- [ ] Slot/tag eligibility.
- [ ] Smart loot baserat på class/spec.
- [ ] Rare off-build drops för experimentation/trading.

Exempel på affixfamiljer:

- Riftforged — Void/Phase Lunge.
- Bloodletter — Bleed/crit.
- Emberbound — Fire/burn.
- Fleet — mobility/dash.
- Colossus — boss damage/defense.
- Energized — resource generation.
- Fracturing — cleave/area damage.
- Warding — mitigation/shields.

### ITEM-06 — Tags ✅/⬜

Implementerade:

- [x] Void, Crit, Mobility, Bleed, Fire, Defense, Energy, Boss.

Kandidater:

- [ ] Frost, Shock, Poison, Holy, Shadow.
- [ ] Melee, Ranged, Spell, Summon.
- [ ] Tank, Heal, Support.
- [ ] Dash, Teleport, Channel, Projectile, Area.
- [ ] Execute, Barrier, Minion, Trap.

### ITEM-07 — Sets 🟨

- [x] Sets kan bestå av valfria slots.
- [x] 2/3/4/6-piece support.
- [x] Specialeffekter och stats.
- [x] Riftstalker Circuit.
- [ ] Set collection UI.
- [ ] Set transmog unlocks.
- [ ] Set upgrade/crafting.
- [ ] Multiple competing sets per class.
- [ ] Partial-set balancing.

### ITEM-08 — Build-defining gear ✅/⬜

- [x] Core kan ändra ability behavior.
- [x] Relics kan reagera på combat events.
- [x] Artifact kan ge tag synergy och stora modifiers.
- [ ] Cores med ability replacement.
- [ ] Relics med aktiva abilities.
- [ ] Artifacts med både bonus och drawback.
- [ ] Sigil combinations/recipes.
- [ ] Corrupted items med risk/reward.

### ITEM-09 — Visuellt equipment 🟨

- [x] Equipment-state synkas till spelarens visuella rig utan att duplicera equip-logik.
- [x] Attachment/visual slots för Primary, Secondary, Head, Shoulders, Chest, Hands, Legs och Boots.
- [x] Höger/vänster hand samt rygg/höft för draw och sheathing.
- [x] Testvapen `Rift-Iron Edge` och testbröst `Bulwark Chest` har visuella prefabs.
- [x] Weapon draw vid ability-start och automatisk sheath vid complete.
- [ ] Separata modulära armor-meshar för samtliga slots.
- [ ] Dölj rätt basmesh-region när en armor-del ersätter kroppen.
- [ ] Per-item attachment offset/rotation/scale för olika vapentyper.
- [ ] Tvåhandsvapen, dual wield, sköldar, bågar och off-hand-regler.
- [ ] Cosmetic/transmog-lager separat från gameplay equipment.
- [ ] Dye/material variants och preview i Character-skärmen.

---

## 10. Loot, corpses och rewards

### LOOT-01 — Corpse looting ✅

- [x] Varje corpse äger sin egen loot.
- [x] Right-click öppnar loot window.
- [x] Hover-highlight.
- [x] Loot one och Take All.
- [x] Ingen dubbel-loot.
- [x] Corpses räknas döda omedelbart.
- [x] Corpse blockerar inte targeting eller movement.
- [x] 60 sekunders konfigurerbar lifetime.
- [x] Despawn väntar om dess loot window är öppet.
- [x] Fullt looted corpse kan försvinna tidigt.
- [x] Corpse state sparas inte.

### LOOT-02 — Drop tables ⬜

- [ ] ScriptableObject loot tables.
- [ ] Tables per enemy family.
- [ ] Tables per region.
- [ ] Tables per dungeon och boss.
- [ ] Nested tables.
- [ ] Weight, min/max count och guaranteed entries.
- [ ] Difficulty modifiers.
- [ ] First-clear rewards.
- [ ] Pity/bad-luck protection.
- [ ] Personal loot.
- [ ] Group loot rules.

### LOOT-03 — Reward presentation ⬜

- [ ] Corpse glow per highest rarity.
- [ ] Loot beam för Epic/Mythic.
- [ ] Distinct drop sound.
- [ ] Boss reward chest.
- [ ] Reward summary screen.
- [ ] Rare-drop announcement.
- [ ] Auto-loot option within short range.
- [ ] Loot filter.

### LOOT-04 — Group loot 🔀

- [ ] Personal instanced loot. **Rekommenderad för modernt MMO.**
- [ ] Need/Greed/Pass.
- [ ] Master loot för organiserade grupper.
- [ ] Free-for-all.
- [ ] Trade window för nyligen droppade party-items.

---

## 11. Crafting, upgrading och economy

### CRAFT-01 — Item sinks ⬜

- [ ] Dismantle till material.
- [ ] Sell to vendor.
- [ ] Repair/durability.
- [ ] Donation till faction/research.
- [ ] Collection/transmog extraction.

### CRAFT-02 — Upgrades 🔀

- [ ] Höj item level.
- [ ] Reroll ett affix.
- [ ] Reroll alla affixes.
- [ ] Temper/add secondary affix.
- [ ] Add/remove Sigil socket.
- [ ] Evolve unique item genom challenge.
- [ ] Ingen direkt upgrade; bättre gear kommer endast från content.

### CRAFT-03 — Professions ⬜

- [ ] Blacksmithing.
- [ ] Runecrafting/Sigilcraft.
- [ ] Alchemy.
- [ ] Engineering/Artificing.
- [ ] Cooking.
- [ ] Gathering: mining, herbs, skinning, archaeology.
- [ ] Specialization paths per profession.
- [ ] Work orders.

### ECON-01 — Currencies ⬜

- [ ] Standard gold.
- [ ] Dungeon currency.
- [ ] PvP currency.
- [ ] Crafting materials.
- [ ] Seasonal currency.
- [ ] Premium currency — endast om affärsmodell kräver det.
- [ ] Currency wallet UI.

### ECON-02 — Trading ⬜

- [ ] NPC vendors.
- [ ] Buyback.
- [ ] Player-to-player trade.
- [ ] Auction house/marketplace.
- [ ] Bind on pickup/equip/account.
- [ ] Anti-fraud och server validation.

---

## 12. World, regions och exploration

### WORLD-01 — Open world 🟨

- [x] Första 420 × 420 meter stora startregionen med terrängvariation, vägnät och tydliga combat-/bebyggelseområden.
- [x] CC0-källpaket: Quaternius Medieval Village MegaKit Standard med 176 importerade FBX-moduler och lokalt bevarad licensfil.
- [x] Tre bebyggelsekluster, tre ruinområden, Rift Crypt-anslutning, 180 träd och 48 stenformationer.
- [ ] World streaming/scene partitioning.
- [ ] Region boundaries och level bands.
- [x] Första vägnät och navigation landmarks.
- [ ] Towns/hubs.
- [ ] Wilderness camps.
- [x] Första dungeonentrén från regionen genom Rift Crypt.
- [x] Första utforskningsruinerna; secrets/rewards återstår.
- [ ] Vertical exploration.
- [ ] Day/night cycle.
- [ ] Weather.
- [ ] Ambient wildlife.

### WORLD-02 — Möjliga biomes 🔀

- [ ] Rift-scarred highlands.
- [ ] Ember wastes/volcanic region.
- [ ] Void forest med levande geometry.
- [ ] Ancient machine ruins.
- [ ] Frozen shattered coast.
- [ ] Sunken marsh/corrupted wetlands.
- [ ] Celestial city/hub.
- [ ] Underground crystal network.
- [ ] Floating islands/endgame zone.

### WORLD-03 — Open-world activities ⬜

- [ ] Dynamic events.
- [ ] World bosses.
- [ ] Elite patrols.
- [ ] Public events med scaling.
- [ ] Strongholds/outposts.
- [ ] Treasure maps.
- [ ] Puzzles.
- [ ] Platforming challenges.
- [ ] Time trials/races.
- [ ] Gathering nodes.
- [ ] Rare spawns.
- [ ] Bounties.
- [ ] Invasions/rift storms.

### WORLD-04 — Map och navigation ⬜

- [ ] World map.
- [ ] Minimap eller compass.
- [ ] Fog of war.
- [ ] Pins och custom markers.
- [ ] Quest tracking.
- [ ] Dungeon difficulty/loot preview.
- [ ] Party member markers.
- [ ] Coordinates.

---

## 13. Dungeons, encounters och bosses

### DUNGEON-01 — Rift Crypt prototype ✅

- [x] Entrance/teleport.
- [x] Tre encounters.
- [x] Gates och progression.
- [x] Checkpoints.
- [x] Death reset.
- [x] Rift Warden boss.
- [x] Reward chest/artifact.
- [x] Exit portal.
- [x] Corpse-loot kompatibelt med encounter completion.

### DUNGEON-02 — Dungeon framework ⬜

- [ ] DungeonDefinition ScriptableObject.
- [ ] Encounter graph.
- [ ] Objective types.
- [ ] Difficulty settings.
- [ ] Party size rules.
- [ ] Reset/lockout rules.
- [ ] Completion score och timer.
- [ ] Reward table preview.
- [ ] Matchmaking hook.
- [ ] Reconnect handling.

### DUNGEON-03 — Dungeonformat 🔀

- [ ] Handcrafted linear.
- [ ] Handcrafted branching med optional bosses.
- [ ] Modular randomized rooms.
- [ ] Procedural layout.
- [ ] Hybrid handcrafted + random modifiers. **Rekommenderad.**

### DUNGEON-04 — Difficulties ⬜

- [ ] Story.
- [ ] Normal.
- [ ] Veteran/Heroic.
- [ ] Mythic/static hard mode.
- [ ] Scaling keys/rifts.
- [ ] Challenge modifiers/affixes.
- [ ] Death limit.
- [ ] Timer.
- [ ] Leaderboards.

### DUNGEON-05 — Första production vertical slice ⬜

- [ ] 10–15 minuters speltid med tydlig utforska → slåss → loota → bygg → boss-loop.
- [ ] Entré från första open-world-zonen.
- [ ] 2–3 varierade vanliga encounters med minst tre fiendearketyper.
- [ ] En elite med läsbar modifier.
- [ ] Ett valbart riskrum med bättre reward.
- [ ] Boss med minst två faser och positioneringsmekanik.
- [ ] Unik set-drop eller build-definierande Artifact.
- [ ] Checkpoint, completion reward och tydlig exit/återgång till världen.
- [ ] Metrics hooks för deaths, completion time och övergivna encounters.

### BOSS-01 — Boss mechanics ⬜

- [x] Frontal attack prototype.
- [x] Ground telegraph prototype.
- [ ] Phase transitions.
- [ ] Adds.
- [ ] Tank busters.
- [ ] Raid-wide damage.
- [ ] Soaks/spreads/stacks.
- [ ] Interrupt rotations.
- [ ] Environmental hazards.
- [ ] Enrage.
- [ ] Secret hard mode.
- [ ] Unique death/reward presentation.

---

## 14. Enemies och AI

### AI-01 — Melee enemy ✅

- [x] Idle/chase/windup/active/recovery/stagger/dead states.
- [x] Awareness, range och attack arc.
- [x] Telegraph.
- [x] Hit reaction och death reaction.
- [x] Corpse state.
- [x] Immediate death event för waves/XP.
- [x] Vanliga melee-träffar skadar spelaren utan automatisk knockback.
- [x] Första importerade monsterutseendet: `Risen Zombie`, återanvändbar prefab med Humanoid-rigg, idle/chase/attack/hit/death-presentation och 36 markanpassade instanser i namngivna encounter-fickor över startregionen.
- [x] Leash och återgång till spawn med full heal efter disengage.
- [ ] Byt procedural zombie-attack/hit/death mot authored clips när ett animationspaket finns; käll-FBX:en innehåller endast rig/bind pose.

### AI-02 — Enemy archetypes ⬜

- [ ] Basic melee.
- [ ] Shielded defender.
- [ ] Ranged attacker.
- [ ] Caster.
- [ ] Healer/support.
- [ ] Assassin/flanker.
- [ ] Charger.
- [ ] Summoner.
- [ ] Controller/CC.
- [ ] Elite med modifier.
- [ ] Mini-boss.

### AI-03 — AI-system ⬜

- [ ] NavMesh/pathfinding.
- [ ] Leash och evade/reset.
- [ ] Group aggro.
- [ ] Threat table.
- [ ] Target selection per role.
- [ ] Patrols.
- [ ] Cover/ranged positioning.
- [ ] Ability priority/behavior tree.
- [ ] Difficulty scaling.
- [ ] Server-authoritative AI för multiplayer.

### AI-04 — Enemy modifiers ⬜

- [ ] Enraged.
- [ ] Shielded.
- [ ] Vampiric.
- [ ] Volatile on death.
- [ ] Teleporter.
- [ ] Elemental aura.
- [ ] Summoner.
- [ ] Suppressor.
- [ ] Random elite affix combinations.

---

## 15. Quests, narrative och world state

### QUEST-01 — Quest system ⬜

- [ ] QuestDefinition ScriptableObject/data.
- [ ] Main quest.
- [ ] Side quests.
- [ ] Class quests.
- [ ] Dungeon quests.
- [ ] Daily/weekly quests.
- [ ] Bounties.
- [ ] Multi-step objectives.
- [ ] Branching choices.
- [ ] Rewards och prerequisites.
- [ ] Quest tracker/journal/map pins.

### NARRATIVE-01 — Presentation ⬜

- [ ] Dialogue boxes.
- [ ] In-world dialogue.
- [ ] Cinematics.
- [ ] Camera sequences.
- [ ] Voice-over.
- [ ] Lore codex.
- [ ] Books/audio logs.
- [ ] Environmental storytelling.
- [ ] Choice consequences.

### WORLDSTATE-01 — Persistent world ⬜

- [ ] Phasing per quest state.
- [ ] Account-wide unlocks.
- [ ] Dynamic faction control.
- [ ] Server events.
- [ ] Seasonal world changes.

---

## 16. Multiplayer och MMO

### NET-01 — Multiplayer strategy 🧊

- [ ] Small co-op först, MMO senare. **Rekommenderad riskreduktion.**
- [ ] Full MMO-backend från början.
- [ ] Peer-hosted co-op.
- [ ] Dedicated authoritative servers.

Krav för riktig MMO:

- [ ] Server-authoritative movement/combat/loot.
- [ ] Authentication och account service.
- [ ] Character service/database.
- [ ] Inventory och economy validation.
- [ ] Zone/instance servers.
- [ ] Matchmaking/group service.
- [ ] Persistence, backups och migrations.
- [ ] Observability, moderation och support tools.
- [ ] Anti-cheat och exploit response.

### SOCIAL-01 — Players ⬜

- [ ] Nearby players.
- [ ] Inspect player.
- [ ] Friends.
- [ ] Ignore/block.
- [ ] Party.
- [ ] Raid.
- [ ] Guild.
- [ ] Guild ranks/permissions.
- [ ] Ready check.
- [ ] Role check.
- [ ] Ping system.
- [ ] Emotes.

### CHAT-01 — Communication ⬜

- [ ] Local/say.
- [ ] Party/raid.
- [ ] Guild.
- [ ] Whisper.
- [ ] Zone/world.
- [ ] System/combat channels.
- [ ] Text filters och reporting.
- [ ] Voice chat.
- [ ] Speech-to-text/text-to-speech accessibility.

### MATCH-01 — Group finding ⬜

- [ ] Manual group listings.
- [ ] Automatic dungeon finder.
- [ ] Role queue.
- [ ] Cross-server matching.
- [ ] Backfill.
- [ ] Vote kick.
- [ ] Deserter rules.

### PVP-01 — PvP 🔀

- [ ] Ingen PvP.
- [ ] Duels only.
- [ ] Battlegrounds.
- [ ] Arena.
- [ ] Open-world opt-in PvP.
- [ ] Faction warfare.
- [ ] Separat PvP balance/scaling.

---

## 17. UI/UX och accessibility

### UI-01 — Nuvarande HUD 🟨

- [x] Prefab-baserade Player och Target Frames med separerad data-binding och återanvändbara bar-komponenter.
- [x] Dark-fantasy/void visual language: obsidianpanel, cyan/violett player-accent och crimson/orange hostile-accent.
- [x] Frame-positioner kan flyttas, kantbegränsas och sparas utan att påverka camera/targeting-input.
- [x] Nameplates och selection state.
- [x] Energy och action bar.
- [x] XP/level.
- [x] Floating damage.
- [x] Dungeon status.
- [x] Build snapshot.
- [x] Gameplay navigation icons.
- [x] Fem unika action-barikoner kopplade från ability-definitionerna med cooldown, charges och usability-feedback ovanpå.
- [ ] Final responsive layout och visual language.
- [ ] Gemensam HUD layout-versionering och migrering av sparade positioner.
- [ ] HUD-skala, safe-area-stöd och färdiga profiler för 16:9, ultrawide och handhållet.
- [ ] Riktiga portrait assets, frame sprites, statusikoner och nio-slice ornament.

### UI-02 — Menyer 🟨/⬜

- [x] Inventory med dark sci-fi/fantasy-window, filter, scroll-grid, selection och icon-first items.
- [x] Character/equipment med paper doll, 16 tydligt namngivna slots och separerad build analysis.
- [x] Talents placeholder.
- [x] Corpse loot delar samma slot- och tooltip-presentation som inventory.
- [ ] Talents complete.
- [ ] Map.
- [ ] Quest journal.
- [ ] Social/guild.
- [ ] Crafting.
- [ ] Collections/codex.
- [ ] Settings.
- [ ] Main menu/login/character select.

### UI-03 — Tooltip system 🟨

- [x] Item data och stat comparison.
- [x] Screen clamping.
- [x] Shared inventory/equipment/loot pattern.
- [x] Side-by-side equipped comparison med grön/röd deltafärg.
- [x] Kandidat-tooltip och vald item-inspection visar `UPGRADE`, tom slot eller uppskattad positiv build-score-delta.
- [x] Set-tooltip visar aktuell progress, samtliga 2/3/4/6-trösklar och ACTIVE/LOCKED-status.
- [ ] Ability tooltips.
- [ ] Buff/debuff tooltips.
- [ ] Advanced comparison modifier key.
- [ ] Source/drop location.
- [ ] Crafting/re-roll ranges.
- [ ] Controller focus support.

### UI-04 — Accessibility ⬜

- [ ] UI scale.
- [ ] Font size options.
- [ ] Colorblind palettes.
- [ ] High-contrast telegraphs.
- [ ] Damage-number options.
- [ ] Screen shake slider/off.
- [ ] Flash reduction.
- [ ] Hold/toggle options.
- [ ] Subtitle size/background/speaker labels.
- [ ] Audio cues för mechanics.
- [ ] Remappable controls.
- [ ] One-handed presets.
- [ ] Photosensitivity mode.

### UI-05 — Localization ⬜

- [ ] All player-facing strings externalized.
- [ ] English och Swedish first.
- [ ] Locale-aware numbers.
- [ ] Font fallback för CJK/Cyrillic.
- [ ] RTL readiness.
- [ ] Dynamic layout expansion.

---

## 18. Art direction, models, sprites och VFX

### ART-01 — Art bible ⬜

- [ ] Color script per region.
- [ ] Shape language per faction.
- [ ] Material language: metal, cloth, stone, rift energy.
- [ ] Scale guide.
- [ ] Texel density guide.
- [ ] Lighting guide.
- [ ] VFX readability guide.
- [ ] UI style guide.
- [ ] Naming/export/import conventions.

### ART-02 — Player character assets 🟨

- [x] Quaternius Regular Male som nuvarande licensierad prototypmodell.
- [x] Unity Humanoid-konfigurerad Quaternius-rigg med befintlig locomotion controller.
- [ ] Final base mesh och slutlig karaktärsidentitet.
- [ ] Production-skeleton/rig eller beslut att behålla Quaternius-riggen.
- [ ] Body customization meshes.
- [ ] Heads, hair och facial features.
- [ ] Skin/hair/eye materials.
- [ ] Armor modularity per equipment slot; nuvarande basmodell är en sammanhängande skinned mesh.
- [x] Weapon attachment points för händer, rygg och höft.
- [ ] Cloth/hair physics.
- [ ] LODs.
- [ ] Damage/hit shaders.

### ART-03 — Enemy assets ⬜

- [ ] Enemy family concept sheets.
- [ ] Base meshes och variants.
- [ ] Elite visual modifiers.
- [ ] Boss-specific model/rig.
- [ ] Corpse poses/material response.
- [ ] LODs och impostors.
- [ ] Readable weak points.

### ART-04 — Environment assets ⬜

- [ ] Modular architecture kits.
- [ ] Terrain materials.
- [ ] Foliage.
- [ ] Rocks/cliffs.
- [ ] Roads/bridges.
- [ ] Dungeon kits.
- [ ] Props och clutter.
- [ ] Interactive doors/chests/shrines.
- [ ] Decals.
- [ ] Skyboxes/skies.
- [ ] Water.
- [ ] Destruction variants.

### ART-05 — Item visuals 🟨

- [ ] Slutlig egen 2D item icon template/art bible.
- [x] Unika importerade ikoner för alla nuvarande weapon/armor/core/relic/sigil/artifact-items.
- [x] Procedural placeholder-silhuetter per weapon/armor/core/relic/sigil/artifact-kategori.
- [x] Central icon-resolver använder itemets authored Sprite, därefter katalogiserad slotspecifik art och sist procedural fallback.
- [x] Rarity borders, inte bara färgad bakgrund.
- [x] Riftstalker Circuit set-emblem.
- [ ] Tag icons.
- [ ] Affix icons vid behov.
- [ ] 3D equipped models.
- [ ] Ground/corpse loot representation.
- [ ] Inventory Sprite Atlas/batching pass när ikonbiblioteket växer.

Iconalternativ:

- [x] Kuraterade rastericons för nuvarande prototypinnehåll; slutlig egen produktionsstil återstår.
- [ ] Renderade 3D-item thumbnails.
- [x] Stiliserade silhuetticons som placeholder-system.
- [ ] Hybrid: 3D render + handmålad finish. **Rekommenderad.**

### ART-06 — UI sprites ⬜

- [ ] Panel nine-slices.
- [x] Runtime buttons med hover/pressed/disabled states; production sprites återstår.
- [x] Runtime slot backgrounds och selection/hover states; production sprites återstår.
- [ ] Inventory bag frames.
- [x] Procedurala equipment slot silhouettes; handgjorda production assets återstår.
- [ ] Health/resource bars.
- [ ] Nameplate frames.
- [ ] Map markers.
- [ ] Cursor set: default, target, loot, interact, invalid.
- [x] Fem initiala statusikoner plus generiska buff/debuff/status-fallbacks; live statusbinding återstår.
- [x] Unika ikoner för Strike, Crushing Blow, Phase Lunge, Phase Dash och Rift Charge.
- [x] Unika ikoner för Berserker, Bulwark och Riftblade.
- [x] Keen Edge har en datadriven passive-ikon och visas i Character analysis.
- [ ] Faction och dungeon emblems.

### VFX-01 — Combat VFX 🟨

- [x] Prototyp motion trail för Phase Dash, Phase Lunge och Rift Charge.
- [ ] Weapon-specific trails med korrekt socket och attackkurva.
- [ ] Impact per damage type.
- [ ] Crit emphasis.
- [ ] Phase Lunge trail/arrival.
- [ ] Rift Charge start/dust/impact och blockerad-charge feedback.
- [ ] Phase Dash afterimage och charge-recharge feedback.
- [ ] Crushing Blow fracture.
- [ ] Buff/debuff auras.
- [ ] Shields/heals.
- [ ] Projectiles.
- [ ] Ground telegraphs.
- [ ] Death VFX.
- [ ] Corpse loot glow/beam.

### VFX-02 — World VFX ⬜

- [ ] Rifts/portals.
- [ ] Weather.
- [ ] Ambient motes/fog.
- [ ] Waterfalls/water.
- [ ] Fire/smoke.
- [ ] Dungeon atmosphere.
- [ ] World-event signals.

---

## 19. Animation

### ANIM-01 — Player locomotion 🟨

- [x] Idle/walk/run via Starter Assets-controller på Quaternius Humanoid.
- [ ] Sprint.
- [ ] 8-direction strafe locomotion.
- [ ] Start/stop/turn animations.
- [x] Jump/fall/land grundflöde.
- [x] Procedural Phase Dash-pose som prototyp.
- [ ] Authored dash variants framåt/sidled/bakåt.
- [ ] Swim/climb/glide om valda.
- [ ] Weapon stance layers.
- [x] Procedural additive hit/death reaction som prototyp.
- [ ] Foot IK och slope adaptation.

### ANIM-02 — Combat 🟨

- [x] Procedurala prototypposer per nuvarande ability.
- [ ] Authored ability animation per weapon/class.
- [ ] Animation events för hit timing.
- [ ] Animator upper-body layer med Avatar Mask; nuvarande presentation appliceras additivt i `LateUpdate`.
- [ ] Cancel windows.
- [ ] Root motion policy.
- [ ] Cast/channel loops.
- [ ] Stun/knockdown/death.
- [ ] Execution/finisher optional.
- [ ] Turn-in-place och facing correction före melee-impact.
- [ ] Foot locking/warping under Charge och stora attacks.
- [ ] Animation-set resolution per weapon family, class och stance.
- [ ] Animation fallback policy när ett item saknar ett kompatibelt set.

### ANIM-03 — Enemies/bosses 🟨

- [x] Zombie-monster med Unity Humanoid-rigg och material från det lokala assetpaketet.
- [x] Locomotion med kompatibla befintliga idle/run-klipp.
- [x] Telegraph/windup/attack/recovery med procedural visual presentation kopplad till den befintliga AI-state-maskinen.
- [x] Hit reaction, death/corpse-flöde och animation-event relay.
- [ ] Ersätt fallback-presentationen med authored zombie-animationer när sådana finns tillgängliga.
- [ ] Hit/stagger.
- [ ] Phase transitions.
- [ ] Death/corpse poses.
- [ ] Additive procedural look/aim.

---

## 20. Audio och musik

### AUDIO-01 — Audio architecture ⬜

- [ ] Audio Mixer: Master/Music/SFX/UI/Ambience/Voice.
- [ ] Volume settings och mute.
- [ ] Snapshot states: exploration/combat/boss/menu/death.
- [ ] Spatial audio/rolloff standards.
- [ ] Audio pooling.
- [ ] Concurrency limits.

### AUDIO-02 — Gameplay sound 🟨

- [ ] Footsteps per surface.
- [ ] Jump/land.
- [x] Genererad prototypaudio för dash/charge, weapon swing, impact och ability denied.
- [ ] Producerade weapon swings/impacts per material och vapentyp.
- [ ] Producerade ability-specific sounds och variationssystem.
- [ ] Crit confirmation.
- [ ] Damage/low-health feedback.
- [ ] Enemy tells.
- [ ] Corpse loot/open/take all.
- [ ] Item rarity stingers.
- [ ] UI navigation.

### AUDIO-03 — Music ⬜

- [ ] Region exploration themes.
- [ ] Dynamic combat layers.
- [ ] Dungeon themes.
- [ ] Boss phases.
- [ ] Town/hub themes.
- [ ] Victory/rare loot stingers.
- [ ] Seamless transitions.

### AUDIO-04 — Voice ⬜

- [ ] Player efforts/barks.
- [ ] Enemy barks.
- [ ] Boss dialogue.
- [ ] NPC dialogue.
- [ ] Narrator.
- [ ] Localization scope.

---

## 21. Save, data och architecture

### SAVE-01 — Lokal save 🟨

- [x] Inventory IDs.
- [x] Equipped slots.
- [x] Specialization.
- [x] PlayerPrefs JSON prototype.
- [x] Corpse state är temporär.
- [ ] Save versioning/migrations.
- [ ] Multiple characters.
- [ ] Atomic file writes och backup.
- [ ] Settings separat från character save.
- [ ] World/quest progression.
- [ ] Instance item rolls.
- [ ] Cloud save.

### DATA-01 — Data-driven content 🟨

- [x] Item definitions.
- [x] Set definitions.
- [x] Ability definitions.
- [ ] Class/spec definitions.
- [ ] Talent definitions.
- [ ] Enemy definitions.
- [ ] Loot tables.
- [ ] Dungeon definitions.
- [ ] Quest/dialogue definitions.
- [ ] Validation tools för IDs/references/balance.

### ARCH-01 — System boundaries ⬜

- [ ] Separera runtime state från immutable definitions.
- [ ] ItemDefinition + ItemInstance.
- [ ] Combat event payloads med source/target/ability/tags.
- [x] Presentation events för player ability start/impact/complete/failure.
- [ ] Gemensamt gameplay-eventformat för player, enemies och framtida server authority.
- [ ] Modifier pipeline med tydlig ordering.
- [ ] Service interfaces för save/loot/inventory.
- [ ] Dependency injection endast där det minskar coupling.
- [ ] Undvik globala FindAnyObjectByType i slutlig production path.
- [ ] Pooling för combat text/VFX/enemies/UI slots.

### ARCH-02 — Multiplayer migration guardrails ⬜

- [ ] Alla inventory-mutations kan flyttas server-side.
- [ ] Loot generation kan seedas/valideras på server.
- [ ] Ability requests skiljs från authoritative results.
- [ ] Stable entity och item IDs.
- [ ] Deterministiska cooldown/resource-regler där möjligt.
- [ ] Ingen klientauktoritet över economy/rewards.

---

## 22. Performance, rendering och platforms

### PERF-01 — Runtime budgets ⬜

- [ ] Mål-FPS och minspec definieras.
- [ ] CPU/GPU/frame-time budgets.
- [ ] Memory budget.
- [ ] Draw-call/triangle budgets per region.
- [ ] VFX particle budgets.
- [ ] Audio voice budgets.
- [ ] Network bandwidth budgets.

### PERF-02 — Optimization ⬜

- [ ] Object pooling.
- [ ] LOD Groups.
- [ ] Occlusion culling.
- [ ] GPU instancing/SRP Batcher.
- [ ] Addressables/content streaming.
- [ ] Async scene loading.
- [ ] Terrain/foliage optimization.
- [ ] UI rebuild/profile audit.
- [ ] Physics layer matrix.
- [ ] Profiler regression captures.

### PLATFORM-01 — Target platforms 🔀

- [ ] Windows PC first. **Rekommenderad.**
- [ ] Linux.
- [ ] macOS.
- [ ] Steam Deck.
- [ ] PlayStation.
- [ ] Xbox.
- [ ] WebGL/WebGPU.
- [ ] Mobile — kräver separat control/performance scope.

### GRAPHICS-01 — Settings ⬜

- [ ] Presets Low/Medium/High/Ultra.
- [ ] Resolution/fullscreen/window mode.
- [ ] Render scale/upscaling.
- [ ] VSync/frame cap.
- [ ] Shadows.
- [ ] Anti-aliasing.
- [ ] Post-processing toggles.
- [ ] FOV.
- [ ] Dynamic resolution.

---

## 23. Testing, tools och development workflow

### TEST-01 — Focused automated tests ⬜

- [ ] Item equip/unequip och Sigil resolution.
- [ ] Save round-trip/migration.
- [ ] Set thresholds 2/3/4/6.
- [ ] Modifier calculations.
- [ ] Corpse item transfer och duplicate prevention.
- [ ] Loot table determinism/weights.
- [ ] XP/level thresholds.
- [ ] Encounter completion ignores corpses.

### TEST-02 — Manual smoke checklist 🟨

- [x] Movement/camera basics.
- [x] Targeting och abilities.
- [x] Crit/floating damage.
- [x] Dungeon flow.
- [x] Inventory/equipment/build effects.
- [x] Corpse looting core logic.
- [x] Automated Play Mode-smoke: five abilities, dash charges, Charge movement/damage, Strike/Lunge damage och weapon draw/sheath.
- [ ] Användarens manuella Fas 6.75-kontroll: movement, camera, jump, samtliga abilities, feel och presentation.
- [ ] Resolution matrix: 1280×720, 1920×1080, 2560×1440, ultrawide.
- [ ] Gamepad flow.
- [ ] Full save/reload session.
- [ ] Long soak test.

### TOOL-01 — Editor/content tools ⬜

- [x] Milestone scene builders.
- [ ] Item editor/validator.
- [ ] Loot table simulator.
- [ ] Build calculator.
- [ ] Encounter authoring window.
- [ ] Spawn/path visualization.
- [ ] Missing icon/description/tag audit.
- [ ] Automated screenshot comparison.
- [ ] Balance export/import spreadsheet.

### CI-01 — Build pipeline ⬜

- [ ] Automated compile.
- [ ] EditMode tests.
- [ ] PlayMode smoke tests.
- [ ] Standalone build.
- [ ] Artifact upload.
- [ ] Version stamping.
- [ ] Release channels: dev/staging/live.
- [ ] Crash reporting.

### GIT-01 — Godkännandeflöde ✅

- [x] Implementera och verifiera lokalt.
- [x] Användaren gör manuella tester.
- [x] Commit/push sker först när nästa uppdrag godkänner föregående arbete.
- [x] Milestone commits hålls tydliga och orelaterade filer lämnas utanför.

---

## 24. Live service, moderation och operations

### LIVE-01 — Live content 🧊

- [ ] Server-configurable events.
- [ ] Daily/weekly rotations.
- [ ] Seasons.
- [ ] Battle pass — endast om senare valt.
- [ ] Limited-time dungeons/world bosses.
- [ ] Hotfixable balance data.
- [ ] Maintenance messaging.

### OPS-01 — Observability 🧊

- [ ] Logs, metrics och traces.
- [ ] Combat/economy telemetry.
- [ ] Error/crash dashboards.
- [ ] Server health och alerts.
- [ ] GM/admin tools.
- [ ] Character restore/audit logs.
- [ ] Economy exploit detection.

### SAFETY-01 — Moderation 🧊

- [ ] Report player/chat/name.
- [ ] Block/mute.
- [ ] Chat filtering.
- [ ] Moderation queue.
- [ ] Sanctions/appeals.
- [ ] Privacy/data export/delete.
- [ ] Parental controls om relevant.

---

## 25. Affärsmodell och distribution

### BIZ-01 — Affärsmodell 🔀

- [ ] Premium buy-to-play.
- [ ] Premium + expansions. **Tydlig kandidat för content-driven RPG/MMO.**
- [ ] Subscription.
- [ ] Free-to-play cosmetics.
- [ ] Hybrid subscription + cosmetics.
- [ ] Ingen monetisering under prototypstadiet.

Guardrails att välja:

- [ ] Ingen pay-to-win.
- [ ] Ingen betald power.
- [ ] Cosmetic shop endast.
- [ ] Battle pass endast cosmetics/convenience.
- [ ] Earnable premium cosmetics.

### DIST-01 — Distribution ⬜

- [ ] Steam.
- [ ] Epic Games Store.
- [ ] Egen launcher/account.
- [ ] Console stores.
- [ ] Patch/CDN pipeline.
- [ ] Regional servers.

---

## 26. Rekommenderad fasordning

### Klart

- [x] Fas 1 — Movement och third-person camera.
- [x] Fas 2 — Targeting, frames och nameplates.
- [x] Fas 3 — Combat, crit och floating damage.
- [x] Fas 4 — Resource, abilities och soft auto-target.
- [x] Fas 5 — Rift Crypt dungeon vertical slice.
- [x] Fas 6 — Inventory, equipment och build management.
- [x] Fas 6.5 — Inventory UX, Character screen och corpse looting.

### Senast godkända milstolpar

- [x] **Fas 6.75 — Character Animation, Combat Presentation, visual equipment foundation och Charge.**
- [x] **Fas 6.8 — Dark-fantasy Player/Target Frames, cast bar och flyttbara unit frames.**

### Pågående mindre uppdatering

- [x] Snabbare zoom, större startzon, första riktiga monsterutseendet och player no-knockback.
  - Zoom, 420 × 420-startregion och no-knockback är implementerade och Play Mode-verifierade.
  - 36 `Risen Zombie`-instanser använder samma prefab och AI-bas och är markanpassade i grupper vid ruiner, vägar, skogar, fält och regionens ytterkanter; targeting, chase, attack, damage, stagger, death, XP, corpse loot, leash/return och arena reset är verifierade.
  - Authored zombie-animationer saknas i källpaketet och är dokumenterade som framtida ersättning för fallback-presentationen.

### Rekommenderat härnäst

- [ ] **Fas 7 — Enemy variety och första production-dungeonen.**
  - Ranged, caster och bruiser först; därefter assassin/support.
  - Interruptible telegraphs, leash, group aggro och elite modifiers.
  - Rift Crypt som 10–15 minuters vertical slice med riskrum, bossfaser och unik reward.
- [ ] **Fas 8 — Open-world foundation.**
  - Zoner/streaming, vägar, landmarks, spawn-områden och dungeonentréer.
  - Safe hub, vendors, stash, fast travel samt world map/minimap.
  - World events och quest/objective foundation; dag/natt och väder efter att content finns.
- [ ] **Fas 9 — Talent tree, status effects och full Vanguard identity.**
  - Berserker, Bulwark och Riftblade som kompletta spelstilar.
  - Aktiva val, passives, ability modifiers, respec och fler ability-slots.
  - Bleed, Burn, Vulnerable och Void-mark med item/tag/set-synergier.
- [ ] **Fas 10 — Item instances, affixes, loot tables och dismantling.**
- [ ] **Fas 11 — Quests, NPCs, narrative och hub-content.**
- [ ] **Fas 12 — Co-op networking vertical slice.**
- [ ] **Fas 13 — Content pipeline, polish och performance.**
- [ ] **Fas 14 — MMO backend/social systems**, endast efter att co-op och core loop är stabila.

---

## 27. Stora beslut som fortfarande är öppna

Prioritera dessa innan motsvarande system byggs:

1. Art direction: arcane sci-fantasy, dark fantasy, bright stylized eller cosmic void.
2. Slutlig combatmodell: GCD, animation locks, auto-attacks och skillshots.
3. Första klassutbud och partyroller.
4. Talentmodell.
5. Item-instance/affixmodell.
6. Open-world struktur och world scaling.
7. Dungeon difficulty/endgamemodell.
8. PvP-scope.
9. Co-op först eller MMO-backend direkt.
10. Plattformar och inputkrav.
11. Affärsmodell.
12. Visuell production scope: 3D realism kontra stylized production.

---

## 28. Definition of Done för framtida funktioner

En funktion markeras normalt ✅ först när:

- Kraven och valda alternativ är dokumenterade.
- Den använder befintlig arkitektur eller motiverar en migration.
- Data/content kan utökas utan onödig hardcoding.
- Normalt användarflöde fungerar.
- Fel- och edge cases har rimligt beteende.
- Save/load påverkas korrekt.
- Input och UI fungerar utan dubbla listeners/instances.
- Relevanta regressioner är kontrollerade.
- Unity kompilerar utan nya errors/warnings.
- Play Mode-flödet har verifierats.
- Användaren har gjort manuell kontroll.
- Dokumentation och denna katalog har uppdaterats.
- Commit/push görs enligt godkännandeflödet.

---

## 29. Nästa beslut

Rekommenderat val:

- [ ] Lägg till authored animation clips för zombiens attack, hit och death, eller välj ett kompatibelt animationspaket.
- [ ] Kör därefter **Fas 7 — Enemy variety och första production-dungeonen** och återanvänd zombie-AI-basen för fler monster.
- [ ] Välj först art direction under `VISION-03`.
- [ ] Välj först combatregler under `COMBAT-03`.
- [ ] Välj en annan funktion genom att ange dess ID.

Exempel på framtida instruktion:

> Fas 6.75 är godkänd. Kör Fas 7 med ranged, caster och bruiser, interruptible telegraphs, leash/group aggro samt ett valbart riskrum i Rift Crypt.
