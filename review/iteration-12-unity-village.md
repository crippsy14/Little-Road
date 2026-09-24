# Iteration 12 — Unity village and open workshop

17 September 2026. The previous goal turn made progress by opening and inspecting the requested visible 24-car test. This iteration replaces Unity's box village with original runtime architecture and checks its actual rendered appearance and access. The full goal remains unfinished.

## Implemented world changes

`VillageKit.cs` builds Harbour Garage and six shop/house frontages from original reusable geometry. The garage has separate walls and an open bay, a ceiling and rafters, service-bay markings, workbench, tool board, shelving, parts boxes, spare wheels and a warm light. Pitched roofs have individual clay tiles and fascia; frontages have glazing, mullions, shutters, shop signs, awnings and chimneys. Planters, benches, clustered olive trees and lamps dress the forecourt. Shared material palettes and static batching combine the architectural rendering. Surface grain is authored in code; no third-party art has been introduced.

Adventure roads now omit the racing kerbs and timing gantry. The race still has its 16-metre road, kerbs and start line. Cool fill light supplements the existing sun so building shadows retain some colour. Resident cars are grounded from their rendered bounds instead of a fixed vertical offset. Free-driving recovery also catches a car falling below the island.

This is a first architecture pass, not concept parity. It does not add the reference's cliffs, bridge, hills, dense landscaping, textured stonework, varied roof forms or distant village. The broad island and sea remain primitive surfaces. The compact and older resident cars retain the previously recorded model limitations.

## Review and fixes

The standalone `--world-review` mode captures the actual adventure scene at six explicit camera poses. It includes a default HUD view, village approach, garage forecourt, workshop interior, wide village and coast. The review moves the player between poses and does not substitute scenery. Garage poses were corrected after build 01 because a lamp obscured the entrance and the interior camera missed much of the workbench; build 02 onward uses the revised poses. These are not pixel-aligned before/after comparisons to build 01.

Build 01's static clearance samples exposed lighthouse geometry intruding into the road at approximately 312–324 metres. Its old world-Z placement offset was replaced by an offset away from the nearest road point. Build 02's 171 road samples and 11 garage samples passed. These finite overlap samples are not a continuous no-clipping proof.

Build 03 added a physical garage test using synthetic gamepad trigger input. The car drove from z=-98 to -107.01, then reversed out to -97.99, with zero contacts and recoveries and 5.6cm maximum lateral drift. Its adventure regression exposed a separate forecourt obstruction during the steering segment. Lamps originally placed through the forecourt were moved to the rear edge between buildings. Build 04 extends the static checks to 27 forecourt samples and makes any contact during the synthetic resident-approach drive a regression failure.

The user-requested visible race ran build 01 with seed 1721. It finished 24/24 and passed sampled ordered-gate checks, but needed one recovery and recorded approximately 12cm of contact penetration. The visual launch is not evidence of accepted racing balance or no-clipping. See `unity-05/visible-watch-20260917-191529.json` and its validation file.

## Visual assessment against document 07

The final captures are in `unity-05/captures-world-04`, with the same authored poses as build 03 and the corrected lamps/ground placement. The final workshop, forecourt, dialogue and race captures were inspected. Scores below are visual judgments. The full visual gate still fails.

| Dimension | Village / garage score | Evidence and remaining difference |
| --- | --- | --- |
| Car quality | 2/5 | Compact silhouette is readable; cabin, lamps and resident prototype models remain crude compared with the reference |
| Architecture and materials | 3/5 | Open workshop and detailed frontages now read as a village; hard edges, repeated forms and basic surfaces remain conspicuous |
| Roadside detail | 2/5 | Paving, markings and furnishings support the frontage; broad bare verges and missing coastal retaining walls dominate the rest of the loop |
| Lighting and atmosphere | 2/5 | Interior is visible and sun/fill separate forms; flat horizon, limited shadow/contact detail and absent landscape layers still weaken the scene |
| Composition and exploration | 2/5 | Garage is a readable destination; houses remain in a sparse straight row on a flat island |
| Prop density and polish | 3/5 | Tools, shelves, planters and street furniture are present; geometry and placement need richer variation and refinement |
| HUD | 3/5 | Default chase capture has clear objective/currency text; large strips and floating resident labels still need polish |

The coastal view remains around 1/5 for landscape, exploration composition and prop density, with 2/5 road treatment and lighting: the reference's landforms and landmark composition are absent. The required race and close-up views retain their earlier outstanding issues; this iteration does not claim a fresh five-view acceptance pass. The next major environment task is a coherent coastal landform/road-edge/bridge composition around the actual route, followed by another full review set.

## Verification status

Build 04 compiled and exported with 102 passing editor checks (`build-04.log`, `editor-checks-build-04.json`). All 171 road, 11 garage and 27 forecourt overlap samples passed. The physical garage entry/reverse-exit check passed again with zero contacts/recoveries and 5.6cm maximum lateral drift (`captures-world-04/clearance.json` and `garage-driving.json`). The synthetic forecourt drive recorded zero contacts and an 8.74-degree steering change; navigation, confirmation and menu throttle blocking passed.

The exported adventure regression then completed the delivery, two finds, Workshop engine purchase/fitting, a real 24-car three-lap race, payout and return to town. All 24 finished with zero recovery resets and missed gates. The Workshop-equipped player placed 23rd in 106.442s and retained 100 credits. Maximum contact penetration was 7.95cm, so the no-clipping requirement still fails. These results are an integration regression, not accepted equipment/AI balance. See `smoke-04.log`, `smoke-04-result.json`, `smoke-04-race.json` and its validation file. The smoke test still relocates between adventure objectives.

A fresh process loaded that profile, verified the completed quest, two finds, owned/fitted engine, settled race and credits, then rejected duplicate awards (`restart-04.log`, `restart-04-result.json`). No ordinary player save was used by these tests.

The final gameplay assembly SHA256 is `49733EF16CDFFD150060C46F21A3FCA322F2D8D5A9875BD84EF8AE3655AED110`. The Windows checkpoint is `outputs/little-road-unity-adventure-05.zip`; its archive hash and per-file verification are recorded in `unity-05/package-manifest.json`. Previous frozen packages remain unchanged. Background runtime timings are not a visible 1080p/60fps acceptance measurement.

Physical DualSense, a continuously driven manual adventure, visible 1080p/60fps profiling, collision penetration/rejoining improvements, audio, expanded parts and traversal, track/class unlocks, three polished master bodies, decal production and the approximately 100-body catalogue remain open. No goal-completion claim is made.
