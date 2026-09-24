# Driving dashboard, live race order and playful UI

**Build 95 progress:** actual course/car map, newest three completed laps and transient final-lap notice are implemented. Both controlled tracks preserve build 94's recorded racing; the three-lap 24-car 720p check validates actual lap passage times and presentation without overlap. Secondary labels remain small at this resolution. See [review](../review/unity-35/race-hud-95-review.md). Move next to the shared preview/results menu treatment below, retaining broader HUD/controller/performance acceptance work.

**Build 94 progress:** transparent driving instruments, eleven/five-entry relative standings and actual lap/best clocks are implemented, with exact recorded Bayview/Cedar behaviour parity. The user approves this visual direction. The clean HUD on/off pair has similar frame times, but both fail the frame budgets; see [review and limitations](../review/unity-35/race-hud-94-review.md). This completes the first visual slice, not the full menu redesign or endurance displays. Physical controller and smaller-window acceptance remain open.

**Latest direction,23 September:** the user chooses Gran Turismo7's sparse race HUD as the starting point and supplies its numbered official illustration. Replace the first cream-card race HUD with transparent readouts and minimal translucent backing, prioritising unobstructed driving. Keep the playful/artistic direction for menus; do not apply large decorative menu panels to the race view. The eleven-entry requirement remains, with compact five-row mode. [Numbered reference mapping](40-race-hud-reference-map.md) preserves the current and future role of all25 items, including fuel/tyres/endurance.

23 September 2026. User-requested next visual milestone, after finishing the personality, shared roster and adventure integration changes in document 38. This is part of the existing campaign scope, not a replacement goal.

## Player experience

