# 70 · Race fields across the map: drivers, the parts curve and the reference table

Richard, 7 October:
- "On Tier C, driver ability should be between 20 and 17 (with the best drivers being closer to or at the front, but some good drivers in the mid and back of pack). Tier C can be between 12 and 16." I read this as class B 17–20 and class C 12–16.
- "Car parts in the race should be a mix of the lowest tier C parts through to some of the parts available at the local garages as a general rule… the further from the starting town you go, the more parts are unlocked… for tier C the further from bayview you are, the harder the races should be."
- "Bayview [should] be the worst Tier B parts mixed in with the best available at the local garage, as we will have this as the first race people do when they unlock tier B, as a ground truth."
- "Lets do the same balancing with the other races as well. Please review the map before hand and see how we will need to balance over a large map with lots of races, and plan how parts should be unlocked in order, with races reflecting the difficulty curve of parts being unlocked, and then create a reference table for the future for parts used for cars in Tier C and B in every planned race track… at what those parts would represent in PP. Keep the drive ratings consistent at this point for Tier C and B, only parts need change."

The code is `FieldRecipe`, with `FieldRecipeChecks` in the build. It started behind `--field-recipe`; since Richard's "make the recipe default" (7 October) it's on unless `--no-field-recipe`.

## 1 · The map, and what it means for balance

`tools/world-plan.py races C` and `races B` list every planned race (doc 58, the world blueprint):

| | Races | Spread | Stars | Built today |
|---|---|---|---|---|
| Class C | 21, at every main town and five C villages | 0–19 km from Bayview | 1–8★, **89★** in all (new: `docs/data/race-stars.json` "C") | Bayview 1★, Hillcrest 2★, Quarry Hills 3★ |
| Class B | 26 (23 in the blueprint, plus today's three towns reversed) | 0–19 km | 1–8★, **120★** in all (doc 67) | Coast Tuners 1★, Valley Tuners 1★, Harbour Reverse 2★, Slate Rallycross 2★, Raceway Reverse 3★, Rallycross Reverse 3★ |
| Tuner Championship | 3 rounds, towns to be chosen | — | 10★ | — |

**What a big open map asks of balance:**
- **No fixed route.** You can drive anywhere, so a race's difficulty can't depend on the order you find it in. It has to come from the race itself, and that's its stars.
- **Stars follow distance from Bayview**, and a town with two races has an easier and a harder one. Class C now follows the same 1–8★ scale as B, since Richard asked for C to get harder further from Bayview. The C stars are:
  - Bayview 1–2★;
  - Hillcrest 2–3★;
  - Bramblewick 2★;
  - Quarry Hills 3–4★;
  - Orchard End 3★;
  - Vinelands 3–4★;
  - Kettle Falls, Cobble Cove and Saltmarsh 4★;
  - My City, Summit Pass and Harbour Point 5★;
  - Pinewood 6★;
  - Redrock, Frostcrown and Port City 7★;
  - Aurora 8★.
- **One rule for every race.** A field comes from three things only: the race's **class**, its **stars** and its **surface** (road, gravel or ice, as `disc` in `race-stars.json`). The track doesn't matter, so any new race gets a field without any new tuning.
- **The parts curve follows the unlock curve.** Each field's best parts are the parts you could have by the time you race there, so the AI is never on parts you can't get.

## 2 · Drivers (the same everywhere in a class)

**The skill scale** (Richard, 7 October): "We take the equivalent scale of 1-20, take whatever the skill was between 8 and 18, and then have a skill of 1-10 for that range instead (1 being the lowest rating)… the current system is jumbled and has too much of an extreme either side."

So the shown driver rating (DR) is now a **skill from 1 to 10**:
- Skill 1 is the old DR 8 (ability mean 0.37), and skill 10 is the old DR 18 (0.89).
- Nothing races outside that, so there are no hopeless drivers and no near-perfect ones.
- The radar shows each ability out of 10 on the same scale. Aggression and patience, being personality rather than skill, show 0–1 as 1–10.

| Class | Skill | Old DR equivalent |
|---|---|---|
| C | 2–3 | 9–10 |
| B | 4–5 | 11–12 |
| A | 6–7 | 14–15 |
| S | 8–9 | 16–17 |
| E | 9–10 | 17–18 |

Each racer's five abilities move together, keeping their style's shape, until their mean shows as the target skill (`RacerAbilities.WithMean`). The old roster sits at about skill 6–8, so class C rivals are now clearly gentler drivers, and the parts carry each class's step.

**History, 7 October:** the first bands were DR 12–16 (C) and 17–20 (B). The B band is what crashed (below) and what Richard replaced with this scale.

The roll is 70% grid position and 30% seeded luck, so the best drivers start at or near the front. Three **wildcards** per race (slots 9–23) roll at least 80% of the band: good drivers starting mid-pack or at the back. The old roster is DR 13–16, so class C barely changes; class B gets genuinely better drivers.

**A whole field of DR 17–20 drivers is new to the AI, and it crashed.** At Coast Tuners, the recipe's drivers on the old kits raised contacts from 0.8 to 7.4 per car. It caused 6 assisted resets and 3 non-finishers in one seed, all piling up 55–65 m into the lap, at the first corner. Holding one ability back at a time found the cause (6 races each, Coast Tuners, seeds 1720/1731/1742):

| Held back | Crashed cars | Contacts per car |
|---|---|---|
| Nothing (DR 17–20 in full) | 4 | 7.4 |
| Racecraft 0.7 | 4 | 8.6 |
| Braking 0.8 | 3 | 6.2 |
| Control 0.7 | 3 | 2.4 |
| **Line 0.7** | **0** | **1.4** |

**Why Line:**
- At Line ~1.0, a driver corners at 95% of the grip limit with no apex error, so the whole pack takes one identical path at the edge of grip.
- The old roster's best Line was 0.85, which corners at 91.8% of the limit.

**The fix,** behind `--traffic-corner-margin` and on whenever `--field-recipe` is:
- With a car within 25 m ahead (or 6 m behind) and 4 m across, a driver corners at no more than **92%**: what the old roster's best drivers used.
- In clear air, Line counts in full, so a DR 20 driver is still quicker than a DR 17 one.
- Study 221 runs the standard keep/reject protocol for it on the normal roster (section 5).

## 3 · The order parts unlock in (the plan)

### Class C: by discovery, along the map

| Order | Parts | Where | Open in fields from |
|---|---|---|---|
| 1 | Bayview's garage: three or four rungs per slot (street → club → sport) | Bayview Pit Stop, from the start (money is the gate) | 1★ |
| 2 | Hillcrest's eight valley parts: Valley V6, twin turbo, hill grip tyres, six-speed, ceramics, rally rack, carbon frame, rally tyres | Found at Hillcrest (1 km), then sold everywhere | 2★ |
| 3 | The gravel kit: gravel tyres and gravel rack | Quarry Hills, rally school gold | 3★ (gravel races) |
| 4 | Bramblewick's backyard tuner (planned, doc 58: "class C parts that aren't in Bayview") | Bramblewick (2.9 km) | 2–3★, once designed |

**What this means:** class C's ceiling is reached early (Hillcrest's parts at 1 km). From 3★ on, C races get harder by **the floor rising**: the whole field fills up to the full C build, the way the player's own car does as their money grows. That's honest to Richard's "more parts are unlocked… depending on where you go".

