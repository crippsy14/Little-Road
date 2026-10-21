# 93. Vinelands, Zone 8: the Outra Banda, the far bank (brief and status, 22 October 2026)

The last zone of the rebuild (docs/85), from the town plan (section 5, "Far bank"). Frame as in docs/86. Built behind
`--vinelands`. **Status: in progress.** Round 2 was stopped before it finished; this doc says where it stands.

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
- **Lanes:** Ponte Nova ends level at the far bank's T (a 1122). From there, the Caminho do Corvo runs south and the
  Caminho da Outra Banda runs north and up by a switchback to the Aldeia.
- **The bank:**
  - the café and about 16 houses along the lane;
  - the groves and the plots;
  - about 10,000 young vines in patches, 4,000 broom lumps, and 700 pines.
- **The Aldeia:** 17 houses round its largo, with the chapel, the cross and the festa bunting.
- **The ruin and its fig.**
- **Residents:** Dona Ilda (the Aldeia) and Sr. Joaquim Corvo.

## Round 1 findings, fixed in code but not yet seen in a build
- **The rim hill.** The district's rim made the far bank a wall: it rose 60 m within 150 m of the bank. The lanes asked
  for more than 20 %, broke into steps, and carved cliffs. The Aldeia sat in a pit, and the ruin was refused (an 18 m
  rise). Now the far bank facing the town (n −650 to 650) has no rim and no vale blend, as was done for the south bowl
  and the Entroncamento.
- **The lanes.** Each lane's end height is capped at what its grade can reach. The Aldeia lane takes a switchback, and
  the ruin has its own terrace at the lane's end.

## The circuit's first race (study 247), found while testing zone 7
- **The long lap's 24-car race.** 0 of 24 finished and 17 cars needed assisted resets: the whole grid stuck behind the
  start line, where the ground stood 18 cm over the road. The start line now has a level pad, also not yet seen in a
  build.
- **The short lap.** Its race hadn't run: the race code checked the lap before the roads were built, so the lap was
  always null. That's fixed too.

## Next
1. Build and shoot round 2 (scratchpad `vl-z8.ps1`, output `vl-z8b`).
2. Check the far bank's land, the lanes, the Aldeia and the ruin.
3. Race both laps (study 247, `tools/run-frame-cost.ps1 -Track vinelands|vinelands-long`). Then the AI sweep at skills
   3, 7, 10 and 13 (Bramblewick's launch bar: 12 out of 12 races with 24/24 finishing).
4. The career wiring: a calendar entry and Sr. Rogério's race menu.
