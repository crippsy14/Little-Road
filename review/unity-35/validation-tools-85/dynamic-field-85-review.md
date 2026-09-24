# Dynamic forecast workers85

Retain the opt-in worker optimization for the next personality/adventure milestone. All six recorded-parity comparisons pass exactly, whole-AI wall cost improves in the24-car cases, and fixed1080p frame tails improve. This is a bounded performance milestone, not satisfaction of the original frame budgets or the wider campaign.

## Change

ContextDriverPolicy now optionally prepares the first ranked dynamic path for the existing persistent forecast-worker pool. An independent scratch planner/preference map reads the same snapshot; prior consumed path distance advances only in a copy. Live ranking, decision timing, two-candidate physical budget, traffic/scenery constraints and recovery remain unchanged. Exact request and observed-traffic matching guard reuse; changed requests fall back to serial prediction. No lower physics rate, older observations, weaker boundaries or hidden pace changes.

The shared snapshot joins all jobs before live decisions. Each worker owns its forecast buffers and receives immutable inputs. Dynamic scratch ranking is repeated on the main thread, an explicit overhead tradeoff rather than a free speedup. Counters on the actual planner remain independent from speculation.

## Evidence

Build85:479 established checks pass;147 source inputs frozen. Cedar disabled85 matches84 exactly. Cedar enabled85 matches disabled85 exactly. Both real-time rendered arms match each other and the disabled arm matches its hidden run. Bayview solo and24-car enabled85 match their84 references exactly. Checks cover stored motion, track/context samples, checkpoint passages, impacts, assisted recovery, overlap peaks and car results; terminal pose and profiling time remain excluded by the existing verifier. No raw-parity tail exception was needed.

| Measurement | Without dynamic preparation | With dynamic preparation |
|---|---:|---:|
| Cedar hidden whole-AI per snapshot |6.166757ms|4.755544ms|
| Cedar rendered whole-AI per snapshot |6.197123ms|4.764426ms|
| Fixed1080p frame p95 |76.168ms|49.793ms|
| Fixed1080p frame p99 |113.165ms|72.245ms|
| Fixed1080p AI per-frame p95 |74.120ms|47.581ms|
| Fixed1080p AI per-frame p99 |105.759ms|70.198ms|
| Warm measured driver/worker allocation |0bytes|0bytes|

Whole-AI cost includes main-thread snapshot preparation, dispatch/join and decisions. Reduction is22.884% hidden and23.119% rendered; decision-only cost halves but that is not the total-cost claim. Hidden forecast reuse grows2134→23999 calls and serial forecasts fall24556→2804. Summed worker elapsed is not added to joined wall time or treated as CPU utilisation.

Both rendered captures stay1920x1080, normal speed,120fps cap, vsync off, non-development builds. They run sequentially without another game/editor benchmark. These are unattended engineering captures while the user walks Leah, not new subjective smoothness acceptance. Earlier mixed1080p/4K captures are not used as timing controls. Light read-only analysis occurred during the second rendered run; one pair supports an exploratory improvement, not a statistical claim across machines.

Bayview field whole-AI cost improves6.930876→6.210073ms per snapshot (10.400%). Bayview solo instead increases0.188030→0.230375ms (22.520%, an absolute0.042345ms) because duplicating ranking/dispatching one forecast costs more than it saves. Keep that tradeoff explicit; this pass targets crowded races and preserves solo behaviour.

## Limits and next milestone

The3ms AI p95,16.7ms frame p95 and33.3ms frame p99 targets still fail. The worst one-second rendered window is not improved (168.616→171.142ms mean); the optimization does not eliminate the worst congested spike. No claim of universally smooth performance or production readiness. Cedar/Bayview incidents and marginal excursions are unchanged, including the user-accepted small road-edge samples. No new pace gain is claimed; existing finish times are identical.

The user explicitly approves moving to personalities, then the all-entrant preview and adventure integration. Follow document38. Current risk preferences mostly feed older context scoring; ensure risk actually affects dynamic passing choices while keeping risk5 neutral parity. Implement braking judgement and modest deterministic form independently of equipment. Additional abilities, per-corner execution, teammate learning and familiarity remain later required slices.

Do not start another indefinite baseline-tuning loop. Retain performance debt and revisit it alongside the integrated playable milestone. No desktop shortcut or adventure build was changed. Full12-hour content, progression, world/body fidelity, parts, endurance/pits, teammate systems, controller review and campaign validation remain incomplete.

Evidence: expectations; dynamic-field-85 hidden/rendered arms and performance-summary.json; bayview-transfer-85 solo/field; build log/checks; source85; archived tools and asset hashes in prototype85 manifest. All test processes are terminal.
