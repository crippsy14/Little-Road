# 56 · Wider game round (2 October, evening; Richard away)

Richard: "look at the wider / full game and iteratively improve on any aspects continually until I get back". This doc records each change with its evidence, including what was tried and not kept. Branch `claude-additions`, not committed.

## 1. Swept contacts for every car (kept)

**Background.** On 25 September, the turn "jerk" was traced to speculative contacts snagging the internal edges of Bayview's road mesh while a car yaws. The player's car was switched to swept contacts (`ContinuousDynamic`). The AI cars stayed speculative until a keep/reject study could run with `--sweep-contacts`.

**Study 215** (same build; Cedar and Bayview; seeds 1709, 1710 and 1711; flag off vs on):

| | finished | assisted resets | contacts | hard contacts | total finish time, all 24 cars |
|---|---|---|---|---|---|
| speculative (off) | 144/144 | 0 | 115 | 1 | 9617.0 s |
| swept (`--sweep-contacts`) | 144/144 | 0 | 54 | 0 | 9616.6 s |

- **Per track:** Bayview was 7.8 s slower in total across three races; Cedar was 8.2 s faster. Cedar contacts fell from 88 to 35.
- **Cost (study 217, Cedar 1709, races run one at a time with `--frame-probe --time-physics`):** physics took 0.122 ms per step with speculative contacts and 0.120 ms with swept. Car logic took 1.050 and 1.047 ms. There is no measurable cost.
- **Verdict: keep.** It meets every rule: all cars finished, there were no new resets, and the total time improved, though only just. Half the contacts is the real gain. Swept contacts are now the default for every car (`RoadCar`). `--speculative-contacts` restores the old mode, and the editor check is `ContactChecks`.
- (Study 216 was started without the frame probe and stopped. Its partial folder is left as it is.)

## 2. Hillcrest houses: three new terraces and roof colours (kept)

**Before.** 62% of Hillcrest's street houses were terraces a–d. All four are the same 9.6 m, three storeys and flat-topped, so every street had one unbroken eaves line.

**New kit pieces** (`source-art/build_bayview_kit.py`, same 5.4 × 8 m plot, so the street layout is unchanged):

- **`terrace_e`, palazzetto.** Four storeys (12.6 m) with a rusticated ground floor, a balustraded balcony, a cornice on modillions, and an *altana* on the roof (a Ligurian rooftop loggia with its own small hipped roof and potted shrubs).
- **`terrace_f`, cottage.** Two storeys (6.6 m) under a tiled pitched roof with its eaves to the street, a dormer, and a bench by the door.
- **`terrace_g`, painted house.** A street-facing gable with an oculus, painted pediments over the windows, string courses and a fanlit door.

Render: `review/kit-renders/terraces-1/terraces.png` (a, e, f, g, c).

**Roof colours.** `BayviewKit.Tint.roof` recolours the tile roofs per house. Hillcrest draws 3/8 fresh terracotta, 1/8 each of four weathered or darker tile shades, and 1/8 grey Ligurian slate. Roof colours use their own random stream, so the town layout (houses, gaps, parked cars) is drawn exactly as before.

**Placement.** The terrace pick list is now ten entries (a, a, b, b, c, d, d, e, f, g). It uses the same number of random draws, so only which terrace goes where changes. Chimney smoke positions were added for the new houses.

**Evidence.**
- [Before](../review/map-sweep/db-bayview-5/drive-090.png) and [after](../review/map-sweep/db-houses-1/drive-090.png), the same frame of the benchmark drive. A cottage now breaks the row on the left.
- [The market square](../review/map-sweep/db-houses-1/drive-110.png): two palazzetti with altanas.
- Performance (`drive-benchmark`, 1920×1080):

  | | mean frame | p99 |
  |---|---|---|
  | before | 4.99 ms | 9.25 ms |
  | after | 5.00 ms | 9.30 ms |

- Checks: `PresentationChecks` now checks that each new terrace fits its plot, that the skyline steps (cottage < plain < painted < palazzetto) and that a roof tint recolours the tiles. The build passed with 2810 checks.

## 3. Stock 1.2: two more recordings tried (not kept)

The Stock 1.2 scores 7.7 in game, held back because its Citroën C2 recording only reaches 3,500 rpm. It scores 4.4 on garble. Offline scores on the Stock 1.2's own in-game drive (`SOUND_OVERRIDE`):

