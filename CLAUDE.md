# Keystone

RTS × voxel sandbox built on FineStructureVoxel. It is both a playable game and the foil that
drives engine development. See [docs/DESIGN.md](docs/DESIGN.md), [docs/PLAN.md](docs/PLAN.md),
[docs/STATUS.md](docs/STATUS.md).

**A first release is in progress: start with [docs/RELEASE_HANDOFF.md](docs/RELEASE_HANDOFF.md).**

## Rules

- **Keystone is content + finescript + config.** No C++ unless a measured bottleneck doesn't
  belong in the engine. If a script needs a mechanism, add it to the engine.
- **Engine stays game-agnostic.** No Keystone vocabulary in engine APIs (see PLAN.md).
- **IP hygiene (DESIGN §2).** Title/terms avoid "Age", "Empire", "Craft", "Mine". The design's
  own table says "Hearth Age" — that's a slip; use **Hearth Epoch** etc. In-game text says
  "Epoch". Also avoid Minecraft/Mojang-resembling names, art, or 16×16 noisy textures.
- **All player-facing text goes through `loc`** — no literal strings in UI scripts.
- Personal open-source project: commit to `main` and push, in every repo here. No PRs.
- Nothing from any employer belongs in these public repos, commit metadata included. Each
  repo sets `user.email` locally to the GitHub noreply address, over the global git config.
  Check it in any new clone: `git config user.email`.
- World storage follows "writers lock, readers don't". See the threading section of the
  engine's `docs/ARCHITECTURE.md`, and don't add a lock to a read path.

## Layout

- On any other machine, start with [docs/PLATFORMS.md](docs/PLATFORMS.md): how to set up, what to
  expect, and the two-machine test. macOS, Linux and Windows all work.
- Engine repo and deps are siblings: `../FineStructureVoxel`, `../game-language` (finescript),
  `../finegui`, `../finenet`, `../FineStructureVK`. The engine links their prebuilt libraries, so a
  change to `finenet` or `finegui` needs `make` in that repo's `build/` before `ninja` sees it.
- `--game-root` means a source tree, and saves go there. With none it's an installed copy, using
  the game it shipped with and a per-user folder. Releases: see the end of `docs/PLATFORMS.md`.
