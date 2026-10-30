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
- Engine work goes on branch `keystone/stage1` in each dependency repo.

## Layout

- Engine repo and deps are siblings: `../FineStructureVoxel`, `../game-language` (finescript),
  `../finegui`, `../finenet`, `../FineStructureVK`.
- Build dir: `../fv-keystone` (Ninja, Release). Fresh configure needs
  `-DCMAKE_POLICY_VERSION_MINIMUM=3.5` (lz4's CMake is too old for CMake 4).

## Script gotchas

finescript traps that cost real time here:

- `(...)` is infix math only. Calls need `{...}`: `(loc "x")` is a parse error.
- `else` needs `do`: `else do ... end`.
- A statement ends at the newline. Named args can't wrap to the next line.
- `match` is a keyword, so it can't be a variable name.
- `random_range` is inclusive at both ends.
- Interpolating a symbol keeps its colon: `"a.{sym}"` gives `a.:name`.
- Script `:air` is normalised to real air by `world_set_block`/`site_add`.
- Indented config lines are continuation data: `.emodel` keys must not be indented.

## Testing

- Run test **binaries** serially (`./finevox_tests`, `./finevox_script_tests`,
  `./finevox_render_tests`). `ctest -j` gives ~33 false failures: each case is a separate
  process and fixtures share temp paths.
- Baseline at branch point: 2100 + 129 + 41 = 2270 passing.
- `tests/run_all.sh` runs the headless gameplay scenarios. Prefer these over watching the
  game: `finevox_headless --script` loads a scenario next to the real game script, and
  any script error fails the run. Assert on stalls as well as outcomes: a warehouse test
  once passed while the whole economy was frozen at 32ms a tick.
- Screenshots: `./run.sh --size 1280 720 --screenshot PATH --screenshot-frame N --exit-after M`.
  `--validation` turns on Vulkan validation, `--stats` prints render counts each second.
