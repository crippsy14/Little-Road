# 67 · Class B: races on today's tracks, and how equipment unlocks across the whole map (plan, 7 October)

Richard (7 Oct): "move onto setting up class B races in the three tracks that we have. We will have to map out exactly
what equipment is planned to be unlocked in what order over the course of the full map … consider the best approach to
unlocking equipment when going to tier B to prevent the feeling of having a world map but being forced to go the same
route through it to unlock parts. When the Tuner tier is unlocked, maybe we open a new Tuner garage which is available in
all major cities, but parts depend on results in general, then we just have to figure out how to spread the races out so
that there is a ramp in difficulty in a way that doesn't feel forced."

This is a plan for review. It builds on doc 44 (the parts ladder), doc 56 §21 (parts are discoveries), doc 58 (the world
and campaign) and doc 65 (the B cars, the free B car and paid kits).

## Where things stand (code, 7 October)
- **Parts.** `Resources/Data/parts.json` has 131 parts: C 40, B 22, A 23, S 22 and E 24.
  - B to E have three priced steps per slot, plus sidegrades (b_rally, a_wet, a_gravel, s_wet, e_economy, e_long,
    e_night).
  - There's no `step` field; Good/Better/Best is implied by price.
  - Only class C parts use home shops (`shop`) or unlock conditions (`unlock`).
- **Unlock tools already built** (`PartDiscovery`):
  - **Home shop:** a part is found at its home shop and then sold everywhere.
  - **Unlock conditions:** `wins:N`, `podiums:<class>:N`, `class:X` and `rallyschool`.
  - **Class filter:** shops list only your open class and below.
- **Races.** `RaceCalendar` has three events, all class C: Bayview Coast, Hillcrest Raceway and Quarry Hills Rallycross.
  - A podium at every C event completes C.
  - B opens only when it has races, and it has none, so the game stops at "Class C complete · new races and parts are
    on the way".
  - Fields are fixed C kits per event (`RaceRoster`). There's no layout reversing yet.

## The approach: one ladder, open everywhere, paced by results

### 1. The Tuner Garage
When class B opens, every main town's Pit Stop gains a **Tuner Garage** counter: the B purple and polished-alloy look
from doc 44, with its own sign. Today that's Bayview, Hillcrest and Quarry Hills; later, every B town.

Every Tuner Garage has:
- **The same catalogue**, so you never travel to buy the ladder.
- **Your free B car**, collected from any of them.
- **Kit conversions** of your C cars (doc 65: paid, and they let a C body take B parts).
- **The B bodies for sale.**

