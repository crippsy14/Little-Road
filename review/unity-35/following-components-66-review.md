# Following-fallback effects depend on the opposing field

66 adds diagnostic own-car/rival scopes and an accepted-event recorder, without changing65's ordinary enabled behaviour.371 build checks pass. Both off and all-car runs match their65 counterparts exactly across every stored motion/event/technical/context/result comparison. All accepted events have owned observations, matching selected/candidate paths and the `moving-leader` model tag; no records are dropped and acceptance counters agree. Excluded drivers never invoke the fallback.

| Same24-car field | Off | All cars | Car0 only | Rivals only |
|---|---:|---:|---:|---:|
| Combined car-seconds |2215.597137 |2207.115875 |2209.440308 |2210.540039 |
| Car0 time |85.501198s |87.650673s |85.501198s |90.716591s |
| Car0 place |4 |4 |4 |8 |
| Fallback attempts / acceptances |0 /0 |233 /9 |6 /4 |188 /15 |
| Impact participant records |10 |16 |6 |12 |
| Peak relative impact |8.087913m/s |8.463668m/s |3.650381m/s |9.098507m/s |
| Unresolved context samples |375 |362 |381 |473 |
| Whole-AI mean per snapshot |3.106847ms |3.383313ms |3.209315ms |3.374355ms |

Car0-only changes its actual decisions but leaves its finish time equal to the reference in this run. Changing rivals alone adds5.215393s to its lap. Adding the same fallback to car0 against that changed field recovers3.065918s, leaving the all-car run2.149475s slower than the original reference. This supports changed opposition as the main source of the apparent preview-car slowdown; it does not establish universally better car0 driving, additive causal contributions, or balance across many races. Each intervention changes subsequent interactions among cars.

The accepted-event chronology supports that interpretation. In the all-car run, car8 first accepts a fallback at race time15.075s, car5 at18.000s, then car0 at18.933s. In car0-only, its four accepted events are later:31.492,31.600,31.908 and32.008s. The same controller encounters different states when rivals change. Do not attribute the entire full-field time delta to a single isolated car input.

## Failures retained

All24 cars finish in every arm with no physical recovery. Both isolated arms pass all13 start/checkpoint/finish deadlines, but each passes only11/12 field checks because of sampled body departures. In car0-only, unchanged-controller car12 has four samples outside the8m half-width, maximum8.022132m. In rivals-only, car20 has three samples reaching8.084854m and car23 four reaching8.057034m. These are approximately2.2–8.5cm excursions, not authorised edge passes; keep the failures rather than change the existing limit. Overall component analysis is30/32, with these two containment failures. Original off/all-car containment remains clean.

The scopes are experimental interventions, not release options or a player bonus. Do not select car0-only because its contact count is lower.65/66 remains a candidate pending robustness, performance and player review; desktop55 stays unchanged. Exact all-car parity carries65's controlled-suite evidence (151/152, old blocked-road delay), but66 did not rerun that full suite and does not claim a fresh controlled pass.

All timings include snapshot/worker joins but are hidden, audit-enabled measurements, not rendered acceptance. The larger fields still do more AI work. Frame-time budgets remain open.

## Next bounded work

Preserve the causal result and stop adding passing heuristics to chase car0's time in one grid. First address measured prediction work while requiring exact behavioural parity. The existing owned rival rollout repeatedly constructs the same constant-velocity/fading-yaw projections for different cars at the same immutable snapshot. Investigate sharing that preparation with a strict exact-state key, while retaining each caller's owned copy, timestamp/thread/step validation and synchronous decisions. Cache misses must compute the original arithmetic; changed observations and source-buffer mutation must invalidate/reject reuse. Measure hit rate and full AI cost rather than assume reuse helps. No altered control cadence, stale decisions or broad framework rewrite.

Keep the component containment failures as explicit robustness cases for subsequent behavioural work. Driver personalities, pre-race equipment/personality UI, adventure integration and full campaign requirements remain open. Hitbox work stays parked.

Evidence: `following-components-66/analysis.json`, raw four-arm reports and accepted events, off/all exact parity reports, isolated field/checkpoint analyses, source/assembly manifest and predeclared expectations.
