# 55 · The engine scorecard (2 October 2026)

Richard: "Up to now we've essentially just been making it and then seeing how it is and fumbling through a little... What method can we use to accurately create engine noises, and have something to compare them against as some kind of scorecard... so we know it's accurate through gears and when letting off?" Then: "Please do as you think best."

On branch `claude-additions`, not committed. Listening page: https://claude.ai/artifact/8zpJcfKs294HWgpAee59PW. It is private and holds short excerpts of the Sonniss recordings; their licence forbids redistributing the raw files, so keep the link private.

## The method

Every recorded engine is set against the real recording it was built from, on that recording's own drive.

1. **Read the driver.** The tool tracks the recording's revs (the same harmonic-sum tracker and Viterbi path as `tools/engine-grains.py`) and reads the driver's inputs from them, on a 10 ms grid:
   - revs rising: on the throttle;
   - revs falling slowly (−120 rpm/s or more): lifted, with engine braking;
   - revs collapsing (faster than −2200 rpm/s, for less than 0.8 s): a gearchange.

   It writes `trace.csv` (time, rpm, throttle, shifting, upshift count).
2. **Drive the game along it.** The sound harness's new `follow:` mode drives `EngineSynth` and `GrainSynth` along the trace in 10 ms steps, in two forms:
   - **raw**: the grains alone and untouched, which judges the playback method;
   - **in game**: the full voice the player hears.

   Options for experiments:
   - `legacy`: the voice before this work, with an old bank file;
   - `solo=N` and `mute=N`: isolate or drop one layer;
   - `exhaust=X`: try a different exhaust shaping.
3. **Measure both the same way, per driving phase.** The phases are pulling, lifted, gearchanges and the moment of lifting off. Each measure scores 10 for a perfect match and 0 at its tolerance:

   | Measure | Tolerance | Catches |
   |---|---|---|
   | Pitch follows the revs | 50 cents (median) | wrong note, shifts out of step |
   | Tone colour (third-octave balance, weighted), pulling and lifted | 6 dB rms | too thin, boomy or bright |
   | Engine character (the first 2×cylinders engine orders, relative), pulling and lifted | 8 dB rms | V8 burble against four-cylinder buzz |
   | Clean note (energy between the harmonics) | 8 dB | static |
   | Cycle-to-cycle life (level wobble) | 2 dB | too clean or too rough |
   | Louder as the revs climb | 6 dB per doubling | the sound of a gear |
   | Lift-off drop (pulling against lifted, same revs) | 6 dB | throttle you can't hear, or an engine that dies |
   | Lifting off (level curve over 1.2 s) | 6 dB rms | the moment of letting off |
   | Gearchanges (level curve over 0.6 s) | 6 dB rms | the shift dip |
   | Swells and falls in the right places (envelope correlation) | 0.6 | level not following the drive |

4. **Self-test.** `--selftest` scores a recording against a copy of itself, 6 dB down and 3 ms late. Every measure comes out at 9–10 (the Skyline, Viper, sprint car and Niro overall: 9.7–9.9), so the scores reflect differences in sound, not in level or timing.
5. **Report.** Each engine gets `review/engine-compare/<bank>/index.html`, containing:
   - the scores for raw and in game,
   - spectrograms with the tracked revs and the inputs read from them,
   - a listening panel with a blind "which one is real?" test.

**Commands:**
- one engine: `python tools/engine-compare.py <bank> [--engine id] [--selftest]`
- all engines: `sh tools/engine-compare-all.sh`, which builds the harness once and scores six at a time, about 40 s in all.

## Baseline and round one

The baseline is in `review/engine-compare-baseline/`. Every engine's in-game voice:

| Recording | Overall | Lifting off | Through the gears |
|---|---|---|---|
| Isuzu Piazza | 6.9 → 8.3 | 4.5 → 8.0 | 7.3 → 7.4 |
| Sprint car V8 | 5.9 → 7.4 | 4.5 → 5.6 | 6.5 → 8.1 |
| Kia Niro hybrid | 7.0 → 7.3 | 6.8 → 8.4 | 6.6 → 6.7 |
| Citroën C2 | 5.9 → 7.1 | 3.8 → 6.8 | 6.1 → 7.0 |
| Nissan R33 Skyline | 6.4 → 6.7 | 5.0 → 6.0 | 5.1 → 5.5 |
| VW Golf | 5.5 → 6.5 | 3.3 → 6.0 | 5.2 → 5.9 |
| Ford Escort RS1800 | 6.7 → 6.1 | 4.1 → 4.0 | 7.1 → 6.0 |
| Dodge Viper | 5.0 → 5.9 | 3.2 → 5.4 | 4.3 → 5.4 |
| Honda S2000 | 4.1 → 5.6 | 3.4 → 5.8 | 3.7 → 5.1 |
| Audi RS4 | 4.6 → 5.5 | 2.4 → 4.8 | 3.6 → 3.7 |
| Škoda Fabia R5 | 5.6 → 4.7 | 5.4 → 3.6 | 6.8 → 6.4 |
| **Average** | **5.8 → 6.5** | | |

What the scorecard found, and what changed:

- **Lift-offs dropped like the engine switching off.**
  - Real engines lose 1–4 dB in the first 0.7 s after lifting. Ours lost 6–8 dB at once.
  - Three causes stacked up: the bank builder levelled coasting cycles at 65% of pulling; playback switched straight to them; and playback added a 0.75× step and a muffling low-pass on top.
  - **Fix:** the bank builder now measures each recording's own lift-off drop and rise with revs, and stores them in the bank (format version 4, header fields `liftDb`, `riseDb`). Both kinds of cycle are levelled alike.
  - Playback applies the drop as a gain, whichever cycles are playing (coasting sweeps rarely cover every rev range). It fades down with a 0.7 s time constant and returns at once on the throttle. The overrun keeps its own tone.
  - **Floors:** the game is heard from behind the car, by the exhaust. Exhaust microphones lift 4–10 dB (the Skyline 9.8, the sprint car 5.0) and gain 4–8 dB per doubling of revs. Engine-bay microphones mostly hear the valve train, which barely changes with load. So the measured drop and rise have floors of 3 dB each.
- **The rumble was filtered out.** The output's 25 Hz DC blocker took 4–12 dB off the lowest engine orders at low revs (cycle rate 8–17 Hz at 1000–2000 rpm). It is now 12 Hz (`EngineSynth.DcCut`).
- **Made-up layers over real recordings.**
  - The modelled flow roar buried the recorded engine orders: the sprint car's character scored 0.8 with it and 6.9 without it.
  - The modelled gearchange thump put a 62 Hz burst into every shift, which no recording has. Removing it raised the shift score on 9 of 11 engines.
  - Both are now off whenever a recording plays; synthesised-only engines keep them.
- **A/B:** `--legacy-lift` brings back the old lift-off, roar and thump, and the harness's `legacy` option the 25 Hz filter.
- **New checks** (`SoundChecks`):
  - every bank carries a measured drop and rise of 3–12 dB;
  - the Skyline's lift-off fades: −1.1 dB after a quarter second, −9.3 dB after two seconds (its measured drop is 9.8 dB).

## Next round

- **Škoda Fabia** (High-comp and Works 2.0): the recording is all flat-shifts and anti-lag. The game clicks where the revs jump between unrelated cycles, and lift-offs fall into gaps with no coasting coverage. In the spectrogram the clicks show as broadband vertical streaks.
- **Skyline:** the real car stays loud for more than a second after lifting. Fade times could be measured per engine, like the drop.
- **Escort:** the real car lifts 6 dB quieter and rises 12 dB per doubling of revs; the game reaches 2 dB and 7 dB. The bank builder's gain clamps (0.25–6) and its 0.02 floor stop it.
- **Gearchanges:** real dips range from 1 to 11 dB. Some recordings have too few clean shifts to judge, and the Niro's "shifts" are its engine stopping.
- **References:**
  - Half the recordings are engine-bay microphones. External or exhaust recordings of pulls and lifts would score what the chase camera hears; getting them means downloading more, which needs Richard's go-ahead.
  - Many game engines rev past their recording (the Skyline pulls to about 3,800 rpm). Those revs can't be scored without a reference that reaches them.

## In game (2 October, evening)

Richard, after listening: "Some sound slightly chipmunky like the tuner engine and the stock engine sounds like it has multiple sounds garbled together in places, with lift off sounding weird on a few of them... Is it worth having some form of in game score?"

### Why the scorecard sounded right and the game didn't

The recording-matched scores only ever play an engine within its recording's revs. In the game, 13 of the 18 engines spend most of a full-throttle pull past their recording's top revs:

| Engine | Recording | Share of the pull above it | Furthest stretch |
|---|---|---|---|
| Stock 1.2 | Citroën C2 | 95% | 1.76× |
| Tuner V6 | Skyline | 97% | 2.07× |
| Race and Hybrid V6 | Skyline | 100% | 2.9× |

Above the recording the voice is made up from squeezed or looped cycles. That is where the chipmunk and the garble live.

### The capture

`python tools/engine-ingame-capture.py [engines]` captures every game engine, two at a time:

- **The drive:** each engine is fitted with its class's first parts (class C: the stock car with that engine) and driven round the test loop with `--trial-auto --sound-drive`. The AI steers and `SoundDrive` works the pedal: full throttle; a 2 s lift at 90% of the redline in 3rd; back on to top gear and top speed; lift 3 s, tip-in 2.5 s, lift 4 s; brake to a stop; idle 3 s; quit.
- **Hard braking:** if the AI brakes hard for a turn, it is allowed to.
- **What is recorded:** the game writes the engine voice alone (`player-engine.wav`, new) and its revs, gear, throttle, shifting and limiter about every 10 ms (`player.csv`). Speakers are silent (`--audio-capture-silent`), the window is small and minimised (`--start-windowed`; otherwise the saved display setting opened every instance at 4K, which crashed two runs), and runs last up to `--audio-capture-seconds`.
- **Batch mode doesn't work:** it runs no audio, so the captures need real windows.
- **The harness matches the game:** the offline harness, driven along a capture's own trace (rpm, throttle, shifting, limiter), reproduces the game's sound band for band (Club motor brightness 511/401/242/254 Hz in game, 511/405/242/255 Hz from the harness). So in-game drives can be re-rendered offline for experiments.

### The score

`python tools/engine-ingame.py [engines]` writes `review/engine-ingame/<engine>/ingame.json`. Each measure sets the game's property against the engine's real recording:

| Measure | What it compares | Tolerance |
|---|---|---|
| Pitch follows the revs | Tracked from the capture. Whole-number fractions down to the cycle rate count, and the fraction is reported. | 50 cents |
| Tone colour at the same revs | Third-octave balance, rev band by rev band (500 rpm), within the recording | 6 dB rms |
| No chipmunk | Tone above the recording against the real engine's highest well-recorded revs, beyond the drift the real engine's own tone shows per octave of revs | 6 dB |
| One clean engine (garble) | How closely each engine cycle repeats the one before, over the full band and the mid band. Best match within ±5% of the cycle length. Counted only when the game falls short of the real engine's full-throttle pulls. | 0.3 |
| No clicks | Sudden bursts above 2 kHz | 3 per second over the real |
| Lifting off | Level curve at the game's own lifts | 6 dB |
| Gearchanges | Level curve at the game's own upshifts | 6 dB |
| Louder as the revs climb | Pulling | 6 dB per doubling |

Measures that were tried and dropped:

- **Brightness slope:** the real Escort itself darkens from 3,000 to 5,000 rpm, because exhaust resonances aren't straight lines.
- **Noise between the harmonics:** the onboard recordings are noisy enough that the game always looked cleaner.
- **Periodicity of the top band only:** too noisy in every signal to separate anything.

### First results

The in-game average is 5.5. The engines that stay within their recordings score best: Club motor 1.4 8.0, Sport 1.6 7.2, Touring 2.0 7.1. The findings, which agree with Richard's ears:

- **Garble:** the Stock 1.2's cycles repeat 0.63 against the real C2's 0.97. The V6s (mid band 0.71 against 0.86), the Fabias and the RS4 also fall short.
- **Chipmunk and a flat top half:** Tuner V6 chipmunk 4.6, Valley V6 2.3, Race V8 2.8, Twelve-hour V8 2.4. Above the recordings the level stops rising with revs: real engines gain 7–9 dB per doubling, the game −0.5 to +2.
- **Gearchanges dip 19–24 dB in game** against 3–11 dB on the real engines. The automatic box touches the limiter (99.5% of the redline) before almost every upshift, and the limiter's cut leaves 8% of the level (`GrainSynth.CutLevel`).
- **Lift-off:** the Workshop tune drops 10 dB in half a second (the real Golf about 1 dB).
- **Screamer V10:** its strongest line is the cycle rate, a once-per-cycle buzz, rather than its firing note.

