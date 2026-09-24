# Worker-side ranking106

Retain106. Move the existing prospective candidate ranking into the persistent forecast workers, then join before authoritative selection. Measured whole-AI wall cost improves18.39% on Bayview and17.19% on Cedar. Both full-field fixtures preserve recorded racing and cumulative dynamic-planner diagnostics exactly. Performance acceptance remains open.

## Implementation and correctness

DynamicForecastPreparation owns a copied observation/traffic snapshot, prior path and prospective planner. The worker ranks candidates and constructs the unchanged physical forecast for the best finite candidate. Final selection uses105's full-input, single-use ranking reuse after the join. RoadPathFeasibility matches the completed worker's actual request because the winning path is now discovered on the worker. Scene queries and authoritative commands stay on the main thread. No physics, update-rate, vehicle, path-selection or visual-quality changes.

The pool rejects shared ranking buffers as well as shared prediction buffers. Owner/active guards, exceptions, joins and disposal remain enforced. No finite candidate supplies no completed physical forecast; ordinary serial fallback remains available. Diagnostic dynamicForecasts counts constructed finite requests, while completedForecastJobs counts successful physical forecasts. Summed worker elapsed now includes ranking; it is contained inside joined snapshot wall time and must not be added to whole-AI wall cost.

506 editor checks pass;168 source/script inputs and the exported assembly are frozen. New checks compare four reordered24-job serial/worker batches across loadouts, profiles, drafting, traffic and prior-path states, including exact ranked curves, physical samples and interval bounds. Mutating the caller's traffic array after capture does not change owned work. Checks cover alias rejection, no finite candidate, a joined worker exception and subsequent successful reuse, plus zero warm dispatch/worker allocations.

105→106 stored motion, checkpoints, impacts, recoveries, overlap peaks, track/context samples and results match on both courses. All24 finish, all processes exit0 and no runtime errors appear. The separate phase analyzer confirms exact cumulative dynamicRejoin diagnostics. These are fixture-scoped results, not proof of every possible race.

## Matched rendered evidence

One sequential pair per track,105 then106; normal speed, fixed1920x1080,120fps cap, vsync off, HUD on, nondevelopment, same mixed-driver24-car field and seed. No competing editor/game or screenshot capture during timing. No frames dropped or resolution changes. Only the first loading observation is omitted; remaining startup/countdown stays in the statistics. This is not a statistical multi-machine benchmark or subjective smoothness acceptance.

| Measurement | Bayview105 | Bayview106 | Cedar105 | Cedar106 |
|---|---:|---:|---:|---:|
| Whole AI ms/physics step |3.8172|3.1154|3.0549|2.5298|
| Mean frame ms |12.308|10.875|10.299|9.532|
| Frame p95 ms |37.283|26.836|23.804|16.021|
| Frame p99 ms |48.753|38.055|41.278|33.296|
| AI per-frame p95 ms |34.836|24.490|22.170|14.218|
| Worst one-second mean frame ms |38.918|36.143|17.308|15.326|

Cedar narrowly meets16.7ms p95 and33.3ms p99 in this pair; p99 has only0.004ms margin, so this does not establish durable acceptance. Bayview misses both, and both tracks miss3ms AI-per-rendered-frame p95. Per-physics-step means are different statistics. Startup maxima remain2.23s Bayview/0.62s Cedar. Warm measured main-driver/worker allocations remain0bytes.

Main prospective ranking falls to zero; workers perform8657 Bayview and21840 Cedar rankings, all reused live. Worker ranking costs4.336s/7.573s cumulatively. Post-loading dispatched jobs equal completed physical forecasts10256/24129; ranking-job counts8633/21816 exclude the initial24 jobs omitted by the frame analyzer. No jobs silently disappear. Serial fallback forecasting remains3.157s/4.027s and live following2.288s/5.275s; those are remaining main-thread targets. All-car planning bursts remain a frame-tail concern.

## Milestone review and next work

Visual quality: accepted101 showroom/HUD/world unchanged. Driving/racing: exact fixture parity, including incidents. Progression/exploration: unchanged and incomplete. Usability: physical DualSense, small-window and current moving acceptance remain open. Performance: measurable improvement retained, budgets open. Production:106 is an intermediate export, no desktop shortcut promotion; known connected second-race recovery issue and full campaign/endurance/content requirements remain.

Next examine the cost of synchronized planning and remaining physical forecast/follower work before choosing another bounded performance change. Preserve full-rate steering and immediate safety. Any deliberate scheduling change would alter observation times and must be labelled and evaluated as such, rather than claiming exact-race parity. User order remains performance first, then fresh online racing-AI research before collective-pace changes. Goal remains active and incomplete.

All build/race processes are terminal. Frozen source, assembly, archived tools, raw four-arm evidence, phase-summary and manifest accompany this review.
