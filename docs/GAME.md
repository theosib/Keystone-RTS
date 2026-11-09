# The Game

Keystone is a real-time strategy game set in a voxel world where everything is real. Other strategy
games keep your stockpile as a number and your buildings as units on a map. Here, your timber is
stacks of timber in a yard, your town is built block by block by workers you can watch, and the fog
of war is what your people can actually see from where they're standing.

You raise a settlement around its Core Hall, which holds the **Keystone**. Keep yours and break
theirs. Then, however it turns out, the world stays yours to go on playing in.

This page describes the game as it stands, and says what's still to come at the end.
[STATUS.md](STATUS.md) is the exact list of what's working, and [DESIGN.md](DESIGN.md) is the whole
plan.

## How a game goes

You start with a Core Hall on open land, a few **Hands**, and yourself, the **Steward**. Hands are
your workers. Nobody tells each one what to do: you place work, and they sort out who does it.

Early on it's a race for food and timber. Hands gather what's around them, and carry it to a yard or
straight to a site that needs it. Sawpits and Forager Huts bring work closer to the resources.
Shelters give people beds, and a settlement grows only as far as it can house and feed itself. Then
you start to think about what's out there: a Pathfinder explores by itself, nearest unseen ground
first, and a Watchtower lets you see further.

Sooner or later someone comes. An idle settlement falls in about twelve minutes to the standard
rival, so by then you want Militia and Shortbows, a Log Palisade across the way in, and a look at the
ground, because height and walls matter here in a way they can't on a flat map.

To win, send soldiers into the enemy's Hall. With no defender there, they break its Keystone in 40
soldier-seconds, so ten soldiers take four seconds and two take twenty. A bar over the Hall shows
both sides how it's going.

## Your stockpile is made of blocks

Every resource you own is somewhere in the world, as a block you can see. A stack holds eight, and
each one is a small cube of its own, so you can read your economy by looking at your yard.

That turns storage into a real problem, and watching it get solved is one of the game's pleasures:

- Hands lay yards out in **rows and aisles**, so that nothing gets walled in behind anything else.
- They top up part-filled stacks first, and tidy up when there's nothing urgent to do.
- **When a yard fills, they build upwards.** They put up posts, lay a mezzanine floor, build stairs,
  and carry on storing upstairs. A busy yard grows into a warehouse nobody designed.

## Fog of war in three dimensions

Vision is true line of sight through the voxel world. A wall hides what's behind it, and a hill
blocks your view of the valley beyond. There are three states: what you've never seen, what you saw
once and remember, and what you can see now. What you remember stays as it was when you last looked,
even if it's since been built on or burned down.

The fog isn't a curtain drawn over data your computer already has. The server sends your client only
what your side can see, so there's nothing there to reveal. Even the panel that names the block
under your cursor can't tell you more than your people know.

## Rivals who play fair

The rival commanders develop, scout and attack by the same rules you do. They have no map hack: a
rival reacts only to what its own units have seen. If you hide your army behind a hill, it doesn't
know that it's there. Up to seven of them can share a world.

## Two ways to see the world

Overhead, it's a strategy game: select units, give orders, lay out buildings from the palette, drag
out walls, and mark runs of blocks for Hands to build, which is also how you lay a road.

A click picks one unit, even out of a crowd, and clicking again picks the next. Where you send units
a pin drops, and stays until they get there. Hold Shift to add a waypoint instead, and they'll go to
each in turn. Soldiers form up facing the way they're going, with spears in front and bows behind.
Your units, and any enemy one of them can see, show through roofs and trees, and a health bar shows
over anyone who's hurt. The menu's **Settings** turns any of that off.

Press Tab and you drop down into the world as the Steward, on foot and in first person. You can walk
your settlement, harvest, dig, and place blocks from your stockpile by hand. It's the same world and
the same game, and a change you make is there for everyone. Your orders don't stop while you're
down there.

You can pause, overhead, in a game of your own. Time stops, and you can still give orders.

## Fighting

Combat works off the world's real shape:

- **Arrows fly.** They're simulated in flight, and real cover stops them.
- **High ground matters.** Shooting from above gives you the advantage.
- **Soldiers break.** Morale gives way, and a beaten force routs instead of dying to the last.
- **Soldiers think a little for themselves.** They guard where you sent them, answer whoever hits
  them, and go to an alarm in proportion to the raid, then back again.
- **Food heals.** A hurt Hand eats early and gets better for it. A soldier can't leave its post, so
  once it's out of the fight a Hand brings it a meal, and it mends over the next twenty seconds.

## When it's over

When a match ends you've won or lost, but the world doesn't have to end with it. **Continue in
Open-Ended World** carries on in the same world with the pressure off, and **Pacify Rivals** ends the
war, so you can keep building whatever you were building.

## Worlds

Every world is generated. The lands are Mixed, Meadow, Woodland, Dense Forest, Highland and Desert,
in three sizes, and each plays differently because each is short of something different. Wood is
scarce in a desert, and room to build is scarce in a dense forest.

## Playing with others

Multiplayer is built in. **Host a Game** opens a lobby for up to eight, with settings and chat, and
everyone on your network finds it under **Join a Game** with no address to type. Each player gets
their own settlement and their own fog of war. You can also open a game you've already started: use
**Invite Players** from the menu, and a friend who joins takes over a rival, or founds a settlement
if there are none left. If someone's connection drops, their seat waits for them.

Keystone can also run as a dedicated server with no window. The game has been played between
machines on different operating systems, including a Mac client on a Linux server.

## Other things worth knowing

- **Readable at a distance.** Each kind of unit shows its team colour in a different place.
- **Languages.** All the game's text comes from language files, with plurals handled properly. There
  are English and partial French translations so far.
- **Saving.** The game saves when you quit and every five minutes.
- **Original art.** Textures, unit models, the icons and the oak-and-iron interface are all generated
  by scripts in `tools/`.

## What's still to come

Today's game is the first of four planned **Epochs**. Each is meant to add a new verb, a new thing
you can do to the world. Everything below this line is design and isn't built yet.

| Epoch | Verb | Adds |
|---|---|---|
| **Hearth** | Settle | The game as it stands |
| **Charter** | Fortify | Quarries, canals, paved roads, masonry and raised walls |
| **Bastion** | Engineer | Deep shafts, waterwheels and aqueducts, siege works and sappers |
| **Forge** | Reshape | Steel, earthworks on a grand scale, and black powder |

Also planned: a Landmark to choose between at each advance, freeform buildings defined by what's in
the room, refitting a timber town to stone in place, cavalry, walls that collapse when you take out
what holds them up, fire that spreads, and the Steward fighting and carrying a captured Keystone.
[DESIGN.md](DESIGN.md) has all of this.

## How it's made

Keystone is **content, script and configuration**, with no C++ of its own. Blocks, units, buildings,
biomes and maps are data files. The rules are [finescript](https://github.com/theosib/FineStructureScript)
in [scripts/](../scripts), and all of the interface is in [ui/](../ui). A mechanism a script needs
goes into the [engine](https://github.com/theosib/FineStructureVoxel), which knows nothing about
Keystone, so that it can serve other games.

Gameplay is tested the way it's played: [tests/](../tests) runs real scenarios on the real game with
no window, and checks not only that things happen but that nothing stalls.
