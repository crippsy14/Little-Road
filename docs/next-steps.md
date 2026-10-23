# Next steps: where the game stands

This is the live status: what's in the game, what's being worked on, what comes next, and what is waiting on Richard.
Add new status at the top of "Latest" in a line or two, and move anything that becomes a decision into
[design.md](design.md) or [world.md](world.md). Round-by-round history lives in git, not here.

## Latest

- **23 Oct: the docs sweep.** About 60 docs were folded into five living records (see [docs/README.md](README.md)).
- **23 Oct: Car Forge models everywhere** (main 861900d, 35a4d35; play copy published): every old class C body shows as
  its Forge namesake, the body shops sell the Forge range, a "My garage" screen, and a smooth body shop preview (24.3 →
  7.3 ms a frame). Class B Tuners keep the old models until their roster is redone.
- **23 Oct: Vinelands is part of the game** (all eight zones and the Circuito da Cumeada; `--no-vinelands`). Its load
  went from 26.6 s to 3.3 s (87b95b2), but start-up as a whole is still about 27 s against the 3.5 s budget: being worked on.
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
`--no-car-lines`, `--no-background-districts`, `--no-world-tiles`.

**Behind flags, off:** `--hybrid` (the City hybrid at Jed's), `--duke-drive` (waits for traffic that can handle a car at
pace), `--distant-mountains`, `--turbo-sound`/`--hybrid-sound`, `--scale-test` (the graybox continent).

## In progress (other sessions, 23 Oct)

- Start-up load time (about 27 s; budget 3.5 s) and race fixes, on `claude-additions`.
- Car models: a new roster in Car Forge.

## Next three pieces

1. **Start-up back inside budget**, and the open Quarry Hills audit line (a car not on the ground, from commits since 21 Oct).
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
- **Class B parts curve:** some Hillcrest C specialities out-pull Good Tuner parts; a real class B gravel tyre; ice tyres.
- **Peak and floor:** a slower stock car would make early upgrades feel bigger (a keep/reject question); retune class S
  gearing in the class S phase.
- **Racing AI ideas:** the launch funnel, keeping the last attack side, the Bayview bridge S pile-up; E-class weak drivers.
- **Genre gaps:** wrong-way warning, a waypoint and route line, difficulty, camera choice, text size, brightness,
  credits screen, replays and ghosts.
- **W1 foundations still to do:** floating origin, data-driven regions, HLOD, streaming the towns' buildings.

## Known issues

- Start-up time (above). Workshop engine feedback at dusk near the Bayview garage (bug #1, cause unknown).
- Car Forge Kestrel: pinched glasshouse and an empty rear interior (bugs #12, #13).
- Audit leftovers: Hillcrest's floating vine row and one hidden sign, road-edge lips in Quarry Hills and Hillcrest
  Valley, two waterline rocks at Bayview, Vinelands' seams and planting.
- A full-throttle start from rest can stall on Bayview Halt's gravel ramp; the 61–62 mph engine note on the club motor
  with the Street turbo (parked).
