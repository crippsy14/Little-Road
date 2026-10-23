# How we work: roles, the cycle, the bars, the tools

This record holds the process and tooling rules that are still in force. Game design is in [design.md](design.md), the world and its places
are in [world.md](world.md), and the latest status is in [next-steps.md](next-steps.md). `CLAUDE.md` has the short version of the working rules.

## 1 · Roles

- **Claude plans, builds and judges first** (Richard, 13 Oct: "plan, execute, and judge quality yourself with agents doing
  specific things").
  - Chooses what comes next from the plan in [next-steps.md](next-steps.md) and the build order in [world.md](world.md).
  - Writes each piece's brief, then researches, builds, measures and fixes it against the bars in §3.
- **Richard directs and playtests.**
  - He approves, edits or vetoes briefs, plays finished pieces, and reports through snaps or chat.
  - Creative direction, story and tone are his calls. So is anything spent, downloaded or published outside his own repos.
- **Design freedom** (Richard, 20 Oct): "If it makes sense, and the game would be better for it, then you [have] total
  freedom to do as you like, with any mechanic or design choice." Make the call, record it as "my call" in the record, and
  keep the safety nets (flags, keep/reject, audits, fingerprints).
- **Research, then proceed** (29 Sep): for a batch of feedback, research the design, then do the whole batch without
  stopping for confirmation, and report at the end.
- **Be bold** (28 Sep): rewrite an approach when it blocks a feature, and model the real part rather than a stand-in.
- **Examples are direction** (20 Oct): Richard's examples (tracks, games, towns, cars) show a direction, not a 1:1
  reference. Research what makes them good, then design something original named after its own place.
- **Design for the finished game** (14 Oct): set thresholds, gates and progression against the full plan (the C ladder to
  10★, the whole map), not against today's few races.
- **Claude's own additions** (30 Sep): "go to town adding whatever you like... be brave and bold!" Free time goes to
  features Claude believes make the game more fun, built in full (flag, check, record).

## 2 · The cycle

1. **Brief.** One page: what it is and why now, real references, the player's experience in three sentences, scope in and
   out, risks, and how it will be judged. Richard says yes, edits or vetoes.
   - A brief is a working doc (`docs/brief-<name>.md`). When the piece lands, fold its decisions into design.md or world.md
     and delete the brief. Git keeps it.
2. **Research.** Real references, and how comparable games do it. Agents can do this; keep the sources in the brief.
3. **Build behind a flag.** New behaviour goes behind an off-by-default `--flag`, with an editor check in
   `unity/Assets/Editor/*Checks.cs` registered in `MigrationBuild.cs`.
   - New districts use the district system from the start: deferred building (`Districts`), their own settle pass, and a
     town layer through `WhenBuilt`. They build in the background after you're driving.
   - Towns are built zone by zone (Richard, 22 Oct: "we should defo do it for all towns and cities from now on"). The
     method is in [world.md](world.md) §2.
4. **Self-judge.** Measure every bar in §3 and keep the evidence: captures, audit text, timing lines. Fix failures, or
   write them down with a reason.
5. **Playtest.** Richard plays the play copy. His snaps land in the bug inbox.
6. **Fix round.** Reproduce each report at its exact pose (`--spot-views`), fix it, and check it with a before and after
   capture.
7. **Default.** When a feature is checked and good, it goes on by default in player builds with a `--no-…` opt-out
   (Richard, 20 Oct: "remove the flag and include it by default"; options waiting on his approval count as approved). Update
   its editor check, and `WorldCache`'s key if it changes the world. Commit, push, and update next-steps.md.

The top of [next-steps.md](next-steps.md) always shows the next three pieces, so Richard can see what's coming.

## 3 · The bars

A piece is done when it clears these, measured rather than eyeballed.

- **Area standards.** `--area-audit` PASS for the area: floating pieces, loose bunting, lips, steep unguarded banks, road
  seams, cars off the ground, hidden shop signs and tipped furniture (`StreetAudit`). The thresholds are in
  [world.md](world.md) §2.
- **Pictures, not just numbers.** A place is judged in pictures against Bayview's harbour front and Hillcrest's streets
  (the 8/10 bar, Richard 21 Oct). Audit passes alone don't make a place good.
- **Cheap reviews** (Richard, 20 Oct): no photo-sweep agents. Run the audit first, photograph only the flagged spots plus a
  few samples (`--view-shots`, which also takes `@Road,s,left|right|along`), and read them yourself at 800×450 or as a
  contact sheet. Use agents only for a whole-town look at a milestone, on about a dozen chosen views.
- **Art direction.** Toy shapes and real light ([design.md](design.md) §11). Colours and materials go through `WorldLook`
  and `TrialWorld.Material`, and `WorldLookChecks` must stay green.
- **Performance budgets** (`tools/drive-benchmark.ps1` and the load timing lines):

  | Budget | Limit |
  |---|---|
  | Ready to drive (a build's second launch) | 3.5 s or less |
  | Race entry | 1.0 s or less |
  | Drive benchmark, frames over 33 ms | 2 or fewer |
  | Drive benchmark, p99 frame | 20 ms or less |
  | A background district's busiest frame | 50 ms or less |

  A new district must not move ready-to-drive by more than 0.1 s.
- **Same world.** A change that isn't meant to change the world keeps the world fingerprint and the town print
  (`--world-fingerprint --no-static-batching`, `--town-print`). The old world's fingerprint `3f164ae90677e486` is checked
  with `--old-bayview --no-prop-forge --distant-mountains --no-bramblewick --no-vinelands --world-fingerprint --no-static-batching`.
  A change that is meant to change the world records its new baseline.
- **Driving.** Roads are drivable or guarded (`--edge-audit`). Races finish with no new assisted resets.
- **Releasable.** No editor-only dependencies at runtime, no assets of unknown licence, and the Unity plan's terms respected.

### Keep/reject for racing changes

- Same build, flag off against flag on, on Cedar and Bayview with seeds 1709, 1710 and 1711.
- Keep only if all 24 cars finish in every race, there are no new assisted resets, and the total finish time for all 24
  cars improves on average. Record every result, failures included.
- `tools/run-pace-parallel.ps1` runs six races at once (results are deterministic); `tools/summarize-racer.py` compares
  them. For a new track, the AI also races it at skill 3, 7, 10 and 13 (12 of 12 races 24/24 was Bramblewick's launch bar).
- Short tuning loops can run on one track in about 5 minutes before the full sweep.

### Regression

- `.\tools\regression.ps1 -Name <run>` runs everything in parallel batches of up to five games in about 7 minutes: both
  smoke tests (with and without Quarry Hills), the lesson, climb, sprint, carving, Blondin, wayfinding, world-tiles and
  festival tests, the area audit, then the sound smoke on its own.
- Run it only at landmarks: before publishing a play copy, or after a change to shared systems (Richard, 8 Oct: "Lets do it
  on landmarks"; 20 Oct: "Don't worry about any full regression checks any time soon"). For ordinary changes, the compile
  check, the build's editor checks and the targeted test are enough.
- Point-to-point drive tests (train, sprint, Blast Run) run with `--no-traffic` until the traffic can overtake (Richard, 12 Oct).

## 4 · Bug reports: the snap tool and the inbox

- **Snap:** F9 on the keyboard (a note box follows), or hold the right stick in for 0.6 s on a pad. A snap saves a PNG and
  a JSON record: position, heading, camera pose, area, time of day, build, save, race event and field, running activity, and
  the note. Snaps land in `%USERPROFILE%\AppData\LocalLow\Little Road Project\Little Road Unity Trial\snaps\<date>\`.
- **Triage:** every snap, and anything Richard mentions in chat, goes into [bug-inbox.md](bug-inbox.md): what's wrong, the
  cause, and open, fixed or won't-fix. `tools/snap-views.py` turns snap records into `--view-shots`, so a snap's exact view
  can be recaptured before and after a fix.

## 5 · Agents

- Reviewers and researchers report; they never edit. Fixes are made in the main session (Richard, 4 Oct: "don't use agents
  to do the fixing").
- **Cost** (Richard, 4 Oct: "the usage is going through the roof"): reviewers run on sonnet or haiku, and there are few of
  them, each with a narrow job. The street reviewer is `street-reviewer-high` (sonnet). Use Opus only when a judgement call
  needs it.
- Each brief states the question, the evidence and the output wanted. Agent reviews are input, not a verdict: check each
  finding against the evidence before fixing anything.
- **AI-first tooling** (Richard, 17 Oct): build tools that suit an AI. Build one only when a loop recurs (the same scratch
  script written about three times). A good tool is self-describing, answers a whole question in one call, and gives compact
  output. Richard-facing views stay (contact sheets, the Studio, docs).

## 6 · Keeping it lean

- **Checks earn their place.** Every editor check guards something a player sees, a bar in §3, or a contract between
  systems. Checks pin ranges rather than exact tuning numbers. A check that only protects code nothing uses is retired with
  that code.
- **Flags have a lifetime.** A study or A/B flag goes, with its dead branch, once the study is written up. `--no-…` escape
  hatches stay while they're useful for comparisons.
- **Claude makes the clean-up calls** (Richard, 13 Oct: "continue to do so from your own call") and proves with same-seed
  races that nothing a player sees changed.
- **Docs** (Richard, 23 Oct: the docs were "bloating the information space"): the living records are design.md, world.md,
  how-we-work.md, next-steps.md and bug-inbox.md. Update the record when a decision changes, rather than adding a doc that
  supersedes it. Study numbers, round logs and history stay in git, not in the records.
- **Generated output** lives in `review/` (git-ignored) and the one dev build `../little-road-dev-build/`. Scratch goes in
  `review/tmp/`. `tools/cleanup.ps1` prunes old evidence to the Recycle Bin after every good build. Delete each capture run as
  soon as it has been checked (Richard, 14 Oct). Per-tick race telemetry is only kept with `-KeepTelemetry`/`--telemetry`.
- **Test windows are always muted** (Richard, 14 Oct). `Start-Quiet` (`. tools/quiet-start.ps1`) opens them without taking
  focus, so Richard's own game keeps the keyboard and pad. `--audible` is only for a deliberate listen. Don't run more than
  six games at once, and never run timing measurements in parallel.

## 7 · Build, test and release

From the project root in PowerShell, with the Unity editor closed:

| Tool | What it does |
|---|---|
| `.\tools\compile-check.ps1` | All C# in about 2 s with Unity's own compiler. Run it before any build. |
| `.\tools\build-dev.ps1` | Syncs the Forge cars and Prop Forge kits, runs the compile check, builds the player to `../little-road-dev-build/` with every editor check, then runs the clean-up. Run it in the foreground. |
| `capture-adventure.ps1 -Name smokeN` | Smoke test: quest, delivery, finds, buy and fit, a 24-car race and payout, reload |
| `capture-map.ps1 -Name x -Rendered -Flags @(...)` | Captures and test flags: `--area-audit`, `--edge-audit`, `--car-audit`, `--contact-review`, `--pause-review`, `--minimap-review`, `--view-shots`, `--spot-views "x,z,yaw[,h]"`, `--probe-line`, the activity tests (`--trap-test`, `--train-race-test`, `--wayfinding-test`, `--crop-test` …) |
| `regression.ps1 -Name x` | The full regression (§3) |
| `run-pace-parallel.ps1`, `summarize-racer.py`, `racer-traffic.py`, `racer-held.py` | Keep/reject races and their summaries; traffic, passes and held-up time |
| `run-frame-cost.ps1 -Track <id>`, `drive-benchmark.ps1`, `route-drive.ps1` | Race frame cost, free-roam frame cost, an autopilot drive |
| `event-race-review.ps1`, `duke-race-review.ps1`, `quarry-run.ps1`, `test-loop.ps1` | Enter any calendar event; the Duke's Challenge; Quarry Hills tests; the 2.5-mile gearing and sound test loop |
| `track-eval.py`, `track-plot.ps1` | Track geometry against the track-design skill's bands |
| `parts-ladder.py` | Writes `unity/Assets/Resources/Data/parts.json` (the parts ladder for every class) |
| `field-table.py` | The PP of the field at every planned C and B race, from `docs/data/` |
| `world-plan.py`, `world-plan-extract.py` | Breakdowns of the planned world from `docs/data/world-plan.json` (the world-plan skill) |
| `sound-harness.ps1`, `engine-compare.py`, `engine-ingame-*.py`, `engine-grains.py`, `fetch-sonniss.py`, `music/compose.py` | Sound: offline rev tests, the engine scorecard ([design.md](design.md) §9), grain banks from Sonniss recordings (kept outside the repo in `../little-road-audio-sources/`), music |
| `bayview_plan.py` | Generates the new Bayview's plan code |
| `publish-play.ps1` | Copies the dev build to Richard's play copy `../little-road-play/` and points the one "Little Road" desktop shortcut at it |
| `cleanup.ps1` | Prunes old evidence (dry run without arguments) |
| `keep-eol.py`, `edit-preserving.py` | Keep each file's line endings as in HEAD; replace text in either line-ending style |

- **Play copy:** publish only a build that passed its checks and a windowed smoke test, from `main`, then tell Richard.
  The shortcut runs with no flags; `-Flags` is for A/B copies only.
- **Forge cars are build output** (17 Oct, "think end product"): `tools/forge-cars.txt` lists the Car Forge designs, and
  `build-dev.ps1` exports and installs only the cars whose key changed. Never commit `Resources/Cars/forge_*`. A Unity
  licence error (exit 198) needs Richard to sign in to Unity Hub.
- **Skills:** `.claude/skills/track-design` (design or judge any track) and `.claude/skills/world-plan` (the planned map,
  places and race catalogue).

## 8 · Git and files

- Commit and push freely to Richard's own repos (crippsy14/Little-Road, crippsy14/car-forge), at verified milestones
  (Richard, 17 Oct). Other repos only when he asks. Never force-push or rewrite history unasked.
- Branches: `main` is what the play copy is built from; `claude-additions` is the working branch and is kept level with
  main. Push over HTTPS (fetch over SSH fails host-key checks from Windows; check the remote with
  `git ls-remote https://github.com/crippsy14/Little-Road.git`). git needs `-c safe.directory=<repo>` from the sandbox.
- Little Road stays on Windows (Unity), so the WSL rule for other projects doesn't apply here. Car Forge is pushed from WSL.
- **Source files:** edit with the Edit tool or byte-level Python, never PowerShell `Get-Content`/`Set-Content` or Git
  Bash `sed -i`. Sources are BOM-less UTF-8, and line endings differ by file (`.gitattributes: * -text`), so run
  `tools/keep-eol.py` before committing.
