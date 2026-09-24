# Expanded parts and 24-car racing review

16 September 2026. Goal remains active. The updated source and Windows development build supersede the earlier four-car prototype.

## Research and requirements

Read race progression, course, parts and item references before replacing the prototype systems. Documents 09–11 record the findings, original balance proposals, full ownership roadmap and PS5 DualSense requirement. The former two-upgrade/three-rival limits are superseded.

The user's recollection of AI stopping at an opening S-bend is now an explicit regression scenario. The source corroborates opening-S bunching, but the exact stopping defect has not been independently reproduced. Difficulty must not depend on stalled AI or rubber-banding against the player's purchases.

## Implemented

- 24 entrants: the player and 23 opponents using the same physical car controller, with authored road guidance, corner planning, traffic avoidance and recovery.
- Three-second countdown, three laps, ordered gates, position/24, results, replay and return to town.
- Fixed C-class rival equipment; purchases do not scale the opposition.
- Six slots, six stock items and twelve purchasable parts. Ownership and fitting are separate; refitting owned parts is free. Garage comparisons describe trade-offs.
- Positive rewards for every valid finish, 100–500 credits in the initial C event, with persistent event settlement to prevent duplicate payouts.
- Version-two saves and migration of the previous engine/tyre purchases, money and progress.

## Evidence

[Functional checks](test-results.json): 32/32 passed, including affordable purchases, ownership, free refitting, weak-engine/long-gearing interaction, duplicate-payout rejection, save round trip and legacy migration, actual driving and 24-car event creation/retry.

Two scripted three-lap trials with the same driving algorithm and fixed field:

| Player setup | Finish | Time | Evidence |
| --- | ---: | ---: | --- |
| Stock | 21 / 24 | 145.45 s | [Stock trial](race-trial-stock.json) |
| Developed road setup | 8 / 24 | 107.50 s | [Developed trial](race-trial-developed.json) |

These are single trials, not distributions or human difficulty measurements. The developed setup is faster but has not met the intended winning-car target. Some opponents needed recovery; routine recovery and pack behaviour remain open issues. Trials stop at the player's finish and therefore do not establish that every trailing rival finishes. No claim is made that the S-bend acceptance gate has passed.

Rendered 1920×1080 evidence: [full starting grid](captures/standalone-03/04-grid.png), [race in motion](captures/standalone-03/05-racing.png), [garage](captures/standalone-03/06-garage.png). The first avoidance implementation oscillated between passing lanes; lane commitment/cooldown corrected that specific behaviour. Review then exposed a low-positioned tall garage panel; a centred container corrected it. The rebuilt standalone executable launched, produced all six review images and exited with code zero. The corrected garage capture was visually inspected and fits the screen with clear margins.

## Open requirements

1. Dedicated alternating-bend, blocked-lane and varied-grid trials; complete all-rival finish checks; fewer recoveries and better overtaking.
2. Full results table, race-specific recovery validation and more edge-case trials.
3. Repeated stock/individual-upgrade/combined-build comparisons and human playtesting, including time to first purchase and first win.
4. Next track geometry and track/class unlock system. The current calendar correctly labels Quarry Link as planned rather than playable; only Bayview C is currently implemented.
5. Physical DualSense USB/Bluetooth tests, remapping/calibration and complete controller menu/back/reconnect behaviour.
6. Advanced ownership families, saved setups, audio, exploration detour/resident-service chain and further visual work.
7. Representative 1080p frame-time measurement. Successful 1080p rendering alone does not prove 60 fps.

The visual scores from iteration 03 remain below the target; adding racing has not satisfied the art-direction gate.
