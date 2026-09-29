# Bayview map sweep (2026-09-25)

Goal: lean Bayview into a Road Trip Adventure 2 toy world without touching the race corridor.
Captures come from `.\tools\capture-map.ps1 -Name <run>` (world review plus `--map-views`); run folders are never overwritten.

| Run | What changed | Clearance |
|---|---|---|
| before | Baseline (flagged: Rowan's Otter inside the shoulder at offset 11) | fail (pre-existing) |
| a1, a2 | Toy terrain shader (meadow patches, wildflowers, soft sandstone bands), toy sea, Neutral grading | fail (pre-existing) |
| b1–b2 | Kit high street; fountain/Rowan moved out of the shoulder | pass |
| b3–b5 | Harbour (sea wall, rail, stairs, pier, boats), kit lighthouse, windmill; palette fix | pass |
| b6 | All island trees and scrub as kit models | pass |
| c1–c2 | Farm, fields, campsite, lookout, village green, car park, chevrons, telegraph poles | pass |
| d1-traffic | `--ambient-traffic` (4 cruising family cars, free roam only, off by default) | pass |

## Checks

- Adventure smoke (`capture-adventure -Name v5-map-b`): all passed. This was after phase B and was not rerun after C/D.
- Frame-cost study 122, `bayview-s1709-map-cd`, rendered, compared with 120 `bayview-s1709-off`:

| | Finished | Collective (s) | Resets | Contacts | Mean frame | p95 frame |
|---|---|---|---|---|---|---|
| 120 (before map) | 24/24 | 803.352 | 0 | 6 | 12.05 ms | 26.73 ms |
| 122 (after C/D) | 24/24 | 803.352 | 0 | 6 | 10.55 ms | 20.88 ms |

Racing is identical; the toy trees render cheaper than the old realistic foliage.

## Rules kept

- Nothing new is solid within 12 m of the road centreline. `VillageReview` checks road support and obstructions every run.
- The sea-wall railing's colliders stop cars going over the promenade edge.
- Chevrons are non-solid.
- Free-roam traffic is behind `--ambient-traffic`.

## Still open

- Harbour Garage is still the older cube build.
- No lay-by viewpoint (it would need a physical road change, which needs the AI protocol).
- Ambient traffic cars never recover if stuck, because finished-car planning skips recovery.

## Fixes and paint shop round (same day)

- Fixes: window/shutter depths (no z-fighting), coral kerbs 6 mm prouder (curve overlap flicker), promenade kept flat to the sea wall, free-roam car pitches/rolls with the ground (`--free-roam-level` restores; target tilt capped at 30 degrees, hard righting past ~40; hill review: no tipping), Puddle roof spare seated on the roof.
- Paint shop: Dot at BAYVIEW PAINTS, 16 colours (first four = old save colours), 150–200 credits, `AdventureStore.Repaint` plus 4 editor checks (877 total). Mara's free repaint removed. `--paint-shop-review` captures in `paint-1`/`paint-2`.
- NPC speech bubbles on bumps: residents (own lines), parked, campsite and traffic cars.
- Adventure smoke v6-paint was an outlier: 6 AI resets, 26 contacts, collective 2852.7 s, with no scenery impacts. The repeat on the same build (v7-paint-repeat) matched earlier runs exactly (0 resets, 2068.7 s). The smoke race isn't fully deterministic, and that run exposed AI physical-recovery "Wait for clearance" loops; an AI robustness item, not a map one.

## Hillcrest Valley (second area)

Road Trip Adventure 2 builds each town around one landmark (Fuji City's castle and moat, Peach Town's roundabout), with shops, residents, two tracks, mini-games and hidden coins, and links towns with set pieces such as the Island Bridge. Hillcrest Valley follows that pattern at a larger scale. Bayview is the tutorial island. Crossing to the mainland is the reveal: the causeway climbs to a cliff, the Headland Tunnel runs through the ridge, and the road comes out above the whole valley.

- **Geography** (`HillcrestMainland.cs`, `HillcrestValley.cs`):
  - One 2.5 m height field, about 2.4 × 1.6 km, drawn as 150 terrain chunks with three levels of detail and their own colliders.
  - A sea cliff and ridge with the road tunnel through it.
  - The Silver river, with a waterfall into the gorge.
  - A lake, a ring of hills and far mountain ranges (the `LittleRoad/Distant` shader).
- **Roads:**
  - Mainline: causeway viaduct → tunnel → vista pull-off → descent with a hairpin → arched river bridge.
  - Quay (the lower town), Via Crest (spirals up the hill to the square and castle), the falls road, the station road and the raceway road.
- **Town** (`HillcrestTown.cs`, 286 buildings on stone plinths where the hill drops away):
  - Lower town: Hillcrest Motors, Grand Hotel, Riverside café, water mill, post office, jetty with boats, the town gate.
  - Market square: town hall with clock tower, church, cafés, deli, gelato, stalls, bandstand, bunting, a balustrade over the valley.
  - Castle: gatehouse, seven towers, walls and keep.
  - Countryside: vineyards, lavender and sunflower rows, draped crop fields, a farm, an olive grove, 12 wind turbines, the balloon field.
- **Life** (`HillcrestLife.cs`, off with `--no-valley-life`):
  - A steam train: out of the ridge, over the gorge viaduct, round the station loop and back.
  - Ten traffic cars on the town roads. They turn round if the player blocks them for 3 s.
  - Parked cars, six balloons and bird flocks.
  - Area title cards: BAYVIEW, THE CAUSEWAY, HEADLAND TUNNEL, HILLCREST VALLEY, HILLCREST, the castle, the falls, the raceway, the viewpoint.
- **Gameplay:**
  - Residents: Gio (Hillcrest Motors, with 8 valley-only parts), Fern, Vito (the Valley Cup, class B), Stella, Duke Rolo (the Castle Climb time trial: par 85 s, 300 credits once), Wren and Moss.
  - Eight Hillcrest stamps (+50 each).
  - After a Valley Cup race, free roam resumes at the raceway.
- **New kit models:**
  - Buildings: tunnel_portal, town_hall, terrace_a/b, station, mill, hotel.
  - Castle: castle_tower, castle_wall, castle_gatehouse, castle_keep.
  - Props: bandstand, wind_turbine, balloon, grandstand, train_loco, train_coach, olive_tree, lavender_row, sunflower_row, map_board, flag_pole, tyre_wall, ad_board.

### Hillcrest Raceway AI (`tools/race-hillcrest.ps1`, 24 cars, 2 laps)

| Run | Circuit | Drivers | Finished | Collective (s) | Resets | Contacts |
|---|---|---|---|---|---|---|
| r1-legacy-s1709 | hilly (8–22 m) | legacy | 0/24 | – | 173 | 414 |
| r2-support | hilly | legacy | 24/24 | 3422.5 | 0 | 360 |
| r3/r4-context-s1709 | hilly | context | failed | – | – | – |
| r5-level-legacy-s1709/1710/1711 | level | legacy | 24/24 ×3 | 3427.9 / 3422.3 / 3396.1 | 0 / 0 / 0 | 368 / 360 / 314 |
| r5-level-context-s1709/1710/1711 | level | context | 24/24 ×3 | 3314.7 / 3164.5 / 3192.8 | 1 / 0 / 0 | 16 / 26 / 22 |

- **r1:** the raceway surfaces lacked `RoadSupport`, and cars on an elevated circuit get no drive without it.
- **r3/r4:**
  - r3 failed on a baked-line height mismatch.
  - r4 failed because `ForecastRoadEnvelope` requires level roads.
  - The first r4 legacy run (`r4-legacy-s1709`) was stopped part-way when the circuit was changed.
- **Decision:** the circuit was made level (y 9) and the Valley Cup uses the context drivers:
  - Collective time is 5.6% lower on average.
  - Contacts drop from about 350 to about 21.
  - Context had one assisted reset: s1709, car 19, "persistent geometry", about 835 m round.
- **Racing line:** baked at load from the live circuit (`RacingLine.BakeSmooth`, the same method as `bake-racing-line-candidates.py --smooth-only`).
- **c2-valley-cup** (`tools/valley-race-review.ps1`): Vito → preview → career race on the 1600 m circuit, player on autopilot. Finished 21/24; class B reward settled (+179).

### Checks

- **Frame cost, Bayview race** (study 123 `bayview-s1709-valley` against 122): identical racing (803.352 s, 24/24, 0 resets, 6 contacts); mean frame 10.67 ms against 10.55 ms. Bayview races skip the valley's town, railway and woods (`CoastalLandscape.ValleyDetail`); they're hidden behind the ridge.
- **Frame cost, free roam** (`capture-map -Rendered --valley-views`, RTX 4070 Ti SUPER), v7:

  | View | Mean | p95 |
  |---|---|---|
  | reveal | 4.57 ms | 4.95 ms |
  | vista | 4.45 ms | 4.83 ms |
  | square | 3.44 ms | 3.89 ms |
  | castle | 3.99 ms | 4.51 ms |
  | quay | 3.72 ms | 4.07 ms |
  | Bayview | 2.82 ms | 3.28 ms |

- **Adventure smoke** (`capture-adventure -Name v9-valley`): passed.
- **Clearance** on the Bayview loop: passes in every valley capture.

### Fidelity, population and railway sweep (Richard's afternoon list)

- **Terrain:** the mainland terrain uses `_ToyVariety`, a new `CoastalTerrain` property that defaults to 0 on the island. It adds:
  - golden dry-meadow patches (~300 m) and lusher low ground (~900 m);
  - cooler, deeper greens above 45 m.
- **Nature kit:** tree_fir, tree_poplar, tree_blossom (a nod to Peach Town), tree_birch, tree_autumn, rock_a, rock_b, rock_cluster, reeds, grass_tuft, flower_bush.
- **Planting by biome:**
  - Firs on the high hills, birches by the lake, autumn maples mixed through the woods.
  - Poplar avenues along the valley road, the raceway road and the falls road.
  - Four blossom orchards.
  - 650 boulders and slabs on slopes and summits.
  - Reeds on the river banks and the lake shore.
  - About 5,200 grass tufts on verges and meadows, plus flowering bushes.
- **Particles and air:**
  - Dust puffs behind the player's car on dirt, grass and runoff.
  - Chimney smoke from about a third of the houses (only within 320 m).
  - Sunlit pollen motes around the player in the valley.
  - Steam from the train (as before).
- **Lighting:** a soft vignette and +5 white-balance warmth. SSAO was already on in the PC renderer.
- **Mountains:** the ranges were rendering white because `Mathf.SmoothStep(a,b,t)` was used as if it were smoothstep(edge0, edge1, x). Snow and foothill colour now use the proper blend.
- **Population:**
  - Eight new townsfolk bodies: bubble, beetle, taxi, camper, gelato (ice-cream van), minibus, ritz, postvan. They're in `CarBodies.Townsfolk` only, never dealt into race fields, so race rosters are unchanged.
  - House rows moved back (setback 12.2) to give front yards.
  - About 70 residents parked outside their homes. E to talk: names plus gossip and hints (stamps, the Castle Climb, parts, the line).
  - Also a gelato van in the square, a taxi rank at the station and a post van at the post office.
  - The new bodies also join the traffic.
- **Railway and fast travel:**
  - The Hillcrest Line now runs the full trip: Hillcrest station loop → valley viaduct → falls gorge → headland (hidden) → sea-cliff portal beside the road tunnel → sea viaduct → loop at Bayview Halt → back. The train stops at both stations; the path is 3.4 km.
  - `TrainNetwork` stations: Bayview Halt (Stanley, at the car park) and Hillcrest (Stella). Tickets cost 20 credits and fade the car to the other forecourt.
  - Hillcrest opens once visited by road (`visit_hillcrest`), so the tunnel reveal isn't skipped.
  - Future areas join by adding a station.
- **Other fixes:**
  - Town lamp posts are non-solid.
  - Traffic keeps to the right, clear of the centre, and turns round after 3 s if blocked.
  - Route drive: the autopilot plans speed from curvature 30 m ahead and drives the right-hand lane.
  - `capture-map -Rendered` for real frame timing. Batch mode doesn't render every frame, so its timing is meaningless. Rendered runs also fail the garage-drive check because synthetic gamepad input needs window focus, which is a harness limit, not a game bug.
- **Frame cost** (v9, rendered, after the sweep): reveal 5.30, vista 5.17, square 3.98, castle 4.35, quay 4.47, Bayview 3.03 ms mean.
- **Final checks (same build):**
  - v10 batch capture: clearance and garage drive pass.
  - v10 adventure smoke: passed.
  - c3 Valley Cup career race: runs and settles; identical to c2.
  - d5 route drive: Bayview car park to the castle courtyard in 110.3 s, 0 recoveries, road grip 100%. The Castle Climb leg (gate to castle) took 64.2 s, against a par of 85 s.
  - v11 rendered frame cost: reveal 5.28, vista 5.19, square 3.98, castle 4.34, quay 4.44, Bayview 3.02 ms mean.
- **Counts:** 391 town buildings, 76 townsfolk, 2,200 trees plus 83 poplars and 118 orchard trees, 650 rocks, 5,204 grass tufts; a 3,425 m train loop with stops at Hillcrest and Bayview Halt.

### Bayview end fixes (2026-09-26, Richard's review of the tour)
- **Junction:** the mainline now leaves the Bayview road's south edge at (68.3, -83.4), not the car park. There's a gap in the track-side wall in adventure mode only; races keep the wall.
- **Bayview Halt:** a walled quay (x 56.3–108.7, z -162.7 to -86) flattened into the island terrain. It has sea walls and promenade rails, a paved forecourt, the station and platform, lamps, palms, benches and three NPC cars. Stanley and the train arrival point moved there. The sea loop is now a stadium shape whose west straight is the platform (rail at y 0).
- **Causeway:** stone arches between piers, pier imposts and cutwaters, an open balustrade (an invisible wall keeps cars on), pilasters, pennants and lamps. A HILLCREST VALLEY gateway stands where the road leaves the quay, with abutments at the quay and the cliff. The sea and valley rail viaducts also got arches on their straight spans.
- **Headland:** the uniform 54 m sand wall is replaced by a broken skyline of grassy downs over low sea cliffs. Only the bluff the tunnels pierce is tall. Mainland rock is now grey limestone with faint strata, and there are sea stacks and boulders along the cliff foot.
- **Portals:** the approach cuts no longer notch the rock above the mouths. Each road mouth is a shelf over the bore (±9 m), with the full face behind. The rail mouths are restored to a rock face behind the portal, with a dark plug just inside. The PortalHole no longer removes the face above the shelf.
- Captures: `v12-bayview-fixes` (first pass) and `v13-bayview-fixes` (final; v23–v32 are new Bayview-end views).
- `d6-junction` route drive, same build: arrived=True in 110.0 s, gate 45.8 s, Castle Climb leg 64.2 s, road grip share 1.00, 0 recoveries.
- Known leftovers: a shallow recess still shows above the road portal, and the rail portal at the sea sits on a sloping cliff face.

### Farms and drive-through wheat (2026-09-26)
- **Rail tunnel mouth:** SeaPortal moved 3.5 m out from the cliff (z -448.5). The dark backing now sits in front of the rock, so the opening reads as a tunnel again (`v14-farms/v29`).
- **Wheat (new):** `WheatField.cs` plus the `LittleRoad/Wheat` shader (added to the MigrationBuild shader list).
  - Stalks are 1.4–1.8 m tall, chunked in 20 m chunks with a canopy-sheet far LOD.
  - A trample texture (0.4 m texels; R = how flat, GB = push direction) bends each stalk about its root. Stalks stay flattened, so crop circles work.
  - Only the adventure player tramples (`WheatField.Player`, set in ValleyLife). Fresh wheat adds a little drag and a spray of chaff.
- **Golden Acre Farm (Hillcrest):** one estate replaces the scattered east-valley lavender, sunflower and hay rows and the small barn at (-10, -1240).
  - Ground: a 300 m levelled pad at FarmCentre (720, -1262), yaw 8.
  - Access: a new Farm lane from the station yard, lined with poplars, with a GOLDEN ACRE FARM gate.
  - Contents: farmyard (house, 1.6× barn, silo, two tractors, bale stack, Farmer Giles to chat to), a fenced 130 × 86 m wheat field with 20 m gaps on all four sides and a scarecrow, sunflower and lavender plots, a ploughed field with 3-D ridges, and a hay meadow with bales.
  - The area banner reads GOLDEN ACRE FARM.
- **Valley patchwork fields:** these now follow the farm's grid and axis in two sizes, and sit on the highest nearby ground so they no longer sink into it.
- **Bayview farm:** a levelled pad in the island terrain (kept out of the race shoulder). House and barn now face an earth yard, with a tractor, a bale stack, a fenced 30 × 18 m drive-through wheat field and a lavender plot. The old flat coloured sheets are gone.
- Captures: `v14-farms` and `v15-farms` (v33–v38 farm views, with a crop circle pressed in for the check), `m20`/`m21`/`m22-farms` (island).
- `d7-farms` route drive, same build as v15: arrived in 110.0 s, 0 recoveries, road grip share 1.00.

### Bayview gatehouses and realism sweep (2026-09-26)
- **Gatehouses (new kit pieces `headland_gate` and `rail_gate`):** both Bayview-side tunnel mouths are now fortified gates on masonry sea bastions. Each bastion is battered masonry with coursed stone, a cornice and corbels, a wet band and weed line at the waterline, and crenellated parapets.
  - Road gate: twin round towers rising from the water, a crenellated and machicolated gate block whose arch matches the bore, a HILLCREST VALLEY board, a crowned crest, red and gold banners and lanterns.
  - Rail gate: square towers with slate pyramids, a clock over the arch, a semaphore signal and a dark mouth built into the kit.
  - The bastions meet between the gates, the causeway and sea viaduct land on them, and the old causeway abutment and rail plug are no longer used on this side. The valley-side portals keep the plain kit.
  - The "railway" sign kind uses a text scale of 1.35. Sea rocks are kept clear of the bastions.
- **Scan findings and fixes:**
  - No access to the Bayview farm, campsite or lookout → dirt tracks off the inside of the loop.
  - Station cars parked on grass → a paved station forecourt with bays. The farm lane now runs round its west edge.
  - Raceway garages and cars on grass → a tarmac paddock (100 × 38 m) with bays.
  - Blank terrace backs facing the entrance road → sash windows and back doors on the terrace kits.
  - Cottages on the town hill with no way to reach them → removed (trees only).
  - A picket fence cutting through the lighthouse cottage and floating on the slope → removed.
  - Ruled lagoon edges by the bridge → a noisy shoreline (still somewhat straight in m10).
  - Viaduct arches and piers hanging over slopes → arches are skipped when the ground rises under them, and piers stand on the lowest ground under their footprint.
- **Known leftovers:** the gorge walls by the falls show grid zig-zags on their top edge, and the lighthouse stands on the lagoon's edge.
- Captures: `v16-sweep` / `m23-sweep` (first pass), `v17-sweep` (final).

### Look overhaul and file clean-up (2026-09-26)
- **Look ("toy shapes, real light"):** the rules are in `docs/art-direction.md`, the numbers in `unity/Assets/Scripts/WorldLook.cs`, and `WorldLookChecks` enforces them in the build (checks=994).
  - Surfaces: `TrialWorld.Material` grades every colour (saturation ×0.8 and capped at 0.70, value 0.05–0.90). Car body paint is exempt.
  - Terrain: `_Stylise` is 0.5 (it was 1, fully flat toy colour), so the grass and rock photo maps show through. Grass colours are more natural and there are fewer flower dots.
  - Light: sun 2.4 with shadow strength 0.92, cooler and dimmer fill and ambient, shadows reaching 240 m (140 m in races), SSAO 0.85 / radius 0.55.
  - Grade: saturation −10, contrast +16, cool shadows and warm highlights, warm white balance, bloom 0.22, vignette 0.2.
  - Clouds: a drifting cloud-shadow cookie on the sun, supported in the terrain, foliage, wheat and distant shaders.
  - Water: river and lake are now deep teal instead of toy cyan. The valley patchwork fields are graded too.
- **Map fixes:** the gorge walls by the falls are smoothed (no more grid zig-zag). Bastion rocks were cleared earlier.
- **Files:** `tools/cleanup.ps1` (dry run by default, `-Apply` sends files to the Recycle Bin, and build-dev.ps1 runs it after every good build).
  - The first run removed 4.7 GB: 1,038 screenshots in superseded capture runs, 1.8 GB of per-tick race telemetry and a 776 MB runaway log.
  - Stray Unity runtime files were also removed from the outputs folder.
  - Scratch saves now go in `review/tmp/` (git-ignored). CLAUDE.md records the rules.
- Captures: `v19-look`/`m24-look` (first pass), `v20-look`/`m25-look` (final). Compare with `v18-sweep`/`m23-sweep`.
- `d9-look` route drive, same build: arrived in 110.0 s, 0 recoveries.

### Fidelity sweep, 3/10 → ~6/10 target (2026-09-26/27)
- **Kit surface shader (`LittleRoad/Kit`, new):** every Blender kit material, plus the code-built masonry (causeway, viaducts, tunnel lining, paving, plinths), gets a surface pattern chosen from the material name (`WorldLook.PatternFor`):
  - plaster mottling and faint runs; coursed stone in running bond (0.86 × 0.42 m), with flags on flat tops; tile joints with lichen; wood grain and board joints; leaf clumps with light through them; worn paint; glass; and natural rock with cracks.
  - Weathering at the base of walls, a slow warm/cool drift between neighbouring buildings, and a bump from the pattern (no tangents needed).
  - Fine detail fades from 35 m to 150 m, so there's no shimmer at distance. SSAO and cloud shadows apply.
- **Kit geometry:** `build_bayview_kit.py` now bakes `KitSurface`/`KitHeight` UV layers on every asset, so the patterns survive static batching and weathering knows the base.
  - Added: window lintels and dark reveals, gutters and back-corner downpipes on gable roofs, terrace gutters, downpipes, chimney caps and pots, rear windows and doors on shops and cafés, fuller round, pine and cypress canopies with boughs.
  - All 77 assets were rebuilt.
- **Checks:** `WorldLookChecks` confirms kit materials use the kit shader with graded colours and that the palette maps to patterns (checks=997).
- Captures: `v21-fidelity` / `m26-fidelity` (first pass), `v22-fidelity` / `m27-fidelity` (final). Close-ups are in f01–f04.
- `d10-fidelity` route drive, same build: arrived in 110.0 s, 0 recoveries.
- **Pass 3:**
  - Foliage spheres are noise-displaced into lumpy leaf masses.
  - Deciduous trees get satellite clumps, which break up their silhouettes.
  - Firs are built from star-rimmed drooping tiers.
  - Stone courses vary block length per row, with rain streaks from ledges.
  - Kits with modelled courses (sea wall, harbour steps, both gatehouses) use the natural-rock surface instead of doubled courses.
  - Market stalls get slatted crates, heaped two-layer produce with leaves on the greens, and a chalk price board.
  - Bunting is smaller (0.62 m spacing, 0.36 m flags) with graded colours.
  - Captures: `v23-fidelity` / `m28-fidelity`. `d11-fidelity` route drive: arrived in 110.0 s, 0 recoveries.
- **Pass 4:**
  - Castle and church stone is aged: `BayviewKit.AgedStone` makes it greyer and darker (×0.86) with wear 2.2, which drives broad weather staining and lichen patches in the kit shader.
  - Cypresses are now a leaning flame of five lumpy sections (no bump tufts).
  - The natural-rock surface is calmer: mottle 0.18, fine grain 0.09, cracks 0.1.
  - Captures: `v24-fidelity` / `m29-fidelity`. No collider or road changes, so the `d11` route drive still stands.
- **Pass 5:**
  - Harbour wall: dark blocks are now a sparse random 10% (`WALL_RND`) instead of the every-fifth diagonal.
  - New house kits:
    - `terrace_c`: arcaded ground floor.
    - `terrace_d`: bay window on brackets and dormers.
    - `house_c`: villa under a hipped roof, with a three-arch loggia, bracketed eaves and garden urns.
    - `house_d`: tower house with a top-floor loggia under a low hip roof.
    - A new `hip_roof` helper supports them.
  - Street rows now draw from 8 types (terraces 62%, villas 10%, tower houses 10%, cottages 18%).
  - `ResidentIdle` (new) on townsfolk, the farmer, the Halt cars and the Bayview car park cars: idle sway on the springs, the nose turns up to 40° towards the player within 14 m, and a hop with a brake-light flash when they speak (bump chatter or E-talk). Only an "Idle pivot" moves; the collider stays put.
  - Captures: `v25-fidelity` / `m30-fidelity`. `d12-houses` route drive: arrived in 110.0 s, 0 recoveries.
  - Not yet seen in motion: ResidentIdle (the captures are stills).

### Terrain and world sweep (2026-09-27)
- **Scores before:** sky 6.5, grass 4, hill rock 3.5, mountains 3.5, water 4, tree layout 4.5, roads 4.5, vineyards/fields 4 (overall ~4.5).
- **Ground (`CoastalTerrain`):**
  - Mid-scale (8–30 m) breakup of rock/grass edges; pasture patches (lusher hollows, paler knolls, darker slopes).
  - Stylise drops close up and on rock, so the photo texture shows.
  - Rock outcrops get crevice and foot darkening.
  - Mainland only: `_RockDesat` 0.7 (greyed photo rock) and `_MeadowBias` 0.07 (more grass on moderate slopes).
- **Mountains:** 460 × 40 grid (was 200 × 14) with ridged octaves. Colours come from height and slope: forest foot, alpine grass, grey-brown rock faces, snow above 860–1000 m on gentle ground only. The first pass had snow from 560 m and read as a white wall.
- **Water:** new `LittleRoad/InlandWater` for the river, gorge and lake (in the MigrationBuild shader list). Depth from the scene depth texture gives shallows, a clear edge, a foam line and deep teal. Ripples are gentle and fade with distance (the first pass looked like mosaic tiles).
- **Trees:** `KitLod` (new) gives kit trees and bushes a far stand-in (80-face lumpy canopy or cone, plus trunk; LOD0 down to 3.5% of screen, off below 0.15%). Woods are now copses (3–12 trees, one main species, some bushes), with a cap of 5,400 trees (was 2,200). Hedgerows run along 65% of the valley fields.
- **Roads:** asphalt pattern 9 in the kit shader (aggregate, repair patches, cracks only in worn stretches). Gravel verges on country roads away from the causeway, tunnel, bridges and town.
- **Vineyards:** each row is one continuous lumpy hedge with grape bunches.
- Captures: `v26-terrain` / `m31-terrain` (first pass), `v27-terrain` / `m32-terrain` (final). `d13-terrain` route drive: arrived in 110.0 s, 0 recoveries.
- **Frame cost (`r1-terrain-cost`, rendered):**
  - reveal 10.41, vista 9.72, square 7.10, castle 7.20, quay 7.59, bayview 5.50 ms mean.
  - v11 (2026-09-25) was 5.28 / 5.19 / 3.98 / 4.34 / 4.44 / 3.02.
  - Bayview also doubled, so the look overhaul (SSAO 0.85, 240 m shadows) and the kit shader are the likely main costs, not the woods. Not yet profiled.

### Frame-cost study (2026-09-27, `tools/look-cost-study.ps1`, rendered 1920×1080, RTX 4070 Ti SUPER, mean ms)
Same build, off-by-default `--perf-*` switches (in `WorldLook`), `--valley-cost-only` skips the captures.

| Run | reveal | vista | square | castle | quay | bayview |
|---|---|---|---|---|---|---|
| p1 base (a/b) | 10.27/10.29 | 9.63/9.62 | 7.16/7.16 | 7.29/7.28 | 7.93/7.67 | 5.55/5.59 |
| p1 no-ssao | 8.87 | 8.40 | 6.10 | 6.18 | 6.52 | 5.01 |
| p1 shadows-110 | 10.31 | 9.68 | 7.16 | 7.35 | 7.61 | 5.56 |
| p1 plain-kit | 5.58 | 5.62 | 4.45 | 5.21 | 4.95 | 3.70 |
| p1 no-clouds | 9.24 | 8.67 | 6.06 | 6.11 | 6.40 | 4.95 |
| p1 sparse-woods | 9.94 | 9.27 | 6.93 | 7.13 | 7.38 | 4.87 |
| p2 base (own kit shadow/depth passes, SSAO downsampled) | 8.85 | 8.34 | 5.81 | 5.72 | 6.06 | 4.88 |
| p3 base (+ kit far path beyond 150 m) | 8.92 | 8.36 | 5.81 | 5.75 | 6.10 | 4.87 |
| p4 plain-kit | 5.18 | 5.12 | 4.10 | 4.59 | 4.40 | 3.52 |
| p4 leaf-cull | 8.68 | 8.14 | 5.59 | 5.59 | 5.84 | 4.64 |
| p4 lit-leaves | 7.77 | 7.41 | 5.33 | 5.61 | 5.44 | 4.06 |

- **Kept:**
  - Kit shader's own ShadowCaster/DepthOnly/DepthNormals passes (−1.4 ms at reveal; borrowing Lit's passes broke SRP batching).
  - SSAO Downsample 1.
  - Single-sided leaves (−0.2 ms; canopies are closed shapes).
  - The far path (neutral).
