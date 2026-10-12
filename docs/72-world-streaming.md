# 72 · World streaming and load times

Richard, 8 October: "We need to consider that the map is going to grow significantly as per the Little Road World Blueprint, so loading the whole map every time is not going to be an acceptable route for both initial adventure mode, and also races. So we will need to research how games normally handle this and then bake that into the game. The load times are fairly mad for how small the game is and the graphics choice etc at the moment."

## 1 · Where the time goes today

Measured 8 October on the dev build (`--load-timing --quarry-hills`, windowed, RTX 4070 Ti Super):

| Stage | Time |
|---|---|
| Building the world (twenty generator steps) | 12.9 s |
| Shader warm-up | 1.8 s |
| **Ready to drive** | **~17 s from launch** (~22 s on the first run after a new build: the map image is re-baked, +4.8 s) |
| Woods streaming in while you drive (`WorldTiles`) | 6.8 s |
| **Entering a race** | **~11.7 s: the whole world is built again from scratch** |

The biggest generator steps at start-up:

| Step | Time |
|---|---|
| Mainland terrain | 2.2 s |
| Valley roads and water | 2.0 s |
| Land | 1.4 s |
| Cliffs | 1.3 s |
| Hillcrest town | 1.3 s |
| Woods near the start | 1.1 s |
| Quarry Hills town | 1.1 s |
| Valley height field (3.2 million cells) | 0.9 s |
| Settling the planting | 0.7 s |

Every one of them produces exactly the same result each time for the same build and flags.

**Scale:** today's world is about 6 km across. The blueprint's continent is 20 × 20 km, about 11 times the area. At today's approach, start-up would pass two minutes, and every race would cost about the same again.

## 2 · How open-world games handle it

| Technique | Who | What it means here |
|---|---|---|
| **Grid cells streamed by distance** | Unreal's World Partition keeps the world as one level cut into grid cells, loading the cells around the player (a "streaming source") and unloading the rest. Spider-Man's New York is about 800 tiles of 128 m; at full speed it loads about one new tile a second. Unity's entity scenes stream "sections" with their own bounds. | The world is a grid of cells; only the cells near the car exist. |
| **Load and unload radii (hysteresis)** | Cells load inside one radius and unload only outside a wider one, so they don't flicker at the boundary. | Two radii per layer. |
| **Priorities and throttling** | Ground first, then props; only a few loads at once; delayed activation so no single frame takes the hit. | Terrain and roads before buildings, buildings before decoration; a per-frame time budget (the woods already stream at 2.5 ms a frame). |
| **HLOD (hierarchical level of detail)** | An unloaded cell is drawn as one merged low-detail mesh and material, built from the lowest LODs. That keeps the horizon full and the draw calls low. | Each cell bakes a cheap proxy (terrain silhouette, building blocks, tree masses) that stands in beyond the load radius. |
| **Cook once, load forever** | Shipped games cook their data offline. Procedural games (Minecraft) generate a chunk once from the seed and its coordinates, save it, and load the saved copy afterwards (region files of 32 × 32 chunks). | Our generators are deterministic, so their output can be saved per cell and loaded instead of regenerated. Generation only happens when a cell's inputs change. |
| **Races in the live world** | Forza Horizon's events start in the open world, with no separate race map to load. | Entering a race keeps the world and just lines up the grid. |
| **Async and off the main thread** | Disk reads and decoding happen on worker threads; the main thread only creates the final objects. | Reading cell files and building mesh data happen on workers; Unity objects are made on the main thread in small slices. |

## 3 · How Little Road builds its world (code map, 8 October)

- **One scene, rebuilt on every switch.** A race is not its own scene:
  - `AdventureSession.StartRace` and `ReturnToTown` both call `LoadingScreen.Load`, which does a single-mode `SceneManager.LoadScene` of the same scene.
  - Everything not marked `DontDestroyOnLoad` is destroyed, and `TrialWorld.CreateRoutine` regenerates the world from scratch.
- **The heavy work is pure and repeatable:**
  - `ValleyField` (3.2 million cells, two 13 MB arrays) logs the same checksum on every load.
  - The mainland is 506 terrain chunks of 160 m, each with three LOD meshes and a collider: about 4.6 million triangles at LOD0. Races build every one of them; `RaceArea` only drops towns, trees and the island's extras.
- **Some steps need physics or registries.**
  - The island's planting, the cliffs' scree and road heights raycast against colliders built earlier.
  - The towns fill registries that later steps read: footprints, reserved spots, `Spots`, `Homes`, the road `Network`.
