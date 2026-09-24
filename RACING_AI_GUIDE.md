# Racing AI: code, results and running the game

Handoff: 24 September 2026. The development goal is paused. This guide maps the current Unity implementation; the Godot project in `game/` is a preserved earlier prototype.

## Current checkpoint

- **Retained driving foundation:** continuous local trajectories, equipment-based physics, observed traffic, passing, light drafting and the first driver personalities. Exact performance improvements through **109** are retained.
- **Latest exported executable: 111**, with **530 editor checks**. Its rolling-recovery experiment is **off by default**. A successful build is not acceptance of every experiment it contains.
- **Smoother viewing configuration:** the optional three-group planning schedule introduced in **108**. The user reported it was substantially smoother. Its broader incident/recovery gates remain open; it is not enabled by the adventure preset.
- **Rejected experiment:** 110's `--third-dynamic-candidate`. Keep it disabled: the Cedar pilot finished only 19/24 cars.
- **Unfinished at pause:** 111's Cedar rolling-recovery candidate exited normally, but its comparison/review has not been completed. Bayview's rolling candidate produced no pace gain and never used the new rolling state. [RollingRecoveryProbe.cs](unity/Assets/Editor/RollingRecoveryProbe.cs) was added after the frozen 111 build and has **not been compiled or run**. It approximates vertical collider size and is a diagnostic, not validated evidence.

The old root launchers and desktop shortcuts are not reliable selectors for the newest experiment. Use the explicit commands below. Do not overwrite a numbered export or evidence folder.

## Where to run and build

On the original PC, the project root is:

```text
C:\Users\cripp\Documents\Codex\2026-09-16\hey\outputs\little-road
```

Open its **`unity`** subdirectory in Unity Hub, using **Unity 6000.6.1f1** with Windows build support. Packages are pinned in [manifest.json](unity/Packages/manifest.json) and [packages-lock.json](unity/Packages/packages-lock.json). The scene is [CoastalTrial.unity](unity/Assets/Scenes/CoastalTrial.unity); [RaceTrial.cs](unity/Assets/Scripts/RaceTrial.cs) configures adventure or trial mode. Starting the editor scene without the experiment flags does not reproduce the benchmark configuration.

Existing Windows exports are **siblings of the project**, not files inside the repository:

| Purpose | Executable relative to this root | Notes |
|---|---|---|
| Latest compiled experiment | `../little-road-context-111-review/Little Road Unity.exe` | Latest exported code; rolling recovery and third candidate remain opt-in. |
| Retained exact optimization checkpoint | `../little-road-context-109-review/Little Road Unity.exe` | Useful stable comparison executable. |
| User-reviewed smoother scheduling | `../little-road-context-108-review/Little Road Unity.exe` | Needs the three-group flags to reproduce the smoother setup. |
| Accepted dark showroom/UI checkpoint | `../little-road-context-101-review/Little Road Unity.exe` | Older AI performance; useful visual reference. |
| Older promoted adventure | `../little-road-unity-windows/Little Road Unity.exe` | Root `Launch Little Road Unity.cmd` points here; not automatically updated by experiments. |

These exports and their complete `_Data` folders remain local. A fresh clone must build an executable; copying the `.exe` alone is insufficient. The root `Watch Experimental 24-Car AI.cmd` still points to the older **traffic-07** export, not the current context driver.

### Watch the latest 24-car simulation

Run PowerShell **from the project root**, with other project editors/games closed. This opens a normal-speed 1920×1080 window and uses a new evidence directory:

```powershell
$runName = 'watch-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
.\tools\run-frame-cost.ps1 -Version 111 -Study 111 -Track cedar -Seed 1709 -Name $runName -Rendered -StaggerContextPlans -ContextPlanGroups 3
```

This selects the mixed roster, equipment, physical recovery, dynamic paths, light drafting, workers and HUD through the checked-in harness. It deliberately leaves the rejected third candidate and unaccepted rolling recovery off. Use `-Track bayview` for the coastal circuit. Omit `-StaggerContextPlans` to compare the unscheduled baseline. The report is under `review/unity-35/frame-cost-111/<runName>/`; the harness waits for finish or the 180-second cutoff.

