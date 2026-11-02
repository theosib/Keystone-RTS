#!/usr/bin/env python3
"""Multiplayer in real windows, driven over MCP: what the bots can't reach, because it's in the UI.

  windows.py BUILD_DIR GAME_ROOT WORK_DIR

Standard library only, so there's no venv to set up.
"""
import json
import os
import shutil
import subprocess
import sys
import time
import urllib.request

build, root, work = sys.argv[1:4]
run = os.path.join(build, "finevox_run")
bot = os.path.join(build, "finevox_bot")
failures = []


def check(what, ok, detail=""):
    print(("PASS " if ok else "FAIL ") + what + ("" if ok else f"  {str(detail)[:300]}"), flush=True)
    if not ok:
        failures.append(what)


def call(port, tool, **args):
    body = json.dumps({"jsonrpc": "2.0", "id": 1, "method": "tools/call",
                       "params": {"name": tool, "arguments": args}}).encode()
    request = urllib.request.Request(f"http://127.0.0.1:{port}/mcp", data=body,
                                     headers={"content-type": "application/json"})
    result = json.load(urllib.request.urlopen(request, timeout=15)).get("result", {})
    text = result.get("content", [{}])[0].get("text", "")
    try:
        value = json.loads(text)
    except ValueError:
        return text
    return value.get("value", value) if isinstance(value, dict) and "truncated" in value else value


def game(port, name, *extra, cwd=None):
    log = open(os.path.join(work, f"{name}.log"), "w")
    args = [run, "--game-root", cwd or root, "--size", "1000", "640", "--mcp-port", str(port), "--name", name, *extra]
    process = subprocess.Popen(args, stdout=log, stderr=subprocess.STDOUT, cwd=cwd or root)
    wait_for(lambda: reachable(port), f"{name} to start")
    return process


def reachable(port):
    try:
        return "fps" in call(port, "client_state")
    except OSError:
        return False


def wait_for(predicate, what, seconds=60):
    deadline = time.time() + seconds
    while time.time() < deadline:
        try:
            if predicate():
                return True
        except OSError:
            pass
        time.sleep(0.1)
    check(f"waiting for {what}", False)
    return False


def in_game(port):
    return call(port, "client_state").get("phase") == "running"


def stop(process):
    process.terminate()
    try:
        process.wait(timeout=10)
    except subprocess.TimeoutExpired:
        process.kill()


def errors_in(name):
    with open(os.path.join(work, f"{name}.log")) as log:
        return [line for line in log if "[ui script]" in line or "[game script]" in line]


print("---- typing in a real text field", flush=True)
HOST, GUEST = 7893, 7894
host = game(HOST, "Host", "--no-save")
guest = game(GUEST, "Guest", "--no-save")

call(HOST, "client_eval", code='set host_port 7935')
call(HOST, "client_eval", code='start_hosting true')
wait_for(lambda: call(HOST, "client_eval", code="screen") == ":lobby", "the host's lobby")

call(GUEST, "client_eval", code='show_join')
wait_for(lambda: len(call(GUEST, "client_eval", code="servers")) > 0, "the guest to find the game")
found = call(GUEST, "client_eval", code="servers")[0]
check("the listing says what it is", found.get("players") == 1 and found.get("joinable") is True, found)
call(GUEST, "client_eval", code="join_game {join_addresses servers[0]}")
wait_for(lambda: call(GUEST, "client_eval", code="screen") == ":lobby", "the guest's lobby")
wait_for(lambda: "Guest joined" in call(HOST, "client_eval", code="chat_tail 5"), "the host to hear of it")

call(HOST, "client_eval", code="lobby_launch")
wait_for(lambda: in_game(HOST) and in_game(GUEST), "the game to start", 30)

# The view goes to the player's home with the first news from the game, and glides there
wait_for(lambda: call(GUEST, "client_eval", code="state") is not None, "the guest's first state")
call(GUEST, "wait", frames=90)
before = call(GUEST, "client_state")["pivot"]
call(GUEST, "client_eval", code='do_action "chat"')
call(GUEST, "wait", frames=3)
for name in "wasd":
    call(GUEST, "client_eval", code=f'input_key "{name}" true')
call(GUEST, "client_eval", code='input_text "wasd is words"')
call(GUEST, "wait", frames=20)
for name in "wasd":
    call(GUEST, "client_eval", code=f'input_key "{name}" false')
after = call(GUEST, "client_state")["pivot"]
# Across the ground, that is: the pivot keeps settling on it
drift = max(abs(before[0] - after[0]), abs(before[2] - after[2]))
check("what you type doesn't pan the camera", drift < 0.01, (before, after))

