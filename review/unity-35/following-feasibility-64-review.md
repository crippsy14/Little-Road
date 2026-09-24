# Missing following control explains some reference rejections

The previous turn made progress by capturing131 useful opening decisions and implementing/testing63, which was rejected rather than promoted.64 replays all captured unresolved candidates from both retained62 and rejected63. The editor exits0 for both inputs. All68 nominal rejection reasons, whole-body bounds, final route gains/speeds and recorded rejected states reproduce exactly:14/14 reference candidates and54/54 experimental candidates. No input audit records were dropped.

| Source | Captured unresolved decisions | Candidates | Following-constrained candidates accepted | Decisions with an alternative |
|---|---:|---:|---:|---:|
| Retained62 reference |2 |14 |11 |2 |
| Rejected63 experiment |11 |54 |7 |1 |

In the reference, every recorded failure was moving side-entry involving car9. With shared moving-leader control included,11 pass the same road/corridor/side-entry checks and three instead conflict with car19. At timestamp34.483s, the originally preferred path to offset6.01m now forecasts18.39m of progress and8.27m/s exit speed; at34.8s the preferred5.49m destination forecasts12.43m and6.46m/s. Both preferred candidates pass the replay's checks. These timestamps are simulation clock, approximately3s ahead of race elapsed time.

In rejected63, nineteen corridor failures remain corridor failures; most side-entry failures also remain. Only the decision at timestamp37.56s gains accepted alternatives, forecasting about0.6m of progress and essentially a stop. A stationary feasible outcome is not evidence of better racing. This result supports retaining63's rejection and does not justify relaxing corridor or traffic checks broadly.

The counterfactual changes only the own-car forecast controls: it adds the shared live moving-leader controller using an owned constant-velocity/fading-yaw rival rollout. Vehicle physics,2-second horizon, control/physics cadence, road/body margin, current corridor and side-entry tests remain the same. Static scenery, live emergency history, future opponent commands, repeated planning and contact impulses are outside the replay. Consequently these accepted alternatives are a hypothesis for live validation, not safety certification or a proven lap improvement.

65 tests one bounded fallback on the reference configuration: only after normal selection/coasting fails due to traffic, recheck the original preferred path with following included and all live physical/scenery checks. Explicitly prevent nominal cached forecasts from serving this changed model. Do not enable60 trajectory retention or63 small-target comparison. Full controlled/field/solo and performance checks remain necessary; scope and thresholds are predeclared in `following-fallback-65-expectations.md`.

Raw evidence: `following-feasibility-64/reference.json`, `experimental.json`, corresponding editor logs, input/source hashes in the manifest, and the expectation document. Desktop stays55 and hitbox work remains parked.
