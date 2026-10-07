# 64 · Farm challenges, the challenge HUD, and fixes to the train race and the castle climb (5 October)

Richard's list after doc 63:
- Farming challenges around Hillcrest: flatten wheat to "harvest" it, knock down apple trees.
- The Slate Quay → Bayview train race was impossible, and the countdown didn't work. All timed events should get a countdown
  like the race start's.
- The Hard Castle Climb: 10 s off the par, and obstacles that are actually in the way.
- Any challenge can be exited from the pause menu, back to whoever offered it. Show prominently that a challenge is under
  way. Research quest and activity UI and overhaul ours ("show it prominently and then have it shrink to the upper corner").

## Research: quest and activity UI

**Forza Horizon's events and PR stunts.** A title card and a 3-2-1 start. During the event, the objective and clock are
pinned in a corner. Speed zones and danger signs show their 1/2/3-star marks, and the pause menu offers Restart and Exit.
Sources: [Speed Zone (FH3)](https://forza.fandom.com/wiki/Forza_Horizon_3/Speed_Zone),
[FH6 PR stunts](https://www.fh6guide.cc/guides/fh6-pr-stunts-guide),
[Game UI Database: FH5](https://gameuidatabase.com/index.php?set=1&sort=2&series=104&tag=132&plat=1).

**Common objective-tracker practice.**
- Keep one tracked goal in a corner, with small animations when it updates.
- New objectives slide in as a toast.
- Progress counters ("3 / 5") beat prose.
- Trackers fade or collapse when they aren't needed, so the screen stays clean.

Sources: [HUD design guide](https://sunstrikestudios.com/en/blog/HUD_design_in_games/),
[UX and UI in game design](https://medium.com/@brdelfino.work/ux-and-ui-in-game-design-exploring-hud-inventory-and-menus-5d8c189deb65).

## The challenge HUD (`ActivityHud.cs`)

Every timed challenge now describes itself with an `Activity`:
- its name, goal and colour;
- who offered it, so Exit can return you there;
- GO time, and either a time limit or a par;
- progress (0–1) with star marks, if it has any;
- a detail line;
- restart and exit actions;
- a `running` check, so the HUD retires it when it's over.

1. **Start.** A big white title card at the top centre: CHALLENGE, the name, the goal, and the star marks or the time. It sits
   over the race-start 3-2-1-GO (`ToyCountdown`, with beeps), and the car is held but can rev.
2. **GO.** The card flies to the top-left corner and shrinks into the **tracker**:
   - the name and the clock, counting down with a limit or up with a par; red, pulsing and ticking in the last 10 s;
   - a progress bar with star ticks;
   - one detail line ("Next star at 50%", "143 m to the courtyard", "gate 2 of 3").

   The tracker pulses and chimes on each star.
3. **No challenge running.** The **quest card** takes the corner. A new objective is announced big at the top (NEW OBJECTIVE)
   for 2.4 s, then shrinks up into the corner card. The card stays a few seconds and fades while you drive; it comes back
   when you stop. It hides during challenges. It replaces the old yellow objective pill.

**Pause menu.** While a challenge runs, the Map tab shows IN PROGRESS · name, with **Restart challenge** and **Exit challenge**.
The header reads "In progress: …". Exit cleans up and puts you back in front of whoever offered it, or at the start flags for
the Gallery Climb. The Pit Stop warp waits until the challenge is over.

**Converted:**
- the Castle Climb (normal and Hard);
- the Quarry Sprint;
- the Gallery Climb;
- Beat the Train;
- the Blast Run (its clock counts down to the shot; the bar shows how far out you are);
- both farm challenges.

The rally lessons keep their own licence screens (they already have retry and exit). The Tip Leap is a stunt, not a timed
event.

## Farmer Giles's challenges (`FarmChallenges.cs`)

Giles is a resident now, by the Golden Acre farmyard.

**The Harvest.**
- **Setup:** Giles bolts a harvester header to your bumper: a red cutter bar with a yellow reel that turns, 6 m wide. The
  wheat (130 × 86 m) regrows for each attempt.
- **Goal:** cut as much as you can in 100 s. Stars at 30, 50 and 70% pay 100, 150 and 250 credits once each, and the best is
  kept.
- **How it works:** the header widens the field's trample footprint (`WheatField.Header`) and cuts with less drag. The chaff is
  soft now: solid, it read as yellow balls.

**Apple Shake.**
- **Setup:** the hay meadow north of the wheat is an orchard now: 52 apple trees in four offset rows. Four bales are left at its
  west end.
- **Goal:** knock as many down as you can in 75 s. Stars at 40, 65 and 90%.
- **How it works:** a tree hit above 2.5 m/s topples away from the car over 0.7 s, and its apples tumble off and roll. Trees
  stay down for the challenge and stand back up for the next. In free roam they stand up again after 40 s.

**Tests** (all in the regression): `--harvest-test` and `--apples-test` drive with a test autopilot through the car's own
throttle and steering. Harvest scored 36% (one star) driving straight rows. `--activity-hud-review` photographs:
- Giles;
- the intro card over the countdown;
- the card flying to the corner;
- the tracker mid-harvest;
- the pause menu with Restart/Exit.

It then exits, and checks that you're back by Giles and nothing is left running.

## Fixes

**Beat the Train.**
- **The race.** The train only runs between Bayview Halt and Hillcrest. At Slate Quay, Mari's menu offered "Race the train to
  Bayview Halt" while holding the train at Hillcrest, kilometres away, so it couldn't be won. The race is now offered only at
  the two stations the train serves.
- **The countdown.** The old 3-2-1 waited `headStart/3` per step, so with no head start each step lasted 0.01 s. Now Bert is
  held long enough that your countdown is at least 3 s, and he leaves `headStart` seconds before your GO. The 3-2-1 is the
  challenge HUD's.
- **Test.** `--train-race-test bayview`: held still through the countdown, then the autopilot (a car rated 107) won in 84.9 s
  against the train's 98 s.

**The Hard Castle Climb.**
- **Par:** 80 → 70 s.
- **Obstacles:** a slalom. Every 38 m (it was 55) a hay bale sits across the middle of one lane (it was at the lane's edge), with
  three cones funnelling in; the next bale is in the other lane. Only hairpins over 40° are skipped (it was 28°).

## Checks
`ActivityChecks` (new; registered in `MigrationBuild`) covers:
- the farm star ladders rise and stay within 100%;
- the time limits;
- the save ids;
- the prizes rise;
- the star count from progress;
- the 3 s countdown.
