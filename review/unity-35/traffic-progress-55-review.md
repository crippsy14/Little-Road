# Traffic-aware progress55: stronger preview passing, optimisation required

55 ranks the existing proposal and one physically checked continuation using the shared live following controller and an owned observed-traffic rollout. It is enabled only by `--compare-continuation --traffic-aware-progress`. No equipment, gap margin, grid, live braking rule or physical control benefit changes.332 build checks pass. Disabled55 preserves corrected53's stored behaviour exactly, including runtime-error checks. Source and hashes are in `source-55` and `prototype-55-manifest.json`.

| Seed1709,24 cars, one lap | Nominal53 control | Traffic-aware55 | Player-reviewed50 |
|---|---:|---:|---:|
| Finishers | 24 | 24 | 24 |
| Combined car-seconds | 2212.647858 | 2215.597137 | 2245.831421 |
| Preview CP1 / finish | 20 / 7 | 19 / 4 | 19 / 8 |
| Preview lap seconds | 89.508553 | 85.501198 | 91.666359 |
| Last finish seconds | 100.914101 | 100.830788 | 102.438728 |
| Large accepted target changes | 344 | 347 | 637 |
| Moving sampled time braking | 6.552021% | 6.874178% | 7.928846% |
| Pair contact events / peak relative speed | 4 / 4.617481m/s | 5 / 8.087913m/s | 4 / 8.755849m/s |
| Body-outside samples / recoveries | 1 / 0 | 0 / 0 | 0 / 0 |

55 retains30.234283 combined seconds of improvement over the version the user watched, while the faster preview car gains20 places and finishes6.165161s sooner. It is2.949280 combined seconds slower than53. The aggregate-time and preview metrics tell different useful parts of the story; neither alone defines race quality. All12 field checks and13 checkpoint deadlines pass. The unchanged2215 target is missed by0.597137s; the6m/s impact review threshold is exceeded. Occasional contacts remain allowed; no blanket zero-contact field requirement is introduced.

## Controlled121/122

All ten scenarios have zero runtime exceptions, contact, sampled departure and recovery. The independent narrow-corridor contract now passes13/13: its unresolved planning sample is gone. The remaining failure is the same fixture's completion-time allowance: car0 finishes33.266548s, versus53's32.591404s and50's31.266289s. The relevant changed passing decisions occur after leaving the wall section. Do not describe this as a collision or a stuck-car failure, and do not silently relax its deadline.

Traffic scoring runs893 comparisons in the field, accumulating7282.7349ms in the diagnostic hidden run. It performs two own-car rollouts and a shared-for-the-pair copied rival rollout per comparison. This cost is included in the policy's normal timing; do not add it to that total again. More accurate traffic scoring has not demonstrated an across-the-board pace improvement, and fixed observed rival motion still limits its accuracy.

## Visible performance check

The normal-speed windowed run (`traffic-progress-55-visible`) disables switch, continuation and pedal-stage capture. It reproduces the diagnostic race's stored motion, events, samples, context samples and results exactly, with no runtime errors. All9452 post-loading frames were captured; warm driver and forecast-worker allocation counters are zero.

The run starts1920x1080 and later records3840x2160. It is not a matched fixed-resolution benchmark. Across the mixed run, frame p95=28.676403ms and p99=59.319897ms; all-AI p95=26.7141ms and p99=57.3968ms. The original frame and3ms AI budgets fail. The loading-adjacent maximum564.919983ms remains recorded. Per-resolution slices are descriptive, not comparable controlled trials: the1080p segment covers countdown through14.19s, the4K segment later racing. Spikes occur in both, so a resolution change alone does not explain the AI timing.

The user subsequently reviewed the moving race and said: “That looks the best yet again! These improvements definitely seem to be doing the trick!” This makes55 the latest player-approved race-feel reference;50 remains a useful earlier comparison. This feedback does not establish that the measured performance failures are resolved. At the user's explicit request, the desktop Racing Prototype shortcut now launches55 with the same normal-speed, windowed24-car settings. Replay output goes to `review/desktop-prototype-replay`, preserving the benchmark evidence. The previous shortcut is backed up as `prototype-shortcut-before-55.lnk`; verified settings are in `desktop-shortcut-55.json`.

Next profile the expensive prediction/traffic-preparation and serial planning work, preserve same-step decisions and exact recorded race parity when optimising, then repeat clean fixed-resolution performance. Keep the modest post-wall timing regression and contact severity visible alongside larger goals; do not spend indefinite effort forcing one metric green while ignoring race feel, performance or campaign work. Personality differentiation, race-preview UI, adventure integration, wider scenario coverage and campaign scope remain open.
