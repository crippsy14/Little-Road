# Passing trajectory experiment: do not promote

The user wants AI work prioritised and hitbox investigation parked. Prototype60 tests preserving the geometry and consumed progress of an unfinished pass while its chosen destination remains unchanged. It is opt-in through `--retain-pass-trajectory`; ordinary planning remains the default. Every retained candidate still undergoes current-state road, scenery and traffic checks. A failed retained candidate permits a freshly anchored alternative. Vehicle physics, track limits, hitboxes and emergency controls are unchanged.

350 build checks pass. The disabled field exactly matches59 across all recorded motion, technical/context samples, events and race results. The enabled same-grid solo also matches59 exactly. Both parity checks include normal process exit and absence of runtime errors. All132 controlled checks pass, including the existing no-room completion allowance that55 missed. All controlled fixtures have zero contacts, sampled body departure or physical recovery; the independent no-room corridor contract also passes.

| One-lap Cedar field | Control | Retained trajectory |
|---|---:|---:|
| Finishers |24 |24 |
| Combined car-seconds |2215.597137 |2213.431000 |
| Upgraded rear-grid car time |85.501198s |88.383827s |
| Its CP1 rank / finish rank |19 /4 |20 /6 |
| Its contact records |2 |0 |
| Field impact records, including participant reports |10 |14 |
| Peak relative impact speed |8.087913m/s |13.184444m/s |
| Sampled body departures |0 |4 |
| Physical recoveries |0 |0 |

All13 start/checkpoint/finish deadlines pass, but the field passes only11/12 existing checks. Car8 exceeds the8m half-width in four samples from29.708s to30.008s, peaking at8.054397m. This is a5.44cm envelope excursion, not an authorised two-wheel verge pass. Keep the existing failure; the separate shared surface/track-limit feature has not been implemented.

The aggregate improves only2.166138 car-seconds and crosses the predeclared2215s review target, while the car whose traffic performance motivated this work loses2.882629s. Peak impact speed also moves further beyond the6m/s review threshold. Those results do not justify promotion or replacing the desktop55 reference. Occasional racing contact remains acceptable in principle; this decision is based on the combined pace, containment and severity evidence, not a new zero-contact rule for the whole field.

## What the evidence establishes

There are2895 retained-trajectory attempts:2662 accepted and233 rejected by the physical checks. The capped optional audit captures1530 decisions,1444 marked retained trajectory;980 of those preserve positive consumed progress.1789 additional audit records are dropped because each car has a64-entry limit. The audit therefore proves exercised paths and permits individual replay, but is not a complete statistical sample. Full counters remain available independently.

For car0, eligible moving pedal samples rise850 to879; braking samples rise50 to96. Final limiting stages change from24 to76 moving-leader samples,8 to3 immediate-safety samples, and18 to17 follower samples. These are same-tick controller-stage measurements, not proof any individual brake was unnecessary. Completing an existing trajectory more consistently has not solved the traffic restriction in this field. At1s its lateral position barely differs (3.533188m control versus3.535080m candidate), and at5s its speed is lower (20.281088 versus16.826530m/s).

Whole-AI time including snapshot/worker joins is3.133159ms per snapshot for control and3.179795ms for the candidate. The candidate enables capped continuation recording while control does not; both include pedal diagnostics. These hidden timings are not a matched performance or rendered-frame acceptance claim. No new visible run is needed for a candidate rejected on behaviour;57's unresolved rendered frame/AI budgets remain open.

## Next AI work

Keep the feature experimental and default-off. Use the recorded early pack states to inspect the interaction between chosen passing destination, achievable lateral motion and the moving leader's actual predicted occupancy. Compare a useful pass with continued following from the same state before changing selection or manoeuvre geometry. The60 result does not establish that repeatedly restarting a trajectory was the dominant traffic delay, nor does it justify weakening emergency braking or road checks. Preserve the clean queue, merges, braking leader, corner and no-room fixtures as independent constraints.

The human67.46s win remains observational evidence of usable vehicle performance and deliberate edge use; it is not an equal-rules target to copy. Driver personality, legal edge/surface handling, frame-time performance, pre-race UI and adventure integration remain separate pending work. Hitbox work stays parked. Desktop remains the user-reviewed55 build.

Evidence: `trajectory-60/comparison.json`, raw reports and analyzer outputs in each arm, `trajectory-60/regressions/analysis.json`, `trajectory-60-expectations.md`, and the source/assembly manifest. Reproduce the concise comparison with `tools/analyze-pass-trajectory.py review/unity-35/trajectory-60`.
