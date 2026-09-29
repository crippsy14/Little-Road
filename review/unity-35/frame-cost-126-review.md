# Study 126: `--rejoin-at-speed` (screen) — REJECTED

**Date:** 29 September 2026.
**Build:** the dev build of 29 September. Its trial field is the compact plus the generated family and sporty bodies; the placeholder coupe and utility were removed on 28 September.
**Screen:** Cedar and Bayview, seed 1709, flag off against flag on in the same build (`tools/run-pace-study.ps1 -Study 126`).

| Run | Finished | Collective | Contacts | Assisted resets | Physical recoveries |
|---|---|---|---|---|---|
| bayview s1709 off | 24/24 | 813.354 | 4 | 0 | 2 (19.7 s) |
| bayview s1709 rejoin | **22/24** | – | **40** | **2** | 1 (**319.8 s**) |
| cedar s1709 off | 24/24 | 2102.658 | 16 | 0 | 0 |
| cedar s1709 rejoin | 24/24 | 2102.658 | 16 | 0 | 0 |

**Verdict: reject.** The keep rule requires all 24 finishers and no new assisted resets; Bayview fails both.

On Cedar the flag changes nothing, because no car rejoins there at speed. On Bayview the moving rejoin puts cars back into traffic, where they collide and stall. The wall-clock run took 192 s against 56 s.

The flag stays in the code, off by default, and is not worth running on the other seeds.
