# Compact glass, roof and material pass — 21 September 2026

The user approved the revised full-game objective in [23](../docs/23-goal-review-and-milestones.md), edited the persistent goal and resumed it. Goal status was verified as active. This checkpoint advances one bounded visual milestone; campaign, controller and overall visual acceptance remain incomplete.

## Changes under review

- Compact recipe 4 brings the roof crown down to the common front/rear frame edge. It removes the separate vertical header strips that made a raised band above the windscreen, and reduces the roof's inward thickness. Wheel pivots and driving/collision logic are unchanged.
- The compact's glass has a neutral tint and 0.72 opacity (previously 0.76), with URP alpha blending configured to preserve specular reflections. A first 0.52 version looked too clear in Unity and was darkened after inspection. Its shader configuration is shared with a retained build material so the transparent variant is available in Windows builds. Glass does not cast an opaque shadow or write to the depth-only pass.
- Body paint smoothness rises from 0.43 to 0.56 across the existing body families. This is a shared material adjustment, not new car geometry for the other families.
- The gallery now captures the existing coastal sky and studio floor into a reflection probe with cars hidden during capture. It previously had no reflection environment. Its new images must not be described as lighting-matched comparisons with old gallery renders. The unchanged world-review camera poses provide a separate gameplay comparison.
- Blender review output paths are resolved to absolute paths. The first invocation exported meshes but failed to save its first render because Blender interpreted the relative render path from a different root; the corrected invocation completed.

Source snapshots are in `unity-22/before`. Final Blender export at `unity-22/blender-03` has 82,504 / 40,124 / 18,930 triangles at LOD0/1/2, down from 83,576 / 40,638 / 19,165. These counts alone do not establish runtime performance.

## Validation in progress

Unity build 01 stalled before project loading while initialising licensing inside the sandbox. Build 02 could not open the project while that process remained alive. The explicitly identified stalled process was terminated, then build 03 was started with installed licensing access. No normal playable export has been replaced.

Required for this pass: successful editor/import checks, actual Unity gallery/world/moving-field inspection, lamp behaviour, wheel envelope preservation, a complete rear-grid race, route/payout regressions and candid rendering measurements. Physical DualSense/manual acceptance remains a separate open item.

Build 03 passed all 177 editor checks. Both batch and normal hidden gallery runs waited indefinitely for their reflection probe: inspection found `realtimeReflectionProbes: 0` in both project quality presets. The existing coastal probes were also never explicitly enabled. Build 04 enables realtime probes in the coastal and gallery setup; their captures remain scripted once, not every frame. The gallery now has a 15-second failure timeout. The initial gallery processes were stopped by their exact recorded process IDs. This finding expands the pass to make the already-authored coastal reflections operational, rather than merely increasing material gloss.

Subsequent bounded diagnostic runs showed that enabling the setting was not sufficient for a hidden player: scheduled capture still timed out, including explicit warm-up renders and time slicing, with no probe texture created. Consequently this is **not proof that normal-window coastal reflections now work**. The final gallery instead uses an explicit `Camera.RenderToCubemap` capture, assigned to a custom probe; its capture returns successfully and all gallery images are written. Its ordinary mipmaps are a diagnostic environment, not evidence of production roughness filtering. The normal coastal scene retains its native one-time probes, now enabled, and requires visible-game verification. See Unity's [cubemap capture API](https://docs.unity3d.com/ja/2022.1/ScriptReference/Camera.RenderToCubemap.html) and [scheduled probe completion API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/reflectionprobe/isfinishedrendering).

Build 08 is the final candidate for this pass, with the darker glass and unchanged recipe-4 geometry. It passes 177 editor checks. Assembly SHA-256: `4658B3809F2271884D16EDD2FE80BF1DD7885AFEE0D44717503A12CCD0A19550`.

## Visual assessment

Actual Unity `gallery-final` front/rear, `world-final` and `field-final/02-race.png` captures were inspected. The roof no longer has the previous upright header band; the cabin is more visible and the paint has stronger highlights. This remains a limited improvement. Roof corners and pillar transitions are still angular, side highlights show uneven surface curvature, and the coarse interior becomes more conspicuous with transparent glazing. The concept's more integrated proportions, trim and material softness have not been reached. The other body families remain earlier prototypes.

