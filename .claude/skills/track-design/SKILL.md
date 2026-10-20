---
name: track-design
description: How to design or critique a race track or circuit for Little Road (any theme, car class or terrain). It covers what makes tracks good, the measured geometry of real circuits with target bands per track type and class, a step-by-step design method, the turtle piece-list build format, and tools/track-eval.py. Use it before laying out, changing or judging any circuit, lane race, hill climb, street race or rallycross track.
---

# Designing tracks for Little Road

Richard's rule: real tracks give direction, not 1:1 references. Never copy or name a real circuit's corners. Name every corner after the place it runs through (farm, church, brook, quarry, harbour).

The research behind this skill was done on 20 Oct 2026. The worked example is *Bramblewick Lanes* (docs/82): the report and the scripts were in that session's scratchpad, `track/expert-design.md`.

## 1. What makes a track great

| Principle | What it means in practice | Source |
|---|---|---|
| Long straight into a slow corner | Long braking zones are where passes happen. Place 2–4 per lap. | Tilke: [GrandPrix247](https://www.grandprix247.com/formula-1-news/tilke-we-cant-build-ten-red-bull-rings), [RaceFans](https://www.racefans.net/?p=36150) |
| The corner *before* the straight matters | A slow corner onto a straight spreads the cars out (the "spring effect"). A medium-fast one keeps the follower in the tow. One per lap usually fits. | [Sidepodcast](https://sidepodcast.com/post/tinkering-with-tilkes-circuits) |
| Downhill braking zones | The easiest way to add overtaking. Uphill braking is short and safe, which suits a dive. | [Sidepodcast](https://sidepodcast.com/post/sending-tilke-to-the-moon) |
| "Passages that provoke mistakes" | Decreasing-radius corners, off-camber exits, crests mid-corner, a kink on a bridge. Mistakes create passes. | Tilke (as above) |
| Variety, not a formula | Mix speeds and angles. Tracks whose corners are all medium speed, with every straight into a hairpin, get criticised. | [enterf1](https://www.enterf1.com/blog/157-adding-variety-to-circuit-design), [PlanetF1](https://www.planetf1.com/features/ranked-the-best-and-worst-f1-circuits-hermann-tilke) |
| Five rational metrics | Race line, apexes (clipping points), width, camber, height variation. | [McMillan, Game Developer](https://gamedeveloper.com/design/a-rational-approach-to-racing-game-track-design) |
| Design the racing line | Width changes a corner: a slow bend at 6 m is a fast sweep at 12 m. Use constant, double-apex, increasing- and decreasing-radius corners as separate tools. | [TKart](https://tkart.it/en/magazine/expert-advice/design-perfect-karting-track) |
| Elevation is character | Cadwell Park in flat Lincolnshire: blind crests (the Mountain), an off-camber descending corkscrew after the main braking zone (the Gooseneck), about 60 m of climb. | [Devitt](https://www.devittinsurance.com/guides/circuits/cadwell-park/), [MotorbikesToday](https://www.motorbikestoday.com/features/Articles/cadwell_track_guide.htm) |
| Width | FIA Grade 1 circuits are at least 12 m wide with straights of 2 km or less. Club circuits run at about 9–12 m. Narrow lanes make braking points and lines critical. | [Motorsport.com](https://www.motorsport.com/f1/news/fia-track-grades-requirements-f1-potential/6508330), [GTPlanet](https://www.gtplanet.net/forum/threads/how-to-design-a-track.221303) |

Every lap needs, at minimum:
- 2–4 overtaking zones (at least one downhill);
- one or two rhythm sections (alternating esses);
- one commitment corner (fast, over a crest or blind);
- one or two mistake generators;
- a vista;
- an escape road straight on at every heavy braking zone.

## 2. Metrics and how to measure them

The numbers below were computed from surveyed centrelines:
- the [TUMFTM racetrack-database](https://github.com/TUMFTM/racetrack-database);
- OpenStreetMap `highway=raceway` ways, via Overpass (© OSM contributors, ODbL).

Method:
- Resample to 2 m and Gaussian-smooth at σ = 10 m. Results move only about ±5% between σ 5 and σ 20.
- **Curved** means a radius under 300 m.
- **A corner** is a run of curvature with one sign deflecting at least 15°. Merge runs less than 30 m apart.
- **Corner radius** is the tightest radius in the corner.

The speeds come from a class C point mass: 0.95 g, 42 m/s top speed, drive 2.8·(1 − v/42) m/s², brakes 7.5 m/s².

| Metric | How to measure |
|---|---|
| Corners per km | corner count ÷ lap km |
| Total turning | Σ\|Δheading\| over the lap, per lap and per km (total absolute curvature) |
| Macro turning | the same sum after σ = 40 m smoothing; ignores wiggles and shows the big shape |
| Curved share | length with R < 300 m ÷ lap length |
| Angle histogram | each corner's total deflection, binned <30, 30–60, 60–100, 100–150, >150° |
| Radius histogram | each corner's tightest radius, binned <30, 30–60, 60–120, 120–250 m |
| Left/right balance | the minority hand's share of all corners |
| Direction changes | consecutive corners of opposite hand; compare with the corner count |
| Longest straight | longest run with R > 300 m, as a share of the lap; also 2nd longest ÷ longest |
| Hull ratio | lap length ÷ convex-hull perimeter. About 1.0 is a plain loop; 1.3 or more folds into its land |
| Speed spread | slowest apex ÷ top speed; mean lap speed; apex mix slow (<15 m/s), medium, fast (25–33), flat (>33) for class C |
| Heavy braking zones | corners where ≥ 12 m/s is scrubbed from the best speed in the 250 m before |

### Reference numbers (measured unless marked)

| Circuit | km | corners /km | turn °/km | macro | curved | hull | angles <30/-60/-100/-150/>150 | longest share | C min v |
|---|---|---|---|---|---|---|---|---|---|
| Cadwell Park | 3.46 | 4.9 | 390 | 279 | .47 | 1.30 | 0/6/6/5/0 | .18 | 10.2 |
| Croft | 3.26 | 3.7 | 345 | 294 | .36 | 1.29 | 1/4/3/2/2 | .17 | 11.0 |
| Oulton Park Intl | 4.28 | 3.7 | 274 | 216 | .37 | 1.18 | 5/2/6/2/1 | .10 | 13.3 |
| Mallory Park | 2.06 | 5.8 | 475 | 278 | .52 | 1.06 | 2/3/5/1/1 | .19 | 8.4 |
| Lime Rock | 2.39 | 3.3 | 320 | 259 | .43 | 1.06 | 1/1/3/2/1 | .25 | 12.6 |
| Thruxton (fast) | 3.74 | 3.2 | 249 | 189 | .40 | 1.03 | 2/4/6/0/0 | .14 | 12.2 |
| Goodwood (fast) | 3.80 | 2.9 | 176 | 158 | .31 | 1.02 | 4/5/1/1/0 | .12 | 18.4 |
| Monza (fast) | 5.77 | 1.7 | 165 | 115 | .19 | 1.04 | 0/4/5/0/1 | .17 | 11.1 |
| Suzuka | 5.78 | 3.3 | 292 | 250 | .43 | 1.24 | 1/7/7/1/3 | .15 | 12.5 |
| Spa | 6.97 | 2.6 | 240 | 200 | .29 | 1.29 | 2/2/11/2/1 | .11 | 11.5 |
| Brands Hatch GP | 3.89 | 2.8 | 272 | 264 | .38 | 1.46 | 2/2/4/2/1 | .11 | 15.1 |
| Zandvoort | 4.29 | 3.3 | 376 | 329 | .47 | 1.53 | 2/2/3/2/5 | .08 | 12.1 |
| Norisring (street) | 2.27 | 2.2 | 292 | 245 | .21 | 1.03 | 0/1/2/0/2 | .19 | 9.1 |

- **Club median (7 circuits):** 3.7 corners/km, 320 °/km, macro 259, curved .40, hull 1.06, direction changes 0.67 per corner, minority hand 41%, 2nd longest straight 0.57 × longest, class C min 12.2 m/s (0.31 of top), mean 27.1 m/s.
- **GP/international median (22):** 3.1 corners/km, 311 °/km, macro 258, curved .36, hull 1.31, direction changes 0.63 per corner, 2nd longest 0.73 × longest.
- **Not measured, published counts only:** Monaco has 19 corners in 3.34 km (5.7/km). Pikes Peak has 156 turns in 20 km (7.8/km).
- **Class C insight:** a weak car takes R ≥ 110 m corners flat unless they follow about 250 m or more of straight or a downhill. **A commitment corner needs a run-up.**

### Target bands by track type (class C; whole lap)

| Metric | Flowing country circuit (~3 km) | Tight street/town (1.5–3 km) | Fast open circuit (3–5 km) | Hill climb / point-to-point | Rallycross (0.8–1.4 km, mixed) |
|---|---|---|---|---|---|
| Corners /km | 4.0–5.5 | 5–8 | 1.7–3.2 | 5–10 (Pikes Peak 7.8) | 5–8 * |
| Turning °/km | 330–430 | 400–650 * | 150–260 | 450–900 * | 500–800 * |
| Macro °/km | 250–330 | 300–450 * | 110–200 | — | — |
| Curved share | .40–.52 | .25–.40 (short junction arcs) | .19–.40 | .45–.65 * | .45–.60 * |
| Hull ratio | 1.15–1.45 | 1.05–1.40 | 1.0–1.35 | — (net climb instead: 5–10% average gradient *) | 1.1–1.4 * |
| Angle mix | spread; ≤1 (max 2) over 150° | mostly 60–100° (junctions); 1–3 hairpins | mostly 30–100°; 0–1 over 150° | hairpins 1–3 per km on the steep parts | 2–3 slow (100–180°), 1 fast kink, 1 jump |
| Radius mix | <30: 2–4; 30–60: 4–7; 60–120: 3–6; 120–250: 2–4 | <30 m: 40–60% of corners | 60–250 m dominate; ≤2 under 30 m | 15–60 m dominate | 15–40 m dominate |
| Minority hand | ≥ 35% | ≥ 30% | ≥ 25% | any | ≥ 30% |
| Direction changes | ≥ 0.6 × corners | ≥ 0.5 × | ≥ 0.4 × | ≥ 0.6 × | ≥ 0.5 × |
| Longest straight | .12–.20 of lap; 2nd ≤ 0.65× longest and ≥ 250 m | .10–.20 | .15–.25 | ≤ .15 | start straight .10–.20 |
| Heavy braking zones | 4–6 | 6–10 | 3–5 | many (but time trial) | 3–4 |
| Class C slowest / ratio | 12–15 m/s, .28–.40 | 8–12 m/s, .20–.30 | 15–19 m/s, .35–.46 | 7–12 m/s | 8–12 m/s |
| Width | 7.5–9 m lanes, 10–12 m in braking zones | 9–12 m, kerbed | 12–16 m | 6–8 m | 12–16 m, with a joker lap section |

\* = estimate. These bands were not measured from real track data; check them when you can.

### Scaling with car class
Pick each radius from the corner speed you want: **R = v² / (μ g)**. Faster classes need bigger radii and longer straights for the same feel, so a C track feels "tight" to an S car.

The class numbers below are assumptions. Check them against the game's car data before relying on them.

| Class | Assumed top speed / grip | Flat-out radius ≈ v²/μg | Radius scale vs C | Straight scale vs C |
|---|---|---|---|---|
| C road cars | 42 m/s, 0.95 g | ~190 m | ×1 (hairpin 15–25, medium 60–120, fast 120–250) | ×1 |
| B tuners | ~50 m/s, 1.05 g | ~240 m | ×1.25 | ×1.2 |
| A touring | ~58 m/s, 1.15 g | ~300 m | ×1.5 | ×1.4 |
| S race cars | ~68 m/s, 1.4 g (aero) | ~340 m | ×1.7–1.8 | ×1.6 |
| E prototypes | ~80 m/s, 1.8 g (aero) | ~360 m | ×1.9–2.0 | ×1.9 |

Hairpins stay hairpins (R 15–30) in every class. They are about braking, not grip, so scale the radii of the medium and fast corners only. Downforce makes fast corners relatively easier for S/E cars: give them more 120–300 m corners, not more hairpins.

## 3. Design method

1. **Read the land first.** Mark the fixed pieces (start/finish, junctions, keep-out zones) and the land's features:
   - crests and the high rim;
   - dips, water (brook, river, harbour), cuttings;
   - landmarks to aim at (church tower, lighthouse, quarry face);
   - where spectators and the village sit.
   Note the slope direction: downhill is where braking zones go.
2. **Place the overtaking zones** (2–4). The main straight goes into the slowest corner. A second straight of 250–350 m, ideally **downhill**, goes into a hairpin or slow corner. Then one or two shorter dives.
   - Make the corner *before* each straight medium-fast, so cars stay close.
   - Put an escape road straight on at each one.
3. **Add the rhythm sections:** alternating corners of 30–60° at R 50–130, with 20–40 m between them. Uphill rhythm reads well.
4. **Add the commitment corners and mistake generators:**
   - a fast corner over a crest, after a run-up;
   - a decreasing-radius corner (e.g. `A 55 90; A 70 45`);
   - an off-camber exit toward water or a drop;
   - a kink on a bridge or crest;
   - a double apex.
5. **Fold the lap into the land.** Bring at least one leg back into the interior: a trombone into a hairpin, or a return leg 40–60 m from its outbound leg, which also gives the vista and spectator view. Aim for a hull ratio of 1.15 or more.
6. **Set the elevation and vistas.** Carve dips (−2 to −4 m) for compressions, raise crests (+1.5 to +2.5 m) at commitment corners and before blind turn-ins, and steepen downhill braking zones to 2–3%. Put a vista on the slowest-to-read straight.
7. **Iterate on the numbers** (§4) until every metric is in its band. Then critique against §1 in words: name each corner's purpose. If a corner has no purpose, cut it or change it.

### Avoiding "straights joined by curves"
- **Turning per km can look fine while the shape is a box.** Two 180° hairpins add 360°, and ±30° wiggles add turning without shape. Check **macro turning** and the **hull ratio**, and look at the plan picture.
- **Never two near-equal long straights:** keep the 2nd ≤ 0.65 × the longest.
- **No more than one hairpin over 150°,** unless it's a hill climb or street track. A hairpin pair makes a queue, not a pass.
- **At least 40% of the lap curved,** with at least 2 corners of R 120–250 and at least 1 flat-out commitment corner.

## 4. Build format and evaluation

Tracks are built from a **turtle piece list**. It starts at a fixed point and heading in the local frame.
- `a` is east; `n` is north; heading 0° is east and 90° is north.
- Pieces go one per line or `;`-separated:
  - `S 40` is a 40 m straight;
  - `A 28 70` is a 28° arc of radius 70 m (**+ = left/anticlockwise, − = right**);
  - `M Name` is a named marker.
- **Two straights are written `S L1` and `S L2`.** The tool solves their lengths so the track ends exactly at the target point.
  - Make them non-parallel, ideally 70–110° apart in heading; near-parallel makes the solve explode.
  - If a solved length comes out negative or tiny, the rest of the layout overshoots. Shrink or redirect the pieces after them; don't fight the solver.

Evaluate with Blender's Python (the only Python on the Windows side):
```
"C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" tools/track-eval.py pieces.txt --name mine \
    --start 300,0 --heading 90 --target -260,0 --street 560 [--village a0,a1,nmax] [--land amin,amax,nmin,nmax] \
    --out review/tmp/mine.json
```
- **Inputs:** `--street` is the fixed start/finish straight added to the lap; use 0 for a closed loop built entirely from pieces. `--village` and `--land` set the keep-outs. The defaults are Bramblewick's frame.
- **What it does:** solves L1/L2, then checks closure, the arrival heading, the land bounds, the village keep-out and self-clearance (it warns under 20 m; aim for 40 m or more across a hedge).
- **What it prints:** corners, total turning and per km, curved share, the angle and radius histograms, direction changes, L/R, the longest straight, and a class C speed profile (slowest, fastest, estimated lap time).
- **Output file:** `--out` writes the points (`pts`), markers and results. Always pass `--out review/tmp/...`, or it writes `<name>.json` in the current folder.

**How `track-eval` differs from §2:**
- It counts each piece-built same-hand group as one corner.
- It adds the two junction turns as R20 corners.
- Its curved share counts arc pieces only, so it reads about 0–0.06 lower.
- It doesn't compute macro turning or the hull ratio. Get them from the `--out` points:
```python
import json,math;p=[tuple(q) for q in json.load(open('review/tmp/mine.json'))['pts']]  # lane only; add the street for a full lap
def hull(P):
    P=sorted(set(P));c=lambda o,a,b:(a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0]);L=[];U=[]
    for q in P:
        while len(L)>1 and c(L[-2],L[-1],q)<=0:L.pop()
        L.append(q)
    for q in P[::-1]:
        while len(U)>1 and c(U[-2],U[-1],q)<=0:U.pop()
        U.append(q)
    h=L[:-1]+U[:-1];return sum(math.dist(h[i],h[i-1]) for i in range(len(h)))
lap=p+[(x,0.0) for x in range(-260,300,2)]   # Bramblewick: the street from W to E
print('hull ratio',sum(math.dist(lap[i],lap[i-1]) for i in range(len(lap)))/hull(lap))
```
For macro turning, smooth the points (a moving average over ±40 m) and sum |Δheading|.

**Loop:** edit the pieces, run the tool, and plot the points (any quick PNG; look at it every round). Adjust until all metrics sit in the band for the track type and class, the solved L1/L2 are sensible, and the issues list is empty.

Then write the design up with:
- a corner table: name, deflection, radius, purpose, approach/apex/exit speeds, camber, elevation;
- the piece list with the solved L1/L2;
- an elevation plan (where to carve or raise, in metres);
- the width plan (per-section widths need code support; `Circuit` has one width per circuit as of 20 Oct);
- the metrics against the bands;
- a self-critique.

Then build behind a flag and race it (CLAUDE.md keep/reject).

## 5. Checklist and common failure modes

Checklist:
- [ ] Every corner has a named purpose and a place-name.
- [ ] 2–4 overtaking zones; at least one downhill; a medium-fast corner before the main straight where possible.
- [ ] At least one rhythm section, one commitment corner (with a run-up), and one or two mistake generators.
- [ ] Metrics in band: corners/km, turning and macro turning, curved share, hull ratio, histograms, L/R, direction changes, straight shares.
- [ ] Elevation used: a crest, a dip or compression, a downhill braking zone, a vista.
- [ ] Escape roads or run-off at every heavy braking zone; verges inside fast corners.
- [ ] No self-crossing (unless a bridge is intended and stated); legs at least 40 m apart or hedged.
- [ ] Width chosen per section and stated.
- [ ] The AI risks are listed for the AI work, not designed around. Design the best track; improve the AI.

Failure modes:
- **One big sweep, or a loop round the field.** Hull ratio about 1.0–1.1; the interior is unused. Fold a leg inward.
- **All corners the same radius.** Everything at R 45–75 gives a monotone medium-speed lap. Spread the radius histogram.
- **No slow corner after the longest straight.** Then there's no braking zone and no passing.
- **Too many hairpins.** Hairpin pairs and zigzags inflate turning per km, queue the cars and undo passes. Keep to one (two at most).
- **Everything flat.** No crests, dips or downhill braking, so there's no character. Carve and raise.
- **Two equal long straights,** or a second straight longer than the main one.
- **Wiggles counted as flow.** Ten ±20° kinks are not a rhythm section. Check macro turning.
- **Fast corners with no run-up.** A class C car never reaches the speed that makes them fast.
