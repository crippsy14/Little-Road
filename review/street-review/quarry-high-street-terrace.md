# Quarry Hills street review: High Street (+ Terrace Row, not reached)

Source photos: `review/quarry-hills/streets-1/streets/quarry-hills/high-street/`. Coordinates (x,z) are the manifest look-at point of the photo named, unless marked "est." (estimated).

## Findings

**QH-HS-01: parked cars sit on the front gardens and point at the doors (systemic)**
- Photos: 001-left, 003-left, 004-left, 005-left, 005-right, 008-left, 009-right, 010-left, 011-left
- Coordinates: whole street, for example (-2768.4,-2394.3), (-2807.3,-2402.3), (-2923.7,-2429.1), (-3000.5,-2449.4)
- Kind: bug. Severity: high, because it is visible from every along-street view.
- What I see: nearly every parked car is on the grass strip between the pavement and the houses, angled with its nose against a front door or portico. It blocks the door. Examples:
  - pink car at the blue door (008-left, 004-left);
  - lime car at the orange door (003-left);
  - yellow car at the orange door (010-left);
  - red pickup at the green door (005-right);
  - cream pickup at the purple door (009-right);
  - blue car at the SLATE CRAFTS shop door (001-left).
- Some cars straddle the pavement edge: the pink pickup in 005-left and the yellow car in 006-right.
- Fix: spawn kerbside parking on the carriageway edge, parallel to the kerb, with gaps opposite doors. Keep the front strip for planters and benches. If cars must stay off the road, keep them at least 1.5 m from any door and align them with the street.

**QH-HS-02: lamp posts pass through parked cars**
- Photos: 004-left (yellow car), 005-left (green car), 006-left (yellow car); likely also 008-right (white van roof rack)
- Coordinates: (-2768.4,-2394.3), (-2807.3,-2402.3), (-2846.1,-2410.8), (-2929.8,-2404.3)
- Kind: bug. Severity: medium.
- What I see: the lamp column overlaps the car's bumper or bodywork.
- Fix: add a clearance check (about 1 m) against lamps and benches when placing parked cars. This becomes moot if QH-HS-01 moves cars to the kerb, but lamps would then need the same clearance.

