# Quarry Road (Quarry Hills) street review

Photos: review/quarry-hills/streets-1/streets/quarry-hills/quarry-road/. Close-ups: review/quarry-hills/sr-qrd-1/.

## Findings

**QR-1. Bench placed inside a parked car.** Bug, high.
- Photos: 011-left, sr-qrd-1/redcarbench. Location about (-2521, -2357).
- The red resident car (it has a plank load) sits right on top of a pavement bench, so the bench slats pass through the car body.
- Similar near-clashes:
  - 004-right, about (-2250, -2296): the bench's back touches the pink jeep's flank.
  - 007-right, about (-2361, -2318): the bench is jammed against the front of the white pickup.
  - 008-right, about (-2405, -2332): the orange car's nose sits over a small bench by a door.
- Fix: the residents' parking placement should reject any spot within about 1.5 m of a bench, planter or lamp, or `Bench()` should check parked-car spots. Run cars after the props are placed and test their bounds against them.

**QR-2. Topiary planter inside a parked car.** Bug, medium.
- Photos: 010-right, sr-qrd-1/bluecar. Location about (-2483, -2333).
- The ball of a potted topiary pokes up through the bonnet of the cream 4x4, and the tub is hidden inside the car.
- Fix: the same clearance test as QR-1, covering garden planters and bushes. Alternatively, skip the planter when that front garden has a car.

**QR-3. Grass growing inside the Pit Stop.** Bug, high (it is a key building).
- Photos: 008-left, sr-qrd-1/pitwide and pitbays. The Pit Stop is at (-2388, -2352).
- Grass tufts sprout across the forecourt and inside the garage and body-shop bays. The bay floors read as lawn, and the body shop's paint spot sits on grass.
- Fix: exclude the Pit Stop footprint plus about 3 m from the tuft and grass scatter. Pave the forecourt (concrete or slate setts).

**QR-4. No driveway into the Pit Stop.** Improvement, medium.
- Photo: pitwide. Location about (-2390, -2340).
- The pavement runs unbroken past the forecourt, so the player crosses the kerb and the grass verge to reach the bays.
- Fix: add a dropped-kerb crossover and a paved apron from the carriageway to the canopy.

**QR-5. Lamp posts doubled up.** Bug, medium.
- Photos: 009-along, 010-right, sr-qrd-1/doublelamps. Location about (-2468 to -2478, -2337), north side.
- Two identical lamps stand about 3–4 m apart on the same pavement. This is probably where the approach lamp run (`quarry.Length-1400` to `-430`, every 24 m, `QuarryHillsTown.cs` line 170) overlaps the town street lamps.
- Fix: end the approach lamps where the town lamps begin, or skip any lamp within 10 m of another.

**QR-6. Residents' cars block front doors and touch facades.** Bug, medium.
- Photos and locations:
  - 003-right, about (-2214, -2280): a dark car in a carriage arch, right against the red door.
  - 011-left, about (-2521, -2357): the red car in front of the purple door.
  - 007-right: the teal pickup in front of the green door.
  - bluecar close-up: the blue car's rear bumper touches the wall plinth.
- The front gardens are shallower than the cars.
- Fix: park cars parallel to the street at the garden's road edge, offset from door centres. Or give some houses a slate-paved hard-standing with a dropped kerb.

**QR-7. Shop signs hard to read; "SIOP" reads as "STOP".** Improvement, medium.
- Photos: 012-along, 012-left, 009-left, 011-right, sr-qrd-1/siopsign. The SIOP shop is about (-2557, -2360), at the square.
- Shop names sit on the balcony front behind the railing bars and flower boxes, so the bars cross the letters.
- "SIOP" by the junction reads as "STOP" from the road.
- Fix: put the sign on a fascia board between the awning and the balcony, in front of the railing. Consider "SIOP Y GORNEL" or a hanging sign so it isn't mistaken for a road sign.

**QR-8. Awning valances hang over the shop door heads.** Bug, low.
- Photos: siopsign, 009-left.
- The scalloped discs cover the top of the doors.
- Fix: raise the awnings about 0.4 m, or shorten the discs.

**QR-9. Town entrance is plain.** Improvement, medium.
- Photos: 000-along, 001-along, 000-left/right, 001-left/right. Location about (-2100 to -2180, -2240 to -2275).
- The road arrives through bare grass verges with one lamp. It is framed by two blank end-of-terrace gable walls, which have only shuttered blanks.
- Ideas:
  - A slate "Croeso i Quarry Hills" sign on a dry-stone wall.
  - Slate field walls along the verges.
  - A painted advert on a gable (an old ghost sign, "Caban" or "Llechi").
  - A bus shelter or a chapel noticeboard.
- These would give a proper gateway and Welsh character.

**QR-10. Pavement ends abruptly.** Bug/improvement, low.
- Photos: 002-left, 002-right. Location about (-2178, -2268) on both sides.
- The town paving stops in a hard square edge and becomes a sandy verge.
- Fix: taper the paving, or end it at a kerb with a lamp or bench. A slate wall that starts here would also mark the town edge.

**QR-11. Empty lawn by the square.** Improvement, medium.
- Photos: 012-left, 012-right, siopsign. Location about (-2565, -2362).
- There is a large bare lawn between SIOP and a sideways-on yellow cottage whose blank gable faces the road. Opposite (north side) is another empty lawn, with only a bench and a post.
- Ideas: a chapel graveyard with slate headstones, a war memorial, a slate-walled garden with benches, or parked cars for the Caban. This is the arrival view of the square.

**QR-12. Square flags aren't Welsh.** Improvement, low.
- Photos: squarestalls, 012-along. Location: the square at (-2600, -2350).
- The flags are plain green/red/white stripes.
- Fix: a Y Ddraig Goch flag (green and white with a red dragon) would read as Wales at once.

**QR-13. Lamp shows dither speckle at close range.** Bug, low.
- Photo: 000-along. Location: the lamp at about (-2103, -2235).
- The lamp in the foreground and nearby trees show a stipple/dither pattern, likely an LOD cross-fade caught mid-fade.
- Check whether this is visible in motion.

## What works
- From 002 to 011 the street is lively: varied terraces, shutters, window boxes, shops with awnings, lamps, parked off-roaders, bunting and a strong view to the clock tower.
- The retaining walls and terraces up the slope at 001–004 look good.
- No floating buildings or terrain poking through the road were seen.
- The repetition of the terrace types is acceptable thanks to colour variety.

## Continue from here
- Reviewed: 000–004 (all three views), 007-right, 008–012 (all three views), and the close-ups in sr-qrd-1.
- Not reviewed (the image reads failed): 005-along/left/right, 006-along/left/right, 007-along, 007-left.
- No close-ups were left planned. A second run (sr-qrd-2) could check:
  - the white pickup and bench at 007-right;
  - the orange car and bench at 008-right;
  - the Pit Stop's red van against the canopy column (pitwide; possible intersection).