- **Findings:**
  - Shadows at 240 m cost nothing, and the extra woods ~0.3 ms.
  - Cloud cookie ~0.35 ms after p2; SSAO ~1.6 ms.
  - The kit shader still costs ~3.6 ms over plain Lit: ~0.9 ms is the leaf pattern, and the rest is not per-pixel (the far path didn't help). Likely batching or draw overhead. Needs an editor profiler session to pin down.
- The `--perf-*` switches stay in the build for future A/Bs.
- **Crops and mountains (same builds as p2–p4):**
  - Valley patchwork fields are now lit crop ridges (0.16–0.7 m by crop) over a furrow-coloured ground sheet, with gentle per-field tone noise.
  - Mountains: warmer grey-brown rock, a dark forest band up to ~300 m at the foot, haze 0.0001 (was 0.00012).
  - Captures: `v28-terrain`, `v30-terrain` (final). `d14-perf` / `d15-perf` route drives: arrived in 110.0 s, 0 recoveries.

### Play-test fixes (2026-09-27, Richard's list)
1. **Hard stop back onto the road:**
   - Cause: mainland road ribbons sit ~0.17 m above the terrain (the corridor is stamped 0.14 m below the road) and the car is a box collider that cannot climb the step.
   - Fix: `Shoulders` adds a drivable 1.8 m gravel ramp from each road edge to the ground (0.02 m below it), on all valley roads and around the raceway runoff. It is skipped where the ground falls away, over the river and in junction mouths. Forecourt and paddock aprons now ramp down at their borders.
2. **Junctions:**
   - Continuous roads are one ribbon each: the valley road (raceway road + quay + falls road) and the station road (station spur + farm lane).
   - T-junctions (mainline/valley road 103°, Via Crest 89°, station road 90°, mainline/Bayview loop 92°) get 6 m corner fillets (drivable), a gap in the through road's edge line, double dashed give-way lines across the right-hand approach lane and a give-way sign. Shoulders and guardrails stop at the mouths.
   - Via Crest and the station road now leave the quay square.
   - Verge trees keep 8 m from road centrelines (one sat on the new Crest corner).
   - The Bayview loop has no fillet (its runoff band is flush), and its painted edge line still crosses the mouth.
3. **Dust:**
   - Transparent, flattened, fading puffs from each rear wheel, 0.35–0.95 m, coloured between grass and dirt.
   - Light tyre smoke when sliding on tarmac (lateral speed > 3.2 m/s).
   - `PuffPool` gained a soft mode (per-puff alpha via property block).
4. **Talking to cars:** `NpcChatter` keeps a registry. Any standing car that isn't handled elsewhere can be talked to (E / Cross) with its own title and lines: parked valley cars, Halt cars, Bayview car park and campsite cars, Farmer Giles, and stopped traffic. Parked valley cars and the camper also turn to face the player.
5. **Hillcrest Raceway v2:**
   - Design principles from the research: long straights into heavy-braking corners, mixed alternating corner speeds, esses, a tightening corner, 1–2 hairpins, and two overtaking zones.
   - 1.69 km, radii 23–106 m: main straight → T1 90° L (r40) → esses → long tightening L → back straight → T5 90° L (r41) → kink → right and left hairpins (r24/23) → fast final L. The layout was checked in a Python evaluator for radius, self-clearance (37 m), lake (165 m) and river (41 m) clearances.

| Run | Drivers | Finished | Collective (s) | Resets | Contacts | Best lap |
|---|---|---|---|---|---|---|
| r6-v2-context-s1709/1710/1711 | context | 24/24 ×3 | 3464.5 / 3502.0 / 3486.4 | 0 / 0 / 0 | 8 / 4 / 6 | 53.67 / 53.68 / 53.64 |

- Route drives: `d16-roads` failed (tree on the Crest corner, fixed); `d17-roads` arrived in 110.8 s, 0 recoveries. Captures: `v31-roads`, `v32-roads` (j01–j05 junctions, r01–r03 raceway).
