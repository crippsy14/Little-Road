# Checkpoint crossing and recovery — 19 September 2026

Goal work resumed after scope planning. First priority is the reported missing player finish/results/payout. The broader 12-hour campaign, fidelity, endurance and ownership scope remains unchanged and unfinished.

## Reproduction and change

The actual pre-fix `RoadCar.TrackGates` method was exercised with a controlled sideways-car state: positive route movement inside the road failed to count because the nose was not facing forward. Restoring forward heading after moving nine metres beyond the line still did not count, because the old acceptance window was only eight metres. See [before evidence](unity-11/checkpoint-before.json). This establishes a defect; it does not establish that this was the exact trajectory in the user's original session.

Checkpoint detection now tests the swept forward route segment and interpolated lateral position at the crossing. Car nose direction and longitudinal speed no longer invalidate an otherwise legal crossing after a spin. Ordered gates still reject off-road shortcuts and reverse crossings. A missed gate stays required; the HUD explicitly tells the player to use R/Triangle to return before it. Race ranking cannot credit distance beyond a missing required gate. Duplicate finish callbacks cannot insert the same car twice.

New missed-gate logs identify the car, pending gate, required progress, current progress, offset and race time, so future playtest reports are diagnosable. AI-only missed-gate recovery is now explicitly race-only; free driving does not have race gates.

The first rendered integration attempt correctly displayed the missed-checkpoint warning, but its immediate Triangle recovery check failed while the field was passing the return point. Recovery previously returned false when that point was occupied, silently losing the request. A player recovery request is now queued until a space is clear, with a waiting message. The test now waits for that real safe recovery rather than assuming an immediate teleport. Build 02 reruns the affected career scenario; the original failed attempt is retained in `unity-11/route-run.log`.

## Validation

The separate Windows review build passed 129 editor checks, including 17 new crossing/order/finish checks and the existing saving/reward failure checks. Scenarios cover both road edges, crossing while sideways/reversed relative to motion, crossing between samples, entering/leaving the legal width, stationary/reverse movement, a missed gate and correct return, start-line wrap, exactly three ordered laps and duplicate finish rejection. These controlled tests do not establish manual handling quality.

Build 01 assembly SHA-256: `2295860653C7F5DE15533CEBB066844E7F1231A10FF145F78DBA56309B708CAB`. Build 02 with queued recovery: `45667EF9BBE59B84C346D43EA40461EAEF150497533532FF543E62743B19ADD3`.

Runtime validation completed. Build 01's free-roam lap completed 683.74 m in 42.63 s with zero contacts/recoveries, at the diagnostic's gentle pace. Its recovery scenario failed as described above. Build 02 passed the affected scenario: an intentionally off-road crossing stayed invalid, a synthetic Triangle input queued recovery while traffic passed, and recovery returned behind the required gate without earning a lap. See [recovery data](unity-11/captures-02/checkpoint-recovery.json) and [the visible warning](unity-11/captures-02/checkpoint-missed.png). The immediate recovery screenshot was taken before the interpolated visual pose caught up; it is not a trustworthy image of the final return position.

The same build-02 career race then completed all 24 entrants with exactly 24 distinct finish events. The stock player car finished 24th in 118.20 s after the deliberate delay/recovery, displayed the real result menu and saved 100 credits. Both the runtime disk reload and an external read of the isolated JSON confirmed credits 100 and raceSerial = settledSerial = 1. See [result data](unity-11/captures-02/race-result.json), [inspected result screen](unity-11/captures-02/race-result.png), [runtime log](unity-11/route-run-02.log) and [editor checks](unity-11/editor-checks-02.json). The test process exited normally.

The fixture uses a controlled initial off-road placement, real Rigidbody movement and synthetic button input, followed by AI driving. This is an integration regression pass, not a manually driven race or physical DualSense validation. No broader balance/performance conclusion follows from this one altered-start race.

## Remaining gates

- The crossing/recovery/result integration regression passed; preserve this case for future changes.
- Manual player finish/rejoin acceptance and physical DualSense testing remain outstanding.
- Stretched grass, driveway intrusion, distant-water noise and tree grounding remain unfixed. Then continue traffic/contact tests and the agreed Bayview/three-master-body fidelity benchmark.
- The normal playable checkpoint remains 10; the separate review build is not yet a replacement package. The user's save is not used by the diagnostic.
