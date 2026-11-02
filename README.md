# Keystone

An RTS on a fully mutable voxel world. Your stockpile is real blocks in a yard your
workers keep tidy. Fog of war is true 3D line of sight. A civilization falls when its
Keystone is taken, and whatever the outcome, the world stays yours to keep playing.

- **Your stockpile is made of blocks.** Every plank is somewhere in the world, stacked by workers
  who keep rows and aisles, and who build a warehouse upwards when the yard fills.
- **Fog of war you can't cheat.** Sight is 3D line of sight, and the server only ever sends what
  your side can see.
- **Rivals who play by your rules.** They react to what their units saw, and nothing else.
- **Command from above, or walk the world.** Press Tab to drop down and work with your own hands.
- **Play together.** Host a game and friends on your network find it, or open a game you've already
  started. Each player has their own settlement and their own fog.

**[Read about the game](docs/GAME.md)**: how a match goes, what's built, and where it's going.

Keystone is **content, script, and configuration**, with no C++ of its own. See
[How it's built](#how-its-built) for what's underneath.

## Installing

Download Keystone for Windows, macOS or Linux from
[Releases](https://github.com/theosib/Keystone-RTS/releases). Windows and macOS each need one extra
step the first time, because the app isn't signed, and the release page says how.

## Running from source

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
| Z F X T L | Shelter, Forager Hut, Sawpit, Watchtower, Log Palisade |
| H M B P | Train a Hand, or muster Militia, a Shortbow, a Pathfinder. Hold Shift, or Shift-click the button, for 8 |
| Blocks bar, or 1-6 | Choose a block. Overhead, drag to mark a run for Hands to build. On foot, right-click places it |
| F11 | Full screen, or back. On a Mac this is the window's green button, or Ctrl+Cmd+F if the system keeps F11 for itself |
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

## How it's built

Keystone is the game. The rest is a family of open-source libraries, each with its own overview:

| Project | What it is |
|---|---|
| [FineStructureVoxel](https://github.com/theosib/FineStructureVoxel) | The voxel game engine: world, rendering, simulation, multiplayer |
| [finescript](https://github.com/theosib/FineStructureScript) | The scripting language all the game's rules and UI are written in |
| [finegui](https://github.com/theosib/finegui) | The GUI, where a widget is a live script data structure |
| [finenet](https://github.com/theosib/finenet) | Networking, with the same API in one process or across the internet |
| [FineStructureVK](https://github.com/theosib/FineStructureVulkan) | Vulkan with the boilerplate gone |

| Document | Covers |
|---|---|
| [docs/GAME.md](docs/GAME.md) | The game: start here |
| [docs/DESIGN.md](docs/DESIGN.md) | The whole design, including what's still to come |
| [docs/STATUS.md](docs/STATUS.md) | What's built, and what isn't |
| [docs/PLATFORMS.md](docs/PLATFORMS.md) | Building and testing on macOS, Linux and Windows, and making releases |

## License

Apache License 2.0: see [LICENSE](LICENSE) and [NOTICE](NOTICE).
