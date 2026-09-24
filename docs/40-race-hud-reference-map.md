# Race HUD reference and information plan

**Reviewed direction:** the user approved the transparent build 94 gameplay layout on 23 September: continue refining this direction. Current speed/drive/pedals, relative order and lap clocks are implemented; this is visual-direction approval rather than completion of all numbered modules. Build 95 adds the course map, recent laps and final-lap notice; see [measured review](../review/unity-35/race-hud-95-review.md). Next carry the separate playful identity into preview/results menus. Keep the centre of the driving view clear.

23 September. User-provided [numbered GT7 illustration](https://www.gran-turismo.com/images/c/i16HBxihYqSTHO.jpeg), checked alongside the [official race-screen manual](https://eu.gran-turismo.com/es/gt7/manual/race/02). The reference combines more information than a normal single view needs. Use its restrained hierarchy and transparency as direction; implement Little Road's own layout/assets. The downloaded reference is outside Unity under references/ui, for analysis only.

The first cream-card HUD was too opaque for the user's intended driving experience. Replace it with small white readouts, shadows for contrast, thin controls and limited translucent backing. Leave the centre/apexes/opponents visible. The player's required ±5 field remains eleven rows by default, compactable to five through H/L3; do not silently reduce the requested field to the reference's smaller relative list.

## Numbered coverage

The left columns identify the reference. The right column records our implementation direction, not a claim that deferred systems exist.

| Ref | Information | Little Road decision/status |
|---|---|---|
|1|Position|Live authoritative race order; finished ties use the actual result order.|
|2|Lap count|Current/required laps. Preserve rear-grid standing starts.|
|3|Rear mirror|Existing Triangle/Q rear view stays. Consider an optional mirror later, measuring the extra rendering cost.|
|4|Track map|Build 95: north-up outline from the actual course, physical car markers, start/finish tick and larger directional player triangle.|
|5|Lap history|Build 95: newest three completed legal laps; grid launch is excluded.|
|6|Final lap|Build 95: three-second notice on starting the last lap of a multi-lap race, subordinate to recovery/finish messages.|
|7|Lap/sector timing|Current lap and personal best now; sector comparisons need recorded legal passages.|
|8|Relative order|Eleven actual entrants, signed time gaps, explicit lap deficits and unavailable history.|
|9|Fastest-lap notice|Later transient race event; distinguish field record from personal best.|
|10|Total time|Small race clock; freeze the player's completed time at finish.|
|11|Braking suggestion|Optional driving assistance later, off in normal racing unless selected.|
|12|Best-lap delta|Later comparison against the player's recorded best lap, not a predicted AI target.|
|13|Steering/shift indicator|Applied steering input now; shift lights require a real RPM/gearchange model.|
|14|Speed/gear/nearby cars|Speed and automatic D/R now. Plan discrete gears and side-awareness warnings separately.|
|15|Fuel/odometer|Endurance module: actual tank level, capacity and distance, with actionable low-fuel warning. Avoid a duplicate speed dial.|
|16|Brake input|Applied brake/reverse pedal meter now; no fictional ABS state.|
|17|Multifunction display|Future compact contextual pages for strategy, tyres, fuel and pits. Resolve controller navigation alongside recovery bindings.|
|18|RPM/boost|Later genuine drivetrain telemetry; do not invent RPM/boost pressure from a cosmetic speed animation.|
|19|Throttle input|Applied signed-pedal throttle now; coasting remains visibly distinct.|
|20|Tyres/damage|Future four-wheel condition display tied to compound, wear and temperature. Damage indicators only for implemented effects.|
|21|Surface water|Show when wet-surface simulation exists; keep absent during today's dry-only races.|
|22–23|Assistance states|Show only installed/enabled systems with actual activity; avoid unused icon clutter.|
|24|Special power reserve|Conditional future equipment display, such as boost or the planned exploration abilities.|
|25|Passed-car information|Optional brief rival identification later; avoid constant popups over overtaking space.|

## Presentation and data requirements

Use a compact bottom-centre speed/drive/pedal group, narrow left relative order, small upper position/lap counters and lap time. Reserve upper-right space for the map and lower-corner space for endurance status, without drawing inactive placeholders. Readability should come primarily from typography, contrast and small shadows; apply translucent backing only where needed. Keep clear player highlighting and sign/label cues rather than relying solely on colour.

Race gaps are sampled observations, never distance divided by current speed. The final timing helper retains a bounded history of actual positions/times and separate stop arrival/departure points. It invalidates recovery/reverse discontinuities, uses legal checkpoint-limited progress, labels lap differences, and falls back to unavailable when history expires. Validate prolonged stops, close passes, crossing the start/finish, same-tick finishes and history wrap.

Fuel consumption, tank upgrades, fuel mass, tyre degradation, endurance race/pit strategy and AI pit decisions remain mandatory campaign work. Integrate their displays when their authoritative state exists. This styling decision does not defer or remove those features from the full goal.

Build review needs actual bright/dark gameplay captures, different window sizes and a matched normal-speed HUD on/off cost comparison. Synthetic gamepad checks are separate from physical DualSense and user visual acceptance. Menus, garage/parts, results and exploration remain a separate but coordinated artistic redesign under document39.
