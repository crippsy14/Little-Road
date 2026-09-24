# Iteration 11 — connected Unity adventure

17 September 2026. This iteration reconnects progression to driving and racing in the Unity project. It does not complete the coastal-world art, handling, balance, body catalogue or expanded ownership roadmap.

## Playable connections

The ordinary executable now opens free driving beside Harbour Garage. Mara offers a parcel, Pip receives it at the lighthouse, and returning to Mara awards 350 credits. Five stable-ID postcards sit on optional roadside detours and award 50 credits each. Rowan offers Bayview Coast C: 24 entrants, three laps, fixed street/club rival equipment and 100–500 credits for every valid player finish.

The garage exposes all seven existing equipment slots and fourteen purchasable parts. Buying adds ownership; fitting is a separate free action. The actual Rigidbody controller receives the fitted power, grip, gearing, braking, steering and mass values. Cosmetic paint is saved independently and follows the player into races. Changing a silhouette does not grant performance, but a new body-selection interface is not part of this iteration.

Finishing settles the current race serial once, saves money and best place, and presents return/retry actions. Retiring gives no payout. Starting a subsequent race invalidates an abandoned race serial. Career play does not expose the standalone trial's AI-toggle shortcut. The developer smoke test uses automatic driving explicitly.

## Persistence and controls

`AdventureProgress.cs` introduces Unity save schema 1. The normal save is `little-road-unity-save-v1.json` in Unity's application persistent-data directory. It is separate from the Godot save; no Godot save import is claimed. Each candidate transaction is validated and written to a temporary file, flushed and atomically replaced before it becomes the live state. A `.bak` retains the previous successful save. Invalid/unsupported saves are rejected without overwrite, and failed commits leave live progress unchanged.

The Canvas UI has keyboard, mouse and Input System gamepad navigation. Driving is blocked while town menus are open. Proximity interactions use the current Rigidbody position and require low speed. Free-driving recovery returns to the nearest route segment without advancing race gates. Physical DualSense USB/Bluetooth, remapping, dead-zone calibration and a manual full-route playtest remain outstanding.

Controls: WASD/arrows or left stick plus R2/L2 to drive; E/Cross to talk when stopped nearby; R/Triangle to recover; Escape/Circle for town menus. Menu arrows/D-pad choose and Enter/Cross submit. During a connected race, Escape/Options returns to town; an unfinished race pays nothing. The standalone developer race remains available with `--trial-auto` or `--trial-race`.

## Evidence

Builds 01–03 compiled with 102 checks, including 18 progression/persistence checks covering rejected purchases, unowned fitting, quest sequencing, duplicate finds/rewards, stale race IDs, reloads, invalid candidates, failed disk commits and unsupported saves. These are transaction-level tests, not playability or visual acceptance.

The first exported smoke test exposed an interaction check using a delayed render position, and screen capture failing in background mode. Both were corrected. Build 02's exported integration test passed: it invoked the visible conversation/garage actions, relocated between residents and two finds, bought and fitted the Workshop tune, entered a real three-lap 24-car race, settled its reward, returned to town and loaded the save again. All 24 finished; the player placed 21st in 106.175s and retained 100 credits. There were no recovery resets or missed gates in that race; contact penetration still reached 7.95cm. See `unity-04/smoke-02.log`, `smoke-02-result.json`, and `smoke-02-race.validation.json`.

The smoke test deliberately relocates between adventure objectives. It proves the exported systems connect, not that the complete route has been driven manually. Build 03's synthetic gamepad check caught a menu handoff defect: the same Cross press could confirm dialogue and reopen the nearby interaction. Build 04 added a short interaction debounce after closing dialogue; the exported test then passed D-pad navigation, Cross confirmation, menu throttle blocking, and actual Rigidbody throttle/steering movement. See `smoke-04.log`. A separate process reloaded the earlier completed profile and rejected duplicate payouts (`restart-02.log`). These are synthetic Input System checks, not physical DualSense testing.

