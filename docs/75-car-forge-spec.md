# 75 · The Car Forge: a new car generator and livery system (spec, 14 October 2026)

**Status: spec for Richard's approval. Nothing here is built yet.**

Richard, 14 October:

> "We should think extreme here, not just body shape, but how do we create something whereby complex shapes (for each
> part of the body individually), that all connect seamlessly, that can also adhere to complex vinyl decals and liveries
> (and not breaking the car roll etc). It's a big job but ultimately one that makes or breaks the game as a whole as they
> are, after all, the main attraction to it."

**Sources:**
- `docs/research/75a-car-catalogue.md`: about 250 reference cars across the five classes, the toy-design principles and the legal notes.
- `docs/research/75b-car-modelling-methods.md`: the modelling and livery techniques compared, budgets, QC and about 45 references.
- The current pipeline map (13 October agent report, summarised in §2).

## 1 · What we want

### 1.1 The classes in the finished game

| Class | Look (Richard, 14 Oct) | Races in the finished game | Body forms needed |
|---|---|---|---|
| **C** | Family cars and genuine "bangers" that look slow. Faster-looking cars only in the fastest C races (8★–10★, today the Rally saloon and Zenith GT). | 21 races and a 10★ finale, from 1★ to 10★. Road, village courses, gravel, a hill climb, ice and street. | About 40: bangers for the low stars, family cars through the middle, warm cars at the top, plus off-roaders |
| **B** | As many unique forms as possible, from scratch. Not only 90s Japanese tuners. | 26 races (1★–8★) and the 10★ Tuner Championship | About 45 new forms, plus every C car's B kit (`<id>_tuner`, 30 so far) |
| **A** | Fast sports cars and road-based supercars, with complex shapes | 29 races, including the Grand Tour | About 30 |
| **S** | Race cars | 26 races | About 30, across disciplines |
| **E** | Purely Le Mans-style cars, across all the real Le Mans grades | 10 races, plus 12 after the credits | About 25, across the categories |

That is about 170 new or rebuilt bodies, plus kits. A tool that needs one to two hours of hand iteration per car (today's
builder, doc 66) can't make that many with real variety. The generator itself has to carry the variety.

### 1.2 Why today's cars converge

The doc 66 review, the pipeline map and the class B line-up (14 Oct) agree on the causes:

- **One shell.** Every car is one lofted superellipse from nose to tail. Panels can't have their own forms: a bonnet,
  a wing and a door are all slices of the same tube. Phase 1 and 2 (per-station exponents, fender pods, a separate
  cabin) loosened this, but the 27 Tuners still read as one family.
- **Booleans for openings.** Booleans for arches and lamps are fragile: workarounds such as gap constants and uncut
  round lamps limit what shapes are possible.
- **Closed shells only.** No open cockpit, open wheels, canopy, floating roof, flying buttress or longtail. Doc 65 says
  so, and the Regale had to be sculpted separately as an SDF.
- **No livery system.** The family cars have no UVs at all. The Regale's livery is hand-wired to that one car and smears
  on upright faces.
