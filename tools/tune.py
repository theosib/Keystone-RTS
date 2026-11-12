#!/usr/bin/env python3
"""Tune a commander's level by playing it against itself: change a number or two, play a set of
duels, and keep the change if it won more than it lost. A (1+1) hill climb, with tools/duel.sh as
the judge.

  tools/tune.py BASE STEPS [--param NAME=LOW:HIGH ...] [--set NAME=VALUE ...] [--seeds N]
                [--minutes M] [--against LEVEL] [--log FILE]

BASE is a level in DIFFICULTY, or in tests/strategies.fsc. Each --param is a whole number the
tuner may change, kept between LOW and HIGH, starting from its --set value or else the middle.
With --against, a change must also not do worse than the champion against that level. Every step
is printed, and appended to --log, as JSON, so a run can be read back.

Standard library only. Games run DUEL_JOBS at a time, as tools/duel.sh does them.
"""
import argparse
import json
import os
import random
import re
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def level_script(base, levels):
    """A script that defines each of levels {name: {key: value}} as BASE with those changes."""
    lines = ['# Written by tools/tune.py', 'set tune_ready false', 'on :tick do',
             '    if tune_ready { return nil }', '    set tune_ready true',
             f'    let base {{DIFFICULTY.get :{base}}}']
    for name, changes in levels.items():
        lines.append(f'    let {name} {{map}}')
        lines.append('    for k in base.keys do')
        lines.append(f'        {name}.set k {{base.get k}}')
        lines.append('    end')
        for key, value in changes.items():
            lines.append(f'    {name}.set :{key} {value}')
        lines.append(f'    DIFFICULTY.set :{name} {name}')
    lines.append('end')
    return '\n'.join(lines) + '\n'


def duel(a, b, script, seeds, minutes):
    """Wins for a, wins for b and draws, over seeds played both ways round."""
    # The strategies come first, so a BASE from there exists when ours is made
    combined = tempfile.NamedTemporaryFile('w', suffix='.fsc', delete=False)
    with open(os.path.join(HERE, 'tests', 'strategies.fsc')) as f:
        combined.write(f.read())
    combined.write(script)
    combined.close()
    env = dict(os.environ, DUEL_SCRIPT=combined.name, DUEL_SWAP='1')
    out = subprocess.run([os.path.join(HERE, 'tools', 'duel.sh'), a, b, str(seeds), str(minutes)],
                         capture_output=True, text=True, env=env).stdout
    os.unlink(combined.name)
    m = re.search(r'^SCORE \S+ (\d+) \S+ (\d+) draws (\d+)', out, re.M)
    if not m:
        sys.exit('no score from tools/duel.sh:\n' + out[-2000:])
    return tuple(int(x) for x in m.groups())


def mutate(current, params, rng):
    changed = dict(current)
    for name in rng.sample(sorted(params), min(len(params), rng.choice([1, 1, 2]))):
        low, high = params[name]
        step = max(1, (high - low) // 6)
        value = changed.get(name, (low + high) // 2) + rng.choice([-step, step])
        changed[name] = min(high, max(low, value))
    return changed


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('base')
    ap.add_argument('steps', type=int)
    ap.add_argument('--param', action='append', default=[])
    ap.add_argument('--set', action='append', default=[])
    ap.add_argument('--seeds', type=int, default=8)
    ap.add_argument('--minutes', type=int, default=20)
    ap.add_argument('--against')
    ap.add_argument('--log')
    ap.add_argument('--random-seed', type=int, default=1)
    args = ap.parse_args()

    params = {}
    for p in args.param:
        name, span = p.split('=')
        low, high = span.split(':')
        params[name] = (int(low), int(high))
    champion = {}
    for s in args.set:
        name, value = s.split('=')
        champion[name] = int(value)
    rng = random.Random(args.random_seed)

    def record(entry):
        line = json.dumps(entry)
        print(line, flush=True)
        if args.log:
            with open(args.log, 'a') as f:
                f.write(line + '\n')

    for step in range(args.steps):
        challenger = mutate(champion, params, rng)
        if challenger == champion:
            continue
        script = level_script(args.base, {'tune_champion': champion, 'tune_challenger': challenger})
        won, lost, drawn = duel('tune_challenger', 'tune_champion', script, args.seeds, args.minutes)
        better = won > lost
        entry = {'step': step, 'challenger': challenger, 'champion': champion,
                 'score': [won, lost, drawn]}
        if better and args.against:
            mine = duel('tune_challenger', args.against, script, args.seeds, args.minutes)
            theirs = duel('tune_champion', args.against, script, args.seeds, args.minutes)
            entry['against'] = {'challenger': mine, 'champion': theirs}
            better = (mine[0] - mine[1]) >= (theirs[0] - theirs[1])
        entry['kept'] = better
        record(entry)
        if better:
            champion = challenger
    record({'final': champion})


if __name__ == '__main__':
    main()
