# 71 · Racing lines and fast cars: architecture review

Richard, 8 October:
- "Plan for restart next time is to sort out the AI so that they don't crash on this course. Barriers don't solve the problem they hide it."
- "I'm thinking the racing line logic may need to change for faster tiers if that's not already happening. We should review the overall architecture for how the racelines are generated to ensure that it makes sense for fast cars. It may be worth having logic whereby a racing line is generated for each car based off of their performance as the race loads?"

## 1 · How it works today

Racing AI v2 (doc 46) works in three layers:

| Layer | What it does | Knows the car? |
|---|---|---|
| **Line** (`RacingLine`) | One line per track, for every car. Bayview and Cedar have offline-authored lines. Hillcrest, Quarry Hills and reversed tracks get `BakeSmooth` at load: the smoothest (least bending) path, with offsets capped at **±4 m** on a 16 m track. A hard **±5 m** limit applies to every line. | **No.** A stock C car and an E prototype take the same path. |
| **Speed plan** (`RacerDriverPolicy.LimitSpeed`) | Each tick, it looks ahead along the planned path. A backward braking pass sets the fastest safe speed at each point from the car's grip, aero, brakes and drag, plus the grade (crest unloading, uphill/downhill). | **Yes.** Grip, brakes, power and aero, and the driver's skill (corner use, brake use). |
| **Control** | Steering is pure pursuit at a point 6–19 m ahead on the planned path, with no feed-forward of the path's own curve. The pedal follows the speed plan. Lane changes (overtaking, defending) re-plan the path from where the car is. | Partly: steering rate and yaw. |

## 2 · What the race data shows (Valley Tuners, barriers off)

The new off-road recorder (`RACER_EXCURSION`) logs every AI car's last 3 seconds as it leaves the asphalt. It was run with `--no-corner-barriers`, so the armco can't hide anything.

| Field | Off-road excursions in 3 races | Where |
|---|---|---|
| Normal B field (skill 4–5) | 2 | the 850 m hairpin |
| Every driver at full ability (to provoke it) | 51 | the three tightest hairpins: ~160 m (radius 25 m), ~860 m (24 m), ~1,075 m (17 m) |

Cars leave the road at the hairpins, not at the 240 m and 930 m corners where they used to be found stuck.

**A typical departure** (car 4, the 850 m hairpin, full ability):
1. **45 m/s on the straight.** The plan starts braking late and gently, assuming about **21 m/s²** of deceleration is there.
2. **It's defending.** On the brakes, it swerves 4.9 m across to cover the inside. Steering goes from 0.14 to full lock in 0.3 s.
3. **The physics share one grip budget** (`DriveModel.Forces`): the yaw takes up to 95% of grip, and braking gets what's left. At full lock that's **31%**. The car manages only about **15 m/s²**.
4. **It reaches the apex 5 m/s over its plan** (32.9 m/s for a 27.7 m/s corner). At full lock it understeers from 1.4 m left of centre to 8.5 m right, off the road.
5. **Throttle while running wide.** The plan re-centres on wherever the car is, sees the corner's exit opening, and asks for **full throttle** at full lock while the car is running wide. A real driver lifts to get the front to grip.

## 3 · What that means

- **The line isn't what crashes them.** Fast cars crash because the **control layer spends grip the speed plan never budgeted for**: lane changes, defending and steering corrections on the brakes. Then it adds throttle while understeering.
- Every class has this. Faster cars (higher speed and higher skill: later braking, 97% brake use, no margin) simply have nothing left when it happens. That's why the old DR 17–20 field crashed, and why the slower skill 4–5 field hardly ever does.
- **The line is still too narrow for fast cars.** Capped at ±4 m, it makes corners tighter than the track (the 17 m hairpin could be far wider), and it's the same for every car. A fast car's best line is different from a slow car's: wider entries, later apexes, and straighter exits where it has the power to use them, plus speed-dependent grip with aero (S and E).

