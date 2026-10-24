---
name: activity-design
description: How to come up with, judge and build an activity for Little Road (a side challenge, physics toy, mini-game, secret or "thing to do" in a place, not a race). It covers the bar every activity must clear (gates that reject an idea outright, then a scored assessment), the catalogue of verbs that aren't "drive from A to B", how rewards fit the economy, how an activity is measured and tested, the one-page brief, and the build method. Use it before proposing, adding, changing or judging any activity, and for the activities sweep.
---

# Designing activities for Little Road

Richard's rules (24 Oct, after the first towns' activities felt underbaked and were put on hold on 21 Oct):
**1) Is it fun? 2) Does it fit the world? 3) Is it measurable, and so testable? 4) It is not "drive from A to B" in
some form. 5) Does it contribute to the game: a part, a one-time payment, a model?** Activities up to now were too open
ended, with no clear goal, and it showed. This skill turns those five into a bar with tests, adds the rest of what a good
activity needs, and gives a method.

Races are not activities: use the track-design skill for them. Where places are and what the map plans is in the
world-plan skill. The design records are `docs/design.md` §12 (characters, features and activities) and each place's
section of `docs/world.md` §3, where ideas wait "for the activities sweep".

## 1. The bar

An idea must pass **every gate**. An idea that fails one is rejected or reworked, never "kept for later as it is".
Gate-passers are then **scored**; only the strongest are built.

### Gates (pass or fail)

