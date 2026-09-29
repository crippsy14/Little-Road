# Graphics scorecard: baseline (28 September 2026)

Scored as if Little Road were going on Steam next month.

**Scale.** 10 = best in class for a stylised indie racer or explorer. 7 = good enough to feature in a trailer. 5 = acceptable and unremarkable. 3 = would draw negative reviews.

**Calibration.** Stylised games that pair toy or simple shapes with real light:
- Art of Rally
- Lonely Mountains: Downhill
- Tiny Glade
- A Short Hike
- Hot Wheels Unleashed (the toy look at a high budget)

**Evidence.** Dev build with 1036 checks:
- `review/map-sweep/m34-graphics/` (Bayview and map views);
- `review/map-sweep/m34-valley/` (valley, junctions, raceway);
- `review/cars/lineup.png`;
- frame cost `review/map-sweep/p5-{before,after}-*`.

All on an RTX 4070 Ti Super at 1920×1080.

**Overall: 5.0 / 10.** The towns (6.5–7) show the style working. The countryside, water, distant scenery and image stability (3–5) pull it down. Players see those in most frames.

## Progress: first pass (29 September 2026)

**Overall: 5.0 → 5.9.**

Evidence:
- `review/map-sweep/m43`, `m44` (grass), `m41` (valley), `m37`/`m38` (Pit Stops and TAA/SMAA).
- `review/ui-screens/body1` (body shop) and `smoke4` (full adventure smoke with a race).
- Frame cost `p8-graphics`: reveal 9.5 ms and Bayview 5.2 ms at 1080p, with grass, wind and TAA all on (was 8.9 / 4.7).

| # | Aspect | Was | Now | What changed |
|---|---|---|---|---|
| 13 | Anti-aliasing and stability | 3.5 | 6 | Temporal AA by default (`WorldLook.Camera`), with history reset on camera cuts. |
| 14 | Pattern filtering | 4.5 | 6 | Kit patterns (joints, courses, board seams, cracks) and terrain flower dots fade by pixel footprint. The rock bump is low-frequency (the verge speckle is gone). |
| 16 | Speed presentation | 3 | 5 | Speed-dependent FOV (+11°) and light race motion blur. |
| 17 | Frame pacing | 5 | 7 | Free roam uses vSync at the display rate. Races keep the 120 fps target they are tuned at (vSync jammed a smoke-test race start). |
| 22 | Ground cover | 3 | 5.5 | `GrassField`: instanced grass tufts and wildflower drifts within 44 m, placed on real ground, swaying in the wind. |
| 28 | Distant mountains | 4.5 | 6 | Finer mesh, shaded gullies and ridges, layered haze and a real snow line. |
| 10 | Atmosphere | 4.5 | 5 | Mountain haze retuned. There's still no valley height fog. |
| 21 / 32 | Trees and world motion | 5 / 5.5 | 5.5 / 6.5 | Canopies sway (Kit leaf pattern, in every pass so shadows follow). The grass sways too. |
| 33 | Vehicle effects | 4 | 5.5 | Tyre marks (rubber on tarmac, ruts off road). Dust and smoke on every car in rendered races. Brake and reverse lights already existed; the baseline row was wrong. |
| 34 | Collectibles | 3 | 5 | Postcard and stamp keepsake models that bob. |
| 36 | World labels | 3.5 | 6 | Only the nearest resident's tag shows, sized for the chase camera. |
| 38 | Loading | 3 | 4.5 | A designed loading card with tips replaces the frozen frame. The build is 7.8 → 7.3 s (cliff colliders and queries). |
| 39 | Graphics options | 2 | 5.5 | Pause > Graphics options: anti-aliasing, grass, shadows, frame rate and field of view (saved). There's no resolution or preset screen yet. |
| 7 | Contact AO | 5 | 5.5 | SSAO radius 0.8 and intensity 1.0, with a warmer ground ambient. |
| new | Town services (Pit Stop) | – | 7 | A uniform branded services building in every town (see `docs/art-direction.md`). |

Bugs found along the way:
- **Mainland residents were buried.** Every resident on the mainland (Gio, Fern, Vito, Stella, Duke, Wren, Moss) sat at y≈0 under the terrain. Only their floating labels showed.
- **Old saves didn't migrate.** Pre-body-shop saves failed to migrate, because JsonUtility reads a missing list as empty, not null. An editor check caught it.

