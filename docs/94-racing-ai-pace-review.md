# 94 · Racing AI pace review: where the time goes, and racecraft v3

22 October 2026. Richard: "review the racing AI and look for improvements on how to make them drive faster whilst also not
crashing ... the racing should feel fast and skilled so any gains are a good thing, the bigger the better."

This doc records the review of Racing AI v2 (doc 46; the grip budget and per-car lines of doc 71), the measurements that
show where a race's time is lost, the change built from them (`--racecraft-v3`), and the keep/reject studies.

## 1 · What was measured first (study 256, the build of 22 Oct, flags as raced)

All races: 24 club/street cars, one lap, the normal roster, seeds 1709–1711, Cedar and Bayview. Solo laps are a standing
start from the back of the grid in the club car.

| | Cedar | Bayview |
|---|---|---|
| Solo lap, every ability 1 | 80.2 s | 48.4 s |
| Solo lap, every ability 0.5 (a class C driver) | 86.7 s | 55.2 s |
| 24-car race, winner (mean of 3 seeds) | 87.1 s | 51.2 s |
| 24-car race, mean car | 95.1 s | 59.5 s |
| 24-car race, every driver at ability 1: total | −2.2% | −1.7% |
| `--racer-corner-scale 1.04` (4% more corner grip use): total | −1.0% | +1.0%, one bad race (4 hard contacts) |
| `--no-traffic-corner-margin` | identical | identical (no roster driver's Line reaches the cap) |

So the driving core is close to its ceiling: perfect drivers gain 2%, more corner grip is pace-neutral and risky, and the
pack corner cap never binds for the roster. The **race** costs far more than the **driving**:

- A car in a 24-car field finishes about 10% behind what the same car does alone, and even the winner is 1–7 s off a solo lap.
- Traffic is the binding speed reason 58–62% of the race, at a mean 6–8 m/s below the car's own plan (86% and 12 m/s in
  the first 15 s; still 42% and 2.5 m/s in the last 15 s on Cedar).
- The finishing order is the grid order: Cedar 1709 finished 7, 0, 1, 3, 2, 4, 9, 6, 11, 5, 8, 12, … by grid slot. A club
  car (10% faster than the street cars around it) starting 23rd passed one car in nine attempts and finished 20th.
- 120–190 attacks a race, 5–11 completed passes. Aborts: ~50% timeout (9 s beside or behind the target), ~40% "side
  closed" within 0.5 s (the target's line reaching the apex closes the inside).
- The planned lane moved 3–6 m nearly every second (380–670 reversals a race in the telemetry), so no lane change was ever
  completed before the target changed. Cars sat in the Attack state 54–58% of the race.
- Contacts were already low: 4–14 soft and 0–2 hard a race.

The skill mapping (doc 46: class C drivers at abilities ~0.5 corner at 75% of the limit) is a design choice and was left
alone: it shows, but it is not where the field loses its time.

## 2 · Racecraft v3 (`--racecraft-v3`, off by default)

Three parts in `RacerDriverPolicy`, each droppable for diagnosis with `--racecraft-v3-without <commit|attack|gap>`:

