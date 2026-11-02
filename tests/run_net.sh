#!/usr/bin/env bash
# Multiplayer over real sockets: a server and bots, each its own process. A bot is a client with
# no window, so this asserts on what a client really holds. Exit nonzero if anything fails.
set -uo pipefail
command -v python3 >/dev/null || python3() { python "$@"; }  # Windows calls it python
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
net="$here/tests/net"
work="$(mktemp -d)"
trap 'kill $(jobs -p) 2>/dev/null; rm -rf "$work"' EXIT
failed=0
port=7930

note() { echo "$1  $2"; [ "$1" = PASS ] || failed=1; }

next_port() { port=$((port + 2)); }

script() {  # Name, then any extra lines to put in front
    local out="$work/$1.$RANDOM.fs"
    { cat "$net/common.fs"; printf '%s\n' "${@:2}"; cat "$net/$1.fs"; } > "$out"
    echo "$out"
}

serve() {  # Any extra arguments go to the server
    "$build/finevox_headless" --game-root "${SERVER_ROOT:-$here}" --serve --port "$port" "$@" \
        >"$work/server.log" 2>&1 &
    server=$!
    for _ in $(seq 100); do
        grep -q 'open on port' "$work/server.log" 2>/dev/null && return 0
        sleep 0.05
    done
    note FAIL "server didn't start"; tail -5 "$work/server.log"; return 1
}

stop_server() { kill "$server" 2>/dev/null; wait "$server" 2>/dev/null; }

bot() {  # Log name, script, then finevox_bot arguments
    local name="$1" file="$2"; shift 2
    "$build/finevox_bot" --game keystone --game-root "$here" --name "$name" --join "127.0.0.1:$port" \
        --script "$file" --cache "$work/cache" "$@" >"$work/$name.log" 2>&1
}

checks_from() {  # Print a log's PASS and FAIL lines
    grep -E '^(PASS|FAIL) ' "$1"
    if grep -q '^FAIL ' "$1"; then failed=1; fi
}

echo "---- a whole game: lobby, chat, settings, launch, fog, ownership"
next_port
serve
bot Alice "$(script alice)" --timeout 60 &
alice=$!
sleep 0.3
bot Bob "$(script bob)" --timeout 60 --load-delay 2.0 &
bob=$!
wait $alice; alice_code=$?
wait $bob; bob_code=$?
checks_from "$work/Alice.log"; checks_from "$work/Bob.log"
[ "$alice_code" -eq 0 ] && note PASS "alice's run" || { note FAIL "alice's run (exit $alice_code)"; tail -5 "$work/Alice.log"; }
[ "$bob_code" -eq 0 ] && note PASS "bob's run" || { note FAIL "bob's run (exit $bob_code)"; tail -5 "$work/Bob.log"; }

t_alice=$(grep '^RUNNING' "$work/Alice.log" | awk '{print $2}')
t_bob=$(grep '^RUNNING' "$work/Bob.log" | awk '{print $2}')
if [ -n "$t_alice" ] && [ -n "$t_bob" ]; then
    gap=$(( t_alice > t_bob ? t_alice - t_bob : t_bob - t_alice ))
    echo "started $gap ms apart"
    [ "$gap" -lt 200 ] && note PASS "everyone starts at once, waiting for the slowest" || note FAIL "started $gap ms apart"
else
    note FAIL "someone never started"
fi
if grep -qE 'script error|\[game script\]' "$work/server.log"; then
    note FAIL "the server's scripts had errors"; grep -E '\[game script\]' "$work/server.log" | head -5
else
    note PASS "no script errors on the server"
fi
stop_server

echo "---- who gets turned away"
next_port
cat > "$work/locked.conf" <<CONF
title: Locked
password: swordfish
max_players: 1
CONF
serve --config "$work/locked.conf"
reject="$(script reject)"
bot Wrong "$reject" --password nope --timeout 10
grep -q 'FAILED_WITH wrong password' "$work/Wrong.log" && note PASS "a wrong password" || { note FAIL "a wrong password"; tail -3 "$work/Wrong.log"; }
"$build/finevox_bot" --game otherworld --name Lost --join "127.0.0.1:$port" --script "$reject" --timeout 10 >"$work/Lost.log" 2>&1
grep -q 'FAILED_WITH this server is running Keystone' "$work/Lost.log" && note PASS "a different game" || { note FAIL "a different game"; tail -3 "$work/Lost.log"; }

idle="$(script watcher 'set MIN_PLAYERS 99' 'set RUN_MS 1')"
bot First "$idle" --password swordfish --timeout 3 &
first=$!
sleep 0.7
bot Second "$reject" --password swordfish --timeout 10
grep -q 'FAILED_WITH this game is full' "$work/Second.log" && note PASS "a full game" || { note FAIL "a full game"; tail -3 "$work/Second.log"; }
wait $first 2>/dev/null
stop_server

