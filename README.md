# Little Road

An original car adventure inspired by Road Trip Adventure / Choro Q HG 2: explore a toy-like world, meet the people who live there, race, and build up a car (and later a team) you care about. Windows, offline, single player.

- **Engine:** Unity 6000.6.1f1, URP, C#, in `unity/`.
- **Art tools:** Blender 5.2, with scripts in `source-art/`.

## Where things are

| Path | What |
|---|---|
| `docs/45-roadmap-and-feature-spec.md` | **Plan of record**: order of work (Phases 0–5), features, world and track growth |
| `docs/44-parts-tiers-and-progression.md` | Classes (C Street, B Tuner, A Touring, S Racing, E Endurance), parts ladder, economy |
| `docs/next-steps.md` | Latest status and what's next |
| `docs/ui-design.md`, `docs/art-direction.md` | How the UI and the world should look and behave |
| `docs/46-racing-ai-v2-design.md` | The racing AI: research, design, the six abilities, and every study result |
| `unity/Assets/Scripts` | Game code (`AdventureGame`, `RaceTrial`, `RoadCar`, UI, world builders) |
| `unity/Assets/Editor` | Build script (`MigrationBuild`) and the editor checks that run on every build |
| `source-art/` | Blender build scripts for the town kit, cars and part models |
| `tools/` | Build, test and review scripts (below) |
| `review/` | Generated evidence (screenshots, logs, reports); mostly git-ignored. Scratch files go in `review/tmp/` |

## Build and run

From the project root in PowerShell, with the Unity editor closed:

```powershell
.\tools\compile-check.ps1   # all C# in ~2 s with Unity's compiler
.\tools\build-dev.ps1       # compile check, player build to ../little-road-dev-build, then cleanup
```

Run `Launch Little Road Unity.cmd`, or `../little-road-dev-build/Little Road Unity.exe`. Windowed tests pass `-monitor 1`.

## Tests and reviews

| Tool | Checks |
|---|---|
| `capture-adventure.ps1 -Name smokeN` | Smoke test: quest, delivery, finds, buy and fit, a 24-car race and payout, reload |
| `run-parts-review.ps1 -Name x` | Parts shop and fitting bay with a virtual gamepad |
| `capture-map.ps1 -Name x -Rendered -Flags '--pause-review'` | Other flags: `--minimap-review`, `--climb-review`, `--contact-review`, `--car-audit`, `--edge-audit`, `--world-review` |
| `run-pace-parallel.ps1`, `run-class-sort.ps1` | Racing comparisons (24-car pace, flag off vs on, six races at once; overtaking by class). `summarize-racer.py` tabulates results |
| `sound-harness.ps1 [-Filter id\|recorded\|layers:<engine>]` | Offline listening and checks for the game's sound code (no Unity, seconds): every engine's rev test (idle, pull, hold at max revs, lift) to `review/sound/`, `recorded` for the real-engine grain banks, `layers:<engine>[+turbo][@fraction]` to measure each layer at any revs, `tone:<engine id>` for level, brightness, pitch, flutter and wobble from 30% to 100% of the redline (doc 47) |
| `test-loop.ps1 [-Kit club] [-Auto] [-Minutes 3] [-Capture name]` | Drive (or watch) the 2.5-mile test loop: two 1-mile straights for gearing, top speed, limiter and engine sound up to class E; `-Capture` records the engine sound and revs |
| `blender --background --python source-art/build_prototype.py -- [--render DIR]` | The Regale, Duke Rolo's prototype (doc 53): sculpted body, parts, the painted livery (`regale_livery.py`) and LODs into `Resources/Cars`. In game: `capture-map.ps1 -Name duke-N -Rendered -Flags @('--duke-review')` |
| `duke-race-review.ps1 -Name dN` | The Duke's Challenge (doc 54): 3 laps of Hillcrest Raceway against Duke Rolo on the maximum build, your car on autopilot (`--duke-race-review`; add `--max-build-test` for equal cars). Trials: `--trial-setup max`, `--trial-body regale`; physics A/B: `--no-aero-grip`, `--no-downforce`, `--aero-reserve <r>` |
| `python tools/engine-compare.py <bank> [--selftest]`, `sh tools/engine-compare-all.sh` | Engine scorecard (doc 55): the game's engine driven along its recording's own drive (revs, throttle and gearchanges read from it), scored 0-10 per measure against the real one, with spectrograms and a blind test in `review/engine-compare/<bank>/index.html`. Harness: `-Filter "follow:<engine>|<bank>|<trace.csv>|<out.wav>[|raw|legacy|solo=N|mute=N]"`; game A/B: `--legacy-lift` |
| `python tools/engine-ingame-capture.py [engine ...]`, `sh tools/engine-ingame-all.sh [--offline[=options]]`, `python tools/engine-ingame-report.py [--offline] [measures]` | In-game engine score (doc 55): every game engine captured in the game itself (test loop, `--trial-auto --sound-drive --audio-capture <wav> --audio-capture-silent`, minimised windows) and scored against its real recording: pitch, tone, chipmunk above the recording, garble (cycle repeat), clicks, lift-off, gearchanges, rise with revs. `--offline` re-renders each captured drive with the current sound code (about a minute for all 18); `SOUND_OVERRIDE=engine=bank,...` tries other recordings. Sonniss 2024: `SONNISS_YEAR=2024 SONNISS_PARTS=9 python tools/fetch-sonniss.py list|fetch` |
| `fetch-sonniss.py`, `engine-grains.py --config tools/engine-banks.json` | Real engines: pull single recordings from Sonniss' official GDC zips into `../little-road-audio-sources/` (outside the repo), then cut them into the grain banks in `Resources/Engines` (doc 47) |
| `python tools/music/compose.py [song]` | Music: composes each theme as MIDI and renders it through FluidSynth + GeneralUser GS (in `../little-road-tools/`) into seamless loops in `Resources/Music`, with previews in `review/music/` (doc 49) |
| `capture-map.ps1 -Name x -Flags @('--rejoin-test')` | Road-edge checks (doc 50): `--rejoin-test` (drive back onto the road at Bayview Halt), `--edge-audit` (lips and steep banks on every road), `--probe-line "x0,z0,x1,z1"`, `--spot-views "x,z,yaw[,height]"`. The title screen: `--title` forces it, `--title-review <folder>` captures it |
| `capture-map.ps1 -Name x -Flags @('--trap-test')` | Claude's additions (doc 51): `--trap-test`, `--train-race-test [bayview]` (also checks the countdown hold and captures the finish beacon), `--horn-test`, `--photo-review`, `--photo-requests-test`, `--fireworks-review --fireworks-now --time night`; `sound-harness.ps1 -Filter horns\|fireworks` |
| `run-frame-cost.ps1 -Rendered -Extra @('--frame-probe','probe')` | Rendered race frame timing (CPU, GPU, AI, physics per frame) |
| `drive-benchmark.ps1`, `route-drive.ps1` | Free-roam frame cost and an autopilot drive to the castle |
| `publish-play.ps1` | Richard's play copy: copies the dev build to `../little-road-play/` (changes only when this runs) and points the "Little Road" desktop shortcut at it, the only one (4 Oct: one build with everything; old extra shortcuts go to the Recycle Bin). |
| `cleanup.ps1` | Prunes old evidence to the Recycle Bin (dry run without arguments) |

Working rules for Claude are in `CLAUDE.md`.
