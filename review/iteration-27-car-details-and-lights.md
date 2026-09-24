# Car details and working rear lights — 19 September 2026

The preceding goal turn made progress and delivered checkpoint 14 with verified coastal relief/masonry changes. This turn returns to car fidelity and incorporates the user's request for red brake lights and white reversing lights. The full game/concept objective remains incomplete.

## Changes under review

- The compact recipe advances to revision 2, with a projected hatch shut line, fuel-door outline, rear bumper insert, passive reflectors, a repositioned rear wiper and a central brake lens. Its previous source/recipe and generator are preserved in `unity-20`.
- Brake and reverse lenses have separate material identities. The older road-body exports previously shared reversing lenses with headlights; their rear lamp faces now receive a separate material during export without changing the preserved Blender sources. The formula prototype had no rear cluster, so its Unity export receives a small original housing, red brake lens and white reverse lens inside the existing rear-wing envelope.
- `VehicleLights` uses each car's own materials, shared across that car's LODs. It illuminates red during forward braking, white once the existing combined brake/reverse command engages reverse, and both when forward input brakes backwards motion. Coasting backwards retains white. Blocked controls and race countdown clear active lamps. These follow the existing controller; no driving-force or AI-speed logic changes are introduced.
- Emission is confined to the lenses: headlights and passive reflectors do not light up in reverse. This pass does not add light beams, headlight controls or projected road illumination.
- A retained emissive material reference keeps the required URP shader variant in the Windows build. State changes update the materials without allocating per-frame copies.

## Model review

The [first Blender rear render](unity-20/blender-01/compact-v1-rear-quarter.png) shows the added tailgate detail but also exposes headlamps standing away from the curved nose. Moving them inward in [revision 02](unity-20/blender-02/compact-v1-front-quarter.png) makes the body obscure part of the lenses. Revision 03 adds real circular apertures and seats the wing indicators against the body surface. These are Blender renders of actual geometry, not proof of the Unity appearance or gameplay.

The hatch seam uses a sampled polyline rather than a heavily subdivided Bezier curve, preserving its surface fit with less geometry. The major body proportions, broad highlight quality, wheel/trim treatment and the other two required master bodies still need substantial work against the vehicle concept. Detail additions do not establish the master-car visual gate.

## Verification in progress

Build 01 passes 167 editor checks, including separate rear lenses on all five imported bodies, material ownership, reverse geometry restricted to the rear, command-state cases and existing LOD/window/wheel checks. Actual controller input, Unity captures and the final revised model remain under review. Checkpoint 14 is still the normal launcher.

## Budget-directed fidelity pass

The user redirected the remaining weekly budget toward car fidelity. The additional work concentrates on the compact master rather than adding gameplay systems or multiplying unfinished body variants.

- Recipe revision 3 lowers and gently widens the cabin as one assembly, including glazing, seals and interior. Window surrounds now have a shallow pressed-metal crown instead of flat strips.
- An uncut shell supplies transferred normals after boolean cuts, reducing false dents in the body reflections. The nose has enough volume to surround both round headlamp apertures; lamp seating was adjusted after inspecting the overly deep recess in Blender revision 05.
- Five tapered alloy spokes, circumferential tyre grooves, disc ventilation detail and fixed calipers replace simpler wheel details. The stationary calipers are separate from rotating wheels.
- Smaller rear lamp housings, moulded lens ribs, a parked front wiper, a slatted grille and surface-following bonnet seams add readable detail. Revision 04 partially hid the reverse lenses in the shell; revision 05 restores their visibility.
- Final source/export is `blender-07`: 83,576 / 40,638 / 19,165 triangles at LOD0/1/2. A Unity close-up exposed a hatch seam crossing the smaller lamp cluster; revision 07 moves the seam inside the lamps and uses three broader lens ribs. This is more geometry than the earlier study; it has not passed a frame-time benchmark. The roof/pillar junctions, panel highlights, tyre materials and all other body families still need work. No master-body visual acceptance is claimed.

Build 02's controller review passes all seven actual input/AI-command states. The first harness run captured before queued input and LateUpdate had settled; its failure is retained in `lights-01`. The corrected harness waits for the command received and one presentation frame, then asserts the lamp state without waiting for the desired lamp result. Build 02 also passes a full 24-car field, static world clearance and connected free-lap/race/result/saved-payout checks. Its physical garage exit fails; this was confirmed when reviewing subsequent runs. These precede the final fidelity geometry and are not substituted for final-build evidence.

