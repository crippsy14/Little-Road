# Next steps (30 September 2026)

**Plan of record (29 September):** [doc 45](45-roadmap-and-feature-spec.md) sets the order of work, Phases 0–5; the parts ladder is in [doc 44](44-parts-tiers-and-progression.md). The campaign is at least 12 hours and open-ended. Next is **Phase 0**: world grid with per-tile map and fog, Racer records, radar chart, a traffic-aware launch, save and IDs, and track standards.

**Update, 30 September: Racing AI v2, clean-up, Bayview and Silver Falls.** All on branch `racing-ai-v2` (not merged).

1. **Racing AI v2** (`RacerDriverPolicy`; research, design and every study in [doc 46](46-racing-ai-v2-design.md)) is the default driver. `--first-ai` races the first AI.
   - Beat the first AI in the keep/reject study (205): all 24 finish, no resets, about 20 s better per race.
   - After the ability spread was widened it's about level (207), because the roster's weaker drivers are now genuinely slower.
   - Abilities: all-worst is 49% slower than all-best on a solo lap; each ability counts.
2. **Race preview:** DR (driver rating 1–20) beside PP on every grid tile and a DR rosette. Hover it, or hold Triangle / R, for the radar.
3. **Clean-up:** the Godot prototype, the Cowork runner, 19 obsolete tools, 17 superseded docs and old evidence are gone (all in git history). `review/` went from 7.8 GB to about 0.3 GB. `cleanup.ps1` keeps it that way.
4. **Bayview:**
   - a seaside high street of terraces with bunting and café parasols;
   - meadow grass on the verges and green;
   - olives, blossom and poplars on the hills;
   - lavender and vine plots.
5. **Water:** sea contact foam at walls, piers, hulls and rocks. **Silver Falls** rebuilt as a real cascade (`LittleRoad/Waterfall`); the river no longer overhangs the gorge.
6. **Parallel races:** `tools/run-pace-parallel.ps1` runs six at once. First-AI races run alone because its worker threads aren't repeatable under load.

