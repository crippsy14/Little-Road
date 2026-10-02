# 54 · The Duke's Challenge (2 October 2026)

Richard (2 October): "Why do we fully scale out all parts for all tiers with the maximum parts then being defined, and then have a challenge against him where you race him around the local track, where he has the maximum parts in the game. It would be a great way of showing how fast cars can get, and a great full circle moment at the end of the game... like fighting Red in Pokémon Blue." Then: "a 3 lap race in the typical format, but it's just a 1 vs 1", and, for the prize, "Love love love that!"

On branch `claude-additions`, not committed.

## The event

- **Where and how.** Talk to Duke Rolo at the castle and choose *The Duke's Challenge*. It is three laps of Hillcrest Raceway, you against him, with the Duke on pole.
- **The challenge card.** It shows both cars' ratings and ideal laps, plus a verdict in the Duke's voice (from "I could stop for tea halfway round" to "On paper we're level").
- **His car and driver.** The Regale (doc 53) on the **maximum build**. His driver style is "Duke": line 1.0, braking 1.0, control 0.95, consistency 1.0 (the best in the game).
- **Prize.** The first win pays **50,000 credits** and gives you **the Regale**.
  - A loss costs and gives nothing.
  - Rematches count as wins, for honour only.
  - A save can hold the Regale only with the `duke_beaten` trophy; a forged save is refused.
- **Afterwards** you return to the castle courtyard, facing the Duke.
- **Code:**
  - `RaceRoster.DukeChallenge`
  - `AdventureStore.FinishDukeRace`
  - `RaceTrial.SettleDuke`
  - `AdventureGame.DukeChallenge`
  - `DukeChallengeChecks`
- **Review:** `tools/duke-race-review.ps1 -Name dN` races it on the watch-cars save with your car on autopilot.

## The maximum build

`EquipmentCatalog.MaxBuild()` picks the parts by coordinate descent on the SpeedProfile ideal lap of Hillcrest Raceway. It starts from the dearest E part in each slot and tries every E and S part one slot at a time until nothing changes. The best part depends on the track, so the build is measured rather than picked by price.

| Slot | Part |
|---|---|
| Engine | `engine_e_12h` |
| Turbo | `turbo_e_twin` |
| Tyres | `tyres_e_qualifier` |
| Transmission | `transmission_e_long` |
| Brakes | `brakes_e_12h` |
| Steering | `steering_e_night` (steering does not change the ideal lap) |
| Chassis | `chassis_e_lowdrag` |

`--trial-setup max` uses it in any trial.

## Physics at E speeds: downforce and aero grip

The first Duke races failed: in the AI-only study (`review/duke-race/solo-max-4`) the maximum build left the road at 75 m/s and fell off the elevated raceway three times. It recorded 25 s of unsupported time, and its best lap was 36.3 s.

**1. Downforce push.** The S and E platforms carry downforce in `parts.json` (S .0025, E .003). It is applied as an extra push into the road of downforce × v², only near the road (within 1.5 m). This keeps the car planted over the crests.

**2. Aero grip** (`DriveModel.AeroGrip`).
- Downforce loads the tyres, so grip = static grip × (1 + 0.5 × downforce × v² / g).
- The 0.5 is tyre load sensitivity: twice the load is not twice the grip.
- For the maximum build that is ×1.06 at 20 m/s and ×1.86 at 75 m/s.
- Its 58 m/s² E brakes now brake at their rating at speed, instead of at the tyres' static 38 m/s².
- **Model consistency.** The physics (`RoadCar`), the AI's speed plan (`LimitSpeed`, `CornerSpeed` solved in closed form with the v² term) and the ideal lap (`SpeedProfile`) all use the same formula.
- **Scope.** Only cars with downforce get it, so classes C to A are unchanged; this is a check.
- **A/B.** `--no-aero-grip` turns it off, and `--no-downforce` turns off both.

