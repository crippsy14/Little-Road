# Car contact and wheel coverage — 19 September 2026

The user drove the preceding copied review build, reported that it played well, and then closed it. That feedback supports the current handling direction, but does not by itself verify a manual race finish, USB/Bluetooth controller modes, or collision correctness. This work preserves its power/grip/steering tuning and tests a collision-detection change separately.

## Baseline and hypothesis

The repeated seed-1720 field measured a 0.021698 m maximum car-box overlap. The largest event involved cars 3 and 13 around 21.10 seconds. This is a collider-pose measurement, not proof that visible car triangles overlap by the same depth.

[Unity 6.6's speculative CCD documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/speculative-ccd.html) describes support for both linear and rotational movement, with a caveat that false contacts can divert objects or make them jump on tessellated surfaces. That suggests a comparison for rotating cars, not an assumption that this mode is universally better. The current sweep mode remains available for an explicit matched baseline.

## Actual model envelope review

[Imported-geometry evidence](unity-15/envelope-01.json) checks actual mesh vertices in 132 steering/spin combinations for each of the compact, coupe, utility and formula bodies. This includes all runtime compact LOD meshes. Steering spans -25 to +25 degrees in 5-degree increments and wheel spin spans a full revolution in 30-degree increments.

No sampled body/wheel geometry escapes the existing collision box horizontally. The compact has at least 0.0200 m side margin in these poses; the other three bodies have approximately 0.0302 m. The smallest front/back margin is approximately 0.0075 m. Therefore there is no evidence here to justify indiscriminately enlarging the cars' boxes. The test covers sampled animation poses, not continuous triangle intersections between moving cars, environment penetration or an entire future 100-body roster.

## Matched 24-car comparison

Build 01 passes 129 editor checks. Assembly SHA-256: `202DE0C472C5C6974CD4584A471F33B00C68F8525865C251BB315664133124F5`. Both three-lap trials use seed 1720, identical equipment/grid and the experimental coast enabled, with no handling-tuning changes. They are isolated background diagnostics, not frame-time benchmarks.

| Evidence | Sweep baseline | Speculative candidate |
| --- | ---: | ---: |
| Finishers | 24 / 24 | 24 / 24 |
| Recovery resets / missed gates | 0 / 0 | 0 / 0 |
| Maximum car-box overlap | 0.021698 m | 0.000874 m |
| Maximum tagged scenery overlap | 0 m | 0 m |
| Audited physics steps | 13,225 | 13,401 |
| Stock time / position | 110.195 s / 24th | 109.987 s / 17th |

The [baseline](unity-15/field-01-sweep.validation.json) reproduces the preceding measurement. The [candidate](unity-15/field-01-speculative.validation.json) reduces maximum overlap by about 96%, without recovery or ordered-gate failures. Contact changes alter the race order; the stock car remains below midfield in this seed, but one event cannot establish the full progression/skill balance. The residual 0.87 mm is not zero, and these are box-collider measurements rather than universal visual no-clipping proof.

The matched stock solo runs complete two laps in **73.046 s (sweep) and 73.146 s (speculative)**, a 0.100 s / 0.14% difference, with no contacts/recoveries or tagged scenery overlap. This is narrow evidence against a major pace regression on the current flat circuit, not a guarantee against false contacts on future terrain.

The [two-car](unity-15/passing-two.validation.json) and [three-car](unity-15/passing-three.validation.json) tests pass. They start in single file with stronger equipment behind. The street car physically passes stock; the club car passes both in the three-car case. All retain the expected finishing advantage and complete three laps without recovery. Maximum car-box overlap is about 0.10 mm in each case. These tests observe actual trajectory progress at 1 Hz, not just finish order.

Build 01 selected the candidate with `--speculative-contacts`. Following these results, build 02 makes speculative detection the normal mode and keeps `--sweep-contacts` as an explicit diagnostic baseline. Power, grip, steering response, contact friction, solver settings and collision-box dimensions are unchanged. It was tested separately before the checkpoint promotion below.

## Default-mode verification

Build 02 passes 129 editor checks and repeats the model envelope measurement. Assembly SHA-256: `73F475250BD436F82C316033466A20CCBA6F058F203A027B7CDD3E10464E7354`. These final checks run without a collision-mode flag and without the optional cliff study, covering the ordinary scene.

The [seed-1721 field](unity-15/field-02-1721.validation.json) finishes all 24 cars, with zero recovery resets or missed gates and passing sampled ordered-gate checks. Stock places 20th in 110.487 s. The 13,394-step pose audit reports no saturated queries, zero tagged scenery overlap and **0.0006485 m maximum car overlap** (0.65 mm). The two tested candidate fields therefore remain below 1 mm, substantially below the original 21.7 mm case; untested collisions can still behave differently.

[World clearance](unity-15/world-02/clearance.json) passes 171 road, 11 garage, 27 forecourt and 1,710 support samples. [Synthetic garage entry/reverse](unity-15/world-02/garage-driving.json) passes with no contacts/recoveries and under 0.009 m lateral error. The [continuous free lap](unity-15/route-02/free-lap.json) completes 683.72 m in 42.68 s with no contacts/recoveries. Maximum offset remains 8.67 m, slightly outside the asphalt but on the supported runoff. The [connected career result](unity-15/route-02/race-result.json) opens the actual result menu after three laps and saves 130 credits from zero. The isolated save records best place 18th, race serial one and settled serial one. This is AI-driven integration evidence, not manual-finish or hardware acceptance.

## Playable checkpoint 12

The tested candidate's 320 distributable files were copied and individually hash-compared before promotion to the normal `little-road-unity-windows` export. The prior export was preserved as `little-road-unity-windows-before-12`; normal and playtest saves were not changed by promotion. Existing launchers now open checkpoint 12, with the experimental scan disabled by default.

Frozen archive: `outputs/little-road-unity-adventure-12.zip`, 134,426,452 bytes, 320 files. SHA-256: `27AECE0919637B2D2D17781B2E1C54F92D62180291C5D2CA67B4464B81BEB11E`. [Package manifest](unity-15/package-manifest.json) verifies every archived file against the export. Earlier ZIPs remain immutable. All diagnostics completed.

The goal remains active. Broader impact/time-loss scenarios, interpolated visual collision coverage, manual checkpoint/rejoin and physical DualSense tests remain open. The tested reduction in overlap does not complete the full no-clipping requirement or the visual target. Continue the Bayview/three-master-car fidelity work and retain the full campaign, parts, endurance and future systems scope.

## Scope notes recorded during this work

The requested future [driver personality, teammate growth and track familiarity](../docs/20-driver-personality-and-teammate-progression.md), and [equipment performance rating/pre-race field preview](../docs/21-performance-rating-and-race-preview.md), are now linked from the scope and project index. They are design notes for a later full specification, not implementations or fixed numerical schemas. Current contact/fidelity work retains priority.
