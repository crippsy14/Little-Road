# 49 — Music

Status, 30 September 2026: first version, on branch `music`.

## What plays when

- **Free roam.**
  - Bayview's theme plays on the island, the causeway and the halt; Hillcrest's theme plays inland.
  - In the tunnel between them, whatever is playing carries on.
  - Changing theme crossfades over 4 s.
- **Races.**
  - Each track plays its race theme; tracks without their own use Bayview's.
  - Race music sits at 0.45, lower than free roam (0.6), so it stays under the engines.
  - On the player's last lap, a **lift stem** fades in over about 3 s. It adds a melody and brighter percussion and plays sample-locked to the race loop, so the extra adrenaline arrives without a new song starting.
- Music follows the scene fade and the Mute setting, and has its own **Music** volume under Settings > Sound. It is silent in batch runs and on the engine sound test.

## The themes

All were composed in `tools/music/compose.py` (theory helpers, then one function per place) and played on General MIDI instruments from GeneralUser GS.

| Theme | Key and feel | Instruments |
|---|---|---|
| Bayview explore | G major, 96 bpm, sunny harbour | nylon-guitar strum, marimba melody, picked bass; glockenspiel in the B section; shaker, soft kick, side-stick |
| Bayview race | G major, 124 bpm | pad, picked bass, guitar strum, drums. Lift: marimba and a square lead doubling the tune, open hats and tambourine |
| Hillcrest explore | D major, 6/8 (a lilting country waltz feel) | steel-guitar arpeggios, accordion melody, bass; strings in the B section; brush kit |
| Hillcrest race | D major, 4/4, 132 bpm | strings, picked bass, steel strum, drums with tom fills. Lift: accordion and flute on the tune, ride cymbal |

## Pipeline

1. Python writes a MIDI file for each song, with no dependencies beyond the standard library.
2. FluidSynth 2.6.1 renders it: `-ni -q -g 0.5 -r 48000`.
   - The FluidSynth binary and the soundfont live outside the repo, in `../little-road-tools/`.
   - GeneralUser GS's licence allows use in games, including bundled renders.
3. **Seamless loops.** Each song is written three times over. The middle pass is cut out, and its first 10 ms is crossfaded with the matching audio from the third pass. Reverb and release tails therefore wrap around correctly, with no click at the seam.
4. Each loop is levelled to RMS 0.1. A race lift stem uses its base loop's gain, so the two mix as composed.
5. The output is stereo, 48 kHz, 16-bit WAV in `unity/Assets/Resources/Music/`, with previews (two passes) in `review/music/`.

Re-render everything with `python tools/music/compose.py`, or a single song with `python tools/music/compose.py hillcrest-race`.

## Code and checks

- `Audio/Music.cs` is created by `AudioDirector.Ensure`.
  - It uses three 2D sources: two for crossfading between themes, plus the lift stem.
  - It reads the theme from `RaceTrial.TrackId` in races and from `AreaBanner.Area` in free roam.
- `--audio-probe` logs `LITTLE_ROAD_MUSIC_PROBE theme=… level=… lift=…` every 3 s.
- `SoundChecks` (build gate) checks two things:
  - each theme loads in stereo and is longer than 20 s;
  - each race loop has exactly the same number of samples as its lift stem, so they stay in step.
- Smoke test, 30 September:
  - A one-lap Hillcrest race played `hillcrest-race` at 0.22 (the Music setting × 0.45), with the lift up to full on the last lap.
  - The adventure start faded `bayview-explore` in to 0.29.
  - No exceptions in either.

## Next

- Hear it in game and adjust the levels, tempos and instruments to Richard's ears.
- Possible additions:
  - a finish sting;
  - a night variant of each theme (fewer instruments, softer);
  - a theme for each new area as the map grows (doc 45).
