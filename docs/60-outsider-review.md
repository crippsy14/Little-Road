# 60 · An outsider's review of Little Road (3 October)

Richard asked for a full review written as if by someone meeting the game for the first time, and for ideas that fit its feel. This is that review: what lands, what gets in the way, and what to add next. The build reviewed is `claude-additions` on 3 October, with Quarry Hills turned on.

## The pitch, as a newcomer sees it

Little Road is a cosy racing adventure. You drive a toy-like car round a small, hand-made world: Bayview island, the Hillcrest valley, and now the slate town of Quarry Hills. You meet the people who live there (every one of them a car), run errands, find keepsakes and enter races that open better parts and new classes. The art direction, "toy shapes, real light", is the hook: everything looks like a well-made model village lit by a real afternoon sun.

## What lands

| | Why it works |
|---|---|
| **Reveals** | The crest above Quarry Hills, the castle spiral above Hillcrest and the causeway out of Bayview all frame the next place before you reach it. That is the game at its best. |
| **Towns that feel lived in** | Bunting lamp to lamp, residents parked outside their own front doors, market stalls, shop signs (Welsh in Quarry Hills), chimney smoke. Busy without being noisy. |
| **The Pit Stop** | Every town has the same garage, parts counter and body shop. You always know where to go, and each town still has its own stock. |
| **Parts as discoveries** | Finding a part at its home shop and then buying it anywhere rewards exploring without punishing the player for coming back. |
| **24-car races** | The AI fields are full, varied and finish cleanly, and the cars come in different shapes. |
| **Little challenges everywhere** | Castle Climb, Quarry Sprint, speed traps, Beat the Train, photo requests, rally lessons. There is almost always something to try within a minute of where you are. |
| **Systems you can trust** | Autosave on every change, saves validated before they're written, and now resuming where you left off. Nothing silently loses progress. |

## What gets in the way

