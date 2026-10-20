# 83. The parts system: where it stands and where it could go (20 October 2026)

Richard, 20 Oct: "Let's discuss the parts properly, as that is an overall thing." This started with Jed's four parts for
Bramblewick (doc 82), which didn't fit the game's slots. It builds on [doc 44](44-parts-tiers-and-progression.md) (the
ladder) and [doc 47](47-parts-drivetrain-and-sound.md) (drivetrain and sound).

## What a part does today
A car is seven slots. Each part feeds one or two numbers into the driving model, which is a grip budget: how hard the tyres
can push, shared between cornering and accelerating or braking.

| Slot | What it sets | What it means on the road |
|---|---|---|
| Engine | The power curve (real cylinders, revs, torque peak; doc 47) and its sound | Speed out of corners, top speed |
| Turbo | Boost, with its spool and lag | More top end, a soft bottom end until it spools |
| Tyres | Grip on tarmac, and grip off it | Cornering speed and braking; the gravel tyres win on the loose |
| Transmission | Gear count, ratio spread, shift time, LSD | How the power is used; gearing is tunable (doc 48) |
| Brakes | Braking force | Braking distance (up to the tyres' grip) |
| Steering | Response and yaw rate | How sharply it turns in |
| Chassis | Mass (and its class platform: drag, and downforce from S) | Everything a bit quicker |

There are **131 parts**: 40 in class C and about 22 in each higher class. 11 are local specialities: 8 at Hillcrest and 3
at Quarry Hills.

Some things players expect from a tuning game **aren't in the model**:
- **Suspension:** body lean only; nothing it does changes grip or time.
- **Air intake and exhaust:** nothing apart from the engine and turbo.
- **The road surface:** the car knows only "tarmac" or "not tarmac", plus a flag for a whole race on gravel. Cobbles,
  rough lanes, kerbs and bumps all drive the same as smooth tarmac.

## Where it strains
1. **Specialities have nowhere to go.**
   - Doc 44 gives each slot three steps per class, and each region's shop adds two or three local parts.
   - With seven slots and one or two numbers each, a speciality is either a bigger number (it pushes past Best, and the
     ladder inflates) or a near-copy. Class C already has three off-road tyres: Trail, Rally and Gravel.
   - Jed's brief (lane springs, an off-road tyre, a cold-air intake, a light battery) is exactly this. Two have no slot
     and one is a fourth off-road tyre.
2. **The trade-offs are thin.** A sidegrade only means something if the road asks a different question somewhere. Today
   the only such question is tarmac or gravel, so the only real sidegrades are tyres.
3. **The AI fields pick up new parts by accident.** A class C speciality with no unlock condition opens to the AI
   fields from the first star (`FieldRecipe.Open`; only Hillcrest's and Quarry Hills' shops are named as exceptions). Add
   a part and every C race's field changes. That needs keep/reject runs on all three tracks and makes adding parts risky.
4. **Each part is also a model and a sound.** C parts have 3D previews in the parts shop (29 models). Engines have
   sound fingerprints (doc 55's scorecard). A new slot multiplies that work across five classes.

## The options
### A. Keep seven slots, and make specialities sidegrades only
Jed sells parts on the existing slots: lane tyres, a calm farm-track rack, a cold-air version of the club engine, and a
battery-delete shell (doc 82's table).
- **For:** cheap, and nothing else moves.
- **Against:** the trade-offs stay thin, and the next four parts-selling places (Highmoor, Lowtown, Copper Gulch,
  Icefall Hut) hit the same wall.

### B. Two changes that add real choices (my recommendation)
**1. The turbo slot becomes "Induction"**, holding naturally aspirated and forced induction alike:
- **Naturally aspirated options:** a cold-air box, then individual throttle bodies in the higher classes. They give
  instant response and no lag, but less peak power.
- **Turbos:** they keep their extra top end and their lag.
- **The choice:** throttle response or peak power, and it suits some tracks better than others. Hill climbs and tight
  lanes favour the instant pick-up; long straights favour boost.
- **Sound:** the engine sound system can voice the difference (intake roar against turbo whistle and blow-off).
- **Cost:** a rename and a handful of new parts. No new slot, and it reuses the turbo code (no spool, no lag).
- **Jed's cold-air box** lands here naturally.

**2. A new Suspension slot, with roads that have a surface quality.**
- **The road:** every road gets a roughness value: 0 for the raceway, about 0.2 for town tarmac, 0.3 for cobbles,
  0.4 for country lanes and 0.6 for gravel.
- **The suspension:** each setup has two numbers.
  - **Compliance:** how much grip it keeps over rough ground.
  - **Firmness:** sharper response and less lean on smooth ground. Body lean already reads a firmness number.
- **The ladder:** three steps per class get firmer and better controlled.
- **The sidegrades:** soft, long-travel setups lose a little on the raceway but win on lanes, cobbles and gravel.
- **What it gives the map:** places now ask different questions. Bramblewick's lanes reward Jed's lane springs, Quarry
  Hills' gravel rewards rally suspension, Hillcrest's cobbles sit in between, and the raceway rewards stiff.
- **What it gives specialities:** a town's local part has a reason to exist, and to be sold there.
- **Cost:**
  - an 8th slot in the data, the shop, the saves (an old save gets the stock part), the parts generator and the AI field
    recipes;
  - about 15 ladder parts across five classes, plus 4-6 sidegrades and their preview models;
  - the physics change: the grip budget gets a roughness term. The AI's speed planning (racing lines, the per-car
    lines, the speed profile) has to read roughness along the line, not one grip number for the whole lap;
  - because it changes every race, it goes behind a flag and through keep/reject on all tracks.
  - My estimate is several rounds of work. The physics and AI part is the risky bit.

**What goes:** the battery and the fourth off-road tyre. The battery would be a frame part with no trade-off. Class C
already has three off-road tyres.

### C. A full tuning sheet
Springs, dampers, anti-roll bars, camber and so on, as sliders in the tuning shop. **Not recommended:** it's sim-racing
depth that fights the toy tone, and the AI would need to understand every setting.

## A fix worth making whichever option is chosen: field windows in the data
Each part gets a **field stars** number: the star level from which AI fields may fit it. A speciality defaults to its
town's race star level (Bramblewick 2★, as Hillcrest; Quarry Hills 3★). This replaces the named shop checks in
`FieldRecipe.Open`, so adding a part never quietly changes an existing race, and a field's kit is decided in one place.

## Specialities: sidegrades or better?
Doc 44 (decision 3) allows a speciality locked behind a tracker ("a better engine in the fourth location needs 3
podiums"). I'd keep that rule but cap it: **a speciality is a sidegrade, or at most one step past Best behind a
tracker, never cheaper and better.** This keeps the ladder from inflating as more towns arrive.

## Hybrids: a battery to manage (Richard's idea, 20 Oct)
Richard, 20 Oct: "it would be pretty sick if, in earlier classes, certain types of engines require batteries as a hybrid
and they get better traction off the line but the battery can run out, so if not managed you end up with a heavier car
with no benefit. When we add pit stops, the battery could be recharged as well." He asked for it to be written up.

Until now "hybrid" was only a part name: class E's Hybrid V6 and V8 and its ERS pack and e-turbo (docs 44, 47) are just
bigger numbers. This gives them a real system, and gives classes C and B an energy-management layer before fuel arrives
in class A, so managing a resource already feels natural by the time it matters. It's the path real racing took: KERS in
F1, then the Le Mans hybrids.

### How it works
- **A hybrid engine needs a battery.**
  - A hybrid engine (an engine part with an e-motor) can't be fitted without a battery part.
  - Battery parts trade capacity against weight, as doc 18's fuel tanks do: a bigger battery deploys for longer but
    weighs more.
  - This is where Jed's "lightweight battery" belongs: a small, light battery for the twisty lanes.
- **Deploying:** the e-motor adds torque from the battery. In our grip-budget model extra torque only helps while the
  tyres have grip to spare.
  - Class C cars are mostly power-limited, so in C it's a clear gain off the line and out of slow corners.
  - In the grippier, more powerful classes the tyres become the limit. There it fills the mid-range and the gaps
    during gear changes instead.
  - To be measured on the test tracks, not assumed.
- **Harvesting:** braking recharges the battery (regeneration). Brake hard into a corner to charge, deploy out of it.
  That rewards good driving and stops the battery being a one-shot boost.
- **Running flat:** an empty battery is dead weight, Richard's penalty. Driving that harvests well claws it back.
- **Controls:** automatic by default (L2 and R2 shift, and the right stick is throttle, so the pad is busy):
  - it deploys at full throttle out of slow corners;
  - a mode switch (Save, Balanced, Attack) sits on the d-pad or the pause wheel;
  - an "Attack" button for overtakes is a possible later extra.
- **The AI** uses the same automatic rule as the player. It stays fair, and the AI work is small: its speed planning
  reads the extra torque from the battery's charge.
- **On screen and in the ear:** a battery bar beside the speedometer (the gadget charge HUD already exists), and an
  e-motor whine layered under the engine (the sound system has room for it).

### Recharging
- **Classes C and B (no pits):** the battery starts each race full, and recharges at any Pit Stop in free roam.
- **From class A:** recharging becomes a pit service, a fixed 3 seconds added to the stop beside fuel and tyres
  (doc 18's service model).
- **Strategy:** a hybrid burns less fuel, so it may skip a fuel stop. Hybrid against pure petrol becomes a real
  strategy choice.

### Up the classes
| Class | Hybrid |
|---|---|
| C | One mild-hybrid engine as a sidegrade (a city-car hybrid) and two batteries: a small boost to learn on |
| B | Proper hybrids with real regeneration; batteries on the ladder |
| A | Recharging joins the pit stops; hybrids save fuel |
| E | The full system: the existing hybrid engines, ERS pack and e-turbo get real batteries |

### Balance and risk
It mustn't win everywhere:
- a small battery;
- the extra weight, which tells when the battery is flat;
- a hybrid makes less peak power than its matching petrol engine.

Those should make it a choice, not an upgrade. It goes in behind a flag and through keep/reject races like everything
else. It also needs a new slot (Battery) that only matters with a hybrid engine fitted. The shop would show it only once a
hybrid engine is owned.

### First step
A prototype after the lanes race: one class C mild hybrid with a battery, regeneration and automatic deploy, behind
`--hybrid`. Measure it against its petrol twin on Bayview and Hillcrest (lap times, launch and corner exits, the effect
of running flat), then the AI's use of it.

## Decisions for Richard
1. **Induction:** the turbo slot becomes Induction, holding both naturally aspirated and turbo parts?
2. **Suspension:** add a suspension slot with road roughness (option B.2)? Now, as part of Bramblewick, or as its own
   piece of work first?
3. **Anything else as a slot?** For example, aero from class A (wings and splitters), or exhaust as its own slot rather
   than inside Induction.
4. **Specialities:** sidegrades, or at most one step past Best behind a tracker?
5. **Field stars in the data**, so new parts don't change existing races?
6. **Hybrids** (above, Richard's idea): agreed in principle ("write it up"). It's prototyped after the lanes race.

Until these are decided, Jed's shop waits. The rest of Bramblewick carries on: per-circuit widths and the lanes race.
