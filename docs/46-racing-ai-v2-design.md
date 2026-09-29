# 46: Racing AI v2: research and design

29 September 2026, branch `racing-ai-v2`. Richard asked for a new racing AI built from the ground up around the six driver abilities, and compared with the first AI as a benchmark. This doc records the research and the design. The first AI's notes are in `docs/archive/racing-ai-v1/`. Its code stays in the game, so the two can race side by side.

## Why start again

The first AI (`ContextDriverPolicy` and about 40 helper classes) is physically honest and deterministic. However, three problems are built into its structure:

- **It hesitates.**
  - Every 0.1 s it re-ranks about six candidate paths from scratch, with no memory of what it was doing.
  - Club cars reversed their planned side 24 times a minute (doc 43).
  - A pass that is 60% done competes equally with "go back behind", so cars lift, pull out, drop back and lift again. This is the pausing Richard sees.
- **Skill and safety are the same numbers.**
  - It corners at 78% of the grip available and brakes at 85% × its braking fraction (`PaceMargins`).
  - Every car is therefore about 4% off the pace before traffic. Neither a skill level nor a difficulty setting can be expressed apart from that.
- **Too many layers decide speed.** These are:
  - a preference map;
  - a planner;
  - physical forecasts;
  - four fallbacks;
  - a moving-leader cap;
  - a static cap;
  - a hold;
  - an immediate guard.

  When a car lifts, it's hard to say why.

## What the research says

