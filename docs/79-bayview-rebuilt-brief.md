# 79. Bayview, rebuilt: the brief (19 October 2026)

**For Richard: approve, edit or veto.** Nothing is built until you say yes (doc 73).

Bayview was the prototype, and the rest of the game has moved on past it. It's also the first place a player sees.
Richard, 17 October: "a complete refresh, in Claude's own taste … nothing is kept". Only the idea stays: **a small
island, the tutorial area, where the story starts**, joined to Hillcrest by the causeway (doc 58: at the origin, with
Coral Key on the horizon to the north-west). Prop Forge (docs 77-78) is what makes it possible: walls, fences,
hedges, trees, houses and street life now come from recipes, not hand placement.

## The player's experience
1. You arrive at the harbour.
   - It's a curve of houses in bold colours, each one different, climbing the hill behind a working harbour.
   - Boats sit at the quay, creels are stacked by the slipway, and the clock tower stands on the front.
2. Every road teaches one thing.
   - A straight past the Pit Stop, then a hairpin, then esses climbing through town to the windmill.
   - Then the fast north cliffs, and the lighthouse headland back down to the harbour.
   - You can always see the lighthouse or the windmill, so you never get lost.
3. The way off the island, the causeway to Hillcrest, is the first big reveal, and the story can use it.

## References
Research is in `review/bayview-references.md`, with sources.
- **The town:**
  - Tobermory: one curved, painted harbour front with the hill behind, and a clock tower.
  - St Ives: a curving pier with a lighthouse on the end, and a lifeboat slipway.
  - Mousehole: granite harbour arms with a narrow mouth.
  - Portmeirion and Portofino: colour, loggias, a town stepping up a slope.
- **The game:**
  - Zelda's Great Plateau: a contained start with a memorable exit.
  - Disney's "weenie": one landmark you can see from everywhere.
  - Mario Kart's Luigi Circuit: a short, hazard-free first track.
  - Forza Horizon: learn by driving, with little text.

## The plan (v1)
Coordinates and lengths come from `tools/bayview_plan.py`, the same data the build will start from.

![Bayview plan v1](../review/bayview-plan/plan.png)

- **The island:** about 430 × 300 m, grown north and west from today's 310 × 240 m.
  - The south-east corner stays exactly where the mainland expects it: the causeway's foot at (68, −83), the Halt
    quay and the railway loop.
  - The harbour is a bay cut into the south shore, closed by two stone arms with an 11-12 m mouth (Mousehole).
- **The Harbour Loop** (the club track, classes C and B; doc 58 says 1.1 km): **1,027 m, tightest bend 12 m radius.**
  Its lessons, in order:
  1. the start straight past the Pit Stop (speed and braking);
  2. the long right up the east coast past the beach huts (a constant-radius corner);
  3. the hairpin;
  4. the esses climbing through town (line choice, a crest);
  5. the windmill;
  6. the north cliffs (fast sweepers, a drop on the outside);
  7. the lighthouse headland, a tightening corner;
  8. the curving harbour front, the colour wall, to the line.
- **The gravel lane** (366 m), round the hill farm: the first rally stage, and a shortcut once it's unlocked.
- **The Bayview Grand Prix** (3.4 km, classes A, S and E, doc 58): the loop plus the causeway and the valley approach,
  with a pit lane on the quay. It's laid out after the loop, from the same data.
- **Landmarks, one in each direction:**
  - the lighthouse on the west point;
  - the windmill on the summit;
  - the clock tower on the harbour front;
  - a pier with a helter-skelter off the east beach;
  - a lifeboat slipway on the west arm.
- **The town:** colour-wall houses along the harbour front, stepping up the hill. They come from a new Prop Forge
  terrace generator, so each house is different but a row still joins up.
- **Also from Prop Forge:**
  - harbour walls and railings drawn along the water;
  - dry-stone and hedge field walls on the farm;
  - street life by rule (doc 78).

## How it's built
Behind `--new-bayview`. The old island stays the default until you've driven the new one.
- **A parallel layout, not edits to the old one.** Today's island is wired into a lot of code:
  - `new Circuit()` doubles as "the world" for Hillcrest and Quarry races, free-roam recovery and about 12 checks;
  - the racing line is hash-locked;
  - tests use fixed coordinates.

  So the new island is its own layout: its own `Circuit`, its own track id (`harbour_loop`) with its own baked line,
  and its own land, coast and town builders. These are selected at world build, and the flag is added to the world
  cache's key.
- **Saves:** old event ids, keepsake names and residents keep their ids, with new positions. A saved spot on the old
  island falls back to the new spawn.
- **Stages (each one judged before the next):**
  1. **Land and roads in grey:** the island, the loop and the gravel lane. The AI races 24/24 with no recoveries;
     you can drive it.
  2. **Harbour and town:** the harbour arms and quay, the terrace generator, the Pit Stop, residents, the quest and
     postcards re-pointed. The adventure smoke test passes on it.
  3. **Landmarks:** the lighthouse, windmill, clock tower, pier and lifeboat station.
  4. **Life and dressing:** street life, boats, harbour clutter, planting, farm.
  5. **Judged:** the area audit and a street review pass; start-up and race entry stay inside their budgets; you
     drive it side by side with the old Bayview.
  6. **Switch:** your call. Then the keep/reject races move to the new track (with the old one recorded for the
     comparison).

