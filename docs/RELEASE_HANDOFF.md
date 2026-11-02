# First Release: Handoff

In-flight work, for whoever picks it up. Delete this file once v0.1.0 is published, and move
anything worth keeping to [PLATFORMS.md](PLATFORMS.md), whose Releases section explains how the whole
thing works. Read that first.

Nothing has been released yet: there are no tags and no releases on GitHub.

## Where it stands

Run 41543868197 of the release workflow, by hand on `main`, had every job green but `release`, which
only runs for a tag. Its packages were tried away from the machines that built them:

| Package | Where | Result |
|---|---|---|
| Windows installer | Windows 11, RTX 4060 Ti | `installed_check.py` passes, about 300fps. Nothing is written beside the program. An install over the last one keeps the save |
| x86-64 AppImage | Ubuntu 22.04, Intel N100, a Wayland desktop | `installed_check.py` passes, about 60fps, natively on Wayland. F11 fills 1920x1080 and comes back, at a window size that isn't the default |
| Arm AppImage | Ubuntu 22.04, an Arm machine with a real GPU, an X desktop with no monitor | `installed_check.py` passes, about 2500fps |
| macOS DMG | | Not tried: it needs a Mac |

Both AppImages use the machine's Vulkan loader and drivers, and bundle only the engine's libraries
and GLFW. What those runs turned up, all fixed:

- **On an X desktop with no monitor the Arm AppImage hung** in `glfwCreateWindow`, spinning. GLFW
  3.4's wait for a window to be visible never times out while other events are queued: the release
  build patches it, see PLATFORMS.md's Releases section.
- **On Wayland the picture was 800x600** whatever the window's size, until it was resized. finevk
  now gives the swap chain the window's size.
- **The AppImage bundled libvulkan**, which is now left out.
- **The Windows package had `.github` and `README.md` in `game\`**, which the others leave out.

finevk's per-image render-finished semaphores, from after that run, are in no CI build yet: the next
run has them.

## What to do next

1. Run the workflow once more and check it's green: `gh workflow run Release --repo theosib/Keystone-RTS --ref main`.
2. `git tag v0.1.0 && git push origin v0.1.0` builds it all again and leaves a **draft** release.
3. Check that the draft has all the files and that its notes read well. The notes are
   `tools/package/release-notes.md`, plus the commits it was built from.
4. Ask the user before publishing: `gh release edit v0.1.0 --draft=false`.

To see a job's log while a run is going, as `gh run view --log` won't until it ends:

```sh
gh api --allow-escape-sequences repos/theosib/Keystone-RTS/actions/jobs/<job id>/logs
```

## What's still untested

- **macOS:** CI's DMG, downloaded in a browser so that it's quarantined for real, following the two
  sets of steps in the release notes. Check too what `LSMinimumSystemVersion` it reports.
- **By hand, at a keyboard:** on Wayland, losing focus and mouse look on foot (Tab). On Windows,
  alt-tab from full screen, which should minimize.
- `tests/installed_check.py` plays a game from an installed copy, quits, reopens, and checks where the
  files went. Point `HOME`, or `APPDATA` on Windows, at an empty temporary directory first. An
  AppImage needs libfuse2, or `APPIMAGE_EXTRACT_AND_RUN=1` where there's none.

On Windows, from a source build, `run_windows.sh` passes. `run_mcp.sh` passes all 77 checks, F11
included, but fails its window run on a validation error every platform has: a screenshot is read
back after its image was presented. `run_ui.sh` passed 4 runs in 8, see PLATFORMS.md's Windows
section. Neither is in the packages.

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
