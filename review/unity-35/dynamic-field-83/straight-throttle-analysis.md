# Opening-straight throttle demand

Applied gas-pedal demand, not measured acceleration or percent engine power. Every car is measured over its own complete spatial section. Groups use time-weighted car-seconds; preview is also included in club and field.

10 Hz observations held to the next sample, clipped at interpolated spatial gates. Approximate exposure, not physics-tick pedal integration. Full throttle >=95%, partial >5%, coast -1% to 5%, braking <-1%. Average throttle includes zero during coast/brake.

No target of 100% everywhere: traffic occupancy, speed limits, grip and braking distance must still be respected. The last 80 m is reported separately because corner preparation may legitimately require braking.

| Section / group | Average throttle ref / dynamic | Full-throttle time ref / dynamic | Brake time ref / dynamic |
|---|---:|---:|---:|
| grid-launch / preview | 50.2% / 47.8% | 20.7% / 9.3% | 0.0% / 0.0% |
| grid-launch / club | 51.0% / 50.8% | 11.2% / 9.9% | 0.0% / 1.3% |
| grid-launch / street | 69.8% / 67.2% | 32.0% / 30.3% | 0.3% / 1.1% |
| grid-launch / field | 64.7% / 62.8% | 26.4% / 24.8% | 0.2% / 1.2% |
| straight-0-200m / preview | 44.6% / 69.5% | 6.4% / 20.7% | 0.0% / 3.0% |
| straight-0-200m / club | 52.3% / 62.6% | 18.7% / 29.8% | 2.9% / 3.8% |
| straight-0-200m / street | 62.4% / 60.5% | 17.3% / 17.2% | 1.1% / 2.0% |
| straight-0-200m / field | 60.0% / 61.0% | 17.6% / 20.1% | 1.6% / 2.4% |
| approach-200-280m / preview | 34.8% / 55.3% | 0.0% / 42.7% | 23.4% / 29.8% |
| approach-200-280m / club | 45.1% / 56.5% | 7.3% / 29.2% | 9.2% / 22.0% |
| approach-200-280m / street | 53.7% / 47.5% | 7.4% / 6.0% | 9.8% / 11.7% |
| approach-200-280m / field | 51.6% / 49.8% | 7.4% / 11.9% | 9.7% / 14.3% |
| whole-opening / preview | 44.2% / 58.5% | 9.5% / 23.6% | 5.3% / 9.9% |
| whole-opening / club | 50.4% / 58.1% | 14.4% / 24.5% | 3.6% / 7.6% |
| whole-opening / street | 62.2% / 59.0% | 18.4% / 17.5% | 2.9% / 4.1% |
| whole-opening / field | 59.2% / 58.8% | 17.4% / 19.2% | 3.0% / 5.0% |
