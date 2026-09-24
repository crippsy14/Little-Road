# Same-grid AI traffic cost

Hypothesis: much of the upgraded car's85.50s standing-race time is traffic cost rather than clear-road capability. Test it without changing the driver, road containment, car, tyres, risk setting or physics. The human67.46s result remains a different driving-rules observation because it deliberately uses two wheels beyond the asphalt.

Add `standing-solo`: exactly one car in grid slot23, the same position/heading/height and standing countdown as car0 of the24-car pack. This is a full lap, unlike existing mid-lap `solo`/`pace-solo` fixtures. Run the solo and24-car field with matching club equipment, seed1709, current58/57 driver options and pedal-stage recording. Capture configuration and telemetry separately.

Verify all build checks and exact ordinary-field behaviour against57. Solo must traverse all checkpoints, finish without recovery/runtime errors and record only autonomous pre-finish input. Confirm actual initial pose, dimensions, tuning and grid distance against the field. Report finish/start-line/sector time, controller-stage braking, speed, road departure, contact and recovery. Any solo failure is evidence against the hypothesis, not a reason to relax the check.

Subtract solo checkpoint times from field times to locate traffic cost; negative sectors can legitimately occur because traffic changes approach and path. Do not interpret every delay as unnecessary braking or force equality with human time. Do not declare race quality improved by adding a diagnostic fixture. Use the measured difference to choose the next AI change. Hidden diagnostic timings cannot establish rendered performance.
