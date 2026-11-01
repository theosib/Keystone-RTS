# Keystone

An RTS on a fully mutable voxel world. Your stockpile is real blocks in a yard your
workers keep tidy. Fog of war is true 3D line of sight. A civilization falls when its
Keystone is taken, and whatever the outcome, the world stays yours to keep playing.

Keystone is built on [FineStructureVoxel](../FineStructureVoxel) and is **content,
script, and configuration**: there is no C++ here. See [docs/DESIGN.md](docs/DESIGN.md)
for the game design and [docs/STATUS.md](docs/STATUS.md) for what's built.

## Running

Build the engine once (see [CLAUDE.md](CLAUDE.md), or [docs/PLATFORMS.md](docs/PLATFORMS.md) on a
new machine), then:

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
| Tab | Step down as the Steward, or back up (WASD, mouse, Space, left-click to break, right-click to place) |
| Build palette | Pick a building, left-click to place, R to turn, right-click to cancel. Hover any button for what it does and what it needs |
| Log Palisade | Drag out a wall. It joins up straight and on the diagonal. Right-click to finish |
| Blocks bar, or 1-6 | Choose a block. Overhead, drag to mark a run for Hands to build. On foot, right-click places it |
| F12 | Screenshot |
| Hover, or look at, a block | See what it is and what it holds |
| Esc | Cancel placement or the block tool, or close the menu |
| Enter | Say something to the other players |
| Cmd+Q / Ctrl+Q | Save and quit |

The game opens on its title screen. Your game saves when you quit and every 5 minutes, and Continue
picks up where you left off. `./run.sh --new` and `./run.sh --continue` skip the title screen. Saves
live in `saves/`, with the one before kept as `autosave.bak`.

To play with others, Host a Game and they'll find it under Join a Game, or Menu, Invite Players in a
game you've already started. `./run.sh --server` runs a game with no window, and
`./run.sh --join ADDRESS` joins one directly.

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
