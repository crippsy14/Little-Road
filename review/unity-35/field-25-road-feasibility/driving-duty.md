# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 75.19 s | 7.7% | 72.0% | 34.7% | 0.00 s |
| 12 | club | 82.22 s | 7.4% | 43.0% | 25.1% | 0.00 s |
| 8 | club | 83.93 s | 8.0% | 48.9% | 22.2% | 0.00 s |
| 16 | club | 87.10 s | 6.0% | 27.0% | 18.7% | 0.00 s |
| 1 | street | 89.65 s | 8.2% | 68.2% | 15.5% | 0.00 s |
| 3 | street | 90.39 s | 7.9% | 52.8% | 13.2% | 0.00 s |
| 0 | club | 90.42 s | 6.2% | 21.0% | 15.8% | 0.00 s |
| 20 | club | 91.24 s | 5.7% | 28.9% | 18.7% | 0.00 s |
| 2 | street | 92.44 s | 7.3% | 37.8% | 13.0% | 0.00 s |
| 7 | street | 92.97 s | 7.3% | 31.3% | 12.7% | 0.00 s |
| 5 | street | 93.54 s | 8.8% | 42.2% | 11.6% | 0.00 s |
| 6 | street | 94.18 s | 8.4% | 32.0% | 12.6% | 0.00 s |
| 13 | street | 94.68 s | 9.6% | 41.6% | 12.8% | 0.00 s |
| 9 | street | 95.12 s | 8.0% | 23.6% | 7.9% | 0.00 s |
| 10 | street | 95.13 s | 9.1% | 37.1% | 13.0% | 0.00 s |
| 14 | street | 95.64 s | 8.7% | 30.9% | 10.1% | 0.00 s |
| 11 | street | 95.97 s | 8.9% | 39.7% | 10.8% | 0.00 s |
| 19 | street | 96.25 s | 9.6% | 15.0% | 11.4% | 0.00 s |
| 15 | street | 96.49 s | 7.5% | 29.2% | 10.0% | 0.00 s |
| 22 | street | 96.81 s | 8.6% | 27.5% | 9.2% | 0.00 s |
| 18 | street | 97.23 s | 7.8% | 25.4% | 8.1% | 0.00 s |
| 17 | street | 97.81 s | 8.2% | 52.2% | 10.4% | 0.00 s |
| 23 | street | 98.40 s | 9.6% | 56.9% | 8.5% | 0.00 s |
| 21 | street | 98.87 s | 9.7% | 55.6% | 10.9% | 0.00 s |
