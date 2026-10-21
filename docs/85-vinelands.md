# 85. Vinelands: the brief (21 October 2026)

> **Restart, 21 Oct evening.** Richard scored the first build 2/10: "a T junction with a church", empty, copy-pasted vines,
> a lazy circle of fields. Bayview and Hillcrest are about 8/10. On his direction, a city planner (an agent) planned a
> real town, not a game level: `review/street-review/vinelands-town-plan.md`. Vinelands is rebuilt from it, **zone by
> zone**. Each zone has a natural shape, its own reasons, architecture, building types, streets, features and life. Each
> gets its own assets before placement, then a picture check against the 8/10 bar.
> The sections below "Restart" are the first build's record. Its land generator, terrace stepping, douro kit, race wiring
> and track tools are kept as tools; its layout is not.


This is the second place on docs/81's list, the class C main town. Its region plan from 7 Oct is
[doc 68](68-vinelands.md), and Richard's answers there still stand: Douro Portuguese, a real-style hill climb, and the
old roadster.

Richard, 21 Oct:
- "Please spin up a game design agent to provide concepts and ideas for the next location... take the ideas you like
  and leave the ones you don't... then continue with the creation."
- "For the track, don't forget to use the skill."
- "Don't worry about activities for now."

The agent's report is `review/street-review/vinelands-concepts.md`. It's built behind `--vinelands` (off) until judged.

## What I took from the design agent
**Kept:**
- **Identity:** "the town you read from across the water", with "a hundred years of one road" as its story. You see
  the whole amphitheatre of terraces from the far side before you drive up into it. The other places are corridors or
  spirals; Vinelands is open, lit and horizontal: contour stripes, a wide river and long sight lines.
- **One road through all three tiers.** The Cais (the quay) is at the river. The climb road runs up the amphitheatre to
  the Belvedere, and the Rua da Torre comes down the ridge into the vila (the town). There's no parallel short cut to
  kill the climb.
- **Landmarks:**
  - the amphitheatre of schist-walled terraces, with three quintas' names in giant letters;
  - the switchback stack, seen whole from the Cais;
  - the iron bridge;
  - the Cais, with wine boats and painted lodges;
  - the Belvedere bell tower;
  - the Adega yard;
  - the cork grove;
  - the far bank's abandoned terraces (scenery for now).
- **Arrival:** through a walled cutting, then the reveal. The Pit Stop stands at the town gate, facing the view.
- **The two tracks' sections, and six hairpins that each differ.**
- **Life:** cars only. Wine tankers, barrel trucks, tractors with grape trailers, Ape-style three-wheelers, wine boats.
- **Palette:** schist grey-brown, whitewash grading to cream, azulejo blue, terracotta, ruby-port trim, vines at harvest
  gold.
- **Its warnings:**
  - Don't let it read as Hillcrest 2: lead with schist stripes, blue tile and the river, not cypress and stucco.
  - Vary the terraces (wall heights, bench widths, steps, a tiny chapel, abandoned benches).
  - Invent the town; don't copy Pinhão.

**Dropped or deferred, and why:**
- The quay tram, the station underpass and the harvest train: the Main Line doesn't reach here yet.
- Monorail carts, the Barrel Race and all things to do: activities wait for Richard's sweep.
- The cobble wave mosaics: decal work for the art pass, if at all.
- **The hill climb as an event:** it's class A, with staggered solo starts, a new race format. Its road is built and
  designed now, since it's the town's main road, and it becomes an event when class A arrives.
- **A second class C race:** deferred. The Vineyard Loop is the C race.

## The land (Vinelands' frame: `a` metres east, `n` north of the vila at (3400, −3280))
| Place | Where | Height |
|---|---|---|
| The vila (the Largo) | (0, 0), on the ridge | ~120 m |
| The Belvedere | (−590, −614), the ridge's knoll | ~200 m |
| The Cais and the river | about a = +950 at n = 0; the river curves north and south | 6–10 m |
| The amphitheatre | the east face between the ridge and the river | ~12% average, terraced |
| The Vale | the back slope west of the ridge | 100 m falling to ~45 m |
| The far bank | east of the river | rising to ~150 m |
| The arrival | Vinelands Road from Bramblewick's gate (2780, −2040) over the ridge's north shoulder | |

## The tracks (the track-design skill; `tools/track-eval.py --loop / --p2p`)
### Vineyard Loop: class C, club, about 1.68 km, anticlockwise, 9 m wide
Its pieces are `review/tmp/vlr5.txt`. The lap starts on the Adega Straight; the two straights marked L1 and L2 below
are solved so the lap closes.

