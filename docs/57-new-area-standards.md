# 57 · Standards for new areas, and what players expect

Richard, 2 October:
- "write a standard and implementation doc on what new areas should require standards-wise to ensure new places are as good as the old";
- and "do some research into racing games in general and ensure we have everything people would expect".

**The four parts of this doc**

1. **Process:** the order in which a new area (a region, a town, a track) is built.
2. **Standards:** what it must reach, measured wherever possible.
3. **Where Bayview, Hillcrest and the valley stand** against those standards today.
4. **Genre checklist:** what racing-game players expect, and Little Road's status against each item.

**Where the numbers come from**
- The area audit (`--area-audit`).
- The placement and edge audits.
- The editor checks.
- The benchmark drive.

A standard that can't be measured says so, and says how it's reviewed instead.

Sources:
- a survey of the existing rules across docs 09–56, `art-direction.md`, `graphics-scorecard.md`, `visual-quality-audit.md`, `ui-design.md` and `rendering-and-streaming.md`;
- the 2 October wider-game round ([doc 56](56-wider-game-round.md));
- web research on genre expectations (sources at the end).

---

## Part 1 · The process

Each step has its tools and an exit criterion. A step isn't done until its exit criterion holds. Run the checks after every batch, not only at the end.

| # | Step | What happens | Tools | Exit criterion |
|---|---|---|---|---|
| 0 | **Brief** | One paragraph covering the region's character, hero landmark, class, events, local parts (2–3 specialities, doc 44), its keepsake set, and its music theme. | doc | Richard has read it. |
| 1 | **Graybox roads and track** | Roads go through `RoadNetwork` (junctions as nodes, 6 m fillets). Tracks follow the track standards (2.10). Pit lanes are reserved now for class A and up. | `RoadNetworkChecks`, `RaceGridChecks` | Every road is drivable end to end. Junctions pass `RoadNetworkChecks`. |
| 2 | **Event and AI validation** | 24-car races on the graybox, on three seeds (1709/1710/1711). | `run-pace-parallel.ps1`, `summarize-racer.py` | 24/24 finish on all seeds. No assisted resets. No repeated stop of 2 s or more. |
| 3 | **Landform** | Terrain, embankments (fill to about 1 in 4), rivers, coast and the seabed. Each region's ramps sample **its own** ground. | `--edge-audit`, `--view-shots` | 0 lips. Every drop is guarded or drivable (2.4). |
| 4 | **Town** | Buildings from the kit, through `BayviewKit.Spawn`. Street furniture, lamps, bunting, parapets, the Pit Stop, and the hero landmark. | `--placement-audit`, `--area-audit` | The town rows of 2.1 pass. 0 floating pieces. 0 loose bunting. |
| 5 | **Art pass** | Palette, materials and night lights. No blank walls. Roof variety. Silhouettes. | `WorldLookChecks`, `PresentationChecks`, `capture-map -Rendered`, night views | Key views, framed arrivals, and day and night captures reviewed. |
| 6 | **Life** | Named residents (talkable), chatting folk, parked cars and cruising traffic. Ambience and the music theme. | `--area-audit`, `--car-audit` | The life rows pass. Every standing car has its wheels on the ground (2.5). |
| 7 | **Discoveries** | Keepsakes, speed traps, photo sights, viewpoints, side quests and recruits. These come last, once roads are final (doc 45 F10). | `--trap-test`, `--photo-requests-test`, `--area-audit` | The things-to-do rows pass. |
| 8 | **Performance** | A benchmark drive through the area, plus a 24-car race with the frame probe. | `drive-benchmark.ps1`, `run-frame-cost.ps1 --frame-probe` | The budgets in 2.9. |
| 9 | **Release** | Build (all editor checks), smoke test, sound smoke (engine-only capture), area audit, then publish. | `build-dev.ps1`, `capture-adventure.ps1`, `--area-audit`, `publish-play.ps1` | Everything green. The area audit for the new region says **PASS**. |

New behaviour goes behind an off-by-default flag with an editor check (CLAUDE.md). Keep/reject studies follow the usual protocol.

---

## Part 2 · The standards

### 2.1 Measured by the area audit

