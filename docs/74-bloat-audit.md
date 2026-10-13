# 74 · Bloat audit (13 October 2026)

This is the first pass of the lean rules in docs/73 §6. A read-only audit classified every editor check, study-only runtime file,
flag and tool as KEEP, MERGE, LOOSEN or RETIRE. Richard then handed the calls over ("do a full cleanup ... from your own call"),
so the removals went in as one tested batch the same day. **Status: done** (results below).

## What the audit found

- **The first racing AI's study machinery was about half the editor checks.**
  - Racing AI v2 (`RacerDriverPolicy`) drives every career race. The first AI (`ContextDriverPolicy`) was only ever built with `--first-ai`.
  - Under v2, 25 of the AI preset's 30 switches never applied, and some study flags threw.
- **Pinned numbers:** checks that pin exact tuning values block good changes (for example "a Bayview podium pays 330").
- **Coverage gap:** the regression didn't race the AI at all.

## What was done

1. **The first AI is retired, not kept as a benchmark.** Keep/reject already compares the same build with a flag off and on, so a second AI added nothing but code. It stays in git history (commit 88131e7 and before).
   - **Game code removed:** 23 first-AI files (about 3,190 lines).
     - ContextDriverPolicy and its path follower, prediction, preference map and plan audit;
     - the forecast workers, batch probe and path cache;
     - dynamic rejoin;
     - road and traffic feasibility, observed-traffic rollout, immediate traffic safety and the progress comparison;
     - moving-leader control, open-space priority and passing progress;
     - the traffic policy;
     - the path-prediction fixture and the motion and path-approach audits.
   - **Orphans removed** (left unused by the above): the coasting envelope, following-fallback selection, opening capture window, passing-target continuation, staggered plan clock and planar drive prediction.
   - **Old investigation harnesses removed** (unused by any tool or doc since September): the performance trial and course, the evening-route review, the vertical-motion audit and the ground-cover geometry audit.
   - **Simplified to keep only what v2 uses:**
     - RaceTrial: the policy set-up and the report fields;
     - RoadCar: the policy slots;
     - the shared snapshot (`RaceWorldSnapshot`), the frame audit, the technical-circuit review and the traffic fixture.

     The AI preset now holds just v2's five switches: recovery, drafting, roster, form and gate-first. `--driver-policy context` still works and means v2.
   - **Check files:** 48 removed, and 5 trimmed to their live parts (grade model, driver identity, driver planning, drafting integration, roster).
2. **Settled A/B switches removed** (defaults unchanged): `--legacy-drag`, `--legacy-reverse`, `--old-ride-height`, `--free-roam-speculative`, `--capped-warm-up`, `--field-recipe-drivers-only`/`-parts-only` and `--tessellated-cedar-support`.
3. **Loosened to the rule, not the number:**
   - Hillcrest Motors stocks its own valley parts (at least 3);
   - payouts fall with place, every finisher is paid and a non-finish pays nothing;
   - podiums pay their place's reward times the event's scale, and Hillcrest pays more than Bayview;
   - the quarry galleries are 4 or more benches of 5–25 m;
   - the livery texture is square and at least 1024 px.

   The remaining range checks were kept: Duke top speed, Cedar length, stock 0–20 time, junction trims. They state design targets.
4. **Tools:**
   - Removed `capture-quarry`, `quarry-race-review`, `valley-race-review`, `valley-tour`, `review-tuner-garage`, `run-pace-study` and `restore-line-endings.py`.
   - `keep-eol.py` now does the per-line restore for mixed-ending files itself, so one script covers line endings.
   - `run-technical-trial.ps1` went from 225 to 97 lines (about 50 dead switches); `run-frame-cost.ps1` and `run-pace-parallel.ps1` lost their first-AI options.
5. **Racing AI coverage:** `regression.ps1` now races one 24-car lap of Bayview (seed 1709) and prints the summary line.
6. **Not done, on purpose:** merging small check files. File count isn't the cost; lines and dead paths are, and merges would only churn history. The hand-typed `--no-…` escape hatches and the `--perf-…` probes stay (docs/73 §6).

## Proof that nothing changed

- **Same races** (study 224 before, 225 after; Cedar and Bayview, seeds 1709–1711, same build settings): all six races give identical finishers, total times, winners, resets, contacts, attacks, passes and aborts.
  - The one counter that differs, plan reversals on Cedar 1710, is 342 against 343. 342 matches the earlier accepted study 223.
- **Build:** all 2,655 editor checks pass. The full regression was stopped part-way at Richard's call (13 Oct: regressions don't need to run so often); the identical races are the proof for this batch.

**Totals:** 7,601 lines removed and 119 added across game code, checks and tools (game scripts 206 → 171 files, editor scripts 93 → 45).
