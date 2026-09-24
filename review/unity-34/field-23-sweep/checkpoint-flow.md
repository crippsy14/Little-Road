# Checkpoint flow

Simulation seconds after countdown/green; start-line passage is distinct from beginning to move.

Timing: validated physics-tick checkpoint events.

| Passage | Deadline after green | On time | Passed by report | Result |
|---|---:|---:|---:|---|
| Start line | 12 s | 24/24 | 24/24 | met |
| Checkpoint 1 | 20 s | 24/24 | 24/24 | met |
| Checkpoint 2 | 29 s | 24/24 | 24/24 | met |
| Checkpoint 3 | 38 s | 24/24 | 24/24 | met |
| Checkpoint 4 | 47 s | 23/24 | 24/24 | isolated-delay-review |
| Checkpoint 5 | 56 s | 18/24 | 24/24 | multiple-car-delay |
| Checkpoint 6 | 65 s | 21/24 | 24/24 | multiple-car-delay |
| Checkpoint 7 | 74 s | 20/24 | 24/24 | multiple-car-delay |
| Checkpoint 8 | 82 s | 22/24 | 24/24 | isolated-delay-review |
| Checkpoint 9 | 91 s | 24/24 | 24/24 | met |
| Checkpoint 10 | 100 s | 24/24 | 24/24 | met |
| Checkpoint 11 | 108 s | 24/24 | 24/24 | met |
| Lap-one finish | 117 s | 24/24 | 24/24 | met |

All-car deadlines are strict. One or two delayed cars are flagged for incident review, not called a traffic jam. Multiple delays flag throughput failure, not a proven cause. Contact correlation does not establish blame or cause.

See `checkpoint-flow.json` for per-car IDs, previous checkpoint and incident evidence.
