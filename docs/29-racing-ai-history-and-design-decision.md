# Racing AI research: historical approaches and our next design

21 September 2026. Research requested after the experimental 24-car field exposed congestion, excessive braking and poor frame pacing. Sources below are developer accounts, technical chapters or the authors' own research. Recommendations are our engineering conclusions, not claims that another game's method will automatically work in Little Road.

## Decision

Use **track-relative tactical context, equipment-aware path/speed planning, and a separately validated vehicle controller**. Retain the clear-track reference as guidance. Add an explicit distinction between occupied space, a developing risk and a beneficial racing opportunity. Keep the physical and checkpoint systems authoritative.

Our current candidates still fail the full-field test. Changing path scores or margins further without isolating the controller and interaction failures is unlikely to be efficient. The next implementation should be split into independently measurable stages below.

## Most relevant historical work

### F1 2011 — context steering

Andrew Fray's [Context Steering chapter, Game AI Pro 2, 2015](https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf), especially sections 18.5–18.6, describes the system used in F1 2011. Separate maps represent danger and interest across the track. Reachability limits which desirable positions can be chosen. The racing line supplies a modest preference; nearby opponents and tactical opportunities influence the result. A potential future concern need not trigger emergency braking. This is a particularly relevant alternative to avoidance dominating every decision.

Fray's earlier [2013 explanation](https://andrewfray.wordpress.com/2013/03/26/context-behaviours-know-how-to-share/) explains why blending isolated steering decisions can lose essential context. It also describes smoothing maps, retaining information between updates, and interpolating between samples. These are useful tools against abrupt direction changes, but a context map by itself is not proof that our physical car can execute the selected movement.

**Our application:** score progress, corner exit and useful passing space separately from body occupancy. Restrict moves to reachable road, then compare their usefulness. Display both maps in diagnostics. Emergency braking needs its own reason and threshold; it should not be the generic result of losing a preferred path.

### Pack racing — the heat vision system

Nic Melder's [The Heat Vision System for Racing AI, Game AI Pro, 2013](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter41_The_Heat_Vision_System_for_Racing_AI.pdf) combines the influence of surrounding cars in a track-width array. It addresses the weakness of responding to one opponent at a time, such as returning to the racing line between two cars that could be passed together. The article keeps information about which opponent influences each position, smooths the result, and distinguishes on-track racing from recovery.

**Our application:** retain a passing corridor while it remains useful across the pack, with full clearance before merging. Record the specific car or boundary constraining each candidate. Recovery remains a separate behaviour. Do not treat “lowest cost on the far side of another car” as a reachable destination.

### Driver: San Francisco — feasible local paths and CPU scheduling

Chris Jenner and Sergio Ocio Barriales' [Fast Cars, Big City, Game AI Pro 3, 2017](https://www.gameaipro.com/GameAIPro3/GameAIPro3_Chapter17_The_AI_of_Driver_San_Francisco.pdf) describes route selection, coarse local paths, and a lower-level optimizer using a simplified vehicle model. The authors required active AI cars to use the player's vehicle simulation without extra power or grip. They also discuss asynchronous work and continuous paths that remain useful while another planning result is being prepared.

**Our application:** evaluate the predicted *driven* path, not just the ideal geometry. A path that appears clear can still be undriveable after momentum and steering response are included. Keep a usable committed path and update it coherently. Cache static route information and bound computation. We will profile before deciding whether Unity Jobs/Burst or asynchronous planning is warranted; raw Unity physics queries cannot simply be moved onto arbitrary worker threads.

### Track representation and control — separate the two problems

Simon Tomlinson and Nic Melder's [Representing and Driving a Race Track, 2013](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter39_Representing_and_Driving_a_Race_Track_for_AI_Controlled_Vehicles.pdf) combines precomputed track information with current-state evaluation. It discusses speed/curvature-dependent look-ahead, braking before a corner, and the difference between a guide line and the actual driven trajectory. Track coordinates suit strategic reasoning; fine collision avoidance needs physical coordinates.

Their [Racing Vehicle Control Systems using PID Controllers](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter40_Racing_Vehicle_Control_Systems_using_PID_Controllers.pdf) emphasizes repeatable tuning scenarios, explicit success metrics and recorded controller errors. It distinguishes speed-control and steering goals and discusses changing conditions such as surfaces and fuel mass.

**Our application:** first prove that a car can hold an inside, middle and outside road corridor through Cedar's corners. Measure lateral drift, overshoot, braking distance and actual-versus-requested speed. Then test transitions between those corridors. Do not assume that a good solo reference-line lap validates arbitrary off-line driving. Changing from pure pursuit to PID is an experiment to justify with evidence, not a requirement of this research.

### MotoGP — useful coordinate-system history, with a caveat

Shawn Hargreaves' [MotoGP: AI coordinate systems, 2009](https://shawnhargreaves.com/blog/motogp-ai-coordinate-systems.html) explains distance-around-track plus sideways position as a convenient basis for relative positions and passing decisions. He explicitly says he did not work directly on the AI and that the account is recollection/hearsay. Use it as historical context, not definitive implementation evidence.