1. **Knowing what to do next.** After Mara's parcel the objective line only says "explore" or "race". A newcomer can't tell what each town offers until they drive up to everyone. *Fix:* a notice board in each town's square (events, recruits, prizes, what's been done), with pins on the map.
2. **Distances are growing.** The train, and now the warp back to a garage, help. Junctions still have no signposts, and the map shows no road names. *Fix:* fingerposts at junctions with distances, road names on the full map, and the map zooming to the region you're in.
3. **Off-road doesn't feel off-road in a race.** Inside the circuit's width, a race uses tarmac grip whatever the surface, so the rallycross drives like the raceway, and rally tyres only matter in free roam. *Fix:* a real loose-surface grip and slip model for races on gravel (doc 59's "gravel feel"), with dust, stone rattle in the sound, and the gravel tyres making a visible difference.
4. **Load time.** About 9 s for Bayview and Hillcrest, but 18–25 s with Quarry Hills. Each new region adds to it. The W1 work in doc 58 (world tiles, streaming) needs to land before more towns do.
5. **Repetition up close.** Long terrace rows reuse a handful of house designs. Hillcrest varies them more. *Fix:* more terrace fronts (shopfronts with awnings, a chapel vestry, a pub with hanging baskets), plus end-of-terrace gables and alleys.
6. **Sound of place.** Each region looks distinct but sounds much the same. *Fix:* ambience per area (gulls and rigging at Slate Quay, sheep and wind on the moor, a brass band practising in the Institute on a weekday evening, the noon blast siren in the quarry).
7. **Rewards for completing a set.** Postcards, stamps and carvings pay 50 credits each, and finishing a set gives nothing extra. *Fix:* a reward for each full set (a paint, a horn, a plate), shown in the collection album.

## Ideas that fit the game's feel

**Events and activities**
- **Town festivals on a calendar.**
  - An Eisteddfod in Quarry Hills: a choir competition you drive between, and a parade of off-roaders.
  - A harvest fair in Hillcrest.
  - A regatta in Bayview.

  Each changes the bunting, the stalls and the traffic for an in-game day.
- **Residents with routines.** Megan opens the Caban at seven; the choir's cars gather at Capel Bethel on Thursday evenings. Even a few routines make towns feel alive between visits.
- **Ghost times.** Your best run in every time trial (Castle Climb, Sprint, Gallery Climb, lessons) races alongside you as a translucent car.
- **Jobs from residents.**
  - Heavy Haul down the haul road.
  - Delivering a cake from Bara to the rugby club before the match.
  - Leading a lost tourist's camper to the campsite.

  These are short and named, and they're paid in credits and friendship.
- **A concours.** Park your car in the square on market day and residents vote on paint and body.
- **The Blondin camera** in the frenzy era (doc 59): races filmed from a camera riding the cableway.

**The car and the garage**
- **Stickers, liveries and number plates**, unlocked by places (a slate-grey "QH" plate from Quarry Hills).
- **Rally kit you can see**: mud flaps, a roof light bar and a sump guard on your own car when fitted.
- **A photo-mode album** that keeps your best shots, with Pip's postcards made from them.

**The world**
- **Sheep on the moor roads** that wander into the lane and move off when you sound the horn.
- **Weather in Quarry Hills first**: mist in the quarry, rain on slate, a rainbow over Glas.
- **The Pine & Peak railway as a real line**, so the slate train can be raced down the valley (Beat the Train, Welsh edition).
- **Viewpoints** that reveal the map around them, as Hillcrest's do, with Quarry Hills' rim cairn as the first.

## What to do next, in priority order

1. The loose-surface grip model for gravel races.
2. Town notice boards, map pins and junction signposts.
3. Streaming and world tiles before the next town (doc 58, W1).
4. Ambience per area.
5. Rewards for full keepsake sets.
6. Festivals and routines, starting with Quarry Hills.

## Phase four: the recommendations, built (3 October, late)

Richard asked for all six. Five are built and tested; the third is partly done (a faster load, not world tiles). Two ideas from the list came along too.

**1. Loose-surface grip (`CarTuning.Loose`).**
- The Quarry Hills Rallycross is loose slate. Every car in it races on a grip 60% of the way from its tyres' road grip to their gravel grip. The physics and the drivers' planning use the same number.
- Results: stock tyres drop from 7 to 4.9; rally tyres rise from 8.5 to 10.9. Gwen now says road tyres grip about half as well there.
- The field runs on gravel tyres: club kit on rally tyres, street kit on trail tyres.
- Dust is slate grey for every car, and the gravel sound plays all race.
- Results on the same build, serials 2, 3 and 4:
  - 24 of 24 finished each time, with no recoveries;
  - total field time 3,860, 3,826 and 3,860 s, against 3,932, 3,907 and 3,923 s before (about 2% faster);
  - the autopilot player car, still on stock tyres, finished last each time (22nd to 23rd before).
- An editor check confirms rally tyres out-grip stock by more than 1.8× on loose ground.

**2. Notice boards, map pins and fingerposts (`Wayfinding`).**
- **Notice boards:**
  - A board stands in each town: by the café in Bayview, and on the squares in Hillcrest and Quarry Hills.
  - Each is placed by search: clear, level ground, 3 to 7 m from a road, facing it. Bayview's loop isn't tagged as road, so it is measured from its centre line.
  - "Read the notice board" lists what the town offers, with your progress: races and podiums, trials and pars, lessons, keepsakes (with a hint where the next one is), speed traps, rides, Rally Day and choir night.
  - Picking a line pins it. The map shows a sky-blue star, and the objective line reads "Pinned: … · 425 m". The pin clears within 20 m of the place.
- **Fingerposts:**
  - White fingerposts stand at the nine mainland junctions.
  - Each finger shows up to two places reached that way, with the shortest network route in quarter miles (for example "Hillcrest 2½ / Bayview 2¾").
- `--wayfinding-test` passes with and without `--quarry-hills`: every board is placed, the pin drives the objective line, and the fingerposts are pictured.

**3. Load time (partial).**
- The terrain chunks' geometry is built on every core, and their colliders are cooked in parallel with `Physics.BakeMesh`. The mainland step fell from 4.9 s to 3.3 s; total load with Quarry Hills went from about 15.9 s to 14.4 s.
- The valley field's road corridors and embankments now run row by row on every core: 2.1 s fell to 0.9 s. It is the same field: the ground heights at all eleven junctions match the earlier build, and a `checksum` is now logged for future comparisons.
- World tiles and streaming (doc 58 W1) are still to do before the next town.

**Still open from the area audit (Quarry Hills).**
- **3 road seams**, each a 10 cm step about 15 m from a junction: two on Chapel Street, one on Terrace Row. The seam report now prints the road line's heights alongside. The line is a smooth 10% grade, but just before the step the surface sits 3–5 cm above it, so some piece overlaps the street near the junction rim (radius 15–16 m). Next: name every collider the probe hits there.
- **6 road-edge lips and 1 steep bank**, as before.

**4. Ambience by area.**
- Three new loops cut from the Sonniss GDC 2023 bundle (raw files outside the repo):
  - sheep and lambs, where there are sheep (up to 8 within 130 m);
  - water lapping at Slate Quay;
  - people in the three town squares by day.
- Quarry Hills' blasting, synthesised: the motor siren winds up, holds and runs down, then the shot thumps out of the pit and rumbles. It plays every 4 to 7 minutes within 1.8 km of the pit, in working hours only.
- No brass band or gulls: the bundle has neither.

**5. Keepsake set rewards.**
- A full album earns a paint, free at any paint counter: Seaglass (Bayview postcards), Heather (Hillcrest stamps) and Quarry Slate (the carvings).
- The paint board gains a fifth row, which shows "?" until the set is earned. The album shows each set's reward.
- Traffic still picks from the shop's sixteen colours.

**6. Festivals and routines (`QuarryFestival`).**
- **Routines:**
  - The Caban opens from 7 to 7; Megan has a "we're shut" line.
  - The quarry blasts from 8 to 5.
  - Choir night runs from 6:30 to 9:45 pm: seven cars park outside Capel Bethel, each with something to say about Calon Lân.
- **Rally Day:** once you've a Rallycross podium (or with `--festival`):
  - twelve flags round the square;
  - a "RALLY DAY · DIWRNOD RALI" banner over the High Street;
  - six off-roaders with pennants parading up and down it;
  - confetti over the square;
  - a line on the notice board, and Gwen's greeting.
- `--festival-test` passes: 12 flags, 6 parade cars moving, and 7 choir cars at half past seven.

**From the ideas list:**
- Sheep in the farm lanes (17 of them) step out of the way of a car, and every sheep within 35 m scatters at the horn.
- The rim cairn at the top of the Gallery Climb reveals all of Quarry Hills on the map (1.5 km).
