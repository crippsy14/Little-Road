# Guarded ranking reuse105

Retain105. Phase measurements found the same local candidate ranking repeated before worker prediction and at final selection. Reusing that result only after full input checks removes roughly4.17s of duplicate ranking on Bayview and7.26s on Cedar. Total measured AI cost per physics step improves17.05% and16.31%; both full-field fixtures preserve recorded racing exactly. The full campaign and performance targets remain incomplete.

## Evidence-led change

104 adds observation-only phase counters for prepared/live ranking, preferences, reference scenery and live following. Its normal-speed recorded Bayview/Cedar races match103 exactly. Bayview phase attribution found4.202s prepared ranking and4.199s repeated live ranking, compared with2.287s live following across the race. The counters measure contained regions, not additional time to add to whole-driver totals.

105 lets the speculative planner own a copy of the observation identity, prior path and traffic. Final selection reuses its small ranked candidate arrays only when time, step, self state, tuning, path, traffic values/order, driver profile, drafting/drag reduction, driving state and planning mode match. Mismatch consumes/invalidates the preparation and follows the original recalculation path. New preparation invalidates old input, including early exits. Reuse is single-use. Final physical forecasts, constraints, fallback selection, decision timing and commands stay authoritative. No changes to car performance, aggression, physics rate or visuals.

Live candidate/evaluation/rejection and selection-history counters remain independent of speculative work. Main-thread selection copies result arrays; no live mutable planner buffers enter workers in this version. Source and live planners must share the same immutable profile object and configuration. Dynamic planner/route data follow the existing synchronous immutable-snapshot contract; future runtime track/profile mutation would require explicit invalidation/versioning.

## Correctness

104:499 checks,165 frozen inputs.105:502 checks,166 frozen inputs. Targeted checks compare solo/traffic ranking with/without prior paths across repeated preparation: all ranked curves, costs, clearances, fallback ranks and cumulative live diagnostics match independent fresh calculation. Adversarial checks reject changed input families, a reused mutable traffic buffer, driver profile and mode; invalidated/consumed preparation cannot be reused. Warm preparation/reuse has no managed allocation.

103→104 and104→105 both preserve stored motion, track/context samples, checkpoint passages, impacts, assisted recoveries, overlap peaks and all car race results on both tracks. All24 finish in every arm without runtime errors. The105 dynamic-planner summaries also exactly match104. This remains fixture-scoped evidence; terminal export positions and profiling times are excluded. Original safety/recovery tests remain; there is no new campaign save or endurance acceptance claim.

## Rendered comparisons

One sequential pair per track,104 then105, normal speed, nondevelopment, fixed1920x1080,120fps cap, vsync off, HUD on, same mixed-personality24-car field/seed. No project editor, competing simulation or screenshot capture during measurement. All observations remain1080p and none are dropped. First loading observation omitted consistently; remaining startup/countdown observations retained. These are matched engineering runs, not a statistical multi-machine benchmark or new subjective moving acceptance.

| Measurement | Bayview104 | Bayview105 | Cedar104 | Cedar105 |
|---|---:|---:|---:|---:|
| Whole AI ms/physics step |4.6324|3.8426|3.6482|3.0533|
| Mean frame ms |14.642|12.346|11.364|10.297|
| Frame p95 ms |50.534|37.676|32.812|23.877|
| Frame p99 ms |63.182|48.708|49.667|40.984|
| AI per-frame p95 ms |47.939|35.200|31.088|22.201|
| Worst one-second mean frame ms |42.179|38.640|24.686|17.213|

Bayview reused8657/8657 rankings: live ranking4,199.05→29.90ms. Cedar reused21840/21840:7,333.82→70.93ms. Prospective ranking remains4.226s/7.365s in105 and is the next measured concurrency candidate. Warm measured driver and forecast-worker allocations remain0bytes. Whole-AI cost includes snapshot/preparation/worker join and decisions; summed worker elapsed is not added again.

Both tracks still fail16.7ms frame-p95,33.3ms frame-p99 and3ms per-rendered-frame AI-p95. Cedar's3.053ms per-physics-step mean is a different statistic and does not meet that gate. Startup maxima remain around2.22s Bayview and0.63s Cedar. Do not claim smoothness or production readiness from the improvement alone.

## Milestone review and next action

Visual quality: accepted101 UI/world/body quality unchanged. Driving/racing: fixture parity including incidents, no new AI-style tuning. Progression/exploration: unchanged and still incomplete. Usability: no new physical-DualSense/small-window acceptance; earlier moving-feedback request remains pending. Performance: retain this16–17% further reduction, gates open. Production: intermediate105, no desktop shortcut promotion; full12-hour campaign, endurance/pits, teammates, progression, reusable content and playtest requirements retained.

Next assess preparing/ranking candidate curves inside the existing owned forecast workers, with snapshot/input ownership, join boundaries, exception handling and measured main-thread cost. Keep physics/scene queries and authoritative decisions on the main thread. Scope this as one bounded concurrency change with exact recorded comparisons; do not rewrite the racing model or lower checks to chase frame budgets. The separate connected recovery defect remains open.

All editor/test processes are terminal. Frozen104/105 source/assemblies, archived runners/analyzers, raw frame/race evidence, phase-summary.json and manifests accompany this review. The phase instrumentation itself was verified before the optimization.

### Latest user sequencing

The user explicitly requests continuing performance until it is satisfactory, then a fresh online review of racing-AI techniques before improving combined field time and racing pace. Performance remains first; research precedes pace changes. Retain the agreed racing feel and judge future pace through combined time, player pace/overtaking, incidents and moving review rather than rewarding cautious traffic flow.
