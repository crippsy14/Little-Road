# 58 · The world and the campaign: Glasswater

Richard, 3 October: "This is the big one Claude! … Generating the map and then going city by city and town by town, and track by track makes more sense so we don't get overwhelmed, but also so that if we change how we want to do things early, we don't have to redo a whole map."

This doc is the design for the full world and the campaign that runs across it. It covers:
- the continent;
- the places on it and how they link;
- which class races where, and how tracks change as you rise;
- the story (prove yourself, get entered in the Aurora Endurance, beat Duke Rolo);
- the post-game;
- the railway wars;
- the design principles we hold ourselves to (from research, with sources);
- the order we build it in.

It builds on, and doesn't replace:
- [doc 45](45-roadmap-and-feature-spec.md) for phases and classes;
- [doc 44](44-parts-tiers-and-progression.md) for the parts ladder;
- [doc 57](57-new-area-standards.md) for area standards.

The interactive version (the generated map, a route planner with drive times per class, race heat maps, and a catalogue of every race) is the **world blueprint** artifact:
https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb (private to Richard; layout B).

---

## 1 · Decisions so far

Each decision was agreed with Richard between 2 and 3 October.

| # | Decision | Notes |
|---|---|---|
| 1 | **Navigation first, then build around it.** | Three systems: a ring motorway round the centre, with spokes out to far places; a railway that works as fast travel; and gravel and ice shortcuts between places. |
| 2 | **One continent, about 20 × 20 km, with its own geography.** | Not a grid and not a "biome per square". Climate places the land: westerly winds, a mountain spine with a rain shadow, colder towards the south. So forest, desert and ice blend over kilometres the way they do on a real drive. The model is New Zealand's South Island, because Bayview sits north of Hillcrest and the world spreads south. |
| 3 | **Layout B, "Glasswater", was chosen.** | A great inland lake fills the centre. My City stands on the dam at its northern end, and the ring follows the shore. |
| 4 | **Lowtown, a sunken town under the lake.** | The old valley town, drowned when the dam was built. It's explored with the Dive kit and holds secret parts, a ghost racer and a lost trophy. |
| 5 | **Coral Key, an island city off the north-west coast.** | Visible from Bayview from the start, and reached only along the 4 km Undersea Road (as in Road Trip Adventure). |
| 6 | **The Dive kit is a gadget, found at Harbour Point.** | A class B discovery, built on the gadget slot (doc 56 §20). |
| 7 | **The start area is full; the drives grow with the car.** | Class C has nine places within 7 km of Bayview. Class B mixes near and far. Classes A and S go all the way round the map. Long drives are fine because the car is faster by then, but it's on us to fill the space between towns. |
| 8 | **Every main location races every class.** | Club layout for C and B; a longer, professional layout for A, S and E. The town's own class still gets the most races, so each class's home towns still move outward. |
| 9 | **The world changes when class A unlocks** (the "frenzy"). | Teams form and the Endurance season is announced. Tracks are rebuilt in their pro layouts, and towns dress up. **C and B races can still be entered on the old tracks:** the event loads the pre-frenzy layout and dressing, and after the race the world returns to its post-frenzy state. |
| 10 | **Before the credits, class E is Aurora Raceway only.** | Ten build-up races, then the Endurance against Duke Rolo. Winning it plays the credits and a post-credits scene. |
| 11 | **Post-game: E races at every main track.** | Beating Rolo unlocks class E events on every pro layout, for post-game fun. It also unlocks **one set of secret parts** that makes your car faster than the field. There's only one set, so your two teammates race on normal parts. |
| 12 | **Villages and satellite towns are built from a recipe.** | Each mixes a railway station, a local track and challenges, a racer to recruit, and cosmetic rewards (horns, paints, body kits, speedometer faces). |
| 13 | **Parts don't spread further.** | Only the five places already marked sell local parts (Bramblewick C, Highmoor B, Copper Gulch S, Icefall Hut E-ice, Lowtown secret), so collecting never becomes a chore. Discovery rules as doc 56 §21. |
| 14 | **The railway wars** (sub-plot, still a sketch). | Four companies compete for the branch lines. Railway challenges shift who runs an area. Control changes how a place looks and the perks it gives, **never whether you can use it**. |
| 15 | **"Don't be afraid of loads of tracks and races."** | Every race is a data row (place, track, layout, class, type), so any subset can be selected later. |

