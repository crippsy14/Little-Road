# Racing AI: reuse review and decision gate

21 September 2026. Requested after the player questioned whether continued custom-planner tuning is productive. This changes the immediate direction from more incremental tuning to a bounded comparison of reusable implementations. The full campaign scope remains unchanged.

## Selected direction

**The player selected the published F1 approach, then asked Astra to own a full implementation plan grounded in independent evidence. Follow [document 32: implementation and acceptance contract](32-racing-ai-ground-truth-and-implementation.md).** Build a separate context policy through the existing physical driver interface. No Omni purchase or import is needed. The options review below is retained as research history; its original purchase-first recommendation is superseded.

## Original recommendation (superseded)

**Evaluate Omni Vehicle AI as the first ready-made candidate, retaining Little Road's physical cars. Do not replace the production AI or buy several competing packages speculatively.** Its documented custom input adapter is a close fit for our existing driver interface. This is a recommendation to evaluate, not a claim of proven superiority: no package has been purchased, imported or run in this review.

If that package cannot be obtained or does not meet the comparison, the preferred custom fallback is a small implementation of the published **F1 2011 track-width context method**, replacing tactical position selection only. It must compete against the frozen baseline before expanding. Our earlier research identified this approach, but our current fixed-endpoint trajectory search does not implement it. More source citations do not close that implementation gap.

Do not turn this into another general architecture rewrite. Preserve equipment, physics, track data, checkpoints, recovery, telemetry and the playable adventure. Preserve the original Bayview policy as the player's qualitative reference. Success is racing that looks and feels better while retaining physical consequences and fair equipment advantages.

## What the evidence currently says

The watched candidate 25 was more racey, but the player reports excessive braking and conspicuous left/centre/right choices. Candidate 29's matched Cedar audit measures 17.04% braking in the field against 4.36% street / 6.21% club solo. Field line adherence is 32.18%, compared with 99.41% / 89.99% solo. These use the same post-start-line denominator, not raw sample counts. Neither a traffic deviation nor braking below solo speed is automatically wrong.

Of about 398 sampled car-seconds braking, 14.90 occur with the immediate guard active. Most are associated with ordinary path tracking or planner following/stopping. Controller-state attribution is not causal proof, but it argues against spending the next milestone solely on the emergency guard.

The current planner has nine geometric candidates with several repeated fixed offsets, 3-second opponent forecasts, hard candidate rejection, and a stationary stop envelope for predicted front conflicts. This offers useful diagnostics but can favor coarse lateral destinations and stop/release behaviour. Candidate 30 changes only the moving-leader speed constraint; it built with 260 checks before this research request, but remains runtime-unvalidated. Further tuning is deferred pending this comparison.

The previous goal turn made concrete progress: ordinary build mode removes the observed development-network trigger, and candidate 29 completed a full field with telemetry. This continuation adds matched solo measurements, the brake-duty audit and a reuse decision. The overall goal remains active and incomplete.

## Candidates examined

