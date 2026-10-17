# 76 · Prop Forge: one built world (design, 17 October 2026)

Richard, 17 October: "the assets in game at the moment, whilst looking cool for a lot of them, are built as stand alone
and then placed as duplicates in coordinates and can look disjointed and very much copy and pasted. If we do this, we
could do things like wall end points, and wall mid sections, then literally draw them onto a road and allow for curves
... We can also consider how the game draws elevation ... so we can have assets added into the game on angles as opposed
to literally all assets needing to be perpendicular."

This is the design: what the world's things should be and how they should sit. The technical spec comes later, after the
Car Forge engine review (`car-forge/review/engine-review.md`), because Prop Forge is built on the same core.

## Why

The world was built as a prototype, and it shows in the joins:
- **Copies at coordinates.** A fence is a 6 m piece stamped along a straight line at one angle. The pieces don't join,
  and a fence can't follow a curve, end, turn a corner or open for a gate except by leaving a piece out.
- **Everything stands upright.** On a slope a fence panel, a bench or a hay bale stays level and digs in on one side or
  floats on the other. The settle pass only lowers a piece until it touches; it never tilts anything.
- **Stock shapes.** Some props are Unity's cylinders and spheres (the Golden Acre silo is a cylinder with a sphere on
  top), beside detailed models built in another style.
- **No shared standard.** The 105 Bayview models and the 35 scripts that build meshes in code each set their own look,
  scale and detail. Nothing checks budgets, joins or style across them, so the toy range doesn't read as one range.

Car Forge has fixed all of this for cars: designs as text, the same design always building the same model, the art
direction applied automatically, quality checks, contact sheets, detail levels and one-step install. Prop Forge brings
that to everything else.

## The idea

One Forge with two workshops on a shared core: **Car Forge** for vehicles and **Prop Forge** for the world. The world is
built from four kinds of thing, each made to fit where it goes rather than copied and dropped.

### 1 · Objects
Single things: a hay bale, a bench, a tree, a post box, a lamp post, a tractor. Each comes from a recipe that makes a
family of variants (a dozen oaks, no two the same; bales round and square, fresh and weathered), so a field of them
doesn't look copied. The variants are chosen by the place's seed, so a place looks the same every visit.

### 2 · Runs: drawn along a line
Walls, fences, hedges, kerbs, railings, crash barriers, bunting, telegraph wires, field gates. A run is drawn along a
path, such as a road's verge, a field's edge or a quay, and **built for that path**, not tiled:
- **Ends, middles and corners.** A wall starts with an end pier, runs as courses, turns a corner with a corner stone and
  finishes with an end pier. Lengths fit exactly, so there's no half-panel poking into a road.
- **Curves.** A wall round a bend bends with it: smooth for a hedge or kerb, faceted in short straight lengths for a
  fence, as real ones are.
- **Openings.** A gate, a driveway or a junction makes a proper opening, with gate posts or piers at both sides,
  found from the roads and drives that cross the line.
- **Joins.** Where two runs meet, they meet: a T in a wall, a fence tied to a wall, a hedge ending at a gatepost.
- **Drawn onto roads.** The game's roads are already curves. A run can be asked for in words like "a dry-stone wall
  along the left verge of the farm lane, from the bridge to the farm gate, opening at the farmyard" and placed from
  that.

### 3 · Plots: buildings fitted to their ground
A house, shop, barn or pit stop is fitted to its plot: its footprint, its street frontage and the slope under it.
Buildings come from kits (walls, roofs, windows, doors, chimneys, shop fronts) in a place's style, so a street of them
is a family with each one different.

### 4 · Cover: planting and ground detail
Trees, bushes, flowers, grass tufts, rocks and puddles placed as nature does: in clusters and drifts, thicker in damp
hollows and along hedges, thinner on exposed tops, never in a grid.

## Sitting on the ground

Each thing follows the rule a real builder or nature would. The game already knows its ground height everywhere, so
each piece is fitted to the ground under its own footprint.

