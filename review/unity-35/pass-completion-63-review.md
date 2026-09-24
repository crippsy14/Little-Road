# Small passing-target returns: reject promotion

63 extends the checked traffic-aware continuation comparison to a small target change that returns into an ahead slower car's current following corridor, when the previous destination clears it. It is opt-in through `--compare-pass-completion`;60 trajectory retention remains disabled. The previous destination is freshly anchored and must pass the unchanged physical checks, then exceed the new path's forecast progress by more than one body length. No hitbox, car tuning, risk setting, track limit or emergency response changes.

363 build checks pass. With the feature disabled, the entire recorded62 field matches exactly, including motion, events, technical/context samples and car results. The source and runtime assembly are frozen in `source-63` and `prototype-63-manifest.json`.

## Measured outcome

| Matched diagnostic field | Control | Small-return comparison |
|---|---:|---:|
| Finishers |24 |24 |
| Combined car-seconds |2215.597137 |2221.770630 |
| Car0 finish |4th,85.501198s |11th,93.049355s |
| Car0 checkpoint1 |19th |19th |
| Car0 checkpoint4 |37.134041s |42.985291s |
| Field impact records |10 |12 |
| Peak relative impact speed |8.087913m/s |8.202739m/s |
| Sampled departure / physical recovery |0 /0 |0 /0 |
| Unresolved context samples |375 |513 |
| Car0 moving-race speed below2m/s after8s |0 samples |29 samples |
| Whole-AI mean per snapshot, including worker joins |3.126007ms |3.785790ms |

The field is6.173492 combined seconds slower; car0 loses7.548157s. All12 field checks and13 checkpoint-flow deadlines pass, but the unchanged2215s collective and6m/s impact review thresholds are missed. Diagnostic timing rises about21%; this is an audit-enabled hidden comparison, not rendered acceptance. Both arms include opening and pedal recording.

Controlled tests pass141/142. The sole failure remains the old no-room post-wall completion allowance: car0 takes33.266548s. All ten controlled scenarios finish without runtime errors, contacts, sampled departure or physical recovery, and the independent corridor contract passes. Clear-road paired street and club times remain identical. Keep the failing timing gate; do not substitute completion alone.

The opening recorder remains bounded to40s:131 records, no drops. Because the candidate reaches checkpoint4 at42.985s, its recording coverage check now fails10/11. This failure is preserved; the normal reference coverage passes. Do not claim the candidate's opening capture includes that later checkpoint.

## What changed and what did not

The new trigger runs1571 times across the field,173 for car0. Car0's traffic-aware score comparisons rise50 to177; accepted continuation counts rise39 to78. Many extra comparisons therefore add work without improving this race. The first car0 physical-sample difference occurs at17.508165s, later than the motivating opening straight; its two recorded contacts at5.183352s and12.816569s match the control exactly. The test does not improve the original early passing example.

Of car0's29 late slow samples,22 have an unresolved road plan and only one has active immediate safety. The sampled near-stops begin around23s and recur around34.6s. This points toward path selection/feasibility as the next specific investigation, rather than a blanket relaxation of emergency braking. It does not prove each rejected path should have been accepted.

Source inspection confirms a relevant model mismatch: `RoadPathFeasibility` checks a nominal own-car forecast without moving-leader control, while the live driver and separate traffic-aware progress scorer apply that control. Side-entry checking already exempts same-corridor following, but interactions with other cars can still differ when the car actually slows behind a leader. A nominal rejection can therefore merit review; it must not simply be ignored. The separate progress scorer also omits live emergency control and future replanning, so a favourable short score is not a guarantee that the subsequent position will retain a viable path.

## Next work and retained reference

Do not promote63 or tune its trigger repeatedly against this one field. Retain the recorded unresolved car0 states around23s/34.6s and reproduce their exact nominal rejection before comparing the same paths with the shared following controller. Measure whether rejected side-entry/corridor outcomes change under actual traffic-limited motion, preserving road envelopes and observed opponents. If that reveals a viable model correction, prove it with the existing clean merge/braking/corner/blocked-road cases and current-step forecast ownership checks before another field run. Account for worker ownership and cost; do not add unbounded nested traffic rollouts.

This is a bounded investigation of a concrete mismatch, not authorisation to rewrite the AI framework or add more destination-persistence rules. Keep small-return comparison and60 trajectory retention default-off. Desktop55 remains the player-reviewed race-feel reference. Driver personalities, frame-time improvements, pre-race loadouts/UI, adventure integration and the full campaign scope remain open; hitbox work stays parked.

Evidence lives in `pass-completion-63`: both raw fields, exact disabled parity, controlled analysis, opening analysis, preview/checkpoint reports and `comparison.json`. The predeclared expectation document retains all thresholds and limitations.
