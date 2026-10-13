# Bug inbox

Every report goes here, whether it's a snap (docs/73 §4) or something Richard mentions in chat. Each one is reproduced at its spot, fixed and
checked, or closed with a reason. Newest first.

**How to snap in game:**
- **Keyboard:** F9. A note box follows; Enter keeps the note, F9 skips it.
- **Pad:** hold the right stick in for 0.6 s.

Snaps land in `%USERPROFILE%\AppData\LocalLow\Little Road Project\Little Road Unity Trial\snaps\<date>\`. Each is a PNG plus a JSON record with the position, camera pose, area, time of day, build and note.

| # | When | Where | What | Status |
|---|---|---|---|---|
| 11 | 13 Oct, snap 135643 | Bayview race, grid (car −17, 0, −87) | "Tier B models in tier C race": some bodies in the class C field look like class B (sporty coupes and big-wheeled cars) | Not a bug: class C fields use class C bodies only. The sporty ones are the top C cars (Sprint hot hatch, Comet coupe, Rally saloon, Roadster hardtop, Zenith GT, sold at Hillcrest's body shop); their B kits (`<id>_tuner`, doc 65) aren't in the game yet and will be B-only. A build check now fails if a B body or kit ever lands in a C field. Richard to say if the top C cars should be kept to higher-star C races |
| 10 | 13 Oct, snap 135233 | Hillcrest, the terraced slope below the town, at dusk | Terrace retaining walls float above the hillside and don't line up | Fixed 14 Oct (checked at the snap's pose): each wall stands on the ground along its whole length, and the vine and lavender rows sit at their lowest ground |
| 9 | 13 Oct, snap 135145 | Hillcrest Castle, the zip-line climb | Camera sits against the tower wall while climbing, so the car can't be seen | Fixed 14 Oct (`--zip-ride-review`, both directions): near a tower the camera frames the deck opening from outside; mid-cable it chases. Also: the trolley now waits at whichever end calls it (no empty run over) |
| 8 | 13 Oct, snap 135124 | Hillcrest Castle courtyard | Rollo's car (the prototype) sinks slightly into the paving at the front | Fixed 14 Oct (checked at the snap's pose): parked residents now clear the ground under all four corners, not just the middle |
| 7 | 13 Oct, snap 134904 | Bayview, the cutting before the hairpin | Power-line poles and cables run up the bare cliff face | Fixed 14 Oct (checked at the snap's pose): no pole more than 4 m above the road, no span that climbs more than 3 m |
| 6 | 13 Oct, snap 134836 | Bayview, the hairpin hillside | Rows of flowering bushes float above the slope; their shadows sit below them | Fixed 14 Oct (checked from the snap's pose and above): each row is tilted to the ground along it and lowered until no part floats; rows where the ground bends too much aren't planted |
| 5 | 13 Oct, snap 134810 | Bayview Halt | Poppy's car is parked on the round flower bed | Fixed 14 Oct: her car and a flower bed were both placed at z −111; she now parks in the free bay at z −134 |
| 4 | 13 Oct, snap 134719 | Valley Viewpoint tunnel, valley end | Missing ground on both sides of the tunnel mouth (blue gaps beside the pavements) | Fixed 14 Oct (both portals checked): the terrain hole for the bore now stops 1 m past the portal face instead of 5 m |
| 3 | 13 Oct, snaps 134609 and 134634 | Hillcrest Valley, the S-bend by the farm | Field stripes run across the road | Fixed 14 Oct (checked at the snap's pose and from above): fields test their whole rectangle for roads (`FootprintClear`) |
| 2 | 13 Oct (snap tool) | All snaps | The snap tool itself: a snap taken within 3 s of the last one shows the previous "Snapped" toast (snap 134634); race snaps don't record the event, class or roster; snaps don't say which activity was running (zip line, climb) | Fixed in 33ab1dc (reaches the play copy with the next publish) |
| 1 | 12 Oct (chat) | Bayview garage, at dusk | A little feedback from the workshop engine | Open: cause not yet known |