Doc 67 left moving C parts onto Rep (stars won) as optional. It isn't needed for the fields, so it stays optional.

### Class B: by stars won (unchanged from doc 67)

| Stars won | Unlocks |
|---|---|
| 0 (B opens) | All seven Good parts |
| 4 | Better tyres, Better brakes; B rally tyres |
| 10 | Better engine, Better steering |
| 18 | Better turbo, Better chassis |
| 28 | Better transmission |
| 42 | Best tyres, Best brakes |
| 58 | Best engine, Best steering |
| 76 | Best turbo, Best chassis |
| 95 | Best transmission |

**Stars you'd have won before a race** (every easier race on the planned map; `FieldRecipe.BStarsBefore`, checked against `race-stars.json` on every build):

| Race | 1★ | 2★ | 3★ | 4★ | 5★ | 6★ | 7★ | 8★ | 10★ |
|---|---|---|---|---|---|---|---|---|---|
| Stars won before it | 0 | 2 | 8 | 23 | 35 | 50 | 68 | 96 | 120 |
| What's open | Good | Good | + Better tyres and brakes | + Better engine, steering, turbo and chassis | all Better | + Best tyres and brakes | + Best engine and steering | everything | everything |

### Planned parts the curve needs

- **Good Tuner parts that don't beat class C.** Hillcrest's best class C parts are better on paper than three of the Good Tuner parts:
  - the twin turbo has power ×1.30 against the big-single's ×1.22;
  - the carbon frame has mass ×0.87 against the strut-braced frame's ×0.95;
  - the Valley V6 has 230 Nm against the twin-cam's 175 Nm.

  So a 1★ B rival mixing them can rate PP 208, above the free Tuner's 196. Doc 44 wants every B part above class C. Either the Good turbo, chassis and engine move up, or Hillcrest's parts move down. That's for Richard; the parts data is unchanged.
