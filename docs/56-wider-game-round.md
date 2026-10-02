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

## Release

Each batch was built, smoke-tested, sound-checked and published to the play copy.
- **Build:** 2812 editor checks.
- **Smoke tests:** smoke28 (houses and contacts), smoke29 (gate and paint), smoke30 (High-comp voice) smoke31 (Bayview roofs) and smoke32 (painted side windows). Each passed: both 24-car races finished and the save reloads.
- **Sound smokes** (`review/sound/smoke28`–`32`): about −12.5 dB with peaks of 0.79–0.81 and no exceptions.
- **Night views** of the new houses, roofs and gate look right ([Hillcrest](../review/map-sweep/night-1/hillcrest.png), [Bayview](../review/map-sweep/night-1/bayview.png)).
