# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 75.15 s | 8.1% | 72.0% | 34.4% | 0.00 s |
| 8 | club | 78.20 s | 6.8% | 60.3% | 27.7% | 0.00 s |
| 12 | club | 83.87 s | 6.3% | 39.0% | 23.6% | 0.00 s |
| 1 | street | 89.60 s | 7.4% | 68.2% | 15.1% | 0.00 s |
| 16 | club | 89.80 s | 9.8% | 24.4% | 18.6% | 0.00 s |
| 3 | street | 90.66 s | 6.2% | 60.5% | 13.4% | 0.00 s |
| 2 | street | 92.17 s | 7.3% | 59.6% | 12.5% | 0.00 s |
| 7 | street | 93.31 s | 6.6% | 42.3% | 12.7% | 0.00 s |
| 5 | street | 93.80 s | 9.1% | 43.9% | 13.0% | 0.00 s |
| 20 | club | 93.82 s | 9.7% | 23.0% | 15.9% | 0.00 s |
| 0 | club | 94.26 s | 8.2% | 20.9% | 15.9% | 0.00 s |
| 9 | street | 95.39 s | 9.8% | 45.6% | 14.0% | 0.00 s |
| 10 | street | 95.87 s | 10.7% | 27.1% | 14.2% | 0.00 s |
| 6 | street | 96.40 s | 9.9% | 27.1% | 11.7% | 0.00 s |
| 11 | street | 96.84 s | 10.7% | 30.8% | 12.2% | 0.00 s |
| 14 | street | 97.22 s | 11.3% | 25.0% | 13.8% | 0.00 s |
| 13 | street | 97.74 s | 11.6% | 21.9% | 13.8% | 0.00 s |
| 15 | street | 98.12 s | 9.4% | 32.9% | 10.3% | 0.00 s |
| 19 | street | 98.61 s | 11.6% | 32.4% | 12.6% | 0.00 s |
| 22 | street | 98.96 s | 10.7% | 37.6% | 13.3% | 0.00 s |
| 17 | street | 99.06 s | 9.3% | 37.8% | 12.1% | 0.00 s |
| 23 | street | 99.41 s | 10.4% | 43.8% | 12.2% | 0.00 s |
| 18 | street | 99.95 s | 10.0% | 20.0% | 13.2% | 0.00 s |
| 21 | street | 100.68 s | 11.4% | 52.6% | 14.1% | 0.00 s |
