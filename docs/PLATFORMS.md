# Keystone on Other Machines

For anyone, agent or person, working on this somewhere other than the Mac it was written on.

**Only macOS has ever built or run any of this.** It was Apple Silicon, Xcode's clang, the LunarG
Vulkan SDK, and Homebrew for the rest. What follows about Linux and Windows comes from reading the
code, not from trying it, so correct this file as you find out.

## Getting the code

All of these sit side by side in one directory, under exactly these names. The builds find each
other by relative path, and three of the names differ from their GitHub repos.

| Directory | Clone from | What it is |
|---|---|---|
| `Keystone` | `theosib/Keystone-RTS` | This game: content, script, config |
| `FineStructureVoxel` | `theosib/FineStructureVoxel` | The engine |
| `game-language` | `theosib/FineStructureScript` | finescript |
| `FineStructureVK` | `theosib/FineStructureVulkan` | Vulkan wrapper |
| `finegui` | `theosib/finegui` | GUI |
| `finenet` | `theosib/finenet` | Networking |

```sh
mkdir FineStructure && cd FineStructure
git clone https://github.com/theosib/Keystone-RTS.git Keystone
git clone https://github.com/theosib/FineStructureVoxel.git
git clone https://github.com/theosib/FineStructureScript.git game-language
git clone https://github.com/theosib/FineStructureVulkan.git FineStructureVK
git clone https://github.com/theosib/finegui.git
git clone https://github.com/theosib/finenet.git
```

**Set the commit identity in every clone**, before committing anything. These repos are public, and
a machine's global git config may hold an address that shouldn't be in them:

```sh
for repo in Keystone FineStructureVoxel game-language FineStructureVK finegui finenet; do
    git -C $repo config user.email 5619995+theosib@users.noreply.github.com
done
```

The rules in [../CLAUDE.md](../CLAUDE.md) apply everywhere: commit to `main`, push, no PRs. Note that
global instructions from another machine don't come with the repo.

## Prerequisites

| Need | For |
|---|---|
| CMake 3.20+, a C++20 compiler, git | Everything. CMake fetches lz4, glm, nlohmann_json and googletest, so the first configure needs a network |
| Python 3 | `tests/run_net.sh`, `tests/run_windows.sh`, `tests/run_mcp.sh`. Standard library only, so no venv |
| bash, rsync | The `tests/*.sh` scripts and `run.sh` |
| Vulkan SDK with `glslc`, GLFW 3, GLM | Only for the windowed client |

## Building

Dependencies first, in this order, each in its own `build/`, and always Release: an empty build type
means `-O0`, and finescript runs 16-24x slower. The engine links what it finds in those directories,
so after changing one, rebuild it there before rebuilding the engine.

**No GPU needed.** `FINEVOX_BUILD_RENDER` is off by default, and with it off you still get
`finevox_headless`, `finevox_bot`, and the unit tests. That's a real server and real clients, so a
headless Linux box is a useful machine for this.

```sh
for dep in game-language finenet; do
    cmake -S $dep -B $dep/build -DCMAKE_BUILD_TYPE=Release && cmake --build $dep/build -j
done
cmake -S FineStructureVoxel -B fv-keystone -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_POLICY_VERSION_MINIMUM=3.5 '-DCMAKE_CXX_FLAGS_RELEASE=-O3 -g -DNDEBUG'
cmake --build fv-keystone
```

**With a window**, add the graphics libraries first, and turn the render module on. `finevox_run`
is quietly left out if `finegui` hasn't been built, so check that it exists afterwards.

```sh
for dep in FineStructureVK finegui; do
    cmake -S $dep -B $dep/build -DCMAKE_BUILD_TYPE=Release && cmake --build $dep/build -j
done
cmake -S FineStructureVoxel -B fv-keystone -DFINEVOX_BUILD_RENDER=ON
cmake --build fv-keystone
```

## Testing

From `Keystone/`. Run the engine's test binaries serially, as [../CLAUDE.md](../CLAUDE.md) says.

| Command | Needs a display | Covers |
|---|---|---|
| `tests/run_all.sh` | No | Gameplay scenarios. About 4 minutes |
| `tests/run_net.sh` | No | A server and bots on real sockets. About a minute |
| `tests/run_ui.sh`, `tests/run_mcp.sh`, `tests/run_windows.sh` | Yes | The client |

`explore` in `run_all.sh` is flaky: see [STATUS.md](STATUS.md).

## Linux

Nothing's known to be broken, but nothing's been tried. In order of what's most likely to bite:

