## Install

### Windows

Run `Keystone-*-setup.exe`, or unpack the zip anywhere if you'd rather not install it. Neither needs an administrator.

Keystone isn't signed, so Windows may say it protected your PC. Click **More info**, then **Run anyway**.

### macOS

Needs Apple silicon. Open the DMG and drag Keystone to Applications.

Keystone is free and open source, and isn't notarized by Apple, so macOS blocks it the first time. Unblock it once, in either of these ways:

**In System Settings**

1. Open Keystone. macOS says it can't verify it: click Done.
2. Open System Settings > Privacy & Security, scroll down, and click **Open Anyway** next to Keystone.
3. Confirm with **Open**.

**Or in Terminal**

```sh
xattr -cr /Applications/Keystone.app
```

That clears the "downloaded from the internet" mark, and Keystone then opens normally.

Right-clicking the app and choosing Open no longer gets around this on recent versions of macOS.

### Linux

Download the AppImage for your machine: `x86_64` for most PCs, `aarch64` for Arm. Then:

```sh
chmod +x Keystone-*.AppImage
./Keystone-*.AppImage
```

It needs a working Vulkan driver and glibc 2.35 or newer, which means Ubuntu 22.04 or anything newer. If it complains about FUSE, run it with `--appimage-extract-and-run`.

## Your files

Saves, screenshots and your window layout are kept apart from the game, so they survive an update:

| | |
|---|---|
| Windows | `%APPDATA%\.keystone` |
| macOS | `~/Library/Application Support/keystone` |
| Linux | `~/.keystone` |

## Playing over a network

Other players on your network find your game in **Join a Game**. Keystone uses TCP 7810 for the game and UDP 7811 to be found, so a firewall may need telling. On a Mac, allow it when macOS asks about your local network.

F11 goes full screen. On a Mac that's the same as the window's green button.
