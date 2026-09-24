# Opening straight: reference versus dynamic

Opening straight only, unchanged 24-car Cedar grid. Spatial field spread is a diagnostic, not a target to minimise: a stretched or compact field can both race well.

Authored start of First braking zone, 280 m from the start line; initial grid is behind the line. This is geometry, not each car's actual brake onset.

Linearly interpolated 10 Hz car-centre progress/offset/speed; rank ties retained within 1 cm spatially and 1 ms at gates. Centre gaps ignore lateral separation and are not clearance or proof that passing is possible.

| Measure | Reference | Dynamic |
|---|---:|---:|
| First car at braking zone (s) | 13.313 | 12.519 |
| Last car at braking zone (s) | 18.605 | 17.534 |
| All-car passage window (s) | 5.292 | 5.015 |
| First-to-last distance when leader arrives (m) | 86.341 | 116.709 |
| Preview arrival (s) | 18.567 | 16.921 |

## Per-car arrival at 280 m

| Car | Equipment | Reference time / rank | Dynamic time / rank |
|---|---|---:|---:|
| 0 | club | 18.567 / [23, 23] | 16.921 / [23, 23] |
| 1 | street | 13.313 / [1, 1] | 13.395 / [3, 3] |
| 2 | street | 13.360 / [2, 2] | 13.338 / [2, 2] |
| 3 | street | 13.537 / [3, 3] | 13.522 / [4, 4] |
| 4 | club | 13.748 / [4, 4] | 12.519 / [1, 1] |
| 5 | street | 13.863 / [5, 5] | 13.802 / [5, 5] |
| 6 | street | 14.038 / [6, 6] | 14.022 / [6, 6] |
| 7 | street | 14.229 / [7, 7] | 14.276 / [8, 8] |
| 8 | club | 14.337 / [8, 8] | 14.236 / [7, 7] |
| 9 | street | 14.563 / [9, 9] | 14.424 / [9, 9] |
| 10 | street | 14.690 / [10, 10] | 14.699 / [11, 11] |
| 11 | street | 14.861 / [11, 11] | 14.923 / [13, 13] |
| 12 | club | 15.412 / [14, 14] | 14.484 / [10, 10] |
| 13 | street | 15.151 / [12, 12] | 15.557 / [14, 14] |
| 14 | street | 15.404 / [13, 13] | 15.767 / [15, 15] |
| 15 | street | 16.562 / [18, 18] | 15.835 / [16, 16] |
| 16 | club | 15.587 / [15, 15] | 14.915 / [12, 12] |
| 17 | street | 17.194 / [19, 19] | 16.193 / [17, 17] |
| 18 | street | 17.806 / [20, 20] | 16.370 / [18, 18] |
| 19 | street | 16.066 / [16, 16] | 16.382 / [19, 19] |
| 20 | club | 17.844 / [21, 21] | 16.877 / [21, 21] |
| 21 | street | 18.605 / [24, 24] | 16.519 / [20, 20] |
| 22 | street | 16.532 / [17, 17] | 16.887 / [22, 22] |
| 23 | street | 18.333 / [22, 22] | 17.534 / [24, 24] |

Full position, lateral offset, speed, gaps and tied ranks at each snapshot are stored in the JSON. Pedal/context labels are observational and cannot establish why an alternative was rejected.
