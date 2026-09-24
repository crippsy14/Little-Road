# Coastal forms and planting — 19 September 2026

An original mesh/art iteration after checkpoint 11. The full game goal is still active; this is not final fidelity acceptance. The normal packaged build remains checkpoint 11 while the separate coast-art review candidate is evaluated.

## Changes

- The previous limestone recipe narrowed every outcrop almost to a point. The new recipe uses broader caps, recessed beds, fractured outlines, tilted layers and explicit angular faces. The generated formations still check their footprint against the road, village and lighthouse clearances, and their visible mesh is also their solid collider. There are 38 accepted formations in the reviewed world.
- Added three shared low-scrub/rockrose variants with branching stems, individual folded leaves, small ivory or rose flowers and simpler distant canopies. There are 423 deterministic placements in patches, outside the road/recovery corridor and collectible approaches. These are soft non-colliding plants, not invisible obstacles. Placement samples the supporting ground around the base. LODs reduce distant leaf geometry; their presence alone is not evidence that the frame-time target is met.
- The distant headland now has staggered ridge lines and smaller-scale breaks rather than one smooth mound. The first ridge revision was rejected in the render review: it made a bright, steep wall loom over the garage. A further revision moves it farther offshore and reduces its height and colour contrast.

The runtime terrain, scrub and flowers are authored in this project's C# mesh recipes. Existing credited surface maps remain in use. A separate third-party cliff source was downloaded for the next comparison, as described below; it is not in this build. The racing surface, car tuning, equipment, rewards and user save format were not edited in this pass.

## Render findings so far

Build 01's [bridge profile](unity-13/world-01/08-bridge-profile.png) shows more readable angular rock faces and low planting than checkpoint 11's pointed masses. The [village view](unity-13/world-01/02-village-approach.png) has some additional slope coverage, but planting remains too sparse and repetitive to match the concept. The new formations still need richer shape variation and better terrain joins.

Build 02's [garage view](unity-13/world-02/03-garage-forecourt.png) exposed the overly dominant headland. It is retained as rejected intermediate evidence, not a visual success. Both builds passed 129 editor checks and the world/garage clearance checks. Build 02 assembly SHA-256: `62DD470488781FC579CD0FF815BED90CF33F2FB86195CA0F543E2EB63295BFA0`.

## Final candidate and traffic evidence

Build 03 moves the headland 220 m farther away and reduces the ridge height, with a cooler material palette. The [garage view](unity-13/world-03/03-garage-forecourt.png) now has breathing room around its roof, and the [coastal view](unity-13/world-03/11-distant-water-time-0.png) separates the background from the nearby outcrop. This is a composition improvement over build 02; the distant form is still simple and visibly procedural.

Build 03 passed 129 editor checks. Assembly SHA-256: `16AFD7B43F75BB18F3284FB74920A5B2D673987799BD7EAFC6430028A757A5EE`. [World clearance](unity-13/world-03/clearance.json) passes the 171 road, 11 garage, 27 forecourt and 1,710 support samples. [Synthetic garage entry/reverse exit](unity-13/world-03/garage-driving.json) completed with zero contacts/recoveries.

The [24-car, three-lap seed-1720 regression](unity-13/field-03-1720.validation.json) completed every entrant with zero recovery resets or missed gates. Stock finished 24th in 110.20 s. The sampled ordered-gate checks pass. The physics-pose audit covered 13,225 steps without a saturated query, measured no overlap with tagged solid scenery, and measured **2.17 cm maximum car-to-car overlap**. This remains an unresolved collision defect, not a no-clipping pass. The background run is not a visible performance benchmark. The [in-motion race capture](unity-13/field-03-1720/02-race.png) was inspected: the new rocks and scrub are visible from normal play distance, with no stretched grass crossing this view.

## Fidelity judgment and next comparison

The changes improve rock silhouette and add low vegetation, but the environment remains around 2–3/5 against the concept, below the required 4/5. Bare slopes, artificial tree crowns, repeated cliffs, plain shop props and vehicle materials still dominate. The new small flowers do not materially close that gap by themselves. The normal launcher remains on checkpoint 11; this candidate stays in `outputs/little-road-coast-art-review` until the art comparison advances further.

To test whether richer geological source geometry closes the gap faster, [Poly Haven Coastal Cliff 01](https://polyhaven.com/a/coastal_cliff_01) has been downloaded into `source-art/vendor/polyhaven/coastal_cliff_01` for evaluation. The provider identifies Rob Tuytel (photography/processing) and Rico Cilliers (cleanup), under [CC0](https://polyhaven.com/license). This is third-party scan geometry, not original project art. The 2K Blender file and its three maps passed provider MD5 checks; local hashes and URLs are retained in the download manifest. No scan geometry is in the Unity build yet.

The read-only Blender inventory, run with automatic embedded scripts disabled, found four mesh LODs: 461,824, 230,912, 115,455 and 57,726 triangles. The source spans approximately 92 by 11 by 10.3 m, in Blender's Z-up axes. See [inventory](../references/coastal-assets/coastal_cliff_01-inventory.json). The provider page's aggregate triangle count is not the runtime LOD cost. A next bounded comparison should export appropriate LODs, place a representative section outside the track corridor, check UV/detail at driving distance and compare the actual render with our authored formations before deciding whether to integrate it. Cost, artistic fit and collider coverage must be measured, not inferred from a catalogue preview.