**3. Driver plan for aero cars** (`RacerDriverPolicy`, `--aero-reserve`, default 0.3).
- **The failure.** With aero grip, seed 1742 (the seed the second real Duke race used) still ran off at the hairpin at about 1060 m.
- **The mechanism.** A car a few m/s over its plan into a braking zone that turns asks for more yaw than the plan assumed. The yaw takes up to 95% of grip, which leaves the brakes about 30%, so the overspeed snowballs.
- **The fix.** Aero cars' braking plans count the lateral load 1.3×, keeping a reserve for trail braking. Straight-line braking is unchanged.
- **Scope.** Applied only when the car has downforce, so class C keep/reject races are bit-for-bit unaffected.

### Results (all logged under `review/duke-race/`)

| Run | Setup | Total (3 laps) | Best lap | Recoveries |
|---|---|---|---|---|
| solo-max-4 / solo-aero-noaero | field driver, push only | 139.4 s | 36.33 s | 3 (below world) |
| solo-aero-1 | field driver, aero grip | 97.95 s | 31.11 s | 0 |
| solo-duke-s1742 | Duke, aero grip, no reserve, seed 1742 | 159.8 s | 30.71 s | 1 |
| r0.3-s1742 | Duke, reserve 0.3, seed 1742 | 97.5 s | 30.96 s | 0 |
| r0.3, 8 more seeds (1709, 1710, 1720, 1731, 1753, 1764, 1775, 1786) | Duke, reserve 0.3 | 97.5–97.9 s | 30.97–31.12 s | 0 |
| r0.5 (1709, 1710, 1742) | Duke, reserve 0.5 | 98.9–99.1 s | 31.41–31.48 s | 0 |

**Full Duke races** (watch-cars save, your car on autopilot):

| Review | What changed | Result |
|---|---|---|
| d1 | before the fixes | The Duke fell off the raceway twice, and the player won. |
| d2 | aero grip | The Duke won in 178 s after two run-offs at the hairpin. |
| d3 | reserve 0.3 | The Duke won in **97.1 s**, clean; your stock-ish car was 123 s behind. |
| d4-equal | `--max-build-test`: your car also on the maximum build | The Duke won in 96.4 s. Your autopilot (a "mixed" field driver) took 172 s with two recoveries. |

**Known issue for Phase 5 (E-class fields).** Weaker AI drivers in E cars still crash, as d4-equal shows. The Duke is fine, and there are no E-class AI fields yet. Before E races exist, the AI needs a keep/reject study at E speeds, probably a larger reserve for lower braking skill. The same planning gap is likely there in class C too, but C cars have enough slack never to reach it. Testing the reserve for every car is a candidate keep/reject study (Cedar + Bayview, seeds 1709–1711).

## Test drives (Richard: "a version where I can control the maxed out car")

`tools/publish-play.ps1` now creates two extra desktop shortcuts next to "Little Road":

- **Little Road - Max Build Hot Laps.**
  - What it is: five laps of Hillcrest Raceway alone in the Regale on the maximum build, using the race HUD.
  - The HUD shows speed, this lap, last lap and best lap.
  - Enter or Options starts the countdown; Esc exits.
  - Flags: `--trial-race --trial-track hillcrest --trial-human --trial-cars 1 --trial-setup max --trial-body regale`.
- **Little Road - Max Build Test Drive** (`--max-build-test`).
  - What it is: free roam on a copy of your live save, taken fresh at each launch (`little-road-max-build-test.json`), with the maximum build bought and fitted.
  - You start in the castle courtyard facing the Duke, so you can race him on equal cars straight away.
  - Saving to slots from the pause menu is off; nothing reaches your real saves.

`--trial-body <id>` (new) sets every trial car's body; `--trial-setup max` is the maximum build.

## Peak and floor (Richard: "is the performance as good as it should be, or do we have room to increase the peak... and even reduce the floor of the stock")