`capture-map.ps1 -Name <run> -Flags @('--area-audit')` writes `area-audit.txt` and `.json`. It has one column per map region (`MapRegions`; a new region declares its `kind`, town or country) and ends with **PASS** or **FAIL** for each region against these bars. The bars live in `AreaAudit.Standards`; change them there and here together.

| Measure | Town | Countryside | Why this bar |
|---|---|---|---|
| Buildings per road km | ≥ 15 | — | Bayview, the sparsest town (its region includes the island loop), has 16. |
| Building designs | ≥ 8 | — | Bayview has 11 and Hillcrest 18. Fewer, and the rows repeat. |
| Most common design's share | ≤ 25% | — | Hillcrest was 72% one villa before 2 Oct. It is now 14%. |
| Same design next door | ≤ 25% | — | Hillcrest is at 10%. |
| Wall colours | ≥ 10 | — | Each town has its own named palette (art-direction). |
| Roof colours | ≥ 4 | — | Uniform roofs read as a toy set (doc 56 §2, §8). |
| Lamps per road km | ≥ 15 | — | Every street is lit at night. |
| Street furniture per road km | ≥ 50 | — | Benches, planters, stalls, flags: no bare pavements. |
| Planting per road km | ≥ 500 | ≥ 1,500 | "No empty lawns" (visual-quality-audit). |
| Named residents | ≥ 4 | — | People you can talk to, each with a role (map labels). |
| Life per road km (residents, folk, parked, cruising) | ≥ 10 | ≥ 5 | "Beautiful but empty" is the most common open-world complaint (Part 4). |
| Pit Stop | 1 | — | Every town has the uniform Pit Stop (art-direction, doc 48). |
| Things to do (keepsakes, speed traps, photo sights) | ≥ 3 | ≥ 3 per road km | About one thing every 30 s of driving. |
| Floating pieces (above the sea) | 0 | 0 | |
| Loose bunting ends | 0 | 0 | Every run hangs from a lamp, pole or building (doc 56 §11). |
| Road-edge lips (12 cm or more, unguarded) | 0 | 0 | The car's body can't climb them (doc 50). |
| Banks too steep and unguarded | 0 | 0 | Over 0.55 per metre and 40 cm within 12 m, with no barrier before the drop (doc 56 §10, 13). |

### 2.2 Look and light (`art-direction.md`, enforced by `WorldLookChecks`)

- **Toy shapes, real light.** Every light, grade, cloud and colour number lives in `WorldLook.cs`, and the build fails if they drift.
  - Create materials only with `TrialWorld.Material`, and terrain with `WorldLook.Terrain`.
  - Never hand-set `_Stylise` or saturated colours.
  - Start a new area from `TrialWorld.Create`.
- **Palette.** Each town has its own named earth-pigment palette, never pastels. The check confirms every entry grades into the limits.
- **Custom shaders** need `_LIGHT_COOKIES`, sRGB vertex colours, `MixFog`, depth and shadow passes, and `LOD_FADE_CROSSFADE`.
- **Fundamentals** (visual-quality-audit), reviewed in captures:
  - value hierarchy, checked in greyscale;
  - silhouettes;
  - framed arrivals, with the hero sightline clear;
  - detail hierarchy (no empty lawns);
  - grounding (contact shadows, foam where water meets anything upright);
  - atmosphere;
  - no pop (pieces cross-fade at their cull distance);
  - motion and life.
- **Quality bar.** Calibrate against Art of Rally, Lonely Mountains, Tiny Glade and A Short Hike. Towns score 7–8 on the graphics scorecard, and a new area reaches at least 7 ("trailer-worthy").

### 2.3 Buildings and streets

