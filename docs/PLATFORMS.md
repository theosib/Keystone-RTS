# Keystone on Other Machines

For anyone, agent or person, working on this somewhere other than the Mac it was written on.

**macOS, Linux and Windows all build and run all of this.** macOS is where it's written: Apple
Silicon, Xcode's clang, the LunarG Vulkan SDK, and Homebrew for the rest. Linux has been run on one
machine: Ubuntu 22.04, x86-64, GCC 11.4, CMake 3.22, and an Intel N100 with Mesa 23.2. Windows has
too: Windows 11, MSVC 19.50 from Visual Studio 2026, CMake 4.2, and an RTX 4060 Ti. See
[Windows](#windows) for how it differs.

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
git clone --recurse-submodules https://github.com/theosib/finegui.git
git clone https://github.com/theosib/finenet.git
```

`--recurse-submodules` matters: ImGui is a submodule of finegui, which won't configure without it.
In a clone that's missing it, `git submodule update --init` in `finegui/` fixes that.

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

On Ubuntu 22.04, this is everything but `glslc`:

```sh
sudo apt-get install --no-install-recommends cmake ninja-build g++ git rsync python3 libvulkan-dev \
    vulkan-tools vulkan-validationlayers libglfw3-dev libglm-dev libxkbcommon-dev libasound2-dev
```

22.04 has no `glslc` package. Unpack LunarG's Linux tarball anywhere and put its `x86_64/bin` on
`PATH` while configuring. Only `glslc` is needed, so a `tar --wildcards '*/x86_64/bin/glslc'` is 30MB
and not 300. Without it the build goes on and leaves out the shaders, so check `fv-keystone/shaders/`.

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

Both of these recipes have been run from clean clones of GitHub, on macOS and Linux. **With a
window**, add the graphics libraries first, and turn the render module on. `finevox_run` is quietly
left out if `finegui` hasn't been built, so check that it exists afterwards.

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

Results on the machine above:

| Test | Result |
|---|---|
| `finevox_tests` | 2308 of 2309. `FactionVisionTest.ManyUnitsStayCheap` fails, see below |
| `finevox_script_tests`, `finevox_render_tests` | All pass, goldens included: a pixel at most 1/255 off the Mac's |
| `finenet_tests` | All 118 pass |
| `tests/run_net.sh` | All pass |
| `tests/run_all.sh` | 28 of 29. `explore` is the flaky one in [STATUS.md](STATUS.md): 2 runs in 8 here and 1 in 7 on a Mac, and the same seed passes on one and fails on the other |
| `tests/run_ui.sh`, `tests/run_mcp.sh`, `tests/run_windows.sh` | All pass, with `--validation` clean on Intel's driver |

A game takes this four-core machine about 3.9ms a tick of the 33 it has, and its integrated GPU
draws the game at 60fps at 1280x720.

- **Vision on a slow CPU.** A full pass for 200 units takes 32ms on an N100 and 7.5ms on an M-series
  Mac, and the test's limit is 30. Every faction gets a pass each 15 ticks, on a tick of its own where
  it can, and the rest of the game has to fit around it in 33ms, so a big army on a small server
  should stutter. That hasn't been seen, only measured. The test is telling the truth, so don't relax
  it: make vision cheaper.
- **Crashes.** `run.sh` skips its core dump handling on anything but macOS, so a crash needs
  `ulimit -c` by hand, and Ubuntu sends cores to apport. The notes on crashes in
  [../CLAUDE.md](../CLAUDE.md) are macOS-specific: `lldb -c`, `.ips` reports.
- **ThreadSanitizer needs Clang here.** `tests/run_net.sh` passes under it with nothing reported.
  GCC 11's TSan can't see `pthread_cond_clockwait`, which every timed wait goes through, so it never
  sees the mutex released. That gave 64 false reports of double locks, lock inversions and races, and
  failed 6 checks. Build with `-DCMAKE_C_COMPILER=clang -DCMAKE_CXX_COMPILER=clang++`, and
  `-fsanitize=thread` in the compile and link flags. Newer kernels then stop TSan with "unexpected
  memory mapping", so run `KEYSTONE_BUILD=<dir> setarch $(uname -m) -R tests/run_net.sh`. Set
  `TSAN_OPTIONS=log_path=<file>` too: `run_net.sh` deletes its logs, and reports with them.

What Linux turned up, which applies to anything added from here on:

- libc++ pulls in far more through its headers. `<shared_mutex>` doesn't give you `unique_lock` on
  libstdc++, and `<cstring>`, `<cstddef>`, `<cmath>` and `<stdexcept>` have to be asked for.
- `unordered_map` of an incomplete type doesn't compile on libstdc++, in a header or anywhere else.
- A static library that ends up in a shared one needs position-independent code. macOS is always
  that, so nothing tells you there.
- A swap chain can be recreated twice at one size, as leaving full screen does here and a Mac never
  did. Anything built on its images has to go by `SwapChain::generation()`, not by comparing sizes.
- GLFW minimizes a full-screen window that loses focus. Under GNOME that's 1fps, and it stays hidden
  after the other window closes, so `Window::toggleFullscreen` turns it off.
- `uint64_t` is `unsigned long` here and `unsigned long long` on a Mac, so don't `reinterpret_cast`
  between it and anything else 64-bit.
- GCC and Clang evaluate function arguments in opposite orders. `vec3(next(), next(), next())` gives
  different results, and two calls that both change something belong in separate statements.
- `std::uniform_int_distribution` gives different numbers on different standard libraries.
  `std::mt19937` itself doesn't.
- Before CMake 3.24 an unknown keyword in `FetchContent_Declare` joins the argument ahead of it, so
  `DOWNLOAD_EXTRACT_TIMESTAMP` shows up as a bad `URL_HASH`.

### Working on it from the Mac over SSH

Everything in the "No" row above runs in a plain SSH session. For the client, the machine needs a
logged-in display. MCP only listens on 127.0.0.1, so reach it with a tunnel:

```sh
ssh -L 7801:127.0.0.1:7801 linuxbox
```

Ubuntu's GNOME is Wayland, and `DISPLAY=:0` is its Xwayland. Logged in over SSH as the same user as
the desktop, nothing needs opening up. It only needs telling where to look:

```sh
export XDG_RUNTIME_DIR=/run/user/$(id -u) WAYLAND_DISPLAY=wayland-0 DISPLAY=:0
export XAUTHORITY=$(ls -t $XDG_RUNTIME_DIR/.mutter-Xwaylandauth.* | head -1)
```

**A locked desktop holds a window to exactly 1fps,** and unlocking it gives 60. Nothing is wrong and
nothing else slows down, so it just looks like a very slow game. GNOME locks after five idle minutes.
`loginctl show-session <n> -p LockedHint` says if it's locked. A window still opens, and a screenshot
at a low `--screenshot-frame` still works, but the `run_ui.sh` family needs it unlocked.

The game's own ports are the other way round: they're open to the network, so a firewall may need
telling about **TCP 7810** (game) and **UDP 7811** (discovery).

## Two machines

This says whether LAN discovery really works. It's passed both ways round, between a Mac and a
Linux box on the same Wi-Fi, with both firewalls off: each found the other within a second or two
and joined. A window on a Mac has played on a Linux server, at 57fps with the N100 at 17% CPU. Join a
Game on Linux listed a Mac's game in half a second and joined it, and listed one from its own
machine too. Repeat it on any new network.

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
window, try the menus: Host a Game on one, and Join a Game on the other. `--join lan` is for bots: a
window takes it as a hostname, and needs an address or the menu.

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

It needs Visual Studio 2022 or later with "Desktop development with C++", the Vulkan SDK
(`winget install KhronosGroup.VulkanSDK`), Git for Windows, CMake and Python. Windows has no package
for GLFW or GLM, so one script builds those into `deps\` beside the clones, then everything else in
order, Release, with Ninja, from PowerShell:

```powershell
Keystone\tools\windows\build.ps1
```

That's the development build in `fv-keystone`, tests included. The scripts in `tests/` and `run.sh`
work as they are from Git Bash, where `../fv-keystone/finevox_run` finds the `.exe`.

Results on the machine above:

| Test | Result |
|---|---|
| `finevox_tests` | All 2313 pass, `FactionVisionTest.ManyUnitsStayCheap` included |
| `finevox_script_tests`, `finevox_render_tests` | All pass, goldens included |
| `finenet_tests`, `finescript_tests` | All 118 and all 488 pass |
| `tests/run_all.sh` | All 29 pass. `guard` failed once with a build running beside it, then passed 10 runs of 10 alone |
| `tests/run_net.sh` | All pass, LAN discovery included |
| `tests/run_windows.sh` | All pass |
| `tests/run_mcp.sh` | All 77 checks pass, F11 included, but the window run fails on what validation reports, below |
| `tests/run_ui.sh` | 4 runs in 8 pass. In 2, from the walk up the steps on, keys never reached the game, on a desktop in use. In the others the Steward climbed 2.01 blocks against 2 within 0.01, or 3, over the wall |
| `tests/installed_check.py` | Passes, from CI's installer, with nothing written beside the program |

A game draws at about 60fps at 1280x720 with `--validation` on. The SDK's validation layers report
a screenshot transitioning a swapchain image it hasn't acquired: `GameClient` reads the image back
in `onPostRender`, after it has been presented. That's every platform's, not Windows' own, and
Linux's run was clean only because its SDK was older. The fix is a hook between the renderer's
submit and its present. finevk reusing a render-finished semaphore still in use by a present was
reported too, and is fixed: there's one per swapchain image now.

What Windows turned up, which applies to anything added from here on:

- **Nothing is exported from a DLL**, so on Windows every library here is static: the engine's own,
  finescript, finevk and finegui. Their CMake does that by itself, and the engine finds `name.lib`
  as well as `libname.a`. A shared library can hide a breach of the one-definition rule that a
  static link won't: two classes or functions of one name in one namespace, even in separate files.
- **`windows.h` is full of macros.** `min` and `max`, and `near` and `far` as nothing, and wingdi's
  `DeviceCapabilities`, which renamed finevk's class in the files that saw it and not the rest.
  Everything is built with `NOMINMAX NOGDI WIN32_LEAN_AND_MEAN`, and finevk doesn't define
  `VK_USE_PLATFORM_WIN32_KHR`, since GLFW makes the surface and that define brings `windows.h` in.
- **Sockets.** finenet's `src/socket_compat.hpp` is where POSIX and Winsock differ. `WSAPoll` doesn't
  sleep with nothing to poll, `SO_REUSEADDR` lets a second server take a port in use (so a listener
  uses `SO_EXCLUSIVEADDRUSE`), and UDP needs `SIO_UDP_CONNRESET` off or one ICMP "port unreachable"
  fails the next receive. Sockets are made uninheritable, so a restarted game doesn't hold the port.
- **No exec, no SIGTERM.** `finevox_run` restarts with `CreateProcessW` and exits. A server stops and
  saves on Ctrl+C, Ctrl+Break, its console closing, logoff and shutdown.
- **Paths.** The content cache is `%LOCALAPPDATA%\finevox\cache`, and an installed game's own folder
  is `%APPDATA%\.keystone`. A path written into a script string
  needs forward slashes, or `\U` and the like read as escapes.
- **Python is `python`**, not `python3`. The test scripts take either.
- **No `__builtin_clz`**: use `<bit>`.

`Window::toggleFullscreen` (F11) fills the screen and puts the window back. It leaves GLFW's minimizing
on losing focus switched on for Windows alone: GLFW keeps a full-screen window `HWND_TOPMOST`, so a
window that stayed put would cover whatever you switched to. The check is in `tests/mcp_check.py`.

## Releases

`git tag v0.1.0 && git push origin v0.1.0` runs `.github/workflows/release.yml`, which builds on
GitHub's machines and leaves a **draft** release: try the packages, then publish it. Running the
workflow by hand builds the same packages with no release. It takes each sibling repo's `main` at
the start, and every build uses those commits, which the release notes list. The notes themselves
are `tools/package/release-notes.md`. `windows-release.yml` is the Windows build, called from there,
and can be run by hand too.

Packages come from GitHub's machines, not from here, so no local paths or usernames end up in them.

Where an installed copy finds things, from `ResourceLocator`:

| Scope | Comes from |
|---|---|
| `engine/` | Found from the executable, by its `shaders/`: beside it, in the directory above, in `../Resources`, or in `../share/finevox` |
| `game/` | `--game-root`, or `engine/game` |
| `user/` | `--user-dir`. Otherwise the game root if `--game-root` was given, so a source tree works as it always has, or else where Minecraft would: `~/.keystone`, `~/Library/Application Support/keystone`, `%APPDATA%\.keystone` |

Saves, screenshots and `imgui.ini` go in `user/`. A build's compiled-in paths don't exist on anyone
else's machine, and a test on the machine that built it can't see that: one like this passed here
until the build directory was hidden. Test a package from somewhere read-only, with the build tree
out of reach.

- **macOS:** `tools/package/macos.sh` makes an `.app` and a DMG. `bundle_dylibs.py` copies in every
  library and rewrites its paths, which breaks signatures, so signing comes last. It's an ad-hoc
  signature: Apple silicon won't run native code with none, and it's free. A browser download still
  needs Open Anyway once, or `xattr -cr`. Only a paid Developer ID takes that away. Homebrew's
  libraries set the oldest macOS it runs on, and that's written into `Info.plist`.
- **Linux:** `tools/package/linux.sh` makes an AppImage per architecture, on Ubuntu 22.04, which sets
  the glibc it needs. It has its own GLFW 3.4, and leaves out libc, the Vulkan loader, GPU drivers and
  libstdc++: a driver loaded into the process wants the machine's own. linuxdeploy would bundle the
  loader, so `linux.sh` excludes it by name.
- **GLFW's X11 wait** is patched, by `tools/package/glfw-x11-wait.patch`. GLFW 3.4, and its master
  branch, wait up to 0.1s for a new window to be reported visible, but count any event already
  queued as having waited, so the time never runs down. On an X desktop with no monitor the report
  never comes, and `glfwCreateWindow` never returned. Ubuntu's GLFW 3.3 doesn't do this, so a source
  build there never showed it.
- **Wayland** leaves a swap chain's size to the program. finevk used 800x600 for that until the
  window was resized, so it now takes the window's framebuffer size.
- **Shaders:** SPIR-V is the same on every machine, and 22.04 has no `glslc` for Arm, so CI compiles
  them once and shares them. A build with no `glslc` leaves them out and still links.
- GitHub's Linux machines have no GPU, so CI runs the AppImage on Mesa's software renderer. Its Mac
  run is only a server, and Windows isn't run at all. A release still needs a run on real GPUs
  before it's published.

- **Windows:** `tools/windows/package.ps1` makes an installer and a portable zip from a windowed
  build (`build.ps1 -BuildDir fv-release -Gui -NoTests`). The game goes in as `Keystone.exe`, with
  `shaders\` and `game\` beside it, the C++ runtime, each repo's `LICENSE` and the notices of what's
  linked in. The installer is Inno Setup, per user, in `%LOCALAPPDATA%\Programs\Keystone`, which
  needs no administrator. Nothing is code-signed, so a download's first run brings up SmartScreen's
  "Windows protected your PC": "More info", then "Run anyway". That fades as a release is downloaded
  more. The SignPath Foundation signs open-source projects for free, which is the way to remove it.

## Where problems are recorded

| Repo | File |
|---|---|
| finenet | `docs/STATUS.md` |
| FineStructureVoxel | `docs/systems/multiplayer.md`, at the end, and `docs/STATUS.md` |
| finegui | `docs/CHECKLIST.md`, at the end |
| Keystone | [STATUS.md](STATUS.md) |
