#110 pace audit and rejected additional-candidate pilot

Keep109's driving behaviour and the existing named three-group scheduling experiment. Do not enable110's extra-candidate option: the preview car improves slightly, but the field regresses substantially. The source option remains disabled by default for reproducibility; no adventure/default/desktop promotion. Full campaign goal remains active.

## Baseline evidence

Two109 diagnostic runs use fixed three-group scheduling, Bayview1711 and Cedar1709,24 mixed drivers/form and unchanged equipment. Existing same-tick pedal-stage, failed/opening/continuation snapshots were enabled. Both reproduce108's matching stored motion, events and results exactly, all24 finish, normal exits and no runtime errors. Diagnostic allocations exclude these captures from performance evidence.

Bayview has8092 valid moving AI command samples,979 braking:451 attributed to the follower,461 moving-leader restriction,67 immediate guard. Cedar has21352 valid samples,1640 braking:727 follower,649 moving leader,264 immediate guard. These counts identify the final controlling stage, not whether braking was unnecessary. The detailed bottleneck analyzer separates traffic-unresolved/coast-continuation states and excludes physical recovery from context attribution. Its initial assertion failure exposed153 samples where recovery correctly stopped calling the context policy; the analyzer now reports these separately instead of assigning stale commands to the AI. Same-tick assertions remain for Racing/Disabled observations.

Cedar car0 has815 pre-finish racing samples:336 below-full commands attributed to the moving leader,285 follower,12 immediate guard,9 unresolved path and13 coast continuation.102/108 Home-straight samples are leader-limited below full pedal. This supports investigating usable passing paths, not indiscriminately reducing braking. Low pedal at a physical speed limit is normal; sample counts are not automatically lost seconds.

PaceCandidateReplay reconstructs the actual course, line, captured bodies/tuning/steering, roster braking/risk/passing style and drafting profile. All868 Bayview and812 Cedar recorded nominal candidate checks reproduce exactly. Among355/309 failed decisions,81/80 contain another finite generated path passing the short physical/traffic/corridor checks. The replay has no scenery scene, aborts on a captured scenery rejection, and does not simulate a whole multi-car manoeuvre. Original checks matching does not turn its counterfactuals into race acceptance.

## The bounded experiment

110 adds `--third-dynamic-candidate`, off by default. After the usual two dynamic choices fail, check the fastest finite untried rank once using unchanged full checks. Existing ranking, shared forecast workers, ordinary controller/safety and physics remain. Counters record attempts/acceptances.512 editor checks pass;173 source/script inputs plus assembly frozen. The option-disabled110 rendered runs exactly reproduce108's corresponding scheduled races, including contacts/results.

Four sequential visible1080p normal-speed110 off/on races, fixed three-group schedule, nondevelopment,120fps cap, no competing editor/game or screenshot capture, pace allocations disabled. All exit0, no runtime errors/resolution changes/dropped captures, zero warm measured driver/worker allocation. A normal process exit is not a successful race: Cedar on reaches the180-second cutoff with only19/24 finishers.

| Metric | Bayview off | Bayview on | Cedar off | Cedar on |
|---|---:|---:|---:|---:|
| Finished |24|24|24|19|
| Collective car-seconds |836.076|854.988|2144.131|Incomplete|
| Car0 seconds |35.475|35.125|81.444|80.494|
| Completed physical recoveries |1|2|0|5|
| Physical-recovery car-seconds |15.308|32.208|0|571.233|
| Assisted resets |0|0|0|7|
| Per-car contact records |2|2|4|22|
| Extra checks / accepted |0/0|417/49|0/0|1214/55|
| Whole-AI joined wall ms/tick |3.552|3.509|2.703|2.913|
| Frame p95 ms |17.881|18.309|11.565|16.377|
| Frame p99 ms |24.063|25.964|18.600|23.608|

Bayview field loses18.913s despite car0 gaining0.350s. Cedar car0 gains0.950s but the field cannot supply a valid collective time. The comparator reports null for incomplete field time/gain; unfinished cars' zero/default times must never become a fake improvement. No pooled gain or acceptance claim is possible. Both fail declared recovery gates; Cedar also fails finish/contact gates. Measured allocation/experimental frame-cost regression gates pass, but Cedar timing covers a much longer/different race, not an equal-duration CPU benchmark. Global frame/AI budgets remain open. Sampled body overlap stays near numerical zero, which does not excuse the gameplay failure or prove continuous separation.

Reject the pilot without running the larger seed matrix. Preserve failures and logs. No tuning of gates or selective car0 headline. The default-off baseline is still the smoother version positively reviewed by the user.

## What changes the next action

Checking one more short-horizon path is not a sufficient correction. The new paths alter later traffic and can lead to repeated unresolved selections and recovery cascades. Do not assume the short forecast's accepted label certifies the whole pass. Investigate path tracking, replanning and changes in live constraints across the manoeuvre before increasing search breadth again.

The existing Bayview1711 car8 loss is especially concrete. Its recorded late states near route576–650m have no contained alternative in the current generated set. Repeated road-bound failures precede the recovery; its first recovery sample is offset7.245m, body extent8.445m and speed26.17m/s. PhysicalRecovery's centre-offset7.2m threshold starts Stabilise; above5.5m/s Plan selects zero speed/steering, then slow sampled manoeuvres. Roughly15s is spent recovering. A two-wheels-off excursion therefore becomes a full-stop manoeuvre even while still moving quickly. This is a diagnostic finding, not permission to remove road constraints or to attribute all recovery time to that threshold.

Next bounded work: replay the approach to establish why the existing curve ceases to be trackable; separately prove a rolling return on explicitly supported shoulder geometry for aligned, moving cars. Any rolling return must use ordinary physical controls, check scenery/traffic/body sweep, and fall back to stopped recovery for blocked, badly angled, missed-gate or unsupported cases. Test clean edge excursions, blocked returns and deep departures before a24-car comparison. Keep the current racing baseline and three-group cadence fixed, and keep110's additional candidate disabled. Reduce real field lost time while preserving the user's accepted urgency and presentation.

Visual/UI acceptance is unchanged. Ownership/progression, exploration, campaign duration, endurance, controller/small-window acceptance and production readiness remain incomplete. The known connected second-race recovery issue remains open. No shortcut changed.
