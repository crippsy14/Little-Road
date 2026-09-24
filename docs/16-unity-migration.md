# Unity migration and verification record

17 September 2026. User approved moving toward Unity, installed it, and resumed the full Little Road goal. Three migration goal turns have now produced the separate project, preserved catalogue, a compiled Windows trial and initial physical race evidence. The full objective is unchanged.

## Authoritative state

Latest: [iteration 09](../review/iteration-09-traffic-and-passing.md) supersedes the initial traffic findings below. Controlled two-/three-car passes from behind are verified. Two final three-lap 24-car seeds completed all cars with no resets/missed gates and no off-road centre-position samples at 1Hz. Stock placed 24th/23rd. Collision-shape penetration still reached 6.3cm; no-clipping is unproven. Latest tested package: `outputs/little-road-unity-trial-02.zip`; iteration 08's ZIP is preserved separately.

- Installed editor: `C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe`.
- New project: `outputs/little-road/unity`, separate from `game` (the Godot checkpoint).
- Base: installed Universal 3D template 17.2.1, with URP 17.6.0 and Input System 1.19.0 from its manifest. Version is pinned to the installed editor, not represented as the earlier recommended 6.3 LTS.
- The editor initialized the template, resolved packages and exited successfully. Evidence: `review/unity-01/import.log`.
- `source-art/export_unity.py` exported four original Blender bodies to `unity/Assets/Resources/Cars/*.fbx`, without changing the `.blend` sources. Editor checks verify metre-scale bounds for all four. Runtime captures exposed and then verified fixes for the reversed FBX nose and culled single-polygon glazing. Wheel animation axes and final contact fidelity still need close review.
- **The new C# code compiles and a Windows development trial runs.** The approval service recovered on the third migration turn. Build 01 identified a missing image-conversion module; that manifest dependency was added. Builds 02 and 03 passed 62 editor checks and exported successfully. The previous three approval failures were service-capacity errors, now resolved.
- Initial race 01 finished 20/24 within 90 seconds and left stock stalled. Build 03 corrected excess contact friction, visual orientation/glazing, per-body collision bounds and post-process shader stripping. Matched-seed actual solo runs completed at 37.63s stock and 28.12s club, without contacts or recoveries. The revised 24-car field completed three laps, all 24 finished, with zero recovery resets; stock placed 20th at 108.66s. Several rivals made large excursions; this is not accepted balance or reliable traffic validation. See [the review](../review/iteration-08-unity-migration.md).
- Existing Godot source, Windows export and packaged checkpoint remain available. The Unity trial does not supersede those tested deliverables.

## Authored migration trial

`DriveModel.cs` ports the arcade engine-power/torque, gearing ceiling, drag, mass and shared cornering/longitudinal grip relationships. Stock, street and club test configurations preserve those relationships. They are development presets, not the complete purchase/fitting UI or save system.

The full current catalogue is transferred to `Assets/Resources/Data/parts.json`: 21 parts (seven stock items and fourteen purchases), seven slots, stable IDs, prices, names, descriptions and tuning values. `EquipmentCatalog.cs` loads this data, validates IDs/slot membership, composes the test setups and retains the class reward calculation for the later progression port. Body art is independent of equipment. The catalogue code compiles, all 21 single-part loadouts pass editor validity checks, and the runtime presets drive the cars. Rewards are not yet connected to a persistent wallet.

`tools/check-unity-catalogue.ps1` was executed successfully on the second migration goal turn. It compares every ported field directly with the preserved Godot catalogue, checks IDs, count, stock slots, slot order and purchase count. All 121 original fields match; source/target SHA256 and scope are recorded in `review/unity-01/catalogue-port-check.json`. This is data-port evidence only, not evidence that C# parsing, purchase/fitting, saving or vehicle behaviour works in Unity.

`RoadCar.cs` applies forward and lateral forces to a dynamic Rigidbody and uses grip-limited yaw. It preserves physical collision impulses. It includes keyboard and current-gamepad input, seeded gradual driver variation, speed planning through upcoming bends, traffic-dependent lateral target selection, ordered lap gates and a recovery attempt behind the last safe point with an occupancy check. Initial runs demonstrate finishing and equipment effects but not the full required passing, recovery and contact tests. Collision boxes now follow each imported body's local render bounds, widened to include the steering envelope; these conservative boxes remain provisional. Use validated per-body hull/compound geometry before claiming final collision fidelity. Contact friction is zero/minimum because custom tyre/rolling forces already model ground resistance.

`Circuit.cs` constructs a new authored closed spline and road mesh, **16 metres wide**, with different corner radii, straights and runoff. Vehicles are not positioned along the spline each frame: they steer toward look-ahead targets using the driving model. Actual racing lines, attainable corner speeds, traffic behaviour and off-course recovery must be measured.

