# Goal review and milestone policy

**Updated 29 September:** "at least 12 hours" is a floor; the campaign is open-ended. Milestones now follow [doc 45](45-roadmap-and-feature-spec.md) Phases 0–5.


21 September 2026. The user approved this objective and authorised resuming development. This is the governing project objective. The user subsequently edited the app's persisted objective to the three paragraphs below; the goal update was received and its status verified as active. Its earlier wording is preserved at the end for historical context only.

## Assessment

The saved objective is still useful as a prototype brief: make a playable Windows slice, preserve exploration/equipment/progression, and repeatedly compare actual gameplay with the concept images. It explicitly requires functional checks and honest reporting, so it is not exclusively about screenshots.

However, it defines completion around a small slice and a tested build. It predates the 12-hour campaign, endurance/pit strategy, expanded ownership, teammate progression and reusable production approach. Its repeated visual-comparison loop can also encourage indefinitely polishing one car or location without proving that the wider game can grow. Revise the objective to reflect the agreed destination; do not add more features merely because the goal is being rewritten.

## Approved objective

Build Little Road into a polished, original Windows single-player car-adventure with at least 12 hours of meaningful first-playthrough content, following the agreed scope in docs/17-scope-and-priorities.md and its linked specifications. Combine enjoyable exploration and resident activities with challenging, fair races of at least 24 cars, rewarding equipment and class progression, varied collectible bodies, endurance racing with fuel, tyres and competent AI pit strategy, and the planned teammate and race-preview systems. Preserve reliable saves, keyboard and DualSense play, readable interfaces and measured performance.

Use the coastal-world and vehicle-equipment concept images as art direction, judging fidelity through actual moving gameplay as well as matched screenshots. Develop reusable vehicle, parts, event, track and world-building workflows by proving them with real content. Deliver bounded, playable milestones; after each milestone review visual quality, driving and race behaviour, progression, exploration, usability, performance and production readiness, then choose the highest-value next work. Avoid indefinite polishing of one asset, premature mass content generation or a broad framework rewrite.

The overall goal is complete only when the agreed campaign and systems are implemented and validated, meaningful campaign duration is supported by first-time playtesting, and visual, functional, controller and performance acceptance is recorded. A prototype or intermediate checkpoint completes a milestone, not the overall goal. Record remaining defects and proposed scope changes explicitly; do not silently drop requirements or treat concept art and automated tests as proof of player acceptance.

## Destination, milestones and next work

The product scope remains in [17](17-scope-and-priorities.md). Approximately 100 curated bodies and more than 60 functional part variants remain planned there; alternate decals are not extra body shapes. Region, track and activity counts in its proposed content budget remain estimates, not newly agreed commitments. The revised objective does not add multiplayer, console ports or other excluded features.

| Milestone | Exit evidence |
| --- | --- |
| Reliable current race loop | Checkpoint 16 has automated evidence for the ramp fix, last-place grid, ordered laps, payout, persistence and recovery. Close outstanding manual finish/rejoin and physical DualSense acceptance; keep the automated checks as regressions |
| Convincing, repeatable visual baseline | Bound each pass to named defects. Inspect garage, chase and moving 24-car views against references; measure rendering performance and LODs. Prove the assembly workflow with another distinct master body before scaling variants |
| Satisfying first progression journey | Fresh-save stock-to-upgrade-to-win sessions, fair earnings, engaging racing, purposeful exploration and a polished buy/compare/fit parts workflow. Equipment improves expected pace without fixing finishing positions |
| Endurance and expansion proof | Short fuel/mass/tyre/pit prototype with credible AI strategy; a second authored track/event and resident content through reusable definitions. Validate these before long endurance events and large content multiplication |
| Campaign production and acceptance | Grow varied activities, classes, bodies and parts; integrate the specified future systems at their planned milestones. Measure first-playthrough duration, test save continuity and finish controller, audio, settings, usability and performance acceptance |

These are evidence gates, not a rigid waterfall. Visual improvements continue during systems and content work. A full 4/5 visual benchmark remains an acceptance target, but an unfinished reflection is not a blanket reason to postpone all progression or content validation.

**Current direction, following the user's subsequent review:** the car's look is sufficiently aligned with the Road Trip spiritual-successor feel to move forward. Finish the requested hollow-cabin cleanup and verify the existing race loop, then prioritise the parts browser and progression. Do not continue minor compact/exterior/reflection polishing or hold that work behind the full visual benchmark. Close manual race/controller and rendering-performance acceptance when practical; retain remaining art issues for a later pass driven by gameplay/content needs.

## Review discipline

For every checkpoint, record the intended player-visible improvement, a finite scope, relevant regression checks, actual results and remaining limitations. Review all the quality areas above even when only one changed; new failures can override the planned next item. Reuse proven checks and broaden testing when changes or evidence justify it.

Keep visual evidence and player experience separate: attractive images cannot establish race enjoyment, and successful automated races cannot establish controller feel. Report what is implemented, what is verified and what remains a design proposal. If measured production rate makes the agreed scope impractical, present a concrete tradeoff for the user's decision rather than quietly reducing it.

## Saved goal at review time

Status: paused. Original objective, preserved verbatim:

> Build and iteratively improve the Little Road playable Windows game toward the coastal-world and vehicle-equipment concept images saved in outputs/little-road/references/concepts, while preserving the interconnected exploration, equipment and progression design in the project brief. Work in milestones: validate tools, prototype driving, complete the small adventure/race slice, replace placeholders with original art, then repeatedly run the game, capture and inspect representative screenshots, compare them against the visual references, fix the largest visual or functional gaps, and retest. Completion requires a tested playable Windows build, recorded visual comparisons and functional checks, and candid documentation of remaining differences; concept images are the art direction, not evidence of implemented gameplay.
