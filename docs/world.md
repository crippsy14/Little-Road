# The world and its places

This record holds the Glasswater continent, the campaign's story, the standards every new place is built to, and each
built place. The game's systems (classes, parts, AI, look) are in [design.md](design.md). Process and tools are in
[how-we-work.md](how-we-work.md).

**Data that tools and checks read** (keep in step with this doc):
- `docs/data/world-plan.json`: the blueprint's 33 places, 27 tracks and 2 Grand Tours (`tools/world-plan-extract.py`).
- `docs/data/race-stars.json`: planned stars and surface per C and B event (read by `FieldRecipeChecks` and `tools/field-table.py`).
- `docs/data/field-recipes.json`: the PP and parts of every field, written by the build.
- The world blueprint artifact: the interactive map, route planner and race catalogue, pinned in Richard's sidebar
  (https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb). The `world-plan` skill and `tools/world-plan.py` give breakdowns.

## 1 · Glasswater and the campaign

### How we build the world
- Richard, 3 Oct: "Generating the map and then going city by city and town by town, and track by track makes more sense
  so we don't get overwhelmed, but also so that if we change how we want to do things early, we don't have to redo a whole map."
- The world is generated from data (places, roads, climate), so moving a place moves its roads, terrain and race rows.
  Every race is a data row (place, track, layout, class, type). "Don't be afraid of loads of tracks and races."
- `--scale-test` (W0) builds the whole continent as graybox from the blueprint (`ContinentPlan`, `ContinentGenerator`,
  `ScaleTest`): drive it in any class's strongest build, `--scale-pacing` writes drive times, `--scale-autodrive a,b,c`
  drives real physics. Drive tests between towns wait until the towns are populated (Richard, 3 Oct).

### The continent (decisions agreed 2–3 Oct)
1. **Navigation first:** a ring motorway (~26–30 km) round the centre with spokes, causeways (one straight across the
   lake), country and mountain roads, and gravel and ice shortcuts that are only quick with rally parts or ice tyres.
   Grade limits per road type, switchbacks where steep, hairpins at 20 m (never under 15 m).
