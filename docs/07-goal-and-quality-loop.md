# Active goal and quality loop

**Updated 29 September:** "12-hour campaign" means **at least** 12 hours; the campaign is open-ended and grows with content. See [doc 45](45-roadmap-and-feature-spec.md) for the current plan.


Set on 16 September 2026 at the user's request. The application goal is active; this document makes the review criteria concrete.

19 September scope expansion: the playable slice remains a milestone within the [12-hour campaign scope](17-scope-and-priorities.md). Finished-game acceptance additionally requires the [endurance/fuel/mass systems](18-endurance-fuel-and-mass.md) and [visual parts browser](19-parts-browser-reference-and-spec.md), retaining every earlier requirement. Passing the slice gate does not complete the expanded game. Planning has resumed; the latest review defects remain open.

## Goal

Build a playable Windows slice of Little Road that approaches the visual character of our coastal-world and vehicle-equipment concept sheets while retaining the interconnected exploration, equipment and progression described in the brief. Keep comparing actual game output with the references and improving discrepancies until the functional and visual gates below pass.

The generated concept sheets are the working visual target. Road Trip Adventure on PS2 remains the reference for mechanics and exploratory character. Interpret visual similarity through composition, proportions, colour, lighting, detail and atmosphere; exact pixel reproduction of an illustrative image is not a meaningful gameplay requirement.

## References

- [Coastal world](../references/concepts/coastal-world.png)
- [Vehicle and equipment](../references/concepts/vehicle-equipment.png)
- [Systems design](05-mechanics-and-system-connections.md)
- [Playable slice](02-project-brief.md)

The smaller first prototype is a milestone on the way to this target. A greybox or attractive still image alone does not complete the goal.

## Repeatable review cycle

1. Select the most important current functional defect or visual mismatch.
2. Make a focused change in the game or source assets.
3. Import assets and run code/scene checks.
4. Run the game with rendering enabled and exercise the changed behaviour.
5. Capture representative screenshots from the actual game at saved camera positions and consistent lighting.
6. Inspect each capture against the appropriate concept sheet. Record the remaining differences explicitly.
7. Check performance on the development machine and verify affected gameplay still works.
8. Save the evidence and select the next largest gap. Repeat if any completion gate fails.

Use the same camera position, field of view, time of day, image size and quality settings when comparing successive versions. Use new viewpoints as well to avoid polishing only a single camera angle.

Following the evening playtest, include continuous route coverage in both free driving and race modes, from the actual chase camera. Fixed views missed stretched grass across the road. Require a manual completion/rejoin test as well as AI trials; a successful automated payout does not clear the user's missing-finish report.

## Required views

| View | What it should demonstrate |
| --- | --- |
| Village approach, behind the car | Readable road, compact teal car silhouette, welcoming buildings, landscaping and inviting distance |
| Garage forecourt | Detailed enough architecture, props and car residents to support close interaction |
| Coastal road | Sea, landforms, convincing road edges, bridge or equivalent landmark, depth and clear route composition |
| Race in motion | Several visible vehicles, camera readability and consistent environment quality under gameplay conditions |
| Vehicle close-up | Coherent body proportions, windows, wheel fit, materials and visibly distinct equipment |

The world need not reproduce the exact concept map. Its road layout must function as an enjoyable driveable space.

## Visual assessment

Assess each required view across these dimensions:

- Car silhouette, proportions and model quality.
- Environment shapes, architecture and material consistency.
- Road geometry, surface treatment and roadside detail.
- Lighting, palette, shadows and atmospheric depth.
- Composition, landmarks and sense of exploration.
- Prop density and polish at normal play distance.
- HUD clarity and its fit with the game world.

Use a descriptive 1–5 scale: 1 = placeholder; 2 = rough direction; 3 = recognisable but with major gaps; 4 = close alignment with only minor differences; 5 = convincing final presentation. Scores are human/visual judgments, not calculated similarity percentages. Each score needs a short explanation and a screenshot reference.

The visual gate requires at least 4 in every applicable dimension across the required views, with no conspicuous missing materials, clipping, floating props or unfinished geometry. If a view fails, continue improving it. Do not lower the rubric silently to declare completion.

## Functional completion gates

- Racing follows the expanded user requirements in documents 09–11: a hard but controllable stock car, meaningful earnings from every valid finish, at least six functioning equipment slots and twelve purchasable parts, and a path to stronger class/track competition. The original two-upgrade/three-rival limits are superseded.
- Keep the full expanded ownership roadmap in scope; record unimplemented families explicitly. No inactive part may be sold as functional.
- A complete 10–15 minute adventure/race loop is playable outside the editor.
- Driving, camera, collisions and recovery work throughout the accessible route.
- Interaction, delivery, rewards, upgrades and collectibles work and persist correctly.
- At least 24 entrants including the player race together. All 23 opponents must finish repeated trials without recurring opening-bend gridlock; ordered checkpoints and retry state remain correct.
- At least one equipment choice visibly changes driving or exploration decisions.
- Automated checks cover important progression and persistence failure cases, including duplicate rewards.
- The Windows export launches and completes the same loop.
- Test keyboard and PS5 DualSense controls on Windows via USB and Bluetooth, including complete menu navigation and reconnect behaviour. Record untested hardware explicitly; generic mappings alone do not verify DualSense support.
- Measure the 1080p/60 fps target on the verified PC during representative driving and racing, not just a stationary scene. Record slow frames and settings.
- Maintain a known-issues list; unresolved progression blockers, crashes or serious visual failures prevent completion.

Subjective handling quality still needs real playtesting. Scripted input can reveal defects but cannot establish that the game feels enjoyable.

## Evidence format

For each meaningful iteration, record the date/build, changed areas, checks run, capture filenames, visual findings, performance observations and next priorities. Keep capture images under `review/captures/` and review notes under `review/`. Do not represent a Blender render or concept illustration as a screenshot of the playable game.

## Current state

Goal unfinished. The approved Unity migration now connects the adventure, garage, delivery, finds, race rewards and separate versioned saving. An original compact study has three LODs; other body masters and the roster remain incomplete. Iteration 12 rebuilt the village; iteration 13 adds sculpted coastal terrain, a real inlet bridge and matching road/shoulder support. Current captures, checks and explicit visual scores are in [the coastal review](../review/iteration-13-unity-coast.md). Visual scores remain below four, and repeated races still expose contact/run-wide/recovery problems. [Iteration 11](../review/iteration-11-unity-adventure.md) records the initial connected integration test and its limitations. All remaining functional gates and broader specifications above remain in force. Screenshots and background runs do not establish 1080p/60fps performance or physical DualSense acceptance. The Godot evening checkpoint and [handling handoff](14-power-grip-and-evening-handoff.md) remain historical references, not the current engine state.
