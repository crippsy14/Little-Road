# Transparent race HUD, build 94

23 September 2026. Retain this as the first transparent driving-HUD implementation. The user reviewed the gameplay image and confirmed: “Yes—continue refining this direction.” This accepts the visual direction, not every outstanding usability or performance requirement.

## Delivered

Live speed, honest automatic D/R, applied throttle and brake/reverse, steering input, position, lap count, current lap, personal best and total race time. The relative panel uses the actual roster and shows eleven entrants by default, with a five-entry H/L3 option. White shadowed text and small translucent backing replace the initial cream cards. Preview and results hide the driving HUD.

The timing helper retains bounded actual passage history, handles stopped cars without extrapolating distance/speed, invalidates recovery/reverse discontinuities, labels lapped cars and uses actual finish order. Unknown or expired observations remain unavailable. The entire numbered reference is mapped in document 40; fuel/tyres/pits and other future modules remain required where specified, without fabricated current gauges.

## Verification

- Nondevelopment Windows build: 496 editor checks; 157 source/script inputs frozen in source-94 and prototype-94-manifest.json.
- Bayview and Cedar exactly match build 88's stored motion, checkpoints, contacts, recoveries, context samples and race results. This verifies the presentation change against these recorded fixtures, not all possible races.
- Synthetic L3 toggles five/eleven rows successfully in the actual player. Gameplay and compact screenshots inspected at 1920×1080; the user accepts this transparent visual direction. Physical DualSense operation, smaller windows and additional lighting conditions remain open.
- Matched clean-view HUD-off/on runs were sequential, normal-speed, nondevelopment, 1920×1080, with no competing project editor/simulation and no screenshot capture during measurement. Stored race behaviour matches exactly; both frame captures are complete.

| Frame time | HUD off | HUD on |
|---|---:|---:|
| Mean |20.482 ms|20.619 ms|
| p95 |70.096 ms|69.765 ms|
| p99 |116.135 ms|119.489 ms|

This single pair shows similar overall frame times, not a reliable isolated HUD-cost estimate. Both fail the existing 16.7 ms p95 / 33.3 ms p99 frame budgets and 3 ms AI p95 budget. Large spikes remain present. Zero observed allocations in measured driver regions does not establish zero UI allocation.

Connected adventure/retry/save-reload completed with **17/18** analysis checks; see race-preview-94/adventure-analysis.json. Both three-lap races finished 24/24 with all ordered gates, real owned equipment, rear starting slot, retry cancellation/confirmation and correct payment. Automated player: event 1720, 23rd in 94.740608 s, 100 credits; event 1731, 16th in 92.807747 s, 130 credits. Wallet 230, quest state and fitted upgrade persisted in the separate-process reload. These connected outcomes differ from build 88; exact parity is claimed only for the controlled standalone fixtures above, not the connected smoke.

The failed check remains race2NoTeleportRecovery. Cars 9 and 18 again require assisted resets near 250 m, around 93 s, after persistent obstruction/recovery; reset progress moves backwards. This is the known recovery defect, not an accepted marginal excursion. Both processes exit normally without runtime errors. Adventure gameplay and result screenshots were inspected: the driving HUD is present during racing and hidden on results. Existing result buttons have poor light-on-light contrast, and the old top bar continues showing elapsed time after the player's finish; carry these into the menu redesign rather than describing results styling as accepted.

## Historical candidates

- 89: initial opaque cream HUD, 494 checks, exact Bayview/Cedar parity. Its coarse timing bins were superseded because prolonged stops could distort historical passage times.
- 90: input/notice improvements and successful synthetic L3 check, 494 checks. Its performance pair is invalid for overhead claims: editor 91 overlapped the end of view-on. Do not reuse those timings as clean evidence.
- 91: actual-motion timing history, 495 checks; build-only, no runtime acceptance.
- 92: failed check fixture; near-identical float values collapsed to the same value. Failed fixture preserved as race-timing-checks-92-failed.cs. No successful exported candidate claimed.
- 93: corrected fixture, 496 checks; build-only, no runtime acceptance.
- 94: current transparent presentation and genuine lap/best timing, 496 checks; evidence above.

No desktop shortcut promotion. Full playful menu redesign, track map/lap history, discrete transmission telemetry, endurance displays, physical-controller review, performance and campaign scope remain unfinished. The full campaign goal remains active.