call(GUEST, "client_eval", code='input_key "enter" true')
call(GUEST, "client_eval", code='input_key "enter" false')
call(GUEST, "wait", frames=3)
wait_for(lambda: "Guest: wasd is words" in call(HOST, "client_eval", code="chat_tail 5"), "the line to reach the host")
check("enter sends it and closes the field", call(GUEST, "client_eval", code="chat_typing") is None,
      call(GUEST, "client_eval", code="chat_typing"))

call(GUEST, "client_eval", code='input_key "w" true')
call(GUEST, "wait", frames=20)
call(GUEST, "client_eval", code='input_key "w" false')
moved = call(GUEST, "client_state")["pivot"]
check("and then the keys are the game's again", moved != after, moved)

stop(guest)
wait_for(lambda: "Guest left" in call(HOST, "client_eval", code="chat_tail 5"), "the host to hear they've gone")
stop(host)
check("no script errors", not errors_in("Host") and not errors_in("Guest"), errors_in("Host") + errors_in("Guest"))

print("---- opening a single-player game to the network, live", flush=True)
SOLO = 7895
solo = game(SOLO, "Solo", "--new", "--no-save", "--seed", "7")
wait_for(lambda: in_game(SOLO), "the game")
call(SOLO, "wait", ticks=30)

tick_before = call(SOLO, "server_state")["tick"]
check("it starts out private", call(SOLO, "client_eval", code="{net_port}") is None)
check("and opens", call(SOLO, "client_eval", code="{net_open 7936}") is None)
check("on the port asked for", call(SOLO, "client_eval", code="{net_port}") == 7936)
call(SOLO, "wait", ticks=15)
check("the game never stopped", call(SOLO, "server_state")["tick"] > tick_before)

script = os.path.join(work, "joiner.fs")
with open(script, "w") as out:
    for part in ("common.fs",):
        out.write(open(os.path.join(root, "tests/net", part)).read())
    out.write("set MIN_PLAYERS 1\nset RUN_MS 3000\n")
    out.write(open(os.path.join(root, "tests/net/watcher.fs")).read())
call(SOLO, "client_eval", code="send_command {=name :pause =paused true}")
wait_for(lambda: call(SOLO, "server_eval", code="{game_paused}") is True, "a paused game")
with open(os.path.join(work, "joiner.log"), "w") as log:
    joiner = subprocess.Popen([bot, "--game", "keystone", "--game-root", root, "--name", "Friend", "--join",
                               "127.0.0.1:7936", "--script", script, "--timeout", "30",
                               "--cache", os.path.join(work, "cache")], stdout=log, stderr=subprocess.STDOUT)
code = joiner.wait()
joined = open(os.path.join(work, "joiner.log")).read()
check("a friend can walk in", code == 0, joined[-400:])
check("taking over the rival", "END faction 2" in joined, joined[-400:])
check("the host's game carried on", in_game(SOLO))
check("a pause doesn't hold a friend up", call(SOLO, "server_eval", code="{game_paused}") is False)
call(SOLO, "client_eval", code="send_command {=name :pause =paused true}")
call(SOLO, "wait", ticks=10)
check("nor can anyone pause with a friend in the game", call(SOLO, "server_eval", code="{game_paused}") is False)
sides = call(SOLO, "server_eval", code="factions[2].commander")
check("and the rival's commander is gone", sides is None, sides)
stop(solo)

print("---- saving, and continuing where you left off", flush=True)
copy = os.path.join(work, "savegame")
shutil.copytree(root, copy, ignore=shutil.ignore_patterns("saves", "screenshots", ".git", "*.png.tmp"))
CONT = 7897
first = game(CONT, "FirstPlay", "--new", "--seed", "7", cwd=copy)
wait_for(lambda: in_game(CONT), "the game")
call(CONT, "client_eval", code="cam_focus -60 20")
call(CONT, "wait", frames=60)
spot = call(CONT, "client_state")["pivot"]
call(CONT, "client_eval", code="quit_game")
try:
    first.wait(timeout=15)
except subprocess.TimeoutExpired:
    check("quitting finishes", False)
    first.kill()
check("it was saved", os.path.isdir(os.path.join(copy, "saves", "autosave")))

second = game(CONT, "SecondPlay", cwd=copy)
check("the menu offers it", call(CONT, "client_eval", code="{has_save}") is True)
call(CONT, "client_eval", code="game_start")
wait_for(lambda: in_game(CONT), "the saved game")
call(CONT, "wait", frames=30)
back = call(CONT, "client_state")["pivot"]
check("the camera is where it was", abs(back[0] - spot[0]) < 1 and abs(back[2] - spot[2]) < 1, (spot, back))
units = call(CONT, "client_entities", faction=1)
check("your people are still there", units.get("matched", 0) >= 8, units)
check("and you're still you", call(CONT, "client_eval", code="my_faction") == 1)
stop(second)

sys.exit(1 if failures else 0)
