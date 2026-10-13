# Bug inbox

Every report goes here, whether it's a snap (docs/73 §4) or something Richard mentions in chat. Each one is reproduced at its spot, fixed and
checked, or closed with a reason. Newest first.

**How to snap in game:**
- **Keyboard:** F9. A note box follows; Enter keeps the note, F9 skips it.
- **Pad:** hold the right stick in for 0.6 s.

Snaps land in `%USERPROFILE%\AppData\LocalLow\Little Road Project\Little Road Unity Trial\snaps\<date>\`. Each is a PNG plus a JSON record with the position, camera pose, area, time of day, build and note.

| # | When | Where | What | Status |
|---|---|---|---|---|
| 11 | 13 Oct, snap 135643 | Bayview race, grid (car −17, 0, −87) | "Tier B models in tier C race": some bodies in the class C field look like class B (sporty coupes and big-wheeled cars) | Open. The record has the track but not the event, class or roster, so check the roster from the save first; it may be the body choice rather than the parts |
| 10 | 13 Oct, snap 135233 | Hillcrest, the terraced slope below the town, at dusk | Terrace retaining walls float above the hillside and don't line up | Open. Pose recorded; recapture in daylight with `--spot-views` |
| 9 | 13 Oct, snap 135145 | Hillcrest Castle, the zip-line climb | Camera sits against the tower wall while climbing, so the car can't be seen | Open. Pose recorded. Needs the climb running to reproduce (the record has no activity field yet) |
| 8 | 13 Oct, snap 135124 | Hillcrest Castle courtyard | Rollo's car (the prototype) sinks slightly into the paving at the front | Open. Pose recorded |
| 7 | 13 Oct, snap 134904 | Bayview, the cutting before the hairpin | Power-line poles and cables run up the bare cliff face | Open. Pose recorded |
| 6 | 13 Oct, snap 134836 | Bayview, the hairpin hillside | Rows of flowering bushes float above the slope; their shadows sit below them | Open. Pose recorded |
| 5 | 13 Oct, snap 134810 | Bayview Halt | Poppy's car is parked on the round flower bed | Open. Pose recorded |
| 4 | 13 Oct, snap 134719 | Valley Viewpoint tunnel, valley end | Missing ground on both sides of the tunnel mouth (blue gaps beside the pavements) | Open. Pose recorded |
| 3 | 13 Oct, snaps 134609 and 134634 | Hillcrest Valley, the S-bend by the farm | Field stripes run across the road | Open. Pose recorded |
| 2 | 13 Oct (snap tool) | All snaps | The snap tool itself: a snap taken within 3 s of the last one shows the previous "Snapped" toast (snap 134634); race snaps don't record the event, class or roster; snaps don't say which activity was running (zip line, climb) | Open. Fixing in the cleanup batch |
| 1 | 12 Oct (chat) | Bayview garage, at dusk | A little feedback from the workshop engine | Open: cause not yet known |