- **What streams today:** only the woods (`WorldTiles`, 250 m tiles, nearest first at 2.5 ms a frame). The map image is cached on disk per build. The terrain chunks are built in parallel, but all of them, every time.

## 4 · The plan

**Targets:**
- entering a race or returning to town: **under 2 s**;
- starting the game: **under 6 s**;
- both staying flat as the map grows to the blueprint's 20 × 20 km.

### Phase 1 · One world, kept across races (the biggest win, lowest risk)

The built world survives the scene reload, the way Forza Horizon's races start in the open world:
1. **A persistent root.** The landscape root (terrain, roads, towns, woods, the woods streamer) is marked `DontDestroyOnLoad` and tagged with a **world key**: the build plus the flags that change the world.
2. **Reuse it.** On the next load with the same key, `CreateRoutine` re-applies only the per-scene state: lighting, fog, sun, colour grade (`RenderSettings` resets with the scene). It reuses the world, and the registries filled when it was built stay valid, because nothing clears them.
3. **The same world for races.**
   - Races run on the free-roam world. Hillcrest and Quarry Hills races already use almost the same world.
   - Bayview's race-only dressing (kerbs, gantry, chequer) becomes an overlay added for the race and removed after.
   - `RaceArea` stops deleting scenery and becomes the streaming focus.
4. **Studies are unaffected.** Keep/reject studies and tests launch a fresh process, so they still build from scratch. `--fresh-world` forces a rebuild for A/B comparison.

**Expected:** race entry and return from ~11.7 s to ~1–2 s.

### Phase 2 · Build near the car first, stream the rest (start-up, and scale)

`WorldTiles` is extended from the woods to everything, on the same 250 m grid:
1. **Decide everything, build near the car.** Builders keep making all their decisions at load: the same random draws in the same order, so the registries and footprints stay right. Only creating the objects is deferred to their tile, as the woods already are. Towns, roads and dressing join.
2. **Terrain chunks by distance.**
   - Chunks within ~600 m are built at load.
   - The rest are built nearest first by the streamer, on worker threads, using a per-frame time budget.
   - Far away, each chunk shows its cheap LOD2 mesh first (the HLOD idea: the horizon is never empty), and swaps up as the car approaches.
3. **Two radii.** Detail loads inside one radius and unloads only outside a wider one, so a cell doesn't flicker at the boundary. Memory then stays flat however big the map gets.

**Expected:** start-up from ~17 s to ~5–6 s, and it stays there as the map grows.

### Phase 3 · Cook once, load forever (the pure data)

The pure, repeatable data is saved to disk, keyed by build and flags like the map cache, and read back instead of regenerated:
- the height field;
- the terrain chunks' vertices;
- the macro tints;
- the island and mountain meshes.

Regeneration only happens after a change. This takes the remaining computation off the critical path, and the cache is per cell, like Minecraft's regions, so it scales with the map.

**Expected:** a further 2–4 s off a cold start, and cheaper streaming.

### Phase 4 · For the full continent (later)

- **Floating origin**, since 20 km strains float precision at the edges.
- **Data-driven regions** that the blueprint's places plug into.
- **Per-region generators** that only run when their cells are wanted.

### Measuring

Every phase is measured with `--load-timing` (start-up) and the race-entry `LITTLE_ROAD_LOAD_STEP` lines, before and after on the same build. The checks:
- **Phase 1:** race-entry seconds; a race then back to town, three times over, without leaks or lost registries; 24/24 races unchanged.
- **Phase 2:** start-up seconds; frame time while streaming (the drive benchmark's p99 must stay inside doc 57's budget); nothing visible pops in within 300 m.

## Sources

- Unreal World Partition: https://dev.epicgames.com/documentation/en-us/unreal-engine/world-partition-in-unreal-engine
- World Partition HLOD: https://dev.epicgames.com/documentation/en-us/unreal-engine/world-partition---hierarchical-level-of-detail-in-unreal-engine
- Streaming and HLODs in UEFN: https://dev.epicgames.com/documentation/en-us/uefn/streaming-and-hlods-in-unreal-editor-for-fortnite
- Unity entity scene streaming (sections, metadata, async): https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/streaming-scene-sections.html
- Meta's Unity open-world streaming sample (grid subscenes, three-state lifecycle, throttling): https://developers.meta.com/horizon/documentation/unity/unity-sample-asset-streaming
- Smooth scene streaming in Unity (delayed activation): https://80.lv/articles/smooth-scene-streaming-with-unity3d
- Insomniac on Spider-Man's tiles (128 m, about 800 tiles, about one a second): https://blog.playstation.com/?p=204132
- Spider-Man technical postmortem (GDC 2019): https://gdcvault.com/play/1026496
- Minecraft region files (chunks generated once, saved and reloaded): https://minecraft.fandom.com/wiki/Region_file_format
- Forza Horizon 2, joining events with no loading: https://mp1st.com/features/forza-horizon-2-preview

