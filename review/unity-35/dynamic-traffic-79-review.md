# Prototype 79: fresh hold and scenery fallback

23 September 2026. Bounded follow-up to 78. 467 build checks pass. Every paired collective gate/course time remains faster than the reference. Contacts in the narrow-road fixture fall from five to zero, but corridor failures remain and the three-car unresolved count worsens. Not promoted; no 24-car dynamic result exists.

The six-candidate traffic set now always includes a freshly anchored hold, including when a retained curve exists. Retention consumes one longer passing approach; the remaining longer approach goes to the faster estimated side. Both short passing sides and guide return remain. After a scenery rejection, the second physical check considers a finite guide alternative instead of only maximizing road-edge clearance. A failed dynamic-plan capture now includes its prior trajectory. The solo planner and two nominal physical-check budget remain unchanged.

An unchanged diagnostic repeat of 78's narrow-road case exactly matches its motion/events/results. Early failed choices are a passing trajectory and an old hold, both rejected by scenery. This supports the scenery-specific guide fallback; it does not establish all scenery safety.

| Fixture | Collective course 78 → 79 (s) | Unresolved 78 → 79 | Contacts 79 |
|---|---:|---:|---:|
| pass | 60.700 → 60.700 | 0 → 0 | 0 |
| queue | 94.000 → 93.983 | 2 → 0 | 0 |
| corner-pair-left | 131.776 → 131.601 | 8 → 4 | 0 |
| corner-pair-right | 131.551 → 131.617 | 17 → 11 | 0 |
| corner-three | 206.546 → 206.346 | 16 → 25 | 0 |
| rear-control-left | 52.116 → 52.116 | 0 → 0 | 0 |
| rear-control-right | 52.108 → 52.108 | 0 → 0 | 0 |
| no-room | 62.150 → 62.175 | 47 → 4 | 0 |

All 18 entrants per side finish, with no contacts, road departures, physical recoveries, runtime errors or measured warm driver allocations. Corner checkpoints are earned. All eight disabled fixtures exactly match 78 motion/events/results, and solo behavior exactly matches 77. These are partial-course experiments, not full standing-grid lap times.

The acceptance report remains failed: strict sustained-pass-by-exit checks fail in pass/queue; right-pair, three-car and narrow-road unresolved counts exceed the reference. Left-pair unresolved counts now equal the reference, not zero. The scenery-contact criterion was explicitly inspected (zero) but is not yet a named field in this version's archived analyzer. The next analyzer adds that explicit check without rewriting this result.

Further attribution found that two late 78 corridor failures arise from cars 160.210/167.112 metres behind on a different Cedar section. Projecting world position along one heading falsely labels them alongside. 79's later failures have similarly impossible corridor bounds. In early 79 left-pair failures, a fresh hold exists but the second check can still choose another lateral movement. These findings motivate 80: route-local adjacency and choosing the fresh hold specifically after a corridor rejection. Retain legitimate nearby closing-car merge reservations and all world-space collision sweeps.

No moving rendered review, full-field scaling or new progression/exploration/UI/save/controller acceptance occurred. CPU costs remain a separate concern; personality/adventure/campaign work and existing defects remain open. Desktop shortcuts remain unchanged.

Evidence: `dynamic-traffic-79-expectations.md`, paired raw runs and `analysis.json`, `disabled-parity-summary.json` and per-case reports, `solo-parity/motion-parity.json`, unchanged 78 `no-room-audit`, build checks/log, frozen `source-79/`, manifest and validation tools. Working source subsequently advances to 80; 79's frozen source hashes, not current working files, identify its binary inputs.
