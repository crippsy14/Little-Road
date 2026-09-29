# Roadmap and feature spec: classes, racers, teams, world

Written 29 September 2026 as the project plan Richard asked for. It covers:
- the class ladder, platforms and parts ([doc 44](44-parts-tiers-and-progression.md));
- named racers with abilities shown as a radar chart;
- racers you find around the world;
- teams from class A, with experience and potential;
- fuel, tyres and pits from class A;
- growing the map and tracks.

It sets the order so that early work is never torn up later, while each feature starts as a thin slice we can feel and tune before committing to its full form.

**Campaign length:** at least 12 hours and open-ended. 12 hours is a floor, not a cap; every region, class and activity added makes it longer, and that's welcome.

It supersedes the "later" notes in [doc 20](20-driver-personality-and-teammate-progression.md) and [doc 21](21-performance-rating-and-race-preview.md) where they overlap, and keeps their principles.

## How we decide the order

1. **Fix the things that are expensive to change, early and small.** These are:
   - save format and IDs;
   - the single racer record that both rivals and teammates use;
   - track design standards (grid length, width, pit lane space);
   - how much of the world map each future region takes.

   Getting these wrong later means reworking finished areas. Getting them right now costs days, not weeks.
2. **Build seams before depth.** Every feature first lands as a data definition plus a thin working slice behind a flag, for example one teammate in one test race. We feel it, then decide how deep to go. Nothing is fleshed out before we know it works.
3. **Graybox before art.** Every track and region goes: plan on the world map → graybox roads and track → event and AI validation (baselines) → town, shops and people → art and dressing → viewpoints and discoveries. Art never lands on a layout that might still move.
4. **One class of campaign at a time.** Finish playable class C before class B content. Later-class systems (teams, fuel) can be prototyped early on test tracks, never on campaign content.
5. **Every feature ships with its test.** Harnesses we already have: smoke, pace study, class-sort, parts review, frame probe, contact review. Each new feature gets its review mode and editor checks, and behaviour changes to the AI keep the keep/reject protocol.
6. **Only show what's real.** A radar axis, a stat or a level appears only once the behaviour behind it exists (doc 38's rule).

## The features

### F1. Racer identity: one record for every driver