- **A real class B gravel tyre.** The B "Tarmac rally tyre" has less dirt grip (12) than class C's gravel tyre (15). B gravel fields run it from 1★ to 10★ because there's nothing above it. Highmoor's rally shed (doc 58, the B gravel parts) is the place for a B gravel ladder.
- **Ice tyres** for Frostcrown (C 7★, B 8★). The parts shop already shows "Ice · soon". Until they exist, ice fields use the road ladder.
- **Bramblewick's C parts** (above).

## 4 · How a field is built

For each slot:
1. **The ladder:** that class and surface's parts, worst first.
   - Class C: by price; tyres by road grip, or by dirt grip on gravel.
   - Class B: the best class C part, then Good, Better and Best.
2. **The ceiling:** the ladder cut to what's open by that race (section 3).
3. **The floor:** a share of the open ladder that rises with the stars: 0 at 1★, 0.1 more per star, 0.7 at 8★, 0.9 at the 10★ championship. It's continuous, so even short ladders rise a little at every star level.
4. **The car's kit level:** 70% grid position and 30% luck, rolled separately from its driver. Each slot is nudged by up to a rung, so cars aren't uniform.

**Class B tyres never drop to class C.** That's the one exception to "the best of the local garage" in Bayview's first B race. Section 5 has the reason.

**Bodies:**
- class C on tarmac: family cars for low kits and sporty ones for high, with the liveried flagship on pole;
- class C on gravel: the off-roaders;
- class B: the Tuners, or the gravel Tuners on gravel.

## 5 · Results at Bayview (same build, flag off against on, seeds 1720/1731/1742)