- **The 90s Japanese tuner brief** (Richard's own note) narrowed class B to one design language.

### 1.3 The bars it must clear

The Forge is judged on these, measured rather than eyeballed (docs/73 §3):

1. **Distinct.** Every car in a class differs from its nearest neighbour by a silhouette distance above a threshold in at
   least two of the side, rear and top views (§5.2).
   - The threshold is calibrated so that today's Tuners Richard called "too similar" fail.
   - A 64 px black side and rear view of every car in a class can be told apart.
2. **Seamless.** Every panel joins its neighbours with no gaps, kinks or z-fighting, unless the gap is a designed shut
   line.
   - Shading is continuous across smooth joins (QC: normals within 2° across seams, plus a reflection-line render).
   - Character lines are crisp where the recipe asks for them.
3. **Livery-ready.** One livery, authored once, fits every car in a class.
   - Stripes stay straight and even, with no stretch over 2:1 on painted panels.
   - Numbers stay crisp.
   - Nothing goes into arches, glass or shut lines.
   - It is identical at every LOD and under body roll and pitch.
4. **Toy look.** The look stays "toy shapes, real light" (docs/art-direction.md):
   - the shared bevel sizes;
   - the caricature proportions (bigger wheels, crushed roofs, short overhangs: §3.4);
   - colours through `WorldLook`.
5. **In budget.** Triangles, materials and draw calls per car are within §6. The drive benchmark budgets in docs/73 still
   hold with a 24-car field.
6. **Compatible.** It keeps the runtime contract (§2.2), so a Forge car drops into races, the body shop, the paint shop,
   BodyMotion, the lights and the collision box with no special cases.
7. **Original.** No real maker's name, badge or protected outline.
   - Each design changes at least three of nose, greenhouse, tail, arches and lamps from any one real car (the
     catalogue's rule of thumb, after the Ferrari, Jeep and Porsche cases).
   - Cars belong to invented makers.

## 2 · Where we start

### 2.1 Today's pipeline

| Part | Today |
|---|---|
| Builder | `source-art/build_family_cars.py`: a 1,800-line Blender script, about 5 s per car |
| Recipes | `source-art/family_recipes.py`: 61 recipes plus 30 kits |
| Body | Lofted superellipse shell, a lofted cabin (no booleans), boolean arches and lamp cut-outs, ray-laid face parts, about 40 "extras" flags |
| Wheels | Their own module: 9 spoke styles, 3 tyre types, 5 finishes |
| LODs | Decimated at 0.48 and 0.22. LOD0 is about 50–62k vertices for a family car; the Regale is 98k |
| Paint | One tintable material (`BodyPaint`). Liveries are geometry (stripes as meshes), except the Regale's painted texture |

The wheels, the lamp, grille and face-part library, the cabin's window-outline method, the render and line-up tools,
and the export tail all carry over. The shell, the booleans and the paint model are what get replaced.

### 2.2 The runtime contract a Forge car must keep

These are the names and data the game depends on, from the pipeline map:

- **Files.** `Resources/Cars/<id>.fbx`, `<id>_lod1.fbx`, `<id>_lod2.fbx`. The root's name is the body id: `ReplaceBody`
  and `CollisionBox` look it up.
- **Wheels.** Four EMPTY pivots named exactly `Wheel_FL/FR/RL/RR` at the axle centres, each with its mesh child. The
  wheels stand on y=0. `WheelVisuals`, `CarContact` and BodyMotion find them by name.
- **Material names, matched by substring.** `BodyPaint`, `Glass` and `MasterGlass`, `Rubber`, `Alloy`, `Headlamp`,
  `TailLight`, `ReverseLight` (rear only), `Amber`, `Plate`, `PanelSeam`, `BrakeMetal`, the rim finishes and so on.
  Since 14 Oct a build check fails on any car material without a colour rule.
- **Collision.** A shoulder width and length per body, in `CarBodies.ShoulderWidth` and `Length`. Today these are typed
  in by hand from the builder's printout; the Forge writes them itself (§4.8).
- **Lights.** Brake and reverse lenses exist on every racing body (`VehicleLightChecks`).
- **Body roll.** BodyMotion rolls each LOD's visual root as a rigid shell and moves the wheels to a separate holder.
  Anything mapped in object space survives this unchanged.

## 3 · The cars (what to build)

### 3.1 Class C: about 40 bodies

| Star band | Who races | From the catalogue |
|---|---|---|
| 1★–3★ | Mostly bangers, some plain family cars | About 12 bangers covering every banger silhouette type: a three-wheeler (Robin-type), a slab-sided Eastern Bloc box (Trabant, Lada, Wartburg), a bulbous British saloon (Allegro, Minor, Ambassador), a tall corrugated French utility (2CV, Renault 4), a rear-engined "ears" car (Zaporozhets, Imp), a fishbowl US compact (Pacer, Gremlin), a tall kei box |
| 3★–7★ | Family cars | Today's 10 family cars plus about 8 new: a monobox MPV, a twin-bubble six-seater (Multipla-type, a silhouette nothing else has), a kei truck, a woodie estate, a 50s tailfin saloon, a split-screen van, a microcar, a box hatch |
| 6★–7★ | Warm cars | Today's Sprint, Comet and Roadster plus about 6 new from the catalogue's 10 (a Fiesta XR2, Uno Turbo, CR-X, Cooper S, AX GTi or 205 GTI type) |
| 8★–10★ | The fastest C cars | The Rally saloon and the Zenith GT (done, 14 Oct), plus 2–3 new homologation-flavoured C cars |
| Rallycross and gravel | Off-roaders | Today's six, plus 2 new (a Land Rover-type utility and a Hilux-type pickup) |

**Kits.** Every C car gets its B kit (`<id>_tuner`), made by the kit layer (§4.6), so new C cars get kits for free.

### 3.2 Class B: about 45 new forms, from scratch

The catalogue's 10 archetypes, each taking 3–6 cars. Japanese coupes are capped at 8 of the 45:

| Archetype | Count |
|---|---|
| Hot hatches, box and round | 6 |
| Homologation and Group B-style road cars (Renault 5 Turbo-type arches behind the doors, a 205 T16, an S1) | 5 |
| Muscle and pony cars | 5 |
| Performance utes, pickups and vans | 4 |
| European coupes and fastbacks (a fish-nosed SM-type, a GTV, a Capri) | 5 |
| Mid-engine small sports and wedges | 4 |
| Roadsters, lightweights and kei sports (a Bond Bug-type wedge three-wheeler) | 5 |
| Shooting brakes and sports estates | 3 |
| Retro-futurist and quirky outliers | 3 |
| Japanese coupes | 5 |

**Today's 27 Tuners** go through the distinctness check (§5.2). The proposal is to keep the best 6–8 Japanese ones that
pass, and retire the rest from B fields. Their models stay in the body shop as cars you can buy.

### 3.3 Classes A, S and E

| Class | Count | Coverage |
|---|---|---|
| **A** | About 30 | All 8 catalogue archetypes: 70s–80s wedges, curvy 60s exotics, front-engine GTs, modern mid-engine, rear-engine, hypercars with layered aero, track-focused roadsters, electric and shooting-brake GTs |
| **S** | About 30 | Touring cars, GT3/GT4, Group 5 silhouettes, Group B and WRC, rallycross, stock cars, open-wheelers, hill-climb specials, drift cars and trophy trucks |
| **E** | About 25 | Every Le Mans category: Hypercar (LMH/LMDh, with fins, and a wingless 9X8-type outlier), LMP1 open and closed, LMP2, LMP3, GTE, LMGT3, 90s GT1 and Group C, 60s–70s Group 6 and longtails |

S and E are why the Forge needs open cockpits, open wheels, canopies and longtails from the start (§4.3).

### 3.4 Design rules for every car (toy caricature)

From the toy-maker and game evidence in the catalogue, §1:

- **Caricature, don't scale.**
  - Wheels 10–20% larger than true scale.
  - Roofs crushed lower.
  - Overhangs short.
  - Each car keeps its 2–3 signature features and loses the rest.
- **Fixed footprint, varied proportion.**
  - Each class has a footprint band (length, width, track).
  - Within it, cars vary nose height, cabin position, roofline type, tail type, arch treatment and wheel size.
- **The silhouette signals speed.**
  - Bangers are tall and boxy, with small wheels, mild cambers and soft round corners.
  - Each class up gets lower and more wedged, with bigger wheels.
  - Halo cars (A, S, E) get the loudest feature: a wing, fins, a canopy or a longtail.
- **Honest collision.** The visible body stays close to the collision box (Rocket League's lesson, and our own
  per-body box).
- **Colour blocking.** Body, trim and glass are separate at 40 px; two-tone zones are part of the livery system (§4.7).

## 4 · The Forge (how)

### 4.1 Pipeline

```
recipe (data) ─► stance + hardpoints ─► skeleton: named seam curves ─► parts: one quad patch each
   ─► cage: patches welded on shared seams, edges tagged smooth / character / hard / gap
   ─► subdivision with creases (Blender's OpenSubdiv, headless) ─► LOD0
   ─► LOD1, LOD2: the same recipe at coarser part resolution, projected onto the LOD0 surface; LOD3 silhouette hull
   ─► separate shells: wing, splitter, buttresses, canopy, roll cage, open-wheel pods
   ─► wheels and the detail library (lamps, grilles, vents, badges) laid onto the new surfaces
   ─► paint classes per vertex, livery landmarks ─► FBX + body data ─► QC report ─► review sheet
```

**Engine.**
- The geometry is plain Python and numpy. It doesn't import Blender, so it's testable, fast, and the QC can run without
  Blender.
- Blender runs headless only to subdivide, read back the result and export FBX. This is the existing export tail.

**Flag.** It sits beside today's builder. Today's cars keep building the old way until each is rebuilt and approved.
No game code changes until §4.7's shader, which goes behind a flag.

### 4.2 Parts that connect seamlessly

The research recommendation (75b §3):

1. **Shared seam curves.**
   - A car's skeleton is a set of named 3D curves: belt line, shoulder, sill, nose edge, tail edge, A, B and C pillars,
     roof edges, bonnet and deck edges.
   - Each curve is sampled once.
   - Every panel that meets a seam reads the same samples, so joins are exact by construction, not by clean-up.
2. **Each panel is its own patch, with its own generator and parameters.**
   - Bonnet: crown, power bulge, vents.
   - Nose: one of 6 section families, square to round.
   - Wings: an annular grid round the arch, with flare rise, reach and lip; no boolean.
   - Doors and sides: a scoop or strake as a recessed sub-patch.
   - Roof: camber, double bubble.
   - Tail: Kamm, duck, fastback, notch, longtail.
   - Plus the sill, the arch tunnel and the diffuser.
   - The bonnet can be flat and square while the wings are round and tall. Today's builder can't do that.
3. **Ribbon rows for smooth joins.**
   - Where a join should be smooth, the first row inside each panel follows the seam's shared tangent, so the
     subdivided surface flows across it.
   - Where it should be crisp, the edge is tagged a character line (semi-sharp crease) or hard.
4. **Gaps are designed.** A `gap` edge becomes a real inset shut line at LOD0 and a drawn line at LOD1.
5. **No booleans anywhere.**
   - Arches, intakes and window openings are built into each patch's own layout: inset rings, as today's cabin
     already does for windows.
   - This removes the fragility list in §1.2.
6. **Separate shells overlap their host by a checked amount (5–10 mm).** Wings, splitters, flying buttresses, floating
   roofs, canopies and roll cages are each their own closed shell. The QC holds a table of which shells may touch.

### 4.3 Body topologies

A car picks one topology. Each is a fixed set of panels and seams:

| Topology | For | Panels beyond the basic set |
|---|---|---|
| Closed three-box, two-box, monobox | C, B, A road cars | Bonnet, boot or hatch, greenhouse |
| Pickup or van | Kei trucks, utes, vans | Cab, a bed or box shell |
| Three-wheeler | Robin-type bangers, the Bond Bug-type | One front wheel and its fairing |
| Open roadster or targa | B and A roadsters | Cockpit tub, windscreen frame, roll hoop |
| Canopy prototype | E prototypes, Group C, hypercars | A canopy shell, fins, longtails, side pontoons |
| Open cockpit | LMP open cars, Group 6 | Cockpit tub, headrest fairing, rollover hoop |
| Open wheel | S formula and hill-climb cars | Monocoque, sidepods, front and rear wing shells, exposed suspension arms (simple rods), wheel tethers |

### 4.4 Keeping the toy look as it gets complex

From 75b §4:

- **Three bevel sizes**, shared by every part, so every edge rounds in the same family: about 2 cm, 5 cm and 12 cm,
  tuned on the first sheets.
- **A curvature budget.** Minimum radii per surface type, checked by QC.
- **Six named section families** per part (round, square, wedge, teardrop, flat-bottom, coke-bottle) rather than free
  exponents, and parameters snapped to a coarse grid (1 cm, 5°). Cars then differ by design, not by noise.
- **A minimum feature size of about 3 cm** at 8 m (about 3 px at 1080p). Anything smaller exists only at LOD0.
- **Real light.** Geometry carries the form; nothing is baked into textures.

### 4.5 Wheels and details

- **Wheels.** The current wheel module stays and grows:
  - wheel size by class (the caricature rule);
  - centre-lock, deep-dish, turbofan and aero covers;
  - slicks with or without sidewall lettering;
  - knobbly and snow tyres.
- **Detail library.** Lamps, grilles, intakes, vents, mirrors, wipers, badges, number plates, light pods, tow hooks,
  fuel fillers, roof racks and roll cages are ported from today's `face` and `extras` library onto the new surfaces.
  - They're laid by ray onto the finished patch, as today.
  - Where they sit in a recess, the recess is built into the patch (no cut).

### 4.6 Kits

A kit is a recipe edit: flares, wheels, aero shells, dropped trim. The panel structure makes this cleaner than today's
`_kit()`. A fender patch with a flare is the same patch with a different reach, so every new C car gets its B kit
automatically. A/S/E kits ("touring", "longtail", "race") use the same layer.

### 4.7 Liveries, vinyls and decals

The design from 75b §2–3.6.

**Livery space, not per-car UVs.**
- Every point on the body maps to `(zone, s, t)`, with zones side (left and right), top, front and rear.
- `s` runs along the car and `t` up it, through a **landmark warp**: front tip, front axle, A, B and C pillars, rear
  axle and tail along the car; sill, belt and roof up it.
- So "a stripe along the belt line" or "a number on the door" lands in the same place on every car, however its
  proportions differ.
- The warp is a few dozen numbers per car, written by the Forge.

**Shader.**
- A URP body-paint shader works out `(zone, s, t)` from the body's object-space position and normal, then samples the
  car's livery atlas.
- Object space means body roll, pitch, heave and every LOD see exactly the same livery. No UVs need to match between
  LODs.
- It keeps the project's rules: `_LIGHT_COOKIES` cloud shadows, and colours graded like `TrialWorld.Material`.

**Paint classes.**
- Each vertex carries its class: paint, two-tone zone, trim, glass, arch interior, underbody, lamp, gap.
- Livery shows only on paint.
- A fade by surface angle stops stripes smearing over arch lips and flares.

**Layers.** A livery is data: a stack of layers, authored once per class and fitted to every car.
- Base paint, and the second-colour zones (roof, bonnet, lower body).
- Stripes: racing, side, pinstripe and hockey-stick.
- Shapes, gradients and fades.
- Numbers and text in MSDF, crisp at any size.
- Sponsor decals: our invented brands, as images.

**Composition.** The stack is composed into a small atlas when a car loads or its livery changes:
- 2048 for the player;
- 1024 for AI cars;
- three zones in one texture.

**Uses.**
- AI fields get liveries from the field recipe: a class C "club" livery, class S team liveries, class E Le Mans numbers
  and class lights.
- The player gets a vinyl editor in the paint shop later. That's its own piece of work; the system is designed for it
  now.
- Dirt, mud and scuffs stay URP decal projectors, parented to the body.

**Proof.** A fixed test livery (parallel stripes, a grid and a big number) is rendered on every car in QC, and the
stretch is measured (§5.3).

### 4.8 Body data written by the Forge

The Forge writes body data for each car (a JSON in `Resources/Cars/`, read by `CarBodies`):
- shoulder width and length (the collision box, measured as today);
- wheel radius and track;
- the livery landmarks;
- the light anchors (light trails) and the class footprint.

This removes the hand-copied dictionaries in `CarBodies.cs`. An editor check confirms every body has its data.

## 5 · Judging the cars (QC)

Every build writes a machine-readable report. The bars in §1.3 are enforced here, with thresholds calibrated on today's
cars (75b §5).

### 5.1 Geometry

Each shell is checked for:
- watertight and manifold;
- no degenerate or flipped faces;
- no self-intersections;
- shell overlaps only where the table allows, at the set depth;
- no thin slivers below the minimum feature size;
- untagged creases (a dihedral angle over 30° that isn't marked);
- normal continuity across smooth seams;
- wheel-to-arch clearance, symmetry and ride height.

### 5.2 Distinctness

- Black orthographic masks of every car: side, rear, top and four three-quarter views.
- Pairwise silhouette IoU, Hu-moment distance, and a side-profile height curve (bonnet height, roof peak, tail slope).
- **Rule:** a new car must clear the threshold against its nearest neighbour in its class in at least two views.
- **Calibration:** the threshold is set so that today's Tuner pairs Richard called too similar fail.
- The report prints each class's similarity matrix, so a whole roster's spread can be seen at once.

### 5.3 Livery

- The stretch of the livery mapping (its Jacobian) on every painted face.
- The test-livery render, with stripe-width variation measured.
- LOD silhouettes match LOD0: IoU of at least 0.98 at LOD1 and 0.95 at LOD2.

### 5.4 Budgets and look

- Triangles, materials and draw calls per LOD (§6).
- Curvature budget and bevel sizes in use.
- Golden-image regression: approved cars' sheets compared after every Forge change, so a change for one car can't
  quietly alter another.

### 5.5 Review with Richard

The QC replaces "is it broken?". Richard judges "is it good?", in the existing loop:
- render sheets per round (front, rear, side, three-quarter, a line-up against its class);
- scored against references;
- exported to the game only when he's happy.

The line-up tool from 14 Oct (`--field-lineup`) shows each class's real fields in-engine.

## 6 · Budgets

Today's family cars are about 50–62k vertices at LOD0, with no livery. The Forge's subdivided panels get their smoothness
from structure, not density. Its targets:

| LOD | Body | Wheels (4) | Whole car | Notes |
|---|---|---|---|---|
| LOD0 | 10–20k triangles | 4 × 1.5k | ≤ 30k | Real shut lines, interior |
| LOD1 | 3–6k | 4 × 400 | ≤ 9k | Drawn shut lines |
| LOD2 | 0.8–1.5k | 4 × 120 | ≤ 2k | Silhouette only |
| LOD3 (new) | 200–400 | Painted on | ≤ 0.5k | Livery baked to colours |

- **Materials:** at most 5 per car (body paint and livery, glass, trim, lamps, tyres and wheels), so the SRP Batcher can
  batch them.
- **Performance proof:** the drive benchmark with a full Forge field must stay within docs/73's budgets, with p99 frame
  time at or under 20 ms.

## 7 · Order of work

Each phase ends with proof, and Richard sees renders at each "review" mark.

| Phase | What | Proof | Rough effort |
|---|---|---|---|
| **F0** | **QC harness first.** Geometry, distinctness and livery-stretch checks, run on today's 60 cars. Calibrate the distinctness threshold on the Tuners. | The similarity matrix of today's B field: the "too similar" pairs measured, not just felt | 3–5 days |
| **F1** | **Core.** Skeleton, three panels (bonnet, wing with arch, door with scoop), cage, creases, subdivision, LOD coarsen-and-project | A test piece passes every geometry check and the reflection-line render | 3–5 days |
| **F2** | **A whole closed car.** All closed-body panels, the six section families, wheels and the detail library ported, kits. Rebuild three of today's cars as the benchmark. | Review: the three rebuilt cars beside the originals, matching or beating them, at a third of the triangles | 1–2 weeks |
| **F3** | **Liveries.** Body data, landmark warp, paint classes, the URP shader behind `--livery`, the atlas composer, MSDF numbers, three test liveries | Review: one livery on ten different cars; stretch and roll tests pass; drive benchmark within budget | 1–2 weeks |
| **F4** | **Class C roster.** The bangers, new family and warm cars, every C kit | Review per batch of about 6; the C distinctness matrix passes | Ongoing |
| **F5** | **Class B from scratch.** The 45 forms by archetype, then the Tuner keep-or-retire pass | Review per batch; the B matrix passes with margin | Ongoing |
| **F6** | **Open topologies.** Roadster, canopy, open cockpit, open wheel, separate-shell aero | Review: one car of each | 1–2 weeks |
| **F7** | **A, S and E rosters,** as those classes come up in the roadmap (docs/45) | Per class | Ongoing |

C and B come first: they're the cars a player meets in the first hours, and today's B field is the problem Richard sees.
F6 can move earlier if A, S or E work comes sooner.

## 8 · Risks

| Risk | Answer |
|---|---|
| Corners where three or five panels meet show bumps on highlights | Built and checked first, in F1, on a three-panel test piece; a fixed corner template |
| Crease sharpness differs by subdivision level or Blender version | LODs are made by projection, not by lowering levels; the Blender version is pinned; calibrate once |
| Complex cars drift from the toy look | The bevel set, section families, curvature budget and golden images; Richard reviews every batch |
| Liveries distort on extreme bodies (very short, very tall) | A clamp on the warp; the stretch QC; per-car landmark overrides |
| Stripes break where side meets top | Liveries designed with the zones in mind (stripes cross on the shoulder line), plus a blend band |
| Tooling on Windows (self-intersection libraries) | Behind one function, with a voxel fallback |
| The roster is large (about 170 cars) | The generator carries the variety; each car is a recipe of a few dozen parameters, made in batches with QC and review sheets |

## 9 · Decisions for Richard

1. **Approve the approach:** a panel-based Forge, with object-space liveries and QC-first.
2. **Today's 27 Tuners:** keep the best 6–8 that pass the distinctness check in B fields and retire the rest from racing
   (still sold in the body shop), or retire all of them?
3. **Invented makers:**
   - Should cars belong to a handful of fictional brands with their own design language (for example a sensible
     Bayview make, a Hillcrest coachbuilder, a Highmoor rally firm), so liveries and names have a home?
   - Or stay brandless?
4. **Bigger wheels and crushed roofs** (the caricature rule): this changes the look of today's C cars too, when they're
   rebuilt. Agree, or keep today's proportions for C?
5. **The player vinyl editor:** designed for now, built later. Agree?
6. **Order:** C bangers and family cars first (F4), then B (F5)? Or B first, since that's where the sameness is?