- **Kit and placement.** Author kit pieces in `source-art/build_bayview_kit.py`, with a footprint and an `ASSETS` entry. Place them with `BayviewKit.Spawn`, the single registry for culling, the audits and streaming later.
- **Variety.** Mixed heights on a shared plot, so the eaves line steps. Roofs tinted per house. Cosmetic choices come from their own random stream, so the layout stays stable (doc 56 §2).
- **No blank walls.** Every side wall a player can see gets painted windows (`side_paint`) or real ones (doc 56 §9).
- **Align to the road as built.** Gates, gatehouses and arches face along the road's heading at that point; check with a `PresentationChecks` test like the castle gate's (under 1 m off the centre, under 10° of skew; doc 56 §4).
- **Hillside streets.** A low stone parapet along the downhill side, with gate-wide gaps at front yards (`HillcrestTown.Parapets`; doc 56 §13).
- **Hero landmark.** It reads from anywhere in the area, has a stepped skyline with one tallest element and colour inside the grey, offers something to do (a ride or shortcut), and is lit at night.
- **Planting.** A settle pass lowers any bush, tree or rock left hanging over the ground (`PlacementAudit.Settle`, before static batching), but place things on the ground in the first place.
- **Clearances** (houses at least 8 m from the road; trees 14 m from road, 10 m from rail, 22 m from the river): see the `ValleyField.RoadDistance` callers. Reserved plots (`HillcrestTown.Reserved`) keep Pit Stops and landings clear.

### 2.4 Roads, edges and barriers

