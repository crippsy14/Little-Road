# Little Road — notes for Claude

Unity 6000.6.1f1 / C# / URP racing-adventure prototype. Start with `README.md` (layout, build and test commands) and `docs/45-roadmap-and-feature-spec.md` (plan of record).

## Current work
Plan of record: `docs/45-roadmap-and-feature-spec.md`. It sets the order of work in Phases 0–5 (foundations; class C campaign; B; A with teams, fuel, tyres and pits; S; E), and the parts ladder is in `docs/44-parts-tiers-and-progression.md`. The campaign is at least 12 hours and open-ended. Latest status is in `docs/next-steps.md`.
The world and campaign design (the Glasswater continent, places, races, story, build order W0–W4, design principles) is `docs/58-world-and-campaign-design.md`; `--scale-test` builds the generated continent (`ContinentPlan`, `ContinentGenerator`, `ScaleTest`).
Racing AI v2 (`RacerDriverPolicy`, the six-ability design; `docs/46-racing-ai-v2-design.md`) is the default driver since study 205, on branch `racing-ai-v2`. The first AI stays in the code as the benchmark: `--first-ai` races it. Its old design notes are in git history (before commit 5f8d401's clean-up). Use `tools/run-pace-parallel.ps1` for keep/reject races (six at once; results are deterministic) and `tools/summarize-racer.py` to compare.

## Working rules
- Windows PC; use PowerShell from the project root. Close the Unity editor before batch builds.
- Before any build, `.\tools\compile-check.ps1` compiles all C# in ~2 s with Unity's own compiler; `build-dev.ps1` runs it first and stops on errors. Run builds in the foreground and put only the long race/capture runs in the background, so compile or editor-check failures show immediately.
- Build with `.\tools\build-dev.ps1`, which replaces the single reusable export `../little-road-dev-build/` (~230 MB). Don't create numbered `little-road-context-<N>-review` exports; they filled the disk. Keep numbering experiments by study folder (`review/unity-35/frame-cost-<N>/`) and review doc, and never overwrite an existing study run or review.
- Race runs delete per-tick telemetry (`race.track.json` etc., ~75 MB/race) unless `-KeepTelemetry` is passed; keep it only for audit/diagnosis runs.
- New behaviour goes behind an off-by-default `--flag` so the same executable can A/B it; add an editor check in `unity/Assets/Editor/*Checks.cs` and register it in `MigrationBuild.cs`.
- Keep/reject: same build, flag off vs on, Cedar + Bayview, seeds 1709/1710/1711. Keep only if all 24 finish, no new assisted resets, and total finish time for all 24 cars improves on average. Record every result, including failures.
- Do not commit or push unless Richard asks.

## Look (colour and lighting)
- Follow `docs/art-direction.md` ("toy shapes, real light"). All light, grade, cloud and colour-limit numbers live in `unity/Assets/Scripts/WorldLook.cs`; `WorldLookChecks` fails the build if they drift.
- Create materials with `TrialWorld.Material` (it grades colours) and terrain with `WorldLook.Terrain`. Don't hand-set `_Stylise` or saturated colours; custom shaders need `_LIGHT_COOKIES` support for cloud shadows.

## Files and clean-up
- Generated output lives in one place: `review/` (git-ignored screenshots, logs and telemetry) plus the single dev build `../little-road-dev-build/`. Scratch saves go in `review/tmp/`.
- `tools/cleanup.ps1` prunes old untracked evidence to the Recycle Bin: capture runs beyond the newest 3 per series, logs older than 7 days or over 50 MB, and per-tick telemetry older than 1 day. It never touches tracked files or images linked from docs. `build-dev.ps1` runs it after every good build; run it with no arguments for a dry run.