---

## 2 · The continent

- **Size:** a 20 × 20 km sheet with about 300 km² of land. That's about 75 times the Hillcrest valley (3.9 km²). Forza Horizon 5's map is about 107 km².
- **Position:** Bayview and Hillcrest sit at their true size on the north coast.
- **Coordinates:** game coordinates carry straight over. Bayview island is at the origin, north is +z, and 1 blueprint km is 1000 game metres. Hillcrest's mainland stays where it is today (x −1100 to 1300, z −2000 to −380).

**Climate (how the land is generated):**
- **Wind:** westerlies bring rain off the western sea. The air drops it on windward slopes and loses it a little with every kilometre inland.
- **The Spine:** it runs north to south down the west. Its western side is rainforest and pine. East of it, the rain shadow makes dry grassland, then scrub. Behind the low eastern range the land turns to red desert and canyons.
- **Temperature:** it falls going south and with height. The southern range carries glaciers, and the far south is tundra with frozen lakes.
- **The lake and the river:** a river runs north from the lake to the coast. Along the river and the lake shore, a green farming belt runs through the dry middle.

**The generator:** the blueprint's generator (value-noise fBm, a moisture march across each row, a temperature model) is the source of truth. The game port is `ContinentGenerator` (§11), so the game's map and the blueprint match.

**Navigation:**
- **The ring motorway** round Glasswater: about 30 km long, with long causeways across the bays and a causeway straight across the lake (R1–R5).
- **Spokes** out to Harbour Point and Port City.
- **Country roads** to the towns.
- **Mountain roads** over the passes.
- **Gravel and ice shortcuts.** They're shorter, but only quick with rally parts or ice tyres. That gives those parts a use beyond their own events.
- **Undersea roads** to Coral Key and Lowtown (Dive kit only).

**Railway:** the Continental main loop plus branch lines from three rival companies (§8).
- **Stations open once you've driven to them yourself,** so the first journey to anywhere is by car.
- **Fast travel** takes about 20 seconds per hop. This is deliberate: the Wind Waker lesson (pitfall 4) says long travel through empty space needs a quick alternative.

---

## 3 · Places

Thirty-three places: 13 main towns, 18 villages and satellite towns, one secret and one island city.

The places in each class, read outward from Bayview:

| Class | Home places |
|---|---|
| **C** (0–7 km) | Bayview, Hillcrest, Quarry Hills, Vinelands; villages Saltmarsh, Bramblewick, Kettle Falls, Cobble Cove, Orchard End |
| **B** (4–12 km) | Harbour Point, Summit Pass, Coral Key; villages Highmoor, Millbrook, Dunmore Junction, Tollgate, Ridgeway Circuit; Lowtown (secret) |
| **A** (8–19 km, all round) | My City (hub), Port City, Pinewood Coast; villages Cinder Flats, Cape Lantern, Lowmere, Larchwood, Fernhollow |
| **S** (all round, the other way) | Redrock Mesa, Frostcrown; villages Icefall Hut, Mirage Wells, Copper Gulch |
| **E** | Aurora Raceway; village Polar Post |

The blueprint holds every place's description, position, roads and station.

**Building a village** (the recipe):
- **Size:** a handful of buildings, one landmark, and one activity done well.
- **Kit:** built from the existing kit and held to doc 57.
- **Tags:** recruit (R), parts (P), track & challenges (T), cosmetic rewards (★).
- **Uniqueness:** every activity carries a twist that belongs to that place (principle 7).

---

## 4 · Races and tracks

