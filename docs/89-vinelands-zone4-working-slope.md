# 89. Vinelands, Zone 4: the working slope (brief, 22 October 2026)

The fourth zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, section 5).
Frame as in docs/86: `a` metres east, `n` north of the Largo's middle. Built behind `--vinelands`.

## What this place is, and why it's here
The terraced amphitheatre between the town and the river is why the town exists. Schist was broken into stone-walled
benches every 5 m of height so vines could grow on a slope too steep to stand on. Three layers of ownership shaped what
grows where:
- **The townsfolk's smallholdings (Courelas de Cima)**, near the town so they can be tended after work. Each little plot
  is its own thing: a few rows of vines, a kale plot, an olive or two, a fig, a hut and a water tank, a wall across the
  bench where the neighbour's begins.
- **The three quintas' land:**
  - **Quinta do Pombal** (north) is the best kept. Its old terraces sit alongside Vinha Nova, planted in 1998 straight down
    the slope the modern way, with no terraces, on wires and posts. It also has an orange grove fed by its spring.
  - **Quinta das Lajes** (middle) has old field blends 70–100 years old, bush vines on traditional terraces. Its walls are
    slate slabs, which give it its name.
  - **Quinta do Fojo** (south) is half abandoned. The manor is a ruin with a fig growing inside it, and the family lives
    in the old stable.
- **The mortórios:** abandoned terraces at the steep tops and where heirs disagree. They have gone grey with broom (gold
  in flower) and rock-rose, with a wild olive here and there and the walls collapsing.

The **Estrada do Meio**, the tractor road along the mid-slope, links the quintas to the Adega. At harvest the tractors
queue along it. **Casal do Meio**, a hamlet of farmhouses round a threshing floor (eira), stands where it crosses the
Calçada, halfway up, so the workers didn't have to climb 140 m every night.

From the river it must read as **a staircase of vines**: walls on every step, each parcel a different texture, a few
white quinta houses with their cypresses, a hamlet in the middle, and grey abandoned corners at the top.

## Shape (frame)
- **Parcels:** `Vinelands.Parcels`, from the plan's polygons. They span n −1000 to 700, between the town's edge and the
  Cais. First match wins, so the mortórios come before the parcels round them.
- **The Estrada do Meio:** from the Fojo's gate (580, −700) north through Casal do Meio (590, 20) to the entry road at
  the Adega (900, 985).
- **The quinta tracks:** zig-zag farm tracks up from the Estrada do Meio, 4 m wide, in schist-brown earth:
  - Pombal from (690, 400);
  - Lajes from (622, −300);
  - Fojo from (580, −700).
- **The quinta yards** (cobbled, level):
  - Pombal (475, 398);
  - Lajes (565, −288);
  - Fojo (432, −705).
- **Casal do Meio:** the threshing floor at (575, 58), its houses in three rings round it.

## Architecture (Car Forge kit `quinta`, plus `douro`, `vila` and `cais` pieces)
- **New pieces:**
  - the lagar (press house);
  - the dovecote (pombal);
  - the workers' cottages (a row of six);
  - the field hut (casebre);
  - the threshing floor (eira);
  - the ruined manor;
  - the open farm shed;
  - harvest crates;
  - the quinta gate with its name.
- **Reused:**
  - `quinta_casa` (the manors);
  - `casa_douro_a/c/d` (farmhouses);
  - `cais_capela` (chapels);
  - `vila_cipreste` (the Pombal's avenue);
  - `vila_horta`, `vila_figueira`, `vila_laranjeira`, `vila_tanque`;
  - `olive_tree`;
  - `tractor`.
- **Built in code:** the vines, scrub and walls, as merged meshes (`Batch`).
  - Old vines are individual low bushes, three rows to a bench near its outer edge, about one in nine missing.
  - Vinha Nova is trellised rows straight down the slope on posts.
  - Broom is grey-green lumps, a fifth of them in gold flower.
  - Every riser gets a schist band. In the mortórios the band breaks, with fallen stone at its foot.

## Life (cars only)
- **Named residents:**
  - Dona Teresa at the Pombal;
  - Engenheiro Rui at the Lajes;
  - Sr. Antero at the Fojo;
  - Tio Manel at the eira.
- Tractors in the yards and harvest crates by the lagares.
- A van on the Estrada do Meio.

## Done when
- From the Cais and the Largo the slope reads as terraces with distinct parcels, not one repeated texture.
- Each quinta is a place with its yard, its buildings and its track.
- Casal do Meio reads as a hamlet.
- The area audit has no new defects, and the frame rate holds.

## Build rounds (22 October)
Code: `VinelandsSlope.cs` (a partial of `VinelandsVila`). Kit: car-forge `quinta` v1 (c839478).
1. **First build.** About 112,000 vines, 420 olives and 45 km of terrace wall as merged meshes, at 1.8 ms a frame from
   the air. Two problems:
   - The mesh batches crossed 65k vertices, and their 16-bit indices wrapped into giant green sheets across the slope.
     Batches now split before a shape starts past 60k and use 32-bit indices.
   - Several quinta buildings were refused, on slopes or touching roads. They moved to clear, level sites.
2. **The Adega junction.** The entry road was profiled in two halves to meet the Estrada do Meio level. Its first try
   pinned the junction to the natural ground, 11 m above the road, so the road fell in steps. It now uses the height the
   road had there in one piece.
3. **Winding and shape.**
   - The bushes faced inward and showed as hollow tents; they now face out.
   - Vines are organic: tapered, irregular, turned a few degrees, in two shades.
   - Casal do Meio's houses stand along the arms of its crossing (17 became 43). Its rings round the eira mostly fell on
     the roads.
   - The Estrada do Meio arrives at the entry road from due south. At 29° to the entry road's arm, the junction had
     ballooned.
4. **Scrub and ground.**
   - Broom is round lumps, not boxes.
   - The plot walls are short and light. They had stood as dark bars the width of the bench.
   - An olive grove covers the unterraced slope above the Cais.
   - The terraces reach to within 100–130 m of the river.
   - The junctions have level pads at their roads' height, and the quay's paving meets the river road's edge.
5. **Finishing.**
   - The grove extends into the Ribeira's bairro, so the slope behind the praça is no longer bare.
   - Plot walls are laid in 2 m pieces on their own ground.
   - The broom is smaller.
6. **Junction seams.**
   - The Estrada do Meio runs on past the Fojo's gate as the plan's Ligação do Fojo, so the Fojo track joins mid-road,
     not at its end.
   - Both roads have 30 m level landings at Casal do Meio.
   - Vinelands' junction ground sits 5 cm under the road, not 20.
