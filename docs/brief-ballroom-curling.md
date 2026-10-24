# Brief: Ballroom Curling (the first minigame)

Richard chose it on 24 Oct from the final two of the activities bracket (`docs/brief-activities.md`). It was written to
the activity-design skill (v2), and it's the first minigame of the rebuild. It also builds the shared minigame harness
that every later one plugs into. Working brief: it's folded into `docs/design.md` §12 and `docs/world.md` (Hillcrest)
and deleted when the minigame is kept.

## The pitch

Duke Rolo holds a winter ball in the long gallery of Hillcrest Castle, and at the Duke's ball the cars curl. The marble
floor is polished like glass under three chandeliers. A string quartet of cars plays, and the guests line the walls
behind velvet ropes. You take your car down the gallery, cut the engine at the line and slide it into the rings. You knock
the footmen's cars out of the house, and at last you face the Duke himself, curling his Regale.

- **Goal sentence:** "Slide closest to the button: most points after three ends wins."
- **Verb:** precision (draw weight and line), with knock-outs (takeouts). A classic sport remade for cars, in a venue
  only this place has.
- **First 10 s:** you roll down the approach, watching the weight gauge climb toward the DRAW band. You cross the hog
  line, the engine cuts with a clunk, and the car glides silently, curling a little as you steer. Will it stop on the
  button? The chandeliers swing past overhead.

## Where: an event arena

- **Entry:** Ambrose, the Duke's butler (a long black limousine), waits in the castle courtyard beside the Duke:
  "His Grace is holding a ball tonight. Curling, naturally. You'll want your best wheels." Talk to him, and an
  invitation card shows the opponent, the rules and the prize. Accept, and the screen fades.
- **The venue:** the castle's long gallery, built as its own space (off-map, or loaded for the match). The castle's
  interiors aren't built, and the gallery needn't match the palazzo's footprint:
  - a marble floor in cream and green, and three chandeliers;
  - tall arched windows onto the night valley, with the town's lights below;
  - portraits of the Duke's cars;
  - a car string quartet on a dais, cello cases on roof racks;
  - guest cars in finery along the walls, behind velvet ropes (the Bramblewick crowd system, re-dressed);
  - the Duke's chair-like dais at the house end, and a scoreboard on an easel.
- **Exit:** after the result card (Rematch, or Back to the courtyard), fade back to Ambrose in the courtyard.
- **Light:** indoor, warm chandelier light, a lighting set of its own. The world's sun, clouds and fog stay outside. The
  look rules still hold: materials through `TrialWorld.Material`, colour limits from `WorldLook`.
- G7 (leaves no trace): nothing is placed in the world but Ambrose.

## How a match plays

**The sheet (toy scale for 4 m cars):**
- 82 m long and 18 m wide;
- the hack (start) at one end, then a 22 m approach to the near hog line (the release);
- the house: rings at 7.5 m, 5 m and 2.5 m, and a 1 m button, centred 58 m from the hog line;
- the back line 8 m behind the tee, with the side boards along both walls.

**A delivery:**
1. You start at the hack, held for 2 s.
2. Drive down the approach; throttle sets your speed. The HUD's weight gauge shows the bands: GUARD (stops before the
   house), DRAW (stops in it), TAKEOUT (hits hard).
3. At the hog line the engine cuts and the car slides. The steering curls it gently, and holding the brake drags it
   shorter (a little).
4. Optional: **sweeping.** Hold Sweep (R1 / Q) and two footman cars with polishing mops on their bumpers rush ahead of
   you, buffing the marble. You slide about 10% further and curl less. It's the curling skill, done the car-world way,
   and the spectacle shot of the minigame. Phase 3, if it plays well.
5. The car stops, and it stays on the sheet as your **stone**: a copy of your car in your colours, a physical body
   that can be hit. You're back at the hack for the next delivery.

