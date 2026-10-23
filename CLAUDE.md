# Little Road — notes for Claude

Unity 6000.6.1f1 / C# / URP racing-adventure prototype. Start with `README.md` (layout, build) and `docs/README.md`.

## The docs: five living records (since 23 Oct)
- `docs/design.md`: every game decision, direction and design rule (classes, career, races, cars, parts, economy, driving, AI, sound, UI, look, characters, the plan of record).
- `docs/world.md`: the Glasswater continent, the campaign and story, the build order, the standards for new places and tracks, and each built place.
- `docs/next-steps.md`: latest status, the next three pieces, what waits on Richard, known issues.
- `docs/how-we-work.md`: roles, the cycle, the bars, keep/reject, regression, agents, tools, git.
- `docs/bug-inbox.md`: snaps and chat reports.
- When a decision changes, edit the record where it lives; don't add a doc that supersedes another. A working brief (`docs/brief-<name>.md`) is folded into the records and deleted when its piece lands. Old numbered docs (`docs/47` etc., still cited in code comments) are mapped in `docs/README.md` and kept in git at 35a4d35.
- Claude plans, builds and judges each piece of work against its bars; Richard approves briefs and playtests, and has given Claude design freedom (20 Oct). The racing driver is Racing AI v2 (`RacerDriverPolicy`) with racecraft v3 (`--no-racecraft-v3` races without it). `--scale-test` builds the generated continent.

## Working rules
- Windows PC; use PowerShell from the project root. Close the Unity editor before batch builds.
- Before any build, `.\tools\compile-check.ps1` compiles all C# in ~2 s with Unity's own compiler; `build-dev.ps1` runs it first and stops on errors. Run builds in the foreground and put only the long race/capture runs in the background, so compile or editor-check failures show immediately.
- Build with `.\tools\build-dev.ps1`, which replaces the single reusable export `../little-road-dev-build/` (~230 MB). Don't create numbered `little-road-context-<N>-review` exports; they filled the disk. Keep numbering experiments by study folder (`review/unity-35/frame-cost-<N>/`) and review doc, and never overwrite an existing study run or review.
- Launch test runs of the game with `Start-Quiet` (`. tools/quiet-start.ps1`) rather than `Start-Process`. It opens the window without activating it, so Richard's own game keeps the keyboard and pad.
- Every test window is muted, without exception (Richard, 14 Oct). `Start-Quiet` adds `--silent` itself, and the game mutes any test run (batch mode, `--trial-auto`, `--world-review`, `--*-test/-review/-audit`); `--audible` opts back in for a deliberate listen only.
- Photos are removed once checked (Richard, 14 Oct): delete each capture run as soon as it has been looked at; the regression removes its own pictures. Keep only images a doc links to.
- Race runs delete per-tick telemetry (`race.track.json` etc., ~75 MB/race) unless `-KeepTelemetry` is passed; keep it only for audit/diagnosis runs.
- New behaviour goes behind an off-by-default `--flag` so the same executable can A/B it; add an editor check in `unity/Assets/Editor/*Checks.cs` and register it in `MigrationBuild.cs`.
- Keep/reject: same build, flag off vs on, Cedar + Bayview, seeds 1709/1710/1711. Keep only if all 24 finish, no new assisted resets, and total finish time for all 24 cars improves on average. Record every result, including failures.
- Commit and push as you see fit to Richard's own repos (crippsy14/*: this game, Car Forge); any other repo only when Richard asks (Richard, 17 Oct).

## Look (colour and lighting)
- Follow the art direction in `docs/design.md` §11 ("toy shapes, real light"). All light, grade, cloud and colour-limit numbers live in `unity/Assets/Scripts/WorldLook.cs`; `WorldLookChecks` fails the build if they drift.
- Create materials with `TrialWorld.Material` (it grades colours) and terrain with `WorldLook.Terrain`. Don't hand-set `_Stylise` or saturated colours; custom shaders need `_LIGHT_COOKIES` support for cloud shadows.

## Files and clean-up
- Generated output lives in one place: `review/` (git-ignored screenshots, logs and telemetry) plus the single dev build `../little-road-dev-build/`. Scratch saves go in `review/tmp/`.
- `tools/cleanup.ps1` prunes old untracked evidence to the Recycle Bin: capture runs beyond the newest 3 per series, logs older than 7 days or over 50 MB, and per-tick telemetry older than 1 day. It never touches tracked files or images linked from docs. `build-dev.ps1` runs it after every good build; run it with no arguments for a dry run.
