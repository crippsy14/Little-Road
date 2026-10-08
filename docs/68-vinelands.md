# 68 · Vinelands: region plan

Richard, 7 October: "Then lets plan out Vinelands".

This doc is step 0 of [doc 57](57-new-area-standards.md)'s process for Vinelands (the brief), plus the district plan, its races and activities, and the build order. It follows [doc 59](59-quarry-hills.md)'s shape. Nothing is built yet.

Its place in the world ([doc 58](58-world-and-campaign-design.md), `tools/world-plan.py place vinelands`):
- **Where:** (9.4, 15.7) km on the continent, which is about (3400, −3280) in game metres. It's 4.6 km from Bayview and 3.9 km from Hillcrest.
- **Climate:** east of the Spine, in the rain shadow, on the edge of the green farming belt that follows the great river north to the coast. The river passes about 1 km east of the town. It's warm and dry, with sun on the slopes: real wine country.
- **Class:** class C, a main town, road discipline. The blueprint describes it as "wine country on the road south".
- **Railway:** a station on Continental's **Main Line** (Bayview → Hillcrest → Vinelands → My City). The Coast Branch passes 1.5 km north through Bramblewick.
- **Roads:**
  - Valley Road South from Hillcrest, via (7.4, 16.9);
  - south-west to the ring at R8, via (8.2, 14.0);
  - Bramblewick to the north;
  - Orchard End to the west;
  - Highmoor to the south-west;
  - Millbrook to the south-east, along the river.
- **Neighbours:**
  - Bramblewick (C village, parts and a recruit), 1.8 km north-west;
  - Orchard End (C, where the Glasswater & Southern line ends), 2.1 km west;
  - Millbrook (B, the mill town below the dam), 1.7 km south-east;
  - Highmoor (B, the rally shed), 2.7 km south-west;
  - Cobble Cove (C, the harbour at the river mouth), 2.8 km north-east.
- **Local parts:** none. Doc 58 decision 13 gives local parts to only five places, and Vinelands isn't one of them.

## The vision

**"The hill that holds the sun."** Vinelands is a river valley of terraced vineyards, after the Douro:
- a great amphitheatre of curving stone-walled terraces climbing about 170 m from a river bend to a ridge-top village and its bell tower;
- white wine estates (quintas) with their names painted in giant letters on the slopes;
- wooden wine boats on the river;
- the main line running along the riverbank, through a station covered in blue-and-white tile panels;
- one road through all of it with the rhythm of the Douro's N222 (named the world's best driving road in 2015). Long sweepers by the river lead into hairpins up the terraces.

The story: it's harvest season when you arrive. The town has run a hill climb up its terraces for a hundred years, and its record is held by a woman in a tiny old roadster. You come as a nobody in a class C car. By the frenzy, the hill climb is a pro event with closed roads and a timing tower, and the whole town turns its square into a festival arena bigger than the town itself.

**Why it isn't Hillcrest or Quarry Hills:**

| Hillcrest | Quarry Hills | Vinelands |
|---|---|---|
| One spiral climb to a castle | Down into the pit, up the galleries | Flowing: river sweepers, then hairpins up a whole hillside of terraces |
| Italian stucco (Gio, Vito) | Welsh slate and chapels | Portuguese whitewash, ochre, blue tiles and terracotta (*question 1*) |
| Still | Industry moving: Blondins, dumpers, the blast | The harvest moving: tractors with grape trailers, the vineyard monorail, wine boats on the river, the harvest train |
| Tarmac | Gravel first | Tarmac at its best: the game's "driving road" |
| Sunny | Drizzle and mist | Hot, gold and green, cicadas, dust on the lanes |
| A castle town | A slate town | A river town: the first navigable river with boats |

**Its look** (doc `art-direction.md`, "toy shapes, real light"): the terraces are the signature. They are bold, readable stripes of green vine rows and pale stone walls, and their curves show the contours from kilometres away. Accents are strong rather than pastel: ruby-red barrels and doors, azulejo blue, ochre and white walls, terracotta roofs.

