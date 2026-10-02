# Next steps (2 October 2026)

**Plan of record (29 September):** [doc 45](45-roadmap-and-feature-spec.md) sets the order of work, Phases 0–5; the parts ladder is in [doc 44](44-parts-tiers-and-progression.md). The campaign is at least 12 hours and open-ended. Next is **Phase 0**: world grid with per-tile map and fog, Racer records, radar chart, a traffic-aware launch, save and IDs, and track standards.

**Update, 2 October, evening (Richard away): the wider game** ([doc 56](56-wider-game-round.md); a running log of this round).
- **Swept contacts for every car (kept, study 215).**
  - All 144 cars finished, with no resets.
  - Contacts went from 115 to 54, and total time from 9617.0 to 9616.6 s.
  - Physics cost is unchanged (study 217).
  - `--speculative-contacts` restores the old mode.
- **Hillcrest houses.** Three new terraces break the street skyline: a four-storey palazzetto with a rooftop altana, a two-storey cottage under a pitched roof, and a painted house with a gable and oculus. Roofs vary between terracotta shades and grey slate. The layout is unchanged and so is frame time (5.00 ms mean).
- **Castle gate.** The gatehouse now follows Via Crest, so the drawbridge lies under the road (it was 2.5 m off, across the road's edge, and floated 0.6 m up). The drawbridge rests on the slope and hangs on real chains.
- **Road paint.** Edge lines no longer saw-tooth round tight bends, on every valley road.
- **Houses, again.** Painted trompe-l'oeil windows on every house's side walls (no more blank row ends), and roof colours on the Bayview high street (two slate).
- **Stock 1.2 sound:** the Renault R4, Morris Oxford and Fiat 500 recordings were tried and none beat the C2 (details in doc 56). It stays at 7.7, and a better recording is needed.
- **High-comp 2.0 voice:** now the DS3 WRC, 8.9 in game (was 7.9), so 15 of 18 engines are at 8 or above. Works 2.0 and Hybrid V8 stay at 7.9; the Viper's 8.3 for the Hybrid V8 comes with a chipmunk score of 2.3, so it wasn't taken.
- **Tools:** `--view-shots` takes a still from any camera position (`capture-map.ps1 -Rendered`).

**Update, 2 October, late afternoon (Richard away): engines to 8 in game, then the wider game** ([doc 55](55-engine-scorecard.md), "Round two").
- **Engine sound:** the offline in-game average went from 5.5 to 8.3, with 16 of 18 engines at 8 or above (Stock 1.2 7.8, Hybrid V8 7.9). This came from new recordings from the Sonniss 2024 bundle chosen by score (Ferrari 458, McLaren 720S, DS3 and Xsara WRC, Puma Rally1, Citroën C5), measured gearchanges and coasting in the banks (v6), longer sustain loops, gentler exhaust shaping, and fairer measures.
- **Title screen:** the surf no longer plays at full there (no ground under any sample now means no world, not open sea).
- **Class S gearboxes:** taller (27 to 28.5 km/h per 1,000 rpm, from 21 to 22): the S V8 was on its limiter near 218 km/h.
- **E-class AI:** weaker drivers no longer crash at E speeds. Aero grip and the real platform drag fixed it: all 0.4 and 0.6 drivers ran clean on three seeds, and the player's autopilot in an equal-car Duke race finished 2-3 s behind him with no recoveries (this morning: 172 s with two). A skill-scaled braking reserve was tried and made no difference, so it was removed.

**Update, 2 October, evening: the engine scorecard** ([doc 55](55-engine-scorecard.md); [listening page](https://claude.ai/artifact/8zpJcfKs294HWgpAee59PW), private).
- **The method:** every recorded engine is driven along its real recording's own drive (revs, throttle and gearchanges read from it) and scored 0–10 on pitch, tone, character, lift-off, gearchanges and rise with revs.
- **Round one:** the average went from 5.8 to 6.5. Lift-offs now fade by each engine's measured drop instead of falling 6–8 dB at once; the low rumble is back; the made-up roar and gearchange thump are gone over recordings.
- **Next round:** Fabia clicks on flat-shifts; per-engine fade times (Skyline); the Escort's level clamps; better exhaust or external references (downloads need Richard's go-ahead).

**Update, 2 October: the Duke's Challenge** ([doc 54](54-dukes-challenge.md); `claude-additions`, not committed).
- **The final boss.** Race Duke Rolo 1v1, three laps of Hillcrest Raceway, against the Regale on the measured maximum build. The first win pays 50,000 credits and gives you the Regale.
- **Aero grip for S/E cars.** Downforce now loads the tyres (×1.86 grip at 75 m/s). The AI's braking plan for aero cars keeps a 30% lateral reserve. The Duke now laps in 30.7–31.1 s, cleanly on nine seeds.
- **Test drives.** Two new desktop shortcuts: Max Build Hot Laps (timed laps of the raceway) and Max Build Test Drive (free roam on a save copy, by the Duke).
- **After Richard's first drive** (his best lap 28.02 s, against the AI's 30.7 s):
  - Platform drag is now real, and the E boxes are taller: 332 km/h on the long box.
  - Race platforms sit on stiff springs, so no more nose lift.
  - Turbo and hybrid-whine sounds are off (`--turbo-sound` / `--hybrid-sound` bring them back).
  - **S gearing is still short** (the S V8 tops out at its limiter around 218 km/h): retune it with the class S phase.
- **Parked.** The Duke's drive (`--duke-drive`) waits for a traffic overhaul that can handle a car at pace.
- **For later:**
  - (a) New, professional-looking tracks for classes A and up, for higher speeds and real cornering (Richard: a Le Mans car on Tsukuba).
  - (b) E-class AI for weaker drivers: they still crash at E speeds, see doc 54.
  - (c) Peak and floor: the maximum build is gear-limited at 76 m/s, and a slower stock car would make early upgrades feel bigger. Both are keep/reject questions once Richard has driven it.
  - (d) Possibly the aero cars' braking reserve for every car, as a keep/reject study.

**Update, 1 October: Richard's play-test fixes** (doc 51, "Richard's first play-test"; still on `claude-additions`, not committed).
- Speed traps no longer stop the car.
- Valley junctions come from a road network (doc 52).
- Photo mode opens on D-pad Up.
- Crickets are muted, and the fireworks wait for full dark and announce themselves.
- The car is held but revs through the train and castle countdowns.
- Glowing finish beacons for both events.
- The engine above the recording is reworked: a walked sustain loop, and pulse overlap-add above 1.5× (doc 47, last section).
- **The Regale** (doc 53): Duke Rolo's prototype, a sculpted toy Le Mans car with a painted purple-and-gold livery (in-world sponsors), parked at the castle as a teaser of the top classes.

**Resolved, not a bug:** the game was on screen 2, which was switched off. The title screen was simply waiting for input, which matches the log ending before `ADVENTURE_READY`. The small bug in the last bullet below (ambience at the title) still stands. The original note is kept below for the record.

**(Was) Open first next time: the game hung at launch (30 September, 19:06).** Richard's normal launch (title screen, 2560×1440 borderless fullscreen) stopped responding.
- **What the log shows.** Player.log ends at `LITTLE_ROAD_DISPLAY applied=FullScreenWindow size=2560x1440`, with no `ADVENTURE_READY` and no exceptions. After 39 minutes the process had used about 4 cores flat out (10,287 s of CPU). The audio thread kept playing the ambience.
- **What that rules in.** It hung either on the title screen or in the world build after Continue. The tests never ran that path at full length: `--title-review` quits after 3 s, and every other run skips the title.
- **A clue, and a small bug.** At the title the ambience runs with no world under it, so every sea-probe ray misses and it reports "all sea" (the loud waves). The title should mute it, or the ambience should wait for the world.
- **How to reproduce.** Launch with no flags at native fullscreen, then press Continue. If needed, add logging around `TitleScreen.Run`, `AdventureSession.Load`, and each setup step in `AdventureGame.Start` (the new parts: `SpeedTraps.Setup`, `Fireworks.Setup`, `TrainRace`, `PhotoMode`).

**Update, 30 September, late night: Claude's own additions.** Richard: "go to town adding whatever you like". Details in [doc 51](51-claudes-additions.md), on branch `claude-additions` (not committed yet).

- **Speed traps:** seven cameras, with star marks computed from each road and the parts ladder.
- **A horn** (H / L3): folk answer, traffic beeps back, and Bert's train whistles.
- **Beat the Train:** race the real train between the stations.
- **Photo mode** (P / D-pad Up): five looks, depth of field and a postcard frame.
- **Pip's photo requests:** six sights to photograph.
- **Fireworks** over Bayview harbour at night.
- **A RECORDS tab** in the pause menu for all of the above.

**Update, 30 September, night: Richard's fixes after the music round.** See [doc 50](50-title-saves-and-class-gating.md) and [doc 47](47-parts-drivetrain-and-sound.md) (last section).

1. **Engine top half.** Above a recording's top revs, the engine played the lift-off recording. It now stretches the pull and takes the extra brightness back out.
2. **Bayview Halt.** You couldn't get back onto the road from the station grass (0 of 32 test runs). It now has ramps (43 of 48; the rest ran into scenery). The mainline's descent from the tunnel now sits on a gentle embankment.
3. **Class gating.** Shops show only your class and below, and Class B stays shut until it has races.
4. **Title screen with three save slots.** Each slot has its own autosave and saved game; save from the pause menu.
5. **Valley activities: on hold.** Richard has a better idea to share next.

Open:
- One full-throttle start from rest stalled on the Halt's gravel ramp (it passes at part throttle).
- The 61–62 mph engine note.

**Update, 30 September, late: music.** There is a theme for each area (Bayview and Hillcrest) and a race version of each; the race version has a last-lap lift. See [doc 49](49-music.md). The work is on branch `music` and not committed yet. The next sound work is effects (tyre squeal and others) and the parked 61–62 mph engine note (club motor with the Street turbo).

**Update, 30 September, evening: parts for every class, real gears, manual shifting, engine sound.** Design and results in [doc 47](47-parts-drivetrain-and-sound.md). On branch `racing-ai-v2` (not committed yet).

1. **Parts ladder:** 129 parts across C, B, A, S and E, plus five platforms. Every engine is a real engine (layout, cylinders, revs, torque curve and sound fingerprint). All 29 older part ids are kept, so saves still load.
2. **Real gears:** `Drivetrain` solves each build's ratios so top gear reaches its top speed; `GearState` drives the car in a real gear with shift cuts, revs, boost and a limiter.
   - `DrivetrainChecks` passes all 322 engine × induction × gearbox combinations: each reaches top speed in top gear and uses every gear.
   - **Launch limit:** it caps net acceleration in every gear. A first-gear-only cap made second gear out-pull first, and the automatic box looped shift/snap on the line.
   - **Study 208** (gears against `--legacy-drivetrain`): the field is faster (Cedar about 9%), and all 24 finish in every race.
3. **Automatic or manual** (Settings > Gears):
   - **Pad:** R2/L2 shift, right stick up/down for throttle/brake.
   - **Keyboard:** Left Shift / Left Ctrl shift.
   - **Dash:** both dashes show the gear, AUTO/MANUAL, and a 16-lamp rev strip with a shift light.
   - A recovery puts the box back in first.
4. **Engine sound, synthesised live:**
   - firing pulses at the real crank angles through per-bank pipes and mufflers;
   - intake, turbo whistle, blow-off or flutter, anti-lag bangs, overrun pops, the limiter;
   - the hybrid motor, straight-cut whine and shift clunks;
   - tyres, surfaces, wind and impacts.

   The eight nearest cars have a voice, and there are volume settings. `tools/sound-harness.ps1` renders every engine's drive to `review/sound/*.wav` and checks its pitch against its firing frequency. The engines are measured, not yet listened to: **Richard's ears are the test.**
5. **AI fixes from the studies:**
   - **Boxed hold** (study 210): a boxed-in car sidesteps at most 2.5 m. It used to jump 10 m across the road, which put a car into Bayview's bridge.
   - **Pass reach guard** (study 209): no dive to a side the car can't reach before braking.

   Both met the keep rule, by margins near noise, and are now on by default (`--no-boxed-hold`, `--no-pass-reach-guard`).
6. **Still open (AI):**
   - At Bayview's bridge corner (progress 330–345), a car on a lane far off the racing line can get its target speed too late and run wide (study 210, seed 1709).
   - The physical recovery then reverses along the road for 15–30 s instead of turning round.

**Update, 1 October, evening: Richard's fourth listen.**
1. **Engine sound follows refits.** It used to change only after a loading screen.
2. **Turbo:** the whoosh swells only while boost builds, with a faint whine then; nearly silent at steady boost. Turbo cars are slightly muffled.
3. **Race start:** silent until the countdown, then a 1.5 s fade-in. Countdown beeps: three short, then a long higher GO.
4. **Birds:** real field recordings (forest and river beds, stereo), very faint and gone above about 12 m/s. Night crickets are synthesised individuals; the recorded chorus sounded like a marching band.
5. **Limiter:** a real 75 ms fuel cut that bounces.
6. **Automatic box:** no more 4th-5th hunting near top speed (the 60 mph stutter). It needs 1.2 s after an upshift, 85% rev room and 15% more pull before a downshift.
7. **Launch revs** climb with the car as the clutch slips; they used to sit flat until 10 mph.
8. **Shifts:** the pulling recording carries on through a gearshift, quieter, instead of cutting to the coasting one.
9. **Test loop** (`test-loop.ps1`): 2.5 miles with two 1-mile straights. Richard's club kit shifts cleanly to 5th at 55 mph and tops out at 67.6 mph.

**Update, 1 October, night: gearing v2 and the tuning shop** (doc 47 "Gearing v2", doc 48).
1. **Fixed ratios per gearbox** (km/h per 1,000 rpm in top gear). Top speed is the best gear's, where drive meets drag or the redline.
   - A weak engine never reaches the top-gear limiter; a strong one bounces on it and needs a longer box. For example, the Valley V6 with a club turbo reaches 106 km/h on the stock box and 125 km/h on the six-speed.
   - A long box on a weak engine tops out a gear down, and the automatic holds that gear on the limiter.
   - **Study 213:** all 24 finish with no resets, and pace is neutral.
2. **Tuning shop:** "Tune gearbox" at Mara's and Gio's garages.
   - **Tune:** the final drive (0.80–1.25) on every box, and each gear on the sport boxes and up. Auto-tune has three presets (acceleration, balanced, top speed).
   - **Screen:** GT-style gear chart, power and torque curve, and the car's numbers (top speed and its gear, 0–60, 0–100 km/h), with a verdict: on the limiter, over-geared or well matched.
   - **Test drive:** a timed lap of the town's circuit (lap time, top speed, 0–60), then back to the screen.
   - **Storage:** tunes save per gearbox; AI rivals race their box's defaults.
3. **Compile check:** it now leaves out engine modules the project hasn't enabled (Screen Capture passed the quick check once and failed the real build).

**Known issue (parked, Richard 1 Oct):** a strange engine note around 61–62 mph on the club motor with the Street turbo. It persists after the gearbox-hunting fix, and it's still to be diagnosed with `test-loop.ps1 -Capture`.

**Next:** the gearing model (fixed ratios per gearbox, where power decides whether the car reaches the limiter), then the tuning shop with test drives.

**Update, 1 October, afternoon: sound polish, a grounded class C, world ambience.**
1. **Engine sound, third listen fixes:**
   - **Loudness:** each recording is levelled to a smooth curve (pulling rises 2x from low to high revs, coasting at 65%). The Escort's pulls had run 19x quieter at low revs than at the top. Engine levels are raised and Master now defaults to 100%. A captured race now holds 0.18–0.23 RMS pulling across the revs, with no clipping.
   - **Lift-off:** the old sound was "like a bus's air brakes". The Street turbo is now recirculating (a soft, dark whoosh). Blow-off valves on the bigger turbos give a short "pssh" whose tone falls, at a quarter of the old level.
2. **Grounded class C** (doc 47, "Balance"):
   - Launch pull, turbo multipliers, frame weight savings and tyre grip are all compressed. Stock takes 8.4 s to 20 m/s, the pack 6.4 s, club kit 5.6 s, the pole car 4.4 s and the best class C kit 4.1 s (they were 5.9 / 3.3 / 2.2 / 1.6 / 1.4 s).
   - Top speeds run 89–126 km/h (were 89–170). Gears last 1.1–2.7 s (were 0.6–1.5).
   - **Study 212:** all 24 finish with no resets, fewer contacts and a much tighter pack (winner to last at Bayview about 10 s, was 13–18 s).
3. **World ambience** (`AmbienceSynth`, `Ambience`):
   - surf by the sea, its level from sampling the ground in a ring around the listener;
   - gusting wind, stronger high up;
   - the Silver Falls roar at the river's steepest drop;
   - river babble that follows the nearest stretch of the Silver river;
   - gulls near the sea and songbirds inland, quiet at night.

   `sound-harness.ps1 -Filter ambience` writes 20 s of each to `review/sound/ambience/`.

**Next (with Richard):** music, tyre squeal and the other effects.

**Update, 1 October, morning: Richard's sound notes, first pass.**
1. **Whine:** this was the turbo whistle. Richard's car had the Street turbo, and the whistle was a near-pure tone climbing to 8.8 kHz. It carried 14× the high-frequency energy of the engine itself (a layer audit with `sound-harness.ps1 -Filter layers:<engine>+<turbo>`). The turbo is now mostly a spool whoosh with a faint whistle under 3.5 kHz, and the energy above 4 kHz is down about 7×.
2. **Reverse:**
   - **Manual:** reverse is a gear, as in GT7 and real cars. Down from first at a standstill selects R; in R the throttle drives backwards and the brake brakes. In a forward gear the brake holds the car at a stop.
   - **All modes:** the forward pedal now brakes a car that is rolling backwards. It used to do nothing, which is also why AI recoveries reversed for 15–30 s.
   - **Study 211** (`--legacy-reverse` A/B): identical except the one race with a recovery, Bayview 1709. Its last car finishes at 42.2 s instead of 74.6 s and the total is 32 s better. Kept.
3. **Listening test:**
   - **In the game:** Settings > Engine sound test, or `--sound-bench <engine id>`. Pick any engine and induction; Cross / Space runs the test (idle, pull to max revs, hold on the limiter, lift back to idle); R2 / F revs it freely.
   - **Harness:** `sound-harness.ps1` writes the same test for every engine to `review/sound/`.
4. **Realism: done with real recordings (1 October, midday).**
   - **Downloads:** 15 engine recordings (about 1.2 GB) from Sonniss' official GDC 2021-2023 zips. `tools/fetch-sonniss.py` reads each zip's index with range requests and pulls single files. They sit in `../little-road-audio-sources/`, outside the repo.
   - **Banks:** `tools/engine-grains.py --config tools/engine-banks.json` turns them into 12 grain banks, about 30 MB, in `unity/Assets/Resources/Engines`. Check images for each recording are in `review/grains/`.
   - **Rev tracker:**
     - harmonic sum plus a Viterbi path;
     - quiet stretches (engine off) are untrusted;
     - on the V8, V10 and flat-12 it follows the bank pulse line (`trackCylinders`).
   - **Mapping:** each part names its recording (`parts.json` `sound`). Class C is now five different cars: Citroen C2, Golf, Escort RS1800, S2000 and Skyline. S and E rev past every road recording (stretched 1.6-2.6× at the redline), so they need race recordings later.
   - **In the game:** the recording replaces the modelled exhaust; the part's exhaust type tones it (road, sport, race); the turbo, blow-off, shift and hybrid layers stay synthesised. `--synth-engines` brings back the old model for comparison.
   - **Checks:** `SoundChecks` loads and plays every bank. `sound-harness.ps1 -Filter recorded` writes every engine's listening test to `review/sound/recorded/`.
   - **Richard's second listen (1 October):** much better, and coasting sounds natural. Revving up "flickered between two rev noises at once" and sounded distorted. Four causes, all fixed:
     1. **Two engines at once.** At part throttle, pulling and coasting grains came from two separate streams, played together. There is now one stream, switching sweeps with hysteresis; part load shapes its level and brightness.
     2. **Crossfade overlap.** Every cycle join overlapped two different stretches of audio. Grains now carry a tail (the recorded start of the next cycle), so a join to the recorded successor reproduces the recording.
     3. **Scattered cycles.** Banks were thinned to scattered cycles per 50 rpm. They are now whole sweeps (version 3 format), and playback steps through consecutive cycles.
     4. **Distortion.** The player's ×1.7 boost sat before the soft limiter. It is now the speaker volume.
   - **`--audio-capture <file.wav>`** records the player's engine as synthesised, with revs, gear and throttle every 10 ms, to show exactly what was heard. It found cause 1, because the throttle is mostly partial while pulling.
   - **Lesson:** batch-mode checks run without sound. After any sound change, run a windowed race with sound before handing over. A setup crash (volume set before the source existed) gave Richard a black screen.
5. **Earlier plan for the realism work (superseded by item 4): granular synthesis from real recordings.** This is the racing-game standard.
   - `tools/engine-grains.py` tracks the revs through a recording and cuts one-cycle grains into a bank.
   - `GrainSynth` plays the bank at any revs. It replaces the modelled exhaust; the turbo, shift and hybrid layers stay procedural.
   - Tested end to end on a synthetic source (`sound-harness.ps1 -Filter grains:<bank>:<engine>`).
   - **Source:** the free Sonniss GDC archive (royalty-free, no attribution, no AI training) has Pole Position Production car recordings. The Citroen C2, Golf, Escort RS1800, Honda S2000, Skyline R33, Skoda Fabia R5, Audi RS4 V8, Viper V10, Testarossa, Kia Niro Hybrid and sprint cars cover the ladder.
   - **Needs Richard to download them.**
5. **Parts sounding alike:** with recordings, each engine step maps to a different car's engine.

**Richard's first listen (30 September, evening). Fix these before any more sound work:**
1. **A very high-pitched noise rises with acceleration.** It's piercing (it bothered the dog).
   - **Suspects:**
     - the turbo whistle (1.6–8.8 kHz with boost);
     - the intake hiss (it grows with revs squared);
     - the valve-train tick (high-passed at 3.5 kHz);
     - pipe-comb resonances;
     - aliasing from very short pulses at high revs.
   - **Next step:** solo each layer in the harness and measure the energy above 4 kHz.
2. **The first three C engines (stock, workshop, club) sound the same** when swapped in the save. The differences need to be clearly audible: displacement, exhaust, cams, intake.
3. **They don't sound like engines:** "more like a PS1/PS2-era kart game". The sound needs to be guttural and real.
   - **Next step:** research better approaches:
     - a physically modelled exhaust (waveguides, as in engine-sim);
     - granular or loop-based synthesis from CC0 engine recordings (Richard approves downloads);
     - hybrid layering.

   Rethink the model rather than tweak it.
4. **Manual gears into reverse:** with the brake and the downshift on the same trigger (L2), you can't brake without shifting. Study how GT7 handles reverse and braking on a pad and wheel, and copy it.
5. **Wanted: a listening test.** Pick any engine and hear it rev from low to high, hold a few seconds at max revs, then come back down, so Richard can sense-check engines as they change. It should run in-game and play through the game's own audio, plus the harness WAVs.

**Next (after the fixes above):** ambience, UI sounds and the location music (doc 47 stage 4), then the parts screen (stage 5), then map expansion.

**Update, 30 September: Racing AI v2, clean-up, Bayview and Silver Falls.** All on branch `racing-ai-v2` (not merged).

1. **Racing AI v2** (`RacerDriverPolicy`; research, design and every study in [doc 46](46-racing-ai-v2-design.md)) is the default driver. `--first-ai` races the first AI.
   - Beat the first AI in the keep/reject study (205): all 24 finish, no resets, about 20 s better per race.
   - After the ability spread was widened it's about level (207), because the roster's weaker drivers are now genuinely slower.
   - Abilities: all-worst is 49% slower than all-best on a solo lap; each ability counts.
2. **Race preview:** DR (driver rating 1–20) beside PP on every grid tile and a DR rosette. Hover it, or hold Triangle / R, for the radar.
3. **Clean-up:** the Godot prototype, the Cowork runner, 19 obsolete tools, 17 superseded docs and old evidence are gone (all in git history). `review/` went from 7.8 GB to about 0.3 GB. `cleanup.ps1` keeps it that way.
4. **Bayview:**
   - a seaside high street of terraces with bunting and café parasols;
   - meadow grass on the verges and green;
   - olives, blossom and poplars on the hills;
   - lavender and vine plots.
5. **Water:** sea contact foam at walls, piers, hulls and rocks. **Silver Falls** rebuilt as a real cascade (`LittleRoad/Waterfall`); the river no longer overhangs the gorge.
6. **Parallel races:** `tools/run-pace-parallel.ps1` runs six at once. First-AI races run alone because its worker threads aren't repeatable under load.

**Next** (Richard's order):
- AI polish: club cars in Cedar's crowded back half; fewer light side-by-side touches; a calm two-car defend test to watch.
- Then the world expansion and the parts expansion (doc 45 Phase 0 onwards, doc 44).

**Update, 29 September, night: seven fixes before the parts doc.**

1. **Back from a race preview** now returns to whoever offered the race: Mara and Gio keep "Fit parts".
2. **Look behind:** both views are fixed, and flipping back restores the forward camera exactly where it sat.
   - **Why it zoomed:** the forward camera trails the car. On flipping back it snapped to the untrailed spot, then drifted back out, which read as a zoom.
   - **Also fixed:** the cut now skips race motion blur and the anti-aliasing history.
3. **Starting grid:** the pole car's nose is on the start line. Rows of three are 11 m apart (about 7 m clear) with a 1.8 m stagger; before, the front row was 8 m back and rows were 5.5 m apart.
   - **Baselines:** new ones are in `review/unity-35/frame-cost-128` (Cedar and Bayview, seeds 1709–1711) and `review/class-sort/grid29-*`. Earlier baselines are superseded and have been kept, not deleted.
4. **Map sea:** coloured by distance from the coast (surf, reef turquoise with patchy reef, then deep blue), from a land mask of 512² downward rays. `WorldMap.Reef`.
5. **Fog:** opaque, and only where you've been. No free Bayview or whole-town reveal; viewpoints still reveal what you can see from them. Masks saved before this (v1) are discarded.
6. **Blur:** the race motion blur is now camera-and-objects, so your own car stays sharp. At night, lamps leave short light trails above about 22 mph (`LightTrails`, `LittleRoad/LightTrail`).
7. **Collision boxes** are fitted to each body: the model's length, and its shoulder-and-tyre width without mirrors (`CarBodies.ShoulderWidth` / `Length`, measured in Blender).
   - **Before:** one shared box forced to 2.32 m wide left 27 cm of air each side of the Puddle, while the longer field cars sank up to 54 cm into each other.
   - **Now:** 1 cm nose-to-tail and 2 cm at the shoulders at contact.
   - **Check it:** `--contact-review` (Richard's test) shoots pairs of cars from above. See `review/map-sweep/contact-old` and `contact-perbody-2`.

Smoke21 passes.

**Update, 29 September, evening: Richard's eight general improvements.**

1. **Fog of war** on the minimap, full map and pause map. It reveals 175 m around the car, 520–620 m at viewpoints and the castle, and whole towns on arrival. It's saved with the adventure. Details are in `docs/ui-design.md`.
2. **Parts tiers plan:** `docs/44-parts-tiers-and-progression.md`.
   - **Classes:** C Street, B Tuner, A Touring, S Racing and E Endurance.
   - **Ladder:** three steps per slot per class plus sidegrades, about 125 parts, with names and a look for each class.
   - **Economy:** about 7.8 h of racing to reach podium builds, and 20 events in 6 regions.
   - **Waiting on:** Richard's four decisions.
3. **Places:** a one-time DISCOVERED card, then the name under the minimap. Compact toasts replace the stamped ones.
4. **Race stutter:** races now pace to the display (72 fps on 144 Hz). Only 1% of frames miss the refresh, down from 22%. See `review/unity-35/frame-cost-127-review.md`.
5. **Parts screen overlaps:** fixed.
6. **Talk prompt:** half the size.
7. **Controls:** no button hints while driving. There's a CONTROLS tab in the pause menu instead.
8. **Look behind:** the camera is locked ahead of the car.

The parts review (34 checks) and smoke20 pass.

**Update, 29 September, afternoon: Richard's six feedback items are done.** Details for the UI are in `docs/ui-design.md` ("As built").

1. **Unreadable loading text.** The loading-screen heading was navy text on a navy outline; it's now brand orange. The result-screen coin mark is a plain label.
2. **Cars clipping into the world.** `StationarySpot` slides each standing car to the nearest clear spot, and townsfolk with no clear spot are dropped. `--car-audit` now finds 1 of 108 (Orla at (275, −927) against a kit `Collision_Body`; the audit now names the parent object, so the next run will say which).
3. **Staring cars.** Parked cars no longer turn to face you.
4. **UI overhaul:**
   - a new HUD: speed dash, a timed objective and a contextual controls hint;
   - the pause hub: MAP (regions, hover counts, full map), COLLECTION, CAR and SETTINGS tabs;
   - back goes one level;
   - glyph-aware prompts in the shops.

   Captures are in `review/map-sweep/pause-2`.
5. **Road-end lips.** Gravel end ramps are added, but skipped where a road runs straight onto a through road: the one at the gate junction stood in the junction.
6. **Castle Climb event** (`CastleClimb.cs`):
   - **Route:** bollards line the route, and every side road gets a close-set line 7 m beyond it. Sharp junction corners are left open, because an edge line there trapped cars that ran wide.
   - **Crowd:** parked cars, townsfolk and valley traffic move behind the bollards. Everyone within 40 m bounces and cheers ("Go go go!").
   - **Zipline:** closed during the climb.
   - **Hard mode:** par 80 s instead of 85, 750 credits instead of 300, and 20 obstacles, each a hay bale across one lane with cones leading into it. It has its own badge.
   - **Evidence:** the autopilot on Via Crest takes 49.1 s with the closure, identical to the run without it and with zero contacts (`review/map-sweep/climb-crest-*-11`). Hard mode arrives in 81.8 s even with the autopilot's clumsy dodging.
   - **Known limit:** this autopilot overshoots the quay-to-Via Crest hairpin from a standing start, with or without the closure. `--climb-from 112` tests past it.
   - **Captures:** `review/map-sweep/climb-3`.

**Parts browser, all categories (later on 29 September).**
- **Browser:** the 3D browser now handles all seven categories. The category stickers, LB/RB or [ / ] switch in place.
- **Models:** every part has a Blender model (`source-art/build_part_previews.py`), with shape differences between tiers. That includes the Valley V6, which no longer borrows the Club motor's model.
- **Stats:** each category compares what it actually changes, such as stopping distances for brakes, road and gravel grip for tyres, and weight for chassis.
- **Locked parts:** they now show in the carousel as "CLASS X / LOCKED".
- **Checks:** the parts review (`review/ui-screens/all-parts-1`, 29 checks) and smoke18 pass.

Smoke17 passes. The build runs 1,060 editor checks, including the new `InterfaceChecks`.

**Still to check by hand:**
- the pause hub with a real pad;
- the Hard par, which is set from autopilot times rather than a human run;
- whether 4.5 m bollard spacing reads as "closed" enough on the open valley stretches.


**Update, 29 September.** Items 1–6 are done:
- race warm-up;
- benchmark ignores its screenshots;
- raceway rubber;
- sea rocks seated;
- causeway end already framed by the Headland Gate, so nothing added there;
- scorecard third pass.

Benchmark db11: worst frame 19 ms, none over 33 ms.

**Item 8, the minimap, is done** (`Minimap.cs`, `LittleRoad/Minimap`, `WorldMap.Bake`):
- **Map image:** the world is baked from above at a fixed afternoon behind the loading card.
- **Corner map:** bottom-right, heading-up, and zooms out with speed. Marks:
  - residents in their colours;
  - Pit Stops (orange squares);
  - the zipline (yellow diamonds);
  - the objective (a pulsing star that waits on the rim when off the map);
  - a north badge on the rim.
- **Full-screen map:** M or the View/Select button opens it north-up with the atlas names. Names are placed by priority with overlaps dropped, and the car waits while it's open.
- **Checking it:** `--minimap-review` captures the corner map in three places plus the full map (see `review/map-sweep/minimap-2`). `--no-minimap` turns it off.

Item 7 (shadow budget) is left for the 10× world: frames are at 5.6 ms today.

**Item 9 is done.**
- **Study 126** (`--rejoin-at-speed`) is **rejected** at the screen: on Bayview, 22/24 finished, with 2 assisted resets and 10× the contacts. See `review/unity-35/frame-cost-126-review.md`.
- **Class-sort baseline:** Cedar sorts cleanly, but Hillcrest leaves 2 street cars stuck behind stock cars. See `review/class-sort/base-review.md`.

The next AI step is the Hillcrest passing investigation. Windowed tests launch on monitor 1 (`-monitor 1`). The remaining Bayview bush at (−93, −1) is cosmetic.

Where the graphics and world work stopped, and what I intended to do next. Nothing is committed yet. All of today's work is in the working tree, and the dev build in `../little-road-dev-build/` is current.

## State at the pause

- **Last benchmark (db10).**
  - Uncapped, the drive averages 5.2 ms, with p99 at 9.4 ms.
  - The five frames over 33 ms are all the benchmark's own 20-second screenshots.
  - The 400–700 ms causeway stalls from db9 are gone. They were shader "fade" variants compiling on the first LOD cross-fade; the loading warm-up now forces cross-fades in every view to compile them.
- **Smoke test (smoke13).** Passes with every change so far: quest, 24-car race and payout, reload, retry and podium renders.
- **Research plan.** All seven items in `docs/visual-quality-audit.md` are in. Since then I've added:
  - the framed tunnel exit;
  - camera shake on impacts;
  - flags and banners in the wind;
  - warmer, darker hill rock;
  - the kit asphalt on the Bayview circuit;
  - a rubbered-in racing line.

## Next, in the order I intended

1. **Warm up fade variants for races too.** `RaceTrial` has no warm-up, so the first cross-fades in a race (props, trees, cars) can hitch the same way the causeway did. Run the same forced-fade pass behind the race loading card, then check with a race frame probe.
2. **Stop the benchmark stills from counting as hitches.** Skip the frame after each capture in `FrameProbe`, so reports show only real stalls.
3. **Racing-line rubber on the Hillcrest Raceway.** Only the Bayview circuit has it. The raceway is built from `HillcrestRoute.Circuit`, a different road builder, so it needs its own strip builder.
4. **Grounding.** Fix the few real placement faults from `PlacementAudit`:
   - the Bayview bush at x −93, z −1;
   - the surf rocks below the headland cliffs.

   Then add a soft dark footing where large kit pieces meet terrain; SSAO alone is thin at a distance.
5. **Framed arrival at the causeway's mainland end.** This is the other arrival moment the audit named. The Headland Gate already frames it from the sea side; from the valley side it needs trees or a landform.
6. **Scorecard update.** Add a third-pass section to `docs/graphics-scorecard.md` covering today's work (body motion, rim and bounce light, fades, terraced hill, flags, road surface, performance), with before/after captures.
7. **Rendering scale-up.** Next in `docs/rendering-and-streaming.md`:
   - a shadow-caster budget (small casters stop casting shadows beyond about 60 m);
   - then GPU occlusion culling (DOTS instancing in the custom shaders);
   - then sector streaming for the 10× world.
8. **Free-roam minimap (Richard's request).** A detailed minimap in adventure mode while free roaming.
   - **Map image.** Build it from the same top-down world render as the atlas (`WorldMap`, `--world-map`), baked at load or shipped as a texture. It will need re-baking when the world changes.
   - **Layout.** A round map in a corner that rotates with the car (or north-up, as an option), with a player arrow.
   - **What it shows.**
     - roads and place names;
     - Pit Stops, residents and the quest objective, each with its own icon;
     - the zipline stations;
     - collectibles only once found (or never, so they stay a discovery).
   - **Behaviour.** Zoom widens with speed. A button opens a full-screen map that uses the atlas labels.
   - **Scaling.** Must hold up in the 10× world: the map texture streams by sector along with the world (see step 7).
9. **Still open from earlier.** AI racing study 126 (`--rejoin-at-speed`) and the class-sort run. Both should start with a fresh baseline, because the trial field changed when the placeholder bodies were removed.

## How to pick up

- Build: `.\tools\build-dev.ps1`
- Check views: `.\tools\capture-map.ps1 -Name <run> -Rendered -Flags '--valley-views'`
- Measure: `.\tools\drive-benchmark.ps1 -Name <run>` (uncapped, no traffic)
- Regression check: `.\tools\capture-adventure.ps1 -Name <run>`
- Place names: the pinned Little Road Atlas (`--world-map` capture, published at https://claude.ai/artifact/KqqihWNLFv8AUXV1Rg4UWp)