## 5 · Progress (8 October)

### Phase 1 — one world, kept across races: done

`PersistentWorld` keeps the free-roam world across scene reloads:
- Races reuse it. Bayview's kerbs, chequer and gantry are a per-race overlay.
- On race entry, the world's pieces away from the circuit are switched off (as the race build used to omit them), and they're switched back on in town.
- The registries drop the entries of pieces that went with the old scene: footprints now record their owner (`BayviewKit.FootprintOwners`).
- `--fresh-world` rebuilds every time, for A/B comparison.

Measured on the same build, windowed and muted:

| Switch | Before (rebuilt) | Kept world |
|---|---|---|
| Entering Coast Tuners | 8.1–8.5 s | 0.8–1.0 s |
| Entering Valley Tuners | 9.3–9.4 s | 1.2–1.4 s |
| Back to town | 14.7–15.0 s | 1.1–1.5 s |

Race frame cost at Valley Tuners:

| | Mean frame | p95 frame |
|---|---|---|
| Old race world | 8.4 ms | 8.3 ms |
| Kept world, unfocused | 9.9 ms | 14.4 ms |
| Kept world, focused | 8.4 ms | 8.5 ms |

Four round trips ran with no errors, and the registries stayed steady at 26,087 entries. Supporting changes:
- **Timing:** `LITTLE_ROAD_SCENE_SWITCH` lines time every switch.
- **Round trips:** `--review-races N` runs N round trips.
- **Short races:** `--race-laps N` shortens any race for tests (Richard: shorter races for quicker feedback).

### Start-up — profiled and cut (Phase 2 and 3 groundwork)

Same machine, warm caches, `--load-timing`:

| Step | Before | Now | What changed |
|---|---|---|---|
| Island land | 1.35 s | 0.32 s | Height rows and the collider clip run in parallel |
| Mainland | 2.19 s | 0.71 s | The height field is cooked to disk (`WorldCache`, 16 ms read, same checksum); the macro tint, the chunk choice and the Quarry Hills tint (cached) run in parallel |
| Cliffs | 1.31 s | 0.37 s | The 45 outcrop shapes are worked out in parallel, then built in order |
| Valley roads and water | 1.95 s | 0.55 s | Exact bounding-box tree for every long polyline distance (`HillcrestRoute.Distance`); padded-box fast paths for the river and road-crossing tests |
| Hillcrest town | 1.28 s | 0.70 s | The same polyline index |
| Woods | 1.10 s | 0.36 s | The same polyline index |
| Quarry Hills town | 1.10 s | 0.68 s | The same polyline index |
| Settle (rail trimming) | 0.68 s | 0.17 s | Footprints in a 16 m grid for the rail and corner-wall trim (0.52 s → 4 ms) |
| **World build** | **12.9 s** | **5.6 s** | |
| **Ready to drive** | **~17 s** (22 s with a cold map cache) | **9.8 s** | |

**Same world:** every logged count is unchanged:
- 731 copses;
- 305 Hillcrest buildings and 2,238 footings;
- 1,529 Quarry Hills buildings;
- 208 trimmed rails;
- 40 cliff formations and 20 scree blocks;
- the height field's checksum (23188135.278).

**Checks:** `WorldStreamingChecks` compares the new spatial indexes against the full scans they replace:
- the footprint grid on 2,000 random queries;
- the polyline index on 2,400 queries over six long lines, exact distance and height.

**Also:**
- `BayviewKit.Occupied` now uses a 32 m footprint grid. It wasn't the bottleneck today, but it keeps the towns' placement from slowing as the map grows.
- Warm-up runs uncapped (vsync off for its frames, 0.1 s).
- Two experiments didn't help and were dropped: half-resolution warm-up, and a first grid version of the polyline index, which made far-away queries far slower (122 s); it was replaced by the tree.

**Left in start-up** (~9.8 s):
- Unity's own start, about 2.1 s before our code runs;
- shader warm-up, about 1.7 s;
- creating about 10,000 kit pieces, about 1.2 s;
- terrain chunk meshes, about 0.5 s;
- the remaining builders, about 3 s.

