# Course defects and water refinement — 19 September 2026

This pass addresses the reported stretched grass and lighthouse driveway, improves tree placement on slopes, and reviews water from driving distance. It includes the preceding checkpoint/recovery corrections. It does not complete the Bayview fidelity milestone or the full game scope.

## Grass: measured source, rendered regression

The original ground-cover source mesh contains 402,768 vertices and 302,076 triangles with a maximum edge length of 0.353 m. No edge exceeds 2 m. After runtime static batching it referred to a different, 16-bit combined mesh. That combined mesh is CPU-unreadable: the initial audit's zero counts after batching are invalid measurements, not proof of an empty or valid mesh. See `unity-12/geometry-before` and `geometry-before.log`.

The grass and animated sea are now created after the static scenery batching step. Grass density and generated geometry are retained. The post-setup audit confirms the original readable UInt32 mesh, the same vertex/triangle counts and no long edges: [geometry evidence](unity-12/geometry-after/after-batching-Low%20coastal%20ground%20cover.json).

The corresponding rendered road views establish the useful outcome: the large triangles crossing the road in [the earlier free-110 view](evening-01/captures/free-110.png) are absent from [the new free-110 view](unity-12/route/free-110.png) and [the race view](unity-12/route/race-110.png). Free-055/165 and race-165 were also inspected. This strongly localizes the corruption to the old batching path without claiming an independently proven Unity index-overflow mechanism.

## Driveway and planting

The lighthouse access lane now starts at the outer recovery shoulder, 12 m from the route centre, rather than extending from the centreline with extra length. The [free-275 view](unity-12/route/free-275.png) shows the lane joining beside the road without a beige strip across the carriageway. The world check now examines its actual mesh vertices for shoulder intrusion, since collider-only checks could miss this visual defect.

Tree placement samples a ring around the root footprint and sinks the base to the lowest supported height. Free-110/165/385 and the village approach show improved slope contact. This is not an exhaustive acceptance of every tree: sparse vegetation, stiff trunks, uneven root/ground transitions and repeated silhouettes still need art work.

## First build evidence

Build 01 passed 129 editor checks. Gameplay assembly SHA-256: `DF39566C9BCA9B84A9E8B183A199EB77EFC0E795AFF34E7D069E1C70831F8A0C`.

- [World clearance](unity-12/world/clearance.json): 171 road, 11 garage, 27 forecourt and 1,710 road/support samples; no listed obstruction. These discrete samples are not universal no-clipping proof.
- [Physical garage test](unity-12/world/garage-driving.json): synthetic trigger entry and reverse exit, no contacts/recoveries, maximum lateral error 0.009 m. Physical DualSense acceptance remains open.
- [Continuous free lap](unity-12/route/free-lap.json): 683.74 m in 42.63 s, zero contacts/recoveries. Maximum offset 8.67 m exceeds the 8 m asphalt half-width, so this is a completed gentle tour, not proof of staying entirely on the asphalt.
- [Career result](unity-12/route/race-result.json) and [inspected result screen](unity-12/route/race-result.png): all 24 distinct entrants finished in the runtime log; stock player car finished last at 110.46 s. The actual result menu opened and 100 credits were saved. An external read of `route-save.json` confirmed credits 100 and raceSerial = settledSerial = 1.

The route was driven by the existing AI, not by manual steering. The user save was not used. This one race does not settle race balance, seed variance, overtaking or collision acceptance.

## Water iteration

Build 01 adds derivative filtering for waves smaller than a pixel, distance attenuation for fine ripples and filtered distant foam noise. It also keeps the animated water out of static batching. The near-water fixed-time test still showed motion (83.6% changed sampled pixels, mean channel difference 21.47/255). This is an animation check, not an art-quality score.

Inspection of free-440/550 showed remaining parallel reflection bands despite reduced fine noise. Build 02 therefore warps the broad wave pattern, adds a third direction and reduces distant broad-wave normal contrast. Two fixed-time driving-height coastal views were added to the world review to make this failure mode directly reviewable.

