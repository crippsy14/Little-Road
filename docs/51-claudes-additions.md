# 51 — Claude's additions

On 30 September 2026 Richard said: "go to town adding whatever you like to this game... have fun and be brave and bold!" This doc covers what I added, on branch `claude-additions` (not committed yet). Each feature below works in free roam only. Races are unchanged.

## Speed traps (`SpeedTraps.cs`)

There are seven cameras on seven roads:

| Camera | Area |
|---|---|
| Coast Road | Bayview |
| The Causeway | Bayview |
| Valley Road | Hillcrest Valley |
| Quay Straight | Hillcrest |
| Farm Lane | Hillcrest Valley |
| Falls Road | Hillcrest Valley |
| Raceway Road | Hillcrest Valley |

**Placing them.** Each camera stands on the straightest point of its road: the point with the least heading change within 70 m either side, clear of road ends. A chequered strip across the road marks where it measures, and a yellow camera stands on a pole beside it.

**Stars.** Driving through a camera flashes it and gives a shutter beep, and a card shows your speed, your stars and your best. Each star pays once: 50, then 100, then 150 credits. Your best speed through each camera is kept in the save, in `AdventureProgress.traps`.

**Where the stars come from.** The marks are computed from the road, not typed in. `SpeedProfile` works out each point's corner limit (from curvature and grip), runs a forward pass of the car's real acceleration envelope with the grade, and runs a backward pass for braking. The trap takes the fastest speed reachable at the camera over a 700 m approach, from either direction.

| Star | Set just under the reach of |
|---|---|
| 1 | the stock car (×0.96) |
| 2 | the Street kit (×0.96) |
| 3 | the best Class C part in every slot (×0.95) |

The marks today are 47–53, 53–60 and 67–76 mph, depending on the approach. They follow the balance and the roads automatically if either changes.

**Map and records.** A camera appears on the map once you've seen that spot, and turns mint at three stars. The pause menu has a new RECORDS tab with a card per camera showing the best speed, the stars and the marks.

**Tests.** `--trap-test` drives through each camera from 150 m back, along the road. Six of seven cameras detected and logged a pass. The two weak runs (the valley miss and a slow raceway pass) were the autopilot's steering on curvy approaches, not detection. Editor checks cover the placement, the marks, pay-once and save validation.

## The horn (`Audio/HornSynth.cs`, `Audio/Horn.cs`)

**Controls.** H or L3 (the left stick press, as in Forza) in free roam.

**The sound.** A car horn is two notes a major third apart (most car horns are a pair of tuned disc horns). Each note is a rounded square wave through a body resonance. The pitch follows the car's body: bubbles toot high, and minibuses and campers blare low. It plays at about the engines' level.

**Who answers.**
- Up to two nearby folk answer in speech bubbles (`NpcChatter.HornReplies`).
- Some passing traffic beeps back.
- Traffic held up behind you gives a polite "beep-beep" after a second.

**Bert's train whistles.** The whistle is a chime of three pipes in a minor triad, breathy with steam noise. It swells in and bends up to pitch as the steam builds. It blows long-short when Bert pulls out of a station, and whenever you honk within about 110 m of the train.

**Implementation notes.**
- Horns and the whistle only hold an audio voice while they're sounding. Unity mixes about 32 real voices, and a dozen idle horns would crowd out the engines.
- Each horn lives on its own child object. An audio filter runs on every AudioSource of its GameObject, so a horn on the car itself would multiply the engine's sound.

**Tests.** `sound-harness.ps1 -Filter horns` renders a toot, a held horn, a bus horn and the whistle. `--horn-test` honks in Hillcrest's market square; Gelato Gina answered "Toot toot! Morning!"

## Beat the Train (`TrainRace.cs`)

**Starting a race.** Stanley at Bayview Halt, or Stella at Hillcrest station, offers "Race the train to...". This appears once you've visited Hillcrest. If the train is out on the line, Stanley radios Bert and it's at the platform after a quick fade. Bert then leaves, whistling, and your GO follows a countdown.