## The district (500 m grid; x east, y north, from the town centre)

The district runs about 2.8 × 2.4 km, from the Vale in the west to the river's far bank.

| Zone | Where | What it is | Real thing behind it |
|---|---|---|---|
| The Cais (river quay) | east, (+1.0, 0) | The river bend: a stone quay with moored wine boats, long wine lodges with painted names, the iron bridge, and the **station** with its tile panels. The hill climb starts here. | Pinhão (quay, lodges, the 1937 azulejo station) |
| The Terraces (hero landmark) | the slope between quay and town, (+0.5, ±0.7) | An amphitheatre of about 50 curving terraces. Walls are 2–5 m high and benches 6–12 m deep, with vine rows along each bench. Three white quintas stand on the slope with their names in giant letters. There are monorail tracks up the steepest walls, and the hill-climb road switches back through it. | Douro terraces; Lavaux (10,000 terraces, walls up to 5–6 m) |
| The vila (town) | the ridge, (0, 0) | Whitewashed and ochre houses with terracotta roofs, the church with its tiled front, the Largo (the square) with a fountain and plane trees, the cellar café, and the **Adega** (the wine co-op), where the Vineyard Loop's paddock is. The Pit Stop is on Valley Road where you arrive. | La Morra; Douro hill villages |
| The Belvedere | top of the ridge, (−0.3, −0.6) | The chapel and bell tower with a viewing balcony over the whole valley. The hill climb finishes here, by the timing tower (which arrives at the frenzy). | La Morra's belvedere and 31 m bell tower |
| The Vale | west, (−1.0, 0) | The gentler back slope: lower vines, a cork-oak grove, olive and almond trees, farm lanes, and the **Vineyard Loop**. | Alentejo cork country; Langhe lanes |
| River Road | along the west bank, (+1.0, −1.2 → +1.2) | Long sweepers between the river and the lowest terraces, south towards Millbrook. It's the lower half of the hill climb, and the home of the speed trap. | N222: 27 km, 93 bends |
| The far bank | east of the river, (+1.4, 0) | Wilder terraces and an abandoned quinta. You reach it over the iron bridge. | Douro's abandoned terraces (*mortórios*) |

**Arrival:** Valley Road South comes over the Bramblewick ridge at (−2.0, +1.2). The whole amphitheatre opens at once: gold-green stripes above a blue river bend, a train crossing the iron bridge, and the bell tower on the skyline (principle 2, the triangle rule).

**The pull** (principle 1): from Hillcrest's eastern hills you can see the bell tower and the white painted letters of the biggest quinta. Ideally they're lit at night too.

**Roads:**
- Valley Road South from Hillcrest into the Largo;
- River Road along the river (the main through road, south to Millbrook and My City);
- the hill-climb road from the Cais up the terraces to the Belvedere, a public road until the frenzy;
- farm lanes in the Vale;
- country roads to Bramblewick, Orchard End and Highmoor;
- the iron bridge to the far bank;
- a **gravel shortcut** through the cork grove to Orchard End (doc 58: gravel shortcuts give rally parts a use).

## Races (doc 58: club layout C/B, pro layout A/S, E post-game)

The blueprint gives Vinelands C:2 and B:1 on its club layout and A/S on the pro layout, with E after the credits.

| Race | Class | Layout | Length | What it is |
|---|---|---|---|---|
| Vineyard Loop | C | club | 1.6 km, 3 laps | Lanes between the Vale's terraces. There's a dip through the co-op yard, a wall-lined esses section, and a long drag past the cork grove. 12 m wide, with straw bales at the walls. |
| River Road Sprint | C | club | 2.8 km point to point | From the Cais south along River Road's sweepers to the Millbrook turn. It's the hill climb's lower half, so the pro event grows out of a road you already know. |
| Vineyard Tuners | B (2★) | club | 1.6 km, 4 laps | The Vineyard Loop for Tuners (docs/data/race-stars.json). |
| Vinelands Hill Climb | A | pro | 3.8 km | The whole road from the Cais to the Belvedere: 3.8 km, about 170 m of climb, sweepers then six hairpins. The roads are closed, with a timing tower at the finish. |
| Vinelands Hill Climb, evening | S | pro | 3.8 km | The same road at dusk, with lanterns on the terrace walls and crowds at every hairpin |
| Vinelands Hill Climb, post-game | E | pro | 3.8 km | After Duke Rolo |