**Next:**
- Valley height fog.
- Water edges and river banks.
- Time of day.
- Raceway paddock and grandstand.
- Resolution and preset screen.
- Async world build for real load-time cuts.
- A pickup flourish for collectibles.

## Progress: fourth pass (30 September 2026)

Bayview was catching up with Hillcrest (Richard: "it feels a little left out"), then the weakest spots elsewhere.

Evidence:
- `review/map-sweep/bayview-before` against `bayview-after-2` (town, green, hills);
- `water-foam-1` / `water-foam-2` (harbour, bridge);
- `valley-fix-7` (`v10b-falls-below`, a new review view looking up at the falls).

| # | Aspect | Was | Now | What changed |
|---|---|---|---|---|
| 3 | Bayview town | 6 | 7 | The high street is a continuous run of two- and three-storey terraces in the seaside palette, with a surf shack and a chip shop, bunting lamp to lamp and café parasols. Frontages step back to clear the race shoulder. |
| 22 | Ground cover (Bayview) | 5.5 | 6.5 | Meadow grass now grows on Bayview's verges and village green (it was excluded 13.5 m from the circuit and across the whole village). |
| 21 | Trees (Bayview) | 5 | 6 | Olive groves, blossom and poplars join the pines and cypress (640 trees); lavender and vine plots on open slopes. |
| 27 | Water | 5 | 6.5 | Contact foam wherever the sea meets something upright (sea walls, pier legs, hulls, rocks). Silver Falls rebuilt: the river now ends at the real cliff edge (it overhung the gorge by 10 m) and falls in an arc of translucent streaked water (`LittleRoad/Waterfall`), framed by rocks, into churning foam. |

Frame cost: the free-roam drive benchmark is 4.9 ms mean and 8.1 ms p95. Its one long frame (0.75 s) is the world-map bake behind the loading card.

## Progress: third pass (28–29 September 2026)

**Overall: 6.8 → 7.4.** The work follows the research audit in `docs/visual-quality-audit.md`.

**Evidence:**
- `review/map-sweep/look-*` (views);
- `rim-off-2` against `rim-on-2`;
- `db11` (uncapped drive benchmark: mean 5.6 ms, p99 9.0 ms, worst frame 19 ms, none over 33 ms);
- `review/ui-screens/smoke14`.

| # | Aspect | Was | Now | What changed |
|---|---|---|---|---|
| 17 | Frame pacing | 7 | 8.5 | **Hitches traced and removed:**<br>• grass fill time-sliced to 1 ms a frame;<br>• reflection probes refresh only nearby, every 4° of sun;<br>• shader and cross-fade variants warmed behind the loading card (free roam and races);<br>• traffic kept out of the castle gate.<br>The benchmark is uncapped and screenshot frames are excluded. |
| 6 | Ambient and indirect | 4 | 6 | Sky-tinted rim and warm ground bounce on all kit pieces. |
| 7 | Contact AO | 5.5 | 6 | Stronger, tighter SSAO. Sea rocks now stand on the seabed (the placement audit found them floating). |
| 31 | Cars in the world | 6 | 7.5 | Body roll, dive, squat and bob on springs; wheels planted; physics untouched. |
| 13 | Image stability | 6 | 7 | TAA actually runs (the UI camera stack had disabled it). Props, trees and car LODs cross-fade instead of popping. |
| 28 | Distant mountains | 6.5 | 7 | Forest climbs to a ragged treeline, giving each range a dark base. |
| 24 / 25 | Terrain | 5.5 / 6 | 6.5 / 6.5 | Warm, darker limestone with strata. Grass holds on all but steep ground (the grey smears are gone). The town hill is terraced with vines, lavender and olives, with dry-stone walls. |
| 29 | Roads | 5 | 6.5 | The Bayview circuit uses the kit asphalt (aggregate, repair patches, cracks). A rubbered-in racing line runs on both circuits. |
| 32 | World motion | 6.5 | 7.5 | Flags and banners fly and hang in the wind (baked freedom), and bunting flutters. |
| 33 / 37 | Effects and camera | 5.5 / 5.5 | 6 / 6.5 | Camera shake on knocks (trauma²). The zipline camera pulls back. |
| 36 | UI | 7 | 7.5 | The HUD and menus are overlays: never blurred by motion blur or the speed FOV. |
| 4 / 3 | Composition | 5 / 8 | 6 / 8 | Tunnel exit framed by pines and cypresses, clear of the castle sightline. |

