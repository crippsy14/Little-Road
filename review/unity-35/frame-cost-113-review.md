# 113: unresolved hold, alone and with path-aware leader (ai-pace-experiments, step 2) — not kept yet

Branch `ai-pace-experiments`. Build 113 = 112 source + `UnresolvedHoldChecks` (6 checks; hold/brake responses moved to static `ContextDriverPolicy.UnresolvedHoldCommand`/`UnresolvedBrakeCommand`, behaviour unchanged) → 543 checks, LITTLE_ROAD_BUILD_OK. Not frozen with `freeze-prototype.py`.

Change under test: `--unresolved-hold` — on `TrafficUnresolved`, instead of full brake and zero steering, follow a fresh `FromMotion` path at the current lateral offset with pedal ≤ 0 and target speed ≤ current speed; the moving-leader limit and immediate guard still brake afterwards.

Protocol: same 113 executable; club, mixed roster, 24 cars, 1 lap, stagger 3 groups, hidden batchmode at 1x; seeds 1709/1710/1711 on both tracks. Arms: `off`, `p3` = `--unresolved-hold`, `p4` = `--path-aware-leader --unresolved-hold` (p4 added with `-SkipBaseline`). The off arm reproduces 112 exactly (Bayview 1709 806.436, Cedar 1709 2144.131). Log: `review/unity-35/pace-study-113.log`.

| Arm | Track | Collective delta per seed (on − off), 1709/1710/1711 | Mean | car0 delta |
|---|---|---|---|---|
| p3 | Cedar | −16.92, −16.33, −8.50 s | **−13.92 s** | +4.73, +2.62, +2.26 s |
| p3 | Bayview | **DNF (21/24)**, −8.10, −30.85 s | — | —, −0.21, +0.04 s |
| p4 | Cedar | −28.56, −20.50, −13.71 s | **−20.93 s** | −1.88, +5.72, +5.25 s |
| p4 | Bayview | +15.91, +13.98, −35.32 s | −1.81 s | −0.38, +0.57, +0.55 s |

Failure — Bayview 1709 p3: 21/24 finish (cars 0, 19, 23 hit the 120 s limit), 10 assisted resets ("Persistent geometry or stationary traffic"), 16 contact records, max 17.54 m/s. At 29.3 s car 18 hits car 17 at 17.5 m/s near progress ~398 m (gates 7–8) and stays stationary there (hit again by lapping car 5 at 55.7 s); following cars pile up at the same spot. Unresolved plans 3.2% → 9.4% of racing samples, leader-limited 43% → 55%.

Other effects: contacts rise in nearly every on-run (Cedar off 4/2/0 → p3 20/6/18, p4 14/6/18; Bayview off 2/2/2 → p4 15/10/12), mostly at lower relative speed. p4 Bayview adds physical recoveries on 1709 (26.1 s) and 1710 (14.8 s). Cedar 1709 context samples: braking 6.3% → 4.8% (p3) / 4.5% (p4), unresolved 1.5% → 4.2% / 3.1%.

Reading: holding position instead of braking recovers pace on Cedar (both arms faster on every seed, p4 best), but it keeps cars moving into situations the planner has flagged as unresolvable, so unresolved time and contacts rise, and on Bayview 1709 it produced a stationary blockage. Car 0's own time is mostly slower on Cedar; the collective gain comes from the field.

Decision: fails keep/reject (p3 has a DNF and new assisted resets; p4 Bayview mean gain is only −1.81 s with two worse seeds and new physical recoveries). Keep both flags off. Next: make the hold conditional (e.g. hold only while the closing speed to the blocking car is low / time-to-contact is long, otherwise brake as before), screen on Bayview 1709 + Cedar, then rerun the full study.
