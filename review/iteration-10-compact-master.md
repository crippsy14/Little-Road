# Iteration 10 — compact study and wheel animation

17 September 2026. Work toward the original vehicle/equipment concept; this is a Unity migration milestone, not completed art or adventure parity.

## Implemented

- Original compact study authored in `source-art/build_compact_master.py`, with editable `source-art/masters/compact_v1.blend` and a versioned recipe. Three geometry passes addressed window seals, roof/header gaps, surface profiles and lamp visibility. Earlier render attempts remain in `unity-03/pass-01` and `pass-02`.
- Rounded lower shell, true wheel-arch cuts, trim, separate glazing, cabin seats/dashboard, panel seams, light lenses and five-spoke wheels. Separate wheel pivots survive FBX export; three detail levels share runtime materials.
- 61,680 / 30,124 / 14,354 triangles across final LOD0/1/2. Hero mesh is above the initial target and still needs optimisation. UV unwrap is provisional; there is no decal editor or approved livery template yet.
- Compact replaces one third of the migration trial field, including the player. The chase camera is closer for usable model review. Existing coupe/utility bodies remain prototypes.
- Wheel animation now selects only the four named axle pivots in each detail level. The former prefix match also animated `Wheel_FL_Mesh` and similar children, visibly swinging wheels away from the body. Evidence: `unity-03/captures-field-master/02-race.png`; corrected wheel capture: `unity-03/captures-field-fixed/02-race.png`.
- Imported model checks exercise twelve spin angles at three steering angles, checking wheel geometry stays near each axle. This covers seven imported models, including all compact LODs. It does not prove steering clearance, suspension travel or absence of collision clipping.
- Gallery includes front/rear/side, forced lower LODs, a steered/spun pose and an old-left/new-right comparison using the game's actual material mapping. The earlier `gallery-01/06-old-left-new-right.png` filename was reversed; use the corrected comparison from gallery 02 onward.
- Separate paint, plastic, rubber, alloy and lamp material responses replace a broadly shared glossy finish; gallery anti-aliasing now matches the race camera. Review of gallery 03 rejected reduced-model glazing changes and dented roof highlights. `source-art/compact_lods.py` now preserves the pane geometry and recomputes reduced-surface normals; imported glass triangle counts are checked against the hero.

## Visual assessment against the concept

The model is substantially more developed than the preserved compact prototype, with readable wheel openings, glazing surrounds and separate body details. It remains behind the reference: cabin/roof transitions are too angular, lamps protrude rather than fitting into the front quarters, tyres lack tread treatment, bumper shaping is basic and the window/paint response needs further refinement. Forced LOD renders require review alongside gameplay distance; no transition or performance acceptance is claimed yet.

The flat trial island, sparse vegetation and simple pit buildings remain placeholders. The concept's coastal town, bridge, landscape composition and integrated exploration have not been recreated in Unity. Improvements to this car must not be counted as world or adventure completion.

## Verification and delivery

Wheel correction build 02 compiled and exported successfully. Its imported geometry checks passed for all seven resources, with maximum measured wheel bounding-centre offset from a pivot about 3.35cm (about 2.2cm for the compact). This small offset comes from asymmetric wheel detail; it does not establish a collision tolerance.

Build 02's seed 1709 completed 24/24 over three laps: stock 23rd, 109.409s; no recovery resets or missed gates, no sampled centres outside the road. Maximum recorded contact penetration was 5.64cm. Build 03's seed 1721 also finished 24/24: stock 23rd, 108.892s. That seed exposed a remaining traffic/contact failure: street car 10 left the road in six 1Hz samples, recovered once and finished last; maximum contact penetration was 12.37cm. Completion is not a passing no-clipping or traffic-stability gate. These results supersede any assumption that iteration 09's zero-reset outcomes generalise to the new bodies.

Build 04 contains the corrected lower LODs and passed 84 editor checks. Its exact exported player completed seed 1709, three laps, 24/24 finishers: stock 23rd in 109.409s, zero resets/missed gates and no sampled off-road centres; maximum contact penetration 5.64cm. Evidence: `unity-03/field-package-1709.json` and its `.validation.json`. This does not erase the seed-1721 failure above. No performance claim is made from background automated runs.

Direct Unity renders inspected: `unity-03/gallery-04/01-master-front.png`, `05-lod2-front.png`, `07-old-left-new-right.png` and the final race capture. The corrected far LOD preserves the windscreen and cleaner roof shading; close-up lamp/rim reduction remains visible, and moving LOD transitions still require review. The comparison shows the old compact on the left and new study on the right. These are genuine engine renders, not the concept illustration.

Windows checkpoint: `outputs/little-road-unity-trial-03.zip` (development trial). Previous trial ZIPs remain unchanged. The final gameplay assembly SHA256 is `2E0A32528450471832DEC1FA9B92FB242E924E4485DECB7F375356F6E7BE2D28`. Packaging evidence in `unity-03/package-manifest.json` records the archive and per-file verification; only use the zip once that verification exists.

## Remaining requirements

Three approved masters, complete UVs/liveries, body generator, silhouette review and ~100 curated bodies remain open. So do continuous contact/no-clipping checks, visible performance profiling, physical DualSense checks, full race-balance matrix, adventure/economy/save migration, controller UI and audio. The full goal remains active.
