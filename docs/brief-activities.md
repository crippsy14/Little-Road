# Brief: the first minigames (working brief, 24 Oct)

Richard (24 Oct): retire every activity, then plan one at a time as a true minigame. "Draw up a list of 10 different
activities using the new skill. Review the output, then tune the skill to improve the output. When that's done, generate
another 10, then do a competition... pick a winner in each round until you are left with the final 2... Be brave...
if we need to teleport into a designated area for them for the duration (think casino, football etc.) then back out, we
can also do that... no limitation, think big."

This brief holds both batches, the review that tuned the skill, and the bracket. It's folded into `docs/design.md` §12
and deleted once the first minigame is chosen.

## Batch 1: the skill as first written

Each idea has a short brief: place, giver, goal sentence, verb, what the first 10 s feel like, metric, reward, the gates
(G1-G7) and the scores (S1-S8, /40).

1. **Conversion Kick** (Quarry Hills rugby ground; Huw from the club). *Push the big ball up the kicking ramp and over
   the bar: five conversions in 90 s.* Physics sport. First 10 s: line up, accelerate, the ball rolls up the ramp and
   sails. Metric: conversions; tiers 2 / 4 / 5 from wider marks. Reward 150/250/400. Gates pass. **35**.
2. **Cellar Skittles** (Vinelands, the quinta's cellar; Fausto). *Nine empty casks, three barrels: knock them all down.*
   Knock-down. Push a full barrel down the ramp and watch it roll. Metric: casks down. Gates pass. **30**.
3. **Last Car on the Ferry** (Slate Quay; the ferry's deckhand). *Park in the last gap on deck before the ramp lifts.*
   Precision. Reverse into a tight slot between parked cars, the ramp groaning. Metric: margin and time; three decks,
   each tighter. Gates pass. **32**.
4. **Slate Floor Drift** (the quarry floor). *Score 5,000 drift points inside the painted ring in 60 s.* Style. Throw
   the car sideways on loose slate and hold it. Metric: points. No giver. Gates pass. **34**.
5. **Dumper Tag** (the quarry pit floor; the dumper drivers). *You're it: tag all five dumpers in 90 s.* Tag in an
   arena. Chase a lumbering dumper that wheels away. Metric: tags and time. Gates pass. **30**.
6. **Reservoir Leap** (a ramp into the reservoir). *Jump as far as you can into the water.* Stunt. Metric: distance by
   buoys. No giver. Gates pass. **28: not built.**
7. **Runaway Tyres** (Bramblewick, Church Hill; Jed). *Herd the twelve runaway tyres back into the yard before they
   reach the green.* Herd. Nudge rolling tyres downhill. Metric: tyres in. Gates pass. **30**.
8. **Swing Bridge** (Slate Quay). *Get through before the bridge closes.* **Fails G1** (A to B scored by arrival).
   Rejected.
9. **The Roadster in the Cellar** (Vinelands; the old winemaker's postcards). *Find the old roadster from three
   postcards' clues.* Find. Unlocks the roadster body. Gates pass. **29**: mastery scores 2, as a one-off secret.
10. **Apple Shake, done properly** (Golden Acre; Giles). *Shake the apples into the cart without felling a tree.*
    Knock-down with restraint: a firm tap drops apples, a hard hit fells the tree and costs you. **33**.

### Review of batch 1

The gates worked: the swing bridge was rejected as A to B in costume. The output was still underwhelming:

- **Anchored on the skill's own examples.** Seven of the ten are the verb table's example column dressed up. The skill
  wrote the ideas instead of the designer.
- **Small.** Every idea is "a car in a field doing one thing". None is an event you'd cross the map for. Road Trip's
  minigames felt like *places with their own rules* (a football pitch, a casino). None of these does.
- **The scores don't separate good from great.** Everything lands at 28-35. The rubric has nothing for spectacle (the
  moment you'd show someone) or for structure (rounds, rivals, a ladder): eight criteria, all about soundness.
- **One-shot.** Each is a single challenge with three tiers, not a minigame with rounds, opponents or levels.
- **Alone.** Only Dumper Tag has another car in it. Contests between cars are the world's natural drama.
- **Secrets score unfairly.** A one-off find can't have tiers or rounds, so the roadster lost on mastery. It needs its
  own track.
- **No event arenas.** The skill never mentions teleporting to a venue, so no idea used one.
- **Rewards are generic** (credits per tier); no minigame has a prize of its own.

### How the skill was tuned (v2)

- **§0 Ideate wide before judging:**
  - three sources for every batch: the place's own work, a classic game or sport remade for cars, and a car-world joke
    ("in a world of cars, what is a ballroom?");
  - the verb table is a checklist after ideating, not a menu;
  - throw away the first obvious idea.
- **Event arenas:** a minigame may take you into a venue built for it and back out (Road Trip-style), as well as
  in-world arenas. Rules for when, how you enter and how you leave.
- **Two new scored criteria:** **S9 Spectacle** (the clip you'd share) and **S10 Structure** (rounds, rivals, a ladder
  or levels). The bar becomes 38 of 50, and still no 1s.
- **Rivals encouraged:** resident cars as contestants, reusing the racing AI.
- **A signature prize per minigame** (its own paint, body, part or title), as well as credits.
- **Secrets judged on their own track** (clue quality, the find, the prize), without mastery or structure.

## Batch 2: the tuned skill

Scores are S1-S10, out of 50.

1. **Ballroom Curling** (event arena: the Duke's ballroom at Hillcrest Castle). Rollo invites you to the Duke's winter
   ball, where the cars curl on a polished marble floor under chandeliers. *Slide into the house: closest cars after four
   ends win.*
   - **Play:** cut your engine at the hog line and slide. Knock the Duke's footmen out of the rings with a cannon shot.
   - **Rounds:** three opponents up to Duke Rolo himself.
   - **Prize:** a "Ballroom" chrome livery.
   - **Score: 46.** Fun 4, toy 5, mastery 5, car 4 (weight, momentum), place 5, story 5, variety 5, cost 3 (a new
     venue and slide physics), spectacle 5, structure 5.
2. **Slate Sumo** (in-world: a round slate platform above the pit; the Quarrymen's Institute). *Push your rival off the
   ring: best of three.*
   - **Play:** a ladder of five named sumo cars, each heavier, to the Great Dumper.
   - **Car:** weight, power and tyres matter, so a build is a strategy.
   - **Prize:** a heavy-chassis sidegrade, "Yokozuna".
   - **Score: 45.** 5,5,4,5,4,4,5,4,4,5.
3. **Lighthouse Darts** (in-world: Bayview pier; Pip). *Launch off the pier ramp onto the floating board: 501 to zero.*
   - **Play:** stunt plus precision; three darts (jumps) a turn against Pip; the water you land in now behaves like
     water.
   - **Prize:** a "Bullseye" paint.
   - **Score: 45.** 5,5,4,4,5,4,5,4,5,4.
4. **Musical Bays** (in-world: The Plough's car park; Maggie). *When the music stops, park in a bay; one fewer each
   round.*
   - **Play:** you and seven residents circle the car park to the band. Bumping is allowed, and the last car parked
     wins.
   - **Why:** a car-world joke you can play.
   - **Prize:** Maggie's "Landlady" title on your licence and a pub sign livery.
   - **Score: 44.** 5,5,4,4,4,5,5,4,4,4.
5. **The Glasswater Cup** (event arena: a stadium). *Car football: a giant ball, two goals, three minutes.*
   - **Play:** 1 against 1 first (cost), 3 against 3 later; a league of the towns' teams.
   - **Prize:** each town's kit as a livery.
   - **Score: 44.** 5,5,5,5,3,4,5,2,5,5.
6. **Golden Acre Links** (in-world: a nine-hole course on the farm; Giles has taken up golf). *Push the giant golf ball
   into the hole in the fewest touches.*
   - **Play:** three courses (the farm, the castle green, the clifftop), where slopes are the hazard.
   - **Prize:** a tweed paint.
   - **Score: 43.** 4,4,5,4,4,4,5,4,4,5.
7. **Conversion Kick** (batch 1, rescored with S9 4 and S10 3). **Score: 42.**
8. **Pinball Mountain** (in-world, on one gallery bench of the quarry). *You are the ball: flippers on the triggers,
   bumpers in the slate, score the jackpot.*
   - **Cost:** scoped to one bench, so not a 1.
   - **Score: 42.** 5,5,5,3,5,3,5,2,5,4.
9. **Plough Skittles** (event arena: the pub's skittle alley, toy-scale). *Ten frames: bowl yourself down the alley and
   steer the spin.*
   - **Score: 41.** 4,5,4,4,4,4,4,4,4,4.
10. **Crate Catch** (in-world: Slate Quay; the crane driver). *Catch the crane's falling crates on your flatbed: twenty
    in a row.*
    - **Score: 41.** 4,5,4,4,5,3,5,3,4,4.

Batch 1 rescored with S9 and S10 tops out at 42 (Conversion Kick), then Slate Floor Drift at 40. Only one batch 1 idea
makes the seeded top ten. The tuning raised the ceiling as intended.

## The bracket

The ten best of all twenty, seeded by score. Each match is judged by four questions:
- Which is more fun in its first minute?
- Which is braver and more memorable?
- Which belongs to Little Road more?
- Which can be built well?

**Round 1** (1 against 10, 2 against 9, and so on):
- **Ballroom Curling** (1) beats Crate Catch (10). Crate Catch is good, but it's one trick; curling has rounds, a boss
  and the Duke's ballroom.
- **Slate Sumo** (2) beats Plough Skittles (9). Skittles is bowling with a car. Sumo is something only cars can do, and
  your build matters.
- **Pinball Mountain** (8) beats Lighthouse Darts (3), the upset. Darts is clever, but being the pinball is the braver,
  stranger and more memorable idea. Its cost is its risk.
- **Musical Bays** (4) beats Conversion Kick (7). Seven cars and a band beat one ball and a bar: it's a party, and it's
  pure car-world.
- **The Glasswater Cup** (5) beats Golden Acre Links (6). Football is the proven big minigame (and Road Trip had it);
  golf is pleasant but quieter.

**Round 2** (five left, reseeded; the top seed has a bye):
- **Ballroom Curling:** bye.
- **Slate Sumo** beats Pinball Mountain. Pinball's wow depends on a flipper-and-table physics build we've never done.
  Sumo's wow comes free from the physics we have, and it's endlessly replayable.
- **Musical Bays** beats the Glasswater Cup. Good car football needs team AI we don't have, and done badly it's
  dreadful. Musical Bays is funnier, cheaper and only ours.

**Round 3** (three left; the bye-holder plays the lowest seed):
- **Ballroom Curling** beats Musical Bays. Both are social and funny, but curling has the cleaner skill, the better
  venue and the story: the Duke's ball, ahead of the Duke's Challenge.

**The final two: Ballroom Curling and Slate Sumo.**

### Are they strong enough?

Yes. They're opposites, which is good for the choice:
- **Curling** is precision and nerve: a slide judged to the centimetre, in a venue you're invited to. It's the Road Trip
  "go somewhere special" feeling.
- **Sumo** is weight and aggression: instant, physical, readable from a distance, with a ladder of characters and a part
  that changes how you build your car.

Both pass every gate with room to spare. Both use physics the game already has: momentum, contact and weight. Neither
is A to B in any form. I'd be happy to build either, so no third batch.