| recording | top rpm | in game | pitch | tone | chipmunk | garble | lift-off |
|---|---|---|---|---|---|---|---|
| Citroën C2 (current) | 3,500 | **7.7** | 8.0 | 7.4 | 8.8 | 4.4 | 8.3 |
| Renault R4 GTL, engine-bay mic (Sonniss 2024, Justsoundeffects) | 4,691 | 6.9 | 9.2 | 6.1 | 1.1 | 9.1 | 6.0 |
| R4 with `raw` / `loopabove=1.2` / `formantcap=1` | | 7.1 / 7.1 / 5.9 | | | 2.4 / 2.4 / 6.6 | | |
| Morris Oxford, onboard (already downloaded) | 1,703 | 5.4 | | | 0.0 | | 3.7 |

- **What it showed.** The R4 fixes the garble but sounds chipmunked when pitched above its own top revs, and that costs more than it gains.
- **Clean-up.** Both banks were removed from `Resources` and from `engine-banks.json`, so nothing ships. The R4 recording stays in `../little-road-audio-sources/Sonniss-GDC2024/`.
- **Fiat 500 interior drive** (Sonic Bat, Sonniss 2023; 13 min, 4 channels, 192 kHz): downloaded and converted to mono 48 kHz (`Fiat500-interior-ch1-48k.wav`, outside the repo). It's a cabin mic array, and every channel is dominated by road rumble below 80 Hz. The tracker locked onto the rumble (560–951 rpm, 41 grains), and the engine is buried, so it isn't usable. The bank was removed.
- **What would actually help.** A clean small four-cylinder recorded through to about 6,000 rpm (exhaust or engine bay, on load). The free 2023 and 2024 bundles have no others. The remaining options are the 2021 and 2022 bundles, or a paid library (Richard's call).

## 4. Hillcrest Castle gate (fixed)

**Found in the benchmark drive** ([frame 130](../review/map-sweep/db-bayview-5/drive-130.png)): the car entered the castle half on and half off the drawbridge, with the camera among floating grey blocks.

**Causes.**
- **Misaligned gatehouse.** Via Crest swings in from its spiral and crosses the wall ring 14–26° off the radial, while the gatehouse faced straight out along the radial. The drawbridge lay 1.3–3.7 m off the road's centre, across its edge.
- **Floating deck.** The deck was flat and 0.24 m proud while the road falls 0.3 m along it, so its far end stood 0.6 m above the road. It has no collider, so cars sank into it.
- **Loose chains.** Each chain was 8 cubes spread over 12 m.

**Fixes.**
- `HillcrestTown.CastleGate()` stands the gatehouse on the road as built, facing out along it (yaw from the road's heading 5 m out). The flanking walls still meet the drums.
- In the kit, the drawbridge pivots at the gate and rests on the bank: it falls 0.29 m over 7 m, and its top rides about 8 cm above the road.
- The chains are now 38 close-set links each, alternate ones turned a quarter.
- New check in `PresentationChecks`: the drawbridge lies on Via Crest. It is now 0.19 m off the road's centre with 3° of skew; before, it was about 2.5 m and 20°.

**Captures** (new review flag, `--view-shots "name:x,y,z>lx,ly,lz;..."`, via `capture-map.ps1 -Rendered`, which now also passes `--start-windowed`):
- [Aerial, aligned](../review/map-sweep/gate-1/gate-aerial.png)
- [Front, deck resting on the road](../review/map-sweep/gate-2/gate-front.png)
- [Low, with chains](../review/map-sweep/gate-3/gate-low.png)

**Road paint on bends (fixed everywhere).**
- **Problem.** The inner edge line saw-toothed on Via Crest's last bend into the gate.
- **Cause.** The cause wasn't the bend. Every valley road stripe was built as separate quads, each offset along its own segment's sideways direction, so 3.4 m out they overlapped and stepped on any bend.
- **Fix.** `CoastalLandscape.StripeQuads` now uses each point's mean direction, so neighbouring pieces share their ends.
- **Check.** An 8 m-radius test bend must have zero seam: it has 0.0 mm.
- [After](../review/map-sweep/gate-4/gate-low.png), against [before](../review/map-sweep/gate-3/gate-low.png).

## 5. Placement audit: Silver Falls rocks (fixed)

`--placement-audit` flagged 27 floating pieces. Each was looked at with `--view-shots` ([before](../review/map-sweep/audit-views-1/falls-close.png), [after](../review/map-sweep/audit-views-2/falls-close.png)).
- **Six falls edge rocks: fixed.** They followed the water sheet's arc, which curves out from the cliff, so they hung 1.6–3.3 m in the air beside it. They now sit on the gorge side below the sheet (`Cascade`). The audit is down to 21.
- **Left as they are:**
  - the three lip rocks, which sit in the river (the gap is to the riverbed, under water);
  - 17 rock clusters on the sea floor (y −15 to −20, out of sight);
  - one Bayview bush at (78, 84), which looks seated on screen ([view](../review/map-sweep/audit-views-1/bush.png)).

## 6. Bayview Halt rejoin, re-run (no change needed)

`--rejoin-test` on the current build (`review/map-sweep/rejoin-swept-1`) gives 43 of 48, the same as on 30 September.
- **Four of the five failures** start at z −150 at 20° and run south into the "Hillcrest Valley" gate pillars and their planters before reaching the road ([top view](../review/map-sweep/halt-views-1/top.png)). That's the test's start geometry, not a missing ramp.
- **The fifth** is the known full-throttle start from rest at 90°, which stalls against the road edge; at part throttle the same spot passes.

## 7. Engine voices: High-comp to the DS3 WRC (kept)

- **Offline re-render** of each engine's own in-game drive with other banks (`SOUND_OVERRIDE`).
- **High-comp 2.0:** Xsara 8.0 → **DS3 WRC 8.9**. It plays only 5% above the recording and scores lift-off 8.9, where it was 5.8. Class B keeps three different voices (Piazza, DS3, McLaren).
- **Confirmed in game.** A fresh capture on the new build scores 8.9. 15 of 18 engines are now at 8 or above in game.
- **Works 2.0** (Xsara 7.9): S2000 6.7, S2000 external 6.9, DS3 7.8, Fabia 6.2, Piazza 7.8 (chipmunk 0.0), Escort 7.9 (already class A's Touring) and Puma 6.5. It stays on the Xsara.
- **Hybrid V8** (McLaren 7.9): Viper 8.3 but chipmunk 2.3, so not taken. RS4 5.8, Carrera GT 7.6, Testarossa 7.5.

## 8. Bayview roofs (kept)

Every building on the Bayview high street had the same orange roof. Each now has a fixed roof colour, west to east:
- fresh terracotta on most;
- weathered and darker tiles on four;
- grey Cornish slate on two terraces.

Views: [from the harbour](../review/map-sweep/bayview-roofs-1/harbour.png) and [along the street](../review/map-sweep/bayview-roofs-1/street.png). After the art grade the terracotta shades are subtle and the slate reads clearly.

## 9. Painted side windows (kept)

Side walls were blank on every terrace and detached house. They show at every row end, every gap and every corner.
- **What changed.** Following Ligurian painted facades, `side_paint` in the kit adds flat trompe-l'oeil windows (frame, closed shutters in the house's shutter colour, sill) to both sides of terraces a–g and houses a–d.
- **Fit.** They stand under 0.12 m proud, inside the 0.35 m gap to a neighbour, and are unbevelled at about 480 triangles a house.
- **Frame time:** 5.01 ms mean, unchanged.
- [Before](../review/map-sweep/db-houses-1/drive-090.png) and [after](../review/map-sweep/db-paint-2/drive-090.png), the same frame. The corner terrace at the junction is in [drive-070](../review/map-sweep/db-paint-1/drive-070.png).

## 10. Junction shoulders (fixed)

**Found by the edge audit** (`--edge-audit`): lips (road-edge steps a car's body can't climb back over) went from 22 on 30 September to 60 now. They sat within about 15 m of every T-junction: the quay at the mainline, Via Crest, and the station road.

**Cause.** The 1 October junction rework (doc 52) skips road shoulders within each junction's radius, so they don't poke into its surface, but nothing replaced them round the junction itself. Grass met a road 16 cm higher.

**Fix.**
- `KerbShoulders` runs a gravel ramp round every junction kerb: the fillets and the far side of the T.
- Road shoulders now stop only where they would actually run onto a junction's surface (`RoadNetwork.OnJunction`, a polygon test), not across its whole radius.

**Result:** lips 60 → 4. The four left are 0.4–1.3 m drops at bridge ends, as before. Banks are unchanged at 204: sea walls and rail cuttings behind railings. [Before](../review/map-sweep/junction-views-1/top.png) and [after](../review/map-sweep/junction-views-2/top.png).

## 11. Hillcrest bunting hung from lamps (fixed; Richard's report)

**Problem.** The bunting across the Quay (every 64 m) and Via Crest (every 90 m) hung in mid-air, 0.7 m beyond each kerb, with nothing at either end. The street lamps alternate sides, so they never lined up with it.

**Fix.**
- `StreetBunting` hangs each run from a lamp post on both pavements, reusing a street lamp within 2.5 m and otherwise adding one.
- A run whose end would stand on another road (by a junction) is left out.
- The market square and zipline lane bunting already hung from lamps and flagpoles.

**Check.** The placement audit now tests every bunting end: each must touch a placed piece within 0.8 m. The result is `buntingEnds=66 looseBunting=0`. [Close-up on the Quay](../review/map-sweep/bunting-views-1/quay-196.png).

## 12. Silent test runs (Richard's request)

**Problem.** Windowed test runs (smoke tests, map captures, benchmark drives, visible trials) played through the speakers.

**Fix.**
- `--silent` sets the listener volume to 0 from the first frame, and the volume settings keep it there.
- Audio still runs, so frame timings stay comparable, and `--audio-capture` still records: it taps the synth before the listener.
- It is passed by `capture-adventure`, `capture-map`, `drive-benchmark`, `review-body-shop` and visible `run-technical-trial` runs. `--audio-capture-silent` implies it.
- `test-loop` and `valley-tour` stay audible, because they're for listening and watching.

**Check.** `SoundChecks` confirms the game is audible unless the flag is passed.

## 13. Barriers where the ground falls away (kept)

**The area audit (doc 57) counted every drop beside a road that a car couldn't climb back from.** Several fixes and audit refinements followed.

**Via Crest.**
- A low stone parapet (with a white cap) runs along the downhill side where the ground falls away and nothing else stands (`HillcrestTown.Parapets`), with a gate-wide gap at each front yard. It is 67 pieces.
- Garden gaps between houses on the downhill side get the same wall.
- [Before](../review/map-sweep/crest-bank-1/crest-b.png) and [after](../review/map-sweep/crest-bank-2/crest-b.png).

**Valley roads and the raceway.**
- Guardrails now go wherever the ground falls steeply within 8 m (over 1.1 m in any 2 m), measured from the road or run-off surface. The old rule needed a 1.8 m drop 3 m out.
- The raceway gets armco along the run-off's outer edge on its climb to the hairpins.
- A guardrail no longer stops where its own road continues under another name (the valley road is a chain of three).

**The audit's barrier test** (`PlacementAudit.Edges`). A drop isn't a trap when either of these stands between the kerb and the top of the drop:
- something solid standing 35 cm or more over the ground (a guardrail, parapet, railing, house or plinth);
- a gap narrower than a car between two such things.

The probe steps every 4 cm, so thin railings count.

## 14. In-race pause menu (Richard's request)

**Problem.** Esc or Options used to quit the race.

**What it does now.**
- It pauses the race (the time scale and all audio) and shows:
  - Back to the race;
  - Restart race (the same event from the grid);
  - View the field (the pre-race grid screen, renamed THE FIELD, with no start button);
  - Options;
  - Retire to town (no payout).
- The card shows your place, lap and race time, plus the leader and the cars ahead of and behind you. Esc, B or Options resumes.
- Once you've finished, Esc goes back to town as before.

**Check.** The adventure smoke now pauses mid-race and exercises every page:
- it checks the race clock holds while paused;
- it opens the field and comes back;
- it opens Options, captures two tabs and returns to the pause;
- it resumes and checks the race carries on (`LITTLE_ROAD_RACE_PAUSE_REVIEW_PASSED`).

Captures: [pause](../review/ui-screens/smoke35/captures/02c-pause.png), [the field](../review/ui-screens/smoke35/captures/02d-field.png).

## 15. The Options screen (Richard's request)

**What it is.** One screen with five tabs: GRAPHICS, DISPLAY, AUDIO, DRIVING and INTERFACE. It opens from:
- the pause hub (SETTINGS > Options);
- the race pause menu;
- the title screen.

Each row shows its value as "‹ value ›", with a help line underneath. Left and right step the value, and every change applies at once (`GraphicsOptions.Step` plus the `Changed` event).

**New settings** (the genre survey in doc 57 marks these as essential):

| Setting | Values |
|---|---|
| Motion blur | On / Off (races) |
| Speed effects | the speed field-of-view widening: Full / Reduced / Off |
| Camera shake | Full / Reduced / Off |
| Speed units | mph / km/h, for every speed shown: dash, race HUD, speed traps, parts shop, field screen, test drive |
| Vibration | Full / Reduced / Off; new controller rumble on knocks and off the tarmac (`Rumble`) |
| Ambience volume | its own slider (it was tied to effects) |
| Minimap | On / Off |
| Button prompts | Automatic / Keyboard / PlayStation / Xbox |

**Check.** `OptionsChecks` confirms:
- every setting is on the screen, with a help line;
- the defaults;
- the unit conversion;
- that stepping wraps round and tells listeners.

Captures: [graphics](../review/ui-screens/smoke36/captures/02e-options-graphics.png), [driving](../review/ui-screens/smoke36/captures/02f-options-driving.png), [title](../review/ui-screens/title-options-2/title-main.png), [pause hub](../review/map-sweep/pause-settings-1/p6-settings.png).

**Sound smoke note.** Your play session set Engines & effects to 60%, and the mixed capture (`player.wav`) follows your volume. The sound smoke now reads the engine-only capture (`player-engine.wav`), which ignores settings: −12.5 dB with a 0.80 peak, unchanged.

## 16. Grounding: planting and cars (kept)

- **Planting.** `PlacementAudit.Settle` runs before static batching. Any bush, tree, rock or reed hanging more than 0.3 m over the ground under its whole footprint is lowered onto it. It moved the last 2 in the world; any new area gets it for free.
- **Cars** (Richard: "some models seem to float"). `CarContact` finds each car's four wheel contact patches.
  - `SeatAll` lowers and tilts every standing car (residents, parked cars, taxis) onto its four wheels, up to 12°, once the world is ready.
  - The area audit measures every car with wheels: all wheels up, one wheel more than 15 cm off, or sunk.
  - `ContactChecks` seats a car on an 8° slope in the editor.

## 17. Cars ride on their wheels (kept, studies 218–219; Richard's report)

**Found by the car-contact check.** Mid-race, 25 of 29 cars rode 18–23 cm above the road with all four wheels in the air.

**Cause.** Each car's physics box is fitted to the bounds of its parts, rotated into the car. A wheel mesh sits at a rolled angle, so its rotated box reaches about 0.4 of its radius below the tyre. The box hung 7–18 cm under the tyres on every body except the compact (0.8 cm), and cars drove on it.

Box bottom below the tyres, per body (`ContactChecks`):

| Bodies | Gap |
|---|---|
| puddle | 6.7 cm |
| postvan, minibus | 7.1–7.5 cm |
| comet | 12.4 cm |
| the family field and sporty bodies | 14.3–17.6 cm |

**Fix.** The box's bottom goes to the model's base, where the tyres touch (every car model stands on y = 0).
- **Study 218** used mesh vertices, which aren't readable in the player. It changed nothing; it's recorded as a failed attempt.
- **Study 219** (Cedar and Bayview, seeds 1709–1711) passed:
  - 144/144 finished with no resets;
  - total time 9616.6 → 9612.6 s;
  - contacts 54 → 57, with 2 hard contacts (Cedar 1710).

  Kept. It's now the default; `--old-ride-height` restores the old box.

**Check.** `ContactChecks` now requires every body's box within 2 cm of its tyres.

## 18. Driving controls: manual on R2/L2 with Cross/Square, and remapping (Richard's request)

**What changed.**
- Manual gears now drive like automatic: R2/L2 (RT/LT, W/S) for throttle and brake. Cross/A shifts up and Square/X shifts down (Left Shift / Left Ctrl on the keyboard). The right-stick throttle is gone.
- Near someone to talk to, Cross/A talks instead of shifting.
- `Controls` holds one controller button and one key per driving action: accelerate, brake, shift up, shift down, look behind, recover, horn. They are saved like the other settings.

**Remapping.** Options gets a CONTROLS tab.
- Confirm a row, then press the new button or key. Backspace restores the default; Esc cancels.
- Binding something already in use swaps the two actions, so nothing is left unbound.
- Reset all controls restores the defaults.

**Where it shows.**
- The pause hub's CONTROLS page lists the live bindings.
- The on-screen hints (look behind, recover, drive) name them.

**Check.** `OptionsChecks` covers the defaults, the button names, the swap, reset, and that the tab lists every action.

## 19. Parts shop: Road / Rally / Ice and class tabs (Richard's request)

**What changed.**
- **One class at a time.** The shop and fitting bay show one class's parts. L1/R1 (Q/E) step through the classes you've unlocked, shown as pills under the title. The shop starts on your newest class; the fitting bay starts on the fitted part's class.
- **Categories moved.** They are a column of icon chips down the left edge (Richard: "a list that is up and down"), moved through with D-pad up/down ([ ] or the arrows on the keyboard). The current chip is gold and steps out, and its name heads the stand. Left/right browse the parts as before.
- **Road / Rally / Ice.** Pills at the top choose the discipline. Parts carry a `discipline` field (empty means road). There are no rally or ice parts yet, so those pills read "SOON" and say so when chosen, instead of opening an empty shop.

**Check.** The scripted shop review (`run-parts-review`) covers the new controls: categories on the D-pad and brackets, the class step, and Rally saying "coming soon". It passes all 36 checks.

[Capture](../review/ui-screens/shop-list-2/parts/01-shop.png).

## 20. Gadgets: the building blocks for jet packs and wings (Richard's request)

Gadgets are a small system alongside the seven performance parts, so saves, stats and the race preview are untouched.
- **The `Gadget` contract.** A gadget is fitted to a car, stepped with its physics given the button's held and pressed state, and shows a 0–1 charge for the HUD. A registry maps ids to behaviours. `Resources/Data/gadgets.json` is the catalogue for sale, with id, name, hint, class, discipline and price.
- **One gadget per save.** It's stored in `AdventureProgress.gadget`; older saves read as none, and validation refuses unknown ones.
- **A remappable button:** R1 / RB / G, under Options > CONTROLS.
- **The player's car runs its gadget** every physics step.
- **The test gadget.** `hop` (`--gadget hop`, not on sale) gives a short upward push, then a 3 s recharge, as the seed of a jet pack.

**Adding a jet pack** takes three things: a `Gadget` subclass, a registry line, and a catalogue row. A HUD charge meter and a shop page come with the first real gadget.

**Check.** `GadgetChecks` covers the catalogue and behaviours, the save field, the default button, and the hop's fire, empty, no-refire and recharge.

## 21. Parts are discoveries (Richard's design)

"When parts are discovered at a new location... those parts should be then available for purchase at every parts shop, and available for fitting at every garage... but you have to visit the specific part shop first." This also allows gatekeeping "like x amount of wins in the said tier".

**How it works** (`PartDiscovery`):
- **Home shop.** A part with a home shop (`shop`, for example Hillcrest's eight valley parts) is **found** the first time you open that shop's parts counter. From then on every parts shop sells it. The shop says "New parts found here: …. Every parts shop stocks them now."
- **Fitting.** The fitting bay fits everything you own, at every garage. It used to hide Hillcrest parts in Bayview.
- **Unlock conditions** (`unlock` on a part):
  - `wins:5`
  - `podiums:B:3`
  - `class:A`

  Until a condition is met, the part shows at its home shop as LOCKED, with the condition in words ("Unlocks with: 3 podiums at class B races"). It's found on the next visit after that.
- **Class gating** (your open class and below) still applies on top.
- **Saves:** `AdventureProgress.partsFound` holds what's been found; parts you already own count as found.

**For new regions** (doc 57 §2.6):
- Each region's 2–3 local parts get its parts shop as their home.
- Gatekept parts get an `unlock`.

**Check.** `PartDiscoveryChecks` covers:
- a new game doesn't see Hillcrest parts in Bayview, though Hillcrest's counter does;
- the visit finds them once, and then Bayview sells them;
- the conditions read plainly and hold parts back until met.

The shop review and the smoke test (which buys and fits parts) pass.

## Release

Each batch was built, smoke-tested, sound-checked and published to the play copy.
- **Build:** 2832 editor checks (later batches).
- **Smoke tests:** smoke28 (houses and contacts), smoke29 (gate and paint), smoke30 (High-comp voice) smoke31 (Bayview roofs) smoke32 (painted side windows), smoke33 (junction shoulders) and smoke34 (bunting, silent runs). Each passed: both 24-car races finished and the save reloads.
- **Sound smokes** (`review/sound/smoke28`–`34`): about −12.5 dB with peaks of 0.79–0.81 and no exceptions.
- **One start-up crash:** a native crash inside the Unity player (before any game code) happened once in a sound smoke (`smoke40`). Two re-runs were clean. It's noted in case it recurs.
- **Night views** of the new houses, roofs and gate look right ([Hillcrest](../review/map-sweep/night-1/hillcrest.png), [Bayview](../review/map-sweep/night-1/bayview.png)).
