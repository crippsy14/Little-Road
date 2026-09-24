# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 75.20 s | 7.5% | 71.8% | 35.7% | 0.00 s |
| 8 | club | 78.14 s | 7.0% | 59.7% | 28.7% | 0.00 s |
| 12 | club | 83.27 s | 7.0% | 31.4% | 25.4% | 0.00 s |
| 16 | club | 83.79 s | 5.2% | 35.0% | 21.8% | 0.00 s |
| 1 | street | 89.51 s | 7.7% | 67.9% | 15.3% | 0.00 s |
| 3 | street | 90.38 s | 6.3% | 62.7% | 13.1% | 0.00 s |
| 2 | street | 91.37 s | 7.4% | 56.7% | 11.6% | 0.00 s |
| 5 | street | 93.27 s | 10.2% | 57.8% | 13.8% | 0.00 s |
| 6 | street | 94.17 s | 9.9% | 57.2% | 14.7% | 0.00 s |
| 10 | street | 94.67 s | 10.6% | 36.3% | 10.6% | 0.00 s |
| 9 | street | 95.18 s | 9.5% | 33.2% | 11.3% | 0.00 s |
| 14 | street | 95.77 s | 10.7% | 32.3% | 11.4% | 0.00 s |
| 11 | street | 95.87 s | 9.2% | 23.5% | 10.5% | 0.00 s |
| 13 | street | 95.87 s | 10.1% | 35.7% | 11.0% | 0.00 s |
| 20 | club | 96.32 s | 5.4% | 34.6% | 3.7% | 0.00 s |
| 7 | street | 96.35 s | 8.8% | 24.1% | 12.8% | 0.00 s |
| 0 | club | 96.98 s | 9.3% | 26.1% | 11.5% | 0.00 s |
| 15 | street | 97.47 s | 8.4% | 36.7% | 11.3% | 0.00 s |
| 22 | street | 98.26 s | 9.6% | 25.5% | 11.5% | 0.00 s |
| 17 | street | 98.61 s | 9.7% | 33.7% | 12.2% | 0.00 s |
| 18 | street | 99.19 s | 9.9% | 21.3% | 12.1% | 0.00 s |
| 19 | street | 99.72 s | 11.7% | 38.2% | 12.1% | 0.00 s |
| 21 | street | 99.90 s | 9.3% | 39.6% | 13.4% | 0.00 s |
| 23 | street | 100.41 s | 12.1% | 46.6% | 12.8% | 0.00 s |