`TrialWorld.cs` provides a separate coastal test setting, broad continuous ground, sea, kerbs, simple pit village, vegetation and URP lighting. This is prototype scenery. The original adventure map, residents, delivery, collectibles, garage, shops and exploration routes have not been ported. It is not a replacement for the coastal-world visual target.

`RaceTrial.cs` starts 24 cars by default, supports manual or automatic player driving, and records ordered finishes, lap progress, contacts, recovery counts, setup, seed, frame times and hardware. Fixed equipment is assigned independently of player pace; no rubber-banding or fabricated finish times are used. The first trial runs three laps, with test arguments for field size and duration. Reports must be inspected for incomplete races rather than treating an orderly process exit as a passing test.

The trial HUD and controls are provisional. Keyboard: WASD/arrows, R recovery, Tab AI toggle, Escape quit. Gamepad: left stick, right trigger throttle, left trigger brake/reverse, north button recovery, select/share AI toggle. The connected device is read dynamically. Real DualSense USB/Bluetooth, menus, disconnect safety, remapping, dead-zone calibration and prompts remain unverified or unimplemented.

## Build and evidence entry points

The editor menu `Little Road > Build and check Windows trial` calls `MigrationBuild.Build`. It creates `Assets/Scenes/CoastalTrial.unity`, assigns the template's PC URP asset, runs model-level power and track-projection checks, checks model bounds, records `review/unity-01/editor-checks.json`, and requests a Windows build under `outputs/little-road-unity-windows`. Use `--review-export` to preserve a separate experimental export.

As of 21 September, builds default to `BuildOptions.None`. Custom local race telemetry and logs remain available. Unity development builds advertise an editor connection over the network, causing repeated Windows firewall prompts as experimental executable paths change. Request `--development-build` explicitly only when Unity development profiling is needed; the build evidence records this choice. No firewall changes or exclusions are required for ordinary offline reviews. Older frozen exports retain their original build mode.

