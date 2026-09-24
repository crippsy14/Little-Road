# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 78.98 s | 15.6% | 66.3% | 37.3% | 0.00 s |
| 12 | club | 87.58 s | 17.8% | 26.8% | 44.0% | 0.00 s |
| 8 | club | 89.23 s | 21.9% | 24.6% | 47.9% | 0.00 s |
| 1 | street | 89.37 s | 7.7% | 66.0% | 21.2% | 0.00 s |
| 3 | street | 94.32 s | 10.2% | 50.3% | 25.5% | 0.00 s |
| 16 | club | 96.14 s | 22.7% | 20.7% | 48.3% | 0.00 s |
| 7 | street | 96.90 s | 15.1% | 27.6% | 27.8% | 0.00 s |
| 2 | street | 98.96 s | 14.2% | 48.2% | 25.8% | 0.00 s |
| 6 | street | 99.63 s | 14.3% | 33.7% | 28.1% | 0.00 s |
| 0 | club | 99.78 s | 22.2% | 23.4% | 43.9% | 0.00 s |
| 9 | street | 100.97 s | 14.3% | 41.6% | 31.1% | 0.00 s |
| 5 | street | 101.57 s | 16.2% | 28.4% | 31.3% | 0.00 s |
| 20 | club | 102.74 s | 24.6% | 13.7% | 48.6% | 0.00 s |
| 13 | street | 103.15 s | 17.1% | 26.0% | 29.3% | 0.00 s |
| 10 | street | 103.47 s | 17.5% | 31.0% | 35.5% | 0.00 s |
| 11 | street | 104.77 s | 15.2% | 43.0% | 26.8% | 0.00 s |
| 15 | street | 105.30 s | 15.7% | 23.5% | 27.4% | 0.00 s |
| 18 | street | 106.54 s | 16.5% | 29.7% | 31.0% | 0.00 s |
| 14 | street | 107.61 s | 15.0% | 40.7% | 28.5% | 0.00 s |
| 23 | street | 108.80 s | 16.2% | 37.3% | 28.2% | 0.00 s |
| 19 | street | 109.54 s | 17.3% | 24.4% | 33.3% | 0.00 s |
| 17 | street | 110.30 s | 17.4% | 20.3% | 31.0% | 0.00 s |
| 22 | street | 111.77 s | 19.6% | 40.5% | 34.2% | 0.00 s |
| 21 | street | 113.15 s | 19.1% | 35.0% | 32.3% | 0.00 s |