**QH-HS-03: cars overlap benches**
- Photos: 001-right (teal fish van over a bench), 010-right (lilac car's nose into the end of a bench)
- Coordinates: (-2655.4,-2346.8), (-3007.3,-2424.9)
- Kind: bug. Severity: medium.
- Fix: use the same prop-clearance check as QH-HS-02.

**QH-HS-04: shop names are unreadable, and SIOP reads as "STOP"**
- Photos: 001-left, 001-right, 002-left, 003-right, 004-right, 009-right, 000-left
- Coordinates: (-2650.9,-2371.9), (-2655.4,-2346.8), (-2968.6,-2414.3)
- Kind: bug. Severity: medium.
- What I see: the shop name lettering (CHEMIST, LLYFRAU, CIGYDD, POST, WOOL, SWEETS and others) sits behind the first-floor balcony railing and flower pots, so bars and pots cut through it.
- SIOP looks like "STOP" because the rail crosses the "I". A player will read it as a road sign.
- Fix: move the name onto a fascia board above the awning, in front of the railing. Use a font or spacing that keeps the "I" in SIOP distinct.

**QH-HS-05: the arched first-floor loggia module is a black void and repeats**
- Photos: 001-left (two side by side), 002-left, 005-left, 005-right (two side by side), 008-left, 009-left, 010-left
- Coordinates: (-2650.9,-2371.9), (-2812.5,-2377.4)
- Kind: improvement. Severity: medium.
- What I see: the arches open onto pure black, and five ball flowers float on the sill. The same module often sits next to itself.
- Fix: give the recess a back wall with a lit interior colour or a window. Stop the generator placing the same module twice in a row.

**QH-HS-06: two lamp posts about 2 m apart**
- Photo: 009-left
- Coordinates: (-2962.1,-2438.9)
- Kind: bug. Severity: low.
- What I see: a lamp is duplicated where the bunting anchors.
- Fix: dedupe lamps within about 6 m, or anchor the bunting to the existing lamp.

**QH-HS-07: stray door-step slabs lie on the grass**
- Photos: 009-left (two slabs, one under a window), 006-left (one slab between doors)
- Coordinates: (-2962.1,-2438.9), (-2846.1,-2410.8)
- Kind: bug. Severity: low.
- What I see: flat cream slabs that are not under any door.
- Fix: place steps from the actual door positions of the building module, or delete them.

**QH-HS-08: a front door sits above a slate retaining wall with no steps**
- Photo: 004-right (blue door above the wall, with a bench pushed against the wall)
- Coordinates: (-2773.4,-2369.3)
- Kind: bug. Severity: low to medium.
- Fix: add a short flight of slate steps through the wall to the door, and move the bench off the wall face.

**QH-HS-09: a low slate wall stub runs across the front of the teal door**
- Photo: 008-right
- Coordinates: (-2929.8,-2404.3)
- Kind: bug. Severity: medium.
- What I see: the wall and the white van block the door. The pavement also ends in a sloped stub onto sand at about (1180,900) in the image.
- Fix: break the wall with a gate or steps at the door, and finish the pavement edge cleanly.

**QH-HS-10: stray slate pillar stubs**
- Photos: 011-along and 011-right (a lone pillar on the verge at the town exit); 000-left (a two-pillar stub in the grass corner by the plaza)
- Coordinates: about (-3045,-2440) est.; plaza corner about (-2608,-2358) est.
- Kind: bug. Severity: low.
- Fix: delete them, or extend them into a proper gateway wall and pier.

**QH-HS-11: the asphalt ends in a ragged seam into gravel at the end of town**
- Photo: 011-along
- Coordinates: about (-3048,-2450)
- Kind: bug or polish. Severity: low.
- What I see: the asphalt meets gravel with a ragged edge, and both pavements stop abruptly.
- Fix: blend the asphalt into the gravel with a tapered edge, end the pavements with a dropped kerb, and add a "Quarry Hills / Bryn Chwarel" town sign.

**QH-HS-12: bare grass lot between the plaza and the SIOP shop**
- Photo: 000-left
- Coordinates: about (-2605,-2368) est.
- Kind: improvement. Severity: medium.
- What I see: a large, flat, empty lawn right at the town square.
- Fix: add a chapel garden or war memorial, slate-slab benches and a red phone box, or market stalls spilling off the plaza. This is the town's showpiece spot.

**QH-HS-13: the town end opens straight onto an empty meadow**
- Photos: 011-left, 011-right
- Coordinates: (-3038.9,-2460.2), (-3045.8,-2435.6)
- Kind: improvement. Severity: low.
- What I see: the last terrace's gable faces an empty meadow, and there is only a tiny sign board.
- Fix: add a painted gable advert (CABAN or chapel notice), a bus shelter or a slate-fenced allotment to mark the edge of town.

**QH-HS-14: front strips are all lawn**
- Photos: whole street
- Kind: improvement. Severity: low.
- What I see: every frontage has the same green lawn strip.
- Fix: for a Welsh slate terrace, replace some lawn with slate-flag forecourts, low slate front walls with gates, bins and milk crates. This would also give parked cars a natural home on the road.

## What works
- The terraces are colourful and varied: shutters, awnings, window boxes and dormers.
- The bunting and blue lamps give a strong high-street identity.
- The plaza with the clock-tower chapel, the market stalls and CABAN reads well.
- Slate retaining walls between plots look right.
- The long downhill view framed by the hills is lovely.
- The main problems are prop placement (cars, lamps, benches) and illegible signs, not the buildings.

## Continue from here
- Reviewed: high-street 000-006 and 008-011 (along, left and right for each).
- Not reviewed: high-street 007 (along, left, right) and all of terrace-row 000-006 (21 photos).
- Close-ups planned but not taken (no capture runs were made):
  - lamp vs car clipping at 004-left, 005-left and 006-left;
  - white van vs lamp and wall at 008-right, about (-2930,-2404);
  - red pickup at 005-right, about (-2812,-2377), possibly floating on the bank.