- **Widths.** Town streets 7–8.5 m, lanes 6.5–7 m, circuits 16 m (see 2.10).
- **Junctions.** Build them with `RoadNetwork`: fillets, one level surface, shared mouth vertices, kerb paint, give-way lines and sign (doc 52). Crossroads are untested and roundabouts don't exist yet; test both before using them.
- **Paint.** Stripes use mean-direction quads (no saw-teeth on bends; `PresentationChecks`: zero seam on an 8 m bend).
- **Getting back on.** Every road edge a car can leave is drivable back up:
  - gravel shoulders along roads (`Shoulders`, sampling the region's own ground);
  - ramps round every junction kerb (`KerbShoulders`);
  - end ramps on free ends.
- **Barriers.** Where the ground falls away (a drop over 1.1 m in any 2 m within 8 m of the edge, or 1.8 m within 3 m), the edge gets:
  - a guardrail on valley roads and circuits;
  - a stone parapet in hill towns;
  - a railing on sea walls and quays.
- **Bridges.** Bridges and causeways have parapets with colliders.
- **The test.** `--rejoin-test` (from both sides, at 90°, 60°, 35° and 20°, part and full throttle) must have no failure that isn't scenery in the way. Bayview Halt: 43 of 48; the 5 that fail are scenery and one known full-throttle start from rest.

### 2.5 Driving and cars

- **Contacts.** Swept contacts for every car (doc 56 §1).
- **Grounding.** Every car model stands with its wheels on the surface:
  - standing cars use `StationarySpot.Settle`;
  - the car-contact check (end of the 2 Oct audit work) measures parked, standing and racing cars.
- **Recovery.** Recovery (R / D-pad down) must find a clear road spot. The AI rejoin rules in `RecoveryRejoinChecks` apply to any new track.
- **Traffic.** Turnarounds sit away from narrow gates: the castle gateway caused physics jams (rendering doc).

### 2.6 Landmark, life and things to do

Every region gets:
- the hero landmark (2.3);
- named residents with talk badges and roles;
- chatting folk and parked cars at homes;
- cruising traffic on its roads;
- one keepsake set with its own album place names (`InterfaceChecks`);
- speed traps (`SpeedTraps.Plan`: on the straightest point, star marks from the parts ladder);
- photo sights (framed in the middle 70%, 90–520 m away, nothing solid in the way);
- viewpoints (520 m reveal);
- a DISCOVERED card for each place;
- a side quest;
- 2–3 local parts with this region's parts shop as their home (`shop`). They're found on the first visit, then sold everywhere. Gatekeep the special ones with an `unlock` (wins, podiums in a class, class reached); see `PartDiscovery`.

Closed-road events use bollards with side roads lined 7 m beyond. Finishes get the gold beacon.

### 2.7 Services

- **Pit Stop.**
  - Uniform in every town: `pit_stop_3` (garage, parts, body shop); Bayview is the 2-bay exception.
  - It sits at the town's arrival point on the main road, frontage to the road, with a ~9 m forecourt and the pylon at the kerb. The pad is levelled to 22 × 34 m (`PitStopChecks`).
- **A town loop or track.** Every town has one for the tuning test drive (doc 48). A region with a body shop sells bodies there; `PitStopChecks` must stop assuming "hillcrest" when the third body shop arrives.

### 2.8 Map, names and interface

- **The region in `MapRegions`.** It needs:
  - `id`, `name`, `blurb` and `kind`;
  - a containment test;
  - an outline of at least 4 points;
  - collection counts.
- **Fog.** The fog reveals as you drive (175 m).
- **Map labels.** Place names, people and Pit Stops are on the map labels (`WorldMap`).
- **Areas.** The region's `AreaBanner` areas exist. `MapRegions` has hard-coded z thresholds today (−452, −380); move them into the region definition before region 3.
- **Text and prompts.** Text has at least 4.5:1 contrast and prompts are at least 24 px. Prompts follow the controller in use, or the Interface setting.

### 2.9 Sound, night and performance

- **Music.** A per-area explore theme and race version, levelled to RMS 0.1, in stereo and over 20 s long (`SoundChecks`). Without one, the area falls back to Hillcrest's theme: write one.
- **Ambience.** The region's water, wind and wildlife, tied to its own features (as the falls and river are).
- **Night.** Street lamps on every street, lit windows (`GlassLit` and `_KitNight`), and the landmark lit. Glowing things use a black base with emission (about 0.5×), or they clip at night.
- **Performance budgets.** Reference PC (RTX 4070 Ti Super, 1080p), `drive-benchmark` through the area:

| | Budget | Today (route Bayview → castle) |
|---|---|---|
| Mean frame | ≤ 6.0 ms | 5.0 ms |
| p99 | ≤ 12 ms | 9.3 ms |
| Frames over 33 ms | 0, except teleports and loading | 1 (the benchmark's teleport) |
| Triangles (mean / max) | ≤ 30 M / 110 M | 19.7 M / 102 M |
| Managed allocation per frame | 0 KB | 0 KB |
| 24-car race (frame probe) | p99 ≤ 12 ms | see `run-frame-cost` studies |

Culling is automatic (pieces under 3.5 m drop out at 280 m, under 7 m at 700 m). Trees use their own LOD.

### 2.10 Tracks (doc 45 F9)

| Classes | Racing width | Behind the start line | Pit lane | Run-off |
|---|---|---|---|---|
| C/B | 10–12 m (towns), 16 m circuits | ≥ 95 m (24 cars, 11 m rows) | none | grass or gravel, armco where the ground falls away |
| A/S | 13–15 m | ≥ 95 m | 10 m lane and boxes, reserved from graybox | proper run-off and barriers |
| E | 14–16 m | ≥ 95 m | longer lane, night lighting | as A/S |

- **Speeds to design for** (m/s, mid-build): C 24–40, B 38–50, A 48–60, S 58–74, E 62–80.
- **Corners.** Mixed radii, with clear braking zones before the overtaking spots. No opening S-bend bottleneck. No 21 m hairpin without testing the AI there (doc 46).
- **Elevated tracks.** Downforce cars need barriers: the maximum build left the elevated raceway at 75 m/s (doc 54).
- **Not checked yet.** These track standards have no editor check. A `TrackStandardsChecks` (95 m, width, pit-lane reservation) is on the backlog (Part 5).

---

## Part 3 · Where the game stands (area audit, 2 October)

`review/map-sweep/area-audit-7/area-audit.txt`, after the 2 October fixes in [doc 56](56-wider-game-round.md) §10–16.

| Measure | Bayview (town) | Hillcrest (town) | Valley (country) |
|---|---|---|---|
| Road km | 1.05 | 1.79 | 3.14 |
| Buildings per road km | 16.2 | 180.8 | 1.3 |
| Building designs · most common design's share · same design next door | 11 · 18% · 21% | 18 · 14% · 10% | 4 · 25% · 0% |
| Wall colours · roof colours | 17 · 5 | 19 · 6 | 4 · 1 |
| Lamps · street furniture · planting per road km | 18 · 68 · 698 | 63 · 99 · 989 | 0 · 119 · 4,446 |
| Named residents · folk · parked · cruising | 6 · 0 · 8 · 0 | 5 · 74 · 4 · 7 | 4 · 2 · 13 · 3 |
| Life per road km | 13.3 | 50.4 | 7.0 |
| Keepsakes · speed traps · photo sights · Pit Stops | 5 · 2 · 2 · 1 | 1 · 1 · 1 · 1 | 7 · 4 · 3 · 0 |
| Floating pieces · loose bunting · lips | 0 · 0 · 0 | 0 · 0 · 0 | 0 · 0 · 0 |
| Banks without a barrier | 3 | 8 | 4 |
| Cars not on the ground (of those checked) | 0 of 15 | 0 of 90 | 0 of 23 |

**Before the 2 October fixes** (first audit):
- floating pieces 1 / 0 / 1;
- lips 0 / 3 / 1;
- banks 17 / 142 / 45;
- Bayview life 5.7 per km, which was partly an undercount.

**What changed:**
- junction shoulders;
- the Via Crest parapet and garden walls;
- steep-drop guardrails and raceway armco;
- the planting settle pass;
- seated standing cars;
- an audit that recognises real barriers, including gaps narrower than a car.

**What's left**, a known list rather than a pass:
- **15 short unguarded drops.**
  - Bayview loop: 2, at (−121, 35) and (−115, −16).
  - The Halt quay's south corner: 1.
  - Via Crest's outer side: 8 corners where the parapet's checks still say no.
  - The valley road's west end by the river: 3.
  - The Falls road: 1.

  Each is under 10 m long. `edges-oct2-g/edge-audit.txt` lists them.
- **The cars' ride height (fixed).** Every racing and driving car rode 7–18 cm above the road on its physics box, which had been fitted to rotated part bounds rather than the tyres. It now sits at the tyres; study 219 kept it ([doc 56](56-wider-game-round.md) §17).
- **Landscape cars** (the farm car, the Bayview car park, the Halt) are seated before static batching. Afterwards their visuals can't move, which is why the first seating pass looked fixed but wasn't. 0 of 128 cars are off the ground.

A new area must show **PASS** before it ships.

---

## Part 4 · What players expect, and where Little Road stands

From the genre research (sources at the end). **E** = essential, **X** = expected, **N** = nice to have. Status: ✅ have · 🟡 partial · ❌ missing.

### In a race
| Item | | Status | Notes |
|---|---|---|---|
| A pause that pauses, with resume, restart and retire | E | ✅ | Added 2 Oct (doc 56 §14). Smoke-tested every release. |
| Settings and controls from the race pause | E | ✅ | Options, including CONTROLS remapping, from the race pause. |
| Standings in the pause | X | ✅ | Leader, ahead and behind, plus View the field (the whole grid with ratings). |
| Position, lap, speed and gear, countdown, final-lap call | E | ✅ | RaceHud. |
| Wrong-way warning | E | ❌ | Backlog. |
| Race minimap | X | ✅ | The course map in RaceHud. |
| Lap and best-lap times, gaps to the cars ahead and behind | X | ✅ | RaceHud timing board. |
| HUD element toggles and scale | X | 🟡 | Minimap toggle only. |
| Rewind | X | ❌ | |
| Replays with TV cameras | X | ❌ | |
| Ghosts in time trials | X | ❌ | |
| Camera choice (chase near and far, bumper) | E | ❌ | Chase camera only. Backlog: near and far, plus a bumper camera. |
| Look behind | X | ✅ | Triangle / Q. |
| Photo mode | X | ✅ | Five looks, depth of field, frame. |

### Modes and structure
| Item | | Status | Notes |
|---|---|---|---|
| Campaign spine with a visible next goal | E | ✅ | The objective pill and "Next:" in the pause. |
| Instant, unlimited retry | E | ✅ | Restart from the pause; Race again from the results. |
| Quick race (any track, any car) | X | ❌ | Trials exist for development only. |
| Time trials with local best times | X | 🟡 | Max Build Hot Laps, speed-trap records. |
| Skill challenges (speed traps, A-to-B, drift) | X | 🟡 | Speed traps, Castle Climb, Beat the Train. No drift or jump. |
| Named rivals and a boss | X | ✅ | Racer identities; Duke Rolo's challenge. |
| Difficulty setting, changeable any time | E | ❌ | AI skill comes from the class ladder. Backlog: an easy/normal/hard field. |
| No visible rubber-banding | E | ✅ | Fair AI (doc 46); keep/reject studies guard it. |
| Results and podium | E | ✅ | |
| Split-screen | N | ❌ | |
| Leaderboards | N | ❌ | |

### Progression and garage
| Item | | Status | Notes |
|---|---|---|---|
| Upgrade ladder with visible stat changes | E | ✅ | 129 parts across five classes. |
| Performance rating | E | ✅ | PP, and DR for drivers. |
| Tuning (gearing and more) | X | 🟡 | Gearing on the tuning screen. No suspension, tyre or downforce sliders. |
| Test drive | E | ✅ | |
| Cosmetics | X | 🟡 | Paint and bodies. No decals or wheels. |
| Collection screen | X | ✅ | The COLLECTION tab. |
| Fair economy, no grind walls | E | 🟡 | Not yet measured in play (doc 45 Phase 1 item 6). |
| Parts shop that doesn't overwhelm (filters, tiers) | X | ✅ | One class at a time (L1/R1), Road / Rally / Ice pills (2 Oct). |

### Open world
| Item | | Status | Notes |
|---|---|---|---|
| World map with markers | E | ✅ | |
| Player-set waypoint and GPS route line | E | ❌ | Backlog. Objectives are marked but there's no route line. |
| Fast travel (earned) | X | 🟡 | The train and zipline exist. No map fast travel. |
| Activity density | E | 🟡 | The area audit measures it. Bayview and the valley pass; Hillcrest town is at the bar. |
| Completion tracker per area | X | ✅ | Pause-map region hover counts. |
| Side quests | X | 🟡 | Mara's delivery, Pip's photos. Valley activities are on hold (Richard's idea). |
| Traffic and life | X | ✅ | Valley cruisers and folk. Bayview's life is now measured too. |
| Day/night and weather | X | 🟡 | Day/night ✅; no weather. |

### Driving
| Item | | Status | Notes |
|---|---|---|---|
| Analogue throttle, brake and steering | E | ✅ | |
| Automatic and manual gears | E | ✅ | Manual: R2/L2 throttle and brake, Cross/Square shifts, remappable. |
| Assists (steering, braking, traction) | X | ❌ | |
| Racing line display | X | ❌ | |
| Vibration with an intensity setting | E | ✅ | Added 2 Oct. |
| Deadzone and sensitivity | X | ❌ | |

### Settings and accessibility
| Item | | Status | Notes |
|---|---|---|---|
| Full remapping, pad and keyboard | E | 🟡 | Driving actions remap on pad and keyboard (Options > CONTROLS, 2 Oct). Steering and menus are fixed. |
| Hold-vs-toggle choice | E | ❌ | Look behind is hold-only. |
| Separate volumes | E | ✅ | Master, engines & effects, music, ambience. |
| Subtitles | E | ✅ | All speech is text; there is no voice. |
| No information by colour alone; colour-blind modes | E | 🟡 | Most colour carries text too; no modes. |
| Text size | E | ❌ | |
| Camera shake, motion blur and speed-effect toggles; field of view | E | ✅ | Added 2 Oct. |
| Units (mph / km/h) | E | ✅ | Added 2 Oct. |
| Settings remembered | E | ✅ | |
| Brightness | X | ❌ | |
| Game speed, high contrast, narration, languages | X / N | ❌ | |

### Presentation and quality of life
| Item | | Status | Notes |
|---|---|---|---|
| Short loads | E | ✅ | About 8 s into the world. |
| Skippable intros | E | ✅ | There are none. |
| Autosave and slots | E | ✅ | Three slots, autosave after every race, purchase and find. |
| Onboarding | E | 🟡 | Mara's opening quest. No driving tutorial. |
| Engine, tyre, surface and impact sound | E | 🟡 | Engines scored (15 of 18 at 8 or above); tyre and knock synthesis exists; surfaces are thin. |
| Crowd and announcer | X | ❌ | |
| Music per area, with track skip | X | 🟡 | Per-area themes ✅; no skip. |
| Achievements / records | X | 🟡 | The RECORDS tab. |
| In-game credits | X | ❌ | `THIRD_PARTY_ASSETS.md` exists, but there's no screen. |

**Top 15 (from the research) at a glance.** These are now in place:
- pause and restart;
- fair AI;
- the core HUD (but no wrong-way warning);
- the next-goal spine;
- the parts ladder;
- separate volumes;
- the comfort toggles;
- units and vibration;
- short loads and autosave.

The gaps are:
- difficulty;
- waypoint and route line;
- remapping (next);
- hold-vs-toggle and text size;
- camera choice;
- rewind and replays;
- in-game credits.

---

## Part 5 · Backlog from this audit, in order

1. **Existing areas to PASS** (Part 3). Bayview life, the last unguarded drops, and any floating pieces the settle pass doesn't reach.
2. **Car contact (done):** standing cars are seated, racing cars ride at their tyres, and the area audit counts any car off the ground.
3. **Work queue: done.** Input remapping, the parts shop filters and class tabs, and the gadget building blocks ([doc 56](56-wider-game-round.md) §18–20).
4. **Wrong-way warning, waypoint and route line, difficulty setting, camera choice.** The essentials still missing.
5. **`TrackStandardsChecks`**: 95 m behind the line, width, pit-lane reservation.
6. **Region definitions instead of hard-coded z thresholds** (`MapRegions`, `AreaBanner`, `GrassField`, `PlacementAudit`), before region 3.
7. **Credits screen, hold-vs-toggle, text size, brightness.**

**Contradictions settled here**
- **Meadow stylise:** `WorldLook.TerrainStylise` (0.35) is the truth; art-direction says 0.5.
- **SSAO:** the renderer asset (1.5 / 0.6) is the truth.
- **Zipline clearance:** ≥ 10 m under a hanging car; the survey reports it.
- **Embankments:** fill to 1 in 4. The edge audit flags anything over 0.55 per metre with no barrier.
- **Circuit width:** 16 m circuits stand for C/B; the 10–12 m figure is for town and road events.

## Sources

- **Accessibility guidelines:**
  - Xbox Accessibility Guidelines: [104](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/104), [106](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/106), [107](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107), [108](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/108), [117](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/117);
  - [Game Accessibility Guidelines (basic)](https://gameaccessibilityguidelines.com/basic/);
  - Forza Horizon 5 accessibility: [menus](https://caniplaythat.com/2021/11/04/forza-horizon-5-accessibility-menu-deep-dive/), [features](https://news.xbox.com/en-us/2021/11/04/gaming-for-everyone-the-accessibility-features-of-forza-horizon-5/).
- **Genre and design:**
  - restarts: [Gangles on restarts](http://gangles.ca/2009/03/12/restarting-downtime-and-variety/), [Trackmania and "one more run"](https://tparkergames.substack.com/p/trackmania-and-the-one-more-run-feeling);
  - pause and empty worlds: [NFS 2015 (no pause)](https://en.wikipedia.org/wiki/Need_for_Speed_(2015_video_game)), [The Crew Motorfest review](https://gamingtrend.com/reviews/the-crew-motorfest-review-beautiful-but-empty/), [Mario Kart World review](https://www.xda-developers.com/review-mario-kart-world/);
  - rubber-banding: [design note](https://game-design-snacks.fandom.com/wiki/Poorly_implemented_rubber_band_AI_in_driving/racing_games_frustrates_the_player), [Hot Wheels Unleashed review](https://stevivor.com/reviews/hot-wheels-unleashed-review-luke-warm-wheels-at-best/);
  - [Gran Turismo 7 review](https://www.pushsquare.com/reviews/ps5/gran-turismo-7);
  - [Forza Horizon 5 review](https://www.pushsquare.com/reviews/ps5/forza-horizon-5);
  - [Road Trip Adventure](https://en.wikipedia.org/wiki/Road_Trip_Adventure);
  - [racing HUD design](https://gamedesignskills.com/game-design/racing/);
  - [motion blur and perceived speed](https://www.researchgate.net/publication/262202710_Simulated_motion_blur_does_not_improve_player_experience_in_racing_game);
  - [racing game sound](https://designingsound.org/2012/05/01/racing-game-sound-study/).
- **Notes on the evidence:**
  - Some points rest on forums or polls (the rubber-banding figure is one forum poll).
  - The XAG 108 page was read through a search summary.