**The hill climb's format** (*question 2*). Real hill climbs run one car at a time against the clock, like Ollon–Villars (8 km through vineyards) and Shelsley Walsh. I'd recommend that format here:
- staggered starts 10 s apart, like a rally stage;
- AI cars visible ahead and behind;
- one results table.

It also makes the narrow terrace road safe for a 24-car field. The C/B era gets the road as a time trial (below), so the pro event is a promotion of something you already know.

## Things to do (13)

| Activity | Type | How it plays | Inspired by |
|---|---|---|---|
| The Climb | T | The hill-climb road as a public time trial from day one: Cais to Belvedere, with records for every class. Inês holds the C record. | Ollon–Villars; Shelsley Walsh; Gallery Climb |
| Grape Run | job | Collect grape crates from three quintas down the terrace lanes and get them to the Adega before the press closes. Bumps spill grapes; a slosh meter shows how full the crates are. | The harvest (*vindima*). Unlike Giles's Harvest (doc 64), it's about carrying a fragile load, not cutting. |
| Barrel Race | T | Push a giant barrel with your car up the town's steepest street to the Largo, against the clock. The barrel's real physics make it rolling chaos. | Bravio delle botti (Montepulciano: 80 kg barrels, 1.8 km uphill) |
| Monorail Dash | job | Drive the vineyard monorail tractor up the steepest terrace wall with a load of harvest. It's a one-off machine, like Dai's dumper. | Mosel monorack (40–60° slopes) |
| Beat the Harvest Train | rail | Race the harvest special from Vinelands to Hillcrest. It's Beat the Train on the main line. | Linha do Douro |
| River Road speed trap | trap | On River Road's longest sweeper | N222 |
| Switchback Leap | ★ | A danger-sign jump off the top hairpin onto the road below: the hill climb's forbidden shortcut | Forza Horizon danger signs |
| Cork Grove Trail | T | A gravel time trial through the cork oaks to Orchard End | Alentejo cork forests |
| Wine-boat ride | ride | A wine boat carries your car across the river to the far bank and back. It's a slow, quiet view of the terraces. | Douro rabelo boats |
| The far-bank quinta | secret | An abandoned estate on the wild bank. There's an old car under a sheet in the cellar (a body, or a paint, *question 4*), plus the 12th tile. | Douro *mortórios* |
| Azulejo tiles | ★ | 12 hand-painted tiles hidden around Vinelands form the keepsake set. Each shows one stage of the wine year: pruning, flowering, harvest, treading, barrels, boats. Collect them all and the station's missing panel is restored. | Pinhão station's 24 panels |
| Photo sights | ★ | Pip's sights: the amphitheatre from the Belvedere, the iron bridge with a train on it, and the painted letters | — |
| Harvest Festival | event | The *Festa da Vindima*: lanterns over the Largo, grape treading in the Adega yard, a parade of decorated tractors. In the frenzy it becomes the **Festa Grande**: an arena bigger than the town is built in the square for the Endurance announcement. | Fête des Vignerons (Vevey: 20,000 seats, held once a generation) |

**Recruits** (doc 45 asks for 2–3 per town). Each joins your team once teams open, if you meet their condition:

| Recruit | Who they are | Condition |
|---|---|---|
| Inês Calado | The hill-climb record holder for 30 years, in a tiny old roadster | Beat her C record on The Climb, then win the Hill Climb in class A |
| Zé of the Adega | The co-op's tractor driver, fastest down the terrace lanes with a full trailer | Grape Run at gold |
| Teresa | The station master, who keeps the harvest train to time | Beat the Harvest Train, and restore the station panel (all 12 tiles) |

