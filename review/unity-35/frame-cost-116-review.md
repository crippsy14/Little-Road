# 116: sustained leader ranking (ai-pace-experiments, step 4 first try): rejected at screen

Branch `ai-pace-experiments`. Dev build (`tools/build-dev.ps1`) = 115 source + `--sustained-leader-ranking` (off by default) + 4 `PassingRankingChecks` → 554 checks, LITTLE_ROAD_BUILD_OK. First study on the reusable dev build (no numbered export).

Why: in the 115 Cedar 1709 race the six club cars spend 42% of racing time capped by a car ahead (75% of that behind street cars, at the street cars' 25 m/s ceiling). The candidate ranker (`ContextPathFollower.EstimateGateTime`, used by `DynamicRejoinPlanner`) counts traffic only for the first 3 s of each candidate's forecast, so holding behind a slower car looks nearly as fast as passing. Car 0's opening audit on the home straight showed "hold" or "guide" ranked first on almost every plan. (Correction to a first reading: over the whole race, 41% of club cars' leader-capped samples already plan a >1.5 m sideways move. They do try to pass, mostly outside the home straight.)

Change: `EstimateGateTime` takes a `trafficSeconds` window; `--sustained-leader-ranking` sets it to the whole horizon.

Screen (seed 1709, flag off vs on, same build):

| Track | Collective delta | car0 delta | gate 1 / gate 2 (field mean) |
|---|---|---|---|
| Cedar | +2.47 s | +3.82 s | −0.04 / +0.26 s |
| Bayview | +6.65 s | +1.77 s | +0.24 / +0.24 s |

All 24 finish, no assisted resets. Club cars' leader-capped share is unchanged (42.2% → 43.3%); braking 4.6% → 5.6%.

Mechanism (car 0 opening audit, `cedar-s1709-s1-audit`): on the straight car 0 now accelerates past 25 m/s (up to 29.7 m/s), but its preferred move back towards the line (+1.1 m) is repeatedly rejected ("corridor": car alongside), and it then brakes hard behind the next car (25.4 → 19.7 m/s at ~270 m). Same pattern as 112: closing faster on traffic it can't get round converts cruising time into late braking.

Decision: not kept (worse on both screens). Ideas for the next try: apply the sustained window only to the blocking car in our own corridor; let pass candidates rejoin the guide after clearing, so they don't pay a corner penalty; and account for cars alongside (corridor) in the ranking, so it doesn't prefer moves the physical check will reject.