Hidden 1920x1080 captures and automated frame-delta statistics do not establish visible 1080p/60fps performance, moving-camera polish or LOD-transition acceptance. Keep those gates open. No 4/5 master-body acceptance or approval for mass variants is claimed.

Final-candidate world clearance passes 171 road, 11 garage, 27 forecourt and 1,710 support samples. Garage entry and reverse exit pass with no counted contacts/recoveries and 0.01823 m maximum lateral error. All seven synthetic-input/AI lamp integration cases pass in `lights-final/lights.json`. Race and connected-route checks are still running at this documentation point.

Build 08's race and route subsequently completed. All 24 finished with ordered gates, no recoveries/missed gates and no airborne steps. Stock starts/finishes 24th in 113.261 s; maximum sampled overlap is 0.8698 mm across 13,593 physics poses, with no tagged scenery overlap or query saturation. Hidden-run frame deltas average 8.352 ms (p95 8.334 ms); the run is capped and does not establish visible rendering performance. The free lap covers 683.74 m in 42.63 s with no contacts/recoveries. Connected results/payout evidence is in `route-final`.

## User-directed cabin completion and stop to minor art work

The user accepts the current car's general look as appropriate to the spiritual successor and requests one practical interior fix, then moving on. Recipe 5 cuts a real passenger well in the closed body shell: the former top deck crossed the seat backs. It adds a carpeted floor, inner door cards/armrests, seat supports, a rear cushion/back, centre tunnel and a tilted steering wheel with column, hub and spokes. The exterior concept and driving model are retained. Final cabin source/render evidence is `blender-04-cabin`; build 09 imports it and adds a diagnostic interior camera view.

The next work is the parts browser and progression, not further small exterior/material changes. Outstanding visual and performance limitations remain recorded, but no longer gate that next milestone. This user direction supersedes the earlier recommendation to continue compact polish or start another master body immediately.

## Final cabin build and checkpoint 17

Build 09 passes 177 checks and all 13 gallery captures, including an actual Unity interior inspection. The passenger well, separate seats, footwells, inner door trim and wheel/column are visible in `gallery-cabin/13-cabin-inspection.png`. Final LOD triangle counts are 85,254 / 41,443 / 19,534. The added interior outweighs the earlier roof reduction slightly; no performance improvement is claimed. Existing collider sizes/centres match build 08 within 0.01 mm, with zero sampled horizontal wheel-envelope escape for the four audited families.

The final build's three-lap field also passes: all 24 finish, stock starts/finishes 24th in 113.336 s, ordered gates pass, recoveries/missed gates are zero. Vertical audit passes with zero airborne steps, maximum bottom height 0.0250003 m and maximum upward speed 0.00324 m/s. Maximum sampled box overlap is 0.8774 mm over 13,602 completed physics poses, with zero tagged environment overlap and query saturation. Capped hidden frame deltas average 8.348 ms / p95 8.334 ms; these remain diagnostic only.

World, lights and connected-payout evidence above is from build 08, before the cabin-only geometry and gallery-camera addition; it is not relabelled as a final-build rerun. The final build separately rechecks model envelopes and a complete field. Assembly SHA-256: `EC4F16A2CCE459D9336EA92E275DF134C17BD417F3C18F9DD3C823F2DDA29A14`.

All review runs are terminal and used isolated saves. Checkpoint 17 is packaged and promoted to the normal launcher; records are `unity-22/package-manifest.json` and `unity-22/promotion.json`. All 320 archived/staged files were verified, and checkpoint 16 is preserved in `outputs/little-road-unity-windows-before-17`. Archive SHA-256: `95CD2CC1AD3BF97836A0AD1F882A7512DBBCAD4DF75A5DC36217367D678C6C28`. The persistent goal remains active and incomplete. Next: engines first in the visual parts browser, using existing catalogue IDs and one authoritative equipment calculation; then expand the same flow across categories.