## Progress: second pass (28 September 2026)

**Overall: 5.9 → 6.8.**

Evidence:
- `review/map-sweep/r1-*` (afternoon, dusk and night).
- `fields-3` and `fields-3-night` (valley ground and frame cost).
- `castle-2` (castle, zipline and a real ride, `zip-ride.txt`).
- `review/cars/closeups-5` (nose, arch and top close-ups of all 24 bodies).
- `review/ui-screens/smoke7`.

Frame cost is unchanged within noise: reveal 9.7 ms, Bayview 5.3 ms and the castle 5.5 ms at 1080p. Night is 9.6 / 5.4 ms, so the lights cost nothing measurable.

| # | Aspect | Was | Now | What changed |
|---|---|---|---|---|
| 9 | Sky and clouds | 5 | 7 | Procedural sky (`DaySky.hlsl`): sun and moon discs, glow, stars, and three-octave clouds matching the shadow cookie. The sea reflects the same sky. |
| 11 | Time of day | 2 | 7 | `TimeOfDay` rewrite: a real sun path, keyed light, ambient and fog, and moonlight. Lit windows, street lamps and headlights at night. Presets or a running cycle in Graphics options. |
| 2 | Palette | 5 | 6.5 | Grade and surface limits pulled off pastel (deeper grass, leaves, asphalt and walls; Ligurian ochres in Hillcrest). |
| 25 / 22 | Terrain surface and ground | 4 / 5.5 | 6 / 6 | The valley is a patchwork: hedged field parcels (pasture, hay, barley, lavender, ley and fallow) with crisp shader hedgerows from a baked edge-distance field. Dark woodland floor under the copses and on steep banks. Drier upland grass. |
| 28 | Distant mountains | 6 | 6.5 | Lit by the real sun and ambient light, with haze scaled by fog luminance. They no longer glow at night. |
| 3 / 19 | Landmarks | 6.5 / 6 | 8 / 7.5 | Hillcrest Castle rebuilt:<br>• a donjon with needle-spired turrets and a 58 m belfry (bell, clock, gold vane)<br>• a palazzo with a loggia<br>• hoarded towers and bannered walls<br>• a drawbridge gatehouse with a heraldic shield<br>• a cobbled courtyard<br>It reads from anywhere in the valley (`castle-2/c03`). |
| new | Zipline | – | 7.5 | The Zip Tower in the north-west wall hoists your car and sends it down a 160 m cable (22 s) to a lower-town station. It winches back up (27 s). The span was sited by `ZipLine.Survey`: at least 10 m clear under the car, with a clear station footprint. |
| 20 | Props (raceway) | 3 | 6.5 | Team paddock garages, a control tower, a timing pylon and a grandstand facade. |
| 30 | Car models | 7.5 | 8 | The tube "shut lines" are gone. Real panel gaps are cut into the body, and fenders bulge over the wheels. Round lamps sit in body-colour cowls (no cut edges). Arch trims are one piece or none. Side trims stop at the arches. |
| 36 | World labels | 6 | 7 | "Talk to x" prompt card instead of labels over cars. |
| 38 | Loading | 4.5 | 5.5 | The world builds as a coroutine behind a progress bar with step labels. |
| 39 | Graphics options | 5.5 | 6.5 | Quality preset, resolution, display mode and time of day. |

Bugs found along the way:
- **Garage drive check.** It had failed since the Pit Stop moved the garage bay: it drove at the old bay, and in rendered runs a plugged-in controller stayed `Gamepad.current`. It now uses the Pit Stop bay and disables other pads while it drives.
- **Kit spin axes.** The windmill and garage tyres spun flat; they now spin about a kit-space axle.

**Next:**
- Valley hedgerows as real geometry near the roads.
- Castle interiors (open the donjon door onto a hall).
- Night pass on the castle (floodlights).
- Contact AO for props.
- Water edges.

