# 47: Parts for every class, real gearing, and sound

30 September 2026. Richard's order for this round:
1. all parts for every class;
2. gearing tied to the fitted parts;
3. automatic or manual gears;
4. the game's sound, with the engine, turbo and shifts first.

Then map expansion. Class plan and prices are in [doc 44](44-parts-tiers-and-progression.md).

## What changes and what stays

**Today:**
- An engine is two numbers: `power_limit` (the speed its power could hold against drag) and `accel` (launch pull).
- A gearbox caps top speed (`ceiling`) and scales pull. A strong engine behind the stock box simply hits the cap: exactly the "powerful engine locked out in 4th" Richard wants gone.

**New:**
- **Every engine is a real engine:**
  - layout and cylinders (I4, V6, V8, V10, flat-6 later);
  - displacement;
  - idle, peak-torque, peak-power and redline revs;
  - a torque curve;
  - aspiration.

  Turbos reshape that curve (boost builds with revs, with lag) rather than multiplying it.
- **Every gearbox is a real gearbox:** gear count, ratio spread and progression, shift time and type (H-pattern, sequential, paddle, seamless). The final drive is solved per build, so top gear always lands the car's real top speed a little past peak power and under the redline.
- **The car drives in a real gear.** Road speed gives revs through the actual ratio, torque at those revs gives wheel force, and shifts cut drive for the gearbox's shift time. Revs, gear, load, boost and shift events drive the sound.
- **What stays:**
  - the arcade handling model (grip budget, drag, yaw);
  - the speed scale per class;
  - how the AI plans. The AI plans on the best-gear envelope, which is what `DriveModel.EngineForce` has always been. So both AIs keep working, and the gap between the envelope and real gears becomes each gearbox's character.

## Engine model

- **Torque curve:** normalised torque Ï„(rpm) from four control points (idle, peak torque, peak power, redline), eased between them:
  - naturally aspirated: broad and rising, peaking in the upper mid-range;
  - turbo: Ï„ multiplied by boost(rpm), which rises from spool start to full boost and then holds.
- **Game units:** the curve is scaled so that:
  - peak **power** matches the engine's `power_limit` top-speed capability (the same drag balance as today);
  - peak **torque** in first gear matches its launch `accel`.

  Class C cars therefore keep their current pace, and every class's numbers stay on the same scale.
- **Display figures:** horsepower and torque for the parts screen are derived from the real layout and displacement, for flavour and comparison. They never feed the physics.
- **Sound fingerprint (used in step 4):** each engine also carries:
  - firing order (evenness);
  - exhaust character (muffled road, sports, open race);
  - intake character;
  - mechanical noise.

## Gearing

The gearbox is solved per build, from engine, turbo, gearbox, tyres, frame and platform:
1. **Top speed:** the drag-limited top speed, from peak power against the platform's drag.
2. **Top gear:** set so that at top speed the engine turns `topRpm` Ã— peak-power revs:
   - `topRpm` is about 1.05 for most gearboxes (just past peak, like a well-geared road car);
   - lower for "long" boxes (over-driven, economical);
   - higher for close boxes (more pull, still clamped under the redline).
3. **First gear:** top gear times the gearbox's **spread** (about 2.9 on a race box, up to about 3.8 on a road box).
4. **Middle gears:** progressive (closer together at the top), like real boxes.
5. **Final drive:** solved from 2â€“4 and the tyre radius, so the physics never needs a "ceiling". A strong engine behind any gearbox reaches its top speed in top gear.
6. **Shift points (automatic, and the AI):** the auto box upshifts where the next gear gives more wheel force, or at the redline, and downshifts to the gear with the most force when braking or pulling.
   - **Manual:** you shift. A soft rev limiter bounces at the redline; downshifts that would over-rev are refused.
   - **Shift time:** cuts drive for 0.45 s (stock H-pattern) down to 0.02 s (seamless). The sound plays the matching rev drop.