Equivalent local command, from PowerShell (normal Unity licensing/package caches are required):

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe' -batchmode -quit -projectPath 'C:\Users\cripp\Documents\Codex\2026-09-16\hey\outputs\little-road\unity' -executeMethod MigrationBuild.Build -logFile 'C:\Users\cripp\Documents\Codex\2026-09-16\hey\outputs\little-road\review\unity-01\build-01.log'
```

After a successful build, automated runs accept:

```text
--trial-auto --trial-cars 24 --trial-laps 3 --trial-duration 240
--trial-seed 1709 --trial-setup stock
--trial-report <absolute JSON path> --trial-captures <absolute folder>
```

Test setup values are `stock`, `street`, `club`. Manual camera captures render at 1920×1080 and intentionally exclude the temporary HUD. Inspect a separate live/full-window capture to assess the interface. A reported frame-time average from an automated run is not sufficient to claim a visible 1080p/60fps target has been met. Capture loading, startup and screenshot overhead separately from steady play.

## Required migration gates

1. Compile the new code, export Windows and inspect model sizes, nose direction, wheel axes, materials, ground contact, shadows and the complete circuit.
2. Verify actual Rigidbody acceleration, braking, coasting, corner exits and grip effects against equipment configuration. The fast `DriveModel` checks alone do not prove vehicle motion.
3. Complete solo repeated-seed/configuration trials, two-/three-car overtakes, deliberate impacts and rejoining, then repeated 24-car races. Inspect finish distributions, lap validity, stops, recoveries, contact depth and unexpected traffic losses. Improve track/AI from evidence before balancing rewards.
4. Profile visible 24-car driving and check keyboard and physical DualSense operation. Do not assume package support proves hardware testing.
5. Bring compact, sports coupe and open-wheel master bodies to the visual target, establish materials/UVs/modules, and expand according to document 15. The four imported bodies are preserved prototypes, not the promised three polished masters.
6. Port adventure, inventory/purchases/fitting, all-finishers rewards, unlocks and versioned persistence. Preserve owned equipment, money and completed activities through a tested save-import path, or keep saves clearly separate until that exists. Do not overwrite the Godot save.
7. Restore and improve the complete coastal slice, audio and controller menus; run standalone functional and visual comparisons against both concepts. Continue toward the broader progression, track and ~100-body requirements in the specifications.

The goal remains active. Compilation, initial equipment effects, controlled passing and two final full-field runs are proven, but none of the broad gates is fully complete. Next: the three higher-fidelity master bodies and reviewed collisions, while retaining physical-input/camera/UI, deliberate-impact and recovery-safety checks. Adventure feature parity and world rebuilding remain required. The tooling blocker is resolved.

## Compact study checkpoint — iteration 10

A new original compact study replaces the player's body and one third of the trial field, with editable Blender source and three detail levels. Runtime review found and fixed double wheel animation; imported-model checks now exercise spin/steering, and lower LOD generation preserves glazing. The close-view model remains above the initial triangle target. New coupe/open-wheel masters, UV/livery production and the full body catalogue are unfinished.

The new geometry requires fresh traffic review: one three-lap seed finished all cars without resets; a second finished all cars but exposed one off-road recovery and up to 12.37cm contact penetration. No-clipping and traffic stability remain open. See [iteration 10](../review/iteration-10-compact-master.md) for exact builds, visuals and limitations. Adventure migration remains required.

## Connected adventure — iteration 11

The ordinary launch now starts in free driving beside Mara's garage. A separate Unity save connects the delivery, five roadside finds, all seven equipment slots/fourteen purchases, fitting, recolouring and Rowan's 24-car C-class race. Race rewards settle once per current event serial and persist with owned equipment and quest state. Explicit developer race flags preserve the prior race-only test entry point.

Build 05 passed 102 editor checks and an exported integration run through delivery, two finds, engine purchase/fitting, a real 24-car three-lap race, payout, return and disk reload. A separate process successfully reloaded the final save. Synthetic controller checks also verified menu navigation/confirmation, menu throttle blocking and physical free-driving acceleration/steering after fixing a dialogue handoff issue. See [the review](../review/iteration-11-unity-adventure.md) for the final artifact and evidence. This is a partial feature-parity milestone: no Godot save import, audio, full manual/controller route acceptance or completed coastal-world art is claimed.

The earlier migration descriptions above record historical checkpoints; their statements that these systems have not been ported are superseded by this section. Other unimplemented requirements and visual/traffic gates remain open.

## Reference documentation

The [iteration 17 water checkpoint](../review/iteration-17-water-and-review-pause.md) replaces the flat sea with moving swells/ripples, depth-based colour/refraction, sky reflections and intersection foam. Build 02 passes 112 editor checks and the world/garage checks; paired game captures verify rendered motion. Archive adventure-10 preserves it. No fresh full-field or visible performance acceptance is claimed. The user requested a pause after water for a run-through: further development waits for their instruction.

The [iteration 16 landscape pass](../review/iteration-16-cliffs-and-ground.md) adds original rock masses, CC0 rock/grass surface maps, finer terrain and low ground cover. Four rendered passes corrected tower-like cliffs, perched rock bases, texture scale and distant colour. Final build 04 passed 111 editor checks, the world/garage checks and a 24-car regression with zero resets/missed gates; the pose audit still measured 2.17cm car overlap. Archive adventure-09 preserves it, with verified file hashes in `review/unity-09/package-manifest.json`. The landscape, water, car art and performance remain below acceptance; the review lists the visible shortcomings.

The [iteration 15 visual pass](../review/iteration-15-foliage-and-light.md) adds original branched foliage, scene reflection probes, longer detailed shadows and a credited CC0 Poly Haven cloud sky with aligned sunlight. Its final world/garage checks and 24-car regression passed their scoped assertions, while real car overlap and the much larger visual gaps remain documented. Archive adventure-08 preserves this pass; it does not constitute visual acceptance against the concept.

The [iteration 14 steering checkpoint](../review/iteration-14-unity-traffic.md) uses physical torque steering, geometric pursuit, earlier edge-risk braking and 120Hz simulation. The two ordinary background fields, visible field, and final connected adventure field all finished without resets or missed gates in their recorded cases. Two-/three-car passing and the connected save/garage loop passed. Real car overlap remains measurable (3.6cm in the final adventure race); physical obstruction recovery and the wider balance matrix remain open. The tested archive is `outputs/little-road-unity-adventure-07.zip`, with file hashes in `review/unity-07/package-manifest.json`. Later visual work changes the mutable export; the archive preserves this checkpoint.

The subsequent [coastal checkpoint](../review/iteration-13-unity-coast.md) replaces the flat island with an authored terrain mesh, a curved inlet bridge, coastal walls and revised planting. The world review now includes two bridge views and 1,710 road/shoulder support ray samples. These complement the garage and forecourt checks; they do not establish continuous no-clipping or correct driving on every off-road slope. Race completion coexists with unresolved run-wide/recovery events, recorded explicitly in the review.

The preceding village checkpoint is [iteration 12](../review/iteration-12-unity-village.md): original modular village architecture, an open workshop, adventure-specific road dressing, clearance checks and a synthetic-input garage drive. Use `--world-review --adventure-save <isolated path> --trial-captures <folder>` to reproduce the world captures and geometry/access evidence. This review mode moves the car between camera poses; its separate entry/reverse-exit test drives through the real Rigidbody controller. It does not prove continuous route playability or physical hardware support.

- [Unity command-line builds](https://docs.unity3d.com/6000.0/Documentation/Manual/build-command-line.html).
- [Input System 1.19 gamepad support](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Gamepad.html).
- [WaitForEndOfFrame](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/WaitForEndOfFrame.html): the capture routine avoids waiting on this yield instruction in batch mode.
