#!/usr/bin/env python3
"""Check a game's MCP tools the way a client meets them, through the stdio bridge.

Usage: mcp_check.py BRIDGE MODE, where MODE is what's listening on FINEVOX_MCP_PORT:
none, headless, window or player. Prints PASS/FAIL lines, and exits nonzero on any failure.
"""
import json
import subprocess
import sys
import time

bridge_path, mode = sys.argv[1:3]
failures = 0


def check(label, ok, detail=""):
    global failures
    print(f"{'PASS' if ok else 'FAIL'} {label}{'' if ok else '  ' + str(detail)[:300]}", flush=True)
    failures += 0 if ok else 1


class Client:
    def __init__(self):
        self.process = subprocess.Popen([sys.executable, bridge_path], stdin=subprocess.PIPE,
                                        stdout=subprocess.PIPE, text=True)
        self.next_id = 0
        self.notifications = []

    def request(self, method, params=None):
        self.next_id += 1
        message = {"jsonrpc": "2.0", "id": self.next_id, "method": method, "params": params or {}}
        self.process.stdin.write(json.dumps(message) + "\n")
        self.process.stdin.flush()
        while True:
            reply = json.loads(self.process.stdout.readline())
            if reply.get("id") == self.next_id:
                return reply
            if "method" in reply:
                self.notifications.append(reply["method"])

    def call(self, tool, **args):
        reply = self.request("tools/call", {"name": tool, "arguments": args})
        if "error" in reply:
            return {"protocol_error": reply["error"]}
        result = reply["result"]
        if result.get("isError"):
            return {"tool_error": result["content"][0]["text"]}
        first = result["content"][0]
        if first["type"] == "image":
            return {"image": first, "note": result["content"][1]["text"]}
        try:
            return json.loads(first["text"])
        except ValueError:
            return {"text": first["text"]}

    def tools(self):
        return {tool["name"] for tool in self.request("tools/list")["result"]["tools"]}


client = Client()
hello = client.request("initialize", {"protocolVersion": "2025-06-18", "capabilities": {},
                                      "clientInfo": {"name": "check", "version": "1"}})
check("the bridge shakes hands", hello["result"]["capabilities"]["tools"]["listChanged"] is True)

if mode == "none":
    gone = client.call("status")
    check("no game is a tool error, not a dead connection", "No game" in gone.get("tool_error", ""), gone)
    sys.exit(1 if failures else 0)


def wait_for_game():
    deadline = time.time() + 30
    while time.time() < deadline:
        status = client.call("status")
        if "ticks" in status and status["ticks"]["count"] > 10:
            return status
        time.sleep(0.2)
    return {}


status = wait_for_game()
check("the game is ticking", bool(status), status)
time.sleep(2.5)  # The bridge finds out the real tools on its next probe
tools = client.tools()
print("tools:", ", ".join(sorted(tools)))
check("the bridge tells the client when a game brings new tools",
      "notifications/tools/list_changed" in client.notifications, client.notifications)

second = client.call("wait", ticks=30)
check("wait lets the game run", second.get("ticks", {}).get("count", 0) >= status["ticks"]["count"] + 30, second)
check("ticks at about the game's rate", 20 < second["ticks"]["per_second"] < 40, second)

if mode == "player":
    check("a player has no server tools", not any(t.startswith("server_") for t in tools), tools)
    check("or client_eval", "client_eval" not in tools and "logs" not in tools, tools)
    refused = client.call("server_eval", code="1")
    check("calling one is refused", "protocol_error" in refused, refused)
    seen = client.call("client_entities", limit=100)
    check("a player sees entities", seen.get("matched", 0) > 0, seen)
    rivals = client.call("client_entities", faction=2)
    check("but none of the rival's, across the map", rivals.get("matched") == 0, rivals)
    sent = client.call("client_command", name="train")
    check("a player can act", "sent" in sent.get("text", ""), sent)
    sys.exit(1 if failures else 0)

check("server tools are there", {"server_eval", "server_entities", "server_blocks"} <= tools, tools)

value = client.call("server_eval", code="(40 + 2)")
check("server_eval returns a value", value.get("value") == 42, value)
error = client.call("server_eval", code="{no_such_native 1}")
check("a script error is a tool error", "tool_error" in error, error)
state = client.call("server_eval", code="factions[1].id", max_depth=2)
check("server_eval reaches the game's globals", state.get("value") == 1, state)
typo = client.call("server_eval", cod="1")
check("a misspelt argument is refused by name", "code" in str(typo.get("protocol_error")), typo)

overview = client.call("server_state")
check("server_state names the game", overview.get("game") == "Keystone", overview)
check("there are no script errors", overview.get("script_error_count") == 0, overview)

hands = client.call("server_entities", type="hand", faction=1)
check("server_entities finds the Hands", hands.get("matched", 0) >= 8, hands)
first = hands["entities"][0]
x, y, z = first["position"]
check("with their health", first.get("max_health") == 30, first)
near = client.call("server_entities", near=[x, y, z], radius=0.5, type="hand")
check("near and radius narrow it", any(e["id"] == first["id"] for e in near["entities"]), near)

rival = client.call("server_entities", faction=2)
check("the server sees the rival", rival.get("matched", 0) > 0, rival)

ground = client.call("server_eval", code="{surface_y 0 0}")["value"]
blocks = client.call("server_blocks", min=[0, ground, 0], max=[3, ground + 1, 2])
check("server_blocks reads a region",
      len(blocks.get("layers", [])) == 2 and len(blocks["layers"][0]["rows"]) == 3
      and len(blocks["layers"][0]["rows"][0]) == 4, blocks)
