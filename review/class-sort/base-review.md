# Class-sort baseline, "base" (29 September 2026)

**Setup:**
- Build: the dev build of 29 September (post body-removal field).
- `tools/run-class-sort.ps1 -Name base`: 15 context cars with neutral drivers on a class-mixed standing grid, 2 laps, Cedar and Hillcrest, seeds 1709–1711.
- The seeds give identical results, since equipment is the only pace difference.

| Track | Verdict | Final inversions | Sorted from | Passes (right / same / wrong way) | Contacts | Recoveries | Assisted |
|---|---|---|---|---|---|---|---|
| Cedar | PASS (all seeds) | 0 | 3446 m (lap 1.67) | 61 / 22 / 7 | 2 | 0 | 0 |
| Hillcrest | FAIL (all seeds) | 6 | never | 57 / 13 / 9 | 9 | 2 | 0 |

**Hillcrest finish order:** `111 2222 333 22 333`. Two street cars (class 2) stay stuck behind three stock cars (class 3) for both laps.

The class 2 cars are clearly quicker (best lap 69.3 s against 75.6 s for class 3), but they find no place to pass. Hillcrest has tighter corners and the lake straight; the faster car probably follows rather than committing to a pass.

**Next for the AI:** look at the Hillcrest passing opportunities for those two cars, using `summarize-leader-reasons.py` on a `-KeepTelemetry` rerun. Then trial a flag against this baseline.
