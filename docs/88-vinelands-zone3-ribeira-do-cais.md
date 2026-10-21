# 88. Vinelands, Zone 3: the Calçada and the Ribeira do Cais (brief, 21 October 2026)

The third zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, sections 1–4,
6 and 7). Frame as in docs/86: `a` metres east, `n` north of the Largo's middle. Built behind `--vinelands`.

## What this place is, and why it's here
The river is why the town has money. In the 18th century the wine came down from the quintas and rested in the lodges at
the river's edge. Then it went down to the coast in square-sailed boats (rabelos). The Ribeira do Cais grew round the
quay:
- the lodges;
- the boatmen's houses, built on stone stores because the river floods up to the ground floor;
- the tascas where the quay ate;
- a chapel for the boatmen's saint.

The Calçada (the road, in Z bends) and the Calçada Velha (the mule path, steps and ramps) climb the amphitheatre to the
town. In 1957 Ponte Nova, three low concrete arches, replaced the ferry where rock bars narrow the river.

It must read as **the river's front door**: a working quay with boats and barrels, painted lodges with their names along
their fronts, a small square where the road arrives, an evening promenade, and the town stacked up the amphitheatre
above it all.

## Shape
- **The quay:** a = 978–997 between the river road and the water, n −62 to 62. Paved in granite setts, with a stone wall
  down into the river.
- **The slipway** at n −35, and **the pontoon** at n 8 with two rabelos alongside.
- **The Praça do Cais:** a 930–966, n −18 to 22, where the Calçada meets the river road. It's paved in waves like the
  Largo, with tascas on its north and south sides.
- **The lodges** across the road from the quay, 40 × 18 m each:
  - Borges Velho, at n −48;
  - Sampaio, at n 56;
  - Cruz & Filhos, at n 112.
- **The chapel** of São Pedro Gonçalves at n −96.
- **The boatmen's houses** along the river road's town side, n 140–305 and −112 to −220.
- **The Passeio Ribeirinho:** the river side of the road, n 64–330, with planes, benches and the railing. It ends at the
  river beach, Praia da Senhorinha (n ~270).
- **Ponte Nova:** off the river road at n −70, east across the river (abutments at a 998 and 1108) to the far bank's foot.
- **The Calçada Velha:** steps from the town's east edge (222, 5) down to the Praça do Cais. They break where they cross
  the road's bends.

## Architecture (Car Forge kit `cais`)
- **The lodges** (`cais_armazem`, three variants): two storeys over granite, tall arched cellar doors, small barred
  windows, a long hipped roof, the name painted along the front. One has a loading hoist, one a visitors' shop.
- **The boatmen's houses** (`cais_casa`, two variants): a stone store below, the home above, an outside stair to a
  covered landing (balcão).
- **The chapel** (`cais_capela`): whitewash, granite, a bell gable.
- **Ponte Nova** (`cais_ponte`): a concrete deck on three segmental arches, two ribs each, spandrel columns, piers with
  cutwaters, balustrades and lamps. The game's road follows its deck curve, and the parapets are solid.
- **The pontoon** (`cais_pontao`), **the slipway** (`cais_rampa`), **bollards** (`cais_cabeco`), **barrels** (`cais_pipas`)
  and **the promenade's railing** (`cais_guarda`).
- **Reused:** the vila houses for the tascas, the café terraces, the cross; the douro kit's rabelos.

## Life (cars only)
- **Named residents:**
  - Mestre Fernando at the boats;
  - Dona Graça at Lodge Sampaio;
  - Sr. Alberto at the Tasca do Rio.
- Cars nosed up to the river on the quay and parked round the praça.
- A cruising car over Ponte Nova.

## Also in this round (Richard, 21 Oct: "tidy up the garage stops as they are filled with grass")
The Vinelands Pit Stop stood on the falling slope west of Rua Direita, and the hill showed in its bays. It now has its
own level pad at the street's height, set back 23 m, with a paved forecourt out to the pavement. Hillcrest, Quarry Hills
and Bayview were checked in pictures and are clean.

## Done when
- From the quay, the lodges, the praça and the town above read as one place.
- The bridge spans open water on its arches, and a car can drive over it and back.
- The Calçada Velha shows on the amphitheatre as a line of steps.
- The town audit has no new defects.

## Build rounds (21–22 October)
Code: `VinelandsCais.cs` (a partial of `VinelandsVila`). Kit: car-forge `cais` v1.
1. **First build.**
   - The Pit Stop's bays were clean on their new pad, but grass showed on one side of the forecourt and a garden wall
     crossed in front of the body shop.
   - The quay's pads pushed land out into the river.
   - Two of the three lodges were refused: one overlapped the praça's houses, one sat against the slope behind.
   - The Passeio's benches and lamps floated over the falling bank.
2. **Fixes.**
   - The forecourt is now a grid at the ground or the floor, whichever is higher, across the whole front.
   - Walls and yards never cross a building's footprint.
   - The lodges moved clear of the praça and may cut into the slope behind (they are cellars).
   - The river road flattens 11 m each side, so the Passeio and the quay stand on it.
3. **Levels.**
   - The praça's pad moved onto the praça (it had climbed the slope in a mound).
   - The quay's pads follow the river road's own height.
   - The channel under Ponte Nova is re-cut between the abutments, 34 m each side. The road's corridor had built an
     embankment beside the deck.
4. **Ponte Nova.**
   - Off the bridge, the road turns south along the far bank. Straight on, it carved a cutting into the hill that looked
     like a tunnel mouth.
   - Its approaches have gravel shoulders (52 lips without them), and its parapets cover the whole deck.
5. **The quay's corners.** Seven wider pads, so the paving no longer dips into the river at its ends.

Audit after round 5 (the town and the vale):
- No floating pieces, no steep banks, no cars off the ground.
- 4 lips of 0.16 m beside the praça, on the river road's west edge.
- 5 small seams: four at Ponte Nova's far abutment and at the far-bank lane's dead end, one at the bridge junction.
  The far bank is zone 8's, and the lane gets rebuilt there.

The Bayview seabed rocks (rock_a, rock_b, rock_cluster) float up to 18 m over the sea floor, under water. Nobody sees
them; they're noted here, not fixed.
