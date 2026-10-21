# 87. Vinelands, Zone 2: Rua Direita, the Mercado and the Jardim da Cerca (brief, 21 October 2026)

The second zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, sections 1–4
and 7). Frame as in docs/86: `a` metres east, `n` north of the Largo's middle. Built behind `--vinelands`.

## What these places are, and why they're here
Three pieces of the town that the old core grew into, each with its own reason:
- **Rua Direita** (Rua Direita means "the straight street", the old carriage road) runs from the saddle south along the
  ridge to the Largo. When the wine trade made money in the 18th and 19th centuries, merchants built along it:
  - continuous 2–3 storey fronts, a shop below and the family above;
  - tiled or colour-washed fronts with long iron balconies;
  - long gardens behind, falling west off the ridge.
  It's the town's high street: the butcher and the fishmonger, the hardware shop, the grocer with its few parking
  spaces, the barber and the haberdasher. The garage and fuel pumps (the Pit Stop) are here too, where the street is
  wide enough and the ambulance and the delivery vans come in.
- **The Mercado and the Terreiro da Feira:** south of the Largo, off the Mirante road down Rua da Feira.
  - The market hall was built in 1958, in plain modern concrete with a tiled panel.
  - The fairground is a terrace cut into the slope. It takes the Wednesday market and the second-Sunday fair, and on
    other days it's the town's main car park.
  - The buses stop at the top of Rua da Feira, because they can't turn in the Largo.
- **The Jardim da Cerca:** the walled garden of a 16th-century Franciscan convent, north-east of the Largo. After 1834 it
  became the town's garden.
  - The cloister is a ruin of arches along its north wall.
  - Inside: box parterres, camellias, a pond, plane trees and palms, and a bandstand for the band on feast days.
  - Its east wall is low, so the benches look out over the whole amphitheatre to the river.
  - You come in through the convent gate off the Pátio dos Tanoeiros.

It must read as **a busy, slightly grander street** than Vila Velha's lanes (wider fronts, shops all along, pavements,
cars parked half on them), **a working market square**, and **a calm, green walled garden**.

## Shape
- **Rua Direita:** both sides of the road from the Largo's north edge (n 30) to about n 520, ~500 m of frontage. Fronts
  stand on a 2.2 m pavement of calçada with granite kerbs, and the back gardens are 8 × 25–35 m strips.
- **Rua da Feira:** a new road from the Mirante road at (−95, −125), east-south-east past the hall to the Terreiro.
  - The hall is 40 × 22 m at (−30, −160).
  - The Terreiro is a level cut terrace at (50, −200), about 90 × 45 m, ringed with plane trees.
- **The Jardim:** a walled rectangle, a 100–200 and n 110–165, terraced in three steps falling east.

## Architecture (Car Forge kit `rua`)
1. **Merchant houses** (`rua_burguesa_8`, `_10`, two variants each): three storeys on 8 or 10 m frontages.
   - Ground floor: one or two shopfronts in granite surrounds, each with a sign fascia.
   - Upper floors: French windows to the floor, each with its own iron balcony (sacada).
   - Fronts are tiled or washed (the wall tint), with white frames, a cornice and a parapet (platibanda) with stone
     finials (pinhas) at its ends.
2. **A solar** (`rua_solar`): a noble house of 16 m. Two storeys, a granite portal with a coat of arms, tall framed
   windows, corner pinnacles and a hipped roof.
3. **The market hall** (`rua_mercado`): a concrete frame with a raised clerestory roof, a colonnade of square pillars
   along the front, a MERCADO MUNICIPAL sign and a tiled mural panel.
