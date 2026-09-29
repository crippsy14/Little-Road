# Study 124: grade-aware context drivers (`--grade-aware`) and the Cedar Hills circuit

28 September 2026. Dev build, 1008 editor checks. Runs: `review/unity-35/frame-cost-124/`, via `tools/run-pace-study.ps1` (24 cars, 1 lap, mixed roster, `--ai-preset field`).

## What changed
- **Cedar Hills** (`Resources/Tracks/cedar-hills.json`, trial track `cedar-hills`): Cedar's exact layout with 23 m of elevation.
  - Knots gained optional `y` (height) and `dy` (grade); level circuits omit them, so Cedar is unchanged. Grades reach 8.9%, and the tightest vertical curve is 424 m.
  - Profile: climbing start straight and first corner, a crest in the esses, a downhill run into the short hairpin, a climb to the wide hairpin, then a 385 m downhill back straight into the final sweep.
  - Its world is a height field around the road with a continuous RoadSupport mesh.
  - It reuses Cedar's authored line offsets (`RacingLine.Transfer`), so only the elevation differs from Cedar.
- **`--grade-aware` (off by default; `GradeModel.cs`):**
  - Accepts elevated roads (the road envelope and `FlatDistance` measure horizontally, valid for circuits that never pass over themselves).
  - The path forecast rides at the road's height and feels gravity along the grade (`DriveModel.Forces` slope term).
  - The speed profile loses braking downhill, gains it uphill, subtracts gravity from the drive, and scales corner grip by the vertical-curvature load factor (crest < 1 < dip).
  - The path follower steers on the horizontal bearing (see the bug below).
  - `--grade-no-physics` is a diagnostic arm: the same, but it plans as if level.
- **Checks (`GradeModelChecks`, 10):**
  - The level Bayview profile is bit-identical with the flag.
  - Cedar Hills keeps Cedar's length within 1%, climbs 20–26 m and stays under 10%.
  - Downhill braking zones plan lower speeds overall, and hills cost planned lap time.
  - Forecast: uphill is slower and downhill faster; a crest unloads the tyres and a dip loads them.
  - Steering on the steepest descent ignores the look-ahead point's height.

## Bug found: 3-D steering bearing
`ContextPathFollower.Follow` took `Vector3.SignedAngle(heading, delta, up)` with a 3-D `delta` to the look-ahead point. SignedAngle is the full 3-D angle, and its sign comes only from the horizontal cross product.
- A look-ahead point below the car became a steering error whose direction depended on the tiny sideways component.
- Looking ~15 m ahead down 8%, that is ~6–7°. On level tracks it is a constant ~2° from ride height alone.
- It caused the weaving and the run-wide pile-up into the downhill hairpin (seed 1710 in the first run).
- Fixed under `--grade-aware` on elevated circuits only, so level racing is unchanged. **Worth its own experiment on Cedar/Bayview**, where the ~2° dither exists in every race.

## Results (collective seconds; assisted resets / physical recoveries / contacts)

| Run | 1709 | 1710 | 1711 |
|---|---|---|---|
| Cedar off / grade | 2116.613 / 2116.613 (0/0/2) | 2124.444 / 2124.444 (0/0/4) | 2113.989 / 2113.989 (0/0/0) |
| Bayview off / grade | 803.352 / 803.352 (0/0/6) | 796.659 / 796.659 (0/0/0) | 794.134 / 794.134 (0/0/4) |
| Cedar Hills, plan as if level (`flatplan`) | 2262.261, 24/24 (1/4/10) | **8/24** (12/5/346) | **10/24** (12/4/350) |
| Cedar Hills, grade model before the steering fix | 2257.779, 24/24 (1/5/6)* | **15/24** (8/7/328) | 2288.513, 24/24 (1/7/12) |
| **Cedar Hills, grade model + steering fix (`grade2`)** | **2197.610, 24/24 (0/10/2)** | **2181.522, 24/24 (0/5/4)** | **2192.070, 24/24 (0/7/2)** |

\* `cedar-hills-s1709-grade-cedarline`. The first `cedar-hills-s1709-grade` run used a runtime-baked line and finished 18/24 (5 assisted resets, a pile-up at the final-sweep braking zone); it was superseded by the transferred Cedar line.

- **Parity:** Cedar and Bayview race identically with the flag on (all six seeds, both builds).
- **Hills:** only the full grade model with the steering fix finishes every field without assisted resets. The collective time is about 3.5% slower than flat Cedar, which is plausible for 23 m of climb. Planning as if level collapses on two seeds.
- **Decision: keep `--grade-aware`** (opt-in). It passes on level tracks (exact parity) and is the only configuration that races elevated circuits.
- **Open:** 5–10 physical recoveries per Cedar Hills race (53–79 s of field time; flat Cedar has none).
  - Most are at ~170 m (the opening, deterministic across seeds) and ~505 m (the climbing esses before the crest).
  - Cars on the edge lane (~6 m) drift past the 7.2 m recovery trigger (`PhysicalRecovery`, |offset| > 7.2) with no contact.
  - Next: find the extra lateral drift on slopes (the sideways speed RMS doubles on the downhill straight) or keep edge-lane choices further inside on grades.
