# Human 24-car Cedar comparison

The user proposed checking whether the upgraded preview car could actually win. Both retained attempts use the same club car, rear grid, seed1709, equipment field and AI options as the57 automated reference. The comparison tool verifies the captured configuration and tuning match. All pre-finish car0 samples identify human input; no runtime errors were found. Raw results and `human-field-57-comparison.json` are retained.

| Run | Finish place | Race time, seconds | Player contact events | Recovery / missed checkpoint |
|---|---:|---:|---:|---:|
| AI reference,57 | 4 | 85.501198 | 2 | 0 / 0 |
| Human attempt1 | 2 | 71.787880 | 20 | 0 / 0 |
| Human attempt3 | 1 | 67.455605 | 2 | 0 / 0 |

Attempt2 is explicitly excluded from driving analysis because the user did not see the window in front and requested another attempt. Its raw evidence and exclusion note remain. The user expressly requested retaining attempt1 and described attempt3 as much more representative.

The winning run improves4.332275s over attempt1 and18.045593s over the same-car AI reference. It reaches start/finish from the rear grid1.674983s sooner; its start/finish-to-start/finish lap is63.772240s versus AI80.142845s. By checkpoint4 its cumulative advantage is11.734329s, about65% of the final difference. This identifies the opening third as a useful investigation region; it does not by itself attribute the difference to one braking or passing rule.

Sample-weighted pedal use: human attempt3 accelerating93.3237%, coasting4.5991%, braking2.0772%; AI87.4700%,7.6119%,4.9181%. Pedal thresholds are±.05; samples more than.16s apart are excluded from duration integration. These are observational driving duties, not a proof that any particular brake action was unnecessary.

The human uses more of the track edge: attempt3's maximum sampled body-envelope offset8.782715m exceeds the8m half-width, with13.5128% of observed time partly outside. The maximum centre offset is7.576248m, inside the road. Attempt1 reaches body8.454829m/centre7.230465m with4.8811% partial-body-outside time. The AI reference keeps body offset≤7.604710m. No required checkpoint was missed; these runs demonstrate a win under the current player's rules, not compliance with the stricter AI whole-body corridor constraint. Preserve that distinction when interpreting the pace gap; do not silently widen the AI constraint or fit its path to this one human lap.

The winning attempt has two contact events, peak relative speed12.881328m/s. Attempt1 has20 events, some repeated during the same encounter, peak15.818048m/s. The user reported unexpectedly large hitboxes. Compare each model's visible shape, wheel/mirror bounds and physical collider in a focused visual/geometry check before changing dimensions. Contact counts alone cannot establish collider mismatch.

The report saves at the player's finish: only2 finishers in attempt1 and1 in attempt3. Later rival finishing times are unavailable, so do not publish a combined field time for these human runs. Human interactions change the traffic, so rivals are not expected to reproduce the automated race exactly. Two attempts establish achievable outcomes for this upgraded setup, not full progression/campaign balance.

The user noticed jumpy frame rate, consistent with the already-failing frame-tail measurements. Attempt3 used fullscreen3840x2160 and received explicit window activation during play; it is not a matched1080p performance benchmark. Subsequent manual starts should confirm foreground and ideally wait for player readiness before countdown. The Windows computer-use skill's `sky.activate_window` successfully foregrounded the running window; launching fullscreen alone did not guarantee focus.

## Follow-up priorities

1. Retain57's exact projection optimisation, keep performance spikes explicit, and improve manual launch readiness so unseen starts do not waste attempts.
2. Review visible car geometry against colliders following the user's feedback.
3. Compare early-race opportunity/pace loss with controlled solo and traffic evidence, accounting for edge usage. Preserve the lively racing the user approved.
4. Use this evidence alongside the planned driver identities, pre-race UI and adventure integration. Do not make a single human lap the new optimisation target or narrow the full campaign goal.
