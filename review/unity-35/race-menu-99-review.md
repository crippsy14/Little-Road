# Illustrated race-day menus and interactive paddock, 99

23 September 2026. This is a bounded expansion of the user's requested playful UI milestone, following the accepted transparent driving HUD. The user rejected 96's plain cream/teal direction as insufficiently illustrated, then requested animated car/parts inspection, a driver graph, PP comparisons and a moving grid. The final direction is implemented in 99; user acceptance and final connected evidence are recorded below when available. The full campaign goal remains active.

## Implemented experience

The actual 24-entry grid keeps immutable entrant equipment, PP and driver snapshots. An original illustrated coastal background, paper/tape details and stronger selection styling replace the original block presentation. PP below the player's rating is green with a downward arrow; above is red with an upward arrow; equal is neutral. A textual comparison accompanies the detail rating. The native text and 3D car remain separate from decorative art.

The car rotates on a display platform. Square/I or Explore Paddock opens a separate read-only inspector: larger rotating car, orbital group of all seven fitted 3D parts, selectable number buttons and model picking, enlarged selected part/hint, and the player's corresponding part for comparison. L1/R1 browse parts; right stick turns the car; Circle/Escape returns to the grid without cancelling entry. Pointer entry or selection pauses the orbit, and explicit pause/resume controls let inspection remain stable. An opt-in grid tour moves/fades rows and cycles pages; browsing stops it.

Rival charts show implemented risk, base brake commitment and consistency. Larger chart area is not a universal better-driver score, especially for risk. Human players receive an explanation of their own control, not an invented AI personality. Future driving skills remain on the existing plan.

Results show the player's fixed finish time, authoritative place, saved reward and wallet with readable return/retry controls. The old post-finish HUD clock is frozen and the redundant bars are hidden on the result screen. Generic adventure choices reuse the high-contrast button treatment, but dialogue/garage/exploration have not yet received a complete visual redesign.

## Models and original artwork

The imported engine models are retained. New original procedural display meshes cover turbo/compressor, tyre/wheel, gearbox, brake rotor/caliper, steering rack and chassis frame. They use actual fitted part IDs and category shapes, with current tier cues mainly in finish/colour. They are stylised inspection models, not full mechanically detailed replicas or wholly unique geometry for every upgrade. Their generated meshes/materials and render textures are explicitly released when switching entrants/closing preview.

The coastal postcard was made with the built-in image-generation tool and saved in unity/Assets/Resources/UI/coastal-race-postcard-v1.png. Exact prompt/provenance: references/ui/coastal-race-postcard-v1.md. It is decorative art, not a screenshot or evidence of implemented world fidelity.

## Verified so far

- Nondevelopment Windows build, 498 editor checks, 162 frozen source/script inputs.
- 212 runtime preview checks pass: actual seven-part loadouts and tuning/driver consistency for all24 entrants, pause behaviour, car rotation without race-time advancement, Square/Circle entry/return, seven part-button selections with motion pause, R1 wrap, cyclic grid tour and original page/confirm controls.
- Controlled Bayview race after preview exactly matches97 recorded motion, gates, contacts/recoveries, context samples and results.97 matched96, and96 matched the prior88 preview fixture. This is not an all-scenarios driving proof.
- Actual 1080p player/rival inspector captures inspected. Car/platform, all seven models, selected-part detail and the rival radar are visible without clipping. Human driver graph is correctly absent. Illustration composites behind the real car with transparent rendering. Physical mouse picking and DualSense acceptance remain distinct from synthetic/button-action checks; smaller windows and motion comfort are still open.

## Iteration history

96 passed200 preview checks and the connected workflow17/18; the known recovery defect was retained. Player outcomes:17th/93.091011s and16th/92.782753s, two130-credit rewards, wallet260 persisted. It was visually superseded at the user's request.

97 passed200 preview checks and controlled parity, with inspected illustration/alpha. No connected run was claimed before the interactive expansion.

98 passed the build but its first preview failed on missing turbo_club: previously only engines had authored display assets. It failed before the review exception hook was installed and was explicitly stopped. Its failed log/exit are retained.99 adds all six category display-model builders plus an earlier scoped diagnostic exception hook. Do not present98 as a successful runtime candidate.

## Next priority and remaining scope

After this UI pass the user explicitly prioritises choppiness/performance, before further UI/content expansion. Use normal-speed rendering evidence and measured expensive phases; preserve racing through controlled comparisons and moving review. Previous clean95 pair still fails frame/AI budgets. This hidden menu/integration work is not fresh performance acceptance, including the additional offscreen renders during paused inspection.

Do not lose the full UI scope, endurance/fuel/tyres/pit strategy, teammates, progression, world/parts/bodies/content or the12-hour first-playthrough goal. No desktop shortcut promotion or production-readiness claim. Connected recovery stalls, physical controller/accessibility and campaign duration remain open.

## Final connected evidence and user direction

99 connected session and new-process reload exited normally. 17/18 integration checks passed; race2NoTeleportRecovery remains false (known recovery limitation). Both24-car three-lap races finished: player23rd/95.132179s and16th/92.807747s;100+130 credits persisted as230. This is functional evidence, not a race parity or performance claim.

The user requested further playfulness and specifically a genuine dark, lit 3D showroom instead of the painted backdrop behind the car.99 is a retained intermediate, not visual acceptance. Next bounded UI pass implements that request; measured performance remains next after the UI pass.