**Main towns:** each of the 13 has two layouts (Aurora has three):
- **A club layout** for C and B. Where a track already exists, it's today's track (Harbour Loop, Hillcrest Raceway).
- **A pro layout** for A, S and E.
  - It's longer, with pit lanes, grandstands, run-off and sponsor boards.
  - It's built on the club layout, so the pro version visibly grows out of the one you know. For example, the Hillcrest Raceway GP extends through the valley and past the castle; it's Duke Rolo's home track.
  - Pit lanes are reserved from class A, per doc 57 step 1.

**Race counts** at each main town:
- one race per class on each layout;
- plus one more in the town's own class;
- Aurora Endurance has 10 E races.

Villages, Lowtown and the two Grand Tours add their own races.

**Grand Tours:** stage races between towns, which make the drives themselves into races.
- **Grand Tour A:** 8 stages, clockwise from My City.
- **Grand Tour S:** 8 stages, anticlockwise.

**Planned totals before the credits** (the blueprint catalogue keeps the live count):

| | C | B | A | S | E | All |
|---|---|---|---|---|---|---|
| Races | 21 | 23 | 29 | 26 | 10 | 109 |
| Post-game E (one per main pro layout) | | | | | +12 | 121 |

Every class is at least double Richard's minimum of 10.

**Old tracks after the frenzy** (decision 9):
- **What it is:** a race entry carries a world state (`pre-frenzy` or `current`). Entering a C or B race after class A unlocks loads the club layout and the pre-frenzy dressing for that race, and the results screen returns you to the current world.
- **What it needs:** the frenzy dressing (banners, team trucks, grandstand extensions) built as a **switchable layer** per town, never baked into the town. That's also what makes each town's pro layout cheap to show and hide.

---

## 5 · The story

**The spine:** prove yourself, earn an entry to the yearly Aurora Endurance, and beat Duke Rolo in it.

| Stage | When | What changes |
|---|---|---|
| **Local racing** | C–B | Club layouts, village fêtes, small crowds, hand-painted signs. You're nobody yet. Duke Rolo is a rumour, and a car glimpsed on his drive (doc 51). |
| **The frenzy** | A unlocks | Teams form, so recruits can now join. The Endurance season is announced. Pro layouts open. Towns get banners and flags, team trucks crowd the motorway, fans camp by the roads, and the radio talks about nothing else. Every town gets at least two visible changes, seen within 60 s of the unlock (principle 8). |
| **The build-up** | S | Countdown posters. Rival teams with names and grudges. Rolo turns up at your races to watch. |
| **The entry** | S → E | Entry to the Endurance is earned in the A and S seasons, by results across the Grand Tours and the main towns' pro layouts. **E itself is Aurora only before the credits** (decision 10). |
| **The Endurance** | E | Ten build-up races at Aurora, then the race by day and by night against Duke Rolo and his team. Credits, then the post-credits scene. |
| **Post-game** | after Rolo | E races at every main pro layout. The one set of secret parts (decision 11). The railway wars and villages continue. Records and ghosts at every track. |

**Reasons to come back to a place**, so the world evolves with you:
- its layout changes with your class;
- its field changes (a C club grid becomes an A team grid);
- your own records and ghosts stay on the board, so you can race your past self;
- the town is redressed at the frenzy;
- a recruit is waiting there;
- its railway company may change.

---

## 6 · The Dive kit and the undersea places

- **Driving underwater is a mode of its own,** not just a road on the seabed. It needs:
  - its own handling: slower, floatier, more drag;
  - its own light: green murk, with caustics from the surface shimmering above;
  - its own sound and fog.
- It's built once and used in both places.
- **Lowtown** is the short first dive. It's reached down the ramp from My City's dam, and its spires can be seen from the Lake Causeway.
- **Coral Key** is the long one: about 4 km of sea floor from Bayview past reefs and wrecks, ending at a whole island city.
- **Pacing rule:** the undersea road has a landmark or event at least every 30 s at class B speed. Long travel through empty space is the Wind Waker pitfall.