## 4 · The plan

**Phase 1 — control that respects the grip budget** (the cause of the crashes; behind a flag, keep/reject protocol):
1. **One grip budget for steering and braking.** While braking, lane changes are limited to the grip the brakes leave, and steering asks for no more than the path needs plus a bounded correction. No defending or attacking move that needs more grip than is left.
2. **Lift when understeering.** At or near full lock, with the car running wider than its path, no throttle; brake gently if over the corner's speed.
3. **Brake to what the car really achieves.** The plan's deceleration comes from the same friction circle the physics use, for the curvature the car will actually be steering.

**Phase 2 — a line for each car, made at race load** (Richard's idea):
1. **Use the track.** The line's corridor is the track width less the car's half-width and a safety margin (about ±6.5 m on a 16 m track instead of ±4). The ±5 m hard limit becomes per-track.
2. **A line per car performance.** At race load, every distinct car (grip, power, braking, aero; grouped so near-identical cars share one) gets a **minimum-time** line. It starts from the smoothest line, then a quick optimiser moves the offsets to cut that car's own lap time, using its own `RacingSpeedProfile`. A grippy, low-power C car gets a near-geometric line; a powerful B car gets later apexes; an aero E car gets higher-speed sweeps.
3. **Cost:** the smooth bake is a few tens of milliseconds per track, and an optimiser over about 200 control points is about 0.1–0.5 s per car group. With about 4–8 groups a race, that's under 3 s at load, cached per track and car.
4. **The checks:**
   - every line stays inside its corridor;
   - each car's own line laps faster for it than the shared one (by `RacingSpeedProfile`);
   - 24/24 on Cedar and Bayview, seeds 1709–1711, plus Hillcrest with the barriers off.

**Measuring success:** off-road excursions at Hillcrest with the barriers off. Do it for the normal B field and for a full-ability field (the stress test), and aim for no departures in either.

## 5 · Phase 1 results (8 October)

`--grip-budget` does three things:
- lane changes are paced to the grip that braking and the corner leave (`LaneChangeDistance`);
- the car lifts at full lock above 12 m/s, and brakes if it's over its plan;
- every car's braking plan keeps the trail-braking reserve that aero cars already had (the corner's sideways load counted 1.3 times).

**Hillcrest stress test** (Valley Tuners, barriers off, every driver at skill 10 = ability 0.895, the top of class E; same build, 3 seeds each):

| | Normal control | Grip budget |
|---|---|---|
| Finished | 67/72 | **72/72** |
| Assisted resets | 7 | **0** |
| Cars needing a physical recovery | 13 | **0** |
| Off-road excursions | 24 | **2** |
| Winner | 94.4–96.9 s | 97.3–99.7 s (about 2.5% slower: the margin on the brakes) |

An earlier version without the reserve took ability 1.0 drivers from 50 excursions to 28. It didn't help at skill 10 (15 → 23), because the remaining failure there was the braking spiral, not lane changes. The reserve fixed that.

**The keep/reject protocol** (study 222: Cedar and Bayview, seeds 1709–1711, the normal roster):

| | Off | `--grip-budget` |
|---|---|---|
| Finished | 144/144 | 144/144 |
| Assisted resets | 0 | 0 |
| Total finish time | 9,612.6 s | 9,619.0 s (+6.4 s, +0.07%) |
| Contacts | 57 | 34 (−40%) |
| Hard contacts | 2 | 0 |

By the letter of the rule (total time must improve) it's a **reject by 1 s a race**: the same pace within noise, but not faster. It is the fix for the crashes, though, and it cuts contacts by 40%. Keeping it is Richard's call.

**Decision (Richard, 8 October):** "Lets do both". The grip budget is the default (`--no-grip-budget` to race without it), and per-car lines are next.

## 6 · Loading times (Richard: "Cache the circuit after the first load per tier… or be more aggressive with removing the world beyond x metres / behind mountains?")

