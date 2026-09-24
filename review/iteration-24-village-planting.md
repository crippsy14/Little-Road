# Village planting and lighter foliage — 19 September 2026

The preceding turn made concrete progress: [iteration 23](iteration-23-foliage.md) produced and reviewed fuller coastal foliage, established unchanged matched race results and identified excessive geometry cost. This turn addresses that cost and replaces the village's conspicuous sphere-based trees and flower clusters. The overall game and visual goal remain unfinished.

## Geometry and rendering

Near foliage retains the preceding blade positions and outlines, with one mesh surface per blade rather than duplicate front/back vertices and triangles. The custom shader renders both faces and orients the lighting and depth normals to the visible face. Shadow/depth culling is also disabled for those materials. This follows the render-state behaviour described in [Unity 6.6's Cull reference](https://docs.unity3d.com/6000.6/Documentation/Manual/SL-Cull.html); actual appearance is checked in the game. Lit flower petals retain their double-sided geometry.

Distant foliage uses half as many representative sprays, enlarged to approximately preserve coverage, plus the existing small interior core. Random draws are preserved so changing that representation does not regenerate the near tree or its branches. This reduces mesh storage and submitted geometry; it is not a measured GPU speedup.

| Shared tree mesh | Previous near vertices | Current near vertices | Previous distant vertices | Current distant vertices |
| --- | ---: | ---: | ---: | ---: |
| Cypress | 126,500 | 63,250 | 36,740 | 12,815 |
| Olive | 130,200 | 65,100 | 34,398 | 10,038 |
| Pine | 183,600 | 91,800 | 48,276 | 13,851 |

Near canopy geometry is halved; distant vertex reductions range from 65% to 71%. Counts are per species/variant and exclude the separate wood and flower meshes. Shared meshes remain outside static combining. LOD transitions and actual frame times still need dedicated measurement; background simulation timings are not 1080p/60 acceptance.

## Village comparison

The four village trees now use the branched olive recipe in place of six opaque spheres per tree. The first review exposed roots sitting directly on paving. Revision 02 adds shallow limestone-edged soil beds and seats the roots lower. The tree locations remain at the back of the forecourt, clear of its driving approach.

Flower boxes now contain leaf blades and ivory/pink petal clusters, with slightly larger garden flowers and a limestone rim. Their previous round green/pink lumps are removed. Planter bodies retain their collision volumes; the decorative flowers remain non-solid.

Compare [previous village approach](unity-16/world-01/02-village-approach.png) with [revision 02](unity-17/world-02/02-village-approach.png), and the [revised garage](unity-17/world-02/03-garage-forecourt.png). The planting has finer silhouettes and the trees are visibly grounded. The [bridge comparison](unity-17/world-01/07-bridge-approach.png) retains the preceding coastal canopy character with lower geometry cost.

Against the [coastal concept](../references/concepts/coastal-world.png), planting moves toward 3/5. The scene as a whole remains 2–3/5: large flat frontages, oversized bare paving, repeated bench/lantern shapes, bare slopes and simplified distant hills are still conspicuous. Car masters and workshop detail are unchanged. This does not meet the 4/5-per-dimension visual gate.

## Build and checks

Revision 01 and 02 each compile successfully with 130 editor checks and no reported shader compilation errors. Revision 02 assembly SHA-256: `6819764A1AAB0B7BD97E6278C0ADCC42F6EDDC393CC0E813AF4D36FA05E076C8`.

[Revision 02 clearance](unity-17/world-02/clearance.json) passes 171 road, 11 garage, 27 forecourt and 1,710 road/shoulder support samples without obstruction. [Synthetic garage driving](unity-17/world-02/garage-driving.json) enters and reverses out with zero contacts/recoveries and 0.00887 m maximum lateral error. This does not establish physical DualSense or manual completion acceptance.

The final [24-car field](unity-17/field-02.json), seed 1721 over three laps, finishes all 24 cars with zero recovery resets or missed gates. [Ordered-gate validation](unity-17/field-02.validation.json) passes. Stock finishes 20th in 110.486763 seconds, matching the prior seed. The overlap audit again measures 0.0006485 m maximum car-box overlap and zero tagged environment overlap across 13,394 completed physics poses, with no saturated queries. The [moving race capture](unity-17/field-02/02-race.png) retains the leaf-rich silhouette; simpler distant canopies are somewhat coarser. This is sampled collision evidence, not universal visual-triangle separation.

The [continuous free-drive lap](unity-17/route-02/free-lap.json) completes 683.57 m in 42.675 seconds with zero contacts/recoveries. Maximum offset is 8.669 m, reaching the shoulder within the runoff. Inspected captures include [the inland bend](unity-17/route-02/free-110.png), [the return to town](unity-17/route-02/free-660.png) and [race traffic](unity-17/route-02/race-330.png). The sampled carriageway remains clear; no claim is made about every intervening rendered frame.

The subsequent [career race](unity-17/route-02/race-result.json) completes three laps, opens the actual result menu and persists 100 credits from zero. The isolated save records 21st place and matching race/settled serial 1. This exercises the real reward path with AI driving and does not substitute for manual finish/rejoin acceptance. All review processes exited. No normal player save was used.

Build evidence and runtime captures are under `unity-17`. This is an intermediate visual checkpoint with the limitations above, not final art/performance acceptance.

## Checkpoint 13

The frozen archive `outputs/little-road-unity-adventure-13.zip` contains 320 verified files, 134,430,736 bytes. SHA-256: `19D02C93CF1E86E7AE2898D011E05F1CE48C76C1B8B5FA43F3CF18FA0641037C`. [Every archive entry was hashed against the reviewed export](unity-17/package-manifest.json).

[Promotion](unity-17/promotion.json) stages and verifies those same 320 files before replacing the normal export. The previous checkpoint 12 export is preserved at `outputs/little-road-unity-windows-before-13`; earlier ZIPs remain unchanged. `Launch Little Road Unity.cmd` now opens checkpoint 13. A reusable promotion script refuses an active normal game, existing backup/stage paths, files escaping the export, changed source hashes or an archive that differs from its manifest.

Next priorities are the broad bare slopes and repeated roadside stonework, alongside the required car masters and atmosphere. Dedicated LOD-transition/rendering profiling remains open; this package is not a declaration that the visual or performance goal is complete.