---

## 7 · Post-game and the secret parts

- **After the credits:** E races open on all 12 main pro layouts, besides Aurora. Your team races them with you.
- **The secret parts** are a single set, earned by beating Rolo.
  - Fitted, they make your car faster than the field.
  - Your teammates race on normal parts.
  - They're a reward to show off with, never a requirement.
  - They stay subject to the AI physical-cap rule (principle 14 applies to the field, not to you).

---

## 8 · The railway wars (a sketch; to design properly later)

**The companies:**

| Company | Lines | Character |
|---|---|---|
| **Continental Railways** | The main loop and the Coast Branch | The old state line: punctual, proud, slow to change |
| **Glasswater & Southern** | From Dunmore Junction: the Moor & Mill Line, the South Shore Line and the lake steamer | Friendly, a little broke |
| **Redline Express** | From Port City: the East Express and the Cape Spur | Flashy and fast. It's buying up lines with promises it won't keep. |
| **Pine & Peak Light Railway** | A volunteer narrow gauge: the Forest Line and the Peak Branch | The underdog |

**How it works:**
- **Railway challenges:**
  - beat the express between two stations;
  - timetable runs;
  - parcel dashes;
  - get a stranded driver to a train before it leaves.
- **What winning does:** it gains ground in an area for one company. Control changes that area's livery and stations and gives a perk, such as faster or cheaper fast travel.
- **Fixed rule:** every station keeps working whoever runs it.
- **Rewards:** company liveries, a steam-whistle or air-horn car horn, ticket-stub speedometers.

**Harbour Point** is where three companies meet, so it's the first battleground.

**Open questions:**
- Can you back more than one company?
- Can areas change hands again?
- Is there an ending where one company wins, or one where they share?

---

## 9 · Design principles we hold ourselves to

Research on 3 October, sources at the end. Each principle comes with a **rule we can check**.

- **Where the numbers come from:** the numbers are ours, proposed for testing. They are not figures from the sources.
- **How to use the rules:** they become part of doc 57's audits as each system arrives.

### World and navigation
1. **Landmarks pull players along.** BotW's "gravity", tuned with playtest heatmaps.
   - *Rule:* from any point on a main road, at least one landmark you can name is visible within about 1.5 km.
2. **The triangle rule: terrain hides and reveals.**
   - *Rule:* no long straight shows more than one point of interest at once. Every region has at least one crest that opens a vista.
3. **The roads are the level.** Playground designs flow into every road like a circuit.
   - *Rule:* every road passes a drivability audit (grade, camber, sight lines, corner rhythm) as part of `--area-audit`. The scale test (§11) reports grades and drive times for every road today.
4. **Discover rather than reveal.** Avoid a checklist of icons (FH6 fog of war; criticism of Ubisoft towers).
   - *Rule:* towns and stations are pre-marked; everything else appears on the map once seen or visited.