4. **Market stalls** (`rua_banca`): trestles with crates of oranges, cabbages and grapes under an awning.
5. **The bus shelter** (`rua_abrigo`).
6. **The fuel pumps** (`rua_bombas`): two pumps on an island under a canopy, beside the Pit Stop.
7. **The bandstand** (`rua_coreto`): octagonal, on a granite base, with slender iron columns and a lantern roof.
8. **The pond** (`rua_lago`): a granite basin with water and a jet.
9. **The cloister ruin** (`rua_claustro`): four arches on columns, broken at the top, with ivy.
10. **The convent gate** (`rua_portao`): a granite arch with a cross and iron gates.
11. **The garden plants:**
    - box parterres (`rua_buxo`): clipped hedge squares round gravel;
    - camellias (`rua_camelia`).
12. **Granite bollards** (`rua_frade`) on the pavements.

The Pit Stop is the uniform `pit_stop_3` (garage, parts and body shop: every town's).

## Features
1. Rua Direita looking south to the church tower: shops, balconies, wall lanterns, cars half on the pavement.
2. The Pit Stop and its pumps on the street's west side at about n 400.
3. The market hall's colonnade and stalls; the Terreiro full of parked cars under the planes; the bus at its shelter.
4. The Jardim: the convent gate, the cloister arches, the bandstand, the pond, the parterres, and the parapet view east.

## Life (cars only)
- **Named residents:**
  - the Pit Stop's three: Sr. Armando at the garage, Inês at parts, Cátia at the body shop;
  - Sr. Vítor, the bus driver, at the terminus;
  - Dona Amélia, at her fruit stall by the hall.
- Cars parked half on Rua Direita's pavements, at the grocer's and on the Terreiro.
- More cruising cars on Rua Direita and Rua da Feira.

## Edges
- **South:** the Largo (zone 1). Rua Direita's first fronts close the Largo's open north side.
- **North:** the Portela (zone 6), at n ~520.
- **West:** the Vale shelf (zone 7) below the back gardens.
- **East:** the amphitheatre (zone 4) below the Jardim's parapet.

## Done when
- Each feature reads as itself in pictures, and the street reads as one continuous merchant street.
- The Pit Stop works: garage, parts and body shop, and it's in the "back to a garage" list.
- The area audit for the town has no defects in the zone (no seams, lips or steep banks), and the Pit Stop is counted.

## Build rounds (21 October)
Code: `VinelandsRua.cs` (a partial of `VinelandsVila`: zone 1's rows, buildings and yards serve both). Kit: car-forge
`rua` v1.
1. **First build.** Rua Direita read well at street level: continuous coloured merchant fronts, signs, lamps, the
   pavements with their kerbs. But:
   - Stopping the road's gravel shoulders under the pavements left 188 road-edge lips (the shoulders are the ramp from
     the edge to the ground). Rua Direita keeps them now, and the pavements lie over them.
   - The market hall and the Pit Stop couldn't stand on the slopes, and the garden's pond and bandstand were refused.
2. **Shape.**
   - Rua Direita bends gently, as the old carriage road would.
   - Four travessas break the frontage: Fonte Nova, Arco, Solar, Flores. Each is cobbled, 30 m deep, with bollards at
     its mouth and the houses' gable windows on it.
   - The hall moved to the Terreiro's north edge, so the fairground is its forecourt.
   - The garden's pieces stand on terrace plinths.
   - The Mirante road meets Rua da Feira level at the bus terminus: two profiles with landings, because the junction on
     the 11 % grade sank under the carved ground.
3. **Levels and kerbs.**
   - Rua da Feira's profile had no solution: 26 m to fall in 179 m at 10 %, so it fell 9 m in its first two metres. The
     Terreiro is now a cut-and-fill terrace no more than 15 m under the terminus, and the street winds round to arrive
     level at its corner (round 5).
   - Lamps stand at the back of the pavement. On the kerb line they pushed the parked cars out into the traffic lane.
   - Refused landmarks write their reason to the log (`LITTLE_ROAD_VINELANDS_REFUSED`).
   - The Pit Stop stands at n ~400.
4. **The hall's plinth.** The fill falls 4.8 m behind it.

Audit after round 4: the town region has no seams or banks, one lip, the Pit Stop counted, life 38 per road km, and
planting passing. Only "things to do" is short, and that waits for the activities sweep.