**The race.** You race the real train by road to the other station's forecourt: over the causeway, through the headland tunnel, down to the valley and along the quay, or back the other way.

**Making it fair.**
- The line is longer than the road: 1,751 m and 1,880 m against 1,304 m.
- So Bert opens the regulator to 24 m/s (54 mph) and gets a head start.
- The head start is set so the train arrives just as a stock car would: at 25% over the stock car's ideal time for the road route (`SpeedProfile`, 78–79 s).
- If even 24 m/s can't make that, the train goes a little quicker with no head start.

**Rewards and records.** The first win each way pays 300 credits. Best times are kept in the save and shown on the Records tab. The objective line and a map star show the goal during the race.

**Tuning with `--train-race-test`.** An autopilot drives the road route at 95% of the ideal profile, in a car rated 107 (just above stock).

| Margin | To Hillcrest | To Bayview |
|---|---|---|
| 12% (train at 89 mph, first design) | lost by 22 m | lost by 160 m |
| 20% (head-start design) | lost by 80 m (fumbled the tight turn into the station road) | won in 82.5 s |
| 25% | the setting kept | |

## Photo mode (`PhotoMode.cs`)

**Controls.** P or D-pad Up opens it in free roam.

**The camera.** The world freezes, the HUD, prompts and minimap go, and a camera orbits your car.

| Action | Keyboard | Gamepad |
|---|---|---|
| Orbit | arrows or mouse drag | right stick |
| Closer | W / S (or scroll) | left stick |
| Height | Q / E | left stick |
| Zoom | Z / X | LB / RB |

**Looks.** There are five looks: Natural, Golden hour, Postcard, Faded film and Black & white.
- They use a post-processing volume of their own, which sits above the world's grade and is only on while the mode is open. They nudge the grade in `WorldLook`; they never replace it.
- Depth of field focuses on the car, and toggles with V or D-pad Left.
- A "Greetings from ⟨area⟩" postcard frame with a stamp toggles with G or X.

