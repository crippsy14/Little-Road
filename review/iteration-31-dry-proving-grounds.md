# Checkpoint 19: dry equipment proving grounds

21 September 2026. Implements the first phase of [equipment performance courses](../docs/24-equipment-performance-test-courses.md). These are development fixtures, not additional campaign tracks or content hours.

## Delivered

The actual RoadCar physics runs a 900 m standing-start straight, a matched 20 m/s brake stop and a 22 m-radius U-turn with approach/exit sectors. Authored closed routes reuse Circuit projection. Optional input policies request throttle/braking and steering through normal car physics; they do not prescribe a pose, lap time or finishing speed. All setups come from the existing equipment catalogue and use the same compact body.

Each run records setup/build identity, timing, speed, controls, route/travel distance, lateral motion, ordered sectors, contacts, recovery and completion status. JSON/CSV retain individual runs; the analyzer produces a summary, conditional checks and a four-panel graph. Repeated fixed-driver results establish equipment repeatability. Natural race-to-race skill variance remains a separate test phase governed by the existing [AI/teammate plan](../docs/20-driver-personality-and-teammate-progression.md).

`tools/run-performance-trials.ps1` runs a fresh batch and prints its output location. Defaults are ten loadouts, three courses and three repeats; hidden runs use accelerated simulation. `-Visible` uses normal time; `-Manual` also requires one course, one loadout and one repeat. The exported **Watch Proving Grounds.cmd** compares stock/club U-turn runs in a window. **Drive Proving Grounds.cmd** starts a manual club run. Both create unique output folders and never read or write adventure saves. Human driving/controller acceptance of these launchers remains open.

`tools/analyze-performance-trials.py <run-folder>` creates the report and plots. Plotting dependencies are pinned in `tools/python-plotting-lock.txt` and locally installed under ignored `tools/python-packages`; they are not game dependencies. This machine's installed package permissions require the analyzer to run outside the restricted shell sandbox.

## Verification and findings

Final build: [build-02.log](unity-24/build-02.log), 177 existing editor/import checks. Assembly-CSharp SHA256: `4B05692E95FD4FA0A7FBF4BA3E37922ACFE456D7FB40F13BF103E1AFD672AC82`.

- [Final matrix report](unity-24/matrix-01/report.md): 90/90 valid runs, 108/108 analysis checks. Ten loadouts × three courses × three repeats, all on the same assembly at 120 Hz. No contacts, recovery resets or missed gates. Deterministic timing ranges were zero; this is expected for this baseline, not a target for racing AI.
- [Graph](unity-24/matrix-01/equipment-comparison.png), raw batch/per-run JSON and CSV, and summary CSV remain available under `matrix-01`.
- Workshop engine improves measured 0–30 mph from 2.698 to 2.071 seconds. Club engine alone remains gearing-limited near 56 mph; adding long gearing reaches about 76.1 mph with slower initial acceleration than the same engine on stock gearing.
- Matched brake stops: stock 15.335 m; sport tyres 12.879 m; sport tyres plus sport brakes 9.702 m. Stronger brakes alone meet the stock tyre grip ceiling.
- Sport tyres raise U-turn minimum speed from 27.1 to 34.7 mph. However, stronger brakes add no further U-turn time gain because the reference driver's conservative braking reserve underuses them. This is an acknowledged policy limitation, not evidence that the brake upgrade has no value.
- [Normal-time comparison](unity-24/timescale-comparison.json): stock and club U-turn times and apex speeds match the accelerated runs. This validates those fixture measurements, not visible rendering performance.
- [Normal Bayview field regression](unity-24/field-final.json): all 24 cars finish three ordered laps; stock player 24th in 113.336 seconds. Zero recoveries/missed gates; maximum sampled car-box overlap 0.8774 mm and tagged-scenery overlap zero. This is sampled evidence, not a universal no-clipping guarantee.

The first pilot exposed small physical straight-line drift with zero steering; a gentle shared centring policy now corrects it through real steering requests. Timer steps and accumulated distance were also made numerically stable before the final matrix. An initial normal-time launcher check only ran stock because PowerShell array arguments were mishandled; array handling was corrected and `realtime-check-02` contains both stock and club. Do not count the initial single run as two-case evidence.

The field run did not persist a new vertical audit because its required captures folder was omitted. No new vertical-audit pass is claimed. Earlier checkpoint 18 browser/career/save evidence and checkpoint 17 world-clearance evidence are retained as prior checks of unchanged systems, not rerun evidence for this build.

## Milestone boundaries and next work

Visuals: plain, readable test geometry; no new Bayview art or campaign fidelity acceptance. Driving/racing: physical fixture results plus a normal full-field regression; physical DualSense and human manual completion/rejoin acceptance remain open. Progression: measurable conditional equipment effects, without new economy or unlock claims. Exploration/content duration: unchanged. Usability: reports and visible/manual entry points exist; human acceptance remains open. Rendering performance: accelerated/headless timings do not establish normal-window frame rate. Production reuse: route definitions and input-policy hooks are small extensions of the existing runtime, not a replacement framework.

Next improve the reference braking policy before treating U-turn times as ideal car limits, then add proper grade/ground support and hill/combined fixtures with ramp-regression coverage. Wet/off-road comparisons wait for real surface systems. Broaden equipment interactions and use evidence to brief later faster-class tracks while preserving Bayview's early-game role. The tyre category remains next in the shared parts browser. AI skills, teammate progression and familiarity follow doc 20's dedicated design pass; no attributes or levelling rules are implemented here.

The broad campaign goal stays active. Checkpoint 19 is packaged and promoted to the normal latest-build path. All 322 archive and delivered files match; the existing desktop shortcut target is verified. Checkpoint 18 is preserved in `little-road-unity-windows-before-19`. Archive SHA256: `4652072A92B5C8627D3B8F23F114281C77712FF89127DB8BCAC9C82BF7B5C055`. [Package manifest](unity-24/package-manifest.json), [promotion](unity-24/promotion.json) and [delivery verification](unity-24/delivery-verification.json) preserve the exact identities.
