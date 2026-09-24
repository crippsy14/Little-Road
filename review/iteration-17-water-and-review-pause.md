# Iteration 17 — water and review pause

17 September 2026. The user requested completion of this water pass, then a pause in development for a quick run-through. That instruction takes precedence over continuing to the other art/gameplay tasks. The full project goal remains unfinished; do not resume changes until the user asks.

## Water implementation

The flat sea box and static foam ribbon are replaced by a visual water mesh with two low-amplitude geometric swells, smaller animated normal ripples, depth-based shallow/deep colour, constrained screen-space refraction, sky reflection, sunlight highlights and broken intersection foam. It uses the already credited Poly Haven sky; no new external assets were downloaded. There is no water collider, buoyancy, swimming, diving or water-traversal equipment. The shader reflects the sky, not a mirrored view of the bridge or other nearby objects.

Build 01's actual bridge captures exposed excessive shine and regularly repeating ripples. Build 02 warps the small-wave pattern, softens the reflected sky, reduces highlights and breaks up the foam. Ten world frames now include the original eight views plus a lower bridge camera at two fixed shader times, 0 and 1.6 seconds. Normal play uses the live clock. These remain real in-game renders, not concept composites.

The 257-by-257 mesh has 66,049 vertices and 131,072 triangles, with denser spacing near the island and bounds expanded for displacement. It is an intermediate water treatment; horizon aliasing, shore transitions, reflection quality and cost need further visual/performance review. No visible frame-rate target or fluid simulation is claimed.

## Evidence

Build 02 exported successfully with 112 editor checks. Gameplay assembly SHA256: `947F8D82BFD46F0F7F495CB89A6D3E1401BF0CC7F7AD25E6833A82B2A268477D`. The runtime reported the water shader supported. The final bridge-profile and low-water captures were inspected: reflections and ripples are softer than build 01, while foam still reads as a stylised edge and near-object reflections remain absent. This is an improved intermediate treatment, not overall visual acceptance.

The final world review passed 171 road, 11 garage and 27 forecourt clearance samples and 1,710 support rays. Physical garage entry/reverse exit had zero contacts/resets and 8.9mm maximum lateral drift. `tools/check-water-motion.ps1` compared 6,000 sampled pixels in a water-only region of the identical low camera: 83.62% changed by more than two levels and mean absolute RGB-channel change was 21.53/255. This verifies that the rendered water responds to time, not that its motion is physically accurate or aesthetically final.

This pass changes scenery rendering, not vehicle physics or equipment. The previous archive's full-field race remains historical evidence and is not a new race result for this build. The new checkpoint is `little-road-unity-adventure-10.zip`, with per-file verification in `unity-10/package-manifest.json`. Its manifest references the prior race report explicitly; the fresh evidence is the build, world-access and water-motion review.

## Run-through

The ordinary Windows launch starts beside Harbour Garage. WASD/arrows drive, E talks to nearby residents when stopped, R recovers to the road and Escape opens town menus. Talk to Mara for the garage/delivery and Rowan for the 24-car race. The separate Unity save persists rewards and equipment. Physical DualSense testing and continuous manual adventure acceptance remain outstanding.

Development is to stop here for the user's review. The much broader visual, ownership, racing, collision, progression and controller requirements remain open.
