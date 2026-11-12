# The Rival commander: what duels measured

How the veteran came to be what it is, and what the next person tuning it should know. The code is
`scripts/commander.fsc` (the commander) and `scripts/combat.fsc` (what a soldier does with an order).
Measured on 2026-11-11 and 12, with `tools/duel.sh` and `tools/tune.py` (see CLAUDE.md).

## Where it started

A player beat the veteran when they expected to lose. The game's trace showed why:

- Its whole army, 7 of 7, went out on each strike, since the veteran kept no guard.
- Two brigands at home called the first strike back from halfway.
- The second reached the player's Hall and spent 50 seconds chasing Hands. It killed four militia
  and no Hands, and never stood at the Keystone. A soldier with nothing to fight took the nearest
  enemy in 16 blocks, Hand or not, before it looked for a Keystone. Hands fleeing home ran past it.
- One of the player's soldiers seen near the veteran's home called everyone back across the map.
  Then "home is clear" started a counter-attack from its own rally point, which pulled the army out
  of the player's town. Three of the player's militia took its empty Keystone.
- Its scout sat in the player's town for seven minutes. Hands fled from it about 40 times, though
  it can't hurt anyone, and from enemy Hands too.

## What changed in the rules (both sides)

- A soldier at a Keystone it could break, or with one in sight, goes for soldiers, and for anything
  else only within reach. It drops a Hand it's chasing for the Keystone (`at_keystone`).
- Hands run from militia and bows, or whoever hit them, not from scouts or Hands (`threat_to`).
- The trace says when a Keystone comes under attack, is held, falls, and who won (`match`).

## What changed in the commander

- Brigands call the army home only if they outnumber the soldiers at home, and keep it there till
  they're gone. Before, any brigand did. Without the second half the army flip-flopped: home, so
  it outnumbered them, so it went out, so they outnumbered the guard (`brigand_defence.fsc`).
- After it pulls back from a lost attack it doesn't counter-attack straight away. The punish
  rule used to skip the cooldown when a defence had ended in the last minute.
- The veteran's numbers: 20 Hands not 8, two sites at once, three forager huts, two spare beds,
  strikes of 7 from minute 7, and militia only.

## How it was measured

Duels of two commanders, headless, 20 minutes, every seed played both ways round, from a frozen
copy of the tree. A level is the veteran with numbers changed (`tests/strategies.fsc`). "The
commander before this work" is `old_veteran` with `OLD_COMMIT=791f63a`.

Two things make small samples mislead. Side 2's home on the default map is much harder to take: one
level won 3 of its 6 games as side 1 and drew 5 of 6 as side 2. And a seed plays differently every
run. A level went from 5-1 against the veteran in one round to 2-5 in the next, on 8 and 12 games.
The results below are 32 games each unless marked.

## Results

Against the veteran of the time (mixed army, 8 Hands), 12 games each:

| Level | W-L-D | What it is |
|---|---|---|
| swords | 9-2-1 | militia only |
| timing | 8-3-1 | 16 Hands, strikes of 7 from minute 7 |
| boom20 | 8-0-4 | 20 Hands, three huts, strikes of 10 from minute 11 |
| boom12 | 6-2-4 | 12 Hands |
| no guard | 7-4-1 | the veteran keeping none back |
| no racing | 6-4-2 | the veteran calling every strike home |
| rush | 4-6-2 | 6 Hands, strikes of 4 from minute 4 |
| boom snipe | 3-3-6 | boom, plus 3 to their Keystone while their army's out |
| snipe | 3-7-2 | the veteran, plus the same |

Then the winners together, as `best` (20 Hands, strikes of 7 from minute 7, militia only):

| Pairing | W-L-D |
|---|---|
| best v veteran | 22-4-6 |
| best, no guard or racing, v veteran | 21-5-6 |
| that v swords | 19-8-5 |
| that v timing | 15-8-9 |
| best v swords | 12-16-4 |
| best v boom20 | 16-10-6 |
| best v the commander before this work | 5-0-27 |

The commander before this work drew nearly everything: in those games it sat at home defending from
minute 2 and never went out. Against a side that never leaves, an attacker that wants an edge
waits, and most games run out of time.

The level without a guard or racing became the veteran. Against it, at the final rules:

| Veteran v | W-L-D | What it is |
|---|---|---|
| rush | 22-5-5 | 6 Hands, strikes of 4 from minute 4 |
| mixed | 20-10-2 | a quarter of the army bows |
| small | 18-14-0 | the old economy: 8 Hands, one site, strikes of 6 |
| guarded | 16-6-10 | two kept at home |
| turtle | 16-8-8 | a guard of 4, out only with half as many again |
| boom | 12-14-6 | strikes of 10 from minute 11 |
| the commander before this work | 14-0-18 | |

It beats everything tried but boom, which it trails within the noise.

`tools/tune.py` then took 8 steps from it, over strike, first strike, Hands and spare beds, with 16
games a step and `--against small`. It ended at strikes of 6 from minute 9 with 18 Hands, but that's
noise: one challenger, strike 6, played 6-6-4 and then 9-5-2 on the next step, and the veteran went
7-7 and 7-9 against `small` in two of its checks. So the veteran's numbers stayed. Near its best,
one number moves a result by less than 16 games can see. A tuner there needs 32 games or more a
step, and a change is only worth keeping once it holds on a fresh 32.

