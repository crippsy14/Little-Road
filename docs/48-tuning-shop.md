# 48: The tuning shop

1 October 2026. Richard: "add a tuning shop to the garage / parts / model shops, with opportunities to test the car on the track you are tuning it at". Reference: GT7's transmission screen, which shows:
- gear ratios with each gear's top speed;
- the final drive;
- a chart of revs against speed per gear, with the rule "gears must not start below 85% torque";
- the power and torque curve with the shift point;
- which gears suit which parts of the track.

This rests on gearing v2 (doc 47): fixed ratios, with power deciding whether a car reaches its top-gear limiter.

## Where

A **Tuning** counter in every town's Pit Stop, beside the garage, parts and body shop (Bayview's tutorial Pit Stop gains it once the first race is won). It tunes the car you're driving.

## What you can tune

A tune is saved per car and gearbox. Changing the gearbox starts from its default tune.
- **Final drive** (every gearbox): 0.80–1.25 × the box's own.
  - It's shown as a *top speed at the redline* slider, as GT does.
  - Shorter gives more pull and less top speed; longer gives the reverse.
- **Individual gears** (sport, short-shift, six-speed and long boxes, and every B–E box): each gear's top speed, within the box's spread.
  - Gears must stay in order, and no gap may drop the engine below 85% of peak torque (the chart shows why).
- **Auto-tune:** sets the final drive so top gear reaches the redline just after the car's own top speed (a hair short of the limiter), keeping the box's spacing. Three presets: *Acceleration*, *Balanced* and *Top speed*.
- **Later (not in the first pass):** limited-slip differential settings on LSD boxes, and a power slider (GT's output adjustment) for balancing into a class.

## The screen

- **Left panel:** the car and its measured numbers: PP, power, torque, 0–60 mph, 0–100 mph and top speed. It updates after every test drive.
- **Middle:**
  - the gear table (each gear's top speed, and its ratio for those who want it);
  - the final drive;
  - the **gear chart**: revs against road speed for every gear, with the redline, the peak-power shift point and the 85%-torque band shaded. It turns amber where a shift would drop below that band.
- **Right:** the power and torque curve, with the peak and the shift point marked.
- **Bottom:** the local track's map, with each braking zone marked by the gear the tune would use there. Richard's example: 2nd and 3rd for the tight sections, 4th for medium corners, 5th and 6th flat out on the straight.
- **Test drive:** the town's track as a solo time trial with the new tune. You come back to the screen with the lap time, sector times, top speed and 0–60, compared with the last run (a measurement history, as GT keeps).
  - The 2.5-mile test loop (`test-loop` track) is always available as a second choice, for top-speed and gearing runs.

## Rules

- Tuning is free: the parts you own set what can be tuned.
- AI rivals race their box's default tune; the tuning shop is the player's edge.
- A tune can't give a gearbox more gears or a wider spread than it has; a better box is still an upgrade.

## Build plan

1. **Data:** a `tunes` map in the save, `{transmissionId: {final, gears[]}}`. Old saves get none (the defaults).
2. **Physics:** `EquipmentCatalog.Stats(fit, tune)` passes the final drive and gear overrides to `Drivetrain`. Checks: tune limits, auto-tune lands 1–3% under the limiter, gears stay ordered.
3. **Screen:** the gear chart and the power curve, drawn with the UI toolkit's panels (as the rev strip is), in the Pit Stop menu style.
4. **Test drive:** a solo time-trial session on the town's track (or the test loop), which returns to the screen with its measurements. It builds on `RaceTrial`'s solo mode.
5. **Keep/reject:** a short study, confirming the AI's default tunes race exactly as before.
