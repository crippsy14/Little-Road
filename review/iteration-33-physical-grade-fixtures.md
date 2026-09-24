# Physical hill, combined course and gravity controls

21 September 2026. Implements the next equipment-test phase from [doc 24](../docs/24-equipment-performance-test-courses.md). Validated and delivered as desktop checkpoint 20; the prior checkpoint 19 is preserved.

## Implemented behaviour

The actual car now traverses an authored 35.2 m hill with 16% sustained grades, 60 m cosine transitions, a 100 m summit plateau and a matching descent. The combined course adds a longer approach, a U-turn and exit. Three additional coasting controls start at 12 m/s and cover 20 m on flat/uphill/downhill sections of the same physical support system. Visible stripes and markers are the real ordered timing gates, retained following the user's positive feedback.

Elevated routes enable `RoadGrounding`: four nonallocating rays identify tagged physical road support, derive a local ground frame and request body alignment through torque. Drive/lateral forces use that frame; pitch/roll are no longer frozen on these routes. It does not prescribe body position, rotation or velocity. PhysX gravity supplies the slope effect once; `DriveModel.Forces` receives zero extra slope. Flat Bayview retains its existing constraints and ground path.

`Circuit` distinguishes horizontal turn curvature from grade, and preserves full route arc length. Fixtures spawn relative to measured support and record route height, slope sine, local clearance, alignment, grounded steps, coasting acceleration and surveyed profiles. Free-roam below-ground recovery uses the local route datum on elevated tracks. The aggregate car collider remains the contact model; this is not a detailed wheel-suspension simulation or evidence for banked roads and multi-car hill racing.

The first geometry check caught numerical false corner curvature in a geometrically straight hill. Horizontal curvature now uses cross/dot `atan2` on projected tangents, avoiding `acos` roundoff near zero. This correction leaves the flat route's existing curvature calculation unchanged.

## A real defect found by the new fixture

Pilot build 02 completed the hill/combined/coast cases, but the club car left the downhill section at nearly 39.9 m/s and could retain that excess speed on the flat. The previous gear limiter clamped net acceleration to zero, effectively letting the engine cancel drag above its gear ceiling. Build 03 instead cuts propulsion above that ceiling while retaining drag and gravity. A following pilot finishes the club hill run near 33.95 m/s, within its 34 m/s gearing range. Downhill momentum remains physical; no velocity is clamped.

The pilot's measured coasting acceleration residuals were approximately -0.0026 to +0.0006 m/s² relative to one gravity component plus drag, far below the ~1.55 m/s² error expected from double-counting this slope. The final matrix repeats the controls with more equipment configurations.

## Build and evidence locations

- Final source build: [build-03.log](unity-26/build-03.log), 191 editor/import/geometry checks. Assembly-CSharp SHA256: `9A9AB5B48941669774928F1AC6375DF0F28E045FE9E982E5C6813FD352545773`. Build identity is also present in runtime module metadata.
- Initial failure and diagnostic evidence: `unity-26/build-01.log`, `pilot-01` and `pilot-02`. Preserve them rather than presenting only successful final tests.
- Final intended grade matrix: `unity-26/grades-final`, five setups × five courses × three repeats (75 runs). `tools/analyze-grade-trials.py` evaluates physical support, gravity residuals, repeatability and conditional climb effects; its report is authoritative when complete.
- Final flat regression: `unity-26/flat-final`, twelve setups × three courses × three repeats (108 runs). The gear-limiter correction may cause small intentional differences from earlier gear-limited steady-speed values; do not claim all old floating-point results are identical.
- Normal Bayview field: `unity-26/field-final.json` and `field-final/vertical-race.json`. All 24 finish three ordered laps, with no recoveries or missed gates; maximum sampled car-box overlap is 0.8774 mm and tagged scenery overlap is zero. Inspect the saved vertical result before claiming that audit passes.

## Milestone review and next priority

This adds real grade response and useful equipment evidence, not finished campaign content. Proving-ground visuals remain plain and readable; the crest capture has been inspected, but no new moving-world visual acceptance is claimed. Progression measurements gain actual climbing/coasting cases; rewards/unlocks and exploration are unchanged. Human controller/manual acceptance, normal rendering performance, banked-road behaviour and hill traffic remain open. The physical support extension is route-specific and reusable without replacing the shared car physics.

The user has explicitly prioritised **AI work after this test pass**. Use the exact [racing AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>) and the [Tsukuba-influenced original technical-circuit brief](../docs/25-technical-ai-test-circuit.md). Start with the legacy input-policy seam and competent solo racer, then physical recovery, traffic and differentiated abilities as that plan specifies. The tyre browser remains in scope but moves behind this AI milestone. Teammate progression/familiarity persistence and endurance integration remain later phases. Do not continue polishing a single proving-ground bend or hill indefinitely.

## Final matrix results

All 75 grade/coasting runs complete validly, with [147/147 analysis checks](unity-26/grades-final/grade-report.md) passing. All 108 flat runs complete validly, with [129/129 checks](unity-26/flat-final/report.md) passing. The largest measured local support clearance is 4.48 mm and alignment error 0.522 degrees; no graded run has an unsupported sampled physics step. Mean coasting-acceleration residuals range from -0.00254 to +0.00051 m/s². Those are observed fixture results, not guarantees for all future track shapes.

The stock climb sector takes 13.275 seconds; workshop engine 11.312 seconds; light chassis 13.056 seconds; full club 8.320 seconds. Long gearing alone is slightly slower on the climb but permits more downhill speed, illustrating why a total-time result needs sector context. The [four-panel grade graph](unity-26/grades-final/grade-comparison.png) has been visually inspected.

The saved 24-car Bayview vertical audit passes: zero airborne steps, maximum upward speed 0.0161 m/s and maximum collider-bottom height 0.02501 m. [Field validation](unity-26/field-final.validation.json) records its bounded checks. Physical DualSense/manual acceptance and actual normal-window performance remain open.

Normal-time stock and club hill runs match the accelerated matrix's elapsed time and exit speed, with no unsupported steps: [timescale comparison](unity-26/timescale-comparison.json). This is physics evidence, not normal-window rendering-performance acceptance.

Checkpoint 20 is packaged and promoted. All 323 archive/delivered files match; the existing desktop shortcut target is verified. Prior checkpoint 19 is preserved in `little-road-unity-windows-before-20`. Archive SHA256: `6D8305BD33C7F2E56A3E0BBD819F8059DA752F2958F01D513F1C961DBC212B91`. See [package manifest](unity-26/package-manifest.json), [promotion](unity-26/promotion.json) and [delivery verification](unity-26/delivery-verification.json). The broad campaign goal remains active.