For manual control, custom fixtures, car count, fullscreen or a different executable, use [run-technical-trial.ps1](tools/run-technical-trial.ps1). It supports `-Manual`, `-Visible`, `-Windowed`, `-Fullscreen`, `-PlaybackRate 1`, `-Executable` and `-OutputDirectory`. **Preserve the full option set from `run-frame-cost.ps1` when comparing manual and AI runs**; the low-level harness defaults to the older legacy policy. The rear view is handled by [DrivingViewInput.cs](unity/Assets/Scripts/DrivingViewInput.cs), including Triangle on the PS5 controller.

To drive the latest adventure rather than the automatic trial:

```powershell
& '..\little-road-context-111-review\Little Road Unity.exe'
```

The connected adventure uses [AdventureDriverPreset](unity/Assets/Scripts/RaceRoster.cs), not every experimental switch. The known second-race recovery problem remains open; this is not a fully accepted campaign build.

### Build the source

[MigrationBuild.Build](unity/Assets/Editor/MigrationBuild.cs) runs the custom editor checks, imports assets, creates the trial scene and exports Windows. It is also exposed as **Little Road → Build and check Windows trial**. The menu's default output is the mutable adventure export; use an explicit fresh output for development:

```powershell
$projectRoot = (Get-Location).Path
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$export = Join-Path (Split-Path $projectRoot) "little-road-local-$stamp/Little Road Unity.exe"
$log = Join-Path $projectRoot "review/build-local-$stamp.log"
& 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe' -batchmode -quit -projectPath "$projectRoot\unity" -executeMethod MigrationBuild.Build --review-export "$export" -logFile "$log"
```

Wait for the process and inspect the log for `LITTLE_ROAD_BUILD_OK`, compiler errors and test failures before launching. Standard builds are nondevelopment: do not add `--development-build` for ordinary viewing, because that enables Unity's PlayerConnection listener and can provoke the firewall prompt. Do not disable Windows Firewall.

Numbered evidence builds additionally use [freeze-prototype.py](tools/freeze-prototype.py) after a successful build with the expected numbered export/log paths. It freezes source hashes and assembly identity and refuses to overwrite an existing archive. The current editor-only probe means rebuilding source is not byte-identical to the archived 111 source set.

## How one AI update works

```text
RaceTrial / RaceRoster / equipment / authored circuit and guide line
    ↓
RaceWorldSnapshot: one immutable observation of every car
    ↓
owned candidate ranking + physical forecast jobs → join workers
    ↓
RoadCar.PlanObserved → ContextDriverPolicy.Decide
    ↓
rank continuous local paths → road/traffic/scenery feasibility → select
    ↓
path follower → unresolved response → moving-leader limit
    → scenery stopping limit → immediate traffic guard
    ↓
DriveCommand (pedal + steering) → DriveModel / Rigidbody physics
```

Ordinary racing uses physical steering and forces. The guide line is a reference, not a position rail. Forecasts cannot see future opponent inputs. Recovery has a separate controller and an explicitly counted assisted-reset fallback.

### Core entry points and ownership

