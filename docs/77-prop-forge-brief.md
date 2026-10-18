# 77 · Prop Forge, stage one: brief (18 October 2026)

**Approved by Richard, 18 Oct**, with one addition: Prop Forge serves the cars too (see "Car props"). An outside
architecture review (Opus, `car-forge/review/prop-forge-architecture-review.md`) then reshaped the plan. What it changed
is in "After the review" at the end. The design is doc 76. This brief covers its order-of-work step 3: the shared
library, the first kits and objects, and a street to prove them. Bayview's own rebuild gets a brief of its own after
this.

## What it is, and why now
The world's props are copies stamped at coordinates (doc 76, "Why"). The class C Car Forge roster (55 cars) shows that
one engine, with checks and contact sheets, gives a range that reads as one set. Prop Forge does the same for the
world. It is next in doc 76's order now that class C is in the game behind `--forge-roster` (class B waits for Richard's
direction). Every new town in doc 58's build order waits on it, starting with the Bayview rebuild.

## The player's experience
Driving into a Bayview street, the walls and fences follow the road round its bends. They end in proper piers and open
for every drive and gate, and they rise and fall with the hill instead of floating or digging in. Nothing looks pasted
twice: the bales in a field are each a little different. Cars slide along a wall without snagging. It looks like one toy
set, with the Forge cars parked in it, and some of them carry the same jerry can that stands on the quay.

## How it works (the decision, confirmed by the review)
- **Prop Forge makes the pieces** (Python, in car-forge `forge/world/`, layered on the shared kernel and QC). It makes:
  - **objects** (a bale, a jerry can): families of seeded variants;
  - **run kits** (walls, fences): ends, middles, nodes (posts and piers that work at any angle), gate posts and caps.
  Each comes with a versioned kit file (the contract below).
