# 114: conditional unresolved hold (ai-pace-experiments, step 2 retry) — kept with path-aware leader

Branch `ai-pace-experiments`. Build 114 = 113 source + `--conditional-hold` (off by default) + 7 more `UnresolvedHoldChecks` → 550 checks, LITTLE_ROAD_BUILD_OK. Not frozen with `freeze-prototype.py`.

Change: `--conditional-hold` uses the 113 hold response on `TrafficUnresolved` only while `ContextDriverPolicy.UnresolvedHoldSafe` holds: no car in our lateral corridor (combined half-widths + 0.35 m + 0.5 m margin) overlaps us lengthwise, and no such car within 80 m ahead would be reached within 3 s at the current closing speed. Otherwise straighten and brake as before. `--unresolved-hold` (unconditional) is unchanged.

Protocol: same 114 executable; club, mixed roster, 24 cars, 1 lap, stagger 3 groups, hidden batchmode at 1x. Screened first on seed 1709 only (both tracks), then the candidate completed on 1709/1710/1711. The off arm reproduces 113 exactly (Bayview 1709 806.436, Cedar 1709 2144.131). Log: `review/unity-35/pace-study-114.log`.

Arms: `c1` = `--conditional-hold`; `c2` = `--conditional-hold --path-aware-leader`.

| Arm | Track | Collective delta (on − off), 1709/1710/1711 | Mean | car0 delta |
|---|---|---|---|---|
| c1 (screen) | Cedar | −19.05 s | — | +2.62 s |
| c1 (screen) | Bayview | +8.39 s (adds a 4.7 s physical recovery) | — | +4.47 s |
| c2 | Cedar | −27.52, −11.04, −18.03 s | **−18.86 s** | −1.28, −1.01, +1.87 s |
| c2 | Bayview | −3.08, −10.87, −41.94 s | **−18.63 s** | +0.23, −1.58, −0.69 s |

c2: all 24 finish in all six races, no assisted resets, no physical recoveries (baseline Bayview 1711 had a 15.3 s one). Contacts: Cedar off 4/2/0 → 2/4/0; Bayview off 2/2/2 → 6/0/4. The Bayview 1709 pile-up from 113 p3 does not recur.

Mechanism (context samples while racing, off → c2): braking falls (Cedar 5.6–6.3% → 4.1–4.7%; Bayview 9.1–11.2% → 6.5–7.1%); unresolved plans rise (Cedar ~1.5% → ~3.5%; Bayview 2.6–4.6% → 5.2–6.8%), since cars now coast through unresolved periods instead of stopping and replanning from low speed. Leader-limited share is about unchanged on Cedar and a few points higher on Bayview.

Scale: −18.7 s collective is about −0.8 s per car per lap, a small part of the ~15 s/lap traffic loss. c1 alone is not kept (Bayview screen worse).

Decision: keep `--conditional-hold --path-aware-leader` together (passes keep/reject: 24/24 everywhere, no new assisted resets, collective improves on all six pairs). Both remain off by default until promoted.

Promoted in 115 (checks=550): `--conditional-hold` and `--path-aware-leader` are now default on; `--no-conditional-hold` / `--no-path-aware-leader` restore the old behaviour. Verified on Bayview 1709 (`review/unity-35/frame-cost-115/`): default = 803.352 (matches 114 c2), both opt-outs = 806.436 (matches the 113/114 baseline). Note that in 115+ studies the pace-study `off` arm is the new default.
