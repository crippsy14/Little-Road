# Iteration 16 — coastal rocks and ground materials

17 September 2026. Continued environment work toward the coastal-world concept. This is an intermediate art pass, not high-fidelity or gameplay acceptance.

## Changes

The authored island mesh now has finer sampling and small-scale erosion in the terrain height function. Runtime cliff meshes add separate rock masses with matching static mesh colliders. Road, workshop, forecourt and resident exclusions remain in place. Plant placement samples the actual terrain/outcrop colliders rather than assuming the original height field is the visible surface.

The terrain shader uses world-space triplanar albedo, normal, ambient-occlusion and roughness maps, with slope-based rock/grass blending and damp shoreline tinting. It uses the CC0 Rock 08 material by Rob Tuytel and Grass Ground by Charlotte Baglioni, both from Poly Haven. The six downloaded source maps are unchanged and MD5-verified; see `unity-09/material-downloads.json` and the root `THIRD_PARTY_ASSETS.md` for URLs, hashes and licences. The provider API reports dimensions in millimetres: Grass Ground's 2510 value represents 2.51m, not 25.1m. Build 03 corrects its mapping scale accordingly. The rock mapping deliberately enlarges the scanned detail, while the shoulder uses it at a finer scale.

Low grass and dry-herb clumps are original, double-sided blade meshes. They remain outside the reserved driving and collectible areas, have no solid colliders, and do not yet have wind or a dedicated distance-LOD system. These details are not a completed vegetation ecosystem or performance-optimised final asset set.

## Visual iteration

Build 01's layered outcrops looked like stacked polygon towers. The actual coastal and bridge captures rejected that approach. Build 02 replaced repeated shelves with weathered curved masses, introduced ground cover and textured shoulders. Its captures exposed another problem: the lower portions of some rocks looked perched above sloping ground. Build 03 changes their profile and embeds every foot into the local terrain, also rejecting steep-slope placements for the small roadside boulders.

All comparisons use actual game camera renders at the existing review poses. The concept sheet remains an art-direction reference and is never presented as implemented gameplay. Build 03's coast, bridge approach and garage images were inspected: the rock bases now join the slopes, but their pointed lobes are repetitive, some surface joins are conspicuous, and the distant headland is too pale and smooth. Ground cover is subtle from the driving cameras despite its geometry cost. The scene still needs more natural landform composition, dense varied planting, convincing water, detailed masonry and road wear. The current cars, broad flat route and repeated architecture also remain visibly below the reference.

## Validation and package

Build 03 passed 111 editor checks and exported successfully. Gameplay assembly SHA256: `7403893711BE0E706CFE1B0B8CCB396EC2A53BAD2D64662453DBF19406CA81A7`. The final world review recorded 39 accepted outcrops and 2,647 ground-cover clumps (302,076 blade triangles). This triangle cost needs profiling and distance management before final performance acceptance.

The world checks passed 171 road, 11 garage and 27 forecourt overlap samples plus 1,710 road/shoulder support rays. Physical garage entry and reverse exit passed with zero contacts/resets and 8.9mm maximum lateral drift. These are scoped samples and one physical path, not a continuous no-clipping guarantee. All eight world views are in `unity-09/captures-world-03`.

The build-03 seed-1720 three-lap race finished all 24 cars without recovery resets or missed gates. Its pose audit sampled 13,225 completed physics steps: 2.17cm maximum car overlap, zero tagged-prop overlap and zero query saturation. This retains a known collision limitation rather than passing a no-clipping requirement. The race camera exposed perched small stones on slopes; build 04 aligns those stones to the actual surface normal and sinks them into it. It also darkens the excessively pale distant-headland material.

Final build 04 passed 111 editor checks; gameplay assembly SHA256 is `52FD41F8A7622FD28ACDCB79BF8F722CBC1679F0251529997C9ABF9AEE56284C`. Its world/garage checks passed the same sample counts with zero obstructions, contacts or resets and 8.9mm maximum garage drift. `captures-world-04/07-bridge-approach.png`, `08-bridge-profile.png` and `captures-field-04/02-race.png` were inspected: small stones now follow the slopes, and the distant hills have a darker sage tone. Their smooth shape and saturated crests still need improvement. The bridge view particularly exposes the plain masonry and flat water.

The final build-04 seed-1720 race finished all 24 entrants over three laps. Stock placed 24th in 110.195s. No recovery, missed gate or 1Hz sampled road departure occurred. The 13,225-step audit again measured 2.17cm maximum car overlap, zero tagged-environment overlap and no saturated queries. The audit covers box colliders at completed physics poses, not visual triangles or interpolated rendering. This scenery regression does not replace the broader balance/collision matrix or a full manual adventure test. World and race reviews briefly ran concurrently; no performance conclusion is drawn from them.

The checkpoint archive is `little-road-unity-adventure-09.zip`; `unity-09/package-manifest.json` records its SHA256 and the verified hash of every archived export file. Earlier frozen archives remain unchanged. `tools/package-unity-checkpoint.ps1` refuses to overwrite an existing archive and verifies the reopened ZIP against its source files. The current export retains the ordinary adventure launcher and the windowed 24-car demonstration.

## Remaining target

This pass does not earn a new 4/5 visual acceptance score. Environment/materials and road/roadside remain around 3/5; car art, props and HUD/labels remain around 2/5. The full five-view rubric still needs a new car close-up and interface review. No representative visible frame-rate benchmark is claimed. Real car overlap, physical obstruction recovery, the wider driver/equipment matrix, expanded progression and physical DualSense testing remain open.
