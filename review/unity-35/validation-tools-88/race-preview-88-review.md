# Connected roster, preview and adventure88

The first personality/preview/adventure implementation is complete in frozen88. It is an intermediate playable milestone, not full campaign completion or unconditional race-quality acceptance. The user requests moving next to the virtual dashboard, live relative standings and playful UI redesign in document39.

## What changed

One immutable roster owns all24 entered cars, actual seven-part loadouts, derived tuning, equipment-only dry-road rating, body/paint, driver snapshot and starting slots. Preview and spawning use the same entries. Opening/cancelling a preview cannot reroll race form, modify equipment or consume a saved race serial. The player starts24th. Changing fitted equipment invalidates an old proposed entry.

The pre-race display has three pages, actual selected3D car models, all fitted part names, rating and implemented driver traits. Keyboard/D-pad navigation and shoulder/page controls are available. Countdown pauses while viewing. Human players are described as controlling their own performance rather than having AI form applied to their input.

Adventure entry uses the retained modern AI, drafting and first personality slice, with an explicit developer legacy fallback. Normal exploration does not inherit those AI flags. The purchased player configuration is preserved. The88 follow-up fixes87's Race again guard: settled events can enter a new roster, with a new preview and correct cancel/confirm behaviour. Cancelling restores the original paid result without making a second payment. Unsettled entries cannot be confirmed twice.

## Evidence

- Build:488 editor checks, nondevelopment Windows export,153 archived source/script inputs, assembly fingerprint in prototype-88-manifest.json.
- Both standalone mixed-field comparisons exactly match87 stored motion, gates, impacts/recovery, context samples and race results.87 previously matched86 on both tracks. Profiling cost fluctuations are not a new optimization claim.
- Preview:200 actual runtime data/input checks pass. All24 displayed loadouts/ratings agree with spawned entries; paused inspection, D-pad, L1/R1, PageDown and Cross confirmation work. All24 finish after confirmation, with no runtime exceptions.
- Connected fresh profile: delivery, two finds, buying/fitting the workshop engine, preview and a24-car three-lap race; retry preview, cancel/reopen without reroll/payment, one new serial on confirm, second24-car three-lap race, return to town, then new-process save reload. Both processes exit normally and reject duplicate payments.
- Screenshots inspected: player/rival preview, connected retry preview and saved result. Text and model remain readable without clipping at1920×1080. Uneven part-name column spacing and block styling are cosmetic limitations for the next redesign, not final art acceptance.

| Connected event | Seed | Finishers | Automated player | Player time | Reward |
|---|---:|---:|---:|---:|---:|
| First |1720|24/24|17th|93.091011s|130|
| Retry |1731|24/24|16th|92.782753s|130|

Wallet260, race/settled serial2, purchased engine and completed delivery/finds persist exactly through the separate-process reload. These are automated driving results, not a claim about the user's pace. The first event has two participant contact records, max impact2.360m/s and one physical recovery. The second has four physical recovery entries and two assisted resets; no participant contacts are recorded.

## Remaining issues and acceptance

The connected analyzer is **17/18**, retaining race2NoTeleportRecovery=false. Drivers9/18 stall near course250m and need assisted resets; driver8 also recovers. All37 ordered passages per car are recorded, reset gate5 remains unchanged, and repositioning moves backwards, but this is still a prolonged racing-quality issue. See [recovery evidence](adventure-recovery-88.md); do not classify it as the accepted occasional marginal road-edge excursion or suppress it to declare the test clean. No global AI-caution retuning was added to hide it.

Physical DualSense/user usability, subjective mixed-personality review, stock connected racing, broad equipment-rating calibration and integrated rendered performance remain open. Accelerated adventure frame times are not valid presentation-performance measurements.85's bounded worker optimization improved measured overall AI cost but did not meet the original frame budgets. The40-run form follow-up in driver-form-86-review.md shows only tiny lap dispersion; broader skills, finishing-rank variation, teammate learning and familiarity remain future work.

Desktop adventure/prototype shortcuts remain unchanged.88 is retained for the next visual milestone with its explicit recovery issue; no claim of production promotion. The full12-hour campaign goal remains active.

Evidence root: race-preview-88. Exact commands are recorded per run in launch.json; exits in exit.json. Immutable source, tools/resources, build checks, reports and screenshots are retained alongside this review.
