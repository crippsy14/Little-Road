# Little Road — notes for Claude

Unity 6000.6.1f1 / C# / URP racing-adventure prototype. Start with `RACING_AI_GUIDE.md` (code map, baselines, build/run commands). The Godot project in `game/` is a preserved older prototype.

## Current work
Racing-AI pace experiments on branch `ai-pace-experiments` (branched from `main` 2586b2fd). Read `docs/42-ai-pace-experiments-handoff.md` for the plan, status and next step before changing anything.

## Working rules
- Windows PC; use PowerShell from the project root. Close the Unity editor before batch builds.
- Build with `.\tools\build-dev.ps1`, which replaces the single reusable export `../little-road-dev-build/` (~230 MB). Don't create numbered `little-road-context-<N>-review` exports; they filled the disk. Keep numbering experiments by study folder (`review/unity-35/frame-cost-<N>/`) and review doc, and never overwrite an existing study run or review.
- Race runs delete per-tick telemetry (`race.track.json` etc., ~75 MB/race) unless `-KeepTelemetry` is passed; keep it only for audit/diagnosis runs.
- New behaviour goes behind an off-by-default `--flag` so the same executable can A/B it; add an editor check in `unity/Assets/Editor/*Checks.cs` and register it in `MigrationBuild.cs`.
- Keep/reject: same build, flag off vs on, Cedar + Bayview, seeds 1709/1710/1711. Keep only if all 24 finish, no new assisted resets, and total finish time for all 24 cars improves on average. Record every result, including failures.
- Do not commit or push unless Richard asks.
