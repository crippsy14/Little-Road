# Prototype 81: first 24-car dynamic-path result

The unchanged original grid finishes **30.409241 car-seconds faster** with dynamic paths: 2209.831879 to 2179.422638. The rear-grid preview improves **3.874054 seconds**, from 90.483315 to 86.609261, and finishes fourth instead of seventh. All 24 finish. These are measured first-lap results, not a promise of the 80-second target or acceptance for the adventure build.

## Scope and integrity

473 build checks pass. The 146-file source archive and assembly manifest identify the executable. Relative to frozen 80 runtime code, only `RaceTrial.cs` changes, allowing a guarded explicit Cedar context field study above three cars. The planner, physical forces, equipment, grid, seed, drafting and open-space behaviour are unchanged. Paired off/on runs use seed 1709, the original mixed street/club grid and club car 0 at the rear; no opponent overrides or car-specific advantages.

The disabled run matches 71's race-active samples, events and official results. Raw motion parity remains marked failed because 71 contains 24 extra samples after the last finisher; all 2400 shared motion samples are exact, with the supplemental terminal-tail explanation recorded separately. No historical failure is overwritten.

## Paired hidden result

| Measure | Reference | Dynamic |
|---|---:|---:|
| Finished | 24/24 | 24/24 |
| Combined official time | 2209.831879 s | 2179.422638 s |
| Preview time | 90.483315 s | 86.609261 s |
| Preview final position | 7 | 4 |
| Preview first checkpoint rank | 22 | 24 |
| Preview sustained passes of initially leading street cars | 12 | 13 |
| Last finisher | 99.956001 s | 98.706306 s |
| Impact participant records | 10 | 6 |
| Peak impact relative speed | 7.627289 m/s | 10.429023 m/s |
| Body departure samples / physical recoveries | 0 / 0 | 0 / 0 |
| Unresolved planning samples | 781 | 371 |
| Braking sample fraction, all active observations | 6.934% | 8.190% |
| Hidden whole-AI mean per snapshot | 3.587961 ms | 6.329354 ms |
| Warm measured driver allocation | 0 bytes | 0 bytes |

Both arms pass 12/12 field checks and all 13 checkpoint deadlines. Sixteen cars improve and eight lose time; collective gain is not universal individual improvement. The preview's launch/first sector still needs work despite its better finish. Braking increases, and the narrow held-behind proxy rises from 3.400 to 3.600 seconds; neither observation by itself proves unnecessary caution. The small-case combined gains cannot simply be multiplied by 24.

The dynamic run contains three paired car-contact events. The strongest is cars 19/12 at 83.268410 seconds, near progress 1850 m, with recorded relative speed 10.429023 m/s. All continue and finish; no sampled body departures, recovery or pose penetration occur. Nevertheless, this exceeds the predeclared non-increasing severity screen, so the aggregate exploratory screen **fails**. Capture that encounter before attributing blame or relaxing safeguards.

## Limits and next work

This build is not promoted; the desktop shortcut and adventure build remain unchanged. Prototype 80's one controlled shortlist miss and two strict pass-confirmation deadline failures remain open. Full-field unresolved decisions are reduced but not eliminated. Dynamic physical forecasts are synchronous; hidden whole-AI cost rises about 76.4%. Zero allocation does not establish frame-rate acceptance.

A separately labelled `view-on` run finished normally and reproduces all race-active motion, track/context samples, incidents, checkpoints and official results. Raw parity remains failed only for a 24-row post-finish motion tail, explicitly explained in its supplement. User judgement of natural movement is separate from this numerical agreement.

The replay starts at 1920x1080 windowed, then switches to 3840x2160 borderless fullscreen; logs and frame dimensions record both. It therefore is **not a fixed-1080p benchmark**. Across the mixed-resolution capture, frame p95/p99 are 74.879/107.986 ms and whole-AI p95/p99 per rendered frame are 72.743/101.233 ms. The frame and AI budgets fail substantially, with zero warm measured driver/worker allocation and no dropped audit frames. AI time includes multiple simulation ticks and joined work per rendered frame; the timings demonstrate serious CPU spikes, not a clean estimate of resolution cost. Preserve the frozen driving result while moving expensive forecast work off the serial critical path; verify behavior equivalence and repeat a controlled rendered capture before claiming smoothness.

Next priorities: review the user's impression and rendered frame costs; capture the strong contact and residual unresolved states; improve shortlist handling using viable current/fresh paths without relaxing physical checks; address serial forecast cost with measured, parity-preserving changes. Then repeat the original and changed equipment grids plus controlled regressions. The approximately 80-second preview, personalities, adventure integration and the full twelve-hour game scope remain incomplete.

Evidence: pre-run expectations; paired raw runs and `comparison.json`; field, checkpoint, preview, braking and driver-frame analyses; disabled raw parity and terminal-tail supplement; runtime source-scope comparison; build log/checks, source archive and assembly manifest.
