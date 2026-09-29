# Technical AI test circuit: Tsukuba influence

21 September 2026. User-requested next work after the hill/combined equipment tests. This is a track/evaluation brief supporting the existing [racing AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>), read in full. It does not replace that plan or introduce another competing ability system.

## Reference and intended challenge

Tsukuba's official guide describes a short home straight, an S sequence feeding a hairpin, a right-angle bend leading into faster left turns, another hairpin before the longer back straight, and a fast final corner. Several corners change radius, making entry and exit planning important. The listed home and back straights are 282 m and 437 m. These are reference facts, not dimensions already implemented in Little Road. [Official Course 2000 guide](https://www.tsukuba-circuit.jp/guide/course-spec.html), accessed 21 September 2026.

The official overview characterises Course 2000 as roughly 2 km, relatively flat, with two straights connected by varied technical corners and banked turns. [Official facility guide](https://www.tsukuba-circuit.jp/guide/guidemap.html). A published [course/longitudinal survey](https://www.tsukuba-circuit.jp/guide/img/course-spec_2000_01.pdf) is available for further geometry reference. Its text extraction is scrambled in places; do not infer precise widths, bank angles or elevation profiles from that extraction alone.

Our inference is that this mix is valuable because it makes the AI prepare for the next corner, manage changing curvature and protect exit speed, while still giving equipment advantages room to show. Author an original layout with a distinct silhouette and identity, using those driving challenges as influence. Do not present the fixture as a licensed replica or reproduce branding. Initially it is a development circuit, not a finished campaign track or a content-hour claim.

## Layout requirements for the first build

| Sector type | AI capability it exposes | Evidence to capture |
| --- | --- | --- |
| Start straight into a tightening turn | Braking-distance preview, traffic compression and entry judgment | Brake point, predicted/actual deceleration, overlap, gate validity |
| Linked left/right bends into a slow corner | Planning beyond the nearest apex and smooth direction changes | Entry/apex/exit speed, steering saturation and following-straight loss |
| Two different hairpins | Inside/outside options, braking and acceleration recovery | Safe lateral space, minimum speed, exit advantage and completed passes |
| Medium-speed bend into a faster arc | Transition timing and holding a line while alongside | Body envelopes, lateral acceleration, aborted moves and line error |
| Longer back straight | Full usable power, gearing and prepared overtakes | Closing speed, manoeuvre commitment, clearance after passing |
| Fast final corner | Sustained grip demand and long preview | Tracking stability, target/applied inputs and speed onto the next lap |

Start with approximately 1.6–2.1 km of continuous road and enough width/runoff for a 24-car field. These are prototype bounds to refine using measured stock/club capabilities, not frozen production dimensions. Retain genuine straights and usable side-by-side room. Check the entire car envelope against usable boundaries; a centreline-only road-departure check is insufficient.

Keep the first layout mostly level so line/traffic faults can be diagnosed independently of the new grade system. Later add mild elevation or banking only with physical validation; the separate hill fixture remains the gravity/contact control. Use smooth tangent/curvature transitions and meaningful turn radii rather than simply increasing waypoint density. Verify projection continuity where adjacent sections run close together and detect self-intersections/overlapping road supports before racing.

Include visible timing/checkpoint markers, corner/sector names, braking-distance boards and a simple overview. The user specifically liked that the proving-ground markers correspond to actual timing gates. Add spectating and reproducible starting scenarios so a problem can be replayed in a visible window. Initial plain scenery should make the driving legible; campaign dressing follows driving/layout acceptance.

## Implementation and validation order

1. Finish the current hill/combined/coasting pass and freeze its evidence. Keep those deterministic equipment tests separate from driver skill variation.
2. Recheck the detailed plan's source integration points. Retain legacy AI behind a developer switch and introduce its input-policy/observation/telemetry seam without changing vehicle forces for a driver profile. The current controlled proving driver is a useful baseline, not automatically the excellent production racer required by phase B.
3. Author/bake the technical layout with physical boundaries, ordered gates and corner regions. Establish clean solo laps across stock and club configurations, including different starts and nearby setups. Compare centreline and candidate fast lines through linked corners using actual laps, not only geometric smoothness.
4. Implement the plan's physical recovery before routine mistakes. Test stopped, sideways, wrong-way and off-road starts, missed gates and waiting for approaching traffic. Report assisted resets separately.
5. Introduce two-/three-car passing and side-by-side corner tests, then closing gaps, aborted passes, stopped traffic and blocked recovery. Use predicted swept car envelopes and the same physics for every racer.
6. Prototype the plan's candidate abilities/styles and contextual errors only after the competent clean driver works. Keep equipment, persistent ability, familiarity and temporary form independent. The first human-review slice should follow the plan's three differentiated drivers plus physical recovery; expand to 24-car rosters after that behaviour is credible.
7. Run full-field seed/grid/equipment variation and measured AI CPU costs. Scale test durations to the actual course/lap length; Bayview's short-race timeout must not falsely fail a longer technical circuit. Retain failures and review moving incidents with the user.

Use the plan's provisional budgets and acceptance hypotheses as starting points, recording adjustments explicitly. Tests must distinguish line quality, braking, car control and racecraft rather than reducing all skill to a straight-line speed multiplier. Teammate XP, familiarity persistence and endurance/pits remain the later integration phase; the user has brought forward AI driving work, not all of those systems simultaneously.

## Status

The hill/combined milestone is delivered in checkpoint 20. Cedar Technical v1 is now authored as a reusable JSON knot/handle asset and a 2.068 km runtime circuit, with 16 m asphalt, 24 m continuous support/runoff, a 385 m back straight, linked bends, two distinct hairpins and a broad final sweep. Its 12 visible timing markers are the actual ordered race gates. The prototype has a live map, corner names, braking boards and a chase/overview camera switch. This is plain test scenery, not accepted campaign art.

Build 01 passes 198 editor checks including seven Cedar geometry checks (length, level datum, full-width projection, nonlocal support clearance, both turn directions, no folded support edges and back-straight length). Actual stock/club baselines each complete three consecutive laps: 11/11 runtime checks pass, with no contacts, assisted resets, missed gates or sampled full-body road departures. See `review/iteration-34-cedar-technical-baseline.md` and `review/unity-27/technical-report.md`. The user-requested visible run has been launched and verified on the desktop. The legacy production AI remains unchanged: policy extraction, excellent reference driving, physical recovery and differentiated abilities are the following phases, not features proved by this track build.

## Reference racing line and adherence

Recovery checkpoints: [iteration 38](../review/iteration-38-physical-recovery.md) records the first 14 passing physical scenarios; [iteration 39](../review/iteration-39-blocked-recovery-and-preview.md) adds permanent-blockage fallback, safe assisted placement and obstacle anticipation while racing. Use `Watch Physical Recovery.cmd` for the stock wall replay. Recovery remains opt-in; broader traffic/corner/body coverage and player acceptance remain open.

Phase A follow-up: the legacy policy has now been extracted behind the timestamped observation/command interface, with a separately selectable shared pre-controller snapshot. [Validation](../review/iteration-35-ai-input-policy.md) preserves the frozen solo and 24-car sampled controls/motion exactly after correcting intermediate float storage. Excellent racing-line driving, physical recovery and skills remain subsequent work.

See [racing-line research and concrete reference implementation](26-racing-line-research-and-reference.md) for the user-requested research, path/speed optimisation and acceptance process. The scoring reference must be a validated racing line, not the road centre.

User addition, 21 September: use an ideal/reference line to measure the percentage of driving time spent following it. Perfect adherence is not required; higher average adherence should normally indicate better consistency in clean solo driving.

Make the reference a versioned authored/baked path with arc distance and lateral offset, initially established by the competent driver's actual clean laps and improved against lap time and sector exit speeds. Do not call the geometric centreline an optimal racing line. Validate a fast reference across stock/club and nearby equipment; where different capability envelopes require different lines, record the reference/profile used rather than comparing everyone against an unsuitable single path.

Measure **percentage of active time within a declared corridor** (initial diagnostic tolerance ±1 m), mean absolute deviation, p95/worst deviation and sustained departures by corner. Keep signed deviation traces and overlay the car path/reference on the survey. A line-following percentage is diagnostic alongside completion, full-body boundaries, lap/sector times and exit speed; driving unnecessarily slowly must not count as excellent driving. Do not reward unsafe corner cutting or missing gates.

Evaluate ordinary adherence on clear-track samples. When racecraft is implemented, label follow/pass/defend/yield/recovery and traffic-constrained samples, report them separately, and measure return to the preferred line when clear. A safe intentional overtake should not be treated as poor line judgement. Keep driver ability, setup, race form and track familiarity separate in paired tests.

The first Cedar audit currently implements **centreline-v1**, explicitly labelled as a geometric baseline, with 10 Hz traces, ±1 m percentage, mean/p95/max deviation and all four body corners projected onto the road. Those measurements do not establish an ideal line or detect every sub-100 ms departure. Accepted fast-line authoring, traffic classification, time-weighted handling of irregular trace intervals and incident-rate telemetry remain phase B/D work in the exact external implementation plan.

Follow-up: the [smooth-reference milestone](../review/iteration-37-smooth-reference-and-human-comparison.md) adds candidate path/speed profiles, a shared-physics solo reference driver, time/distance-weighted adherence and physical candidate comparisons. `Watch Reference Racer.cmd` replays the latest benchmark fullscreen with its line overlay; `Record Your Cedar Laps.cmd` records human comparison laps separately. The user's first session is preserved, with lap 2 excluded from pace comparison at their request. Traffic intent classification and incident-rate telemetry remain later work.

Support correction, traffic candidate 24: Cedar's level road now uses a single planar collision surface at its 0.025 m tarmac datum. This removes measured false sideways impulses from speculative CCD against coplanar mesh seams; car collision mode is preserved. The former 25 mm road-to-runoff blend is now physically level in this deliberately flat fixture. Visual layers, route, 16 m asphalt, dirt grip and body-boundary scoring are unchanged. The old collision geometry remains available with `--tessellated-cedar-support`; versioned candidate 23 also preserves the original replay. This is a Cedar-only correction, not validation of terrain contacts on Bayview or the separate grade fixtures. See `review/unity-34/notes.md` and `field-23/road-contact-analysis.json` for the exact replay and isolated comparison.