1. **Lane commitment (commit).** The heat line still proposes the best lane every plan tick, but a change of 1.5 m or more
   from the lane held at the last accepted change is only taken when the lane is blocked (a body in it, or boxed in), the
   new lane is far better (4 heat units: a slow car's hill in ours), or 1.2 s have passed and the gain is at least 1 heat
   unit (`AcceptLaneChange`). The held lane is kept exactly, as a shift from the racing line, so a car follows the line in
   its lane rather than re-rounding to a bin. Small adjustments are free, but the anchor stops a lane being crossed in
   small steps.
2. **Attacks own their lane (attack).** While attacking, the lane is the one beside the target (its predicted position plus
   the car widths and clearance), taken directly rather than rolled for on the heat line, as long as it lies in the car's
   wall-free range. The door closing (room beside the target below a car's width) is given 1.5 s to reopen instead of
   0.5 s, never ends an attack while the cars overlap (the target must leave a car's width), and when it does end one the
   cool-down is 1 s rather than Patience's 1.2–5.2 s: the target's move is not the attacker's failure.
3. **Gap acceptance (gap).** The car being attacked is followed on 60% of the reaction time and half the standstill gap
   (Game AI Pro ch. 38: smaller gaps while overtaking), and not followed at all once we are beside it, within 0.2 m of
   clearing its width; the close guard still brakes for bodies.

Telemetry added to `race.json` and `tools/summarize-racer.py`: `trafficLossSeconds` (seconds lost behind traffic: the
deficit to the car's own plan over its speed, capped), `attackSeconds`, `laneChanges`.

## 3 · Round one (study 257: the three parts as first built)

Same build, flag off against `--racecraft-v3`, Cedar and Bayview, seeds 1709–1711; and the same pair with every driver at
ability 1 (`--racer-abilities all-1`), where the top of the skill scale and the pack corner cap show. All 48 races 24/24, no
assisted resets.

| Roster field, 3 seeds | Cedar off | Cedar v3 | Bayview off | Bayview v3 |
|---|---|---|---|---|
| Total finish time | 6,848.3 s | 6,784.7 s (−0.9%) | 4,284.9 s | 4,322.8 s (+0.9%) |
| Winner (mean) | 87.1 s | 84.5 s | 51.2 s | 48.8 s |
| Last car (mean) | 100.6 s | 100.0 s | 66.8 s | 67.9 s |
| Completed passes | 23 | 46 | 21 | 42 |
| Lane changes / reversals | 2,206 / 1,145 | 1,576 / 987 | 2,234 / 1,350 | 1,297 / 787 |
| Contacts, soft / hard | 22 / 0 | 31 / 0 | 32 / 2 | 33 / 2 |
| Time in the Attack state | 54% | 57% | 57% | 60% |

| Every driver at ability 1 | Cedar | Bayview |
|---|---|---|
| Total finish time | 6,695.5 → 6,617.5 s (−1.2%) | 4,212.3 → 4,173.2 s (−0.9%) |
| Completed passes | 25 → 67 | 25 → 44 |
| Contacts, soft / hard | 31 / 1 → 40 / 1 | 32 / 0 → 53 / 7 |

What it showed:

- Passes doubled and lane changes fell by a third, and the winners were 2–4 s quicker, but the field spread: the
  back-markers lost what the leaders gained, so the totals barely moved and Bayview's got worse.
- The attack lane bypasses the lane commitment, and cars were still attacking 55–60% of the time, so the lane still jumped:
  every car in a train has a plan faster than the train, so "my plan beats its speed" was true for everyone, and each
  abort's 1 s cool-down was followed by a new attack on a new side.
- Attacks into a lane already occupied by a car diagonally ahead ended as side-by-side stalemates that slowed both cars and
  blocked the lane behind; everyone diving for the inside of Bayview's bridge S (progress 300–450, where the line sweeps from
  the left edge to the right in 60 m) stopped the pack there: 30–100 car-seconds under 8 m/s a race in every arm, car-to-car
  contacts (no barrier), and the full-ability field's 7 hard contacts.

Narrow roads (Bramblewick Lanes, 8–9 m, the normal roster, study 258 on the round-one build): all 24/24, no resets, totals
10,363.3 → 10,351.8 s (−0.1%), passes 25 → 62, contacts 20 → 21, hard 1 → 0. The commitment is safe where there is no room.

Round two therefore adds two judgements and softens the gap: an attack needs the lane beside the target clear of other cars
from our tail to 12 m past it (`LaneClear`), and the plan-based speed advantage only counts against a *free* car (no car within
15 m ahead of it, within 3.5 m across); a car in a train is attacked on the real closing speed alone. The target of an attack is
followed on 75% of the reaction time and 70% of the gap (was 60% and half), and the follow is dropped only once its width is
fully cleared.

## 4 · Round two (study 259)

Same protocol and build family (the round-two code, 22 Oct 22:30). All 48 races 24/24, no assisted resets.

| Roster field, 3 seeds | Cedar off | Cedar v3 | Bayview off | Bayview v3 |
|---|---|---|---|---|
| Total finish time | 6,848.3 s | 6,814.0 s (−0.5%) | 4,284.9 s | 4,230.9 s (−1.3%) |
| Winner (mean) | 87.1 s | 84.9 s | 51.2 s | 48.8 s |
| Last car (mean) | 100.6 s | 99.7 s | 66.8 s | 66.5 s |
| Seconds lost behind traffic, per car | 23.4 | 23.0 | 24.8 | 22.5 |
| Seconds in the Attack state, per car | 50.7 | 31.1 | 33.8 | 24.1 |
| Attacks / completed passes | 563 / 23 | 374 / 48 | 381 / 21 | 284 / 26 |
| Lane changes / reversals | 2,206 / 1,145 | 1,412 / 863 | 2,234 / 1,350 | 1,294 / 767 |
| Contacts, soft / hard | 22 / 0 | 31 / 2 | 32 / 2 | 21 / 1 |

| Every driver at ability 1 | Cedar | Bayview |
|---|---|---|
| Total finish time | 6,695.5 → 6,625.1 s (−1.1%) | 4,212.3 → 4,106.5 s (−2.5%) |
| Completed passes | 25 → 33 | 25 → 31 |
| Contacts, soft / hard | 31 / 1 → 31 / 0 | 32 / 0 → 30 / 1 |

**Verdict:** round two passes the keep/reject letter: every car finishes, no new assisted resets, and the total improves by
88 s over the six roster races (15 s a race, −0.8%; −1.6% with full-ability drivers). Contacts are level (53 → 52 soft,
2 → 3 hard). Attack time is down 40%, lane changes a third, reversals a quarter, and passes doubled on Cedar. It is a modest,
clean gain, not the big one asked for, so the flag stays **off by default** for now (Richard's call), and the work goes on
one track at a time (section 5).

## 5 · Where it stands (22 October, evening) and tomorrow

Richard, late on 22 Oct: "We only need to run on a couple of tracks at a time, to reduce feedback loops ... focus on hillview
and try and speed up that and, when you've confirmed you're happy with progress, try it on the full 20 min sweep." Richard confirmed
Hillcrest Raceway: iterate racecraft v3 on Hillcrest only, then the full sweep.

State:

- **Code:** `--racecraft-v3` (round two) is in the working tree, uncommitted, in `RacerDriverPolicy.cs`, `RaceTrial.cs`
  (telemetry), `RacerChecks.cs` (two checks) and `tools/summarize-racer.py`. Off by default, so nothing a player sees has
  changed. The dev build of 22:30 carries it. `tools/racer-traffic.py` is new.
- **Evidence on disk:** studies 257 (round one) and 259 (round two) under `review/unity-35/`. Studies 253 and 256 (the
  review's own sweep) were pruned by `build-dev.ps1`'s cleanup (it keeps the newest two; they are in the Recycle Bin, and
  their numbers are in sections 1 and 3). Study 258's Vinelands arm was stopped mid-run and its folders removed; the
  round-one Bramblewick arm is in `review/tmp/study258-round1/`.
- **Another session** ("Three towns remodel") shares this working tree and dev build; its Vinelands edits
  (`VinelandsVale.cs`, `CoastalTrial.unity`, docs/92) are uncommitted beside mine. Commit only the files above.

The loop for tomorrow (about five minutes a turn): edit → `compile-check.ps1` (2 s) → `build-dev.ps1` (about a minute when
only scripts changed; eight when assets did) → `run-pace-parallel.ps1 -Study 26N -Tracks @('hillcrest') -Label <name>
-Flags @('--racecraft-v3', ...)` (six races in parallel, about three minutes) → `racer-traffic.py` and `summarize-racer.py`.
First check that a one-lap club race at Hillcrest fits `run-frame-cost.ps1`'s 180 s duration (the Valley Tuners took 95 s;
club cars are slower); lengthen it if the last car needs more. Vinelands trials need `--vinelands` in every arm.

## 6 · The Hillcrest loop (23 October)

One track, three seeds, six races in parallel (about 2.5 min), the flag-off arm re-raced with every build and byte-identical
each time (the v3 code is behind its flag). Every arm is added to study 261 under its own label, so one folder holds the loop.
Richard's second ask (23 Oct): "when a car is faster but behind a car in front, they coast at < 100% throttle for a while.
Instead, have them be more aggressive in looking for overtakes."

`tools/racer-held.py` measures that case from the 1 Hz samples: seconds a car is held (traffic binds its speed, 3 m/s or more
in hand), the share of them with a free lane beside, and the share of those on a straight with no attack under way ("idle").

| Hillcrest, 3 seeds, roster field | off | v3 round two | v3 round three |
|---|---|---|---|
| Total finish time | 6,036.8 s | 5,978.7 s (−1.0%) | 5,998.3 s (−0.6%) |
| Winner / last car (mean) | 72.1 / 89.5 | 70.8 / 89.0 | 71.2 / 89.2 |
| Attacks / passes | 513 / 35 | 362 / 43 | 410 / 42 |
| Contacts, soft / hard | 53 / 4 | 55 / 2 | 66 / 3 |
| Held seconds per car / with a free lane / idle | 36 / 14 / 3.1 | 35 / 14 / 4.3 | 37 / 15 / 3.6 |

Round three (counting the plan surplus for a car held behind one in a train, and half the cool-down after a corner blocks the
pass) was Richard's ask taken literally: more attacks, no more passes, more contacts, 20 s worse. Reverted. The willingness
was never the problem: in two thirds of the free-lane seconds the car was already attacking.

**Why attacks don't convert.** A club car (12% more top speed, 25% more acceleration) traced through study 261 sat alongside
its street target for nine seconds, 5–6 m to its left, throttle capped by Follow the whole time, until the timeout. Nothing was
in its way beside the target; the cap came from a third car 10–15 m ahead *in its own lane*, running level with the target. It
had pulled into a second train beside the first. The clear-lane check only looked 12 m past the target. Timeouts are 40% of
all aborts, the door closing 30%.

**Round four** (23 Oct): a lane open only if the next car in it beyond the target, within 45 m, was pulling away from the
target by at least 1 m/s, and an attack ended at once (1 s cool-down) when the car ahead in our own lane was within 12 m and
no quicker than the target. Raced beside a "bumper to bumper" arm (`--racer-gap-scale 0.6 --racer-reaction-scale 0.7` on top
of v3) to answer Richard's lane question with numbers.

| Hillcrest, 3 seeds | off | v3 round two | round four | round four + closer following |
|---|---|---|---|---|
| Total finish time | 6,036.8 s | 5,978.7 s (−1.0%) | 6,012.3 s (−0.4%) | 5,978.5 s (−1.0%) |
| Attacks / passes | 513 / 35 | 362 / 43 | 337 / 29 | 289 / 20 |
| Aborts: timeout / door closed / blocked | 197 / 177 / – | 167 / 114 / – | 50 / 82 / 125 | 36 / 61 / 136 |
| Seconds attacking, per car | 43 | 35 | 20 | 17 |
| Contacts, soft / hard | 53 / 4 | 55 / 2 | 57 / 1 | 85 / 3 |

The diagnosis held (the timeouts became early "blocked" aborts, one for one) but the cure did not: passes fell and the gain
halved. A car that gives up an attack tucks in behind and coasts, which is the very behaviour Richard dislikes, while a car
running alongside, even going nowhere, kept the pack wider and the total quicker. Closer following everywhere bought the same
−1.0% as round two with 60% more contacts, so it fails "without crashing". **Round four reverted; round two is the version.**
Richard (23 Oct): the nudging idea is dropped.

**Richard's lane question** (23 Oct: does it look for any car-width gap on a straight, and could it take a nearly-enough gap
by running bumper to bumper or nudging through?). What it does: the heat line judges 57 quarter-metre bins at one look-ahead
point 10–35 m up the road; a car's body there is a wall and its soft edge a metre wide, so a gap is taken when it is a car's
width plus the driver's clearance (0.45–0.85 m a side); an attack also needs a car's width and 15 cm beside the target to the
usable edge. Nearly-enough gaps are not tried, and nothing nudges: Follow keeps 0.6–1.3 m at a standstill and the close guard
brakes inside 0.8 s of contact. My view: keep it that way. Contacts knock a box-collider car off its line, and the pile-ups
measured at Bayview's bridge and Hillcrest's hairpin come from cars arriving three abreast; deliberate nudging would read as
crashing, not skill, and the measured problem is not gap width but attacking into lanes that lead nowhere. Closer following
is the testable half of the question, hence the bumper-to-bumper arm.

## 7 · Verdict (23 October): the full sweep, study 262

Round two is the version. The build of 23 Oct carries it; the keep/reject sweep (CLAUDE.md: same build, flag off against on,
Cedar and Bayview, seeds 1709–1711) reproduces study 259 to the tenth of a second, as races are deterministic.

| | Off | `--racecraft-v3` |
|---|---|---|
| Finished | 144 / 144 | 144 / 144 |
| Assisted resets | 0 | 0 |
| Total finish time, six races | 11,133.2 s | **11,044.9 s (−88.3 s, −0.8%)** |
| Cedar / Bayview totals | 6,848.2 / 4,285.0 s | 6,814.0 / 4,230.9 s |
| Winner (mean), Cedar / Bayview | 87.1 / 51.2 s | 84.9 / 48.8 s |
| Completed passes / attacks | 44 / 944 | 74 / 658 |
| Seconds in the Attack state, per car | 42 | 28 |
| Lane changes / reversals | 4,440 / 2,495 | 2,706 / 1,630 |
| Contacts, soft / hard | 54 / 2 | 52 / 3 |

With Hillcrest −1.0% (study 260), Bramblewick −0.1% and safe (study 258), and −1.6% for a full-ability field (study 259), it
**passes the keep/reject rule** on every track raced. **Richard, 23 Oct: "merge it in and make it the default."** Racecraft v3 is
the default from the build of 23 Oct; `--no-racecraft-v3` races without it (the keep/reject arms above are that flag's "off").
Merged into main (0246f2c), full regression rc3 passed (`review/rc3-regression.txt`: the 24-car Bayview race 24/24 at the v3
total; the area audit's shortfalls are the known ones plus one Quarry Hills car not on the ground that predates this work on
the branch), and published to the play copy at 12:50 on 23 Oct. What
a player sees: fewer cars weaving across the road, fewer pull-outs that go nowhere, twice the completed passes and more
side-by-side racing, a field about 1% quicker; solo pace and the skill spread unchanged.

What the two days taught, plainly:

1. The driving core is at its ceiling for the game's physics; corner grip and braking are not where time is.
2. A 24-car standing start costs the field about 10%, and most of that is physical: the first-corner funnel and trains that
   can only dissolve from the front at the rate of real pace differences.
3. Racecraft moves the total by about 1%. Attacking more does not pass more (round three); giving up sooner passes less and
   coasts more (round four); following closer gains pace only by paying in contacts.
4. The total-time metric rewards a wide, dense pack; the eye rewards decisive moves. Round two is where both agree.

Ideas still open, each measured against Hillcrest's total time, passes, grid-order correlation and contacts:

1. **Keep the attack side.** The attack lane bypasses the lane commitment, and each new attack may pick the other side, so
   the lane still jumps between attacks; re-use the last side for a few seconds unless it is closed.
2. **The funnel.** 83–87% of the first 15 s is spent following at a 12 m/s deficit on every track. Measure how much of the
   mean car's loss is the launch queue (compare each car's progress at 15 s with the solo car's) before touching it.
3. **Tighter trains.** Following a car that is accelerating (a corner exit) on the Gipps gap sized for it braking now; an
   anticipation term for the leader's observed acceleration is already there (`seenAccel`) and could count for more.
4. **Bayview's bridge S** (progress 300–450) piles up in every arm (30–100 car-seconds under 8 m/s a race); a rule against
   arriving three abreast where the line crosses the road would help Bayview whatever else changes.
5. **Tuning knobs** (`--lane-dwell`, `--lane-gain`) so the commitment constants can be swept without a rebuild.