## Out of scope
- Interiors.
- The story's script, which needs your direction.
- The Grand Prix's valley section beyond the causeway, which is Hillcrest's.
- Coral Key.

## Risks
- **The keep/reject races** (Cedar and Bayview, seeds 1709-1711) lose Bayview when the switch happens. Results from
  before and after aren't comparable, so a new baseline is recorded.
- **Load time:** a bigger island and more props. The start-up budget (doc 73) is checked at every stage.
- **The mainland's absolute coordinates** (the quay, the rail loop, the z > −380 and −452 lines) constrain the south
  coast. The plan keeps them.

## Questions (each has a default I'll take if you'd rather not choose)
1. **Size:** growing the island to about 430 × 300 m is what makes a 1 km loop possible without it crossing itself.
   *Default: yes.* The alternative is a smaller island with a figure-of-eight under a stone bridge.
2. **Lap direction:** clockwise, so the harbour front is the last thing before the line. *Default: yes.*
3. **Character:** Cornish and Scottish harbour (granite, slate, bold paint), with Italian colour and loggias kept for
   Hillcrest. *Default: yes.*

## Decisions and progress
- **19 Oct: Richard approved the brief with the defaults:** the island grows, the lap runs clockwise, and the character
  is a Cornish and Scottish harbour.
- **Stage 1, the island in grey: done (19 Oct).** It's behind `--new-bayview` (`NewBayview.cs`, `NewBayviewPlan.cs`,
  generated by `tools/bayview_plan.py`).
  - **The land:** the coast, the harbour bay, a plateau rising to the windmill's summit, cliffs on the north and west,
    and a beach on the east.
  - **The roads:**
    - the Harbour Loop with its own elevation;
    - the gravel lane;
    - the causeway link (the mainline now starts on the loop's start straight).
  - `new Circuit()` returns the new loop when the flag is on.
  - **The racing line:** the race keeps the track id `bayview`, so events and saves line up. Its line is baked from
    the live loop (`harbour-loop-smooth-v1`), not the old hash-locked one.
  - **The race:** 24/24 finished with no recoveries, seed 1709; winner 48.6 s for the 1,027 m lap.
  - **The old island:** the world's fingerprint without the flag is unchanged (`3f164ae90677e486`).
  - **Still to do** in stage 2:
    - the old village and countryside, the bridge and the cliffs aren't built on the new island;
    - the residents and the Pit Stop still stand at their old places, so the adventure smoke test fails until then.
- **Stage 2, harbour and town: done (19 Oct).** All still behind `--new-bayview`.
  - **The harbour:** granite walls round the bay's shore and both faces of each arm, with round heads; the land drops
    inside the walls' cores, so none shows through the battered faces.
    - The quay is paved in granite setts from the road's kerb to the harbour wall, with a blue promenade railing,
      lamps and benches. The arms have paved decks.
    - The harbour front (quay, road and town) is level at quay height. In stage 1 the road climbed away from the
      harbour and the town stood 10 m up a grass slope.
  - **The town row:** 16 buildings along the harbour front (terraces, the cafe, the post office and shops), each its
    own bold paint, with slate roofs and a flagstone pavement.
  - **The Pit Stop:** garage and parts bays on a paved forecourt by the start straight. Its levelled square now meets
    the hill in a long grassy bank, not a cut ledge.
    - Trees, scrub, boulders and meadow grass keep off the quay, the town, the arms and the forecourt
      (`NewBayview.BuiltDistance`).
  - **Residents:** the garage and parts hosts stand at the Pit Stop's bays, and the cafe and paint residents on the
    town's pavement.
  - **The loop, reshaped:** the north-east hairpin had a 10 m radius. The 16 m road's inside edge folded over itself
    and cars jammed there in the adventure race.
    - It's now a 30 m arc and a 30 m S-bend out of it, and the south-west bend is a 28 m arc. The tightest radius is
      19 m and the lap is 1,079 m.
    - The loop is a centripetal Catmull-Rom (here and in the plan tool), so unevenly spaced points don't kink it.
  - **Fixes the new island needed:**
    - **Free roam:** a car on a track with hills only drives on road-support meshes. With the flag, free roam keeps
      the old "tilt with any ground" handling, so the island and the mainland stay drivable.
    - **The run-off:** a 24 m sheet under the road stood through the asphalt where the loop climbs as it turns. With
      the flag it's two shoulders.
    - **The smoke test** places the car in front of each resident by the way they face, not 5 m north.
  - **Checks:**
    - **The adventure smoke test passes with the flag:** both races finished 24/24 with no assisted recoveries.
    - **Races on seeds 1709, 1710 and 1711:** 24/24 finished with no resets; winners took 48.9-52.7 s.
      (`review/unity-35/frame-cost-0/bayview-s17xx-nb-stage2`)
    - The fingerprint without the flag is unchanged (`3f164ae90677e486`).
  - **Left for later stages:**
    - the steep bank behind the town row (a retaining wall or terraced gardens);
    - the corner where the west arm meets the harbour wall;
    - the post box resident, which still stands where the old formula puts it.
