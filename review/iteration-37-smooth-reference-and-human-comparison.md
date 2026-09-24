# Smoother solo racing reference and human pace comparison

21 September 2026. Progress within phase B of the existing AI plan. This is a bounded Cedar engineering milestone; the overall campaign goal remains active.

## Finding and change

The user's four-lap club-car recording is preserved in `review/technical-runs/20260921-151207-48cdcf`. At their request, lap 2 is excluded from pace comparison. Approximate times for laps 1, 3 and 4 are 63.7, 63.0 and 62.6 s; the official best is 62.601 s. All samples in those laps are labelled human. The run has one missed gate and one assisted recovery associated with the lap-2 excursion. Other selected laps use the road edges, with some body overhang beyond asphalt, so they are useful pace/context evidence rather than automatically satisfying the stricter AI full-body clearance criterion. No human trace was used as optimiser input or copied into the generated line.

The original reference driver tracks its candidate closely but slows sharply in sector 4. Its speed profile is constrained by local geometric curvature peaks: the old path reaches roughly 0.0573 /m in that section. Scalar offsets interpolated on the road's C1 geometry reintroduce short curvature changes even when the offset controls look smooth. This is evidence about the planning path, not a need to grant the AI extra power or grip.

The baker now has a separate `--smooth-only` experiment: periodic C2 Cartesian cubic B-spline samples from the bounded control polygon, stored as schema 2 with source-track hash. Runtime keeps these points instead of reconstructing them from a scalar road offset. Constructor checks preserve local route correspondence, finite points, height and the original ±5.01 m centre corridor; editor checks project a conservative body envelope at each sample. Existing schema-1 paths remain available as controls.

The first smooth bake exceeded the existing corridor and build 05 correctly rejected it. Its control polygon was then inset to ±4.6 m, allowing for spline corner rounding while keeping the final ±5.01 m constraint. The retained `build-05.log` records that failed attempt. Build 06 passes 220 editor checks. Vehicle forces, grip, steering response and the reference driver commands are unchanged by this experiment.

## Physical evidence

The original five candidates each ran three laps with stock and club equipment: all ten completed without contacts, recovery, missed gates or sampled full-body departures. `candidate-matrix-03/comparison.json` contains 30/30 outcome checks. Minimum-curvature was the fastest of those seeds: 92.694 s stock and 66.334 s club, versus centre-control times of 95.710 s and 68.750 s. Early/late shifted and half-offset variants were retained rather than discarded from the record.

The smooth club pilot completes three clean laps, best 64.976 s, improving the previous candidate by 1.358 s. The actual sector-4 loss to the user's lap 4 falls from about 1.74 s to 0.94 s. Across the lap, the AI remains about 2.37 s slower than the user's best, with the road-edge allowance difference still relevant. See `human-vs-smooth/human-comparison.png` for position, speed and sector traces. A higher line-adherence percentage alone would have missed the old path's unnecessary slowing.

Final repeated runs and their acceptance checks are in `smooth-validation/comparison.json` and `reference-validation.json`. The validation protocol is three independent launches per stock/club setup, three laps each, plus a held-out street setup from the centred grid rather than the usual left-hand solo start. All these use ordinary compact-car physics and actual ordered checkpoints. Boundary observations are 10 Hz; they do not prove continuous clearance between samples.

Final result: **23/23 consolidated checks pass**. The three stock traces are identical to one another, as are the three club traces. Best laps are 91.078 s stock (1.616 s faster than the previous seed) and 64.976 s club (1.358 s faster). The held-out street car records 84.213 s. All seven validation runs finish every lap with zero contacts, recovery, missed gates and sampled body departures. Maximum sampled body offsets are 5.889 m stock and 6.038 m club, against the asphalt half-width of 8 m. Time within ±1 m of the selected path is 99.81% stock and 90.39% club, excluding grid travel. Deterministic repeats are intentional for this no-error benchmark; natural race-form variation remains a separate driver-system requirement.

Frozen build: `outputs/little-road-racing-line-06-review/Little Road Unity.exe` relative to workspace. Assembly SHA256: `9105EF51BE8B3F5A001967D1435B2E2631F4059918430AB5495F3B74DCC72D65`. The original ten-run matrix uses build 04, assembly `F5DCA77CB17EAF91477ED9DB7A5E801EB527550168605FC1F49161AFE7EC114F`.

The first batch harness retained only process IDs and could not recover exit codes after players terminated; its final harness error is preserved. All ten race reports and shutdown logs exist. The harness now keeps the original process objects; subsequent repeated runs record exit code 0. Aborted `candidate-matrix-02` is explicitly marked interrupted at the user's pause request and contributes no acceptance evidence. The earlier script-policy/module startup failures contain no physical runs and are also excluded.

## Playable checkpoint and limits

`Watch Reference Racer.cmd` opens three fullscreen club laps with the smooth path and braking/acceleration overlay. The original Cedar legacy replay and the human recorder remain separate entry points. F11 is implemented; the clickable fullscreen control is the visually verified switching path. The regular desktop adventure is still checkpoint 20, with production races using the existing legacy driver.

The generated resource keeps its candidate label. This establishes a stronger bounded solo benchmark, not a global optimum or completion of phase B for every track and car. The present catalogue tops out at the club setup; a future faster racing loadout still needs explicit calibration, and Bayview/other geometry have not been proved with this new driver. General traffic planning, driver abilities/form, teammate learning and pit strategy remain unimplemented. Physical recovery is the next major AI behaviour in the agreed order, before routine mistakes are enabled. Keep these clear solo controls as regressions when adding it.
