# Development backlog

**Updated 29 September:** the order of work is now set by [doc 45](45-roadmap-and-feature-spec.md), in Phases 0–5. The campaign is at least 12 hours and open-ended; 12 hours is a floor, not a cap.


Updated 19 September 2026. Checked means implemented with the stated evidence, not final polish. [Document 17](17-scope-and-priorities.md) is the current scope and priority order; older progress entries below are historical. The goal remains unfinished.

## Current priorities after the evening review

The user resumed planning on 19 September, asking to define scope before further implementation. See [route investigation and evidence](../review/evening-01/notes.md). This planning pass does not claim any of those defects fixed.

Subsequent goal resumption: [iteration 18](../review/iteration-18-checkpoint-recovery.md) reproduces and corrects a sideways-crossing rejection, adds missed-gate guidance and queues recovery until traffic clears. The separate build passes 129 editor checks and a rendered missed-gate/synthetic Triangle/full-career-race regression with 24 finishers and 100 saved credits. Keep manual finish acceptance open; original-session causation is not proven. [Iteration 19](../review/iteration-19-course-defects.md) removes the stretched grass from inspected route views, corrects the lighthouse lane, improves slope planting and softens distant water. Next: traffic/contact evidence and the larger Bayview/vehicle fidelity gaps.

- [ ] Reproduce and correct the manual player's missing finish/results/payout; inspect checkpoint handling and verify recovery plus exactly-once saved rewards.
- [x] Correct stretched grass triangles across the village/start and first bends; continuous AI laps and distributed captures in both modes, measured intact grass geometry and inspected affected views in iteration 19.
- [x] End the lighthouse driveway outside the carriageway; mesh-vertex clearance and free-275 visual evidence in iteration 19.
- [x] Reduce distant-water noise from driving-camera views; fixed near/far captures and water-motion checks in iteration 19. Final sea/shore art remains below the fidelity target.
- [ ] Finish root/ground transitions and other scenery polish; footprint-based planting improves slope contact but does not establish every tree is visually accepted.

- [ ] Revalidate passing, recovery, collision, driver variance and equipment effects before accepting the current race.
- [ ] Bring Bayview and three distinct master bodies to the existing visual rubric, inspect a continuous lap and measure performance before scaling world/roster production.
- [ ] Build the original-inspired category list and 3D part carousel with comparison, ownership/fitting and DualSense navigation (document 19).
- [ ] Add an opt-in controller diagnostic panel and reusable synthetic input scenarios; test physical USB/Bluetooth separately (document 11). Defer system-wide emulator input injection.
- [ ] Add load-dependent fuel burn, tank upgrades and changing carried-fuel mass to the shared player/AI driving model (document 18).
- [ ] Add tyre wear, pit services, safe lane entry/release and predictive AI strategy; prove short tests before long events.
- [ ] Validate race suspend/resume, pit checkpoint accounting and exactly-once endurance payouts.
- [ ] Deliver and playtest the 12-hour campaign budget, including tough 24-car endurance events, while retaining all earlier ownership/world requirements (document 17).
- [ ] Later design pass: specify distinct driver behaviours, teammate racing progression and separate track familiarity, with the user's ability examples treated as illustrative (document 20).
- [ ] Later design pass: specify a simple equipment-based performance rating and pre-race player/teammate/opponent comparison (document 21).


## Foundation and prototype

- [x] Research the confirmed PS2 reference and record sources.
- [x] Create the project, concept art and design documents.
- [x] Verify PC, Godot 4.7.2, Blender 5.2.2 and model export/import.
- [x] Implement driving, braking/reverse, grip, camera collision and road recovery.
- [x] Build a coastal road/town and import an original editable car.
- [x] Add three residents, delivery states, one-time reward and five collectibles.
- [x] Implement save/load with malformed-data rejection.
- [x] Launch the earlier four-car Windows prototype and inspect 1080p captures.
- [ ] Finish terrain, pavement and resident grounding throughout the world.
- [ ] Human playtest handling, complete delivery, controls and collisions.

## Expanded racing milestone