- Build dir: `../fv-keystone` (Ninja, Release with symbols). A fresh configure needs
  `-DCMAKE_POLICY_VERSION_MINIMUM=3.5` (lz4's CMake is too old for CMake 4), and
  `-DCMAKE_CXX_FLAGS_RELEASE="-O3 -g -DNDEBUG"` so core dumps have line numbers.
- Race hunting: `../fv-tsan` is the same build with `-fsanitize=thread` in the C/C++ and
  linker flags. Run the real game on it. It should report nothing.
- Dependencies must be Release too (`cmake -DCMAKE_BUILD_TYPE=Release` in each `build/`).
  An empty build type means `-O0`, and finescript runs 16-24x slower.

## Script gotchas

finescript traps that cost real time here:

- `(...)` is infix math only. Calls need `{...}`: `(loc "x")` is a parse error.
- `else` needs `do`: `else do ... end`. So do `while` and `for`: only `if` takes a `{ ... }` body.
- A statement ends at the newline. Named args can't wrap to the next line.
- `match` and `source` are keywords, so they can't be variable names.
- `random_range` is inclusive at both ends.
- Two integers divide as integers: `(102 / 200)` is 0. A fraction (a UV, a share) needs a
  float on one side: `(102 / 200.0)`.
- Maths is plain functions: `{sqrt x}`, `{max a b}`, `{abs x}`. There's no `math.` map.
- `on` is a keyword, so it can't name a parameter.
- `if (x) { ... }` takes one statement (`a.push b` and `a.set i v` are fine there). For more, use
  `do ... end`, and the same goes for a `match` arm.
- `{}` is an empty block, not an empty map.
- A method can be called on a call's result since finescript b7ea438: `{{loc "x"}.split " "}`.
  A field always worked: `{UNITS.get k}.cost`.
- `set a[i] v` doesn't assign: it silently replaces `a` with the array `[i]`. Use `a.set i v`.
- A closure made in a `for` loop shares the loop's variable: every button built that way acts
  on the last value. Make the closure in a function that takes the value as a parameter. That
  also keeps it alive: put in a local array or map right in the loop body and handed straight to
  `ui_show`, it dies with the loop ("Closure scope has expired").
- A closure in a local array that the UI keeps can't read that array: after the function
  returns, the name is nil. Keep what it needs in a global or a parameter.
- Interpolating a symbol keeps its colon: `"a.{sym}"` gives `a.:name`.
- Interpolation can't nest quotes: `"{loc "x"}"` doesn't parse. Assign it to a `let` first.
- Script `:air` is normalised to real air by `world_set_block`/`site_add`.
- Indented config lines are continuation data: `.emodel` keys must not be indented.
- Every layer of a `.blueprint` starts at the same corner, so a narrower layer is centred with
  blank rows and leading spaces, and one row too many is a whole row out of line.
- `.emodel` parts are boxes drawn with no culling. Where same-facing faces of two parts that look
  different overlap, keep them 2cm apart: level ones fight at every distance, and 1cm speckles at
  range. `tools/check_models.py` checks this, and `tests/run_all.sh` runs it.
- `.geom` UVs are image coordinates: `v = 0` is the top row, so a side face's top edge has the
  lower `v`. `finevox_render_tests` goldens can't see this; `MeshTest.*SideFaces*` covers cubes.
- A `.geom` face with a name of its own always takes the block's top texture, and tiles are 32x32,
  so a shape that needs two looks puts both on one tile (see the bush in `tools/gen_blocks.py`).
- A block that isn't `opaque` reads as foliage to sight: it thins the view through it, but doesn't stop
  it. A shape that doesn't fill its cell has to be non-opaque, or its neighbours stop drawing the
  faces next to it.
- A new unit order must `ent_stop` first. Otherwise the old route finishes, reads as
  `:arrived`, and the new order is taken as done.
- The UI runs after the scene has drawn. `marker_*` geometry shows a frame later, so it goes
  through `markers` and `flushMarkers`, which allow for the camera having moved since.
- `on :command` runs during a pause, so a command that shouldn't work with the clock stopped has to
  check `game_paused`, and `on :tick` doesn't, so what clients need to hear goes in `publish_all`.
- A finished building keeps its site, and `site_progress` on it is how much still stands (`integrity`).
  Anything that changes a building's blocks, the Steward included, gets put back by Hands as a repair.
- `order_units` takes a click into the fog as it is, so a scenario has to let a unit see a far
  building (30 ticks will do) before it sends anyone there. Buildings give no sight of their own:
  with nobody near, even the owner's `inspect_at` on one reads as stale.
- Path searches are the main CPU cost: never re-issue `ent_goto` every think. See the
  backoff in `give_up` and the re-search rule in `chase`.
- Hands get building work from a task board per side ("The task board" in `jobs.fsc`, and the
  engine's `docs/systems/task-board.md`), not from `site_claim`. It re-solves every `BOARD_INTERVAL`
  ticks and can move a Hand to another cell: anything that sends a Hand somewhere on its own must
  `drop_job` (which does `board_release`) or the board still counts it for its old cell. A Hand that
  walks with a load checks the board as it goes (`lost_task`, `answer_board`).
- A Hand that squats about 3s at an empty cell and walks off is in `wait_for_room`. `cell_occupied`
  counts any body that crosses into the cell: a Hand in the next cell within 0.3 of the edge, or a
  head from the cell below. Placing never checks the load, and Hand scaffolding is free. The
  `carry_marks` setting shows each Hand's load over its head, and a red ring while it waits.

## Testing

- Run test **binaries** serially (`./finevox_tests`, `./finevox_script_tests`,
  `./finevox_render_tests`). `ctest -j` gives ~33 false failures: each case is a separate
  process and fixtures share temp paths.
- Baseline: 2405 + 188 + 47 = 2640 passing, 4 skipped. Check exit codes, not the tail of the
  output: a segfault mid-run prints no summary at all.
- After an engine header change, build with bare `ninja`. Naming targets leaves
  `finevox_headless` stale, and every `run_all.sh` scenario then dies with exit 139.
- A UI change needs a real run: `gui_click x y` sends ImGui a real click from a UI script,
  `gui_hover x y` rests the mouse to show a tooltip, and `do_action "toggle_view"` presses a
  bound action. A scratch `--game-root` with extra lines at
  the end of `ui/main.fs` drives them without touching the real tree.
- `gui_click` goes straight to ImGui. To test who gets an event, use `input_key`,
  `input_button` and `input_move`, which go down the whole listener chain, as
  `tests/run_ui.sh` does. That run opens a window, so it's separate from `run_all.sh`.
- `tests/run_ui.sh` wants the machine to itself. Beside another window its frame-timed input
  misses, and a dozen checks fail with nothing wrong. Clicks in it are pixel positions, so anything
  that moves the palette moves them, and 720 tall is about all the palette has room for. The menu
  clicks are pixels too. On 2026-11-12 its four menu checks (Done, New Game, Cancel, Resume)
  failed on the Mac at unchanged code too: on their own they say nothing about a change.
- A window run always goes through the lobby. Until 2026-11, the lobby's random seed beat `--seed`,
  so `run_ui.sh` and `run_mcp.sh` played a new world every time. A check that fails on some runs
  and not others could have been that, so if one turns out flaky, check the world is the same first.
- `assert` compiles away in Release, so the `test_phase*` binaries in finevk and finegui
  check little there. Rebuild with `-UNDEBUG`, or use a check that always runs.
- `tests/run_net.sh` runs a server and bots over real sockets, through a proxy that cuts and slows
  them. `tests/run_windows.sh` drives real windows over MCP, for typing, opening a game live and
  saving. Both are separate from `run_all.sh`. Set `KEYSTONE_BUILD=../fv-tsan` to run them for races.
- Run the bots with `--cache` pointed at a temp directory, or they use the real content cache.
- `tests/run_all.sh` runs the headless gameplay scenarios. Prefer these over watching the
  game: `finevox_headless --script` loads a scenario next to the real game script, and
  any script error fails the run. Assert on stalls as well as outcomes: a warehouse test
  once passed while the whole economy was frozen at 32ms a tick.
- Enemies spawned near home for a look at them count as raiders: Hands drop their work and flee.
  Only armed ones (militia, bows) or one that hit them: a scout or a Hand can't hurt anyone.
- `place_building` returns nil on taken ground (`plot_of`, `yard_plot`), which takes in the Hall and
  the yard, so place relative to `f.x`, not to a Hand that may be standing by them.
- `game_exit 0` later in the same tick overrides an earlier `game_exit 1`. See `failed` in `tests/hunt.fsc`.
- `guard` fails about 1 run in 6 on unchanged code, "the wrong number answered the alarm" with
  nobody answering. The cause isn't known yet.
- `explore` failed once in a dozen runs (2026-11-11), "it left over a sixth of the map" with 109 dead
  ends, and passed twice straight after. Same: cause unknown, rerun first. So does `hunt`, "the order
  fell behind its quarry", about 1 run in 5 on unchanged code.
- Brigands are a faction (`BRIGAND_ID`, 15) with no settlement, zone or Keystone. A loop over
  `factions` that reads a zone, a Hall or the match has to go through `is_side`, and a scenario's
  own loops too: `resume.fsc` once died on it.
- `tools/duel.sh A B [SEEDS] [MINUTES]` plays two commanders headless and prints `SCORE`.
  `old_veteran` and the like are the commander at `OLD_COMMIT`, renamed by `tools/old_commander.py`.
  `DUEL_SWAP=1` plays every seed both ways round, and always use it: side 2's home on the default
  map is much harder to take, and one side's games alone mislead. `DUEL_JOBS=8` suits the Mac.
  The same seed plays differently each run (threads): 12 games swung a level from 5-1 to 2-5, so
  judge on 32 (16 seeds both ways). That's about 3 minutes of 20-minute games.
- Levels to try go in `tests/strategies.fsc` (`DUEL_SCRIPT`), as the veteran with numbers changed,
  so nothing in the tree changes. `tools/tune.py` hill-climbs a level's numbers by playing it
  against itself. A duel reads scripts as each game starts, so editing the tree mid-run mixes
  versions: play from a copy (`rsync` the tree, set `KEYSTONE_BUILD`, and `GIT_DIR` for `old_`).
- What duels measured, and why the veteran is what it is: `docs/AI.md`. In short, the economy
  matters most (20 Hands on two sites beat 8 Hands 22-4), bows are poor value for what they cost,
  and a held-back guard, racing home attacks and Keystone raids measured neutral or worse.
- Duels only measure commanders against commanders. A player who opens with five shelters and grows
  to 20+ Hands beat the old 8-Hand veteran easily (2026-11-11), which is what led to the above.
- Commanders fight with whatever combat the scripts have, so a combat rule changes the AI's results.
  A set fight showed holders losing 6 v 6 to a group that marched in, till guards waited for enemies
  to come near and soldiers went for the nearest wounded enemy.
- `site_progress` counts cells that already match as done, so a site that's never been touched
  can read 44/140. Measure the gain from when it was placed.
- `vision_can_see_ent` can drop out for a moment, even five blocks off. Don't end anything on one read.
- A soldier guards `s.post`, and walks back if it's more than 6 blocks off with nothing to do. A
  scenario that moves one with `ent_set_pos` has to set `s.post` too. See `place` in `tests/guard.fsc`.
- A client only knows what the protocol tells it, even in single player: see the engine's
  `docs/systems/multiplayer.md`. Its world comes from a `WorldReplica` that sweeps for changes and can
  lag by a second, so a player's own action needs `predict_block` or `expect_change` beside its
  `send_command`. A prediction stands for 150ms plus two round trips.
- `factions[1]` is not "the player": there can be several, and `my_faction` says which in the UI.
  In game scripts, `faction_of sender` is who a command is from.
- `ui_publish` with no audience goes to everyone. Anything about one side needs `=faction f.id`.
- `ent_goto =partial` fails as soon as a hop gains no ground, so a lake reads as `:failed` even
  when there's a way round from elsewhere. Failed means from here, not ever.
- Light doesn't lag with it: the client draws old blocks with the server's current light. A
  mesh that reads a block's own cell can see it dark for a few frames after it turned solid.
- Loading relights the whole world, so a light bug only exists in a game in progress and a
  reload hides it. Check it with `light_at` from a scenario, as `tests/light.fsc` does.
- Tree shade comes from `blocks-sky-light: true` on `canopy`: see-through, but the sky stops there.
  Without it, sky light falls straight past a non-opaque block, as it does past bushes.
- A block shape can follow what's above it: `connect_up_*` faces show with one of its `connects`
  group on top, `open_up_*` without. That's how a boulder under another is flat on top.
- Screenshots: `./run.sh --size 1280 720 --screenshot PATH --screenshot-frame N --exit-after M`.
  `--validation` turns on Vulkan validation, `--stats` prints render counts each second.
  A screenshot taken at frame N in two runs can be at different game ticks: compare after the
  map has settled, around frame 1500, or the explored ground differs.
- To measure a frame's cost, add `--no-vsync` to `--stats`. On the Mac it runs at 60-80fps
  whatever it draws, 400k triangles included, so count `tris=` there. The GPU shows on the N100.
  `built=` is mesh builds a second, never 0 in a live game: Hands keep changing blocks.
- Canopy is drawn with tufts close up and plain further out, switching where a block is
  `detail_pixels` tall (`game.conf`). A UI script's `detail_pixels n` changes it in a running game,
  0 drawing everything in full, and returns the distance in blocks. Why 24: at 1920x1080 on the
  N100, tufts everywhere cost 10fps at the usual camera distance (65 to 55), and 24 gave the
  cubes' 64-65 while reaching 40 blocks. 16 reached 61 and cost the same as tufts everywhere.

## Multiplayer

- `./run.sh` opens on the menu. `--new` and `--continue` skip it, `--join ADDR --name N` joins, and
  `--server [--config FILE] [--port N]` serves with no window. For two windows on one machine, give
  each its own `--mcp-port`.
- `scripts/lobby.fsc` declares the pre-game: settings, who can launch. A `=manifest` on a setting
  or a choice reaches into `game.conf`, which is how a map type changes generation.
- Keep `world_biomes` in `game.conf`, or the new biomes join a default world and every seed changes.

## Tracing

- `trace :category ...` writes a line to `logs/game-<date>-<time>-<pid>.log` (newest 20 kept), with
  the tick: the game's own account of why it did things. A headless run writes none unless given
  `--trace FILE`. With `--mcp` the lines go to the `logs` tool too. Ask a player for the newest
  file when they report something that already happened.
- Categories so far: `setup` (seed, each side's player or rival level), `commander` (a rival's
  decisions: defend, gather, strike, pull back, raid), `brigands` (a band's target and leaving),
  `death` (who, where, and who hit it last), `muster_wait`/`training_wait` (why a queue isn't moving,
  once per change), `muster`/`training` (spawns), `flee`, `path` (gave up, scaffolding), `place`
  (gave up, who was in the way), `board` (a summary every 20 solves), `build`, `match` (a Keystone
  under attack, held, fallen, and the winner). Trace a decision
  when it changes, not every tick.
- A game hosted from the lobby saves to `saves/hosted`, not `autosave`. To see its end state, copy
  the tree and run `finevox_headless --game-root COPY --resume COPY/saves/hosted --script peek.fsc`
  with a script that logs what you want and exits. `finevox_run --server --continue` waits in
  the lobby and never runs the game.
- An entity id says whose it was: look for the `muster`/`training` line that spawned it. One no
  side spawned is a brigand.

## Live inspection (MCP)

- `./run.sh --mcp` serves the running game to the `keystone` MCP server in `.mcp.json`:
  `server_*` tools see the whole simulation, `client_*` see what the player sees, fog included.
  `server_eval` and `client_eval` run finescript with the game's or UI's globals and natives.
  Prefer these to scratch scripts and screenshots when looking at a live problem.
- The bridge in `.mcp.json` stays connected with no game running. A second game on the same
  port fails to bind and carries on without a server: use `--mcp-port N` and `FINEVOX_MCP_PORT`.
  So the `keystone` tools may be talking to the player's own game: `lsof -iTCP:7801` before
  changing anything through them, and test on another port.
- With no MCP client to hand, tools are a plain POST: JSON-RPC `tools/call` to
  `http://127.0.0.1:7801/mcp`.
- `--mcp-player` offers only what a player could use, for LLM players.
- To catch a blink, call `client_screenshot` in a loop with a small `region` and `max_size: 8`.
  It keeps up with the frame rate, and a graph of mean brightness shows what stills can't.
- `tests/run_mcp.sh` tests it end to end, through the bridge, in a real window. It's separate
  from `run_all.sh`. Design and measurements are in the engine's `docs/systems/game-runtime.md`.
  "the server has the rival's ground" failed 1 run in 7 on an N100, in a check before anything
  that run had changed, back when every run had a world of its own (see above). The F11 checks
  miss now and then on macOS, where going full screen is animated. Rerun before suspecting a change.

## Crashes

- `run.sh` turns core dumps on. macOS only writes one if the binary has `get-task-allow`,
  and relinking drops it, so `run.sh` re-signs when needed. A core is 7GB or more: it keeps
  the newest and turns itself off under 25GB free.
- Read one with `lldb -c /cores/core.<pid> ../fv-keystone/finevox_run`.
- Your save survives a crash up to the last autosave. `--no-save` or `--exit-after` keep test
  runs away from it, and anything by hand against `--game-root .` otherwise resumes it.
- `lldb -p <pid> -b` attaches to a running game, and a one-shot breakpoint on
  `GameClient::Impl::updateCamera` gives you `this`. finevk and finegui have no debug info, so
  read their structs by offset. Each stop is a long `dt` for the game.
- Reports with stacks for every thread: `~/Library/Logs/DiagnosticReports/finevox_run-*.ips`.
  Often enough on their own: look at what the other threads were doing.
