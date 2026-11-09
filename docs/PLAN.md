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