2. **One continent** about 20 × 20 km (~300 km² of land), with its own geography (model: New Zealand's South Island).
   Climate places the land: westerlies and a mountain Spine in the west (rainforest and pine west, dry grass east, red
   desert behind the eastern range), colder to the south (glaciers, tundra), a river running north from the lake to the
   coast, and a farming belt along it.
3. **Layout B, "Glasswater":** a great inland lake in the centre, My City on the dam at its north end, the ring along the shore.
4. **Lowtown:** a sunken town under the lake: secret parts, a ghost racer, a lost trophy. Reached with the Dive kit.
5. **Coral Key:** an island city to the north-west, visible from Bayview from the start, reached only by the 4 km Undersea Road.
6. **The Dive kit** is a gadget found at Harbour Point (a class B discovery). Undersea driving is its own mode: slower,
   floatier, green murk and caustics, its own sound. Rule: a landmark or event at least every 30 s at class B speed.
7. **Drives grow with the car.** Class C's nine places are within 7 km of Bayview; B mixes near and far; A and S go all
   round. It's on us to fill the space between towns.
8. **Every main town races every class:** a club layout for C and B, and a longer pro layout for A, S and E (pit lanes,
   grandstands, run-off) built on the club layout so it visibly grows from it. The town's own class gets the most races.
9. **The frenzy:** when class A unlocks, teams form, the Endurance season is announced, pro layouts open and towns dress
   up (at least two visible changes per town within 60 s). It's a switchable layer per town, never baked in: C and B
   races still load the pre-frenzy layout and dressing (race entry carries `pre-frenzy|current`).
10. **Before the credits, class E is Aurora Raceway only:** ten build-up races, then the Aurora Endurance against Duke
    Rolo. A win rolls the credits and a post-credits scene.
11. **Post-game:** E races at every main pro layout (12 besides Aurora). Beating Rolo unlocks one set of secret parts that
    makes the player's car faster than the field: a reward, never a requirement.
12. **Villages** come from a recipe: a handful of buildings, one landmark, one thing done well. Tags: recruit, parts,
    track and challenges, cosmetic rewards (horns, paints, kits, speedometer faces).
13. **Parts don't spread:** only five places sell local parts: Bramblewick (C), Highmoor (B), Copper Gulch (S),
    Icefall Hut (E, ice) and Lowtown (secret). Everything else comes from the Pit Stops' ladder.
14. **Railway wars** (sketch): four companies (Continental Railways' main loop and Coast Branch; Glasswater & Southern from
    Dunmore Junction; Redline Express from Port City; the Pine & Peak volunteer narrow gauge from Quarry Hills). Railway
    challenges change who runs an area (livery, stations, a fast-travel perk), never whether you can use a station.
    Harbour Point is the first battleground. Stations open once you've driven to them; a hop takes about 20 s.
    OPEN: backing more than one company, areas changing hands again, one winner or a shared ending.

Coordinates: Bayview island is the origin, north is +z, 1 blueprint km is 1,000 game metres.

**Open terrain questions (W1):** Frostcrown sits 650 m above a lake near sea level (move it to the glacier foot, or make
the lake a high glacial lake); North Shore motorway's 113 m fill becomes a viaduct; tunnels and viaducts need designing.

### The places
33 places: 13 main towns, 18 villages, Lowtown and Coral Key. By class, outward from Bayview:

| Class | Main towns | Villages |
|---|---|---|
| C (0–7 km) | **Bayview**, **Hillcrest**, **Quarry Hills**, **Vinelands** | Saltmarsh, **Bramblewick**, Kettle Falls, Cobble Cove, Orchard End |
| B (4–12 km) | Harbour Point, Summit Pass, Coral Key | Highmoor, Millbrook, Dunmore Junction, Tollgate, Ridgeway Circuit; Lowtown (secret) |
| A (8–19 km) | My City (hub), Port City, Pinewood Coast | Cinder Flats, Cape Lantern, Lowmere, Larchwood, Fernhollow |
| S | Redrock Mesa, Frostcrown | Icefall Hut, Mirage Wells, Copper Gulch |
| E | Aurora Raceway (club 2.4, GP 6.0, Endurance 9.0 km) | Polar Post |

Built places are in bold. Planned race totals before the credits: C 21, B 23 (26 with today's reversed layouts),
A 29, S 26, E 10, plus 12 post-game; Grand Tours for A (8 stages clockwise from My City) and S (anticlockwise).

Pacing from the scale test (ideal lines): top speeds stock 89, C 130, B 171, A 213, S 308, E 317 km/h. Bayview to
Hillcrest takes 0:31 in class C, Vinelands 2:43, Harbour Point 4:50, Aurora 15:30 (7:51 in E). S to E is a small speed
step, so E must differ in grip, aero and endurance.

### The story
Prove yourself, earn entry to the yearly Aurora Endurance, beat Duke Rolo.
- **Local racing (C, B):** club layouts, fetes, small crowds, hand-painted signs. Rolo is a rumour.
- **The frenzy (A):** teams form so recruits can join, the Endurance is announced, pro layouts open; banners, team trucks, radio.
- **Build-up (S):** countdown posters, rival teams with grudges, Rolo watches your races. Entry to E is earned by results
  across the Grand Tours and pro layouts.
- **Endurance (E):** ten Aurora races, then the day-and-night race against Rolo, the credits, a post-credits scene.
- **Post-game:** E at every pro layout, the secret parts, the railway wars and villages carry on, records and ghosts everywhere.
- Reasons to return: layouts change with the class, the field changes (club grid to team grid), your records stay, the
  frenzy redresses towns, recruits wait, railway companies change hands.

### Design principles (research, 3 Oct), each with a checkable rule
- **World:** a nameable landmark visible within ~1.5 km of any main road; no long straight shows more than one point of
  interest; every region has a crest that opens a vista; every road passes the drivability audit; discover rather than
  reveal (only towns and stations pre-marked, no icon checklist).
- **Pacing:** an interesting moment at least every 30 s on main roads (60 s remote) at class C cruising speed; density
  over size (a region that can't meet its content budget shrinks); the same challenge template at most 3 times per region.
- **Progression:** every results screen names the next goal and where; no prize-money drop between classes; at least
  three goals open at once; every cosmetic visible or audible in normal driving; each class adds a mechanic (B gravel,
  jumps, the Dive kit; A teams, fuel, tyres, pits; S aero and night; E endurance strategy and remixed layouts).
- **Racing:** 40–70% first-attempt win rate on the critical path; any catch-up bounded and invisible, none in the last 20%
  of a race, and no AI beyond its car's physical cap; assists never block progress; restart in 3 s or less.
- **Story:** at least two valid next goals; recruits react to progress; every village activity says something about its
  place, never a bare fetch; the toy test: five minutes of free driving with no goal, and the tester keeps going.
- **Onboarding:** driving within 30 s of a new game, the first race within about 5 minutes, at most three new concepts per
  step, nothing can fail in the first race, three to five next goals in the first hour.
- **Scope:** a vertical slice before scale; a graybox path C → E → Rolo → credits before any region is polished; the
  critical path to the credits about 12 hours, everything else optional (the campaign is at least 12 hours and open-ended).

### Build order (doc 81, 20 Oct; nearest first in each wave, each main town before its villages)
- **W0** scale test: built. **W1** foundations: world tiles, streaming, background districts and the persistent world
  are built ([design.md](design.md) §13); floating origin, data-driven regions (replacing `MapRegions`' z thresholds),
  race rows as data and world-state layers are still to do.
- **W2, the C ring:** Bramblewick (built), Vinelands (built), then Orchard End, Kettle Falls, Cobble Cove, Saltmarsh.
- **W3, the B slice:** Harbour Point (Quayside Sprint 1.4 km, Coast Road GP 5.2 km), then Coral Key with the undersea mode.
- **W4, by class:** B (Tollgate, Highmoor, Millbrook, Summit Pass, Dunmore Junction, Ridgeway Circuit, Lowtown with My
  City); A (My City, Fernhollow, Pinewood Coast, Cinder Flats, Larchwood, Lowmere, Port City, Cape Lantern); S (Mirage
  Wells, Redrock Mesa, Copper Gulch, Icefall Hut, Frostcrown); E (Polar Post, Aurora). Then the frenzy layer, the
  Endurance, the credits and the post-game.
- **Alongside, in the round of the first place that needs it:** the UI art pass (its own brief for Richard first, bug
  #18); roads beyond the three towns; railway branches (Coast Branch with Bramblewick, Glasswater & Southern with Orchard
  End); local parts (Bramblewick C, Highmoor B); recruits (Bramblewick's milk-float champion, Orchard End's apple-cart
  racer) who join teams at class A; the class B car roster redo.

## 2 · Standards for every new place

### Richard's bar (21–22 Oct)
- On the first Vinelands build: "about a 2/10... Bayview / Hillcrest are closer to 8/10... it should feel like a local city
  with its own life and vibrancy, not just assets placed near roads." He expects four or five more phases after a first pass.
- "Try zoning specific areas (not as a grid, but as a natural size and shape for each zone depending on what it is), have
  features, reasons for each zone, architect types, building types... We don't want this to just become a task of
  'Create generic point on map, move on to creating next generic point on map'." After Vinelands' first zones (22 Oct):
  "Love this approach. We should defo do it for all towns and cities from now on."
- Hillcrest's lesson (3 Oct): it "feel[s] quite small as it was all mostly based around the spiral up to the castle".
  New towns are layered, moving and distinct.
- Each place is plainly distinct in culture: Bayview Cornish and Scottish harbour, Hillcrest Italian, Quarry Hills Welsh,
  Bramblewick English West Country, Vinelands Portuguese.

### The zone method (the standard for all towns and cities)
1. **Plan a real town first** (a city-planner agent): its growth story, districts each with an era and a reason, a street
   hierarchy down to stepped lanes and courtyards, public spaces, daily rhythms. Not a game level.
2. **Split it into zones of natural shape** (a ridge wedge, a quay strip, a knot of lanes). Each zone gets a brief: what it
   is and why it's here, its shape, architecture and kit, streets, features ("the things you remember"), life, edges with
   its neighbours, and "done when".
3. **Make the zone's own kit first** (Car Forge or Prop Forge), checked on a prop sheet.
4. **Place by the brief's logic**, as if the place had grown and been used: detail at eye level, nothing repeated next
   door, houses that step with the slope, a refused house leaving a garden wall or yard (never open grass), walls that
   never close a way, no mechanical rows, circles or strips.
5. **Check in pictures** (about nine views) against the 8/10 references, plus `--area-audit` with no defects in the zone.
   One zone per round, done well. Density is scaled to what plays and performs: it should feel dense, not literal.

### The per-place loop (since 20 Oct)
Brief (Richard's questions asked once, each with a default) → kits → build behind the place's flag → checks
(`--area-audit` and the street audit at zero defects, the old world's fingerprint unchanged, races 24/24 on seeds
1709–1711, the AI at skill 3/7/10/13, photos of flagged spots) → one review agent on a ~12-view contact sheet (a launch
review until LAUNCH-READY) → default on, commit, push, merge to main, publish the play copy.

**Activities are on hold** (Richard, 21 Oct: "Don't worry about activities for now"). The activities in the first towns
were underbaked, and he will do a sweep later with a new approach. New places get the land, roads, town, landmarks, life
and races. Activity ideas are written into the place's section "for the activities sweep". The audit's things-to-do
count falling short in a new place is expected.

**A world of cars** (Richard, 20 Oct): no human figures anywhere. Crowds, marshals, spectators and shopkeepers are parked
or moving cars; residents are named cars where they live and work. Drivers are only implied.

### Area audit bars (`--area-audit`; the numbers live in `AreaAudit.Standards`; change both together)

| Measure | Town | Country |
|---|---|---|
| Buildings per road km | ≥ 15 | |
| Building designs / most common design's share / same design next door | ≥ 8 / ≤ 25% / ≤ 25% | |
| Wall colours / roof colours | ≥ 10 / ≥ 4 | |
| Lamps per road km / street furniture per road km | ≥ 15 / ≥ 50 | |
| Planting per road km | ≥ 500 | ≥ 1,500 |
| Named residents / life per road km (residents, parked and cruising cars) | ≥ 4 / ≥ 10 | ≥ 5 life |
| Pit Stop | 1 (main towns) | |
| Things to do | ≥ 3 | ≥ 3 per road km |
| Floating pieces, loose bunting ends, unguarded lips ≥ 12 cm, unguarded steep banks (> 0.55/m and 40 cm in 12 m), cars off the ground, road seam steps ≥ 6 cm, hidden shop signs, tipped furniture | 0 | 0 |

A shop sign is hidden when two of three sight lines (straight on and ±30° at eye height) are blocked (`StreetAudit`).

### Build rules
- **Placement:** every piece goes through `BayviewKit.Spawn` (the one registry for culling, audits and streaming). Repeated
  buildings and props come from Forge kits. Every prop sits at the angle of its land (`GroundFit`, [design.md](design.md) §11).
- **Variety:** mixed heights so eaves step; roofs tinted per house; no two same designs side by side; no blank side walls;
  gates face the road; hillside streets get a low stone parapet downhill with gate-wide gaps.
- **A hero landmark per area:** it reads from anywhere, has a stepped skyline with one tallest element, colour inside the
  grey, something to do, and is lit at night. Hillcrest Castle is the model.
- **Clearances:** houses ≥ 8 m from the road; trees 14 m from roads, 10 m from rail, 22 m from rivers.
- **Widths:** town streets 7–8.5 m, lanes 6.5–7 m (Bramblewick's lanes race at 9 m), circuits 16 m (C and B town events
  10–12 m). Each circuit can have its own width (Richard, 20 Oct: "for sure").
- **Junctions** go through `RoadNetwork`: 6 m fillet corners tangent to both kerbs, roads trimmed to where the corners
  begin, one level junction surface, kerb paint, a gap in the through road's edge line, give-way line and sign. Roads the
  network doesn't own are external arms (corner pieces added, side road tucked 0.4 m under). Y splits and skewed Ts work;
  crossroads are untested, and roundabouts don't exist yet.
- **Getting back on:** gravel shoulders, ramps round kerbs, end ramps. `--rejoin-test` must have no non-scenery failures.
- **Barriers** where the ground drops more than 1.1 m in 2 m within 8 m of the edge (or 1.8 m in 3 m): guardrail on valley
  roads and circuits, stone parapet in hill towns, railing on sea walls; bridges get collider parapets. `BankBarriers`
  walls any bank left unguarded. Barriers don't fix an AI that leaves the road (Richard, 7 Oct: "Barriers don't solve
  the problem, they hide it"); judge the AI with them off.
- **Streets** (from the reviews): a home car stands in the middle of its garden, at least 1 m clear of lamps, benches and
  fences, or is left out; no lamp within 6 m of another; no drive in front of a shop; pavements stop at the junction
  surface; hillside plinths from the footprint grid (more than 5 m against a house: don't build; 0.5–5 m: stepped
  footing); field walls only in runs of 8 m or more, with end piers; a sea wall needs sea along most of its length.
- **The Pit Stop** (Richard, 28 Sep: like a Pokémon Centre): one uniform services building in every main town, at the
  arrival point on the main road, never deep inside a landmark. Garage bay (teal: the local race, fitting, body swaps,
  tuning, the Tuner Garage), parts bay (yellow) and body shop (pink: whole new bodies and paint, looks only).
  `pit_stop_3`; Bayview, the tutorial town, has `pit_stop_2` (garage and parts only). It stands on a levelled, paved
  forecourt (no grass: Richard, 21 Oct), with the pylon and its giant spinning tyre at the kerb. Villages have no Pit
  Stop: a local tuner (Jed at Bramblewick) instead. Every town has a loop for the tuning test drive.
- **Region and map:** a `MapRegions` entry (id, name, blurb, outline, collection counts), fog reveals, map labels for
  places, people and Pit Stops, the DISCOVERED card. Text contrast ≥ 4.5:1, prompts ≥ 24 px.
- **Sound and night:** an explore and a race music theme per area, its own ambience, street lamps on every street, lit
  windows, the landmark lit.
- **Background building:** a new district builds after you're driving and must not move ready-to-drive by more than 0.1 s.

### Tracks
Every circuit, lane race, hill climb, street race or rallycross is designed with the `track-design` skill and checked
with `tools/track-eval.py`.
- **Examples are direction** (Richard, 20 Oct: "if I give examples it's more direction than a 1 for 1 reference
  point"). Never copy or name a real circuit's corners; name corners after the place they run through.
- Every lap has 2–4 overtaking zones (one downhill), a rhythm section, a commitment corner and one or two mistake
  generators. A loop round a field with light turns fails (Richard, 23 Oct, on the first Cumeada: "a very safe design
  with all corners being generic and light turns").
- **Short and long versions** (Richard, 21 Oct: "Certain tracks have a short and long version... It doesn't have to be
  every location... definitely not every time"): class C races a 3-lap short layout; later classes race a long layout that
  turns back on itself with a technical extension, not a bypass. Vinelands has it.
- **Route closures** (23 Oct): block every arm the race doesn't use with a wall of hay bales across its mouth, 1 m out from
  the junction edge. No direction boards: Richard found them "too confusing".
- Race-day dressing: start gantry, chequers, grid boxes for 24, painted flush kerbs (solid 10 cm kerbs stop cars), bale
  walls at heavy stops, corner boards, spectator cars. Finishes get the glowing gold beacon.
- Track standards: racing width C/B 10–12 m in towns and 16 m on circuits, A/S 13–15 m, E 14–16 m; at least 95 m behind
  the start line for 24 cars in 11 m rows; pit lanes reserved from the graybox for A and up; run-off from A. Design speeds
  (m/s): C 24–40, B 38–50, A 48–60, S 58–74, E 62–80.
- The AI must race every new track at skill 3, 7, 10 and 13, 24/24 with no resets (Richard, 20 Oct).

## 3 · The built places

All four C towns and Bramblewick are on by default in player builds. The editor's own checks and bakes still build the
old world unless asked.

### Bayview (rebuilt 19–20 Oct; default; `--old-bayview` for the old island)
- **Role:** the small tutorial island where the story starts, joined to Hillcrest by the causeway, with Coral Key on the
  horizon. Richard, 17 Oct: "a complete refresh, in Claude's own taste... nothing is kept" (it was "a bit bland"); only the
  idea stays.
- **Approved 19 Oct with the defaults:** the island grows to about 430 × 300 m; the lap runs clockwise so the harbour front
  comes last; the character is a Cornish and Scottish harbour (granite, slate, bold paint, not pastel or beige).
- **The Harbour Loop** (club, C and B; track id `bayview`): 1,079 m, every stretch teaching one thing: the start straight
  past the Pit Stop, the long right up the east coast, the hairpin, esses through town, the windmill, fast north cliffs,
  the lighthouse headland's tightening corner, the curving harbour front to the line. A 366 m gravel lane round the hill
  farm is the first rally stage. Planned: the Bayview Grand Prix (3.4 km, A/S/E: the loop, the causeway and the valley
  approach, pit lane on the quay).
- **Landmarks, one each way:** the lighthouse on the west point, the windmill on the summit, the clock tower on the quay,
  the pleasure pier with its helter-skelter off the east beach, the lifeboat station and slipway on the west arm.
- **Town:** a 16-house harbour row from the Forge terrace generator, the Pit Stop (garage and parts), the harbour at work
  (granite stair to a pontoon, fishing boats, buoys, creels, fish market, beach huts, a hill farm with sheep).
- **Residents:** Mara (garage), Pip (lighthouse; photo requests), Poppy (Bayview Halt).
- **OPEN:** people (cars) on the quay, a real beach (the east one is a steep sand bank), the dark retaining wall behind the
  row, bare cut slopes; sand run-off width is Richard's call.

### Hillcrest (Italian hill town, castle, valley and raceway)
- 2.4 × 1.6 km, Italian earth palette. Via Crest spirals up to **Hillcrest Castle** (donjon with needle spires, 58 m
  belfry, palazzo with loggia, drawbridge gatehouse) and the **Zip Tower** ride (160 m cable). The hill inside the spiral is
  terraced with vines, lavender, olives and dry-stone walls. Home of the old Continental main line and the Bayview Halt.
- Remodelled with Prop Forge (20 Oct): terraces, civic buildings, the castle set, balustrades, dry-stone terrace walls.
- **Races:** the Valley Cup on Hillcrest Raceway (C), the Castle Climb (time trial with Hard mode), Beat the Train;
  class B Valley Tuners and Raceway Reverse. The Duke's Challenge is raced here. Planned: Hillcrest Raceway GP, a pro layout
  through the valley past the castle, Duke Rolo's home track.
- **Residents:** Gio and Vito (garage), Rollo (the Regale parked at the castle), Orla, Farmer Giles (Golden Acre Farm).
- **OPEN:** a floating vine row at (160.5, 36.3, −1040.2), one hidden sign, valley road-edge lips; castle interiors and
  night floodlights.

### Quarry Hills (Welsh slate town; default since 4 Oct; `--no-quarry-hills`)
- **Vision:** "the mountain the town dug". Twelve slate galleries (15 m benches, 1.4 km wide) above the turquoise pit lake
  Glas, Blondin cable cranes, a gravity slate railway to Slate Quay, grey terraces. The quarry is winding down and the town
  bets on motorsport: you arrive as it opens a rally school. In the post-game the quarry has closed and the town is the
  home of rally. District 3.2 × 2.6 km, about twice Hillcrest. Reached by Quarry Road, whose crest opens the whole view.
- **Zones:** Slate Quay (harbour, rail terminus), the town (terraces, Capel Bethel, the Quarrymen's Institute, Caban cafe,
  the Pit Stop on Quarry Road), the Great Wheel, the quarry floor (rally school, rallycross loop, Barracks Row as the
  frenzy paddock, working machines), the galleries (hero landmark, two inclines), Glas, the Blondins, the tips, the
  Pine & Peak spiral, forest and moor with standing stones.
- **Richard's answers, 3 Oct:** weather waits; the Blondin is the ride, the inclines are scenery; the rally school comes
  before local races and parts (gold in all six lessons unlocks the rally kit); the dumper is a fun one-off; Welsh names
  yes (Gwen, Dai, Mari, Huw, Bethan, Nia, Megan).
- **Built:** Gwen's rally school (six lessons with bronze, silver and gold, and a Gran Turismo-style licence screen), the
  Quarry Hills Rallycross (C, 1.5 km, 16 m wide, 3 laps, 24 off-roaders; Huw's garage), Slate Rallycross and Rallycross
  Reverse (B), the Gallery Climb hill climb, the Quarry Sprint time trial, the Blondin ride, 12 slate carvings, the Blast
  Run (race Dai up the haul road before the noon blast) and the Tip Leap, sheep, the rugby ground, the Great Wheel.
- **Local parts** (Bethan's Grit & Gravel): gravel tyres and a gravel rack, behind `unlock: "rallyschool"`; class B rally
  tyres there from 4★. Nia's body shop sells the off-road bodies.
- **Planned (for the activities sweep):** the planned race layouts (Quarry Link C 3.1 km, Ridge Run B 3.4 km, Quarry Hills
  Stage A 6.5 km and its floodlit S night version, Galleries GP E 7.2 km); Glas Zip, Beat the Slate Train, Cavern Run,
  Heavy Haul, the drowned workings (a class B Dive-kit return), the Quarry Festival in the frenzy, and the rugby
  conversion kick (Richard, 7 Oct: "a fun physics game": push the ball over the bar, nobody tells the player).
- **Recruits:** Gwen (all six golds and a win on the Quarry Hills Stage), Dai (beat him in the Blast Run), Mari (the slate
  train race and all 12 carvings).

### Bramblewick (English West Country village; default since 21 Oct; `--no-bramblewick`)
- "The lanes where the hedges meet overhead": a green with a duck pond, St Michael's church, the thatched pub The Plough,
  cob-and-thatch cottages, a dairy, an orchard, 232 hedged fields, sunken lanes, a ford and a humpback bridge.
  2.9 km from Bayview by Valley Road South (7.5 m) over the pass from the farm lane.
- **Bramblewick Lanes** (C, 2★, 3 laps, about 3.18 km, 9 m lanes): designed with the track-design skill, every corner
  named for the village (Dairy T, Brook Esses through the ford, Barn Corner, Hedge Gap, Church Hill, Green Hairpin, Mill
  Bend, the humpback bridge kink). Jed offers it. Launch review: LAUNCH-READY.
- **Residents:** Jed (backyard tuner), Maggie (The Plough), Reverend Clem, Peg (dairy), Ted the lanes marshal.
- Six lucky horseshoes (a keepsake set), three speed traps, photo sights. Jed sells the City hybrid 1.5 (behind `--hybrid`).
- **OPEN:** Jed's other rural parts (lane tyres, farm-track steering, cold-air engine, battery-delete shell: they wait on
  the parts slot decisions, [design.md](design.md) §5); the milk-float duel with Peg (needs the Forge milk float); the
  Coast Branch halt.

### Vinelands (Portuguese river town; default since 23 Oct; `--no-vinelands`)
- **Identity:** a class C main town east of the Spine, 4.6 km from Bayview, reached from Bramblewick's gate. "The town you
  read from across the water": an east-facing amphitheatre of schist terraces above a 100 m-wide river, a granite crest
  village, open and horizontal where the other towns are corridors or spirals. One road runs through all three tiers.
  Douro flavour (Richard, 7 Oct: "Portuguese is good!"): schist, whitewash, azulejo blue, terracotta, ruby-port trim.
  It must not read as a second Hillcrest; the town is invented, not a copy of Pinhão.
- **The eight zones** (each built from its own brief and kit, 21–23 Oct):
  1. **Largo and Vila Velha:** the oldest core on the crest; the Largo with wave-pattern calçada, the Matriz church and its
     terrace, the town hall, Café Central; tight granite-and-whitewash row houses, stepped lanes, the washing tanks.
  2. **Rua Direita, the Mercado and the Jardim da Cerca:** the merchant high street, the Pit Stop on a paved pad, the 1958
     market hall and fair terrace, a walled convent garden with a bandstand.
  3. **The Calçada and the Ribeira do Cais:** the river's front door: three wine lodges, the quay with rabelo boats, the
     promenade and beach, the Ponte Nova to the far bank.
  4. **The working slope:** schist benches every 5 m of height; smallholdings, three quintas (one modern on wires, one old
     bush vines, one half abandoned), abandoned terraces gone to scrub, a hamlet round a threshing floor.
  5. **The Belvedere and the Mirante:** returnees' villas, the cemetery, windmills, the bell-tower knoll; below it the
     1930s road climbs in six bends (the Subida das Seis Voltas, 2.25 km, built as road).
  6. **Alto da Portela and the Entroncamento:** the newest edge (football ground, school, fire station) and the co-op
     Adega, bottling plant and cooperage.
  7. **Eiras and the Vale:** the sandy back garden: orchards, olive grove, cork oaks, walled fields, and the circuit.
  8. **The Outra Banda:** the far bank the whole town looks at: a café at the bridge foot, citrus, a hamlet with a chapel,
     a ruined quinta with a huge fig.
- **Circuito da Cumeada** (the Vale's farm lanes, closed by the club; host Sr. Rogério), redesigned with the track skill
  after Richard's "very safe design" note: **short lap** (C, 3★, 2 laps, 2.52 km, 14 corners: the Gancho do Sobreiro
  hairpin, a 250 m downhill plunge) and **long lap** (B "Cumeada Tuners", 2★, 2 laps, 3.63 km: a 690 m main straight, a big
  stop, and a technical infield that turns back on itself). Both raced 24/24 at skills 3/7/10/13.
- **Hill climb:** solo staggered starts against the clock (Richard, 7 Oct: "Real style is fun so lets do that") on the
  Seis Voltas, for A, S and E when class A arrives; the lower classes get it as a public time trial.
- No local parts. Life is wine tankers, barrel trucks, tractors with grape trailers, three-wheelers and wine boats.
- **For the activities sweep** (Richard liked the old roadster: "a good opportunity to get a cool model after performing
  the task required"): the far-bank cellar secret (an old roadster body), the Barrel Race ("Fun physics toy!"), the Grape
  Run, azulejo tiles as keepsakes, the harvest festival.
- **OPEN:** planting top-up (the audit fails on planting per road km), a handful of small junction seams and lips, two
  refused warehouses by the river; the Main Line doesn't reach here yet.

## 4 · Planned places with decisions already made

- **My City** (Richard, 7 Oct): "when you find it, it should be run down and empty, but you are gifted it as your city
  space. You can then purchase to flatten certain areas and then find people through the world that want to join your
  city, like the original." You pay once for each resident's land (moving and removing is free) in a city-planner view
  with your own roads, traffic lights and decorations; "My City has huge potential." The hub on the dam at Glasswater's
  north end, 8.8 km from Bayview, class A's home, the Grand Tour A start, the ramp down to Lowtown; Waterfront Circuit
  (B, 5★) and GP Park (5.5 km). OPEN: what residents give back, the planner's rules, when it unlocks.
- **Orchard End** (recruit: the apple-cart racer; Blossom Run, 3★; Glasswater & Southern railway), **Kettle Falls**
  (climb, 4★), **Cobble Cove** (cobbled quay sprint, 4★), **Saltmarsh** (low-tide beach sprint, gravel, 4★; foghorn horn,
  sea-glass paint). Everything from Harbour Point on is in the data files and the world-plan skill.
