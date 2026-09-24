# Final-straight launch and rear-grid start

21 September 2026. Focused checkpoint-16 work; the broad persistent goal remains paused while this explicitly requested pass is completed. Checkpoint 15 stays the normal playable build until verification/promotion.

## Reproduction

The diagnostic build preserves checkpoint-15 driving, surfaces and starting order and adds opt-in `--vertical-audit`. This records collider-bottom height and upward velocity at each completed physics step, plus recent collision names/normals/impulses. It applies no forces or pose corrections.

`unity-21/baseline-field`: seed 1721, 24 cars, three laps. Vertical acceptance fails even though race completion can pass. Player 0 launches at progress 599.70 m, position approximately (-120.53, 0.10, -15.28), upward speed 7.503 m/s. Its collider bottom later reaches 2.922 m above the flat datum. Car 8 reaches 4.86 m, upward speed 9.70 m/s. The immediate contact is the island mesh at (-120, -0.019, -16.5), with normal (-0.030, 0.732, 0.681) and vertical impulse 6447.3. Flat route support and a redundant flat bridge deck are also in the contact trace.

The evidence identifies terrain rising out of the inlet beneath the bridge exit. The car can contact that concealed slope despite the flat drivable surface above it. Unity also documents that speculative CCD can produce inclined ghost contact normals on tessellated geometry; that is relevant context, not a substitute for our reproduction. [Unity speculative CCD](https://docs.unity3d.com/6000.0/Documentation/Manual/speculative-ccd.html)

## Changes under review

- Keep the rendered island intact, but clip its collider outside the 24 m corridor already supported by the road. Shared intersection vertices preserve the cut boundary. Keep the existing asphalt/shoulder support heights and the garage transition fix.
- Remove redundant bridge-deck collision under that same supported corridor. Bridge parapets remain collidable.
- Preserve speculative car collision, traction, power, gravity and steering. Do not clamp vertical movement or hide the jump with a grounding force.
- Keep player identity 0, loadout and rival seeds/equipment unchanged. Assign the player the last grid slot and move opponents forward. Stagger successive columns by 0.6 m so the player is measurably last on the actual route rather than tied with two cars in the final row.
- Validate unique slots, spacing, pre-line positions and player-last ordering for 1/2/3/24/32 cars in pack and line layouts. Runtime remains capped at the existing 24 entrants; helper coverage for 32 is not a claim of an expanded event.
- Capture actual starting positions/progress/lap/gate state and tuning in `starting-grid.json` for review runs.

## Expansion research

The [reusable content recommendation](../docs/22-reusable-content-pipeline.md) proposes incremental shared car assembly, catalogue-backed parts presentation, and track/event/quest definitions. Keep the current tuning IDs/save boundary and uGUI integration; introduce prefabs and authoring tools alongside real content, without a wholesale rewrite. This research does not implement the future browser or campaign systems.

Final regression results, artifact hashes and promotion will be recorded below.

## Verified candidate build 02

- 177 editor checks pass. Assembly SHA-256 `056518D364A6FED1C0A7897D93417ACCB04DBA62E4EFC578B0145E5EA2284C14`.
- Matched solo stock/seed-1721/three-lap runs retain identical starting position and driving settings: baseline maximum bottom height 1.001 m, upward speed 4.239 m/s, 191 airborne physics steps; candidate approximately 0.025 m, upward speed 0.000742 m/s, zero airborne steps.
- Candidate 24-car field starts the player 24th, progress -47.700 m, gate/laps zero. Driver equipment labels and seed match the baseline. All 24 finish; stock 24th / 113.261 s. Ordered-gate validation passes, zero recoveries/missed gates. Across the full field, maximum collider-bottom height approximately 0.025 m, maximum upward speed 0.0115 m/s and zero airborne steps.
- Contact audit: maximum car-box overlap 0.8851 mm across 13,593 physics poses, zero tagged-scene overlap and zero query saturations. These are sampled collision observations, not universal triangle no-clipping proof.
- World/garage: 171 road, 11 garage, 27 forecourt and 1,710 support checks pass. Physical garage entry/reverse exit pass; maximum lateral error 0.0183 m, zero counted contacts/recoveries. The previous shoulder-transition correction remains functional.
- Actual Unity rear-grid and free-drive final-approach captures were inspected. No visible scenery or car model was changed. The existing glass, roof/pillar and broad material/highlight issues remain fidelity priorities.
- The controlled missed-gate/recovery scenario rejects the off-road crossing and accepts synthetic Triangle recovery with no unearned laps. Physical DualSense/manual play acceptance remains open.
- Connected `route`: free lap 683.59 m / 42.62 s, zero contacts/recoveries, maximum offset 8.671 m within supported runoff. Career starts 24th, finishes three laps, opens the actual result menu and saves 100 credits from zero; race/settlement serials both 1. All vertical audits pass. Free-roam maximum initial bottom height 0.141 m reflects the review spawn, with no upward launch or airborne steps; career field maximum approximately 0.025 m.
- The independent `recovery` career run also completes three laps, shows results and saves 100 credits from zero after the deliberate missed gate and Triangle recovery. Its 0.191 m maximum bottom height is the controlled placement; vertical checks still pass, with no airborne steps.
- Every review process is terminal and every save is isolated. No normal player save is touched. Existing equipment, rewards and car meshes are retained.

## Next fidelity focus

Recommend the compact's glass/reflection response, roof/pillar transitions and broad panel highlights, reviewed under consistent Unity lighting in garage, chase and moving-pack views. Use that polished master to define a shared body/lighting/wheel contract and then author the next distinct body against it. This is the next proposed milestone, not an art change in checkpoint 16. The broad goal remains paused as discussed with the user; no automatic improvement loop is restarted.

Checkpoint packaging and promotion are recorded in `unity-21/package-manifest.json` and `promotion.json`. The desktop shortcut already targets the mutable normal export and will follow this promotion without needing to be replaced.
