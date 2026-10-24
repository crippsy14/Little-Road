# Brief: build near the start, stream the rest

Working brief (23 Oct). Approved by Richard: "go ahead with the brief and then start building it". Folded into
[design.md](design.md) §13 and deleted when the last stage lands.

## Goal

Ready to drive in **3.5 s or less** (a build's second launch) **from wherever the save starts you**, with the same world
once everything has arrived, and a load time that doesn't grow with the map (20 × 20 km to come).

Caching the expensive parts got 7.24 s → 6.52 s (da1657d). That road ends well short: the world build is still ~5.4 s of
many 0.1–0.9 s parts, and the town set-up after it ~1.3 s, and every new place adds to both.

## Where the time goes (Bayview spawn, second launch, 6.52 s)

| Part | s | Needed at a Bayview start? |
|---|---|---|
| First loading frame | ~0.2 | yes (shrinkable) |
| Island: land, planting, mill, ground cover | ~1.1 | yes |
| Mainland: valley field, tint, chunks | 0.64 | the near chunks only |
| Valley roads and water (34 roads: ribbons, shoulders and junctions, bridges, rails) | 0.84 | the near roads only |
| Railway (incl. Bayview Halt) | 0.16 | the Halt |
| **Hillcrest's town** (castle, streets, station, raceway, countryside, farm) | **0.89** | **no**: behind the headland ridge, 1 km away |
| **Mainland trees** | **0.27** | **no** (they wait on the town's reserved ground) |
| Settle: runs flush, rails, pieces, cars | 0.90 | in proportion to what's built |
| Static batching, sea | 0.25 | partly |
| Town set-up: dressing, places, wayfinding, player, valley life, camera, warm-up | 1.3 | partly |

## The design

**Everything is a district; the near ring builds at load.** A built place (its town, its decoration, later its roads and
terrain) is built behind the loading card only if the car starts within reach of it. Anything further is a background
district, using the machinery Quarry Hills (12 Oct) and the far places (23 Oct) already prove: same builder code as a
coroutine, its own settle pass, what waits for it set up on arrival, and the world fingerprint identical either way.

- **The spawn decides.** The car starts at the return spot after a race, else the save's spot (if still clear ground),
  else Bayview's forecourt. `WorldTiles.Focus` is that spot, and every deferral is decided from it, so a save left in
  Vinelands builds Vinelands at load and leaves Bayview and Hillcrest for later.
- **Near ring:** a district builds at load when the spawn is within **300 m of its ground** (not its centre: the
  Hillcrest valley's built layer spans the raceway to the farm). The far places keep their 1.5 km rule until each is
  checked against the ring.
- **Order:** nearest the spawn first, except that a district that shares ground or data with another builds after it
  (`after`): Quarry Hills, Bramblewick and Vinelands after Hillcrest's valley layer, as at load (the town clears and Quarry
  Hills adds chimneys; the farm overlaps Bramblewick's ground; parked cars and townsfolk draw from shared random sequences
  in place order). So stage 1 defers Hillcrest only when those three are deferred too: an island-side start. Re-sorting
  by where the car is now would change which parked car stands where with how you drove; left out unless a stage needs it.
- **Reach guard:** a district's frame budget goes from 3 ms to 12 ms once the car is within 600 m of its ground, and it
  finishes at once (one hitch, logged) if the car gets within 150 m before it has arrived. A hitch is better than driving
  into an empty valley. (At today's ~7 s to build Hillcrest in the background and ≥20 s of driving from Bayview to the
  valley, the guard should never fire from a normal start.)
- **Frames:** each background builder yields between its parts. Bar: no background frame over 50 ms (today's worst are
  Quarry Hills' 190 ms and Vinelands' 180 ms; the new districts must not add to that list).
- **Everything that reads a place's data waits for it:** residents, fingerposts, photo sights, the train's stations,
  chimney smoke, homes, parked cars, speed traps (`Districts.Waiting` / `WhenBuiltSteps`, as Quarry Hills does).

## Stages (each behind `--near-start` until all are in, then default on)

1. **Hillcrest's valley layer as a district:** the town (`HillcrestTown`, as a coroutine) and the mainland trees, with
   its own holder and settle pass; Hillcrest's residents and life set up on arrival; the live order and the reach guard
   in `Districts`. Expected: ~1.3 s off a Bayview start.
2. **Each place's roads with the place:** the road network is still solved at load (16 ms), but ribbons, shoulders,
   junctions, guardrails and bridges of roads on a deferred district's ground are drawn when it builds (~0.4 s).
3. **Terrain chunks by distance:** the near chunks at load, the rest built on their tiles (0.47 s now).
4. **Bayview's island as a district** when the car starts away from it (and its town set-up with it).
5. **The town set-up:** valley life, ambient traffic and the warm-up scoped to what's built (1.3 s now).
6. **Default on** after the checks below, and the play copy.

## Status

**Stage 1 built 23 Oct, behind `--near-start` (off).** From Bayview's forecourt, second launches on a quiet machine:
ready **3.20–3.33 s** (default 4.71–6.03 s the same hour; the world build 3.85 → 2.54 s, its settle 431 → 29 ms). Hillcrest
arrives 1.7–1.8 s after you can drive (the reach guard didn't fire; Bayview's start is 360 m from the valley, on the
boosted budget); everything has arrived 13.6 s after ready (12.1 s by default). On a busy machine (23 Oct afternoon) the same
build read 4.42 s against 6.04 s. Starts elsewhere are unchanged (they build Hillcrest at load): Hillcrest's square 5.0 s,
Quarry Hills 6.7 s, Bramblewick's green 6.1 s, Vinelands' Adega 7.9 s. Those are stages 3 to 5's work.

- **Same world:** the default fingerprint is unchanged (`b66582872086e34d`); with `--near-start` the same 39,935 pieces,
  renderers and colliders, no piece moved by more than settling noise (39 lines of millimetres and 0.0001 of a quaternion
  on hay bales and rocks in the far places, as between the far-district modes). Town print the same but for one parked car
  at the falls lookout, which varies run to run by default too.
- **What it took** (each found by the fingerprint diff): the dressing of everything on Hillcrest's ground waits for it
  (`PropForgeDressing.Later`; it dressed the quay's bench before the town's garden path existed); the town's tyre runs are
  numbered from where the load stood (`TyreBarriers.Offset`: Bayview's refused run took number 0 first); settling ignores
  the deep sea floor (six crop rows set into a hillside fell 51 m to it, as the sea exists before a late district settles);
  the town's non-solid primitives lose their colliders at once, not at the frame's end (a district finished in one go met
  the square's paving disc and the garden paths still solid: no bollards round the fountain).
- **Frames (split 23 Oct, night):** the town's slow parts (the castle, the lower town, the house rows, pavements and
  parapets, the raceway, the countryside, the farm and its wheat field, the hill slopes, the patchwork fields) yield once 8 ms
  of work has gone, and the runs' flush fetches its kits' arrays a few ms a frame. Hillcrest's busiest piece 109 → 44 ms;
  its frames over 50 ms 6 → 0-1. The default build is unchanged (`b66582872086e34d`), and a second fingerprint taken after
  the districts have built in the background as in play (`--fingerprint-late`) is the same with and without the flag
  (`7d8006e4486d813e` by default). Bramblewick, building next while the valley's tiles stream in, has 4-5 frames over 50 ms
  against 1 by default; the worst are still Quarry Hills' and Vinelands' own (to 160 ms).
**Stage 2 built 23 Oct (night), same flag.** From an island-side start the far places' roads (Quarry Hills', Bramblewick's,
Vinelands': 27 lines) and every junction one of them meets are drawn in the background, pass by pass in ValleyRoads'
order (ribbons and paint, guardrails, bridges, shoulders, junction surfaces and kerbs, mouths), as their own district
`far-roads`, before Hillcrest (so the town still finds them there, as at load). Its ground is every 25 m cell within 40 m of
their lines: rails, fingerposts, traps and bank barriers there wait for it. Ready from Bayview **2.88-2.91 s** (stage 1:
3.2-3.3 s; default 6.0 s that hour); the roads step at load 631 → 271 ms; the far roads take 0.5 s in the background, busiest
piece 42 ms. Fingerprints: identical to stage 1's at once and late (`b88f926fda848dbe`), the default unchanged; town print
the same (the lookout car and Vinelands' bus resident vary run to run by default too). Other starts unchanged.

**Stage 4 built 24 Oct, same flag.** From a start more than 300 m from Bayview's island (±280 x ±262 m), what stands on it is
built as the district `island`, first in the queue: the planting and the tower mill, the sea railway and Bayview Halt
(their runs flushed, rails trimmed and pieces settled as the load's settle pass did), the ground cover, then TrialWorld's
part (the circuit's surfaces and support, the Pit Stop, New Bayview's town, a frame between its parts, its runs drawn as
laid); then its standing cars seated, everything on its ground dressed, its banks barriered. Its land and collider stay at
load (the causeway's piers and the Halt quay's shoulders stand on them). Bayview's residents, Pip's lighthouse, its board
and fingerposts and its loop traffic arrive with it (BayviewArrived); scene roots it makes go under the kept world
(`NearStart.Kept`). Quiet machine, alternating, second launches:

| Start | `--near-start` | default |
|---|---|---|
| Bayview | 2.92-3.02 s | 4.69-4.84 s |
| Hillcrest's square | 4.16-4.31 s | 4.87-5.09 s |
| Quarry Hills | 6.10-6.12 s | 6.79-6.93 s |
| Bramblewick's green | 5.46-5.50 s | 6.07-6.16 s |
| Vinelands' Adega | 7.26-7.46 s | 7.91-8.08 s |

Fingerprints from a Hillcrest start the same with and without the flag, at once and late (`d172e57e109edb33`,
`b5b7c3eedb065962`; millimetres of settling aside); Bayview's unchanged. Town print the same (one parked car that varies by
default). The island arrives about 1.2 s after you can drive; its busiest piece 56 ms (a terrace's first spawn), the first
traffic car 70 ms. Found on the way: at load the tower mill is spawned before the kit's animated root exists, so it counts
as animated and isn't placed; built later it would be, so its entries are taken back out (NearStart.IslandRoutine).

**Fixed (24 Oct):** the reach guard from stage 1 ran in every build, flag or not, and Vinelands' ground (a broad circle that
reaches Bramblewick's green) made it finish Vinelands in one 3 s frame right after a Bramblewick start. It and the new
start-on check (`Districts.CompleteAt`: a refused saved spot puts the car at Bayview after the world was planned round the
spot) now watch only `--near-start`'s own districts (`guarded`), whose grounds are tight. The play copy was never affected.

- **Known:** the headland's sea rocks (part of the trees step) appear a second or two after you start; a build's first
  start from a new place audits the bank sites for that set of pending districts (17.4 s from Hillcrest's square, 10.8 s
  from Vinelands, by default too); the warm-up skips Hillcrest's views when it's still to come.

## Checks (every stage)

- Ready to drive from five spawns, second launch: Bayview forecourt, Hillcrest's square, Quarry Hills, Bramblewick's
  green, Vinelands' Largo (saves with `spot` set; `--load-timing`). Bar ≤ 3.5 s from each by stage 6; each stage must
  not make any spawn slower.
- World fingerprint, `--near-start` vs without, on the same build, cooked and with `--no-world-cache`: identical (the
  fingerprint completes every district first). Town print the same both ways.
- Busiest background frame per district (the `LITTLE_ROAD_DISTRICT built` lines).
- No exceptions; `LITTLE_ROAD_DISTRICT` shows the order and any reach-guard finish.
- Editor check (`WorldStreamingChecks`): the near-ring decision for the five spawns, and the live order.

## Known risks

- The edge audit's bank sites are cached per build *and* per pending scope; a new scope means a fresh audit, so the
  Ponte Nova's barrier flake (next-steps, known issues) can show up in on/off comparisons. Read fingerprint diffs, not
  hashes.
- A build's first launch (~37 s) is the map bake, which completes every district. Separate from this brief.
- Race entry already completes the districts in the race's area (`PersistentWorld`); warps complete everything.