**Class C, Bayview Coast 1★** (your car: the watch-cars save's Puddle):

| | Off | On |
|---|---|---|
| Finished | 72/72 | 72/72 |
| Assisted resets | 0 | 0 |
| Mean total time (24 cars) | 2342.1 s | 2277.5 s (−2.8%) |
| Winner | 83.1 s | 77.4–81.9 s |
| Last | 104.5–105.0 s | 105.9–110.1 s |

**Class B, Coast Tuners 1★, first try:** the recipe mixed Tuners on class C tyres (road grip 10.2) with Tuners on Good tyres (14.5). It finished 71/72 with 4 assisted resets. The field spread about 20 s over three laps of the 683 m loop, so the leaders caught the back markers on lap 3, and two pairs wedged at the same corner. **Fixed:** B cars always run B tyres; the best-C-to-Good mix now covers the other six slots. (That run also left your car in the Puddle by mistake: the test save had no C podiums, so B wasn't open. Fixed in the review hook.)

**Class B, Coast Tuners 1★, final** (your car: the free Sakura on its Good parts, PP 196):

| | Off (old 1★ kits, old drivers) | On (recipe, with the traffic corner margin) |
|---|---|---|
| Finished | 72/72 | 72/72 |
| Assisted resets | 0 | 0 |
| Cars needing a physical recovery | 0 | 1 (stopped 8 s) |
| Mean total time (24 cars) | 1692.5 s | 1698.2 s |
| Winner | 62.1–62.2 s | 58.9–59.8 s |
| Last | 75.9–76.3 s | 77.2–85.3 s |
| You | P18–P23 | P15–P22 |

On the way there (all 3 seeds each):

| Run | Finished | Assisted resets | Crashed cars |
|---|---|---|---|
| Recipe, no margin | 72/72 | 0 | 4 |
| Recipe drivers on the old kits | 69/72 | 6 | 4 |
| Recipe kits with the old drivers | 72/72 | 0 | 0 |

That's why the margin exists (section 2).

**The Bayview C → 1★ B step** (Richard's earlier question), with both on the recipe:
- total time 2277.5 s → 1698.2 s, **−579.3 s (−25.4%)**;
- with the old fields, it was 2342.1 s → 1692.5 s (−27.7%).

The earlier −26.6% had your car in the class C Puddle in the B race.

**Study 221** (`--traffic-corner-margin` alone, the standard protocol: Cedar and Bayview, seeds 1709–1711, the normal roster):
- all 12 races are identical off and on: 24/24, no resets, the same times;
- the 92% cap never binds for today's drivers (best 91.8%);
- it's harmless but not faster, so it's on only with `--field-recipe`, where the DR 17–20 drivers need it.

**Every other built event** (fr7: same build, recipe off against on with the traffic margin, seeds 1720/1731/1742; your car is the Puddle in C and the free Sakura in B):

| Event | Stars | Off: finished · assisted · crashed cars | On: finished · assisted · crashed cars | Mean total, off → on |
|---|---|---|---|---|
| Valley Cup (C, Hillcrest) | 2★ | 72/72 · 0 · 0 | 72/72 · 0 · 1 | 3609.7 → 3348.8 s (−7.2%) |
| Quarry Hills Rallycross (C, gravel) | 3★ | 72/72 · 0 · 0 | 72/72 · 0 · 0 | 3843.2 → 3506.2 s (−8.8%) |
| Harbour Reverse (B, Bayview) | 2★ | 72/72 · 0 · 2 | 70/72 · 0 · 1 | 1682.2 → 1675.0 s |
| Slate Rallycross (B, gravel) | 2★ | 72/72 · 0 · 0 | 72/72 · 0 · 0 | 2860.2 → 2970.0 s (+3.8%) |
| Rallycross Reverse (B, gravel) | 3★ | 72/72 · 0 · 0 | 72/72 · 0 · 0 | 2777.9 → 2886.0 s (+3.9%) |
| **Valley Tuners (B, Hillcrest)** | 1★ | 72/72 · 4 · 7 | **43/72 · 35 · 15** | not comparable |
| **Raceway Reverse (B, Hillcrest)** | 3★ | 72/72 · 1 · 2 | **70/72 · 20 · 19** | 2595.6 → 3108.7 s |

**What this says:**
- **Class C is ready.** Every C event finishes clean, and the fields get harder with the stars: −2.8% total time at Bayview 1★, −7.2% at Hillcrest 2★, −8.8% at Quarry Hills 3★.
- **Class B on gravel is clean**, and slightly slower than before: the old gravel kits ran a star above the event at the front.
- **Class B at Hillcrest Raceway is not ready.** The DR 17–20 drivers crash there even with the traffic margin. Hillcrest already crashed a few B cars with the old field (4 assisted resets at Valley Tuners), so the raceway is marginal at B speeds. Doc 54 noted the elevated raceway's missing barriers. The recipe's drivers make it much worse.
- **Hillcrest, diagnosed** (Valley Tuners, 3 seeds each):

  | Run | Finished | Assisted resets | Crashed cars |
  |---|---|---|---|
  | Old field (recipe off) | 72/72 | 4 | 7 |
  | Recipe with the traffic margin | 43/72 | 35 | 15 |
  | Recipe drivers on the old kits | 64/72 | 28 | 33 |
  | Recipe, Line held at 0.7 | 69/72 | 6 | 10 |
  | Recipe, all corner use ×0.97 | 72/72 | 6 | 12 |
  | Recipe, all corner use ×0.94 | 64/72 | 19 | 13 |

  **Where they stop.** The assisted resets bunch at two places on the 1,689 m lap:
  - **About 240 m:** cars end up 40–60 m off the circuit. The old field stops here too.
  - **About 930 m:** cars end up about 20 m off, and only the top drivers reach it.

  B-speed cars leave the raceway at those two corners and get stuck in the terrain off it. Holding Line back helps most, but no driver margin removes it, and the old field already does it now and then.

  **So this is the track, not the field.** Doc 54 found the raceway's missing barriers with the maximum build, and these corners need barriers or run-off back to the road (doc 57 §2.4: every drop guarded or drivable). That's the next job before class B fields can be the default at Hillcrest.

**The skill scale, all nine built events** (sk1: same build, the recipe on by default, seeds 1720/1731/1742; your car is the Puddle in C and the free Sakura in B at its normal ability):

| Event | Finished | Assisted resets | Mean total | Your Sakura (B) |
|---|---|---|---|---|
| Bayview Coast (C 1★) | 72/72 | 0 | 2454.2 s | — |
| Valley Cup (C 2★) | 72/72 | 0 | 3605.6 s | — |
| Quarry Hills Rallycross (C 3★) | 72/72 | 0 | 3679.3 s | — |
| Coast Tuners (B 1★) | 72/72 | 0 | 1861.1 s | P5–P12 |
| Valley Tuners (B 1★) | 72/72 | 0 | 2897.1 s | P7–P11 |
| Harbour Reverse (B 2★) | 72/72 | 0 | 1856.8 s | P11–P19 |
| Raceway Reverse (B 3★) | 72/72 | 1 (your autopilot car, stopped 60 s) | 2871.1 s | P9–P24 |
| Slate Rallycross (B 2★) | 72/72 | 0 | 3122.4 s | P18–P21 |
| Rallycross Reverse (B 3★) | 72/72 | 0 | 3051.5 s | P16–P22 |

**648 of 648 finished.** The simpler scale fixed Hillcrest as well: Valley Tuners went from 35 assisted resets to 0. The near-perfect drivers were the cause, and the scale no longer has them.

**The step from Bayview C to B 1★** is now 2454.2 s → 1861.1 s (−24.2%). The class C winner takes 88–91 s, against 77–82 s with the old roster: class C rivals are gentler drivers now.

**Watch races** (Coast Tuners, seed 1720):

| Your Sakura | Result |
|---|---|
| Normal ability | P20, 75.2 s (on the 17–20 field) |
| Skill 1 equivalent (all abilities 0) | Last, 110.7 s |
| Skill 10 (all abilities 0.895), on the 4–5 field | P13, 77.2 s |

**Hillcrest Raceway's corner barriers** are built anyway (`CoastalLandscape.CatchSides`, checked in `FieldRecipeChecks`):
- armco on the outside of every bend tighter than 120 m, 30 m either side;
- that includes both corners where class B cars left the circuit, at about 240 m and 930 m;
- the straights stay open.

## 6 · The reference: every planned C and B race

From `tools/field-table.py` (data: `docs/data/field-recipes.json`, written by the build).
- **Rival PP** is over the 23 rivals on three seeds.
- **Floor kit → ceiling kit** is a car on every slot's floor part against one on every ceiling part.
- PP is the dry-road rating, so gravel fields are rated as if on tarmac. Ice\* uses the road ladder until ice tyres exist.

For comparison:
- your stock starter is **PP 100**;
- the free Tuner on its seven Good parts is **PP 196**.

### Every race

| Class | Stars | Race | Place | km from Bayview | Surface | Rival PP: min · mean · max | Floor kit PP → ceiling kit PP | Built as |
|---|---|---|---|---|---|---|---|---|
| C | 1★ | Bayview Coast | Bayview | 0.0 | Road | 100 · 134 · 160 | 100 → 160 | bayview |
| C | 2★ | Bayview Coast, the harder round | Bayview | 0.0 | Road | 117 · 146 · 171 | 100 → 175 | - |
| C | 2★ | Valley Cup | Hillcrest | 1.0 | Road | 117 · 146 · 171 | 100 → 175 | hillcrest |
| C | 2★ | Bramblewick lanes | Bramblewick | 2.9 | Road | 117 · 146 · 171 | 100 → 175 | - |
| C | 3★ | Valley Cup, the harder round | Hillcrest | 1.0 | Road | 120 · 150 · 172 | 100 → 175 | - |
| C | 3★ | Quarry Hills Rallycross | Quarry Hills | 3.5 | Gravel | 121 · 147 · 168 | 103 → 166 | quarry |
| C | 3★ | Blossom Run | Orchard End | 3.8 | Road | 120 · 150 · 172 | 100 → 175 | - |
| C | 3★ | Vineyard Loop | Vinelands | 4.6 | Road | 120 · 150 · 172 | 100 → 175 | - |
| C | 4★ | Quarry Link | Quarry Hills | 3.5 | Gravel | 127 · 150 · 170 | 121 → 166 | - |
| C | 4★ | River Road Sprint | Vinelands | 4.6 | Road | 127 · 154 · 173 | 123 → 175 | - |
| C | 4★ | Kettle Falls climb | Kettle Falls | 5.2 | Road | 127 · 154 · 173 | 123 → 175 | - |
| C | 4★ | Cobbled quay sprint | Cobble Cove | 5.8 | Road | 127 · 154 · 173 | 123 → 175 | - |
| C | 4★ | Low-tide beach sprint | Saltmarsh | 6.6 | Gravel | 127 · 150 · 170 | 121 → 166 | - |
| C | 5★ | Waterfront Street | My City | 8.0 | Road | 141 · 156 · 173 | 123 → 175 | - |
| C | 5★ | Summit Climb | Summit Pass | 8.2 | Road | 141 · 156 · 173 | 123 → 175 | - |
| C | 5★ | Quayside Sprint | Harbour Point | 8.6 | Road | 141 · 156 · 173 | 123 → 175 | - |
| C | 6★ | Forest Sprint | Pinewood Coast | 10.8 | Gravel | 143 · 155 · 170 | 142 → 166 | - |
| C | 7★ | Canyon Sprint | Redrock Mesa | 13.4 | Gravel | 144 · 158 · 170 | 143 → 166 | - |
| C | 7★ | Frozen Lake Oval | Frostcrown | 15.9 | Ice* | 151 · 163 · 173 | 144 → 175 | - |
| C | 7★ | Docks Street Race | Port City | 16.8 | Road | 151 · 163 · 173 | 144 → 175 | - |
| C | 8★ | Aurora Club | Aurora Raceway | 19.3 | Road | 158 · 166 · 175 | 144 → 175 | - |
| B | 1★ | Coast Tuners | Bayview | 0.0 | Road | 178 · 193 · 208 | 189 → 196 | b_coast |
| B | 1★ | Valley Tuners | Hillcrest | 1.0 | Road | 178 · 193 · 208 | 189 → 196 | b_valley |
| B | 2★ | Harbour Reverse | Bayview | 0.0 | Road | 178 · 193 · 208 | 189 → 196 | b_harbour_rev (reversed) |
| B | 2★ | Slate Rallycross | Quarry Hills | 3.5 | Gravel | 175 · 190 · 205 | 186 → 193 | b_slate |
| B | 2★ | Vineyard Tuners | Vinelands | 4.6 | Road | 178 · 193 · 208 | 189 → 196 | - |
| B | 3★ | Raceway Reverse | Hillcrest | 1.0 | Road | 179 · 196 · 211 | 189 → 201 | b_raceway_rev (reversed) |
| B | 3★ | Rallycross Reverse | Quarry Hills | 3.5 | Gravel | 176 · 191 · 205 | 186 → 195 | b_rallycross_rev (reversed) |
| B | 3★ | Tollgate | Tollgate | 5.5 | Road | 179 · 196 · 211 | 189 → 201 | - |
| B | 3★ | Highmoor | Highmoor | 5.8 | Gravel | 176 · 191 · 205 | 186 → 195 | - |
| B | 3★ | Millbrook | Millbrook | 6.3 | Road | 179 · 196 · 211 | 189 → 201 | - |
| B | 4★ | Reef Street | Coral Key | 4.3 | Road | 194 · 204 · 217 | 189 → 217 | - |
| B | 4★ | Dunmore Junction | Dunmore Junction | 8.4 | Road | 194 · 204 · 217 | 189 → 217 | - |
| B | 4★ | Quayside Sprint | Harbour Point | 8.6 | Road | 194 · 204 · 217 | 189 → 217 | - |
| B | 5★ | Waterfront Tuners | My City | 8.0 | Road | 196 · 206 · 217 | 189 → 217 | - |
| B | 5★ | Summit Climb | Summit Pass | 8.2 | Road | 196 · 206 · 217 | 189 → 217 | - |
| B | 5★ | Quayside Sprint, the harder round | Harbour Point | 8.6 | Road | 196 · 206 · 217 | 189 → 217 | - |
| B | 6★ | Reef Street, the harder round | Coral Key | 4.3 | Road | 199 · 211 · 222 | 199 → 222 | - |
| B | 6★ | Forest Sprint | Pinewood Coast | 10.8 | Gravel | 193 · 203 · 213 | 193 → 213 | - |
| B | 6★ | Ridgeway | Ridgeway Circuit | 12.0 | Road | 199 · 211 · 222 | 199 → 222 | - |
| B | 7★ | Summit Climb, the harder round | Summit Pass | 8.2 | Road | 210 · 217 · 223 | 199 → 223 | - |
| B | 7★ | Lowtown | Lowtown | 11.8 | Road | 210 · 217 · 223 | 199 → 223 | - |
| B | 7★ | Canyon Sprint | Redrock Mesa | 13.4 | Gravel | 203 · 209 · 213 | 193 → 213 | - |
| B | 7★ | Docks Street Race | Port City | 16.8 | Road | 210 · 217 · 223 | 199 → 223 | - |
| B | 8★ | Ridgeway, the harder round | Ridgeway Circuit | 12.0 | Road | 217 · 226 · 233 | 217 → 233 | - |
| B | 8★ | Frozen Lake Oval | Frostcrown | 15.9 | Ice* | 217 · 226 · 233 | 217 → 233 | - |
| B | 8★ | Aurora Club | Aurora Raceway | 19.3 | Road | 217 · 226 · 233 | 217 → 233 | - |
| B | 10★ | Tuner Championship (three rounds) | three towns with B races, to be chosen | - | Road | 222 · 230 · 233 | 217 → 233 | - |

### The part windows behind it (per class, surface and stars)

**Class C, road** (each cell: the floor part → the ceiling part; a single name means the whole field runs it)

| Stars | Mean PP | Tyres | Brakes | Engine | Turbo | Transmission | Steering | Chassis |
|---|---|---|---|---|---|---|---|---|
| 1★ | 134 | Stock tyres → Club sport tyres | Stock pads → Big-bore front | Stock 1.2 → Sport 1.6 | Naturally aspirated → Sport turbo | Stock 4-speed → Short-shift 5-speed | Stock rack → Sport rack and wheel | Stock frame → Stripped shell |
| 2★ | 146 | Stock tyres → Hill grip tyres | Stock pads → Ceramic brakes | Stock 1.2 → Valley V6 2.5 | Naturally aspirated → Twin turbo | Stock 4-speed → Six-speed box | Stock rack → Rally rack | Stock frame → Carbon frame |
| 3★ | 150 | Stock tyres → Hill grip tyres | Stock pads → Ceramic brakes | Stock 1.2 → Valley V6 2.5 | Naturally aspirated → Twin turbo | Close-ratio 5-speed → Six-speed box | Stock rack → Rally rack | Stock frame → Carbon frame |
| 4★ | 154 | Touring tyres → Hill grip tyres | Sport pads → Ceramic brakes | Workshop tune → Valley V6 2.5 | Street turbo → Twin turbo | Close-ratio 5-speed → Six-speed box | Quick rack → Rally rack | Light frame → Carbon frame |
| 5★ | 156 | Touring tyres → Hill grip tyres | Sport pads → Ceramic brakes | Workshop tune → Valley V6 2.5 | Street turbo → Twin turbo | Five-speed sport → Six-speed box | Quick rack → Rally rack | Light frame → Carbon frame |
| 6★ | 160 | Road sport tyres → Hill grip tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Five-speed sport → Six-speed box | Precision rack → Rally rack | Seam-welded frame → Carbon frame |
| 7★ | 163 | Road sport tyres → Hill grip tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Long-ratio 5-speed → Six-speed box | Precision rack → Rally rack | Seam-welded frame → Carbon frame |
| 8★ | 166 | Road sport tyres → Hill grip tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Long-ratio 5-speed → Six-speed box | Precision rack → Rally rack | Seam-welded frame → Carbon frame |

**Class C, gravel** (each cell: the floor part → the ceiling part; a single name means the whole field runs it)

| Stars | Mean PP | Tyres | Brakes | Engine | Turbo | Transmission | Steering | Chassis |
|---|---|---|---|---|---|---|---|---|
| 1★ | 130 | Trail tyres | Stock pads → Big-bore front | Stock 1.2 → Sport 1.6 | Naturally aspirated → Sport turbo | Stock 4-speed → Short-shift 5-speed | Stock rack → Sport rack and wheel | Stock frame → Stripped shell |
| 2★ | 142 | Trail tyres → Rally tyres | Stock pads → Ceramic brakes | Stock 1.2 → Valley V6 2.5 | Naturally aspirated → Twin turbo | Stock 4-speed → Six-speed box | Stock rack → Rally rack | Stock frame → Carbon frame |
| 3★ | 147 | Trail tyres → Gravel tyres | Stock pads → Ceramic brakes | Stock 1.2 → Valley V6 2.5 | Naturally aspirated → Twin turbo | Close-ratio 5-speed → Six-speed box | Quick rack → Gravel rack | Stock frame → Carbon frame |
| 4★ | 150 | Trail tyres → Gravel tyres | Sport pads → Ceramic brakes | Workshop tune → Valley V6 2.5 | Street turbo → Twin turbo | Close-ratio 5-speed → Six-speed box | Quick rack → Gravel rack | Light frame → Carbon frame |
| 5★ | 152 | Trail tyres → Gravel tyres | Sport pads → Ceramic brakes | Workshop tune → Valley V6 2.5 | Street turbo → Twin turbo | Five-speed sport → Six-speed box | Precision rack → Gravel rack | Light frame → Carbon frame |
| 6★ | 155 | Rally tyres → Gravel tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Five-speed sport → Six-speed box | Precision rack → Gravel rack | Seam-welded frame → Carbon frame |
| 7★ | 158 | Rally tyres → Gravel tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Long-ratio 5-speed → Six-speed box | Sport rack and wheel → Gravel rack | Seam-welded frame → Carbon frame |
| 8★ | 160 | Rally tyres → Gravel tyres | Race pads → Ceramic brakes | Club motor 1.4 → Valley V6 2.5 | Club turbo → Twin turbo | Long-ratio 5-speed → Six-speed box | Sport rack and wheel → Gravel rack | Seam-welded frame → Carbon frame |

**Class B, road** (each cell: the floor part → the ceiling part; a single name means the whole field runs it)

| Stars | Mean PP | Tyres | Brakes | Engine | Turbo | Transmission | Steering | Chassis |
|---|---|---|---|---|---|---|---|---|
| 1★ | 193 | Tuner sport tyres | Ceramic brakes → Drilled two-pot | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Rally rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 2★ | 193 | Tuner sport tyres | Ceramic brakes → Drilled two-pot | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Rally rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 3★ | 196 | Tuner sport tyres → Tuner performance tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Rally rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 4★ | 204 | Tuner sport tyres → Tuner performance tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → High-comp twin-cam 2.0 | Twin turbo → Twin-scroll turbo | Six-speed box → Tuner six-speed | Rally rack → Tuner rally rack | Carbon frame → Half cage |
| 5★ | 206 | Tuner sport tyres → Tuner performance tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → High-comp twin-cam 2.0 | Twin turbo → Twin-scroll turbo | Six-speed box → Short-shift six | Rally rack → Tuner rally rack | Carbon frame → Half cage |
| 6★ | 211 | Tuner performance tyres → Tuner semi-slick | Drilled two-pot → Carbon-metallic kit | Twin-cam 16V 1.8 → High-comp twin-cam 2.0 | Big-single turbo → Twin-scroll turbo | Tuner six-speed → Short-shift six | Quick-ratio rack → Tuner rally rack | Strut-braced frame → Half cage |
| 7★ | 217 | Tuner performance tyres → Tuner semi-slick | Drilled two-pot → Carbon-metallic kit | Twin-cam 16V 1.8 → Tuner V6 3.0 | Big-single turbo → Twin-scroll turbo | Tuner six-speed → Short-shift six | Quick-ratio rack → Tuner wheel and rack | Strut-braced frame → Half cage |
| 8★ | 226 | Tuner performance tyres → Tuner semi-slick | Braided big brake → Carbon-metallic kit | High-comp twin-cam 2.0 → Tuner V6 3.0 | Twin-scroll turbo → Tuner twin turbo | Short-shift six → LSD six-speed | Tuner rally rack → Tuner wheel and rack | Half cage → Carbon panels |
| 10★ | 230 | Tuner performance tyres → Tuner semi-slick | Braided big brake → Carbon-metallic kit | High-comp twin-cam 2.0 → Tuner V6 3.0 | Twin-scroll turbo → Tuner twin turbo | Short-shift six → LSD six-speed | Tuner rally rack → Tuner wheel and rack | Half cage → Carbon panels |

**Class B, gravel** (each cell: the floor part → the ceiling part; a single name means the whole field runs it)

| Stars | Mean PP | Tyres | Brakes | Engine | Turbo | Transmission | Steering | Chassis |
|---|---|---|---|---|---|---|---|---|
| 1★ | 190 | Tarmac rally tyres | Ceramic brakes → Drilled two-pot | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Gravel rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 2★ | 190 | Tarmac rally tyres | Ceramic brakes → Drilled two-pot | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Gravel rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 3★ | 191 | Tarmac rally tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → Twin-cam 16V 1.8 | Twin turbo → Big-single turbo | Six-speed box → Tuner six-speed | Gravel rack → Quick-ratio rack | Carbon frame → Strut-braced frame |
| 4★ | 199 | Tarmac rally tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → High-comp twin-cam 2.0 | Twin turbo → Twin-scroll turbo | Six-speed box → Tuner six-speed | Gravel rack → Tuner rally rack | Carbon frame → Half cage |
| 5★ | 201 | Tarmac rally tyres | Ceramic brakes → Braided big brake | Valley V6 2.5 → High-comp twin-cam 2.0 | Twin turbo → Twin-scroll turbo | Six-speed box → Short-shift six | Gravel rack → Tuner rally rack | Carbon frame → Half cage |
| 6★ | 203 | Tarmac rally tyres | Drilled two-pot → Carbon-metallic kit | Twin-cam 16V 1.8 → High-comp twin-cam 2.0 | Big-single turbo → Twin-scroll turbo | Tuner six-speed → Short-shift six | Quick-ratio rack → Tuner rally rack | Strut-braced frame → Half cage |
| 7★ | 209 | Tarmac rally tyres | Drilled two-pot → Carbon-metallic kit | Twin-cam 16V 1.8 → Tuner V6 3.0 | Big-single turbo → Twin-scroll turbo | Tuner six-speed → Short-shift six | Quick-ratio rack → Tuner wheel and rack | Strut-braced frame → Half cage |
| 8★ | 217 | Tarmac rally tyres | Braided big brake → Carbon-metallic kit | High-comp twin-cam 2.0 → Tuner V6 3.0 | Twin-scroll turbo → Tuner twin turbo | Short-shift six → LSD six-speed | Tuner rally rack → Tuner wheel and rack | Half cage → Carbon panels |
| 10★ | 221 | Tarmac rally tyres | Braided big brake → Carbon-metallic kit | High-comp twin-cam 2.0 → Tuner V6 3.0 | Twin-scroll turbo → Tuner twin turbo | Short-shift six → LSD six-speed | Tuner rally rack → Tuner wheel and rack | Half cage → Carbon panels |

## 7 · What's next

- **Next session (Richard, 7 October): fix the AI so it doesn't crash at Hillcrest Raceway.** "Barriers don't solve the problem, they hide it." The corner barriers (built 7 October) only catch cars that have already left the road. The work is to find why AI cars run wide at the ~240 m and ~930 m corners, and stop it in the driving itself:
  - The old field did it too (4 resets at Valley Tuners).
  - So did the first DR 17–20 drivers, heavily.
  - The skill-scale field didn't (0 resets), but nothing has fixed the cause.

  Race with the barriers switched off (or count how often cars touch them) so the AI is judged on staying on the road.
- ~~The recipe as the default~~ (Richard, 7 October).
- Design the planned parts: Bramblewick's C parts, Highmoor's B gravel ladder, and ice tyres.
- Re-run `tools/field-table.py` whenever parts or planned races change; the build rewrites the data.
