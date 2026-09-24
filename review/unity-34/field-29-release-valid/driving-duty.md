# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 12 | club | 80.78 s | 12.3% | 48.6% | 33.9% | 0.00 s |
| 8 | club | 86.31 s | 16.8% | 36.3% | 38.5% | 0.00 s |
| 4 | club | 93.12 s | 19.2% | 25.8% | 46.5% | 0.00 s |
| 1 | street | 94.07 s | 13.8% | 61.7% | 25.7% | 0.00 s |
| 16 | club | 94.49 s | 20.3% | 19.8% | 46.4% | 0.00 s |
| 3 | street | 96.75 s | 14.1% | 23.7% | 24.5% | 0.00 s |
| 2 | street | 98.32 s | 15.1% | 34.0% | 27.5% | 0.00 s |
| 9 | street | 98.95 s | 14.7% | 35.5% | 33.0% | 0.00 s |
| 20 | club | 100.26 s | 23.1% | 17.0% | 45.8% | 0.00 s |
| 6 | street | 100.30 s | 13.6% | 40.7% | 27.9% | 0.00 s |
| 5 | street | 101.14 s | 16.9% | 27.8% | 30.5% | 0.00 s |
| 15 | street | 102.00 s | 14.0% | 37.2% | 26.3% | 0.00 s |
| 0 | club | 103.45 s | 23.5% | 17.8% | 47.0% | 0.00 s |
| 14 | street | 103.46 s | 14.1% | 36.6% | 24.4% | 0.00 s |
| 7 | street | 104.31 s | 17.5% | 37.6% | 31.1% | 0.00 s |
| 18 | street | 105.44 s | 16.4% | 37.5% | 30.9% | 0.00 s |
| 10 | street | 106.67 s | 17.6% | 36.7% | 35.1% | 0.00 s |
| 19 | street | 106.89 s | 16.7% | 19.2% | 31.2% | 0.00 s |
| 13 | street | 108.19 s | 17.5% | 30.7% | 27.6% | 0.00 s |
| 21 | street | 108.98 s | 17.0% | 40.9% | 32.9% | 0.00 s |
| 11 | street | 109.98 s | 20.2% | 21.7% | 31.3% | 0.00 s |
| 23 | street | 110.00 s | 16.8% | 30.6% | 29.5% | 0.00 s |
| 17 | street | 111.20 s | 18.7% | 29.2% | 29.1% | 0.00 s |
| 22 | street | 112.01 s | 17.8% | 30.0% | 32.7% | 0.00 s |
