# Study 125: false missed-gate stops (`--gate-first`) and the Hillcrest Raceway queues

28 September 2026. Dev build, 1010 editor checks. Runs:
- Raceway: `review/hillcrest-race/s125-{off,gatefirst}-s{1709,1710,1711}` (`tools/race-hillcrest.ps1`, 24 context cars, 2 laps, `--ai-preset field --bayview-transfer-study`).
- Cedar, Bayview and Cedar Hills: `review/unity-35/frame-cost-125/` (`tools/run-pace-study.ps1`, 24 cars, 1 lap). Cedar Hills runs `--grade-aware` in both arms.

## The fault
Cars stopped dead on the elevated raceway, and whole fields then queued behind them. On seed 1711 this meant 59 stalls, 7974 car-seconds stationary and 82 assisted resets. The race was deterministic: r7, r10 and r11 were identical stall for stall.

Diagnosis (`tools/analyze-stalls.py`, `PHYSICAL_RECOVERY` log lines):
- **Grip loss ruled out.** `unsupportedSeconds` (a new per-car diagnostic) was about 0 for racing cars. The one exception was 24 s from a car parked off the edge of the runoff.
- **Handling unchanged by the hills.** Lateral slip, yaw error, heading error and time near the edge match between the level (r6) and elevated (r7) raceways.
- **The trigger.** At 35.49 s on lap 1, car 16 entered `Stabilise` at progress 844.85 m, with the gate at 844.34 m.
  - `PhysicalRecovery` treats progress more than 0.5 m past an uncredited gate as a skipped checkpoint.
  - Recovery runs inside `Plan()`, before `TrackGates()` in the same `FixedUpdate`.
  - Progress is a projection onto 1.5 m route segments. On the inside of a tight corner it jumps by about offset × segment bend when the nearest segment changes.
  - Turn 5's inside line first appears 0.54 m past gate 6 in a single 28 m/s tick (editor check).
- **The cascade.** Recovery braked a racing car to a halt mid-corner. The pack piled in, `Wait for clearance` held them all, and assisted resets put cars back into the queue.
- **Not hills-specific.** The same false trigger hit the level raceway (r6 s1709, turn 5 on lap 2) and r7 on every seed. Cedar, Bayview and Cedar Hills never trigger it (0 of 45 recovery starts).

## Change: `--gate-first` (off by default; `RoadCar.GateFirst`)
The car credits a checkpoint crossed this tick right after updating progress, before the driver and physical recovery read `nextGateProgress`. The later `TrackGates` call is skipped.

Checks (`CheckpointChecks`):
- The flag is off by default.
- The raceway's turn-5 inside line can land more than 0.5 m past gate 6 in one tick. This is the hazard the flag removes; measured 0.54 m.

## Results (collective seconds of all 24 finishers; assisted resets / physical recoveries / contacts)

| Run | 1709 | 1710 | 1711 |
|---|---|---|---|
| Raceway off | 4051.8 (4/12/13) | 4270.1 (7/7/24) | 11757.5 (82/22/40) |
| **Raceway gate-first** | **3545.9 (1/3/8)** | **3592.1 (1/6/8)** | **3520.7 (0/5/7)** |
| Cedar off / gate-first | 2116.613 / 2116.755 (0/0/1 both) | 2124.444 / 2125.119 (0/0/2 both) | 2113.989 / 2113.705 (0/0/0 both) |
| Bayview off / gate-first | 803.352 / 803.411 (0/0/3 both) | 796.659 / 796.084 (0/0/0 both) | 794.134 / 794.100 (0/0/2 → 0/0/1) |
| Cedar Hills grade / grade+gate-first | 2197.610 / 2197.793 (0/10/1 both) | 2181.522 (0/5/2) / not run | 2192.070 (0/7/1) / not run |

Stalls (at least 2 s below 3 m/s while racing), raceway off → gate-first:
- 1709: 12 (454 car-s) → 4 (55 car-s)
- 1710: 11 (733 car-s) → 6 (63 car-s)
- 1711: 59 (7974 car-s) → 3 (28 car-s)

Last finisher off → gate-first: 237.7 → 185.3 s, 383.5 → 205.5 s, 790.8 → 163.4 s. The winner is unchanged where there was no jam (113.0 / 113.1 s and 112.2 / 112.2 s).

- **Cedar and Bayview:** not bit-identical. Crediting a gate one tick earlier changes what the planner reads on that tick, so races diverge chaotically by about ±0.7 s. The mean change is −0.003 s over the six races, with no resets or recoveries in either arm.
- **Cedar Hills:** only seed 1709 ran with the flag, and it matched (+0.18 s, the same 10 recoveries). The study was stopped early at Richard's request because the flag cannot act there: 0 of 45 Cedar Hills recovery starts match the false-gate signature. `cedar-hills-s1710-gradegf` is incomplete and marked `ABORTED.txt`.
- **Decision: KEEP.**
  - Every race finished 24/24 with no new assisted resets.
  - Cedar and Bayview are neutral (mean −0.003 s).
  - The raceway improves by 12–70% collective, with assisted resets down from 4/7/82 to 1/1/0.
  - `--gate-first` joins the shared AI preset (`RaceAiPreset`), so adventure races and `--ai-preset field` trials use it from the next build. Study baselines from 126 on include it.

## Still open
- **Recovery at speed.** A car crossing the 7.2 m offset at racing speed is braked with the wheel straight: `Stabilise` sets steering 0 and full brake.
  - At 34 m/s that slides it off the runoff (r10 car 8, 420 m, stranded 20 s at a 15 m offset).
  - A steer-back-while-slowing rejoin would keep such cars on the tarmac. This needs its own flag.
- **The residual raceway stops:** 3–6 short recoveries per race at 130–210 m and 470–510 m.
