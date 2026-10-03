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
