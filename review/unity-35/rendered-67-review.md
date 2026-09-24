# Prototype67 normal-speed rendered comparison

Both runs use the unchanged67 assembly, Cedar24-car rear-grid club versus mixed field, seed1709, risk5 and all-car following feasibility fallback. The only driving-system option changed is exact shared observed traffic preparation. Both run windowed1920x1080 at1x,120fps cap, vsync0, non-development, with detailed event audits disabled. Each owned process exits normally; no runtime errors are detected. No computer-use automation was invoked and the desktop shortcut remains55.

## Results

| Measure | Serial preparation | Shared preparation |
|---|---:|---:|
| Post-loading captured frames |9521 |9755 |
| Mean observed frame |10.8943ms |10.6317ms |
| Frame p95 |28.5607ms |27.4211ms |
| Frame p99 |55.9578ms |49.4897ms |
| AI p95 per rendered frame |26.7471ms |25.4733ms |
| AI p99 per rendered frame |53.6567ms |47.6020ms |
| Mean whole-AI per snapshot |3.404311ms |3.267092ms |
| Worst one-second mean frame |31.6995ms |26.4634ms |

Whole-AI cost per snapshot falls4.03%. Frame p95 improves3.99% and p9911.56% in this pair. These are observed values, not statistical proof across machines or sessions. Different frame counts reflect frame pacing, not different simulated races. Every captured frame stays1920x1080; both have zero dropped records and zero measured warm driver/snapshot/worker managed allocation. The normal-speed serial run exactly reproduces the hidden serial reference, and the normal-speed shared run exactly reproduces every stored motion/event/technical/context/result array from the normal-speed serial run. No post-finish supplement is needed for these comparisons.

All24 cars finish in both. Combined time remains2207.115875s and car0 remains fourth at87.650673s. This performance change does not improve or worsen the recorded driving. Prior contact, controlled timing and66 isolated-scope containment limitations remain unresolved.

The original AI p95<=3ms, frame p95<=16.7ms and frame p99<=33.3ms gates **all fail in both arms**. Shared preparation helps but does not fix visible frame spikes. Loading-adjacent maxima remain recorded (573.706ms serial and562.863ms shared); only the first loading frame is excluded under the established analyzer rule.

## Attribution and next action

The new offline `tools/analyze-ai-spikes.py` groups frames with/without a forecast batch and reports disjoint main-thread decision/snapshot wall regions plus existing stage totals. It does not modify runtime, redefine acceptance or add nested timers together. Unity delta time is an elapsed interval, so adjacent-frame duration is shown only as a diagnostic, not precise GPU/presentation attribution.

Every frame in each arm's slowest5% of AI frames contains a planning batch. Main-thread decisions account for86.67% of that group's AI wall time in serial and85.34% in shared; snapshot preparation and joined workers account for the rest. Shared planning frames average27.35ms of AI work, versus1.45ms on other frames. Exact shared rival preparation reduces accumulated traffic-progress rival preparation from2848.5ms to1565.8ms, while accumulated synchronous road forecast cost remains approximately10.7seconds per race. Remaining serial forecasting is a better target than additional cache-hit tuning or graphics changes based on these measurements.

Keep67's implementation available as an opt-in optimisation, with failed performance gates explicit. The next bounded performance experiment should reduce the remaining synchronous forecast work while retaining fresh observations, physical checks and chosen-path equivalence. Inspect the costly candidate/continuation forecasts before choosing how to batch or reduce repeated calculation; do not restore the already-rejected29 staggered scheduling change or lower control frequency. Moving player review remains separate from telemetry. Individual driver traits, race preview and adventure integration remain subsequent milestones, and the full campaign goal stays active.

Evidence: `rendered-67/analysis.json`; both raw run directories, launch/exit/logs, exact parity reports, `driver-frame-analysis.json` and `ai-spike-analysis.json`; existing frozen67 source/assembly manifest. No rebuild or engine-source change was required for this validation.
