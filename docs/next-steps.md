# Next steps: where the game stands

This is the live status: what's in the game, what's being worked on, what comes next, and what is waiting on Richard.
Add new status at the top of "Latest" in a line or two, and move anything that becomes a decision into
[design.md](design.md) or [world.md](world.md). Round-by-round history lives in git, not here.

## Latest

- **24 Oct: near start, stage 4** ([brief-near-start.md](brief-near-start.md), `--near-start`, off): from a start away from
  Bayview, what stands on the island is built in the background. Every start is faster with the flag: Bayview 3.0 s (default
  4.8), Hillcrest 4.2 (4.9), Bramblewick 5.5 (6.1), Quarry Hills 6.1 (6.9), Vinelands 7.4 (8.0); the same world. Also fixed:
  stage 1's reach guard ran without the flag and finished Vinelands in one 3 s frame after a Bramblewick start (main only,
  never in the play copy). Next: stage 3 (terrain chunks) and 5 (the town set-up), and the far places' own load at their
  starts (Vinelands at load is ~2.5 s).
- **23 Oct (late night): near start, Hillcrest's frames and stage 2** ([brief-near-start.md](brief-near-start.md),
  `--near-start`, off): Hillcrest's background build split (its busiest piece 109 → 44 ms, frames over 50 ms 6 → 0-1), and the
  far places' roads drawn in the background before it. Ready from Bayview **2.9 s**; the same world, checked at once and
  after a background build as in play (`--fingerprint-late`). Next: stage 3 (terrain chunks by distance) or 4 (Bayview's
  island as a district, for starts elsewhere: 5-8 s now).
- **23 Oct (night): build near the start, stage 1** ([brief-near-start.md](brief-near-start.md), behind `--near-start`,
  off): from Bayview, Hillcrest's valley layer (town and trees) builds in the background, the places that share its ground
  after it. Ready **3.2–3.3 s** against 4.7–6.0 s the same hour; Hillcrest arrives 1.8 s after you can drive; the same world
  (fingerprint and town print). Starts elsewhere unchanged for now (Hillcrest 5.0 s, Quarry Hills 6.7, Bramblewick 6.1,
  Vinelands 7.9). Next: split Hillcrest's five slow parts (to 109 ms a frame), then stages 2-5.
- **23 Oct (evening): start-up 7.24 s → 6.52 s** (two second launches: 6.53, 6.51). Three more things are cooked in the
  world cache: the island's heights (476 → 15 ms), the road lines' ground samples (201 → 99 ms; Vinelands' were 116 ms), and
  the valley's tint texture with its mips (128 → 11 ms, the same checksum). Each build fingerprints the same cooked and with
  `--no-world-cache`. Also fixed: since 67f2efb a comment had swallowed the world build's reset (footprints, placed pieces,
  ground fit, bunting, the first load step), so a rebuilt world kept the last build's footprints.