column = client.call("server_blocks", min=[0, ground, 0], max=[0, ground + 1, 0])
check("with the ground solid and the air above it",
      column["layers"][0]["rows"] == ["#"] and column["layers"][1]["rows"] == ["."], column)
big = client.call("server_blocks", min=[0, 0, 0], max=[200, 200, 200])
check("a huge region is refused, not attempted", "limit" in big.get("tool_error", ""), big)

client.call("server_eval", code='print "mcp probe says hello"')
said = client.call("logs", contains="mcp probe")
check("logs catch script output", said.get("lines") == ["mcp probe says hello"], said)

probe = client.call("server_eval", code='set mcp_ticks 0\non :tick do\n    set mcp_ticks (mcp_ticks + 1)\nend\nmcp_ticks')
client.call("wait", ticks=10)
counted = client.call("server_eval", code="mcp_ticks")
check("an eval can leave a probe running", isinstance(counted.get("value"), int) and counted["value"] >= 9, (probe, counted))

# Counted by a handler of its own: the game may well act on a real command before we look
client.call("server_eval", code="set mcp_commands 0\non :command do\n    if (cmd.name == :mcp_probe) { set mcp_commands (mcp_commands + 1) }\nend")
client.call("server_command", name="mcp_probe")
# A game behind time runs several ticks in a row before it reads commands again. Late is fine,
# which is why this polls, but lost or doubled isn't.
heard = {}
for _ in range(25):
    client.call("wait", ticks=2, seconds=1)
    heard = client.call("server_eval", code="mcp_commands")
    if heard.get("value", 0) >= 1:
        break
client.call("wait", ticks=5, seconds=1)
heard = client.call("server_eval", code="mcp_commands")
check("server_command reaches the game script, once", heard.get("value") == 1, heard)

if mode == "headless":
    check("no client tools without a client", not any(t.startswith("client_") for t in tools), tools)
    done = client.call("server_eval", code="game_exit 0")
    check("game_exit ends a headless game", "value" in done, done)
    sys.exit(1 if failures else 0)

# ---- With a window

view = client.call("client_state")
check("client_state reports the view", view.get("embodied") is False and view.get("fps", 0) >= 0, view)

mine = client.call("client_entities", faction=1)
check("client_entities sees your own units", mine.get("matched", 0) >= 8, mine)
check("and marks your body", sum(e["you"] for e in mine["entities"]) == 1, mine)

real = client.call("server_entities", faction=2)
seen = client.call("client_entities", faction=2)
check("fog of war: the client sees fewer rivals than the server",
      seen.get("matched", 0) < real.get("matched", 0), (seen, real))

corner = [90, ground - 2, -2]
far_real = client.call("server_blocks", min=corner, max=[94, ground + 3, 2])
far_known = client.call("client_blocks", min=corner, max=[94, ground + 3, 2])
check("the server has the rival's ground", any(c != "." for l in far_real["layers"] for r in l["rows"] for c in r), far_real)
check("the client doesn't", all(c == "." for l in far_known["layers"] for r in l["rows"] for c in r), far_known)

answer = client.call("client_eval", code="view_mode")
check("client_eval reads the UI's globals", answer.get("value") == ":tactical", answer)
client.call("client_eval", code='do_action "toggle_view"')
client.call("wait", frames=3)
turned = client.call("client_state")
check("client_eval drives the game", turned.get("embodied") is True, turned)
client.call("client_eval", code='do_action "toggle_view"')
client.call("wait", frames=3)


def toggle_fullscreen(before):
    """F11, as a player would press it. Returns the new viewport once it's stopped changing:
    macOS animates this, and ignores another toggle until that's done."""
    client.call("client_eval", code='input_key "f11" true')
    client.call("client_eval", code='input_key "f11" false')
    last = before
    for _ in range(40):
        client.call("wait", frames=10)
        now = client.call("client_state")["viewport"]
        if now == last and now != before:
            return now
        last = now
    return last


def pixels():
    note = client.call("client_screenshot", max_size=0)["note"]
    return [int(n) for n in note.split(",")[0].split("x")]


windowed = client.call("client_state")["viewport"]
windowed_pixels = pixels()
full = toggle_fullscreen(windowed)
check("F11 fills the screen", full[0] > windowed[0] and full[1] > windowed[1], (windowed, full))
full_pixels = pixels()
check("and the picture fills it too",
      full_pixels[0] > windowed_pixels[0] and full_pixels[1] > windowed_pixels[1],
      (windowed_pixels, full_pixels))
print(f"full screen: {full} window, {full_pixels} pixels")
back = toggle_fullscreen(full)
check("F11 again puts the window back", back == windowed, (windowed, back))
check("and the picture", pixels() == windowed_pixels, windowed_pixels)

shot = client.call("client_screenshot")
check("client_screenshot returns a JPEG",
      shot.get("image", {}).get("mimeType") == "image/jpeg"
      and shot["image"]["data"].startswith("/9j/"), shot)
width, height = (int(n) for n in shot["note"].split(",")[0].split("x"))
check("shrunk by default", max(width, height) == 800, shot["note"])
bytes_sent = len(shot["image"]["data"]) * 3 // 4
print(f"screenshot: {shot['note']}")
check("small enough to keep in a conversation", bytes_sent < 200_000, bytes_sent)
crop = client.call("client_screenshot", region=[100, 100, 64, 32], max_size=0)
check("region crops", crop.get("note", "").startswith("64x32"), crop)
outside = client.call("client_screenshot", region=[100000, 0, 10, 10])
check("a region off the window is a tool error", "tool_error" in outside, outside)

status = client.call("status")
check("status has both clocks", status["frames"]["count"] > 0 and status["ticks"]["count"] > 0, status)
sys.exit(1 if failures else 0)
