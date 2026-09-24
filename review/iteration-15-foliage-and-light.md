# Iteration 15 — foliage, cloud sky and light

17 September 2026. First environment fidelity pass following the user's request to push the visuals substantially further. This is progress toward the reference, not visual acceptance.

## Implemented and inspected

`CoastalVegetation` supplies original mesh recipes for cypresses, coastal olives and umbrella pines, with three variants of each. Branches and roots have matching static mesh colliders; near foliage has folded leaf geometry around clustered inner growth. A separate distant crown mesh reduces detail with distance. The same reserved road, workshop and resident areas remain clear. The old village planter/tree props have not yet been replaced, and biome density is still sparse.

Two local reflection probes capture the actual assembled coast and garage for glossy materials. Main-light shadows extend 110m at 4096 resolution, with the existing soft-shadow and ambient-occlusion settings. Colour grading reduces the earlier saturation boost. This is not a baked global-illumination solution or a measured performance acceptance result.

The first-party sky shader samples [Poly Haven's Kloofendal 48d Partly Cloudy (Pure Sky)](https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky), by Greg Zaal and Jarod Guest, under [CC0](https://polyhaven.com/license). The downloaded 2K HDR file is unchanged, verified against the provider hash, and credited in `THIRD_PARTY_ASSETS.md`, which the build copies into the export. The imported half-float texture clips the original extreme HDR range at 65504; the runtime scan recorded a sun elevation of 47.90039 degrees. The shader's panorama orientation and directional light share the same brightest-pixel calculation. This is static sky photography with runtime exposure/rotation, not a weather or time-of-day system.

## Iteration from actual captures

Build 01's leaf-only canopies looked hollow at distance, and the extended shadows left the garage frontage too dark. Build 02 added dense inner growth, changed light direction and reduced contrast. Build 03 introduced the cloud sky and automatic sun alignment; captures exposed thin vertical panorama seams and an abrupt colour boundary above the sea. Build 04 fixes longitude sampling derivatives and blends the lower sky into the scene's fog colour. Its garage and bridge-approach captures were inspected, and the visible seam is absent in those views.

Use `unity-08/captures-world-04/03-garage-forecourt.png`, `06-coastal-road.png` and `07-bridge-approach.png` for the current output. The camera poses match the previous world review. The sky and sunlit garage are a clearer improvement than the vegetation, which still reads as repeated sparse clusters in the wide view.

## Validation

Build 04 passed 105 editor checks and exported. Gameplay assembly SHA256: `427D502ABE6DB81B428DAE53DF1225C6501B29D8E4935109BA528934F03070E8`. The final world review passed 171 road, 11 garage and 27 forecourt overlap samples plus 1,710 support rays. Physical garage entry and reverse exit passed with zero contacts/resets and 8.8mm maximum lateral drift. These are sampled clearance and a specific driving path, not a continuous no-clipping guarantee. The eight world camera captures were saved.

The seed-1720 three-lap race finished all 24 cars, with stock 24th in 110.195s. No recovery, missed gate or 1Hz road-centre departure was recorded. The 13,225-step pose audit reported 2.17cm maximum car overlap, zero tagged-prop overlap and zero query saturations. The expected collision limitation remains. The race capture `captures-field-04/02-race.png` was inspected: the new tree silhouettes and longer shadows are visible, but the bare slopes and unfinished utility bodies remain prominent. No representative visible performance benchmark or new full adventure smoke is claimed by this art regression; the connected-loop checks were completed in the preceding functional checkpoint.

The previous functional checkpoint remains preserved as `little-road-unity-adventure-07.zip` (SHA256 `05258BEE33A02DBDC51E9918653E182E2EE047734A9E2D5BF966A4CFA3EED26F`). The visual checkpoint is `little-road-unity-adventure-08.zip`, with per-file verification in `unity-08/package-manifest.json`. The mutable Windows export contains this visual pass; the older archive does not include these foliage, sky or lighting changes.

## Comparison against the concept

The art direction remains `references/concepts/coastal-world.png` and `vehicle-equipment.png`. The cloud sky, warm frontage and varied tree silhouettes begin to support that direction, but the scene still exposes large smooth bare slopes, featureless water, simple masonry, sparse landscaping, repeated block-shaped buildings and unfinished cars. Clouds alone do not make the whole scene high fidelity. The light resident labels also lose contrast over the sunlit garage and need a legibility pass.

| Dimension | Current applicable score / 5 | Remaining visual gap |
| --- | --- | --- |
| Car quality | 2 | Angular cabin/lamps, basic tyres and older coupe/utility bodies |
| Environment shapes/materials | 3 | Bare height-field hills, plain masonry and coarse distant landforms |
| Road and roadside | 3 | Broad uniform verges, little surface wear or planted detail |
| Lighting/atmosphere | 3 | Cloud sky and sun alignment improve depth; water response, bounce light and atmospheric integration remain basic |
| Composition/exploration | 3 | Existing loop, village and bridge; no denser interconnected settlement or broader route network |
| Props/polish | 2 | Repetitive sparse vegetation and simple planter/bench/workshop props |
| HUD/labels | 2 | Current label contrast on bright walls needs correction; HUD design remains provisional |

No row establishes the required four in every dimension across all five representative views. A fresh car close-up and full live performance review are still required.

Next major visual work should address the land itself: authored cliff formations, rock/soil/grass materials with surface detail, mixed ground cover and planted verges, then coast water and the village's dense landscaping. Car-body fidelity remains a separate substantial art task. Physical collision overlap, obstruction rejoining, equipment/driver balance matrices, expanded progression and the other game requirements from the project brief are still open.
