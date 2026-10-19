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
- **Round 2:** the terrace generator (car-forge `terraces` kit) is drafted: seven families, four variants each, rows
  that join up, a sign socket per house.