**Checks** run on every build, over every combination of parts in a class (in `DrivetrainChecks`, registered in `MigrationBuild`):
- top speed is reached in top gear, with revs at top speed between 0.95 Ã— peak power and the redline;
- every gear is used in a standing-start run;
- the real-gear run stays within a set margin of the envelope (so no gearbox is useless);
- each ladder step is faster than the one before.

## The ladder

Speed scale by class (m/s, top speed of a mid build): C 24â€“40, B 38â€“50, A 48â€“60, S 58â€“74, E 62â€“80. New regions are designed for their class's speeds (doc 45 F9).

Each class has three steps (Good / Better / Best) per slot, plus the local specialities and sidegrades from doc 44. Hillcrest's specialities stay class C (Richard's decision 3). Figures are starting points for the balance harness.

- **Engines:** power_limit / accel, then real flavour.
  - **C:**
    - Stock 1.2 I4, 24 / 5.6, redline 6,000;
    - Workshop tune, 29 / 7.1, redline 6,400;
    - Club motor 1.4 I4, 36 / 9.4, redline 6,800;
    - Sport 1.6 I4 (new), 39 / 10.4, redline 7,200;
    - speciality: Valley V6 2.5 (Hillcrest), 40 / 11.2, redline 6,600.
  - **B:**
    - Twin-cam 16V 1.8, 42 / 11.5, redline 7,800;
    - High-comp twin-cam 2.0, 45 / 12.4, redline 8,200;
    - Tuner V6 3.0, 48 / 13.2, redline 7,600.
  - **A:**
    - Touring 2.0, 50 / 13.0, redline 8,500;
    - Touring 2.0 Evo, 53 / 13.8, redline 8,800;
    - Works 2.0 (individual throttle bodies), 56 / 14.6, redline 9,200.
  - **S:**
    - Race V6 3.0, 58 / 15.0, redline 10,500;
    - Race V8 4.0, 62 / 16.0, redline 9,800;
    - Screamer V10 3.5, 66 / 17.0, redline 13,500.
  - **E:**
    - Hybrid V6, 62 / 16.5;
    - Hybrid V8, 66 / 17.5;
    - Twelve-hour hybrid V8, 70 / 18.5;
    - sidegrade: Economy hybrid, 64 / 16.0, best fuel efficiency.
- **Induction:** power Ã— / torque Ã—, then spool.
  - **C:**
    - naturally aspirated;
    - Street turbo, 1.22 / 1.12, slow spool;
    - Club turbo, 1.45 / 1.23;
    - Sport turbo (new), 1.52 / 1.27;
    - speciality: Twin turbo (Hillcrest), 1.60 / 1.30.
  - **B:** Big-single 1.35 / 1.20, Twin-scroll 1.45 / 1.28 (faster spool), Twin turbo 1.55 / 1.33.
  - **A:** Touring twin-scroll 1.30 / 1.20, Works turbo 1.40 / 1.25, Anti-lag 1.45 / 1.30 (no lag, crackle).
  - **S:** Race turbo and intercooler 1.30 / 1.20, Twin race turbos 1.40 / 1.25, Qualifying boost 1.50 / 1.30.
  - **E:** E-turbo 1.25 / 1.25 (no lag), ERS pack 1.30 / 1.35, Twin e-turbo 1.40 / 1.40.