| Candidate | Verified public evidence | Fit for Little Road / decision |
| --- | --- | --- |
| **Omni Vehicle AI, Ash Dev** | Publisher documentation exposes an input provider for custom controllers, path-following, obstacle sensing and context steering. Store lists v1.5, released 13 June 2025, originally Unity 2022.3.4, with URP compatibility. | Best ready-made evaluation candidate among those inspected. Adapt inputs into RoadCar; establish Unity 6000.6 compatibility in isolation. No verified 24-car or equipment-aware benchmark. |
| **F1 2011 context steering, Andrew Fray** | Published developer account separates ordinary line following from tactical track position and emergency slowdown; describes interpolation between sampled positions. | Best documented design fallback. Our implementation would still require engineering and runtime validation. No drop-in F1 source package was found on the publisher's chapter index. |
| **EPS Racing AI, EpicStar** | Author advertises RCC and Dreamteck Splines integration, predictive braking, recovery and controller customization. Download is v1.0.0; some advertised behaviour is explicitly future v2 work. | More integration dependencies than Omni for our custom controller; not first choice. No verified 24-car result. [Author page](https://epicstargames.itch.io/eps-racing-ai). |
| **Agents of Steer, negleft** | Official store lists v1.6 from 16 November 2023, original Unity 2021.3.16. Publisher's indexed demo page exists, but its current assets page did not expose a working playable demo during this review. | Keep as reserve, not a purchase recommendation without confirming current integration and a runnable demo. [Store](https://assetstore.unity.com/packages/templates/systems/agents-of-steer-racing-ai-and-racing-system-102311). |
| **TUM Graph-Based Local Trajectory Planner** | Author's Python implementation returns feasible action alternatives and allows a separate behaviour selector; repository identifies LGPL-3.0. | Useful deep reference for separate geometry/speed/behaviour responsibilities, but a research integration/port would add substantial work. Real-vehicle speed is not evidence of 24 simultaneous Unity drivers. [Repository](https://github.com/TUMFTM/GraphBasedLocalTrajectoryPlanner). |
| **Speed Dreams / VDrift** | Established complete simulators with source. Speed Dreams documents continuing USR work; VDrift identifies GPLv3. | Valuable whole-race references, not Unity components. Copying driver implementations would require deliberate licensing and integration decisions. No source imported. [Speed Dreams release](https://www.speed-dreams.net/en/blog/release-2-4-0), [VDrift](https://github.com/VDrift/vdrift). |
| **Small public Unity repositories** | AIVehicleRoutingBuddy explicitly lists no overtaking or collision avoidance despite its near-production description. HTANV's Unity-Game-Race-Ai has conflicting MIT metadata and a proprietary statement in its README, and describes 2–4 AI scenes. | Neither is an established solution to our missing racecraft. Do not substitute marketing descriptions or repository stars for evidence. [RoutingBuddy](https://github.com/deye1986/AIVehicleRoutingBuddy), [HTANV](https://github.com/HTANV/Unity-Game-Race-Ai). |

## Omni: concrete integration and limitations

The [publisher's manual integration guide](https://soft-pilot-e91.notion.site/How-to-create-integration-Manually-12d54a67048d8093946cf8ecb88c5119) exposes `GetSteerInput()`, `GetAccelerationInput()` and `GetHandBrakeInput()`. Our adapter would consume AI input through `DriveCommand` and retain RoadCar's physics. Confirm negative-input braking/reversing semantics, axis signs and update timing against actual package source before connection. We do not need to migrate to RCC or replace the player's controls.

The [context-steering documentation](https://soft-pilot-e91.notion.site/Context-Steering-Script-New-19c54a67048d80298901c209719b16dc) describes radial sphere casts, goal/inertia interest, obstacle danger and separate critical-path checks. This is not the same implementation as Fray's track-width context map. Inference: its local sensing could still miss some corner interactions or overreact to nearby cars; only a track comparison can settle that.

The [driving controller documentation](https://soft-pilot-e91.notion.site/AIVehicleController-11d54a67048d8007a824c16982ae87e4) describes configurable turn/obstacle speeds, braking and input smoothing. It does not establish automatic calibration from Little Road's grip, mass and power. Test stock, street and club without manually compensating away their physical differences. Endurance degradation and pit strategy remain our game systems.

The public documentation was read successfully in a browser, despite a 404 from the text-fetch service. This review inspected documentation, not the paid implementation. [Store](https://assetstore.unity.com/packages/tools/behavior-ai/omni-vehicle-ai-298299) displayed **€55.20 before tax** in the research session; checkout region/currency may differ. Acquisition is a user choice, not authorized by the research request. [Publisher tutorial playlist](https://www.youtube.com/playlist?list=PLtYhPiKW6dMX9Qv5JUnvzW4MgpH_m9cQ_) verified as a link; videos have not been watched or treated as performance evidence.

## F1 method: what would actually change

Fray's [chapter, sections 18.4.4–18.6](https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf) uses interest and danger across available track width. Selection respects reachable space; interpolation allows continuous lateral positions despite a small sample array. The guide line is a preference, and informative risk need not become emergency braking. The low-level driver still handles normal driving. This is a published design account, not a complete physics or safety implementation.

Our proposed adaptation: one continuous, reachable lateral target with a speed request, feeding an equipment-aware follower. Show the interest/danger/selected-target trace so we can explain an unwanted move. Retain local physical checks, but avoid passing both old and new tactical commands through competing avoidance layers. An adapter that simply renames our nine existing lanes would not constitute this experiment.

## Bounded comparison and stop rule

One adapter/prototype, one measured correction round, then a recorded keep/reject decision. Do not spend another chain of candidate numbers tuning a package or the current planner without a visible decision point.

1. **Isolated integration:** import an acquired package into a disposable Unity project first, inspect runtime dependencies/source/licence, then connect the candidate to our existing driver seam. No production replacement. If no package is acquired, use the F1 fallback as a separate selectable policy, not another patch inside the current planner.
2. **Same physical tests:** paired street/club solo, ordinary and hard leader braking, faster-car pass, three cars in reverse performance order, narrow no-pass corridor, mirrored shared corners. Record legitimate collision delays and recovery; no position teleporting, grip boosts or hidden speed advantages.
3. **Visible race first:** 24 cars on Cedar and the original Bayview setting at 1x. Review before extensive personality work. Record brake/line percentages, speed and sector loss, finish spread, checkpoint passage counts, contacts/departures and CPU/frame percentiles. A fast but visibly erratic or aggressive-crashing candidate fails the intended result.
4. **Fair gate:** retain current checkpoint deadlines; require clean solo completion close to the matched reference and useful equipment-led passes. Require a clear reduction in repeated brake/reaccelerate episodes and lateral indecision without greater contact severity or road departures. Use identical loadouts, arrangements and physics for comparisons. A different random seed is not independent variation until the policy actually uses it.
5. **Decision:** keep the candidate only if moving review and measurements support it. If integration requires replacing our physics or a major framework migration, reject that route early. If both candidates fail, explicitly reassess the controller/track model with concrete traces; do not restart an indefinite tuning loop or claim passing narrow fixtures completes racing.

No paid assets, external source code or additional software were installed in this review. Candidate 29 remains the latest runtime-tested experimental export; normal desktop adventure remains checkpoint 20. New research does not itself establish better racing.
