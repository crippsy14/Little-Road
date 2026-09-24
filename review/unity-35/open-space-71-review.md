# Prototype71: take usable open space

The user approved drafting70 and requested more willingness to commit into open forward space before implementing individual personalities.71 tests a forward-progress term in the existing candidate preference. It rewards a viable passing approach over following slower traffic and penalises returning from an open route into that traffic. It does not command arbitrary full throttle, change car capabilities or promise a pass based only on an empty destination.

`OpenSpacePriority` prepares an owned, bounded two-second progress estimate from current vehicle capability and observed traffic motion. The25 existing lateral samples use the same smooth transition as ordinary path proposals. Eight coarse time samples account for a blocked approach as well as the destination. A gain/loss relative to holding the present offset adds/subtracts at most2 preference points. The existing physical road/corridor/scenery checks, traffic forecasts, following controller, emergency guard, margins, inputs and100ms planning cadence remain. Both live and prospective preference maps apply the option; selected paths are still part of exact forecast identity. The coarse tactical estimate is not a collision certificate.

438 build checks pass. New checks cover open-side progress, left/right symmetry, an occupied destination, no rear-induced yielding, exact empty-road choice, unchanged side reservations, zero warm allocations and retaining an already-open passing route. Disabled71 exactly reproduces approved70's hidden race arrays/results/incidents. The scope and screening thresholds were written before the race comparison in `open-space-71-expectations.md`.

| Original field, drafting on in both | Priority off | Priority on |
|---|---:|---:|
| Combined car-seconds |2223.453552 |2209.831879 |
| Preview time / finish |89.216957s /6 |90.483315s /7 |
| Preview CP1 crossing rank |20–21 |22 |
| Preview sustained passes of initially leading street cars |13 |12 |
| Possible held-behind proxy |5.4998s |3.3998s |
| Braking sample duty |7.4845% |6.9341% |
| Impact participant records |16 |10 |
| Peak relative impact |12.034184m/s |7.627289m/s |
| Unresolved samples |838 |781 |
| Body-boundary samples / recovery episodes |0 /0 |0 /0 |
| Hidden whole-AI mean per snapshot |3.492979ms |3.627145ms |

All24 finish; both arms pass12/12 field checks and13/13 checkpoint deadlines. Collective time improves13.621674s (0.613%), but the preview car slows1.266357s and loses one place. Braking decreases0.55035 percentage points. The held-behind proxy is not proof that every remaining delay is avoidable. Stronger rivals can change the preview's outcome; no car receives special treatment. This passes the declared first-field screen, not broad AI or rendering acceptance. Warm driver/snapshot/worker allocation remains zero. The new preference adds about3.84% mean whole-AI cost in this hidden pair; unchanged fixed-resolution budgets still require measurement.

Controlled regressions and the changed equipment arrangement are complete, with results below. Preserve70 as the user-approved reference; desktop55 is unchanged. Individual personalities remain the next milestone, with this driving request taking precedence.

Evidence: `prototype-71-manifest.json`, `source-71`, `build-context-71-checks.json`, `open-space-71/off/motion-parity.json`, `open-space-71/comparison.json` and raw off/on reports. Supplemental controlled/held-out/rendered results follow below when complete.

## Wider validation

The second equipment arrangement also finishes24/24 without recovery. Priority on removes the four baseline road-edge samples and reduces impact participant records10 to4 and unresolved samples738 to679. Braking falls7.3016% to6.9153%. Combined time is0.649841s slower (2186.279297 to2186.929138), while the preview improves2.257782s (82.618568 to80.360786), staying fourth. Both fields therefore show less braking and fewer contact records, but pace gains are not uniform. The second field's peak relative impact rises2.634509 to5.170708m/s. The enabled second field passes12/12 field and13/13 checkpoint checks; warm allocation remains zero.

Controlled results are **150/152**, versus70's151/152. The old narrow-corridor completion-delay failure remains, and its independent zero-unresolved-plan requirement newly fails with16 active samples. All ten scenarios still finish with zero contact, road departure, recovery or runtime errors. The new failure belongs to car1 at18.3–23.0s, after the wall section (wall end1404.25m; failures start1637.52m). Rear-approach reservations are present in the first captures. This is not grounds to shrink the contract to the walls or ignore the failure; capture candidate rejection reasons before making a causal fix.

71 is therefore an experimental viewing candidate, not a replacement for approved70. The main-field screen passes, and the user was told about the new controlled planning failure before launching `view-on`. Actual willingness to use space needs moving review. No shortcut/default change has been made. Keep the user-requested goal of committed forward racing; do not reverse the change solely to optimise one preview lap, but do not call the unresolved regression accepted either.

## Viewing and failure evidence

The first visible run was missed. After the user said they were ready, `replay-on` ran and exited normally. It exactly reproduces `view-on` in recorded motion, checkpoints, contacts, recoveries, track/context samples and race results; the initial visible run likewise reproduces the hidden candidate. The user subsequently reported much better racing throughout the field and approved the behavioural direction. They requested lower aggregate time and an approximately80-second preview lap, highlighting launch/pass delay, incomplete acceleration alongside traffic and corner pace. See `docs/36-racing-pace-milestone.md`. This visual approval does not resolve the controlled planning or performance failures.

The first visible capture is1920x1080 at1x playback, with9296 captured frames, zero capture drops and zero warm measured driver/snapshot/worker allocation. Frame p95 is29.194805ms and p99 is57.209095ms; whole-AI p95 is27.4537ms. The existing frame and AI budgets remain failed. These are absolute measurements, not a matched rendered off/on performance comparison. Zero capture drops does not mean hitch-free rendering.

The diagnostic `no-room-audit` exactly reproduces the controlled run. Its15 failed-selection events contain84 candidate rejections for the current reserved corridor and7 for moving side entry by car0; no audit entries were dropped. These events differ from the16 periodically sampled unresolved flags. Audit clock includes countdown; the earlier18.3–23.0s figures are race-relative sample times.

At the first event, car1 is at route1637.521m, offset−3.51613m, moving inward with start slope+0.08346. Car0 is9.5846m behind at offset−0.69029m. The existing rear-approach rule reserves an immediate right boundary at−3.41029m (rival offset minus both half-widths and0.4m). Every tested candidate crosses that stationary boundary within0.10s, even when its eventual target is farther left. The swept-traffic check runs before this corridor check, so these particular candidates reached the corridor rejection without an earlier predicted side-entry conflict. Later events include a real predicted moving-side-entry rejection, which must remain separate.

This establishes the immediate rejection mechanism, not that removing the corridor would produce acceptable racing. The next bounded investigation should evaluate when a rear reservation becomes necessary relative to physical overlap and the car's achievable lateral movement, while retaining swept-traffic/road/scenery checks and the independent no-room contract. Do not blanket-ignore rear traffic, relax physical margins, or count fewer unresolved flags alone as success. No runtime changes were made during this diagnostic follow-up. Detailed evidence is in `no-room-audit/rejection-summary.json` and the raw failed-plan audit.
