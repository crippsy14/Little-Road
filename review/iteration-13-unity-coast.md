# Iteration 13 — sculpted coast and a driveable inlet bridge

17 September 2026. Previous goal turn: progress. The village was implemented, tested and packaged with recorded visual shortcomings. This iteration addresses the largest of those shortcomings: the flat island and absent coastal structure. The full goal remains active.

## Implemented changes

`CoastalLandscape.cs` replaces the flattened cylinder island with an original height field and a matching static mesh collider. It adds interior ridges, coastal cliffs, a lower sea, a distant southern headland, shoreline foam, rooted cypresses, embedded roadside rocks and intermittent stone walls. The terrain holds the established road, village and lighthouse turnout at their existing datum. Road elevation and banked/suspended vehicle physics have not been redesigned in this pass.

A carved inlet crosses the road at distance 577.09m. A 50m bridge follows the circuit's slight curve, with a 25m structural deck, continuous parapets and three open masonry arches on each side. The road and 24m shoulder have matching mesh collision surfaces, including over the inlet. The bridge deck is also a physical surface; the inlet is not an intact invisible island beneath a painted road.

The original terrain shader blends meadow and limestone using surface slope, height and world-space texture grain. It uses the installed URP lighting interfaces. This replaced conspicuous triangle-by-triangle material boundaries. Cypress crowns use authored irregular ring meshes, with finite-vertex validation before batching. Static architecture remains in editable C# modules, and no third-party art has been introduced.

World signs now use a separate depth-tested font shader, so terrain and buildings can occlude them without changing the Canvas UI material. Resident labels face the camera, hide beyond 24m and respect line-of-sight. The garage notice lettering was reduced to fit its board after proper depth testing exposed its oversized text.

## Iteration evidence and defects found

- Build 01 introduced the landforms and bridge. Existing road/village clearance samples and the physical garage entry/reverse-exit check passed. Captures exposed hard triangular material changes and striped foliage.
- Build 02 added slope blending and irregular crowns, plus two bridge camera views and 1,710 road/shoulder ray samples. Support checks passed, but a fractional power of a slightly negative `sin(pi)` generated NaN tree vertices. The combined foliage vanished; this was visible in captures and reported in the runtime log. Build 03 clamps the crown radius input and validates every generated mesh vertex.
- Bridge profile captures exposed gaps above the arches and then inward-facing masonry triangles. Curved infill, correct outward winding and hard face normals fixed these issues. A continuous deck replaces overlapping slab segments; parapets are continuous curves with matching collision meshes.
- Build 04's first garage capture omitted the player while other views showed it. A separate repeat of the same build showed the car at the same pose. Build 05 explicitly synchronises physics and render transforms at review teleports, waits for a physics step and a longer settled interval, and logs the rendered pose and visible renderer count. This is a capture-path correction; it is not evidence that all runtime rendering issues are resolved.

The review uses the same six village/coast poses as iteration 12, with two additional bridge poses (`07-bridge-approach`, `08-bridge-profile`). Images are actual Unity output. Review mode relocates the car between poses; its garage test subsequently uses actual trigger input and Rigidbody motion.

## Racing observations

Build 02's three-lap stock test (seed 1709) finished all 24 cars. It passed sampled ordered-gate checks but needed one recovery for a missed gate, with maximum contact penetration of 10.04cm. Car 1 left the road around the finish line at 33–36 seconds; this was not an inlet-crossing failure. See `unity-06/field-02.json` and its validation file.

Build 03's connected adventure test completed delivery, collection, purchase/fitting, a real 24-car race, payout, return and reload. The Workshop-equipped player placed 15th and received 130 credits. All 24 finished, but two cars needed recovery and one missed a gate; maximum contact penetration was 7.05cm. One car stopped on the village forecourt after running wide; another missed a gate near course distance 171m. These are unresolved traffic failures. The scenery pass does not establish accepted balance, no-clipping or reliable recovery-free racing. Build 04 repeated the connected loop successfully, without resolving those traffic issues.

## Visual comparison

Compare the actual world captures against `references/concepts/coastal-world.png`. The terrain now provides foreground, middle-distance and background landforms, a coast-facing road edge and a real bridge landmark. It remains far simpler than the reference: rounded height-field hills, broad bare slopes, repetitive narrow trees, plain water/sky, basic stone materials and limited settlement density are conspicuous.

| Dimension | Coastal view score / 5 | Evidence and remaining gap |
| --- | --- | --- |
| Car quality | 2 | Existing compact is readable but retains angular cabin, lamp and tyre shortcomings |
| Environment shapes/materials | 3 | Cliffs, ridges and arches replace the flat island; terrain/stone detail and tree variety remain basic |
| Road and roadside | 3 | Wide supported route, coping and coastal walls now frame the journey; verges and surface wear need refinement |
| Lighting/atmosphere | 2 | Landform depth and distant colours help; flat sea/sky, lighting transitions and contact detail remain weak |
| Composition/exploration | 3 | Road, inlet and village now connect visually; no richer branch routes, varied elevations or dense distant settlement yet |
| Props/polish | 2 | Trees and rocks populate the slopes; sparse, repeated forms do not match the concept's landscaping |
| HUD | 3 | Existing adventure/race UI remains readable; large overlay strips and provisional presentation persist |

The garage retains iteration 12's scores, with a more legible landscape backdrop. The visual gate of at least four in every applicable dimension still fails. A fresh vehicle close-up is not claimed by this environment review. Steep off-road slopes and access outside the designed route need their own driving review; the current car body constrains pitch/roll, so general all-terrain handling is not established.

## Final checkpoint status

Build 05 passed 104 editor checks and exported successfully. `captures-world-05/clearance.json` records 171 clear road overlap samples, 11 garage samples, 27 forecourt samples and 1,710 supported road/shoulder points. The garage drive entered and reversed out without contacts or recovery, with 3.9cm maximum lateral drift. All seven relocated capture poses logged matching physical/render positions and visible player renderers; the garage, coast and bridge images were inspected. No invalid mesh bounds or shader error was reported in this final world run.

The final exported adventure test passed the connected quest/finds/purchase/fitting/race/payout/return/reload sequence, including synthetic menu and driving input. All 24 cars finished. The Workshop-equipped player finished 15th and retained 130 credits. The run still had two recovery resets, one missed-gate event and 7.05cm maximum contact penetration. These remain failures against the full traffic/no-clipping requirements even though the integration assertions passed. Evidence: `smoke-05.log`, `smoke-05-result.json`, `smoke-05-race.json` and `smoke-05-race.validation.json`.

A fresh process reloaded the same isolated profile and rejected duplicate rewards (`restart-05.log`, `restart-05-result.json`). Test profiles are separate from the ordinary player save. The adventure smoke relocates between its objectives; a continuous manual journey and physical DualSense acceptance remain unverified.

The checkpoint archive is `outputs/little-road-unity-adventure-06.zip`. Its hash and verification of every archived file against the export are recorded in `unity-06/package-manifest.json`. Gameplay assembly SHA256: `84B0244E44745B083A43CDFBC074F3A014482A15B9046296369E2E58E288E977`. Prior frozen checkpoints are preserved. No representative visible performance benchmark was performed during this background regression.

Next functional priority is the observed corner-exit/run-wide/contact behaviour and physical rejoining. Next environment work should refine landscape shapes, varied vegetation, masonry, coast water/sky and the village's integration with the land, followed by the same saved-camera comparison. The complete parts/traversal roadmap, track/class progression, three polished master bodies, decals and approximately 100-body catalogue, audio, manual adventure testing, physical DualSense USB/Bluetooth and a measured visible 1080p/60fps result all remain required.