| What | Rule on a slope |
|---|---|
| Posts, poles, lamp posts, signs | Stay plumb (vertical); sunk into the ground, never floating or cut off |
| Trees | Grow plumb, with a slight random lean; the root flare spreads into the slope |
| Walls, dry-stone | Follow the ground: the courses rise and fall with it |
| Walls, brick and garden | Step down in level lengths, each step capped |
| Fences | Posts plumb; rails rake parallel to the ground |
| Hedges, kerbs, barriers | Follow the ground, and the road's camber where they run beside one |
| Bales, rocks, logs, crates on the ground | Lie on the slope, tilted to it, settled slightly in |
| Buildings | Stand level on a platform cut into the slope: a plinth or foundation shows on the low side, a retaining wall on the high side, steps up to the door |
| Benches, tables, bus shelters | Level, on a small paved pad or with legs that reach the ground |

The test is simple: nothing floats, nothing is cut by the ground where it shouldn't be, and nothing leans that should
stand straight.

## One look

- **One toy range.** The same art direction, colours and "toy shapes, real light" as the cars, and the same scale,
  so a Forge car parked by a Forge wall looks like one toy set.
- **Places have styles.** Bayview is a seaside town: painted render, beach huts, railings. Quarry Hills is Welsh
  slate: stone walls, slate roofs. Vinelands is Portuguese: whitewash and terracotta. A place's style is a palette of
  materials and kit pieces, so its walls, buildings and props all belong together.
- **Wear.** The same idea as the class C roster's wear layer: moss on a north wall, a lichen-spotted stone, a gate
  with a sagging bar, a faded shop sign. It's comic and lived-in, not grim.

## Bayview, rebuilt from the ground up

Bayview was the game's prototype, and the whole game has moved on since. It's also the first place a player sees.
Richard, 17 October: a complete refresh, in Claude's own taste ("it's a bit bland"). Nothing is kept: layout, roads,
track and props are all new. Only the idea stays: **an island, the tutorial area** where the story will start. It sits
where doc 58 puts it, at the origin off the north coast, with Coral Key on the horizon and the mainland (Hillcrest) a
kilometre away.

**A first direction** (to be worked up in Bayview's own brief):
- **A harbour island with a strong face.** A toy version of the brightly painted harbour towns, such as Tobermory, St
  Ives and Portmeirion: a curved harbour front of houses in bold colours, each a different one, rising up the hill
  behind. It shouldn't be pastel or beige. Slate and terracotta roofs, white window frames, flower boxes.
- **Things you remember.** A lighthouse on the point, a pier with a helter-skelter, a slipway with fishing boats and
  lobster pots, a clifftop windmill, a little ferry. A landmark is visible from every road, so the player always knows
  where they are.
- **A small island that teaches.** Its roads and the new track are laid out to teach: a long straight and a hairpin by
  the harbour, sweepers over the headland, a gravel lane round the back. The first races go round the island, and the
  way off it (a causeway, a bridge, a ferry ramp) is a moment the story can use.
- **Nautical details everywhere,** made with Prop Forge: rope bollards, railings drawn along the quay, bunting strung
  between lamp posts, harbour walls following the water's edge, drystone field walls on the hill farm.

The first recipes are proven on one Bayview street before the rest follows. New towns are built with Prop Forge from
the start.

## Order of work
1. The Car Forge engine review's agreed changes, which shape the shared core.
2. The class C roster (`car-forge/docs/roster-c-design.md`).
3. Prop Forge: the shared core split out without moving any approved car, then the first recipes (a dry-stone wall
   run, a fence run, a tree family, the hay bale, a cottage kit) proven on one Bayview street.
4. Bayview rebuilt, then the next towns built with it from the start.

## How it's judged
- **The standards the game already has:** the area audit (doc 57) and a street review for every street, both passing.
- **Speed:** the start-up and race-entry budgets (doc 73) held.
- **Nothing obviously copied:** no two identical props side by side in a view, and no run that visibly repeats.
- **The ground rules:** every rule in the table above checked on every piece. Nothing floats, buries or leans wrong.
- **Joins:** every run ends, turns and opens properly, with no half panels in roads.
- **Richard's drive round,** side by side with the old Bayview.

## Out of scope
- The ground and roads themselves: the game keeps building terrain and roads, and Prop Forge fits things to them.
- Interiors you can enter.
- Destruction, such as knocking walls down.

## Decisions (Richard, 17 October)
- **Bayview gets a complete refresh, in Claude's own taste.** It's still the tutorial island, with its own style.
- **Nothing is kept,** not even the track. Bayview is rebuilt from the ground up; only the idea of a tutorial island,
  where the story will start, carries over.
