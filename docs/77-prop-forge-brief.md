# 77 · Prop Forge, stage one: brief (18 October 2026)

**Waiting on Richard: yes, edit or veto.** The design is doc 76. This brief covers its order-of-work step 3: the shared
core, the first recipes, and one Bayview street to prove them. Bayview's own rebuild gets a brief of its own after this.

## What it is, and why now
The world's props are copies stamped at coordinates (doc 76, "Why"). The two Car Forge rosters (class C: 51 cars; class
B: 84) show that one engine, with checks and contact sheets, gives a range that reads as one set. Prop Forge does the
same for the world. It is next in doc 76's order now that both rosters are in the game behind `--forge-roster`. Every
new town in doc 58's build order waits on it, starting with the Bayview rebuild.

## The player's experience
Driving into a Bayview street, the walls, fences and kerbs follow the road round its bends. They end in proper piers and
open for every drive and gate, and they rise and fall with the hill instead of floating or digging in. Nothing looks
pasted twice: the bales in a field and the oaks along a lane are each a little different. It looks like one toy set,
with the Forge cars parked in it.

## How it works (the one technical decision to approve)
- **Prop Forge makes the pieces.** It's Car Forge's core, shared: mesh kernel, subdivision, art-direction colours,
  quality checks, contact sheets, LODs, FBX export, and the build-output sync. It makes **objects** (a bale, an oak) as
  families of seeded variants, and **run kits** for walls, fences and kerbs: an end, a middle that stretches, a corner,
  a post, a rail and a gate pier. Each kit file carries what the game needs: where pieces join, how far a middle may
  stretch, and which ground rule the kit follows (doc 76's table).
- **The game lays them.** At build time it lays a run along a road curve or any polyline, with the district system's
  deferred building. It fits lengths exactly and bends or facets round curves. It cuts openings where drives cross,
  keeps posts plumb, rakes rails, and tilts and settles objects on the slope. The game already has the roads and the
  ground height, so nothing about the world has to be exported to Prop Forge.
- **Why not build the whole street offline?** Offline, every change to a road or the terrain would mean exporting the
  world and rebuilding the models. Done the game's way, runs follow whatever the road becomes.

## Scope
- **In:** the shared core split out of Car Forge, with every class C and class B car's structural golden unchanged.
  Then the first recipes:
  - a dry-stone wall run;
  - a post-and-rail fence run;
  - a kerb;
  - an oak family;
  - the hay bale (round and square).
  Then the game's run layer and ground rules behind `--prop-forge`, with editor checks, proven on one Bayview street
  beside today's.
- **Out:** buildings and plot kits (stage two), the Bayview rebuild itself (its own brief), planting drifts, and
  interiors.

## References and research (agents, sources kept here)
- Real walls and fences: dry-stone wall construction (courses, throughs and copes, wall ends), post-and-rail spacing,
  and kerb profiles.
- How other games do it: spline meshes and procedural walls in Unreal, Houdini's fence tools, Planet Coaster's and
  Cities: Skylines' path props, Townscaper's joins, and Forza Horizon's roadside dressing.

## Risks
- **Bending meshes along tight curves** pinches and stretches. Fences facet instead, and walls are limited to a
  minimum radius; anything tighter becomes a corner piece.
- **Draw calls and build time:** many small pieces. Each run is merged into one mesh per district chunk, and the
  start-up and race-entry budgets (doc 72) are measured.
- **Splitting the core could move a car.** The structural goldens and all 313 engine tests must hold after the split.

## How it's judged (doc 73, section 3)
- The ground rules, measured on every piece: nothing floats, nothing is buried, nothing leans that should stand plumb.
- Joins: every run ends, turns and opens properly, with no half pieces in a road.
- No two identical objects side by side in a view.
- The area audit and a street review pass on the proving street.
- The start-up and race-entry budgets hold.
- Richard drives the new street side by side with the old one.