- **Gearboxes:** gears / spread / shift seconds / type (H = H-pattern, S = sequential, P = paddle, SL = seamless).
  - **C:**
    - Stock 4 / 3.6 / 0.45 / H;
    - Close-ratio 5 / 3.3 / 0.40 / H;
    - Five-speed sport 5 / 3.6 / 0.32 / H;
    - Short-shift 5 / 3.6 / 0.24 / H;
    - speciality: Six-speed (Hillcrest) 6 / 3.8 / 0.28 / H;
    - sidegrade: Long-ratio 5 / 4.0 / 0.35, top revs 0.95.
  - **B:** Six-speed 6 / 3.7 / 0.28 / H, Short-shift six 6 / 3.7 / 0.20 / H, LSD six 6 / 3.7 / 0.20 / H (better exit traction).
  - **A:** Touring close six 6 / 3.2 / 0.18 / H, Touring sequential 6 / 3.3 / 0.11 / S, Works sequential 6 / 3.2 / 0.08 / S.
  - **S:** Sequential dog box 6 / 3.0 / 0.07 / S, Paddle shift 7 / 3.2 / 0.05 / P, Seamless shift 7 / 3.2 / 0.02 / SL.
  - **E:**
    - Endurance paddle 7 / 3.3 / 0.05;
    - Hybrid seamless 7 / 3.3 / 0.02, with the e-motor filling the shift;
    - Twelve-hour box 7 / 3.3 / 0.03;
    - sidegrade: Low-drag long box 7 / 3.6, top revs 0.95.
- **Tyres:** road / dirt grip.
  - **C:**
    - Stock 7 / 3.5;
    - Touring 9 / 5;
    - Road sport 12 / 3;
    - Club sport (new) 13.2 / 3.5;
    - speciality: Hill grip (Hillcrest) 14 / 5;
    - sidegrades: Trail 8 / 10, Rally (Hillcrest) 10 / 12.5.
  - **B:** Tuner sport 14.5, Tuner performance 15.5, Tuner semi-slick 16.5; sidegrade: Tarmac rally 13.5 / 12.
  - **A:** Semi-slick 17.5, Touring race 18.5, Works race 19.5; sidegrades: Wet, Gravel.
  - **S:** Hard / Medium / Soft slick 20.5 / 21.5 / 22.5; sidegrade: Race wet.
  - **E:** Endurance hard / medium 21 / 22, Qualifier 23; sidegrade: Night/wet.

  Grip stays in the arcade scale (Ã—1.65 m/sÂ²). S and E will add downforce with speed rather than bigger numbers.
- **Brakes:**
  - **C:** Stock 14, Sport pads 19, Race pads 25, Big-bore front (new) 28; speciality: Ceramic (Hillcrest) 30.
  - **B:** 31 / 33 / 35.
  - **A:** 36 / 38 / 40.
  - **S:** 42 / 45 / 48.
  - **E:** 44 / 47 / 50.
- **Steering:** response / yaw.
  - **C:** Stock 3 / 0.88, Quick 4.5 / 1.05, Precision 6 / 1.18, Sport rack and wheel (new) 6.5 / 1.23; speciality: Rally (Hillcrest) 7 / 1.28.
  - **B:** up to 8 / 1.38.
  - **A:** up to 9 / 1.44.
  - **S and E:** up to 10 / 1.5.
- **Frame:** mass Ã— on the platform.
  - **C:** Stock 1.0, Light 0.87, Seam-welded (new) 0.84, Stripped shell (new) 0.80; speciality: Carbon (Hillcrest) 0.78.
  - **B to E:** 0.95 / 0.90 / 0.86 on their platform's base mass.
- **Platform:** new slot, one per class, milestone price.
  - C Street: 850 kg, drag 1.00, owned.
  - B Tuner: 880 kg, drag 0.95.
  - A Touring: 950 kg, drag 0.90.
  - S Racing: 780 kg, drag 0.80, downforce.
  - E Endurance: 850 kg, drag 0.75, downforce.

  A platform decides the bodies that fit it and which class's parts can be fitted.

## Stages

1. **Data:**
   - parts schema 2: real engine, induction and gearbox specs, sound fingerprints, class, a platform slot, and chassis renamed Frame;
   - the whole ladder above;
   - save migration (existing saves keep their parts and get the C platform);
   - catalogue checks.
2. **Drivetrain:**
   - `Drivetrain` built from a fitted car;
   - the per-build gearbox solve;
   - gear-aware `DriveModel.Forces` for the car;
   - `EngineForce` becomes the best-gear envelope for the AI;
   - automatic box for AI and player;
   - `DrivetrainChecks`;
   - a keep/reject race comparison (study 208) to confirm the field's pace survives.