**Saving.** Photos save to `Pictures\Little Road\` at screen resolution. The frame is captured with the photo; the help and the look name are not.

**Test.** `--photo-review` captures each look on the causeway.

## Pip's photo requests (`PhotoRequests.cs`)

Pip, the lighthouse postie, makes Bayview's postcards and wants new photos of six sights:
- the lighthouse;
- Bayview Halt;
- Hillcrest Castle (it counts from Wren's viewpoint, 474 m away);
- Silver Falls;
- Bert's train;
- the balloons.

**What counts.** A photo counts for each sight that is well framed (the middle 70% of the picture) and within reach. It must also be in clear view: some part of the sight (the point itself, above it or either side) with nothing solid in the way. Railings and posts under 0.6 m thick don't count as in the way. Each sight pays 100 credits once.

**Where it shows.** Pip's dialogue lists what's still wanted, and the Records tab has a chip per sight.

**Test.** `--photo-requests-test` shoots each fixed sight from a natural vantage point: 5 of 5 count.

## Fireworks over Bayview harbour (`Fireworks.cs`, `Audio/FireworkSynth.cs`)

**When.** Once it's fully dark (`TimeOfDay.Night` above 0.9), a one-minute show goes up from a barge in the harbour, then again every three minutes. Nights run three times faster than days, so a clock time would last only seconds. Within 1.5 km a card announces "Fireworks over Bayview harbour!", so distant booms make sense. (It used to start at 0.75, still dusk; see Richard's play-test below.)

**The show.**
- It's toy fireworks: chunky glowing spheres rather than fine sparks.
- The bursts are peonies, rings and gold willows that crackle, with a rapid finale.
- Each burst has a coloured light that lights the water.

**Sound.** The launch whoosh plays at the barge. The boom arrives at the speed of sound, so from across the bay you see the burst before you hear it. There's a crackle for the willows.

**Getting the colour right.** It took three tries, and the lessons matter for any future glowing thing at night:
1. **Lighting.** A burst's own point light lit its sparks white. The sparks now have a black base and use emission only.
2. **Exposure.** Night exposure lifts the dark sky about 5×, so emission at 7×, 2.4× and even 1.6× clipped to pure white (254, 254, 253 in the capture).
3. **The answer.** 0.5× emission lands at about 2 after exposure: coloured, and it still blooms.

**Test.** `--fireworks-review --fireworks-now --time night` captures the show from the promenade.

## Richard's first play-test (1 October)

Richard played the claude-additions copy and sent a list. Each item, and what changed:

1. **"The speed traps stop the car when you succeed at them like a crash into the line."**
   - The cause was the confetti. Each piece is a primitive whose collider was removed with `Destroy`, which waits until the end of the frame, so for one physics step the burst was a wall of boxes.
   - Confetti, firework sparks, the trap's boost blocks and the train's steam and dust puffs now use `DestroyImmediate`.
   - `--trap-test` now checks speed 0.6 s after each camera: the car carries on at speed through all seven.
2. **Road joins with gaps and markings that didn't meet.** The valley's junctions now come from a road network; see doc 52.
3. **"Photo mode didn't seem to work on the up d pad."** The press that opened photo mode was read again in the same frame by photo mode's own input, which closed it. Photo mode now ignores input on the frame it opens.
4. **"Something weird around dawn / dusk"** (outside the Bayview garage).
   - Two things fitted. The cricket layer was muted, as Richard suggested.
   - The fireworks were going up at dusk, booming in from the harbour with no explanation. They now wait for full dark and announce themselves (above).
5. **Driving off during the countdown.** For the Beat the Train countdown and the Castle Climb's "get ready", the car is now held still (`RoadCar.holdStill`), but the clutch is in, so it revs (`GearState.Held`, up to the limiter).
   - `--train-race-test` floors the throttle through the countdown: 0.00 m moved, 6400 rpm.
   - `--climb-review` does the same: 0.00 m, 6400 rpm.
   - A `DrivetrainChecks` check covers the held gearbox.
6. **"The tuner engine going up in revs sounded like two different noises combined."**
   - The Skyline recording only pulls to 3,760 rpm; the Tuner V6 revs to 7,600. Above the recording, the "nearest cycle" ping-ponged between two or three recorded cycles, a flutter 9–10 dB over the cycle's own note.
   - It now plays a sustain loop of the recording's top cycles in recorded order. Doc 47 has the details, including the second round below.

**Added mid-list: a glowing target for the finish** (`FinishBeacon.cs`).
- The Castle Climb and Beat the Train show a column of gold light where the finish is, tall enough to see over the town and the castle walls.
- A soft ring lies on the ground at the edge of the finish zone, laid onto the terrain point by point.
- **The material.** It uses the night light trails' additive unlit shader. URP Lit transparent materials rendered either solid in the player build or as a dull tint.
- **Night.** The glow drops to 30% at night, when exposure lifts it about 5×.
- **The castle's beacon** stands 6 m from the court's centre towards the gate, clear of the donjon that hid it. Its 10 m ring lies wholly inside the 16 m finish, so driving into the ring always counts.

**Later the same day: "the revs sound great up to about 80% of max and then after it goes very high pitch and changes to be weird."**
- Above the recording, the Skyline's cycles are played faster to reach the revs. At 80% of the V6's redline that's 1.67×, which lifts the exhaust's resonances with the note: a chipmunk.
- Above 1.5× the grains are no longer sped up further. Their pulses are laid closer together instead (pitch-synchronous overlap-add), so the note keeps rising and the pipes stay put.
- A capture of the game's own audio also showed the sustain loop's seam: a 4 dB wobble twice a second. The loop is now walked back and forth with level-matched cycles.
- Doc 47 has the measurements.

**Then: the prototype.** Richard: "create a super detailed prototype model with a really detailed livery... make that model the owner of the castle and a teaser". The Regale is Duke Rolo's car now; see doc 53.

## Also fixed along the way
- **Wheels that didn't turn.** Richard: "the wheels don't turn when driving so it looks like cars are floating along the surface".
  - **The cause:** BodyMotion lifts the wheels out of the body so they don't lean with it. It moved everything named `Wheel_...`, which included the meshes (`Wheel_FL_Mesh`), so the meshes were pulled out of their pivots. The pivots spun at the right rate, but the tyres sat still.
  - **The fix:** it now moves only the pivots.
  - **Rolling radius:** each body rolls its wheels at its own radius (the axle height: 0.345 m on the Regale, 0.39-0.43 m on the family cars), where every car used to assume 0.425 m.
  - **Check:** `WheelChecks` builds cars with BodyMotion attached and confirms a quarter turn of the pivots turns every tyre a quarter turn.

- **Grass on the causeway.** Tufts floated at railing height over the causeway (seen in photo mode). The deck is road, so grass had been planted on the parapet tops. Grass now keeps off the causeway.
- **Save-slot check.** The save-slot "newest" check could tie when two saves landed in the same clock tick. Ties now go to the slot being played.

## Controls added

| Action | Keyboard | Gamepad |
|---|---|---|
| Horn | H | L3 |
| Photo mode | P | D-pad Up |

Both are on the pause menu's Controls tab.

## The Duke's drive (`DukeDrive.cs`)

**Parked, 2 October.** Richard: the town traffic isn't built for a car doing something at pace, so the drive now waits for a proper traffic overhaul. It runs only with `--duke-drive`, and the Duke stays at the castle. In its place: the Duke as the final boss (see next-steps).

Added on 1 October, while Richard was out ("add whatever you like... be brave").

**The drive.** Every few minutes of free roam, Duke Rolo takes the Regale out:
- out of the castle courtyard and down Via Crest, through the market square;
- along the quay and onto the mainline, into the headland tunnel;
- out of sight in the tunnel he turns round, then drives the whole way back up to his spot.

He first leaves 150 s in, then every 330 s; never during the Castle Climb or Beat the Train. Starting the Climb calls him home at once, because it closes his road. Within 900 m you get a card: "Duke Rolo is taking the Regale out for a spin!" While he's out, his talk prompt at the castle is hidden.

**His driving.**
- **Route:** 3.5 km, about 3 minutes there and back, keeping right.
- **Speed:** up to 20 m/s in town and on Via Crest, 24 m/s on the mainline. Corners are taken at 7 m/s² across, walking pace in the courtyard and through the turn.
- **Traffic:**
  - He slows behind you and the valley traffic.
  - After a moment held up, he gives a polite double toot.
  - The cruisers give way to him as they do to you (`RoadCruiser.GiveWayTo`), and turn round if he holds them up, so the two can't deadlock.
- **Wheels:** they roll with his speed and steer into the bend ahead.

**His engine.** The first top-class engine you can hear: the E-class V8 recording through the paddle box, from its own 3D source (`DukeEngine`).
- It revs out to 8,500 rpm when accelerating, with shift cuts.
- When cruising it short-shifts while the next gear stays above 3,600 rpm, so it sits at about 4,800 rpm at 20 m/s.

**Shadow the Duke.** Stay within 45 m of him from the castle gate down to the tunnel (4 s of slack in all) and he pays 250 credits, once. The record sits with the train race's in `AdventureProgress.traps` as `duke_shadow`. His dialogue mentions it.

**Tests.**
- **Editor:** `DukeDriveChecks` covers:
  - the route out and back, turning in the middle of the tunnel;
  - at most 24 m/s, walking pace in the courtyard;
  - on the road everywhere (at most 3.4 m from a centreline, in his lane);
  - save validation.
- **Review:** `capture-map.ps1 -Name x -Rendered -Flags @('--duke-drive-review'[,'--duke-shadow'])` films the drive with a chase shot every 10 s and logs every 5 s.
- **Results:** duke-drive-3 completed the round trip in 192 s. duke-drive-4 paid the shadow at the turn (104.5 s), with one toot behind Via Crest traffic on the way back.
- `--no-duke-drive` leaves him parked.
