# Keystone (Working Title) — Unified RTS × Voxel Sandbox Concept

*Alternative working titles (avoiding any "___ of ___" or "Age / Empire / Craft" patterns): **Keystone**, **Stonewright**, **Deepbastion**, **Hearthhold**, **Sovereign Ground**.*

---

## 1. Executive Synthesis: How the Two Concepts Merge into a Stronger Third Game

Rather than mashing [HEARTH_AND_BASTION.md](../../test_voxel_game/HEARTH_AND_BASTION.md) and [Deepground — RTS × Voxel Sandbox Concept.md](Deepground%20%E2%80%94%20RTS%20%C3%97%20Voxel%20Sandbox%20Concept.md) together, this design selects the strongest orthogonal mechanics from each, filters them through the real capabilities of `FineStructureVoxel`, and introduces two defining systems from [rts-notes.txt](rts-notes.txt): **physical block-stack warehousing with autonomous organization AI** and **two-tier fair-play AI under true 3D Fog of War**.

| Orthogonal Axis | Chosen Direction & Source | Why This Wins |
|-----------------|---------------------------|---------------|
| **Era Progression** | **Four Eras, Four Verbs** (*Deepground*) + **2-Choice Landmark Specialization** (*Hearth & Bastion*) | Each epoch unlocks a new *verb* on the voxel world (*Settle → Fortify → Engineer → Reshape*), while choosing 1 of 2 Landmark wings per transition gives every match strategic branching. |
| **Late-Game Tech Cap** | **Explosives & Kinetic Power, NOT Musket Lines** (*Deepground* + *Hearth & Bastion*) | Stopping before massed firearm infantry preserves the melee/cavalry/archery counter-triangle and high-ground castle warfare. Epoch IV's black-powder payoff is **terrain-shaping blasting charges, sappers, and siege bombards**, paired with water/wind kinetic automation. |
| **Conquest & Defeat** | **Capturing the Physical Keystone** (*Deepground* + [rts-notes.txt](rts-notes.txt#L13)) | Every civilization is anchored by a physical **Keystone** in its Core Hall. Dislodging and capturing (or shattering) an enemy's Keystone is a clear, physical, dramatic win condition that avoids hunting down every stray worker or hut. |
| **Resource Storage** | **Physical Resource Blocks + Vertical Warehouse AI** ([rts-notes.txt](rts-notes.txt#L5)) | Resources are stored as actual world blocks with visual fill levels—never abstract chest inventories. Workers organize yards by material, keep walkways clear, and **build floors and stairs upwards** to create multi-story storehouses as reserves grow. |
| **Building Semantics** | **Ghost Blueprints + Room/Fixture Rules + In-Place Epoch Refits** (Both) | Standard `.vxsc` blueprints make macro play fast; custom freeform rooms activate via required interior blocks (anvil, furnace, chimney) with fair block-based costs; in-place Epoch Refits let masons upgrade timber buildings to stone without demolition. |
| **Commander & Avatar** | **Dual Camera + Embodied Steward with Combat Cap & Standing Orders** (Both) | You can zoom into first/third-person at any time to chop the first trees, dig a secret tunnel, or carry a captured Keystone—but the avatar's combat power is strictly capped so it never becomes a solo hero game, and Standing Orders keep the settlement running while you're embodied. |
| **Combat & Siege** | **Counter Triangle + 3D Voxel Cover + Sapping + Fire + Morale** (Both) | Combines 3D elevation range bonuses, physical crenellation arrow interception, structural collapse cascades, sapping/counter-mining with Listening Posts, timber fire spread, and **Morale/Routing** (replacing religious "conversion" tropes). |
| **AI Architecture** | **Two-Tier AI (Embodied Worker Needs/Logistics + Non-Cheating Rival Commanders)** ([rts-notes.txt](rts-notes.txt#L3)) | Tier 1 manages individual worker jobs, needs, and warehouse construction. Tier 2 emulates human players under the exact same 3D Fog of War and physical economy—no map-hacking or free resources (except an optional "Unfair/Titan" tier). |
| **Match End & Sandbox** | **Configurable Win/Loss + "Play On & Pacify Rivals"** ([rts-notes.txt](rts-notes.txt#L11)) | Play fixed-objective matches or open-ended worlds. When any match is won *or lost*, you can convert it to an open-ended sandbox and optionally switch enemy factions to **Passive** so you can rebuild and enjoy your world in peace. |
| **Engine & Networking** | **Server-Authoritative (`FineStructureVoxel` + `finenet`), NOT Lockstep** (*Hearth & Bastion* over *Deepground*) | *Deepground* proposed fixed-point P2P lockstep, which would require rewriting `FineStructureVoxel` and leaks full map state to client map-hacks. Server-authoritative architecture (using `finenet` local queue bridges in single-player) gives cheat-proof 3D Fog of War and zero engine rewrite. |

---

## 2. Clean-Room IP, Visual, & Asset Guardrails (Both Ancestors)

Because this game bridges two iconic genres, IP and trade-dress hygiene must guard against looking like a clone of **either** classic historical RTS games **or** dominant voxel block games:

1. **Naming & Title Hygiene:**
   - Never use "Age", "Ages", "Empire", "Empires", "Craft", or "Mine" in the title or core terminology, and avoid any `"___ of ___"` title structure.
   - Use original systemic terms: **Epochs** (Hearth, Charter, Bastion, Forge), **Hands / Steadsfolk** (workers), **Steward** (player avatar), **Core Hall** (settlement center), **Keystone** (civilization anchor), **Lodestones** (deep capture points), and **Grand Monument**.
2. **Voxel Visual & Texture Identity (Avoiding Sandbox Look-Alikes):**
   - **No noisy 16×16 pixel-art block textures:** Use clean, stylized 32×32 (or procedural material-shaded) textures with distinct architectural trim, mortar lines, and surface normal/roughness contrast so terrain and masonry read cleanly from an overhead RTS camera.
   - **Architectural Block Shapes:** Leverage `FineStructureVoxel`'s custom block models and collision shapes (`shapes/*.collision`) for sloped buttresses, arched lintels, roof tiles, crenellations, and timber framing—so buildings look like crafted architecture rather than blocky cubes.
   - **No 3×3 Recipe-Grid Crafting UI:** Items and tools are forged at physical workshop stations (Anvils, Smelters, Sawpits) by workers or via contextual station queues.
3. **RTS UI, Audio, & Faction Identity:**
   - **Original HUD Layout:** Built in `finegui` with a clean top telemetry ribbon and contextual radial/docked build palettes that support seamless Overwatch ↔ First-Person transitions, rather than copying classic 1990s bottom-frame RTS HUDs.
   - **Original Audio & Morale Mechanics:** All horns, worker acknowledgments, and alerts are original. Units break via **Morale & Routing** when flanked or cut off—never via chanting priests converting units across teams.
   - **Fictional Terrain-Bound Cultures:** Playable factions are original cultures shaped by geography and engineering (e.g., mountain delvers, marsh hydraulic engineers, steppe wagon-clans, highland sun-masons)—never real-world historical nations or stock fantasy races.
4. **Multi-Language UI (`i18n`) From Day One:**
   - Every UI label, tooltip, alert, tech description, and command card in `finegui` and `finescript` resolves through a locale string table (`loc("ui.epoch.advance", ...)`), supporting UTF-8, pluralization/parameterized templates, dynamic button resizing for longer translations, and CJK/extended font fallback.

---

## 3. Physical Block Stockpiles & Autonomous Warehouse AI

A signature innovation of this game is that **every resource is physically stored as voxel blocks in your settlement**—turning storage, layout, and vertical construction into a core gameplay and AI spectacle.

### 3.1 Resources as Multi-Stage Voxel Blocks
- **No Infinite Chests:** Harvested resources are not hidden inside 1-block containers with abstract 9,999-item counters. Instead, workers place actual **Resource Blocks** inside designated Stockpile / Storehouse zones:
  - **Provisions:** *Grain Sack Piles*, *Smoked Meat Racks*, *Root Cask Stacks*
  - **Timber:** *Cordwood Stacks*, *Sawn Plank Bundles*
  - **Stone:** *Dressed Ashlar Pallets*, *Rubble Cages*
  - **Ore & Metal:** *Ore Heaps*, *Copper/Iron/Steel Ingot Crates*
  - **Bullion (Trade & Tributes):** *Treasury Coffers* (stored inside the Core Hall or Vault)
- **Visual Fill Levels:** Each Resource Block holds up to a fixed maximum (`1..MaxUnits`, stored in its block state / `DataContainer`) and changes its 3D block mesh across fill stages (e.g., quarter-stack, half-stack, three-quarter stack, full block). Looking at your storehouse tells you your economic state without even reading the HUD.
- **Physical Vulnerability:** Your top-bar HUD sums the resources across all intact, connected stockpiles—but if raiders breach your outer wall and torch your Timber Yard or haul off your Ingot Crates, those resources are physically lost or stolen.

### 3.2 The Storage Problem & Quartermaster Organization AI
Because a booming Epoch II–IV economy processes thousands of units of Timber, Stone, Ore, and Provisions, dumping blocks randomly on the ground would quickly choke walkways and bury needed materials behind walls of stone. Therefore, dedicated **Quartermaster / Logistics AI** governs how workers manage stockpiles:

1. **Spatial Sorting & Defragmentation:**
   - During low-urgency ticks, workers consolidate partially filled blocks of the same resource into full blocks to reclaim floor space.
   - Stockpile zones auto-partition (or follow player tags) so Timber stacks near Sawmills/Carpenters, Ore stacks near Smelters, Stone stacks near active curtain-wall blueprints, and Provisions go into covered Granaries/Cellars.
2. **Walkable Haul Aisles:**
   - Workers never place resource blocks in a solid, inaccessible cube faster than they can reach them. The stockpile planner enforces **2-block storage rows separated by 1-block walking aisles** so every resource block has at least one accessible adjacent face.
3. **Building Upwards (Autonomous Multi-Story Warehouses):**
   - When a stockpile's ground footprint reaches capacity, workers don't just sprawl endlessly outward across valuable farmland.
   - Instead, once a column of resource blocks is stacked to reach height (2 blocks high), or within a designated Storehouse footprint, workers **automatically construct timber or stone support pillars, lay a plank/slab mezzanine floor above the lower stacks, and build stairs or ramps** to the new level!
   - Over the course of a match, a busy stockpile organically grows into a bustling **multi-story voxel warehouse or terraced depot**, with workers climbing stairs to stack and retrieve materials on the 2nd, 3rd, and 4th floors.

---

## 4. Two-Tier AI & True 3D Fog of War

AI is a foundational pillar on two distinct levels: individual embodied agents inside a settlement, and rival civilizations competing or cooperating across the map.

### 4.1 3D Voxel Fog of War (Shared by Humans and AI Alike)
Because `FineStructureVoxel` is server-authoritative, Fog of War is enforced cleanly in 3D space:
- **Unexplored (Shrouded):** Never-visited chunks are hidden until physically scouted.
- **Explored / Remembered Terrain Snapshot:** Once scouted, you see the terrain and buildings *as they existed when your units last had line-of-sight*. If an enemy secretly quarries a moat, builds an Epoch Keystone, or digs an underground tunnel outside your vision, your map still shows the old terrain until a scout looks again.
- **Active 3D Line-of-Sight:** Computed using real voxel elevation and occlusion (`raycast_blocks`). A lookout on a high cliff or a 12-block Watchtower sees far across valleys and over low walls, while dense forest canopies, hills, and cave roofs block sightlines.
- **Subterranean Fog & Acoustic Detection:** Underground tunnels are completely hidden from surface observers unless they spot the tunnel mouth or build **Listening Posts** (which detect the vibrations of enemy sappers digging within a 3D radius).

### 4.2 Tier 1: Individual Worker & Unit AI (Jobs, Needs, & 3D Navigation)
Every worker (**Hand / Steadsfolk**) is an autonomous entity driven by `AIBrain` goals and a settlement-wide `WorkClaimRegistry`:
- **Jobs & Task Reservation:** Workers claim specific block targets (chopping a tree, quarrying a rock seam, hauling to a stockpile block, placing the next block of a blueprint, or organizing a warehouse floor) so 15 workers never collide on the same block.
- **Embodied Needs:** Workers have lightweight, readable physical needs:
  - *Nourishment:* Periodically grab a ration from a Provisions block or Granary (well-fed workers haul and build faster; starving workers lose morale and work slowly).
  - *Shelter & Safety:* Require housing volume (dwellings) to maintain settlement morale, and flee to the Core Hall or garrison inside nearby towers when raiders attack—ringing an alert bell or picking up militia spears if cornered.
- **Constructive 3D Pathfinding:** Because voxel terrain is mutable, workers don't just fail when a target block is 3 blocks too high or across a ditch—they can place temporary **scaffolding blocks, ladders, ramps, or warehouse floors** to reach high construction courses or deep quarry benches, then dismantle temporary scaffolding when a structure finishes.

### 4.3 Tier 2: Civilization / Rival Commander AI (Fair-Play Human Emulation)
Rival AI civilizations (both solo-play opponents/allies and extra slots in multiplayer) are built to **emulate human players faithfully without cheating**:
- **Zero Information or Resource Cheating:** On standard difficulty levels, the AI has **no map-hack vision** and **no free resource injection**. It sees only the chunks its own scouts and towers have uncovered in the 3D Fog of War, must physically survey to find underground ore veins, and must manage its own physical block stockpiles and worker needs.
- **Human-Like Strategic Loop:**
  1. *Reconnaissance:* Dispatches scouts along terrain ridges and river valleys to locate resources, chokepoints, and rival borders.
  2. *Adaptive Base Layout:* Places blueprints, road networks (for the +25–50% haul speed bonus), irrigation ditches, and terrain-hugging curtain walls bridging natural cliffs.
  3. *Counter-Composition & Siege:* Reads what its scouts actually saw in your base (e.g., heavy cavalry → trains polearms; high stone walls → builds trebuchets or dispatches sapper crews to tunnel underneath).
- **Difficulty Tiers via Decision Depth & Attention, Not Cheats:**
  - *Apprentice (Paced):* Slower build-order cadence, telegraphed scouting, rarely uses underground sapping or multi-front flanks.
  - *Standard (Fair Rival):* Balanced economy, active scouting, uses high ground, roads, and combined-arms sieges.
  - *Veteran (Master Tactician):* Tight build orders, counter-scouting, river diversions, counter-mining, and coordinated multi-prong assaults—still 100% within fair rules and Fog of War.
  - *Titan (Explicitly Labeled Overdrive):* Optional ultra-hard tier for players who want an outnumbered survival challenge where the AI receives an explicit, transparent harvest/production multiplier.

---

## 5. The Four Epochs: Four Verbs & The Physical Keystone

Progression is capped at **strictly four Epochs**. Every transition unlocks a new way to manipulate the voxel world, and early-era walls and structures remain relevant throughout the match (reinforced via in-place Epoch Refits or tunneled under rather than rendered obsolete).

### 5.1 The Keystone (Advancement & Conquest Anchor)
- At the center of your **Core Hall** stands your civilization's **Keystone**—a multi-block carved monument that embodies your realm's sovereignty.
- **Advancing Epochs:** To advance, you commission an **Epoch Wing / Landmark** (choosing 1 of 2 doctrinal specializations for that era) and physically build the next tier of your Keystone assembly block-by-block. Rival scouts can see a rising Keystone and raid to delay it (damaging an unfinished Keystone construction pauses advancement, though a completed Epoch is never lost).
- **Conquest via Keystone Capture:** A civilization is conquered when an attacker breaches its Core Hall, breaks the Keystone's housing, and **captures the Keystone** (either shattering it on site in Destruction mode, or loading the heavy Keystone block onto a carrier squad / Steward avatar and hauling it out of the citadel to claim the realm!).

### 5.2 The Four Epochs at a Glance

| Epoch | Core Verb | Voxel & Economic Unlocks | Military & Siege Unlocks | Landmark Choice (Pick 1 of 2 to Advance) |
|-------|-----------|--------------------------|--------------------------|------------------------------------------|
| **I. The Hearth Age** *(Fully Playable Standalone!)* | **Settle** | Surface foraging/hunting, basic tilled plots, woodcutting, surface stone gathering, physical block stockpiles + multi-floor timber storehouses, gravel paths, log palisades. | Militia Clubmen/Spearmen, Slingers/Shortbowmen, Pathfinder Scouts, basic Watchtowers. | **Great Granary** *(irrigated crop yield + worker growth)* **OR** **Bronze Smeltery** *(early metal tipping + free spear squad)*. |
| **II. The Charter Age** | **Fortify** | Open-pit stone quarrying, shallow copper/bronze mining, irrigated canal farming (+50% growth near water), paved cobblestone roads, multi-story masonry warehouses, local barter market. | Shielded Men-at-Arms, Elevated Bowmen, Light Outriders (cavalry), Crew Battering Rams, Dressed Stone Walls & Gatehouses, **In-Place Epoch Refits**. | **Masons' Guildhall** *(cheaper stone + wider structural support spans)* **OR** **War College** *(veteran promotions + siege workshop blueprints)*. |
| **III. The Bastion Age** | **Engineer** | Ore surveying & deep shaft mining (iron/coal, timber shoring), **kinetic waterwheels & sluice gates** (powering mills & trip-hammers), aqueducts, Bullion minting & Treasury Vaults. | Halberdiers, Crossbow Arbalests, Heavy Lancers, **Sapper Crews & Counter-Mining**, **Listening Posts**, Counterweight Trebuchets, Siege Towers. | **Grand Conservatory** *(+50% kinetic water/wind workshop output)* **OR** **Ordnance Foundry** *(alchemical blasting charges & heavy bombards)*. |
| **IV. The Forge Age** | **Reshape** | Steel blast furnaces, wind/water-automated ore crushers & sawmills, mass earthwork excavation, sloped star-bastion masonry, **Grand Monument** construction. | Elite Shock Infantry, Field Engineer Corps (pontoon bridges, gabions), **Alchemical Blasting Charges** (carve craters/breaches), **Siege Bombards & Mortars** *(no musket line-infantry spam)*. | **Capstone:** Construct & defend a customizable **Grand Monument** or conquer rival Keystones. |

---

## 6. Hybrid Building, Combat, & Terrain Warfare

### 6.1 Blueprints + Freeform Room Rules + In-Place Refits
1. **Standard `.vxsc` Blueprints:** Stamp pre-designed buildings with a translucent ghost preview that automatically levels foundations or builds support skirts on sloped terrain. Hands fetch blocks from stockpiles and build bottom-to-top.
2. **Freeform Building with Room Rules:** Any player-designed enclosed volume with a door becomes a functional RTS building when outfitted with its required **Station Block & Interior Fixtures** (e.g., a *Smithy* requires a minimum room volume, Furnace, Anvil, and a chimney vent open to the sky; a *Barracks* requires a Banner-Rack and weapon racks).
   - **Fair Cost & Cap:** Cost equals the actual blocks placed, and production speed is capped by tier so custom designs win through **clever geometry, cliffside integration, and defensibility** rather than stat exploits.
   - **Instant Readability:** Every valid functional room automatically hangs an owner-colored banner and role emblem outside its entrance.
3. **In-Place Epoch Refits:** Clicking *Refit* on an earlier-era structure queues masons to swap thatch for slate and timber for stone block-by-block without tearing down your town layout.

### 6.2 Voxel Combat & Siege Mechanics
- **Counter Triangle + Terrain Layer:**
  - *Polearms* beat *Cavalry*; *Cavalry* beats *Ranged & Siege*; *Ranged* beats *Infantry*; *Siege* beats *Fortifications*.
  - *Terrain Modifiers:* Actual voxel height grants ranged units **+1 horizontal range per 2 blocks of elevation** and **+15% damage**. Steep/broken ground slows cavalry charges; shallow water slows infantry; narrow tunnels and 3-block bridges force tight fronts where a small shieldwall holds off a large army.
- **Physical Cover via Raycasts:** Alternating stair/slab crenellations on battlements physically intercept 50–70% of incoming projectiles (`raycast_blocks`) while defenders shoot through the embrasures.
- **Structural Integrity, Sapping & Blasting:**
  - Every material has a horizontal support span (Wood ~6, Stone ~10, Reinforced Masonry ~16).
  - **Trebuchets & Bombards** shatter load-bearing lower courses, causing unsupported upper walls to collapse into climbable rubble slopes.
  - **Sappers** tunnel beneath walls, prop chambers with timber struts, and burn the struts to drop towers from below—countered by **Listening Posts**, deep stone foundations, counter-mines, or breaching a moat to flood the enemy tunnel!
- **Fire & Morale:**
  - Timber palisades and thatch roofs can ignite and spread fire (extinguishable by water channels or worker bucket brigades), creating natural urgency to refit key defenses to stone.
  - Units pushed past their breaking point (flanked, starving, cut off, or losing their Keystone) **rout and flee** rather than fighting to the last pixel or being magically converted.

---

## 7. Game Modes, Victory Conditions, & "Play On / Pacify"

### 7.1 Match Setup & Victory Options
When starting a game (Solo vs. AI or Multiplayer), players can configure fixed teams, open diplomacy (v2), and choose one or more Victory Conditions—or select **Open-Ended Sandbox** from the start:
1. **Keystone Conquest:** Capture or shatter every rival civilization's Keystone.
2. **Grand Monument:** Construct your Epoch IV Grand Monument (meeting volume/material requirements) and defend it until its beacon ignites.
3. **Deep Lodestones:** Excavate down to ancient subterranean Lodestone chambers, fortify them underground, and hold a majority for a countdown timer.
4. **Open-Ended Chronicle:** No match-ending timer; build, trade, and war across a vast procedural world at your own pace.

### 7.2 Post-Match Continuation ("Convert to Open-Ended & Pacify Rivals")
In traditional RTS games, the moment the victory or defeat screen appears, the city you spent an hour engineering is frozen and discarded. Here, **your world is always yours to keep**:
- Whenever a match is **Won or Lost**, the end-of-match screen offers:
  - **"View Chronicle & Exit"** (standard match wrap-up and stats)
  - **"Continue in Open-Ended World"** — immediately converts the match into an endless sandbox session.
  - **"Pacify Rivals" Toggle:** When continuing (especially after a *loss* where enemy armies are actively overrunning your town, or after a *win* when you just want to build in peace), you can check **Make Rivals Passive**. All remaining AI factions immediately cease hostility, withdraw invading squads to their own borders, and transition into peaceful/neutral neighbors—letting you rebuild your ruined keep, experiment with waterwheels, or expand your city indefinitely without getting ravaged.

---

## 8. Version Triage & Development Progression

To ensure we get to a **fun, playable game quickly** and never get bogged down in feature creep, every idea is triaged into four clear stages:

### Stage 1: The Epoch I Playable Core ("The Hearthfire Slice")
*Goal: A genuinely fun, self-contained settlement & skirmish game playable with ONLY Epoch I complete.*
- **Engine Primitives (`FineStructureVoxel` C++):**
  1. **Localization (`i18n`) Table** in `finegui` / `finescript` so all UI is multi-language from the start.
  2. **Dual Camera & Selection (`TacticalCamera`, `screenRayToWorld`, `entitiesInScreenRect`)** for seamless First-Person Steward ↔ Overwatch RTS control.
  3. **Structure Template & Ghost Blueprint Tracker (`BlueprintSite`)** using existing `.vxsc` schematics and `rotateSchematic`.
  4. **Task Reservation (`WorkClaimRegistry`), Constructive 3D Pathfinding (placing stairs/floors), & Surface Speed Multipliers** (for roads).
  5. **3D Fog of War & Terrain Snapshot Memory** per faction/player.
  6. **Ballistic `ProjectileSystem`** with voxel raycast cover and elevation scaling.
- **Game Policy (`finescript` & Data):**
  - Epoch I buildings (*Core Hall + Keystone*, *Shelter*, *Forager*, *Sawpit*, *Watchtower*, *Log Palisade*).
  - **Physical Resource Blocks** (Provisions, Timber, Stone) with visual fill stages + **Quartermaster AI** that organizes yards, keeps aisles clear, and builds multi-story storehouse floors upwards!
  - Worker jobs & basic needs (hunger/housing).
  - Epoch I units (*Militia*, *Shortbowmen*, *Scouts*) with Morale/Routing and high-ground bonuses.
  - **Tier 2 Rival AI (Epoch I capable):** Fair-play AI opponent operating under 3D Fog of War that builds an Epoch I camp, scouts, and raids your Keystone.
  - **Keystone Capture** win/loss flow + **"Continue in Open-Ended World (with Pacify Rivals)"**.
- **Why this works standalone:** Even before Epochs II–IV exist, players can establish a village, watch workers build multi-story timber warehouses, fortify hills with palisades and watchtowers, fight tactical skirmishes under 3D Fog of War, capture an enemy Keystone, and keep building in peace afterward.

### Stage 2: Version 1.0 Complete ("Four Epochs of Terrain Warfare")
*Goal: Full 4-Epoch RTS × Voxel Sandbox experience.*
- **Engine Primitives (`FineStructureVoxel` C++):**
  - **Structural Integrity & Collapse Cascades** (load-bearing spans, falling rubble on breach).
  - **Acoustic / Subterranean Activity Sensor** (for Listening Posts detecting underground digging).
  - **Fire Propagation & Extinguishing** on flammable block types (bounded cellular update alongside `FluidSimulator`).
- **Game Policy (`finescript` & Data):**
  - **Epochs II, III, and IV** (*Charter*, *Bastion*, *Forge*) with 2-choice Landmark wings and **In-Place Epoch Refits**.
  - **Freeform Room Rules & Fair Cost Evaluator** (custom buildings recognized via Station blocks + interior fixtures + chimney/enclosure rules).
  - **Fluid-Driven Economy & Siege:** Irrigated farming, defensive moats, kinetic waterwheels/sluices, sappers, counter-mining, trebuchets, and Epoch IV blasting charges/bombards.
  - **Four Asymmetric Cultures:** *Karst Delvers* (mountain/subterranean), *Fenwardens / Riverward* (marsh/hydraulic), *Wind Clans* (steppe/packable wagon-camps), and *Sunstone Marches* (highland masonry/fire-siphons).
  - **Full Tier 2 Civilization AI** across Apprentice, Standard, Veteran, and Titan difficulties.

### Stage 3: Version 2.0 Compatible Upgrades ("Living Diplomacy & Shared Worlds")
*Goal: Deep systemic expansions that plug cleanly into the v1.0 architecture without breaking save or content compatibility.*
- **Dynamic Diplomacy, Agendas, & Betrayal ([rts-notes.txt](rts-notes.txt#L11)):**
  - Enemy vs. ally is no longer fixed (unless locked in match setup).
  - Every AI civilization rolls or is assigned a **Personality & Agenda** (e.g., *Paranoid Territorial*—trades generously as long as you never build within 60 blocks of their border, but launches a surprise attack if you encroach; *Mercantile Opportunist*—can be bought off with Bullion tributes; *Hegemonic*—demands vassalage from weaker neighbors; *Steadfast Ally*—honors mutual defense pacts).
  - **Physical Tributes & Treaties:** Propose ceasefires, border demilitarized zones, trade routes, or joint war targets. Demonstrate allegiance by sending actual **Tribute Caravans** hauling physical Bullion or Resource Blocks to a rival's Core Hall, or by attacking their enemies.
  - **Surrender & Vassalage:** Faced with overwhelming siege, an AI civilization may offer surrender—handing over a tributary share of its physical output or swearing fealty rather than fighting to annihilation—or feign submission and rebel if your army marches away and leaves your Keystone unguarded!
- **Shared Co-Op Settlements:** Multiple players co-commanding one civilization with optional role specializations (Architect, Quartermaster, Marshal).
- **Riverine & Coastal Naval Logistics:** Barges, pontoon bridges, and gunboats navigating voxel rivers and canals.

### Stage 4: Features Reserved for *Shattered Lands* & Other Future Engine Titles
*Because `FineStructureVoxel` is a general-purpose multi-game engine and [GAME_CONCEPT.md](../../test_voxel_game/GAME_CONCEPT.md) (*Shattered Lands*) remains our broad "kitchen-sink" North Star in the background, ideas that don't fit a tight 4-Epoch RTS are not thrown away—they simply belong to *Shattered Lands* or future games on the same engine:*
- **Oligosynthetic Signal Logic, Conveyors, & Factory Automation:** Belongs in *Shattered Lands* (Tier-3 tech) or an automation-focused game, keeping this RTS's four-epoch pre-industrial identity razor-sharp.
- **Persistent 24/7 MMO "Realm" Servers with Offline Raid Windows** (*from Deepground*): Requires dedicated server ops, anti-griefing offline shields, and multi-week real-time timers; better suited to a dedicated survival-MMO ruleset than a 45–90 minute RTS/sandbox hybrid.
- **Deep RPG Character Progression, Dungeons & Boss Raid Loops:** Core to *Shattered Lands*, which will directly reuse the Structure Template Stitcher, Ballistic Projectiles, Structural Integrity, and `AIBrain` upgrades built during this RTS detour.

---

## 9. Multi-Game Engine Architecture: Why This Detour Accelerates *Shattered Lands*

This RTS × Voxel Sandbox concept is a **concrete, playable game detour**—not a replacement for [GAME_CONCEPT.md](../../test_voxel_game/GAME_CONCEPT.md) (*Shattered Lands*). By adhering strictly to **"Engine = Mechanisms (C++), Finescript = Policy (Scripts & Data)"**, every single C++ subsystem built to make Epoch I–IV playable is designed as a **domain-agnostic engine primitive** reusable across *Shattered Lands* and future games:

| Engine Primitive (C++ Mechanism) | How This RTS Uses It | How *Shattered Lands* & Future Games Reuse It |
|----------------------------------|----------------------|-----------------------------------------------|
| **1. UI Localization (`i18n` String & Font System in `finegui`)** | Multi-language HUD, tech tooltips, diplomacy dialogs, and alerts. | Every future game on the engine gets full multi-language UI support out of the box. |
| **2. Structure Templates, `.vxsc` Ghost Preview & Diff Tracker** | Blueprint stamping, block-by-block worker construction, and in-place Epoch Refits. | Procedural dungeons, surface villages, mob nests, boss arenas, and player schematic-building gadgets in *Shattered Lands*. |
| **3. Constructive 3D Pathfinding & `WorkClaimRegistry`** | Workers navigating mutable terrain, building warehouse floors/stairs upwards, and claiming harvest/build tasks without colliding. | Companion NPCs, village artisans, automated drones/golems, and intelligent horde sappers that bridge trenches or climb walls. |
| **4. Multi-Stage Physical Storage Blocks (`DataContainer` → Mesh Variant)** | Resource blocks (Timber stacks, Stone pallets, Grain sacks) that visually fill from 1 to Max capacity. | Physical loot piles, ore stockpiles, fuel bunkers, and immersive crafting stations in survival/RPG games. |
| **5. 3D Fog of War & Per-Viewer Terrain Snapshot Memory** | Prevents map-hacking; hides enemy quarries, Keystones, and tunnels until scouted; powers fair-play AI vision. | Exploration maps, line-of-sight stealth mechanics, subterranean radar/echolocation, and team-based multiplayer modes. |
| **6. Dual Camera Rig (`FirstPerson` ↔ `Overwatch`) & Frustum Selection** | Seamless shift between embodied Steward play and overhead RTS squad command. | Creative/architect building mode, tactical companion command, and turret/drone control in *Shattered Lands*. |
| **7. Ballistic Projectiles, Structural Integrity & Fire Cellular Sim** | Elevation-scaled arrows behind crenellation cover, trebuchet wall breaches, sapper collapses, and burning timber palisades. | Bows, crossbows, thrown explosives, spell projectiles, and 7DTD/Teardown-style base defense against siege hordes in *Shattered Lands*. |