The listening page has an "In game" table and a 16 s in-game clip and spectrogram for each engine.

### Next

- Recordings that reach each engine's revs: the real fix for chipmunk and the flat top half. A download list for Richard's approval.
- Joins above the recording that keep the cycles repeating (garble).
- A limiter cut that stutters instead of dropping out, and in-game shift dips like the real ones (a sound-side change; the gearbox's shift point affects lap times and would need keep/reject).
- The Workshop tune's lift-off.

## Round two: towards 8 in game (2 October, afternoon, Richard out)

Richard: "Please continue with these fixes until we have at least a score of 8 on all of the in game engines... If you need more downloads then you don't need to ask... just be wary of licensing."

### Tools

- **Offline in-game scoring.** `sh tools/engine-ingame-all.sh --offline[=harness options]` re-renders every engine's captured in-game drive through the sound harness (the capture's own revs, throttle with braking, shifting, gear and limiter) and scores it with the in-game measures. It takes about a minute for all 18, and was within ±0.3 of the real captures (average 5.46 offline, 5.47 in game).
- **Harness options**, so experiments need no code change: `cut=`, `shiftlevel=`, `loopabove=`, `formantcap=`, `pulse=0`, `loop=0`, `tonecorr=0`, `muffle=1`, `cal=`, `solo=`, `mute=`. They are applied before the voice calibrates its level; before that fix, `cal=` silently did nothing.
- **Other scorer tools:**
  - `SOUND_OVERRIDE=engine=bank,...` tries other recordings for engines without changing the game data.
  - `python tools/engine-ingame-report.py [--offline] [measures]` prints the table and notes.
- **Sonniss 2024:** `tools/fetch-sonniss.py` takes `SONNISS_YEAR=2024 SONNISS_PARTS=9`. The 2024 licence was read (`review/tmp/sonniss-2024-licence/`) and matches the earlier bundles: royalty-free use and modification in games, no attribution, no selling the raw files, no AI training.

### Measures made fair (each applied to the game and the recording alike, and each a rule rather than a tuning)

- **Whole recordings.** The in-game references now read each recording whole, as the bank builder does (cached in `review/engine-ingame/ref-<bank>.pkl`). The first 90 s left the S2000's reference stopping at 3,000 rpm while its cycles went to 7,350.
- **Tone colour** compares wherever the real recording has the revs, on full-throttle (within 12 dB of the loudest) steady pulling, from 0.3 s after a gearchange. The game's test drive is flat out. Gentle part-throttle rises, and the recovery after each shift, have a different tone.
- **Chipmunk** compares the spectral *envelope* from 3× the firing frequency (at least 400 Hz) to 6 kHz, shape only, beyond the real recording's highest revs, less the real engine's own drift per octave of revs. Third-octave balance across rev bands was dominated by single harmonics moving from band to band, which real engines do too.
- **Rise with revs** uses steady pulling only (from 0.3 s after a gearchange).
- **Rev-limiter bounce** is not pulling.
- **Gearchanges:** a reference "shift" deeper than 15 dB is not a gearchange (the Niro hybrid's engine stopping), so there is no measure then.

### Sound changes

- **Bank version 6** (`tools/engine-grains.py`). The header now carries each recording's measured lift-off drop, its rise with revs while pulling (from the recording's loud pulls; the Skyline was 3.7 dB from cycle levels, 6.2 from pulls) and while coasting, and its median gearchange curve. The curve is 0.6 s in 50 ms steps, beyond what the revs explain, read with the scorecard's own driving states.
- **Gearchanges:** playback holds the level through a shift and plays the recording's curve. The limiter cut leaves 50% (was 8%). The automatic box touches the limiter before most upshifts, and at 8% every shift dropped out.
- **Above the recording** the level keeps rising at the recording's rate (up to +12 dB). The sustain loop stays for anything above the recording; `LoopAbove` is kept for experiments. Only looping beyond 2× brought the Skyline's 1.4× static back (the sound check caught it).
- **Exhaust shaping is gentler:**
  - road: a −3 dB shelf above 3.5 kHz (was a 2.6 kHz low-pass);
  - sport: +1.5 dB at 170 Hz;
  - race: +2 dB at 2.8 kHz with a 1.2× drive.

  The turbo's 3.6 kHz muffle no longer applies over recordings.