- [x] Research classes, parts interactions, traffic failure cases and earning progression before changing systems.
- [x] Specify 24 cars, fixed opposition, all-finishers payouts and tough starter progression.
- [x] Implement six equipment slots, twelve purchases, separate ownership/fitting and comparisons.
- [x] Add migration of earlier saves without removing purchased equipment or money.
- [x] Implement 23 physically driven rivals, countdown, three laps, gates, position, payout and retry.
- [ ] Verify every opponent finishes repeated trials; eliminate recurring opening-bend jams.
- [ ] Test slow/stopped obstacles, alternating bends, varied grids and recovery safety. Record stops and recoveries.
- [ ] Verify human-played races and finish/payout edge cases in the standalone build.
- [ ] Balance stock, single upgrades and complete setups against identical fields using repeated trials.
- [ ] Test the 30–60 minute earning loop and alternative adventure income.
- [ ] Expand results presentation and track/class availability without implying unbuilt tracks are playable.
- [ ] Author Quarry Link, surface-specific setups and its top-12 unlock.
- [ ] Add further tracks/classes while preserving earned licences and replayable earlier events.

## Ownership beyond the first six slots

- [ ] Saved named setups, eligibility and visible fitted parts.
- [ ] Suspension and differential models before selling related parts.
- [ ] Wet/snow surfaces and specialist tyres with measurable trade-offs.
- [ ] Aero, energy/endurance and boost with suitable events.
- [ ] Water/air/clearance equipment and useful exploration routes.
- [ ] Night lighting, navigation tools and sponsorship.
- [ ] Original bodies, wheels, paint/liveries, horns and instrument themes.
- [ ] Resident service, garage rewards and lasting world progression.

The expanded ownership specification is the source of truth. Later staging does not remove those families from the intended game.

## Controls, art and delivery

- [ ] Build the varied road, sports, prototype and open-wheel body roster, with class/track-specific rival mixes (document 13).
- [ ] Match every body, wheel animation and visible attachment to its collision envelope; inspect contacts against cars and the environment.
- [ ] Implement angle/relative-speed crash response and paired clean-versus-impact timing tests; avoid severe repeated-contact penalties.

- [x] Add an open workshop, facade detail, vegetation and a three-arch bridge; export and inspect eight views. See review/iteration-05.md for remaining visual gaps and bridge-traffic observations.
- [ ] Verify PS5 DualSense on Windows via USB and Bluetooth using physical hardware.
- [ ] Complete controller-only menus, correct prompts, remapping, calibration and reconnect handling.
- [ ] Add engine/surface audio, ambience, feedback and volume controls.
- [ ] Improve car silhouette, architecture, landscape, road edges, coastal/bridge composition and lighting toward concepts.
- [ ] Review village, garage, coast, race and close-up views against the unchanged visual rubric.
- [ ] Profile 1080p driving and a visible 24-car pack on the user's PC.
- [ ] Export validated milestones, package sources/evidence and state limitations clearly.

The 10–15 minute target now applies to the introduction. Developing a winning car is a longer progression loop.

## Evening checkpoint and next priority

Iterations 15–16 add the cloud sky, aligned sunlight, reflection probes, branched foliage, original coastal outcrops, photographic ground materials and grass clumps. Actual captures have driven repeated geometry and material corrections; this is still below the visual reference. Next environment priorities: less repetitive rock silhouettes and surface joins, believable water/shoreline, detailed masonry, richer village planting, road wear and vegetation distance management. Profile the current vegetation before expanding its density. The car masters and all unchecked gameplay/controller requirements remain in scope. See the iteration 15 and 16 reviews for evidence, rejected visual passes and remaining gaps.

Iteration 13 adds the Unity coast: sculpted terrain with matching collision, a three-arch inlet bridge, continuous supported road/shoulders, stone walls, revised trees and a distant headland. The expanded review samples road/shoulder support and captures bridge approaches/profiles. Runtime review fixed invalid crown vertices, bridge face/infill problems and signs showing through hills. All 24 cars completed the tested races, but run-wide events still caused recoveries; this is not reliable recovery-free racing or accepted balance. See review/iteration-13-unity-coast.md. Next priorities include physical rejoining/corner-exit traffic, finer environment art, manual route acceptance and the other unchecked requirements below.

