# 119: gap seeking (ai-racing-review goal 4 prompt: look between and around cars): rejected after full study

Dev build, 532 checks (+5 `GapSeekingChecks`). New race counters `gapCandidates` (fixed in the next build to include candidates handed over by the parallel pre-planner) and `gapSelections`.

Change (`--gap-seeking`, off by default): every traffic plan finds the lateral gaps between all cars within 60 m ahead or alongside, and to both road edges (0.2 m margin instead of 0.55). Up to 3 of the widest gaps not already covered become extra ranked candidates (planner capacity 6 → 9). Each still needs the ordinary physical check.

Screen (seed 1709) looked good: Cedar −6.52 s, Bayview −3.73 s, car0 −0.82 / −1.34 s. Gap targets were selected 39 / 25 times. With `--pass-commitment` added (`gspc`) it was bad: Bayview +111.5 s (two physical recoveries, 34.7 s), Cedar +5.9 s. Commitment and gap-jumping conflict.

Full study (same build, off vs gs):

| Track | Collective delta 1709 / 1710 / 1711 | Mean | car0 | club-car mean finish |
|---|---|---|---|---|
| Cedar | −6.52 / +8.82 / +2.88 s | +1.73 s | −0.82 / +1.95 / −0.65 s | 77.96 → 76.20 s |
| Bayview | −3.73 / +25.33 / +11.77 s | +11.12 s | −1.34 / −0.09 / −0.79 s | 31.65 → 32.68 s |

All 24 finish, no assisted resets; Bayview 1710 adds two physical recoveries (22.2 s). Cedar gate 2 is +0.75 s on every seed: the extra lateral options disrupt the opening pack.

Decision: not kept. Lesson: the seed-1709 screen was a false positive. Screen with seeds 1709 + 1710 from now on.

## Shoulder use (`gssh` = `--gap-seeking --use-shoulder`): rejected at screen (two seeds)

533 checks (+1: a 0.4 m body overhang is rejected with the normal 0.1 m margin and accepted with the 1 m shoulder allowance). `--use-shoulder` sets the forecast body margin to −1 m (the body may overhang the painted edge onto the shoulder; the car's centre, which sets grip, stays on the asphalt). The ranker, gap targets and traffic-progress comparison use the same allowance.

Result: failures from the start. Cedar 1709/1710: 8/24 and 7/24 finish, 16 assisted resets each, field-mean gate 1 at 66 s. Bayview: 23/24, 4–5 resets, 24–35 physical recoveries. Something at or just past the painted edge is not drivable as modelled (banks, scenery, or the start grid near the edge). Watch a rendered race before any retry; off by default.
