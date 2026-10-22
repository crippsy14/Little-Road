# 92. Vinelands, Zone 7: Eiras, the Vale and the Circuito da Cumeada (brief, 22 October 2026)

The seventh zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, sections 1,
5 and 6b). Frame as in docs/86. Built behind `--vinelands`.

## What this place is, and why it's here
The west side of the ridge is the town's back garden. The ground is mixed sandy soil, not schist, so there are no
terraced vines. The land works differently:
- **Eiras**, below the Largo: the workers' and farmers' cottages on the lip, with balconies to the sunset, the old
  threshing floor, and the carpenter.
- **The Vale shelf**, falling west. Plots are small near the town and bigger further out:
  - the kitchen gardens along the brook from the spring (Hortas da Ribeira);
  - the almond and fruit orchards (they blossom white in February);
  - the olive grove on the dry north-west flank;
  - the cork-oak montado on the poor south-west ground, with the cork depot at its edge;
  - four farmsteads, each with a barn, a threshing floor and a fig.
- **The Circuito da Cumeada**: farm lanes the club closes on race days, laid out round the shelf so the noise stays
  behind the ridge. The Eiras' balconies look down on the main straight. The paddock is in the infield, entered from the
  old Vale road.

## The circuit (track-design skill)
**The short lap** (`Vinelands.CircuitSpec`; draft `review/tmp/cc4.txt`, `tools/track-eval.py --loop`): 1.85 km,
counter-clockwise from the start line at (−470, −260), 9 m wide.

| Corner | Deflection, radius | Purpose |
|---|---|---|
| Recta da Eira | 369 m straight north | The main straight and the overtaking zone. The Eiras look down on it. |
| Curva da Eira Velha | +40°, R90 | Braking from the straight into a medium left. |
| Lomba do Alto | −18°, R140 over a rise | The commitment kink, blind over the brow. |
| (unnamed) | +68°, R80 | Onto the north side, a short straight west. |
| Curva do Carvalhal | +85°, R75 | Fast-ish left onto the west leg, falling. |
| Esses do Amendoal | +28° / −28°, R100 | The rhythm section through the almonds. |
| (unnamed) | −20°, R160 | A kink. |
| Gancho do Sobreiro | +110°, R22 | The slowest corner and the heaviest stop, downhill. The second overtaking zone. |
| (unnamed) | 190 m straight east | Climbs back. |
| Chicane da Ribeira | −40° / +40°, R40 | The mistake generator by the brook. |
| Curva da Fonte | +95°, R45 | The medium-fast corner before the main straight, so cars stay close. |

**Metrics:** 11 corners (5.9/km); 309°/km turning; curved share .35 by the tool, about .41 measured; longest straight
.20 of the lap; second straight .51 of the longest; slowest corner 14.3 m/s; an estimated class C lap of 72 s.

**Self-critique:**
- The hull ratio is 1.01, a loop round the field. The skill's band for flowing country circuits is 1.15+, but the
  measured club median is 1.06 (Lime Rock, Mallory, Thruxton), and the infield holds the paddock.
- Corners per km is a touch high: the esses and the chicane are close together.