| Section | Pieces | Purpose |
|---|---|---|
| Adega Straight | S L2 (303 m) | The main straight, downhill west through the Adega yard. The draft. |
| Lagar | A 60 38; A 60 60 | The slowest corner after the longest downhill: **the main pass**. It opens on exit. |
| Lagar Kink | A 15 200 | Flat, onto the bench. |
| Long Bench | S 60; A 30 160 | Climbing along a terrace: wall on one side, a drop on the other. |
| Wall Esses | A −40 40; A 75 30; A −45 35 | Left, right, left between dry-stone walls: **the mistake generator**. |
| Press Dip | S 30 | A compression over the levada culvert, then a light crest. |
| Cork Run | A −40 80; A 40 60; A −50 70 | Rhythm through the cork oaks, uphill. |
| Cooperage Hairpin | A 150 22 | The tight hairpin into the cooperage yard at the top: the slowest point. |
| Quinta Straight | S L1 (74 m); Barrel Kink A −14 150, A 14 150; S 90 | The second straight, broken by a flat kink over a crest (**commitment**). |
| Quinta Gate | A 105 65 | Medium-fast, between the quinta's gateposts: **the second pass**. |
| Olive Grove | A −55 70; A 55 70 | Medium-fast esses onto the main straight, so followers stay in the tow. |

**Metrics against the bands:**
- 14 corners (8.3/km, counting the two kinks);
- 504°/km, macro 379;
- 52% curved;
- the longest straight is 18% of the lap; the second is 0.30 of it;
- 8 left, 6 right; 10 direction changes; 51 m self-clearance;
- slowest 14.3 m/s; an estimated 71 s lap.

**Known deviation:** the hull ratio is 1.03, against 1.05 or more. The lap is a loop round the Adega's own vineyard. For
a 1.7 km club lap that's normal; the skill's short club circuits measure 1.06.

### Vinelands Hill Climb road: about 3.7 km, point to point, built now, raced in class A
Its pieces are `review/tmp/hc1.txt`.
- **Cais Start**, then **Rabelo Reach**: about 1.1 km of river sweepers (R 220–300), where time is made.
- **Station Kink**, then **Lodge Corner**, a 92° right between the lodges onto the terraces.
- **The stack:** six different hairpins, 150–200 m apart, zig-zagging up the face:
  - Letter (wide, under the painted name);
  - Cellar Cut (R16, the tightest, blind in a cutting);
  - Press (a double apex);
  - Monorail (R24);
  - Outer (R30, off-camber, the viewpoint on the outside);
  - Last (tightening onto the bench).
- **Long Bench:** 400 m, the breather.
- **The Lip:** a blind brow, then a left sweeper.
- **Belvedere Avenue:** the finish at the tower.

**Metrics:**
- 3.68 km; 190 m of climb (5.1%);
- 14 corners, half of them hairpins, 7 left and 7 right;
- 56 m self-clearance between legs;
- the slowest corner 12 m/s for class C.

It has fewer corners per km than the hill-climb band (3.8 against 5–10). That's deliberate: the fast river half, then
precision. The band is an estimate.

### Short and long versions (Richard, 21 Oct: "certain tracks have a short and long version")
- **Short:** the Vineyard Loop is the short version, raced by class C over 3 laps.
- **Long ("Vineyard Grand"):** class B and up race a long version. It keeps most of the loop and breaks out at the Adega
  junction onto the ridge roads, perhaps out past the Belvedere and back, for a lap of roughly 3.5–4.5 km.
- **Higher tiers** still visit the short loop now and then, with more laps.
- **Status:** the long layout is designed with the track skill when class B reaches Vinelands. The Adega junction is
  where it branches, so nothing built now blocks it.

## Build order (each round: build, check, targeted review)
1. **Land and roads (graybox):**
   - the frame; the land (ridge, river, amphitheatre face, vale, far bank) blended into the valley;
   - the grid grown east and south, plus `Outside`, the map, regions and the cache key;
   - Vinelands Road from Bramblewick's gate;
   - the climb road, the Rua da Torre, River Road;
   - the Vineyard Loop with its spur.
   - Checks: the area audit, and the old world unchanged without the flag.
2. **The Vineyard Loop's race:** wiring, plus a sweep at skill 3, 7, 10 and 13, 24/24.
3. **The terraces:** a terrace generator (walls as instanced strips, vines as row strips), and a frame-cost study.
4. **The town and its kits:**
   - whitewash and tile houses; the church; the Belvedere tower; the Adega;
   - the quintas and their letters; the Cais lodges; the iron bridge;
   - the Pit Stop at the town gate.
5. **Life:** cars only.
6. **The launch loop:** views, race photos, reviews; the flag comes off.

