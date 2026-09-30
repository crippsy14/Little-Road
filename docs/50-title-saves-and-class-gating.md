# 50 — Title screen, save slots, class gating and road edges

Status, 30 September 2026: built and checked, on branch `music` (not committed yet).

## Title screen and save slots

Richard asked for an intro screen with save management: pick a slot to load from and save to, with autosaves kept apart as games usually do.

- **Title screen** (`TitleScreen.cs`). It shows once per launch, before the world builds. The Bayview theme plays over it and carries on through loading. It offers:
  - **Continue**: the newest autosave, with its slot, class, podiums, credits, car and time.
  - **New game**: pick a slot. A slot already in use asks first; its old game is kept as a `.bak` file.
  - **Load game**: three slot cards, each with **Autosave** and **Saved game**. Loading a saved game that is older than its autosave asks first.
  - **Quit**.

  Test and review runs skip the title: any `--` flag other than `--audio-probe`, or `--adventure-save`. `--title` forces it on; `--title-review <folder>` captures its pages and quits.
- **Slots** (`SaveSlots.cs`). Each of the three slots has two files in the save folder:
  - its **autosave** (`little-road-slot-N-autosave.json`), which is the live store, written after every race, purchase and find as before;
  - its **saved game** (`little-road-slot-N.json`), written from the pause menu.

  The current slot is kept in `little-road-slot.txt`. Every copy goes through a temporary file and keeps a `.bak`.
- **Pause menu > Settings > Save game** writes the game into the saved game of any slot. Saving into another slot moves the game there, autosave included, so it keeps saving where it was last saved. A slot holding a different game asks first.
- **Migration.** The save from before slots, `little-road-unity-save-v1.json`, becomes slot 1's autosave with its original timestamp. The old file stays where it was.
- **Checks.** `AdventureChecks` covers the migration, a new game, Save, save-as-another-slot, and loading a saved game over a newer autosave (with its backup). The title pages (`review/tmp/title-review-1`) and the pause Save page (`review/map-sweep/pause-save-1`) were captured and reviewed.

## Only your own class and below

Richard: a first playthrough should see only Stock-type gear until more is unlocked; the story explaining unlocks comes much later.

- **A class opens only once it has races.** Podiums at both Class C races still complete Class C. But Class B has no events yet, so the ladder waits: "Class C complete · new races and parts are on the way".
- **Shops list the open class and below**, with no locked peek at the next class. The fitting bay shows owned parts the same way, plus whatever is on the car. Menus never name the next class.
- Richard's save owns the Tuner V6 and Tuner turbo, bought while Class B counted as open. They stay owned, but the fitting bay only shows them while they're on the car.

## Road edges at Bayview Halt

"If you go onto the train station area, you can't return."

- **Cause.** Across the Halt quay, the island ground is cut 8 cm below the causeway deck (`CausewayCut`), so the mainline stands about 11 cm proud of the grass. The gravel ramps that let a car climb back on (`Shoulders`) measured the ground with the *valley* height field, which doesn't cover the island. It read the sea floor, so it built no ramps there. The car's body box then stopped dead against the road's side.
- **Fix.** Ramps along the mainline across the quay, from the island's own surface up to the road as built. The ribbon dips to −0.14 m at the south end.
- **Drive test** (`--rejoin-test`: the car starts on the grass either side of the road, at 90°, 60°, 35° and 20° to it, at part and full throttle):
  - Before the fix: 0 of 32 runs got back on.
  - After: 43 of 48.
  - Of the five that didn't: four ran into scenery (the gate pillar and its planter), and one full-throttle start from rest stalled on the smooth gravel ramp, while the same spot passes at part throttle. That one is a car-physics quirk, recorded here and not yet chased.

## Embankments in the valley

The edge audit (`--edge-audit`) now also measures **banks**: the steepest climb back up to the road within 12 m, over 40 cm high. It found the mainline's descent from the tunnel sitting on a ridge with 55–66° banks up to 16 m deep. The road corridors blend to the ground over a fixed width however high the road runs.

Now, where a road runs above the ground, its fill spreads until the bank is about 1 in 4, like a real embankment. The fill only ever raises ground and doesn't touch the raceway, the town hill, the lake or the gorge. Flagged points on the mainline went from 160 to 13; the 13 left are the Halt's sea wall, which has a railing.

A 24-car, two-lap Hillcrest race still finishes all 24 with no assisted recoveries.

## Review tools added

- `--rejoin-test`: the drive test above. It writes `rejoin-test.txt` and captures a picture of each failure.
- `--probe-line "x0,z0,x1,z1;..."`: every solid surface under a line, every 0.25 m.
- `--spot-views "x,z,yaw[,height];..."`: an angled view and a view from above of each spot, with the car parked there.
- `--edge-audit`: now covers the Bayview loop and the raceway, stops at open water, and adds banks and roads within 25 m of a railway.

All of these run through `tools/capture-map.ps1 -Flags @(...)`.
