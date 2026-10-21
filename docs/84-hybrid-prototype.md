# 84. The hybrid prototype (21 October 2026)

This is Richard's idea from 20 Oct (doc 83, "Hybrids"): some engines need a battery. The battery gives better traction
off the line, but it can run flat. A battery that isn't managed leaves a heavier car with no benefit. This doc covers the
first step, prototyped behind `--hybrid`, off by default.

## What's built
- **`HybridPower`** (`unity/Assets/Scripts/HybridPower.cs`): one pure step per physics tick, the same for the player
  and the AI.
  - **Deploy:** at full throttle (90% or more) and below 26 m/s (about 58 mph), the e-motor adds drive from the battery.
    That covers launches and corner exits, not the straights. Its push is the motor's power at that speed, capped at
    2 m/s² by the motor's torque near standstill. It goes in before the grip budget (`DriveModel.Forces`), so the
    tyres clip it like any other drive. It also keeps pulling through gear changes, where the engine's drive is cut.
  - **Regen:** braking harvests 60% of the braking power, up to the motor-generator's limit.
  - **Running flat:** no deploy, and the pack's weight stays in the car (`CarTuning.mass`). Richard's penalty is real:
    a flat hybrid is slower than its petrol twin.
- **The City hybrid 1.5** (`engine_city_hybrid`, class C, 1,100 credits): a 92 hp Atkinson-cycle 1.5 with an 18 kW
  motor, a 110 kJ pack and 15 kW of regen, at 6% more weight.
  - That's roughly six seconds of full deploy from full. A lap's braking gives back about a pack and a half.
  - Flat, it's a heavy Workshop-tune car. Charged, it beats the Club 1.4 out of slow corners.
  - It sits beside the Club 1.4 as a sidegrade, not an upgrade.
- **Where to get it:** Jed's crates are unpacked. With `--hybrid` he has "Buy parts", and the City hybrid is his
  shop's find (Bramblewick's speciality, by the dairy whose milk float is "on charge today").
  - It's kept out of every AI field (`FieldRecipe.Open`), so races are unchanged until the AI is tested with it.
- **On screen:** a blue BAT meter on the dash, beside the throttle, shown only when your car is a hybrid.
- **In the race report** (`race.json`, per car): `deploySeconds`, `emptySeconds`, `deployKj`, `regenKj` and
  `finalCharge`.
- **Without `--hybrid`** the catalogue leaves the part out entirely, so the game is exactly as before.
- **`--hybrid-everyone`** fits the same mild system to every car's own engine, so the same field can race flag off
  against flag on.
- **Checks** (`HybridChecks`, in the build's editor checks):
  - the part is in the data but not in the catalogue without the flag;
  - charged beats petrol off the line, and flat loses to it;
  - launching drains the battery, and braking refills it;
  - there's no deploy at part throttle or above 26 m/s;
  - the motor's push is capped at low speed.

## Design decisions (mine, under Richard's design freedom)
- **No Battery slot yet.** A new slot touches save validation, every seven-slot screen and the AI kit recipes, and doc
  83's slot decisions are still open. The prototype's battery comes with the engine. A separate Battery part, trading
  capacity for weight, follows once the slots are settled.
- **Automatic deploy only.** The pad is busy (L2 and R2 shift, and the right stick is throttle). The Save, Balanced and
  Attack modes come after the measurements show whether management needs them.
- **The AI uses the same rule**, but its speed planning doesn't yet know about the motor. It plans with the petrol
  envelope and simply gets the extra push. Measure first, then teach it.

## Next
1. Same-field races, `--hybrid-everyone` off against on (Cedar and Bayview, seeds 1709-1711), recorded below. Check how
   long cars run flat, and whether the AI copes.
2. Measure the City hybrid against its petrol twin on Bayview and Hillcrest.
3. Then decide on the modes, the Battery slot, and whether hybrids move up the classes as doc 83's table says.

## Results
`tools/hybrid-summary.py <study> [base arm]` reads the race reports: finishers, resets, contacts, total time, and the
battery's use.

**Study 243: the same field, `--hybrid-everyone` against flag off.** Cedar, Bayview, Hillcrest and Bramblewick Lanes,
seeds 1709-1711.
- **Round 1:** every race finished 24/24, with no resets and normal contacts. The AI copes with the extra push without
  being told about it. The field was 1.5-2.5% quicker in total.
  - But the energy loop didn't work. Regen gave back only 25-50 kJ a race against about 140 kJ deployed, so every
    battery was empty at the flag. Cars spent 13-36 s flat against 8-10 s deploying: a one-shot launch boost.
  - The cause: regen was scaled by pedal depth, and the AI mostly brakes lightly. A light brake at speed still sheds
    tens of kW.
- **Round 2 (`hybrid-regen`):** regen is now limited by the motor-generator (15 kW), not the pedal.
  - Harvest tripled: 75-167 kJ a race.
  - Deploy rose 35-70%, to 11-16 s. Flat time fell to 11-28 s.
  - All 24/24, no resets.
  - Total time against flag off: Cedar +52.5 s, Hillcrest +47.8 s, Bramblewick +45.1 s, Bayview +7.5 s
    (-5.5, -20.8, +48.8). Bayview is the short island lap, where the launch is most of the deploy.
  - With automatic "deploy whenever you can", the pack still ends near empty. That's expected: the energy gets used,
    and the weight penalty when flat is real. Managing the battery (the modes) is what would keep a reserve.

**Study 244: the City hybrid against its petrol twin, the Club 1.4.** `--field-engine <id>` (a test flag) fits one engine
to every car, so the same field races all Club 1.4 against all City hybrid. Four tracks, three seeds.
- All 24 races finished 24/24, with no resets.

| Track | City hybrid vs Club 1.4 (field total, per race) | Per car |
|---|---|---|
| Bayview (short, tight, stop-start) | 21.6 s quicker | 0.9 s quicker |
| Hillcrest | 30.0 s slower | 1.2 s slower |
| Cedar | 34.2 s slower | 1.4 s slower |
| Bramblewick Lanes (long; about 20 s flat a race) | 79.7 s slower | 3.3 s slower |

- **Verdict: kept, as a sidegrade with a character.** It wins where launches and slow corners dominate and loses where a
  long lap runs the battery flat. That's the "choice, not an upgrade" doc 83 asked for.
- **Watch:** contacts were higher with the hybrid in two Hillcrest seeds (38 and 42 against 20 and 14), but not in the
  third (12 against 24). The AI doesn't plan for the motor's push; check again before hybrids reach the AI fields.
- **Not yet seen:** the BAT meter. The HUD review's capture shows the camera only, not the overlay.
  To see it: `--hybrid`, buy the City hybrid at Jed's, and race.

## Where it stands (21 Oct)
The prototype is done and measured, behind `--hybrid`. Next, once Richard has played it:
- the modes (Save, Balanced, Attack), so the battery can be managed rather than spent whenever possible;
- the AI's speed planning reading the motor;
- the Battery slot, with doc 83's slot decisions;
- recharging at Pit Stops in free roam.