The next structural step is phase 2 proper: build the districts away from the start in the background (decide now, build later), then instancing for the kit pieces.

### Paused 8 October (evening)

**Full regression** (sr-streaming-1), everything passes:
- both smoke tests, lessons, climb, sprint, carvings, Blondin, wayfinding, world tiles, blast, leap, festival, train and sound;
- the farm tests show their known autopilot shortfall;
- the area audit is the same as sr16, apart from one new item.

**The new audit item:** resident Emyr's parked car in Quarry Hills (−2894, −2401) has one wheel 0.21 m off the ground. sr16 predates the class B commit as well as today's work, so the cause isn't known yet. Next: re-run `--area-audit` to see whether it repeats, then look at that spot.

**Unity splash screen off** (`m_ShowUnitySplashScreen: 0`; Unity 6 allows this on Personal). Edited but not yet built or measured. Expected about 2 s off start-up.

**Next avenues:** see section 4's phase 2 (background districts) and the list above. Nothing from 8 October's streaming work is committed (last commit edc5077).

## 6 · Progress (9 October)

**Ready to drive: 9.8 s → 5.5 s.** Race entry is now 0.7–0.9 s and the return to town 1.0–1.1 s (`switchtime`, two round trips, Bayview coast race). The world is unchanged, and `--world-fingerprint` checks that directly. It hashes every piece's name, position, rotation and scale, plus its renderers (layer, material name, shader, colour, pattern), colliders and LODs. The hash is 1284ec0b6b27644d before and after every change below.

| Change | Saved | Notes |
|---|---|---|
| Unity splash screen off (`m_ShowUnitySplashScreen: 0`) | 2.5 s | Unity's pre-roll fell from 2.66 s to 0.18 s. Unity 6 allows this on every plan, Personal included, so a release can ship without the splash. |
| Map cache keyed by build and world flags only | ~1 s on test runs | The map was re-baked whenever any test flag changed. |
| Kit templates (`BayviewKit.TemplateFor`) | 0.35 s | Each kit piece is prepared once as an inactive prototype: tint materials converted, collision boxes added. Each placement then instantiates it. `--no-kit-templates` uses the old path. |
| Material key and palette memo | 0.65 s | Kit materials are cached by stable properties (name, rounded colour, pattern, aged), not by object identity. `WorldLook.PatternFor` is memoised. |
| Mainland chunks | 0.06 s | The collider is now cut from LOD 0's grid instead of computed twice. Chunks away from the tunnel skip the portal test (it ran on 4.6 M triangles). Bounds are worked out once, off the main thread. |
| Racing-line bake cached (`RacingLine.BakeSmooth`, `WorldCache`) | 0.3 s | 24,000 smoothing iterations for the Hillcrest Raceway's rubbered line now take 15 ms when cached. Hillcrest and Quarry race entry use the same bake. Keyed by the road's control points. |
| **Music and ambience loops: compressed in memory, loaded in the background** | **0.7 s** | See below. |

**Determinism fixes found on the way.** `Physics.RaycastAll` returns hits in no fixed order. Three places took the first hit and so depended on it:
- `GalleryClimb.Down` and both `RoadTop` functions now take the highest hit;
- `TipLeap.Clearance` now takes the nearest.

The fingerprint caught it: flag heights changed with templates on, because object creation order changed.

**Emyr's car** (the one new area-audit item on 8 October): a re-run (aa-recheck-1) was clean, so it was a one-off.

**Warm-up trim tried and dropped.** Running 40 frames instead of 64 saved only 0.14 s. The full counts stay, to guard against first-sight hitches.

### Finding the audio stall: `LoopProbe`

The first warm-up frame after loading took 730 ms, even with every camera off, no garbage collection and no physics cost. `LoopProbe` (`--warm-probe`) answers questions like that in any build, development or not:
- it puts a timestamp before every step of Unity's player loop;
- it logs the steps over 3 ms for each loading frame and the first warm-up frames, with the load steps marked in line.

It showed **427 ms in PostLateUpdate's delayed-call pass**, in the frame where the player's car got its audio. With `--no-audio` that frame was clean.

**The cause:** the 11 music and ambience WAVs (5–7.7 MB each) were imported as **Decompress On Load**, with preload and background loading off. So the first `Play()` of each decoded the whole Vorbis clip on the main thread. Races paid it too, with the race theme and its lift stem.

