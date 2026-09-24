#108: three planning groups, broader validation

106 remains the unchanged-racing performance baseline.108 keeps scheduling opt-in; it is available for moving review, not promoted as a fully accepted replacement.509 checks pass, including six group settings across six fixed-step rates;170 inputs and assembly are frozen. No handling, equipment, graphics or immediate-control changes. See107 for why tiny batches were too costly and108-expectations for the declared gates.

## Three paired seeds on each course

Twelve sequential normal-speed1920x1080 runs,108 off/on, seeds1709/1710/1711, same mixed24-car field and graphics. No competing editor/game or screenshot capture. All24 finish in every arm; normal exits, no runtime errors, no dropped/resized frames or warm measured main-driver/worker allocations. These are one-machine measurements, not confidence intervals or human acceptance.

| Track | Frame p95 off range | Frame p95 on range | Frame p99 on range | Whole-AI joined wall increase |
|---|---:|---:|---:|---:|
| Bayview |26.80–28.98ms|16.50–18.15ms|21.75–24.36ms|6.01–8.75%|
| Cedar |16.14–18.39ms|11.60–13.84ms|18.88–19.15ms|6.12–8.47%|

Warm route plans per-rendered-frame p95 falls24→8. The improvement is smoothing bursts, not reducing total joined AI wall cost. Per-frame AI p95 still misses3ms. Bayview misses16.7ms in two seeds; all candidates meet33.3ms p99. Startup maxima remain and are not hidden by the steady-state statistics.

| Seed | Bayview collective gain | Bayview car0 gain | Cedar collective gain | Cedar car0 gain |
|---|---:|---:|---:|---:|
|1709|6.319s|1.325s|1.225s|3.832s|
|1710|17.745s|1.025s|1.325s|4.207s|
|1711|-23.821s|2.159s|1.233s|1.700s|

Positive means faster; collective values are summed car-seconds. Bayview mean collective gain is only0.081s despite its6.319s median; Cedar mean is1.261s. Do not advertise the best seed or car0 gain as a general field-speed breakthrough. Cedar1709 car0 is85.276→81.444s;1710 is84.148→79.941s. Scheduling changes observation times, so these are not deliberate pace-rule gains.

## Incidents and failed gates

Only Cedar1711 passes every declared108 gate. Other failures remain in schedule-comparison.json. Bayview has one recurring two-car opening entry, cars23/0 near6.908s, reported relative speed10.71m/s across all three seeds. Cedar contact records are4/2/0, with peak relative speed8.806/13.669/0m/s. These exceed the provisional5m/s severity gate in two seeds. Contact speed is not impulse or actual speed loss; the present event schema cannot establish mechanical severity. Candidate sampled car/environment overlap remains zero in all six races, but sampled overlap cannot prove no between-sample contact.

Bayview1711 car8, with no contact record, enters physical recovery near30.491s/progress647.60m and returns to Racing near45.794s;15.308s in recovery, including4.300s waiting and2.242s reversing. It finishes46.069s. This explains a material field-time regression needing investigation; no assisted teleport or missed-gate increase occurs. Occasional racing contact is allowed by the user. The concern is understanding repeatable encounters and lost time, not enforcing a zero-crash game.

Off-versus107: Cedar exact parity passes. Bayview motion/checkpoints/track/context/overlap/recovery and finishing times/ranks match, but contact callbacks differ as in107's off/106 comparison. Strict event/result parity failures remain; do not remove contact counters from the verifier to make them pass. The cause of callback variability remains unestablished.

## Decision and next action

Keep106's proven17–18% worker-ranking saving and keep108 scheduling as a measured option for review. No default/desktop promotion yet. Before deciding scheduling retention, inspect the opening contact's impulse/actual speed response and the isolated recovery; do not label relative contact speed alone a severe crash. A separate exact performance opportunity identified in source is repeated identical right-vector/rotation calculations in swept collision checks, used by both immediate safety and traffic path feasibility. Reuse immutable per-query values without changing arithmetic, thresholds, update times or commands, and validate against frozen math plus recorded racing.

Fresh online research is complete in docs41. Larger pace work remains queued after the performance decision: attribute lost throttle/braking and compare physically feasible candidate choices before changing margins. Visual quality, ownership/progression, exploration and broad campaign scope are unchanged; physical-controller/small-window and complete performance acceptance remain open. Full goal remains active.

## User preview and archive status

The user asked for a peek. cedar-user-preview ran108, Cedar seed1709, three planning groups,24 cars. It started at1920x1080 and then switched to3840x2160 borderless fullscreen; it is a viewing run, excluded from the matched performance pairs. Session14978/game20352 subsequently exited0. All108 processes are terminal. matrix-summary.json contains all six comparisons, including failures; the source-matched archive includes the completed preview. No shortcut changed.
