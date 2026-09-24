# Connected event recovery issue

Keep this distinct from the user-accepted small road-edge excursions. It is a sustained stall, not a clean quality pass and not evidence that personality risk is successfully producing interesting crashes.

Frozen88, second connected adventure race, seed1731,24 mixed drivers,3 laps, purchased workshop engine on otherwise stock player. Evidence: race-preview-88/adventure/race-race-2.json and corresponding track/roster/drivers reports, plus player.log. The integration runner completes an actual fresh quest/purchase/race/retry sequence; it is accelerated and is not a rendering-performance benchmark.

Driver8 first enters recovery near course progress254m at15.75s. Drivers9 and18 subsequently remain around250m until assisted reset at93.33s and93.89s. Their saved checkpoint remains5 before and after reset. Both are placed backwards:253.36→248.37m and250.19→244.13m, so no checkpoint progress is granted. Their original positions are approximately(x120.28,z15.06) and(x120.45,z11.55). Driver8 also recovers near progress1579m later in the race.

All24 eventually finish, but driver8 takes132.59s and drivers9/18 take173.50/174.15s. There are four physical recovery entries and two assisted resets. This run records zero participant contact events, so do not assume a car collision was the original cause. Distinguish planner/road-boundary behaviour, physical recovery clearance, and any actual scene obstruction in a future isolated investigation.

The integration analyzer retains race2NoTeleportRecovery=false (17/18 overall checks pass); do not filter the run or describe the full analysis as passing. The connected transaction/UI checks succeed and progression stays intact. Retain the field behaviour for the next user-requested visual milestone, leave desktop shortcuts unchanged and keep this defect open. A later race-quality pass should reproduce this seed/sequence or establish a smaller fixture before changing global caution settings.
