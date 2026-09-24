# 120: pace margins (ai-racing-review goal 3): strong on Cedar, not yet safe on Bayview

Dev build, 535 checks (+2 `PaceMarginChecks`, which pin the defaults). All AI grip and braking use now lives in `PaceMargins` (was scattered constants). `--pace-margins` raises one step: cornering 0.78 → 0.84, steering cap 0.85 → 0.90, racing-line braking 0.85 → 0.90, driver braking fraction in path envelopes ×1.1 (≤ 0.95).

Screen, seeds 1709 + 1710 (off arm reproduces earlier baselines):

| Track | Collective delta 1709 / 1710 | Mean | car0 |
|---|---|---|---|
| Cedar | −13.96 / −25.34 s | −19.65 s | +5.07 / +0.08 s |
| Bayview | −13.09 / +31.29 s | +9.10 s | −0.64 / +11.44 s |

All 24 finish, no assisted resets. Bayview 1710: car0 went off (16.5 s physical recovery), finishing last (46.5 s vs 35.1 s).

Reading: a large, consistent gain on Cedar. On tighter Bayview the extra grip use removes the margin, and one mistake costs more than the gain. Next try: a gentler step (e.g. cornering 0.81, braking ×1.05), or scale the step by track/driver. Off by default.
