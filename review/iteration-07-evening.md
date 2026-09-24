# Evening checkpoint — 17 September 2026

The user requested a written handoff, a couple of model/world passes toward the concepts, then an evening stopping point. The overall game and visual target remain unfinished.

## Changes and screenshot review

**First pass:** rounded compact body edges and added original utility, low coupe and open-wheel studies in editable Blender files. The C field now mixes compact, utility and coupe bodies. Added coastal cypresses, small shoulder plants/rocks, terrain surface variation and clouds.

**Second pass after viewing actual renders:** moved rear lights out of intersecting bodywork, replaced solid cloud blobs with softer procedural cloud cards, and tightened the body-gallery camera. Replaced the expensive render-mesh collision hull input with a 48-vertex outer hull per model. These are constructed from 26 supporting planes enclosing the visible meshes, including front steering extremes. Full collision/recovery acceptance is still outstanding.

Actual source-build captures: [village](captures/evening-07-final/01-village.png), [coastal road](captures/evening-07-final/02-coast.png), [compact close view](captures/evening-07-final/03-car.png), [grid](captures/evening-07-final/04-grid.png), [race](captures/evening-07-final/05-racing.png), [seven-slot garage](captures/evening-07-final/06-garage.png), [bridge](captures/evening-07-final/07-bridge.png), [workshop](captures/evening-07-final/08-workshop.png), [body studies](captures/evening-07-final/09-body-gallery.png). Body studies are a rendered asset gallery, not a new race class or a player body-selection screen.

Compared with [the coastal concept](../references/concepts/coastal-world.png), the car curves, silhouette variety and landscape dressing move in the intended direction. The world remains visibly simpler and more sparse: repeated vegetation, a basic horizon, smooth hills instead of a convincing rocky coastal composition, simple bridge masonry, limited materials and a boxier car cabin. The lighting lacks the concept's depth. Clouds are an intermediate stylised treatment. These are incremental passes, not a match to the reference.

| Visual dimension | Current / 5 | Remaining gap |
| --- | --- | --- |
| Car silhouette and finish | 2 | Rounded surfaces improve; glass, cabin, wheels and detailed material separation need more work |
| Architecture | 3 | Workshop identity is readable; repeated buildings and bridge need richer authored detail |
| Terrain and coastal composition | 2 | More surface variation; cliffs, distant land and coast arrangement remain basic |
| Vegetation and dressing | 2 | More silhouettes and shoulder detail; still visibly procedural and sparse |
| Lighting and materials | 2 | Better sky interest; reference warmth, depth and surface richness remain missing |
| HUD and menus | 3 | Legible seven-slot garage; information hierarchy and presentation still provisional |

Target remains at least 4 in every dimension, with repeated gameplay screenshot review.

## Handling and parts

Implemented torque-limited launch, power-limited high-speed acceleration, rolling resistance, quadratic drag, slope load and shared cornering/acceleration/braking traction. Gearing sets the theoretical ceiling. Engine and turbo power decide the available pulling force; tyres constrain its use in bends. AI drives through the same controller. Garage shows estimated level-road speed separately from gearing speed. Street/Club turbos add a seventh functional slot and two purchases, for fourteen purchasable upgrades total. Save version three migrates older progress.

[Power and grip results](power-trials.json): **9/9 checks pass**, including engine/gear interaction, turbo acceleration, tyre-limited corner exits, hill load, purchase/fitting and version-two migration. These are model-level checks of production handling code; they are not physical track trials. Stock reaches 20 m/s in about 4.85 seconds in the ideal flat calculation. Tall gearing alone is slower to accelerate and still settles near 24 m/s. Strong engine plus tall gearing reaches 20 m/s in about 2.97 seconds; adding Club turbo reduces that to about 2.33 seconds. These initial numbers still need the user's desired slower progression and physical lap checks.

**38/38 functional checks pass on the final source**, covering progression, saving, actual-car road driving, 24 entrants, gate integrity, retry and ongoing rivals behind the results screen. These do not establish full-field balance or absence of clipping.

The standalone Windows candidate exported successfully and captured all eight gameplay/menu views from its embedded resources, including the revised hull data and garage. [Standalone race screenshot](captures/evening-07-export/05-racing.png). This is a launch/render smoke test, not a complete human playthrough or 60-fps measurement. The downloadable build is labelled an evening development checkpoint.

A separate full 24-car headless trial was stopped at the requested evening checkpoint after a slow run, before it produced a completed result. [Attempt record](evening-field-attempt.json). No current-code finish count, placing or balance pass is claimed. Profile the full-field runtime next session; this also limits confidence in the current development build's racing performance. The export SHA-256 is `C66862F56035CEA3849A162FC99A24A47D79CC5F81C411F0A1219CBEB0B7FFEE`.

## Next session

The priority is a wider authored race circuit, usable passing lines, reliable off-course rejoining and slower, more differentiated acceleration. The current narrow village route remains unchanged. Rerun solo/equipment, two-/three-car overtaking, crash-cost, blocked-bend and 24-car matrices after those changes. Earlier narrow-course balance results are historical diagnostics, not acceptance of the new physics. The developed build was previously losing much of its solo advantage in traffic.

Continue the six-body initial roster and broader catalogue, later classes/tracks, model collision checks, DualSense hardware checks, audio and race performance profiling. The full details and decisions are preserved in [the handling handoff](../docs/14-power-grip-and-evening-handoff.md).
