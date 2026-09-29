# Situational racing and full-field throughput

**Current implementation authority:** [document 32](32-racing-ai-ground-truth-and-implementation.md) supersedes the tactical candidate-search design below with the selected F1-inspired context policy. The fixed throughput deadlines, honest incident reporting and performance requirements here remain applicable. Historical candidate descriptions are retained as evidence, not instructions to stack the old planner beneath the new driver.

21 September 2026. Complements the [AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>) and [racecraft fixtures](27-recovery-and-racecraft-fixtures.md). Baseline competence comes before personalities.

## What “current best racing line” means

The offline reference line describes a useful clear-track approach to the circuit. A live driver needs a **best feasible trajectory from its actual position, heading and speed**, including where it should brake and accelerate. That trajectory changes with equipment, available road, nearby cars and upcoming corners. It is a local approximation, not a claim of finding the mathematically optimal line every frame.

Plan the path and speed together. An inside line usually needs a different entry speed and exit approach from an outside line. Being beside another car constrains which road can be used. Returning to the reference line must wait for clearance; adherence percentage must never reward cutting across another driver or joining a queue unnecessarily.

Use separate layers with clear responsibilities:

1. Reference route and corner speed guidance: reusable track geometry, legal gates and equipment-dependent grip, acceleration and braking.
2. Race decisions: look for useful space, compare staying behind with passing, retain a useful decision, and abort when the opportunity closes.
3. Local trajectory: bounded candidate paths from the actual pose, achievable speed profiles, predicted body clearance and room through the corner exit.
4. Continuous control: steering, throttle and braking through the ordinary vehicle physics. No position prescribing, extra AI grip or hidden equipment knowledge.
5. Recovery: wrong heading, departure, missed gate or genuine obstruction. Ordinary traffic following is not a reason to reverse or reset.

This separation follows the racing-specific strategic/tactical/control architecture described by Simon Tomlinson and Nic Melder in [Game AI Pro, chapter 38](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter38_An_Architecture_Overview_for_AI_in_Racing_Games.pdf). That chapter also discusses multi-opponent space selection and hysteresis to avoid indecisive overtaking. We use these principles; its optional race choreography is not part of our fair-equipment simulation.

## Failure being corrected

The first local implementation passed sparse two-/three-car fixtures but jammed badly with 24 cars. It rejected an entire path when a predicted obstruction lay ahead, even where braking and continuing behind the obstruction were possible. Holding an offset relative to the reference line could also move several cars laterally together through a corner. This produced unnecessary stops, repeated recovery and poor racing.

Development candidate 11 changes local paths to road coordinates, considers a separate reference approach, repairs a blocked future path with a braking constraint, scores reachable progress, caches each observed opponent's short prediction across candidate comparisons, and staggers planning updates. It records planner call count, total, mean and maximum wall time. These are experimental changes, subject to the tests below; neither the architecture nor one successful run establishes acceptance.

Still required: stable safe commitment and explicit abort reasons, braking-zone opportunity controls, reliable side-by-side cornering, varying starts/speeds/body envelopes, and moving visual review. Predictions use observed motion; they must not consult opponents' future controls. Longer-range predictions are uncertain and should inform decisions without turning remote hypothetical conflicts into immediate deadlock.

## User-proposed field test

Measure elapsed time from the first car moving above 1 m/s to the last car completing its first valid lap. Also record the launch of every entrant, finish spread, sustained passes, stationary time, contacts, departures, missed gates, physical recoveries and assisted resets. Throughput alone can reward reckless behaviour, so it must be combined with the other checks.

For the initial Cedar 24-car street/club standing-grid fixture, paired solo standing laps measured 86.434 s (street) and 67.322 s (club). The provisional cutoff is `ceil(slower solo × 1.25 + 8 s) = 117 s`. Keep it fixed while improving this fixture. The extra allowance covers congestion and rear-grid launch travel; it is an engineering target, not a campaign race time or universal track rule.

The executable analyzer requires all 24 to complete, first movement under two seconds, every car moving within eight seconds, the 117-second field window, no assisted resets/missed gates/sampled body departures, no measured pose penetration above 2 cm, no recovery during the first 15 seconds, and at least one sustained club-over-street pass from a meaningfully trailing start. Contacts and later physical recovery remain reported diagnostic quantities; they need explicit review even if all hard checks pass. Sampling has limits and does not prove continuous clearance.

Evidence lives in `review/unity-34`; `tools/analyze-field-race.py` retains failed results as well as improvements. Small fixture passes cannot substitute for this full-field check.

### Start-line and checkpoint throughput

The user's additional requirement distinguishes beginning to move from actually clearing the start line. All 24 entrants should cross the start line within **12 simulation seconds after green** in this initial fixture. For each subsequent checkpoint, use `ceil(max(matched solo passage times) × 1.25 + 8 s)` after green. This yields deadlines of 20, 29, 38, 47, 56, 65, 74, 82, 91, 100 and 108 seconds for checkpoints 1–11, and 117 seconds for the first-lap finish. These are provisional Cedar-specific thresholds, held fixed during this iteration.

`tools/analyze-checkpoint-flow.py` records on-time and total passage counts, each car's passage time, late/missing IDs, previous checkpoint, and contacts/recovery observed in the affected sector. One or two late cars are labelled **isolated delay — review**; three or more are labelled **multiple-car delay**. Those labels describe the measured distribution, not a proven cause. Contact evidence can suggest an incident but does not establish blame or causation. Keep the strict all-car timing result visible even when one incident explains its failure; do not tune natural incidents out merely to turn every report green.

The game now records validated checkpoint crossings at the physics tick, including their required race progress, so resets cannot invent passages and the initial start crossing is distinct from the lap-one finish. Older runs use explicitly labelled 10 Hz gate transitions plus official finish times. Candidate 15's six corridor runs validated all 13 ordered passage events and agreement with official finish times. Its visible 24-car run cleared the start and checkpoint 1 on time with all cars, but only 17 finished within 117 seconds; the field remains unaccepted. See the [subsequent user review and design](30-anticipation-and-collision-response.md).

## Performance acceptance

Measure performance independently from driving quality. Smoother cars may reduce recovery work but are not a substitute for an efficient planner.

- Keep steering and physical integration at their existing rate; schedule the more expensive planning at a lower rate distributed across cars.
- Bound candidate count and prediction horizon. Reuse buffers and cached opponent predictions rather than recomputing them for every candidate.
- Measure planning time and frame times before choosing further optimizations. Track lookup and collision queries are suspects, not established bottlenecks without measurements.
- Use accelerated hidden runs for behavioural iteration. Their frame times are not a real-time presentation benchmark. Measure the full visible 24-car race at 1×, without concurrent test processes, including starts and crowded corners.
- Retain full collision, checkpoint, equipment and recovery behaviour while optimizing. Do not improve frame rate by silently disabling the systems under test.

Personality and skill differences should vary the quality and willingness of these decisions only after competent baseline racers can use the available road. They must not mask a shared inability to pass or negotiate corners.