| Area | Code reference / symbols | Responsibility |
|---|---|---|
| Trial lifecycle and configuration | [RaceTrial.cs](unity/Assets/Scripts/RaceTrial.cs) | CLI flags, track, roster, countdown, finish/export and diagnostics. |
| Entrants and adventure defaults | [RaceRoster.cs](unity/Assets/Scripts/RaceRoster.cs): `RaceEntrant`, `RaceRoster`, `AdventureDriverPreset`, `CarPerformanceRating` | Immutable fitted parts, physical tuning, identity, grid and preview values. Explicit CLI overrides take precedence. |
| Shared observation | [RaceWorldSnapshot.cs](unity/Assets/Scripts/RaceWorldSnapshot.cs): `Capture`, execution order −50 | Stable-ID snapshot before controller updates; dispatch and join owned forecasts. Currently bounded to **24 cars**; larger fields require capacity and validation work. |
| Driver contract | [DriverPolicy.cs](unity/Assets/Scripts/DriverPolicy.cs): `ObservedCar`, `DriverObservation`, `DriveCommand`, `IDriverPolicy` | Actual body dimensions/centre, motion, immutable traffic, equipment and command interface. |
| Physical vehicle | [RoadCar.cs](unity/Assets/Scripts/RoadCar.cs): `FixedUpdate`, `PrepareRoadForecast`, `PlanObserved` | Sensor/driver selection, actual Rigidbody forces, physical collider, manual control and recovery integration. |
| Alternative drivers | [LegacyDriverPolicy.cs](unity/Assets/Scripts/LegacyDriverPolicy.cs), [ReferenceDriverPolicy.cs](unity/Assets/Scripts/ReferenceDriverPolicy.cs), [TrafficDriverPolicy.cs](unity/Assets/Scripts/TrafficDriverPolicy.cs) | Preserved historical baselines. Do not mistake them for the current context driver. |
| Main racing policy | [ContextDriverPolicy.cs](unity/Assets/Scripts/ContextDriverPolicy.cs): `PrepareForecast`, `Select`, `Decide`, `PlanCommand` | Candidate selection, continuation/fallback, staged commands and diagnostic attribution. |

### Trajectories, speed and racecraft

| Area | Code references | Important behavior |
|---|---|---|
| Track and projection | [Circuit.cs](unity/Assets/Scripts/Circuit.cs), [TechnicalCircuit.cs](unity/Assets/Scripts/TechnicalCircuit.cs), [RouteProjectionIndex.cs](unity/Assets/Scripts/RouteProjectionIndex.cs) | Shared road geometry, wrapped progress, width, nearest projection and authored Cedar layout. |
| Guide and equipment speed profile | [RacingLine.cs](unity/Assets/Scripts/RacingLine.cs): `RacingLine`, `RacingSpeedProfile` | Authored/baked guide plus grip, acceleration and backward braking limits. Resource assets live under `unity/Assets/Resources/Tracks` and `RacingLines`. |
| Local continuous choices | [ContextPreferenceMap.cs](unity/Assets/Scripts/ContextPreferenceMap.cs) | Track-width preferences, relevance, reachable space and rear-pressure filtering. Sampled preferences are not permanent three-lane rails. |
| Personal dynamic route | [DynamicRejoinPlanner.cs](unity/Assets/Scripts/DynamicRejoinPlanner.cs): `Prepare`, `PrepareTraffic`, `EstimateGateTime` via follower | Up to six continuous local candidates toward a common downstream horizon; usual detailed shortlist is two. Includes current-motion/continuation and passing alternatives. |
| Actual path tracking | [ContextPathFollower.cs](unity/Assets/Scripts/ContextPathFollower.cs): `ContextPathState`, `Lateral`, `BuildEnvelope`, `Follow` | Smooth curve geometry, velocity-heading pursuit, local curvature limits and acceleration/braking feed-forward. Live and forecast control must agree. |
| Committing to a pass | [OpenSpacePriority.cs](unity/Assets/Scripts/OpenSpacePriority.cs), [PassingProgress.cs](unity/Assets/Scripts/PassingProgress.cs), [PassingTargetContinuation.cs](unity/Assets/Scripts/PassingTargetContinuation.cs), [PassTrajectory.cs](unity/Assets/Scripts/PassTrajectory.cs) | Opportunity/commitment and retained passing targets; inspect active flags before changing historical helpers. |
| Following versus passing | [MovingLeaderControl.cs](unity/Assets/Scripts/MovingLeaderControl.cs), [FollowingFallbackSelection.cs](unity/Assets/Scripts/FollowingFallbackSelection.cs) | Ahead same-corridor stopping envelope and explicitly checked following alternatives. A leading car must not give way merely because someone approaches from behind. |
| Coasting and drafting | [CoastingEnvelope.cs](unity/Assets/Scripts/CoastingEnvelope.cs), [DraftingModel.cs](unity/Assets/Scripts/DraftingModel.cs) | Coasting when feasible and a modest drag reduction from an observed car ahead. Does not invent engine power or bypass gearing. |

