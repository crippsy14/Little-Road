# Coasting review — prototype 41

22 September 2026. This is an opt-in racing prototype, separate from the desktop adventure build. The user explicitly accepts occasional contact and wants decisive racing rather than excessive caution.

## What changed

The path follower can release the throttle instead of braking when a short coast remains inside the existing speed envelope. Corner limits, vehicle physics, equipment and steering are unchanged. Traffic following and emergency braking remain authoritative. The physical forecast uses the same coasting rule as the real follower. The rejected timed rear-corridor experiment stays disabled.

## Same 24-car field

| Measurement | Retained baseline | Coasting candidate |
| --- | ---: | ---: |
| Combined first-lap finishing time | 2,239.666 s | 2,237.225 s |
| Last car finishes | 100.414 s | 99.589 s |
| Moving samples with braking | 10.851% | 6.622% |
| Moving samples with light braking | 6.189% | 3.123% |
| Moving samples with near-zero pedal | 1.766% | 6.250% |
| Moving samples with heavy braking | 0.735% | 1.131% |
| Car-pair contact events | 1 | 5 |
| Largest recorded relative impact speed | 2.769 m/s | 4.907 m/s |
| Cars finishing | 24 | 24 |
| Sampled road departures / recoveries | 0 / 0 | 0 / 0 |
| Unresolved active planning samples | 31 | 138 |

Combined time improves by2.441 seconds (0.109%); braking duty decreases by4.228 percentage points (39.0% relatively). Brake duty uses the same10Hz moving-AI population and thresholds in both runs; it is not the older brake-duty definition from other reports. Contact events are paired participant records, not twice as many crashes. These five separate events all involve cars that continue and finish; there are no recovery episodes or sampled road departures. Lower brake duty alone is not acceptance: stronger braking becomes more frequent and failed path selections increase.

All11 field checks and13 checkpoint deadlines pass. The unchanged no-unresolved gate remains2/3, an explicit defect. No claim that traffic prediction or fallback planning is solved. Field contact is reviewed by frequency, severity and race effect rather than a universal zero-contact rule.

## Controlled encounters and performance

Build303checks passes with coasting enabled. The10-run physical regression suite passes42/42 checks. Queue passing, the narrow corridor, both close rear merges, sudden leader braking and the three-car corner finish cleanly. The corridor also passes its existing13/13 independent-witness contract. Rear passes complete by3.883/3.875s against the unchanged6.146s deadline. Paired solo street/club cases preserve their reference-controller results; these partial-course runs do not establish a new full-lap solo pace.

The visible1920×1080,1× run exactly matches the hidden candidate in recorded motion, inputs, decisions, checkpoints, contacts and results. Frame p9513.009ms and p9916.200ms meet the existing frame targets. The separate3ms AI budget is still missed at11.051ms p95, and a573ms startup hitch remains. Warm measured driver/worker allocations and dropped audit frames are zero. Keep these limits visible; frame smoothness does not mean all performance work is complete.

## Decision and remaining review

Follow-up observation-only prototype42 exactly reproduces both baseline and coasting races. It attributes214/252coasting hard-brake samples to the path follower,37to the immediate guard and1to the leader limit. This explains which controller issued the pedal, not whether it was necessary. The existing cycle detector finds65->25raw brake–accelerate–brake candidates (~61.5% fewer), including potentially justified corner/traffic transitions. Do not call all65unnecessary or claim the unexplained-braking gate passed without incident review. Keep the guard unchanged while judging follower behaviour.

Retain as an opt-in candidate for moving review, with the previous smoother baseline available. The player's judgement of decisiveness, brake tapping and contact is pending. No production replacement or overall goal completion. Do not add blanket caution to remove every contact. Investigate the extra abrupt corrections/failed selections as specific behaviours if they harm the racing feel; personality variation must not conceal a universal controller defect.

Evidence: `field-41-control`, `field-41-coast`, `field-41-visible`, `field-41-repeat`, `coasting-41/analysis.json`, `prototype-41-manifest.json`, and `source-41`. All three identical-seed candidate races reproduce every stored motion/input/decision/event/result exactly, with zero finishing-time spread. This is a scoped repeatability result, not proof across hardware or varied race conditions. All build/game processes are terminal.
