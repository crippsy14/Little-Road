# Same-grid AI pace: traffic accounts for15.45 seconds

59 adds the `standing-solo` diagnostic fixture: car0 alone, in the same slot23 and pose used in the24-car pack, with normal standing countdown and complete checkpoint/lap accounting. It changes no AI behaviour.340 build checks pass; all19 paired diagnostic checks pass. Actual first recorded position, heading, progress and road offset match exactly, as do car tuning, driver planning and shared configuration.

| Club car0 | Solo |24-car field |
|---|---:|---:|
| Start-line crossing |3.125025s |5.358349s |
| Checkpoint4 |26.341364s |37.134041s |
| Finish |70.046638s |85.501198s |
| Contact / recovery / missed gates |0 /0 /0 |2 /0 /0 |
| Same-tick eligible pedal samples |699 |850 |
| Braking samples below−.01 |36 |50 |

Removing traffic saves15.454559s. By checkpoint4 the field has lost10.792677s;2.233324s is lost just reaching the start line. Every gate is passed in order. Solo has no sampled road departure. Both runs exit normally without runtime exceptions and use autonomous input throughout. Hidden diagnostic timing is not rendered performance evidence.

All solo braking samples originate with the path follower. Of the field's50 braking samples,24 have their final limiting stage in moving-leader control,8 in immediate safety and18 in the follower. Five braking samples occur while a path is unresolved (an overlapping classification). These counts identify applied stages; they do not establish which brakes were unnecessary. The bigger time loss is not explained merely by a large increase in braking duty.

The field exactly matches57's checkpoint passages, impacts, overlaps, recovery events, technical samples, context samples and car results. Its raw motion array contains24 additional records at101.005745s, after the100.830788s final finish. The original raw parity failure remains; `post-finish-motion-supplement.json` proves the entire earlier prefix is identical and all extra records are post-finish.

## Early passing observation and next experiment

In39 car0 samples before40s, the chosen passing destination is at the6.29m outer sampling boundary while moving-leader control cuts the follower's requested pedal by more than.05. At1.000011s, it wants full power toward offset6.29 but remains at3.533188m, only about3cm sideways from its3.5m starting position. Its3.229805m/s speed is capped by a3.478424m/s leader limit and pedal falls to.098999. Around7.7–8.9s it again targets6.29 while restricted to roughly25m/s behind car21.

Source inspection shows each accepted path is freshly anchored with a22–60m transition; accepting it resets consumed distance to zero every planning update. This is a plausible contributor to slow execution of an otherwise stable passing intention. It is not yet proven by a changed-controller comparison. Do not remove leader/emergency control to force acceleration through an occupied space.

Next test a bounded, revalidated continuation of the *actual in-progress trajectory* when the preferred destination is unchanged, rather than repeatedly rebuilding the start of that same manoeuvre. Existing55 continuation retains a destination but still freshly anchors its trajectory, so it does not answer this question. Forecast the retained geometry from the current measured car state and updated consumed distance; road/body, scenery, adjacent/rear traffic and side-entry checks must still pass at the current observation. Changed or unsafe opportunities must allow replanning. Live following/emergency controls remain authoritative. Prepared forecasts and synchronous fallback must construct identical candidates, and committing a retained path must preserve its consumed distance.

Predeclare a disabled-baseline parity check; first use low-speed blocked-leader/passing and launch situations to check lateral response, physical feasibility and passing time. Then use the existing merge, no-room, corner and full-field cases, and the same-grid solo as a clear-road regression. Capture actual selection/continuation decisions; keep old checks and collision/pace thresholds. Compare collective pace, preview checkpoints, contact consequences, road departure/recovery, target reversals and whole-AI cost. A greater willingness to complete a pass must not become an inability to abandon a stale one. This is an experiment, not approval to bypass road limits or adopt a permanent rail.

## User steering retained

The user explicitly asked to park hitbox work and focus on AI.58's editor-only footprint export is retained as unfinished evidence; no colliders were changed and no further hitbox work is scheduled now. The manual readiness gate built in58 passes unit/build checks, but the user stopped Computer Use before focused start acceptance could be checked. That UI acceptance remains open; do not keep relaunching manual windows for it while the user wants AI progress.

The user's67.46s win deliberately uses two wheels beyond the asphalt edge. It is2.591034s faster than this solo AI, but that is not an equal-rules comparison or a target lap the AI must copy. The permitted-edge/surface/risk plan is now in `docs/34-driver-identity-prototype.md`. Performance spikes, driver identities, preview UI, adventure integration and full campaign requirements remain open. Desktop reference stays55.
