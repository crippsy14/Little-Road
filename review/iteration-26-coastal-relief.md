# Coastal relief study — 19 September 2026

The preceding goal turn made progress: it completed the masonry revision, closed repeated roadside wall gaps, and recorded passing world, garage, 24-car and connected free-drive/race/payout checks. That candidate remains separate from normal checkpoint 13. This turn addresses the large cliff surfaces highlighted by the actual driving-view comparison; the full game and concept-fidelity goal remain unfinished.

## Starting evidence and change

The [previous coastal view](unity-18/world-06/06-coastal-road.png) shows long, almost vertical planar streaks beneath small ledges. The old outcrop generator used 11 perimeter rings and flat normals on every triangle. Following the local ground at the base avoided floating rocks but stretched those few faces over the hillside.

The first revision uses denser perimeter/height sampling, uneven sedimentary beds, narrow vertical joints, and two smaller scales of original procedural geometric relief. It averages normals across shallow changes while retaining harder ledge/fracture breaks. Each candidate formation still checks its generated vertices against the reserved road, village and resident areas, and buries its base into the actual height field. Render and static collider use the same mesh. Planting continues to raycast the resulting surfaces.

The previous source is preserved in [CoastalCliffs-before.cs.txt](unity-19/CoastalCliffs-before.cs.txt). This is an original authored/procedural geometry revision using the existing credited limestone maps; no additional external asset has been introduced. The optional scanned-cliff experiment remains separate and disabled by default.

## Review status

Revision 01 compiles with 130 editor checks. The [coastal view](unity-19/world-01/06-coastal-road.png) has less severe straight polygon striping than the prior build, but the upper forms are too rounded in places and the lower banks remain broad, bare slopes. Its world/garage review passes 171 road, 11 garage, 27 forecourt and 1,710 support samples, with zero garage contacts/recoveries and 0.0148 m maximum lateral error. There are 40 accepted outcrops versus the prior 38, since the reshaped footprint changes clearance acceptance.

Revision 02 adds irregular fallen limestone blocks around exposed formations. Eight shared original meshes are constructed from intersecting cutting planes, with matching mesh colliders, then placed against the actual ground/outcrop surfaces. Generated vertices must clear the same protected areas. Plant placement now recognizes these rock surfaces too. The terrain height function adds uneven bank relief beyond 14 m from the route; the asphalt and supported shoulder remain inside the unaffected area. Village and resident flattening still apply afterward.

Revision 02 compiles with 130 checks and passes world clearance. The scene has 40 outcrops and 94 fallen blocks. Its [coastal view](unity-19/world-02/06-coastal-road.png) rejects part of the placement: blocks on steep surfaces read like climbing holds, and some sit on the caps. Revision 03 restricts fallen blocks to lower banks with surface-normal Y at least 0.66, excludes the formation crest, samples a wider apron and embeds them more deeply. Its placement ray excludes existing fallen blocks, while later vegetation rays include them.

Revision 03 compiles with 130 checks and passes world/garage checks. It reduces fallen blocks to 42, but [its coastal render](unity-19/world-03/06-coastal-road.png) still shows some as thin plates projecting from the slope. Aligning their up axis to the ground normal and burying them deeply caused that appearance. Revision 04 places thicker blocks with approximately level orientation on shallow banks only (normal Y at least 0.88), reduces the repetitive outcrop ripples and uses a tighter normal-smoothing angle to retain more fracture edges. The first two placement rules were insufficient; passing clearance did not make those placements visually acceptable.

## Revision 04 evidence

Build 04 passes 130 editor checks. Gameplay assembly SHA-256: `2F61ACC6BFA22C8D7208EA72C1F2CC884D248DBECD89901B9A893111C20B01B1`. See [build log](unity-19/build-04.log) and [editor checks](unity-19/editor-checks-04.json).

The runtime creates 40 outcrops and 22 fallen blocks from eight available shared variants. Each outcrop has 5,248 triangles (209,920 total), versus 704 per old outcrop. This is a substantial geometry increase, not a free fidelity gain; LOD and interactive rendering-performance checks remain necessary.

The [coastal comparison](unity-19/world-04/06-coastal-road.png) removes the conspicuous plates seen in revision 03 and reduces the original straight polygon striping. The [bridge profile](unity-19/world-04/08-bridge-profile.png) still exposes overly smooth bank transitions, broad cliff faces and unfinished bridge structure. The [village approach](unity-19/world-04/02-village-approach.png) retains the planted street and clear carriageway. Outcrop shape is an incremental improvement, but scene composition, material variation, lighting, distant settlement and the car masters remain below the concept. Overall visual assessment remains around 2–3/5; the required 4/5 gate has not passed.

[World clearance](unity-19/world-04/clearance.json) passes 171 road, 11 garage, 27 forecourt and 1,710 support samples. [Synthetic garage entry/reverse](unity-19/world-04/garage-driving.json) passes with zero contacts/recoveries and 0.0149 m maximum lateral error. These are scoped checks, not universal no-clipping proof.

The [24-car, three-lap seed-1721 race](unity-19/field-04.validation.json) finishes all entrants with zero recoveries or missed gates and passing sampled ordered-checkpoint progression. Stock finishes 18th in 109.4120 s. The pose audit samples 13,303 completed physics steps, reports no query saturation, maximum car-box overlap 0.0002365 m and tagged scenery overlap 0 m. The different finishing result from the preceding build does not establish a balance change or validate natural driver variance; this is one regression field.

The [continuous free lap](unity-19/route-04/free-lap.json) completes 683.72 m in 42.68 s with zero contacts/recoveries. Maximum centre offset is 8.669 m: the car uses the shoulder but stays inside the 12 m runoff boundary. The [inland bend](unity-19/route-04/free-220.png) shows the more uneven banks and closed wall joins; the [coastal chase view](unity-19/route-04/free-550.png) retains a clear road beside the revised cliffs and masonry. These are actual moving-camera captures, not generated reference art.

The [connected career race](unity-19/route-04/race-result.json) completes three laps, opens the actual result menu, and saves 130 credits from zero for 16th place. The [result capture](unity-19/route-04/race-result.png) and [isolated save](unity-19/route-save-04.json) agree; race and settlement serials both equal 1. The coastal race capture at 550 m was also inspected. This is AI-driven flow evidence, not clearance of the earlier manual finish report or physical-controller acceptance. All revision 04 review processes have exited.

## Delivered checkpoint

Checkpoint 14 includes this revision and the preceding masonry pass. The immutable archive `outputs/little-road-unity-adventure-14.zip` contains 320 files and is 134,437,852 bytes. SHA-256: `3AE56EECEA37B70354196256A902D1C65ED7C3BBD3A593C6894C3F732551A28A`. Every archived file was hashed against the tested export; see [package manifest](unity-19/package-manifest.json).

[Promotion](unity-19/promotion.json) staged and verified all 320 files, then made this the normal `little-road-unity-windows` export used by the launcher. Checkpoint 13's former export is preserved as `little-road-unity-windows-before-14`; prior frozen archives remain unchanged. The normal player save was not touched.

The road layout, equipment and race logic are unchanged. The full visual/gameplay goal remains active and incomplete. The next substantial art work should revisit the three car masters and the broader coast composition/lighting; further tiny surface changes alone will not close the gap to the reference.
