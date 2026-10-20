# 80. The three towns remodelled: the programme (19 October 2026)

Richard, 19 Oct: "work on this continually with an agent helping you through, until we've got to the point of the whole
three towns being redone with new assets, no clipping etc, based off of the prop engine. If you get that done, generate
a plan for the whole map, with an ordered list of towns, and start working through that."

## How each round runs
1. **Plan:** a short list of work, written here before the build starts.
2. **Build:** behind the existing flags.
   - `--new-bayview` implies `--prop-forge` from round 1.
   - The old world keeps its fingerprint (`3f164ae90677e486`). Since 20 Oct that is `--old-bayview --no-prop-forge`:
     Prop Forge and the new Bayview are the game's default.
3. **Check:**
   - the area audit (docs/57) for the towns touched;
   - close-up photos;
   - the adventure smoke test with the flags;
   - races on seeds 1709-1711 when a track changes;
   - the fingerprint;
   - the full regression at each commit.
4. **Review:** a Sonnet `street-reviewer-high` agent reads a fresh `--street-review` photo set of the towns touched.
   - It reports only.
   - I triage its findings. Those I agree with join the next round's list; the others are recorded with the reason.
5. **Commit** and push; record the round here; plan the next.

**Done means:**
- each town passes the area audit with zero defects;
- the street review finds no high-severity defects;
- every repeated building and prop comes from a Prop Forge kit;
- nothing clips or floats.

## Inventory (19 Oct, Explore agent)
**Already Prop Forge (when the flag is on):**
- houses a-d and cottages a-c;
- every tree, rock, lamp, bench and fence;
- street furniture and home life;
- the farm kit;
- boats and the promenade rail;
- runs: dry-stone, hedge, post-and-rail and armco;
- sheep;
- the new Bayview's landmarks.

**Not yet Prop Forge, most visible first:**
- **Both towns:**
  - `terrace_a`-`terrace_g`: 62% of Hillcrest's frontage and 80% of Quarry Hills';
  - the civic buildings: town_hall, church, cafe, shop, post_office, hotel, mill, barn, station;
  - fountain, market_stall, town_gate.
- **Hillcrest:**
  - the castle set: gatehouse, towers, stretched curtain walls, donjon and palace;
  - box parapets, the square balustrade, the lookout rails and the dry-stone terrace walls;
  - the station forecourt and the farm silo;
  - the causeway balustrade and bridges.
- **Quarry Hills:**
  - the crawiau slate fences (thousands of thin boxes) and the back-yard walls;
  - phone and post boxes built from primitives (the street kit has both);
  - rock_stand a-d and the spoil heaps;
  - the quay track, wagons and crane, and the Blondin towers;
  - telegraph poles, tyre walls and ad boards.

## Rounds
| Round | Work |
|---|---|
| 1 | **New Bayview passes its audit.** <br>• dry-stone walls on steep roadsides <br>• the causeway link's 0.4 m step <br>• lamps, furniture and life up to the standards <br>• house variety <br>• the sunk spawn car <br>• the street review's findings |
| 2 | **The terrace generator** (Prop Forge `terraces` kit): `terrace_a`-`g` stand-ins with the same footprints and signs, each house different and the rows still joining up. |
| 3 | **Civic buildings:** town hall, church, cafe, shop, post office, hotel, mill, barn and station stand-ins; fountain, market stall and town gate. |
| 4 | **Hillcrest structure:** the castle set; parapets, balustrades and lookout rails as Forge rails; terrace walls as dry-stone runs; the silo; the causeway balustrade. |
| 5 | **Hillcrest judged:** area audit, street review, fixes. |
| 6 | **Quarry Hills structure:** crawiau as a slate-slab run kit; yard walls as runs; phone and post boxes from the street kit; slate rock stands and spoil heaps; quay works; Blondin towers. |
| 7 | **Quarry Hills judged:** area audit, street review, fixes. |
| 8 | **The whole map:** an ordered list of towns (from docs/58 and the world plan) and the first of them. |

