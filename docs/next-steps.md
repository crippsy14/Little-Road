# Next steps (3 October 2026)

**22 October, late: racing AI pace review ([doc 94](94-racing-ai-pace-review.md)); racecraft v3 behind `--racecraft-v3`.**
- The review found the driving core near its ceiling (full-ability drivers only 2% quicker; more corner grip pace-neutral and
  risky) and the race itself the cost: a 24-car field finishes about 10% behind its solo pace, the order is the grid order,
  and 95% of attacks aborted because the lane target jumped 3-6 m every second.
- Racecraft v3 (lane commitment, attacks that own a clear lane, closer following of the target) passes keep/reject in round
  two (study 259): 24/24, no resets, -88 s over six races (-0.8%), passes doubled on Cedar, attack time -40%, contacts level.
  A modest gain; the flag is off by default and uncommitted.
- 23 October: the Hillcrest loop (studies 260-261) tried more attacking (worse), giving up blocked attacks sooner (fewer
  passes) and closer following (same gain, 60% more contacts); round two stayed the version. The full sweep (study 262)
  passes keep/reject: 144/144, no resets, -0.8%, passes 44 -> 74, contacts level. Richard: "merge it in and make it the
  default"; the default since the 23 Oct build (`--no-racecraft-v3` races without it), merged into main (0246f2c), regression
  rc3 passed and the play copy published 23 Oct 12:50. The audit's new line, a Quarry Hills car not on the ground, is not
  the AI's: it came with the branch's commits since 21 Oct and is open.

**23 October: Vinelands is part of the game.** All eight zones ([docs 85–93](85-vinelands.md)) and the redesigned
Circuito da Cumeada ([doc 92](92-vinelands-zone7-eiras-vale-circuit.md)) are on by default in player builds;
`--no-vinelands` leaves the place out. The two races are a class C race on the short lap (3★, two laps) and a class B race
on the long lap (2★, two laps), both with Sr. Rogério. In the AI sweep, all 24 races finished 24/24. A race with no flags
on the racecraft v3 build also finished 24/24, with no resets. No regression was run, at Richard's call: rc3 had just
passed in the AI thread. Still open:
- the planting top-up, since the area audit fails on planting per road km;
- a handful of small junction seams.

**21 October: Bramblewick is part of the game, and the hybrid prototype is under way.**
- **Bramblewick** ([doc 82](82-bramblewick.md)) is on by default in player builds; `--no-bramblewick` leaves it out. This
  covers the West Country village, the vale, and the Bramblewick Lanes race with Jed.
  - The lanes race was judged LAUNCH-READY (launch review r4).
  - The AI races it at skill 3, 7, 10 and 13: 12 of 12 races 24/24, no resets.
  - Published to the play copy after a full regression.
- **Hybrids** ([doc 84](84-hybrid-prototype.md)): Richard's battery idea, prototyped behind `--hybrid`.
  - The City hybrid 1.5 is at Jed's, with regen and automatic deploy, and a battery bar on the dash.
  - `--hybrid-everyone` is for same-field keep/reject races.

**19 October: Bayview rebuilt ([doc 79](79-bayview-rebuilt-brief.md)): stages 1-3 done and stage 4 under way, behind `--new-bayview`.**
- **Approved with the defaults:** the island grows to about 430 × 300 m, the lap runs clockwise, and the character is a Cornish and Scottish harbour.
- **Built:**
  - the island and its 1,079 m Harbour Loop;
  - the granite harbour with its arms;
  - the paved quay, with railings, lamps and benches;
  - the town row along the harbour front, in bold paint;
  - the Pit Stop on its own forecourt;
  - the landmarks: a granite clock tower on the quay, the lifeboat station and its slipway on the west arm, a
    pleasure pier with a helter-skelter off the east beach, the lighthouse on the west point, the windmill on the
    summit;
  - harbour life: steps down the quay wall to a pontoon, boats and buoys bobbing in the basin, creels on the quay,
    beach huts by the pier, and a granite retaining wall behind the town.
- **Checks:**
  - the adventure smoke test passes with the flag;
  - races on seeds 1709-1711 finish 24/24 with no resets;
  - the world without the flag is unchanged.
- **Next:** the rest of stage 4 (people on the quay, a real beach, the bare cut slopes), then stage 5, the judging.
- **Also done:** a far version for each Prop Forge house (houses@2).

**18 October, late night: Prop Forge stage two ([doc 78](78-prop-forge-stage-two.md)): the world remodelled, behind `--prop-forge`.**
- **Remodelled:**
  - every tree and rock, in seeded families;
  - Quarry Hills' walls in slate dry stone, its field gates and its sheep;
  - Armco guardrails, rumble kerbs and coping stones;
  - lamp posts, benches, fences, tents and bales;
  - three kinds of boat.
- **New street life:** bins, bollards, a phone box and a pillar box, A-boards, creels and crates, deckchairs, placed by rules in every town.
- With the flag off, the world is unchanged (fingerprint `3f164ae90677e486`).
- **To see it:** start the game with `--prop-forge`.

**18 October, night: Prop Forge stage one, steps 0-6 ([doc 77](77-prop-forge-brief.md)).** All of it is behind flags
(`--run-range`, `--run-range-review`, `--prop-forge`); the normal game is unchanged. Full write-up: car-forge
`docs/log.md`.
- **The run range** (`--run-range`): a fixture strip of roads (a straight with a junction and a drive, a crest and a
  hollow, a 30% bank, and 40, 15 and 8 m bends) where every run and object is checked and captured.
- **A post-and-rail fence:**
  - posts plumb, rails raked between them;
  - openings framed by gate posts;
  - 2.4 m spacing, 61 triangles a metre.
- **A dry-stone wall that bends:**
  - piers at its ends, openings and corners of any angle;
  - pieces bent along the road and sheared to follow the ground, with no cracks at the joins;
  - a new dry-stone shader pattern, its cope stones on edge;
  - one smooth collider, so a car sliding along it keeps 100% of its speed.
- **Hay bales:**
  - eight variants that never repeat close by;
  - round bales on a slope turn their axis downhill;
  - each settled into the ground;
  - a straw pattern.
- **The jerry can, one object with two users:** six stand on the range's quay, and the Cob carries the same can on its
  tailgate (`prop:jerry_can@1`) at its real size. Only the Cob changed; the other 54 cars are identical.
- **Step 6, Bayview behind `--prop-forge`** (doc 77, "Stage one, as built"):
  - **The coast road's stone wall** is one dry-stone run along the bends, with a pier either side of every opening.
  - **The farm's paddock** is a post-and-rail fence, gatepost to gatepost round its corners.
  - **The farm's bales** are Prop Forge's, and three jerry cans stand on the Halt quay.
  - **Proof:**
    - Flag off, the world's fingerprint is identical to the code before this step.
    - The area audit is unchanged.
    - A street review's verdict: "the new dry-stone wall works".
- **To try it:** start the game with `--prop-forge` and drive the coast road past the farm.
- **Since then:**
  - the piers are no longer "a bit like bins": end piers carry the wall's cope, and gate piers have a pitched capstone
    and a ball;
  - the area audit counts Prop Forge's pieces;
  - the Bayview build is down from 97-142 ms to 66 ms;
  - the full regression passes.
- **Next:**
  - placement-audit footprints;
  - caching solved runs (60 ms warm budget);
  - breadth (step 7).
- **Two defaults I took, which Richard can overrule:** props keep their real size on cars, and re-dressed cars are
  shown as before/after sheets.

**18 October, evening: the class C art pass.**
- **The review:** an art-director review agent went through all 55 class C cars in one voice
  (car-forge `review/artistic-1`). I applied most of it (car-forge `tools/artpass_c.py`):
  - silhouettes split apart: true saloons, a tall-hatch family with clear sizes, long and short cars;
  - one signature each;
  - borrowed kit removed.
  No pair reaches the 0.90 look-alike mark any more.
- **Collision boxes:** Forge cars' boxes are now centred on the body. Props hanging off one end (the Ladders' ladder,
  the Pronto's hot box) had shifted the box and left cars stuck. Bayview 24/24 four times after the fix.

