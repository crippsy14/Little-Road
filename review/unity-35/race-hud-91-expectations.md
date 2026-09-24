# Retain actual stop timing in the HUD91

89 renders the first coast-style dashboard and timing panel and preserves88 recorded racing exactly on both tracks.90 adds L3 compact control, clearer pedal labels, manual-start/recovery prompts and a clean-view benchmark option. During review, a leading-car stop between4m timing markers was identified as a source of distorted interpolation. Neither89 nor90 is final timing acceptance.

91 replaces those coarse markers with a fixed36,002-observation ring per entrant, roughly five minutes at120Hz and6.6MiB for24 cars. Stationary observations do not consume repeated entries; restarting records the departure separately from the original arrival. Common-position time lookup uses binary search and interpolation only between actual motion observations. Reverse/reset clears the ring, missing old history remains unavailable, lap/finish handling stays explicit. This is read-only telemetry and does not change driver controls or physics.

Add a causal stop/restart fixture: leader0→10m at1s, stationary until20s, then20m at21s. Its passage at9m must remain0.9s throughout; passage at15m must be20.5s. Preserve all earlier signs, trailing-stop, recovery, finish, rank/window and drive-selector checks.

Final checks: build fixtures; actual HUD/L3 renders, exact recorded race parity against88/89 on both courses; fixed1920×1080 normal-speed clean-view off/on pair without screenshot captures during measurement. Report full frame-time distributions and AI costs separately; a single pair is a scoped observation, not proof of production frame budgets. Screenshots and synthetic L3 are not physical DualSense/user visual acceptance. Continue the full UI redesign next; do not call this the complete visual milestone.
