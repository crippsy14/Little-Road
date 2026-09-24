# Forecast/ranking performance103

24-car Bayview and Cedar rendered comparisons support retaining103: total measured AI cost per physics step improves19.63% and17.31%, respectively, with identical recorded racing. This is a performance increment within the full campaign goal, not performance acceptance or campaign completion.

## Change and correctness

ForecastPathCache generates the original half-metre geometry samples on demand, retaining the same200-metre lookup window and exact interpolation. Every Prepare invalidates all entries and copies the current path; only its own forecast instance uses those buffers. The ranking follower computes each relevant rival's route distance and route-relative velocities once per observation rather than repeating them at each downstream sample. Arrival-dependent positions/constraints still update per sample, and traffic iteration order remains unchanged.

No physics-rate, decision-cadence, personality, safety-margin, path-ranking formula, graphical-quality or roster changes. No stale observation reuse. The original immutable101 binary is the baseline.

499 editor checks pass. An editor-only frozen101 eager-cache oracle confirms exact output over110,700 varied guide/rejoin/offset/consumed-distance queries, including the loop seam and per-path invalidation; unconsumed geometry is skipped. Existing prediction/driver/recovery tests remain. Source165 inputs and assembly identity are frozen.

The first102 build stopped in the newly added exact oracle check, before player export: its independently regrouped math expression did not match exactly. The runtime optimization was unchanged for103; replacing that expression with the actual frozen101 implementation, including intermediate table storage, passed. Preserve build-context-102.log; do not claim102 built or ran successfully. The frozen oracle is editor-only and does not ship in the player.

Both24-car comparisons have exact equality of stored motion, track/context samples, ordered checkpoint events, impacts, assisted recoveries, overlap peaks and all car race results. All24 finish in every arm, normal exits and no runtime errors. Terminal export pose and profiling timestamps remain outside recorded-parity scope. These two fixtures do not prove every campaign case.

## Rendered evidence

Each track runs101 then103, sequentially, nondevelopment, normal speed,1920x1080,120fps cap, vsync off, HUD on, same24-entry mixed-personality field and seed. No project editor, competing simulation or screenshot capture during timing. Every observation stays1080p and no observations are dropped. First loading observation omitted consistently; countdown and other startup stalls are retained. One matched pair per track is exploratory evidence, not a statistical multi-machine benchmark.

| Measurement | Bayview before | Bayview after | Cedar before | Cedar after |
|---|---:|---:|---:|---:|
| Whole AI ms/physics step |5.7582|4.6279|4.4229|3.6571|
| Mean frame ms |20.601|14.644|13.414|11.379|
| Frame p95 ms |70.387|50.384|40.196|33.006|
| Frame p99 ms |117.168|62.984|72.749|50.040|
| AI per-frame p95 ms |67.552|48.172|38.377|31.139|
| Worst one-second mean frame ms |152.958|42.662|138.937|25.825|

Whole-AI cost includes snapshot preparation, joined worker wait and final driver decisions; summed worker elapsed is not added again. Warm measured main-driver and worker allocations remain0bytes. Allocation coverage excludes rendering/HUD and other systems.

The16.7ms frame-p95,33.3ms frame-p99 and3ms AI-p95 budgets still fail on both tracks. The retained startup maximum is around2.25s on Bayview and0.64s on Cedar. There are still significant planning spikes. Lower average/tail times are not a claim of consistently smooth gameplay. User moving-review feedback is pending at this record's creation.

## Milestone review and next action

Visual quality: accepted101 showroom/HUD assets and quality settings unchanged; no new screenshot or subjective visual acceptance claimed. Driving/racing: exact recorded behaviour preserved in both full-field fixtures, including existing incidents. Progression/exploration: unchanged; no new content, balance, save or duration claim. Usability: windows remain fixed1080p for measurement; physical controller and wider accessibility acceptance still open. Performance: useful17–20% whole-AI improvement, budgets unmet. Production readiness: intermediate build, no desktop shortcut or production promotion; full campaign scope remains required.

Continue with remaining main-thread planning bursts, especially repeated prospective/live route ranking and physical forecast work; measure specific phases before a larger concurrency change. Preserve the physical checks and racing feel. Track connected second-event recovery separately rather than disguising it as a performance fix. Endurance/pits, progression, teammates, wider world/body/UI content and12-hour first-time-playtest acceptance remain open.

Evidence: frame-cost-102 contains101/103 Bayview/Cedar rendered pairs, per-run analysis/parity and summary.json. Build103 log/checks, source103, validation-tools103 and prototype103-manifest.json retain the corresponding code/tools/identities. All four test processes and the editor are terminal.