### Physical checks, recovery and performance

| Area | Code references | What must remain true |
|---|---|---|
| Vehicle capability | [DriveModel.cs](unity/Assets/Scripts/DriveModel.cs), [EquipmentCatalog.cs](unity/Assets/Scripts/EquipmentCatalog.cs) | Parts determine power, mass, drag, gearing, grip and brakes. AI traits do not secretly upgrade equipment. |
| Physical path rollout | [ContextPathPrediction.cs](unity/Assets/Scripts/ContextPathPrediction.cs), [PlanarDrivePrediction.cs](unity/Assets/Scripts/PlanarDrivePrediction.cs) | Bounded flat/contact-free prediction using the same control/force model. A short accepted rollout is not proof of an entire pass. |
| Road and scenery feasibility | [RoadPathFeasibility.cs](unity/Assets/Scripts/RoadPathFeasibility.cs), [ForecastRoadEnvelope.cs](unity/Assets/Scripts/ForecastRoadEnvelope.cs) | Actual body envelope, moving side entry, supported corridor and swept scenery checks. |
| Traffic prediction | [TrafficPathFeasibility.cs](unity/Assets/Scripts/TrafficPathFeasibility.cs), [ObservedTrafficRollout.cs](unity/Assets/Scripts/ObservedTrafficRollout.cs), [ObservedMotionPredictor.cs](unity/Assets/Scripts/ObservedMotionPredictor.cs) | Observed velocity/yaw, bounded traffic sweeps and route-aware adjacency; no clairvoyance. |
| Immediate collision response | [ImmediateTrafficSafety.cs](unity/Assets/Scripts/ImmediateTrafficSafety.cs) | Runs at physics cadence; tests nominal/coast/braking responses. Does not choose the racing route or write velocity. |
| Physical recovery | [PhysicalRecovery.cs](unity/Assets/Scripts/PhysicalRecovery.cs): `PhysicalRecovery`, `RecoverySensor` | Stabilise, forward/reverse, wait for traffic, return to missed gates, separately counted assisted reset. |
| Experimental moving rejoin | [RollingRecovery.cs](unity/Assets/Scripts/RollingRecovery.cs), [RoadSupport.cs](unity/Assets/Scripts/RoadSupport.cs) | Coast/brake-only return for aligned moving cars on explicitly supported shoulder. Body/scenery/traffic checks and stopped-recovery fallback. `--rolling-recovery` remains off. |
| Worker ownership and joining | [ForecastWorkerPool.cs](unity/Assets/Scripts/ForecastWorkerPool.cs), [DynamicForecastPreparation.cs](unity/Assets/Scripts/DynamicForecastPreparation.cs) | Copied owned inputs; persistent workers; join before authoritative commands; scene queries stay on main thread. |
| Exact reuse | [ForecastPathCache.cs](unity/Assets/Scripts/ForecastPathCache.cs), `DynamicRejoinPlanner.PrepareOwned/TryUsePrepared` | Demand-generated geometry and single-use ranking reuse only for matching complete input/configuration. Mismatch falls back to fresh work. |
| Optional burst smoothing | [StaggeredPlanClock.cs](unity/Assets/Scripts/StaggeredPlanClock.cs) | Spreads approximately 0.1-second planning across groups; steering and immediate guard remain full-rate. Changes observation timing, so exact race parity is not expected. |

### Personalities, progression and UI

[DriverIdentity.cs](unity/Assets/Scripts/DriverIdentity.cs), [DriverPlanningSettings.cs](unity/Assets/Scripts/DriverPlanningSettings.cs) and `Resources/Data/drivers.json` provide stable identity, braking judgement, risk/passing clearance and seeded race-to-race form. Risk 0/5/10 currently corresponds to preferred passing spare space 0.80/0.35/0.10 m; hard physical checks still apply. Consistency currently changes braking-form amplitude, not every driving skill. Teammate levelling, track familiarity and a full skill system remain future scope.