**Out of play:** a stone that crosses the back line ("into the orchestra"), touches the side boards ("into the
buffet"), or stops short of the far hog line is removed.

**A match:**
- **Three ends, three deliveries each side per end,** alternating, so 18 deliveries and about 5-6 minutes.
- **The hammer** (the last delivery of an end) goes to the side that didn't score the end before. The opponent has it
  in end 1.
- **Scoring each end:** only the side with the stone closest to the tee scores, one point for each of its stones in the
  house that's closer than the other side's closest. A tie after three ends goes to an extra end.
- **On the scoreboard:** the score by end, the hammer, and the stones left as little car icons. At the end of each end,
  a measuring ring shows who's "shot".

**Opponents (a ladder, one match each):**
1. **Fennimore the footman:** a stately old saloon. He plays only draws, with wide aim.
2. **Contessa Marchetti:** a red 60s coupe, of one of Hillcrest's old families. She loves takeouts.
3. **Duke Rolo, in the Regale:** accurate. He plays guards and freezes, and keeps the hammer well.

**The opponents' brains:** a shot planner over the same slide model. It picks a shot (draw to the button, draw behind a
guard, take out their shot stone, guard), solves the release speed and line for it, then adds aim and weight error
from that opponent's skill. Seeded, so a match replays the same way.

## The one new mechanic: slide mode

Cars have frictionless colliders, and all their motion comes from `RoadCar`'s forces. So a slide is a mode of
`RoadCar`. Past the hog line it applies none of the drive, lateral grip or steering servo. Instead it decelerates along
the velocity, curls the velocity by the steering, and spins the wheels to match.

Placed stones are frozen copies of the car body (no `RoadCar`). They carry a small `CurlingStone` component that runs
the same deceleration and takes the hits through PhysX, with stone-like bounce. Curling stones are fairly elastic:
restitution of about 0.6 between stones, tuned in phase 2.

These numbers are to measure in phase 1, not to trust:
- deceleration about 1.15 m/s², so a 9 m/s release slides about 35 m (a draw) and 13 m/s takes out;
- curl up to about 1.5 m of drift over a full slide;
- release speed capped at 16 m/s.

**Your car's weight:** car mass varies by body (`850 × tuning.mass`). A heavier car takes out better and is harder to
stop. Proposed: mass counts, clamped to 0.8-1.3 of the middle, so your body choice is a small strategy, but no build
breaks it. Engine power, tyres and class don't matter (the engine's off), so it's class-proof by design: any car, any
class, the same sheet.

## Measurable and testable

**Metric:** the points difference of a match, and its result. Logged:
`LITTLE_ROAD_ACTIVITY curling opponent=<id> metric=<diff> result=<win|loss> ends=<a-b,...>`.

**Tiers are the ladder:**
- bronze: beat Fennimore;
- silver: beat Marchetti;
- gold: beat the Duke.

**Automated (`--activity-test curling`, the first use of the shared harness):**
1. **Slide calibration:** deliveries at 6-16 m/s on the stock car and the heaviest and lightest bodies. Each logs where
   it stops; distance must rise with speed, and the DRAW band must stop in the house.
2. **Planner accuracy:** 20 noiseless draws to the button must stop within 0.5 m on average, and a noiseless takeout
   must clear a stone on the button.
3. **Matches:** AI against AI against each opponent finishes in under 8 minutes, with consistent scores.
4. **Difficulty:** a player-bot (the planner with a "keen amateur" error) plays each opponent over 20 seeds.
   - The targets are a win rate of about 80% against Fennimore, 50% against Marchetti and 20% against the Duke.
   - The opponents' error is tuned until those hold.

**Editor check (`CurlingChecks`):**
- sheet and house dimensions, and line distances;
- the scoring function on fixed cases (counting, ties, the hammer rule, out-of-play);
- save ids, and that the rewards are wired.

**Capture:** the venue from the hack and from above with `--view-shots`.

**Determinism:** seeded planner errors on PhysX's fixed step, so a seed replays the same match.

## Rewards

**Credits, once each:**
- 150 for beating Fennimore;
- 250 for Marchetti;
- 400 for the Duke.

That's 800 in all, at class C money; later classes don't change it (it's the Duke's ball, not a race).