**The fix:** they're now **Compressed In Memory**, with **Load In Background** on. This is the standard setting for long loops and music; Unity decodes them as they play, on the audio thread.
- `--audio-probe` now reports each bed's and the music's load state and play position. All were loaded and advancing within 3 s.
- The music already re-syncs its lift stem to the base loop every frame, so a stem that finishes loading later lines up.
- `WorldStreamingChecks` fails the build if any loop goes back to a main-thread decode.

### Where start-up goes now (5.46 s, warm caches)

| Stage | Time |
|---|---|
| Unity start, before our code | 0.18 s |
| World build | 3.67 s |
| Town setup (places, wayfinding, player, valley life, camera) | 0.35 s |
| Shader warm-up (64 frames over 8 views; the first frame is 261 ms) | 1.13 s |

The world build, by step (ms):

| Step | ms |
|---|---|
| Mainland terrain (geometry 285, meshes 121, colliders 79) | 700 |
| Hillcrest town (3,893 kit pieces) | 403 |
| Cliffs (shapes 315) | 388 |
| Quarry Hills | 387 |
| Land (island) | 321 |
| Quarry Hills town | 289 |
| Valley roads and water | 262 |
| Mainland trees | 231 |
| Settle | 161 |

**Test tool:** `--review-retire S` (with `--review-races N`). Each race runs for S seconds, then leaves through the pause menu's "Retire to town" (`RetireRace`), as a player would. Round trips then time entry and exit without waiting for a lap.

**First run (s21):** one round trip, 0.9 s in and 1.1 s out. It then stopped in town. The town only enters the next review race when `settledSerial==raceSerial`, and a retired race never settles. To fix when work resumes: settle the serial on retire in review runs, or skip that test with `--review-retire`.

### Paused 9 October: next steps, in order

1. **Background districts (phase 2 proper).**
   - Build Quarry Hills (town, country and quarry; about 0.75 s) after the car is drivable.
   - Time-slice it across frames, with a budget of a few ms per frame, behind the loading card's "ready".
   - First, list what reads those districts during set-up: the map bake, the residents list, the train network, BlastRun and the festival. Each must wait for the district or tolerate its absence.
   - The pattern then scales to every new district from the World Blueprint. Behind a flag, with the fingerprint compared after the background build completes.
2. **Mainland terrain meshes through `Mesh.AllocateWritableMeshData`.**
   - Fill the vertex and index buffers in parallel jobs, then apply all 2,000 meshes in one call.
   - Meshes cost 121 ms on the main thread and geometry 285 ms, which is mostly List growth: pre-sized arrays would roughly halve it.
   - Same meshes; checked with the fingerprint.
3. **Cliff shapes** (315 ms, already parallel):
   - per-vertex `List<Vector3>` adjacency allocations contend in the GC;
   - flat arrays should cut it to about 100 ms.
4. **Kit pieces** (about 10,000, about 0.3 s spent in `BayviewKit`):
   - GPU instancing, or `Graphics.RenderMeshInstanced` for repeated scenery (trees, rocks, fence posts), so they are no longer separate GameObjects;
   - it helps both load time and frame cost.
5. **Shader warm-up** (1.13 s):
   - record a `GraphicsStateCollection` during a normal drive;
   - warm exactly those variants instead of rendering 64 frames over 8 views;
   - check with the drive benchmark (frames over 33 ms) that no first-sight hitches come back.
6. **Race entry** (0.7–0.9 s): profile it with `LoopProbe` the same way. The 24 cars' engine banks (`Resources.Load` plus `GrainBank.Read` for each car) are the likely next item, and could be shared or cached per engine.
7. **Later:**
   - floating origin for the full continent;
   - HLOD (merged far meshes per district) once districts stream.

**Before committing:** a full regression, which is a landmark. A sound check, too: listen once in town and in a race, since the music and ambience loading changed.

## 7 · Progress (12 October): background districts, clean-up

### Background districts (phase 2): Quarry Hills

Quarry Hills is now built after the town is ready, a few milliseconds a frame, whenever the car starts more than 1.5 km from it. It was behind `--background-districts` at first; Richard made it the default on 12 October, after a full regression. `--no-background-districts` builds everything at load, for comparisons.

**Results:**
- **Ready to drive:** 4.3 s, against 5.3 s with the district built at load.
- **District build:** it arrives about 3 s later, over about 90 frames. The busiest frame is about 40 ms, and only one or two reach that; most stay within the 3 ms budget.

