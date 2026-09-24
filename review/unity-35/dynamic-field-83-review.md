# Prototype 83: field completion restored with faster collective pace

83 restores **24/24 finishes with no physical recovery**, improving complete-field time from retained 81's 2179.422638 to **2136.924683 car-seconds**, a **42.497955-second gain**. Preview time is effectively unchanged: 86.609261 to **86.559273 seconds**, with finish position fourth to seventh as other cars improve. The approximately 80-second preview target remains open. This is a useful experimental milestone, not production promotion or completion of the racing/game scope.

## Bounded change

Keep 82's blocking-leader passing proposals, but replace the second physical-check choice after a moving-traffic rejection when a cheap geometric alternative appears clear. The proxy reuses the primary forecast's longitudinal schedule and existing observed-opponent projections, substitutes the alternative desired curve, and tests swept geometry over the existing short traffic horizon. It ranks candidates only: a selected alternative must still pass the unchanged physical road, traffic and scenery checks. If the proxy finds none, the old fallback remains. Six candidates and two detailed physical checks remain; power, grip, equipment, grid, following and emergency limits are unchanged.

The proxy is imperfect because desired geometry is not actual tracking and another curve can have a different speed schedule. Exact replay verifies 6/6 original checks in the two initial states and 112/112 in 41 additional captured states. It finds a physically accepted alternative in both initial misses and ten additional misses. Nine additional states still have a viable alternative that the shortlist misses. These are deliberately captured failure states; improvement here is not a general false-positive/false-negative rate or proof of an entire manoeuvre.

477 build checks pass, including parallel-versus-crossing geometry, warm zero allocation and no mutation of the physical forecast. The 146-file source archive and manifest identify the executable. Flag-off reproduces every stored motion, track/context sample, checkpoint and impact of rejected 82, plus official finish results. Raw parity remains failed for two incomplete-car-9 recovery clocks: their differences exactly equal the 0.033325-second difference between timed-out exports. That narrow supplement is recorded separately; the raw failed report remains.

## Complete-field comparison against retained 81

| Measure | Watched 81 | 83 |
|---|---:|---:|
| Finished | 24/24 | 24/24 |
| Combined official time | 2179.422638 s | 2136.924683 s |
| Preview lap / place | 86.609261 s / fourth | 86.559273 s / seventh |
| Preview first-checkpoint rank | 24 | 16 |
| Preview arrival at 280 m | 18.567 s | 17.160 s |
| Club average throttle, 0–200 m | 44.1% | 62.6% |
| Club time at >=95% throttle, 0–200 m | 0.0% | 29.8% |
| Preview average throttle, 0–200 m | 39.2% | 69.5% |
| Physical recoveries | 0 | 0 |
| Body-over-road-edge samples | 0 | 3 |
| Impact participant records | 6 | 12 |
| Peak impact relative speed | 10.429023 m/s | 5.311061 m/s |
| Unresolved planning samples | 371 | 340 |
| Braking fraction, active observations | 8.190% | 7.657% |
| Hidden whole-AI mean per snapshot | 6.329354 ms | 6.188379 ms |
| Warm measured driver allocation | 0 bytes | 0 bytes |

All 13 checkpoint deadlines pass; field checks are 11/12. The failed check is three body-over-edge samples for car 6 at 17.708–17.908 seconds, progress 357.54–360.59 m, reaching **5.76 cm beyond the nominal 8 m boundary**. This is small but remains a real failure of the declared screen. There is no pose penetration or recovery in the field. Contact frequency is higher but peak severity lower; do not turn that into an automatic risk/personality acceptance.

The geometric fallback runs 874 times, examines 2513 alternatives and supplies 657 second-check choices; its measured total cost is 128.716 ms over the hidden race. Overall AI cost remains high. The small mean difference versus 81 is not a dedicated performance improvement claim. Serial forecast work and rendered spikes remain open.

## Controlled regression result

All eight two-/three-car fixtures finish with zero contacts, sampled departures or recoveries. Seven have identical gate and finish times to retained 80 (the same controlled dynamic behaviour retained by 81). The three-car corner gains no pace: combined gate time is 0.186300 seconds slower and remaining-course finish sum 0.099926 seconds slower. It has five unresolved samples versus one. The two prior strict pass-confirmation deadline failures remain. The analysis screen therefore still fails; no additional controlled median pace gain is claimed. Equal timing alone is not a claim of exact motion parity.

## Moving review and next priority

At the user's request, `view-on` completed after the controlled matrix finished, with no competing game simulation. It matches hidden83 exactly for all recorded motion, track/context samples, checkpoints, incidents and car results. The user judged this the best racing yet, while noting an unnecessary pinch brake and a missed outside pass on a flowing corner. These remain observations for diagnosis, not confirmed causes. Render capture spans1920x1080 and3840x2160: frame p95/p9973.975/110.861ms, whole-AI p95/p9971.976/104.188ms;0 warm driver/worker bytes. Performance budgets fail and this is not a fixed1080p benchmark. Treat its rendering measurements and user judgement separately from hidden results. The user suggests some remaining passing difficulty may be legitimate competition as opponents use more acceleration. That is plausible, not established: retain per-car gaps, speed advantage, throttle stages and available-space evidence rather than optimising a forced finish rank or maximum throttle duty.

Next preserve the stronger opening/collective racing while addressing serial planning cost with parity checks, and inspect the small boundary excursion plus the controlled shortlist regression. Avoid delaying the wider game indefinitely for tiny metric tuning, but retain unresolved defects explicitly. Visual fidelity, body/world content, progression, exploration, UI, DualSense, saves and campaign duration have no new acceptance from this AI milestone. Personalities, race-preview integration, endurance/teammate systems and the full twelve-hour scope remain incomplete. Desktop prototype and adventure builds remain unchanged.

Evidence: expectations; replay inputs/provenance and coverage; paired raw runs and retained-reference comparison; flag-off raw parity and timeout-clock supplement; controlled matrix/analysis; opening position/throttle reports; source, build log/checks and archived tools. Windowed review remains separately labelled; user requests matched Bayview legacy/latest solo and24-car comparisons next.
