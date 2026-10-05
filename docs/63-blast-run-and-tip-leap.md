# 63 · The Blast Run and the Tip Leap (5 October)

Richard gave me an hour of free time ("let your creative juices flow"). I picked two of Quarry Hills' planned activities
(doc 59, "things to do") that weren't built yet and are the most fun to watch: a race against a quarry blast, and a
danger-sign super jump. Both are in free roam in Quarry Hills; races are unchanged. Nothing is committed.

## The Blast Run (`BlastRun.cs`)

**Starting it.** Dai the Dumper now waits on the quarry floor at the foot of the haul road (a new resident, "blast"). The
dumper that grinds up and down the haul road is now his mate Emrys, and it waits in the cab during a run.

**What happens.**
1. You and Dai line up side by side at the foot of the haul road. The blasting siren starts winding up with the countdown.
2. GO: race him up the haul road, over the crest and past Chapel Street's mouth (the safe line, 375 m). The siren wails as you
   climb, and falls silent seven seconds before the shot: the last warning.
3. The shot, on time: the real blast sound (the ambience's `BlastSynth`, its shot now timed on demand), a flash, and a line of
   dust bursting off a bench of the big gallery face across Glas. The dust boils up into a column about 90 m high, and a front
   rolls across the lake and the floor towards the haul road. Rock is flung out over the water, and 16 boulders are shaken off
   the cutting onto the haul road's lower reach behind you. The camera jolts with distance (`CameraShake.Kick`).
4. Past the safe line before the shot: cleared, and your time goes on record. Ahead of Dai too: his 300 credits, once
   (`trial_blast_run`). Still in the zone at the shot: the dust swallows you.

The boulders stay on the road for 90 s (for the photo), then the quarry clears them and Emrys gets back to work.

**The times come from the road**, as Beat the Train's do. The stock car's ideal time over the course (`SpeedProfile`) is
23.5 s. Dai takes 1.3 times that (31 s), and the shot comes at 1.6 times it (38 s). On road tyres on the gravel, beating Dai
should take clean driving or Gwen's gravel tyres. **Not yet tried in a physics drive**: the test carries the car.

**Test.** `--blast-test`: talk to Dai, start, carry the car up the course a fifth faster than Dai, wait for the shot. It passed:
cleared in 24.4 s, Dai beaten, 300 credits, shot fired, 16 boulders. With `--trial-captures` it also photographs the shot from
above the haul road (`review/quarry-hills/blast-5`). It's in the regression (`blast`).

**Editor check** (`QuarryHillsChecks`): the siren wails, is silent in the last 7 s, and the shot lands exactly when asked;
the prize and record ids pass save validation; Dai is slower than ideal and the shot later than Dai.

## The Tip Leap (`TipLeap.cs`)

**Where.** Take the Blondin up to the big tip. Run 150 m down its ridge to the slate kicker (10 m long, about 11 degrees, with
tarmac grip) at the floor-side end, 41 m above the quarry floor. Yellow DANGER · TIP LEAP boards and chevrons mark it, and the
map lists it.

**Scoring.** The distance is measured from the kicker's lip to where you touch down. Stars at 45, 75 and 105 m pay 75, 150 and
250 credits once each, and your best is kept (the speed traps' record). A card shows the distance, the stars and the next mark.

**Landing.** A toy-physics assist eases the fall to 9 m/s near the ground, so you land and roll out. Without it, the drop
would fold the car.

**Test.** `--leap-test`: the car at 32 m/s (72 mph) on the run-up, physics from there. It flew 88.6 m (two stars, 225
credits) and landed upright. A mid-air photo is in `review/quarry-hills/leap-2`. It's in the regression (`leap`).

## Changed along the way
- `BlastSynth.Fire(shotAt)`: a shot at any time 15 s or more after the siren starts; the siren holds longer and still runs down
  7 s before. `Ambience.FireBlast(at,shotAt)` plays it from the charge, and the random ambient blasting waits until it's over.
- `CameraShake.Kick(amount)`: a jolt from the world.

## Ideas for next time
- Dai as a recruit once teams open (doc 59: "beat him in the Blast Run").
- A physics autopilot run of the Blast Run on road and gravel tyres, to set Dai's time from real driving.
- A landing ramp below the Tip Leap so the longest jumps land on a slope, and the Tip Trails down the other tips.
- The Blast Run and the Tip Leap on the pause menu's Records tab (today they show on the map's place list).

## Regression (sr14)
Every test passes, including the new `blast` and `leap` tests: both smokes, all six lessons gold, climb, sprint, carvings 12/12,
Blondin, wayfinding, festival. The sound smoke reads 6833. The area audit is unchanged from sr13.

The exception is `world-tiles`: that game crashed natively inside Unity while streaming (no script exception; the crash
report is in `%TEMP%/Little Road Project/Little Road Unity Trial/Crashes`, 17:54). Run alone straight after, it passed twice
(`wt-recheck-1/2`, 6 s each). The same crash signature hit the sr13 run as it quit, after its pass was logged, and a similar
one on 3 October. It looks like an intermittent native crash under five parallel games, not today's changes, but it's worth a
look.