## Progress
**21 Oct, rounds 1–4, behind `--vinelands`** (it needs Bramblewick: its road runs on from Bramblewick's gate).

**Land** (`Vinelands.Natural`, blended into Bramblewick's vale):
- The ridge: the vila at 150 m, the Belvedere's knoll at 230 m, the north shoulder at 108 m.
- The amphitheatre: about 14% average, steepening towards the top.
- The river: 100 m bank to bank, 5 m deep. Its south gorge bends, so you can't see out to the map's edge.
- The Vale's shelf for the loop, the far bank, the rim hills, and a coast falling to the sea bed.
- The valley grid now reaches east to x 5250 and south to z −5130: 7.3 M cells, built in about 1.8 s (cached in release
  builds).
- Round-by-round fixes:
  - a floating slab (land beyond the district now grades to the sea, and the sea bed is −40 m, not −16 m);
  - a 22% Vale flank (now 400 m wide);
  - a too-deep arrival cutting (the shoulder lowered);
  - a narrow river ditch that hid its water.

**The terraces, the signature:**
- The land steps the amphitheatre every 5 m of height: a gently rising bench, then a steep riser.
- `VinelandsTown.Terraces` lays a dry-stone band over every riser and rows of vines along the planted benches, as merged
  strips (contours found by bisection every 3 m). Some benches are left bare, as abandoned terraces.
- From across the water the face now reads as terraced vineyards.
- Round 3 laid 247 km of vines (881k triangles), so round 4 trims it to five rows a bench.

**Roads** (all through the valley's road network):
- Vinelands Road from Bramblewick's gate (1.45 km, through the shoulder's cutting).
- The Subida (5.9 km): River Road's north end, the Cais, the climb, the Belvedere, the Rua da Torre, the Largo.
- Adega Lane, the Vineyard Loop, and the Adega Vines track.
- Junction fixes:
  - The Largo's three arms are spread to about north, west and south-south-west.
  - Adega Lane comes square into the Adega from the north; at 7° the junction swelled to 80 m.
  - The loop is split at Press Dip into two roads, so no junction carries one road's two ends; that left a gap under the
    start line.

**The town** (the `douro` kit in Car Forge, 20 pieces: houses a–g, the church, the Belvedere tower, the adega, the wine
lodge, the quinta, the rabelo):
- The Rua Direita and the Rua da Torre: tight streets with each house's design differing from its neighbour's, and
  shops and cafés with signs near the Largo.
- The Largo: the church, the fountain, plane trees, lamps.
- The Pit Stop at the town gate, the Adega, the Belvedere tower in the road's hairpin.
- The Cais: three named wine lodges, a granite quay wall, rabelos moored.
- Three quintas with their names in giant letters, read across the water.
- Area audit, town: buildings, designs, wall and roof colours, lamps and planting all pass. Residents, life and things to
  do wait for their rounds (things to do wait for Richard's activities sweep).

**Fixes outside Vinelands, found on the way:**
- `BankBarriers` tried to lay armco (single pieces, not a run) when dry stone and post-and-rail both failed, and threw.
  Kits must now pass `Runs.Runnable` first.
- Its site cache keys by Bramblewick and Vinelands too.
- Rabelo boats are allowed to float in the placement audit.
- `AreaAudit.ExtraNature` counts planting built as merged meshes.

**Tools:** `tools/track-eval.py --loop / --p2p`, and `tools/track-plot.ps1`, which draws a design.

**Next:**
- Round 4's results: the AI sweep on the Vineyard Loop (skill 3/7/10/13), the vines' frame cost, the town's views.
- Then the loop's race wiring for the career (a host at the Adega), life (cars), and the launch loop.

## Restart: the town plan (rev. 2) and the zones
**Taken from the plan:**
- **The growth story.** The town is a crest town that grew along the ridge while the slope stayed farmed.
- **The through road misses the Largo.** It crosses the saddle and descends to the river by the co-op. Rua Direita is a
  spur from the saddle.
- **Eight bairros,** each with its own era and reason.
- **The street hierarchy,** down to stepped lanes, courtyards and the Calçada Velha.
- **The public spaces,** a geology-driven countryside, daily rhythms (where life goes), the circuit on the Vale behind the
  ridge, and the Seis Voltas climb in the south bowl.

**Adapted:**
- The town is called Vinelands in the game; the bairros, streets and places keep the plan's names.
- Density is scaled to what plays and performs, so it should feel dense, not literal.
- The circuit and the climb are re-laid with the track skill, from the plan's routes.
- The far-bank railway and halt are later scenery (the Main Line doesn't reach here yet).

**Zones, in build order:**
1. **The Largo and Vila Velha.** The crest core, its stepped lanes and pátios, the church, the town hall.
2. **Rua Direita.** The merchant ribbon from the saddle to the Largo, the market and fairground, the convent garden.
3. **The Calçada and Ribeira do Cais.** The road and the mule steps to the river, the quay, the lodges, the promenade,
   the beach, Ponte Nova.
4. **The working slope.** The smallholder belt, the three quintas and their parcels, the mortórios, the Estrada do
   Meio, Casal do Meio.
5. **The Belvedere and Mirante.** The villas, the cemetery, the windmills, and the Seis Voltas climb.
6. **Alto da Portela and Entroncamento.** The school, pitch, health centre and fire station; the co-op winery quarter
   and the entry road.
7. **Eiras and the Vale.** The west lip, the hortas, the orchards, the montado, the farmsteads, the club circuit and
   its paddock.
8. **The far bank.**

