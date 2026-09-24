# Opening-straight throttle demand

Applied gas-pedal demand, not measured acceleration or percent engine power. Every car is measured over its own complete spatial section. Groups use time-weighted car-seconds; preview is also included in club and field.

10 Hz observations held to the next sample, clipped at interpolated spatial gates. Approximate exposure, not physics-tick pedal integration. Full throttle >=95%, partial >5%, coast -1% to 5%, braking <-1%. Average throttle includes zero during coast/brake.

No target of 100% everywhere: traffic occupancy, speed limits, grip and braking distance must still be respected. The last 80 m is reported separately because corner preparation may legitimately require braking.

| Section / group | Average throttle ref / dynamic | Full-throttle time ref / dynamic | Brake time ref / dynamic |
|---|---:|---:|---:|
| grid-launch / preview | 48.4% / 50.2% | 3.7% / 20.7% | 0.0% / 0.0% |
| grid-launch / club | 52.5% / 51.0% | 4.0% / 11.2% | 0.0% / 0.0% |
| grid-launch / street | 72.9% / 69.8% | 39.9% / 32.0% | 0.0% / 0.3% |
| grid-launch / field | 67.4% / 64.7% | 30.2% / 26.4% | 0.0% / 0.2% |
| straight-0-200m / preview | 39.2% / 44.6% | 0.0% / 6.4% | 1.2% / 0.0% |
| straight-0-200m / club | 44.1% / 52.3% | 0.0% / 18.7% | 0.2% / 2.9% |
| straight-0-200m / street | 59.2% / 62.4% | 12.7% / 17.3% | 0.8% / 1.1% |
| straight-0-200m / field | 55.4% / 60.0% | 9.5% / 17.6% | 0.7% / 1.6% |
| approach-200-280m / preview | 30.3% / 34.8% | 17.3% / 0.0% | 46.2% / 23.4% |
| approach-200-280m / club | 32.1% / 45.1% | 6.4% / 7.3% | 24.2% / 9.2% |
| approach-200-280m / street | 45.1% / 53.7% | 5.9% / 7.4% | 18.3% / 9.8% |
| approach-200-280m / field | 41.8% / 51.6% | 6.0% / 7.4% | 19.8% / 9.7% |
| whole-opening / preview | 39.5% / 44.2% | 5.7% / 9.5% | 12.9% / 5.3% |
| whole-opening / club | 43.1% / 50.4% | 2.5% / 14.4% | 6.1% / 3.6% |
| whole-opening / street | 58.8% / 62.2% | 17.0% / 18.4% | 4.9% / 2.9% |
| whole-opening / field | 54.8% / 59.2% | 13.3% / 17.4% | 5.2% / 3.0% |