**Rewards** (cosmetic only, since there are no local parts here):
- ruby-port red paint;
- the azulejo livery (blue-and-white tile pattern);
- the co-op's harvest-bell horn;
- a barrel-end speedometer;
- a wine label with your car on it in the post-game.

**Music:** Portuguese guitar and accordion for exploring; a quicker version with a snare drum for racing; harvest songs on the results screen.

**Ambience:**
- cicadas;
- the river;
- tractors;
- church bells on the hour;
- the train's horn along the valley.

## Vinelands by era

| Era | Classes | What you see |
|---|---|---|
| Local racing | C–B | Harvest season. Tractors with grape trailers are slow traffic on the lanes, the Adega steams, pickers fill the terraces, and the Vineyard Loop has straw bales and hand-painted signs. |
| The frenzy | A | The hill climb becomes a pro event: closed roads, the timing tower at the Belvedere, team hospitality in the quintas, a TV helicopter over the terraces, banners hung down the terrace walls, and the Festa Grande arena in the Largo. |
| The build-up | S | The evening climb: lanterns on the walls and crowds on every hairpin. Rolo's car is seen in a quinta's courtyard. |
| Post-game | E | Your record on the timing tower, a vintage named after you, and Inês's roadster in the Adega as a museum piece |

## How we build it (doc 57 process)

