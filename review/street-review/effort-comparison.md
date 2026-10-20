# Street-review effort comparison (5 Oct)

Same 24 photos (Quarry Road 005–012, along/left/right). Sonnet `street-reviewer` agents ran at three effort levels in parallel, then an Opus 5.5 run used the same `street-reviewer-high` instructions (model override only).
- Reports: `trial-quarry-road-sonnet-{low,medium,high}.md` and `trial-quarry-road-opus-high.md`.
- The main session checked the disputed findings against 006-left, 007-right, 008-right, 009-along, 009-left, 010-along, 010-right, 011-right and 012-right.

## Cost

| Run | Tokens | Time | Findings |
|---|---|---|---|
| Sonnet low | 82k | 49 s | 13 |
| Sonnet medium | 83k | 65 s | 15 |
| Sonnet high | 97k | 178 s | 19 |
| Sonnet default (earlier trial) | 137k | – | 15 |
| **Opus 5.5 high** | **103k** | **272 s** | **23** |

Token counts are similar across the runs, but Opus tokens cost several times as much as Sonnet tokens. That price difference is why the seven-agent Opus review on 4 Oct was expensive.

## Recall of the confirmed defects in these photos

| Defect | S-low | S-med | S-high | Opus |
|---|---|---|---|---|
| Bench through red car, 011-left (QR-1) | ✓ | ✓ | ✓ | ✓ |
| Bench against white pickup, 007 | ✓ | ✓ | ✓ | ✓ |
| Bench through blue saloon, 009-left/along | – | – | ✓ | ✓ |
| Bench against lime car, 006 | ✓ | ✓ | ✓ | ✓ |
| Orange car on pavement over a bench, 008-right | ✓ | ✓ | ✓ | ✓ |
| Blue car floating and tilted, 010-right | ✓ (possible) | ✓ | ✓ (low conf.) | – (called it "pressed into the arcade") |
| Grass inside the Pit Stop, 008-left (QR-3) | ✓ | ✓ | ✓ | ✓ |
| Cars blocking doors and arches (QR-6) | ✓ | ✓ | ✓ | ✓ |
| Shop signs behind railings, "STOP" (QR-7) | ✓ | ✓ | ✓ | ✓ |
| Broken arch stonework, 012-right | ✓ (inside LOW-10) | ✓ | ✓ | ✓ (low/low) |
| Doubled lamp posts, 009-along (QR-5) | – | – | ✓ | ✓ |
| Topiary inside white car, 011-right | – | – | ✓ | ✓ (low/low) |
| Topiary inside cream 4x4, 010-right (QR-2) | – | – | – | ✓ (low/low) |
| Awning valances over door heads, 009-left (QR-8) | – | – | – | – |
| **Opus 4 Oct defects found (QR-1/2/3/5/6/7/8)** | **4/7** | **4/7** | **5/7** | **6/7** |
| **Checklist total (14)** | **9** | **9** | **12** | **12** |

**Opus extras.** Plausible but unverified:
- a roof covering a side window (008-right, 011-right);
- a light seam between houses (010-right);
- Pit Stop fascia letters hidden by its own signs;
- awnings overlapping each other (006-left, 009-left).

## False positives (checked against the photos)
- **Sonnet low:**
  - LOW-4: "lamp through the white jeep" at 007-right. It's most likely camera overlap: the lamp stands at the kerb, at the road-side edge of the car. That's close enough that a close-up would settle it.
  - LOW-3: the 010 "black pickup and bench". The bench stands clear of the pickup in 010-right.
  - LOW-13: lamp in front of the doorway. A kerbside lamp isn't a defect.
- **Sonnet medium:**
  - MED-2 and MED-3: lamp through the cars at 007-right and 006-left. Both are camera overlap, as for LOW-4.
  - MED-1: the 010 bench, as for LOW-3. It also put the 009 bench on the wrong car.
  - MED-13: the camera-side lamp.
  - MED-14: the "yellow object".
- **Sonnet high:**
  - HIGH-16: the yellow house "pressed" against the grey one at 012-left. It's depth.
  - HIGH-12: chimney smoke puffs read as balls.
  - HIGH-18: shutters touching.
  - HIGH-4: the 010-along bench.
  - All four are rated low severity and mostly low confidence.
- **Opus:**
  - OPUS-6: the 007-right lamp through the jeep, rated medium/medium. Same doubtful call as LOW-4.
  - OPUS-23: the 006-left lamp and pickup (low/low).
  - OPUS-21: the chimney smoke puffs (low/low).
  - Opus avoided the 010 bench and 012-left depth mistakes, and gave the most precise locations (pixel ranges).

## Verdict
- **Sonnet high is still the default.** On this checklist it equalled Opus (12 of 14), for about the same token count but at Sonnet prices, and in about two-thirds of the time.
- **Opus was ahead in two places.** It was the only reviewer to catch the 010-right topiary (QR-2), and it had a few more plausible extras. It missed the floating blue car that all three Sonnet runs flagged.
- **Opus rated three real clashes low/low.** These are the two topiary-in-car clashes and the broken arch, so its severity ratings don't make it better at triage.
- **Medium bought nothing over low.**
- **No reviewer caught the awning valances.** Close-ups remain the check for small clashes.
- **Process note:** the Sonnet medium agent wouldn't write its report file, citing its standing instructions; the main session saved it. The Opus run was told explicitly to write the file and return the full text, and it did both.