## A. Art direction
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 1 | Style coherence | 6 | "Toy shapes, real light" works in the towns (v08 square, v18 street). The countryside drops to flat lime terrain with lollipop trees, a different, lower-fidelity game. |
| 2 | Palette and colour balance | 5 | Town hues are graded and pleasant. Meadows are one saturated lime everywhere (v13, v20). The deep-blue panorama sky clashes with the pale, hazy ranges (v04). |
| 3 | Landmarks and identity | 6.5 | The gatehouse, castle, Bayview seafront, lighthouse and viaducts are memorable silhouettes (h3, v09). The castle mound is an empty green dome. |
| 4 | Scale and believability | 5 | Toy proportions are consistent. Believability breaks on the river in a straight walled canal (v11 right), fields laid like carpets on slopes, and very large empty meadows. |

## B. Lighting and atmosphere
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 5 | Sun and shadows | 6.5 | Warm low sun, 4 cascades at 4096, soft shadows to 240 m. Good forms in towns. Hills beyond shadow range go flat. |
| 6 | Ambient and indirect light | 4 | A trilight ambient with no bounce, light probes or GI. Arch interiors, eaves and shaded streets are uniformly flat. There's no warm bounce off sunlit walls. |
| 7 | Ambient occlusion and contact | 5 | SSAO is on at low sample count. Props and buildings sit on the ground without contact darkening; stalls and cars float slightly visually. |
| 8 | Reflections and specular | 4 | Realtime probes only in parts of Bayview, no SSR. Windows and car paint mostly reflect a generic sky. Wet or smooth surfaces don't exist. |
| 9 | Sky and clouds | 5 | A good HDR panorama with drifting cloud shadows. The painted clouds are static and don't match the moving shadows. The sky is one fixed late afternoon. |
| 10 | Aerial perspective and fog | 4.5 | Island fog (exp², 0.0014) is heavy; the mainland is thin. There's no height fog in the valleys or around water. The new mountain haze now overshoots into a pale icy band (v04, v13). |
| 11 | Time of day and weather variety | 2 | One fixed afternoon, no weather. Car lights and an evening review exist in code but aren't part of play. |

## C. Post-processing and image quality
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 12 | Tonemapping and grade | 6 | Neutral tonemapping with a gentle filmic grade reads well. It's slightly flat in the midtones. The Neutral mode leaves some richness on the table. |
| 13 | Anti-aliasing and image stability | 3.5 | SMAA only (no TAA, no MSAA). Pixel crawl on road shoulders (v04), moiré on roof tiles and paving (v08, v18), and shimmer on rails, bunting and fences in motion. This is the biggest "cheap-looking" cue in video. |
| 14 | Texture and pattern filtering | 4.5 | Procedural kit patterns (tiles, stone, paving) aren't filtered for distance, so they alias into noise at 30 m and beyond. |
| 15 | Bloom and lens | 5 | Subtle bloom, fine. No sun glare or lens flare looking into the low sun. |
| 16 | Speed and motion presentation | 3 | No motion blur, speed-dependent FOV, camera shake or speed lines. Races at 34 m/s feel like 15. |
| 17 | Frame pacing and output | 5 | 4.7–8.9 ms frames (plenty of headroom). Free roam is capped at 60 with vSync off, so it tears on high-refresh screens and runs no faster. |

