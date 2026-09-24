# Coastal masonry study — 19 September 2026

The preceding goal turn made concrete progress and delivered [checkpoint 13](iteration-24-village-planting.md). This turn targets the long, flat roadside/bridge walls, which remained conspicuous next to the improved planting. The game, campaign and visual goal remain unfinished.

## Construction and revisions

The wall surfaces now have three staggered stone courses, individually chamfered edges, small joints, separate coping blocks and recessed mortar. Widths and corner wear vary deterministically. Continuous backing colliders handle contact rather than thousands of separate stone corners. The bridge parapet collider is unchanged; revision 06 changes roadside collision geometry as described below. This does not establish universal no-clipping or crash acceptance.

`CoastalMasonry` builds original mesh geometry in six bounded batches, each no larger than 48,000 vertices, outside the landscape static-combine hierarchy. The scene reports 2,700 stones, 259,200 vertices and 118,800 triangles. It uses five shared material tones and the already credited Rock 08 diffuse/normal maps; [asset attribution](../THIRD_PARTY_ASSETS.md) now records this use. The new surface normals require tangent data, also supplied on the older bridge meshes.

The [first render](unity-18/world-01/07-bridge-approach.png) was too brown and had overly strong dark joints. The old bridge structure also looked unrelated to the new upper wall. Revised materials lighten and desaturate the stone, reduce normal-map strength and use the same surface family on the bridge structure. Narrower chamfers reduce the outlined appearance. Mortar is recessed at the ends and below the coping, where the original backing otherwise hid the new end/top details.

The [revision 04 wall view](unity-18/world-04/07-bridge-approach.png) reads as pale coastal limestone without the initial heavy outlines. Its [bridge profile](unity-18/world-04/08-bridge-profile.png), however, exposes stretched UVs on primitive piers and some curved arch faces. Revision 05 switches those structural elements to the existing world-space rock mapping, preserving physical texture scale across tall pieces and curved surfaces. The individual wall stones retain their own projected UVs. The bridge is still a simplified structure, not finished landmark art.

The moving revision 05 lap exposed repeated gaps between the old straight wall sections around outside bends ([free-drive view](unity-18/route-05/free-220.png)). Revision 06 makes these sections follow the circuit, with shared endpoints at the existing 3.6 m spacing. Their backing mesh colliders follow the same curve. Nominal wall offset, width and height are preserved, as are deliberate turnout/collectible openings. This closes the unintended gaps in both the visible wall and its collision surface. It is a physical geometry change and needs fresh route/field checks.

No driving, equipment, car collision-box or race-logic tuning is changed. The authored bridge deck/collider remains in place. The optional scanned cliff study remains disabled by default.

## Build evidence

Revision 06 compiles successfully with 130 editor checks. Gameplay assembly SHA-256: `1E6556BFE2CA271B38F70B131018754A2FF6A1C575B9E0212D29C4CF73547E4D`. See [build log](unity-18/build-06.log) and [editor checks](unity-18/editor-checks-06.json). Its runtime still reports 2,700 stones in six batches.

[Revision 05 world clearance](unity-18/world-05/clearance.json) passes 171 road, 11 garage, 27 forecourt and 1,710 support samples without obstruction. [Synthetic garage entry and reverse](unity-18/world-05/garage-driving.json) passes with zero contacts/recoveries and 0.0129 m maximum lateral error. These checks cover the sampled paths, not every possible collision with the walls.

The revision 05 moving review completed a 683.57 m free lap in 42.67 s with zero contacts/recoveries, then a three-lap career race. The actual result screen paid 100 credits for 21st place into an isolated review save; race and settlement serials both became 1. This review stops shortly after the player finishes and does not prove that all 24 entrants finish.

Revision 06 checks:

- [World clearance](unity-18/world-06/clearance.json): 171 road, 11 garage, 27 forecourt and 1,710 support samples pass with no obstruction.
- [Synthetic garage drive](unity-18/world-06/garage-driving.json): entry/reverse pass, zero contacts/recoveries, 0.0149 m maximum lateral error.
- [24-car, three-lap field](unity-18/field-06.validation.json), seed 1721: all 24 finish, zero recovery resets or missed gates, ordered-checkpoint samples pass. Stock finishes 20th in 110.4868 s, matching checkpoint 13. The completed-physics-pose audit samples 13,394 steps with no query saturation, maximum car-box overlap 0.0006485 m and tagged environment overlap 0 m. This measures collider poses, not every visible triangle or interpolated frame.
- [Continuous free lap](unity-18/route-06/free-lap.json): 683.57 m in 42.67 s, zero contacts/recoveries. Maximum centre offset 8.669 m enters the shoulder but stays within the 12 m runoff boundary.
- [Moving bend comparison](unity-18/route-06/free-220.png): the repeated open wall joints visible in revision 05 are closed, with the wall following the outside bend. Deliberate route openings remain.
- [Connected career result](unity-18/route-06/race-result.json): three laps complete, actual result menu opens, 18th place pays 130 credits from a zero balance into the [isolated review save](unity-18/route-save-06.json). Both race/settlement serials are 1. The [result capture](unity-18/route-06/race-result.png) agrees with the saved balance. This is an AI-driven flow check, not clearance of the earlier manual finish issue or physical controller acceptance.

All three revision 06 review processes exited after their checks. The normal player save was not used. These hidden captures and tests are not a rendering frame-time benchmark; the additional masonry geometry still needs an interactive performance pass.

The [revision 06 bridge profile](unity-18/world-06/08-bridge-profile.png) removes the stretched texture seen in revision 04. Its piers and spandrels remain broad, simple shapes; those surfaces are not yet built from convincing individual structural stones. The [driving view](unity-18/world-06/07-bridge-approach.png) reads more clearly as pale masonry and gives the road edges useful scale. Against the coastal concept, roadside detail approaches 3/5; overall environment shape, atmosphere and prop density remain around 2–3/5. The car is unchanged. The 4/5 visual gate is still not met.

The direct concept comparison still exposes larger priorities: the angular cliff silhouettes and stretched-looking terrain slopes dominate these views; the bridge needs coherent structural masonry beyond its parapets; and the coast lacks the layered settlement, landmarks, softer lighting and set dressing of the reference. Improving wall detail alone does not resolve those differences. Future work should address those larger shapes and composition alongside the three car masters, rather than repeatedly polishing the same wall material.

The candidate export is `outputs/little-road-masonry-review`. Checkpoint 13 remains the normal launcher/export while this study is reviewed. No new frozen checkpoint is implied by compilation alone.

Subsequent delivery: this masonry revision is included with the [coastal relief pass in checkpoint 14](iteration-26-coastal-relief.md), after fresh world, field and connected-route checks. That later review records packaging and promotion to the normal launcher.