**Signature prize:** beating the Duke wins the **"Ballroom" livery**, mirror chrome with gold pinstripes, and the
title **Champion of the Winter Ball** on your licence. The Duke, beaten:
> "Splendid! But a ballroom is not a racetrack, darling. Come and see me on the raceway."

That points you at the Duke's Challenge. Rematches are for honour and the record (your best points difference against
each opponent).

**RECORDS:** this minigame brings back a RECORDS tab, built for minigames this time: per opponent, won or not, your
best points difference and your matches played.

## Gates and scores (skill v2)

**Gates:**

| Gate | Result | Why |
|---|---|---|
| G1 Not A to B | Pass | There's no destination; the score is where your stones rest relative to theirs. |
| G2 Fits the world | Pass | Cars only, toy and comic. The Duke's ball is his character. |
| G3 Measurable | Pass | Points by end, seeded. |
| G4 Testable | Pass | Calibration, planner, AI matches and the bot's win rates, all automated. |
| G5 Contributes | Pass | 800 credits, the Ballroom livery and a title. |
| G6 One sentence | Pass | "Slide closest to the button: most points after three ends wins." |
| G7 No trace | Pass | An event venue, with only Ambrose in the world. |

**Scores** (38+ to build):

| # | Criterion | Score | Why |
|---|---|---|---|
| S1 | Fun: the verb | 4 | A delivery is a weight-and-line judgement, and the slide is tense. |
| S2 | Toy | 5 | Knocking cars about on marble is funny; the next delivery comes at once. |
| S3 | Mastery | 5 | Weight, line, curl, shot choice and the hammer: real curling depth. |
| S4 | Uses the car | 4 | Momentum and mass, and the steering curl. |
| S5 | Of this place | 5 | The Duke's castle, his ball, his staff and his Regale. |
| S6 | Story | 5 | Ambrose's invitation, a ladder of characters, and the Duke, who points you at his Challenge. |
| S7 | Variety | 5 | No other minigame slides or curls. |
| S8 | Cost | 3 | A new venue and the slide mode, though it reuses crowds, residents, HUD and AI. |
| S9 | Spectacle | 5 | A takeout scattering cars under the chandeliers, and the footmen sweeping. |
| S10 | Structure | 5 | Ends and the hammer, three opponents, a boss. |
| | **Total** | **46/50** | |

## Build plan

Each phase sits behind `--activity-curling` until Richard keeps it. Each phase ends with its automated checks passing,
and Richard plays the end of phases 1, 3 and 5.

1. **Graybox slide.**
   - A plain sheet off-map, with lines and rings painted.
   - Slide mode in `RoadCar`, a solo delivery and the weight gauge.
   - The shared harness: `--activity-test`, the `LITTLE_ROAD_ACTIVITY` line, minigame records in the save.
   - Calibrate the slide.
   - **Richard plays:** does a delivery feel good on its own?
2. **Stones and scoring.** Placed stones, collisions and bounce, out-of-play, the measuring ring and end scoring, and
   `CurlingChecks`.
3. **A match.**
   - Ends, the hammer, the scoreboard HUD and the planner.
   - Fennimore as the first opponent, then the bot's win-rate tuning.
   - Sweeping, as an option to try.
   - **Richard plays a match.**
4. **The ladder.** Marchetti and the Duke, their styles, the rewards, the RECORDS tab and the result card with Rematch.
5. **The ball.**
   - The venue: walls, windows onto the night valley, chandeliers, portraits, the quartet, guest crowds and the indoor
     light set.
   - Ambrose and his invitation, the fades in and out.
   - Sound: the slide's hiss and the quartet.
   - **Richard plays it from the courtyard.**
6. **Keep or reject;** then flag on by default, the docs folded in, this brief deleted.

## Questions for Richard

1. **Your car's weight counts** (clamped), so the body you bring matters a little. Or should every car slide the same?
2. **Sweeping** with the footman cars: build it in phase 3, or leave it out unless the slide needs more control?
3. **When:** the ball any time you visit, or only in the evening (the castle lit, Ambrose waiting from dusk)?
4. **Match length:** three ends of three deliveries (about 5-6 minutes). Or four ends, for more strategy?
