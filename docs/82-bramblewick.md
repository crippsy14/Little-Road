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

## Where to pick up (handover, 20 Oct)
1. **Finish step 2:**
   - The T at (560, −1156) still has 4 road seams and Hillcrest Valley 2 road-edge lips: tiny height steps where Valley
     Road South's ribbon meets the junction and the station road. See `--area-audit`'s seam list (`Valley Road South`
     around chunk 30,17).
   - Try the `MouthShoulders`/`KerbShoulders` treatment the Quarry Hills junctions got (`HillcrestValley.ValleyRoads`),
     or start the road's `lift` at the junction's height.
   - Hillcrest Valley's life per road km (4.5, needs 5) and things to do per km (2.7, needs 3) dip under the bar with
     the new road's kilometres. Dress the pass with hedge banks, a fingerpost, a farm gate, a photo spot and a speed
     trap, which brings both back over the bar.
   - The map: `WorldMap` X0/Size (`WorldMap.cs:18`), `ExploredMap.Cells` and `HillcrestValley`'s `MacroRect` don't
     reach x 2400 yet.
2. **Step 3, the village:** the green and pond, the parish church and lychgate, The Plough and the thatched cottages
   from the `village` kit, the tuner's barn, and the dairy from the `farm` kit (milking parlour, Dutch barn, churn stand,
   cattle grid). Lanes as `hedge_bank` runs. The new ids need BayviewKit footprints in the village's own code, as
   `QuarryHillsTown.Footprint` does.
3. **Then:**
   - fields and life (cows from the `cows` kit; the fields redo, bug #20);
   - things to do (the race, the milk-float duel, the parts, the railway halt);
   - judged: area audit, a targeted review on one contact sheet, then the flag comes off.
4. **Still to make:** the milk float (Car Forge) and the tuner's parts on the doc 44 ladder.
5. **Richard's four questions** above still have only my defaults.

How each place is built and judged is in [doc 81](81-whole-map-order.md), "How each place is built".

## Questions for Richard (each with my default)
1. **Style:** English West Country (thatch and cob, sunken lanes, a village green)? Default: yes.
2. **The milk-float duel:** a one-on-one sprint down her lane, her float quicker through the bends than on the
   straights? Default: yes.
3. **The tuner's parts:** rural C parts (springs for rough lanes, an off-road tyre, a cold-air intake, a lightweight
   battery)? Default: those four, priced on the doc 44 ladder.
4. **The race:** a 24-car race on single-track lanes is tight. Should the lanes be wide enough for two abreast, with
   passing places for the third? Default: yes (about 6.5 m).
