# Blocked recovery and obstacle anticipation

21 September 2026. The previous goal turn made concrete progress on physical recovery; this turn rechecked that source and its terminal test processes, then closed the permanent-wait defect identified in iteration 38. This remains an AI development checkpoint, not campaign completion.

## Behaviour

Recovery now distinguishes a wait supported by moving traffic from a static geometric/stationary-traffic deadlock. Moving traffic can justify a long wait. A recovery episode lasting about sixty seconds with at least twenty-five unproductive seconds requests an assisted fallback. A forty-second temporary blockage remains below that deadline and resumes without relocation. The moving-traffic threshold is currently 0.25 m/s; these thresholds remain tuning values, not a claim of general dense-queue acceptance.

The fallback searches bounded locations behind both current and last-safe progress and behind the next required checkpoint. Each destination is checked against scenery, present car occupancy and one second of predicted approaching traffic. A blocked destination is rejected and a farther-back location is considered; if none is clear, the car waits and retries at half-second intervals. No gate is advanced. A completed physical rejoin cancels any pending fallback request.

Every successful assisted reset records its reason, time, old/new position, old/new progress, checkpoint before/after and number of rejected destinations in the race report. It remains a reset, never a successful physical recovery. The fixture reports distinguish its explicit position jump from ordinary per-tick movement.

The new trap scenario exposed a second defect on the return lap: checking only the current heading could detect a barrier beside a bending racing line too late. The recovery sensor now checks the anticipated racing-line path while racing as well as while rejoining, with the preview extending with stopping distance. Shared vehicle forces remain unchanged.

## Evidence and frozen build

Final candidate: `outputs/little-road-recovery-11-review/Little Road Unity.exe` relative to the workspace. Assembly SHA256: `17B5C16E17A0A1BB2C369E04267712F4318828FDF591C8867AC62CCDEFA381B8`.

- `unity-31/editor-checks-11.json`: **231 editor checks**. Added cases cover scenery/approaching traffic at a reset destination, forty-second temporary blockage, persistent deadlock, moving traffic beyond sixty seconds, traffic subsequently stopping, and cancellation of a pending fallback after physical rejoin. The ninety-second moving-traffic wait is a controlled policy-observation test, not a ninety-second physical traffic scene.
- `unity-31/matrix-10/analysis.json`: **120/120 runtime checks across 18 stock/club runs** after the obstacle-preview correction. This extends the preceding fourteen cases with permanent traps and forty-second temporary blockages.
- `unity-31/matrix-11/analysis.json`: **120/120 checks on the final build**, across all 18 completed runs. Every process exits with code 0. All entrants finish with zero contacts and overlap below the 2 cm bound. The two deliberately permanent traps each produce exactly one reported assisted reset; the other sixteen runs produce none. Missed-gate protection, physical continuity outside reported resets, blocked destination rejection and passing traffic checks all pass.
- `unity-31/regressions-10.json`: **15/15 comparisons** against the frozen clean three-lap club reference and production Bayview 24-car race. Car outcomes and timestamped input/motion traces remain exact. These comparisons use build 10; build 11 adds only pending-fallback cancellation and its editor regression.

The permanent trap deliberately obstructs the first nominal fallback point. Stock and club each retain about a minute of lost time, reject that destination and reset once roughly eight route metres backwards, with unchanged gate index. They then complete the lap. The two forty-second temporary-blockage runs wait and finish with no reset. Original sideways, wrong-way, off-road, wall, missed-gate, nine-second wait and approaching-traffic scenarios remain acceptance controls.

Build 09's matrix is retained at **118/120**: both permanent-trap runs made one contact on the return lap, despite safe initial reset placement. Their impacts were not removed or accepted because penetration happened to be zero. Build 10's anticipatory path check eliminates those contacts. Deadline assertions allow two physics ticks between the separately accumulated report and recovery clocks; this addresses float accumulation at the boundary, not a reduced waiting requirement.

## Visual, usability and production review

See [live visual observations](unity-31/visual-observations.md). A normal-speed wall replay completed; a quarter-speed replay showed the initial barrier, the car rejoining around its end and resumed racing with a readable recovery HUD. Inspection was through sampled live screenshots, not continuous video. No visible intersection was present in the inspected rejoin frame. The precise reverse phases remain telemetry evidence because they fell between the inspected frames.

The slow replay was deliberately closed after its initial recovery. Alt+F4 reached input shutdown but left its process alive; the exact review PID/executable was verified and terminated. **This early-close shutdown issue remains open.** It is excluded from successful-exit evidence. The normal-speed completed run and automated exits are recorded separately. The launcher now supports explicit quarter-speed through 4x playback, while human comparison recordings require real time.

No car or world art changed. Cedar's floating boards, flat terrain and simple pits remain engineering scenery, and the campaign's art direction remains unchanged. No new progression, resident activity, campaign duration, save acceptance or physical controller acceptance is claimed. The production desktop adventure remains checkpoint 20 with its legacy driver. Geometry-query CPU cost for a full active recovery field and real rendering performance remain to be profiled; concurrent accelerated tests cannot establish those budgets.

## Next bounded work

Proceed to following and a two-car straight pass, then the user's reverse-speed three-car queue. The integration boundary in `docs/27-recovery-and-racecraft-fixtures.md` explicitly prevents ordinary following from being mistaken for physical recovery. Keep recovery independently available for actual spins, off-road excursions, missed gates and deadlocks. Braking-zone passing and safe aborts follow the basic pass; skills/individual race form then build on credible control and traffic decisions.

Broader corner/high-speed recovery, different body envelopes, traffic prediction under turning/braking and dense queues remain open. The permanent stationary-trap defect is addressed by the new fallback; this does not establish recovery acceptance for every situation or authorise wholesale replacement of production AI.
