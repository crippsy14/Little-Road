# 112: path-aware leader limit (ai-pace-experiments, point 2) — rejected as implemented

Branch `ai-pace-experiments`. Build 112 = 111 source + `--path-aware-leader` (off by default) + 7 `PathAwareLeaderChecks` (537 checks, LITTLE_ROAD_BUILD_OK).

Change: `MovingLeaderControl` skips a car ahead when we are closing (>0.5 m/s, catch within 4 s) and the committed path's lateral offset clears both the car's current and drifted offset at the catch point and for a body length after. Applied identically in live control and `ContextPathPrediction` forecasts.

Protocol: same 112 executable, flag off vs on; club, mixed roster, 24 cars, 1 lap, stagger 3 groups, hidden batchmode at 1x; seeds 1709/1710/1711 on both tracks. The off arm reproduces the 108/111 stagger results exactly (e.g. Bayview 1709 806.436, Cedar 1709 2144.131).

| Track | Collective delta per seed (on − off) | Mean | car0 delta |
|---|---|---|---|
| Cedar | +2.99, +18.80, +12.38 s | **+11.39 s (worse)** | +7.25, +2.32, +4.05 s |
| Bayview | −5.43, +18.66, −15.23 s | −0.67 s | −1.91, −0.92, −0.37 s |

All 24 finish everywhere, no assisted resets. Cedar contacts rise (4/2/0 → 6/12/8 participant records, ≤5.5 m/s relative). Bayview 1710-on adds a 10.9 s physical recovery; 1711-on removes baseline's 15.3 s one.

Mechanism (Cedar, context samples while racing): leader-limited time falls from 33–38% to 25–29%, but unresolved plans rise from ~1.5% to ~2.0–2.3% and braking from ~5.9% to ~6.7%. Releasing the cap lets cars close faster onto a pass that is then often withdrawn at the next plan, converting cruise-behind time into late braking/unresolved emergency stops and low-speed contact.

Decision: keep the flag off. The underlying observation (a third of racing time is leader-capped) stands; a retry needs pass commitment/hysteresis (only lift the cap for a pass selected consistently across plans) and should be tested together with point 3's gentler unresolved response.
