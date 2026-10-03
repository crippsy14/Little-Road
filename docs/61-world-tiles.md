# 61 · World tiles and load time (W1, first pass; 3 October, late)

Richard asked for the world tiles (doc 58, W1) and then the remaining area-audit findings. This doc covers what was measured, what was built, the results, and what W1 still needs.

## What the load spends (Quarry Hills on, windowed, before)

Free roam took 18.6 s to load:

| Stage | Seconds | Notes |
|---|---|---|
| Building the world | 12.9 | Terrain, cliffs, roads, towns, woods. Instantiating kit pieces was only about 2.5 s of it. |
| Shader warm-up | 1.5 | The camera shows the world's main views. |
| Baking the map | 3.1 | A 4096-pixel render of the whole world, redone on every load. |
| The rest | 0.4 | Places, wayfinding, life, the HUD. |

Every race entry and every return to free roam rebuilds the world, so this cost repeats often.

`--load-timing` reproduces the breakdown. It logs `LITTLE_ROAD_LOAD_STEP` per build step, including its kit time and count, and `LITTLE_ROAD_LOAD_MARK` per start-up stage, then quits.

## What was built

**The map, baked once per build (`WorldMap.Bake`).**
- The baked map is compressed (DXT1, 8 MB) and kept under `persistentDataPath/map-cache`.
- It's keyed on the build, the map size, the quality level and the world flags.
- Later loads read it back in about 6 ms.
- The four newest files are kept, because the play copy and the dev build each have their own.
- `--no-map-cache` turns it off.

**World tiles (`WorldTiles`).**
- **Grid:** the world sits on a 250 m grid.
- **Decisions:** the builders still decide everything at load, with the same random draws in the same order.
- **Streaming:** the woods away from the start are queued on their tiles, about 19,000 trees, bushes and tufts. These are the mainland woods and Quarry Hills' woods.
- **Background building:** a background builder puts them up nearest-tile-first at about 2.5 ms a frame. Everything is in place 5 to 7 s after arriving.
- **Near the start:** within 450 m of where you start, it's built at once.
- **Reservations:** queued spots are reserved, so notice boards, fingerposts, festival flags and parked cars avoid them (`WorldTiles.Blocked`).
- **Settling:** each streamed piece settles onto the ground as it's built, as the world's own settle pass does.
- **Tests, audits and the map bake:** they call `Complete()` first and see the whole world.
- **Races:** they build no woods, so they never defer.
- `--no-world-tiles` builds everything at load, for A/B comparison.

**Parallel terrain (phase four, kept).** The terrain chunks, collider baking and valley corridors run on every core.

## Results (same build)

| | Before | After |
|---|---|---|
| Free-roam load, warm map cache | 18.6 s | 13.5 s |
| Free-roam load, first after a new build | 18.6 s | 18.5 s (bakes and saves the map) |
| World build step | 12.9 s | 11.5 s |
| Pieces built (all-at-once vs at load + streamed) | 26,566 | 7,324 + 19,242 = 26,566 |
| Frame cost, woods / Quarry Hills woods / valley | 4.6 / 5.5 / 7.9 ms | 4.5 / 5.2 / 8.4 ms |

`--world-tiles-test` passes: 19,242 pieces streamed in 5.4 s with the car standing. The editor check covers queueing, reservation, completion and races never waiting.

## The Quarry Hills audit findings

**Before:** 6 lips, 3 seams and 1 steep bank. All of them sat at the four back-street junctions.

**The cause:** Chapel Street and Terrace Row climbed at their full 10% right up to the tee. So they reached the junction 1.1–1.3 m above or below the through road, and the junction surface tilted to meet them.

**The fixes:**
- **Landings.** The back streets now leave and meet their through roads on a 20 m landing at 1%, then climb (`QuarryHills.BackStreetLanding`). They now meet within 0.2–0.4 m.
- **1 m ribbon segments.** Quarry Hills' street ribbons are cut into 1 m segments. Terrace Row's 2 m quad across an 18° bend folded into a 10 cm crease.
- **Retaining walls.** Slate walls stand along the top of drops beside the town streets, where the ground falls more than 1.2 m into the gardens (`QuarryHillsTown.RetainingWalls`). Each run carries on 1.5 m past its ends, so short ones don't look like stubs.
- **Guardrails over a lower road.** A guardrail no longer skips a road a metre or more below it.

**After:** 1 lip, 0 seams, 0 banks, no floating pieces, and every car on the ground.
- **The last lip:** 0.21 m at the Terrace Row / High Street corner. The junction's corner ramp ends above the strip of ramp beside Terrace Row's mouth, because the ground there sits 0.3 m above the mouth.
- **Two fixes that failed:**
  - Pulling the ground down 7 m round junctions made 7 lips elsewhere.
  - Keeping corner ramps off the mouth strips made 24 lips.

  Both were reverted.
- **Next:** blend the mouth strip's inner edge to the corner ramp's outer edge where they meet.

**Diagnostics added:**
- The seam report prints the road line's heights and every surface under each step.
- The edge audit prints a metre-by-metre profile of what lies outward from each lip and bank.

## Not yet done in W1 (doc 58)

- **Streaming the towns' buildings.** Kit time is about 0.9 s across the two towns. They also register footprints and spots that later placement reads, so they'd need splitting into "decide" and "build" first.
- **Terrain streaming.** The mainland's terrain chunks already have LOD.
- **Floating origin.** The world spans about 6 km, so precision at the edges is under a millimetre. The continent will need it.
- **Data-driven regions** (replacing `MapRegions`), race rows as data, and world-state layers.
- **Keeping the world across races.** It's rebuilt each time today. This would be the biggest load win, but races build a lighter world, and keep/reject studies depend on it.
