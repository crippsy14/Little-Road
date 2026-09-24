# AI input policy extraction and racing-line research

21 September 2026. Phase A of the user's exact external [AI implementation plan](<C:/Users/cripp/Documents/ChatGPT/Road Trip/docs/racing-ai-implementation-plan.md>). Broad goal stays active; no new driver abilities or campaign content are claimed.

## Changes

`IDriverPolicy.Decide` receives a timestamped, stack-scoped read-only observation and emits bounded signed pedal/steering plus diagnostics. `LegacyDriverPolicy` owns the previous planner's internal state and RNG. It has no car, transform or Rigidbody reference. `RoadCar` still owns forces, shared grip, steering response, progress, contacts and recovery. The proving-ground `commandSource` and keyboard/controller paths are retained.

The default compatibility observation captures the same cached traffic fields as the old driver. Separately, `--shared-driver-snapshot` selects one race-level pre-controller observation of every car, in stable driver-ID order, using recomputed position/projection/speed. `RaceWorldSnapshot` has explicit earlier execution order and a reusable fixed-capacity buffer; the read-only span cannot be retained across ticks. The mode is recorded in the race report and is developer-only, not silently switched into the adventure. New policies should use the common snapshot; legacy mode exists for matched comparisons.

Build 02: `outputs/little-road-ai-policy-fields-review/Little Road Unity.exe` (relative to workspace). Assembly SHA256 `9781254CA869523ED661D23E392A2304EE3585553303A474B3E66234DA308BB9`. The normal desktop adventure remains checkpoint 20; the user's Cedar replay still launches the frozen pre-extraction `little-road-ai-review` candidate. No running user build was overwritten.

## Finding and correction

Build 01 compiled and passed 201 editor checks. Its stock/club three-lap times matched the frozen Cedar baseline, but sampled numerical differences grew to roughly 1.6 mm lateral deviation. In a 24-car field, interaction histories diverged: contact entries rose from 284 to 322 and some individual finish times changed by up to 3.199 s. Three independent launches of each build reproduced their own outcomes, so this was not dismissed as ordinary run noise. All cars still finished without assisted resets or missed gates.

The strict failed comparison is retained in `unity-28/policy-exact-hypothesis.json`, alongside all build-01 runs. Restoring the original intermediate **field storage** for predicted offset, target speed and steering in the extracted policy removed the observed divergence. Final build-02 verification passes **35/35 checks** in `unity-28/policy-comparison.json`: stock and club Cedar runs plus all three independent 24-car Bayview comparisons have exactly matching timestamped sampled commands/motion and race outcomes, with zero finish-time difference. Bayview contact count returns to 284. Shared-snapshot mode also completes all 24 cars without recovery or missed gates and stays below the retained contact-depth limit. All launched test processes are terminal.

Post-finish absolute progress/offset at report-writing time are intentionally excluded from exact outcome comparisons: finishers continue circulating until the render-frame report runs, whose latency varies by a few physics steps even between identical old-build launches. The timestamped motion arrays, finish times, placement, lap/gate results and contact/depth outcomes are compared exactly. No broad time tolerance was introduced to hide the build-01 regression.

## Verification and limits

- Build 02 compiles with **201 editor checks** in `build-02.log` / `editor-checks-final.json`, including finite bounded commands, deterministic replay of frozen observations, and meaningful braking/steering responses.
- `bayview-before`, `bayview-before-r2`, `bayview-before-r3` are three frozen pre-extraction runs, 24 cars × 3 laps, seed 1721. Final counterparts are `bayview-fields`, `bayview-fields-r2`, `bayview-fields-r3`.
- `stock-fields` and `cedar-fields` are three-lap solo comparisons against `unity-27/solo-stock` and `solo-club`.
- `snapshot-fields` checks 24-car completion/progress/contact depth with the shared observation mode. This is not full racecraft acceptance or an AI CPU benchmark. Build-01 shared-snapshot Cedar/Bayview smoke evidence is also retained separately.
- Four stock/club U-turn/hill controls on build 01 remain valid and have exactly the checkpoint-20 times (`equipment-regression.json`). Build 02 changes only intermediate policy storage; controlled equipment fixtures bypass that policy. These controls do not replace the full earlier equipment matrix or prove every driver policy.

No new excellent racing line, physical recovery, passing planner, ability schema or error generation is accepted by this extraction. Existing contact-entry counts are not deduplicated collision episodes; racecraft/contact quality still needs substantial work. No manual controller or normal-window GPU performance acceptance is inferred from headless tests.

## User clarification and next work

The user correctly clarified that adherence must measure a **racing line**, not the road centre. [Doc 26](../docs/26-racing-line-research-and-reference.md) records primary coaching/research sources and the concrete plan: legal body corridor, candidate path generation, path-dependent speed profiles, calibrated acceleration/braking/turning, offline lap-time search and actual repeated physical validation. A best-known clean reference is not a claim of global optimality. Keep car/equipment dependence and traffic-intent exclusions explicit.

Next implement phase B's reference racing path, speed planner, follower and visible debug overlay. Preserve legacy mode and the frozen evidence. Do not repeatedly tune one test or invent a reference by labelling the centreline ideal.
