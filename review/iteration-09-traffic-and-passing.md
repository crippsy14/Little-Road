# Iteration 09 — traffic, braking and passing

17 September 2026. Previous goal turn classified as progress: it produced a Windows trial and evidence of traffic failures. This iteration diagnoses those failures and reruns controlled overtaking and two full fields. The complete adventure and visual goal remains active.

## Changes supported by evidence

Previous late finishers travelled roughly four circuit lengths for a three-lap race: leaving the road missed a gate and required another circuit. Gates now track absolute ordered distance. Recovery cannot advance them; it returns behind the lesser of the last safe position and pending gate. Missed gates are counted explicitly, and recovery waits if its destination is occupied.

AI braking look-ahead previously assumed nearly full brakes while requesting strong steering. It now reserves traction for turning using the same tyre budget as the controller, plus a speed-dependent reaction distance. Equipment power and grip are unchanged.

Impact traces then showed glancing contacts immediately preceding the largest departures. The shared player/AI controller now has angular damping 10, a physical angular-speed cap of 1.5 rad/s, and 12 position / 8 velocity solver iterations. Impacts still affect translation and speed; cars are not corrected onto the road. This reduces unwanted collision spin.

Controlled tests can start cars in single file, seven metres apart, with explicit opponent loadouts. Reports now include ordered-gate progress, timestamped impact entries and penetration during both initial and sustained contacts. Finishers' HUD placing follows finish order instead of their later circulating position.

## Final build results

These use actual Rigidbody motion. Concurrent independent processes tested outcomes, not GPU performance.

| Case | Result |
| --- | --- |
| Two cars, stock then club 7m behind, seed 1709 | Club passed by the 3.02s sample; club 28.433s, stock 38.100s; no contacts/resets/missed gates |
| Three cars, stock / street / club at 7m intervals, seed 1709 | All three expected passes observed in on-road samples; club 30.100s, street 33.900s, stock 38.166s; no resets/missed gates; two contact entries per upgraded car |
| 24 cars, three laps, seed 1709 | 24/24 finished; winner 84.003s, last/stock 109.309s; zero resets/missed gates; maximum sampled offset 6.04m |
| 24 cars, three laps, seed 1721 | 24/24 finished; winner 81.486s, last 108.209s; stock 23rd at 108.192s; zero resets/missed gates; maximum sampled offset 6.74m |

Road half-width is 8m. Neither final field contains an off-road centre-position sample at 1Hz, including finishers circulating until the last result. This is not continuous wheel-clearance proof or a guarantee across all seeds.

Outcomes varied without forced placing: street car 10 finished 9th then 17th; street car 13 finished 18th then 13th. Two seeds are an example, not enough to accept the full natural-variance specification. Seeds currently influence initial preferred line/pace as well as evolving pace variation; stable driver personalities remain a separate design choice. Stock's 24th/23rd finishes match the intended difficulty direction, but economy and progression balance remain untested. Opponents use fixed street/club equipment; bodies grant no hidden performance.

## Intermediate failures retained

The first build of this iteration (`field-1709.json`) finished all cars but needed four missed-gate recoveries and reached a sampled 16.12m offset. It proved the new recovery avoided the extra-lap penalty while showing that braking changes alone did not solve contact departures. Car 9's impacts at 35.03s and 35.37s preceded its large heading error. The final build needed no resets in either tested field.

The initial three-car case finished the club car at 30.966s; final contact response reduced that to 30.100s. This is a paired scenario observation, not a single-parameter causal experiment: angular damping, angular cap and solver iterations changed together.

## Collision gate still open

Final maximum collision-shape penetration: **0.06348m** (1709), **0.04904m** (1721), and 0.02755m in the three-car case. These solver contact separations do not directly measure rendered-mesh intersection. Conservative boxes include empty space, but these numbers do not prove no visible clipping. Earlier entry-only measurements were incomplete; sustained contacts are now included.

No-clipping and forgiving-crash timing gates remain open. Next checks need contact-focused captures, validated body hulls/compound shapes, a matched clean/deliberate-impact case and explicitly blocked recovery destinations. The intermediate run exercises recovery in traffic, not every safety case.

## Evidence and reproducibility

Files under `review/unity-02`:

- `build-01.log`, `build-02.log`: successful exports; `editor-checks-build-02.json`: 62 editor checks.
- `passing-two.json`, `passing-three.json`, `field-1709.json`: intermediate evidence.
- `damped-two.json`, `damped-three.json`: final controlled cases and `.validation.json` companions.
- `damped-field-1709.json`, `damped-field-1721.json`: final fields and `.validation.json` companions.
- `captures-damped-field-1709/01-grid.png`, `02-race.png`, `03-final.png`: final live-camera captures.

`tools/check-unity-passing.ps1` verifies each faster car started at least 5m behind, then led by more than 3m in an on-road physical sample before the slower car finished, retained its finish advantage, completed its lap and needed no reset. Passing is not inferred solely from finish order.

`tools/summarize-unity-field.ps1` checks completion, lap counts and sampled ordered-gate increments, then reports offsets, contacts, resets and penetration without treating those metrics as automatically acceptable. One-second samples cannot prove every fixed-step invariant.

Final gameplay assembly SHA256: `6D4A87B8A7EF4EC8E7E17336A86854978BE6F15328DCD7764221B4E0FF5C70DF`. Output remains `outputs/little-road-unity-windows`. Iteration 08's ZIP is preserved; this package is `outputs/little-road-unity-trial-02.zip`.

## Visual review and next milestone

Inspected final `02-race.png`: spread-out traffic and imported cars work, but coarse body surfaces, simple glazing, sphere-tree scenery and flat island remain far below both concepts. No visual gate passes. Next: the three improved original master bodies, starting with a rounded compact with deliberate topology, wheel arches, panel/glazing detail, better wheels/materials and reviewed collision geometry. Establish reusable modules/UV/export conventions from document 15, with distinct coupe and open-wheel silhouettes.

Do not substitute this trial for the connected adventure. World composition, garage/residents/delivery/collectibles, purchase/fitting/save migration, economy, audio, controller menus and real DualSense tests remain pending. No owned test/editor processes remained live at handoff.