**23 September interactive-menu steering:** the user requests a more playful illustrated experience, a car spinning on a platform, an explorable looping group of actual 3D fitted parts, a Football Manager-style driver attribute chart, gentle cyclic grid movement and green/red PP comparisons (lower than the player's car is green, higher is red). Keep clear labels/arrows alongside colour and pause motion during inspection. Implemented driver risk, brake commitment and consistency are the initial chart axes; do not invent racing-line/turning/overtaking skills or assign an AI personality to the human player. Keep the vehicle/parts data authoritative and entry inspection read-only. An optional grid tour may cycle/fade rows while normal navigation remains stable. After this bounded interactive pass, performance is the next priority.

1. A compact virtual dashboard shows current speed, gear and acceleration/braking input, using the transparent reference direction above. Prioritise a readable speed numeral, a clear drive indicator and responsive pedal meters without duplicating information in large decorative dials. Distinguish throttle input from measured acceleration: the pedal may be fully pressed while acceleration is low. Use actual simulation values; the current continuous drivetrain uses an honest D/R automatic drive indication until discrete gears exist. Support forward, reverse and stationary states, and the chosen speed units.
2. A live race-order panel shows the player with up to five competitors ahead and five behind. Near the front or back, fill the remaining rows from the available field to retain up to eleven entries. Highlight the player and show rank, driver name and time difference relative to the player. Make the gap convention explicit and consistent. Track race order through legal lap/checkpoint progress, not physical proximity: nearby lapped cars are not necessarily adjacent competitors. Show unavailable timing honestly until enough data exists; do not manufacture a precise gap by dividing distance by current speed, particularly during braking, stops or crashes. Choose and validate a lightweight timing history at common track positions, with separate finished and lap-down states.
3. Redesign the actual game UI into a playful, artistic visual identity. Cover exploration HUD, resident dialogue, garage and parts selection, race preview, countdown, live race HUD, results/rewards and settings. Use the coastal and vehicle-equipment reference images as art direction. Original illustrated motifs, soft shapes, warm materials and expressive hierarchy should replace the current block-menu presentation while keeping names, prices, stats and controls readable. Keep the 3D parts/car inspection central to ownership.

## Approach and order

The user's latest sequence is preview/results first, then a focused performance pass because gameplay is visibly choppy. Queue further garage/exploration/menu expansion behind that pass; keep the broader redesign required.

After the transparent race-view slice, prioritise the pre-race and results screens as one reusable menu treatment before spreading it across the garage and exploration. Actual build 94 result captures reveal light text on near-white buttons and an old top-bar elapsed clock continuing beyond the player's finish time. Fix both in this pass. Keep actual 3D car inspection, full loadout/personality information, clear keyboard/controller focus and the retry/cancel/payment flow. Judge it using real screens, not isolated component mockups. The driving HUD retains its restrained transparent identity while menu art can be warmer and more expressive.

First inspect the existing UI and telemetry sources, then establish a small visual direction with a driving HUD and race-preview screen in actual game renders. Prove typography, palette, panel shapes, selection/focus treatment, button prompts and restrained motion on these screens before carrying the same components across other menus. Preserve each screen's real actions and information, including purchase/fitting feedback and exactly-once reward flow.

Build the dashboard and race-order data separately from their presentation. Reuse RoadCar, RaceTrial and the immutable roster as the authority for speed, inputs, identity and entered equipment. Time-gap sampling and ordering must have a bounded memory/CPU cost, reuse buffers and avoid adding per-frame work to every AI driver. Refresh display text at a suitable cadence while smoothly animating instruments; rendering cadence must not alter physics or race results.

The race view should retain road visibility, especially braking markers, apexes, opponents and rear-view use. Allow compact HUD options if eleven rows obstruct the scene. Account for long names, more than 24 entrants, small windows, 1080p/fullscreen, colour contrast and controller focus. No colour-only indication of player selection or input state. Physical DualSense review remains distinct from synthetic input checks.

## Acceptance

- Speed, direction and applied pedal values agree with recorded simulation telemetry; no invented performance changes.
- Gear semantics match the implemented transmission or are explicitly presented as a visual approximation until a discrete transmission exists.
- Race order and gaps handle countdown, overtakes, close side-by-side cars, start/finish crossing, different laps, stops and completed races without flickering ranks or falsely precise timing.
- The eleven-entry window retains the player, handles first/last position and smaller fields, and uses actual roster names.
- Menu redesign preserves purchases, fitting, cancel/back, race confirmation, results, payout and save/reload; keyboard and controller navigation remain clear.
- Inspect actual moving gameplay and rendered screenshots across representative screens/resolutions. Compare frame-time cost using the same track, field and graphics settings.
- User visual/usability review completes this milestone. Concept images and automated tests alone do not establish acceptance.

Keep broader driver skills, teammate progression, endurance and campaign content on their existing roadmap. Do not extend baseline AI refinement indefinitely before this visual work.

## Showroom direction,23 September

The user requests that the car display be a genuine showroom, such as a dark room with reveal lighting, rather than a generic PNG backdrop. Use a real isolated3D studio, lit turntable and body highlights while the model rotates. The coastal illustration belongs to the wider menu. Keep all inspection read-only and tied to the entered vehicle and fitted equipment. Give the parts orbit a workshop inspection-mat treatment, clear selection markers and separate driver/part notes. Preserve pause/resume, manual rotation and keyboard/controller return paths. Performance remains the next workstream after this bounded visual pass.

**101 acceptance checkpoint:** the user viewed the actual showroom/parts screenshot and answered "Yes—keep this direction."498 editor checks,212 runtime preview checks, controlled Bayview recorded parity and connected17/18 checks complete. Rewards/retry/save reload pass; the known second-race recovery limitation remains. [Review/evidence](../review/unity-35/race-menu-101-review.md). Close this bounded visual pass and move next to measured performance; physical-controller and broader UI/campaign acceptance remain open.

**Post-UI performance103:** accepted101 UI retained unchanged. The first measured optimization improves whole-AI cost17–20% and frame tails with recorded racing parity on both courses; budgets remain open. Continue performance per [review](../review/unity-35/frame-cost-103-review.md) before spreading the menu redesign further.

**Performance106:** guarded reuse105 and owned worker ranking106 preserve the accepted visual and racing output. Latest pair improves whole-AI cost18.39%/17.19%; Cedar narrowly meets frame targets once, Bayview and per-frame AI budgets remain open. [Evidence](../review/unity-35/frame-cost-106-review.md). User sequence is performance, fresh racing-AI research, then collective pace—not another UI/art pass yet.
