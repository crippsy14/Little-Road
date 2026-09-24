# Reference-driver braking and hill readiness

21 September 2026. Follows checkpoint 19's equipment proving grounds. This pass changes the controlled measurement driver, not campaign AI skills or car/part tuning.

## Change and investigation

`PerformanceCourse` now works backwards through upcoming corner limits, querying `DriveModel.Forces` to find the fitted brakes' usable deceleration after cornering traction. It retains a reaction margin and conservative corner limits. This replaces one fixed fraction of tyre grip that capped braking even on a straight. A continuous pedal regulator replaces alternating full braking/positive throttle around the production racing driver's overspeed threshold. The fixture records `fixed reference policy v2`; geometry stays `dry-proving-grounds-v1`. Actual forces, steering torque, contacts and equipment definitions are unchanged.

The initial hypothesis that sport brakes would save over 0.05 seconds with the stock engine and sport tyres was falsified. `pilot-01` (new braking envelope, old pedal regulator) produced equal 22.2333-second runs. A prematurely started larger `matrix-01` was stopped and remains explicitly incomplete. A matched faster setup in `pilot-02` showed 0.0333 seconds of benefit, all in the braking approach. `pilot-03` with the smoother regulator showed 0.0417 seconds at higher power, but still equal low-power totals. No equipment values, track geometry or finishing times were changed to make an upgrade win.

The final matrix therefore includes twelve setups: the original ten plus matched club-engine/long-gearing/sport-tyre cars with stock versus sport brakes. Acceptance distinguishes physical stopping benefit, low-power non-regression and a faster-car effect exceeding three physics steps (0.025 seconds). This is a detection threshold, not a claim that the upgrade's price or race benefit is satisfying. The earlier >0.05-second low-power hypothesis remains recorded as false in the report. The small observed gain needs later multi-zone track design and human balance feedback.

`tools/compare-performance-batches.py` compares matching fixture/configuration/repeat identities across builds. It refuses incomplete or duplicate evidence, requires all supplied baseline cases to remain present, and checks unchanged straight/brake controls. New cases without earlier data are listed explicitly and receive no before/after claim.

## Evidence

Final candidate source build: [build-02.log](unity-25/build-02.log), 177 editor/import checks. Assembly-CSharp SHA256: `5D64648D65E867B3AC014B61EDB6F4FC6DEF38C99A1DD62F3442495094C51073`.

Pilot artifacts are retained under `unity-25/pilot-01`, `pilot-02` and `pilot-03`. The original checkpoint 19 executable supplies `baseline-fast` to complement its original 90-run matrix. This gives an actual before/after comparison for the faster U-turn setups, not a reconstructed simulation.

The normal [24-car Bayview run](unity-25/field-final.json) completes three ordered laps for every racer, with the stock player 24th in 113.3361 seconds. No recoveries or missed gates; maximum sampled car-box overlap 0.8774 mm, tagged-scenery overlap zero. The [saved vertical audit](unity-25/field-final/vertical-race.json) passes: zero airborne steps above its 0.5 m threshold, maximum upward speed 0.00324 m/s and maximum collider-bottom height 0.02501 m above the flat datum. This is flat Bayview evidence only. Physical controller, manual completion/rejoin and normal-window performance remain open.

## Milestone review and next work

Visuals remain unchanged in Bayview. The plain proving-ground apex capture was inspected for readable placement and road clearance; it does not establish campaign fidelity or moving-gameplay acceptance. Driving measurements now use fitted braking limits and smooth pedal requests. Production race behaviour retains its matched full-field result. Progression has stronger measurement evidence, but no economy or campaign unlock changes. Exploration and meaningful campaign duration are unchanged. Reports, version identifiers and preserved failure evidence improve repeatability; visible/manual proving-ground modes still need human acceptance. Accelerated hidden runs do not measure normal rendering performance.

Next implement the surveyed hill and combined course through actual physical ground/grade support. The [grade-support audit](../docs/24-equipment-performance-test-courses.md#grade-support-implementation-boundary) covers horizontal curvature versus grade, pitch/roll constraints, local support-relative spawning/clearance/recovery and avoiding double gravity. Preserve the dry controls and current ramp-regression evidence. Do not spend more iterations trying to manufacture a large low-power brake benefit on this single bend. The tyre browser and wider campaign work remain queued; future AI skills use [doc 20](../docs/20-driver-personality-and-teammate-progression.md).

## Validated development status

The [final 108-run matrix](unity-25/matrix-02/report.md) passes all 129 checks. The [before/after comparison](unity-25/matrix-02/baseline-comparison.md) passes 157 checks over 96 matched runs; twelve new straight/brake cases have no earlier baseline and receive no before/after claim. Stock U-turn time improves from 24.1583 to 23.6167 seconds and full club from 17.6667 to 16.8333 seconds. The matched faster-car sport-brake gain is 0.0417 seconds; the stock-engine gain remains zero at whole-run timing resolution. The graph and raw evidence remain alongside the reports.

This validated change is in `little-road-braking-review`; the normal desktop build remains checkpoint 19. Keep this candidate separate while the hill/combined work proceeds rather than treating a measurement-driver adjustment as a new campaign-content release. The broad goal remains active.

## Visible-demo correction

The user reported that the initial `user-visible-01` windows did not appear. A process/window handle alone was insufficient evidence of user visibility. Relaunched `user-visible-02` outside the restricted shell with `-Visible`, three stock then three club runs at normal time (PID 16568). Computer Use found the unique game window, activated it and captured the live stock run at 12.75 seconds on the user's desktop. Use that launch/activation verification for subsequent requested visible demos. Do not infer controller or usability acceptance from this screenshot. Normal-time hidden stock/club checks also matched the accelerated final matrix in `timescale-comparison.json`.