**The long lap** (Richard's short-and-long idea, used here): the short lap as far as the west leg, then off before the
Gancho onto the montado road (`Vinelands.ExtSpec`):
- a descending left into the cork oaks;
- the Curva do Montado (+95°, R40), a heavy stop;
- the Recta dos Sobreiros east;
- a long left up to rejoin the lap after the chicane.

About 2.4 km, for the faster classes; it skips the Gancho and the chicane, so it flows more. The race ids are
`vinelands` (short) and `vinelands-long`.

**Race day:**
- the start's chequers, grid boxes for 24, and a gantry with a banner;
- painted flush kerbs on both laps;
- bale walls outside the heavy stops;
- a board with each corner's name;
- spectators' cars at the Gancho, the chicane and the Eira Velha.

## The circuit, second design (23 October)
Richard looked at the first lap's map and said it was "a very safe design with all corners being generic and light
turns". He was right. Against its own skill, the first lap:
- was a loop round a field (hull ratio 1.01);
- was mostly light turns, with one real hairpin;
- had no mistake generators;
- had its elevation left to the ground rather than designed.

The redesign used the skill properly, with a design harness that ports the ground (`Vinelands.Natural` on the Vale
side) and measures every band. Map: `review/tmp/cumeada-v2-map.svg`.

**The land.** The Vale is a shelf tilted up to the south. North–south runs are steep (13–16 % south of n −100) and
east–west runs are gentle (about 5 %). The lap is built around that: a plunge off the high ground, rhythm sections
climbing back, and a hairpin folded deep into the infield. The grade limit is 10 %; at 8 % the climb back to the start
needed a 15 m cutting.

**The short lap (class C, 3★, two laps), 2.52 km:**

| # | Corner | What it is |
|---|---|---|
| 1 | Recta da Eira | 330 m straight north, downhill at 8 %, with a view over the Vale. The start line is 130 m up it, so the grid stands on the straight. |
| 2 | Curva da Eira Velha | Left, 100°, R26, braking downhill. Overtaking zone 1. The long lap goes straight on here, so it doubles as the escape road. |
| 3 | Esses das Amendoeiras | Right-left (R70/R60). Rhythm section. |
| 4 | Lomba do Carvalhal | Right, 25°, R150 over a 2.5 m carved crest, blind. The commitment corner, after a 120 m run-up. |
| 5 | Curva do Carvalhal | Double-apex left round the farm, 120°, tightening from R130 to R50. Mistake generator. |
| 6 | Fundão | Right kink through a 2.5 m carved dip. Compression. |
| 7 | Curva das Courelas | Decreasing-radius left, 105°, R110 to R40. Mistake generator. |
| 8 | Curva do Muro | Off-camber right kink, climbing past a dry-stone wall. |
| 9 | Curva da Cruz | Left, 95°, R55, onto the plunge. |
| 10 | Mergulho | 250 m downhill into the infield. Overtaking zone 2. |
| 11 | Gancho do Sobreiro | Right hairpin, 170°, R21, round a big cork oak. The slowest corner, 14 m/s. |
| 12 | Subida | The climb back out, 180 m. |
| 13–14 | Esses do Alto, Curva do Alto | Uphill rhythm, cresting. |
| 15 | Curva do Largo | Medium-fast left onto the main straight, so cars stay close. |

**Its metrics:**
- 14 corners (5.6/km); 405°/km of turning; macro turning 362°/km.
- Curved share 0.43; hull ratio 1.26.
- Angles 2/5/3/3/1 and radii 2/5/4/3 across the bands.
- 43 % right-handers; 0.86 direction changes per corner.
- Longest straight 0.13 of the lap; the Mergulho is 0.76 of the main straight.
- Five heavy stops; the lap falls 54 m; cuttings and banks no deeper than 6.6 m.

**The long lap (class B, 2★, two laps), 3.80 km.** Richard's direction: the long lap should "turn back in on itself"
and add "a highly technical extended section… challenge, depth, and excitement", not be a bypass of the short lap.
- It goes straight on at the Eira Velha, so its main straight is 690 m.
- A big stop at the Curva da Ribeira (R30).
- The fast Esses da Ribeira, then the Alto da Ribeira: a 3 m carved crest taken flat at R220.
- The Gancho do Casal: a hairpin back in on itself.
- The technical infield among the almonds:
  - the Chicane do Lagar;
  - the Caracol, a 185° carousel at R45;
  - the switchback Esses do Pombal;
  - the Curva da Adega.
- A square T at the Curva do Palheiro, rejoining before the Carvalhal.

Both junctions are square Ts. The first lap's narrow fork left lips, and both arms had to be held level to fix it.

**Estimated lap times**, from a point-mass model on the line with grade (class C from the skill; class B from its
class table). On 21 Oct the AI averaged 23.9 m/s round the old loop at skill 10, close to this model's class C pace.

| | Short lap | Long lap |
|---|---|---|
| Class C | 104 s | 162 s |
| Class B | 96 s | 150 s |

The races are two laps each, about 3.5 and 5 minutes. Class A may take the long lap at three laps later (Richard).

**Code:**
- `Vinelands.CircuitSpec` and `ExtSpec`; straights solved to close; `Carves` for the crests and the dip;
  `CircuitGrade` is 0.10; junctions at `ValeAt`, (−470, 269) and `PalheiroAt`.
- The farms that stood on the new lines moved: the Casal das Amendoeiras and the Casal da Ribeira.
- The paddock is in the infield between the straight and the Subida.
- Fields are now cleared on a 5 × 5 grid.

## Architecture
- **Reused:**
  - the douro farmhouses;
  - `quinta_eira`, `quinta_alpendre`, `barn`;
  - `portela_saude`, as the Clube Automóvel's clubhouse;
  - `tent`, `market_stall`;
  - `tree_blossom` (almonds), `olive_tree`, `tree_round` (cork oaks);
  - `vila_horta`, `vila_figueira`, `reeds`;
  - `slate_stack`, tinted as the cork depot's planks.
- **No new kit.** The Vale is land, trees and the circuit.

## Life
- **Named resident:** Sr. Rogério at the clubhouse, who explains both laps (the career calendar comes with the race
  wiring).
- Cars in the paddock, spectators at the corners, a car on the Vale road.

## Done when
- From the Largo's west edge and the Eiras, the Vale reads as varied farmland with the circuit laid on it.
- Both laps are clean in the audit: no seams, lips or banks.
- A race runs on each lap.

## Not in this zone
The career calendar entry and the AI sweep at skills 3, 7, 10 and 13 (Bramblewick's launch bar) are the race-wiring
step, done once the whole place is built.

## Build rounds (22 October)
Code: `VinelandsVale.cs`; the laps in `Vinelands.cs` (`CircuitSpec`, `ExtSpec`, `Lap`, `LapLong`); race ids `vinelands` and
`vinelands-long` in `RaceTrial` and `tools/run-technical-trial.ps1`.
1. **First build.** The long lap's junction overlapped the Gancho: it left 30 m before the hairpin. The extension now
   leaves the west leg after the esses and runs round the Gancho's outside, keeping 39 m from the lap and meeting it
   again square on. Long lap: 2.34 km.
2. **Junction heights.** On the lap's grade, the junctions stood 1.3 m off their arms. The lap is now profiled in four
   pieces that meet level on 20 m landings.
3. **Steps.** Pinned to the natural ground, one piece needed 12 % and broke into 6 m steps. Each junction now takes the
   height the whole lap's profile gives it, so the steps are gone.
4. **The paddock.** Level with the straight, it stood as a table with cliffs. It's now at the infield's own height,
   70 m west, clear of the straight's corridor.
5. **Fields.** 113 walled fields (stubble, pasture, plough) across the shelf, growing larger westward. They were lifted
   into cushions by the paving filter; a ground-hugging mesh replaced it. The cork stacks floated on their slope and
   were removed.

Audit after round 5: nothing off the ground or floating in the Vale. The circuit's seams are 0.18 m at most, at its
three junctions.