## Progress
- **Round 1: done (19 Oct). The new Bayview passes the area audit**, from 10 short to none.
  - **Roadside walls:**
    - Cornish dry-stone, 12.2 m out, along every stretch where the land falls away steeply within 12 m of the kerb.
    - They're found with the audit's own raycasts: casting from higher up read tree trunks as ground.
    - The causeway link has dry-stone parapets down to the Halt quay.
    - Steep banks: 259 to 0.
  - **The causeway link's step:** 0.4 m to 0.
    - The link now holds quay level past the start straight's shoulder.
    - The mainline's causeway cut now follows its deck north of the Halt quay; it had cut the land to 0 m under a
      road at 0.9 m.
  - **Furnished to the town standard:**
    - lamps down the start straight;
    - a fish market, telescopes, flower beds and benches on the quay;
    - flag poles and a map board at the Pit Stop;
    - a kiosk and parasols at the pier's root, and benches, a telescope and a flag on the pier;
    - picnic tables on the lighthouse headland and the windmill summit;
    - five parked cars on the forecourt.
  - **Variety:** the town row cycles all seven terrace designs, and the beach huts are cut to five.
  - **Small fixes:**
    - review mode seats the car on the real ground (it sat 1.1 m into the forecourt);
    - `--new-bayview` now implies `--prop-forge`;
    - the audit names the Prop Forge pieces off the ground;
    - the fireworks barge counts as afloat.
  - **Checks:**
    - the smoke test passes with the flag;
    - seeds 1709-1711 finish 24/24 with no resets, the same as before;
    - the fingerprint is unchanged;
    - the regression is unchanged.
  - **The street review** of the island (a Sonnet agent, photos taken before these fixes) folds into round 2.
- **Round 2: done (19 Oct).** The terrace generator, and the round 1 review's agreed findings
  (`review/street-review/r1-new-bayview.md`).
  - **Terraces** (car-forge `terraces` kit): terrace_a-g stand-ins, seven families, four variants each.
    - The families: balconied, shop with an arched front, front-gabled cottage, Georgian with pediments, shop with a
      mansard, fisherman's with a bay window, and a balustraded first floor.
    - Party walls are plain and roofs stop flush, so rows join up.
    - Each house has a sign socket and a chimney socket. The chimney smoke in Hillcrest and Quarry Hills now rises from
      the Forge model's own stack (`ForgeStandIns.ChimneyOr`); without the flag it's the old table's point.
  - **The sea (R1-1, high):**
    - The pale rectangle on the open sea came from the harbour-arm term in the island's height. It held the whole
      sea floor up at -16 m to the land grid's edge.
    - Now the floor shelves to -44 m offshore and to the grid's edge, the basin inside the arms stays one shallow
      turquoise and blends out across the mouth, and a deep floor lies under all the sea.
    - The mainland's sea floor matches it with the flag.
    - The harbour walls run down to the deeper floor, with a light cope and a dark weed line at the tide.
  - **Run-off and lane (R1-2, R1-10):** the run-off shoulders start at the asphalt's edge, and the gravel lane's strip
    stops at the loop's run-off; both had lain on the road.
  - **The windmill (R1-4):** a Forge tower mill on the summit, facing the harbour: stage, cap, fantail, and four
    sails as their own piece, turning.
  - **The hills (recommendation 2):** a hill farm with a farmhouse, barn, tractor, washing line, beehives and bales,
    and a dry-stone walled field with a gate and six grazing sheep.
  - **The bends (recommendation 4):** 14 chevron boards on the outsides of the tight bends, and tyre walls where no
    stone wall already guards.
  - **The cliffs (R1-5):** 143 Forge rocks along the waterline and up the faces.
  - **Trees (recommendation 8):** a fifth smaller on the island, so the buildings read bigger.
  - **Not taken:** narrowing the sand run-off (recommendation 3). Sand run-off is the game's racing convention;
    that's for Richard to decide.
  - **Also fixed:**
    - a stage 1 slip of mine had commented out the "cliffs" load step;
    - `quarry-run.ps1 -Hidden` runs a test with no window at all (for when Richard is using the PC).
- **Round 3: civic buildings (19 Oct).** car-forge `civic` kit:
  - shop (3), cafe with a terrace of tables and parasols (2), post office with a clock pediment and pillar box (2);
  - hotel with chequered balconies and a rooftop name (2), gambrel barn (2);
  - ice-cream kiosk (2), fountain (2), market stall (3).
  - Each keeps its id's footprint and height; signs sit on the fascia.
  - Two signed houses in the new Bayview's row became shop terraces: their signs had been hidden behind a balcony.
  - **Standing cars:** CarContact sits a car a little deeper where the ground twists under it. Residents and home cars
    are seated where they end up, which fixed a Quarry Hills resident with a wheel 0.25 m up.
  - **Checks:**
    - the fingerprint without the flags is unchanged;
    - the smoke test passes;
    - the area audit without the flags: no cars off the ground.
  - **Street reviews of Hillcrest and Quarry Hills** (two Sonnet agents, photos with `--prop-forge`) are under way;
    their findings go into round 4.

