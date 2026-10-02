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
