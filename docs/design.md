# Little Road: the game design record

Every game decision, direction and design rule still in force, in one place. Richard's own words are quoted with the
date. "My call" marks a decision Claude made under the design freedom Richard gave on 20 Oct. The world and its places
are in [world.md](world.md), process and tools in [how-we-work.md](how-we-work.md), and the live status in
[next-steps.md](next-steps.md).

## 1 · Vision and tone

- An original car adventure inspired by Road Trip Adventure / Choro Q HG 2: explore a toy-like world, meet the people
  who live there, race, and build up a car (and later a team) you care about. Windows, offline, single player.
- **Toy shapes, real light** ([§11](#11--look-toy-shapes-real-light)). Toy proportions and comic wear, never grim; real
  late-afternoon light; rich mid-value colour, not pastel (Richard, 28 Sep: more realism and detail, "less pastel").
- **A world of cars** (Richard, 20 Oct): no human figures anywhere. Residents, crowds and marshals are cars.
- **The campaign** is at least 12 hours and open-ended (Richard, 29 Sep): prove yourself, earn entry to the Aurora
  Endurance, beat Duke Rolo ([world.md](world.md) §1).
- **Design for the finished game** (Richard, 14 Oct): "When considering game mechanics, please consider the game as it
  would be in the finished state, not the current one."
- **Be bold** (Richard, 28 Sep): rewrite an approach when it blocks a feature (the photo sky became a procedural sky for
  day and night); model the real part, not a stand-in line.
- **Fair racing:** no rubber-banding, no hidden speed boosts, no rivals scaled to the player's purchases. Difficulty comes
  from equipment, lines, braking, traffic and the track.

## 2 · Classes and the career

| Class | Name | Cars | Design speeds (m/s) |
|---|---|---|---|
| C | Street | your first car, local bolt-ons; family cars and bangers | 24–40 |
| B | Tuner | aftermarket parts, track days; as many unique forms as possible | 38–50 |
| A | Touring | works touring cars on real circuits; road sports cars and supercars | 48–60 |
| S | Racing | GT racers, sports racers, open-wheel, rally, every discipline | 58–74 |
| E | Endurance | Le Mans-style prototypes and GTs, day into night | 62–80 |

- **The five classes** were confirmed by Richard on 29 Sep. New regions are designed for their class's speeds.
- **Opening chain:**
  - B opens when all of class C is complete (Richard, 29 Sep). Today C is complete with a podium at every C event; a
    Street Championship can replace that once C has more events.
  - A opens when B is complete. This is "the frenzy": teams, pro layouts, works garages.
  - S opens when A is complete, and E when S is complete: Aurora's ten build-up races, then the Endurance against Duke
    Rolo and the credits.
  - The post-game follows the credits.
- **Each class repeats one pattern** (7 Oct): its garage opens everywhere (B the Tuner Garage, later classes their own
  counters and works garages at team HQs), its Good parts are on sale at once, Better and Best arrive with the stars won
  in that class, a few specialities sit in named places, and a finale completes the class.
- **Gating** (Richard, 30 Sep): a class opens only once it has races. Shops list only the open class and below, with no
  locked peek, and menus never name the next class. A story explaining unlocks ("achieve x and y") is a much later goal.
- **Stars** (Richard, 7 Oct): every event has a star rating from 1 to 10 across the whole map. Class finales are 10★,
  everything else 1–8★. Stars come from the field's strength, rise with distance from Bayview and access cost, and are
  fixed per event, never scaled to the player. Race cards show the field's stars, its rating against your car's, the
  surface, laps, pay and stars on offer. The planned stars per event are in `docs/data/race-stars.json`
  (C 21 races, 89★; B 26 events, 120★).
- **Class B** (Richard, 6–7 Oct):
  - **Getting in:** unlocking B gives one free car, a choice of three starters, one per drivetrain (Tsubasa R FWD,
    Sakura S RWD, Kitsune GTI-R AWD), fitted with the seven Good Tuner parts.
  - **Kit conversions:** owned C cars convert to B kits for a fee (the money sink; not built yet). Only a B body takes
    B parts. This replaced buying a class platform for B. Whether A to E get a platform purchase or a free car plus kits
    is decided when each class is reached (OPEN).
  - **Rep:** the total stars of the events you've won, each event counted once, so skill into harder races moves you
    faster. Rep unlocks the Better and Best parts (§5).
  - **The Tuner Championship** is the 10★ finale: three races, harder fields, slightly longer, with "tuner event"
    dressing (banners, crowds, fireworks) shown only for it. It opens at 80★ including a win at an 8★ event, at three
    B towns to be chosen later (Richard, 8 Oct). Winning it completes class B.
  - **Today's six B events** run on the three towns' tracks, forwards and reversed: Coast Tuners and Valley Tuners 1★,
    Harbour Reverse and Slate Rallycross 2★, Raceway Reverse and Rallycross Reverse 3★. Vinelands adds Cumeada Tuners (2★).
- **Fuel, tyre wear and pits arrive with class A** ("you've made the professional league"; Richard, 29 Sep). B stays pure
  driving and upgrades. Pit-lane strategy AI is built and tested on one pit-lane test track during class B.

## 3 · Races, pay and fairness

- **24 cars a race** (the player and 23 rivals), each with its own identity, colour, loadout and driving profile. Races
  are 2–3 minutes in class C, usually 3 laps. Grid rows of three, 11 m apart, pole car's nose on the line.
- **Where you finish** (targets, never coded): stock car 18th–24th; one sensible upgrade gains several places; a balanced
  early kit 10th–16th; a developed C build matched to the track can podium, and wins need a clean drive. A stock car should
  struggle to reach 12th.
- **Pay:** every valid finish pays, once per completed event (saved with the record, so it can't pay twice). An early DNF
  costs nothing; quitting pays nothing. Class C pays 100 (24th–19th), 130, 170, 220, 280 (4th), 330, 400, 500 (1st).
  Later classes multiply: B ×2.8, A ×6, S ×11, E ×19, plus ×(1 + 0.15 × (stars − 1)). E events run about three times as
  long and pay about three times as much. A first win should come after 30–60 minutes. No wear, repair bills or entry
  fees in C and B.
- **Tracks and events are separate data.** A second class on the same layout is a class event, not a new track. Only
  authored, validated tracks unlock; planned content shows as unavailable. Earned licences are never revoked by updates.
  Earlier classes stay replayable with fixed fields.
- **Track design:** see [world.md](world.md) §2, "Tracks" (the track-design skill, short and long versions, route closures).
- **Collisions:** no visible interpenetration, no ghosting, no camera tricks to hide contact. Per-body collision boxes fit
  the model's length and shoulder-and-tyre width without mirrors; swept contacts for every car. A glancing hit costs
  moments, a moderate one 1–3 s, a heavy frontal 3–6 s, scaled by closing speed and angle. No repair bills, destroyed parts
  or retirement from damage.
- **Recovery:** R / D-pad down finds a clear spot behind the next checkpoint.

## 4 · Cars

- **The body is identity; parts are performance.** A body never secretly adds power or downforce. Changing body keeps
  your owned parts and saves. Recolours don't count as bodies. Bodies are bought at the body shop (Richard, 28 Sep: "go
  from a basic car to a Nissan Skyline"), and kit versions of favourites carry up the classes.
- **The look per class** (Richard, 14 Oct):
  - **C:** family cars and real slow-looking "bangers". Faster-looking cars race only in the fastest C races (8–10★).
  - **B:** "as many unique forms as possible", not only 90s Japanese tuners. Every C car gets a B kit (`<id>_tuner`).
  - **A:** road sports cars and supercars.
  - **S:** race cars across disciplines.
  - **E:** Le Mans cars across all the real Le Mans grades.
  - About 170 bodies across the five classes.
- **Class C roster** (Richard, 17 Oct): about 50 cars with room for more. The everyday body range, plus rogue toys (ice
  cream vans, rust buckets, a hearse is fine). Every race has a few cars that look fast for class C without being sports
  cars. Every car should be "as cool as the Kestrel". 55 Forge cars race by default.
  - **Fields:** the flagship on pole, quick-looking cars at the front, the band in the middle, two or three veterans at the
    back. At most two of a model, never side by side. A rival's colour comes from the race's paint list.
- **Class B cars:** the Forge class B roster was withdrawn on 18 Oct, to be redone with Richard's direction from the start.
  Until then B fields use the 21 legacy Tuner bodies (Japanese 1990s–2000s icons, Richard 6 Oct: "a classic Japanese 1990s
  / early 2000s" feel, never a copied badge) and six off-road Tuners. Their design rule stays: the **drivetrain is the
  choice** (AWD grip and launch, RWD balance and slide, FWD precision, rotary lightness), each with a strength and a
  weakness. The Shiro GT is class B's dream car and top prize.
- **The Car Forge** (Richard, 14 Oct: the cars "make or break the game... the main attraction"): one panel-based generator
  for every car, in the private repo crippsy14/car-forge. His answers:
  - approach approved;
  - fully ground up: retire every existing car (Tuners, C cars, kits) as replacements land, one system only (legacy
    backup: git tag `cars-legacy-v1`);
  - brands for higher-tier cars;
  - bigger wheels and crushed roofs;
  - a player vinyl editor designed now, built later.
  - The goal: "high fidelity cars that are varied, unique as much as possible, and toy-like, whilst still holding up to
    seeing a noticeable difference in tiers."
- **Fidelity first:** "no more prototypes"; the engine had to be trusted before cars were built (the golden platform, met
  17 Oct). Definition before triangle budget. The legacy C cars are the benchmark for character, not for copying.
- **Toy caricature rules** for every car:
  - Caricature, don't scale: wheels 10–20% larger, crushed roofs, short overhangs, two or three signature features.
  - The silhouette signals speed: bangers tall and boxy on small wheels; each class lower, more wedged, on bigger wheels.
    Halo cars get the loudest feature (wing, fins, canopy).
  - Tomica-style diecast detail: real panel gaps and arches (never painted lines), crisp edges, flush lamps.
  - Original: invented makers; change at least three of nose, glasshouse, tail, arches and lamps from any one donor.
  - Readable at 40 px; the body stays close to its collision box.
  - Measured bars: distinct silhouettes, seamless surfaces, livery-ready, in budget, original.
- **Liveries:** object-space, so they stay identical across LODs and body roll; authored as layer stacks per class (two-tone
  zones, stripes, numbers, invented sponsors); on by default (`--no-livery`). Players repaint at the paint shop.
- **Forge models everywhere** (23 Oct, after Richard found old models in the play copy): every old class C body shows as
  its Forge namesake wherever a car is made (`CarBodies.Look`), so fields, traffic, parked cars and residents are all
  Forge cars. The roster's own characters (the Chippy, the Gold Top milk float, the Ladders, the Pronto and others) drive
  and park in the towns. The body shops sell the Forge range (Hillcrest 43 cars, Quarry Hills 11 gravel cars; old
  namesakes keep their prices, new cars are priced by star band). New saves start in the Forge Puddle, and old saves
  move to the namesakes. The Tuners, the Pit Stop's vehicles and the Regale keep their models until they have Forge
  designs. `--no-forge-roster` brings the old models back.
- **Forge cars are build output**, synced from `tools/forge-cars.txt` by `build-dev.ps1`.
- **Car props** (Richard, 18 Oct: Prop Forge serves the cars too): a car's jerry can, hot box or hamper is a Prop Forge
  object at the car's anchor. Versions are pinned, so a prop change never moves a car until it is bumped, re-checked and
  seen. Swaps go car by car with a before and after sheet. A car prop keeps its real size (my call, Richard can overrule),
  stays within about 1.5k triangles at LOD0, and never counts towards the collision box.
- **Playful kits** (Richard, 6 Oct: "be playful and brave"): a box car in class S lapping a great circuit one day is in
  the spirit. Some matte paints are an open idea.
- **Review:** render sheets each round, scored against references; QC answers "is it broken?", Richard judges "is it good?"
  in game.

## 5 · Parts

- **Seven slots today:** Engine, Turbo, Tyres, Transmission, Brakes, Steering, Chassis (frame). Platforms (one per class:
  class, bodies, drag, downforce) sit under the chassis.
- **Three steps per slot per class** (Richard, 29 Sep, chose three over four): Good, Better and Best, about +10% each on
  the main stat, with the next class's Good about 5% above this Best. Sidegrades only where there's a real trade-off.
  About 131 parts across the classes, all modelled, with a look per class (C cast iron and teal, B anodised purple, A works
  white and gold, S carbon and titanium, E carbon with orange HV cabling). The ladder is written by `tools/parts-ladder.py`.
- **Parts are discoveries** (Richard, 2 Oct):
  - **Finding:** a part is found the first time you visit its home shop. From then on every parts shop sells it and every
    garage fits it, so you can mess about in Bayview with the best parts you've found.
  - **Gating:** a part can be held back by a condition (`wins:N`, `podiums:<class>:N`, `class:X`, `rallyschool`,
    `stars:B:N`), shown at its home shop until met.
  - No per-shop-only stock.
- **Ownership:** stock parts are always owned; old parts are kept and refitting is free; fitted parts show on the car and
  change its sound; cosmetics never change performance; the garage shows price, owned, before and after, and the class.
- **Local specialities:** each new location's shop brings two or three, revealed on discovery, some behind a results
  tracker that can be earned several ways. Only five places sell local parts ([world.md](world.md) §1, decision 13).
  Planned specialities beyond those shops are found as discoveries: the Dive kit (Harbour Point), A's wet and gravel
  tyres, S's race wets, E's economy hybrid, long box and night tyres.
- **Class C unlocks by discovery:** the Bayview garage at 1★; Hillcrest's eight valley parts from 2★; the gravel kit from
  the Quarry Hills rally school. Hillcrest's valley parts stay class C.
- **Class B unlocks by stars won** (Rep): all seven Good parts at 0★; Better tyres and brakes at 4★; Better engine and
  steering 10★; Better turbo and chassis 18★; Better transmission 28★; Best tyres and brakes 42★; Best engine and steering
  58★; Best turbo and chassis 76★; Best transmission 95★. About a fifth of the map's stars can stay unwon.
- **The parts system review** (doc 83, 20 Oct; Richard: "Let's discuss the parts properly"). These are open, and
  Claude's to decide under the design freedom:
  - Turbo becomes **Induction** (cold-air box, ITBs or turbos).
  - A **Suspension** slot with per-road roughness (raceway 0, town tarmac about 0.2, cobbles 0.3, lanes 0.4, gravel 0.6).
  - Aero from class A; perhaps a separate exhaust.
  - **The speciality rule:** a sidegrade, or at most one step past Best behind a tracker; never cheaper and better.
  - **Field-stars in data:** the star level from which AI fields may fit a part (a speciality defaults to its town's star
    level).
  - Not taken: a full tuning sheet ("fights the toy tone").
- **Planned part families** (each needs a behaviour test and a place where it matters): suspension, differential, aero,
  energy, traversal gadgets (the Dive kit, jet packs, wings: the gadget slot, contract and test hop exist), lighting,
  sponsor equipment, wheels, horns, meters, stickers.

## 6 · Economy

- Credits. Step prices (Good / Better / Best): C 300 / 650 / 1,100; B 780 / 1,690 / 2,860; A 1,620 / 3,510 / 5,940;
  S 3,000 / 6,500 / 11,000; E 5,100 / 11,050 / 18,700.
- A competitive build (Better in four key slots, Good in three) costs C 3,500, B 9,100, A 18,900, S 35,000, E 59,500.
  A podium build (Best in four, Better in three) costs C 6,350 to E 107,950.
- About 20 races per class to a podium build and about 50 to own everything. Side income (exploration, deliveries,
  postcards, trials) is about a quarter of each class's. Adventure rewards must not instantly buy a winning car.
- Prices follow measured gains, not tiers. A fresh game starts with 0 credits and a complete car.
- Class B's body shop sells 27 Tuner bodies at 5,500–14,000 (the Shiro GT on top).

## 7 · Driving, drivetrain, tuning and hybrids

- **Real engines and gearboxes** (Richard's order, 30 Sep: parts for every class, then gearing tied to parts, then auto
  and manual, then sound). Every engine has a layout, cylinders, revs, a torque curve and a sound fingerprint; turbos
  reshape the curve with lag. Every gearbox has its gear count, spread, shift time and type (H, sequential, paddle,
  seamless). The car drives in a real gear with shift cuts and a limiter. Handling stays arcade.
- **Gearing v2** (Richard, 1 Oct): fixed ratios per gearbox, so top speed is governed by the gears, and power decides
  whether the car reaches the limiter. A weak engine never reaches the top-gear limiter; a strong one needs a longer box.
- **A grounded class C** (Richard, 1 Oct: upgrades gave "far too much performance too quickly", "through gears like a Le
  Mans prototype"): slower, incremental upgrades that together let you challenge the pack. Stock takes about 8.4 s to
  20 m/s and the best C kit about 4.1 s; C tops out at 89–126 km/h.
- **Controls** (GT7 on a pad as the model): automatic or manual (Settings). R2/L2 are throttle and brake; on manual,
  Cross shifts up and Square down (keyboard Left Shift / Left Ctrl). This replaced the 30 Sep layout of shifting on the
  triggers with the right stick as the pedals, after the brake and downshift clashed on L2. Reverse is a gear on manual
  (down from first at a standstill). On every mode the forward pedal brakes a car rolling backwards. Every driving
  control is remappable on pad and keyboard. The automatic box doesn't hunt between gears, and the shift clunk is
  "understated".
- **The tuning shop** (Richard, 1 Oct: "with opportunities to test the car on the track you are tuning it at"): a Tune
  counter at each Pit Stop's garage, free. Final drive 0.80–1.25 on every box; each gear on sport boxes and up; auto-tune
  presets for acceleration, balance or top speed. A GT-style gear chart, power and torque curve, and the car's numbers,
  with a timed test lap of the town's circuit. Tunes save per car and gearbox; rivals race their box's defaults.
- **Loose surfaces** (3 Oct, after the outsider review): grip on loose slate and gravel sits 60% of the way from a
  tyre's road grip to its gravel grip, for the physics and the AI alike, so gravel tyres matter in the rallycross.
- **Aero and the top classes** (2 Oct, after Richard's first drive of the maximum build: "siiiiiick! The speed felt
  good"). He asked for a higher top speed for bigger tracks, no nose lift ("feel glued to the track"), and "remove the turbo
  noises all together".
  - Downforce cars (S and E) get aero grip, about ×1.86 at 75 m/s, shared by the physics, the AI's plan and the ideal lap.
  - Platform drag is real: B 0.95, A 0.9, S 0.7, E 0.6 of a street car's.
  - Race platforms sit on stiff springs, and the E gearboxes are taller.
  - The maximum build tops out at about 332 km/h.
- **Peak and floor** (Richard, 2 Oct, asking whether there's room to raise the peak "and even reduce the floor of the
  stock"): on Hillcrest Raceway the stock car laps in 76.5 s and the best C build in 57.1 s. A slower stock car would make
  early upgrades feel bigger, but it changes every C race: a keep/reject question when taken up.
- **Hybrids** (Richard, 20 Oct: "pretty sick if, in earlier classes, certain types of engines require batteries as a
  hybrid... better traction off the line but the battery can run out, so if not managed you end up with a heavier car
  with no benefit"):
  - Built behind `--hybrid`: the **City hybrid 1.5**, a class C sidegrade to the Club 1.4 at Jed's (Bramblewick).
  - It deploys automatically under hard throttle at low speed and regenerates under braking; a flat battery is dead weight.
  - Richard, 21 Oct: "a baseline battery effect and leave it there, so it's easier to judge performance impact for things
    like PP." So there are no modes and no battery slot; PP counts the motor at half strength. The same rule applies to
    the player and the AI. It wins on stop-start laps and loses on long ones.
  - Planned up the classes: B proper hybrids; A recharge as a pit service; E the full system (ERS, e-turbo).
- **Fuel, tyres and pits (class A, planned):**
  - **Fuel:** a GT-style fuel mix (1–6), changeable mid-race, with burn from throttle, power and mix. Tanks are parts with
    capacity and mass, and fuel mass burns off. Running out limps you on, slowly.
  - **Tyres:** compounds trade grip against life, with progressive wear and warnings.
  - **Pits:** a physical pit lane and box, with fuel-only, tyre-only and combined stops. The cost is race time.
  - The same model for player and AI. E events are a 45-minute stint, a 90-minute race and a day-into-night finale.

## 8 · Racing AI

- **Racing AI v2** (`RacerDriverPolicy`, Richard 29 Sep: built from the ground up around six driver abilities) is the
  driver. Its layers are persona, strategy, tactics, speed and control:
  - **Persona:** six abilities and a personality, set per race.
  - **Strategy:** an intent state machine (launch, race, attack, defend, recover).
  - **Tactics:** a heat line across the track.
  - **Speed:** one arbitration with one logged reason.
  - **Control:** pure pursuit.
  - **Etiquette:** the car with overlap on the inside at turn-in owns the apex; a car alongside keeps its lane; a
    defender makes one move, never in a braking zone.
- **The six abilities:** Braking, Line, Control, Consistency, Racecraft and Strategy (fuel and tyres, from class A),
  plus Aggression and Patience. Each ability has a real effect. All-worst is about 49% slower than all-best on a solo lap
  (Richard asked for about 50%).
- **Skill 1–10** (Richard, 7 Oct: "have a skill of 1-10 for that range instead... the current system is jumbled"): shown as
  DR on the race preview with a radar. Bands by class: C 2–3, B 4–5, A 6–7, S 8–9, E 9–10. A field's skills roll 70% from
  grid position and 30% seeded luck, with three wildcards per race.
- **The field recipe** (default; Richard, 7 Oct). A field comes from the race's class, stars and surface, never the
  track, so any new race gets a fair field with no tuning.
  - Each car's parts sit in a window from a floor to what's unlocked by that race. The floor rises 0.1 of the open ladder
    per star, to 0.9 at 10★.
  - Class B tyres never drop to class C's.
  - Bayview's first B race is the ground truth (Richard, 7 Oct): the worst B parts mixed with the best at the local
    garage, raced against the free Tuner on Good parts.
  - Richard's direction: parts in a race mix from the lowest C tier to some of the local garage's; the further from the
    start, the more is unlocked and the harder the races; drive ratings stay consistent and only parts change.
  - `tools/field-table.py` shows the PP of every planned C and B field (stock starter PP 100, the free Tuner on Good
    parts PP 196).
- **The grip budget** (default; Richard, 8 Oct: "Lets do both"): lane changes are paced to the grip left, and cars lift at
  full lock. **Per-car racing lines** (default): the field is grouped by performance and each group gets its own
  minimum-time line inside a ±5.6 m corridor, baked at race load and cached.
- **Racecraft v3** (default since 23 Oct; Richard: "merge it in and make it the default"): lane commitment (no lane change
  without a real gain), attacks that own the lane beside the target, and closer following of the target. Rejected after
  trying: more aggression, earlier giving up, closer following everywhere, and nudging. No "nearly-enough gap" attempts:
  a gap is taken only with the car's width plus 0.45–0.85 m a side.
- **Rules:** the AI races the same physics as the player, never beyond its car's physical cap. Any future difficulty layer
  is bounded and invisible, with nothing in the last 20% of a race. Races are deterministic. Barriers don't fix an AI
  that leaves the road (Richard, 7 Oct).
- Richard also watches the races: telemetry can't tell a good racer from a lucky one.

## 9 · Sound and music

- **Engines are real recordings** (Richard, 30 Sep, first listen: "more like a PS1/PS2-era kart game"; the sound must be
  "guttural and real").
  - Each engine part plays a grain bank cut from a real car's recording (Sonniss GDC bundles: royalty-free in games, no
    attribution, raw files never in the repo).
  - The part's exhaust type tones it, and the intake, shift and limiter layers are synthesised.
  - Each engine step maps to a different car, so parts sound different.
  - The eight nearest cars have a voice.
- **The engine scorecard is the test for every sound change** (Richard, 2 Oct: a way to "accurately create engine
  noises, and have something to compare them against as some kind of scorecard... accurate through gears and when
  letting off"; then "at least a score of 8 on all of the in game engines").
  - Each engine is driven along its own recording's drive and scored 0–10 on pitch, tone, character, chipmunk above the
    recording, garble, clicks, lift-off, gearchanges and rise with revs.
  - The in-game average is 8.3, with 15 of 18 engines at 8 or more. The Stock 1.2 needs a clean small-four recording up
    to about 6,000 rpm.
  - Rules: don't buy score with loudness; real engines lose only 1–4 dB in the first 0.7 s of a lift-off; above a
    recording's top revs, use a sustain loop rather than speeding grains past 1.5×. The game is heard from behind, so
    exhaust-side references are preferred.
  - Downloads for sound don't need asking (Richard, 2 Oct), but watch the licences.
- **Turbo and hybrid whine are off** (Richard, 2 Oct; `--turbo-sound`, `--hybrid-sound` bring them back). Turbo cars are
  slightly muffled.
- **Race start:** silent until the countdown, then a 1.5 s fade-in; three short beeps and a long, higher GO.
- **Ambience:** surf from the ground round the listener, gusting wind, the Silver Falls roar, river babble, gulls and
  songbirds by day (real field recordings, faint, gone above about 12 m/s), synthesised crickets at night, and area
  ambience (sheep, quay water, town squares, Quarry Hills' blasting siren in working hours).
- **Music:** an explore theme and a race theme per area, composed as MIDI and rendered through FluidSynth
  (`tools/music/compose.py`). Each race theme has a last-lap lift stem that fades in over about 3 s. Music sits under the
  engines (race 0.45, free roam 0.6) with a 4 s crossfade between areas. Bayview is G major at 96 bpm (nylon guitar,
  marimba) and Hillcrest D major in 6/8 (steel guitar, accordion). Every new area needs its own themes.
- **The engine sound test** (Richard asked for one, 30 Sep): Settings > Engine sound test, or `--sound-bench <engine>`:
  idle, a pull to max revs, a hold on the limiter and the lift back to idle, through the game's own audio.
- **Horn** (H / L3): the pitch follows the body; folk answer, traffic beeps back, sheep scatter, Bert's train whistles.

## 10 · Interface

- **Rules** (Richard, 29 Sep: the UI "looks great but feels muddled and bolted together"):
  - one predictable structure, under seven top-level entries;
  - LB/RB (Q/E) switch tabs everywhere, and Back goes up exactly one level;
  - a footer of working buttons in the player's own glyphs;
  - predictable focus, with mouse, keyboard and pad all working;
  - a minimal HUD with progressive disclosure;
  - contrast of 4.5:1 or more, and large type.
- **Driving HUD:** the speed dash bottom centre, a heading-up minimap bottom right that zooms out with speed, and the
  "Talk to…" card above the dash ("Slow down to talk to…" while moving). **No control hints on screen while driving**
  (Richard, 29 Sep): the CONTROLS tab lists every action for keyboard, Xbox and PlayStation, and the prompts that do show
  (talk cards, shops, menus) name the live bindings in the last device's glyphs. Credits and collectibles show as a
  toast when they change.
- **Talk badges:** people show a round talk badge in the world within about 38 m, dimmed behind scenery and pulsing for
  the one in range. No floating names. Near someone, Cross talks rather than shifting.
- **Saves resume where you parked**, and the pause map can warp you to the nearest Pit Stop you've visited.
- **Challenges and objectives** (Richard, 5 Oct): a timed event gets a countdown like a race start; a challenge shows big,
  then "shrink[s] to the upper corner"; any challenge can be exited or restarted from the pause menu. With no challenge
  running, the quest card announces a new objective big for 2.4 s, then sits in the corner, fading while driving.
- **The pause hub** (Esc / Start; the world and audio pause) has tabs for MAP, COLLECTION, CAR, SETTINGS, CONTROLS and
  RECORDS. Changes to the car happen at a Pit Stop.
  - **In a race,** the pause menu offers back to race, restart, view the field, options, and retire to town (no pay).
- **Options:** GRAPHICS, DISPLAY, AUDIO, DRIVING, INTERFACE and CONTROLS, from the title, the pause hub and the race
  pause. Settings include units, vibration, motion blur, speed effects, camera shake, the minimap, button prompts and
  ambience volume.
- **Shops** (GT7 model): buying and fitting are separate counters. Each card has one status badge and one action button,
  with the fitted part compared against the preview. One class at a time (L1/R1). Categories sit in a vertical list on the
  left (Richard: "a list that is up and down"). Road, Rally and Ice pills show the discipline. Locked parts show the
  condition that opens them. The body shop's preview turns the car smoothly every frame, with the world hidden behind
  the full-screen shop.
- **My garage** (23 Oct): at every garage bay, a screen for the models you own: the focused car on a turntable in your
  paint, its card (name, what it is, its signature, class and surface, "looks never change pace"), and a strip of every
  car you own. Cross drives the focused car.
- **Fog of war:** the maps show only where you've been, revealed in a 175 m circle round the car. Viewpoints reveal 520 m
  and the castle 620 m, and arriving in a town reveals the town. People, Pit Stops and names stay hidden until explored.
  A place shows a DISCOVERED card on the first arrival, then its name sits under the minimap.
- **Wayfinding:** a notice board in each town lists its offers and your progress; pinning one puts a star on the map.
  Fingerposts give distances in miles at the mainland junctions.
- **Title screen:** Continue, New game, Load game, Quit. Three save slots, each with an autosave (after every race,
  purchase and find) and a saved game (from the pause menu). Overwriting another game asks first.
- **Snap tool:** F9, or hold R3 ([how-we-work.md](how-we-work.md) §4).
- **The UI art pass** (bug #18, "AI-ish UI elements need a revamp") waits on its own brief for Richard.

## 11 · Look: toy shapes, real light

- **The direction:** chunky, readable toy shapes (rounded kit buildings, simple props, toy cars) under real
  late-afternoon light. Moved away from toy colours under flat light on 26 Sep.
- **All light, grade, cloud and colour-limit numbers live in `WorldLook.cs`**, and `WorldLookChecks` fails the build if
  they drift. Create materials with `TrialWorld.Material` (it grades colours: saturation capped, brightness rolled off;
  only car paint is exempt) and terrain with `WorldLook.Terrain`. Never hand-set `_Stylise` or saturated colours.
  Custom shaders need `_LIGHT_COOKIES` (cloud shadows), sRGB vertex colours, fog, depth and shadow passes, and LOD
  cross-fade.
- **Palette:** earth pigments, never pastels, with a named palette per town (Hillcrest ochre, saffron, terracotta and
  Pompeian red with bottle-green shutters; Bayview bold harbour paint; Quarry Hills slate; Bramblewick thatch and cob;
  Vinelands whitewash, schist and azulejo blue).
- **Light:** one strong sun with a dim cool fill, SSAO, TAA; a neutral tonemap with cool shadows and warm highlights;
  a procedural sky with a real sun path (about a 20-minute day, nights three times faster); drifting cloud shadows.
  Reviews use a fixed afternoon unless `--time` is given. Water is deep teal-green and takes its blue from the sky.
  At night, windows, lamps and headlamps light up.
- **The fundamentals checked in captures:** value hierarchy (the greyscale test), a controlled palette, silhouettes,
  framed arrivals, detail hierarchy (no empty lawns), grounding (contact shadows, foam where water meets things),
  atmosphere, image stability (no shimmer, pop or hitches), motion and life.
- **The quality bar:** calibrated against Art of Rally, Lonely Mountains, Tiny Glade and A Short Hike (Hot Wheels
  Unleashed for the toy look): 10 is best in class, 7 trailer-worthy, 5 unremarkable. A new area must reach 7 or more.
  Biggest remaining levers: valley height fog, water edges, vegetation life, speed presentation.
- **Prop Forge: one built world** (Richard, 17 Oct: today's assets "can look disjointed and very much copy and pasted...
  wall end points, and wall mid sections, then literally draw them onto a road and allow for curves").
  - **Four kinds of piece:**
    - objects: seeded variant families, the same every visit;
    - runs: walls, fences, hedges, kerbs, railings, barriers and bunting, drawn along named roads with ends, corners,
      openings and joins;
    - plots: buildings fitted to footprint and slope;
    - cover: planting in clusters and drifts, never a grid.
  - One toy range, at the same scale as the cars, in a style per place. Wear is comic and lived-in.
  - Richard, 18 Oct: "Go crazy, no holds barred. Everything in the game can be replaced if it would benefit from a
    remodel." A new model must beat the old one outright to stay: variety alone isn't enough.
- **Every prop sits at the angle of its land** (Richard, 18–19 Oct, "for this and moving forward"; `GroundFit`):
  - posts, poles, lamps and signs plumb and sunk;
  - trees plumb with a slight lean;
  - dry-stone walls follow the ground, while brick walls step level;
  - fence posts are plumb and their rails rake;
  - bales, rocks and crates tilt to the slope and settle;
  - buildings stand level on a platform (plinth on the low side, retaining wall on the high side);
  - benches stand level on a pad.
  A car sliding along a wall keeps its speed (the wall-slide test).
- **Budgets:** a whole car at most 30k triangles at LOD0, 5 materials; walls at most 600 triangles a metre; a full Forge
  field keeps the race p99 frame within 20 ms.

## 12 · Characters, features and activities

### Duke Rolo and the final boss
- Duke Rolo owns Hillcrest Castle. Beating him is the final boss (Richard, 2 Oct): "a great full circle moment at the end
  of the game... like fighting Red in Pokemon Blue."
- **The Regale** (Richard, 1 Oct: a "super detailed prototype model with a really detailed livery... go all out"): his toy
  Le Mans prototype in purple and gold, whose sponsors are Hillcrest and Bayview folk. It is parked at the castle as a
  teaser: "Cars like her race in the top classes. Climb the ladder and you'll meet them." It is known but never sold.
- **The Duke's Challenge** (built; Richard: "a 3 lap race in the typical format, but it's just a 1 vs 1"; the prize:
  "Love love love that!"): three laps of Hillcrest Raceway, one against one, with the Duke on pole in the Regale on the
  measured maximum build, driven by the best driver in the game. The first win pays 50,000 credits and gives you the
  Regale. A loss costs nothing, and rematches are for honour. In the full game it's the campaign's end, after the
  Aurora Endurance ([world.md](world.md) §1).
- **The Duke's drive** round town is parked (Richard, 2 Oct) until traffic can handle a car at pace (`--duke-drive`).
- **Test drives** for Richard: Max Build Hot Laps and Max Build Test Drive (`--max-build-test`).

### Racers, recruits and teams (planned, from class A)
- **One racer record** for every rival, recruit and teammate: name, look, home, bio, personality, the six abilities with
  a potential cap (rivals don't grow, teammates do), car preferences and recruitment terms. Rivals persist across events.
- **Finding racers:** people in the world, often off the main road. Talking adds them to your Contacts (radar,
  potential, home, what they want: races, podiums, beating them, a favour), shown as a tracker. Recruits can be found
  from class C (Bramblewick's milk-float champion, Orchard End's apple-cart racer, Gwen, Dai and Mari at Quarry Hills).
- **Teams** (one per class A, S and E): you and up to two found racers, managed in the garage's TEAM tab. Teammates are
  two of the 24 entrants with the same AI, and their parts are bought per car (a late-game money sink). No team orders at
  first. XP from distance and results; levels 1–20. A suggested 50% share of a teammate's pay; small track familiarity
  for the player's team only. OPEN: team orders (not before S, if at all), wet races in class S.
- **Only show what's real:** a radar axis or stat appears only once its behaviour exists.

### Things to do
- **Kept:** photo mode (P / D-pad Up: five looks, depth of field and a postcard frame), fireworks over Bayview harbour
  after dark, the Hillcrest Castle zipline (Richard: "more just navigation than an activity... to help skip the climb"),
  the generic activity framework (HUD, countdown, pause restart and exit) for the minigames to come, and the gadget slot's
  test hop.
- **Retired, 24 Oct** (Richard: "retire _all_ activities we had and build a new set from the ground up with these new
  rules... treating it as a true minigame with all the same focus we give everything else"; "I was personally treating
  them like afterthoughts"):
  - **Claude's additions:** the speed traps, Beat the Train, Pip's photo requests.
  - **Challenges:** the Castle Climb and Hard; Farmer Giles's Harvest and Apple Shake; Quarry Hills' rally school, the
    Gallery Climb, the Quarry Sprint, the Blast Run and the Tip Leap; the Blondin; Shadow the Duke.
  - **Collections:** the four keepsake sets and their albums.
  - **Town events:** Rally Day and choir night.
- **What stayed of them:** the places and characters (Bert's train, the castle, Golden Acre's wheat and orchard as
  scenery, the galleries, the rallycross loop). Giles, Gwen and Pip are residents with ordinary chat; Dai went with the
  Blast Run.
- **What changed:** the gravel tyres and rack are plain discoveries at Bethan's (they were the rally school's prize). The
  paints the keepsake sets paid stay with the saves that earned them.
- Their code is in git before the retirement commit, if a minigame wants a piece back.

### Activities: one minigame at a time
- **History:** the valley activities went on hold on 30 Sep ("a better idea"; Claude's additions came instead). On 21 Oct
  all new activities went on hold: the first towns' were underbaked. On 24 Oct everything built was retired (above).
- **The bar** (Richard, 24 Oct: "Is it fun? Does it fit into the world? Is it measurable and, therefore, testable?
  Confirm it's not just drive from A to B in some form. Does it contribute to the game?"): the activity-design skill
  (`.claude/skills/activity-design`). Seven gates every idea must pass, eight scored criteria, the verbs that aren't A to
  B, rewards within the economy, measured tiers and an automated test per activity.
- **The plan:** one activity at a time, each a true minigame with the focus a place or a track gets:
  1. a brief;
  2. Richard's yes;
  3. a graybox behind a flag, with its automated test and measured tiers;
  4. a playtest and a keep-or-reject;
  5. then art and polish.
- **The first one: Ballroom Curling** (Richard, 24 Oct, from the final two of the bracket in `docs/brief-activities.md`;
  the runner-up was Slate Sumo). Its brief is `docs/brief-ballroom-curling.md`. It also builds the shared harness the skill describes (`--activity-test`, the `LITTLE_ROAD_ACTIVITY` line,
  tier records) and a RECORDS tab for minigames.
- **Side income is zero until they come:** about 12,800 credits of one-time pay went with the retirement (traps 3,300,
  photos 1,900, the school 1,860, keepsakes 1,550, the climbs 1,050, the farm 1,000, the rest about 2,200).
- Ideas he liked, kept for that sweep: the rugby conversion kick at Quarry Hills ("a fun physics game"), Heavy Haul ("a fun
  one-off"), the Barrel Race at Vinelands ("Fun physics toy!"), and the old-roadster cellar secret ("a good opportunity to
  get a cool model after performing the task required").

## 14 · The plan of record: order of work

The order of classes and systems (doc 45, 29 Sep). The order of places is in [world.md](world.md) §1.
- **Principles:** fix expensive-to-change things early (save format and IDs, the racer record, track standards); seams
  before depth (a data definition and a thin slice behind a flag, felt and tuned first); graybox before art; one campaign
  class at a time (later-class systems only on test tracks); every feature ships with its test mode and editor checks;
  racing changes go through keep/reject.
- **Phase 0, foundations:** the world grid and per-tile map, the racer records and radar, versioned saves, track
  standards as editor checks.
- **Phase 1, class C complete:** the C parts ladder (done), Contacts and recruitable racers, the C ring of places, a team
  prototype behind a flag, the C economy measured in play.
- **Phase 2, class B:** kits and the B ladder (done), B events across the map, the Tuner Championship, the fuel, tyre and
  pit prototype on a test track with its strategy AI.
- **Phase 3, class A, the professional league:** teams for real, XP and levels, fuel, tyres and pits on, the Strategy
  ability, pro layouts with pit lanes, the frenzy.
- **Phase 4, class S:** aero-era bodies, night races, S teams. **Phase 5, class E:** Aurora, long races, day into night,
  multi-stop strategy, the Endurance and the credits. Then the post-game.
- Throughout: the performance budget, contact and grid checks, Richard's playtests.

## 13 · Technology rules that shape design

- **Load budgets:** ready to drive in 3.5 s or less, race entry 1.0 s or less (Richard, 8 Oct: loading the whole map every
  time "is not acceptable"; the map will grow to 20 × 20 km). The free-roam world persists across races; races start in
  the live world.
- **Streaming:** woods and scattered pieces stream on a 250 m tile grid; districts build in the background after you're
  driving; baked data (the valley's height field and tint, the island's heights, the road lines' ground samples, cliffs,
  racing lines, bank sites, the map image) is cooked once per build and cached, and a cooked world must fingerprint the
  same as `--no-world-cache`.
- **Background districts (23 Oct):** every place further than 1.5 km from where the car starts is a background district,
  queued nearest first (from Bayview: Bramblewick, Quarry Hills, Vinelands; `--no-far-districts` builds the two far places at
  load as before, `--no-background-districts` everything). Each district owns its ground until it has built: rails, fingerposts,
  speed traps and bank barriers there wait for it, and when it arrives it gets its own settle pass (rails trimmed, pieces
  seated, streets dressed, banks barriered, static batching) and its residents, parked cars, keepsakes and photo sights. The
  streets' dressing is seeded from each piece and the bank barriers are named by where they start, so a place comes out the
  same whether built at load or later (the world fingerprint checks it: the same pieces, settled poses within millimetres).
  A district seats only its own standing cars (seating the world's again moved them all a little). A race in the kept world
  finishes only the districts its area touches; the rest carry on once the town is back. A district's runs' meshes are made
  on every core, then handed over a few a frame (`Runs.FlushRoutine`); its residents, parked cars, dressing and static
  batching come a few a frame too. Everything placed once per world (dressing, bank barriers) is skipped when the kept
  world comes back (`PersistentWorld.Reused`).
- **The world must not change** unless a change means to: the world fingerprint and town print guard it. Never rely on
  physics hit order.
- **Frame budget:** drive benchmark p99 20 ms or less, no more than two frames over 33 ms, zero managed allocation per
  frame; nothing may pop in within 300 m. Small pieces stop drawing at 280 m (under 3.5 m) or 700 m (under 7 m).
- **Still to build for the full map:** floating origin, HLOD for the horizon, data-driven regions, streaming the towns'
  buildings, GPU instancing.
