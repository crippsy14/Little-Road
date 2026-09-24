# Human Cedar comparison laps

Human comparison only; no training data or accepted reference changed

Lap 2 is retained in raw evidence but excluded at the user’s request.

| Lap | Time (s, approx.) | Used | Centre outside asphalt samples | Body outside asphalt samples |
|---|---:|---|---:|---:|
| 1 | 63.7 | Yes | 2 | 45 |
| 2 | 70.6 | No: off-track attempt | 42 | 65 |
| 3 | 63.0 | Yes | 0 | 35 |
| 4 | 62.6 | Yes | 4 | 59 |

Selected flying laps differ by 0.44 s. Human official best: 62.601 s; candidate AI: 64.976 s. The candidate is 2.37 s (3.8%) slower.

Whole run: {'contacts': 0, 'recoveries': 1, 'missedGates': 1}. All retained samples are labelled by input owner. Off-road observations are kept, not silently classed as clean.

## Interpretation

Compare measured pace, speed/position plots and legal road usage together: candidate adherence alone is not proof of optimal pace. Do not automatically copy the human line or train on these samples.

## Measurement limits

- Individual lap and sector splits interpolate 10 Hz telemetry; displayed to 0.1 s. Official best lap is timed by ordered gates.
- Lap 1 begins at the timing-line crossing after grid acceleration; compare flying laps 3 and 4 for steady pace.
- The road/body boundary audit is stricter than the permissive checkpoint corridor. User-selected laps are not automatically full-body-on-asphalt laps.
- Centreline adherence in the raw human report is not an ideal-line score.
- Builds may differ; same reported tuning and geometry are verified, but inspect the corresponding build manifests before attributing changes.