**Our application:** our existing distance/offset representation is appropriate. Preserve its wraparound and lap semantics, while checking actual world-space body envelopes at close range.

## More recent work: useful validation lessons

Sony AI's [How to Train Your Race Car, July 2022](https://ai.sony/blog/how-to-train-your-race-car) describes GT Sophy's distinct scenarios for starts, corner battles and slipstream passing, plus separate skill evaluation and human testing. Random traffic exposure alone did not reliably cover some important situations. This supports retaining our small controlled fixtures alongside the full-grid test; it does not imply we need reinforcement learning.

Sony AI's [sportsmanship account, September 2022](https://ai.sony/blog/dont-cross-that-line-how-our-ai-agent-learned-sportsmanship) describes excessively cautious behaviour arising from overly broad collision penalties, as well as the opposite problem of exploiting gaps in fault rules. The team also improved how opponents were represented. Our conclusion is to judge race progress and responsibility together: proximity is not itself a crash, and speed alone does not make a successful racing driver.

TUM's [Graph-Based Local Trajectory Planner](https://github.com/TUMFTM/GraphBasedLocalTrajectoryPlanner), associated with its 2019 publication, returns feasible action alternatives such as following or passing on either side. It is a useful example of separating feasible trajectories from the behaviour that chooses one. It is Python research software for autonomous racing, not a Unity game component we can simply install. No code from it has been imported.

## Implementation sequence for Little Road

The traffic15 user review adds a more specific [anticipation and immediate collision-response design](30-anticipation-and-collision-response.md), informed by Alien: Isolation's separation of responsibilities. It identifies current scoring and traffic-relevance issues and defines the next acceptance fixtures. Read that alongside the sequence below.

These are proposed acceptance steps, not completed features:

1. **Prove local driving without traffic.** Fixed road corridors through the complete circuit, then deliberate changes of corridor at several entry speeds and equipment levels. Validate bounds, lap pace and transition response. Add a prediction-versus-actual trace to expose model mismatch.
2. **Add tactical context.** Sample usable road width ahead. Record occupied body space, closing speed, corner approach and progress opportunity separately. Generate a few useful alternatives instead of running expensive full checks on every nominal offset. Preserve ordinary vehicle inputs.
3. **Handle shared corners.** Test two equal cars alongside on both sides of each representative corner. Neither may insist on the ideal apex through the other. Test early braking by the leader, a closing passing gap, and enough room versus insufficient room. Speed must match the chosen radius and equipment.
4. **Add commitment and release rules.** Hold a viable passing side, change it when a materially better safe opportunity exists, and abort explicitly if it closes. After the pass, merge only with full-body clearance. A driver already alongside must not be pushed out simply because another prefers that line.
5. **Re-run the established following/pass/narrow-corridor/three-car controls.** Include gaps and speeds beyond the initial cases. Keep paired solo controls and physical-recovery fixtures.
6. **Pass the 24-car throughput test and moving review.** Use the fixed [117-second provisional Cedar gate](28-situational-racing-and-field-throughput.md), clean gates and body bounds, meaningful equipment-led passes, and no recovery-driven queue escape. Repeat with varied starts/seeds once the first case is sound.
7. **Measure real-time performance separately.** Use one 1× process, record frame percentiles and subsystem costs, and inspect crowded starts/corners. Accelerated batch timings cannot establish acceptable visible frame rate. Optimize the measured bottlenecks while retaining collision and physics behaviour.
8. **Only then add personalities and skills.** Existing ability/teammate plans remain the specification. Their parameters will modulate judgement, execution and opportunity choice rather than disguise universal congestion.

The decision is to keep the existing track, car physics, equipment model, checkpoint rules, telemetry and useful solo reference. The tactical planner and its connection to local control need stronger separation and more direct tests. This is a focused AI change, not a restart of the game.

## Watch/read next

- [GDC 2013: The Next Vector — Improvements in AI Steering Behaviors](https://gdcvault.com/play/1018262). Fray's [digest](https://andrewfray.wordpress.com/2013/05/15/context-behaviours-digest/) identifies his context-steering explanation in the second half. Talk page verified; the video has not been watched in this review.
- The F1 context-steering chapter and heat-vision chapter above are the most directly relevant short technical reads.
- The Driver: San Francisco chapter is the deeper follow-up for feasible local motion and workload scheduling.

Sources linked at their publishers/authors rather than copied into the project. No new software installation is needed for the proposed conventional AI work.

## Reuse-first reassessment

The player has requested a fresh review of reusable implementations before more custom tuning. [Document 31](31-racing-ai-reuse-review.md) now governs the next AI decision: evaluate Omni Vehicle AI through our existing input seam, or prototype the published F1 track-width context method as a separate candidate. One bounded comparison and correction round must end in a visible keep/reject decision. The existing planner is not already an implementation of that F1 method. No package is acquired and no replacement is accepted yet.
