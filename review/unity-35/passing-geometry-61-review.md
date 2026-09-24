# Passing geometry replay: no basis for a shorter live curve yet

The preceding goal turn made progress by completing60's controlled/full-field comparison, preserving its failures and rejecting promotion. This turn adds a reproducible editor diagnostic and evidence that changes the next AI action. Hitbox work remains parked; no live policy or vehicle tuning changes were made.

`PassingGeometryReplay.Run` reconstructs64 owned car0 observations from60's capped trajectory audit. Every original nominal forecast reproduces exactly: accepted result, road bound, route gain, exit speed and recorded position/velocity/heading. The initial editor invocation exposed a C# ref-struct capture error in the diagnostic; it was corrected by passing the observation explicitly to a static evaluator. The corrected editor invocation exits0 and reports64/64 reproduction. This is editor validation, not a new playable build or a claim that the full build suite ran again.

Each observation compares the captured retained path, freshly anchored paths with0.5/0.75/1/1.25 times the original transition length, and holding the current offset. All share the same car, target where applicable, physical model, observed opponents and two-second following forecast. Nominal road, corridor and moving-side-entry checks remain enabled. Original input, output, assumptions and all rejected alternatives are preserved.

## Results and important sample limits

The actual captures span simulation timestamps0.108333–9.391667.33 have speed below2m/s, including countdown observations;31 are moving. The64-entry limit fills early, so this is not evidence about the entire first15 seconds or a representative full lap. `summary.json` explicitly separates these populations.

For the31 moving observations:

| Alternative | Pass nominal and traffic-road checks | Mean forward gain versus normal fresh curve |
|---|---:|---:|
| Captured retained trajectory |31/31 |−0.0138m |
| Half-length fresh curve |30/31 |−0.0457m |
| Three-quarter-length fresh curve |30/31 |−0.0190m |
| Normal fresh curve |31/31 |0m |
| Quarter-longer fresh curve |31/31 |+0.0105m |
| Hold current offset |31/31 |+0.0229m |

Both shortened variants have one moving-side-entry rejection involving car20. All other reported differences are small two-second predicted distances, not lap-time improvements. No feasible shortened variant gains forward distance over the normal curve in this selected sample. Do not promote a global shorter transition on this evidence.

The shortened shape does move laterally sooner. At approximately timestamp3.98s and3.22m/s, the half-length path ends at offset4.89m rather than4.15m for the normal curve, aiming at6.29m. Yet its forward gain is6.43m versus6.49m. The driver still has to make substantially more lateral progress to complete the pass. A short forecast can penalise the initial sideways movement without seeing the later reward of clearing the leader. This is a hypothesis about scoring scope, not proof that removing the leader limit is safe.

The traffic rollout keeps observed rival speed constant and decays observed yaw. It does not know that a currently stationary/slow rival will accelerate. Stationary cases forecast only about0.9m of own progress over two seconds, so their nearly identical scores are especially weak evidence for real launch racing. Neither this replay nor its source audit simulates rival future inputs, repeated replanning, emergency control, scenery or contact impulses. Nominal acceptance is not collision certification.

## Next bounded AI action

Before another live passing change, obtain a focused post-countdown baseline capture covering car0's opening traffic through checkpoint4 without spending most of the audit on stationary decisions. Retain current physical controller and field configuration, and verify recorded behaviour parity. Use actual motion history to measure how quickly the relevant slower car accelerates and how long a checked lateral pass takes to clear it. Separate the question of choosing a passing opportunity from the emergency collision guard.

Evaluate the utility of pass completion and likely exit speed over that manoeuvre, rather than only a fixed two-second route gain. Any observation-based acceleration estimate needs bounded uncertainty and braking/merge coverage; do not read future opponent inputs or assume all rivals will accelerate. If a longer or multi-stage tactical comparison is warranted, keep it bounded and outside per-physics-step all-candidate work, with current physical checks still authoritative. These are follow-up hypotheses requiring evidence, not an approved redesign or implemented AI behaviour.

Keep60's retained trajectory opt-in/default-off and desktop55 as the user-reviewed reference. Natural racing, driver personalities, performance, preview UI and adventure integration remain the wider milestone. Campaign requirements remain unchanged.

Evidence: `passing-geometry-61/replay.json`, `passing-geometry-61/summary.json`, the expectation document, editor logs and `passing-geometry-61/manifest.json`. Reproduce the summary with `tools/analyze-passing-geometry.py review/unity-35/passing-geometry-61/replay.json`.
