#!/usr/bin/env bash
# Two commanders play each other on a few seeds, headless, to measure them:
#   tools/duel.sh LEVEL_A LEVEL_B [SEEDS] [MINUTES]
# Prints each game's result and the last report from each side, then the score. The first side is
# LEVEL_A. Games run DUEL_JOBS at a time (4). DUEL_WATCH=1 logs both Keystones every five seconds.
# DUEL_SCRIPT=FILE is loaded too, for levels that aren't in the tree: see tests/strategies.fsc.
# DUEL_KEEP=DIR keeps each game's log and trace there. DUEL_SWAP=1 plays every seed both ways round.
# A level named old_<level>, such as old_veteran, is the commander as it was at OLD_COMMIT.
set -uo pipefail
here="$(cd "$(dirname "$0")/.." && pwd)"
build="${KEYSTONE_BUILD:-$here/../fv-keystone}"
a=$1 b=$2 seeds=${3:-4} minutes=${4:-25}
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
# old_<level> plays the commander from OLD_COMMIT (by default the one before this work)
python3 "$here/tools/old_commander.py" "${OLD_COMMIT:-2074147}" > "$work/old_commander.fsc"
# A game is "seed side": side 1 plays LEVEL_A, or LEVEL_B when swapped
one() {
    local seed=${1% *} swap=${1#* } first=$a second=$b extra=()
    [ -n "${DUEL_SCRIPT:-}" ] && extra=(--script "$DUEL_SCRIPT")
    [ "$swap" = 1 ] && first=$b second=$a
    "$build/finevox_headless" --game-root "$here" --seed "$seed" --ticks $((minutes * 1800)) \
        --trace "$work/$seed-$swap.trace" --script "$work/old_commander.fsc" ${extra[@]+"${extra[@]}"} \
        --script "$here/tests/duel.fsc" --define "LEVEL_A=$first" --define "LEVEL_B=$second" \
        --define "DUEL_WATCH=${DUEL_WATCH:-}" > "$work/$seed-$swap.log" 2>&1
}
export -f one
export build here a b minutes work
games() {
    for seed in $(seq 1 "$seeds"); do
        echo "$seed 0"
        [ -n "${DUEL_SWAP:-}" ] && echo "$seed 1"
    done
}
games | xargs -P "${DUEL_JOBS:-4}" -I{} bash -c "one '{}'"
wins_a=0 wins_b=0 draws=0
while read -r seed swap; do
    out="$(cat "$work/$seed-$swap.log")"
    first=$a second=$b
    [ "$swap" = 1 ] && first=$b second=$a
    result="$(grep -m1 'DUEL over' <<< "$out" | sed 's/.*DUEL //')"
    winner="$(sed -n 's/.*winner \([0-9]*\).*/\1/p' <<< "$result")"
    if [ -z "$result" ]; then
        result="no winner in $minutes minutes"
        draws=$((draws + 1))
    elif { [ "$winner" = 1 ] && [ "$swap" = 0 ]; } || { [ "$winner" = 2 ] && [ "$swap" = 1 ]; }; then
        wins_a=$((wins_a + 1))
    else
        wins_b=$((wins_b + 1))
    fi
    echo "seed $seed: $first v $second: $result"
    grep 'DUEL minute' <<< "$out" | tail -2 | sed 's/.*DUEL /    /'
    grep -E 'error|FAIL' <<< "$out" | head -3
done < <(games)
echo "SCORE $a $wins_a $b $wins_b draws $draws"
if [ -n "${DUEL_KEEP:-}" ]; then
    mkdir -p "$DUEL_KEEP"
    cp "$work"/*.log "$work"/*.trace "$DUEL_KEEP"/ 2>/dev/null
fi
