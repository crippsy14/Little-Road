# Shared rival prediction preparation: exact behaviour, modest hidden saving

67 avoids repeatedly projecting the same observed rivals during one snapshot. The opt-in `--shared-observed-traffic` cache matches the complete ordered observed state, timestamp, circuit identity, physics step, control interval and horizon. Each caller retains its own copied result. Prediction arithmetic, planning cadence, following control, emergency checks and physical rules are unchanged. The optimisation remains default-off pending rendered performance validation.

All 395 build checks pass, including 24 new checks for exact serial/shared projections, changed-key invalidation, caller-copy isolation, thread ownership and zero warm managed allocation. Source and assembly hashes are frozen in `prototype-67-manifest.json` and `source-67`.

## Matched hidden results

Four sequential runs use the same 24-car Cedar field, seed1709, upgraded rear-grid car, mixed equipment and 4x playback. Detailed opening/pedal/following/continuation event audits are disabled in both arms; driver-frame and standard race telemetry remain enabled. Timings include snapshot preparation and worker joins, not just individual driver decisions.

| Configuration | Serial mean AI / snapshot | Shared mean AI / snapshot | Observed reduction | Combined race time, both arms | Preview car, both arms |
|---|---:|---:|---:|---:|---:|
| Reference driving |3.094463ms |3.006421ms |2.845% |2215.597137s |85.501198s, fourth |
| Following fallback enabled for all |3.370053ms |3.268932ms |3.001% |2207.115875s |87.650673s, fourth |

All 24 cars finish in each run. Both new serial arms reproduce their66 counterparts exactly across every stored motion/event/technical/context/result comparison. The following serial/shared pair also passes every raw parity comparison. The reference shared arm has identical earlier motion, checkpoint passages, impacts, recoveries, overlaps, technical/context samples and race results, but its raw motion comparison fails because the serial export includes24 additional samples after all cars finished. The retained supplement verifies the entire shared motion prefix exactly and places every extra sample at101.005745s, after the final finish100.830788s. This is a documented post-finish export difference, not an unconditional raw-parity pass.

All four runs have zero recorded driver/snapshot/worker managed allocation after simulation second5, no dropped frame records, normal exits and no detected runtime errors. Hidden 4x measurements do not satisfy real-time rendered-frame acceptance. One matched pair per configuration demonstrates an observed saving, not a confidence interval or a guarantee across hardware and race seeds.

The reference shared cache records347 hits /546 misses and builds12,790 individual rival projections, versus20,539 projections implied by893 serial preparations of23 rivals. With following fallback enabled, progress comparison records444 hits /495 misses and11,648 projections; following fallback records116 hits /117 misses and2,761 projections. Combined14,409 projections replace26,956 serial projections implied by1,172 preparations. These counts support reduced duplicate work; measured whole-AI time, rather than the cache hit rate, is the performance result.

## Retained limitations and next work

Keep the implementation as an opt-in candidate. The desktop shortcut still launches player-reviewed55. No hitbox or live driving adjustment was made in67. This optimisation preserves65/66's driving tradeoffs:8.481262 fewer combined car-seconds, but a2.149475s slower preview car, more contact records and the existing controlled blocked-road timing miss. The asymmetric66 scope experiments' body-boundary failures also remain open.67 did not rerun or newly pass the controlled suite; exact race parity and the cache-specific build checks cover this change's present evidence.

The next performance check should compare serial/shared normal-speed runs at fixed1920x1080 with the same driving configuration and detailed event audits disabled. Preserve the original AI/frame tail budgets; do not claim that a3% hidden mean reduction fixes the observed stutters. Then address the recorded AI robustness cases without making the whole field more cautious or adding special rules for the preview car. Driver personalities, pre-race UI, adventure integration and the campaign goal remain unfinished.

Evidence: `shared-traffic-67/analysis.json`, each arm's raw reports and driver-frame analysis, three full raw parity passes, reference raw parity failure plus explicit post-finish supplement, `shared-traffic-67-expectations.md`, build log and frozen source/assembly manifest.

## Subsequent normal-speed check

The planned fixed1080p comparison is complete: exact recorded race parity,4.03% lower whole-AI mean per snapshot and frame p9955.96ms to49.49ms. All original AI/frame tail budgets still fail. See `rendered-67-review.md`; the optimisation remains opt-in and desktop55 is unchanged.