echo "---- a game that's started is closed"
next_port
serve
quick="$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 2500')"
bot Solo "$quick" --timeout 30 &
solo=$!
for _ in $(seq 100); do grep -q RUNNING "$work/Solo.log" 2>/dev/null && break; sleep 0.1; done
bot Late "$reject" --timeout 10
grep -q 'FAILED_WITH this game has started' "$work/Late.log" && note PASS "a stranger can't walk in" || { note FAIL "a stranger can't walk in"; tail -3 "$work/Late.log"; }
wait $solo
stop_server

echo "---- finding a server on the LAN, and joining it"
next_port
serve
lan="$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 500')"
"$build/finevox_bot" --game keystone --game-root "$here" --name Finder --join lan --script "$lan" \
    --timeout 30 --cache "$work/cache" >"$work/Finder.log" 2>&1
found=$?
grep '\[bot\] found' "$work/Finder.log" | head -1
[ "$found" -eq 0 ] && note PASS "found it and played" || { note FAIL "found it and played (exit $found)"; tail -4 "$work/Finder.log"; }
stop_server

echo "---- a dropped connection comes back to the same seat"
next_port
serve
proxy=$((port + 1))
python3 "$net/proxy.py" "$proxy" "$port" --cut-on "$work/cut" >"$work/proxy.log" 2>&1 &
proxy_pid=$!
sleep 0.3
resilient="$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 6000')"
"$build/finevox_bot" --game keystone --game-root "$here" --name Dropped --join "127.0.0.1:$proxy" \
    --script "$resilient" --timeout 60 --cache "$work/cache" >"$work/Dropped.log" 2>&1 &
dropped=$!
for _ in $(seq 400); do grep -q RUNNING "$work/Dropped.log" 2>/dev/null && break; sleep 0.05; done
sleep 1
touch "$work/cut"
wait $dropped
code=$?
kill $proxy_pid 2>/dev/null
grep -q 'cut a connection' "$work/proxy.log" && note PASS "the connection really was cut" || note FAIL "the connection was never cut"
grep -q 'phase :reconnecting' "$work/Dropped.log" && note PASS "the client noticed" || note FAIL "the client didn't notice"
if [ "$code" -eq 0 ] && grep -q 'END faction 1 ' "$work/Dropped.log"; then
    note PASS "and got its faction back"
else
    note FAIL "and got its faction back (exit $code)"; tail -6 "$work/Dropped.log"
fi
grep 'END' "$work/Dropped.log"
stop_server

echo "---- content a client doesn't have"
next_port
cp -R "$here" "$work/game"
rm -rf "$work/game/saves" "$work/game/screenshots"
echo "# The server's a newer version" >> "$work/game/ui/main.fs"
SERVER_ROOT="$work/game" serve
fetch="$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 500')"
bot Older "$fetch" --timeout 30
code=$?
root="$(grep -o 'root .*' "$work/Older.log" | head -1 | cut -d' ' -f2-)"
[ "$code" -eq 0 ] && note PASS "a client with an older game joins" || { note FAIL "a client with an older game joins (exit $code)"; tail -4 "$work/Older.log"; }
if [ -n "$root" ] && [ "$root" != "$here" ] && grep -q "newer version" "$root/ui/main.fs"; then
    note PASS "and runs the server's files"
else
    note FAIL "and runs the server's files ($root)"
fi
stop_server

echo "---- a client with no game at all"
next_port
serve
"$build/finevox_bot" --game keystone --name Generic --join "127.0.0.1:$port" \
    --script "$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 500')" --timeout 30 \
    --cache "$work/generic" >"$work/Generic.log" 2>&1
code=$?
root="$(grep -o 'root .*' "$work/Generic.log" | head -1 | cut -d' ' -f2-)"
[ "$code" -eq 0 ] && note PASS "it joins" || { note FAIL "it joins (exit $code)"; tail -4 "$work/Generic.log"; }
if [ -n "$root" ] && [ -f "$root/game.conf" ] && [ -f "$root/ui/main.fs" ] && [ ! -e "$root/scripts" ]; then
    note PASS "with the client's files and none of the game's rules"
else
    note FAIL "with the client's files and none of the game's rules ($root)"
fi
stop_server

echo "---- a slow link"
next_port
serve
proxy=$((port + 1))
python3 "$net/proxy.py" "$proxy" "$port" --delay 120 --kbps 400 >"$work/proxy.log" 2>&1 &
proxy_pid=$!
sleep 0.3
slow="$(script watcher 'set MIN_PLAYERS 1' 'set RUN_MS 8000')"
"$build/finevox_bot" --game keystone --game-root "$here" --name Slow --join "127.0.0.1:$proxy" \
    --script "$slow" --timeout 90 --cache "$work/cache" >"$work/Slow.log" 2>&1
code=$?
kill $proxy_pid 2>/dev/null
[ "$code" -eq 0 ] && note PASS "plays at 240ms and 400kbit/s" || { note FAIL "plays at 240ms and 400kbit/s (exit $code)"; tail -4 "$work/Slow.log"; }
grep 'END' "$work/Slow.log"
stop_server

[ "$failed" -eq 0 ] && echo "ALL PASSED" || echo "SOME FAILED"
exit "$failed"
