# Checkpoint flow

Simulation seconds after countdown/green; start-line passage is distinct from beginning to move.

Timing: validated physics-tick checkpoint events.

| Passage | Deadline after green | On time | Passed by report | Result |
|---|---:|---:|---:|---|
| Start line | 12 s | 24/24 | 24/24 | met |
| Checkpoint 1 | 20 s | 24/24 | 24/24 | met |
| Checkpoint 2 | 29 s | 24/24 | 24/24 | met |
| Checkpoint 3 | 38 s | 12/24 | 12/24 | pending |
| Checkpoint 4 | 47 s | 0/24 | 0/24 | pending |
| Checkpoint 5 | 56 s | 0/24 | 0/24 | pending |
| Checkpoint 6 | 65 s | 0/24 | 0/24 | pending |
| Checkpoint 7 | 74 s | 0/24 | 0/24 | pending |
| Checkpoint 8 | 82 s | 0/24 | 0/24 | pending |
| Checkpoint 9 | 91 s | 0/24 | 0/24 | pending |
| Checkpoint 10 | 100 s | 0/24 | 0/24 | pending |
| Checkpoint 11 | 108 s | 0/24 | 0/24 | pending |
| Lap-one finish | 117 s | 0/24 | 0/24 | pending |

All-car deadlines are strict. One or two delayed cars are flagged for incident review, not called a traffic jam. Multiple delays flag throughput failure, not a proven cause. Contact correlation does not establish blame or cause.

See `checkpoint-flow.json` for per-car IDs, previous checkpoint and incident evidence.
