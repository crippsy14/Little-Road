# Shutdown investigation and requested full-field preview

21 September 2026. Continued from iteration 40. Goal turn makes progress through controlled shutdown evidence, explicit windowed launching and configurable traffic starts. The user then requested a fun experimental 24-car race; prioritise opening that preview before further shutdown investigation.

## Shutdown observations

Same frozen traffic-04 executable:

- `baseline-fullscreen`: 4x speed, graphics-worker mode, no live capture; normal exit 0.
- `direct-fullscreen`: 4x speed, single-threaded rendering via Unity's documented `-force-gfx-direct`; normal exit 0.
- `inspected-fullscreen`: 1x speed, graphics-worker mode, live game capture at 14.1 simulated seconds; normal exit 0.
- `detached-fullscreen`: usual launcher at 1x speed; process no longer present and log reaches CodeReloadManager/PlayerConnection cleanup. Exact exit code unavailable because the originating launcher had returned.

Traffic-05 adds only `--start-windowed`, which overrides the saved fullscreen preference without saving a new preference:

- `windowed-05`: 1x speed; log confirms Windowed; normal exit 0. Attempted capture displayed an occluding app, so do not claim that image verified the game.
- `slow-inspected-05`: 0.25x speed; initially fullscreen, subsequently switched to windowed during the run (recorded in the log; cause not inferred). A valid foreground game capture shows the car at 7.4 simulated seconds in windowed mode. Both cars finish, but the process remains live after Input System shutdown and is deliberately stopped after 30 seconds by the verified-process probe. See `shutdown.json`. This reproduces the failure with the launching process retained; it is not explained merely by a detached launcher. Do not describe this mixed-mode run as a pure fullscreen control.

No root cause or fix established. Failures so far correlate with the slower/longer visual review conditions; duration, time scale, repeated capture and display interaction are not isolated. Next compare slow inspected and uninspected runs with the direct-rendering control, keeping display mode fixed. Do not add forced process termination to game shutdown as a purported fix.

The probe saves exact launch arguments and process outcomes. A report file is not treated as proof that the process exited; forced cleanup is explicitly distinguished from normal exit. All runs leave campaign saves alone.

Unity's [player command-line reference](https://docs.unity3d.com/6000.0/Documentation/Manual/PlayerCommandLineArguments.html) documents the single-threaded-rendering diagnostic. The historical [Windows input shutdown issue UUM-91181](https://issuetracker.unity.com/issues/11183) is a research lead only; it does not establish the cause here. The installed version is 6000.6.1f1.

## Traffic fixture changes

Traffic-06 adds explicit recorded starting gap (12–60 m) and speed (5–30 m/s), with initial full-body clearance validation. Defaults remain 24 m / 18 m/s. These variations have been built but not yet run, so no broader acceptance is claimed.

Traffic-07 adds the user-requested `field` preview: 24 cars on Cedar's normal stationary grid, normal checkpoint/lap accounting, ordinary shared vehicle physics, new traffic policy and physical recovery. It bypasses mid-lap fixture placement and starting-velocity injection. Full-field telemetry records decisions but does not reuse the two-car straight's pass scoring. The normal adventure build is unchanged.

`Watch Experimental 24-Car AI.cmd` launches three laps at normal speed, with the camera car using club equipment and the usual street/club opponents. It is an experiment, not a passed 24-car traffic-AI milestone. Congestion, collision, recovery and planner CPU cost remain to be measured.

## End-of-day handoff

User requested pausing after opening the preview. Build 07 passed 239 editor checks and was opened at 1x speed, 24 cars, three laps, normal standing grid, in `field-visible-07` (launched PID 26840; verify live state rather than reusing this ID blindly). The game is deliberately left for the user to watch; no final race outcome is claimed here.

A live view at 25.3 s shows the camera car 23rd at 9 mph and considerable congestion around the start. Logs show repeated physical-recovery/rejoin transitions among queued cars. This is the first concrete full-field failure to investigate next: normal grid waiting is still being confused with being stuck, and the traffic planner rejects many options in the dense pack. Do not call this a successful 24-car AI integration or silently disable recovery to make the test pass. Inspect the completed report if present before choosing the fix. The user may steer or close the preview.

The slow shutdown probe reproduced the hang and cleaned up its own process before the field preview opened. No shutdown fix was established. Do not repeat broad testing just because a prior wait expired; check actual process/report state on resume.

User feedback after watching: the 24-car preview becomes a "traffic jam simulator". Explicit restart priority: first get cars behaving like racers in the full field, then introduce personalities. Improve grid launch, decisive use of clear space, sustained racing pace, overtaking and sensible following/recovery transitions using equal-competence drivers first. Assess actual moving 24-car races as well as local fixtures; personality differences must build on competent baseline racecraft. This note does not resume the paused goal.
