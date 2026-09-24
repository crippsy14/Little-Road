# Moving-traffic approach check46: useful separation, incomplete racecraft

23 September 2026. Previous turn made concrete progress by implementing and measuring44/45 and retaining their failures. This turn adds a physical side-entry check, runs15 standalone comparisons/regressions, and preserves the previous recordings when disabled. Build46 passes315 checks. No production or desktop shortcut promotion.

The new opt-in `--traffic-path-check` evaluates the existing own-car physical forecast against nearby, currently side-separated opponents. It predicts their observed velocity and fading yaw over1.25s, using swept rotated boxes with collider centres and angular padding. Same-corridor following remains with the existing longitudinal controller. A blocked crossing can try the current offset; if no checked path remains after traffic rejection, steering-centre/full-brake inputs replace continuing the stale crossing. Actual vehicle physics remains authoritative. These are nominal observations, not known future opponent intent or a general collision guarantee.

## Results

| Evidence |43 risk0 |45 risk5, soft only |46 risk5 plus check |
| --- | ---: | ---: | ---: |
| Finishers |24 |24 |24 |
| Combined first-lap car-seconds |2237.225189 |2195.035492 |2235.475616 |
| Car0 checkpoint1 |24th |19th |20th |
| Car0 finish |10th |7th |8th |
| Car0 finish seconds |93.049355 |89.433571 |91.166481 |
| Last finisher seconds |99.589424 |100.147621 |103.688423 |
| Pair contacts |5 |22 |2 |
| Maximum impact relative speed |4.9072m/s |8.5559m/s |0.3583m/s |
| Sampled road departure / recovery |0 /0 |0 /0 |0 /0 |
| Active unresolved samples |138 |193 |374 |

All field and checkpoint-flow gates pass. The declared initial field screen passes, including contact frequency/severity. However,46 gives back40.440124 of45's42.189697 car-second gain; only1.749573s remains versus43. This is not acceptance of the user's average-risk5 racing feel. Zero-contact optimisation is not the goal, and374 unresolved samples remain a defect.

Field controls with the option disabled preserve exact43 risk0 and45 soft-only risk5 motion/commands/context/events/results. The new checker rejects2702 candidate paths in the enabled field; total own forecast calls rise25679→28464. Its measured aggregate hidden-run check cost is1571.4ms. This is not rendered-frame acceptance; real-time AI/startup budgets remain open.

## Controlled evidence and coverage limit

The declared two-car `crossing-pass` case completes identically with the check off/on, with zero candidate traffic rejections. Both cars finish cleanly at61.1975/77.7864s and the fast car confirms its pass at3.4500s. It does not reproduce44's crowded crossing failure and must not be cited as proof that failure is fixed. The build-level physical forecast test does reject a colliding outside-to-inside approach while permitting a clear outside pass and a route with a distant opponent. That validates the mechanism, not the full incident response.

All10 existing controlled regression runs finish without contact, body departure or recovery;59/62 checks pass. The same three45 failures remain: the second queue pass is too late for the straight, no-room completion is36.3422s against29.7580s reference, and braking-leader completion is29.9080s against27.0247s reference. Rear merges still meet independent deadlines, the narrow corridor contract passes, and clear solo times remain unchanged. Do not relax these failures because the field screen is green.

## Next bounded work

Use45/46 to separate fast path choice from collision rejection. Diagnose the three delayed passing cases, especially whether the selected target still sits inside the downstream leader envelope, whether a changing opponent corridor is being chased, and how quickly a checked faster alternative becomes available. Choose a passing approach based on achievable forward progress with traffic, not destination space alone. Keep the45 pace advantage as a target rather than accepting46's near-baseline collective time as completion.

Capture an exact pre-controller observation and selected trajectory from the crowded failure before its first serious contact. The current two-car approximation is insufficient. Preserve full car geometry/velocity/steering and all relevant neighbours for a deterministic incident replay, without reading future controls or assigning artificial results. An observation-only capture must demonstrate unchanged motion before it can anchor that replay.

Continue measuring the preview car and whole field together. Mixed drivers, broader seeds/starting positions, moving player review, controller/UI integration, production performance and the12-hour campaign remain incomplete. Overall goal remains active.
