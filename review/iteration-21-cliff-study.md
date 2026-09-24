# Scanned cliff comparison — 19 September 2026

This is a work-in-progress comparison in `outputs/little-road-cliff-study`, enabled with `--cliff-study`. The normal launcher remains on checkpoint 11. The full fidelity and gameplay goal remains active.

## Source and derivative

The credited CC0 Poly Haven Coastal Cliff 01 source is described in [asset credits](../THIRD_PARTY_ASSETS.md). The unmodified vendor Blender file was processed with embedded script execution disabled. The exporter clips a 24 m central section, retains UVs, and exports three LODs of 30,447 / 15,311 / 3,674 triangles. These are the actual derived meshes, not the provider's aggregate catalogue count. Runtime changes fit the derivative to this slope; they do not modify the vendor source.

## Build 01: rejected placement and material

The first build passed 129 editor checks. Its [bridge view](unity-14/world-01/08-bridge-profile.png) shows an obviously rectangular, dark glossy slab above the slope. This is a visual failure despite detailed source geometry. The scan is too high, its cropped ends are exposed, and the runtime-created Lit material needs a serialized shader variant reference before its appearance is assessed fairly.

The [clearance sampling](unity-14/world-01/clearance.json) passed 171 road, 11 garage, 27 forecourt and 1,710 support samples. Synthetic garage entry/reverse also passed with no contacts or recovery. These checks do not validate the slab's visual integration or prove universal collision safety. The scan's runtime bounds were centred at (-88.11, 16.41, -7.00), with extents (4.10, 5.01, 12.00); every high-LOD vertex remained outside the protected corridors.

## Revision under review

Build 02 serializes the exact Lit normal-map/metallic-gloss keyword combination into a Resources material, reduces smoothness, lowers and shifts the face toward the slope, and buries the two cut ends using identical world-space deformation across all LODs. The high-LOD visible mesh also supplies the collider. A dedicated driving-distance camera supplements the unchanged comparison cameras.

Build 02 passed 129 editor checks but runtime startup correctly rejected the downhill placement: 1,168 high-LOD vertices entered a protected corridor. No screenshots or world-clearance pass were produced for that build. The failed review process was stopped by its verified PID/executable path. Build 03 retains the material correction and fits the anchor in 0.5 m increments within a bounded area, checking every high-LOD vertex before accepting a position. The protected margin is unchanged.

Build 03 exposed a transform error in that preflight: it removed the imported FBX root's unit/axis conversion, whereas the instantiated model retains it. The final world-space vertex guard still rejected the same 1,168 vertices, so the failed diagnostic was stopped. Build 04 corrects the preflight matrix to include that conversion. This is a placement-tool bug, not a reason to relax corridor clearance.

## Material and foundation comparison

Build 04 passes world/garage checks and the final scan corridor guard. Its [driving-distance view](unity-14/world-04/13-cliff-driving-distance.png) still fails visually: the photo surface is much too dark against this coast, and the open lower edge hangs in front of the slope. The serialized material removes the conspicuous wet highlights, but this does not establish shader stripping as the sole cause of the first render's appearance.

Build 05 uses the existing world-space Rock 08 limestone material on the scanned geometry. The [bridge view](unity-14/world-05/08-bridge-profile.png) is substantially more coherent in colour and readable relief. The cropped ends turn six metres back into the hillside rather than being pulled down as long curtains. The lower face follows the terrain; a remaining high section of the scan's uneven bottom edge is still exposed in the [close view](unity-14/world-05/13-cliff-driving-distance.png). World/garage checks pass, but that visual seam prevents acceptance.

Build 06 measures the lower/upper edge of half-metre columns from LOD0 and uses that shared foundation profile across all LODs. This replaces the fixed-height blend that missed the higher portion of the lower edge. The optional `--cliff-photo-surface` flag retains the UV photo-material comparison; the default `--cliff-study` uses the same credited limestone maps as the rest of this environment.

