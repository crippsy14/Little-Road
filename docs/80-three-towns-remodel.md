# 80. The three towns remodelled: the programme (19 October 2026)

Richard, 19 Oct: "work on this continually with an agent helping you through, until we've got to the point of the whole
three towns being redone with new assets, no clipping etc, based off of the prop engine. If you get that done, generate
a plan for the whole map, with an ordered list of towns, and start working through that."

## How each round runs
1. **Plan:** a short list of work, written here before the build starts.
2. **Build:** behind the existing flags.
   - `--new-bayview` implies `--prop-forge` from round 1.
   - The world without flags keeps its fingerprint (`3f164ae90677e486`).
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
