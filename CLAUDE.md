# Keystone

RTS × voxel sandbox built on FineStructureVoxel. It is both a playable game and the foil that
drives engine development. See [docs/DESIGN.md](docs/DESIGN.md), [docs/PLAN.md](docs/PLAN.md),
[docs/STATUS.md](docs/STATUS.md).

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

- On any other machine, start with [docs/PLATFORMS.md](docs/PLATFORMS.md): only macOS has been
  built so far, and that's where to find how to set up, what to expect, and the two-machine test.
- Engine repo and deps are siblings: `../FineStructureVoxel`, `../game-language` (finescript),
  `../finegui`, `../finenet`, `../FineStructureVK`. The engine links their prebuilt libraries, so a
  change to `finenet` or `finegui` needs `make` in that repo's `build/` before `ninja` sees it.
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
- Maths is plain functions: `{sqrt x}`, `{max a b}`, `{abs x}`. There's no `math.` map.
- `on` is a keyword, so it can't name a parameter.
- `if (x) { ... }` takes one statement. For more, or for `a.push b`, use `do ... end`, and the same
  goes for a `match` arm.
- `{}` is an empty block, not an empty map.
- `set a[i] v` doesn't assign: it silently replaces `a` with the array `[i]`. Use `a.set i v`.
- A closure made in a `for` loop shares the loop's variable: every button built that way acts
  on the last value. Make the closure in a function that takes the value as a parameter.
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
- A new unit order must `ent_stop` first. Otherwise the old route finishes, reads as
  `:arrived`, and the new order is taken as done.
- The UI runs after the scene has drawn. `marker_*` geometry shows a frame later, so it goes
  through `markers` and `flushMarkers`, which allow for the camera having moved since.
- Path searches are the main CPU cost: never re-issue `ent_goto` every think. See the
  backoff in `give_up` and the re-search rule in `chase`.

## Testing

- Run test **binaries** serially (`./finevox_tests`, `./finevox_script_tests`,
  `./finevox_render_tests`). `ctest -j` gives ~33 false failures: each case is a separate
  process and fixtures share temp paths.
- Baseline: 2269 + 174 + 45 = 2488 passing, 4 skipped. Check exit codes, not the tail of the
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
- `place_building` returns nil on taken ground (`plot_of`, `yard_plot`), which takes in the Hall and
  the yard, so place relative to `f.x`, not to a Hand that may be standing by them.
- `game_exit 0` later in the same tick overrides an earlier `game_exit 1`. See `failed` in `tests/hunt.fsc`.
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
- Screenshots: `./run.sh --size 1280 720 --screenshot PATH --screenshot-frame N --exit-after M`.
  `--validation` turns on Vulkan validation, `--stats` prints render counts each second.

## Multiplayer

- `./run.sh` opens on the menu. `--new` and `--continue` skip it, `--join ADDR --name N` joins, and
  `--server [--config FILE] [--port N]` serves with no window. For two windows on one machine, give
  each its own `--mcp-port`.
- `scripts/lobby.fsc` declares the pre-game: settings, who can launch. A `=manifest` on a setting
  or a choice reaches into `game.conf`, which is how a map type changes generation.
- Keep `world_biomes` in `game.conf`, or the new biomes join a default world and every seed changes.

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
