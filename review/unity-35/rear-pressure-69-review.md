# Prototype69: leaders should not yield to rear pressure

The user observed leaders moving aside or slowing for an approaching rear car, and clarified the intended rule: the chasing car must find a pass; the leader keeps racing its clear line until the rival is alongside. A closing speed advantage is not a request for the leader to yield. Preserve avoidance of genuine side occupancy or crossing conflicts; do not let a passing car push the leader away merely by following closely.

## Reproduction and change

The constructed straight diagnostic reproduces an unwanted target move from-0.0708m to-3.3776m when a same-corridor car remains5,6 or7m behind, despite no longitudinal body overlap. Rear speeds12,18 and26m/s all produce it, so even a slower follower applies this pressure. The source classified rear cars inside summed half-lengths+3m as relevant and gave their lateral danger full weight. Neither the moving-leader controller nor the immediate guard owns a rear braking response. The initial diagnostic produces steering displacement, not braking; it is not a replay of the specific watched incident.

Opt-in `--rear-pressure-filter` replaces that arbitrary close-rear relevance zone with physical longitudinal adjacency or the existing side-merge reservation. A non-adjacent rear car retained solely for that reservation contributes no soft lateral danger. Ahead and alongside preferences, corridor constraints, physical forecasts, moving-side-entry checks, following and emergency response remain. Both live/prospective preference maps use the same option.

415 build checks pass. The corrected diagnostic reproduces the clear target, steering and pedal exactly for all18 non-overlapping same-corridor observations. Separate checks preserve ahead/alongside behaviour and side-merge corridor limits. The three live rear-control cases (same line, left and right approaches) finish without contact, departure or physical recovery. The lead street car completes each in29.425s and has no braking in its first50 samples (five seconds). These fixtures demonstrate the declared encounters, not universal no-yield correctness.

## Broader results and retained issues

Disabled69 exactly reproduces enabled68 across all stored race motion/events/context/results. Both full fields complete24/24 with no physical recovery and all13 start/checkpoint/finish deadlines met.

| Same24-car field | Filter off | Filter on |
|---|---:|---:|
| Combined car-seconds |2207.115875 |2214.639038 |
| Preview time / place |87.650673s /4 |91.733009s /9 |
| Impact participant records |16 |16 |
| Peak relative impact |8.463668m/s |9.155983m/s |
| Unresolved context samples |362 |758 |
| Body-boundary samples |0 |4 |

The enabled field passes11/12 field checks. Car0 exceeds the8m half-width at39.301-39.601s, maximum8.102164m, entering the short hairpin; the sampled pedal is coasting and the immediate guard inactive. This is an open10.2cm boundary excursion, not a relaxed tolerance. More unresolved choices and slower aggregate/preview pace require investigation. Faster laps achieved through unwanted yielding are not the desired behavioural target, but the new results also cannot be called a general improvement solely because the focused test is fixed.

The established ten-case controlled suite passes151/152, retaining only the previous blocked-road completion-delay failure. Every controlled case finishes with zero contact/departure/recovery and no detected runtime errors. Additional metadata checks verify the new filter and both exact-performance options in every run; the original analyzer does not itself check those newly added flags. No acceptance threshold was changed.

The user requested a viewing run. `player-review` launches this candidate at normal speed/windowed1080p, with rear view retained, after these checks; user judgement and its resulting telemetry remain pending. Desktop55 remains unchanged. This is an opt-in candidate, not final AI acceptance. Next use the viewing feedback and the crowded-field rejection evidence to preserve leader ownership while improving valid passing/continuation choices; do not restore artificial yielding just to recover an old time.

Evidence: original/corrected observations contain63 rows each (7 gaps x3 offsets x3 speeds), `observation-comparison.json`, three rear-control runs, off/on field/checkpoint reports, `regressions/analysis.json` and new-option validation, build log and frozen source/assembly manifest. The observations establish63 constructed initial decisions per arm; no statistical confidence or later-lap braking claim is implied.


## Completed viewing and new priority

The user reviewed the replay as substantially better racing, with no obvious visible logic flaws.69 becomes the new race-feel reference. The replay reproduces enabled69 raw race arrays exactly. Its resolution changes from1920x1080 to3840x2160; frame p9527.4515ms/p9953.0819ms are not a fixed1080 performance comparison. Original budgets remain failed, with zero warm driver/worker allocation and no dropped frame records. Desktop55 is unchanged.

A subsequent exact-parity decision audit (`decision-audit`) records131 car0 opening decisions without drops; the broader failed-candidate capture drops36 entries and captures nominal forecasts only. It suggests spare-space margin exhaustion and unresolved-plan straight braking around the hairpin, but does not establish a single causal fix. Retain these leads without delaying the user-requested bounded drafting/pace work and move toward personalities.