Build 06's foundation improves the uneven edge but cannot close an open photographed surface by itself. Build 07 adds downward skirts on true lower boundary edges, welding position keys for edge counting so UV seams do not create internal walls. These extensions use the same limestone surface and collider mesh. The final runtime LOD triangle counts, including skirts, are **30,663 / 15,465 / 3,714**. The export inventory counts remain the unextended source derivative.

## Current candidate

Build 07 passes 129 editor checks. Assembly SHA-256: `7E7423B397C35DE015ADFED701C41B727ED9A80138955B0508863646E6B2FF83`. [World clearance](unity-14/world-07/clearance.json) passes all 171 road, 11 garage, 27 forecourt and 1,710 support samples. [Garage entry/reverse](unity-14/world-07/garage-driving.json) completes with zero contacts/recoveries and less than 0.009 m lateral error. Runtime bounds are centred at (-92.11, 2.65, -7.00), with extents (7.10, 12.84, 12.00), and zero transformed high-LOD vertices intrude into protected corridors.

The [close comparison](unity-14/world-07/13-cliff-driving-distance.png) has much richer small-scale relief than the adjacent authored low-resolution formation, and the shared material avoids the nearly black photo slab. The [road view](unity-14/world-07/06-coastal-road.png) also exposes why this is **not approved for broad placement**: the mass is too rectangular, its cut crest is conspicuous, the lower transition still reads as an attached face in places, and stretching the height to fit the terrain over-emphasizes vertical streaks. A scanned surface is useful source geometry, but this derivative needs an authored cap/back/foot treatment or a different section before it improves the whole composition convincingly. The remaining terrain, trees, bridge masonry and car presentation are still around 2–3/5 against the concept; the required 4/5 gate has not passed.

## Driving and race evidence

The [continuous free lap](unity-14/route-07/free-lap.json) completed 683.60 m in 42.62 s, with zero contacts/recoveries. Its maximum offset was 8.67 m, beyond the 8 m asphalt half-width but inside the supported runoff; this is not a stay-on-asphalt pass. The same isolated session then completed the three-lap career race, displayed the [actual result menu](unity-14/route-07/race-result.png) and saved 100 credits from zero, with race/settled serial both one. See [result evidence](unity-14/route-07/race-result.json). The coastal free/race chase views at 550 m and 605 m were inspected. These are continuous AI-driven route captures, not manual acceptance.

The separate [24-car seed-1720 trial](unity-14/field-07-1720.validation.json) completed all 24 cars with zero recovery resets and zero missed gates. Stock finished 24th in 110.195 s. The 1 Hz sampled ordered-gate checks pass. The 120 Hz physics-pose audit covered 13,225 steps with no saturated queries, measured zero overlap against tagged solid scenery, and again measured **0.021698 m car-to-car overlap**. That approximately 2.17 cm overlap remains unresolved; the scenery change does not claim to fix it. Collider overlap is also not a direct visual-triangle intersection test.

The two isolated diagnostics ran concurrently; neither is a frame-time benchmark. No manual-finish, physical-controller or universal no-clipping acceptance is implied by these results. All diagnostic processes completed. The goal remains active.

## User playtest and next work

At the user's request, build 07 was copied to a separate `outputs/little-road-live-preview-20260919-134332` directory and launched windowed in ordinary free roam with the experimental cliff enabled. `unity-14/live-preview.json` records the process, directory and separate persistent `review/user-playtest-save.json` profile. Startup logged `LITTLE_ROAD_ADVENTURE_READY` with zero credits. Do not overwrite that live directory or stop the user's process while they are driving; future builds should continue using the separate review export. The normal launcher still points to checkpoint 11.

Next functional work should investigate the remaining car overlap and animated-wheel/body collision coverage. For art, keep this scan comparison available but do not scatter the current rectangular derivative throughout the coast. Author the cap, back and foot as a coherent mass, and improve the bridge/planting and three master cars against the same concept views. The full campaign, parts, endurance, performance and hardware acceptance scope remains unchanged.
