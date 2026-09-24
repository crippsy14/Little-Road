# 118: pass commitment (ai-racing-review goal 1) — foundation, pace neutral; kept as opt-in

Dev build, 524 checks (+5 `PassCommitmentChecks`). New per-car race counters (on in every build, behaviour-neutral): `planReversals` (planned-line jumps >1.5 m that reverse within 3 s), `passCommits`, `passCompleted`, `passAborted`, `abortTarget/Timeout/Unaccepted`. `summarize-pace-study.py` shows reversals per car-minute and commitments.

Change (`--pass-commitment`, off by default): once a traffic plan moves the car >1.5 m to one side of the car it is planning around, candidates ending >0.5 m on the other side of that car rank 0.5 s worse for up to 3 s (`DynamicRejoinPlanner.Commit`). Ends as completed when level with or ahead of the target, or aborted: target gone or >40 m ahead, timeout, or no accepted path. Ordering only; physical checks unchanged.

Screen (seed 1709; off arm identical to 117: Cedar 2116.613, Bayview 803.352):

| Arm | Track | Collective delta | car0 | reversals/car-min | commits (done / aborted) |
|---|---|---|---|---|---|
| pc (target = planner's current blocker) | Cedar | +2.53 s | +2.01 s | 19.5 → 17.6 | 488 (35 / 441) |
| pc | Bayview | −0.65 s | −0.91 s | 35.7 → 31.9 | 334 (33 / 286) |
| pc2 (target = committed car) | Cedar | −3.20 s | +2.08 s | 19.5 → 15.6 | 519 (162 / 340) |
| pc2 | Bayview | +0.96 s | −0.49 s | 35.7 → 30.2 | 381 (173 / 188) |

All 24 finish, no assisted resets. In `pc`, 62% of aborts (275/441) were the planner's blocker switching to another car in the pack; `pc2` tracks the committed car itself (aborts by target 37, timeout 216, unaccepted 87 on Cedar).

Reading: commitment works as intended (completions ×5, fewer reversals) but does not by itself find pace. Most unfinished commitments now time out: the car holds its side and still cannot get by, because the only tool is a lateral offset. That is what goals 2 (corner-aware set-up, later braking on the inside) and 4 (alongside rules) address. Kept as an opt-in flag to build goal 2 on; judge the combination with a full study.

## Goal 2: corner-aware set-up + later braking on the inside (`ca`), rejected at screen

Dev build, 527 checks (+3 `CornerAwarePassChecks`: the inside side matches the road's turn direction on Cedar corners of both directions; a higher braking fraction brakes later and never earlier). `--corner-aware-pass` (with `--pass-commitment`):
- before committing, candidates ending on the outside of the next corner (20–150 m ahead) rank 0.2 s worse
- while committed, alongside the target (overlapping, laterally clear) on the inside, the live follower plans braking at `BrakeFraction + 0.05 + 0.01·risk` (≤ 0.95)
- a corner-aware pass completes only once a full car length ahead

| Track | Collective vs off | car0 | reversals/car-min | commits (done / aborted) | late-brake ticks |
|---|---|---|---|---|---|
| Cedar | +0.25 s (pc2 −3.20) | +5.75 s | 17.5 | 395 (18 / 359; timeout 218, unaccepted 109) | 4683 (~39 s) |
| Bayview | +3.67 s (pc2 +0.96) | −1.05 s | 30.7 | 214 (9 / 188) | 3051 (~25 s) |

All 24 finish, no assisted resets (Bayview adds one 5.4 s physical recovery). Late braking fires, but passes rarely complete; commitments time out.

Reading: club cars have 34 m/s vs the street cars' 25 m/s, so passes on straights should be easy on power. Yet commitments mostly time out, and earlier audits showed repeated "corridor" / "moving side entry" rejections. The limit looks like pack density (no free lane in a three-wide street-car pack), not overtaking technique. Next levers: alongside rules / lane negotiation (goal 4) and the parked grid-order question. `--corner-aware-pass` stays in code, off by default, pending that.