Build 02 passed 129 editor checks and the same world clearance and garage driving checks. Assembly SHA-256: `D3C512C234A0194247C996187FB6E64022E257871A6CF5FE608A720336F9E637`. The [distant coastal view](unity-12/world-02/11-distant-water-time-0.png) and its [second time sample](unity-12/world-02/12-distant-water-time-1-6.png) show substantially softer bands than build 01's road capture. The [close view](unity-12/world-02/09-water-time-0.png) retains ripples and intersection foam. The near-water motion check passes with 83.85% changed sampled pixels and mean channel difference 19.34/255. Regularity is reduced, not eliminated; sky-only reflection, simple foam and distant sea/sky blending remain intermediate art.

## Current visual assessment

Compared directly with `references/concepts/coastal-world.png`. These are qualitative judgments on the inspected gameplay views; no final visual acceptance is claimed.

| Dimension | Score | Evidence and remaining difference |
| --- | --- | --- |
| Car shape/materials | 3/5 | World-02 forecourt and workshop: rounded compact is recognisable, but glazing/interior, lamps and paint remain too simple; older resident/racer bodies are cruder |
| Environment/architecture | 2/5 | World-02 village approach and distant coast: recognisable shop forms, but bare hills and repeated pointed outcrops dominate; concept has layered coastal geology and a richer village |
| Road/edges | 3/5 | Route free-110/275 and race-110: corrected grass/lane, broad readable route; repetitive walls, uniform shoulder and surface need authored transitions |
| Lighting/palette/depth | 3/5 | World-02 forecourt and water: coherent daylight, improved sea, but dark foliage/car glass and weak distant land layering fall short |
| Composition/landmarks | 2/5 | World-02 distant coast: village is readable, but little layered architecture, horizon interest or connected cliff/bridge composition compared with the concept |
| Props and polish | 2/5 | World-02 workshop: shelves/tools exist but remain blocky and sparse; flowers/topiary are simple spheres and landscaping lacks variety |
| HUD | 2/5 | Route race result: functional readable text, but oversized plain panels remain a prototype treatment |

This does not replace the required view-by-view 4/5 acceptance review. None of these dimensions is accepted as final.

## Final candidate route and delivery

Build 02 completed another continuous free lap (683.74 m, 42.63 s, zero contacts/recoveries, maximum offset 8.67 m). The second full career race again finished all 24 distinct entrants; the stock player finished 24th in 109.99 s. The [result screen](unity-12/route-02/race-result.png), [runtime result](unity-12/route-02/race-result.json) and external read of `route-save-02.json` agree on 100 saved credits and raceSerial = settledSerial = 1. Both runtime review processes exited normally. Captures are in `unity-12/route-02`; free-110/275/550 confirm the grass/lane/water corrections on this exact candidate. The free-000 diagnostic frame occurs immediately after its initial placement, before the camera settles, and is not a normal chase composition.

This candidate is checkpoint 11. The package manifest records every shipped file hash and verifies the ZIP contents against the export. Earlier checkpoint ZIPs remain immutable. The normal launcher is updated through replacement of the mutable Windows export, preserving the user's persistent save location. Packaged `REVIEW.txt` records both implemented fixes and unaccepted requirements.

Packaging completed: `little-road-unity-adventure-11.zip`, 113,230,831 bytes, 320 files, SHA-256 `92DAC7E0F5C28C7B597BBC610B9BB8501245257988A2BE7A92B162598EC361C2`. Every promoted runtime file matched the tested candidate, and every archive entry matched the promoted export. See [package manifest](unity-12/package-manifest.json).

## Remaining work

The road defects are visibly improved, but the world remains below the coastal concept. Large bare slopes, repeated pointed outcrops, simple town trees, thin architectural detail and dark/simple vehicle materials are still prominent. The next fidelity pass must improve these forms and materials rather than treating a clean road as visual completion. Manual race finish/recovery, physical controller checks, broader traffic/contact tests and visible frame-time acceptance remain open. The 12-hour campaign, expanded equipment, visual parts browser, endurance/fuel/pits and body/decal catalogue remain in the full scope.
