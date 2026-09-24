# Passing-space anticipation47: controlled overtakes fixed, field slower

23 September 2026. Build47 passed320 checks. The first build caught a test heading that was not aligned with the opponent's own road tangent; the test was corrected to use that tangent and a floating-point tolerance. No runtime behaviour was changed to satisfy it.

The opt-in prediction shifts an ahead car's preference position using its observed road-relative lateral velocity, bounded by a1.25s closing horizon,0.75s decay and1.5m displacement. Hard present side reservations and46's swept side-entry check remain. All cars receive the same behaviour; car0 has no special advantage.

## What improved

The unchanged controlled regression suite passes72/72 checks. This resolves the three45/46 failures:

- Queue: both slower cars are passed on the straight; the second sustained pass is confirmed at8.3000s rather than12.3666s. Car0 completes in26.6497s instead of27.8580s.
- No-room: car0 completes in31.2663s instead of36.3422s, within the original reference allowance. The narrow corridor contract still passes.
- Braking leader: car0 completes in27.4830s instead of29.9080s, within the original allowance; sustained pass at5.7333s.

All10 controlled runs finish cleanly, with no contacts, body departures or recoveries. Rear-merge deadlines, corner gates and solo controls pass.

## Why it is not promoted

The24-car result is slower:2240.865967 combined car-seconds versus46's2235.475616, missing the predeclared2215s target. Car0 reaches23rd at checkpoint1 and finishes9th, worse than46's20th/8th. All24 finish without road departure/recovery; there is one light pair contact, peak0.25293m/s. Active unresolved samples rise374→495. The field and checkpoint deadlines still pass, but these do not substitute for the desired racing progress.

The disabled control has exact samples, context, incidents, checkpoint events and results, and an exact common raw-motion prefix through the last finisher. The original raw-array parity check remains false:46 captured2520 motion rows through104.0050s while47 captured2496 through103.0053s; the final finish was103.6884s. The additional24 rows in46 are entirely after every car finished. This recording-tail distinction is documented, not silently erased from the raw gate.

48 tests anticipation only for the car whose stopping-distance envelope limits forward pace in the own corridor, retaining present-position preferences for surrounding traffic. This follows the measured single-queue success without assuming every nearby vehicle's lateral movement should shift the pack's preferred gaps. Keep47 available as a separate experimental mode; no production or desktop change.
