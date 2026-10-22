# 93. Vinelands, Zone 8: the Outra Banda, the far bank (brief and status, 22 October 2026)

The last zone of the rebuild (docs/85), from the town plan (section 5, "Far bank"). Frame as in docs/86. Built behind
`--vinelands`. **Status: built; land and circuit fixes in round 4.** The races wait for Richard's go.

## What it is
The far bank is the view the whole town looks at. It holds:
- **By the water:** the strip at the bridge's foot, where the old ferry landed, with the Café do Barqueiro, a handful of
  houses, orange and lemon groves, and kitchen plots.
- **On the slope above:** young vines replanted by small growers, then abandoned terraces gone to broom, then pine.
- **The Aldeia da Outra Banda:** a village on its spur, with 20 houses, eight people all year, and the chapel's August
  festa (bunting across its largo).
- **The Quinta do Corvo:** a ruined manor at the end of the lane south, with its huge fig and three replanted hectares.
- **Railway:** the plan's far-bank railway and halt wait for the Main Line.

## Built (VinelandsOutraBanda.cs)
- **Lanes:** Ponte Nova ends level at the far bank's T (a 1122). From there, the Caminho do Corvo runs south along the
  water and the Caminho da Outra Banda runs north, then up by a switchback to the Aldeia.
- **The bank:**
  - the café and 15 houses along the lane;
  - the groves (53 trees) and the plots;
  - about 10,000 young vines in patches, 4,000 broom lumps, and 700 pines.
- **The Aldeia:** 16 houses round its largo, with the chapel, the cross and the festa bunting.
- **The ruin and its fig** at the Corvo lane's end.
- **Residents:** Dona Ilda (the Aldeia) and Sr. Joaquim Corvo.

## Rounds
1. **The rim hill.** The district's rim made the far bank a wall: it rose 60 m within 150 m of the bank. The lanes asked
   for more than 20 %, broke into steps, and carved cliffs. The Aldeia sat in a pit, and the ruin was refused (an 18 m
   rise).
   - Fix: the far bank facing the town (n −650 to 650) has no rim and no vale blend, as was done for the south bowl and
     the Entroncamento.
   - Fix: each lane's end height is capped at what its grade can reach. The Aldeia lane takes a switchback, and the ruin
     has its own terrace at the lane's end.
2. **Seen in the review tour.** The strip, the Aldeia and the ruin all stood right, but dark scars ran up the hills
   north and south of the far bank and across the sea gap by the Adega.
   - Cause: the rim hills were switched on at the river channel's edge (100 m from the water), so they stood as a cliff
     up to 55 m tall along both banks.
   - Fix: they now rise from the banks over 200 m.
   - Fix: the far-bank mask also fades in across the channel instead of switching on at the river's middle line.
3. **The circuit's steps** (found by the audit, and the real reason the first race's grid stuck). The road jumped 2 m
   at the start line, at the Vale road's junction, and 3 m at the long lap's return.
   - Cause: each piece of the lap ran between junction heights taken from the whole lap's 8 % profile, but with its
     20 m level landings a piece can't climb that much. The profile then forced its ends into a step.
   - Fix: the junction heights are relaxed until each piece can reach the next.
   - Result: all three junctions' arms now meet within 0.2 m, and road seams fell from 23 to 9.
4. **The river and the long lap's junction.**
   - The vale blend raised the river's bed north of the Aldeia, burying the river under a hill: its water ended in a
     straight edge. The channel is now kept out of the blend.
   - The river's surface stayed at Water (6 m) to the district's edge, 18 m above the beach. It now falls to the sea
     over its last 180 m, as rapids into the bay (`Vinelands.WaterAt`).
   - The long lap leaves the short lap at a narrow angle. Its junction's level patch reached 45 m down the lap, which
     fell 2 m under it. Both arms are now level for 50 m there.

## Race wiring (behind `--vinelands`)
- **Class C:** the Circuito da Cumeada, three laps of the short lap, 3★ (docs/70's Vineyard Loop).
- **Class B:** the Cumeada Tuners, three laps of the long lap, 2★ (docs/70's Vineyard Tuners).
- Both are offered by Sr. Rogério at the clubhouse. The B race shows once class B is open. After a race the player
  returns to the start line. Both events are tagged in `docs/data/race-stars.json`.

## Next
1. Check round 4 (`vl-z8d`): the river north and its mouth, the long lap's junction, and the audit.
2. Race both laps (study 247, `tools/run-frame-cost.ps1 -Track vinelands|vinelands-long`). Then the AI sweep at skills
   3, 7, 10 and 13 (Bramblewick's launch bar: 12 out of 12 races with 24/24 finishing).
3. Clear the audit's last lips and seams, which are small junction seams of 0.07–0.14 m in places.
