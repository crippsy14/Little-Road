# Focused anticipation48: not selected

23 September 2026. Build48 passes322 checks. The first build caught an implementation ordering error: blocking-car selection was mistakenly placed inside gap-centre generation, which this experiment disables. It was moved before preference scoring; the unchanged new test then passed. Final build log is `build-context-48b.log`.

This variant predicts lateral motion only for the ahead car whose stopping-distance envelope limits desired pace in the current corridor. Other preference positions stay unchanged, while46's physical side-entry check still observes every relevant side encounter. It uses the same vehicle power, grip, physics step and candidate budget.

| Outcome |46 |47 all relevant |48 blocking car |
| --- | ---: | ---: | ---: |
| Combined24-car seconds |2235.475616 |2240.865967 |2238.174957 |
| Car0 checkpoint1 |20th |23rd |22nd–23rd tied physics tick |
| Car0 finish |8th |9th |8th |
| Car0 seconds |91.166481 |92.316200 |91.716347 |
| Pair contacts |2 |1 |2 |
| Peak relative impact |0.3583m/s |0.2529m/s |0.4174m/s |
| Active unresolved samples |374 |495 |479 |

All24 finish with no sampled road departure/recovery, and field/checkpoint gates pass.48 improves on47's field result but still misses the2215s pace and checkpoint1≤20th targets. It is not selected as the default. Few contacts alone are not success for the user's intended risk5 racing.

Controlled regressions pass80/82. Queue and braking-leader improvements remain, but no-room regresses to33.4999s (outside the original timing allowance) and records seven active unresolved-plan samples, failing that separate corridor requirement. There are no contacts, road departures, resets or missed gates in the ten controlled runs.47's all-relevant version remains the only anticipation variant to pass the complete controlled suite (72/72 under its recorded configuration checks).

The disabled control repeats46's race behaviour and the same recording-tail distinction seen in47: full raw-motion-array equality is false, while sampled/context motion, events and results are equal. The supplemental `control-motion-window.json` records whether motion through the last finish matches, retaining the original unmodified raw gate.

## Decision and next evidence

Keep43 as the desktop reference; retain45's faster experimental pace and46's much lighter contact as separate comparisons. Neither47 nor48 is promoted. They establish that lateral anticipation can fix isolated passing delays but does not by itself produce a quicker full field.

Stop adding preference-weight variants for this symptom. Inspect the control interaction: a selected passing trajectory can remain constrained by current-corridor following, and a rejected side-entry path currently has only a full-speed hold candidate before emergency braking. Capture exact pre-controller observations and candidate trajectories during those delays. Test whether a physically forecasted coast/limited-acceleration continuation or an already-clearing passing path can preserve forward progress; do not simply disable braking or declare an unchecked path safe. Preserve the physical guard, scoped risk0 control, all existing controlled thresholds and the user's tolerance for occasional racing contact.

The exact crowded-incident replay remains unproven, as does moving player acceptance and rendered performance for these candidates. Mixed personalities, race-preview UI, adventure integration, fidelity/content and the broader12-hour scope remain required. Goal remains active.