- Discovery: see the `SO_REUSEPORT` note in `finenet/docs/STATUS.md`.
- The windowed client on real hardware. `FineStructureVK` has Linux paths that haven't been run here.
- `run.sh` skips its core dump handling on anything but macOS, so a crash needs `ulimit -c` by hand.
  The notes on crashes in [../CLAUDE.md](../CLAUDE.md) are macOS-specific: `lldb -c`, `.ips` reports.
- ThreadSanitizer should work better than it does on macOS, and is worth running: a build with
  `-fsanitize=thread` in the compile and link flags, then `KEYSTONE_BUILD=<dir> tests/run_net.sh`.

### Working on it from the Mac over SSH

Everything in the "No" row above runs in a plain SSH session. For the client, the machine needs a
logged-in display. MCP only listens on 127.0.0.1, so reach it with a tunnel:

```sh
ssh -L 7801:127.0.0.1:7801 linuxbox
```

The game's own ports are the other way round: they're open to the network, so a firewall may need
telling about **TCP 7810** (game) and **UDP 7811** (discovery).

## Two machines

This is the test that's never been run, and it's the one that says whether LAN discovery really
works. A Mac and a Linux box will do. Run it both ways round, with the server on each.

```sh
# On one, in Keystone/
../fv-keystone/finevox_headless --game-root . --serve

# On the other, in Keystone/. watcher.fs needs these two lines first, so build the script as
# tests/run_net.sh does
cat tests/net/common.fs > /tmp/find.fs
printf 'set MIN_PLAYERS 1\nset RUN_MS 5000\n' >> /tmp/find.fs
cat tests/net/watcher.fs >> /tmp/find.fs
../fv-keystone/finevox_bot --game keystone --game-root . --name Finder --join lan --script /tmp/find.fs
```

It should print `[bot] found <address>`, and then `END faction 1`. Then, if either machine has a
window, try the menus: Host a Game on one, and Join a Game on the other.

If it isn't found, try each route alone before changing anything:

| Try | It tells you |
|---|---|
| `--join <address>:7810` | Whether TCP gets through at all |
| The firewall, with UDP 7811 open | Whether that's all it was |
| Wired on both, or another Wi-Fi | Whether the access point filters multicast or isolates clients |
| Waiting 15 seconds | Whether the /24 sweep finds it, which has never been seen to work |

On a Mac, the first network use by a new binary can bring up a Local Network prompt, and if it's
refused `join.blocked` should appear in the browser. That has never been seen either.

Put what you find in `finenet/docs/STATUS.md`, under "Not tested".

## Windows

Expect this one to need real work before anything links.

| Where | What has to change |
|---|---|
| `finenet/src/{stream_transport,listener,discovery}.cpp` | All POSIX sockets: see `finenet/docs/STATUS.md` |
| `FineStructureVoxel/CMakeLists.txt` | Looks for finescript and finenet as `.dylib`, `.so` or `.a` only, and finegui as `.a` only, so an MSVC `.lib` isn't found. It carries on without them and fails later, or leaves out `finevox_run` |
| `FineStructureVoxel/src/game/mcp_server.cpp` | Its own POSIX sockets, for the MCP server, plus `tests/test_mcp_server.cpp` |
| `FineStructureVoxel/apps/finevox_run.cpp` | `execv` to restart, which `new_game` and `to_menu` rely on. `_spawnv` then exit, or relaunch |
| `FineStructureVoxel/src/net/content.cpp` | `getpid` and `<unistd.h>`. Symlinks need a privilege, but it already falls back to copying |
| `FineStructureVoxel/src/game/game_host.cpp` | `SIGTERM` barely exists there, so a server can't be asked to stop and save except by console control events |
| Paths | The cache uses `$XDG_CACHE_HOME` or `$HOME`, so it needs `%LOCALAPPDATA%` |
| `Keystone/run.sh`, `tests/*.sh` | bash. Git Bash should do, with `finevox_run.exe` |

`module.cpp`, `resource_locator.cpp` and `frame_capture.cpp` already have Windows branches, untested.
`FineStructureVK` has Windows paths in its CMake, also untested.

## Where problems are recorded

| Repo | File |
|---|---|
| finenet | `docs/STATUS.md` |
| FineStructureVoxel | `docs/systems/multiplayer.md`, at the end, and `docs/STATUS.md` |
| finegui | `docs/CHECKLIST.md`, at the end |
| Keystone | [STATUS.md](STATUS.md) |
