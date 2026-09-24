# Prototype57: cheaper exact track projection

The user approved55's moving race as the best yet; the desktop shortcut remains55 at their request.57 preserves that behaviour while reusing immutable spatial bounds within each route-projection query. Parent traversal already calculated both child bounds; recursion now consumes those values instead of calculating them again. The current best distance is still checked on entry, preserving pruning, arithmetic, traversal order and earliest-segment ties. No planning cadence, vehicle physics, safety margin or candidate decision changes.

## Evidence and performance

Both56 (stage timers only) and57 pass332 build checks, including exhaustive projection comparisons on normal, elevated, distant and tied geometry. Source snapshots and assembly hashes are in their manifests.

The sequential hidden56/57 comparison passes all recorded motion, context, checkpoint, impact, overlap, recovery and result equality checks, normal exits and runtime-error checks. Total AI wall cost, including snapshot work and joined forecast workers, is3.503196→3.131527ms per snapshot tick: a10.6094% reduction in this pair. Decision-region cost falls10.0888%. Traffic-progress cost falls7534.7342→5851.1685ms; rival preparation4017.0035→2807.4737ms. These stage totals include a slightly different number of post-finish processing ticks (905 vs900 comparisons); the whole-AI comparison normalises by snapshot count. Hidden measurements establish a promising cost reduction, not rendered performance acceptance.

The56 baseline exports24 extra motion records at101.005745s, after the final finisher at100.830788s. Its original raw55 parity result remains failed for that length mismatch; the separately recorded supplement proves an identical2424-record prefix and all extra records after final finish. The first56 run lost its process exit-code handle through a wrapper mistake and remains unaccepted for normal exit. The subsequent matched pair captured owned process exits correctly. No runs were silently overwritten or their failed checks removed.

The visible57 run exactly reproduces55's entire recorded race and exits normally without runtime errors. All10025 post-loading frames remain1920x1080 at normal speed. Driver and forecast-worker allocations after5s are zero; no frames are dropped. Mean frame10.4120ms, p9523.7245ms, p9950.9120ms; all-AI mean3.8753ms, p9521.8945ms, p9948.8645ms. The worst one-second frame mean is20.8321ms around race21.54s. The loading-adjacent maximum569.7651ms remains recorded. Existing16.7/33.3ms frame-tail and3ms AI budgets still fail.55's visible capture changed from1080p to4K, so do not claim a controlled rendered percentage improvement against it.

All24 still finish; car0 remains fourth in85.501198s and collective time2215.597137 car-seconds. Prior55 controlled timing and contact-threshold limitations remain; this exact optimisation does not fix them.57 is a useful measured performance step, not a completed performance milestone.

## Human comparison

At the user's suggestion, the launcher now permits visible manual driving in the normal multi-car field while retaining real-time enforcement. Other multi-car diagnostic fixtures remain unavailable in manual mode. The existing runtime already assigns car0 human controls and the other23 AI controls; it labels telemetry by input owner and saves on human finish (or exit). No controller/runtime change was needed.

The user explicitly selected Ready now; `human-field-57-run1` runs the same club car, rear grid, field, seed and one lap at normal speed. Human motion changes the other racers' observations, so compare pace, checkpoint positions and passing opportunities, not exact AI trajectory parity. Telemetry captured on player finish may omit later rival finishes; report that limitation. A single human attempt can demonstrate a possible result or expose handling/traffic problems, but cannot prove broad race balance. Record any reported errors or recoveries openly rather than discarding the entire lap automatically.

## Next work

Analyse the human attempt, then reassess the highest-value next work with it. Remaining frame spikes still need a bounded solution; avoid losing the reviewed racing behaviour or spending indefinitely on one timing threshold. Individual driver traits, pre-race loadout/personality presentation and adventure integration remain the next player-facing systems, within the complete12-hour campaign scope.