- **The game lays them** (C#). A run is a spec written as data, laid along a road or a polyline:
  - it references roads and landmarks by name;
  - its seed is its own name.
  The game solves each run once, after everything that cuts it (drives, gates, junctions, plots) is known. The
  solver's output is a **layout as data**: a list of (piece, variant, matrix, deform values) with contact samples and
  footprints. It is hashable, cacheable in `WorldCache`, testable without meshes, and drawn as merged chunks (30-50 m
  of run), never one GameObject per piece.
- **One ground.** A `Ground.Surface(x, z)` provider (roads first, then the region's height field, then raycasts) is read
  by both the solver and the audits.
- **Why not bake offline:** the roads, heights and plots would have to be exported to Python, and the export goes stale
  with every road change and every A/B flag. The game already lays runs this way (parapets, guardrails); this
  formalises it.

## The kit contract (v0, a schema with a C# reader and an editor check)
- **Every kit or object carries:** its id, version and hash; its kind (`modular`, `deform`, or a `profile` swept along
  the curve); its origin and axes; material classes and pattern space; variants seeded by index; LOD meshes with a
  triangle budget per metre (runs) or per object; a collision proxy; and a ground mode per piece (plumb, rake, follow,
  step, tilt, level) with contact samples, skirt and sink.
- **A run kit also carries:**
  - piece roles;
  - sockets whose cross-sections match;
  - a stretch range, or a set of lengths;
  - bend rules (minimum radius, turn per piece, faceting tolerance);
  - deform density;
  - rail clearance;
  - opening widths with their framing pieces;
  - wear slots.
- **How the pieces travel.** Run pieces travel as plain mesh data (positions, normals, UVs, material class) with a JSON
  sidecar, read straight into `NativeArray`s. They don't go through FBX and Blender. Objects that Unity imports as
  prefabs keep FBX, with meta GUIDs derived from the asset id.

## Car props (Richard, 18 Oct)
The cars' props are simple primitives, and many read as plain blocks (the Pronto's hot box, the Picnic's hamper, the
jerry cans). Prop Forge's objects are detailed recipes, and a car places one by name and version (`prop:jerry_can@1`)
at the same anchors.
- **Versions are pinned.** A recipe change never moves a car until its design is bumped, re-gated, re-frozen and seen.
- **Swaps are car by car.** No global swap.
- **Each object has a car budget:** at most about 1.5k triangles at LOD0, 300 at LOD1, and none at LOD2. It brings its
  own LODs, and Car Forge never subdivides it again.
- **The prop keeps its world size on a car**, so the can on a pickup matches the can on the quay. This is the
  reviewer's recommendation, and Richard can overrule it.
- **Props never count towards a car's collision box,** and a test enforces that.

## Stage one, in order (each step proves something before the next)
0. **Guards.**
   - Freeze structural goldens for all 55 roster cars and add the check to the regression.
   - Scope the game sync's keys to what each asset depends on, so a prop edit doesn't re-export every car.
   - Add a test that keeps the layers in order (the kernel never imports car or world code).
   - *Proves:* any car that moves is caught, and prop iteration is cheap.
1. **The contract and the test range.**
   - The kit schema v0 and its C# reader.
   - `Ground.Surface`.
   - `--run-range`, a fixture strip with:
     - a straight, a crest, a hollow and a 30% cross-slope;
     - 8, 15 and 40 m bends;
     - a T-junction, a drive opening and a T-join between runs;
     - a run too short to fit;
     - a run beside a shoulder.
   - *Proves:* one contract, one ground, and a fixture for every check.
2. **A rigid fence, end to end.**
   - Posts plumb, rails raked with span clearance, openings from drives.
   - Merged chunks, collision, oriented footprints.
   - `--run-debug` (an overlay) and `--run-report` (a line per run).
   - Timed on the range.
   - *Proves:* the whole pipeline, with real numbers.
3. **A dry-stone wall, deforming.**
   - Bending on rotation-free frames, any-angle piers, a skirt.
   - Follow by vertical shear, a world-space stone pattern, a merged collider.
   - A wall-slide test.
   - *Proves:* the hard geometry, with no cracks and no snags.
4. **The hay bale family.**
   - Seeded variants, tilted and settled.
   - The repetition metric.
   - *Proves:* object families.
5. **The jerry can, two users.**
   - On the quay, and on one car as `prop:jerry_can@1`.
   - Every other car identical by golden.
   - *Proves:* one library with two users, and no approved car moves.
6. **A proving street** behind `--prop-forge`.
   - The area audit and a street review.
   - The flag-off world fingerprint unchanged, and the flag-on baseline recorded here.
   - *Proves:* the doc 73 bars in a real place.
7. **Then breadth.**
   - The kerb profile handed to the road system.
   - The crate, hamper, parcel and tyre, each opted in per car.
   - The oak family, moved to the planting work.

## Risks
- **Load time.** Bayview is built at load, so its runs are on the ready-to-drive path. The solve and the mesh fill run
  in jobs, and their output is cached. The budget: at most **+60 ms warm and +200 ms cold** at ready-to-drive.
  Background districts time-slice each chunk inside the 50 ms busiest-frame limit.
- **The game's registries assume one box per piece** (occupancy, culling, the placement audit, rail trimming). Runs
  register oriented footprints per segment, carry their own `LODGroup` per chunk, and give their contact samples to the
  audits.
- **Snags along walls:** handled by merged collision and the wall-slide test.
- **Triangles:**
  - a wall at most 600 a metre at LOD0 and 150 at LOD1;
  - a fence at most 250 a metre at LOD0;
  - stone detail from the shader's world-space pattern, not from geometry.

## How it's judged (doc 73, section 3), each with a measure
- **Plumb:** within 0.5°, sunk 0.10-0.35 m.
- **Follow:** no base sample above the ground or below the skirt; the top line within 2° of the ground's slope.
- **Step:** each step level within 0.3°, every step capped.
- **Rake:** rails within 1° of the line between post feet, clearance in range every 0.25 m.
- **Tilt:** within 4° of the ground under the contact patch, sunk 2-8 cm, no sample more than 2 cm in the air.
- **Level:** within 0.5°, on a pad or with every foot within 2 cm.
- **Joins:** gaps at sockets under 2 mm, no coplanar overlaps.
- **Openings:** framed on both sides, within the kit's widths.
- **Roads:** no part of any run inside a road or junction polygon.
- **Repetition:**
  - no same-variant object within 8 m at a similar yaw;
  - no short repeating variant sequence along a run;
  - every run too short to fit reported, never dropped.
- **Standards:** the area audit and a street review pass; the budgets above hold; Richard drives the street.

## After the review (what changed, and why)
- **Taken:**
  - **The order.** A thin slice end to end before breadth, with the guards first, since none of the 55 roster cars had
    a structural golden and every prop edit would have re-exported every car.
  - **Layouts as data.** The solve happens after the cutters are known, never laid and then trimmed.
  - **One ground provider.**
  - **The full kit contract**, with mesh data for run pieces instead of FBX.
  - **Run-aware registries and merged collision.**
  - **Pinned, opt-in car props with their own budgets.**
  - **Measures for every rule.**
  - **The test range and the debug tools.**
  - **Load-time budgets set up front.**
- **Cut from stage one:**
  - the oak (its own subsystem, moved to planting);
  - the kerb (it stays with the road system, with a profile from Prop Forge);
  - moving the core into a new package (layered instead, and extracted only when a second user needs it);
  - four of the five car objects (the jerry can proves the pattern).
- **Defaults I've taken on the review's two questions** (Richard can overrule either):
  - re-dressed cars are shown as a before/after contact sheet per batch, not one at a time;
  - props keep their world size on cars.

## Stage one, as built (18 Oct)
Steps 0-5 are written up in car-forge `docs/log.md`. In short:
- the guards hold (54 of 55 roster cars identical through every step; only the Cob moved, on purpose);
- the fence and the dry-stone wall lay clean on the run range (`--run-range`), and a car sliding along the wall keeps
  100% of its speed;
- 113 bales place without repeats;
- the jerry can is one object with two users: the range's quay and the Cob's tailgate, at the same world size.

**Step 6, the proving ground: Bayview, behind `--prop-forge` (`PropForgeBayview`).**
- **What it lays:**
  - **The coast road's stone wall** is one dry-stone run (767 m) instead of 3.6 m stamped sections. It keeps the same
    gaps (`CoastalLandscape.RoadsideGap`), and a pier stands either side of each of its 7 openings.
  - **The farm's paddock fence** is one post-and-rail run from gatepost to gatepost, round four corner posts, instead
    of 6 m kit pieces.
  - **The bale stack** by the barn and three round bales beside it.
  - **Three jerry cans** on the Halt quay, at a spot clear of every footprint, collider and drawn thing.
- **How it's built:**
  - The ground is the island's drawn grid (`CoastalLandscape.Drawn`); the cans stand on the quay's own surface.
  - It's built last of the world, so it sees everything placed.
  - The flag is in `WorldCache.WorldFlags`.
- **Load:** first 97-142 ms at build, most of it drawing the bent wall (99 ms). Two changes brought it to 66 ms:
  - the drawer writes each piece straight from the kit's arrays into its chunk's buffers, where it used to make a mesh
    per piece and merge twice;
  - a bent piece works out its frame once per slice.
  The wall now draws in 35 ms. That's inside the cold budget of 200 ms, and just over the warm budget of 60 ms;
  caching the solved runs closes it.
- **World fingerprint** (`--no-static-batching`):
  - Flag off: `3f164ae90677e486`, the same as a build of the code before step 6 (set aside and rebuilt to compare).
  - **Flag on, the baseline: `ed4ad91e9d65d382`**, with the new piers (`dry_stone@2`). It was `4361850cf9a9ac29` with
    the first piers.
- **Area audit:** flag on and off identical in every defect. Bayview's three steep banks with no barrier were there
  before. With the flag on, the audit now counts Prop Forge's pieces too:
  - every laid run piece and placed object is a piece;
  - a bale is planting;
  - anything that failed its own ground checks counts as floating.
  Oriented footprints for the placement audit are still to do (see Risks).
- **Street review** (`--street-review --street-review-town bayview`, report in
  `review/street-review/prop-forge-coast-wall.md`): "the new dry-stone wall works". It sits on the ground the whole
  length, with no cracks between pieces, nothing over the road, and clean piers at the junction. No high findings. Its
  notes:
  - the piers' caps read "a bit like bins". Fixed in `dry_stone@2`: an end or corner pier carries the wall's own cope
    across its top, a gate pier has a pitched capstone and a ball, and a pier's body is coursed to the top (its own
    material class, with the cope band off);
  - the old ashlar parapet starts about 5 m past the wall's last pier at the bridge (the gap is the old wall's too);
  - a few signs stand close behind the wall, where they stood by the old one.
- **The full regression passed** with step 6 in (the `pf-stage1` run): every game test, 24/24 in the race, and both
  adventure smokes.
- **Still to do before Bayview is rebuilt with it:**
  - oriented footprints for the placement audit;
  - caching the solved runs (the warm budget);
  - then breadth (step 7).