[RacePreviewUI.cs](unity/Assets/Scripts/RacePreviewUI.cs), [RacePaddockInspector.cs](unity/Assets/Scripts/RacePaddockInspector.cs) and [CarShowroom.cs](unity/Assets/Scripts/CarShowroom.cs) display the same entrant records used by the race. [RaceHud.cs](unity/Assets/Scripts/RaceHud.cs), [RaceTiming.cs](unity/Assets/Scripts/RaceTiming.cs), [RaceLapHistory.cs](unity/Assets/Scripts/RaceLapHistory.cs) and [RaceTrackMap.cs](unity/Assets/Scripts/RaceTrackMap.cs) handle live position, gaps/laps and dashboard presentation. Add new traits to their actual planning consumer and telemetry before presenting them as functional abilities.

## Best established results and comparison baselines

There is no meaningful single fastest result across different tracks, equipment, starting positions and driver rosters. **Collective time is the sum of all entrants' official finish times**, and is unavailable if anyone fails to finish. Official standing-grid time includes the run from the grid after countdown; flying/start-line lap time is a different clock. Partial-course fixtures and the human's more permissive edge use must remain labelled.

The table records the best useful established milestones and known outstanding gates, not a cherry-picked unconditional leaderboard. A [machine-readable run index](review/test-results-index.csv) also preserves every readable `race.json` summary available at handoff, including failures and manual runs. It is an inventory, not automatic acceptance. Historical reviews and raw reports take precedence over rounded values here.