## What it says

- **The economy decides most games.** Hands compound: more Hands, more timber, more beds, a bigger
  army. Earlier duels had 12 or 16 Hands losing to the 8-Hand veteran. That no longer holds, and
  one likely reason is that Hands then fled scouts, which costs a big workforce the most. Between
  12 and 20, the number barely changed boom against boom.
- **But not if it's late.** Out at minute 7 with 7 beat out at minute 11 with 10, 5-0. A big
  economy wants an army early enough to use it. Against a small economy with an early army, like
  `swords` (8 Hands, out at minute 7), holding two back at home lost 12-16, and sending everyone
  won 19-8. The levels don't form a ladder: each beats some and loses to others.
- **Bows are poor value.** A shortbow does 3.5 damage a second to a militia's 8, has 35 health to
  60, and costs 8 timber to 4. Arrows stop at walls, and militia close the distance fast. Militia
  alone beat the mixed army 9-2. That's the game's balance, not the AI's: a fix belongs in
  `UNITS`, and the counter rule in `next_unit` comes back into play once bows are worth having.
- **Clever beats nothing.** A guard kept back, a strike that races an attack on home, and three
  sent at their Keystone while their army is out each measured neutral or worse. They're gone:
  the code is smaller for it.

## Cost

The commander thinks twice a second and costs next to nothing. What did cost was finescript itself:
every `let`, `set`, `.set` and `.push` of a map or array walked everything it could reach looking
for closure cycles, and so did every function return. That was over half the game thread. Each scope
now counts the closures made over it, and with none it skips both walks (FineStructureScript
`ddc3e3e`). A five-minute duel went from 4.2 to 0.9 ms a tick.

What the game thread spends now, in a duel at minute 8 or so: pathfinding about 31%, all scripts
24%, finding blocks that want a game tick 15% (`UpdateScheduler::generateGameTickEvents` lists every
loaded subchunk each tick, when only a few have such blocks), vision 14%, physics 10%.

## How this generalizes

A commander, here or in any game where sides build an economy and fight, makes the same handful of
decisions on a slow clock. Keystone's are in brackets.

- **Grow or spend.** Income compounds when it goes into producers [Hands, huts], and stops when it
  goes into an army or defences. Every strategy is a schedule for switching from one to the other.
  Boom, rush and turtle are the usual three corners. Here a rush lost to everything, but an early
  strike off a small economy (`swords`) still beat a slow boom, so the loop is there.
- **Caps.** Something limits how much a side can have: supply, housing, upkeep [beds]. The cap is
  usually the real constraint, and a planner that doesn't model it plans the wrong thing.
- **What it believes.** Sightings with an age, forgotten after a while [`c.seen`, `SEEN_SECONDS`].
  Under fog, every decision is made against a belief, never the true state.
- **Force against force.** Whether to fight comes down to an estimate of two groups' strength. The
  simplest is a head count [`edge`]. A better one weighs health and damage per second, and how
  well each side's units fight together (Lanchester's laws: melee adds up linearly, ranged fire
  that can focus closer to squared).
- **Races.** Two attacks under way at once is a race: my time to take theirs against their time to
  take mine, against my time to walk home. Both tries at it here (racing, the snipe) measured no
  better than calling everyone home, so the commander doesn't do either.
- **Where.** What is near, what is threatened, where the front is, and which way is safe. Every
  engine ends up wanting an influence map for these.
- **Who does what.** Assigning workers to jobs [the task board].

The levels that matter are numbers in a map: how many workers, when to strike, how big an edge,
how big a guard. Between the variants tried here, the numbers changed the outcome far more than
the code.

## What would make the engine programmable for this

The engine already has the right shape: it supplies mechanisms that know nothing of the game, and
the game's scripts supply policy and vocabulary. For strategy, that suggests:

1. **Policy as data.** A level is a map of numbers the code reads on every think, so a strategy
   can be tried without editing code (`tests/strategies.fsc`). Most of what's worth trying is a
   change of numbers, not of code.
2. **A harness that plays a game against itself.** Headless runs, seeds, traces, and games in
   parallel (`tools/duel.sh`) are the fitness function, and `tools/tune.py` climbs on it: change a
   level's numbers, play the games, keep what wins. The method knows nothing of the game. What it
   needs from the engine is fast, repeatable headless play, and what limits it is noise: the same
   seed plays differently each run, so every step costs a few dozen games.
3. **Engine natives only where a script would loop over space or many candidates.**
   Pathfinding and the task board are that already. Nothing measured here needed another: the
   commander's thinking is cheap, and what was slow was the interpreter (see Cost). The likely
   candidates, in order, once a script needs one:
   - an influence map: per-side strength over a coarse grid, with fall-off and decay, asked
     "where is the front", "how safe is here", "which way round";
   - a fight estimate: given two lists of (health, damage per second, range), who wins and with
     how much left, by Lanchester or a few steps of simulation;
   - an economy projection: given recipes (inputs, outputs, time, prerequisites, caps), how long
     to reach a target, which is the core of a build-order planner.
   None of these speaks Keystone: units, recipes and caps would come in as data.
