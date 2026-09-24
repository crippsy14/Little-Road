# Iteration 14 — physical steering and traffic

17 September 2026. Work remains in progress. The visual target and complete adventure/parts/roster/controller requirements are unchanged.

## Changes

The AI now steers toward a geometric pursuit target instead of multiplying the heading error by a fixed steering gain. Its target speed accounts for the curve at its chosen lateral offset, and projected lateral motion brings braking forward when the car is heading toward the road edge. A passing choice persists for at least 0.8s. Smooth seeded pace variation remains in the driving inputs; reported times are actual checkpoint crossings.

Manual and AI steering now use a Rigidbody torque servo instead of imposing a rotation with MoveRotation. Contact impulses can resist steering. Checkpoints also require positive movement along the course and a forward-facing car, preventing backwards traversal from earning progress. The road remains 16m wide with its existing shoulder and scenery.

Build 02 increases the physics rate from 60 to 120Hz in racing and adventure driving. This is a simulation-accuracy change, not an engine-power upgrade. Build 03 adds read-only collision diagnostics for runs with a report path. It records overlap between car box colliders and other cars/tagged solid props at the actual Rigidbody poses from completed physics steps. This supplements the retained contact-separation measurements.

The pose audit uses Unity's [ComputePenetration](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.ComputePenetration.html) with explicit positions/rotations. [Contact separation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ContactPoint-separation.html) is a distinct measurement that can fluctuate with simulation corrections. Neither measurement alone proves that all rendered triangles stay separate: wheel steering, interpolated poses and environmental meshes require further inspection. Query saturation and sampled-step counts are recorded so an absent or incomplete audit cannot be mistaken for zero overlap.

## Recorded runs

| Build / case | Result | Recovery / missed gates | Maximum contact depth |
| --- | --- | --- | --- |
| 01 / visible 24-car seed 1720 | 24 finished; stock 23rd, 110.826s | 0 / 0 | 9.85cm |
| 01 / three-car line, seed 11 | Club, street, stock; all three expected passes observed | 0 / 0 | 0.025cm |
| 02 / 24-car seed 1720 | 24 finished; stock 24th, 110.195s | 0 / 0 | 2.17cm |
| 02 / 24-car seed 1709 | 24 finished; stock 24th, 110.320s | 0 / 0 | 4.20cm |

Every listed full-field run passed sampled ordered-gate checks and had no 1Hz centre-position sample outside the 16m road before finishing. This does not establish continuous body containment or collision acceptance. The first visible run was inspected in its actual game window. The other runs were background regressions, with no claim of a representative performance benchmark. The two physics rates produce different contact histories; the table does not isolate a universal percentage improvement.

Builds 01–03 each passed 104 editor checks. Build 03 gameplay assembly SHA256 is `A45525C650263C9BD58ECB40A63675C54921366A901A3E1817EFFD8F30AB10D2`.

Build 03's seed-22 three-car test passed all expected overtakes. Its pose audit sampled 13,020 physics steps without overflowing its query buffer and measured 0.36mm maximum car overlap. The seed-33 two-car test also passed: club overtook stock and finished in 86.293s versus 108.762s, both without recovery. Its 13,053-step audit reported no car overlap. These remain two controlled examples rather than a complete multi-seed matrix.

The connected adventure smoke passed synthetic menu/throttle/steering input, delivery, two finds, purchasing/fitting the Workshop engine, a full 24-car race, payout, return and reload. All 24 finished without resets or missed gates; the equipped player finished 22nd in 109.237s and retained 100 credits. The 13,197-step pose audit measured 3.614cm maximum car overlap, closely matching its 3.615cm contact-depth measurement. This confirms an unresolved physical collision problem rather than dismissing it as a reporting artefact. No tagged-prop overlap or saturated query was observed. A separate process reloaded the isolated profile and rejected a duplicate reward. This smoke relocates between objectives and does not replace a continuous manual adventure test.

`captures-world-03/clearance.json` passed 171 road, 11 garage and 27 forecourt clearance samples plus 1,710 road/shoulder support rays. `garage-driving.json` passed physical entry and reverse exit. The world run captured the existing eight views and logged completion. `Watch 24-Car Race.cmd` opens the requested 24-car window using the same runtime commands as the developer trial.

## Visual comparison

The actual `unity-07/captures-field-02-1720/02-race.png` was compared with `references/concepts/coastal-world.png`. The road accommodates a broad field, but the art remains well below the concept: plain sky, smooth bare hills, sparse repetitive cypresses, basic roadside masonry and angular older coupe/utility bodies dominate this view. The compact's roof, glass, lights and tyres also need refinement. This functional iteration does not raise the existing visual scores of 2–3 to the required four. No new art assets are claimed.

## Outstanding acceptance

Physical reversing/rejoining after being obstructed has not been implemented; the last-resort teleport still exists. Clean ordinary fields do not prove the deliberate stopped-car or impact scenarios. Matched solo/equipment comparisons, stable driver personalities with race-day variation, more seeds, two-car tests and collision time-cost experiments remain required before accepting race balance. Stock finishing near the rear is compatible with the design but is insufficient to establish the desired grind or upgrade curve.

The three polished body masters, roughly 100 curated shapes, decals, expanded parts and track/class progression, richer exploration, audio, physical DualSense tests and visual/performance gates remain open. The previous frozen adventure-06 archive is preserved. This functional checkpoint is packaged as adventure-07; subsequent art source is separate from this compiled checkpoint.
