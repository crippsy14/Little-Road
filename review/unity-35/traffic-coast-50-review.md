# Prototype50: natural-racing reference, pace still open

The player described the combined visible run as a night-and-day improvement and the most natural race so far. Preserve that exact executable/configuration as the visual reference. The repeated hidden combined run reproduces all stored motion, checkpoints, impacts, overlap peaks, samples, context decisions and race results exactly. This makes the diagnostic comparison relevant to the reviewed race; it does not establish broad seed coverage or rendered performance acceptance.

Build50b passes326 build checks. Source/tool/doc snapshot: `source-50`; executable/hash: `prototype-50-manifest.json`. The desktop shortcut remains43.

| Seed1709, 24 cars, one lap | Control46 behaviour | Coast fallback only | Coast + passing-space anticipation (reviewed) |
|---|---:|---:|---:|
| Finished | 24 | 24 | 24 |
| Combined car-seconds | 2235.475616 | 2238.524872 | 2245.831421 |
| Last finish, seconds | 103.688423 | 103.055244 | 102.438728 |
| Preview CP1 / finish | 20 / 8 | 19 / 6 | 19 / 8 |
| Preview lap seconds | 91.166481 | 88.433815 | 91.666359 |
| Pair contact events | 2 | 7 | 4 |
| Maximum relative impact speed, m/s | 0.358347 | 13.960556 | 8.755849 |
| Body outside / recovery count | 0 / 0 | 0 / 0 | 0 / 0 |
| Unresolved active samples | 374 | 358 | 436 |
| Capped path attempts / accepts | 0 / 0 | 447 / 241 | 390 / 203 |
| Moving sampled time braking | 7.6297% | 7.6871% | 7.9288% |

All arms pass11 field checks and13 checkpoint deadlines. Neither new arm passes the declared2215 car-second pace target or6m/s maximum-impact review threshold; both are below the10 contact-event threshold. Occasional contact is explicitly allowed by the user. These review thresholds flag severity for investigation, not a requirement to remove all contact or make every driver cautious. Field contact counts include recorded post-finish events: coast-only has two car0/car1 events after car0 finished; its13.96m/s peak is during the race. Counts should not be described as unique colliding pairs or collision blame.

The combined arm passes92/92 controlled checks across ten scenarios, including queue, no-room, both rear merges, braking leader, three-car corner and paired solos. No controlled contact, road departure or recovery. Results retain47's useful controlled improvement: queue car0 completes26.649693s; no-room31.266289s; brake-leader27.483013s. Coast fallback has not demonstrated a collective pace or overall braking reduction. Nominally feasible coasting is a usable additional control, not proof that coasting always makes the race faster.

Disabled comparison preserves every stored checkpoint, impact, recovery, overlap, sample, context sample and car race result from46. Full raw motion-array parity remains false:46 contains24 extra rows after its last finish103.688423s. All2496 rows through that last finish are exactly equal. Original raw failure and supplemental race-window evidence are both retained; no gate was rewritten.

## Concrete next investigation

`field-combined/peak-contact-trace.json` captures the car0/car13 incident at48.594822s (8.755849m/s relative impact). Around47.703s, car0 targets offset-4.02 while passing car13; at47.803s the selected target jumps to+6.29, then returns to-3.70 by48.003s. Both cars subsequently experience unresolved planning/braking states. The trace establishes a rapid target reversal and contact sequence, not causality or which driver is to blame. No capped path is active in the sampled incident window, so simply attributing the contact to the coast cap would be unsupported.

The coast-only peak at42.935280s involves car12/car5. Car5 slows from roughly11m/s to2m/s through repeated traffic-unresolved full-brake decisions; car12 closes, alternates its leader classification, and accelerates again as the longitudinal ordering changes. No sampled capped path is active in that window either. This is a second distinct incident, not evidence that the combined version has the same cause.

Next capture the accepted candidate and rejected alternatives at large target reversals, with exact observations, previous path and nominal forecast. Existing failure-only audits cannot explain why an accepted opposite-side path won. Test whether a checked continuation on the passing side can preserve progress before changing sides; avoid fixed lanes, forced rankings, blanket lower speed or globally enlarged gaps. Retain the reviewed50 build while investigating, then use the unchanged controlled tests and matched visual comparison. More seeds, clean rendered-performance measurement, full risk/skill variation and adventure integration remain open.
