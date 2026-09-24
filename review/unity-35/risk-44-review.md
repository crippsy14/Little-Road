# Risk44: rejected combined passing-space candidate

The previous goal turn made progress by adding and validating preview-car checkpoint metrics. This turn implements the first risk-sensitive preference experiment, preserves exact risk0 behaviour, and exposes a missing part of the passing decision.

Build44 passed310 checks. Risk0 exactly reproduces43's stored motion, commands, context samples, checkpoint events, contacts, recovery, overlap evidence and results. The ordinary desktop shortcut still targets43.

Risk5 combines smaller soft spare-space penalties with extra candidate targets at observed gap centres. Car0 improves from24th to18th at checkpoint1 (11.6666s), but later crashes and finishes20th at162.6490s. Only22/24 finish within180s. There are70 participant contact records, a10.9681m/s peak impact,88 sampled body departures, and4035 active unresolved-path samples. No complete-field combined time is reported when two finishers are missing. This candidate is rejected; an early position gain does not excuse the subsequent race failure.

## First serious incident

Telemetry before car0's collision with car15 at12.6416s is informative. At12.21s car0 travels31.64m/s near offset+5.94, following a target near+6.00; car15 is ahead near offset+2.76 at16.92m/s. At12.41s car0 selects an opposite-side target near-6.14 and steers left. Its own road forecast accepts the path. By12.61s the path is unresolved, but the prior crossing path is retained. Contact follows at10.9681m/s relative speed.

This is evidence of a planned crossing into an approaching side-by-side encounter, not proof that all tight gaps are invalid. Code inspection confirms that the current feasibility test forecasts the own car against the road/scenery and a presently occupied centre corridor; it does not forecast opponent occupancy over the manoeuvre. Existing emergency following response also excludes opponents once behind. Smaller soft margins have exposed that limitation. Do not simply restore blanket caution and call risk5 complete.

## Next causal comparison

Build45 adds an explicit `--no-gap-centres` ablation. It removes only added candidate targets, retaining the exact risk5 score at every original sample, checked at build time. Run risk0 and risk5 with this flag, identical seed/grid/parts and existing field gates. The numerical acceptance thresholds in `risk-44-expectations.md` remain unchanged. This isolates the extra candidates from the spare-space preference before adding a manoeuvre occupancy check. Do not change physics, grant hidden speed, disable side protection or infer player acceptance from the new rank metric.
