# Opening-straight throttle demand

Applied gas-pedal demand, not measured acceleration or percent engine power. Every car is measured over its own complete spatial section. Groups use time-weighted car-seconds; preview is also included in club and field.

10 Hz observations held to the next sample, clipped at interpolated spatial gates. Approximate exposure, not physics-tick pedal integration. Full throttle >=95%, partial >5%, coast -1% to 5%, braking <-1%. Average throttle includes zero during coast/brake.

No target of 100% everywhere: traffic occupancy, speed limits, grip and braking distance must still be respected. The last 80 m is reported separately because corner preparation may legitimately require braking.

| Section / group | Average throttle ref / dynamic | Full-throttle time ref / dynamic | Brake time ref / dynamic |
|---|---:|---:|---:|
| grid-launch / preview | 47.3% / 48.4% | 3.7% / 3.7% | 0.0% / 0.0% |
| grid-launch / club | 52.1% / 52.5% | 4.0% / 4.0% | 0.0% / 0.0% |
| grid-launch / street | 72.9% / 72.9% | 39.3% / 39.9% | 0.0% / 0.0% |
| grid-launch / field | 67.3% / 67.4% | 29.8% / 30.2% | 0.0% / 0.0% |
| straight-0-200m / preview | 53.4% / 39.2% | 16.8% / 0.0% | 2.0% / 1.2% |
| straight-0-200m / club | 57.3% / 44.1% | 20.4% / 0.0% | 1.4% / 0.2% |
| straight-0-200m / street | 58.9% / 59.2% | 14.1% / 12.7% | 0.7% / 0.8% |
| straight-0-200m / field | 58.5% / 55.4% | 15.6% / 9.5% | 0.8% / 0.7% |
| approach-200-280m / preview | 38.3% / 30.3% | 13.7% / 17.3% | 14.1% / 46.2% |
| approach-200-280m / club | 43.2% / 32.1% | 13.5% / 6.4% | 5.1% / 24.2% |
| approach-200-280m / street | 42.1% / 45.1% | 0.3% / 5.9% | 8.0% / 18.3% |
| approach-200-280m / field | 42.4% / 41.8% | 3.3% / 6.0% | 7.4% / 19.8% |
| whole-opening / preview | 48.6% / 39.5% | 11.9% / 5.7% | 3.6% / 12.9% |
| whole-opening / club | 53.1% / 43.1% | 14.7% / 2.5% | 1.8% / 6.1% |
| whole-opening / street | 58.3% / 58.8% | 16.7% / 17.0% | 2.1% / 4.9% |
| whole-opening / field | 57.1% / 54.8% | 16.2% / 13.3% | 2.1% / 5.2% |
