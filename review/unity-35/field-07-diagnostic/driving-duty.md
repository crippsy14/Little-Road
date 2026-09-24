# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 8 | club | 77.43 s | 8.1% | 58.5% | 22.9% | 0.00 s |
| 4 | club | 82.23 s | 7.2% | 49.4% | 13.0% | 0.00 s |
| 12 | club | 82.71 s | 8.5% | 31.3% | 27.8% | 0.00 s |
| 20 | club | 87.73 s | 13.5% | 19.3% | 36.5% | 0.00 s |
| 16 | club | 87.79 s | 11.4% | 30.8% | 26.3% | 0.00 s |
| 1 | street | 90.11 s | 7.3% | 64.3% | 20.6% | 0.00 s |
| 0 | club | 90.42 s | 11.9% | 22.2% | 31.1% | 0.00 s |
| 2 | street | 91.82 s | 7.1% | 42.7% | 16.4% | 0.00 s |
| 3 | street | 92.26 s | 7.1% | 38.7% | 13.6% | 0.00 s |
| 5 | street | 94.07 s | 8.8% | 47.6% | 19.3% | 0.00 s |
| 6 | street | 94.42 s | 8.8% | 40.5% | 14.0% | 0.00 s |
| 7 | street | 95.58 s | 9.0% | 60.4% | 19.1% | 0.00 s |
| 9 | street | 97.22 s | 9.2% | 49.4% | 19.1% | 0.00 s |
| 10 | street | 97.56 s | 8.6% | 59.0% | 17.9% | 0.00 s |
| 22 | street | 98.16 s | 10.6% | 35.8% | 11.6% | 0.00 s |
| 13 | street | 98.52 s | 12.3% | 27.0% | 11.4% | 0.00 s |
| 14 | street | 98.94 s | 9.6% | 37.7% | 13.5% | 0.00 s |
| 11 | street | 99.26 s | 9.0% | 34.7% | 12.3% | 0.00 s |
| 15 | street | 99.51 s | 11.3% | 58.5% | 16.4% | 0.00 s |
| 17 | street | 100.01 s | 9.7% | 51.3% | 20.5% | 0.00 s |
| 21 | street | 100.61 s | 8.4% | 59.9% | 19.2% | 0.00 s |
| 18 | street | 101.01 s | 10.4% | 54.7% | 15.7% | 0.00 s |
| 19 | street | 101.52 s | 9.9% | 50.3% | 20.7% | 0.00 s |
| 23 | street | 102.22 s | 9.5% | 67.0% | 20.8% | 0.00 s |
