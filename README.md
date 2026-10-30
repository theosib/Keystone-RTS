# Keystone

An RTS on a fully mutable voxel world. Your stockpile is real blocks in a yard your
workers keep tidy. Fog of war is true 3D line of sight. A civilization falls when its
Keystone is taken, and whatever the outcome, the world stays yours to keep playing.

Keystone is built on [FineStructureVoxel](../FineStructureVoxel) and is **content,
script, and configuration**: there is no C++ here. See [docs/DESIGN.md](docs/DESIGN.md)
for the game design and [docs/STATUS.md](docs/STATUS.md) for what's built.

## Running

Build the engine once (see [CLAUDE.md](CLAUDE.md)), then:

```sh
./run.sh
```

| Input | Action |
|---|---|
| Left-drag | Select your units |
| Right-click | Send them there |
| WASD / arrows, screen edge | Pan |
| Middle-drag, or hold Option/Alt and move the mouse | Grab the ground and drag it |
| Q / E | Rotate |
| Scroll | Zoom |
| Tab | Step down as the Steward, or back up (WASD, mouse, Space, left-click to work) |
| Build palette | Pick a building, left-click to place, R to turn, right-click to cancel |
| F12 | Screenshot |
| Esc | Cancel placement, or quit |

## Layout

| Path | What |
|---|---|
| `game.conf` | Manifest: world size, scripts, fog of war |
| `scripts/` | Game logic, game thread. `main.fsc` sources the rest |
| `ui/` | HUD, render thread. Talks to the game only through commands. `ui/skin/` is its look |
| `blocks/` `entities/` `models/` `blueprints/` `biomes/` `features/` | Content |
| `lang/` | Every player-facing string, per locale |
| `tools/` | Generators for textures, animation clips, and fill-stage blocks (Python, uses `.venv`) |
| `tests/` | Headless gameplay scenarios. `tests/run_all.sh` runs them all |

Regenerate assets with `tools/gen_textures.sh`, `python3 tools/gen_models.py`,
`python3 tools/gen_blocks.py`, and `.venv/bin/python tools/gen_ui_skin.py`.

The UI font is [Almendra](https://fonts.google.com/specimen/Almendra) by Ana Sanfelippo, under the
SIL Open Font License: see `ui/skin/Almendra-OFL.txt`.