3. **Controls:** automatic or manual in Settings. Manual uses L2/R2 to shift and the right stick forward/back for throttle and brake (Richard's choice, like GT7 on a pad); keyboard shift keys too. The HUD gets a gear and rev display.
4. **Sound:** runtime engine synthesis from revs, load, boost and the engine's fingerprint; turbo, shifts, tyres, wind and surfaces; world ambience (CC0 recordings where synthesis is weak, each download batch approved by Richard); music themes per location with a race layer. Design below.
5. **Parts screen and looks:** new stat bars (power and torque curve, gear chart), and part preview models in each class's family look.

## Results so far (30 September)

**Stages 1â€“3 are done.**

**Launch limit (changed from the plan above):** it caps *net* acceleration (drive less drag) at the tuning's acceleration, in every gear.
- **First gear only:** second out-pulled first from rest, and the automatic box looped (upshift at 0 m/s, then snap back to first).
- **Capping drive itself:** A/S/E cars couldn't reach top speed, because drag at 60 m/s (about 22 m/sÂ²) is more than their ladder acceleration.
- **Now:** the net cap fades naturally with drag.

**Long boxes:** the sidegrade long boxes put top gear at peak power (1.0), not 0.95. At 0.95 a weak engine topped out in the gear below.

**Checks:**
- `DrivetrainChecks`: 322 combinations. Each reaches its top speed in top gear, finishes in top gear and uses every gear; revs at top speed sit between 0.93 Ã— peak power and the redline. The slowest against its gearless ideal is 1.52Ã—.
- **Manual box:** holds the limiter, and an upshift takes the gearbox's shift time. A downshift that would over-rev is refused.
- **Stock 0â€“20 m/s:** 5.83 s with real shifts.

**Study 208 (gears, default, against `--legacy-drivetrain`):** all 24 finish in every race. Totals in seconds:

| Race | Geared | Legacy |
|---|---|---|
| Cedar 1709 / 1710 / 1711 | 1964.7 / 1941.0 / 1938.4 | 2144.0 / 2156.0 / 2145.9 |
| Bayview 1709 / 1710 / 1711 | 792.2 / 854.5 / 789.5 | 803.6 / 810.0 / 804.6 |

- The geared field is faster: the net cap pulls harder at mid speed than the old flat cap.
- Bayview 1710 had one assisted reset. A car dived 10 m across the road into the bridge corner, because a boxed-in car could step out any distance. Boxed hold fixes it (study 210).

**Sound:**
- `tools/sound-harness.ps1` renders every engine (with its class's inductions) to `review/sound/`. Each voice is at its firing frequency; cross-plane V8s show their cycle rhythm.
- Cost: 1.5â€“3 ms per second of sound per voice.
- **Lessons:**
  - Even-fire vees need near-identical banks: a 1% pipe difference put a half-order note on a V10.
  - Pulses are placed at fractional sample positions.
- In the game, the `--audio-probe` log shows the carrier arriving through Unity's 3D gains and the output following revs and gears.

## Balance: a grounded class C (1 October)

Richard, a few upgrades into his career: the car gained "far too much performance too quickly" and went "through gears like a Le Mans prototype". He wants a slower, grounded feel, upgrades that are incremental, and enough of them together to challenge the pack.

**Measured before** (the game's physics replicated in `review/tmp/kitsim.py`):
- a fully upgraded class C car launched at 1.8 g and reached 30 m/s (108 km/h) in 2.5 s;
- each gear lasted 0.6â€“1.5 s.

**Why it was so strong:** each engine step raised both power and launch pull, turbos multiplied both, light frames divided by mass on top, and tyres doubled the grip across the class.

**New ladder, class C:**
- **Engines** (power limit / launch pull): stock 24/3.0, Workshop 26.5/3.4, Club 1.4 29/3.8, Sport 1.6 31/4.2, Valley V6 32/4.6.
- **Turbos** (power Ã— / torque Ã—): street 1.10/1.05, club 1.18/1.09, sport 1.24/1.12, twin 1.30/1.15. The Street turbo has a recirculating valve (no blow-off).
- **Frames:** 0.93 / 0.91 / 0.89 / 0.87.
- **Tyres, road grip:** 7 / 8 / 9 / 9.6 / 10.2. Trail stays 7.5 on road and rally 8.5; both keep their dirt grip.

**Classes Bâ€“E:** launch pull and turbo torque are scaled down to match, so each class stays a step above the one below. Power limits (top speeds) are unchanged.

| Kit | 0â€“20 m/s | Top speed | Seconds per gear |
|---|---|---|---|
| Stock | 8.4 s (was 5.9) | 89 km/h | 1.9â€“2.7 |
| Richard's car | 7.0 s (was 4.2) | 92 km/h | 1.5â€“2.3 |
| Street (the pack) | 6.4 s (was 3.3) | 102 km/h (was 115) | 1.9â€“2.6 |
| Club (every fourth car) | 5.6 s (was 2.2) | 114 km/h (was 152) | 1.4â€“2.2 |
| Valley (pole) | 4.4 s (was 1.6) | 126 km/h (was 169) | 1.4â€“2.2 |
| Best class C | 4.1 s (was 1.4) | 126 km/h (was 170) | 1.1â€“2.0 |

**Single upgrades from stock:**
- engines: +15, +38, +50, +62% acceleration;
- turbos: +6 to +17%;
- frames: +6 to +12%;
- tyres: +14 to +46% grip.

**Checks:** the stock-acceleration check now expects 7â€“11 s to 20 m/s. The drafting check runs from 70% to 88% of the car's own top speed.

**Study 212** (the grounded balance, against study 211's baseline on the same code): all 24 finish in every race with no resets. Contacts are down (10â€“30 against 12â€“40) and hard impacts too (0â€“1 per race against 1â€“6).

| Race | Winner (was) | Last car (was) | Winner-to-last spread (was) |
|---|---|---|---|
| Bayview | 30.5â€“32.7 s (24.0â€“24.1) | 40.9â€“41.9 s (36.7â€“42.2) | ~10 s (13â€“18 s) |
| Cedar | 83.8â€“86.2 s (62.8â€“66.4) | 97.6â€“103.9 s (86.6â€“87.9) | 14â€“18 s (21â€“25 s) |

The pack runs closer together and races take about 13â€“15% longer.

## Gearing v2: fixed ratios (1 October)

Richard: a car's maximum speed should be governed by its gear ratios, with power deciding whether it reaches the limit. An underpowered car shouldn't hit the top-gear limiter until it has much more power, which is when it needs a better transmission. GT7's tuning screen is the reference.

**Before:** every build's ratios were solved so top gear landed at its power-limited top speed. The gearbox never limited anything.

**Now:**
- **Fixed ratios:** each gearbox has fixed ratios, its top gear given as km/h per 1,000 rpm (`kmhPer1000` in `parts.json`). The other gears follow the box's spread and progressive spacing.
- **Final drive:** a multiplier (default 1) scales every gear. It's the tuning shop's top-speed slider (doc 48).
- **Top speed** is the best gear's: where its drive meets drag, or its redline.
  - A weak engine never reaches the top-gear limiter.
  - A strong one sits on it (the 75 ms fuel cut bounces), so it needs a longer box or a longer final drive.
  - A long box on a weak engine tops out a gear down (over-geared). The automatic holds that gear on the limiter rather than shifting up into one that can't hold the speed.

| Class C box | km/h per 1,000 rpm | Top gear at 6,500 rpm |
|---|---|---|
| Stock 4-speed | 16.1 | 105 km/h |
| Close-ratio 5 | 16.9 | 110 km/h |
| Five-speed sport / short-shift | 18.3 | 119 km/h |
| Six-speed | 21.0 | 137 km/h |
| Long 5-speed | 22.2 | 144 km/h |

Bâ€“E boxes run 21â€“29 km/h per 1,000 rpm, sized 10â€“20% beyond their class's power-limited top speeds.

**Examples** (replica `review/tmp/gearing2.py`):
- The stock car tops out at 89 km/h, short of its limiter.
- The Valley V6 with a club turbo reaches 106 km/h on the stock box (on the limiter) and 125 km/h on the six-speed.
- Most class C engines on the long box top out in 4th.

**Checks** (`DrivetrainChecks`):
- every combination reaches 97% of its best-gear top speed (95% when bouncing on the limiter) and uses every gear up to it;
- nothing is badly over-geared (at least 88% of the power-limited speed);
- limiter-bound builds end on the top-gear limiter;
- the design intents above;
- in manual, first gear bounces on the limiter (26 cuts in 2.5 s).

## Making gears matter: air drag and the automatic box (1 October)

Richard: in automatic, braking from 80 to 30 mph stayed in 5th, then accelerated as normal. Either the gear ratio made no difference or its effect was too low.

**Two causes:**
1. **The box:** the automatic stepped down only once the revs fell below 55% of the torque peak.
2. **The physics:** the old arcade drag (0.006 vÂ²) forced every engine to carry so much power, to reach its top speed, that a launch cap flattened acceleration in the low gears. At 30 mph, 2nd, 3rd and 4th all sat on the cap (5.7 m/sÂ²), and 5th still gave 4.5.

**Fixes:**
- **Drag:** 0.0035 vÂ² (`DriveModel.DragK`; `--legacy-drag` restores 0.006). Power is rescaled so every car keeps its top speed.
  - The launch limit caps *net* acceleration, so launches are unchanged.
  - The cap now binds only in the low gears. At 30 mph the club car pulls 5.2 m/sÂ² in 2nd or 3rd, 3.9 in 4th and 2.7 in 5th.
  - Upper-gear acceleration is honestly engine-limited: club kit takes 11.8 s to 30 m/s (was 9.8).
- **Automatic box:**
  - Under braking it steps down one gear at a time (each blipped), keeping the revs at 85% of the torque peak or more; coasting, 60%.
  - It never drops into a gear over 88% of the redline.
  - A floored throttle kicks straight down to the best gear with rev room, skipping gears.
  - Checks: braking from 65 to 30 mph goes 4th to 3rd at 3,779 rpm; floored at 30 mph in top gear, it reaches 3rd within 1 s.
- **Shift clunk:** halved (Richard: "understated").
- **Study 214** (new drag against `--legacy-drag`): all 24 finish with no resets; contacts are similar or fewer. Races take a little longer by design: Bayview about +13 s per race (1.4% of the field's total), Cedar about +65 s (3%). Cars brake a little more at Cedar because they carry speed longer. Kept as the requested physics change, not an AI improvement.

## Sound design (stage 4)

The game has no sound yet. Everything is synthesised live from the simulation, so every engine, turbo and gearbox in the ladder sounds different without a sample library. CC0 recordings fill in where synthesis is weak (crowds), each batch approved by Richard.

**Architecture:**
- DSP lives in plain C# classes (`EngineSynth`, `TyreSynth`, `AmbienceSynth`, `MusicSynth`), rendered on the audio thread through `OnAudioFilterRead`.
  - Each car's `CarAudio` plays a constant (DC) clip through a 3D `AudioSource` and multiplies it by the synthesised signal, so Unity's distance and panning apply.
  - The main thread writes the car's state (revs, load, boost, gear, shift events, slip, surface) each frame; the audio thread reads it and smooths it.
- `AudioDirector` gives the nearest cars (up to 8, plus the player) a live voice; the rest are silent. It also holds the volume settings (Master, Effects, Music) and does nothing in batch mode (race studies).
- The same DSP renders offline in the editor checks (`SoundChecks`). They check each engine's pitch against its firing frequency, levels, NaN and clipping, and the cost per voice. They also write a WAV rev sweep of every engine to `review/sound/` for listening.

**Engine (firing-pulse synthesis):**
- **Crank:** a phase runs through the 720Â° four-stroke cycle at the live revs. Each cylinder fires at its angle in the firing order, into its bank's exhaust:
  - I4: even, 180Â°;
  - V6: 120Â°, each bank 240Â°;
  - flat-plane V8: each bank an even 180Â° (the rasp);
  - cross-plane V8: 1-8-7-2-6-5-4-3, each bank uneven 180/270/180/90Â° (the burble);
  - V10: 72Â°.
- **Pulse:** each firing is an exhaust blowdown pulse (alpha-function shape) with some combustion noise. Its size follows load (throttle Ã— torque at these revs) and displacement per cylinder. Small cycle-to-cycle jitter gives life; lumpy race cams add more at idle.
- **Exhaust, per bank:** a pipe (a feedback delay at the pipe's length, open-end reflection) into a muffler (resonant low-pass plus a body resonance):
  - `road`: long pipe, cut-off about 700 Hz, the most muffled;
  - `sport`: about 2 kHz;
  - `race`: short headers and open pipes about 6 kHz, with a gentle saturation for rasp.
- **Intake:**
  - `box`: soft;
  - `cone`: induction hiss;
  - `itb` and `trumpets`: a strong induction howl, the firing pulses through a resonance.
- **Mechanical:** valve-train tick. Straight-cut gear whine on sequential, paddle and seamless boxes.
- **Overrun:** at high revs off throttle, weak pulses. Sport and race exhausts pop and crackle.
- **Limiter:** skips firings in a pattern, so it bounces.
- **Hybrid (E):** the motor-generator's whine rises with road speed, driving on throttle and harvesting under braking.

**Turbo:**
- A shaft-speed state follows boost, which gives the whistle (two partials), plus an intake whoosh.
- On a lift from boost:
  - with a blow-off valve: a psssh;
  - without one: compressor flutter.
- **Anti-lag:** bangs off throttle while the whistle stays up.
- **E-turbo:** no lag, plus an electric whine.

**Shifts (from `GearState.ShiftEvent`):**
- `H`: clunk (a low thump and a click);
- `S`: a metallic clack, and an ignition-cut bang on hard upshifts;
- `P`: a quieter clack with a throttle blip on downshifts;
- `SL`: a tick.

The rev drop and blip come from the gearbox's own revs.

**Tyres and world:**
- Tyre squeal from grip use near the limit (a resonant, wobbling squeal plus scrub).
- Rolling noise by surface: an asphalt roar, gravel crackle, a grass swish.
- Wind rising with speed for the player's car.
- Impact thuds from contacts.
- **Ambience (synthesised):**
  - surf along Bayview's coast (wave swells and wash);
  - the Silver Falls roar;
  - river babble;
  - gusting wind and rustling near trees;
  - birds: gulls on the coast, songbirds inland.
- **UI:** soft toy-like pings for menus, purchases and toasts, and race start beeps.

**Music (procedural, per location, with a race layer):**
- A small sequencer plays composed themes on synthesised instruments: plucked strings (Karplus-Strong), FM electric piano, marimba, pads, bass and drums, with a reverb.
- Each location has an explore layer. Races add drums and a driving bass over the same theme, lifting on the last lap.
- Themes:
  - **Bayview:** a sunny coastal G major at 96 bpm (ukulele, marimba, shaker).
  - **Hillcrest:** warm folk in D major, 6/8 (picked guitar, reed melody, upright bass).
  - **Cedar:** airy E dorian (electric piano, pad, brushes).
  - **Silver Falls:** shimmering A lydian arpeggios.
  - **Garage and menus:** a lounge groove.

