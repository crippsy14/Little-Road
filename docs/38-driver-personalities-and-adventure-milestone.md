**87–88 connected first slice:** actual entry roster, equipment-only rating,3D car/loadout/personality preview and retained AI now connect to adventure.88 fixes Race again, previews the next field, preserves the original reward on cancel and commits one new serial on confirmation. Two24-car three-lap events finish and260 credits persist through a new-process reload. One second-event group needs prolonged recovery, including two assisted resets; this remains an explicit issue rather than a clean race-quality pass. See [88 review](../review/unity-35/race-preview-88-review.md). Physical DualSense, player review, stock connected racing and rendering budgets remain open. Next user-requested work is [dashboard, relative timing and playful UI](39-driving-hud-and-playful-ui.md).

**86 first personality slice:** stable24-driver identities; independent braking judgement/risk; immutable seeded race form. Neutral preserves both tracks exactly. Risk0/5/10 sustained passes9.150/7.333/6.742s in one clean matched fixture; early braking enters6.605m sooner with unchanged opening throttle. Both mixed fields finish24/24 without recovery.484 build checks and13 causal live checks pass. The40 physical form runs are now complete: steady lap SD0, variable0.026279s, a tiny effect rather than a demonstrated finishing-position distribution. See [form follow-up](../review/unity-35/driver-form-86-review.md) and [86 review](../review/unity-35/driver-identities-86-review.md). Broader manoeuvres/skills, familiarity and teammate progression remain future work.

# Driver personalities into adventure

23September. The user explicitly asks to keep the performance pass bounded, then move into personalities and adventure after extensive baseline AI refinement. This supplements documents20,21,34 and the external racing-AI plan; it does not replace teammate growth, track familiarity, other skills or the full campaign scope.

## Immediate sequence

1. Complete85's joined dynamic forecast comparison. Retain it only if measured overall cost improves while recorded behaviour remains equivalent. Do not continue a long series of performance experiments before personality work; disclose remaining frame budgets.
2. Implement a small authored driver roster with actual braking judgement, risk preference and consistency/race form. Validate matched-car behaviour before presenting those traits as meaningful. Preserve the exact neutral83/84 baseline.
3. Use one immutable race-entry roster for both a controller-readable pre-race display and the actual spawned cars. Show all24 actual loadouts, body/livery, equipment-only rating and implemented driver traits. Do not roll a separate display roster or re-roll form on opening detail panels.
4. Integrate the retained AI/roster into Bayview adventure entry, countdown, rear-grid start, checkpoint/finish, exactly-once rewards and save/reload. Keep a developer baseline switch and source/build evidence; do not silently update desktop shortcuts before the playable milestone is ready.

## First active driver dimensions

Braking judgement changes the usable braking estimate in the reference and local path speed calculations. It never changes actual braking force, tyres, engine power or attainable straight-line speed. Existing deterministic neutral/earlier/early presets establish the mechanism; normal roster values should stay near the capable baseline, with visibly cautious characters as exceptions rather than making the whole field timid.

Risk is a0–10 preference for spare passing space and contested opportunities, average near5. Retain the user-accepted current risk5 behaviour as neutral. Risk0 can decline marginal gaps; risk10 can accept less spare space while physical footprint, scenery and the same collision/traction constraints remain authoritative. Verify this affects decisions in the current dynamic planner: merely storing/displaying a risk value is insufficient. No forced collision quota, invisible power or target finishing order.

Consistency controls the spread of small race-form variation, sampled once per race from a stable driver ID, race seed and subsystem key. Start with immutable effective planning parameters so live control and worker forecasts see exactly the same driver. Do not use frame-by-frame random steering/throttle. Keep average capability separate from spread and explicitly report clipping/skew near braking limits. Corner-opportunity execution events, reaction errors and additional abilities remain a later slice, with owned event state and causal tests as described in34.

Only implemented and validated influences are shown to players. Do not show fictional active ratings for line judgement, car control, racecraft, patience or familiarity before their respective behaviours exist. Driver identity is independent of equipment, cosmetic body and decal; teammate learning and familiarity have no invented production values in this slice.

## Data and integration boundary

- Authored driver definition: stable ID, display name, descriptive style, base braking judgement, risk and consistency. Validate ranges and duplicate IDs when loading the small local asset.
- Effective race driver: definition ID, race seed, sampled form and immutable DriverPlanningSettings. Use a specified integer hash/PRNG, independent of entrant order, cosmetics and Unity random state. No live random draws in forecasts.
- Race entrant: entrant ID, driver record, actual fitted part IDs, body/livery and starting slot. Clone/validate equipment at entry; derive CarTuning once from that record. The same record drives preview and spawning.
- Rating: versioned, equipment-only estimate derived from the existing catalogue and interactions. Driver form/risk, name and body shape cannot inflate it. Prove its rough ordering with existing equipment measurements; never promise finishing position.

Avoid a broad framework rewrite. Add these seams to RaceTrial/RoadCar and AdventureGame where their responsibilities already exist. Trial flags keep neutral comparisons available. Adventure integration must preserve the player's purchased configuration and avoid applying AI form to human input.

## Validation and review

Before running trait sweeps, fix profile values, variation bounds and sample population in an experiment note. Exact neutral parity on Cedar/Bayview; same car/physics under every profile; deterministic driver/form independent of cosmetics and list order; changed driver parameters invalidate forecast reuse. Measure straight throttle, braking entry and corner exit rather than merely accepting different lap totals. Use sufficient paired seeds for variance claims, not three runs.

Then clean passing/rear/three-car tests and mixed24-car races on both courses. Track finished counts, checkpoints, collective time, per-driver ranks/laps, incidents and recovery. Occasional marginal road-edge samples are accepted by the user; retain them diagnostically and distinguish sustained excursions/shortcutting. Do not turn unfamiliarity or a slower driver into a pile-up by design.

Player review should judge recognisable, enjoyable differences. Since the user is away walking Leah, unattended runs provide engineering evidence only; save a visible demonstration for their return rather than asserting subjective acceptance. Rendered performance and controller navigation must be measured on the integrated milestone; fixed1080p controls should remain distinct from mixed-resolution observations.

Full adventure acceptance includes stock and upgraded entries, actual preview/loadout agreement, player24th grid slot, all gates, finish screen, reward exactly once, return to exploration and reload. Validate keyboard and physical DualSense with a user session. Production-ready fidelity,12hours of meaningful content, additional parts/bodies/events, endurance/pits and teammate progress remain incomplete.
