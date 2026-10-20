# 82. Bramblewick: brief (20 October 2026)

Step 0 of [doc 57](57-new-area-standards.md) for the first place on [doc 81](81-whole-map-order.md)'s list. These are
my calls, made so the work isn't blocked; the questions at the end are Richard's, each with the default I'll use until
he answers.

## Its place in the world
- **Where:** (8.4, 17.2) km on the continent, about **(2400, −1780)** in game metres.
  - 2.9 km from Bayview;
  - 1.7 km east of Hillcrest's Golden Acre Farm;
  - 1.8 km north-west of Vinelands.
- **What doc 58 says:** "Farm village south-east of Hillcrest. A backyard tuner sells class C parts that aren't in
  Bayview, and the milk-float champion signs for your team once teams open, if you've beaten her down her lane."
- **Class C village:** one race (2★ in [doc 70](70-race-fields-across-the-map.md), "Bramblewick lanes"), local parts
  (one of only five places that sell them), a recruit. Reward: a tractor-gauge speedometer.
- **Railway:** the Coast Branch passes through (docs/68), so the village gets a halt.
- **Roads:**
  - Valley Road South leaves Hillcrest's valley and runs through Bramblewick to Vinelands;
  - a gate at the far side of the village, closed until Vinelands opens (as Kettle Falls').

## The vision: "the lanes where the hedges meet overhead"
An English West Country farm village. The other places are Cornish/Scottish (Bayview), Italian (Hillcrest), Welsh
(Quarry Hills) and Portuguese (Vinelands, next), so this is the one that is plainly English:
- **The village:**
  - a green with a duck pond, a stone church with a squat tower and a lychgate;
  - a thatched pub, *The Plough*;
  - a dozen cob-and-thatch cottages round the green and down one street.
- **The farms:**
  - a dairy (where the milk floats come from) with cows in the fields;
  - an orchard on the slope toward Orchard End;
  - hay meadows and arable fields in a patchwork, bounded by hedges.
- **The lanes:** single-track lanes sunk between high hedge banks (Devon lanes), with passing places, a ford through
  the brook, and a humpback bridge. These are what the race and the milk-float duel are run on.
- **The backyard tuner:** a tin-roofed barn behind a cottage, parts on the bench and a car on axle stands. This is the
  parts shop, not a Pit Stop: Bramblewick is a village, so its Pit Stop services stay at Hillcrest.
- **The look:** golden thatch, cream and pink cob, green hedges, red post box, white-painted fingerposts; low and
  rounded, toy-like, under the same light as the rest (art direction).

## What you do there
| Thing | What | Notes |
|---|---|---|
| **Race: Bramblewick Lanes** (C, 2★) | A 24-car race round the lanes: village green, the ford, the humpback bridge, past the dairy | About 2.4 km a lap; the lanes are narrow, so overtaking is at the passing places and on the green's straight |
| **The milk-float duel** | One-on-one down her lane against the dairy's milk-float champion; win and she'll join your team once teams open | Her electric float is quicker than it looks round the bends; the lane is hers |
| **The backyard tuner** | Sells class C parts that aren't in Bayview | Rural parts on the doc 44 ladder: stiffer springs for rough lanes, an off-road tyre, a cold-air intake, a lightweight battery. Discovered here, then sold everywhere (doc 56 §21) |
| **Finds and photo spots** | A keepsake in the church porch; a photo from the humpback bridge; a speed trap on the straight past the dairy | To doc 57's per-km bar |
| **Life** | Residents (the tuner, the milk-float champion, the publican, the vicar); cows; ducks on the pond; a tractor on the lanes | |

## Kits it needs
| Kit | New pieces | Why |
|---|---|---|
| **houses** | `cottage_thatch` a-c: cob walls, thick thatch roofs with eyebrow dormers | There are no thatched cottages yet |
| **civic** (or a new `village` kit) | `church` with a squat tower and lychgate; `pub` (*The Plough*); `village_hall` | Forge has no church |
| **hedges** | A *hedge bank* run: an earth bank with the hedge on top, 2-2.5 m high, for the sunken lanes | The hedge kit is a plain hedge on the flat |
| **farm** | Milking parlour, a dairy yard with churn stand, cattle grid, hay barn | Some farm pieces exist (gate, trough, churns, silo) |
| **life** | Cows (Friesian), ducks | Sheep exist |
| **fields** | Crops that part and flatten under a car, and the field patchwork (bug #20) | The fields redo |
| **Car Forge** | The milk float, an electric three-wheeler or four-wheeler with crates; a tractor for the lanes | For the duel, and for life |
| **roads** | A ford (a dip with water and a depth post), a humpback bridge, passing places | New road features |

## Build order (each a round: build, check, targeted review)
1. **Kits:** thatched cottages, church and pub, the hedge bank run, cows, the milk float. Contact sheets.
2. **Land and roads** behind `--bramblewick`:
   - the terrain round (2400, −1780);
   - Valley Road South from Hillcrest's valley to the village and on to the Vinelands gate;
   - the lanes, the ford and the bridge.
3. **The village:** the green, pond, church, pub, cottages, the tuner's barn and the dairy.
4. **Fields and life:** the patchwork with the new crops, hedges, cows, residents.
5. **Things to do:** the race (24/24 on three seeds), the milk-float duel, the parts, finds and photo spots, the
   railway halt.
6. **Judged:** area audit at zero, a targeted review; the flag comes off.

## Progress
- **20 Oct, kits (step 1), Car Forge:**
  - `village`: three thatched cob cottages (two variants each: thick thatch with rolled eaves, eyebrow dormers, a
    block-cut ridge, a porch or a rose), the parish church (west tower with battlements, pinnacles and a clock; nave,
    chancel, south porch), the lychgate, and *The Plough*;
  - `hedge_bank`: a run kit, a stone-faced bank with the hedge on top;
  - `cows`: Friesians, body and head apart, for grazing.
  - A `Thatch` colour joins the palette.
  - Still to come: the milk float (Car Forge), the dairy buildings and the fields redo.
- **20 Oct, land and roads (step 2), in progress behind `--bramblewick` (off by default):**
  - `Bramblewick.cs`: a farmland vale at about 34 m round (2400, −1780), framed by low hills, over the valley's east ring
    of hills; a pass through the ring from Golden Acre Farm.
  - Valley Road South (7.5 m) leaves the farm lane at a T (560, −1156), climbs over the pass and runs down through the
    vale to a gate at (2780, −2040). It is level for 20 m off the T.
  - Hooks:
    - `ValleyField`'s grid reaches east to x 3500 and south to z −2500, and `Natural` blends the vale in;
    - the terrain chunks reach south there;
    - the road network splits the station road at the T;
    - shoulders and guardrails along the new road;
    - a `MapRegions` entry ahead of the valley;
    - `WorldCache`'s key gains `-bw`.
  - **Checked:**
    - the old world's fingerprint is unchanged (`3f164ae90677e486`);
    - photos of the vale, the pass, the T and the gate look right.
    - Building the mainland takes 2.4 s with the flag on, against 1.4 s without: the larger grid.

- **20 Oct, the village (step 3):** the green with its pond, reeds, benches and trees; St Michael's with its lychgate
  and churchyard wall; The Plough, the pillar box and phone box; 35 thatched cottages with hedged gardens and apple trees;
  street lamps; the tuner's barn; the dairy (milking parlour, Dutch barn, churn stand) with Friesians in the field.
- **20 Oct, fields and life (step 4):**
  - **The fields redo (bug #20), everywhere:** wheat, barley, rapeseed and young crop are a standing canopy at crop
    height (`CropField`), with drill rows and a wind swell close up. The car ploughs a swath through it that lies flat
    and stays down (`CropTrample`, the `Crops` shader). `--crop-test` drives across a field and photographs it.
  - **The vale's patchwork:** 232 fields on a jittered lattice, so every field is a different four-sided shape and
    neighbours share their hedges. 42% pasture (a third with heifers), then hay, wheat and rapeseed. 893 hedge runs,
    each boundary hedged once with a gateway (and gate) in about half; hedgerow oaks; 20 copses on the rough ground; the
    orchard on the north slope.
  - **Life:** four named residents with their own lines (Jed the tuner, Maggie at The Plough, Reverend Clem on the
    green, Peg at the dairy); four village cars up and down Valley Road South; parked cars by the green, outside the pub,
    in six lay-bys along the pass and in the dairy yard.
  - **The audit:** a Prop Forge hedge now counts as planting, one per 2.6 m (the bushes it replaced). Bramblewick and
    the vale pass planting and life; only things to do are short, which is step 5. Frame cost over the vale is about
    1.3 ms.

- **20 Oct, things to do (step 5, first part):**
  - **Six lucky horseshoes,** a keepsake set: St Michael's porch path, the duck pond, the orchard, the dairy yard, a
    field gate out in the vale, and the top of the pass. Each pays 50 credits. They have their own album in the pause
    menu, sharing the postcards' row so the page keeps its three rows. There's no set paint yet: paint indices are saved,
    so one only for `--bramblewick` could break a save loaded without it; it joins when the flag comes off.
    `--horseshoe-test` collects all six (6/6, none blocked).
  - **A speed trap,** "Vale Straight", on Valley Road South's straightest run outside the village.
  - **Four photo sights:** St Michael's tower, The Plough, the duck pond and the dairy.
  - **The area audit:** Bramblewick (village) and Bramblewick Vale both pass.

## Where to pick up (handover, 20 Oct, after step 5's finds)
1. **The rest of step 5:**
   - the race (24/24 on seeds 1709-1711): per-circuit widths agreed (Richard, 20 Oct). Next: `Circuit` gets its own
     width, read by the line bounds and gates; the other races stay at 16 m;
   - the milk-float duel (Peg);
   - Jed's parts: waiting on doc 83's decisions;
   - the railway halt.
2. **Still to make:** the milk float (Car Forge).
3. **Leftovers:** two lips at the T (480, -1110), 0.13-0.16 m; the floating vine row (160.5, 36.3, -1040.2); two
   Bayview waterline rocks.
4. **Richard's four questions** below still have only my defaults.

How each place is built and judged is in [doc 81](81-whole-map-order.md), "How each place is built".

## The race needs a call: lane width (20 Oct)
Building *Bramblewick Lanes* turned up a clash between the brief and the race code:
- **The race code assumes one width for every circuit.** `Circuit.Width` is 16 m. The shared racing line may sit
  ±5.6 m off the centre (`RacingLine.MaxOffset`), each car's own line ±5.4 m (`CarLines.Bound`), and the 12 checkpoint
  gates test against the same half-width. Hillcrest's raceway and the Quarry Hills loop are both built 16 m wide.
- **The brief wants lanes.** Question 4's default is about 6.5 m, two abreast with passing places, between hedge banks.

The options:
1. **Per-circuit width (my recommendation).** `Circuit` gets its own width, which the line bounds and gates read from.
   Bramblewick's race runs on 7 m lanes with 9 m passing places and the green's straight at 12 m. The other races
   keep 16 m, so their keep/reject results don't move. This is the real lanes race the brief describes, but
   24 cars on 7 m is a hard AI problem: expect several rounds before 24/24 with no assisted resets. It could also start
   with a smaller field (12-16).
2. **A 16 m circuit on the vale's roads.** It works with today's code at once, but it's a road race, not lanes. The
   hedge banks would be 8 m back from the racing line.
3. **Lanes for free roam and the milk-float duel only** (one on one, no grid), with the 24-car race on a wider loop
   (option 2).

Two smaller things come with any option:
- **Saves:** a podium id the calendar doesn't know makes a save fail to load, so a Bramblewick podium earned with the
  flag on breaks the save with it off. Save validation should keep unknown event ids instead of rejecting the save.
- **Progress:** a new class C event moves the class C completion gate (every C event needs a podium). That's right for
  the finished game, but it changes current saves' progress.

## Jed's parts need a call too (20 Oct)
The brief's four parts (lane springs, an off-road tyre, a cold-air intake, a lightweight battery) don't match the game's
part slots: engine, turbo, tyres, transmission, brakes, steering, chassis. There's no suspension, intake or battery slot.
Also, a class C part with a home shop and no unlock condition is open to the AI fields from the first star
(`FieldRecipe.Open`). Any new part would change every existing C race's fields, so it needs keep/reject runs.

My proposal, on the existing slots, each at home in Bramblewick and opened to AI fields at 2 stars (as Hillcrest's
parts are):
| Part | Slot | Price | Stats | Between |
|---|---|---|---|---|
| **Lane tyres** | tyres | 900 | road 8.8, dirt 8 | touring and the Hillcrest rally tyre: an all-rounder for mud on tarmac |
| **Farm-track steering** (the "springs") | steering | 800 | slower, steadier response; less yaw | a calm rack for bumpy lanes |
| **Cold-air engine** (the intake) | engine | 1250 | the club 1.4 with +4 hp up top | club and sport16 |
| **Battery-delete shell** (the light battery) | chassis | 850 | mass 0.92 | light and seam-welded |

Or add the missing slots (suspension, intake, electrics) properly. That's a bigger change across the parts shop, the
parts ladder (`tools/parts-ladder.py`, docs/47) and the AI fields.

The railway halt waits on the Coast Branch, which isn't built yet (docs/68). The milk float is a Car Forge model, which
goes through Richard's photo rounds (model iteration loop).


## Richard's answers (20 Oct)
- **Style:** West Country, as planned. (Hoxne, in Suffolk, was a fond reminder from his childhood visits, not a change
  of style.)
- **The race:** per-circuit widths, "for sure". Bramblewick Lanes runs on real lanes (about 7 m, with wider passing
  places) and the other races keep their 16 m.
- **Jed's parts:** to be discussed properly as a whole-game question. See [doc 83](83-parts-system.md).

## Questions for Richard (each with my default)
1. **Style:** English West Country (thatch and cob, sunken lanes, a village green)? Default: yes.
2. **The milk-float duel:** a one-on-one sprint down her lane, her float quicker through the bends than on the
   straights? Default: yes.
3. **The tuner's parts:** rural C parts (springs for rough lanes, an off-road tyre, a cold-air intake, a lightweight
   battery)? Default: those four, priced on the doc 44 ladder.
4. **The race:** a 24-car race on single-track lanes is tight. Should the lanes be wide enough for two abreast, with
   passing places for the third? Default: yes (about 6.5 m).
