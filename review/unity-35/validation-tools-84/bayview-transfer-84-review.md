# Bayview driver transfer84

The latest83 AI package transfers to the unchanged683.497m Bayview adventure circuit with a meaningful pace gain. This is an opt-in standalone comparison, not campaign integration. The campaign continues to use legacy-v1 and neither desktop shortcut changes.

| One standing lap, same rear-grid preview | Original adventure driver | Latest dynamic driver | Gain |
|---|---:|---:|---:|
| Club car alone |30.875s|24.458s|6.417s /20.8%|
| Club preview in24-car field |39.701s|35.209s|4.493s|
| Combined24-car finish times |903.178s|817.039s|86.139s /9.5%|
| Last finisher |40.968s|39.268s|1.700s|

Both fields finish24/24 and record312 checkpoint passages, including each start-line crossing and all gates. Preview rank improves19th to13th. These times start at countdown end, including launch from the rear grid; measured start-line-to-finish times are also stored: solo27.475→21.333s and preview in traffic32.418→29.017s. These are different clocks and should not be mixed with each other or Cedar's longer circuit.

The solo pair has no contacts, road-edge samples or recoveries. Latest field has zero recorded contacts and zero physical/assisted recoveries, but20 body-edge samples across cars8,9,14,17, between20.308 and30.808s. Maximum footprint reaches8.277m from centre,27.75cm beyond the nominal8m boundary. This remains a failed strict diagnostic road-containment criterion. The user accepts these occasional marginal excursions as part of racing. Keep raw containment failures visible, but do not make zero body-edge samples a standalone gameplay blocker or introduce extra caution solely to eliminate them. Distinguish brief marginal excursions from sustained off-road driving, shortcutting/checkpoint bypass, loss of control or repeated recovery; broader track-limit rules remain to be specified. Original field has zero edge samples but232 contact-participant records, peak10.216m/s; these are repeated participant reports, not232 distinct crashes. No claim of overall fault-free acceptance.

Build84 adds only the opt-in Bayview test adapter, a compatible geometric racing-line asset and its validation. Bayview keeps its actual Circuit, TrialWorld, vehicle physics and grid. Both arms share one executable, body models, collision settings, tuning, equipment distribution, seed1709 and120Hz physics. The baseline uses the frozen legacy decision equations and cached observation order used by adventure; latest retains shared snapshots, physical recovery and light drafting. Thus the comparison concerns complete driver packages; it does not isolate the dynamic line from every other AI improvement. Solo removes traffic/drafting opportunities.

The new reference uses the same bounded bending-energy/cubic-spline approach as Cedar:~8m controls,24000 iterations,4m offset bound. It was not tuned against lap results. The build's479 checks include conservative reference-body clearance, finite stock/club speed profiles and rejection when Bayview geometry changes. The generated line and source are frozen separately from the older Cedar reference. A geometric seed is not a certified optimum.

Matched setup and exit/error checks all pass. This is one deterministic seed and one lap per condition, sufficient for an initial transfer comparison but not multi-seed balancing, personalities, endurance or rendered-performance acceptance. The Cedar24-car control passes exact equality of stored motion, track/context samples, checkpoints, incidents, recovery, overlap peaks and car results against83; the adapter preserves the prior racing behaviour. Serial forecasting performance remains unresolved.

User feedback on83 is retained: best racing yet, one unnecessary-looking pinch brake and a missed outside pass through a flowing corner. Outside-corner pass choice remains a future diagnostic; no controller tuning for those observations is bundled into84. The broader twelve-hour adventure scope remains active and incomplete.

Evidence: `bayview-transfer-84/comparison.json`, `comparison.md`, four raw runs with track telemetry/starting grids/launch metadata, `bayview-transfer-84-expectations.md`, build84 log/checks, source84 and prototype84 manifest.
