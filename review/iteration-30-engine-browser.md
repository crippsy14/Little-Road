# Checkpoint 18 candidate — visual engine browser

21 September 2026. Delivered checkpoint 18 from `unity-23/build-05.log`. Final browser and connected career checks pass. `unity-23/package-manifest.json` and `promotion.json` verify all 320 archived/staged files. Archive size is 136,055,521 bytes; SHA-256 `48C28A3C4A0EB80E991D61831D76DDD6B2BF5BEB5062465BE24E6BEA998AF94F`. The desktop shortcut's normal export is updated; checkpoint 17 is preserved in `little-road-unity-windows-before-18`.

## Implemented

Harbour Garage's engine category now uses a category list, overlapping 3D part cards, larger selected engine, owned-car preview and setup comparison. The three engines are original Blender assets with visibly different intake arrangements. Stock, Workshop and Club presentation IDs use the existing equipment catalogue; tuning and prices are unchanged. Other categories remain available through the existing text menus.

Selection only previews. Buying spends credits and adds ownership; fitting is a separate free action and changes the physical car's tuning. The selection persists when backing out and returning. D-pad/left stick and arrows navigate; right stick and Q/E rotate; shoulders and brackets change categories. Mouse buttons remain available. Back returns to the garage before returning to the road.

The panel names the fitted and highlighted engines and compares the complete loadout: relative power index, modeled 0–30 mph acceleration and power/gearing-limited top speed. These use the actual force/tuning code, not a parallel balance table. They are labeled estimates. Fuel, range and lap-time predictions are not invented ahead of their systems.

The preview service uses one isolated camera with cached neighbour cards and whole-car image; selected geometry rerenders only on selection/rotation. Imported shared meshes are retained, while owned preview render textures and converted materials are released when closing. Render-target and displayed-image aspect ratios match to preserve model proportions.

## Review and regressions caught

The first runtime review caught Unity's default right-stick UI navigation competing with model rotation. UI navigation now disables those right-stick bindings while retaining left-stick, D-pad and keyboard controls. A second review caught selection disappearing when a purchased part disabled its Buy button; focus now advances to the separate Fit action. Shutdown also guards against the preview camera already being destroyed. These failed attempts remain in `unity-23/browser-01.log` and `browser-02.log`; they are not passing evidence.

Build 04 / `browser-03` passed 24 input, inventory, save and preview checks. Build 05 additionally corrects render/display aspect ratios and identifies the fitted engine in the comparison heading; final-build checks must be recorded separately below.

## Milestone assessment and next work

- **Visual quality:** actual 1920×1080 Unity captures show distinct engine shapes, readable labels and the confirmed original-inspired arrangement in Little Road colours. This is a first complete category, not final whole-game visual acceptance. Moving gameplay remains checkpoint 17's baseline.
- **Driving/racing:** no handling or rival balance changes. Garage throttle blocking and live equipment power updates are exercised. The final build passes the connected career/race test below; past race evidence is not relabeled as this build.
- **Progression/exploration:** the first paid engine provides a clear visible benefit while the stock gearbox still limits top speed. No new resident activity, map region or campaign duration is claimed. Automated travel between residents in the career smoke is relocated, not a first-time driven playthrough.
- **Usability:** keyboard and synthetic controller flow exercised. Physical DualSense USB/Bluetooth, normal play-distance feedback, broader aspect ratios and accessibility options remain open.
- **Performance:** idle preview rendering is cached. This is not a measured visible-window 60-fps or full garage memory/performance acceptance claim.
- **Production readiness:** three engine masters and exports, a working store-backed browser, and an isolated regression entry point exist. The renderer/presentation mapping must be generalised and proven with a second category before claiming a scalable complete catalogue. Other six current categories, filters, saved loadouts, appearance editing, fuel/tyre presentation and the expanded parts catalogue remain unfinished.

Next highest-value work: extend the same browser to tyres, proving useful grip/surface comparisons and variant silhouettes, then cover the remaining existing slots. Keep the new Le Mans/F1 reference-led roster queued for its later production phase; avoid returning to minor compact polish. Future development presets/credits/teleports and permission to reset development saves are recorded in document 17.

## Final-build evidence

Gameplay assembly SHA-256: `D64255F3E9176D6523393B55FC289AAD3D366DA45E0E8934E4D3370E5819C6CE`.

- `build-05.log` and `editor-checks.json`: successful export and all existing 177 editor/import checks.
- `browser-final/parts-browser-review.json` and `browser-final.log`: 24 checks pass on the final assembly, including a separate controller press to fit after buying, free keyboard refitting, reload, remembered selection and right-stick rotation without navigation. Preview/cancel leaves both live inventory and disk bytes unchanged. A timed idle interval adds no preview renders.
- Six actual Unity captures in `browser-final`; selected affordable Workshop, owned-but-unfitted Workshop and Club comparison visually inspected. The stock setup estimates 0–30 mph in 2.70 s; changing only the engine to Workshop estimates 2.08 s. Workshop and Club both reach the stock gearbox ceiling of about 55.9 mph. These numbers illustrate the model, not observed player lap times.
- `career-final.log`: connected adventure smoke passes real delivery/collectible transactions, purchase/fitting through this browser, synthetic free driving, a full career race, result menu, saved payout, reload and duplicate reward rejection. Resident-to-resident travel in this test uses relocation.
- `career-final-race.json` and `.validation.json`: all 24 cars finish three ordered laps, no recovery resets or missed gates; player with Workshop engine and other stock parts finishes 24th in 110.970 s. This is not a matched-seed stock/upgrade comparison and does not prove a satisfying first-upgrade progression by itself. Keep that balance question in the next matched progression review.
- `career-final-progress.json`: quest stage 3, two finds, Workshop fitted, 100 credits, race/settlement serial 1. The result screenshot displays the saved 100-credit award.
- 13,318 completed-physics-pose samples, zero query saturation, maximum car-box overlap 0.9003 mm and zero tagged scenery overlap. This is sampled collider evidence, not proof against every possible visual contact issue.
- Hidden/capped race timings average 8.343 ms, p95 8.334 ms; they do not establish visible rendering performance. The existing world/garage clearance evidence is from checkpoint 17, unchanged by this UI work, and was not rerun.

Final test processes exited normally. No production save was reset or used by these tests. The overall campaign goal remains active and incomplete.

## Subsequent user steering

The user requested controlled loadout test courses and using their measurements to design faster-class professional-style tracks. [Document 24](../docs/24-equipment-performance-test-courses.md) records the matrix, telemetry, control/AI distinction, real grade-support gap and track-design feedback loop. Prioritise the dry straight/U-turn runner alongside the next parts category; the fixture suite is planned, not part of checkpoint 18. The previous and current goal turns made concrete progress: engine assets/browser, tested/published build, and this new scoped validation requirement.