| # | Gate | The test | Fails when |
|---|---|---|---|
| G1 | **Not A to B** (Richard's 4) | Delete the destination. Is there still a game? The score must come from *what you do*, not *where you end up*. | Timed runs, deliveries, checkpoint routes, races against a train or a resident between two points, chases to a place, "escape" runs, cargo runs scored by arrival. Moving through a small arena *is* allowed when the verb is the point (a slalom, a skid pad, a gymkhana). |
| G2 | **Fits the world** (Richard's 2) | A world of cars: no people, no animals, nothing living but plants (Richard, 20 and 24 Oct). Would a toy car in *this* place plausibly do it? Toy tone: comic, never grim. | It needs a person or an animal; it could be anywhere (nothing of this place in it); it's violent or grim; it breaks the late-afternoon "toy shapes, real light" look (design §11). |
| G3 | **Measurable** (Richard's 3) | One number decides success (a score, a time inside an arena, a count, a distance), logged, and the same inputs give the same result. | Success is a judgement ("looks cool"), or random outcomes decide it, or it can't be logged. |
| G4 | **Testable by Claude** | An automated run can play it (`--activity-test <id>`: a scripted driver, a bot or replayed inputs), it reports `LITTLE_ROAD_ACTIVITY <id> metric=... result=...`, and an editor check guards its layout. | Only a human can tell whether it works. |
| G5 | **Contributes** (Richard's 5) | It gives something the game keeps: a part discovery or gate, a body, a paint, a one-time payment, a keepsake, a recruit contact, a gadget or a route (see §3). | Its only reward is "fun" or a repeatable payout. |
| G6 | **Clear in one sentence** | The card's goal line says what to do and how you win, in under 15 words ("Knock all twelve barrels into the cellar in 60 s"). | It needs a tutorial screen, or the goal can't be stated without "explore" or "try". |
| G7 | **Leaves no trace** | Its props exist only while it runs, or are part of the place in their own right; nothing of it stands in a road, and it never blocks traffic or a race. | Props left in the world (cones, bales and boards in free roam were Richard's snaps of 24 Oct); it uses a road that races or traffic need. |

### Scored (1-5 each; build at 30+ of 40, and never with a 1)

| # | Criterion | 1 | 5 |
|---|---|---|---|
| S1 | **Fun: the verb** (Richard's 1) | Holding the throttle. | The first 10 seconds are already satisfying: you steer, judge and react, and a near miss feels good. |
| S2 | **Fun: the toy test** | Failing is dull or punishing. | Messing about with it is fun even when failing; failure is funny and restarts in 3 s or less. |
| S3 | **Mastery** | One try shows everything; luck decides. | Bronze is easy, gold takes practice; a better driver clearly does better; worth a record. |
| S4 | **Uses the car** | The car is a cursor. | It exercises the driving model or a part (braking, weight, drift, gravel, jumps, a gadget), and ideally teaches something races need (the rally school taught gravel). |
| S5 | **Of this place** | Generic. | Only here: it uses the place's work, landmark or character (the quarry's blasting, the vineyard's barrels, the harbour's crane). |
| S6 | **Story** | No one asks you. | A resident gives it with a line in character; it ties into the place's story or the campaign; you'd tell someone what happened. |
| S7 | **Variety** | Same verb as another activity in this place or the next. | A verb no nearby activity uses (§2). |
| S8 | **Cost** | New systems, art and physics all at once. | Built from existing kits, physics and UI, with at most one new mechanic. |

**Class-proof** is a check, not a score: an activity is either locked to one class (and its targets set for that class's
cars), or its targets scale by class. A time or distance that an E car trivialises is a defect (design §1: design for
the finished game).

## 2. Verbs that aren't A to B

Pick the verb first, then dress it in the place. Each place should spread its activities over different verbs.

| Verb | What you do | Examples that fit the world |
|---|---|---|
| **Physics toy / sport** | Move objects with the car to score | the rugby conversion kick at Quarry Hills (Richard liked it); car football; barrel bowling at the cellar; the Barrel Race at Vinelands; Apple Shake |
| **Precision** | Place the car exactly | park in the ferry's last gap; stop on the line at the quay edge; thread a gate slalom; a gymkhana in the station yard |
| **Style** | Score how you drive | drift score on the quarry floor; donut rings on the beach; a skid-pad figure of eight |
| **Stunt** | Distance, height, airtime, landing | the Tip Leap; a ramp jump into the reservoir; the longest roll down the slate tip |
| **Herd / push** | Steer loose objects into a pen | runaway tyres rolling down Church Hill into the garage; loose milk churns back onto the stand; push a stuck tractor out of the mud |
| **Knock-down / clear** | Topple or clear targets in a set time | skittles of hay bales; fallen branches off the lane after a storm (scored by count, inside an area) |
| **Tag / pursue (arena)** | Touch, follow or evade a resident car inside a bounded area | tag with the quarry dumpers on the pit floor; keep within 5 m of the Duke's chauffeur round the castle courtyard for 60 s |
| **Find / secret** | Read clues in the world and find something | the old-roadster cellar secret (Richard: "a good opportunity to get a cool model after performing the task required"); a hidden car from a postcard's view |
| **Timing** | Act in a window | under the swing bridge before it closes; across the level crossing between trains (scored by margin, inside the crossing area) |
| **Observe / capture** | Frame a shot | Pip's photo requests (built) |
| **Build / stack** | Place objects to a shape | stack crates with a forklift gadget (when gadgets come) |

Red flags in a pitch: "deliver", "get to", "before the ... arrives", "escape", "follow the route", "checkpoint". If one
appears, apply G1 hard.

## 3. Rewards

Rewards follow the economy (design §6): side income is about a quarter of each class's income, and "adventure rewards
must not instantly buy a winning car". A class C race pays 100 to 500.

- **Every activity pays once per tier, never per repeat.** Bronze, silver and gold each pay the first time; repeats are
  for records.
- **One-time money:** class C about 150 / 250 / 400 for bronze / silver / gold (the class multipliers of §3 above C).
- **Part discoveries and gates:** a part can be gated on an activity, with a new `activity:<id>:<tier>` condition beside
  `wins:N` and `rallyschool` in `PartDiscovery` (to add with the first activity that needs it). Use this for the place's local speciality or a sidegrade, never for a straight
  upgrade that skips the ladder.
- **Bodies and models:** the best rewards, for secrets and gold tiers: an unsold body (the old roadster), a livery, a
  paint. Never a body that beats the class ladder.
- **Keepsakes** (the place's collection), **recruits** (a contact for the class A teams), **gadgets** and **routes**
  (opening a shortcut or a gate) are rewards too.
- Show the reward on the card before you start.

## 4. Measuring and testing

The shared harness (`--activity-test <id>`, the `LITTLE_ROAD_ACTIVITY` line, tier records) doesn't exist yet: build it
with the sweep's first activity, so every later one plugs into it. Each activity defines, in its brief and its code:
- **The metric** (one number), how it's logged (`LITTLE_ROAD_ACTIVITY <id> metric=<value> tier=<none|bronze|silver|gold>`)
  and the fail state.
- **Tier targets from a measured reference, not guesses:** run the automated player (`--activity-test <id>`) on the
  stock car for the activity's class and on the class's podium build. Bronze is reachable on the stock car, gold needs
  a near-reference run on a good build. Record the reference runs in the brief, as speed traps compute their marks from
  the parts ladder.
- **Determinism:** fixed seeds for any randomness; the same inputs give the same metric.
- **An editor check** (`unity/Assets/Editor/*Checks.cs`, registered in `MigrationBuild.cs`): its arena is clear of roads,
  its props spawn and despawn, its reward is wired.
- **A capture** at its start pose with `--view-shots` (and `--hide` to find what draws something odd).

## 5. Method

1. **Collect.** Start from the place's "for the activities sweep" notes in `docs/world.md` §3 and §2 above. Aim for
   three to five activities per town, one or two per village, each with a different verb.
2. **Brief** each idea on the one-page template (§6). Write the goal sentence first: if it won't come, the idea isn't
   ready.
3. **Gate** it (G1-G7). Rewrite or reject; record rejections and why, in the place's section.
4. **Score** it (S1-S8) and check variety against the place and its neighbours. Build only 30+ of 40.
5. **Prototype** a graybox behind an off-by-default `--activity-<id>` flag with its editor check and automated test.
   Measure the reference runs and set the tiers.
6. **Playtest** with Richard; keep or reject, and record the result in `docs/design.md` §12 and the place's section.
7. **Finish** (art, dialogue, reward wiring) only after it's kept. Then turn the flag on by default.

The sweep's first job is to put every built activity (design §12, "Things to do") through §1, and keep, rework or retire
each one.

## 6. The brief (one page)

```
Name:            (in the place's voice)
Place / spot:    (town, coordinates of its arena, why here)
Giver:           (resident car, their line)
Goal sentence:   (under 15 words, says how you win)
Verb:            (§2)
First 10 s:      (what your hands do, and why it feels good)
Fail / restart:  (how you fail; restart in ≤ 3 s)
Metric:          (one number) + tiers: bronze / silver / gold (from the reference runs)
Class:           (locked to X, or how targets scale)
Reward:          (per tier, §3)
Props:           (what spawns, where, and that it all goes when it ends)
Builds on:       (existing systems and kits; the one new mechanic, if any)
Test:            (--activity-test plan, editor check)
Gates:           G1 ... G7 pass/fail, with a line each
Scores:          S1 ... S8, total /40
```

## 7. Failure modes (what made the first activities underbaked)

- **A to B in costume:** a timed climb, a sprint, an escape or a race against the train is a race without the racing.
  These fail G1.
- **No clear goal:** "explore", "have a go" or a physics toy with nothing to score. Fails G3 and G6.
- **No reason to do it:** no reward, or a repeatable payout that breaks the economy. Fails G5, or §3.
- **Clutter:** props left standing in the world and in the road after the activity. Fails G7.
- **Generic:** it could be in any town. Scores low on S5.
- **Untestable:** only a human can say whether it works, so it breaks silently. Fails G4.
- **Not class-proof:** trivial in a fast car, impossible in a slow one.
