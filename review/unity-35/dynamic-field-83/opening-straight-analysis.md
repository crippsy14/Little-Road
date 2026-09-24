# Opening straight: reference versus dynamic

Opening straight only, unchanged 24-car Cedar grid. Spatial field spread is a diagnostic, not a target to minimise: a stretched or compact field can both race well.

Authored start of First braking zone, 280 m from the start line; initial grid is behind the line. This is geometry, not each car's actual brake onset.

Linearly interpolated 10 Hz car-centre progress/offset/speed; rank ties retained within 1 cm spatially and 1 ms at gates. Centre gaps ignore lateral separation and are not clearance or proof that passing is possible.

| Measure | Reference | Dynamic |
|---|---:|---:|
| First car at braking zone (s) | 12.519 | 12.519 |
| Last car at braking zone (s) | 17.534 | 18.587 |
| All-car passage window (s) | 5.015 | 6.068 |
| First-to-last distance when leader arrives (m) | 116.709 | 124.369 |
| Preview arrival (s) | 16.921 | 17.160 |

## Per-car arrival at 280 m

| Car | Equipment | Reference time / rank | Dynamic time / rank |
|---|---|---:|---:|
| 0 | club | 16.921 / [23, 23] | 17.160 / [19, 19] |
| 1 | street | 13.395 / [3, 3] | 13.438 / [2, 2] |
| 2 | street | 13.338 / [2, 2] | 13.583 / [4, 4] |
| 3 | street | 13.522 / [4, 4] | 13.443 / [3, 3] |
| 4 | club | 12.519 / [1, 1] | 12.519 / [1, 1] |
| 5 | street | 13.802 / [5, 5] | 14.237 / [7, 7] |
| 6 | street | 14.022 / [6, 6] | 13.936 / [6, 6] |
| 7 | street | 14.276 / [8, 8] | 14.381 / [8, 8] |
| 8 | club | 14.236 / [7, 7] | 14.853 / [10, 10] |
| 9 | street | 14.424 / [9, 9] | 14.986 / [11, 11] |
| 10 | street | 14.699 / [11, 11] | 15.585 / [13, 13] |
| 11 | street | 14.923 / [13, 13] | 15.597 / [14, 14] |
| 12 | club | 14.484 / [10, 10] | 14.681 / [9, 9] |
| 13 | street | 15.557 / [14, 14] | 15.195 / [12, 12] |
| 14 | street | 15.767 / [15, 15] | 15.839 / [15, 15] |
| 15 | street | 15.835 / [16, 16] | 16.123 / [16, 16] |
| 16 | club | 14.915 / [12, 12] | 13.614 / [5, 5] |
| 17 | street | 16.193 / [17, 17] | 17.516 / [21, 21] |
| 18 | street | 16.370 / [18, 18] | 16.647 / [18, 18] |
| 19 | street | 16.382 / [19, 19] | 16.319 / [17, 17] |
| 20 | club | 16.877 / [21, 21] | 17.650 / [22, 22] |
| 21 | street | 16.519 / [20, 20] | 17.186 / [20, 20] |
| 22 | street | 16.887 / [22, 22] | 18.072 / [23, 23] |
| 23 | street | 17.534 / [24, 24] | 18.587 / [24, 24] |

Full position, lateral offset, speed, gaps and tied ranks at each snapshot are stored in the JSON. Pedal/context labels are observational and cannot establish why an alternative was rejected.