**Before step 1** (things Vinelands needs that the game doesn't have yet):
- **A flag:** `--vinelands`, off by default, with `VinelandsChecks` registered in `MigrationBuild.cs`.
- **The road there:** Valley Road South from Hillcrest, about 2.4 km, with a closed gate at the Bramblewick turn (like the Kettle Falls gate).
- **The map:** today's map is 5.9 km square; it must grow to about 8 km to reach Vinelands. Doc 57 §2.8 says `MapRegions`' hard-coded z thresholds must move into the region definitions "before region 3". Vinelands is region 4, so this comes first.
- **Streaming:** the district is 4.6 km out. It uses world tiles (doc 61), the way Quarry Hills does.
- **The railway:** the Main Line extended from Hillcrest's station to Vinelands station. Like every station, it opens once you've driven there.

**Then doc 57's steps:**

1. **Graybox roads and tracks:**
   - River Road, the hill-climb road, Valley Road and the Vale lanes, all through `RoadNetwork`;
   - the Vineyard Loop (12 m, ≥ 95 m behind the line);
   - the River Road Sprint grid;
   - the hill climb with a reserved paddock at the Cais.
2. **Event and AI validation:**
   - 24 cars on seeds 1709/1710/1711 for the Vineyard Loop (C and B) and the River Road Sprint;
   - the six hairpins tested with the AI (doc 57 warns about 21 m hairpins).
3. **Landform, the big new piece:** a **terrace generator**. From the slope's height field, it cuts contour-following benches:
   - walls as instanced stone strips;
   - vines as row-strip meshes with LODs, never one object per plant;
   - the river bend, the quay and the far bank.

   The terraces need their own frame-cost study (§2.9 budgets), since there will be about 50 benches visible at once.
4. **Town:**
   - whitewash, ochre and tile houses from the kit with terracotta roofs;
   - the church, the Adega, the Largo, the Belvedere and its tower, and the quintas with their painted letters;
   - the station with its tile panels;
   - the wine lodges and the iron bridge;
   - the uniform Pit Stop on Valley Road.
5. **Art pass:**
   - the terrace stripes reading from far away;
   - quinta letters and the tower lit at night;
   - day and night captures of the arrival view.
6. **Life:**
   - named residents and Portuguese chatter;
   - tractors with grape trailers as traffic;
   - wine boats on the river;
   - monorail carts on the walls;
   - pickers on the terraces;
   - storks or swifts over the church;
   - the music theme.
7. **Discoveries, last:** tiles, photo sights, the speed trap, the Leap, recruits and the far-bank secret.
8. **Performance:** a benchmark drive from the arrival to the Belvedere, plus a 24-car race with the frame probe.
9. **Release:** the full regression (it's a landmark), an area audit **PASS**, then publish.

## Richard's answers (7 October)

| Question | Answer | What it means for the plan |
|---|---|---|
| 1. Flavour | "Portuguese is good!" | Douro Portuguese: names, signs, the tiles, Portuguese guitar. |
| 2. Hill Climb format | "Real style is fun so lets do that" | Staggered solo runs against the clock, 10 s apart, one results table, for A, S and E. |
| 3. The Barrel Race | "Fun physics toy!" | It stays: the barrel up the steepest street to the Largo, against the clock. |
| 4. The far-bank secret | "Old roadster sounds cool! A good opportunity to get a cool model after performing the task required" | The far-bank quinta's cellar holds an **old roadster**, a new body made in the car builder, which is yours once you've done what the secret asks. Its condition is to be designed with the secret. |
| 5. Order | "Lets note these as a plan and continue with class B balance" | Vinelands waits; class B balance comes first. |

Also from the same message, for Quarry Hills: a **rugby conversion kick** at the rugby ground (doc 59, "Next for Quarry Hills").

## Questions for Richard (answered above)

1. **Flavour:** I've suggested Portuguese (Douro), because Hillcrest already reads Italian and Quarry Hills is Welsh. The alternatives are French (Alsace's half-timbered villages and storks) or Swiss (Lavaux's lakeside terraces). Which do you want?
2. **The Hill Climb's format:** real-style staggered solo runs against the clock (my suggestion), or a 24-car mass race up the hill?
3. **The Barrel Race:** pushing a barrel up a street is a physics toy rather than a race. Is it in the game's spirit, like the Blast Run, or too silly?
4. **The far-bank secret:** a body (an old roadster to restore), or just a paint and a tile?
5. **The order:** build Vinelands next (W2), or finish the class B work first (the championship plan, balance per star, the C kit conversions)?

## Sources (research, 7 October)

| Topic | Sources |
|---|---|
| Ollon–Villars hill climb | https://www.zwischengas.com/en/articles/events/Bergrennen-Ollon-Villars-2017.html ; https://a-e.li/reports/ollon2017/ |
| Mosel monorack | https://glossary.wein.plus/monorack-track ; https://www.wineandspiritsmagazine.com/regional-tasting-report/mosel-tasting-report/headed-straight-upweingut-franzen-in-bremmer-calmont |
| Douro terraces, rabelos, railway | https://luxeadventuretraveler.com/pinhao-heart-of-douro-valley/ ; https://loudavymkrokem.cz/en/douro-valley-portugal-things-to-do/ |
| N222 | https://theportugalnews.com/news/portugal-home-to-worlds-best-road/34635 ; https://detour-roadtrips.com/home/detour-173-is-the-n22-in-portugal-really-the-worlds-best-road |
| Pinhão station tiles | https://en.wikipedia.org/wiki/Pinh%C3%A3o_railway_station ; https://aleluia.pt/en/project/pinhao-railway-station |
| Lavaux | https://everything-everywhere.com/lavaux-vineyard-terraces-switzerland/ ; https://whc.unesco.org/document/152145 |
| La Morra belvedere | https://stradadelbarolo.it/en/territorio/itinerari/belvedere-langa-barolo/ ; https://italyreview.com/la-morra.html |
| Bravio delle botti | https://en.wikipedia.org/wiki/Bravio_delle_botti |
| Fête des Vignerons | https://en.wikipedia.org/wiki/F%C3%AAte_des_Vignerons ; https://www.swissinfo.ch/eng/culture/f%C3%AAte-des-vignerons_how-vevey-organises-its-once-in-a-lifetime-winegrowers-festival/45035540 |
| GT7 Alsace | https://simracingsetup.com/gran-turismo/gran-turismo-7-complete-track-list/ |
