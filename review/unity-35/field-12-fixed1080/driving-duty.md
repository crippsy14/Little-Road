# Driving duty audit

10 Hz sampled controls held to next sample; line error interpolated within each interval. All percentages share post-start-line time to last recorded unfinished-car sample. Intervals over 0.25 s are excluded; no extrapolated finish tail. Context is controller state, not causal proof. Below-solo flag means braking at least 2 m/s below a non-braking matched solo at that road location. Traffic, lateral path and vehicle state can justify it. Samples cannot establish sub-frame control smoothness.

| Car | Parts | Finish | Brake | On line | Full throttle | Below-solo braking |
|---|---|---:|---:|---:|---:|---:|
| 4 | club | 77.75 s | 7.1% | 67.1% | 15.7% | 0.00 s |
| 12 | club | 85.98 s | 8.6% | 32.7% | 21.3% | 0.00 s |
| 8 | club | 88.33 s | 9.8% | 37.8% | 15.8% | 0.00 s |
| 0 | club | 90.52 s | 7.3% | 17.7% | 20.0% | 0.00 s |
| 1 | street | 90.76 s | 9.9% | 61.1% | 22.4% | 0.00 s |
| 16 | club | 90.97 s | 8.0% | 27.6% | 14.0% | 0.00 s |
| 2 | street | 92.07 s | 9.0% | 43.5% | 16.0% | 0.00 s |
| 3 | street | 92.52 s | 7.8% | 35.3% | 13.4% | 0.00 s |
| 7 | street | 93.48 s | 10.5% | 41.5% | 16.2% | 0.00 s |
| 6 | street | 93.87 s | 8.3% | 43.3% | 15.3% | 0.00 s |
| 5 | street | 94.66 s | 11.0% | 67.1% | 20.6% | 0.00 s |
| 10 | street | 95.06 s | 10.0% | 55.0% | 14.9% | 0.00 s |
| 20 | club | 95.44 s | 7.4% | 23.9% | 12.9% | 0.00 s |
| 9 | street | 95.63 s | 8.5% | 46.2% | 13.8% | 0.00 s |
| 13 | street | 96.24 s | 10.3% | 31.4% | 15.9% | 0.00 s |
| 11 | street | 96.66 s | 9.5% | 34.8% | 10.9% | 0.00 s |
| 17 | street | 97.21 s | 9.2% | 38.0% | 11.6% | 0.00 s |
| 19 | street | 97.38 s | 9.3% | 31.1% | 11.0% | 0.00 s |
| 14 | street | 97.73 s | 8.7% | 16.6% | 11.2% | 0.00 s |
| 22 | street | 97.80 s | 11.3% | 23.1% | 11.5% | 0.00 s |
| 23 | street | 98.36 s | 10.9% | 29.1% | 12.4% | 0.00 s |
| 15 | street | 98.51 s | 10.0% | 32.7% | 16.1% | 0.00 s |
| 18 | street | 98.75 s | 9.3% | 21.0% | 10.2% | 0.00 s |
| 21 | street | 99.97 s | 10.2% | 34.0% | 15.1% | 0.00 s |
