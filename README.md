# Little Road

An original car adventure inspired by Road Trip Adventure / Choro Q HG 2: explore a toy-like world, meet the people who live there, race, and build up a car (and later a team) you care about. Windows, offline, single player.

- **Engine:** Unity 6000.6.1f1, URP, C#, in `unity/`.
- **Art tools:** Blender 5.2, with scripts in `source-art/`.
- **Older prototype:** the Godot version is preserved in `game/`.

## Where things are

| Path | What |
|---|---|
| `docs/45-roadmap-and-feature-spec.md` | **Plan of record**: order of work (Phases 0–5), features, world and track growth |
| `docs/44-parts-tiers-and-progression.md` | Classes (C Street, B Tuner, A Touring, S Racing, E Endurance), parts ladder, economy |
| `docs/next-steps.md` | Latest status and what's next |
| `docs/ui-design.md`, `docs/art-direction.md` | How the UI and the world should look and behave |
| `docs/46-racing-ai-v2-design.md` | The racing AI: research, design, the six abilities, and every study result |
| `docs/archive/racing-ai-v1/` | The first racing AI's design notes (superseded; kept as reading) |
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
| `run-pace-parallel.ps1`, `run-pace-study.ps1`, `run-class-sort.ps1` | Racing comparisons (24-car pace, flag off vs on, six races at once; overtaking by class). `--first-ai` races the first AI as the benchmark; `summarize-racer.py` tabulates results |
| `run-frame-cost.ps1 -Rendered -Extra @('--frame-probe','probe')` | Rendered race frame timing (CPU, GPU, AI, physics per frame) |
| `drive-benchmark.ps1`, `route-drive.ps1` | Free-roam frame cost and an autopilot drive to the castle |
| `cleanup.ps1` | Prunes old evidence to the Recycle Bin (dry run without arguments) |

Working rules for Claude are in `CLAUDE.md`.