Build 03 failed because the output argument was a directory instead of an executable path; the corrected build 04 succeeds with 167 editor checks. Its field finishes all 24 cars with zero recoveries/missed gates; stock is 23rd in 110.612 seconds. Maximum sampled box overlap is 0.3777 mm over 13,275 physics poses, tagged scenery overlap is zero and queries do not saturate. Its connected route completes a free lap and three-lap career race, opens the actual menu and saves 100 credits from zero. These are regression observations, not final balance/performance acceptance.

Build 05 carries the final cosmetic seam/rib cleanup. Its Unity close-ups show complete round headlights, visible brake/reverse lenses and improved window surrounds. All four audited body families pass 132 sampled wheel poses with zero horizontal envelope escape. The low LOD visibly simplifies tyres/windows when forced in a close-up; transition/performance acceptance remains open.

Physical garage exits in builds 02, 04 and 05 stop consistently around z=-98.557 rather than reaching -98, despite entering successfully and recording no counted contacts or recoveries. The normal export remains checkpoint 14 while a focused contact diagnostic identifies the obstruction; this failure is not cleared by the passing static world samples or race.

The build 06 diagnostic identifies horizontal contacts against `Generous limestone runoff` at y=0.011. Exit velocity reaches effectively zero at the same position: it is a real raised-collider edge, not merely a short test timeout. The usual race contact counter excludes ground, explaining its zero. Build 07 replaces the separate raised road/shoulder colliders with a continuous support mesh: 0 m at the outer shoulder, 0.011 m inside the shoulder and 0.025 m at the asphalt, joined by gentle slopes. Road centre height, visible scenery, tyre/power/steering tuning and garage test thresholds remain unchanged. The diagnostic is confined to world-review mode.

## Final build 07 verification and handoff

- Build succeeds with 167 editor checks. Gameplay assembly SHA-256: `69C95BE9DC69AD3331D5E9341C7059CC77F26D3CA165A95E58D760C67645156F`.
- `lights-07/lights.json`: all seven actual input/AI-command lamp states pass. Actual Unity `gallery-05` images use the final unchanged car meshes; `lights-07` and `world-07` show them with the final road support.
- `world-07`: 171 road, 11 garage, 27 forecourt and 1,710 support samples pass. Garage entry and reverse exit both pass, with zero counted contacts/recoveries and 0.0366 m maximum lateral error. Exit reaches z=-97.984 with positive outward velocity. No timeout, endpoint or collision assertion was relaxed.
- `field-07.json` and its validation: all 24 finish three laps; ordered gates pass. Stock is 21st in 110.553 s. Maximum car-box overlap is 0.4693 mm across 13,366 physics poses, tagged scenery overlap is zero and queries do not saturate. These are sampled collider observations, not universal no-clipping or final balance proof.
- `route-07`: continuous free lap 683.60 m / 42.70 s, zero contacts/recoveries, maximum offset 8.671 m within supported runoff. The connected three-lap career race opens the actual result menu and saves 100 credits from zero, race/settlement serial 1.
- Final tests are terminal and use isolated saves. The final model is an improvement, still below the concept benchmark. Preserve glass, roof/pillar transitions and smoother broad body highlights as the next compact art priorities, then apply the construction to the other master bodies. No further art pass is started here.

The user requested pausing after this checkpoint because the weekly allowance was at 1%. Package and promotion verification are recorded in `unity-20/package-manifest.json` and `promotion.json`; pause the persistent goal after promotion. Resume only when requested.

## User playtest feedback after delivery

The user likes the appearance and requests two changes for next time:

1. Start the player at the back of the race grid for more challenge (last among the actual entrants).
2. Investigate an invisible ramp on the final straight before start/finish that launches cars high into the air. This is reported, not yet reproduced or diagnosed. Earlier passing completion/contact checks do not establish correct vertical motion; add explicit airborne/vertical-motion observations to the next route review. Inspect the recent continuous support mesh as one possible cause, without assuming it is responsible.

Fix the launch defect before the next fidelity pass, then verify rear-grid starts and valid checkpoint/lap counting. No game code or playable build is changed in response to these notes; the goal remains paused.
