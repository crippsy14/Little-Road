#109 exact swept-collision geometry reuse

Compute each car's perpendicular axis once per swept collision query instead of repeatedly normalizing the same cross product. Keep arithmetic order, intervals, thresholds, forecast cadence and driving decisions unchanged. Scheduling remains off in the matched benchmark to isolate this change.

Before accepting: compare at least40,000 deterministic varied/degenerate swept queries against frozen108, including exact collision fraction; verify zero warm allocations and existing editor checks. Run108 then109 sequentially on Bayview and Cedar, normal visible1080p, same24-car seed1709 mixed field. Require all24 finish, normal exit, no runtime errors, complete capture, zero warm driver allocations and exact recorded racing/diagnostics. Preserve any contact-callback parity failures rather than relaxing the test. Record mean whole-AI wall cost and frame p95/p99; do not claim a gain unless the matched measurements support it. Existing16.7/33.3ms frame and3ms AI frame p95 targets remain open.

The user reports108's preview is much smoother. This is subjective presentation feedback, not acceptance of all scheduler incidents or broad field-pace improvement. Preserve108 as a separate opt-in experiment.