- **New recordings and banks:**
  - Sonniss 2023: the S2000 external (`s2000-ext`, to 8,804 rpm).
  - Sonniss 2024: Ferrari 458 (to 9,010), McLaren 720S (8,025), Porsche Carrera GT (8,740), Ford Puma Rally1 (8,071), Citroën DS3 WRC (7,716) and Xsara WRC (7,413).
  - Chosen by score: Race V8 → Ferrari 458, Race V6 → DS3 WRC, High-comp → Xsara WRC, Works 2.0 and Hybrid V6 → Puma Rally1, Touring Evo → S2000 external, Hybrid V8 → sprint car.
  - Tried and not kept: Carrera GT for the Screamer V10 (garble), McLaren for the Twelve-hour V8, the C2 pass-by (its Doppler shift mis-tagged the revs).

### Round two results

Each game engine's recording was chosen by its in-game score among the recordings that reach its revs. Within a class every engine keeps its own recording, and V8s keep V8 recordings where one scores. The Twelve-hour V8, the Duke's car, is the 458's V8, though a rally four scored 0.3 higher.

| Class | Engine | Recording |
|---|---|---|
| C | Stock 1.2 | Citroën C2 |
| C | Workshop tune | VW Golf |
| C | Club motor | Ford Escort |
| C | Sport 1.6 | Isuzu Piazza |
| C | Valley V6 | **Citroën C5** |
| B | Twin-cam | Isuzu Piazza |
| B | High-comp | **Citroën DS3 WRC** (from the Xsara, 2 Oct evening: 8.0 → 8.9; doc 56) |
| B | Tuner V6 | **McLaren 720S** |
| A | Touring | Ford Escort |
| A | Touring Evo | **Citroën DS3 WRC** |
| A | Works | **Citroën Xsara WRC** |
| S | Race V6 | **Citroën DS3 WRC** |
| S | Race V8 | **Ferrari 458** |
| S | Screamer V10 | Dodge Viper |
| E | Hybrid V6 | **Ford Puma Rally1** |
| E | Hybrid V8 | **McLaren 720S** |
| E | Twelve-hour V8 | **Ferrari 458** |
| E | Economy hybrid | **Citroën C5** |

**Offline in-game scores** (each engine's captured drive re-rendered):

- **Average 8.29 (was 5.47), 16 of 18 at 8 or above.**
- The last two: Stock 1.2 at 7.8 and Hybrid V8 at 7.9.
- One drive per engine carries about ±0.5 of noise. The Race V8 read 8.4 on one capture and 7.6 on the next, from different gearchange timing and AI braking.

**What holds the last two back:**

- **Stock 1.2:** its Citroën C2 recording stops at 3,500 rpm, so 95% of a pull is synthesised above it (cycles stop repeating like the real engine's, 0.77 against 0.94). No reliable small-car recording to 6,000 rpm was found (the C2 pass-by's Doppler shift broke its rev tracking). The C5 scores 8.5 but would give two class C engines one voice.
- **Hybrid V8:** it is 0.2 short on rise with revs at the top. With 60% calibration headroom it reaches 8.1 (and 17 of 18 engines reach 8), but that costs 4.4 dB of loudness, and the engines were too quiet before (Richard, 1 Oct). Not taken.

**Update, 2 October, evening** ([doc 56](56-wider-game-round.md)):
- **High-comp moved to the DS3 WRC.** It now plays only 5% above the recording, against 21% on the Xsara. In-game capture 8.9 (was 7.9): 15 of 18 engines are at 8 or above in game.
- **Still short:**
  - Stock 1.2, 7.7: R4, Morris and Fiat 500 recordings tried, none better.
  - Works 2.0, 7.9: S2000, S2000 external, DS3, Fabia, Piazza, Escort and Puma all score lower or share a voice within class A.
  - Hybrid V8, 7.9: the Viper scores 8.3, but its chipmunk score is 2.3 at 60% above its recording, the very fault Richard heard, so it wasn't taken. RS4, Carrera GT and Testarossa score lower.

**Kept switchable for experiments:** `EngineSynth.CalibrateToPeak` (no gain), `GrainSynth.LoopAbove`, `CutLevel`, `ShiftLevel`, `LoopMax/LoopSpan/LoopFloor` (now 80 / 110 / 0.8, from 40 / 48 / 0.85).
