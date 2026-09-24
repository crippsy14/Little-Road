# Live recovery review

21 September 2026. Inspected through the computer-use skill's live game-window screenshots. This is sampled live visual inspection, not continuous video or player acceptance.

- `visible-wall-09`: normal-speed stock wall run completed. The inspected 21.4-second frame shows one physical rejoin, no assisted resets, a legible recovery HUD and ordinary racing on the home straight. The initial manoeuvre had already happened before the first inspection; this run alone does not visually establish reversing.
- `visible-wall-slow-10`: quarter-speed replay. Inspected the countdown facing the barrier, the 5.7-second rejoin with the car turned around the barrier's left end, and the 13.3-second return to racing. The observed rejoin leaves visible space between car and barrier; HUD reports one physical rejoin and zero assisted resets after it. This is a visual smoke review of the sequence, not proof of clearance at every intermediate frame. The reverse intervals are recorded in the physics trace, but were not captured in the inspected screenshots.
- The car is recognisable and the chase view, road, reference overlay and state panel are readable. Cedar's flat ground, floating boards and simple pits remain test scenery. No vehicle/world art changes or new visual-fidelity score are claimed.
- The slow run was intentionally closed with Alt+F4 after the initial rejoin. It is excluded from full-lap acceptance. The full normal-speed run and separate headless suites provide completion evidence.
- That Alt+F4 close reached input-system shutdown but left the review process/window alive across subsequent checks. Its exact executable/PID was verified and the visual-only process was terminated. Retain this as a review-launch shutdown defect; do not describe it as a normal successful exit. Normal automated exits are checked separately in the suite.

The technical-trial launcher now supports `-PlaybackRate 0.25` through `4`. Existing defaults remain real time for visible runs and 4x for hidden runs. Recorded human comparison laps reject any rate other than 1. Quarter-speed observation cannot serve as a real-time driving-feel or rendering-performance benchmark.
