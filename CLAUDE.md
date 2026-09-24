# Little Road — notes for Claude

Unity 6000.6.1f1 / C# / URP racing-adventure prototype. Start with `RACING_AI_GUIDE.md` (code map, baselines, build/run commands). The Godot project in `game/` is a preserved older prototype.

## Current work
Racing-AI pace experiments on branch `ai-pace-experiments` (branched from `main` 2586b2fd). Read `docs/42-ai-pace-experiments-handoff.md` for the plan, status and next step before changing anything.

## Working rules
- Windows PC; use PowerShell from the project root. Close the Unity editor before batch builds.
- Build to a new numbered sibling export (`../little-road-context-<N>-review/`); never overwrite an existing export or evidence folder.
- New behaviour goes behind an off-by-default `--flag` so the same executable can A/B it; add an editor check in `unity/Assets/Editor/*Checks.cs` and register it in `MigrationBuild.cs`.
- Keep/reject: same build, flag off vs on, Cedar + Bayview, seeds 1709/1710/1711. Keep only if all 24 finish, no new assisted resets, and total finish time for all 24 cars improves on average. Record every result, including failures.
- Do not commit or push unless Richard asks.