The refreshed UI updates wallet/objectives while menus are open and uses dark text on the selected gold button. Captures include the actual Canvas interface. Final scene checks also replace the flattened lighthouse footing's default capsule with a matching static mesh collider and size parked resident colliders from their rendered models; continuous no-clipping acceptance remains open.

The user-requested visible 24-car run used build 03 and seed 1721. Its window was brought to the foreground and inspected while lap 3 was running. It finished 24/24; `unity-04/visible-watch-24.json` records the result. No visible frame-rate acceptance is claimed from simply watching this test.

### Final exported checkpoint

Build 05 passed all 102 editor checks and the exported smoke test, including synthetic controller navigation/confirmation, blocked menu throttle, and physical free-roam acceleration/steering. The connected race finished 24/24 with zero resets and missed gates; the Workshop-tune player finished 24th in 106.075s and received 100 credits. Maximum recorded contact penetration was 7.93cm, so no-clipping remains unproven and currently fails the intended standard. See `smoke-05.log`, `smoke-05-result.json`, `smoke-05-race.json` and its validation file. Different runtime outcomes across builds are not evidence of accepted balance.

A separate process loaded that final profile, verified quest stage 3, two finds, owned/fitted Workshop tune, 100 credits and settled race serial, then rejected duplicate delivery/race awards (`restart-05.log`, `restart-05-result.json`). The original test profile and its backup remain under review evidence, separate from the normal player save.

The checkpoint is packaged as `outputs/little-road-unity-adventure-04.zip`. The gameplay assembly SHA256 is `28B3883502C3685349B028BA05B1F2FCD6D2B237FCD1E9517A4AD96F8BA7FEB3`. The archive's per-file verification and hash are recorded in `unity-04/package-manifest.json`; previous race-only zips are preserved. Final captures are in `unity-04/captures-smoke-05`. The result capture was taken immediately on settlement, before the button colour transition/selection had finished; it is not evidence of the settled menu's final focus appearance.

## Visual review and remaining work

Captured conversation and garage pages are readable and show their actual game backgrounds. The prototype pit buildings and flat island remain far behind the coastal concept. A simple lighthouse marks the delivery destination; it is a navigation landmark, not finished environment art. Town signs, scenery materials, shadow fill, garage framing, scenery density and the adventure layout need further passes. The known compact art gaps and contact penetration remain unresolved.

Against the unchanged document-07 rubric, the garage interaction view (`unity-04/captures-smoke-04/01-mara.png`) scores as follows. These are visual judgments, not similarity measurements:

| Dimension | Score / 5 | Evidence and gap |
| --- | --- | --- |
| Visible car quality | 2 | Resident utility is still a simple older prototype; the dialogue partially obscures the player |
| Architecture/materials | 1 | Box facades lack an open workshop, shaped roofs, frontage detail and grounded materials |
| Road/roadside detail | 2 | Driveable surface exists, but racing kerbs continue through a bare town forecourt |
| Lighting/atmosphere | 2 | Deep building shadows obscure cars; missing landscape depth and reflected fill |
| Composition/exploration | 1 | Flat open grass replaces the concept's layered village, coast, bridge and distant hills |
| Prop density/polish | 1 | Few tools, planters, furnishings or readable physical wayfinding props |
| HUD/UI | 3 | Readable, navigable dialogue with clear options; large overlay and framing still need polish |

This view fails the required score of four in every applicable dimension. The full five-view acceptance set has not been recaptured or passed in Unity. Next visual work should build the actual garage/village and coastal composition around the connected journey, not treat this functional port as art parity.

Audio, the complete expanded parts families, usable traversal upgrades, further tracks/classes and unlocks, three approved master bodies, decal tooling and the ~100-body roster remain required. Qualification for the planned Quarry Link is described in dialogue; no unbuilt track is presented as playable. The full goal remains active.
