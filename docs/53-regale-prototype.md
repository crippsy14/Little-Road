# 53 — The Regale: Duke Rolo's prototype

On 1 October 2026 Richard asked:

> Could we create a super detailed prototype model with a really detailed livery. go all out on it and make it something you're proud of. We can then make that model the owner of the castle and a teaser as to the type of cars in the later game before the player gets there.

The **Regale** is a toy-scale Le Mans prototype in Duke Rolo's purple and gold. It is parked in the castle courtyard as Duke Rolo himself, and the Duke now mentions it when you talk to him: "Cars like her race in the top classes. Climb the ladder and you'll meet them."

![Regale v5, low front three-quarter](../review/cars/regale5-4/regale-ref1-front-low.png)
![In the courtyard](../review/map-sweep/duke-3/duke-front.png)

## The model (`source-art/build_prototype.py`)

**Run it** with `blender --background --python source-art/build_prototype.py -- [--render DIR] [--no-export] [--quick]`. It writes:
- `source-art/family/regale.blend`;
- `Resources/Cars/regale.fbx` and its two LODs (149k, 73k and 36k triangles);
- `regale_livery.png`.

**The body is one sculpture**, not a loft. It is a signed-distance field (numpy) made of:
- a low tub and sidepods;
- a teardrop canopy and a spine to the tail;
- a raised keel between the front pods;
- four fender pods: rounded boxes swept along the car, their tops falling to the nose and kicking up at the tail.

These are joined with a polynomial smooth minimum, so the blends are fillets. Wheel wells (with a soft rolled lip), the floor and the sidepod intakes are carved out the same way.

**From field to mesh.**
1. The field is polygonised with surface nets on a 1.5 cm grid.
2. Each vertex is moved onto the exact surface with three Newton steps.
3. The mesh is reduced to 22% (collapse).
4. The windows are then cut by planes. Doing this after the reduction keeps the screen, the teardrop side lights and their carbon seals straight.

**Parts.**

| Area | Parts |
|---|---|
| Front | Carbon splitter and dive planes; sunk LED headlamp blades with amber corners and running-light blades; louvres over the front wheels |
| Top | Mirrors on stalks; roof scoop, camera pod and antenna; windscreen wiper |
| Rear | Swan-neck rear wing (cambered main plane, flap, gold Gurney, liveried endplates); shark fin; one swept tail-light blade on a carbon strip; rain light; reversing lamps; twin tailpipes; five-strake diffuser |
| Fittings | Quick-fill caps; red tow loops |

**Wheels.**
- Low-profile slicks, wider at the rear.
- Ten-spoke forged gold rims with a centre lock.
- Drilled carbon-ceramic discs.
- Red calipers on the uprights.

The wheel radii are 0.41 m and 0.43 m: a 0.45 m rear wheel spun past the build's 1.05 m wheel-bounds check.

## The livery (`source-art/regale_livery.py`)

**How it's drawn.** The livery is flat artwork, drawn as filled 2D curves and text in a scene of its own, using the game's fonts (Lilita One and Nunito). It is rendered with Workbench at 2048 px.

**Layout.** One texture holds three views at 0.21 UV per metre (about 2.3 mm a pixel):
- the right side;
- the left side (laid out so its lettering reads the right way too);
- the top.

It also has a carbon patch for the floor.

**Mapping.** Each face of the `LiveryMap` material takes the view it faces:
- **Sides** when |n.x| > 0.35. With a stricter threshold, the sash broke on the sidepods' shoulders.
- **Top view, always**, for the keel and the spine; an exception keeps thin upright plates like the fin on the side views. Their flanks used to tear the stripes into steps.
- **Top view** for walls that face in towards the centreline. They used to show the far side's art.
- **The carbon patch** for the floor only.

**Grading.** The finished PNG is graded with `WorldLook.Surface`'s own numbers, so it sits within the world's colour limits.

