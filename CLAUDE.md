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

- Engine repo and deps are siblings: `../FineStructureVoxel`, `../game-language` (finescript),
  `../finegui`, `../finenet`, `../FineStructureVK`.
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
- `{}` is an empty block, not an empty map.
- A closure made in a `for` loop shares the loop's variable: every button built that way acts
  on the last value. Make the closure in a function that takes the value as a parameter.
- Interpolating a symbol keeps its colon: `"a.{sym}"` gives `a.:name`.
- Interpolation can't nest quotes: `"{loc "x"}"` doesn't parse. Assign it to a `let` first.
- Script `:air` is normalised to real air by `world_set_block`/`site_add`.
- Indented config lines are continuation data: `.emodel` keys must not be indented.
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
- Baseline: 2254 + 149 + 45 = 2448 passing, 4 skipped. Check exit codes, not the tail of the
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
- `tests/run_all.sh` runs the headless gameplay scenarios. Prefer these over watching the
  game: `finevox_headless --script` loads a scenario next to the real game script, and
  any script error fails the run. Assert on stalls as well as outcomes: a warehouse test
  once passed while the whole economy was frozen at 32ms a tick.
- The client's world is `WorldReplica`, which sweeps for changes and can lag by a second. A
  player's own action needs `predict_block` or `expect_change` beside its `send_command`. The
  server's word overrides a prediction after 3 ticks.
- Loading relights the whole world, so a light bug only exists in a game in progress and a
  reload hides it. Check it with `light_at` from a scenario, as `tests/light.fsc` does.
- Screenshots: `./run.sh --size 1280 720 --screenshot PATH --screenshot-frame N --exit-after M`.
  `--validation` turns on Vulkan validation, `--stats` prints render counts each second.

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