- **23 Oct: start-up 13.0 s → 7.2 s** (ready to drive, a build's second launch; budget 3.5 s). Bramblewick and Vinelands
  now build in the background after Quarry Hills, as districts ([design.md](design.md) §13; `--no-far-districts`), each
  dressed, barriered and settled when it arrives; a race in the kept world finishes only the districts its area touches.
  Same pieces either way (39,935; the fingerprints differ only in settled poses by millimetres and one borderline bank
  barrier on the Estrada do Cais). Also fixed: the kept world was dressed and barriered again on every return to town.
  Still to cut (the world build is 5.9 s): the island heights (0.48 s), the mainland chunks (0.46 s), the roads (0.88 s),
  Hillcrest's town (0.9 s), the runs' flush (0.54 s), the town's set-up (1.3 s). A build's first launch is about 37 s: the
  map bake completes every district at once. Play copy published from main 3b2f2da (Richard, 23 Oct: no regression first).
- **23 Oct: the docs sweep.** 65 docs were folded into five living records (see [docs/README.md](README.md)).
- **23 Oct: Car Forge models everywhere** (main 861900d, 35a4d35; play copy published): every old class C body shows as
  its Forge namesake, the body shops sell the Forge range, a "My garage" screen, and a smooth body shop preview (24.3 →
  7.3 ms a frame). Class B Tuners keep the old models until their roster is redone.
- **23 Oct: Vinelands is part of the game** (all eight zones and the Circuito da Cumeada; `--no-vinelands`). Its load
  went from 26.6 s to 3.3 s (87b95b2); start-up as a whole was 27 s, then 13 s (runs meshed on every core), then 7.2 s (above).
- **23 Oct: racecraft v3 is the default** (`--no-racecraft-v3`): lane commitment and committed overtakes, −0.8% race
  time, passes 44 → 74, contacts level. Regression rc3 passed, and the play copy was published from main on 23 Oct.
- **21 Oct:** Bramblewick is on by default; the hybrid prototype is behind `--hybrid` (Richard: "a baseline battery
  effect and leave it there").

## What's in the game (player builds, default)

- **Places:** the rebuilt Bayview, Hillcrest (town, castle, valley, raceway), Quarry Hills, Bramblewick and Vinelands
  ([world.md](world.md) §3).
- **Racing:** 24-car races on every built track, class C and the six class B Tuner events, with the field recipe,
  racing AI v2 with per-car lines, the grip budget and racecraft v3. The Duke's Challenge (the final boss's race) can be
  tried early; the full ladder isn't built.
- **Cars:** the Car Forge class C roster (55 cars) with Forge liveries; the legacy Tuner bodies for class B;
  per-body collision boxes, swept contacts, wheel camber.
- **World:** Prop Forge's remodelled props and runs everywhere, background districts, the persistent world across races,
  traffic with courtesy (`--traffic-yield` on), ambient loop traffic at Bayview, fields you can drive through
  (`CropField`), the stop-look camera. The distant mountain ring is off (the map's edge is the sea).
- **Systems:** parts discovery and class gating, the Tuner Garage, the tuning shop, real gearing with auto and manual
  boxes, recorded-engine sound, music per area, ambience, three save slots, the pause hub, options, the snap tool.

Opt-outs exist for everything that became a default: `--old-bayview`, `--no-prop-forge`, `--no-forge-roster`,
`--no-livery`, `--no-wheel-camber`, `--no-traffic-yield`, `--no-ambient-traffic`, `--no-stop-look`,
`--no-quarry-hills`, `--no-bramblewick`, `--no-vinelands`, `--no-racecraft-v3`, `--no-field-recipe`, `--no-grip-budget`,
`--no-car-lines`, `--no-background-districts`, `--no-far-districts`, `--no-world-tiles`.

**Behind flags, off:** `--near-start` (build near the start: stage 1), `--hybrid` (the City hybrid at Jed's), `--duke-drive` (waits for traffic that can handle a car at
pace), `--distant-mountains`, `--turbo-sound`/`--hybrid-sound`, `--scale-test` (the graybox continent).

## In progress (other sessions, 23 Oct)

- Car models: a new roster in Car Forge.

## Next three pieces

1. **Build near the start** ([brief-near-start.md](brief-near-start.md)): stages 1, 2 and 4 are in behind `--near-start`
   (3.0 s from Bayview, 4.2-7.4 s elsewhere). Next stage 3 (terrain chunks by distance) and 5 (the town set-up), and the far
   places built at their own starts (a Vinelands start builds all of Vinelands at load), then default on and the play copy. Also the open Quarry Hills audit line (a car not on the ground, from commits
   since 21 Oct), and the background districts' busiest frames (Quarry Hills' kettle street 190 ms, Vinelands' parts 180 ms;
   the bar is 50 ms).
2. **Vinelands leftovers:** the planting top-up and the small junction seams and lips.
3. **Orchard End**, the next place on the build order ([world.md](world.md) §1): brief first, zone by zone.

## Waiting on Richard

- **Class B cars:** the Forge class B roster was withdrawn on 18 Oct, to be redone with his direction from the start.
- **The UI art pass** (bug #18: "AI-ish UI elements need a revamp"): its own brief for him first.
- **The activities sweep:** he plans a new approach to activities; new places get none until then.
- **Story script and tone** (residents' lines, the frenzy, the credits): his direction.
- Smaller calls: Bayview's sand run-off width; the railway wars' open questions; My City's open questions.

## Open, decided by Claude when reached (design freedom, 20 Oct)

- **Parts slots** ([design.md](design.md) §5): Induction in place of Turbo, a Suspension slot with road roughness, the
  speciality rule, field-stars in data. Jed's rural parts wait on this.
- **Platforms for A to E:** a platform purchase, or a free car plus kits as in class B.
- **Class B parts curve:** some Hillcrest C specialities out-pull Good Tuner parts; a real class B gravel tyre; ice tyres.
- **Peak and floor:** a slower stock car would make early upgrades feel bigger (a keep/reject question); retune class S
  gearing in the class S phase.
- **Racing AI ideas:** the launch funnel, keeping the last attack side, the Bayview bridge S pile-up; E-class weak drivers.
- **Genre gaps:** wrong-way warning, a waypoint and route line, difficulty, camera choice, text size, brightness,
  credits screen, replays and ghosts.
- **W1 foundations still to do:** floating origin, data-driven regions, HLOD, streaming the towns' buildings.

## Known issues

- Start-up time: 4.7–6.5 s by default (the machine's load moves it), 3.0 s with `--near-start` from Bayview; starts
  elsewhere 4.2-7.4 s with it (4.9-8.0 without).
- An intermittent native crash in the first-launch warm-up (Unity player, no managed stack): once in ~15 first launches on
  23 Oct (with `--near-start --no-quarry-hills --no-bramblewick --no-vinelands`); crash reports from 2, 3 and 5 Oct too.
- A build's first start from a place it hasn't started from before audits the bank sites for that set of pending
  districts: 17.4 s from Hillcrest's square, 10.8 s from Vinelands (seconds, cached after).
- The Ponte Nova's bank barrier (4514, -3350) is found on some builds' first launch and not others (22 or 21 Vinelands
  bank sites, cached per build): the edge audit counts anything solid as a guard, and a moving thing near the bridge's
  edge (likely a resident's idling car, which isn't a `RoadCar`) can hide the drop. Fix: the audit skips anything that moves. Workshop engine feedback at dusk near the Bayview garage (bug #1, cause unknown).
- The editor check `CarLinesChecks` fails a build when the machine is busy (the stock C racing-line bake must take under
  2 s; it took 2077 ms with test runs going): build again.
- An early race (before the background districts have built, about the first 15 s) completes the districts its area
  touches behind the loading card.
- Car Forge Kestrel: pinched glasshouse and an empty rear interior (bugs #12, #13).
- Audit leftovers: Hillcrest's floating vine row and one hidden sign, road-edge lips in Quarry Hills and Hillcrest
  Valley, two waterline rocks at Bayview, Vinelands' seams and planting.
- A full-throttle start from rest can stall on Bayview Halt's gravel ramp; the 61–62 mph engine note on the club motor
  with the Street turbo (parked).
