# First Release: Handoff

In-flight work, for whoever picks it up. Delete this file once v0.1.0 is published, and move
anything worth keeping to [PLATFORMS.md](PLATFORMS.md), whose Releases section explains how the whole
thing works. Read that first.

Nothing has been released yet: there are no tags and no releases on GitHub.

## Where it stands

The last run of the release workflow, on `main`, by hand, with no release attached:

```sh
gh run watch 41539151175 --repo theosib/Keystone-RTS --exit-status
```

https://github.com/theosib/Keystone-RTS/actions/runs/41539151175

The run before it was cancelled. These are the furthest each job had got:

| Job | Last known | Not seen yet |
|---|---|---|
| `windows` | Passed: installer and zip built | Has never been run from a package, see below |
| `linux` (x86-64, Arm) | Compiled. Packaging failed on the icon's size, fixed in this run | linuxdeploy's own packaging, and the smoke run on Mesa's software renderer in Xvfb |
| `macos` | Compiled. Packaging was still running when cancelled | CI's DMG, its smoke run as a server, and the oldest macOS it claims |
| `release` | Skipped on purpose: it only runs for a tag | Has never run |

Expect more small failures in `linux` and `macos`: each run so far has got a step further. To see a
job's log while the run is going, as `gh run view --log` won't until it ends:

```sh
gh api --allow-escape-sequences repos/theosib/Keystone-RTS/actions/jobs/<job id>/logs
```

Packaging scripts are in `tools/package/`, and Windows' in `tools/windows/`. Fix on `main`, push,
then `gh workflow run Release --repo theosib/Keystone-RTS --ref main`. A push by itself runs nothing.

## What to do next

1. Get a run where every job but `release` is green.
2. `gh run download <run> --repo theosib/Keystone-RTS` gets the packages.
3. Test them, as below. A package has to be tried on a machine that didn't build it.
4. `git tag v0.1.0 && git push origin v0.1.0` builds it all again and leaves a **draft** release.
5. Check that the draft has all the files and that its notes read well. The notes are
   `tools/package/release-notes.md`, plus the commits it was built from.
6. Ask the user before publishing: `gh release edit v0.1.0 --draft=false`.

## What to test

`tests/installed_check.py` plays a game from an installed copy, quits, reopens, and checks where the
files went. Point `HOME`, or `APPDATA` on Windows, at an empty temporary directory first. It uses MCP,
so the machine needs an unlocked desktop. CLAUDE.md's "Testing" and "Live inspection" sections have the
rest.

**Windows** is the priority, since the changes below went in without ever running there:

- **Saves moved.** An installed copy now keeps saves, screenshots and `imgui.ini` in
  `%APPDATA%\.keystone`, not in `game\saves` beside the program. The user asked for that on every
  platform, as Minecraft does. Check that a save survives an update, and that nothing writes beside
  the program. `keystone.iss` still spares `game\saves` when it clears old content, which is harmless
  but can go.
- **Finding the game.** `finevox_run.cpp`'s Windows-only search was replaced by
  `ResourceLocator::findEngineRoot` and `executableDir` in the engine. Its Windows branch has been
  compiled by CI and has never run.
- **GUI shaders.** They used to load from finegui's build directory, a path compiled in, which only
  works where the game was built. They now go in `shaders\` with the others. Run a package with the
  build tree hidden, or on another machine.
- **F11.** `Window::toggleFullscreen` has never run on Windows. It should give no title bar at the
  desktop's resolution, and on alt-tab should minimize, since GLFW keeps a full-screen window
  `HWND_TOPMOST`. `tests/mcp_check.py` presses it for real.
- `tests/run_ui.sh`, `tests/run_mcp.sh` and `tests/run_windows.sh` haven't been run on Windows.

**Linux x86-64**, on a desktop. The user will say which machine:

- The AppImage has GLFW 3.4, built with Wayland, so on a Wayland desktop it'll run natively. All
  testing so far went through Xwayland, because Ubuntu's own GLFW is the X11 build. So this path is
  new: try the window, F11, losing focus, mouse look on foot (Tab), and a window size that isn't the
  default. If Wayland is trouble, add `-DGLFW_BUILD_WAYLAND=OFF` to the GLFW step in `release.yml`
  rather than fix it for the first release.
- `~/.keystone` should get the saves. The bundled libraries shouldn't include libc, libvulkan, GPU
  drivers or libstdc++. linux.sh prints the list, and `installed_check.py` prints what really loaded.

**Linux Arm**, on a machine with a real GPU. The user will say which one. It may have no monitor
attached, in which case X runs a small empty screen, and Vulkan still works there. F11 does nothing
with no monitor, and that's deliberate. Tell the user if a test really needs a monitor, because
connecting one means moving the machine. No test of the Arm build on a real GPU has happened yet.

**macOS** can only be tested on a Mac, by the user or a session there. A DMG built locally passed
`installed_check.py` from the read-only image, with the build tree hidden, and with MoltenVK loading
from inside the app. CI's DMG hasn't been tried. Worth doing before publishing: download it in a
browser, so that it's quarantined for real, and follow the two sets of steps in the release notes.
`xattr -cr` is the command the notes offer. Check too what `LSMinimumSystemVersion` it reports:
Homebrew's libraries on CI set it.

## Not to be confused with a problem

On a slow machine (`docs/PLATFORMS.md` has the numbers):

- `FactionVisionTest.ManyUnitsStayCheap` fails on an Intel N100, at about 32ms against 30.
- `explore` in `run_all.sh` fails now and then on Linux and macOS alike.
- `the server has the rival's ground` in `run_mcp.sh` has failed 2 runs in 10 there, before anything
  the run changed. Cause unknown.

Linux has passed everything else on the merged code, in a source build. macOS has passed everything.

## Rules that apply

- Commit to `main` and push, with no PRs, in all six repos. `git fetch` first: more than one session
  has been working on this. Check `git config user.email` is the noreply address.
- **Nothing about any employer goes in these public repos:** no machine names, accounts or paths, in
  files or commit messages. Linux machines here are just "an x86-64 machine" and "an Arm machine".
- Packages are built on GitHub, not locally, so that no local path ends up in a release.