Iteration 12 replaces the Unity box village with original open-workshop and shop geometry, tiled roofs, facade details and forecourt props. Runtime clearance checks caught and corrected a lighthouse intrusion and lamp placement across the resident approach. Physical synthetic-input garage entry/reverse-exit passes with no contact or recovery. The detailed evidence and final integration status are in review/iteration-12-unity-village.md. Landscape depth, bridge/road-edge composition and richer coastal scenery are the next major visual gap; collision/recovery, body masters, audio, wider progression and hardware acceptance remain required.

Iteration 11 reconnects the Unity adventure: three residents, delivery, five finds, seven-slot garage, purchasing/fitting, paint, a paid 24-car race and atomic versioned saving now exist in the normal launch. An exported full integration loop and separate-process save reload passed; synthetic gamepad tests exposed and then verified a menu handoff fix. The test relocates between objectives, so a complete manually driven adventure and physical DualSense acceptance remain open. A simple lighthouse and pit-village placement support the loop, but the coastal-world art is still provisional. See review/iteration-11-unity-adventure.md. Next priorities retain contact/recovery fixes and the three-master car work, and now include a continuous route playtest, fuller town/coast rebuilding and audio rather than another disconnected race-only milestone.

Latest Unity checkpoint, iteration 10: a rebuilt compact study now runs in the field with three detail levels. Runtime and gallery review fixed double wheel animation, reduced-model glazing changes and roof-normal defects. The compact is still short of the concept; coupe/open-wheel masters, decals and the body generator remain required. New-body races completed all cars, but seed 1721 exposed one off-road recovery and up to 12.37cm contact penetration. The zero-reset outcomes from iteration 09 do not generalise to this field. Next: inspect that contact/recovery trace, refine the compact cabin/lamps/tyres, create genuinely distinct coupe/open-wheel masters, and resume adventure/world migration. Full controller/human tests and the remaining race matrix stay open. See review/iteration-10-compact-master.md.

17 September Unity migration: the user installed Unity and resumed the full goal. A separate URP project, C# driving/circuit/traffic trial, editor build checks, reporting and original FBX exports have been authored. Template initialization and Blender exports succeeded. Compilation, Windows export and runtime evidence for the new scripts are pending: automatic approval review rejected the build launches because its review model was at capacity. See document 16. All unchecked adventure, progression, visual and racing requirements remain in force; the Unity trial is not feature parity.

Catalogue follow-up: the complete current seven-slot / 21-part catalogue now exists as Unity JSON with stable IDs; a direct data comparison passed for 121 original fields, including all fourteen purchases. Test loadouts reference it through the authored C# catalogue. This does not yet establish Unity runtime, ownership UI, rewards or persistence. Build approval remained unavailable on the second resumed goal turn.

Third migration turn: approval recovered. Windows build 03 compiles and passes 62 editor checks. Stock/club physical solo laps completed in 37.63s/28.12s with matched seed, no contacts or resets. The revised three-lap 24-car trial finished all cars with zero resets; stock placed 20th. Several rivals left the road substantially, so traffic and balance gates remain open. See review/iteration-08-unity-migration.md for captures, hashes and failures. Next: excursion and missed-gate rejoining diagnosis, controlled behind-starting overtakes, player/camera/UI checks, then the three polished master bodies and adventure feature migration. The approval-service blocker is resolved.

17 September: four body studies and two model/world review passes are saved. Seven slots and fourteen purchases now include turbo upgrades; production handling models power, drag and a shared grip budget. 38 functional plus nine power/grip/save checks pass. See review/iteration-07-evening.md and docs/14-power-grip-and-evening-handoff.md.

Next: wider race layout, off-course rejoining, passing freedom and gradual acceleration before balance tuning. Profile the full field, including repeated road sampling/traffic planning and convex contact cost. Previous narrow-course matrices must be rerun after these changes. The user requested an evening stopping point; the wider project remains unfinished.
