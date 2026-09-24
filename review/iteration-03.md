# First playable development review

16 September 2026. Goal remains active. This is evidence of progress, not a completion claim.

## Implemented

An original, editable Blender car with independent wheel pivots; a generated coastal loop and town; a CharacterBody3D driving controller and follow camera; three residents and dialogue; a delivery with one-time reward; engine and tyre purchases; recolouring; five collectibles; a simple race; pause and local save/load.

Engine: Godot 4.7.2. Renderer: Forward+ using Vulkan on the RTX 4070 Ti SUPER. Asset tool: Blender 5.2.2 LTS.

## Visual iterations

1. [Initial village capture](captures/iteration-01/01-village.png) exposed back-facing generated road/terrain triangles. The ocean was visible through the missing ground.
2. [Second village capture](captures/iteration-02/01-village.png) confirmed the geometry fix and exposed washed-out terrain and gaps between road ribbon segments.
3. [Third village capture](captures/iteration-03/01-village.png) confirmed improved colour handling, lighting and road continuity. [Car close-up](captures/iteration-03/03-car.png) and [coast view](captures/iteration-03/02-coast.png) document the current asset and scenery state.

4. The standalone Windows build rendered at 1920×1080 and exited cleanly. Its first capture exposed a collision-shape offset that buried the lower tyres. The corrected build passed a wheel-baseline check and produced [this car close-up](captures/standalone-02/03-car.png), [village view](captures/standalone-02/01-village.png) and [coastal view](captures/standalone-02/02-coast.png). The player now rests on the road. The garage slab no longer crosses the road. Lighthouse ground and foundations were adjusted, but terrain transitions and some pavement edges remain visibly rough.

The capture harness now lets physics settle before taking pictures. Older captures retain their defects as iteration evidence. This verification covers the three shown views, not every prop or vehicle in the world.

## Current visual assessment against the concept sheets

| Dimension | Current assessment | Evidence / next correction |
| --- | --- | --- |
| Car model | 2/5 | Recognisable small teal car, but slab-like cabin, limited wheel arches and too little body shaping |
| Architecture | 2/5 | Consistent basic buildings, but repetitive forms, blank side walls and no convincing garage frontage |
| Road and edges | 2/5 | Continuous route now renders correctly; surface remains plain, and paving/edge transitions need detail |
| Lighting and atmosphere | 2/5 | Colour clipping improved; sky is flat and the world lacks the reference's richer shadows and depth |
| Composition and exploration | 2/5 | Route exists, but the initial view lacks an inviting coastal vista, bridge and strong landmark composition |
| Prop density | 1/5 | Large empty areas, primitive trees and sparse roadside detail |
| HUD | 3/5 | Readable restrained palette; dialogue interaction and varied display sizes still need visual testing |

These are qualitative observations from the actual captures, not numerical image-similarity measurements. All visual completion gates remain open.

## Functional evidence

[Test results](test-results.json): 23/23 scripted checks passed. The suite instantiates the actual scene and controller. The latest 30-second simulated steering run reached about 500.4 metres along the loop with maximum measured centreline offset about 1.73 metres.

Coverage: quest order, duplicate rewards, purchase charges, collectible IDs, save/load state, repeated save replacement, malformed-save rejection, floor collision and wheel baseline, acceleration, braking/reverse, sea-level recovery, hills/bends, ordered race gates, reward non-repetition and restart state.

Limits: tests use scripted input and direct state transitions. They do not yet establish a complete human-played delivery/race session, fair AI competition, gamepad operation, 10–15 minute pacing or a sustained 1080p performance result. Race opponents currently follow a path without vehicle collision or overtaking logic. There is no race countdown yet.

## Next priorities

1. Test resident interactions through actual controls and inspect every roadside building, pavement edge and NPC for grounding defects.
2. Expand rendered review coverage to the garage dialogue and a race in motion.
3. Improve the car's cabin/body transitions and wheel arch treatment.
4. Rework the village composition toward the coastal reference, add visible coastline/bridge, richer vegetation, fences, stonework and small props.
5. Add audio, stronger navigation cues and the equipment/route/resident-service depth prototype.
6. Replace basic path-driven rivals with tested racing behaviour, and measure performance during a complete driving/racing run.

No required end-state feature has been removed or relaxed to fit the current implementation.