| Test family | Recorded result / useful best | Evidence and limits |
|---|---|---|
| Current build checks | **530 checks**, prototype 111; 175 frozen source/script inputs | [111 manifest](review/unity-35/prototype-111-manifest.json), [checks](review/unity-35/build-context-111-checks.json). Post-build Editor probe excluded. |
| Equipment proving grounds | **183 valid runs**: 108 flat and 75 grade/coasting; **129/129** flat and **147/147** grade analysis checks | [Physical grade review](review/iteration-33-physical-grade-fixtures.md). Includes the 35.2 m hill, U-turn/combined courses and gravity controls. |
| Capability smoke | **27/27** valid; straight stock/street/club **40.400/37.792/28.342 s**; U-turn **23.617/20.692/16.833 s**; braking from 20 m/s **15.335/12.201/9.239 m** | [Capability table](review/unity-35/capability-summary.md). Repeats identical in this environment, not broad calibration proof. |
| Dynamic solo corners | **32/32** runs finish cleanly; median corner gains **0.579 s** at 20 m/s and **0.550 s** at 25 m/s | [77 review](review/unity-35/dynamic-rejoin-77-review.md). Eight equipment/entry combinations improve; partial-course times, not full race laps. |
| Two-/three-car dynamic traffic | All **18 entrants across 8 fixtures** finish; corner-pair collective gains **5.075/4.425 s**, corner-three **7.233 s**; no contacts/departures/recovery | [80 review and full eight-row table](review/unity-35/dynamic-traffic-80-review.md). Two strict pass-confirmation deadlines still fail. 83's regression suite retains those failures. |
| Cedar neutral 24-car baseline | **2,136.924683 car-seconds**, preview **86.559273 s** / 7th; **24/24** finish, no recovery; **42.497955 s** better than 81 | [83 review](review/unity-35/dynamic-field-83-review.md). Three minor footprint-edge samples remain; 85 preserves its racing. |
| Bayview original versus dynamic | Solo club **30.875 → 24.458 s**; 24-car sum **903.178 → 817.039 s** (**86.139 s gain**); preview **39.701 → 35.209 s** | [84 transfer](review/unity-35/bayview-transfer-84-review.md). Both fields finish; candidate has marginal edge excursions, no contacts/recovery. |
| Current unscheduled mixed roster | Bayview **812.755 car-seconds**, preview **37.809 s**; Cedar **2,145.356 car-seconds**, preview **85.276 s**; all 24 finish | [86 identities](review/unity-35/driver-identities-86-review.md). Mixed and neutral are different competitions; exact optimizations preserve their baselines. |
| Scheduler: best observed Bayview field in the three-seed study | Seed 1709 **806.436 car-seconds**, preview **36.484 s** | [108 matrix](review/unity-35/frame-cost-108/matrix-summary.json), [review](review/unity-35/frame-cost-108-review.md). Not a universally accepted best: impact-speed screen fails; seed 1711 loses 23.821 combined seconds through recovery. |
| Scheduler: best observed Cedar preview in the three-seed study | Seed 1710 **79.944 s**, versus approximately **84.15 s** unscheduled | [108 raw matrix summary](review/unity-35/frame-cost-108/matrix-summary.json). The older prose review says 79.941; this guide uses the recorded summary's 79.944221. Reaches the approximate 80-second aspiration for this seed; contact screen remains failed. Seed 1709 is **81.444 s** and **2,144.131 car-seconds**. |
| Scheduler: best observed Cedar collective in that study | Seed 1711 **2,132.017548 car-seconds**, preview **81.643806 s** | [108 matrix](review/unity-35/frame-cost-108/matrix-summary.json). This pair passes all declared 108 gates; a different seed/roster draw from the neutral 83 benchmark, not a matched 4.9-second algorithmic gain. |
| Scheduler: multi-seed collective result | Bayview mean gain only **0.081 s**; Cedar mean **1.261 s** | [108 review](review/unity-35/frame-cost-108-review.md). All twelve off/on fields finish; do not advertise the best seed as the average. |
| Human achievable race | Representative attempt **67.455605 s**, 1st; earlier retained attempt **71.787880 s**, 2nd; same-build AI **85.501198 s**, 4th | [57 human review](review/unity-35/human-field-57-review.md). Same upgraded car/rear grid. Human uses more road edge; reports stop at player finish, so no valid whole-field total. Unseen attempt 2 excluded explicitly. |
| Risk trait causal fixture | Risk 0/5/10 sustained pass at **9.150/7.333/6.742 s**; follower finish **26.875/26.450/26.241 s** | [86 identities](review/unity-35/driver-identities-86-review.md). Clean identical-equipment moving-start encounter; no universal risk-benefit claim. |
| Consistency/form | **40/40** solo runs finish cleanly; variable SD **0.026279 s**, range **24.433060–24.524725 s**; steady observed SD **0** | [86 form](review/unity-35/driver-form-86-review.md). Small real effect; not yet convincing lively position variance. |
| Light drafting | Isolated 26–32 m/s acceleration **1.2083 → 1.1083 s** | [70 review](review/unity-35/drafting-70-review.md). Retained/user-liked; field effects mixed, not a guaranteed collective gain. |
| Rolling recovery fixtures | **86/86** checks across **12** stock/club scenarios; all finish, no contacts/assisted resets. Clear rejoin takes about **1.17–1.28 s**, still moving around **18.3–18.5 m/s** | [111 fixture analysis](review/unity-35/rolling-111/fixtures/analysis.json). Includes blocked/wrong-way/off-road/missed-gate controls. Synthetic starting scenarios, not proof of field recovery. |
| Rolling recovery field pilot | Bayview remains **836.076 car-seconds**, preview **35.475 s**, same **15.308 s** recovery | [111 Bayview comparison](review/unity-35/frame-cost-111/bayview-after/rolling-comparison.json). No rolling state used: failed effectiveness gate. Cedar review pending. |

### Rendering/AI performance records

These are normal-speed, rendered 1920×1080 comparisons on the development PC (RTX 4070 Ti SUPER), not hidden/accelerated runs. Whole-AI means include shared snapshot/preparation, joined worker wait and decisions. Do **not** add summed worker elapsed to joined wall time. Zero allocation only covers the instrumented warm driver/worker regions, not the whole game.

