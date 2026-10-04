# 62 · Street review fixes, the working quarry, and the rally licence (4 October)

Richard asked for four things on 4 October:
- Review every street in every town from photos. Sub-agents reported; the fixing stayed in the main session.
- Raise material fidelity in the quarry, where the floor "looked like a kitchen floor's repeating tiles".
- Add standing rocks and working machines that watch the race when you race.
- Make the rally licence work like Gran Turismo's.

## The street review

**Photos.**
- `--street-review` (in `VillageReview`) photographs every town street every 40 m: a driver's-eye view ahead and both frontages.
- Output goes to `review/quarry-hills/streets-1/`: 408 photos across 13 streets, with `streets.json` giving each camera.

**Reviewers.**
- Seven reviewers each took a group of streets.
- Richard paused them part-way to save usage. Their findings, and where to continue, are in `review/street-review/*.md` and its `README.md`.

**Fixed.** The finding ids are the reviewers'.

| Finding | Fix |
|---|---|
| Residents' cars on pavements, through lamps and benches, into doorways (QH-HS-01/02/03, QR-1/2/6, QL-8, VC-1/2, HCL-5) | The front-garden spot sat inside the house's collider (Quarry Hills) or across the pavement (Hillcrest), so settling pushed cars anywhere within 14 m. A home car now stands in the middle of its garden and may only slide 1.2 m along the frontage. It must be 1 m clear of lamps, benches, bushes and fences, or it's left out. Each car stands on a slate (or stone) hard-standing. |
| Shop names behind the balcony railings (VC-7, HCL-3, QH-HS-04, QR-7, QL-9, CH-3) | Shop terraces (`terrace_b`) have no balcony now. The sign board sits clear above the awning. |
| "SIOP" read as STOP | The square's corner shop is SIOP Y GORNEL; the street grocer is GROSER. |
| Black arched loggias with floating flowers (QH-HS-05, CH-4) | A stone back wall with glazed doors in each bay, and terracotta pots under the flowers (`house_d`, `house_c`). |
| The same front twice in a row | No two of the same design side by side, in both towns. |
| Lamps doubled 2–4 m apart (QR-5, QH-HS-06) | No lamp within 6 m of another. Bunting snaps to the nearest lamp within 6 m. |
| Grass in the Pit Stop's bays and on the square (QR-3, QL-13), and in the road out of Bayview (BV-1) | Verge tufts and meadow grass stay off Quarry Hills' plots, square and Pit Stop, and off the mainline's first stretch. |
| Sunk bench ("beams on the paving") and the map board lying face-up (QL-12/13) | Square furniture stands on whichever is higher, the paving or the ground. |
| Retaining-wall and field-wall stubs (CH-1, HR-1, KF-1) | A retaining wall needs 6 m of points, and one missing point doesn't break it. Field walls are laid in runs of 8 m or more with an end pier. A gate only follows a real wall. |
| A wall halfway up a cutting bank (QL-3) | Field walls only on ground that's level across the wall's line, within 1.6 m of the road. |
| Gate boards blank from behind (QL-4, HCL-6) | The town gate has its name on both faces. |
| Houses hanging over banks (QL-1, BV-2) | Plinths are sized from a 5 × 5 grid under the footprint, not just its corners. The Bayview farmhouse and barn get stone plinths. |
| Farm tracks draped down road cuttings (BV-3/4) | Each track starts after the last step steeper than 30%. |
| Guardrail along the kerb in front of house doors (HCL-1) | Once the towns are built, rail pieces with a building within 9 m behind them are removed (10 this build). |
| A bush standing in the river (HCL-2) | Bushes and tufts stay out of the river. |
| The Pit Stop has no way in (QR-4) | A 16 m paved apron from Quarry Road to the forecourt, with the pavement broken across it. |
| Square flags not Welsh (QR-12) | Y Ddraig Goch either side of the square's centre pole (`flag_wales`). |
| Quarry Hills reads as Ligurian (CH-5, QL-10) | Three quarrymen's cottages: grey rubble stone, slate roofs, small white sashes, a porch, a bay and dormer (`cottage_a/b/c`). They make up most houses away from the square and a quarter in the middle. |
| Every front garden is the same lawn (QH-HS-14, CH-6) | Gardens without a car get a fence of upright slate slabs (crawiau, as in the slate valleys) with a gap for the gate. |