**18 October, later: class C hand pass; class B cars removed.**
- **Class C hand pass** to the Kestrel standard (car-forge `tools/closeup.py`, `tools/handpass_c.py`):
  - drawn lines on every car;
  - per-car touches (the Beetle's round back, signs, a serving window, working dents).
  All 55 pass; Bayview 24/24 twice.
- **Class B removed** (Richard: to be redone with his direction from the start). Class B fields use the Tuner bodies
  again, and the engine's class B features stay for that round.

**18 October: both Car Forge rosters race, behind `--forge-roster`.**
- **Class C:** 51 cars, after two expert reviews and hand passes. Fields are dealt by grid slot: the flagship on pole,
  quick-looking cars at the front, the band in the middle, veterans at the back; at most two of a model, never side by
  side. The rally cars also race on gravel. Bayview, flag on and off: 24/24 finished, no recoveries.
- **Class B** (`car-forge/docs/roster-b-design.md`): 84 cars:
  - the three starters (Tsubasa, Sakura, Kitsune);
  - a B kit for each class C car (street, drift, rally or sleeper);
  - 30 originals, with one expert review round done.
  Coast Tuners, Slate Rallycross and Raceway Reverse: 24/24, no recoveries.
- **Collision boxes:** Forge bodies' boxes stop at the car's own top, so props such as the Gelato's cone don't count.
  146 Forge cars are synced as build output.
- **Waiting on Richard:**
  1. The class B cast list (`roster-b-design.md`, "Open questions").
  2. **The Prop Forge stage one brief ([doc 77](77-prop-forge-brief.md))**: yes, edit or veto.
- **Class C top fields filled:** the class's best (Sprint, Comet, Roadster, Kestrel, Merlin, Osprey) now race up to
  10★, and four trims (Otter GT, Heron Rally, Cob Rally, Fell Rally) join at 7-10★. 9★: 10 tarmac and 7 gravel cars
  in band; it was 2 and 1.

**17 October: Car Forge ready for the class C roster; two designs.**
- **Car Forge** (`car-forge/docs/acceptance.md`): G1–G9 met, and the batch tool generates 24 gated class C designs in
  about 2 minutes. Twelve Forge cars are reinstalled, with livery styles in `ForgeLivery` (uncommitted); the dev build
  passes its 2898 checks.
- **The class C roster design** (`car-forge/docs/roster-c-design.md`): 50 cars. The 31 existing ones are rebuilt at the
  Kestrel standard, keeping their names and roles, and 19 new ones are mostly rogue toys and quick-looking warm trims.
- **Prop Forge ([doc 76](76-prop-forge-design.md))**, a design for one built world. Walls and fences drawn along roads
  with ends, corners and openings; props fitted to slopes; buildings on platforms; Bayview rebuilt with it.
- **Next three pieces:**
  1. The Car Forge engine review's agreed changes (a review agent is running).
  2. The class C roster: new concepts, kits, wear, generation and a hand pass on every car, then into the game behind a flag.
  3. Prop Forge's first recipes, proven on one Bayview street.

**13 October: the snap tool, how we work, and the cleanup.**
- **Snap tool** (F9, or hold R3), with a note box and a bug inbox (docs/73, `docs/bug-inbox.md`).
  - Richard's first ten snaps are logged as inbox #3–#11. Each one has enough to find and reproduce the spot.
  - The tool now also records:
    - the race event, seed and field;
    - the running activity (for example the zip line).
  - It no longer captures the previous snap's toast.
- **Cleanup ([doc 74](74-bloat-audit.md)).**
  - The first racing AI is retired, along with its study code, orphans and old harnesses: 7,601 lines removed.
  - Pinned checks are loosened to rules, unused tools are removed, and one line-ending script remains.
  - Races are unchanged: studies 224 and 225 are identical.
  - The regression now races a 24-car Bayview lap.
- **14 October:** snap bugs #3–#10 fixed and checked; muting enforced for every test window; photos removed once checked;
  wheel finishes and stripes now show in game (they had no colour rule); Rally saloon and Zenith GT race only 8★–10★ C events.
  **The Car Forge spec ([doc 75](75-car-forge-spec.md))** is written for Richard's approval: a new panel-based car generator
  with object-space liveries, for about 170 bodies across C, B, A, S and E.
- **Next three pieces (rolling plan, docs/73 §2):**
  1. **Fix round for the inbox, in order.** First the field stripes on the valley road and the tunnel-mouth gaps (things a player drives through), then the floating bushes, terrace walls and cables, then the placements (Poppy, Rollo), then the zip-line camera and the class C field.
  2. **The 12 open area-audit banks** listed below.
  3. **The first self-planned district brief** from doc 58's build order.

**12 October, afternoon.**
- **Speed-up list ([doc 72](72-world-streaming.md) section 8):**
  - ready to drive in 3.0 s (3.7 s on a build's first launch);
  - race entry 0.7–0.9 s;
  - terrain meshes built in bulk, cliff rings cooked, race engines prepared on all cores, warm-ups once per build.
- **Area audit, steep banks (doc 57: guarded or drivable): 17 → 12.**
  - **Via Crest (9 → 5).** Its downhill row leaves a 2.5 m gap between some neighbours, and the hillside falls away behind it. Those gaps are now garden gaps closed by a short stone wall, as the wider gaps already were.
  - **Tried and dropped:** a continuous wall plus garden gates along the whole frontage. It cleared 8 of the 9, but walled off shop doors and looked like a fence.
  - **What stopped the parapet at the other spots:** the parapet is skipped wherever a house's world-aligned footprint box covers the wall line. On the curving street a rotated house's box reaches metres past its walls.
- **Still open (12, plus 1 lip):**
  - **Via Crest (5):** at (137,−1225), (192,−1247), (214,−1020), (271,−1090) and (211,−1158). These spots need a closer look.
  - **The Bayview circuit's two drops** at (−121,35) and (−115,−16), where a stone wall ends short of a rock outcrop.
  - **The Raceway road's verge (3)** near (−119,−906), with a drop 8–12 m out.
  - **Single spots (3):** the mainline at (71,−113), the Falls road at (639,−688), and a Quarry Hills junction lip.

  For the Raceway road I tried railing roads within 30 m of the river and treating only real crossings as bridges. Neither changed it, so both were reverted; the cause there isn't found yet.
- **Traffic courtesy, behind `--traffic-yield` (off; Richard to decide).**
  - **Yield:** a valley or Quarry Hills traffic car with you close behind in its lane pulls 1.2 m towards the verge and eases to about half speed.
  - **Jam release:** pressed against a stopped player for 4 s, a traffic car turns its collider off for 3 s and drives on. In the regression the train test's car sat 70 s against one; nothing that pushes into a scripted traffic car can move it.
  - **New test:** `--overtake-test` drives the car up behind a Quay road car and out to the crown of the road to pass. It passes in 10.8–10.9 s with no bumps with or without the yield: the Quay road is wide enough already.
  - **So:** the yield shows no measured gain there. The jam release is the part that matters, but it isn't reproduced on demand yet.
- **Checked, already fixed:** the title-screen ambience (the "all sea" waves). `Ambience.Survey` treats no ground under any probe as no world.

**12 October: loading and streaming, part 3 ([doc 72](72-world-streaming.md), section 7).** Quarry Hills now builds in the
background after you're driving (the default; `--no-background-districts` builds it at load). Ready to drive is 4.3 s, against 5.3 s when it's
built at load. The world and the town come out the same either way: a stronger world fingerprint and a new `--town-print`
check both match. Also:
- the worked-slate surface is computed in parallel (279 to 62 ms);
- per-tick race telemetry is written only with `--telemetry`;
- the world and map caches keep only the two most recently used builds;
- the clean-up covers race reports, scratch and Blender backups (1.6 GB went to the Recycle Bin);
- `--review-retire` round trips work.

The later items (MeshDataArray terrain, cliff arrays, instancing, the GraphicsStateCollection warm-up, race-entry engine banks,
then floating origin and HLOD) are listed in doc 72 section 6. Committed on 12 October after a full regression (sr-districts-1): everything passed, and the area audit improved (Emyr's car fixed). The train race lost once, stuck behind valley traffic, and won on reruns. So the point-to-point drive tests (train, sprint, blast) now run with `--no-traffic` until the traffic can overtake. Richard listened on 12 October and the music and ambience sound right (compressed in memory, loaded in the background). **Open sound note:** a little feedback at dusk from the workshop engine near the Bayview garage. The cause isn't known yet; noted for later, not investigated.

**4 October:** [doc 62](62-street-review-quarry-works-licence.md) covers four things:
- The street review's fixes: parked cars, shop signs, Welsh cottages and slate fences, and more.
- The quarry's new look: a gravel floor, a rutted rallycross, standing rocks and spoil heaps.
- The working machines: an excavator, dump trucks and a loader, which line up to watch the rallycross.
- The Gran Turismo-style rally licence screens.

**Paused 8 October (Richard switched to work). Resume here:**
1. **Per-car lines bug, fixed but not yet raced:** on Harbour Reverse (Bayview reversed), a baked line cut a tight bend past the ±5.6 m corridor and the race stopped at load (`ArgumentException: Cartesian racing-line sample left its local road corridor`, in `CarLines.Prepare`). Fixed in the build: `RacingLine.Sample` puts any point that strays back at its clamped offset; a group whose line still fails drives the shared line with a warning; `CarLinesChecks` now builds both test cars' lines on Bayview reversed. **Still to do:** race Harbour Reverse to confirm. The other five events of the six-race check all finished 24/24.
2. **Then commit** (Richard: "Lets commit that"): run the full regression first (a landmark), restore line endings, and commit on `claude-additions`. Nothing from 7–8 October is committed yet. git needs `-c safe.directory=<repo>` (the folder's owner differs).
3. **Next job: map loading and streaming** (Richard, 8 October). The map will grow a lot (the world blueprint), so loading the whole map at game start and again for every race isn't acceptable for adventure mode or races. Research how games handle it (world partition and streaming cells, HLOD, baked data, async loading), then build it in before expanding further. Today: about 16 s to start the game and about 11.7 s to enter a race, because the world is rebuilt from scratch ([doc 71](71-racing-lines-and-fast-cars.md) section 6).

**Next session (Richard, 7 October):** fix the racing AI so it doesn't run off Hillcrest Raceway (~240 m and ~930 m into the lap). "Barriers don't solve the problem, they hide it": the new corner armco catches cars that have already left the road, so judge the AI with them off or by counting barrier touches. See [doc 70](70-race-fields-across-the-map.md) section 5.

**7 October, evening:** race fields across the map ([doc 70](70-race-fields-across-the-map.md), behind `--field-recipe`). Driver ratings are fixed per class (C 12–16, B 17–20). Parts come from each race's class, stars and surface, following the unlock curve. Class C now has planned stars too (21 races, 89★). `tools/field-table.py` gives the PP of the field at every planned C and B race. A DR 17–20 field crashed at Bayview's first corner (Line ~1.0); `--traffic-corner-margin` fixes it (study 221). Also noted as plans: Vinelands' answers (doc 68), the rugby conversion kick (doc 59), and My City as your own city ([doc 69](69-my-city.md)).

**7 October, later:** the Vinelands plan ([doc 68](68-vinelands.md)), step 0 of doc 57. It's Douro-style wine country: a river bend, about 50 stone-walled terraces up to a ridge-top village, and one road from river sweepers to hairpins that becomes the pro Hill Climb. It covers two C races and a 2★ B race, 13 things to do, three recruits, the azulejo-tile keepsakes, the eras and the build order (the map, region thresholds and Valley Road come first). Five questions for Richard are at the end.

**7 October:** the plan for class B races on today's three tracks, and for how equipment unlocks across the whole map
([doc 67](67-class-b-races-and-unlocks.md)):
- a Tuner Garage in every town;
- parts unlocked by results anywhere (Rep);
- forward and reversed events at three field strengths;
- a championship finale.

It needs Richard's decisions (doc 67, last section).

**6 October:** the class B Tuners ([doc 65](65-class-b-tuner-cars.md)) and car builder v2 ([doc 66](66-car-builder-v2.md)):
- Phase 1 added per-station sections, fender pods, road tyres and wheel finishes.
- Phase 2 added a lofted cabin with windows by outline, face outlines and integrated noses.
- The Hayate, Kaze, Raijin and Hoshi now each have their own cabin and face (`review/cars/tuner-v3`).
- The cars are renders only; export them to the game once Richard is happy.

The farm/HUD round (doc 64) is still uncommitted: the farm autopilot tests need fixing first.

**5 October, evening:** Farmer Giles's Harvest and Apple Shake, a new challenge HUD (title card → corner tracker, quest card) with Restart/Exit in the pause menu, the train race and countdown fixed, and a harder Hard Castle Climb: [doc 64](64-farm-challenges-and-activity-hud.md).

**5 October, later:** in free time I built the Blast Run (race Dai up the haul road before the quarry blast) and the Tip Leap (a danger-sign jump off the big tip): [doc 63](63-blast-run-and-tip-leap.md).

**5 October:** the street review is finished and its findings fixed, low-severity ones included (doc 62, round four). The reviewer is the Sonnet `street-reviewer-high` agent (`review/street-review/effort-comparison.md`).

**Plan of record (29 September):** [doc 45](45-roadmap-and-feature-spec.md) sets the order of work, Phases 0–5; the parts ladder is in [doc 44](44-parts-tiers-and-progression.md). The campaign is at least 12 hours and open-ended. Next is **Phase 0**: world grid with per-tile map and fog, Racer records, radar chart, a traffic-aware launch, save and IDs, and track standards.

**The world plan (3 October):** [doc 58](58-world-and-campaign-design.md) is the design for the whole world and campaign. It covers:
- the **Glasswater** continent (about 20 × 20 km, generated from climate);
- 33 places, 121 races and the story: prove yourself, get entered in the Aurora Endurance, beat Duke Rolo;
- the post-game;
- the railway wars;
- 24 design principles from research, each with a checkable rule.

The world blueprint artifact is the interactive version.

**W0, the scale test, is built.** `--scale-test` builds the whole continent from the blueprint's data in the game's own coordinates:
- rough terrain coloured by biome;
- graybox roads with switchbacks on steep ground;
- blocks for towns, the sea and the lakes.

You can drive it in any class's strongest build:
- **Class:** 1–5 picks C–E, 0 the stock car, M the max build; D-pad left/right on a pad.
- **Map and jumps:** N or View opens the map; Tab or D-pad up/down jumps between towns.
- **Pacing:** a trip timer logs every drive between places, and `--scale-pacing` writes drive times for every road and class.
- **Autopilot:** `--scale-autodrive a,b,c` drives real physics along the roads.
- **Shortcut:** none since 4 October (one build, one "Little Road" shortcut); run the play copy with `--scale-test`.

Results are in doc 58 §12. **Next (W1):** foundations (world tiles, streaming, floating origin, data-driven regions, race rows, world-state layers), then the C ring town by town.

**Quarry Hills is part of the game (since 4 October; built behind `--quarry-hills` on 3 October, [doc 59](59-quarry-hills.md) "Build status").** `--no-quarry-hills` leaves it out. The play copy has one shortcut, "Little Road". What's in it:
- the slate town of about 1,560 buildings in terrace blocks, with the square and the Pit Stop;
- the galleries with inclines, Glas, the tips, the quarry floor, the rally school and the Quarry Sprint;
- the Blondin, and Slate Quay with its rail terminus;
- 6 named residents, about 110 townsfolk, Welsh traffic and Dai the Dumper.

Gwen's Quarry Sprint time trial is playable. Against doc 57's standards, Quarry Hills passes everything but the edge defects (1 lip and 2 banks, against Hillcrest's 8 banks), and its frame cost is below Hillcrest's.

**Round two (evening):**
- Gwen's six rally lessons, whose six golds unlock the gravel tyres and gravel rack at Grit & Gravel;
- the Blondin as a ride;
- the Gallery Climb hill climb up the galleries;
- 12 slate carvings with their own album;
- denser lamps and furniture.

Test windows now open without stealing focus (`tools/quiet-start.ps1`). Next is the Sprint as a 24-car race, Heavy Haul and the Blast Run, the railway, then **Bramblewick**, the first village.

**Phase three (3 October, night; doc 59 "Phase three"):**
- the saved position and warp to a garage;
- the map covering Quarry Hills, and the Slate Quay station;
- the road seam audit;
- the matte slate;
- the rebuilt Gallery Climb with checkpoints;
- the Quarry Hills Rallycross (Class C, 24 off-roaders);
- six off-road car models;
- the countryside: farms, sheep, the rugby ground, the Great Wheel, walls and gates.

An outsider's review with ideas is [doc 60](60-outsider-review.md).

**Phase four (3 October, late; doc 60 "Phase four"): the review's recommendations, built.**
- Loose-surface grip for the Rallycross: 24/24 finishers on three serials, no recoveries, and gravel tyres now matter.
- Notice boards with map pins in all three towns, and fingerposts in miles at the nine mainland junctions.
- Load time: parallel terrain chunks, collider cooking and valley corridors. World tiles (W1) are still to do.
- Ambience by area: sheep, quay water and town squares, plus Quarry Hills' blasting siren in working hours.
- A paint reward for each full keepsake set.
- Quarry Hills routines and Rally Day.
- Sheep in the farm lanes that move for cars and the horn, and the rim cairn revealing the map.

New tests: `--wayfinding-test` and `--festival --festival-test`.

**W1, first pass (3 October, late; [doc 61](61-world-tiles.md)): world tiles and the audit findings.**
- The baked map is cached on disk.
- About 19,000 woodland pieces stream in on a 250 m grid around you, settling and reserving their spots.
- Free-roam load fell from 18.6 s to 13.5 s on a warm cache, with the same world piece for piece and the same frame cost.
- In Quarry Hills:
  - the back streets now have level landings at their junctions;
  - the street ribbons are cut into 1 m segments;
  - retaining walls stand along garden drops.
- The audit went from 6 lips, 3 seams and 1 bank to 1 lip.
- Still to do in W1: streaming the towns, floating origin, data-driven regions, and keeping the world across races.

**Update, 2 October, night: Richard's list, tied off** ([doc 56](56-wider-game-round.md) §13–20; [doc 57](57-new-area-standards.md)).
- **In-race pause:** resume, restart, view the field, options and retire. The race truly pauses.
- **Options screen:** six tabs. New settings: units, vibration, motion blur, speed effects, camera shake, minimap, button prompts, ambience. Opens from the title, the pause hub and the race pause.
- **Controls:**
  - manual gears on R2/L2 with Cross/Square shifts;
  - driving controls remappable on pad and keyboard (Options > CONTROLS).
- **Parts shop:** one class at a time (L1/R1), categories on the D-pad, Road / Rally / Ice pills (rally and ice coming soon).
- **Parts are discoveries:** found at their home shop, then sold at every shop and fitted at every garage, with optional unlock conditions (wins, podiums in a class, class reached).
- **Parts shop categories** are a vertical list on the left (D-pad up/down); L1/R1 change class.
- **Gadgets:** the building blocks for jet packs and wings: contract, catalogue, save field, button, car hook, and a test hop.
- **Standards doc** (doc 57), covering:
  - the build process for a new area;
  - measured standards with an area audit (`--area-audit`) that says PASS or FAIL per region;
  - a genre checklist from research, with Little Road's status.
- **Fixes the audit drove:**
  - cars now ride on their wheels (the physics box hung 7–18 cm under the tyres; study 219 kept);
  - standing cars are seated on their wheels;
  - the Via Crest parapet, steep-drop guardrails and raceway armco;
  - junction shoulders;
  - the planting settle pass.
- **Left:** 15 short unguarded drops (listed in doc 57).
- **Genre gaps:** a wrong-way warning, a waypoint and route line, difficulty, a camera choice.

**Update, 2 October, evening (Richard away): the wider game** ([doc 56](56-wider-game-round.md); a running log of this round).
- **Swept contacts for every car (kept, study 215).**
  - All 144 cars finished, with no resets.
  - Contacts went from 115 to 54, and total time from 9617.0 to 9616.6 s.
  - Physics cost is unchanged (study 217).
  - `--speculative-contacts` restores the old mode.
- **Hillcrest houses.** Three new terraces break the street skyline: a four-storey palazzetto with a rooftop altana, a two-storey cottage under a pitched roof, and a painted house with a gable and oculus. Roofs vary between terracotta shades and grey slate. The layout is unchanged and so is frame time (5.00 ms mean).
- **Castle gate.** The gatehouse now follows Via Crest, so the drawbridge lies under the road (it was 2.5 m off, across the road's edge, and floated 0.6 m up). The drawbridge rests on the slope and hangs on real chains.
- **Road paint.** Edge lines no longer saw-tooth round tight bends, on every valley road.
- **Houses, again.** Painted trompe-l'oeil windows on every house's side walls (no more blank row ends), and roof colours on the Bayview high street (two slate).
- **Junction shoulders.** Gravel ramps round every valley junction. The edge audit's un-climbable lips went from 60 to 4.
- **Bunting** across the Quay and Via Crest now hangs from lamp posts, and the placement audit checks every end.
- **Silent test runs** (`--silent`): automated windowed runs no longer play through the speakers.
- **Stock 1.2 sound:** the Renault R4, Morris Oxford and Fiat 500 recordings were tried and none beat the C2 (details in doc 56). It stays at 7.7, and a better recording is needed.
- **High-comp 2.0 voice:** now the DS3 WRC, 8.9 in game (was 7.9), so 15 of 18 engines are at 8 or above. Works 2.0 and Hybrid V8 stay at 7.9; the Viper's 8.3 for the Hybrid V8 comes with a chipmunk score of 2.3, so it wasn't taken.
- **Tools:** `--view-shots` takes a still from any camera position (`capture-map.ps1 -Rendered`).

**Update, 2 October, late afternoon (Richard away): engines to 8 in game, then the wider game** ([doc 55](55-engine-scorecard.md), "Round two").
- **Engine sound:** the offline in-game average went from 5.5 to 8.3, with 16 of 18 engines at 8 or above (Stock 1.2 7.8, Hybrid V8 7.9). This came from new recordings from the Sonniss 2024 bundle chosen by score (Ferrari 458, McLaren 720S, DS3 and Xsara WRC, Puma Rally1, Citroën C5), measured gearchanges and coasting in the banks (v6), longer sustain loops, gentler exhaust shaping, and fairer measures.
- **Title screen:** the surf no longer plays at full there (no ground under any sample now means no world, not open sea).
- **Class S gearboxes:** taller (27 to 28.5 km/h per 1,000 rpm, from 21 to 22): the S V8 was on its limiter near 218 km/h.
- **E-class AI:** weaker drivers no longer crash at E speeds. Aero grip and the real platform drag fixed it: all 0.4 and 0.6 drivers ran clean on three seeds, and the player's autopilot in an equal-car Duke race finished 2-3 s behind him with no recoveries (this morning: 172 s with two). A skill-scaled braking reserve was tried and made no difference, so it was removed.

**Update, 2 October, evening: the engine scorecard** ([doc 55](55-engine-scorecard.md); [listening page](https://claude.ai/artifact/8zpJcfKs294HWgpAee59PW), private).
- **The method:** every recorded engine is driven along its real recording's own drive (revs, throttle and gearchanges read from it) and scored 0–10 on pitch, tone, character, lift-off, gearchanges and rise with revs.
- **Round one:** the average went from 5.8 to 6.5. Lift-offs now fade by each engine's measured drop instead of falling 6–8 dB at once; the low rumble is back; the made-up roar and gearchange thump are gone over recordings.
- **Next round:** Fabia clicks on flat-shifts; per-engine fade times (Skyline); the Escort's level clamps; better exhaust or external references (downloads need Richard's go-ahead).

**Update, 2 October: the Duke's Challenge** ([doc 54](54-dukes-challenge.md); `claude-additions`, not committed).
- **The final boss.** Race Duke Rolo 1v1, three laps of Hillcrest Raceway, against the Regale on the measured maximum build. The first win pays 50,000 credits and gives you the Regale.
- **Aero grip for S/E cars.** Downforce now loads the tyres (×1.86 grip at 75 m/s). The AI's braking plan for aero cars keeps a 30% lateral reserve. The Duke now laps in 30.7–31.1 s, cleanly on nine seeds.
- **Test drives.** Two new desktop shortcuts: Max Build Hot Laps (timed laps of the raceway) and Max Build Test Drive (free roam on a save copy, by the Duke).
- **After Richard's first drive** (his best lap 28.02 s, against the AI's 30.7 s):
  - Platform drag is now real, and the E boxes are taller: 332 km/h on the long box.
  - Race platforms sit on stiff springs, so no more nose lift.
  - Turbo and hybrid-whine sounds are off (`--turbo-sound` / `--hybrid-sound` bring them back).
  - **S gearing is still short** (the S V8 tops out at its limiter around 218 km/h): retune it with the class S phase.
- **Parked.** The Duke's drive (`--duke-drive`) waits for a traffic overhaul that can handle a car at pace.
- **For later:**
  - (a) New, professional-looking tracks for classes A and up, for higher speeds and real cornering (Richard: a Le Mans car on Tsukuba).
  - (b) E-class AI for weaker drivers: they still crash at E speeds, see doc 54.
  - (c) Peak and floor: the maximum build is gear-limited at 76 m/s, and a slower stock car would make early upgrades feel bigger. Both are keep/reject questions once Richard has driven it.
  - (d) Possibly the aero cars' braking reserve for every car, as a keep/reject study.

**Update, 1 October: Richard's play-test fixes** (doc 51, "Richard's first play-test"; still on `claude-additions`, not committed).
- Speed traps no longer stop the car.
- Valley junctions come from a road network (doc 52).
- Photo mode opens on D-pad Up.
- Crickets are muted, and the fireworks wait for full dark and announce themselves.
- The car is held but revs through the train and castle countdowns.
- Glowing finish beacons for both events.
- The engine above the recording is reworked: a walked sustain loop, and pulse overlap-add above 1.5× (doc 47, last section).
- **The Regale** (doc 53): Duke Rolo's prototype, a sculpted toy Le Mans car with a painted purple-and-gold livery (in-world sponsors), parked at the castle as a teaser of the top classes.

**Resolved, not a bug:** the game was on screen 2, which was switched off. The title screen was simply waiting for input, which matches the log ending before `ADVENTURE_READY`. The small bug in the last bullet below (ambience at the title) still stands. The original note is kept below for the record.

**(Was) Open first next time: the game hung at launch (30 September, 19:06).** Richard's normal launch (title screen, 2560×1440 borderless fullscreen) stopped responding.
- **What the log shows.** Player.log ends at `LITTLE_ROAD_DISPLAY applied=FullScreenWindow size=2560x1440`, with no `ADVENTURE_READY` and no exceptions. After 39 minutes the process had used about 4 cores flat out (10,287 s of CPU). The audio thread kept playing the ambience.
- **What that rules in.** It hung either on the title screen or in the world build after Continue. The tests never ran that path at full length: `--title-review` quits after 3 s, and every other run skips the title.
- **A clue, and a small bug.** At the title the ambience runs with no world under it, so every sea-probe ray misses and it reports "all sea" (the loud waves). The title should mute it, or the ambience should wait for the world.
- **How to reproduce.** Launch with no flags at native fullscreen, then press Continue. If needed, add logging around `TitleScreen.Run`, `AdventureSession.Load`, and each setup step in `AdventureGame.Start` (the new parts: `SpeedTraps.Setup`, `Fireworks.Setup`, `TrainRace`, `PhotoMode`).

**Update, 30 September, late night: Claude's own additions.** Richard: "go to town adding whatever you like". Details in [doc 51](51-claudes-additions.md), on branch `claude-additions` (not committed yet).

- **Speed traps:** seven cameras, with star marks computed from each road and the parts ladder.
- **A horn** (H / L3): folk answer, traffic beeps back, and Bert's train whistles.
- **Beat the Train:** race the real train between the stations.
- **Photo mode** (P / D-pad Up): five looks, depth of field and a postcard frame.
- **Pip's photo requests:** six sights to photograph.
- **Fireworks** over Bayview harbour at night.
- **A RECORDS tab** in the pause menu for all of the above.

**Update, 30 September, night: Richard's fixes after the music round.** See [doc 50](50-title-saves-and-class-gating.md) and [doc 47](47-parts-drivetrain-and-sound.md) (last section).

1. **Engine top half.** Above a recording's top revs, the engine played the lift-off recording. It now stretches the pull and takes the extra brightness back out.
2. **Bayview Halt.** You couldn't get back onto the road from the station grass (0 of 32 test runs). It now has ramps (43 of 48; the rest ran into scenery). The mainline's descent from the tunnel now sits on a gentle embankment.
3. **Class gating.** Shops show only your class and below, and Class B stays shut until it has races.
4. **Title screen with three save slots.** Each slot has its own autosave and saved game; save from the pause menu.
5. **Valley activities: on hold.** Richard has a better idea to share next.

Open:
- One full-throttle start from rest stalled on the Halt's gravel ramp (it passes at part throttle).
- The 61–62 mph engine note.

**Update, 30 September, late: music.** There is a theme for each area (Bayview and Hillcrest) and a race version of each; the race version has a last-lap lift. See [doc 49](49-music.md). The work is on branch `music` and not committed yet. The next sound work is effects (tyre squeal and others) and the parked 61–62 mph engine note (club motor with the Street turbo).

**Update, 30 September, evening: parts for every class, real gears, manual shifting, engine sound.** Design and results in [doc 47](47-parts-drivetrain-and-sound.md). On branch `racing-ai-v2` (not committed yet).

1. **Parts ladder:** 129 parts across C, B, A, S and E, plus five platforms. Every engine is a real engine (layout, cylinders, revs, torque curve and sound fingerprint). All 29 older part ids are kept, so saves still load.
2. **Real gears:** `Drivetrain` solves each build's ratios so top gear reaches its top speed; `GearState` drives the car in a real gear with shift cuts, revs, boost and a limiter.
   - `DrivetrainChecks` passes all 322 engine × induction × gearbox combinations: each reaches top speed in top gear and uses every gear.
   - **Launch limit:** it caps net acceleration in every gear. A first-gear-only cap made second gear out-pull first, and the automatic box looped shift/snap on the line.
   - **Study 208** (gears against `--legacy-drivetrain`): the field is faster (Cedar about 9%), and all 24 finish in every race.
3. **Automatic or manual** (Settings > Gears):
   - **Pad:** R2/L2 shift, right stick up/down for throttle/brake.
   - **Keyboard:** Left Shift / Left Ctrl shift.
   - **Dash:** both dashes show the gear, AUTO/MANUAL, and a 16-lamp rev strip with a shift light.
   - A recovery puts the box back in first.
4. **Engine sound, synthesised live:**
   - firing pulses at the real crank angles through per-bank pipes and mufflers;
   - intake, turbo whistle, blow-off or flutter, anti-lag bangs, overrun pops, the limiter;
   - the hybrid motor, straight-cut whine and shift clunks;
   - tyres, surfaces, wind and impacts.

   The eight nearest cars have a voice, and there are volume settings. `tools/sound-harness.ps1` renders every engine's drive to `review/sound/*.wav` and checks its pitch against its firing frequency. The engines are measured, not yet listened to: **Richard's ears are the test.**
5. **AI fixes from the studies:**
   - **Boxed hold** (study 210): a boxed-in car sidesteps at most 2.5 m. It used to jump 10 m across the road, which put a car into Bayview's bridge.
   - **Pass reach guard** (study 209): no dive to a side the car can't reach before braking.

   Both met the keep rule, by margins near noise, and are now on by default (`--no-boxed-hold`, `--no-pass-reach-guard`).
6. **Still open (AI):**
   - At Bayview's bridge corner (progress 330–345), a car on a lane far off the racing line can get its target speed too late and run wide (study 210, seed 1709).
   - The physical recovery then reverses along the road for 15–30 s instead of turning round.

**Update, 1 October, evening: Richard's fourth listen.**
1. **Engine sound follows refits.** It used to change only after a loading screen.
2. **Turbo:** the whoosh swells only while boost builds, with a faint whine then; nearly silent at steady boost. Turbo cars are slightly muffled.
3. **Race start:** silent until the countdown, then a 1.5 s fade-in. Countdown beeps: three short, then a long higher GO.
4. **Birds:** real field recordings (forest and river beds, stereo), very faint and gone above about 12 m/s. Night crickets are synthesised individuals; the recorded chorus sounded like a marching band.
5. **Limiter:** a real 75 ms fuel cut that bounces.
6. **Automatic box:** no more 4th-5th hunting near top speed (the 60 mph stutter). It needs 1.2 s after an upshift, 85% rev room and 15% more pull before a downshift.
7. **Launch revs** climb with the car as the clutch slips; they used to sit flat until 10 mph.
8. **Shifts:** the pulling recording carries on through a gearshift, quieter, instead of cutting to the coasting one.
9. **Test loop** (`test-loop.ps1`): 2.5 miles with two 1-mile straights. Richard's club kit shifts cleanly to 5th at 55 mph and tops out at 67.6 mph.

**Update, 1 October, night: gearing v2 and the tuning shop** (doc 47 "Gearing v2", doc 48).
1. **Fixed ratios per gearbox** (km/h per 1,000 rpm in top gear). Top speed is the best gear's, where drive meets drag or the redline.
   - A weak engine never reaches the top-gear limiter; a strong one bounces on it and needs a longer box. For example, the Valley V6 with a club turbo reaches 106 km/h on the stock box and 125 km/h on the six-speed.
   - A long box on a weak engine tops out a gear down, and the automatic holds that gear on the limiter.
   - **Study 213:** all 24 finish with no resets, and pace is neutral.
2. **Tuning shop:** "Tune gearbox" at Mara's and Gio's garages.
   - **Tune:** the final drive (0.80–1.25) on every box, and each gear on the sport boxes and up. Auto-tune has three presets (acceleration, balanced, top speed).
   - **Screen:** GT-style gear chart, power and torque curve, and the car's numbers (top speed and its gear, 0–60, 0–100 km/h), with a verdict: on the limiter, over-geared or well matched.
   - **Test drive:** a timed lap of the town's circuit (lap time, top speed, 0–60), then back to the screen.
   - **Storage:** tunes save per gearbox; AI rivals race their box's defaults.
3. **Compile check:** it now leaves out engine modules the project hasn't enabled (Screen Capture passed the quick check once and failed the real build).

**Known issue (parked, Richard 1 Oct):** a strange engine note around 61–62 mph on the club motor with the Street turbo. It persists after the gearbox-hunting fix, and it's still to be diagnosed with `test-loop.ps1 -Capture`.

**Next:** the gearing model (fixed ratios per gearbox, where power decides whether the car reaches the limiter), then the tuning shop with test drives.

**Update, 1 October, afternoon: sound polish, a grounded class C, world ambience.**
1. **Engine sound, third listen fixes:**
   - **Loudness:** each recording is levelled to a smooth curve (pulling rises 2x from low to high revs, coasting at 65%). The Escort's pulls had run 19x quieter at low revs than at the top. Engine levels are raised and Master now defaults to 100%. A captured race now holds 0.18–0.23 RMS pulling across the revs, with no clipping.
   - **Lift-off:** the old sound was "like a bus's air brakes". The Street turbo is now recirculating (a soft, dark whoosh). Blow-off valves on the bigger turbos give a short "pssh" whose tone falls, at a quarter of the old level.
2. **Grounded class C** (doc 47, "Balance"):
   - Launch pull, turbo multipliers, frame weight savings and tyre grip are all compressed. Stock takes 8.4 s to 20 m/s, the pack 6.4 s, club kit 5.6 s, the pole car 4.4 s and the best class C kit 4.1 s (they were 5.9 / 3.3 / 2.2 / 1.6 / 1.4 s).
   - Top speeds run 89–126 km/h (were 89–170). Gears last 1.1–2.7 s (were 0.6–1.5).
   - **Study 212:** all 24 finish with no resets, fewer contacts and a much tighter pack (winner to last at Bayview about 10 s, was 13–18 s).
3. **World ambience** (`AmbienceSynth`, `Ambience`):
   - surf by the sea, its level from sampling the ground in a ring around the listener;
   - gusting wind, stronger high up;
   - the Silver Falls roar at the river's steepest drop;
   - river babble that follows the nearest stretch of the Silver river;
   - gulls near the sea and songbirds inland, quiet at night.

   `sound-harness.ps1 -Filter ambience` writes 20 s of each to `review/sound/ambience/`.

**Next (with Richard):** music, tyre squeal and the other effects.

**Update, 1 October, morning: Richard's sound notes, first pass.**
1. **Whine:** this was the turbo whistle. Richard's car had the Street turbo, and the whistle was a near-pure tone climbing to 8.8 kHz. It carried 14× the high-frequency energy of the engine itself (a layer audit with `sound-harness.ps1 -Filter layers:<engine>+<turbo>`). The turbo is now mostly a spool whoosh with a faint whistle under 3.5 kHz, and the energy above 4 kHz is down about 7×.
2. **Reverse:**
   - **Manual:** reverse is a gear, as in GT7 and real cars. Down from first at a standstill selects R; in R the throttle drives backwards and the brake brakes. In a forward gear the brake holds the car at a stop.
   - **All modes:** the forward pedal now brakes a car that is rolling backwards. It used to do nothing, which is also why AI recoveries reversed for 15–30 s.
   - **Study 211** (`--legacy-reverse` A/B): identical except the one race with a recovery, Bayview 1709. Its last car finishes at 42.2 s instead of 74.6 s and the total is 32 s better. Kept.
3. **Listening test:**
   - **In the game:** Settings > Engine sound test, or `--sound-bench <engine id>`. Pick any engine and induction; Cross / Space runs the test (idle, pull to max revs, hold on the limiter, lift back to idle); R2 / F revs it freely.
   - **Harness:** `sound-harness.ps1` writes the same test for every engine to `review/sound/`.
4. **Realism: done with real recordings (1 October, midday).**
   - **Downloads:** 15 engine recordings (about 1.2 GB) from Sonniss' official GDC 2021-2023 zips. `tools/fetch-sonniss.py` reads each zip's index with range requests and pulls single files. They sit in `../little-road-audio-sources/`, outside the repo.
   - **Banks:** `tools/engine-grains.py --config tools/engine-banks.json` turns them into 12 grain banks, about 30 MB, in `unity/Assets/Resources/Engines`. Check images for each recording are in `review/grains/`.
   - **Rev tracker:**
     - harmonic sum plus a Viterbi path;
     - quiet stretches (engine off) are untrusted;
     - on the V8, V10 and flat-12 it follows the bank pulse line (`trackCylinders`).
   - **Mapping:** each part names its recording (`parts.json` `sound`). Class C is now five different cars: Citroen C2, Golf, Escort RS1800, S2000 and Skyline. S and E rev past every road recording (stretched 1.6-2.6× at the redline), so they need race recordings later.
   - **In the game:** the recording replaces the modelled exhaust; the part's exhaust type tones it (road, sport, race); the turbo, blow-off, shift and hybrid layers stay synthesised. `--synth-engines` brings back the old model for comparison.
   - **Checks:** `SoundChecks` loads and plays every bank. `sound-harness.ps1 -Filter recorded` writes every engine's listening test to `review/sound/recorded/`.
   - **Richard's second listen (1 October):** much better, and coasting sounds natural. Revving up "flickered between two rev noises at once" and sounded distorted. Four causes, all fixed:
     1. **Two engines at once.** At part throttle, pulling and coasting grains came from two separate streams, played together. There is now one stream, switching sweeps with hysteresis; part load shapes its level and brightness.
     2. **Crossfade overlap.** Every cycle join overlapped two different stretches of audio. Grains now carry a tail (the recorded start of the next cycle), so a join to the recorded successor reproduces the recording.
     3. **Scattered cycles.** Banks were thinned to scattered cycles per 50 rpm. They are now whole sweeps (version 3 format), and playback steps through consecutive cycles.
     4. **Distortion.** The player's ×1.7 boost sat before the soft limiter. It is now the speaker volume.
   - **`--audio-capture <file.wav>`** records the player's engine as synthesised, with revs, gear and throttle every 10 ms, to show exactly what was heard. It found cause 1, because the throttle is mostly partial while pulling.
   - **Lesson:** batch-mode checks run without sound. After any sound change, run a windowed race with sound before handing over. A setup crash (volume set before the source existed) gave Richard a black screen.
5. **Earlier plan for the realism work (superseded by item 4): granular synthesis from real recordings.** This is the racing-game standard.
   - `tools/engine-grains.py` tracks the revs through a recording and cuts one-cycle grains into a bank.
   - `GrainSynth` plays the bank at any revs. It replaces the modelled exhaust; the turbo, shift and hybrid layers stay procedural.
   - Tested end to end on a synthetic source (`sound-harness.ps1 -Filter grains:<bank>:<engine>`).
   - **Source:** the free Sonniss GDC archive (royalty-free, no attribution, no AI training) has Pole Position Production car recordings. The Citroen C2, Golf, Escort RS1800, Honda S2000, Skyline R33, Skoda Fabia R5, Audi RS4 V8, Viper V10, Testarossa, Kia Niro Hybrid and sprint cars cover the ladder.
   - **Needs Richard to download them.**
5. **Parts sounding alike:** with recordings, each engine step maps to a different car's engine.

**Richard's first listen (30 September, evening). Fix these before any more sound work:**
1. **A very high-pitched noise rises with acceleration.** It's piercing (it bothered the dog).
   - **Suspects:**
     - the turbo whistle (1.6–8.8 kHz with boost);
     - the intake hiss (it grows with revs squared);
     - the valve-train tick (high-passed at 3.5 kHz);
     - pipe-comb resonances;
     - aliasing from very short pulses at high revs.
   - **Next step:** solo each layer in the harness and measure the energy above 4 kHz.
2. **The first three C engines (stock, workshop, club) sound the same** when swapped in the save. The differences need to be clearly audible: displacement, exhaust, cams, intake.
3. **They don't sound like engines:** "more like a PS1/PS2-era kart game". The sound needs to be guttural and real.
   - **Next step:** research better approaches:
     - a physically modelled exhaust (waveguides, as in engine-sim);
     - granular or loop-based synthesis from CC0 engine recordings (Richard approves downloads);
     - hybrid layering.

   Rethink the model rather than tweak it.
4. **Manual gears into reverse:** with the brake and the downshift on the same trigger (L2), you can't brake without shifting. Study how GT7 handles reverse and braking on a pad and wheel, and copy it.
5. **Wanted: a listening test.** Pick any engine and hear it rev from low to high, hold a few seconds at max revs, then come back down, so Richard can sense-check engines as they change. It should run in-game and play through the game's own audio, plus the harness WAVs.

**Next (after the fixes above):** ambience, UI sounds and the location music (doc 47 stage 4), then the parts screen (stage 5), then map expansion.

**Update, 30 September: Racing AI v2, clean-up, Bayview and Silver Falls.** All on branch `racing-ai-v2` (not merged).

1. **Racing AI v2** (`RacerDriverPolicy`; research, design and every study in [doc 46](46-racing-ai-v2-design.md)) is the default driver. `--first-ai` races the first AI.
   - Beat the first AI in the keep/reject study (205): all 24 finish, no resets, about 20 s better per race.
   - After the ability spread was widened it's about level (207), because the roster's weaker drivers are now genuinely slower.
   - Abilities: all-worst is 49% slower than all-best on a solo lap; each ability counts.
2. **Race preview:** DR (driver rating 1–20) beside PP on every grid tile and a DR rosette. Hover it, or hold Triangle / R, for the radar.
3. **Clean-up:** the Godot prototype, the Cowork runner, 19 obsolete tools, 17 superseded docs and old evidence are gone (all in git history). `review/` went from 7.8 GB to about 0.3 GB. `cleanup.ps1` keeps it that way.
4. **Bayview:**
   - a seaside high street of terraces with bunting and café parasols;
   - meadow grass on the verges and green;
   - olives, blossom and poplars on the hills;
   - lavender and vine plots.
5. **Water:** sea contact foam at walls, piers, hulls and rocks. **Silver Falls** rebuilt as a real cascade (`LittleRoad/Waterfall`); the river no longer overhangs the gorge.
6. **Parallel races:** `tools/run-pace-parallel.ps1` runs six at once. First-AI races run alone because its worker threads aren't repeatable under load.

**Next** (Richard's order):
- AI polish: club cars in Cedar's crowded back half; fewer light side-by-side touches; a calm two-car defend test to watch.
- Then the world expansion and the parts expansion (doc 45 Phase 0 onwards, doc 44).

**Update, 29 September, night: seven fixes before the parts doc.**

1. **Back from a race preview** now returns to whoever offered the race: Mara and Gio keep "Fit parts".
2. **Look behind:** both views are fixed, and flipping back restores the forward camera exactly where it sat.
   - **Why it zoomed:** the forward camera trails the car. On flipping back it snapped to the untrailed spot, then drifted back out, which read as a zoom.
   - **Also fixed:** the cut now skips race motion blur and the anti-aliasing history.
3. **Starting grid:** the pole car's nose is on the start line. Rows of three are 11 m apart (about 7 m clear) with a 1.8 m stagger; before, the front row was 8 m back and rows were 5.5 m apart.
   - **Baselines:** new ones are in `review/unity-35/frame-cost-128` (Cedar and Bayview, seeds 1709–1711) and `review/class-sort/grid29-*`. Earlier baselines are superseded and have been kept, not deleted.
4. **Map sea:** coloured by distance from the coast (surf, reef turquoise with patchy reef, then deep blue), from a land mask of 512² downward rays. `WorldMap.Reef`.
5. **Fog:** opaque, and only where you've been. No free Bayview or whole-town reveal; viewpoints still reveal what you can see from them. Masks saved before this (v1) are discarded.
6. **Blur:** the race motion blur is now camera-and-objects, so your own car stays sharp. At night, lamps leave short light trails above about 22 mph (`LightTrails`, `LittleRoad/LightTrail`).
7. **Collision boxes** are fitted to each body: the model's length, and its shoulder-and-tyre width without mirrors (`CarBodies.ShoulderWidth` / `Length`, measured in Blender).
   - **Before:** one shared box forced to 2.32 m wide left 27 cm of air each side of the Puddle, while the longer field cars sank up to 54 cm into each other.
   - **Now:** 1 cm nose-to-tail and 2 cm at the shoulders at contact.
   - **Check it:** `--contact-review` (Richard's test) shoots pairs of cars from above. See `review/map-sweep/contact-old` and `contact-perbody-2`.

Smoke21 passes.

**Update, 29 September, evening: Richard's eight general improvements.**

1. **Fog of war** on the minimap, full map and pause map. It reveals 175 m around the car, 520–620 m at viewpoints and the castle, and whole towns on arrival. It's saved with the adventure. Details are in `docs/ui-design.md`.
2. **Parts tiers plan:** `docs/44-parts-tiers-and-progression.md`.
   - **Classes:** C Street, B Tuner, A Touring, S Racing and E Endurance.
   - **Ladder:** three steps per slot per class plus sidegrades, about 125 parts, with names and a look for each class.
   - **Economy:** about 7.8 h of racing to reach podium builds, and 20 events in 6 regions.
   - **Waiting on:** Richard's four decisions.
3. **Places:** a one-time DISCOVERED card, then the name under the minimap. Compact toasts replace the stamped ones.
4. **Race stutter:** races now pace to the display (72 fps on 144 Hz). Only 1% of frames miss the refresh, down from 22%. See `review/unity-35/frame-cost-127-review.md`.
5. **Parts screen overlaps:** fixed.
6. **Talk prompt:** half the size.
7. **Controls:** no button hints while driving. There's a CONTROLS tab in the pause menu instead.
8. **Look behind:** the camera is locked ahead of the car.

The parts review (34 checks) and smoke20 pass.

**Update, 29 September, afternoon: Richard's six feedback items are done.** Details for the UI are in `docs/ui-design.md` ("As built").

1. **Unreadable loading text.** The loading-screen heading was navy text on a navy outline; it's now brand orange. The result-screen coin mark is a plain label.
2. **Cars clipping into the world.** `StationarySpot` slides each standing car to the nearest clear spot, and townsfolk with no clear spot are dropped. `--car-audit` now finds 1 of 108 (Orla at (275, −927) against a kit `Collision_Body`; the audit now names the parent object, so the next run will say which).
3. **Staring cars.** Parked cars no longer turn to face you.
4. **UI overhaul:**
   - a new HUD: speed dash, a timed objective and a contextual controls hint;
   - the pause hub: MAP (regions, hover counts, full map), COLLECTION, CAR and SETTINGS tabs;
   - back goes one level;
   - glyph-aware prompts in the shops.

   Captures are in `review/map-sweep/pause-2`.
5. **Road-end lips.** Gravel end ramps are added, but skipped where a road runs straight onto a through road: the one at the gate junction stood in the junction.
6. **Castle Climb event** (`CastleClimb.cs`):
   - **Route:** bollards line the route, and every side road gets a close-set line 7 m beyond it. Sharp junction corners are left open, because an edge line there trapped cars that ran wide.
   - **Crowd:** parked cars, townsfolk and valley traffic move behind the bollards. Everyone within 40 m bounces and cheers ("Go go go!").
   - **Zipline:** closed during the climb.
   - **Hard mode:** par 80 s instead of 85, 750 credits instead of 300, and 20 obstacles, each a hay bale across one lane with cones leading into it. It has its own badge.
   - **Evidence:** the autopilot on Via Crest takes 49.1 s with the closure, identical to the run without it and with zero contacts (`review/map-sweep/climb-crest-*-11`). Hard mode arrives in 81.8 s even with the autopilot's clumsy dodging.
   - **Known limit:** this autopilot overshoots the quay-to-Via Crest hairpin from a standing start, with or without the closure. `--climb-from 112` tests past it.
   - **Captures:** `review/map-sweep/climb-3`.

**Parts browser, all categories (later on 29 September).**
- **Browser:** the 3D browser now handles all seven categories. The category stickers, LB/RB or [ / ] switch in place.
- **Models:** every part has a Blender model (`source-art/build_part_previews.py`), with shape differences between tiers. That includes the Valley V6, which no longer borrows the Club motor's model.
- **Stats:** each category compares what it actually changes, such as stopping distances for brakes, road and gravel grip for tyres, and weight for chassis.
- **Locked parts:** they now show in the carousel as "CLASS X / LOCKED".
- **Checks:** the parts review (`review/ui-screens/all-parts-1`, 29 checks) and smoke18 pass.

Smoke17 passes. The build runs 1,060 editor checks, including the new `InterfaceChecks`.

**Still to check by hand:**
- the pause hub with a real pad;
- the Hard par, which is set from autopilot times rather than a human run;
- whether 4.5 m bollard spacing reads as "closed" enough on the open valley stretches.


**Update, 29 September.** Items 1–6 are done:
- race warm-up;
- benchmark ignores its screenshots;
- raceway rubber;
- sea rocks seated;
- causeway end already framed by the Headland Gate, so nothing added there;
- scorecard third pass.

Benchmark db11: worst frame 19 ms, none over 33 ms.

**Item 8, the minimap, is done** (`Minimap.cs`, `LittleRoad/Minimap`, `WorldMap.Bake`):
- **Map image:** the world is baked from above at a fixed afternoon behind the loading card.
- **Corner map:** bottom-right, heading-up, and zooms out with speed. Marks:
  - residents in their colours;
  - Pit Stops (orange squares);
  - the zipline (yellow diamonds);
  - the objective (a pulsing star that waits on the rim when off the map);
  - a north badge on the rim.
- **Full-screen map:** M or the View/Select button opens it north-up with the atlas names. Names are placed by priority with overlaps dropped, and the car waits while it's open.
- **Checking it:** `--minimap-review` captures the corner map in three places plus the full map (see `review/map-sweep/minimap-2`). `--no-minimap` turns it off.

Item 7 (shadow budget) is left for the 10× world: frames are at 5.6 ms today.

**Item 9 is done.**
- **Study 126** (`--rejoin-at-speed`) is **rejected** at the screen: on Bayview, 22/24 finished, with 2 assisted resets and 10× the contacts. See `review/unity-35/frame-cost-126-review.md`.
- **Class-sort baseline:** Cedar sorts cleanly, but Hillcrest leaves 2 street cars stuck behind stock cars. See `review/class-sort/base-review.md`.

The next AI step is the Hillcrest passing investigation. Windowed tests launch on monitor 1 (`-monitor 1`). The remaining Bayview bush at (−93, −1) is cosmetic.

Where the graphics and world work stopped, and what I intended to do next. Nothing is committed yet. All of today's work is in the working tree, and the dev build in `../little-road-dev-build/` is current.

## State at the pause

- **Last benchmark (db10).**
  - Uncapped, the drive averages 5.2 ms, with p99 at 9.4 ms.
  - The five frames over 33 ms are all the benchmark's own 20-second screenshots.
  - The 400–700 ms causeway stalls from db9 are gone. They were shader "fade" variants compiling on the first LOD cross-fade; the loading warm-up now forces cross-fades in every view to compile them.
- **Smoke test (smoke13).** Passes with every change so far: quest, 24-car race and payout, reload, retry and podium renders.
- **Research plan.** All seven items in `docs/visual-quality-audit.md` are in. Since then I've added:
  - the framed tunnel exit;
  - camera shake on impacts;
  - flags and banners in the wind;
  - warmer, darker hill rock;
  - the kit asphalt on the Bayview circuit;
  - a rubbered-in racing line.

## Next, in the order I intended

1. **Warm up fade variants for races too.** `RaceTrial` has no warm-up, so the first cross-fades in a race (props, trees, cars) can hitch the same way the causeway did. Run the same forced-fade pass behind the race loading card, then check with a race frame probe.
2. **Stop the benchmark stills from counting as hitches.** Skip the frame after each capture in `FrameProbe`, so reports show only real stalls.
3. **Racing-line rubber on the Hillcrest Raceway.** Only the Bayview circuit has it. The raceway is built from `HillcrestRoute.Circuit`, a different road builder, so it needs its own strip builder.
4. **Grounding.** Fix the few real placement faults from `PlacementAudit`:
   - the Bayview bush at x −93, z −1;
   - the surf rocks below the headland cliffs.

   Then add a soft dark footing where large kit pieces meet terrain; SSAO alone is thin at a distance.
5. **Framed arrival at the causeway's mainland end.** This is the other arrival moment the audit named. The Headland Gate already frames it from the sea side; from the valley side it needs trees or a landform.
6. **Scorecard update.** Add a third-pass section to `docs/graphics-scorecard.md` covering today's work (body motion, rim and bounce light, fades, terraced hill, flags, road surface, performance), with before/after captures.
7. **Rendering scale-up.** Next in `docs/rendering-and-streaming.md`:
   - a shadow-caster budget (small casters stop casting shadows beyond about 60 m);
   - then GPU occlusion culling (DOTS instancing in the custom shaders);
   - then sector streaming for the 10× world.
8. **Free-roam minimap (Richard's request).** A detailed minimap in adventure mode while free roaming.
   - **Map image.** Build it from the same top-down world render as the atlas (`WorldMap`, `--world-map`), baked at load or shipped as a texture. It will need re-baking when the world changes.
   - **Layout.** A round map in a corner that rotates with the car (or north-up, as an option), with a player arrow.
   - **What it shows.**
     - roads and place names;
     - Pit Stops, residents and the quest objective, each with its own icon;
     - the zipline stations;
     - collectibles only once found (or never, so they stay a discovery).
   - **Behaviour.** Zoom widens with speed. A button opens a full-screen map that uses the atlas labels.
   - **Scaling.** Must hold up in the 10× world: the map texture streams by sector along with the world (see step 7).
9. **Still open from earlier.** AI racing study 126 (`--rejoin-at-speed`) and the class-sort run. Both should start with a fresh baseline, because the trial field changed when the placeholder bodies were removed.

## How to pick up

- Build: `.\tools\build-dev.ps1`
- Check views: `.\tools\capture-map.ps1 -Name <run> -Rendered -Flags '--valley-views'`
- Measure: `.\tools\drive-benchmark.ps1 -Name <run>` (uncapped, no traffic)
- Regression check: `.\tools\capture-adventure.ps1 -Name <run>`
- Place names: the pinned Little Road Atlas (`--world-map` capture, published at https://claude.ai/artifact/KqqihWNLFv8AUXV1Rg4UWp)