**How it works (`Districts`):**
- **Builder:** Quarry Hills' builders are coroutines. At load they run straight through (`TrialWorld.RunNow`), so the normal build is unchanged. In the background they yield between pieces of work: each house, street, farm and part of the scenery.
- **Slate geometry:** computed on a worker thread while the game runs.
- **Settle pass:** the district then gets its own settle, as the world's pass gave it at load:
  - rails and corner walls near it are trimmed (`TrimRails` can now run region by region);
  - its planting is settled and standing cars seated;
  - it is static-batched as one batch.
- **What waits for it (`Districts.WhenBuilt`)** is set up one piece a frame:
  - the Quarry Hills residents;
  - its notice board and fingerposts (other junctions get theirs at load);
  - the festival, Tip Leap and Blast Run;
  - its townsfolk, parked cars and chimney smoke. These carry on the same random draws as at load, so they come out the same;
  - the Quarry Road speed trap.
- **Tile streamer:** it leaves the district's tiles alone until the district has arrived. At load the town is set up before any tree streams, and placing townsfolk around a built tree gives a different answer from placing them around a reserved spot. That explained Emyr's car, the 8 October audit item.
- **Finishing at once:** anything that needs the whole world calls `WorldTiles.Complete()`, which now finishes the district first. That covers tests, audits, the map bake on a cache miss and the fingerprint. A scene load (`PersistentWorld.TryReuse`) finishes it too and drops the old scene's set-up. Leaving for a race within those first 3 s takes that path. Its code is short, but no run has exercised it yet: every test mode finishes the district at once.

**Checks:**
- **World fingerprint (`--world-fingerprint`), now much stronger:**
  - it hashes every renderer and collider under the world's root (bounds and materials), not only kit pieces;
  - it sorts its lines, so build order doesn't matter;
  - it is taken straight after the world build, with every streamed tile built and a frame for deferred destroys;
  - moving things (sheep, spinners, balloons, idling cars, physics bodies) count by name only;
  - `--no-static-batching` takes batching out of the comparison, since batched renderers report their batch's bounds.

  Built at load and built in the background give the same hash: `b8809b600b793ca4` (unbatched).
- **Town print (`--town-print`):** a hash of everyone standing in the town, the boards and fingerposts, the speed-trap cameras and the activities.
  - Background and at-load builds match, apart from cars nudged around moving traffic.
  - Those also differ between two at-load runs (Camper Jo, Eleri, Huw), so that's existing behaviour, not the change.
- **`WorldStreamingChecks`** covers the district mechanics (order, holding the ground, answering at once once built) and cache pruning.

### Slate surface and others

- **Worked-slate surface:** its geometry is computed on every core, then made in order (279 to 62 ms). It is byte-identical: the dump compares equal to the old code's.
- **Racing-line bake and audio loops:** as in section 6.

### Clean-up: why files piled up, and what stops it now

- **Per-tick race telemetry** (about 75 MB a race; nothing reads it): the game now writes it only with `--telemetry`, which the race tools' `-KeepTelemetry` passes.
- **World and map caches** (about 45 MB a build): keyed to the build, because a new build may build a different world. Now only the two most recently used builds keep their files (a read counts as a use; the play copy and the dev build share the folder). The game prunes once per launch.
- **Test evidence:** `tools/cleanup.ps1` now also covers:
  - race-report folders older than 3 days, beyond the newest 6 in each folder;
  - `review/tmp` older than a day;
  - Blender `.blend1` backups;
  - two capture runs kept per series instead of three.

  It runs after every build. 12 October's pass moved 1.6 GB to the Recycle Bin.

### Next

- The same pattern for Hillcrest's far parts and every new district from the World Blueprint.
- The later list in section 6: MeshDataArray terrain, cliff arrays, instancing, the GraphicsStateCollection warm-up, race-entry engine banks, floating origin and HLOD.

### Regression and commit (12 October)

Full regression sr-districts-1, with background districts on by default:
- **Passed:** smoke tests (with and without Quarry Hills), lesson, climb, sprint, carvings, Blondin, wayfinding (both), world tiles, blast, leap, festival and sound.
- **Farm tests:** they show their known autopilot shortfall.
- **Area audit:** the same as 8 October, minus Emyr's car (fixed).
- **Train race:** it lost once, with the test car stuck behind valley traffic at (392, −929). Two reruns won (87.9 s and 92.2 s).

Richard (12 Oct): drive tests run without traffic until the traffic can overtake. `--no-traffic` leaves out the moving traffic (parked cars and townsfolk stay). The regression's train, sprint and blast tests use it, and all three pass (train won in 83.2 s).