## Paused 19 Oct, evening: where to pick up
**Round 4, in progress (uncommitted):**
- **The crawiau** (Quarry Hills' slate front-garden fences) as split-slate slabs.
  - car-forge `forge/world/kits/slate.py` has eight variants, modelled in the unit cube so the existing per-slab
    matrices place them unchanged.
  - `QuarryHillsTown.Combine` uses them with `--prop-forge`.
  - **First fix:** the kit names `'SlateRock'`, but a kit with no stand-ins must use material classes. Change it to
    `'stone'` (`['stone'] * len(F)` in `crawiau()`); the combiner gives them its own slate material anyway.
- **Then:**
  - build, and photograph a Quarry Hills street with `--prop-forge`;
  - check the fingerprint and the regression;
  - commit car-forge (`slate.py`) and little-road (`QuarryHillsTown.cs`).

**Round 4, the rest:**
1. **The street reviews:** fold in `review/street-review/r2-hillcrest.md` and `r2-quarry-hills.md` (two Sonnet
   reviewers, still running at the pause). Triage them as R1's was, in a table in the same file, and fix the high
   and medium findings.
2. **Quarry Hills back-yard walls:** dry-stone slate runs (`Runs.Solve` with `restyle=slate`, as the country walls
   do), not boxes (`QuarryHillsTown.WallBox`).
3. **Phone and post boxes** (`QuarryHillsCountry.TownBits`): the street kit's `phone_box` and `pillar_box`.

**Round 5, Hillcrest structure:**
- the castle set (gatehouse, towers, curtain walls, keep, donjon, palace) as a `castle` Forge kit with the Blender
  sizes (measured in the round 3 notes: tower 11 m across and 36 m tall, wall 20 x 4 x 11 m, and so on);
- the square's balustrade, parapets and lookout rails through `ForgeRail` / the armco kit's baluster and coping;
- the terrace walls on the vine hill as dry-stone runs.

**Rounds 6-7:** each town judged (area audit, street review), then the whole-map plan (round 8).

**Smaller things noted:**
- the long dark retaining wall behind the new Bayview's town row needs steps or gardens;
- the east beach is a steep sand bank;
- the sand run-off width is Richard's call (R1 recommendation 3).

**Ways of working that helped:**
- `quarry-run.ps1 -Hidden` runs a test with no window.
- Never delete a run's photos while a reviewer agent is reading them: the regression's clean-up emptied R1's folder
  mid-review.

- **Round 4: done (20 Oct).** Quarry Hills' review findings (`review/street-review/r2-quarry-hills.md`), the yard
  walls and the town's boxes, and a cheaper way to review. Everything new is behind `--prop-forge`.
  - **Load time** (committed first, f38dc00): `NewBayview.On` and `PropForge.Enabled` read the command line once.
    - The new Bayview's island land: 8.9 s to 0.5 s. The mainland: 2.3 s to 1.5 s.
  - **The crawiau** as split-slate slabs from the car-forge `slate` kit (f38dc00, car-forge dcacf20).
  - **Signs (QH2-02, QH2-06):**
    - A shop's fascia is deeper. A terrace's name is lettered at 0.8 size, so it fits the fascia under the floor above.
    - A shop never has a balcony.
    - `terrace_b` variants 0-1 are shops and 2-3 houses. A placement with a shop's name takes a shop, one without takes
      a house, so no fascia stands empty.
    - The Forge's `terrace_e` and `terrace_g` (always shops) get names in Quarry Hills.
  - **Lamps (QH2-03, QH2-04):** at the party wall between two fronts, not in front of a door or a name.
  - **Parked cars (QH2-05):**
    - A 5.4 m front has no room for a car, so there are no front drives in Quarry Hills.
    - Cars park nose-in in the gaps between houses, and down the middle of the back lanes.
    - The town keeps its life: 112 homes, against 69 before.
  - **House_d's loggia (QH2-07):** a shaded recess with glass, not stone that read as bricked up.
  - **Back-yard walls:** the back walls are slate dry-stone runs.
    - A row's back walls join into one run, even where the houses run the other way along the street.
    - A run's ends move in off steep ground.
    - Any run under 8 m, or with a pier that can't stand level, is stone boxes instead, as are the short walls between
      yards.
    - Result: 150 runs, and the town builds in 1.1 s (0.9 s before; 2.0 s when every wall was its own run).
  - **Phone and post boxes:** the street kit's `phone_box` and `pillar_box`.
  - **QH2-01 (the house over the cutting at Quay Lane's end):**
    - Not seen in the new photos of Quay Lane at 380-570 m.
    - Houses and pavements now stand on the ground as drawn (`Drawn`), not the height field. Nowhere in town do the two
      differ, so the cause wasn't found.
    - Watch for it in the next look at Quay Lane.
  - **Benches (QH2-08, QH2-20):** they tilt with the land by design (GroundFit, Richard 18 Oct). The square's photo shows
    one leaning about 8 degrees on the slope, legs on the ground: not tipped.
  - **Checks:**
    - The fingerprint without flags is unchanged (`3f164ae90677e486`).
    - The area audit with `--prop-forge`: Quarry Hills' floating pieces and hidden signs are at zero. Left: one road-edge
      lip (there since round 3), one steep bank, and one parked car slightly off its pad.
    - The full regression wasn't run (Richard, 20 Oct: only when needed).
  - **Not done:**
    - QH2-09-22 (pavement ends, slope textures, blank gables and the rest) go into round 7.
    - Old Bayview's bunting crosses FISH & CHIPS; that's old Bayview only, which the new Bayview replaces.

- **Round 5: done (20 Oct). Hillcrest's structure,** behind `--prop-forge`.
  - **The castle** (car-forge `castle` kit): curtain towers, walls, the gatehouse, keep, donjon and palazzo.
    - Ported from the Blender castle at the same sizes and facings, so they stand where the game places them.
    - The palazzo's arcade and top-floor loggia now read as open; in the Blender model they were inside the solid wall.
    - The gatehouse keeps the road clear through its arch. A Forge piece can now carry more than one collision box
      (`collision.more`: both drums and the block over the arch), and it has its gate sign socket.
  - **The square's balustrade:** the railings kit's turned balusters under the saddleback coping.
  - **The lookout:** the coping rail on turned posts.
  - **The vine hill's terrace walls:** dry-stone runs along the rows, or the box where a pier can't stand level.
  - **The farm's grain silo** (farm kit): a corrugated bin on a concrete ring, a red roof and vent, a caged ladder and a
    fill pipe.
  - Already Forge before this round: the causeway's balustrade and the town parapets' copings.
  - **Checks:**
    - The fingerprint without flags is unchanged.
    - The area audit's Hillcrest defects are the same as before: one floating piece and six steep banks, both still to
      fix.
    - Hillcrest's town builds in 0.72 s, against 0.48 s, because of the terrace runs.
    - Photos: three of the castle and a sheet of four (balustrade, lookout, silo, terraces).
  - Published to Richard's play copy before this round (main 87e5aed, with `--prop-forge --new-bayview`).

- **Round 6: done (20 Oct). Quarry Hills' structure,** behind `--prop-forge`.
  - **The roadside kit** (new): telegraph poles, tyre walls, ad boards and chevron signs, at the Blender kit's sizes and
    sockets. They replace those pieces in every town.
    - Telegraph poles: a taper, a crossarm on braces, glass insulators at the wires' height, steps.
    - Tyre walls: real hollow tyres, painted by the stack; a lighter far version (7-sided tyres).
    - Ad boards: a framed panel on A-frame legs, with the brand on its socket.
    - Chevron signs: cut chevrons on two posts.
  - **The quarry kit** (new):
    - slate wagons, loaded and empty, on flanged wheels: the quay's rake of eight, and the incline's two, pitched with
      the slope;
    - the quay's hand crane: a riveted mast, a lattice jib over the water, the chain, hook and winch;
    - racked slate stacks (the Blender kit's `slate_stack`) for the quay's shipping stacks, which were boxes.
  - **The standing stones:** `rock_stand_a`-`d` in the rocks kit. A cleaved slate monolith with a fallen slab, two
    leaning slabs, a split boulder in its rubble, and field boulders.
  - **Left as they are:** the spoil heaps (part of the ground), the Blondin's iron lattice towers (beams, looking
    right) and the quay's sleepers and rails.
  - **Checks:** the fingerprint without flags is unchanged; the area audit's defects are no different; photos of the
    quay, wagons, incline, standing stone and quarry floor.
  - **Review:** one Sonnet agent on two contact sheets (14 views), covering rounds 4-6:
    `review/street-review/r6-targeted.md`.

- **Round 7: done (20 Oct). The review's fixes, the leftovers, Richard's snaps, and the flags into the game.**
  - **From the targeted review** (`review/street-review/r6-targeted.md`):
    - **Terrace walls:** each terrace's walls are one continuous dry-stone run along its curve. Where a run can't stand,
      each piece tries alone, then falls back to its box: 115 runs.
    - **The silo:** self-coloured seams and a door in place of the dark grid that read as glass; 4 m clear of the barn.
    - **The quay:** a heavier crane, and slate stacks along the wagon rake and round the crane's foot.
    - **Garden paths:** each Quarry Hills house's fence gate stands before its front door (a new `Door` socket on the
      houses and terraces kits), with stepping slabs from the pavement to the door.
    - **Back rows:** no unnamed shops.
    - **The balustrade:** the square's parapet no longer doubles up behind the balusters; under them it is only the
      retaining face.
    - **The drawbridge:** it was there all along; the review's camera was below the road.
  - **Steep banks** (`BankBarriers`): the edge audit's unguarded banks get a wall along their top once the world is
    built. Dry-stone first, then post-and-rail or armco where a pier can't stand.
    - Hillcrest's banks went from 6 to 0, and Hillcrest Valley's from 4 to 0.
    - The sites are cached per build and flag set: 1.8 s on a build's first launch, about 20 ms after.
  - **Richard's snaps** (bug inbox #15-#21):
    - Houses built over each other at a Hillcrest junction: buildings now test each other's footprints.
    - Phone boxes in the castle, and the zipline entrance blocked: fixed.
    - Tyres singly at random: the new Car Forge `tyres` run kit lays each corner's outside as one barrier, on the
      raceway, the Quarry Sprint loop and the new Bayview.
    - Opened for round 8: the UI's look and the fields.
  - **In the game by default** (Richard, 20 Oct): Prop Forge, the new Bayview, the Car Forge class C roster and
    stop-look.
    - Opt-outs: `--no-prop-forge`, `--old-bayview`, `--no-forge-roster`, `--no-stop-look`.
    - The editor's checks and bakes still build the old world unless asked.
    - Left off for Richard: `--traffic-yield`, `--livery`, `--wheel-camber` and `--ambient-traffic`.
  - **Checks:**
    - The old world's fingerprint is unchanged.
    - The smoke test passes on the default build.
    - The audit: Quarry Hills has one road-edge lip; Hillcrest has one floating vine row; Bayview has two rocks at the
      waterline; Hillcrest Valley has two lips. All are carried into round 8.

- **Round 7's review:** approved (`review/street-review/r7-targeted.md`). The agreed fixes are in:
  - tyre barriers at the run-off's edge round whole corners;
  - bank walls running 8 m past their banks;
  - lighter garden-path slabs.
- **Round 8: the whole-map plan is written:** [doc 81](81-whole-map-order.md), all 33 places in build order. It starts
  with the class C ring.
  - The first place is Bramblewick. Its brief is [doc 82](82-bramblewick.md); Richard's four questions have defaults.
  - This programme continues in docs/81 and docs/82.

## Reviewing without photo sweeps (20 Oct)
Richard, 20 Oct: the street-review agents (three photos every 40 m, read by an agent) used a lot of usage for little
return. From now on:
1. **Measure first.** `--area-audit` now checks the review's commonest findings (`StreetAudit`):
   - **hidden signs:** every shop name seen from the street at eye height, straight on and 30 degrees either side; a name
     with two of its three sight lines blocked is hidden;
   - **tipped furniture.**

   It also lists where each defect is (cars off the ground, banks, lips), and a `--view-shots` camera for each.
2. **Photograph only what's flagged, plus a few samples.** `--view-shots` also takes `name:@Road name,s,left|right|along`,
   the street review's own views by street and distance. I read the photos myself at 800 x 450, or as one contact sheet.
3. **Agents only for a whole-town look at a milestone,** on a contact sheet of a dozen chosen views, not a sweep.

Used in round 4: seven audit runs and four photo runs, about 15 small pictures, and no agents.

## Where to pick up
- **Leftovers for round 8:**
  - the floating vine row (Hillcrest);
  - two waterline rocks (Bayview);
  - the road-edge lips (Quarry Hills, Hillcrest Valley)..
- **Still open from the Quarry Hills review:** QH2-09-22 (pavement ends, blank gables, slope textures).
- **Round 8: the whole map** (Richard, 20 Oct). It also takes in the UI art pass (bug #18) and the fields redo
  (bug #20).
  - Plan every town from docs/58 and the world plan, with its parts and races, in order.
  - Then build the towns one at a time, in the same loop: build, check, a targeted agent review, then fold its agreed
    suggestions into the next round.
- **Smaller things:**
  - the long dark retaining wall behind the new Bayview's town row needs steps or gardens;
  - the east beach is a steep sand bank;
  - the sand run-off width is Richard's call (R1 recommendation 3).
