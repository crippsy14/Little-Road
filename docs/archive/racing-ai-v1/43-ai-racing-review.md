# 43: Racing AI review: a fresh look

24 September 2026. A from-scratch review of the Little Road racing AI through the lens of commercial sim-racing AI (iRacing / rFactor / ACC style). It asks what a racing-AI lead would keep, question and change, independent of the experiment history in `42-ai-pace-experiments-handoff.md`. Numbers come from build 115/116-equivalent races (Cedar and Bayview, seed 1709, 24 cars, mixed roster).

## What the system is

- **Car:** point-mass "friction circle" model (`DriveModel`). Braking, cornering and power share one grip budget, plus drag and a gear ceiling. Honest and fast, with no tyre model. A sensible base for an arcade-sim game.
- **Solo pace:** a precomputed racing line (`RacingLine`) with a forward/backward speed profile (`RacingSpeedProfile`), followed by pure pursuit.
- **Traffic:** every 0.1 s (staggered in 3 groups) each car builds ~6 lateral candidate paths (hold, guide, pass left/right of the blocking car, slower variants). It ranks them by an estimated time to a gate 120–360 m ahead and physically forecasts the top 2 against road, scenery and other cars. If both fail it falls back through several layers (guide fallback, coast continuation, following fallback, conditional hold). A separate speed chain (follower → moving-leader cap → static cap → immediate guard) then sets the pedal.
- **Drivers:** `DriverIdentity` gives each entrant a braking fraction (0.65–0.75), a risk level (0–10, which sets passing clearance and preference padding), consistency and race-to-race form.

## What is good

- **Physically honest.** Candidate paths are forecast with the same car model the car drives with, and the safety layers are real, measured and deterministic. Races are reproducible to the millisecond, which is rare and valuable.
- **The harness.** Pace studies, per-stage audits and field-mean sector times make experiments cheap to judge.
- **Personality hooks** (risk, braking, consistency, form) already exist per driver.

## Where a racing-AI lead would push

Ordered by expected pace and feel impact.

### 1. No commitment: the plan flips sides every couple of seconds

Measured on Cedar: club cars change their planned lateral target by more than 1.5 m **33 times a minute**, and **24 of those reverse direction within 3 s**. Street cars: 25 and 18 per minute. Each plan is chosen afresh from a snapshot, with no memory of what the car was doing. A pass that is 60% complete competes on equal terms with "go back behind".

Commercial AIs treat an overtake as a **state that persists**:

- follow
- set up (pick a side, close up, use the tow)
- commit (pull alongside)
- complete (clear by a car length, then rejoin the line)
- abort (a clear reason: door closed, gap gone, braking point reached without overlap)

Once committed, a car only leaves the state for an explicit abort reason, not because another option ranked 0.05 s better on the latest snapshot. Experiments 112 and 116 both failed in exactly this way: cars closed faster, the pass was withdrawn at the next plan, and the time went into late braking.

**Recommendation:** add intent memory. Keep the current candidate generation and physical checks, but put a small state machine above them. The chosen side and target car persist for a minimum time or until a named abort fires, with a hysteresis bonus for the committed candidate in ranking. Measure reversals/min, pass attempts, completions and aborts alongside pace. This is the highest-value next experiment.

### 2. Overtakes are only lateral offsets; racing overtakes happen in braking zones

The pass candidates are "move to the other side of the car ahead" with the same speed envelope. Real overtakes (and good AI ones) mostly happen by:

- **Out-braking on the inside.** Be alongside or overlapping at the braking point on the inside of the next corner; brake later and take a tighter line. The victim has to concede.
- **Slipstream on a straight.** Tuck in, use the tow (`DraftingModel` exists), pull out late with a speed advantage.
- **Better exit.** Back off slightly before a corner, get a better exit, and pass on the following straight.

None of these can be expressed as "choose an end offset". The ranker has no notion of *which side the next corner is on*, *where the braking point is*, or *who owns the corner*.

**Recommendation:** once commitment exists, have the set-up state pick the side from the next corner's direction (inside of the upcoming braking zone). Let the committed attacker brake later (a braking-zone overtaking delta scaled by risk) only while it has an overlap on the inside.

### 3. Driver skill is expressed as safety margin

The solo speed profile corners at **78% of available grip** and brakes at **85% × (braking fraction / 0.75)**. The live path envelope brakes at the driver's braking fraction (**65–75%**) of capability, and the steering target caps at 85% of grip. The result is 64.98 s solo on Cedar vs Richard's 62.60 s: 3.8% off before any traffic. There's no single "pace" knob; skill, caution and safety reserve are all the same numbers.

Sim-racing AIs normally build the limit line near the true limit (~95–98%) and express skill as:

- a **pace scale** (small speed/brake-point offset)
- **consistency noise** (lap-to-lap and corner-to-corner variation)
- **racecraft** (how aggressive in wheel-to-wheel)

Safety reserves live in the traffic logic, not in the solo line.

**Recommendation:** separate three things: (a) the physical limit line, (b) per-driver pace and consistency (a skill rating, which also gives the difficulty slider for players), (c) traffic safety margins. Then the fastest AI can match a good human solo, and difficulty is one number.

### 4. Side-by-side racing is handled as a hard corridor, not racing rules

When a car is alongside, the reachable corridor shrinks and lateral moves toward it are rejected ("corridor", "moving side entry"). That is safe, but it means cars alongside each other can't negotiate: nobody concedes, nobody claims the corner. 116's failure showed repeated corridor rejections.

Racing AIs use simple, legible conventions:

- the car with overlap at turn-in on the inside owns the apex
- the outside car leaves a car width
- a car that is alongside holds its lane rather than drifting across
- a defender makes at most one move and not in the braking zone

**Recommendation:** after 1–2, add an explicit "alongside" state with these rules. It also gives natural-looking racing that players read as fair.

### 5. The decision is spread over too many layers to reason about

Passing and speed are decided across the preference map, dynamic planner ranking, the physical check, four fallback layers, the moving-leader cap, the static cap, the conditional hold and the immediate guard. Each is sensible alone, but when something goes wrong it is hard to say which layer caused it. The stage audit shows most never bind (static scenery 0%, guard 2%).

**Recommendation:** after the intent state machine exists, one decision per plan should produce *(intent, target car, side, reason)* → one trajectory → one speed arbitration with a logged binding reason. Fold the fallbacks into intent transitions (e.g. "no safe path" = abort to follow/hold). This is step 3/5 of the handoff, done around intents instead of as a pure refactor.

### 6. Race format: fast cars start at the back

18 of 24 cars are street class (25 m/s ceiling) and the six club cars start mostly behind them (car 0 from slot 23). Every race therefore opens with six cars that must pass most of the field on a tight straight. That's a legitimate game scenario, but it makes passing the only lever that matters and hides progress elsewhere. Parked in the handoff: a mixed grid with the fastest three at the front for non-endurance races.

## Suggested order

1. **Pass commitment / intent memory** (above the existing planner). Metrics: reversals/min, attempts/completions/aborts, collective and club-car time, gate 1/2 times. Screen on Cedar + Bayview 1709.
2. **Corner-aware set-up:** pick the inside of the next braking zone, and let a committed attacker with overlap brake later (risk-scaled).
3. **Separate the pace skill from safety margins;** add a skill rating (also the player difficulty slider).
4. **Alongside rules** (overlap owns the apex, leave a car width, one defensive move).
5. **Consolidate** fallbacks and speed caps around the intents, with one logged reason.

Add to the pace-study summary: plan reversals per minute, pass attempts/completions, and positions gained by club cars. Pace alone can't tell a better racer from a luckier one.
