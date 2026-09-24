# Coastal foliage candidate — 19 September 2026

This is a source-art candidate, not a replacement for the normal checkpoint 12 export. The new performance-points/race-preview request is recorded in [its future design note](../docs/21-performance-rating-and-race-preview.md); it remains deferred alongside driver and teammate progression.

## Change and comparison

The old large opaque canopy cores read as green balls, especially on olives, while pines looked like flat plates. The candidate removes those near-view cores, distributes more folded blades through the outer canopy, bends the broadleaf branches and makes pine clusters deeper. Distant meshes retain representative leaf sprays and smaller interior cores. A dedicated opaque foliage shader adds wrapped diffuse light and a modest backlight term without shiny leaf highlights. Tree placement, driving and equipment tuning are unchanged.

Trees and scrub now sit outside the landscape static-batching root, retaining shared meshes and their 32-bit indices. This avoids submitting the denser foliage to the combine path implicated in the earlier grass corruption. It is an architectural precaution, not evidence of a measured frame-time improvement.

Compare the identical bridge camera in [checkpoint 12](unity-15/world-02/07-bridge-approach.png) and [this candidate](unity-16/world-01/07-bridge-approach.png). The candidate has fuller, porous canopies and fewer conspicuous spherical cores. The race view [at driving distance](unity-16/field-01/02-race.png) shows the same improvement around the inland bend, with a clear road. The foliage remains rather dark and uniformly distributed; large bare slopes, angular rocks and primitive roadside boulders still dominate the environment.

The [village-wide view](unity-16/world-01/05-village-wide.png) is essentially unchanged: its separate ornamental trees and planters still use obvious rounded primitives. The coast recipe does not replace those assets. The [coastal concept](../references/concepts/coastal-world.png) remains substantially richer in planted borders, architecture, road edges, atmospheric layering and car detail.

## Build and initial checks

- Build 01 succeeds with 130 editor checks. Assembly SHA-256: `7FA33F137950CC18D762704AA3D5372A1B7F0794AAD66B7F400B44364C148DFE`.
- [World clearance](unity-16/world-01/clearance.json) passes 171 road, 11 garage, 27 forecourt and 1,710 road/shoulder support samples with no reported obstruction.
- [Synthetic garage driving](unity-16/world-01/garage-driving.json) enters and reverses out with zero contacts/recoveries and 0.00894 m maximum lateral error. This is not physical-controller or manual acceptance.
- Build log: [build-01.log](unity-16/build-01.log). Runtime fixed-camera log: [world-01.log](unity-16/world-01.log).

The [24-car, three-lap field](unity-16/field-01.json), seed 1721, completes with all 24 finishers, zero recoveries and zero missed gates. [Sampled ordered-gate validation](unity-16/field-01.validation.json) passes. Every car's finishing place and time matches the checkpoint 12 run of the same seed, including the stock car in 20th at 110.486763 seconds. The completed-pose audit records 0.0006485 m maximum car-box overlap, zero environment overlap, 13,394 sampled steps and no saturated queries. These results support preserving race behaviour in this comparison; they do not establish universal no-clipping or a frame-rate target.

## Cost and acceptance

The runtime records near/far vertex counts of 126,500/36,740 for cypress, 130,200/34,398 for olive and 183,600/48,276 for pine, per shared variant. Shared geometry avoids duplicate mesh storage, but does not make all visible triangles free. This first pass is deliberately a visual comparison and is too expensive to accept without profiling and simpler distant representations. No 1080p/60 fps claim follows from the background runs.

The [continuous free lap](unity-16/route-01/free-lap.json) covers 683.72 m in 42.683 seconds with no contacts or recoveries. Its 8.669 m maximum lateral offset extends onto the shoulder beyond the 8 m asphalt half-width, within the runoff. Inspected moving-camera captures include free-drive distances 110, 220, 440 and 660 m and race distances 330 and 660 m. They show a clear carriageway without the prior stretched grass in these sampled views. These captures do not establish absence of every transient defect between frames. The village's rounded ornamental planting remains particularly obvious on returning to the start line.

The subsequent [connected career race](unity-16/route-01/race-result.json) completes three laps and opens the actual result menu. The [isolated save](unity-16/route-save-01.json) records 100 credits, 21st place and matching race/settled serial 1, starting from zero credits. This exercises the real payout path using AI driving; it does not replace manual finish or physical DualSense acceptance. The review process exited normally. No normal player save was used.

Current visual assessment remains 2–3/5: foliage silhouette/detail improves toward 3, but environment coherence, roadside detail and atmospheric depth remain around 2. Cars are unchanged. The 4/5 gate is not met. Required next work is to preserve the improved silhouette at lower near/far cost, assess LOD transitions in motion and then replace the village's primitive planting. Checkpoint 12 stays the normal playable build.