| Retained change / study | Bayview result | Cedar result | Evidence |
|---|---|---|---|
| 103 demand geometry | Whole-AI **5.7582 → 4.6279 ms/tick**, −19.63% | **4.4229 → 3.6571**, −17.31% | [103](review/unity-35/frame-cost-103-review.md); exact recorded racing. |
| 105 ranking reuse | **4.6324 → 3.8426**, −17.05% | **3.6482 → 3.0533**, −16.31% | [105](review/unity-35/frame-cost-105-review.md); exact recorded racing. |
| 106 worker ranking | **3.8172 → 3.1154**, −18.39% | **3.0549 → 2.5298**, −17.19% | [106](review/unity-35/frame-cost-106-review.md); exact recorded racing. |
| 109 axis reuse | **3.11631 → 3.09484**, −0.689% | **2.52933 → 2.51300**, −0.645% | [109](review/unity-35/frame-cost-109-review.md); 40,000 bit-exact swept checks and full race parity. Small single-pair effect. |
| 108 smoother three-group schedule | Frame p95 **16.50–18.15 ms**, p99 **21.75–24.36 ms** | p95 **11.60–13.84 ms**, p99 **18.88–19.15 ms** | [108 three-seed study](review/unity-35/frame-cost-108-review.md). Total joined AI cost rises ~6–9%; smoothing bursts is distinct from reducing total cost. |

Budgets remain **16.7 ms frame p95**, **33.3 ms frame p99**, **3 ms AI per rendered frame p95** and zero warm measured allocation. Bayview and AI p95 still miss some targets; startup stalls remain. Later 110 baseline arms measured p95 **17.881/11.565 ms** and p99 **24.063/18.600 ms** (Bayview/Cedar), but these single pairs are not a new algorithmic performance milestone.

**Do not promote the tempting failed result:** 110's extra candidate improved Cedar preview to **80.494 s**, but only **19/24** finished, with **7 assisted resets**. Bayview lost **18.913 collective seconds**. [Full rejected pilot](review/unity-35/pace-audit-110-review.md). Incomplete fields have null collective time, never a sum that treats unfinished cars as zero.

## Tests, telemetry and safe extension points

| Need | Start here |
|---|---|
| Normal-speed 24-car timing and allocations | [run-frame-cost.ps1](tools/run-frame-cost.ps1), [analyze-driver-frames.py](tools/analyze-driver-frames.py), [DriverFrameAudit.cs](unity/Assets/Scripts/DriverFrameAudit.cs) |
| Collective pace, rank, incidents, checkpoint flow | [analyze-field-race.py](tools/analyze-field-race.py), [analyze-checkpoint-flow.py](tools/analyze-checkpoint-flow.py), [analyze-opening-straight.py](tools/analyze-opening-straight.py), [TechnicalCircuitReview.cs](unity/Assets/Scripts/TechnicalCircuitReview.cs) |
| Exact optimization regression | [verify-context-race-parity.py](tools/verify-context-race-parity.py), [analyze-planning-phases.py](tools/analyze-planning-phases.py); keep original failed reports and scoped exceptions |
| Why a car lifts or brakes | `FollowerPedal`, `AfterLeaderPedal`, `BeforeSafetyPedal`, `AppliedPedal`, `PedalTimestamp` in context policy; [analyze-pedal-stages.py](tools/analyze-pedal-stages.py), [analyze-pace-bottlenecks.py](tools/analyze-pace-bottlenecks.py) |
| Reconstruct a rejected local route | [ContextPlanAudit.cs](unity/Assets/Scripts/ContextPlanAudit.cs), [PaceCandidateReplay.cs](unity/Assets/Editor/PaceCandidateReplay.cs), [DynamicTrafficReplay.cs](unity/Assets/Editor/DynamicTrafficReplay.cs) |
| Controlled passing/corners/rear approach | `tools/run-dynamic-traffic-*.ps1`, [run-traffic-fixtures.ps1](tools/run-traffic-fixtures.ps1), [run-rear-approach-controls.ps1](tools/run-rear-approach-controls.ps1) and their matching analyzers |
| Physical recovery | [RecoveryFixture.cs](unity/Assets/Scripts/RecoveryFixture.cs), [run-recovery-fixtures.ps1](tools/run-recovery-fixtures.ps1), [analyze-recovery-fixtures.py](tools/analyze-recovery-fixtures.py), [RollingRecoveryChecks.cs](unity/Assets/Editor/RollingRecoveryChecks.cs) |
| Equipment balance | [PerformanceCourse.cs](unity/Assets/Scripts/PerformanceCourse.cs), [PerformanceTrial.cs](unity/Assets/Scripts/PerformanceTrial.cs), [run-performance-trials.ps1](tools/run-performance-trials.ps1) |
| Editor invariants | [MigrationBuild.cs](unity/Assets/Editor/MigrationBuild.cs) invokes the `*Checks.cs` classes in `unity/Assets/Editor`; worker, reuse, geometry, recovery and trait suites live here |

