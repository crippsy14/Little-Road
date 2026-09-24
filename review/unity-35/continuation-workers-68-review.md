# Prototype68: bounded continuation forecasts on existing workers

67's rendered evidence put most slow-frame AI time in main-thread decisions. Its24-car field performs6,942 synchronous road checks, including1,024 continuation attempts.68 adds opt-in `--parallel-continuation-forecast`: when the prospective first target triggers the existing large-switch comparison, prepare at most one fresh continuation to the previous target on the existing forecast workers. Ordinary candidate order and all later checks remain unchanged. This is speculative preparation, not a new path choice or delayed decision.

The additional request owns separate prediction buffers and the existing exact ForecastRequest identity. It is joined in the same snapshot before live driver decisions, consumed at most once on an exact match, and discarded on a new primary preparation. Unmatched/undispatched/stale requests compute synchronously. Traffic-constrained fallback bypasses and invalidates the nominal secondary result. Actual road-envelope, moving-side-entry, corridor and static-scene checks still run on the main thread. No scenery query runs on a worker. Shared job capacity is48 for24 cars; worker count stays8. Driver control and planning times are unchanged.

402 build checks pass, including seven new secondary-result checks: serial acceptance/progress, one-use semantics, stale/changed requests, current corridor enforcement, undispatched/superseded requests, following-model separation and zero warm allocation. The first compile attempt caught a test helper capturing a ref-struct observation; the helper now takes that observation explicitly. The failed compile log is retained. The succeeding build and frozen source/assembly manifest are authoritative.

## Hidden comparison

Both runs use the same68 assembly, seed1709,24-car Cedar field, rear-grid club car, mixed equipment, all-car following fallback and shared observed-rival preparation. Detailed event audits are disabled. Disabled68 exactly reproduces67's recorded race. Enabled68 exactly reproduces disabled68's motion, events, technical/context samples and race results. Both exit normally with no detected runtime errors. No post-finish motion supplement is needed.

| Measure | Off | On |
|---|---:|---:|
| Whole-AI mean per snapshot |3.248644ms |3.146408ms |
| Main decision region mean |2.726069ms |2.584272ms |
| Secondary prepared / consumed |0 /0 |1288 /992 |
| Synchronous road checks |6948 |5951 |
| Accumulated synchronous/check lookup forecast time |10755.19ms |9022.79ms |
| Sum of worker elapsed time |39583.76ms |41479.76ms |

Whole-AI time falls3.147%, while the main decision region falls5.202%. The joined snapshot cost increases and is included in the whole-AI result. Worker elapsed time is a sum across concurrent jobs, not CPU utilisation or an extra value to add to joined wall time.992 of1288 extra predictions are consumed (77.0%); the remainder are speculative work. Absolute preparation/check counters include a different number of terminal processing ticks despite exact exported race parity; use per-snapshot whole-AI values for the cost comparison rather than assume the997-check difference all arises from992 hits.

All24 cars finish in both with identical combined2207.115875s and car0 fourth at87.650673s. Warm driver/snapshot/worker managed allocation and dropped frame records are zero. This is one matched seed and hidden4x playback, not rendered acceptance or broad performance confidence. Existing racing/contact/controlled timing and isolated-scope boundary limitations remain open. The optimisation stays opt-in and desktop55 stays unchanged.

## Normal-speed verification

The matched fixed1080p1x pair is complete, running enabled first and control second. Both remain1920x1080, with zero warm measured allocation/dropped records, normal exits and no detected runtime errors. Enabled visible output exactly reproduces hidden output and the visible control across all recorded motion/events/context/results. Whole-AI mean per snapshot falls3.286924ms to3.170421ms (3.54%). Frame p95 falls27.5926ms to25.6524ms and p9950.1353ms to47.2017ms. AI p95 falls25.5564ms to23.7694ms. All original AI/frame tail budgets still fail. Keep the optimisation opt-in; it improves this pair but is not a completed performance milestone. These single-pair measurements do not establish statistical confidence.

The user then observed leaders slowing/moving aside for approaching rear cars and prioritised leader entitlement to a clear line until a rival is alongside.69 investigates this behaviour next. Initial constructed observations reproduce an unwanted3.3m target displacement for a same-corridor car5-7m behind, even when the rear car is slower. They do not reproduce braking or establish the cause of the exact watched incident. Physical merging/alongside constraints must remain. Hitbox work stays parked, and broader personalities/race-preview/adventure/campaign requirements remain open.