Every racer in the game (rivals, recruitable drivers, teammates) is one persistent **Racer** record, replacing today's 24 ad-hoc trial identities:
- **Who:** id, name, portrait or livery colours, home location, a short bio line.
- **Personality:** Aggression 0–10 (today's "risk"), Patience.
- **Abilities:** their current value and potential cap for each (F2). Rivals never grow; teammates do.
- **Car preferences:** preferred bodies and livery, used when the game builds their car for an event.
- **Recruitment:** where they can be found and what they require (F5).

Rivals persist across events: the same Ravi Moss shows up in Bayview and Hillcrest races with the same abilities, so you learn who's who. Race rosters are drawn from a region's racers plus travelling rivals.

- **Data:** `Resources/Data/racers.json` (replaces the driver identity asset). The save stores met/found flags, teammate XP and current abilities per racer id.
- **First slice:** convert the existing 24 identities into Racer records with names, home towns and today's three real traits. The preview and results screens show names you'll recognise.

### F2. Driver abilities as a radar chart

Instead of one "risk" slider, each racer has **six abilities (0–100)** drawn as a radar, plus **personality tags** shown beside it. Aggression and patience describe style, not skill, so they're not on the radar. Each ability controls a real part of the AI; that's the rule that makes the chart honest.

| Ability | What it changes in the AI | Status |
|---|---|---|
| **Braking** | How late and how accurately they brake (braking judgement: usable-braking estimate) | Exists (prototype 43/86) |
| **Consistency** | Spread of race-day form and corner-to-corner variation | Exists (86) |
| **Line** | How close to the ideal line they drive: apex accuracy, track use, corner speed within real grip | To build |
| **Control** | How near the grip limit they drive and how well they recover from slides and bumps | To build |
| **Racecraft** | Spotting and committing to clean passes, and defending position; aggression sets how often they try | Partly exists (risk → passing space) |
| **Strategy** (class A+) | Tyre and fuel management, pit timing, fuel-mix choice | Arrives with F9 |

- **Overall rating:** the mean of the abilities that exist, shown as one number, like the car's performance rating. Car rating and driver rating always stay separate: a great driver can't beat physics.
- **The chart:** a filled polygon for current ability, plus a ghost outline for potential on teammates and scouted racers. Axes that don't exist yet show greyed ("coming").
- **Order:** re-present the three real traits on the radar first. Then Line and Control, each one AI study under the keep/reject protocol. Racecraft grows out of the risk work. Strategy comes with F9.
- **The spread of rivals is authored:**
  - class C rivals mostly 35–60;
  - class A rivals 55–80;
  - a few standout rivals per region with one outstanding ability, for example a braking specialist.

### F3. Teams (from class A Touring)

**Shape:**
- **Team:** you plus up to two racers you've found. Each class from A has its own team (A, S, E), because a racer's potential decides how far up the ladder they're useful.
- **Arrival:** teams arrive with class A as part of "you've made the professional league". The team menu lives in the garage (the Mara/Gio fitting bay gets a TEAM tab).
- **Managing:** for each team seat you can see their radar, level, car and form; change their parts and body; remove them; or **Call** a replacement.
- **Call** lists every racer you've found (F5), their availability and requirements. Anyone who's been in a team keeps their experience, so you can bring back an old teammate at any tier.
- **Teammate cars** are built from your garage. You buy parts for them, one copy per car, and fitting is free as ever. That's the late-game money sink that makes earnings matter. Default suggestion: start with duplicates of your cheapest parts.
- **In the race:** teammates are two of the 24 entrants. They race with the same AI as anyone else, driven by their own abilities, with no team orders at first.
- **Rewards:** you get a share of each teammate's payout (suggested 50%). Class A adds a team standings table per series.

**First slice (prototype early, on a test track, behind `--team-prototype`):** one found racer as a teammate in a C test event. Shows a garage TEAM tab, the racer's car and parts, and their radar on the preview and results screens. This checks the AI side (two friendly cars in one field), the grid and the UI before any content depends on it.

### F4. Experience, levels and potential

- **Earning XP:** teammates earn XP per race from distance raced, result against their expected place, and clean laps. It tapers if the race is far below the class they're in, so you can't farm easy races. Rivals don't level.
- **Growth:** each racer has a growth profile (fast or slow learner, which abilities grow first) and a **potential cap per ability**. XP raises abilities toward the cap and never past it. That's why exploring for better racers matters: a local hero may cap at 60 while a racer hidden in the mountains caps at 90.
- **Levels:** a readable 1–20 that summarises progress; the radar shows the actual abilities.
- **Track familiarity** (doc 20) comes later and small: per-track comfort that adds a little to Line on that track, only for the player's team.
- **First slice:** XP and growth on the prototype teammate. Tune the rates by feel on a handful of races before any numbers are fixed.

### F5. Finding racers around the world

- **Where they live:** recruitable racers are people in the world, like the residents today, placed across all regions and often off the main road. That gives the empty fogged areas a purpose.
- **Collecting them:** talking to one adds them to your **Contacts** (a pause-menu page with radar, potential, home and requirements). You can collect them in class C, long before teams exist, so the world has them from the start and class A pays off what you gathered.
- **Requirements** can be any of:
  - races completed;
  - podiums in a class;
  - beating them in a race;
  - reaching a class;
  - a small favour or delivery.

  Each shows as a tracker on their card ("Prove yourself: 3 podiums, 1/3").
- **Better racers are harder to reach:** harder to find, with bigger asks.

### F6. Classes, parts and trackers

This is [doc 44](44-parts-tiers-and-progression.md) as decided:
- **Classes:** C Street, B Tuner, A Touring, S Racing, E Endurance.
- **Parts:** 3 steps per slot per class plus sidegrades.
- **Unlocks:** class C covers all the early regions; class B opens globally when class C is complete.
- **Location parts:** each location's parts appear when it's discovered, some behind results trackers that can be met at any event.

### F7. Chassis platforms and body kits

- **Platform:** the class gate. It sets the class, the bodies that fit and which parts can be fitted. The **Frame** slot keeps the in-class weight ladder.
- **Bodies per platform:** new bodies plus kit versions of favourites (Puddle → Puddle Tuner → Puddle Touring).
- **Collision:** per-body boxes keep collisions honest; bodies on one platform share a size band.

### F8. Fuel, tyres and pits (class A, extended in E)

- **Fuel mix:** a GT-style setting from 1 to 6, changeable in the race. Burn = throttle × delivered power × mix.
- **Engines:** each gets an efficiency figure.
- **Tyres:** compounds with their own wear rates, worn faster by load and sliding.
- **Pits:** real pit lanes (speed limit, box, service time).
- **Strategy AI:** its own AI study track.
- **Test track first:** the systems and the strategy AI are built and tuned on one test track with a pit lane, during the class B phase, so class A content lands on a working system.

### F9. World and tracks

The world grows region by region. **Decide the world's footprint now**:
- **The problem:** the fog-of-war mask and the map bake are tied to the 2.5 km map square, so growing the map later would reset exploration.
- **Recommendation:** fix a 10 km × 10 km world grid now, with the map baked per region tile and the explored mask stored per tile. Then new regions slot in without resetting anyone's fog.
- **Placeholders:** reserve each future region's rough area on it now (placeholder ground at sea level). The first slice is the per-tile storage and baking, with today's content as tiles.

**Track standards** (editor checks, like the grid check):

| Class | Racing width | Behind the start line | Pit lane | Run-off |
|---|---|---|---|---|
| C, B | 10–12 m | ≥ 95 m of straight or gentle curve (24 cars at 11 m rows) | Not needed | Grass and gravel |
| A, S | 13–15 m | ≥ 95 m | **Required**: reserve a 10 m lane plus boxes from graybox onwards | Proper run-off, barriers |
| E | 14–16 m | ≥ 95 m | Required, longer lane, night lighting | As A/S |

**Regions and events** (numbers rough; each region runs through the graybox → event → town → art → discoveries steps):

| Order | Region | Class | Venues and events | New in this region |
|---|---|---|---|---|
| Done | Bayview | C | Bayview Coast | Home, first Pit Stop |
| Done | Hillcrest Valley | C | Hillcrest Raceway, Castle Climb | Viewpoints, zipline |
| 3 | Quarry Hills | C | Quarry Link (gravel), Ridge Run | Gravel tyres matter; 2–3 racers to find |
| 4 | Harbour Point (small coastal town) | C | Harbour street sprint, Lighthouse loop | Local parts behind trackers, one standout rival |
| — | Class B opens globally | B | Reversed and extended versions of the C tracks, plus a new region | Platforms, body kits |
| 5 | Summit Pass (mountain) | B | Hill climb, pass sprint, downhill | Hardest-to-reach racers |
| 6 | Port City (touring league) | A | Touring circuit (full and club layouts), night street race; pit lanes | Teams, fuel and tyres |
| 7 | GP Park | S | GP layout, national layout, wet race | Aero-era bodies |
| 8 | Endurance Ring | E | 45- and 90-minute races, day-to-night finale | Long strategy |

### F10. Discoveries in the fog

Viewpoints (big reveals), activities (time trials, deliveries, photo spots), collectibles and recruitable racers go into the empty areas of each region. They're added in each region's last step, once roads are final.

## The order of work

Each phase ends with a playable build Richard can feel, the smoke and pace tests passing, and a short review doc.

**Phase 0: Foundations** (fixes the expensive-to-change things, small):
1. **World grid:** the per-tile map bake and explored mask (F9 first slice).
2. **Racer records:** `racers.json`, and the 24 identities converted to named Racers (F1 first slice).
3. **Radar chart** in the race preview and results, showing only the 3 real abilities (F2 first slice).
4. **Launch fix:** a traffic-aware AI launch (or a tighter grid) against study 128's start hits (flagged study).
5. **Save and IDs:** a versioned save for racers (found, XP, abilities) and platforms, with migration tests.
6. **Track standards:** as editor checks.

**Phase 1: Class C campaign complete** (content plus AI depth):
1. **Class C parts ladder** (doc 44): re-price, rename the frame, add location specialities and trackers.
2. **Contacts and recruitable racers** placed in Bayview and Hillcrest (F5). You can collect them now; they're used later.
3. **AI: Line ability**, then **Control** (each a keep/reject study), shown on the radar when kept.
4. **Quarry Hills** (region 3), then **Harbour Point** (region 4), through the region steps; class C events there.
5. **Prototype, not campaign:** the `--team-prototype` teammate in a C test event, plus XP and growth (F3 and F4 first slices). Tune by feel.
6. **Economy pass for class C:** time to a competitive and a podium build, and total C hours measured in play.

**Phase 2: Class B**
1. **Platforms and body kits** (F7): the C→B platform purchase, and Puddle Tuner and friends.
2. **Class B parts ladder**, the global B unlock, and B events on reversed and extended C tracks.
3. **Summit Pass** (region 5).
4. **AI: Racecraft** grown from the risk work (passing and defending).
5. **Fuel, tyre and pit prototype** on one pit-lane test track, plus the strategy AI study, to be ready for class A (F8).

**Phase 3: Class A (the professional league)**
1. **Teams for real:** garage TEAM tab, Call, parts for teammates, team payouts and standings (F3 full).
2. **XP, levels and potential** tuned from the prototype (F4 full); the first track familiarity.
3. **Fuel, tyres and pits** on in class A events; Strategy on the radar.
4. **Port City** (region 6) with pit-lane circuits.

**Phase 4: Class S.** GP Park, aero-era bodies and platforms, class S teams, stronger rivals, bigger recruit requirements.

**Phase 5: Class E.** Endurance Ring, long races, night, multiple stops, strategy variety, class E teams.

**Throughout every phase:**
- a performance budget (frame probe, including a race);
- the contact review and grid checks for new bodies and tracks;
- a playtest gate with Richard;
- balance passes using the pace, class-sort and proving-ground harnesses.

## Decisions I suggest we fix now (Phase 0)

1. **World grid of 10 km × 10 km with per-tile maps and fog.** Yes or no? This one matters most to settle early.
2. **One Racer record** for rivals, recruits and teammates, with the six-ability radar as the target (three abilities real today).
3. **Teammate cars** built from parts you buy for them (one copy per car), and a 50% team share of their payouts.
4. **A team per class from A**, where any found racer can be called at any tier and keeps their experience.
5. **Track standards** as above: 95 m behind the line, and pit lanes reserved from graybox for A+.

## Open questions to settle when we get there (no need now)

- **Team orders or strategy calls** (swap positions, hold, push)? My suggestion: not until class S, if at all.
- **Rival growth:** should rivals grow over the campaign (a rival "career"), or stay fixed as the class ladder already scales them?
- **Familiarity:** whether it applies to rivals, or to the player's team only.
- **Weather** (wet races) in class S. It needs wet tyres, AI wet skill and visuals; a separate spec when we reach it.
