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
