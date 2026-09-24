# Course map and recent laps, build 95

23 September 2026. This is concrete progress on the accepted transparent HUD direction. Build 94 was the preceding completed presentation slice. Retain the full campaign objective; this milestone does not complete the UI or game.

## Changes

The upper-right map reuses actual course geometry with fixed north-up, uniform scale and a start/finish tick. Opponents use physical positions; the player has a larger directional triangle drawn last to remain distinguishable in a packed grid. Static course geometry and changing markers use separate UI meshes. Marker refresh follows the existing 10 Hz display cadence, and distant off-course markers remain inside the map bounds. This is a positional display, not a source for AI navigation or legal race order.

A compact list retains the three most recent completed laps. Sampling follows the car's existing ordered-gate completion state; grid-to-line time is excluded. A three-second final-lap notice appears on entering the last lap of a multi-lap event. Recovery and finish notices take priority. No driving, equipment or economy logic changes.

## Evidence

- 498 editor checks, nondevelopment Windows build, 159 frozen source/script inputs. New checks cover bounded/deduplicated/reset lap history, invalid times, north-up map orientation, aspect ratio, height independence and off-course bounds.
- Bayview and Cedar match build 94 exactly for recorded motion, legal gates, contacts/recoveries, context samples and race results. These are controlled fixture results, not a universal behaviour proof.
- Three-lap 24-car run at actual 1280×720: all 24 finish, normal exit and no runtime exceptions. All nine runtime evidence checks pass. Displayed L2 25.239 s and L1 31.826 s match physical start/finish passage differences; the final-lap notice appears at the appropriate transition. Actual player lap times: 31.825815 / 25.238724 / 23.686382 s. These are automated results, not user pace or a new AI-speed claim.
- Actual 1080p map gameplay and 720p final-lap screenshots inspected. Course, markers, recent laps and best time are visible with no panel overlap, and the driving centre stays clear. Secondary labels are small at 720p: adjustable HUD scale/readability remains a useful usability follow-up. This inspection does not replace physical DualSense or user acceptance of this new slice. The prior transparent direction was user-approved.

## Performance comparison

The first view-off run changed from 1920×1080 to 3840×2160 during viewing. The user confirmed switching it while watching. Preserve that evidence, but exclude its pair from matched HUD-cost claims. This is not a rendering defect or user error. Both original runs still preserve recorded race behaviour.

A new sequential pair under view-off-1080p / view-on-1080p completed with all frames at 1920×1080 and no dropped observations. Runs are normal-speed nondevelopment players, no screenshots during measurement, and no competing project editor/simulation. Stored race behaviour matches exactly.

| Frame time | HUD off | HUD on |
|---|---:|---:|
| Mean |20.645 ms|20.649 ms|
| p95 |69.332 ms|69.904 ms|
| p99 |119.918 ms|117.444 ms|

These are similar overall frame times in one valid pair, not a precise isolated cost measurement or proof of zero HUD cost. Both fail the existing 16.7 ms p95 / 33.3 ms p99 frame targets and 3 ms AI p95 target. Large stalls remain. There are zero observed allocations in measured driver/forecast regions after five simulated seconds; those regions exclude HUD allocation and rendering.

## Milestone review and next work

Visual/usability: retain this small race-view addition, then move to the pre-race/results menu treatment. Fix the known result-button contrast and post-finish top-bar clock while preserving 3D car/loadout/personality information and the existing retry/save flow. Avoid indefinite polishing of the HUD before advancing that requested UI milestone.

Driving/race behaviour: exact controlled parity on both courses; no new AI tuning. The connected recovery stall from build 94 remains open and was not retested or fixed here. Progression/exploration: unchanged; this pass does not establish stock balance, campaign content duration or new resident content. Fuel/tyres/endurance, teammate progression, wider bodies/content and full campaign scope remain required.

Production readiness: intermediate review build, no desktop shortcut promotion. Physical controller and broader accessibility review, performance budgets and the full menu redesign remain open. Frozen source, tools, telemetry, screenshots and fingerprints accompany this review.
