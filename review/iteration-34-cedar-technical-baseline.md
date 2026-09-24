# Cedar Technical — physical layout and legacy-driver baseline

21 September 2026. New development circuit requested by the user, inspired by Tsukuba's mix of connected corners and genuine straights. Original authored layout, not a replica or a campaign-content claim.

## Delivered for review

- Candidate: `../../little-road-ai-review/Little Road Unity.exe`; run with `--trial-auto --trial-track cedar`. The normal desktop adventure remains checkpoint 20.
- Repeatable visible launcher: `../Watch Cedar Circuit.cmd`, using `tools/run-technical-trial.ps1` with unique evidence folders. Three club-car laps at real time; C toggles overview, Tab/Share toggles player control, Escape quits. This development run never opens an adventure save.
- JSON knot/handle definition: `unity/Assets/Resources/Tracks/cedar-technical.json`. Continuous tangents, approximately 2 m chord sampling; not claimed curvature-continuous or an optimized driving line.
- 2067.791 m route, 16 m road, 24 m continuous blended support/runoff, 385 m genuine back straight. First tightening corner, linked esses, two hairpins, faster final sweep. Initial level datum deliberately isolates driver-line behaviour from grades.
- Road paint/posts at all 12 actual ordered race gates, corner boards, 100/50 m braking boards, basic pits, live map and overview camera. Plain test scenery; floating sign boards and simple kerbs remain art debt.

## Evidence

`unity-27/build-01.log`: successful Unity 6000.6.1f1 build, **198 editor checks**. Copied checks in `unity-27/editor-checks.json`. Includes length, flat datum, projection across ±6 m, nonlocal support clearance, both turn directions, no folded support edges, back-straight length. Shared RoadCar/DriveModel and the legacy planner were not edited.

Runtime assembly SHA256: `BAA506EC702DCD96A0BC4770E53525746D4186A2DEB772EA48B8871708341AB6`.

`unity-27/solo-stock` and `solo-club`: same seed 1709, three consecutive physical laps per setup, 120 Hz physics, 4x simulation. These are two race runs/six laps, **not three independently seeded runs**. `technical-analysis.json` passes **11/11 checks**: completed ordered laps, no collision/recovery/missed gate, no sampled body-road departure, matching geometry/physics and terminal trace coverage. All four collider footprint corners are individually projected at 10 Hz. This is not continuous-time collision/boundary proof or 24-car acceptance.

| Setup | Measured lap times (s) | Within 1 m of centreline | Mean absolute deviation | Max body offset |
|---|---|---:|---:|---:|
| Stock | 102.926, 101.621, 101.129 | 89.1% | 0.48 m | 4.77 m |
| Club | 75.882, 75.068, 74.913 | 65.8% | 0.68 m | 4.76 m |

Lap times exclude travel from the starting slot to the timing line. First lap starts at low speed; do not interpret all lap-time change as natural skill variance. The full `race.json` retains start-to-finish race duration and ordered gate results. `race.track.json` stores 10 Hz commands, target/actual speeds, positions, corner labels, body offsets and the course survey.

`technical-baseline.png` plots first-lap actual paths, speed and signed centreline deviation; inspected after rendering. The centreline is explicitly **not an accepted ideal racing line**. The faster club car has lower centreline adherence: line percentage alone is not a quality score. The user's new requirement is recorded in doc25 for a versioned fast reference path, declared corridor tolerance, sector/exit-speed comparisons and traffic-intent exclusions.

The user-requested `user-visible-01` run was launched elevated at 1x (PID 32172) and the unique candidate app window was located, activated and captured using the Computer Use skill. The live screenshot showed the car at the first corner, 13.0 s, with Cedar HUD/map and gate 2. A process handle alone was not treated as visibility proof. Its final result was still pending when this handoff note was written; it may include user inputs and is not assumed to be an automated acceptance run.

The visible run later completed all three laps with zero contacts and recoveries. Its `race.json` is now present; it remains a user-visible run rather than an assumption that no user input occurred.

## Next work

Follow the exact external `C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md`. The circuit baseline was frozen before changing its driver so later effects are attributable. Next extract the legacy input policy with unchanged behaviour, then establish an excellent solo reference line/speed profile using measured physical laps. Add actual physical recovery before mistakes, then passing/traffic and candidate abilities. Retain this raw legacy baseline for comparison.

No claim of finished AI, optimal line, 24-car Cedar racing, final art, normal-window GPU performance, controller acceptance, endurance or campaign progress. Do not replace the live candidate build while the user's demo is running.
