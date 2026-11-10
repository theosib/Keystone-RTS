#!/usr/bin/env bash
# Two commanders play each other on a few seeds, headless, to measure them:
#   tools/duel.sh LEVEL_A LEVEL_B [SEEDS] [MINUTES]
# Prints each game's result and the last report from each side. The first side is LEVEL_A. Games
# run four at a time. DUEL_WATCH=1 logs both Keystones every five seconds. A level named
# old_<level>, such as old_veteran, is the commander as it was at OLD_COMMIT.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
a=$1 b=$2 seeds=${3:-4} minutes=${4:-25}
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
# old_<level> plays the commander from OLD_COMMIT (by default the one before this work)
python3 "$here/tools/old_commander.py" "${OLD_COMMIT:-2074147}" > "$work/old_commander.fsc"
one() {
    "$build/finevox_headless" --game-root "$here" --seed "$1" --ticks $((minutes * 1800)) \
        --script "$work/old_commander.fsc" \
        --script "$here/tests/duel.fsc" --define "LEVEL_A=$a" --define "LEVEL_B=$b" \
        --define "DUEL_WATCH=${DUEL_WATCH:-}" > "$work/$1.log" 2>&1
}
export -f one
export build here a b minutes work
seq 1 "$seeds" | xargs -P 4 -I{} bash -c 'one {}'
for seed in $(seq 1 "$seeds"); do
    out="$(cat "$work/$seed.log")"
    result="$(grep -m1 'DUEL over' <<< "$out" | sed 's/.*DUEL //')"
    [ -z "$result" ] && result="no winner in $minutes minutes"
    echo "seed $seed: $a v $b: $result"
    grep 'DUEL minute' <<< "$out" | tail -2 | sed 's/.*DUEL /    /'
    grep -E 'error|FAIL' <<< "$out" | head -3
done