**Recommendation: the free car is a choice of three starters, one per drivetrain** (doc 65: "drivetrain is the
choice"):

| Starter | Drive | Character |
|---|---|---|
| Tsubasa R | FWD | Cheap, stable, brakes late |
| Sakura S | RWD | Light and playful |
| Kitsune GTI-R | AWD | Grip everywhere, including gravel |

### 2. Tuner Rep: parts unlock by results anywhere
Richard's "parts depend on results in general" becomes one number per class: **Rep**. Every B result anywhere earns it.
The Better and Best parts unlock at Rep thresholds. No single race, town or route is ever required.

**How Rep is earned:**

| Result | Finish | Podium | Win |
|---|---|---|---|
| Rep (× the event's field stars, 1–3) | 1 | 3 | 5 |

- **First-time results** at each event count in full.
- **Repeats** count a third, so grinding one race works but is slow, and trying a new place pays.
- **What's shown:** the Tuner Garage shows the bar and the next thing it unlocks ("Better tyres at 10 Rep: 4 to go").
  That follows doc 58 principle 9: show the lock before the key, and name the next goal.

**The B ladder.** All seven Good parts are on sale when B opens. Better and Best are staggered slot by slot, so a new
part arrives every few races (doc 58 principle 10: mix reward schedules, avoid cliffs).

| Rep | Unlocks (parts.json ids) |
|---|---|
| 0 (B opens) | All Good: engine_b_twincam, turbo Good, tyres_b_sport, transmission_b_six, brakes Good, steering Good, chassis_b_strut |
| 10 | Better tyres, Better brakes |
| 20 | Better engine (engine_b_highcomp), Better steering |
| 30 | Better turbo, Better chassis (chassis_b_halfcage) |
| 40 | Better transmission (transmission_b_shortsix) |
| 55 | Best tyres (tyres_b_semislick), Best brakes |
| 70 | Best engine (engine_b_v6), Best steering |
| 85 | Best turbo, Best chassis (chassis_b_carbon) |
| 100 | Best transmission (transmission_b_lsd) |

**Why this order:**
- **Tyres and brakes first:** they're cheap, and the change is felt at once in corners.
- **Power in the middle:** engine and turbo arrive mid-class.
- **The LSD gearbox last.**

**Pacing.** A typical B race earns about 5 Rep (a podium at a 1–2★ field), so 100 Rep is about 20 races. That matches
doc 44's "about 20 races to a podium build". Money still gates too, using doc 44's B prices.

**Sidegrades stay discoveries**, found at a home shop (doc 56 §21):
- **B rally tyres:** home shop is Bethan's at Quarry Hills, so you visit once and they're sold everywhere.
- **Later:** Highmoor's B specialities (doc 58 decision 13) and the Dive kit at Harbour Point (decision 6).

**Code needed:**
- an unlock form `rep:B:30` in `PartDiscovery`;
- a `step` field (or an id-ordered list) in parts.json;
- `AdventureProgress` keeping Rep per class, plus which event results have already counted.

### 3. Races everywhere, at every level: a ramp you choose
**Every race shows its field strength:**
- **Stars:** ★ to ★★★ within the class.
- **Ratings:** a field rating beside your car's rating, like Gran Turismo's recommended PP.

**Every town has an easier and a harder event,** so wherever you go there's something at your level, and the ramp
comes from your choices, not the map (doc 58 principle 13). Fields are fixed per event, never scaled to you (principle
14).

| Stars | AI field build (B parts) | Who it's for |
|---|---|---|
| ★ | Good in every slot | A new B car straight from the Tuner Garage |
| ★★ | Better in 4 key slots, Good in 3 (doc 44's competitive build) | Mid-class |
| ★★★ | Best in 4, Better in 3 (the podium build) | End of class; the championship |

Field cars are the B bodies (the 24 Tuners and the kits), with the off-road B bodies at the rallycross.

### 4. Class B on the three tracks we have
We have three tracks, so the cheapest way to six B events is to run each one forwards and reversed. Doc 44 already
planned "Hillcrest Raceway reversed".

**New tech: reversed layouts.** A circuit flag that reverses its spline and turns the grid round. About a day of work,
including checks that the AI's racing lines and every recovery point work backwards. After that, every future club
layout gets a reversed B event for free.

| Event | Track | Direction | Laps | Field | Pays (×) |
|---|---|---|---|---|---|
| Coast Tuners | Bayview Coast | Forwards | 3 | ★ | 1.0 |
| Harbour Reverse | Bayview Coast | Reversed | 4 | ★★ | 1.2 |
| Valley Tuners | Hillcrest Raceway | Forwards | 3 | ★★ | 1.5 |
| Raceway Reverse | Hillcrest Raceway | Reversed | 3 | ★★★ | 1.8 |
| Slate Rallycross | Quarry Floor (gravel) | Forwards | 4 | ★★ | 1.8 |
| Rallycross Reverse | Quarry Floor (gravel) | Reversed | 4 | ★★★ | 2.1 |

- **Payouts:** the class multiplier with the event scale, so a ★★★ podium is worth the drive.
- **Variety:** the rallycross pair is where the rally tyres and the off-road B cars matter. That's B's new thing, per
  principle 12: "B: gravel, jumps and the Dive kit".

**Later B events, as the map grows (doc 58: 23 B races before the credits):**
- the Castle Climb as a 12-car hill race;
- a Quarry Road point-to-point;
- the club layouts at Harbour Point and Summit Pass;
- Ridgeway Circuit;
- Lowtown.

They slot straight into the same star scheme.

### 5. Finishing class B without a fixed route
Today a class completes with a podium at every event. With 23 B races eventually, that turns into a checklist and forces
the hardest races. **Recommendation:**
- **The Tuner Championship:** a three-round finale, one round per town on the ★★★ events.
- **When it opens:** with 60 Rep and podiums at any four different B events.
- **Winning it completes class B.**

That gives the class a finale, and before it the player picks their own way.

## The whole map: what unlocks where and when (all classes)
Every class repeats the same pattern, so players learn it once:
- the class's own garage opens everywhere;
- Good parts are on sale at once, then Better and Best arrive by class Rep;
- a few specialities live in named places;
- the finale completes the class.

| Class | How it opens | Garage (everywhere) | Catalogue (by class Rep) | Specialities and discoveries (home shop, then everywhere) | Finale that completes it |
|---|---|---|---|---|---|
| **C Street** | New game | Pit Stops: Sid, Rosa, Bethan | C ladder: 21 parts (today some sell everywhere from the start; moving them onto Rep is optional) | Hillcrest's eight valley parts (Rosa); Quarry gravel kit (Bethan, rally school gold); Bramblewick's local parts (doc 58) | Today: a podium at all three C events. With more C events: a Street Championship. |
| **B Tuner** | Class C complete | **Tuner Garage**: free starter, kits, B bodies | B ladder: 21 parts by Tuner Rep (table above) | B rally tyres (Quarry Hills); Highmoor's B parts; Dive kit (Harbour Point) | Tuner Championship |
| **A Touring** | B complete: **the frenzy** (teams, pro layouts) | **Works garages** at the team HQs | A ladder: 21 parts by Touring Rep, plus fuel, tyre wear and pits (doc 44) | A wet and gravel tyres (no local A shops: doc 58 decision 13) | Grand Tour A, with results across the pro layouts |
| **S Racing** | A complete | **Race shops** | S ladder by Racing Rep | S race-wet tyres; Copper Gulch's S parts | Grand Tour S; Endurance entry earned |
| **E Endurance** | S complete | Aurora's paddock | E ladder by the 10 build-up races | Economy hybrid, long box, night tyres; Icefall Hut (E-ice) | The Aurora Endurance against Duke Rolo, then the credits |
| **Post-game** | Beat Rolo | | | The one set of secret parts (Lowtown) | |

Platforms (doc 44 §2) fit in like this:
- **Class B:** the free starter and the kit conversions replace the B platform purchase (doc 65).
- **Classes A to E:** whether each opens with a free car plus kits is a decision for when we get there.

## What race cards and screens show (for these B races)
- **The race card** shows:
  - the field stars and field rating against your car's rating;
  - the surface (tarmac or gravel);
  - the laps;
  - the pay;
  - the Rep on offer ("Podium: +6 Rep").
- **The results screen** shows the Rep earned and the next unlock, and names the next goal and where it is (principle 9).
- **The Tuner Garage** shows the Rep bar, the ladder, and locked parts with their Rep ("Best engine at 70 Rep").

## Build order (once agreed)
1. **Reversed layouts** for the three circuits, with checks (about 1 day).
2. **Data:** step order in parts.json; `rep:B:N` unlocks; Rep and first-results in the save; the B multiplier and
   event scales.
3. **B race events:** the six events in `RaceCalendar`, ★ to ★★★ B AI builds in `RaceRoster`, race-card text, and the
   class-complete rule (the championship).
4. **The Tuner Garage counter** in each Pit Stop: catalogue, starter choice and kit conversion.
   - The B bodies need exporting to the game first: `CarBodies` entries, collision envelopes and LODs (doc 65).
5. **Keep/reject races** for the B fields: all 24 finish, no new assisted resets. Then balance Rep and prices on the
   harnesses.

Spotted along the way: Gio's race button at Hillcrest says "Valley Cup · B", but it's the class C race. The fix belongs
in step 3.

## Decisions for Richard
1. **Rep:** one results number per class that unlocks Better and Best, earned at any race. Or a different gate?
2. **The starter:** a choice of three B cars (FWD Tsubasa, RWD Sakura, AWD Kitsune), or one fixed car?
3. **The six B events:** the three tracks forwards and reversed, each town with one easier and one harder field. OK to
   build reversed layouts first?
4. **Finishing B:** the Tuner Championship (opens at 60 Rep and podiums at any four events) instead of a podium at every
   event?
5. **Field stars and ratings:** show them on race cards?

## Richard's decisions (7 October)
1. **Rep is the total stars of the events you've won**, each event once. Beating a harder race through skill moves you
   on faster. (This replaces the finish/podium/win points above.)
2. **The free B car is a choice of three starters.**
3. **The six B events** on today's three tracks, forwards and reversed. The reversed layouts give room to do whatever
   you like without being a completionist.
4. **The Tuner Championship:** three races, harder opponents, slightly longer races. The tracks get a "tuner event"
   uplift for it: new decorations, cheering crowds and fireworks.
5. **PP per car on show.** Bands of the field's average PP set an event's star difficulty, and the race shows only its
   stars, so the two mechanics stay separate.
6. **Stars run 1–10 over the whole map.** The championship is the 10★ event; the rest of the map is 1–8★.
7. **The Tuner Garage needs reasonable steps in stars**, so cleaning out three locations can't unlock most of the
   ladder.

### What that makes the numbers
**Stars across the B map.**
- **Today:** the six events on today's three tracks are the early B towns, at 1★ to 6★.
- **Later:** B towns further out (Harbour Point, Summit Pass, Highmoor, Ridgeway, Lowtown) carry the 4★–8★ events.
- **Planned B map** (doc 58: 23 B races): about 95 stars in all, and the championship needs about half of that.

**The ladder by stars won.** Thresholds are set against the whole B map, so today's six events (21 stars) open the
Better parts and the first Best pair, not most of the ladder.

| Stars won | Unlocks |
|---|---|
| 0 | All seven Good parts |
| 3 | Better tyres, Better brakes |
| 7 | Better engine, Better steering |
| 12 | Better turbo, Better chassis |
| 18 | Better transmission; the B rally tyres can be found at Quarry Hills |
| 26 | Best tyres, Best brakes |
| 36 | Best engine, Best steering |
| 48 | Best turbo, Best chassis |
| 60 | Best transmission |

**The Tuner Championship:**
- **When it opens:** 40 stars won, including a win at a 6★ event.
- **Before the B towns exist:** while only today's six events exist (21 stars), it opens at 15 stars plus the 6★ win.
  This is a data row to retune as B towns arrive.
- **The races:** three rounds (Bayview, Hillcrest, Quarry Hills), each a lap longer than that track's B event, with
  ★★★-plus fields at the top of the B PP band.
- **The dressing:** a switchable "tuner event" layer per track (banners, cheering crowds, fireworks at the finish),
  built the doc 58 frenzy way, so it shows only for the championship.

**PP bands for the stars.** A car's PP is `CarPerformanceRating` (already computed for every entrant). The event's
stars come from its field's average PP:
- 1★ starts at a field on B Good parts;
- 8★ is a field on the B podium build;
- 10★ is beyond that, the best drivers on the podium build;
- the bands are fixed in data and checked by a test.

## Build status (7 October, paused at Richard's request)
**Done, uncommitted, and compiling** (`tools/compile-check.ps1` passes):
- **Reversed layouts.** `Circuit.Reversed()` (point 0 stays the start line); `RaceRoster.Reversed` and the saved
  evidence; RaceTrial flips the track after building the world and bakes a fresh line; `--trial-reverse` for trials.
  `run-frame-cost.ps1` and `run-technical-trial.ps1` now accept `hillcrest` and `quarry`.
  - **Study 220** (forwards against reversed, three tracks, seeds 1709–1711, 24 cars, one lap): all 18 races finished
    24/24 with no assisted resets.
  - Total finish times for the 24 cars: Bayview 923.7–944.6 s reversed against 916.1–929.4 s forwards; Hillcrest
    2001–2019 s against 1990–1997 s; Quarry 1567–1576 s against 1578–1597 s.
- **The calendar** (`RaceCalendar`): events carry track, stars, laps and direction. The six B events are Coast Tuners
  1★, Valley Tuners 2★, Slate Rallycross 3★, Harbour Reverse 4★, Rallycross Reverse 5★ and Raceway Reverse 6★.
  - Class C completing now opens class B.
  - `StarsWon` adds up the stars of the events won.
  - B doesn't complete yet; that's the championship.
- **Saves:** `wins` (event ids) records first wins; older saves get an empty list.
- **Parts:** the 14 Better/Best B parts carry `stars:B:N`; B rally tyres have home shop Quarry Hills and need 3★.
  - `PartDiscovery`: parts sold everywhere with a condition stay locked, with the condition shown, until it's met.
  - `NextStars` gives the next target.
- **Fields:** `TestLoadout("b1".."b10")` gives a field by stars. `RaceRoster.AdventureTuner` builds a B event's field:
  front rows a star up, back rows a star down, rally tyres at Quarry, C bodies until the B bodies are exported.
- **Garages:** Mara (Bayview) offers her town's Tuner races once B opens, and Huw (Quarry Hills) has a "Tuner races"
  menu. The race preview shows the event, class and stars. Gio's button now says "Valley Cup · C".
- **The results notice** shows the stars won and the next Tuner part's target.
- **Editor checks** (`AdventureChecks`) are updated for B: C opens B; Good on sale; 3★ unlocks Better tyres; field kits.

**Not yet tested in a build:** the B events themselves (no B race has been run in the game yet), the garage menus and
the preview header.

**Next, in order:**
1. **Build and run:** the editor checks, the regression, and one adventure B race per town.
2. **Gio's menu:** add Hillcrest's "Tuner races" (Gio and Vito), and Gwen at Quarry Hills.
3. **The Tuner Garage:** export the B bodies (CarBodies entries, collision bands, LODs), then the starter choice of
   three and the kit conversions.
4. **The Tuner Championship** (10★): three rounds with points across a fixed field, one lap longer, and the "tuner
   event" dressing (banners, crowds, fireworks). It completes class B.
5. **Balance:** keep/reject races for the B fields at each star level, and tune prices and payouts.

## The B map: every planned race and its stars (8 October)
Richard (7 Oct): "a more elongated increase in stars to cover our whole planned map".

**Source.** The world blueprint's layout B catalogue (https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb; doc 58 §3–4):
- every main town has one club race for class B, and the B towns (Harbour Point, Summit Pass, Coral Key) have two;
- the B villages and Lowtown have their own;
- that's 23 races, plus today's three towns raced reversed: **26 events**.

**How the stars were set.** Stars rise with distance from Bayview and with what it takes to get there: Coral Key and
Lowtown need the Dive kit from Harbour Point. A town with two events has an easier and a harder one.

| Stars | Events (place · track) | Distance from Bayview |
|---|---|---|
| 1★ | Coast Tuners (Bayview · Harbour Loop); Valley Tuners (Hillcrest · Raceway) | 0–1 km |
| 2★ | Harbour Reverse (Bayview); Slate Rallycross (Quarry Hills); Vineyard Loop (Vinelands) | 0–5 km |
| 3★ | Raceway Reverse (Hillcrest); Rallycross Reverse (Quarry Hills); Tollgate, Highmoor and Millbrook (villages) | 1–6 km |
| 4★ | Quayside Sprint (Harbour Point); Reef Street Circuit (Coral Key, Dive kit); Dunmore Junction | 4–9 km |
| 5★ | Quayside Sprint, the harder round (Harbour Point); Summit Climb (Summit Pass); Waterfront Circuit (My City) | 8–9 km |
| 6★ | Reef Street Circuit, the harder round (Coral Key); Forest Sprint (Pinewood Coast); Ridgeway Circuit | 4–12 km |
| 7★ | Summit Climb, the harder round (Summit Pass); Lowtown (secret, Dive kit); Canyon Sprint (Redrock Mesa); Docks Street Race (Port City) | 8–17 km |
| 8★ | Ridgeway Circuit, the harder round; Frozen Lake Oval (Frostcrown); Aurora Club Circuit | 12–19 km |
| 10★ | **The Tuner Championship** (three rounds) | |

**Totals.** 120 stars on the map. Today's three towns are 12 of them (10%).

**The ladder** (`stars:B:N` in parts.json; the Good step is on sale when B opens). Each threshold is a share of the 120
stars, so the ladder fills in across the whole class:

| Stars won | Share of the map | Unlocks |
|---|---|---|
| 4 | 3% | Better tyres, Better brakes; B rally tyres (found at Quarry Hills) |
| 10 | 8% | Better engine, Better steering |
| 18 | 15% | Better turbo, Better chassis |
| 28 | 23% | Better transmission |
| 42 | 35% | Best tyres, Best brakes |
| 58 | 48% | Best engine, Best steering |
| 76 | 63% | Best turbo, Best chassis |
| 95 | 79% | Best transmission (the LSD six-speed) |

**What this means in play:**
- **Today's map:** winning all six of today's events (12★) opens the first two rungs. The Better gearbox needs the
  first wins out of the home region, and the last Best parts need wins well out in the map.
- **Nothing's compulsory:** about a fifth of the map's stars can be left unwon and the ladder still completes.

**The Tuner Championship** opens at 80★ including a win at an 8★ event.
- **Its races** (Richard, 8 Oct): three races, at three towns with class B races, which we'll decide later. Any planned
  B town can host a round, not only today's three.
- **Building it** waits for its towns. A test flag can open it early.

**Pay** rises 15% a star (rewardScale = 1 + 0.15 × (stars − 1)).

**Fields** come from `TestLoadout("b<stars>")`: a 1★ field is on Good parts and an 8★ field is above the podium build.

## The planned world, on tap (8 October)
Richard asked for a way to reach the planned towns quickly and to keep the map to hand.
- **The world blueprint** is pinned in his claude.ai sidebar.
- **`docs/data/world-plan.json`** is the blueprint's data in the repo: 33 places, 27 tracks, 2 Grand Tours. It's
  extracted by `tools/world-plan-extract.py`.
- **`docs/data/race-stars.json`** has the planned stars per event.
- **`tools/world-plan.py`** prints breakdowns: per class, the race catalogue with stars, one place, all places. Its
  totals match doc 58: C 21, B 23, A 29, S 26, E 10, plus 12 post-game.
- **The `world-plan` project skill** (`.claude/skills/world-plan/`) points any session at all of this.

## Build status (8 October)
**Done today** (uncommitted, built, 2,910 editor checks passing):
- **Stars over the whole planned map** (the section above): today's six events are now 1–3★, and the ladder's thresholds
  run 4–95★ as shares of the map's 120★.
- **All six B events raced in the game** (`tools/event-race-review.ps1`, which uses the new `--event-race-review <id>`
  hook): no assisted resets and no exceptions; Harbour Reverse showed 23 finishers only because the script closed while
  one car was still running.
  - The player's test save is on class C parts, so it finished last in every race, as expected against B fields.
  - Before the new bodies, one Raceway Reverse run had one assisted reset (car 4, about 125 s). It didn't recur.
- **The Tuner bodies are in the game:** 21 road Tuners and 6 off-road B bodies exported (FBX and LODs in
  `Resources/Cars`, .blend in `source-art/family`), registered in `CarBodies` (Tuner, TunerOffRoad, TunerGravel)
  with measured shoulders and lengths.
  - B fields deal them: road Tuners on tarmac; off-roaders plus Kumo, Fubuki, Taiyo and Kitsune on gravel.
- **Hillcrest's Tuner races:** Gio, Vito, Huw and Gwen all offer "Tuner races".
- **The Tuner Garage:** every Pit Stop's garage bay, once B is open.
  - **The free Tuner:** a choice of Tsubasa R, Sakura S or Kitsune GTI-R, claimed once, with the seven Good Tuner parts
    fitted.
  - **Bodies:** all 27 Tuner bodies for sale (5,500–14,000; the Shiro GT is the top prize).
  - **Fitting:** a body has a class (`CarBodies.Class`). Tuner parts fit Tuner bodies only, and swapping back to a
    class C body refits its slots with the best class C parts owned.
- **Editor checks** cover the starter, fitting by body class, the refit, and the B prices.

**Still to come:**
- The C car kit conversions (once the kit treatments are chosen).
- The Tuner Championship (three races, towns to choose).
- Balance at each star level (keep/reject races for each B field, prices and payouts).
- A look at the Tuner Garage and race preview screens in a rendered capture.
