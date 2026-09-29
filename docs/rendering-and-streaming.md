# Rendering and streaming: what draws, what it costs, and how the world grows 10×

Written 28 September 2026 after Richard noticed slow-downs while driving.

The target is a world at least ten times today's area, about 2.5 km × 2.3 km now.

## How to measure

`.\tools\drive-benchmark.ps1 -Name <run> [-Flags ...]` drives the autopilot route at 1920×1080 with moving traffic and residents off (`--no-valley-life`: they blocked the autopilot on the castle climb):
- Bayview junction.
- The causeway.
- The Headland Tunnel.
- Hillcrest.
- Up Via Crest to the castle.

The car is stock with the Precision steering rack, capped at 20 m/s.

`FrameProbe` records every frame to `review/map-sweep/<run>/frame-probe.txt` and `frames.csv`:
- time;
- SetPass calls;
- triangles;
- shadow casters;
- managed allocation;
- position.

The summary covers percentiles, the worst frames and the cost by 100 m cell.

The test screen runs at 144 Hz, so frame times come in 6.94 ms steps.

A `-Flags '--no-…'` switch A/Bs a change in the same build:
- `--no-distance-cull`;
- `--no-warm-up`.

## Where the frame goes (db3, before and after distance culling)

| Measure | Culling off (db3-nocull) | Culling on (db3-cull) |
|---|---|---|
| Mean / p95 / p99 frame | 7.43 / 8.80 / 13.9 ms | 7.09 / 7.45 / 11.9 ms |
| Frames over 33 ms | 29 (11 at the gate jam) | 18 |
| Triangles per frame (mean / max) | 26.4M / 34.4M | 25.1M / 34.3M |
| Shadow casters (mean) | 10,900 | 10,000 |
| SetPass calls (mean) | 283 | 278 |
| Managed allocation | 0 | 0 |

What this says:
- **Throughput is fine on this GPU (RTX 4070 Ti Super) but heavy.** 25 million triangles a frame is mostly not props:
  - terrain chunks at LOD0 near the camera;
  - the woods (5,400 trees);
  - instanced grass;
  - cars;
  - every one of these again in each of the 4 shadow cascades.

  Distance culling of props only shaved 5%.
- **The slow-downs were hitches, not throughput:**
  - **First sight.** 35–55 ms frames came at the start and on first sight of the valley after the tunnel, where shaders and pipeline states were prepared on first draw. `AdventureGame.WarmUp` now shows the camera the main views behind the loading card.
  - **Reflection probes.** With the day cycle on, every 2° of sun re-rendered a 520 m reflection probe with a 900 m far clip: the whole world, six more times, every ~7 s. That is now every 4°, only for probes within 150 m, with small props left out and a 700 m far clip.
  - **Traffic jam.** 70–118 ms frames were a physics jam when valley traffic turned round in the castle gateway. That traffic now turns 45 m short.
- **No garbage-collection pressure.** Nothing allocates per frame.

## What is in place now

- **Frustum culling** (Unity): nothing outside the camera's view is drawn.
- **LODs:**
  - terrain chunks (3 levels);
  - cars (3 levels);
  - trees and bushes, which swap to far stand-ins (`KitLod`).
- **Distance culling by size.** `BayviewKit.CullLayer` sorts every kit piece:
  - under 3.5 m: stops drawing (and casting shadows) at 280 m;
  - under 7 m: stops at 700 m;
  - bigger: always drawn.

  Pieces with their own LOD are exempt.
- **Grass** is drawn only within 44 m of the camera.
- **Screen UI** is drawn as an overlay. There is no camera stack, so TAA is really on.

## Next steps, in order

1. **Shadow budget (biggest single cost).**
   - Small casters stop casting beyond ~60 m.
   - Cascade 4 carries only terrain and buildings.
   - Check whether 3 cascades at 4096 look the same as 4.
   - Measure with db runs.
2. **Terrain and woods detail.**
   - Chunk LOD0 only within ~150 m.
   - Tree stand-in switch distance tuned by screen size.
   - Grass density scaled by the quality preset.
3. **GPU occlusion culling (hide what hills and buildings cover).** Unity 6's GPU Resident Drawer with GPU occlusion culling does exactly this, and it suits a world built at runtime.
   - It needs our custom shaders (Kit, CoastalTerrain, Distant, Wheat, Grass) to support DOTS instancing (`DOTS_INSTANCING_ON` and per-material property metadata).
   - Renderers with other shaders fall back to today's path, so it can be switched on shader by shader.
   - Expected gain is largest in the towns and behind the headland.
4. **Streaming for a 10× world.** Building everything at load won't scale: today's world already takes ~7 s and holds everything in memory.
   - **Sectors.** Split the world into 256 m sectors. Each is built by its generator from a fixed seed (they already are deterministic), when the camera comes within ~1.2 km, and torn down beyond ~1.6 km.
   - **Far view.** Beyond that, each sector shows one merged low-detail proxy per material (HLOD), baked when the sector is first built. The distant mountain ring stays as it is.
   - **Registry.** `BayviewKit.Spawn` already routes almost all placement through one place, so sector ownership can be added there. The same goes for the footprint registry parked cars now use.
   - **Races.** Races build only their circuit's sectors.
   - **Traffic.** Traffic and residents live in active sectors only.

## Results after the fixes (db5 to db7)

**db5** (same route, no traffic):
- mean 7.0 ms, p99 7.0 ms;
- one frame over 16.7 ms and none over 33 ms;
- the frame rate holds at the display's 144 Hz for the whole drive.

**db7** added the rest of the look plan (rim and bounce light, body motion, cross-fades, the terraced town hill and the forested mountain bases):
- p95 8.4 ms, p99 9.7 ms.
- Its six ~150 ms frames fall exactly every 20 s. They are the benchmark's own drive stills (`drive-*.png`), not play.