### Pacing and density
5. **Nested loops: about 3 s, 30 s and 3 minutes** (Griesemer's "30 seconds of fun").
   - *Rule:* in free roam at class C cruising speed, an interesting moment comes at least every 30 s on main roads and every 60 s on remote ones. It's measured by sampling routes; the scale test lays the groundwork.
6. **Density matters more than size** (Odyssey's miniature gardens; The Crew Motorfest).
   - *Rule:* a content budget per km². A region that can't meet it shrinks, or its roads loop back on themselves instead of padding.
7. **Vary the context, not the volume** (Mario Kart World's repeated P-Switch templates).
   - *Rule:* the same challenge template is used no more than 3 times per region, and each village's activity has a twist that belongs to that place.
8. **Change the world so old places feel new** (FH4's seasons).
   - *Rule:* the frenzy is visible within 60 s, with at least 2 changes per town.

### Progression and rewards
9. **Show the lock before the key, and always name the next goal** (Hodent; Hopson's "post-reward pause").
   - *Rule:* every results or unlock screen names the next goal and where it is.
10. **Mix reward schedules and avoid cliffs** (Hopson).
    - *Rule:* no drop in prize money between classes, and at least 3 parallel goals always open. Parts discoveries and village rewards are the variable layer.
11. **Rewards must be meaningful and visible** (FH5 wheelspin filler).
    - *Rule:* every cosmetic can be seen or heard in normal driving, with no duplicates or filler.
12. **Fun is learning: each tier teaches something new** (Koster).
    - *Rule:* each class adds a mechanic or track feature:
      - B: gravel, jumps and the Dive kit;
      - A: teams, fuel, tyres and pits;
      - S: aero and night;
      - E: endurance strategy and remixed layouts.

### Racing feel and difficulty
13. **Keep players in the flow channel** (Csikszentmihalyi, Chen).
    - *Rule:* difficulty comes through choices in the world (easier local races, parts, rivals). Aim for a 40–70% first-attempt win rate on the critical path.
14. **Catch-up must be bounded, invisible, and fade near the finish** (Pure's balancing).
    - *Rule:* no AI exceeds its car's physical cap, and there's no catch-up help in the last 20% of a race. Tested with the keep/reject harness.
15. **Opt-in assists with a trade-off** (Mario Kart 8 Deluxe Smart Steering).
    - *Rule:* assists in Options cost a little (no shortcuts) and never block progress.

### Story and motivation
16. **Autonomy, competence, relatedness** (self-determination theory).
    - *Rule:* at least 2 valid next goals at every point, and recruits react to your progress.
17. **Small side activities, rooted in their place** (The Witcher 3).
    - *Rule:* every village activity says something about its village or the world. Never a bare fetch.
18. **Design for several motivations** (Quantic Foundry).
    - *Rule:* every region has a hook for competition, discovery, completion and design.
19. **The toy test** (Schell).
    - *Rule:* given 5 minutes of free driving with no goal, a playtester chooses to keep going.

### Onboarding and UX
20. **Teach by doing, with a light load** (Hodent; Miyamoto's World 1-1).
    - *Rule:* driving within 30 s of a new game, first race within about 5 minutes, at most 3 new concepts per step, and nothing that can fail in the first race.
21. **Guide early, then open up** (GT7 menu books; Choro Q's licences).
    - *Rule:* the first hour has a visible list of 3–5 next goals.

### Scope and production
22. **Vertical slice before scale.**
    - *Rule:* one region at ship quality before a second climate zone is built out, measured in time per km² of content.
23. **Playable end to end before polish** (Derek Yu).
    - *Rule:* a graybox path from C to E through Rolo and the credits is playable before any region is polished.
24. **Most players never finish** (Steam achievement studies: a median of about 10% complete).
    - *Rule:* keep the critical path to the credits at about 12 hours, with everything else optional.

### Known pitfalls of open-world racers, and our answers

| Pitfall | Our answer |
|---|---|
| An empty world with no reason to drive (The Crew 2; Mario Kart World's free roam) | Principles 5 and 6. Villages, Grand Tours and railway challenges sit on the roads between towns. |
| No quick restart (Burnout Paradise had to patch one in) | Restarts take 3 s or less with no drive back. The race pause already has Restart (doc 56). |
| Getting lost in open-route races | Every race has a route line and checkpoints. The waypoint and route line is already on doc 57's genre list. |
| Slow travel through empty space (Wind Waker) | Fast-travel train, a landmark every 30 s on the undersea road, and Grand Tours that turn the drives into races. |
| Icon clutter | Principle 4, plus map filters. |
| Visible rubber-banding | Principle 14. |
| Reward inflation and repeated micro-challenges | Principles 7 and 11. |
| Excessive length | Principle 24. |

---

## 10 · The build order: one piece at a time

Richard's instruction: generate the map, then go **city by city, town by town, track by track**, so we're never overwhelmed and early changes don't mean redoing everything. The world is generated from data (the blueprint's places, roads and climate), so a change to a place moves its roads, terrain and race rows with it.

| Step | What | Exit criterion |
|---|---|---|
| **W0** | **Scale test** (§11, built now). The generated continent as rough terrain, graybox roads and blocks for towns. Drive it in any class's car. Measure drive times and grades. | Richard has driven it and judged the scale and pacing. Distances are adjusted in the blueprint if needed. |
| **W1** | **Foundations:** world grid and tiles, terrain streaming and LOD, floating origin, data-driven regions (replacing `MapRegions`' hard-coded z thresholds), race rows as data, world-state layers (pre-frenzy / frenzy / post-game). | Bayview and Hillcrest load inside the continent with no visible seam, and the frame cost stays inside the doc 57 budget. |
|  | *W1 first pass (3 Oct): the world grid with streamed woodland and a cached map ([doc 61](61-world-tiles.md)).* | |
| **W2** | **The C ring:** Vinelands, Quarry Hills and the five C villages, each through doc 57's process. The first railway branch. | Each passes `--area-audit`. Every C race has 24 finishes on 3 seeds. |
| **W3** | **A vertical slice of B:** Harbour Point plus the Dive kit and Coral Key. The undersea mode is proven here. | The undersea pacing rule holds (§6). |
| **W4+** | The rest, one town at a time, in class order. Then the frenzy layer, Aurora, the Endurance, the credits, and the post-game. | Principle 23: the graybox path to the credits exists before polish. |

---

## 11 · The scale test (W0)

`--scale-test` builds the whole continent from the blueprint's data, in the same coordinates the game uses.

- **Terrain:** the climate generator ported from the blueprint, at 10 m resolution, coloured by biome and streamed in 1 km chunks with two levels of detail.
- **Roads:** graybox roads on every blueprint road. Grade limits are enforced per road type, and roads form embankments over water.
- **Places:** class-coloured beacons with names, blocks for buildings, and the sea and the lakes.

**What you can do in it:**
- **Pick a car:** any class's car. `--scale-class C|B|A|S|E` gives the strongest road build in that class. You can switch in game: 1–5 picks C–E, 0 the stock car, M the maximum build; on a pad, D-pad left/right.
- **Move around:** jump between places (Tab / D-pad up and down, like the train). Swap to an aerial camera (C). Recover to the nearest road (R).
- **Read the HUD:**
  - your speed, the biome, your height and the road grade;
  - the nearest place;
  - a trip timer between places, which writes a log line on every arrival.

**The pacing report:** `--scale-pacing <file>` drives no car. It measures every road with `SpeedProfile` (the ideal-lap model used for builds) for every class build, and writes:
- road lengths, grades and times;
- the drive time from Bayview to every place for every class.

The results of the first build go in §12.

---

## 12 · Scale-test results

Studies are in `review/scale-test-1` to `scale-test-8`. Each run's photos, log and pacing report stay in its own folder.

**The world as generated** (scale tests 1–3):
- **Land:** 301 km², the same as the blueprint.
- **Lakes:** Glasswater sits 28 m above the sea. The frozen lake is 6 m above it.
- **Timings:**
  - generation takes 1.8 s on a background thread;
  - the scene is ready in about 6 s;
  - about 7 ms a frame at 1280×720 while driving.
- **Climate:** biomes fall where the design says. Redrock is sand desert, Pinewood rainforest, Frostcrown snow and glacier, Hillcrest meadow.
- **Roads:** 57 roads, 212 km in all. The ring motorway is 26 km.

**What the first runs taught the generator.** Each fix stayed in.

1. **Town pads raised the sea floor around Bayview into discs.** Pads now only flatten land.
2. **Today's valley read as a slab.** The blueprint's "today's Hillcrest is land" rule is a rectangle, and in 3D it showed as a raised rectangle with a straight coast. It's now a rounded, ragged outline, and the meadow colour blends out instead of stopping at a line.
3. **Mountain towns had to be cut down to keep the roads legal.**
   - With straight roads at legal grades, Summit Pass had to drop 181 m and Frostcrown 168 m. The R5 ring junction had to rise 155 m.
   - Roads now get **switchbacks where the ground is steep.** The legs cross the slope, the hairpins are rounded, and each leg swings out just enough for its own stretch of hill.
   - Since then, no town or junction has moved more than 5 m. Twelve roads have hairpins, including Summit Pass, Pass West, Glacier Climb, the Old Quarry Road and Tundra Road.
4. **The eastern range sat on the R3 ring junction**, which made a 125 m motorway cutting. The range now ends just short of the ring, and the cutting has gone. The blueprint has been updated to match.

**Pacing (ideal lines, `SpeedProfile`, from a standstill at each junction).**

Top speeds of each class's strongest road build: stock 89, C 130, B 171, A 213, S 308, E 317 km/h.

| From Bayview to | km | stock | C | B | A | S | E |
|---|---|---|---|---|---|---|---|
| Hillcrest | 1.0 | 0:46 | 0:31 | 0:24 | 0:20 | 0:16 | 0:15 |
| Vinelands | 5.2 | 4:00 | 2:43 | 2:05 | 1:43 | 1:20 | 1:17 |
| Harbour Point | 9.9 | 7:03 | 4:50 | 3:42 | 3:00 | 2:12 | 2:07 |
| My City | 8.8 | 6:31 | 4:27 | 3:25 | 2:49 | 2:09 | 2:05 |
| Summit Pass (over the hairpins) | 12.9 | 13:02 | 9:13 | 7:11 | 6:06 | 4:56 | 4:47 |
| Coral Key (sea floor) | 4.4 | 3:04 | 2:05 | 2:14 | 2:12 | 2:05 | 1:58 |
| Redrock Mesa | 17.9 | 12:44 | 8:40 | 7:47 | 6:55 | 5:38 | 5:28 |
| Port City | 19.5 | 14:22 | 9:41 | 7:24 | 6:02 | 4:31 | 4:21 |
| Frostcrown | 23.2 | 18:36 | 12:33 | 9:37 | 8:03 | 6:24 | 6:11 |
| Aurora Raceway | 28.0 | 22:40 | 15:30 | 11:57 | 10:05 | 8:06 | 7:51 |

What the table says:
- **The class ladder shrinks the world as intended.** At class C, Aurora is a 15-minute expedition. At class S it's an 8-minute run.
- **Class C's home towns** (Hillcrest, Vinelands and the villages) are 0.5 to 3 minutes away. That matches principle 5's loops, provided each stretch has something on it.
- **Hairpin and sea-floor roads stay slow in every class.** Above class B, the Summit Pass and Coral Key times barely improve. That's good for variety, but those roads must be worth driving for their own sake (principle 3).
- **S to E is a small step.** S builds already reach 308 km/h, so E's difference has to come from grip, aero and endurance rather than top speed (principle 12). This is a parts-ladder question for doc 44.
- **Real drivers will be slower.** These are ideal lines. The autopilot drives (below) give real-physics times.

**Where the plan fights the terrain** (from each road's deepest cut and highest fill):
- **North Shore motorway (R1–R2):** 113 m of fill where it crosses the river valley. In the real world this is a viaduct.
- **Glacier Climb:** 148 m of fill. **Frozen Lake Road:** 97 m of cut and 117 m of fill. Both come from Frostcrown sitting 650 m up on the glacier, directly above a lake near sea level. The region pass for Frostcrown should either move the town to the glacier's foot, or make the lake a glacial lake high on the range.
- **Tundra Road:** 102 m of cut. **Glacier Road West:** 129 m of fill.

All of these are listed for the W1 region passes, where tunnels and viaducts get designed properly.

**Driving it for real (scale test 8, the autopilot on real physics):**

- **Class E, Bayview to My City:** 8.8 km in 2:24 against an ideal 2:02. No falls, no rescues.
- **The autopilot found two generator problems, both fixed:**
  - **Lips where roads overlap or meet.** At the R1, R7 and Summit Pass junctions, and where the Lake Causeway shares a corridor with the My City road, the lesser road now takes the greater road's height.
  - **Pinched hairpins.** Splined switchbacks were squeezed to about 2 m at the tip, and a car fell off one on Pass West. Hairpins are now 20 m arcs, checked at 15 m or more.
- **Tour drives are set aside** until towns are populated (Richard, 3 October). We'll judge the drives between towns once there's something to see on them. The autopilot (`--scale-autodrive`) stays for that.

---

## Sources (research, 3 October)

- Road Trip Adventure: https://en.wikipedia.org/wiki/Road_Trip_Adventure ; https://tvtropes.org/pmwiki/pmwiki.php/YMMV/ChoroQ
- BotW gravity and triangles: https://gmtk.substack.com/p/how-nintendo-solved-zeldas-open-world ; https://www.nintendolife.com/news/2017/10/zelda_breath_of_the_wilds_ingenious_design_is_all_about_triangles_apparently
- Forza Horizon world design: https://www.gamesradar.com/games/forza-horizon/forza-horizon-6-looks-like-the-pinnacle-of-open-world-racing-games-and-a-vibe-check-from-playground-sold-me-immediately-it-really-is-just-about-driving-wherever-you-want-and-exploring/ ; https://news.xbox.com/en-us/2026/04/08/forza-horizon-6-preview-japan-collectibles-seamless-races-open-world-design/ ; https://www.gtplanet.net/ralph-fulton-forza-horizon-4-is-a-homecoming-for-playground-games/
- 30 seconds of fun: https://www.engadget.com/2011-07-14-half-minute-halo-an-interview-with-jaime-griesemer.html
- Density: https://time.com/4816417/super-mario-odyssey-interview-e3/ ; https://gamingtrend.com/reviews/the-crew-motorfest-review-beautiful-but-empty/
- Repetition: https://www.videogameschronicle.com/review/mario-kart-world-review/ ; https://traxion.gg/opinion-mario-kart-worlds-free-roam-mode-feels-like-an-afterthought/
- Rewards and motivation: https://celiahodent.com/gamers-brain-ux-onboarding/ ; https://www.gamedeveloper.com/design/behavioral-game-design ; https://quanticfoundry.com/gamer-types/ ; https://selfdeterminationtheory.org/SDT/documents/2010_PrzybylskiRigbyRyan_ROGP.pdf
- Fun and flow: https://www.raphkoster.com/gaming/gdco12/Koster_Raph_Theory_Fun_10.pdf ; https://www.jenovachen.com/flowingames/Flow_in_games_final.pdf
- AI balancing: https://www.gamedeveloper.com/design/the-pure-advantage-advanced-racing-game-ai
- Assists: https://www.shacknews.com/article/99831/mario-kart-8-deluxe-adds-accessibility-but-nintendo-has-room-to-grow
- Side quests: https://www.gamedeveloper.com/design/designing-side-quests-study-these-7-games-and-some-chris-avellone-pointers-
- Onboarding: https://www.gamedeveloper.com/design/how-miyamoto-built-i-super-mario-bros-i-legendary-world-1-1 ; https://www.gamesradar.com/gran-turismo-7-menu-books-how-many/
- Scope: https://game-developers.org/game-development-vertical-slice ; https://www.derekyu.com/makegames/deathloops.html ; https://www.sciencedirect.com/science/article/abs/pii/S1875952118300181
- Pitfalls: https://www.quartertothree.com/fp/2018/07/06/the-astonishing-cluelessness-of-the-crew-2/ ; https://www.shacknews.com/article/56907/februarys-burnout-paradise-patch-adds ; https://zeldauniverse.net/2013/08/13/aonuma-explains-the-wind-waker-hds-swift-sail/ ; https://screenrant.com/forza-horizon-5-filter-map-icons-reasons-guide/
