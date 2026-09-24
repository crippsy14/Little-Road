# Opening straight: reference versus dynamic

Opening straight only, unchanged 24-car Cedar grid. Spatial field spread is a diagnostic, not a target to minimise: a stretched or compact field can both race well.

Authored start of First braking zone, 280 m from the start line; initial grid is behind the line. This is geometry, not each car's actual brake onset.

Linearly interpolated 10 Hz car-centre progress/offset/speed; rank ties retained within 1 cm spatially and 1 ms at gates. Centre gaps ignore lateral separation and are not clearance or proof that passing is possible.

| Measure | Reference | Dynamic |
|---|---:|---:|
| First car at braking zone (s) | 11.530 | 13.313 |
| Last car at braking zone (s) | 17.188 | 18.605 |
| All-car passage window (s) | 5.658 | 5.292 |
| First-to-last distance when leader arrives (m) | 129.002 | 86.341 |
| Preview arrival (s) | 15.977 | 18.567 |

## Per-car arrival at 280 m

| Car | Equipment | Reference time / rank | Dynamic time / rank |
|---|---|---:|---:|
| 0 | club | 15.977 / [17, 17] | 18.567 / [23, 23] |
| 1 | street | 13.367 / [3, 3] | 13.313 / [1, 1] |
| 2 | street | 13.449 / [5, 5] | 13.360 / [2, 2] |
| 3 | street | 13.410 / [4, 4] | 13.537 / [3, 3] |
| 4 | club | 11.530 / [1, 1] | 13.748 / [4, 4] |
| 5 | street | 13.935 / [7, 7] | 13.863 / [5, 5] |
| 6 | street | 13.886 / [6, 6] | 14.038 / [6, 6] |
| 7 | street | 14.324 / [8, 8] | 14.229 / [7, 7] |
| 8 | club | 14.385 / [10, 10] | 14.337 / [8, 8] |
| 9 | street | 14.376 / [9, 9] | 14.563 / [9, 9] |
| 10 | street | 14.790 / [11, 11] | 14.690 / [10, 10] |
| 11 | street | 14.989 / [13, 13] | 14.861 / [11, 11] |
| 12 | club | 14.870 / [12, 12] | 15.412 / [14, 14] |
| 13 | street | 15.293 / [14, 14] | 15.151 / [12, 12] |
| 14 | street | 15.536 / [16, 16] | 15.404 / [13, 13] |
| 15 | street | 15.348 / [15, 15] | 16.562 / [18, 18] |
| 16 | club | 13.243 / [2, 2] | 15.587 / [15, 15] |
| 17 | street | 16.030 / [18, 18] | 17.194 / [19, 19] |
| 18 | street | 16.072 / [19, 19] | 17.806 / [20, 20] |
| 19 | street | 16.469 / [20, 20] | 16.066 / [16, 16] |
| 20 | club | 16.545 / [21, 21] | 17.844 / [21, 21] |
| 21 | street | 16.638 / [22, 22] | 18.605 / [24, 24] |
| 22 | street | 17.000 / [23, 23] | 16.532 / [17, 17] |
| 23 | street | 17.188 / [24, 24] | 18.333 / [22, 22] |

Full position, lateral offset, speed, gaps and tied ranks at each snapshot are stored in the JSON. Pedal/context labels are observational and cannot establish why an alternative was rejected.
