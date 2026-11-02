"""Check a packaged game the way a player gets it: play, screenshot, quit, reopen.

    installed_check.py EXECUTABLE USER_ROOT [extra arguments]

EXECUTABLE is the installed Keystone, USER_ROOT the folder it should keep the player's files in.
Run it with HOME (or APPDATA) pointing at an empty temporary directory, so as to leave the real
folder alone. A test on the machine that built the package can pass by accident, so hide the build
tree first, or run this somewhere else. Exits nonzero if anything fails. Needs an unlocked desktop.
"""
import json
import os
import subprocess
import sys
import tempfile
import time
import urllib.request

exe, user_root, *extra = sys.argv[1:]
PORT = 7894
LOG = os.path.join(tempfile.gettempdir(), "installed_check.log")
failures = 0


def call(tool, **args):
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "tools/call",
                       "params": {"name": tool, "arguments": args}}).encode()
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}/mcp", body,
                                 {"Content-Type": "application/json"})
    reply = json.loads(urllib.request.urlopen(req, timeout=20).read())
    content = reply["result"]["content"][0]
    return json.loads(content["text"]) if content.get("type") == "text" else content


def launch(*args):
    log = open(LOG, "a")
    return subprocess.Popen([exe, "--mcp-port", str(PORT), "--size", "1280", "720", *extra, *args],
                            cwd=os.path.abspath(os.sep), stdout=log, stderr=log)


def wait_for(ok, seconds=90):
    end = time.time() + seconds
    while time.time() < end:
        try:
            state = call("client_state")
            if ok(state):
                return state
        except Exception:
            pass
        time.sleep(1)
    return None


def report(label, ok, detail=""):
    global failures
    print(("PASS " if ok else "FAIL ") + label, detail)
    failures += 0 if ok else 1


open(LOG, "w").close()
game = launch("--new")
state = wait_for(lambda s: s.get("in_game"))
report("a new game opens from the install", bool(state), state and f"fps {state.get('fps')}")
if state:
    if sys.platform == "darwin":
        out = subprocess.run(["vmmap", str(game.pid)], capture_output=True, text=True).stdout
        paths = sorted({line.split()[-1] for line in out.splitlines() if ".dylib" in line and "/" in line})
        outside = [p for p in paths if p.startswith(("/opt", "/usr/local", "/Users"))
                   and "Keystone.app" not in p]
        report("nothing loaded from outside the app", not outside, outside[:5])
        report("MoltenVK came from the app", any("Keystone.app" in p and "MoltenVK" in p for p in paths))
    elif sys.platform.startswith("linux"):
        with open(f"/proc/{game.pid}/maps") as maps:
            paths = sorted({l.split()[-1] for l in maps if ".so" in l})
        print("from the image:", [os.path.basename(p) for p in paths if ".mount_" in p or "appimage" in p.lower()])
        print("drivers:", [p for p in paths if "vulkan" in p.lower() or "lvp" in p or "radeon" in p or "intel" in p])
    call("client_eval", code='request_screenshot "screenshots/keystone.png"')
    call("wait", frames=10)
    call("client_eval", code='do_action "quit"')
    try:
        game.wait(timeout=30)
    except subprocess.TimeoutExpired:
        report("quit ends it", False)
        game.kill()

print("user root:")
for base, _, files in os.walk(user_root):
    for name in sorted(files)[:3]:
        print("  ", os.path.relpath(os.path.join(base, name), user_root))
report("the save is in the user folder", os.path.isfile(f"{user_root}/saves/autosave/game.cbor"))
report("the screenshot is too", os.path.isdir(f"{user_root}/screenshots"))
report("and the UI layout", os.path.isfile(f"{user_root}/imgui.ini"))

game = launch()
state = wait_for(lambda s: "phase" in s, 40)
report("reopens on the menu", bool(state), state)
has = call("client_eval", code="has_save").get("value") if state else None
report("and knows there's a game to continue", has in (True, "true"), has)
game.terminate()
game.wait(timeout=20)
sys.exit(1 if failures else 0)