| Source | What we take from it |
|---|---|
| Tomlinson and Melder, [An Architecture Overview for AI in Racing Games](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter38_An_Architecture_Overview_for_AI_in_Racing_Games.pdf) (Game AI Pro, ch. 38) | Four layers: persona (skills), strategic (behaviour states, a few times a second), tactical (steering and speed goals every frame) and control (pedals and steering), plus collision avoidance alongside all four. Skill multiplies the grip used for cornering and braking, mapped to a narrow band near the limit (for example 98–99%), because 1% compounds over a lap. Behaviours are a small state machine (race, overtake, defend, recover); each scores a utility, and a hysteresis bonus keeps the current one. After an aborted overtake, wait a few seconds before trying again. While overtaking or defending, drive closer to the limit and accept smaller gaps. A slow "biorhythm" (for example a square wave that dips briefly) makes good drivers occasionally beatable without spreading the field. |
| Melder, [The Heat Vision System](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter41_The_Heat_Vision_System_for_Racing_AI.pdf) (ch. 41) | Choose lateral position from a one-dimensional "heat line" across the track. Racing-line preference, car positions, draft cones and blocking targets all write into it. Smooth it, then roll a ball downhill from the car's current position, so a car alongside acts as a wall that can't be crossed. Keep a record of which car wrote each cell, for speed decisions and debugging. Driver traits scale the heat shapes: aggressive drivers write narrower car shapes, so they need less room to pass. This passes a whole pack in one move instead of weaving car by car. |
| Tomlinson and Melder, [Representing and Driving a Race Track](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter39_Representing_and_Driving_a_Race_Track_for_AI_Controlled_Vehicles.pdf) (ch. 39) | Steer at a look-ahead point (a "runner") whose distance grows with speed. Brake when the braking distance to any slower point ahead runs out. Racing lines are stored as an offset per track node, and offset driving uses parallel lines. Use track coordinates for strategy and world coordinates for close avoidance. |
| Fray, [Context Steering](https://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf) (Game AI Pro 2, ch. 18; used in F1 2011) | The same idea as heat vision: interest and danger maps across the track. The racing line is only a mild preference. Remember the previous choice to avoid flicking between options. |
| Jimenez, [The Pure Advantage](https://www.gamedeveloper.com/design/the-pure-advantage-advanced-racing-game-ai) (Black Rock, 2009) | Continuous skills from 0 to 1. Balance the race by adjusting skill toward target points, with a boost at the start that fades, and no AI improvement in the last 20% so the player can catch up. This is a later difficulty and balance layer, not part of the driving itself. |
| iRacing [AI roster attributes](https://www.iracing.com/ai-rosters/) | A commercial sim splits driver quality into separate skill (corner speed and line), aggression (spacing and willingness to overtake), optimism (braking and throttle confidence), smoothness (steering) and age (reaction time). This is close to our six abilities. |
| rFactor 2 AI settings ([discussion](https://www.overtake.gg/threads/what-dictates-ai-strength-in-rf2.157655/)) | Strength sets brake power, brake grip and corner grip usage. The AI limiter cycles drivers between optimal and weaker spells, which is the source of mistakes. |
| Sony AI, GT Sophy (in [doc 41](archive/racing-ai-v1/41-racing-ai-research-and-next-pace-pass.md)) | Test starts, corner battles and slipstream passes on purpose. Penalising all contact made drivers timid, and loose fault rules made them exploit gaps. Judge pace and etiquette together. |
| Gipps car-following model (traffic engineering) | A safe following speed that assumes the car ahead might brake hard: you follow closely at speed without lifting, and only brake when the gap truly runs out. |

## Design

One class, `RacerDriverPolicy`, plus a few small helpers. It plugs into the existing `IDriverPolicy` seam, so the car physics, checkpoints and recovery are shared with the first AI. It has been the default driver since study 205; `--first-ai` selects the first AI, which remains the benchmark.

```text
Persona (six abilities + personality)       per race
   ↓ scales everything below
Strategy: intent state machine              ~10 Hz, staggered per car
   Launch · Race · Attack · Defend · (Recover: existing PhysicalRecovery)
   ↓ intent, target car, side, reason
Tactics: heat line across the track         ~10 Hz
   racing line pull · cars (predicted) · draft cone · inside-of-corner · edges
   → target offset (rolled downhill from where the car is; cars alongside are walls)
   ↓
Speed: one arbitration, one logged reason   every tick
   limit line (near the real limit) · offset-corrected corner limits ahead ·
   Gipps following speed for the car in our lane · close-range guard
   ↓
Control: pure pursuit on the offset path + pedal feed-forward    every tick
```

### Limit line

Each car builds its own speed profile along the track's racing line. It uses:

- the fitted car's grip, power, drag, gearing and brakes;
- the driver's grip usage, which is near the physical limit (about 90–97% of the real cornering limit rather than 78%);
- the driver's braking usage (about 88–98% rather than about 64%).

Grade-aware tracks use the same `GradeModel` loads as the first AI. When a car drives at an offset from the line, the corner limit ahead uses the offset path's curvature, κ′ = κ / (1 − κ·d). The inside of a corner is tighter and the outside is wider, so a car passing round the outside really does have to go slower.

### Speed: one decision

The target speed is the lowest of:

1. **Limit line ahead:** the lowest `sqrt(v(s)² + 2·a_brake·s)` over the braking distance, using offset-corrected limits along the planned lateral path.
2. **Following:** the Gipps safe speed behind the car occupying our lane. It uses a reaction time, the leader's likely braking and a standstill gap, all scaled by Racecraft and Aggression. It isn't applied to a car we're pulling alongside, whose lane we've left.
3. **Close guard:** for bodies within a few metres, a time-to-contact check in world space. This is the only place hard braking comes from traffic.

Whichever of these binds is logged as the reason, so every lift has one explanation.

### Intent

| State | Enters when | Does | Leaves when |
|---|---|---|---|
| **Launch** | race start | Holds its grid column with strong heat. Gipps following behind the car ahead in the same column; reaction delay from Control and Consistency. | first corner reached, or 6 s passed and lanes are spread out |
| **Race** | default (utility 500) | Racing line with mild heat; follows at a close, safe distance; drafts on straights | another state's utility beats Race + hysteresis |
| **Attack** | closing on or following a car, with pace or a draft, and the next corner offers an inside line, or there's a straight with speed in hand | Picks a side (the inside of the next corner, or the draft exit), keeps that side and target, and uses narrower car heat (Aggression). With overlap on the inside, it may brake later (Braking × Aggression). | done: ahead by one car length plus a margin; aborted: gap closed on that side, no overlap by the braking point, or a timeout. After an abort there's a cool-down (Patience). |
| **Defend** | a faster car is close behind and not yet alongside, and we're outside a braking zone | One move to cover the inside of the next corner (Racecraft, Aggression) | the attacker is alongside (you must then leave a car's width), the move is done, or a braking zone arrives |
| **Recover** | the existing `PhysicalRecovery` takes over | unchanged | unchanged |

Etiquette, from the research and doc 43:

- A car with overlap on the inside at turn-in owns the apex, and the outside car leaves a car's width.
- A car alongside keeps its lane (in the heat line, a car alongside is a wall).
- A defender makes one move and never moves in the braking zone.

### The six abilities

Each ability runs from 0 to 1. Every ability has a real effect in the code, and none is a label only.

| Ability | Effect in v2 |
|---|---|
| **Braking** | Braking usage (0.88 → 0.98 of what the brakes and tyres allow) and brake-point accuracy (scatter of a few metres, seeded per corner). With Aggression, it also sets how much later it brakes when overtaking on the inside. |
| **Line** | Corner grip usage (0.90 → 0.97 of the cornering limit). Apex accuracy is a seeded lateral error per corner, up to about 0.8 m at 0. |
| **Control** | How quickly the throttle goes back on at the exit (pedal rate), steering smoothness, launch reaction, and how quickly the car settles after contact or a slide. |
| **Consistency** | How big and how often the "off spells" are, when grip and braking usage dip for a few seconds (square wave with a low duty cycle). Also rare small mistakes, like a late brake or running wide, and race-to-race form. |
| **Racecraft** | Judging overtakes: when an attack is worth it, choosing the inside of the next corner, using the draft, defending, reaction time and gap judgement in traffic (Gipps reaction time 0.45 → 0.2 s). |
| **Strategy** | Fuel mix, tyres and pit timing from class A. Unused, and not shown, until then. |

Personality: **Aggression** narrows car heat and standstill gaps, raises the utility of attacking and defending, and extends late braking. **Patience** lengthens the cool-down after an abort and the time spent following before a marginal attempt.

For now, the abilities come from the existing driver records:

- Braking: (braking − 0.55) / 0.2.
- Consistency: kept as it is.
- Aggression: risk / 10.
- Line, Control, Racecraft: derived from style.

This means the same roster races both AIs. Doc 45 F1 gives every racer an explicit radar later.

### What v2 does not do

- No parallel physical forecasts, worker pools or candidate ranking. The heat line plus a single speed decision replaces them. It costs a few hundred operations per car per tick, which should bring AI time from about 3 ms per tick to well under 0.5 ms.
- No change to the car physics, checkpoints, grid or recovery, so the comparison with the first AI is fair.

## How it's judged

The keep/reject protocol (`CLAUDE.md`) applies:

- the same build, `--racer-v2` off (first AI) versus on;
- Cedar and Bayview, seeds 1709, 1710 and 1711.

Keep it only if:

- all 24 cars finish;
- there are no new assisted resets;
- the total finish time of all 24 improves on average.

Also reported:

- time spent lifting for traffic (hesitation), and its reasons;
- attacks, completed passes and aborts;
- contacts and their severity;
- the start (the first 10 s);
- frame cost.

Richard watches the result as well: telemetry alone can't tell a good racer from a lucky one.

## Build order

1. **Solo.** Limit line, control and the speed decision. Target: a club car's solo lap faster than the first AI's, with no departures.
2. **Traffic.** Heat line, Gipps following, close guard. Target: 24 cars finish on both tracks.
3. **Intent.** Launch, Attack and Defend with etiquette. Target: fewer hesitations and more clean passes.
4. **Abilities.** Everything scaled by the six abilities; seeded spells and mistakes.
5. **Keep/reject** against the first AI; then adventure races switch to v2 when it wins.

## Progress log

29 September 2026. All runs are hidden 4× screens in `review/unity-35/frame-cost-201/`, club/street mixed roster, 24 cars, one lap. The first AI (`context-v1`) is the benchmark from the same build.

First AI benchmark (4× screen):

| | 1709 | 1710 | 1711 | Mean |
|---|---|---|---|---|
| Cedar | 2,115.9 | 2,110.4 | 2,118.7 | 2,115.0 |
| Bayview | 919.0 | 793.8 | 797.4 | 836.7 |

| Build | What changed | Cedar 1709 | Bayview 1709 | Notes |
|---|---|---|---|---|
| v2a | first field race | 3,092 | — | cars lifted instead of using full power below the limit; recovery treated the grid as blocked |
| v2b | full power below the limit; recovery trusts v2 in traffic | 2,470 | — | runs wide at the 21 m hairpin; 80% of attacks abort |
| v2c–d | corner grip 0.84–0.92; recovery at 10 m (v2 handles short excursions); slower-car lanes cost heat | 2,257 | 874 | edge margin 6.2 m |
| v2e–g | path curvature against the road; leader-acceleration anticipation; launch headway | 2,250 | 875 | the road-based curvature slowed on-line pace 1.8 s |
| v2h | exact line curvature + off-line difference; roll-downhill lane choice | 2,215 | 20/24 | Bayview failed: understeer off the road |
| v2j | **pure pursuit from the direction of travel** (the car slides a few degrees near the limit) | 2,192 | 829 | nothing leaves the road, even holding either edge for a lap |
| v2k–m | line committed through corners; attacks need a real speed advantage judged at the other car's position; servo-aware corner speed | 2,176 | 827 | Richard watched Bayview 1709: "a lot more racey", start better, nothing ran wide |
| v2n–p | shorter launch column hold; faster cars behind respected; coasting when following | 2,159–2,196 | 814–839 | v2o's sideways-drift prediction double-counted the line and was removed |
| v2q | closer following (reaction 0.12–0.25 s, standstill gap 0.6–1.3 m); creep round stationary cars; recovery at 13 m | 2,114.4 mean | 787.0 mean | all 24 finish, no resets, all six seeds; contacts 38–61 per Cedar race (first AI 6–9) |

Failures kept on record: Cedar 1710 with closer following, before the creep-round fix, finished 23/24 with 7 assisted resets. A car rejoining from the grass waited for clearance while the queue behind waited for it.

Open items: contact count; defending needs a calmer test to judge; the abilities' effect per driver (radar spread); frame cost.

### Formal keep/reject studies (normal speed, same build, flag off vs on)

Races are deterministic: hidden 4× screens and normal-speed runs of the same build give identical results, including when run in parallel (`tools/run-pace-parallel.ps1`, six at once, about 100 s for a track's six races). Frame-cost timing still needs one race at a time.

| Study | Change | Cedar total, 3 seeds (first AI 6,345.0) | Bayview total, 3 seeds (first AI 2,510.2) | Hard contacts per race (first AI 1–2) | Finish / resets |
|---|---|---|---|---|---|
| 203 | v2q | 6,343.1 | 2,360.9 | 4–21 | all 24/24, none |
| 204 | full side margin (≥ 0.45 m); earlier side nudge | 6,347.1 | 2,418.7 (a car stuck at a roadside object on Bayview 1709 for 70 s) | 1–7 | all 24/24, none |
| 205 | planned lines kept within 5.6 m of the centre | 6,378.5 | 2,357.8 | 1–7 | all 24/24, none |

Study 205 passes the protocol: all 24 finish, no new assisted resets, and total finish time improves by 118.9 s over six races (19.8 s per race). The gain is all Bayview. Cedar is 33.5 s behind, and 132 s of the Bayview gain is the first AI's poor seed 1709. Soft contacts remain higher than the first AI's (15–64 against 1–8).

**29 September: v2 is the default driver** (Richard), for adventure races and trials alike. `--first-ai` races the first AI as the benchmark; `--racer-v2` is still accepted and changes nothing.
