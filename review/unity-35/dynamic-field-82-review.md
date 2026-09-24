# Prototype 82: blocking-leader targeting rejected after field failure

82 repairs a demonstrated omission in the passing candidate family, but fails the full-field test and is **not promoted**. The preview finishes in 81.602150 seconds, 5.007111 seconds faster than 81. Only 18 of 24 cars finish by the declared limit, however, so **there is no valid collective finish time**. The retained watchable reference is 81; desktop and adventure builds are unchanged.

## Change and evidence

An explicit flag makes passing proposals centre on the forward car whose same-corridor following limit restricts attainable speed, instead of a nearer alongside car. If no such leader exists, the old selection remains. Ranking, six-candidate/two-check budgets, vehicle forces, equipment, grid and physical road/traffic/scenery checks are unchanged. This target identification is based on potential equipment pace, not yet the speed demanded by the current corner path; that distinction deserves examination before broader use.

475 build checks pass, including mirrored outer-column and start-line-wrap proposal witnesses plus zero warm allocation. The 146-file archive and manifest identify the executable. Three captured original 81 checks replay exactly; the new outer passing proposals are faster-ranked and pass their short physical forecasts. The 82 flag-off full-field run exactly matches all stored 81 motion, track/context samples, events and results. Thus the changed field outcome is attributable to the opt-in experiment, not a changed grid or physics.

| Measure | Dynamic 81 / flag off | Targeted 82 |
|---|---:|---:|
| Finished | 24/24 | 18/24 |
| Collective official time | 2179.422638 s | Incomplete |
| Preview finish | 86.609261 s, fourth | 81.602150 s, fourth |
| Preview arrival at 280 m | 18.567 s | 16.921 s |
| First / last arrival at 280 m | 13.313 / 18.605 s | 12.519 / 17.534 s |
| Club average applied throttle, 0–200 m | 44.1% | 52.3% |
| Club time at >=95% throttle, 0–200 m | 0.0% | 18.7% |
| Field average applied throttle, 0–200 m | 55.4% | 60.0% |
| Body departure samples | 0 | 35 |
| Physical recoveries | 0 | 5 |
| Impact participant records | 6 | 12 |
| Peak impact speed | 10.429023 m/s | 4.378032 m/s |
| Unresolved planning samples | 371 | 5378 |
| Warm measured driver allocation | 0 bytes | 0 bytes |

Opening throttle and passage improve, but those gains cannot accept an incomplete race. The enabled run lasts much longer with stalled/finished cars; its lower mean AI cost is **not evidence of a performance optimisation**. No rendered 82 acceptance or controlled-matrix promotion is claimed. Further promotion tests are deferred because the first field screen already fails.

## First-corner investigation

Car 9 is unresolved and braking near progress 314 m at about 16 seconds. It slows behind car 8 and contacts it at 18.191488 seconds, near progress 339 m, at 1.956274 m/s relative speed. Subsequent contacts involve cars 11 and 13; cars 9/11/13/20/21 later remain in a queue near the same section. Several samples show backward motion during braking before recovery. This is a timeline, not proof that target choice alone caused the first contact or that braking/reverse semantics are the sole cause.

A separate 30-second instrumented `corner-audit` capture records failing selections, prior paths and pedal stages. It exactly matches the full run through the recorded 30.107973-second prefix: motion, track/context samples, impacts and checkpoint events. It is intentionally truncated, not a completion trial. Car 9 has 50 captured unresolved decisions with none dropped.

The existing editor diagnostic reproduces **6/6 original nominal checks** from car 9's first two captured failures. At clock 18.908333 s (race time about 15.917 s), the checked guide and clearance fallback both predict side entry by car 7; candidate rank 5, ending near +0.71 m, passes the same physical checks. At clock 19.841667 s, the guide and hold fallback predict side entry by car 10, while ranks 1 and 4, ending near +1.80 m, pass. This demonstrates another physical-shortlist miss rather than absence of all viable proposals. Counterfactual short-check success still does not prove the whole race will recover.

Prioritise better selection of the second physical check after a moving-traffic rejection, preserving the two-check budget and all constraints. Do not simply relax safety or globally increase throttle. Keep both the potential-pace versus actual-corner-demand distinction and the brake/reverse observation as additional hypotheses, not accepted fixes.

## Next bounded work and full scope

Preserve 81's user-approved cornering while repairing straight-line passing. Use the new opening position/throttle reports as repeatable diagnostics, then require complete-field progress and preserved corner behaviour. Diagnose the captured first-corner failure and the known shortlist omission; do not simply increase throttle or reduce collision margins. Serial planning spikes remain a separate priority for parity-preserving performance work. Personalities, adventure integration, progression, exploration, UI and the twelve-hour campaign remain incomplete.

Evidence: expectations; build log/checks/source manifest; exact source-state replay; paired raw runs and comparison; disabled exact parity; field/checkpoint/preview/braking analyses; opening positions and throttle reports; separately labelled corner diagnostic.
