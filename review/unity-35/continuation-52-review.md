# Continuation52: useful pace evidence, not selected

52 implements one extra checked previous-target continuation before a large preferred target switch, enabled only by `--compare-continuation`. It chooses the alternative only when nominal two-second progress improves by more than a whole own body length. It does not force a finish position, grant equipment performance, remove live leader/emergency controls or change the grid. See declared `continuation-52-expectations.md`,51's exact counterfactual evidence and `source-52`/manifest for implementation.

326 build checks pass. Disabled52 exactly reproduces all stored50 combined motion, events, samples, context samples and car race results. No post-finish parity exception is needed this time.

| Seed1709, 24 cars, one lap | Reviewed50 control | Continuation52 |
|---|---:|---:|
| Finishers | 24 | 24 |
| Combined car-seconds | 2245.831421 | 2212.647858 |
| Last finish seconds | 102.438728 | 100.914101 |
| Preview CP1 / finish | 19 / 8 | 20 / 7 |
| Preview lap seconds | 91.666359 | 89.508553 |
| Accepted large target changes | 637 | 344 |
| Dropped switch captures | 0 | 0 |
| Moving sampled time braking | 7.928846% | 6.552021% |
| Pair contact events / peak relative speed | 4 / 8.755849m/s | 4 / 4.617481m/s |
| Body-outside samples / recoveries | 0 / 0 | 1 / 0 |

52 attempts1171 continuation comparisons and chooses837. Aggregate time improves33.183563 car-seconds (about1.48%), meeting the unchanged2215 target. All13 checkpoint deadlines, preview progress and contact review thresholds pass. One sampled body boundary excursion fails physical completion and the field screen. Fewer large target changes and less sampled braking are useful behavioural evidence, not a claim of visually better racing or proof that every remaining brake command is necessary.

## Controlled regressions:97/102, five failures retained

- No-room completes without contacts/departure/recovery and slightly faster than50, but has9 unresolved active samples; existing independent corridor contract12/13 fails.
- Merge-left: both finish, no contact/departure, and the faster rear car passes by3.366695s. However, the leading car needs10 physical recovery episodes and finishes109.212074s versus50's29.516315s. Clean completion and delay checks fail. This is a serious regression, not acceptable variation or a reason to widen the test limits.
- Corner-three: car0 still finishes60.172295s, but the two slower cars have one pair contact at6.4s (2.754814m/s), recovery/reset events and finish145.728165/152.326553s. Clean completion and delay checks fail. A minor bump alone would not justify rejecting general field racing; the resulting recovery and delay failures do.

The feature remains opt-in and is not selected. Keep50 as the strongest player-reviewed natural-racing reference, with its documented pace/contact limits; desktop43 remains unchanged. Do not launch52 as a proposed replacement on the strength of its full-field total alone.

## Next evidence and work

Capture chosen continuation overrides in the failing merge-left and corner-three fixtures, including both forecasts and the previous path. The current switch audit only records accepted large final changes, so an override that retains its target is deliberately absent and needs a separate labelled event. Identify the first changed decision and the subsequent braking/recovery cause. The nominal own-car progress estimate omits same-corridor leader control; overvaluing unusable forward progress is a hypothesis, not yet an established cause of these failures. A comparison must account for usable traffic space and preserve route/steering continuity, not simply reward the largest free-car forecast gain.

Retain fixed cars, scenarios and deadlines. Use the player-reviewed50 baseline and the existing controlled tests for the next candidate, then paired seeds and a clean1920x1080 real-time frame/AI allocation check before broader promotion. Personality variation, race-preview UI, adventure integration and the full campaign requirements remain open.

## Subsequent correction53

The controlled52 logs reveal repeated NullReferenceException from an unguarded optional switch recorder; the audited field did not have that error. Original results remain above, but the recovery failures cannot be attributed to the driving rule as initially hypothesised.53 corrects the recorder, reproduces the faster field exactly without auditing, and passes110/112 stronger controlled checks with no exceptions/contact/departure/recovery. Only no-room timing and one unresolved plan remain. See `recorder-53-review.md` and the preserved additional `continuation-52/regressions/runtime-exception-review.json`.
