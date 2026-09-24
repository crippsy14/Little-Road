# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 78.09 s | 6.2% | 67.3% | 12.9% | 0.00 s |
| 8 | club | 82.89 s | 7.2% | 49.7% | 15.1% | 0.00 s |
| 16 | club | 84.84 s | 9.2% | 31.2% | 17.0% | 0.00 s |
| 12 | club | 88.07 s | 9.1% | 29.1% | 19.9% | 0.00 s |
| 1 | street | 90.71 s | 7.7% | 47.2% | 18.8% | 0.00 s |
| 2 | street | 91.43 s | 7.7% | 49.2% | 16.7% | 0.00 s |
| 20 | club | 91.94 s | 6.8% | 24.9% | 13.2% | 0.00 s |
| 3 | street | 92.42 s | 7.4% | 51.5% | 17.7% | 0.00 s |
| 6 | street | 93.23 s | 6.8% | 50.5% | 16.1% | 0.00 s |
| 5 | street | 93.74 s | 9.6% | 51.6% | 18.0% | 0.00 s |
| 7 | street | 94.65 s | 9.2% | 46.2% | 19.6% | 0.00 s |
| 10 | street | 95.06 s | 9.8% | 39.6% | 12.9% | 0.00 s |
| 14 | street | 95.47 s | 9.1% | 47.8% | 13.2% | 0.00 s |
| 9 | street | 95.70 s | 9.2% | 25.6% | 9.9% | 0.00 s |
| 13 | street | 96.08 s | 9.7% | 48.1% | 16.8% | 0.00 s |
| 19 | street | 96.58 s | 10.5% | 27.2% | 12.1% | 0.00 s |
| 11 | street | 96.88 s | 8.3% | 26.4% | 9.3% | 0.00 s |
| 0 | club | 96.96 s | 6.8% | 29.5% | 7.0% | 0.00 s |
| 17 | street | 96.96 s | 9.7% | 20.6% | 9.9% | 0.00 s |
| 15 | street | 97.31 s | 8.5% | 18.1% | 10.5% | 0.00 s |
| 22 | street | 97.74 s | 8.5% | 44.8% | 11.3% | 0.00 s |
| 18 | street | 97.85 s | 7.1% | 19.1% | 9.5% | 0.00 s |
| 21 | street | 98.26 s | 9.1% | 32.8% | 11.0% | 0.00 s |
| 23 | street | 98.66 s | 8.4% | 43.3% | 10.8% | 0.00 s |
