# Keystone — Implementation Plan

Target: **Stage 1 "Hearthfire Slice"** from [DESIGN.md](DESIGN.md) §8 — a self-contained,
fun Epoch I game. In-flight status lives in [STATUS.md](STATUS.md); this file records the
*shape* of the work and the reasons for it.

## Ground rule: Mechanism vs. Policy

- **Engine (`FineStructureVoxel` + deps) = mechanism.** Domain-agnostic C++. Must make sense
  in *Shattered Lands* or any other game. No RTS vocabulary in engine APIs: "faction", "work
  claim", "blueprint site" are fine; "Keystone", "Hand", "Epoch" are not.
- **Keystone = policy.** Content, config, and finescript. Starts with **zero C++**.
  C++ is added only for a measured bottleneck that doesn't belong in the engine.

## Engine gaps the design doc did not list

The design's §8 primitive list assumes more than exists. Verified missing at branch point:

| Gap | Evidence |
|---|---|
| Entities are never drawn | `EntityRenderer` builds state only; no entity shader/pipeline/draw call anywhere |
| No game host | `examples/render_demo.cpp` (2400 lines) hardcodes blocks, hotbar, atlas, world |
| Block textures ignored | `.model` `texture:` parsed but unused; atlas is random solid colours |
| No game-level script | Scripts run per-block/per-entity only |
| Mob natives lack targets | `mob_*` act on implicit `ud->entityCtx`; central job logic can't address units by id |
| No faction on entities | Needed by fog filtering, team tint, targeting |
| Pathfinder too small | Default 200 iterations, 32 blocks, no cost model |
| Feature placement hardcoded | `render_demo` adds placements in C++ |

## Architecture

```
Keystone/                        finevox libraries
  game.conf          ──────────▶ finevox_game   (headless-capable runtime: manifest, content,
  blocks/ entities/ models/                      worldgen, session, ScriptGameSubsystem)
  blueprints/ textures/ lang/    finevox_client (Vulkan host: atlas, entity draw, markers,
  scripts/*.fsc (game thread)                    tactical camera, UI script host)
  ui/*.fs       (render thread)  finevox_run / finevox_headless (executables)
```

**Two script worlds, one wire.** Game logic runs on the game thread (`scripts/main.fsc`); UI runs
on the render thread (`ui/main.fs`). They share nothing: UI → game is `send_command` (a Value
through `GameActions::sendAction`), game → UI is `ui_publish` (CBOR through a queue). That is the
same boundary multiplayer needs, so single-player stays honest about server authority.

**Headless first.** `finevox_headless` runs a real match with no GPU. Gameplay is verified by
scripted scenario tests, not by watching a window.

**Fog of war is a replica, not a shader trick.** The client renders a second `World` holding only
what its faction has seen, synced per-subchunk from the authoritative world when visible. Unseen
terrain literally isn't there; remembered terrain stays stale until re-scouted (DESIGN §4.1).

## Milestones

| # | Milestone | Done when |
|---|---|---|
| K1 | Boot | `finevox_run --game-root Keystone` shows textured generated terrain; a scripted worker walks and is visible |
| K2 | Command | Tactical camera, box select, right-click move; UI ↔ game bridge; localized HUD |
| K3 | Economy | Path service, work claims, blueprint sites, resource blocks; workers gather → stockpile → build |
| K4 | Warehouse | Quartermaster aisles, defrag, floors and stairs upward |
| K5 | Conflict | Faction fog, projectiles, Epoch I units, morale/rout |
| K6 | Rival | Fair-play commander AI under fog; raids |
| K7 | Match | Keystone capture, win/lose, Continue + Pacify, polish pass |

Later stages (DESIGN §8 Stage 2+) are out of scope until the slice is fun.

## Script-cost budget

finescript is a tree-walking interpreter. Per-tick-per-unit script is out. Units think at
2–5 Hz (staggered) and C++ carries motion between thinks. Commander AI thinks at ≤1 Hz. Any
hot loop over blocks or cells belongs in an engine native.

## Settings

The player's own settings, which belong to them and not to a game: kept with `pref` and `set_pref`
in `prefs.txt` in their folder, and listed as rows in `ui/settings.fs`. Today there are three, all
about what's drawn over the world. The rest, roughly in order:

