# Local traffic planning: following and first passes

21 September 2026. Bounded AI development milestone; the campaign goal remains active and the desktop adventure remains checkpoint 20.

## Candidate and reproduction

Frozen executable: `../../little-road-traffic-04-review/Little Road Unity.exe`.
Managed assembly SHA256: `C420AC2385155EDAF60B8F21E3705ABF000E0522CE80804849EBB8101CD81C66`.
`Watch Traffic Passing.cmd` opens the three-car queue at normal playback speed. It starts partway around Cedar, then closes once the field finishes. These partial-course times are not flying lap times or campaign race results.

Build evidence: [239 editor checks](unity-32/editor-checks-04.json). Runtime evidence: [manifest with all eight normal process exits](unity-32/matrix-04/manifest.json), [105/105 checks](unity-32/matrix-04/analysis.json). Reproduce with `tools/run-traffic-fixtures.ps1 -Executable <frozen exe> -OutputDirectory <new folder> -IncludeQueue`, then `tools/analyze-traffic-fixtures.py <folder>`.

Regression evidence: [15/15 exact comparisons](unity-32/regressions-04.json) preserve the three-lap club reference and existing three-lap 24-car stock race, including timestamped motion/inputs and outcomes. [Both control processes exit zero](unity-32/regression-exits-04.json). This does not claim that the new traffic planner has passed a 24-car race.

## Behaviour implemented

`TrafficDriverPolicy` plans at 10 Hz from the common observed state. It evaluates smooth forward paths toward the racing line and passing offsets, braking-limited speed profiles, actual car box dimensions, static swept clearance, road width and projected opponent occupancy. The existing vehicle physics apply the steering and signed pedal commands. No extra grip, power, forced overtaking position or opponent-controller access is introduced.

A closing-speed advantage allows a passing attempt; side commitment reduces indecision, and a following path remains available when a pass loses its advantage or clearance. A clear track delegates to the frozen solo driver. Physical recovery still handles wrong heading, departures and persistent failure to make progress; an ordinary car ahead no longer immediately triggers recovery.

Immediate prediction uses observed velocity; longer prediction follows the known racing line with observed lateral error/movement. A rear car approaching an unchanged lane must manage its following distance: the front driver does not brake merely because a long-range prediction places that rear car inside it. Immediate occupancy and lane-change checks remain active. This is a limited prediction model, not knowledge of opponents' future inputs.

Observation projection is cached once per planning cycle rather than repeated for every path sample. Planning and rendering performance have not yet been profiled adequately for a 24-car field.

## Results

All eight final scenarios finish with zero reported contacts, assisted resets, physical recovery episodes, missed gates, sampled body departures, collision-query saturations or unexplained stops. Pose overlap stays below the 2 cm acceptance threshold. The lead car does not unnecessarily brake on the initial clear straight.

| Fixture | Observed outcome |
| --- | --- |
| Equal stock equipment, 24 m spacing | Maintains order; follower finishes 1.142 s behind. |
| Club behind stock, open straight | Full-body clearance at 4.342 s; sustained pass confirmed at 6.342 s, still on the straight. |
| Club behind stock, single-car-width barrier corridor | Follows without passing in the corridor, then completes the remaining course safely behind the leader. |
| Club / street / stock, fastest at rear, 24 m spacing | Club passes street with sustained confirmation at 20.150 s and stock at 22.600 s. Both confirmations are after the back straight. Street remains behind stock. No forced three-wide manoeuvre. |
| Stock solo, traffic versus reference | Exact timestamped motion/inputs and result. |
| Club solo, traffic versus reference | Exact timestamped motion/inputs and result. |

The queue is one deterministic starting condition, not evidence that every feasible pass will be found. Its passes through the later course do not replace the planned controlled braking-zone, overlap-at-turn-in, abort and exit tests.

## Failures retained and corrected

The first pilots are in `unity-32/*-01`. Equal-speed following and the narrow case repeatedly stopped and entered recovery. Incorrect release ordering initially exposed zero speed to controllers; passing commitment and following-path selection could retain an inappropriate lateral offset.

`matrix-02` fixed ordinary following but failed the narrow case later in the course: contact, a prolonged deadlock and assisted resets. Its open pass also provoked needless braking from the lead car. Long constant-velocity predictions through corners and reactions to rear closing traffic contributed to the failure. These failed runs remain available; their reset-assisted position changes are not valid overtaking evidence.

`matrix-03` passes all 92 two-car/solo checks after prediction changes. `matrix-04` adds cached projections, all-pair queue telemetry and eight geometry checks, and passes the expanded suite.

## Visual review and milestone audit

Actual running build 04 was inspected through the computer-use skill at quarter-speed in `visual-pass-04`. Sampled views at 0.8 s, 3.4 s, 8.3 s, 12.7 s and 25.1 s show the approach, left-side separation alongside the coupe, return to the line, corner entry and home exit. No visible intersection in those samples. This was sampled live inspection, not continuous video or player acceptance; the three-car run still needs moving review.

The visible run completed both cars with the same times as the hidden pass fixture, zero contacts/recoveries/missed gates and zero measured pose overlap. **Shutdown failed:** after automatic completion its log stopped at Input System shutdown while the process and window persisted for over 80 seconds. The verified review process was forcibly stopped; see [shutdown audit](unity-32/visual-pass-04/shutdown-audit.json). This extends the prior early-close symptom to a completed, visually inspected run. Cause is not established; investigate visible/fullscreen/capture versus hidden execution before general distribution. All eight hidden fixture runs and both regression controls exited normally.

| Area | Assessment and next requirement |
| --- | --- |
| Driving/race behaviour | Controlled following and first passes are credible enough for a development checkpoint. Vary gap/speed/body size; add deliberate braking-zone and unsafe-opportunity controls before dense racing. |
| Visual quality | Existing car bodies remain readable and separated; Cedar remains a plain technical circuit. Rear glass shows strong sky glare at one corner. No new world/art fidelity claim. |
| Usability | Test HUD identifies pass/follow/rejected paths. `Merge` can describe small line-tracking error after a pass, so its reason is sometimes misleading. Improve diagnostic intent labels. |
| Progression/exploration | No new campaign content, rewards, parts, endurance or resident activities. No content-hour claim. |
| Controller | Existing controls retained; no new physical DualSense acceptance. |
| Performance | Cached repeated projections; no measured 24-car planning budget or gameplay frame-time acceptance yet. Concurrent accelerated tests are not performance evidence. |
| Production readiness | Opt-in fixture build only. Skills, personality, robust abort/side-by-side corner rules and full-field acceptance remain incomplete. Visible-review shutdown failure reproduced after automatic completion; hidden runs exit normally. |

Highest-value next work: expand traffic scenarios with varied closing gaps and speeds, inspect the three-car manoeuvres in motion, then establish explicit braking-zone opportunity/abort cases and measure planner cost before scaling to 24 cars. The broader campaign scope is unchanged.