Measured from the race logs (`LITTLE_ROAD_LOAD_STEP`):

| When | Time | Where it goes |
|---|---|---|
| Starting the game | ~16 s | Building the world: 13.5 s |
| **Entering a race** | **~11.7 s** | **The whole world built again from scratch**, though it's identical to the free-roam world |

The race-entry rebuild, step by step (Valley Tuners):

| Step | Time |
|---|---|
| Mainland terrain | 4.2 s |
| Valley height field (3.2 million cells) | 1.9 s |
| Valley roads and water | 1.7 s |
| Trees | 1.4 s |
| Hillcrest town | 1.3 s |
| The rest | 1.2 s |

**What to do, best first:**
1. **Don't rebuild for a race.** Keep the free-roam world and switch it into race mode, so race entry goes from ~12 s to about 1 s. This is the biggest win and the biggest change: the race scene is built separately today.
2. **Cache the generated world.** It's identical every time for the same build and flags, so generate it once and save it: the height field is about 13 MB and the meshes perhaps 100–200 MB. That speeds up game start and race entry alike. The cache key is the build version plus the flags that change the world.
3. **Caching racing lines** is worth doing and costs almost nothing: KB per track per car group. Per-car lines are cached on disk already. But lines were never the wait.
4. **More aggressive culling** (distance, behind mountains) helps frame rate rather than loading. Races already cull outside a radius (764 m at Valley Tuners), which is why Quarry Hills' town costs 0 s at a Hillcrest race.

## 7 · Phase 2: a line for each car (8 October)

`CarLines` is the default since study 223 (`--no-car-lines` races the shared line):
- **Grouping:** at race load, the field is grouped by performance: grip (to 1), brakes (to 3), top speed (to 4 m/s), power at 25 m/s (to 1) and aero.
- **The bake:** each group gets a minimum-time line. It starts from the track's shared line, and coordinate descent moves ~8 m control points within ±5.4 m (steps of 1, 0.5, 0.25 and 0.1 m), keeping each move that cuts the car's own point-mass lap. The lap uses the friction circle, aero, power and drag, plus a small smoothness term. It's then sampled as the usual periodic B-spline.
- **Corridor:** every line is limited to ±5.6 m (`RacingLine.MaxOffset`, the AI's usable edge), up from ±5.
- **Caching:** groups are baked in parallel before the cars spawn and cached on disk (`persistentDataPath/racing-lines/`, a few KB each), so a group is only ever baked once.
- **Check:** `CarLinesChecks` runs on Hillcrest Raceway's real geometry:
  - a stock C car's own line laps 75.97 → 75.31 s for it;
  - the free Tuner's laps 45.13 → 44.56 s;
  - the two lines differ by up to 2.7 m;
  - each bakes in about 1.3–1.7 s.

**Study 223** (Cedar and Bayview, seeds 1709–1711, the grip budget on in both arms):

| | Shared line | Per-car lines |
|---|---|---|
| Finished | 144/144 | 144/144 |
| Assisted resets | 0 | 0 |
| Total finish time | 9,619.0 s | **9,585.8 s (−33.2 s)** |
| Bayview total | 2,767.3 s | 2,737.5 s (−1.1%) |
| Bayview contacts | 12 | 3 |

**Kept.** At Hillcrest, at skill 10 with no barriers, it's level on safety: 3 excursions and 1 assisted reset, against 5 and 2. Pace is within 0.5%. Most of the Hillcrest gain had already come from the grip budget.

**Load cost:**
- Cedar and Bayview: 2 groups, about 0.3 s (Bayview) to 2 s (Cedar) the first time, 9 ms cached.
- Hillcrest's mixed class B field: 9–10 groups, about 2 s the first time. A field with one new group still costs about 1.4 s, the time of a single bake. Speeding up the bake itself (incremental lap evaluation) is a follow-up.