**Not done yet:**
- **Bayview:** the sign in the rock (BV-5) and the wall gap (BV-6).
- **Via Crest:** the castle gate lip (VC-3), the fork-island sign and kerb (VC-4/5), and the planter against a window (VC-6).
- **Hillcrest:** the Pit Stop's two waiting cars and disc signs (HCL-4), the zipline plaza (HCL-7) and the café chairs (HCL-8).
- **Quarry Hills:**
  - stray step slabs, doors above walls and pillar stubs (QH-HS-07–10);
  - pavement ends (QL-11, QR-10);
  - awning height (QR-8);
  - a bench at a door (HR-2);
  - the gallery's stretched end (HR-5).
- **Improvements:**
  - gardens on the bare lawns (the memorial garden found no free plot once the cottages filled the lawn by the square);
  - town-edge signs;
  - quay dressing;
  - the Bayview lay-by.

## The quarry: fidelity

- **The floor (Kit shader, split slate's flat tops).** The 45 cm grid of random squares is gone. It's now:
  - compacted gravel speckle at two sizes, with a scatter of loose stones;
  - outlines of broken bedrock slabs, only in a few patches;
  - drifts of pale dust and damp dark hollows that shine a little.

  Per-chip colour fades once a chip is under a few pixels, so it doesn't shimmer at range.
- **The rallycross.** It was a flat cream strip. It's now darker slate gravel, finer, with two polished wheel ruts and loose stones on the berms. The ruts come from the strip's across-coordinate in its second UV.
- **The floor's edge.** Ragged bays and spurs at two scales, instead of a smooth painted line.
- **The galleries.**
  - Split slate keeps its beds at any range (the cheap far path made one flat dark band).
  - They now show broad paler faces and dark wet streaks down from each bench.
  - The gallery slate is a little lighter.
- **Standing rocks** (`rock_stand_a`–`d`):
  - 63 placed round the floor's edge: slate monoliths, leaning slab pairs, split boulders and boulder clusters, with rubble at their feet;
  - a ring of nine standing stones on the moor by the Kettle Falls road.
- **Spoil heaps.** Three rubble heaps on the floor. You can drive up them.

## The working quarry (`QuarryWorks`)

**In free roam.** In the floor's north-east corner, clear of the circuit and the rally school:
- **The excavator** (`excavator_base` and `excavator_top`) digs at the heap and slews round to load. Its arm is a hinged part.
- **Two articulated dump trucks** (`haul_truck`) run a circuit:
  1. load under the excavator;
  2. drive out to the tip and reverse up to it;
  3. tip (the body lifts on its rear hinge);
  4. come back round.

  Only one truck at a time uses the loading bay or the tip.
- **A wheel loader** (`wheel_loader`) shuttles between a stockpile and the slate stacks, lifting and dumping its bucket.

**In a quarry race** (`RoadCar.LooseRace`), the trucks and the loader park side by side behind the start straight, facing the track, 28 m off it. The excavator turns to watch with its arm raised.

**Review flags.** `--works-pose` and `--works-raceday` freeze them for stills.

## The rally licence (`LicenceScreen`)

- **Opening it.** "Rally licence" in Gwen's menu opens the licence screen:
  - **The test list:** the six tests down the left, each with its medal chip.
  - **The card:** the focused test's brief, its gold, silver and bronze times with their prizes, and your best.
  - **Navigation:** pick a test to start it, or Exit licences (circle / Esc), which puts the car back beside Gwen.
- **After every attempt,** a result card shows "Test cleared!", "No medal" or "Test failed". It gives the medal, the time, cone penalties, credits and new bests. It offers Try again (cross), Next test (square) and Back to licences (circle).
- **The test.** `--lesson-test` drives the whole flow:
  - Gwen, the licence screen, test 1;
  - then Next test from each result card;
  - then Back to licences and Exit.

  It passes (`screen=True started=True resultCards=6 back=True exitToGwen=True`, all gold).

## Round two (4 October, afternoon)

**Richard's five requests**
- **Pause menu in races.**
  - The cause: Restart threw "Race entry changed". The entered roster was seeded for the race number before the race began, so it no longer matched the save.
  - The fix: Restart now builds a fresh roster for the same event, seeded for the next number (`AdventureSession.RestartRoster`).
  - The failed restart also left the menu up with the race unpaused, so Resume did nothing. If a restart can't start now, the race simply carries on.
  - The race pause review checks that Restart can enter the same event.
- **Quarry Hills garage.** Huw's garage offers the Rallycross, as Mara's and Gio's offer their races.
- **Lift-off sound.**
  - Lifting off now plays the pulling recording stepped back down through the revs, eased to about −2 dB and a little darker (`GrainSynth.ReverseLift`). It replaces the coasting recordings, which were all off by a lot.
  - `--coast-recording` plays the old coasting sound, for A/B.
  - The sound check now tests the new ease-down. The sound smoke reads 6835 (6890 before).
- **Pit Stop vehicles.** There are three new models, each in the Pit Stop's colours (`source-art/family_recipes.py`):
  - the garage's recovery truck (`pitgarage`): a tow crane, hook, wheel-lift and roof lights;
  - the parts van (`pitparts`): tyres stacked on its roof rack;
  - the body shop's panel truck (`pitbody`): a rack of coloured body panels.

  Each carries a door badge: a spanner, a tyre or a paint drop. Every town's garage, parts and body-shop host drives the matching one, and the hosts keep their local names.
- **Race loading (`RaceArea`).**
  - The area: the Hillcrest and Quarry Hills races build only the world around their circuit (its bounding circle plus 450 m).
  - Always built: the terrain, and Hillcrest's raceway buildings.
  - Left out unless inside the area: the towns, the railway, woods and farms, the quay, and Bayview island with its village and loop.
  - The leash: a car more than 160 m off the circuit is put back on it.
  - A/B: `--race-full-world` builds everything, as before.

  | Race (world build) | Before | Area |
  |---|---|---|
  | Quarry Hills Rallycross | 12.8 s | 6.4 s |
  | Hillcrest Raceway | 14.1 s | 9.3 s (measured with the regression running alongside) |

  Every race finished 24 of 24. Returning to free roam still rebuilds the whole world; keeping the world across races (doc 61) is the next big win.

**More street fixes**
- **Awnings (QR-8).** Shop awnings are raised clear of the door heads (the shop terrace and the shop, café and post office).
- **Benches (HR-2).** Benches snap to the party wall between two houses, so none stands in front of a door.
- **Planter (VC-6).** The house planter is moved clear of the window's flower box.
- **Café (HCL-8).** The café's outside tables are pulled 1.1 m closer to the building.
- **Zipline plaza (HCL-7).** The flower beds are clear of the gateway's piers, and the picnic tables are away from the kiosk.
- **Pavement ends (QR-10, QL-11, HCL-10, QH-HS-11).** A pavement's own end, away from junctions and the square, finishes at a stone post.
- **Plinths (QH-HS-08/09).** A house on a high plinth gets steps up to its door from the garden.
- **Square edge (QH-HS-10, HR-1).** No yard walls within 36 m of the Quarry Hills square, so the lone wall block there is gone.
- **Fork-island sign (VC-4).** No change: the reviewer saw the plain grey back of a correct give-way sign.

**Round three (same afternoon)**
- **Garden-front wall.** The stone wall across a Quarry Hills front garden, in front of the doors, was a junction corner wall from the road builder.
  - Junction corner walls are now combined after the towns stand, and any piece within 3 m of a building is dropped (`TrimRails`, now run in every build).
  - Back-yard walls keep 9 m from any road's centreline, so one street's yards no longer stand in the next street's front gardens.
  - Retaining walls keep 4 m from buildings.
- **Gardens.** Public gardens on bare lawns in Hillcrest (gravel paths, flower beds, benches, cypresses, a small fountain). Two of three were placed: Station Road and the river corner. The castle lawn is too close to the castle approach roads.
- **Gallery faces (HR-5).** Split slate takes a third, top-down projection on leaning faces, so the galleries' curved end no longer streaks.
- **Test runs ignore real controllers** (`TestInputIsolation`). In `--silent`, batch-mode, `--trial-auto` and any `-test`, `-review` or `-audit` run, the real gamepads, keyboards and mice are disabled. A player's controller is read by every running copy, focus or not. The tests' virtual pads still work.
- **View shots.** They accept a bare "g" for ground level, and wait in real time.
- **Lesson test.** It carries the car through the flick at 15 m/s; at 14 m/s it landed exactly on gold's 10.5 s and read silver under load.

**Still open**
- **Castle gate lip (VC-3):** not reproduced in a close-up.
- **Bayview's sign in the rock and wall gap (BV-5/6):** the close-up tool takes its heights from the mainland, so these weren't checked.
- **The gallery's stretched end (HR-5).**
- **The improvements list:** gardens on lawns, town-edge signs, quay dressing and the Bayview lay-by.
