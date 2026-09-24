# Light drafting before driver personalities

The user approved prototype69's race feel and asked for a small following-car acceleration benefit. The chasing car still earns its pass. Drafting must not make the leader yield, add engine power, bypass gears, or make driving ability irrelevant. The prototype remains separate from adventure integration.

## Physical model and limits

Use a maximum **8% reduction of the aerodynamic resistance term**. This is an initial gameplay choice, not an asserted real-world percentage. Rolling loss, engine/torque curves, tyre grip, braking force, mass and gearing stay unchanged. More acceleration comes from less resistance; the car also slows slightly less when coasting or braking in the wake. Apply the same force law to human and AI cars.

[NASA's drag equation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/drag-equation/) supports the speed-squared aerodynamic term and explains that drag coefficients depend on the object and experimental conditions. It does not establish our chosen strength or wake dimensions. This is a game model, not CFD or a simulation of downforce loss.

The wake requires a forward-moving source ahead, closely aligned headings and the same local section of the track. Strength rises smoothly above12m/s to full strength at26m/s, fades laterally, and ends35m behind the rear body. It fades near bumper overlap rather than rewarding contact. Body centres and half-lengths define clearance. Vertical separation, another track branch, opposing/stopped cars and side-by-side cars give no benefit. Multiple cars cannot stack the benefit. There is no lasting boost after leaving the wake and no launch assistance from standing still.

Equipment performance ratings remain equipment-only. The current target-speed profile remains the race-driving ceiling; drafting can help reach it earlier but is not permission to raise corner speeds. Profile braking calculations and the short coast envelope use maximum possible drag reduction as a conservative bound. This can slightly change the enabled solo profile even without a source, so compare tow/no-tow physics separately from enabled/disabled whole-race behaviour.

## Shared physics and prediction

Prototype flag: `--light-drafting`, runner `-LightDrafting`. Initially supported only in the context driver development fixtures. Every live car receives its wake from the same pre-controller snapshot, including a human-controlled car. The force code is shared by live Rigidbody driving and planar forecasting.

Nominal, continuation and traffic-progress forecasts carry their own copied opponent observations. Reuse requires exact source values and the same drafting mode. A worker never reads mutable live traffic or future rival inputs. Wake prediction extrapolates observed constant velocity/heading over the bounded two-second horizon; this is an explicit approximation, refreshed at each real planning update. The wake is recomputed at the forecast control cadence while physics retains120Hz integration. Cornering sources may therefore differ from the prediction. Existing following forecasts still use their richer observed turning rollout for following control; the wake estimate is the same in both models.

The immediate collision controller uses maximum wake strength throughout its short own-car rollout. This intentionally avoids assuming excessive deceleration before an imminent collision. It can be conservative and must be checked for extra braking; it does not implement a new yielding rule or wider collision margin.

## Acceptance and evidence

Before selecting the option for a viewed build:

- Verify zero-effect arithmetic, smooth pull-out/distance fade, source exclusions, no stacking, route seams/branches and ordinary gear limits.
- Compare time26â€“32m/s and distance under identical throttle with unchanged equipment. Also run real two-car Rigidbody straight/pull-out fixtures; do not use a changed race outcome alone as proof of drag correctness.
- Verify serial/worker forecast equality, copied-source ownership, changed-source invalidation and zero warm managed allocation.
- Repeat the established controlled overtaking, no-room, merge and braking fixtures. Keep the old blocked-road timing failure visible; any new physical recovery, departure or unexpected contact in these clean fixtures prevents selection.
- Run drafting off/on against69 at seed1709. Disabled motion/events/results must reproduce69. Keep existing field/checkpoint limits; require24 finishes, no recovery, no increase in sampled road departure, collective time no more than2% slower and preview time no more than2s slower before considering selection. These are candidate screening limits, not evidence of speed improvement.
- Check a held-out field if the first candidate passes, and fixed-resolution rendered performance before claiming performance acceptance. Seed alone does not meaningfully vary this deterministic controller: use the same six club/eighteen street cars but put the five club rivals at IDs2,6,10,14,18 instead of4,8,12,16,20. Set seed1811 and retain all other settings. Preserve the existing frame budgets rather than relaxing them. Compare contact severity, unresolved decisions and braking duty explicitly.

Drafting telemetry records source ID and resistance reduction separately from existing motion records. Physical fixtures use120Hz reporting for their initial acceleration/pull-out comparison; ordinary race telemetry cadence is unchanged. Store raw results, configuration, errors and failures. Do not replace desktop55 merely because the new executable exists.

After this bounded feature, driver personalities remain the next AI milestone under `34-driver-identity-prototype.md`: varied racing judgement and risk using the same physical equipment, rather than artificial power or a rule that leaders move aside.


User viewing update: the drafting replay felt good and the user explicitly asked to keep it. Treat70 with drafting as the new race-feel reference, retaining its recorded performance/contact limitations. Before personalities, the user requests a bounded experiment in committing to usable open space ahead.71 tests forward-progress priority in candidate selection; physical limits remain authoritative. Desktop55 remains unchanged.