1. **Shape of the screen.** Tabs (Display, Interface, Controls, Sound, Language), since one list
   won't hold everything. Each row says what it does in a tooltip, and Restore Defaults per tab.
2. **Display.** Window, full screen (F11 exists) or borderless; resolution; frame cap and vsync;
   view distance; field of view on foot. Needs engine natives: the window and swapchain are C++, and
   some of these mean recreating the swapchain.
3. **Interface.** UI scale (finegui's global scale, with the fixed pixel layouts in `ui/main.fs`
   made relative first); which panels show (work queue, sides, stock); tooltips on or off; health
   bars, halos and pins as now; colours for players who can't tell red from green, which means
   faction colours chosen from a safe palette rather than hard-coded.
4. **Controls.** Rebinding. Bindings live in `input/`, read once at start; the engine needs to
   rebind at run time and save the player's own over the game's. Edge panning, its speed, and
   mouse look sensitivity and inversion on foot.
5. **Sound.** Master, effects and music volumes, once there's any audio.
6. **Language.** The `lang/` files there are, named in their own language. Changing it rebuilds the
   UI, which the scripts can do since every string goes through `loc`.

Settings that change a game (land, rivals, fog) stay in the lobby: they're the game's, and the host's.

## Water, animals, boats, cavalry and siege

Asked for on 2026-11-19, after the slice. In the order they depend on each other. Nothing has Epochs
yet, so the pieces that belong to a later one need Epochs first (step 5). The counter triangle these
fill in is DESIGN §6.2's: spears beat horse, horse beats bows and siege, bows beat foot, siege beats
walls.

| # | What | Epoch | Engine | Keystone |
|---|---|---|---|---|
| 1 | Rivers, lakes, beaches | the world | `WaterwayPass`, `nav_max_wade` | Sites kept dry, a ford on each side's line to the middle |
| 2 | Soldiers worth their cost | I | none | `UNITS` numbers, judged by duels and set fights. Bows are poor value now (AI.md) |
| 3 | Animals and hunting | I | none expected: an animal is an entity with a script brain | Deer that run, boar that fight back, a Hunter's Lodge, meat into provisions |
| 4 | Fishing from the bank | I | none | A Fishing Hut on a shore, Hands fishing the water by it |
| 5 | Epochs | II-IV | none | The Keystone's tiers and the Landmark choice, gating the palette and `UNITS` |
| 6 | Boats | I fishing, II ferries, III war | Entities that float and steer on water: navigation on the surface, and carrying passengers | A Dock that builds them. Fishing boats go further out, ferries carry units over, war boats carry bows |
| 7 | Mounted warriors | II outriders, III lancers | Probably none: a rider and horse can be one unit with one model | A Stable. Fast, a charge, weak to spears |
| 8 | Siege weapons | II ram, III trebuchet and tower, IV bombard | Projectiles that break blocks where they land. Collapse needs structural integrity (DESIGN Stage 2) | A ram for gates and palisades, a trebuchet that breaks walls from range |
| 9 | Bridges | II or III | none: units walk over blocks already | A bridge blueprint that spans water. Laying blocks over a river works now, as a sandbox does |

### Water

Rivers are a barrier. They're crossed at fords and marshes, both a block deep, which units wade: the
difference between them is only how they look (a gravel bed, or mud with tussocks). Lakes are rimmed
with a shallow edge, and both have sand beaches. All water is at sea level, so rivers and lakes meet
where they cross. The numbers are in `game.conf`.

Nothing in the noise promises a way across, and one seed in six had none between the two sides. So
`ford_the_way` lays a gravel ford wherever the line from the middle of the map, out through a
settlement to the edge, meets deep water: every side can reach the middle, and brigands, who come in
along that line, can reach it.

### What a unit costs

Units don't cost the same, and shouldn't: a dearer unit is a better one for what it costs, with a
weakness of its own that something cheaper can use. Militia are cheap and sturdy, and have to close
in. Bows cost more, reach far, gain from height and walls, and are poor at close quarters. Later,
horse costs more again, is fast and hits hard on the charge, and spears stop it. Siege is the dearest
and slowest, and breaks what nothing else can.

So the test of a new unit's numbers is that no one kind wins on its own: an army of only that kind,
or only militia, does no better than one that mixes them, played as duels both ways round
(`tools/duel.sh`, CLAUDE.md), and a set fight of equal spending goes the way its strengths say it
should.
