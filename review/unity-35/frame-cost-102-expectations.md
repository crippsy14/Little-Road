# 102: remove redundant ranking/forecast computation

Prior101 showroom pass is progress: authoritative code, visual acceptance and runtime evidence. No active process remains at start of this pass. The full campaign remains incomplete; latest sequencing prioritises choppiness now.

95's valid1080p HUD-on evidence has69.90ms frame p95 and67.51ms AI-per-frame p95; median frame8.33ms. AI planning bursts dominate the tail in that evidence; HUD on/off is similar. Target repeated calculations rather than change physics rate, planning frequency, traffic constraints or racing personalities.

102 lazily fills the original half-metre forecast geometry table, with identical arithmetic, table window and per-request invalidation. Ranking precomputes traffic route-relative velocities/distances once per immutable observation instead of each downstream sample. Own buffers; no cross-car/snapshot reuse. Added geometry oracle checks exact eager interpolation across changed paths and track seam; existing dynamics checks retained.

Run498+ editor checks, freeze102, compare101/102 controlled full-field Bayview and Cedar stored motion/events/results. Sequential normal-speed nondevelopment1920x1080 HUD-on rendered runs with no competing editor/simulation or screenshots during timing. Include startup/countdown after only the first loading observation; report mean/p95/p99, worst one-second window, whole-AI cost and allocations. Retain failures and distinguish CPU improvement from meeting budgets. Inspect actual moving gameplay before any subjective smoothness claim. No shortcut promotion or campaign completion claim.
