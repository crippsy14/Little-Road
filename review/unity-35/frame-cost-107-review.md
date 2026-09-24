#107 scheduling experiment: smoother tails, inefficient small batches

Keep106 as the retained default.107 is opt-in evidence, not accepted scheduling.509 checks pass;170 inputs and assembly are frozen. First plans are immediate; subsequent expensive route selections are staggered across physics steps. Full-rate controls/safety remain. Clock tests prove cadence, phase distribution, recovery suppression, step/time reset and no warm allocation. Every run exits0,24/24 finish, all frames1080p with no drops or warm measured allocations.

Same-build sequential off/on normal-speed rendered comparisons, same roster/seed, no competing editor/game/captures:

| Measurement | Bayview off | Bayview on | Cedar off | Cedar on |
|---|---:|---:|---:|---:|
| Frame p95 ms |26.858|13.513|16.169|12.117|
| Frame p99 ms |38.045|17.690|33.154|17.186|
| Whole AI ms/physics tick |3.111|4.894|2.539|4.271|
| Warm plans/rendered-frame p95 |24|4|24|2|
| Per-car impact records |4|6|0|6|
| Maximum impact relative speed m/s |2.553|3.510|0|2.438|
| Physical/assisted recoveries |0/0|0/0|0/0|0/0|

Collective time improves0.235 car-seconds Bayview and13.747 Cedar; car0 loses0.033s Bayview and gains2.174s Cedar. These are scheduling effects, not deliberate pace tuning. Both on-runs meet16.7/33.3ms frame tails once, but the3ms AI-per-frame p95 remains open. Whole-AI wall cost rises57%/68%: small batches sacrifice parallelism and add frequent join overhead. Do not call smoother frame tails a reduction in total computation. No subjective moving acceptance yet.

Both fail the predeclared25% impact-count gate. These are per-car callback records, generally two per car-to-car encounter, not six distinct crashes. No recoveries occur and contact speeds are modest. The percentage-only gate is unsuitable as a durable criterion when baseline is zero, especially given the user's explicit acceptance of occasional racing contact. Retain the failures; any future revised criterion must be explicit and stated before those runs, with severity and recurring incident patterns as well as count. Do not silently relabel107 as passing.

Off-versus106: Cedar matches all exact parity checks. Bayview matches every stored motion/checkpoint/track/context/overlap/recovery record and finish timing/rank, but two additional collision callbacks appear for cars20/22 at19.05s,1.825m/s. Their contact counters/maxImpact differ. These are during the race, not terminal-tail records. The strict parity failure is retained; the cause of callback variability is not established, and sampled motion parity cannot prove identical unsampled contact events. No claim of full Bayview event parity.

## Next bounded experiment

Try three groups of eight drivers across the100ms planning cycle. This keeps parallel workers occupied while avoiding24 simultaneous final selections. Retain107's per-step distribution as an explicit comparison setting. Goal: retain frame-tail improvement while reducing joined whole-AI cost toward106. This is a scheduling-cost experiment, not another driving-rule change. Avoid an open-ended parameter search; reject if a single bounded group comparison does not justify its complexity.

Fresh online primary-source research is now recorded in docs/41-racing-ai-research-and-next-pace-pass.md. It was read during test waits; no pace-rule implementation has begun. Finish performance decision before the lost-time audit/pace intervention.

Milestone review: accepted visual UI/world unchanged; all races complete but contact evidence prevents full equivalence claim; progression/exploration unchanged and incomplete; physical controller/moving acceptance open; performance promising but tradeoff unresolved; no production/desktop promotion and full campaign goal remains active. All107 processes terminal.
