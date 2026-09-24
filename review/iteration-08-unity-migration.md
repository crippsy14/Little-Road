# Iteration 08 — first Unity Windows trial

17 September 2026. This is migration evidence, not completion of Little Road or replacement of the preserved Godot adventure build.

## Outcome and scope

Unity 6000.6.1f1 / URP 17.6.0 compiles the C# migration and exports a Windows development player. The circuit is 683.50m long with a 16m racing surface and driveable runoff. The final trial completed three laps with all 24 physical cars, fixed equipment and no recovery resets. The stock player car placed 20th. Equipment effects are demonstrated in matched-seed solo runs, but broad balance, natural result variation, overtaking reliability and safe collision behaviour are not yet proven.

The scene is deliberately a driving trial: simple flat island, pit-village placeholders, imported original prototype cars and temporary HUD. The Godot adventure, economy, saved progression, residents, delivery, collectibles, workshop interactions and detailed village/bridge remain preserved separately and await migration. The ~100-body ambition remains in document 15; no polished master bodies have been completed here.

## Test evidence

| Evidence | Result | What it establishes |
| --- | --- | --- |
| `unity-01/catalogue-port-check.json` | 21 parts, 14 purchases, seven slots, all 121 source fields match | Stable IDs/prices/tuning/text survived data transfer |
| `unity-01/build-01.log` | Failed: missing `Texture2D.EncodeToPNG` | Template lacked explicit image-conversion module; dependency added |
| `unity-01/build-02.log`, `build-03.log` | Successful Windows exports | C# compilation and player export work on installed toolchain |
| `unity-01/editor-checks.json` | 62 checks passed | URP assigned, catalogue loadouts valid, power/grip model checks, track projections and four FBX metre-scale bounds |
| `unity-01/field-first.json` | 20/24 completed one lap within 90s; stock only progressed 57m with three resets | Initial build had serious handling/import problems; this is failure evidence |
| `unity-01/solo-stock.json` | One lap 37.633s, no contacts/reset; seed 1709 | Actual physical stock car can complete the trial circuit |
| `unity-01/solo-club.json` | One lap 28.116s, no contacts/reset; same seed/grid | Club setup is about 25.3% quicker in this paired run; not a full single-part or repeated-seed matrix |
| `unity-01/field-revised.json` | 24/24 completed three laps; winner 81.770s; stock 20th, 108.659s; last 143.542s; zero resets | One complete full-field run on corrected build, including delayed/off-course rivals |

Solo tests ran concurrently as independent processes, so their frame-time samples are not GPU benchmarks. Field tests ran offscreen with explicit camera captures. No result establishes visible 1080p/60fps performance or hardware controller feel. The actual game code and identical test seed, rather than artificial lap-time jitter, produced the timing difference.

## Changes supported by the runs

- FBX imported the nose along Unity -Z, while the controller drives +Z. Corrected the visual root by 180 degrees; subsequent captures show the rear in chase view.
- Prototype window polygons were invisible from their outward side in the FBX port. Made their URP glass material double-sided, retaining the former glTF appearance. This is an import fix, not new interior/glass modelling.
- Replaced a fixed collision box with per-body renderer bounds and a conservative steering-width allowance. This accommodates the tall utility's load and the shorter coupe, but remains a coarse contact model.
- Removed physical contact friction from the sliding body box because custom tyre forces already include resistance. The original default-ground combination added an unintended brake, particularly hurting stock. Corrected runs show stock can circulate, but several fixes changed together, so no isolated friction-only causal measurement is claimed.
- Kept the flattened island's collision on its static mesh instead of the primitive cylinder's unsuitable scaled capsule.
- Disabled stripping of post-processing resources used by the runtime-generated volume; the repeated missing-post-process shader warnings are absent in the corrected run.
- Added per-second motion telemetry for speed, target speed, throttle, steering, track offset, heading error and progress, in addition to contact/recovery/finish summaries.
- Finishers continue circulating so they do not park across the timing line.

## Visual comparison against the unchanged concepts

Inspected `references/concepts/coastal-world.png`, `vehicle-equipment.png`, initial `unity-01/captures-first/01-grid.png` and `02-race.png`, then corrected `captures-solo-stock/02-race.png` and `captures-field-revised/02-race.png`.

The corrected captures establish proper orientation, visible glazing, grounded car silhouettes and working road/kerb rendering. The broader surface visibly provides room for different lateral positions. That is useful migration evidence, not visual parity.

The coast reference has terraced terrain, rocky shores, an arched bridge, a lighthouse, layered distant scenery, detailed warm-plaster buildings, readable signage, flowers and an inhabited garage. The Unity trial currently lacks these compositions and details. Flat ground, sparse elongated sphere trees, block buildings and uniform sea/sky are obvious placeholders. Lighting/material richness, camera framing and car size in the composition need further work.

The vehicle reference has rounded wheel arches, coherent body panels, inset glazing/pillars, detailed lamps, tyre profiles, interior silhouettes and believable manufactured joints. The current inherited bodies have coarse surfaces, rough normals and related silhouettes; tinting and import fixes do not meet this target. Rebuild the compact, sports coupe and open-wheel master designs before scaling variants.

No visual gate is marked passed. The gameplay captures exclude HUD; interface readability needs a full-window capture and human review.

## Unresolved failures and next experiments

Eight rivals exceeded the nominal road half-width during their race. The largest measured lateral excursion was **28.79m** (car 15), with several seconds beyond the runoff. Cars 9, 15 and 17 finished much later than the main field. Zero resets therefore does not mean clean or reliable driving. Inspect collision-induced yaw, lane changes through bends, tyre-budget allocation during off-road braking, and ordered-gate progression after an excursion. Missing a gate may require another circuit under current logic; diagnose and implement safe rejoining behind missed gates without skipping race progress.

The stock car had 23 contact entries in the full race. Contact counts are not unique crashes or collision-severity proof; a sustained bump may produce several entries. Add timestamped impact evidence and matched clean/impact runs. Check penetration, steering envelopes and attachments visually and numerically before claiming no clipping.

Add a single-file starting-grid option for the controlled two-/three-car tests: the existing 3-wide grid starts those fields abreast, so it cannot prove a faster car passed from behind. Repeat solo seeds and equal-equipment fields to assess natural result variation independently of equipment and grid.

Validate wheel rotation/steer axes, player input, gamepad reconnect, reverse/recovery and camera obstruction. The live placing display also needs to keep finish-order positions once cars have finished instead of relying on circulating track progress. Preserve the finish-order report as authoritative.

Do not tune the economy against this one race or claim the intended grind is balanced. All purchased part families, tracks/classes, progression persistence, audio, controller menus and final visual improvements remain on the backlog.

## Export identity

Build 03 output: `outputs/little-road-unity-windows/Little Road Unity.exe` plus its neighbouring runtime files.

- Executable SHA256: `61695BA6E3D61056DB5F10A955B41F680E78F85626FFC909D4E7A93ECB20DF9E`.
- Gameplay assembly SHA256: `BDEA0DD19C20124CB3038286CFAE24C262F2A385FC3999FA60866CCB392F0CEB`.

Both files matter: Unity's player executable may stay identical while the game assembly changes. The previous Godot executable, ZIP and source checkpoint were not overwritten. No owned test/editor processes remained running when this review was written.

The trial runtime is packaged in `outputs/little-road-unity-trial.zip` (73,505,447 bytes), excluding Unity's `BackUpThisFolder_ButDontShipItWithYourGame` development backup. The ZIP includes the executable, game assembly, runtime dependencies and the trial control/limitations README.