## D. World assets
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 18 | Town buildings | 6.5 | Shutters, balconies, flower boxes, varied terraces (v18, f01). Some repetition along long streets. The kit shader's patterns carry them. |
| 19 | Landmark structures | 6 | Castle, gatehouse, bridges, lighthouse and windmill. The castle is blocky close up. The raceway grandstand is a plain white box. |
| 20 | Props and street furniture | 6 | Market stalls, bunting, lamps, bandstand and café sets are strong (v08). The raceway paddock garages are flat primary-coloured boxes (v11, about 3). |
| 21 | Trees | 5 | Kit trees are charming close up (m12). The countryside is sparse and repetitive, with scattered lollipops on bare grass (v20). No wind movement. |
| 22 | Ground cover and grass | 3 | Meadows are flat colour with isolated grass cards (v13). No density, no colour breakup at 5–50 m, no wildflower drifts. This is the single largest area of the screen. |
| 23 | Crops and farmland | 4 | Vineyards read as green discs on sticks, lavender as flat purple stripes (v13). The wheat field is good. |
| 24 | Terrain shape | 5.5 | Pleasant rolling hills. Grey slide patches look like missing texture (v11, v20). The river runs in a canal with vertical walls. |
| 25 | Terrain surface | 4 | One lime tone, faint visible tiling. Rock blends are streaky. There's no path wear, soil, scree or damp near water. |
| 26 | Rock and cliffs | 5.5 | The gorge and sea cliffs have form (09). The talus is repetitive. |
| 27 | Water | 5 | The animated sea and river are pleasant. Missing: depth colour gradient, shore foam, wet sand or banks, and contact foam on piers. The lake is a hard-edged disc. Harbour pier legs glow cyan underwater. |
| 28 | Distant scenery (mountains) | 4.5 | It was about 3 (grey cardboard wall). Now layered but over-hazed with no readable snow line. It's in nearly every valley frame. |
| 29 | Roads | 5 | Clean asphalt, good markings, junction dressing. No wear, patching, cracks, drains, tyre marks or kerb detail. Shoulders alias badly. |
| 30 | Car models | 7.5 | The strongest art: 16 characterful bodies with real detail (`review/cars/lineup.png`). |
| 31 | Car rendering in the world | 6 | Paint and shadows read well (01). No body roll or suspension visuals, and a generic reflection. |

## E. Motion, effects and life
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 32 | World motion and life | 5.5 | Turbines, balloons, birds, trains, traffic, water and drifting clouds. Trees, flags, bunting and grass (apart from the wheat) are frozen. |
| 33 | Vehicle effects | 4 | Dust and tyre smoke are improved. No skid marks, sparks, water splashes, exhaust or brake lights on braking. |
| 34 | Collectibles and pickups | 5 | New bobbing postcard and stamp models (were 3). There's no pickup flourish (burst, sound or stamp animation). |

## F. Presentation
| # | Aspect | Score | Evidence and what holds it back |
|---|---|---|---|
| 35 | HUD and UI style | 6 | Chunky pill HUD matches the toy style. The controls bar is heavy for a permanent overlay. |
| 36 | World labels | 3.5 | Resident name tags still float huge through buildings at chase range ("MARA / GARAG", 01), even after shrinking with distance. |
| 37 | Camera and framing | 5.5 | A sound chase camera. No speed response, no dynamic framing in corners, no photo-worthy angles in races. |
| 38 | Loading and transitions | 3 | Every race entry and return rebuilds the world: 7.8 s (new timing log), with no loading screen polish. |
| 39 | Graphics options | 2 | Only a fullscreen toggle. Steam players expect resolution, quality presets, vSync, frame cap, AA, shadows and FOV. |

## What moves the overall score most (proposed order)
Each is ranked by screen share multiplied by the gap to 7:
1. **Image stability:** TAA or SMAA-high plus distance-filtered kit patterns (13, 14, 29). Every frame, and especially in motion.
2. **Ground cover and terrain colour:** grass density and colour breakup, flower drifts, meadow tone variation, and real textures in the slide patches (22, 25, 2). This is the largest area on screen.
3. **Distant ranges and atmosphere:** tune the haze to a real snow line and rock colour, add valley height fog, and match the sky (28, 10, 2).
4. **Speed presentation:** speed FOV, subtle motion blur in races, and camera response (16, 37).
5. **Vegetation life:** wind sway on trees, flags and bunting, and denser, varied countryside planting (21, 32).
6. **Water edges:** depth colour, shore foam, banks and a natural river channel (27, 24).
7. **Frame pacing and options:** vSync or refresh-matched cap and a graphics menu (17, 39).
8. **Indirect light:** light probes or bounce plus contact AO for props (6, 7).
9. **Vehicle effects:** skid marks, brake lights and sparks (33).
10. **The rest:** loading (38), labels (36), raceway paddock and grandstand (20, 19), time of day (11).

## Changes already made on 28 September (in this build)
- **Terrain and foliage shadow and depth passes.** Both shaders now have their own passes, matching Kit. Frame cost is unchanged (p5-before vs p5-after, within 0.1 ms), so it's a tidy-up, not a speed-up.
- **Distant ranges.** Rows were refined 3× (the streaks are gone), gullies and ridges are shaded, and haze was added. The haze now needs pulling back (item 3).
- **Resident labels** shrink with distance. That wasn't enough (item 36).
- **Collectibles** are now postcard and stamp models.