The same driver (the Duke's abilities) on Hillcrest Raceway, seed 1709, three laps each (`review/duke-race/ladder-*`, `r0.3-s1709`):

| Kit | Best lap | Top speed | Grip | Brakes |
|---|---|---|---|---|
| Stock | 76.5 s | 24.9 m/s (56 mph) | 7.0 | 14 |
| Street | 68.3 s | 27.8 m/s (62 mph) | 8.0 | 19 |
| Club | 61.7 s | 31.3 m/s (70 mph) | 9.0 | 27 |
| Valley (best class C kit) | 57.1 s | 34.7 m/s (78 mph) | 10.2 | 32 |
| **Maximum build** | **30.7 s** | **75.6 m/s (169 mph)** | 23 (× up to 1.9 aero) | 58 |

The ideal-lap model (SpeedProfile, the centreline at 64% of grip) gives stock 87.0 s, valley 66.4 s and maximum build 38.3 s. It is a rating figure: real drivers on the racing line beat it.

**Readings:**
- **The peak is gear-limited.** The maximum build reaches 75.6 m/s against its 76 m/s gearing ceiling on the long straight. Raising the peak means a longer E box or more top-speed parts. The track would then be the limit: corners already take up most of the lap, which is Richard's "Le Mans prototype on Tsukuba" point.
- **Classes B, A and S have no parts in the shops yet** (Phases 2–4), so the jump from valley (57 s) to maximum (31 s) is currently a single leap.
- **The floor could drop.** Stock is 25 m/s with 7 grip. A slower stock car (less power or grip) would make early upgrades feel bigger. It would also change every class C race and the parts ladder (doc 44), so it should be a separate keep/reject question.

## Richard's first drive (2 October, afternoon)

Richard on the hot laps: "That was siiiiiick! The speed felt good". **His third lap was 28.02 s**, about 2.7 s quicker than the AI Duke's 30.7 s. That supports skill beating the best AI by a decent margin, and it's a good baseline for now. His notes and what changed:

**1. "Up the max speed to be similar to the actual cars... especially as they will be racing bigger tracks."**
- **Platform drag is now applied.** `parts.json` always gave each class platform a drag factor (B .95, A .9, S .8, E .75), but the physics never applied it, so every car had street-car drag.
- **Where it applies:** `CarTuning.dragSave` (from `EquipmentCatalog.DragSave`) scales the air drag in the physics (`DriveModel.Forces`), the gearbox's top speeds, the AI's plans (`RacerDriverPolicy`, `RacingLine` and the other policies) and the ideal lap.
- **Class C is unchanged:** its factor is exactly 1, which is a check.
- **Data:** E platform 0.6, S 0.7 (low-drag bodywork).
- **E gearboxes made taller:** 25.9 → 33.0 km/h per 1000 rpm, and the long box 28.5 → 36.0. Every E car was sitting on its limiter at 250–275 km/h.
- **A/B:** `--no-platform-drag`.

| | Before | Now |
|---|---|---|
| Top speed, long box (the bigger-track build) | 76 m/s (274 km/h) | 92.2 m/s (332 km/h) |
| Raceway build (the optimiser now picks the twelve-hour box) | — | 87.9 m/s (316 km/h); the Duke touches 87.4 m/s on the straight |
| Duke best lap (6 seeds, all clean) | about 30.7 s | 30.1–30.4 s |
| Full race d5 | — | 94.3 s, clean |

**2. "The front of the car lifts when speeding up... feel glued to the track."**
- The body's lean (`BodyMotion`, visual only) was the same for every car: 0.42° per m/s², up to 4°. The maximum build's 12 m/s² put its nose up the full 4°.
- **Fix:** platforms now carry a suspension `firm`ness (C 0, B .25, A .65, S 2.3, E 3), and the body leans 1/(1+firm) as much. E cars lean 25% as much (1° at full power).
- Downforce cars also settle up to 3.5 cm lower as speed builds.

**3. "The turbo noise is doing that really high pitch noise... remove the turbo noises all together."**
- **Off:** the turbo whistle, whoosh, blow-off, flutter and anti-lag pops.
- **Also off:** the hybrid motor's whine. On the maximum build's twelve-hour hybrid it climbed with road speed to about 4 kHz (with a 12 kHz harmonic) at full throttle. It was probably the high tone heard: the turbo layer measures almost silent at steady revs.
- **Kept as nice-to-haves:** `--turbo-sound`, `--hybrid-sound`.
- **Check:** `SoundChecks` confirms both are silent by default.

**Check count:** it went from 2826 to 2803 because the taller E boxes no longer hit their limiter, so the "ends on the top-gear limiter" drivetrain check applies to fewer builds. Smoke23 passed.