**The design.**
- **Base.** Royal purple, deepening to the sills.
- **Sash.** A gold sash with cream pinstripes, pointed at both ends, rising from the nose to the canopy flank.
- **Behind the rear wheels.** Three gold kicks.
- **Top.** Twin gold stripes nose to tail, running down over the nose and the tail.
- **Crest.** Hillcrest Castle on a shield, under a crown, on the keel.
- **Numbers and names.** Number 1 in a cream roundel, with "DUKE ROLO" and a crown on the door. REGALE on the fin and the engine cover; SCUDERIA ROLO on the endplates.
- **Halftone.** Fades on the sidepods and the rear pod tops.
- **Shut lines and fasteners.** Door and access panels.
- **Safety markings.** A rescue arrow, a high-voltage triangle (it's a hybrid) and a tow arrow.

**Sponsors.** All are Hillcrest and Bayview folk:

| Sponsor | Where |
|---|---|
| GINA GELATO | Sill |
| BERT'S RAILWAY ("steam since 1923") | Tail flank |
| OLIVE GROVES, PIP'S POSTCARDS, VALLEY RACEWAY, CASTLE TOURS | Contingency stickers |

## In the game

**Materials (`TrialWorld.CarVisual`).** The runtime gives car materials flat colours by name. The Regale adds five:
- `LiveryMap`: white, with `Cars/<body>_livery` as its base map;
- `CarbonFibre`;
- `GoldTrim`;
- `CaliperRed`;
- `CanopyGlass`: smoked and opaque. The canopy is a solid shell, so clear glass would show nothing inside.

The older `Livery` (red) rule matched "LiveryMap" first and tinted the texture red. The new check caught it, and the rule now excludes it.

**The body (`CarBodies`).**
- `CarBodies.Prototype = "regale"` is in a new `Teasers` list: known, but never sold or raced.
- It has its measured shoulder width (2.30 m) and length (4.53 m).
- Duke Rolo's resident body is now the Regale (it was the Zenith GT).

**Checks.**
- `Editor/PrototypeChecks.cs` checks that:
  - the model and its 2048 px livery load;
  - the livery material carries the texture, untinted;
  - the carbon, gold, caliper and canopy finishes have their own colours;
  - every liveried vertex maps inside the texture (296k corners);
  - sampled livery colours sit within WorldLook's limits.
- MigrationBuild's import checks now cover the teaser bodies: metre scale, four wheel pivots, wheels on their axles.

**Capture.** `capture-map.ps1 -Name duke-N -Rendered -Flags @('--duke-review')` shoots the Duke's car from four sides at the castle.

## Version 2: a real Le Mans shape (1 October, evening)

Richard on version 1: it "looked a bit chunky and not that kind of le mans aesthetic". He sent photos of a current hydrogen Le Mans prototype concept to work towards, asking for "as close to 1 - 2 - 1 as we can get".

**Proportions.** The reference's proportions are scaled into the game: about 5.1 m long, 2.0 m wide and 1.06 m tall, with 0.72 m wheels on a 3.15 m wheelbase.

| | Version 1 | Version 2 |
|---|---|---|
| Length | 4.53 m | 4.95 m (with splitter, wing and diffuser) |
| Width | 2.30 m | 2.02 m |
| Canopy top | 1.18 m | 0.99 m |
| Wheel radius | 0.41 / 0.43 m | 0.345 m |
| Wheelbase | 2.48 m | 2.93 m |
| Overhangs | short | long front, short tail |

**Shape** (`body_sdf`):
- **Nose.** A wide, nearly flat deck from a sharp lip at the nose back into the canopy's base, with low fender shoulders. Under the lip, open intakes with a dark back wall, divided by slim cream blades.
- **Sides.** A door shoulder carries the fender line back over a deep black cavity behind each front wheel, where the arch air leaves. Low sidepods rise towards the rear wheels.
- **Cockpit.** A narrow teardrop canopy, mostly glass.
- **Tail.** A full-width tail with an open diffuser cave below and the rear tyres open from behind.

**Parts.**

| Area | Parts |
|---|---|
| Front | Thin LED line along the nose lip; slit headlamps with amber corners |
| Top | Vents in the fender tops; mirrors inboard by the canopy; roof snorkel and camera |
| Rear | Tall shark fin to the wing; wing at roof height on swan-neck pylons with tall endplates; full-width light bar; six-strake diffuser; orange tow strap |
| Wheels | Dark twelve-spoke rims with gold lips and centre locks; unbranded cream sidewall stripes |

**Livery** (`regale_livery.py`, version 2). This is in the reference's manner, but in Duke Rolo's colours and marks only; nothing of the real car's makers or sponsors.
- A near-black body with a purple nose band, edged in gold and raked back.
- Thin angular gold and cream linework along the door and over the rear wheel.
- A big italic "01" in purple, outlined in cream, inside a slanted cream frame on the sidepod.
- A gold endplate reading SCUDERIA ROLO, and the castle-and-crown badge at the top of the fin.
- The black cavity, and the sponsors on small stickers.

**Mapping.** The UV scale is now 0.20 per metre, so the longer car fits the texture.

**The length cap.** At 4.95 m the car is past the import check's 4.8 m cap for long bodies. Teaser bodies now have a cap of 5.2 m of their own: they are true to type, and not raced yet.

**Comparing with the reference.** `build_prototype.py -- --render DIR` renders the reference photos' angles: `ref1-front-low`, `ref2-front-high`, `ref3-head-on`, `ref4-side`, `ref5-rear`. `--body-only` renders the sculpture alone, in white, for shape passes. Version 1 was replaced in place (it was never committed).

## Versions 3 to 5: from 4/10 to 9/10 (1 October, night)

Richard scored each round against his reference photos; he sent renders only, with nothing published to the game between rounds. His notes, and the changes made for each:

| Version | Richard's notes | What changed |
|---|---|---|
| v3 (4 → 6) | "the front looks far too front heavy beyond the front wheels"; the canopy, the headlamps, the fin and the gloss (from my own list) | Proportions measured off his side photo, using the wheel's diameter as a ruler: wheelbase 3.43 m (70% of the length), 0.75 m overhangs, roof 1.10 m. A taller, narrower canopy set further forward, headlamp blades on the fender corners, the fin rising out of the roof, a deeper clear coat. Renders in a dark studio with strip lights, as in his photos |
| v4 | the top "flat front to back... the reference has a lot of overlapping body pieces with ups and downs"; "a lot more angular"; the rear should be "hollow, with the brake lights going across on a light frame" | Extra reference photos from Wikimedia Commons: the H2 concept beside a GR010, GR010 track shots and a Peugeot 9X8 from behind. The body is now separate pieces with 2-3 cm blends instead of 6-9 cm: a raised nose plank and sunken channels, flat-topped fenders, a cockpit tub with a brow round the canopy, a spine over a lower rear deck, rear fenders cut off square. The rear is open: a light frame, with the gearbox block and diffuser visible inside |
| v5 (9) | the front "looks like a box with a hole in it"; get "the drivers seating area shape nailed" | See below |

**The v5 front** (`body_sdf`, `RECESSES`, `aero`):
- **Nose panel:** domed side to side, falling to a sharp lip with an LED line, overhanging a deep mouth. Inside the mouth: two cream blades in a V, a centre pillar and a vane.
- **Channels:** the panel narrows towards the cockpit, so channels open beside it. Head-on the front now steps through three levels: fender tops, nose panel, channel floors.
- **Fenders:**
  - crowned tops with crisp shoulders, and raked fronts;
  - a headlamp pocket across each top with two LED blades, wrapping back along the shoulder;
  - a dark corner intake below it, framed by a cream fence;
  - two dive planes a side, and a slatted vent cut into the top over each wheel.
- **Arch lips:** carbon lips round every arch.

The pockets are boxes carved out of the body. Their edges are cut exactly with plane bisects, so the dark interiors have straight edges; colouring by whole faces left them ragged.

**The v5 cockpit** (`canopy_sdf`): head-on, a rounded trapezoid that narrows to a flatter roof; v3's ellipsoid was an egg. From the side it keeps the line Richard called perfect. It stands on a raised brow.

**Other v5 changes:**
- **Wheels:** deep-dish rims with ten forked spokes from a sunken centre-lock hub out to a gold lip, with the disc and caliper visible between them. The rim barrel had to become an open tube, because a capped cylinder hid everything inside it.
- **Light frame:** a flat 7 cm blade instead of a round tube.
- **The arch crinkle, fixed:** the window planes were cutting across the whole car and leaving thin slivers on the fender tops. They now cut only around the canopy.

**Size:** 5.01 m by 2.03 m, 162k triangles (LODs 82k and 42k).

**Still open:** straighter, thinner spokes; more cut lines on the fender fronts below the headlamps; the livery smears a little on upright front faces (they take the top view); and the canopy glass is so dark the screen's shape barely shows.

**Reference for future cars:** the method is reusable.
1. Measure proportions off photos, using the wheel's diameter as the ruler.
2. Build the body as signed-distance pieces blended with small fillets, so it layers rather than one smooth blob.
3. Carve dark pockets with `RECESSES` and cut their edges exactly.
4. Project the livery from side and top views.
5. Render from the reference photos' own angles, plus close-ups.

## Next, if wanted

- A Class S or E line-up built the same way: the SDF body makes new shapes cheap.
- The Regale in the body shop as a locked teaser ("Class S").
- Headlamps and the tail blade checked at night.