Python analyzers use Python 3; plotting additionally uses the versions in [python-plotting-lock.txt](tools/python-plotting-lock.txt). Typical read/analyze commands, after a run has finished:

```powershell
python .\tools\analyze-driver-frames.py .\review\unity-35\frame-cost-111\YOUR-RUN
python .\tools\verify-context-race-parity.py BASELINE-FOLDER CANDIDATE-FOLDER
```

Detailed plan/pedal audit modes allocate and must be separated from performance measurements. Recovery stops calling the racing policy, so stale context-stage samples cannot be attributed to recovery braking. Contact participant records are not unique crashes; relative contact speed is not collision impulse. Sampled overlap cannot prove continuous absence of clipping.

For a new change, hold build/track/line/grid/parts/seed/roster/flags fixed, declare the intended metric and failure bounds, run causal small fixtures first, then both 24-car tracks and several paired seeds. Record all finishes, collective and car0 times, checkpoint clearing, overtakes, recovery/assists, road excursions, contacts and frame tails. Never buy a faster preview time with a stranded field or hidden power bonus.

## Research, scope and next unresolved work

- [41: research and next pace pass](docs/41-racing-ai-research-and-next-pace-pass.md): primary sources including F1 context steering, racing track representation, local trajectory planning and GT Sophy testing lessons. This is an original implementation inspired by published approaches, not imported F1 AI.
- [32: ground truth and implementation](docs/32-racing-ai-ground-truth-and-implementation.md), [33: physical feasibility](docs/33-physical-motion-and-path-feasibility.md), [37: dynamic trajectories](docs/37-dynamic-local-racing-trajectories.md).
- [38: personalities and adventure](docs/38-driver-personalities-and-adventure-milestone.md), [20: future driver/team progression](docs/20-driver-personality-and-teammate-progression.md).
- [17: full scope](docs/17-scope-and-priorities.md): twelve-hour campaign, exploration, ownership, content, endurance/fuel/tyres/pits and teammate progression remain required. This AI milestone does not complete the game.

On resumption, finish the 111 evidence review and investigate why the supported moving return rejects the actual Bayview recovery state. Preserve the user-liked urgency, drafting, smoother presentation and the existing field baseline. The extra candidate remains rejected. Connected second-race recovery, broader frame tails, meaningful personality variance, physical-controller/small-window acceptance and the wider game remain open.

## What is stored in Git

Source, editable assets, Unity metadata/settings, research, reviews, result summaries, launch configurations and review-linked screenshots are committed. Large per-tick telemetry, repetitive unreferenced capture sequences, caches and exported players remain local, by the user's request. Nothing is deleted. [Local evidence inventory](review/local-evidence-inventory.csv) lists the excluded review files and sizes; [run index](review/test-results-index.csv) preserves compact race outcomes. Some older reviews link to local-only raw evidence: restore it from the original PC or rerun the documented fixture to perform those detailed replays on a fresh clone. A Git clone is therefore a source/results handoff, not a full raw-evidence or playable-binary backup.