**Next** (Richard's order):
- AI polish: club cars in Cedar's crowded back half; fewer light side-by-side touches; a calm two-car defend test to watch.
- Then the world expansion and the parts expansion (doc 45 Phase 0 onwards, doc 44).

**Update, 29 September, night: seven fixes before the parts doc.**

1. **Back from a race preview** now returns to whoever offered the race: Mara and Gio keep "Fit parts".
2. **Look behind:** both views are fixed, and flipping back restores the forward camera exactly where it sat.
   - **Why it zoomed:** the forward camera trails the car. On flipping back it snapped to the untrailed spot, then drifted back out, which read as a zoom.
   - **Also fixed:** the cut now skips race motion blur and the anti-aliasing history.
3. **Starting grid:** the pole car's nose is on the start line. Rows of three are 11 m apart (about 7 m clear) with a 1.8 m stagger; before, the front row was 8 m back and rows were 5.5 m apart.
   - **Baselines:** new ones are in `review/unity-35/frame-cost-128` (Cedar and Bayview, seeds 1709–1711) and `review/class-sort/grid29-*`. Earlier baselines are superseded and have been kept, not deleted.
4. **Map sea:** coloured by distance from the coast (surf, reef turquoise with patchy reef, then deep blue), from a land mask of 512² downward rays. `WorldMap.Reef`.
5. **Fog:** opaque, and only where you've been. No free Bayview or whole-town reveal; viewpoints still reveal what you can see from them. Masks saved before this (v1) are discarded.
6. **Blur:** the race motion blur is now camera-and-objects, so your own car stays sharp. At night, lamps leave short light trails above about 22 mph (`LightTrails`, `LittleRoad/LightTrail`).
7. **Collision boxes** are fitted to each body: the model's length, and its shoulder-and-tyre width without mirrors (`CarBodies.ShoulderWidth` / `Length`, measured in Blender).
   - **Before:** one shared box forced to 2.32 m wide left 27 cm of air each side of the Puddle, while the longer field cars sank up to 54 cm into each other.
   - **Now:** 1 cm nose-to-tail and 2 cm at the shoulders at contact.
   - **Check it:** `--contact-review` (Richard's test) shoots pairs of cars from above. See `review/map-sweep/contact-old` and `contact-perbody-2`.

Smoke21 passes.

**Update, 29 September, evening: Richard's eight general improvements.**

1. **Fog of war** on the minimap, full map and pause map. It reveals 175 m around the car, 520–620 m at viewpoints and the castle, and whole towns on arrival. It's saved with the adventure. Details are in `docs/ui-design.md`.
2. **Parts tiers plan:** `docs/44-parts-tiers-and-progression.md`.
   - **Classes:** C Street, B Tuner, A Touring, S Racing and E Endurance.
   - **Ladder:** three steps per slot per class plus sidegrades, about 125 parts, with names and a look for each class.
   - **Economy:** about 7.8 h of racing to reach podium builds, and 20 events in 6 regions.
   - **Waiting on:** Richard's four decisions.
3. **Places:** a one-time DISCOVERED card, then the name under the minimap. Compact toasts replace the stamped ones.
4. **Race stutter:** races now pace to the display (72 fps on 144 Hz). Only 1% of frames miss the refresh, down from 22%. See `review/unity-35/frame-cost-127-review.md`.
5. **Parts screen overlaps:** fixed.
6. **Talk prompt:** half the size.
7. **Controls:** no button hints while driving. There's a CONTROLS tab in the pause menu instead.
8. **Look behind:** the camera is locked ahead of the car.

The parts review (34 checks) and smoke20 pass.

**Update, 29 September, afternoon: Richard's six feedback items are done.** Details for the UI are in `docs/ui-design.md` ("As built").

1. **Unreadable loading text.** The loading-screen heading was navy text on a navy outline; it's now brand orange. The result-screen coin mark is a plain label.
2. **Cars clipping into the world.** `StationarySpot` slides each standing car to the nearest clear spot, and townsfolk with no clear spot are dropped. `--car-audit` now finds 1 of 108 (Orla at (275, −927) against a kit `Collision_Body`; the audit now names the parent object, so the next run will say which).
3. **Staring cars.** Parked cars no longer turn to face you.
4. **UI overhaul:**
   - a new HUD: speed dash, a timed objective and a contextual controls hint;
   - the pause hub: MAP (regions, hover counts, full map), COLLECTION, CAR and SETTINGS tabs;
   - back goes one level;
   - glyph-aware prompts in the shops.

   Captures are in `review/map-sweep/pause-2`.
5. **Road-end lips.** Gravel end ramps are added, but skipped where a road runs straight onto a through road: the one at the gate junction stood in the junction.
6. **Castle Climb event** (`CastleClimb.cs`):
   - **Route:** bollards line the route, and every side road gets a close-set line 7 m beyond it. Sharp junction corners are left open, because an edge line there trapped cars that ran wide.
   - **Crowd:** parked cars, townsfolk and valley traffic move behind the bollards. Everyone within 40 m bounces and cheers ("Go go go!").
   - **Zipline:** closed during the climb.
   - **Hard mode:** par 80 s instead of 85, 750 credits instead of 300, and 20 obstacles, each a hay bale across one lane with cones leading into it. It has its own badge.
   - **Evidence:** the autopilot on Via Crest takes 49.1 s with the closure, identical to the run without it and with zero contacts (`review/map-sweep/climb-crest-*-11`). Hard mode arrives in 81.8 s even with the autopilot's clumsy dodging.
   - **Known limit:** this autopilot overshoots the quay-to-Via Crest hairpin from a standing start, with or without the closure. `--climb-from 112` tests past it.
   - **Captures:** `review/map-sweep/climb-3`.

**Parts browser, all categories (later on 29 September).**
- **Browser:** the 3D browser now handles all seven categories. The category stickers, LB/RB or [ / ] switch in place.
- **Models:** every part has a Blender model (`source-art/build_part_previews.py`), with shape differences between tiers. That includes the Valley V6, which no longer borrows the Club motor's model.
- **Stats:** each category compares what it actually changes, such as stopping distances for brakes, road and gravel grip for tyres, and weight for chassis.
- **Locked parts:** they now show in the carousel as "CLASS X / LOCKED".
- **Checks:** the parts review (`review/ui-screens/all-parts-1`, 29 checks) and smoke18 pass.

Smoke17 passes. The build runs 1,060 editor checks, including the new `InterfaceChecks`.

**Still to check by hand:**
- the pause hub with a real pad;
- the Hard par, which is set from autopilot times rather than a human run;
- whether 4.5 m bollard spacing reads as "closed" enough on the open valley stretches.


**Update, 29 September.** Items 1–6 are done:
- race warm-up;
- benchmark ignores its screenshots;
- raceway rubber;
- sea rocks seated;
- causeway end already framed by the Headland Gate, so nothing added there;
- scorecard third pass.

Benchmark db11: worst frame 19 ms, none over 33 ms.

**Item 8, the minimap, is done** (`Minimap.cs`, `LittleRoad/Minimap`, `WorldMap.Bake`):
- **Map image:** the world is baked from above at a fixed afternoon behind the loading card.
- **Corner map:** bottom-right, heading-up, and zooms out with speed. Marks:
  - residents in their colours;
  - Pit Stops (orange squares);
  - the zipline (yellow diamonds);
  - the objective (a pulsing star that waits on the rim when off the map);
  - a north badge on the rim.
- **Full-screen map:** M or the View/Select button opens it north-up with the atlas names. Names are placed by priority with overlaps dropped, and the car waits while it's open.
- **Checking it:** `--minimap-review` captures the corner map in three places plus the full map (see `review/map-sweep/minimap-2`). `--no-minimap` turns it off.

Item 7 (shadow budget) is left for the 10× world: frames are at 5.6 ms today.

**Item 9 is done.**
- **Study 126** (`--rejoin-at-speed`) is **rejected** at the screen: on Bayview, 22/24 finished, with 2 assisted resets and 10× the contacts. See `review/unity-35/frame-cost-126-review.md`.
- **Class-sort baseline:** Cedar sorts cleanly, but Hillcrest leaves 2 street cars stuck behind stock cars. See `review/class-sort/base-review.md`.

The next AI step is the Hillcrest passing investigation. Windowed tests launch on monitor 1 (`-monitor 1`). The remaining Bayview bush at (−93, −1) is cosmetic.

Where the graphics and world work stopped, and what I intended to do next. Nothing is committed yet. All of today's work is in the working tree, and the dev build in `../little-road-dev-build/` is current.

## State at the pause

- **Last benchmark (db10).**
  - Uncapped, the drive averages 5.2 ms, with p99 at 9.4 ms.
  - The five frames over 33 ms are all the benchmark's own 20-second screenshots.
  - The 400–700 ms causeway stalls from db9 are gone. They were shader "fade" variants compiling on the first LOD cross-fade; the loading warm-up now forces cross-fades in every view to compile them.
- **Smoke test (smoke13).** Passes with every change so far: quest, 24-car race and payout, reload, retry and podium renders.
- **Research plan.** All seven items in `docs/visual-quality-audit.md` are in. Since then I've added:
  - the framed tunnel exit;
  - camera shake on impacts;
  - flags and banners in the wind;
  - warmer, darker hill rock;
  - the kit asphalt on the Bayview circuit;
  - a rubbered-in racing line.

## Next, in the order I intended

1. **Warm up fade variants for races too.** `RaceTrial` has no warm-up, so the first cross-fades in a race (props, trees, cars) can hitch the same way the causeway did. Run the same forced-fade pass behind the race loading card, then check with a race frame probe.
2. **Stop the benchmark stills from counting as hitches.** Skip the frame after each capture in `FrameProbe`, so reports show only real stalls.
3. **Racing-line rubber on the Hillcrest Raceway.** Only the Bayview circuit has it. The raceway is built from `HillcrestRoute.Circuit`, a different road builder, so it needs its own strip builder.
4. **Grounding.** Fix the few real placement faults from `PlacementAudit`:
   - the Bayview bush at x −93, z −1;
   - the surf rocks below the headland cliffs.

   Then add a soft dark footing where large kit pieces meet terrain; SSAO alone is thin at a distance.
5. **Framed arrival at the causeway's mainland end.** This is the other arrival moment the audit named. The Headland Gate already frames it from the sea side; from the valley side it needs trees or a landform.
6. **Scorecard update.** Add a third-pass section to `docs/graphics-scorecard.md` covering today's work (body motion, rim and bounce light, fades, terraced hill, flags, road surface, performance), with before/after captures.
7. **Rendering scale-up.** Next in `docs/rendering-and-streaming.md`:
   - a shadow-caster budget (small casters stop casting shadows beyond about 60 m);
   - then GPU occlusion culling (DOTS instancing in the custom shaders);
   - then sector streaming for the 10× world.
8. **Free-roam minimap (Richard's request).** A detailed minimap in adventure mode while free roaming.
   - **Map image.** Build it from the same top-down world render as the atlas (`WorldMap`, `--world-map`), baked at load or shipped as a texture. It will need re-baking when the world changes.
   - **Layout.** A round map in a corner that rotates with the car (or north-up, as an option), with a player arrow.
   - **What it shows.**
     - roads and place names;
     - Pit Stops, residents and the quest objective, each with its own icon;
     - the zipline stations;
     - collectibles only once found (or never, so they stay a discovery).
   - **Behaviour.** Zoom widens with speed. A button opens a full-screen map that uses the atlas labels.
   - **Scaling.** Must hold up in the 10× world: the map texture streams by sector along with the world (see step 7).
9. **Still open from earlier.** AI racing study 126 (`--rejoin-at-speed`) and the class-sort run. Both should start with a fresh baseline, because the trial field changed when the placeholder bodies were removed.

## How to pick up

- Build: `.\tools\build-dev.ps1`
- Check views: `.\tools\capture-map.ps1 -Name <run> -Rendered -Flags '--valley-views'`
- Measure: `.\tools\drive-benchmark.ps1 -Name <run>` (uncapped, no traffic)
- Regression check: `.\tools\capture-adventure.ps1 -Name <run>`
- Place names: the pinned Little Road Atlas (`--world-map` capture, published at https://claude.ai/artifact/KqqihWNLFv8AUXV1Rg4UWp)
