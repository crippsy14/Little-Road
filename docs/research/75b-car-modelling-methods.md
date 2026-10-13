# Procedural car bodies and a universal livery system: research and recommendation

Date: 13 Oct 2026. Scope: replace the single-loft body in `source-art/build_family_cars.py` with a part-based generator, and replace the per-body 2048 livery atlas (`source-art/regale_livery.py`) with a livery system that works on every generated car.

Source note: the web-search tool returns summaries, so most claims below are summary-level. I fetched and read three pages in full: the Hutch car-decal write-up, the Unity 6.6 Decal Projector reference and the Unity 6.6 Decal Renderer Feature reference. Statements marked **[mine]** are my engineering judgement or background knowledge, not backed by a fetched source. Statements marked **[verify]** are specifics I could not confirm and should be tested before relying on them.

---

## 0. Executive summary

- **Body.** Build each body panel as its own small quad grid, a "panel patch" with its own parametric generator. Neighbouring patches share named seam curves, so boundaries are identical by construction. Weld everything into one quad **control cage**. Tag edges (hard, semi-sharp, gap) and run **Catmull-Clark subdivision with creases** (OpenSubdiv, via Blender's Subdivision Surface modifier).
- **Separate shells.** Floating roofs, wings, splitters and intake lips are separate closed shells, because those really are separate pieces. They are placed with a controlled overlap, never a boolean.
- **LODs.** Make LOD1 and LOD2 by coarsening the panel grids and snapping to the limit surface. Do not use lower subdivision levels, because semi-sharp creases decay with level.
- **No boolean cuts.** Wheel arches, scoops and intakes are built into panel topology templates, which fits the project's existing "no booleans" lesson (doc 66).
- **Livery.** Do not use per-body UV atlases, and do not use URP Decal Projectors for liveries. Use a **canonical livery space**:
  - The car body's object-space position is mapped to livery coordinates `(zone, s, t)` through a small per-car **landmark warp**. Landmarks are axles, A/B/C pillars, beltline, roofline and so on, and the warp is piecewise-linear.
  - The body shader evaluates this in the fragment stage, so the result depends only on object-space position. That makes it identical across LODs and unchanged by body roll or pitch.
  - Liveries are authored once as a stack of 2D layers in canonical space: stripes, shapes, MSDF text and numbers, sponsor images. They are composited into a 3-zone atlas (side, top, front/rear) when a car loads or its livery changes.
  - A per-vertex "paintable class" mask excludes glass, arch interiors, trim and underbody.
- **Quality control first.** Build the automated QC harness (topology, seam continuity, curvature, silhouette distance, livery stretch) before the generator, and calibrate its thresholds on the existing hand-tuned cars.
- **Not usable offline.** The ML/dataset work (DrivAerNet++, DeepSDF/VehicleSDF) is useful only for reference proportions and test fixtures. It does not produce stylised, game-ready, part-structured, sharp-featured cars.

---

## 1. Surface representations: options compared

Scores are 1 to 5, higher is better. They are my judgement for **this** use case: stylised toy cars, parts that must join cleanly, a Python/Blender pipeline, FBX export with 3 LODs, and no robust-boolean dependency. "Effort" is scored so that 5 means cheap to build.

| Approach | Seamless joins | Sharp + smooth | Per-part parameterisation | Robustness (no self-intersection) | UV / paint ability | LOD ability | Poly-budget control | Impl. effort (5 = cheap) | Total /40 |
|---|---|---|---|---|---|---|---|---|---|
| A. Current: single lofted superellipse + separate cabin + booleans | 2 | 3 | 1 | 3 | 3 | 3 | 3 | 5 (already built) | 23 |
| **B. Panel patches welded into a cage, Catmull-Clark + creases (recommended)** | **5** | **5** | **4** | **4** | **4** | **4** | **4** | **3** | **33** |
| C. Explicit bicubic B-spline / Bezier patch per panel, tessellated | 3 (G0 easy; G1 needs matching) | 4 | 5 | 4 | 5 (native uv) | 5 (tessellate at any density) | 5 | 2 | 33 |
| D. NURBS / T-splines | 4 (T-splines merge patches gap-free) | 5 | 4 | 3 | 5 | 5 | 4 | 1 (no usable Python/Blender tooling) | 31 |
| E. Coons / Gordon fills from a feature-curve network | 5 (patches share curves) | 3 (smooth by default, creases manual) | 4 | 3 (curve ordering and direction pitfalls) | 4 | 4 | 4 | 3 | 30 |
| F. SDF smooth-union + surface nets / dual contouring | 5 (joins are automatic) | 2 (fillets are free, sharp creases are hard) | 3 | 3 (DC not guaranteed manifold) | 1 (no parametrisation; needs unwrap) | 2 (re-mesh, not coarsen) | 2 | 3 | 21 |
| G. Blender Geometry Nodes as the authoring engine | depends on the surface underneath | depends | 4 | depends | depends | depends | depends | 2 (node graphs are hard to diff, test and run headless) | n/a (tooling choice) |
| H. Shape grammar / rule system for choosing and arranging parts | n/a | n/a | 4 (rules over parameters) | n/a | n/a | n/a | n/a | 3 | n/a (a layer on top of B) |
| I. ML / learned shape spaces (DrivAerNet++, DeepSDF, VehicleSDF) | 3 | 1 | 1 | 2 | 1 | 2 | 1 | 1 | 12 |

Key reasoning per approach follows.

### B. Cage + Catmull-Clark with creases (recommended)

- **Why it works for joins.** Catmull-Clark is defined on one connected quad mesh. If two panels share the same boundary vertices in the cage, the limit surface is automatically continuous across the join. Away from extraordinary vertices (valence != 4) the limit is C2. At extraordinary vertices it is C1 only. This is the standard behaviour of the scheme, summarised in the OpenSubdiv overview, which also notes that feature-adaptive refinement is built around extraordinary vertices and creases.
- **Sharp and smooth.** Semi-sharp creases come from DeRose et al., and OpenSubdiv implements them in its `Sdc::Crease` rules. Sharpness is a float per edge. The OpenSubdiv `Crease` reference says semi-sharp creases are reduced by 1.0 per subdivision level and sharp ones stay sharp.
- **Level dependence.** The decay per level means the visual crease radius depends on the number of subdivision levels. This matters for LODs, which is why section 3.3 below does not make LODs by changing the level.
- **Blender support.** The Subdivision Surface modifier and the Geometry Nodes Subdivision Surface node both use OpenSubdiv. Per the Blender commits, the node has an Edge Crease field (clamped 0 to 1) and a Vertex Crease field. Creases are stored as an edge attribute (`crease_edge`) and a vertex attribute (`crease_vert`) **[verify the attribute names in the exact Blender version]**.
- **Calibration.** Blender's 0..1 crease maps nonlinearly to OpenSubdiv sharpness **[mine, verify]**. Calibrate empirically: build a test strip with crease values 0, 0.25, 0.5, 0.75, 1.0 and measure the resulting radius.
- **Cost.** The cage topology has to be designed per panel type (templates). That design work is real, but it is a one-time cost per part type and it is what makes the cars good.

### C. Explicit patch per panel (bicubic B-spline or Bezier, tessellated)

- Very direct per-part control and native (u,v) per panel. Tessellation density is a free LOD parameter.
- Joining adjacent patches smoothly (G1) needs explicit tangent matching. Extraordinary corners, where three or five patches meet, are the classic hard case. Subdivision solves exactly this case, which is why B scores higher on robustness of joins at corners.
- This is a good alternative if panel corners are always regular, but cars have many 3-way corners.

### D. NURBS / T-splines

- NURBS control points must lie in a rectangular grid, and complex models built from several NURBS patches often have gaps and overlaps where trimmed surfaces meet (2019 review). T-splines (Sederberg et al., SIGGRAPH 2003) were introduced to merge patches with different knot vectors into one gap-free model, with local refinement.
- This is the CAD gold standard for G2 surfaces, but there is no maintained open-source T-spline library usable from Python/Blender. Fusion's T-spline conversion is reported (forum, user report) to be capped at about 10K STL or 25K OBJ faces. The patent landscape around T-splines is also a risk **[mine]**.
- Reject for implementation effort and tool availability. For our stylised toy look, G2 across every join is not needed.

### E. Coons / Gordon fills

- Both come from the automotive industry (Gordon at GM, Coons at Ford). A Coons patch interpolates four boundary curves, and a Gordon surface interpolates a grid of guide and profile curves via a Boolean sum.
- **Fit.** Designers think in feature curves, and curve-network interpolation matches how a panel layout is specified.
- **Pitfalls.**
  - Curve ordering, direction and intersections must be valid: curves must intersect in a rectilinear layout, and a commercial Blender add-on for concept-car bodies notes that wrongly directed curves twist the patch into a saddle.
  - The output is smooth by default, so creases and shut lines still need separate handling.
- **Use here.** I recommend Coons as the **fill function inside each panel**, computing the interior of a panel grid from its four shared boundary curves. Then B handles the join smoothing and creases.

### F. SDF modelling + meshing

- **Smooth union.** Inigo Quilez's `smin` family gives free fillets between parts. The polynomial version is a cheap, local blend of two distance values, and it is not associative, so blend order matters.
- **Naive surface nets.** One vertex per cell, simple and robust, but sharp corners are pulled inward because no normals are used.
- **Dual contouring.** Places vertices by minimising a quadric error function (QEF) using Hermite data, so it can recover sharp creases and corners. It guarantees neither a 2-manifold nor a watertight mesh.
- **Sharp features from SDF grids.** Plain grids give poor intersection and normal estimates near features. Carrera et al. ("Dual Contouring of Signed Distance Data", SIGGRAPH 2026) address this with a QEF refined from the SDF grid. It is recent and I have not tested it.
- **Why not.**
  - The output has no quad flow and no panel structure, so there is no clean per-part livery mask.
  - Density is uniform rather than where curvature needs it.
  - Producing three consistent LODs means re-meshing at different resolutions, so silhouettes shimmer between LODs.
  - Our art direction needs crisp bevels and shut lines, which are hard to hold with SDF smooth-min.
- **Where SDF is still useful [mine].** As a QC tool (volume, thickness, unexpected overlap detection by voxelisation), and possibly for organic one-off props.

### G. Geometry Nodes

- Good as a human authoring environment, and a commercial "3D NPR Car Generator" add-on shows that stylised cars with swappable bumpers, roofs, hoods and wings are doable as node groups, including procedural decals.
- For a generator that must run headless, be unit-tested, produce QC reports, and be driven by recipe data, Python + numpy is easier to diff and test **[mine]**. Use GN only for hand-built part templates if you want them; do not make it the core.

### H. Shape grammars

- Orsborn, Cagan and co-workers built parametric shape grammars for vehicle side-views (Buick and Harley-Davidson derived rules, class-specific parameter ranges, cross-over vehicles) and a statistically derived grammar using PCA of real vehicle shapes (2008). A 2024 HAW Hamburg thesis (Dornhof, German, open access) builds a 3D form grammar prototype.
- Müller et al. (2006) CGA shape is the standard reference for split and repeat rules with context sensitivity.
- **Use here.** Grammar ideas belong in the recipe layer: choose and constrain parts and parameters per car class (for example "a mid-engine supercar must have a rear intake and cannot have a long hood"). They are not a geometry engine.

### I. Academic / ML work: what is usable offline

| Work | What it is | Usable in an offline Blender/Python pipeline? |
|---|---|---|
| DrivAerNet++ | 8,000 car designs from 26 ANSA-morphed parameters on realistic sedan baselines (estate, fast and notchback), with CFD data and part segmentation | Not as a generator. It needs commercial ANSA and gives only realistic sedans. Useful for **proportion ranges** (ramp angles, ride height, overhangs) and as extra test meshes for the QC harness. Check the licence before bundling. |
| VehicleSDF (Toyota Research Institute, arXiv 2410.18986) | DeepSDF latent space trained on ShapeNet cars, to generate cars meeting geometric specs | Weights/code may run, but output is blobby, smooth, has no panels and is not game-ready. Reject. |
| Multi-agent car design (arXiv 2503.23315) | VLMs + DeepSDF + LLMs for styling and aero evaluation, benchmarked on DrivAerNet | Concept-stage tooling. Reject. |
| Single-shape GAN (Nozawa et al., NSF PAR 10422898) and sketch-to-3D-car reconstruction | 3D from one sketch/silhouette | Reject (noisy meshes). |
| KAIST sketch-based car exterior/interior design papers (CHI 2025, 2026) | Human-in-the-loop sketching | Not applicable. |
| Soddu generative cars (Generative Art 2016) | Artistic generative design | Inspiration only. |

I found **no** paper called "Procedural Modeling of Cars", "SketchCar", "Car-GPT" or "CarCrafter" that fits this brief. I found no game-industry paper on procedural car bodies at all, so this is a gap in the public literature rather than something I missed.

---

## 2. Liveries and decals: how games do it, and options compared

### 2.1 What is publicly known

- **Forza (FH3/FH5, Motorsport).**
  - Each vinyl or decal is one layer. Higher-numbered layers draw over lower ones. The car sides are edited separately. FH5 raised livery texture resolution (Forza forum and GTPlanet).
  - Vinyl groups are authored on a blank canvas, so one design can be applied across many cars. This was a design goal from Forza 3 (GameSpot).
  - Turn 10 and Playground do not publish how layers are mapped to the car. A forum poster's guess is that layers are rendered to an internal RGBA bitmap and then wrapped onto the car. **That is a guess, not documentation.** Forum reports (a racing truck whose shared livery failed) suggest per-body placement quirks exist.
- **Rocket League (Epic help pages).**
  - Most decals are made for one specific car body; "universal" decals work on many. The split suggests decals are tied to each body's texture layout, with universal ones using a shared mapping. **That is an inference, not documented.**
- **Roblox patent US12597197.**
  - "Local space texture mapping based on reverse projection" describes placing a decal texture into a target texture map from a position, orientation and scale in the mesh's local space, by a per-pixel projection. This is the "project into UV space at bake time" approach.
- **Hutch (technical test, fetched in full).** A mobile racing studio's decal-painting prototype in Unity:
  - Sticker decals are tested per fragment against an oriented box in car-local space using an SDF, then projected onto the box's back plane.
  - The car mesh is drawn into a RenderTexture with a vertex shader that remaps each vertex's UV to clip space, so the mesh is flattened into its own UV layout.
  - Two textures are kept: a painted texture and a runtime texture (painted plus current preview).
  - Normal-based rejection stops paint bleeding onto far-side surfaces. They avoided `Physics.Raycast` because high-poly collision is expensive.
  - It does not discuss seams or mips. This UV-baked approach depends on good UVs.
- **Unity assets.** "Studio Livery Creator" and similar tools are UV-based and need the model to be unwrapped. A runtime vehicle decal asset claims UVs need not be nicely unwrapped by projecting into a render texture.

### 2.2 Technique comparison for our needs

| Technique | Works on every generated car with one authored livery | Stays correct under body roll/pitch | LOD-safe | Crisp numbers/text | Stretch control | Runtime cost | Notes |
|---|---|---|---|---|---|---|---|
| **Per-body UV atlas** (current: 3 views in one 2048 map via `livery_uvs()`) | Poor: art is tied to each body's metre layout | Yes (object space) | Fragile: UVs must be regenerated per LOD, and mapping changes with body | OK (baked) | Poor on arches | Low | Fragile as described in the brief |
| **URP Decal Projector** (screen-space or DBuffer) | Yes, in principle | Yes if parented to the body | Yes (projects onto whatever mesh is drawn) | Good | **Poor**: stretches on surfaces turned away from the projector (Unity issue tracker acknowledges this; mitigation is Angle Fade) | Per projector draw; screen-space is recommended on tile-based mobile, DBuffer needs a DepthNormal prepass | Projects onto **every** mesh inside its box, which includes the ground, other cars and wheels, so layers and bounds must be managed. Direct assignment of a decal material to a mesh does nothing. Needs a Decal shader graph material. |
| **Mesh decals** (quad on the surface with custom UV) | Needs per-car placement | Yes | Needs a mesh per LOD | Good | Good if UV authored | Extra draw per decal | Unreal's docs describe the pattern: geometry must hug the surface with a depth bias. Good for a few hero decals, not for a full wrap. |
| **Triplanar / box projection in the shader, in raw object-space metres** | Yes, but art scale varies by car size | Yes | Yes | Good | Medium; blends at 45 degrees | Low | Closest to what we want, but without a landmark warp one livery does not fit different wheelbases and roof heights. |
| **Canonical livery space with landmark warp, evaluated in the fragment shader (recommended)** | **Yes** | **Yes** | **Yes** | **Good (MSDF composite)** | **Good (warp) + a mask for steep surfaces** | **Low (one texture sample set)** | Needs per-car landmark data from the generator |
| **Pre-baked livery UV on each vertex (UV1/UV2) using the same canonical function** | Yes | Yes | Yes, as long as each LOD gets its own baked values | Good | Same as above | Lowest | Good fallback for LOD2/far or non-PC targets. Interpolation across big triangles is only approximately equal to per-fragment evaluation. |

### 2.3 Recommendation for ~100 generated cars

Use the **canonical livery space with landmark warp, evaluated in the fragment shader**, with the atlas composited on load. Reasons:

1. A function of object-space position alone has no dependence on UVs, so UV island layout, LOD generation and mesh changes cannot break a livery.
2. The warp is what lets one authored livery fit different bodies. A stripe that starts at the front axle and ends at the C-pillar does so on a hatchback and on a supercar.
3. The body shader is the only consumer. No runtime decal draw calls.
4. Decals that must not stretch over wheel arches and panel gaps are controlled by masking (section 5.6), not by projection geometry.

Keep the URP Decal Projector for **dynamic, non-livery effects** only: skid marks, mud splats, damage scuffs and the like. Parent them under the body for correct rolling, and restrict them with rendering layers so they do not hit other cars.

### 2.4 Crisp numbers and text

- **SDF** (Valve, SIGGRAPH 2007): store a distance field in a low-resolution texture, generated from a high-resolution image. Smooth edges and effects (outlines, shadows) work with alpha thresholding. Corners round off.
- **MSDF** (Chlumsky, Sloup, Šimeček, 2018; the `msdfgen` and `msdf-atlas-gen` tools): three channels recover sharp corners almost perfectly. The shader takes the median of RGB. Plain SDF atlases lack sharp corners; MSDF/MTSDF atlases have them.
- **Plan.** Use MSDF for numbers, letters and logos where the source is vector. Composite them into the livery atlas with a blit shader that samples the MSDF at composite resolution. The result is crisp at 2048 and mips cleanly. For the player's car, an optional per-fragment path can draw one or two number decals directly from the MSDF in the body shader, so they stay sharp at any zoom.
- Raster sponsor images go straight into the layer stack, and their max useful size is set by atlas texel size.

---

## 3. Recommended architecture

### 3.1 Data model

Everything is data. A car is one recipe file (JSON or Python dict) that the generator reads. No geometry is hand-edited.

```
CarRecipe
  stance:     wheelbase, track_f, track_r, tyre_radius_f/r, ride_height, overhang_f/r, body_width, body_length
  hardpoints: axle positions, wheel centres, arch centres and radii        (derived from stance, never typed twice)
  skeleton:   named 3D curves shared by parts ("seams")
                belt_line, shoulder_line, sill_line, nose_edge, tail_edge,
                a_pillar, b_pillar, c_pillar, roof_edge_L/R, hood_edge, deck_edge, ...
  stations:   section frames along the car (x positions with height/width/curvature) used to place seam curves
  parts:      ordered list of Part instances (below), each with its own params
  style:      bevel family (small/medium/large radii), gap width, curvature budget
  livery_landmarks: filled in by the generator (below)
```

Seam curves are sampled once into N points each (for example 33 per curve, so every grid edge on both sides of a seam uses the same samples). Both panels that touch a seam read the same array, which is what makes the join watertight by construction rather than by cleanup.

### 3.2 Part types

Each part type is a function `(recipe, skeleton) -> PanelPatch` returning a small quad grid plus tags. Examples:

| Part | Surface | Notes |
|---|---|---|
| Hood / bonnet | Coons fill between nose_edge, hood_edge (left and right) and cowl line, plus a crown bulge parameter | Optional power-bulge and vent recess |
| Nose / front bumper | Own section family (square or round), own radius | Splitter lip is a separate shell |
| Front/rear fender | Annular grid around the wheel arch; flare parameters (rise, reach, thickness) | The arch lip is a ring, so there is no boolean |
| Arch tunnel | Short tube that joins the fender ring to the inner wall | Unpainted class in the livery mask |
| Door / sill / side scoop | Side grid in the (s,t) plane with a depth map for the scoop (smoothstep recess) and a crease around the lip | Intake mouth is a hole in the grid with a ring |
| Greenhouse (pillars + roof) | Own lofted shell on its own station set | Floating roof: separate shell, with pillars as thin separate shells |
| Roof | Own panel or shell, own camber | Optional gap to the pillars |
| Deck / rear / tail | Own section family | Flying buttresses are thin separate shells that blend into the deck |
| Diffuser / floor / splitter | Flat shell with fins | |
| Wing | Airfoil extrusion + endplates + stanchions | Separate shell with a controlled overlap into the deck |
| Intake / vent | Ring + recessed floor, optionally a grille texture on a mask | |
| Lamps / grille | Inlaid panels that sit in a recess in the host panel (use the existing ray-placed approach as a fallback) | Lamp glass is its own material ID |

### 3.3 How joins work

1. **Shared seam curves** (above) give G0 continuity exactly.
2. **Ribbon rows for tangent continuity.** The first interior row of each panel is placed along the seam's shared tangent frame (a "ribbon"), so both neighbours subdivide into a visually tangent-continuous surface. Where a hard edge is wanted, the ribbon is skipped and the edge is tagged crease.
3. **Weld** all panels in the cage at seam vertices. Edges get one of three tags:
   - `smooth`: crease 0.
   - `character`: semi-sharp crease (for example 0.5 to 0.8 in Blender units, calibrated).
   - `hard`: full crease.
   - `gap`: an authored inset channel (see section 4.3).
4. **Corners.** Valence-3 corners (where three panels meet, for example fender/hood/bumper) are placed by template, and the cage design ensures extraordinary vertices land on creased corners or flat regions, not on prominent curved highlights.
5. **Separate shells** for things that are physically separate (wing, splitter, floating roof, buttresses). Each is closed and has its own cage. They overlap their host by a required minimum (for example 5 mm to 10 mm) and the QC checks it. No booleans.
6. **Subdivide** with Blender's Subdivision Surface modifier (OpenSubdiv), with creases on. Boundary mode, use of `Keep Corners`, and levels are fixed constants per LOD.
7. **LODs.**
   - **LOD0** = fine cage grids, subdivision level 2 (or 1 for simple parts).
   - **LOD1 and LOD2** = the same recipe at coarser grid resolution (parts have a `res` parameter), at a lower subdivision level, then **projected onto the LOD0 limit surface** (shrinkwrap / nearest point). This keeps silhouettes and crease radii matched.
   - Reason: semi-sharp creases lose 1.0 sharpness per level (OpenSubdiv `Sdc::Crease`), so simply lowering the level changes crease character.
   - **LOD3 / far** = merged silhouette hull, about 200 to 400 triangles, with the livery baked to vertex colours.
8. **Normals.** Smooth normals from the limit surface; split vertices only on `hard` creases.

### 3.4 Cutouts and scoops without booleans

- Arches: annular fender grid + arch tunnel (above).
- Intakes and scoops: rectangular or oval sub-regions of a panel grid. The grid is built with the region as its own sub-patch (inset ring + depth profile), so the recess boundary is a real edge loop that gets a crease.
- Large cuts (a full open-air scoop or a channel) become two shells that meet along a seam.
- The existing `cabin` cutout approach in doc 66 (hole edge vertices snapped to the outline, no boolean) is the same idea and can be carried over as the "window opening" part.

### 3.5 Implementation approach

- Build the generator in **plain Python + numpy** with no `bpy` import (testable, fast, runs in CI), emitting a cage as arrays: `verts`, `quads`, `edge_tags`, `part_ids`, `paint_class`.
- Use **headless Blender** only to (1) build the Blender mesh from the cage and set `crease_edge`, (2) apply the Subdivision Surface modifier and read back the evaluated mesh, (3) FBX export of LODs. Keep the existing `export_unity.py` as the export tail.
- If Blender per-call overhead becomes the bottleneck, implement Catmull-Clark with creases in numpy **[mine]** (about 200 lines, including the sharp/semi-sharp rules from the OpenSubdiv `Sdc` reference). That makes the QC loop independent of Blender.
- Keep wheels, tyres, lamps and interior as the existing separate modules. Only the body shell changes.

### 3.6 Livery mapping

**Livery space.** A point on the body maps to `(zone, s, t)`:

- `zone` is one of `side` (left or right), `top`, `front`, `rear`, chosen from the interpolated smooth normal: dominant axis with a soft blend band of about 10 degrees.
- For **side**: `s` runs along the car length and `t` up the body.
  - `s_obj` (metres along the car) maps through a **piecewise-linear warp** of N landmarks to canonical `s` in [0,1].
  - The landmarks are: front bumper tip, front axle, front arch end, A-pillar base, B-pillar, C-pillar base, rear arch start, rear axle, tail end.
  - `t` is warped similarly using sill, beltline, roof (so a "door stripe" always lies between sill and belt).
- For **top**: `s` as above, and `w` = lateral position divided by the local half-width of the body.
- For **front/rear**: `w` and `t` against the local face width and height.
- **Left side**: `s` is flipped so lettering reads left to right on both sides (as the current Regale livery already does with its mirrored left view).

**Per-car data.** The generator computes the landmark table from the recipe (it knows where each pillar and axle is) and writes it to a small ScriptableObject or a 1D float texture of about 16 texels per axis. Per-car cost is a few dozen floats.

**Shader.** The body shader (in Unity, a custom URP shader; remember the project rule that custom shaders need `_LIGHT_COOKIES` support for cloud shadows):

1. Reads object-space position and smooth normal.
2. Looks up `(zone, s, t)` through the warp.
3. Samples the livery atlas (3 zones in one texture, or a texture array), multiplies by the paintable mask.
4. Blends over base paint colour; livery colours are graded the same way `TrialWorld.Material` and `WorldLook` grade the world, as `regale_livery.py` already does (do not hand-set saturated colours).

**Paintable mask.** The generator writes a `paint_class` per vertex into a vertex channel (UV3 or vertex colour): painted body, glass, arch interior, underbody, trim, lamp, tyre. Livery alpha is multiplied by `paint_class == painted`. This is what keeps decals off arch interiors and shut-line channels.

**Steep-surface fade.** Within the painted class, fade livery alpha as `|n . projection_axis|` falls below a threshold (for example 0.35), so stripes do not smear across wheel-arch lips and fender flares where the surface turns away from the projection direction. This is the same idea as the URP Decal Projector's Angle Fade.

**Atlas.** Compose when a car loads or its livery changes:

- Player car: 2048 x 2048 (the current budget and about 2.2 mm per pixel along a 4.5 m car).
- AI cars: 1024 x 1024 (about 4.4 mm per pixel), BC7-compressed after compositing, about 1 MB each **[mine, estimates]**.
- The layer stack is vector-first: shapes, stripes (as polygons or distance-field bands), MSDF text and numbers, raster sponsor images. Composition is a series of blits into a RenderTexture, using the two-buffer pattern (committed + preview) from the Hutch prototype.

**Body motion.** Because the mapping is a function of **object-space** position on the body mesh, body roll and pitch (a rigid transform on the visual body) cannot change it. Do not use world-space or screen-space mapping for liveries. Dynamic decals that use URP Decal Projectors must be parented to the body transform.

**LODs.** All LODs use the same function of position and the same atlas. The generator emits LODs that match the LOD0 silhouette (section 3.3), so the livery looks the same at every distance. Per-vertex pre-baked `(s,t)` is available as a cheaper variant for LOD2.

**Failure modes and tests.** Section 5.5 and 5.6 describe the QC for livery stretch: for each paintable face, the Jacobian of livery coordinates (metres per livery unit) gives stretch ratios; the QC flags paintable faces above a threshold and renders a stripe-pattern test image.

---

## 4. Keeping a cohesive stylised look while increasing complexity

### 4.1 Rules to adopt (all **[mine]** unless noted)

- **Bevel vocabulary.** A fixed set of three bevel radii (for example 2 cm, 5 cm, 12 cm) shared by all parts. Every crease is assigned one of the three. This gives the "toy shapes" family look even as forms get complex.
- **Curvature budget.** A minimum radius of curvature per class of surface (body panel, flare, scoop lip), enforced in QC.
- **Quantised parameters.** Snap proportions to a coarse grid (for example 1 cm for lengths, 5 degrees for angles) so cars do not differ by noise.
- **Archetype section families.** A small number (about 6) of named section shapes (round, squared, wedge, teardrop, flat-bottom, ...), selectable per part, rather than free exponents. This fixes the original problem that one section family was shared by the whole car without making every car free-form.
- **Real-light rule.** Follow `docs/art-direction.md` and `WorldLook`: geometry carries the form, not baked lighting.

### 4.2 Minimum feature size at gameplay distance

Pixel size at distance `d` for vertical field of view `fov` and screen height `H`:

`pixel_size = 2 * d * tan(fov/2) / H`

Worked example **[mine, arithmetic]**: `d = 8 m`, `fov = 60 degrees`, `H = 1080` gives about 8.5 mm per pixel. A feature should span roughly 3 to 4 pixels to read, so about 3 cm at 8 m.

Rules that follow:
- Bevel radii below about 2 cm only exist to catch a highlight at close range (LOD0) and drop out at LOD1.
- Panel gaps: make them an exaggerated, toy-scaled 1.5 to 2 cm at LOD0 with a dark floor; at LOD1 and beyond replace them with a line in a vertex channel or a shader-drawn line.
- Check each LOD switch distance against the same formula: a feature must be at least 3 px at the distance where the LOD that contains it is replaced.

### 4.3 Panel gaps and shut lines: normal map vs geometry

Evidence from practitioners (BeamNG wiki, Polycount threads):

- Dense racing games (Forza, Gran Turismo) use very high polygon counts and real geometry for body shells (BeamNG wiki says 100 to 200 thousand, usually laser-scanned; a Polycount poster on a 35 to 40k Codemasters-spec car says most curvature comes from geometry because texture memory was small).
- Normal-map-only detail is hard to vary because the bake is tied to the low-poly geometry. That matters for us, because every car is generated and will change.
- A recurring problem is UV islands per panel: splitting vertices at the panel edges causes a lighting mismatch on flat surfaces that does not bake out. This does not apply to our object-space livery, since we do not need panel UV islands.

Recommended for us **[mine]**:
- **LOD0:** real inset channels along `gap` edges (two extra rows of quads, floor lowered by about 1 cm, vertex colour or material ID dark). Because gaps are authored in the cage as edge tags, they are free to add.
- **LOD1:** no channel; a shader-drawn dark line from a vertex attribute (distance to the nearest gap edge, baked per vertex).
- **LOD2/3:** nothing.
- Do not rely on a baked normal map for shut lines.

### 4.4 Polygon budgets for 24-car fields with 3 LODs

Published numbers are scarce. The only concrete data points I found:

- Unity asset-store sports cars of about 9,356 to 9,974 triangles at LOD0, with LOD1 about 5.3 to 5.9k, LOD2 about 2.2 to 2.7k, LOD3 about 327 (roughly halving each step).
- A forum thread suggests 15k to 25k for the highest detail LOD, halving per LOD, and notes that wheels multiply the cost by 4. An unverified forum claim says Forza cars were 50 to 60k at LOD0.

Proposed budget for our stylised look **[mine]**:

| LOD | Body tris | Wheels (4) | Notes | Switch distance (example) |
|---|---|---|---|---|
| LOD0 | 8k to 14k | 4 x 1.2k | gap channels, small bevels, interior visible | 0 to about 15 m |
| LOD1 | 3k to 5k | 4 x 400 | no gap channels, simplified interior | about 15 to 40 m |
| LOD2 | 0.8k to 1.5k | 4 x 120 | silhouette only | about 40 to 100 m |
| LOD3 | 200 to 400 | none (painted on) | impostor mesh | beyond |

A 24-car field with a typical distribution (4 near, 8 mid, 12 far) is then roughly 4 x 19k + 8 x 8k + 12 x 2.3k, about 170k triangles per view. That is small for a mid-range PC; the real cost drivers are **draw calls and shadow cascades**, so keep to about 3 to 4 materials per car (paint + livery, glass, trim, lamps) and use the SRP Batcher **[mine]**.

---

## 5. Quality control automation

Principle: the generator produces a car, then a QC script produces a **machine-readable report** with a pass/fail per check, and thresholds are calibrated on the existing hand-tuned cars (use the Tuner family and the Regale as the "good" and "too similar" samples).

### 5.1 Topology and robustness

| Check | Method | Fail rule |
|---|---|---|
| Watertight / manifold per shell | `trimesh` `is_watertight`, `is_winding_consistent`, `is_volume`; edge-use counts; libigl `is_edge_manifold` / `is_vertex_manifold` **[verify API names in your versions]** | Any open or non-manifold edge in a shell that should be closed |
| Degenerate faces | area < 1e-8 m2, zero-length edges | Any |
| Flipped normals | signed volume > 0 per shell | Negative volume |
| Self-intersection within a shell | CGAL-backed libigl self-intersection test (detection mode); `manifold3d` as a robust alternative **[mine]** | Any pair, unless in an allow-list |
| Inter-shell intersection | Pairwise test between shells with a **declared allowed-overlap table** (wing into deck, buttress into deck, ...), measure penetration depth | Unlisted pair intersects, or listed pair under/over the required overlap |
| Thin geometry | Ray-cast thickness or SDF sampling inside shells | Thickness below the minimum feature size |

### 5.2 Triangle and quad quality

- Triangle minimum angle, aspect ratio, edge length below `min_feature / 4`. For quads, the Jacobian ratio from the Verdict metrics **[mine; Verdict is a mesh-quality library, not searched]**.
- Count extraordinary vertices and where they sit: flag any on a high-curvature highlight region.

### 5.3 Curvature and smoothness

- **Dihedral-angle histogram** per shell: any edge above 30 degrees that is not tagged `hard` or `character` is an untagged crease.
- **Seam normal continuity:** compare vertex normals on both sides of each shared seam in the evaluated mesh; flag > 2 degrees where the seam is meant to be smooth.
- **Curvature continuity (G2-like).** Compute principal curvatures per vertex (libigl `principal_curvature` **[mine]**) and check the curvature along each seam curve for jumps relative to the neighbouring interior values.
- **Zebra / reflection-line render.** Zebra analysis is the standard visual test for G2: at G1 the stripes meet and bend sharply at a seam; at G2 they pass smoothly (Rhino documentation and vendor tips). Automate by rendering with a striped environment (a matcap with stripes in Blender Workbench), then measuring discontinuities in the stripe edge direction across the seams. A forum caution: passing zebra is not the same as Class-A quality, and dense stripes make small parts hard to read. We need "stylised and clean", not Class-A.

### 5.4 Silhouette distinctness between cars

Silhouette-based comparison is standard in 3D shape retrieval (Chen et al.'s Light Field Descriptor: 10 silhouettes from a dodecahedron, Zernike moments and Fourier coefficients; later salient-view work). Limits: silhouettes capture only exterior shape and need pose normalisation (the car is already aligned to axes).

Pipeline **[mine]**:
1. Render binary orthographic masks of each car: side, front, top, and four 3/4 views, at 256 x 256, with consistent scale (and a second set normalised by bounding box).
2. Per view, compute IoU between every pair of cars. Per pair: `distance = 1 - mean IoU over views`.
3. Add OpenCV Hu-moment distances (`cv2.matchShapes` / `HuMoments`) as a scale- and rotation-insensitive second metric.
4. Add a **side-profile height function** `h(s)` sampled at 64 points (hood height, roof peak position, tail slope) with an L2 or DTW distance. This is the feature a player actually notices in a driving camera.
5. **Rule:** each new car must exceed a distance threshold against its nearest neighbour in at least two of the three orthographic views, and the fleet's distance matrix must show no cluster tighter than the threshold.
6. **Calibrate** the threshold on existing cars: pairs that Richard called "too similar" (the doc 66 problem: four Tuners converging) set the lower bound; known-different class pairs set the upper bound.
7. Optional: an embedding distance (CLIP or LPIPS on 3/4 renders) as a perceptual check **[mine, untested]**.

### 5.5 UV and livery stretch

For any mesh that has real UVs (lights, wheels, interior), use the standard parameterisation measures from the benchmark and survey literature:

- Per-triangle **symmetric Dirichlet** energy (Smith and Schaefer 2015), area-weighted, plus worst-case.
- **Angle discrepancy** between 3D and UV triangles (conformality), singular values of the Jacobian `sigma1`, `sigma2` (ideal 1, 1).
- **Area distortion** via `J2 = sigma1^2 * sigma2^2`.
- Count of **flipped or collapsed triangles**. The benchmark gives collapsed (zero-UV-area) triangles the maximum penalty so averages cannot hide them.
- Report histograms, not just means. Averages hide outliers.

For the **livery space** (no UVs), the same Jacobian idea applies with `(s,t)` as the parameter domain:

1. For each paintable face, compute the 2x2 Jacobian of `(s,t)` with respect to the face's tangent-plane coordinates, in metres per livery unit.
2. Flag faces with anisotropy `sigma1/sigma2 > 2` and area scale outside a band, and report the fraction of paintable surface that fails.
3. **Render test.** Apply a fixed test livery (parallel stripes, a grid, a big number) to the car, render the side, top and 3/4 views, and measure the variation in stripe width and the amount of smearing near arches. This catches problems the Jacobian misses at zone blends.

### 5.6 LOD and budget checks

- LOD silhouette IoU vs LOD0 above 0.98 (LOD1) and 0.95 (LOD2) in the three orthographic views **[mine, thresholds to calibrate]**.
- Symmetric Hausdorff distance from each LOD to the LOD0 limit surface below 1.5 cm for LOD1 **[mine]**.
- Triangle and material counts per LOD within the budget table.
- Wheel-to-arch clearance, left/right symmetry (if the recipe is symmetric), ride-height sanity.
- **Golden-image regression:** contact sheets compared with SSIM against approved baselines.

---

## 6. Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Cage topology templates take longer than expected (corners with valence 3 or 5 landing on visible highlights) | High | Medium | Build the QC harness and a three-panel prototype first (hood, fender, door), including a valence-3 corner. Keep part templates small and reuse a single corner template. |
| Subdivision crease sharpness differs between levels and Blender versions | Medium | Medium | Fix levels per LOD; calibrate Blender crease values empirically; build LODs by coarsening and projection onto LOD0 rather than by lowering the level. Pin the Blender version. |
| Panel seams show shading kinks at joins | Medium | Medium | Ribbon rows for tangent continuity; seam-normal QC (section 5.3). |
| Thin separate shells (wings, buttresses, floating roof) show light leaks, z-fighting or intersection errors | Medium | Medium | Required overlap with QC; minimum thickness; solid caps. |
| Livery warp makes distorted text on unusual bodies (very short wheelbase, very high roof) | Medium | Medium | Warp limits (clamp the stretch of each segment); the QC stretch metric; per-car override of landmarks. |
| Fragment-shader cost of the warp + atlas | Low | Low | One 1D LUT sample per axis plus one atlas sample; vertex-baked variant for LOD2. |
| Zone seams at 45 degrees show breaks in stripes | Medium | Medium | Design liveries with zones in mind (stripes cross the side/top boundary on the shoulder line); blend band; optional shoulder-line "wrap" zone. |
| Bounding the generator to the "toy look": complex shapes drift away from the art direction | Medium | High | Bevel vocabulary and curvature budget enforced by QC; review sheets using the existing `review/` workflow. |
| Python tooling gaps (self-intersection tools need CGAL-backed libraries that can be painful to install on Windows) | Medium | Low | Wrap the detection behind one function; fall back to voxelised overlap detection. |
| Custom car-paint shader must keep project conventions (`_LIGHT_COOKIES`, graded colours) | Low | Medium | Start from the existing world shaders and `TrialWorld.Material` grading. |
| Public evidence on how commercial racing games map liveries is thin | n/a | Low | The recommendation rests on engineering reasoning plus the Hutch, Unity and patent sources, not on reverse-engineered game internals. |
| Licence and availability of research datasets (DrivAerNet++) | Low | Low | Use only as reference; check the licence before bundling anything in the repo. |

---

## 7. Suggested order of work (rough effort in days is my estimate)

1. **QC harness (3 to 5 days).** Topology, silhouette-distance, seam-normal checks, run against the existing hand-built cars to calibrate thresholds.
2. **Cage + subdivision pipeline with a three-panel prototype (3 to 5 days).** Hood, fender-with-arch, door with a scoop. Prove seams, creases, LOD1 coarsen-and-project.
3. **Part library (10 to 15 days).** Remaining body, greenhouse, floating roof, wing, splitter, intakes, diffuser, using the shared seam skeleton.
4. **Livery space + shader (4 to 6 days).** Landmark warp, paint-class mask, atlas composite, MSDF numbers, stripe and number tests.
5. **Port two or three existing cars (for example Comet, Hoshi) as regression, then add supercars.** Run the `--area-audit`-style checks and the keep/reject discipline in CLAUDE.md. Gate the new body path behind an off-by-default flag as the project rules require.

---

## 8. References

**Subdivision, creases, Blender**
- OpenSubdiv documentation and overview: https://graphics.pixar.com/opensubdiv/docs/intro.html and https://graphics.pixar.com/opensubdiv/architecture.html
- OpenSubdiv `Sdc::Crease` reference (semi-sharp decay per level, sharpness rules): https://graphics.pixar.com/opensubdiv/docs/doxy_html/a01312.html
- Blender commit, updated Geometry Nodes Subdivision Surface node (crease field 0 to 1): https://developer.blender.org/rB88d295f9
- Blender commit, vertex crease input: https://developer.blender.org/rB1a98bec40ec
- Blender manual, Subdivision Surface node: https://docs.blender.org/manual/ko/3.4/modeling/geometry_nodes/mesh/subdivision_surface.html

**Surfaces, SDF, splines**
- Inigo Quilez, smooth minimum: https://iquilezles.org/articles/smin
- Carrera et al., Dual Contouring of Signed Distance Data: https://arxiv.org/html/2604.00157v1 and https://gatc.cs.columbia.edu/projects/dual-contouring-of-signed-distance-data.html
- Isosurface polygonisation overview (marching cubes, surface nets, dual contouring): https://people.compute.dtu.dk/janba/gallery/polygonization.html
- Sederberg et al., T-splines and T-NURCCs (SIGGRAPH 2003): https://history.siggraph.org/?p=115890 and review https://link.springer.com/doi/10.1186/s42492-019-0010-0
- Gordon surfaces for curve networks (CAESES): https://www.caeses.com/2019/04/
- OpenCASCADE Gordon fill: https://dev.opencascade.org/doc/refman/html/class_geom_fill___gordon.html
- Coons-patch car body add-on for Blender (commercial): https://superhivemarket.com/products/surface-mesh
- Geometry Nodes NPR car generator: https://blenderartists.org/t/procedural-npr-car-generator/1645186

**Procedural and learned car design**
- Orsborn, Cagan et al., parametric shape grammar for cross-over vehicles: https://scholarsmine.mst.edu/mec_aereng_facwork/1185
- Orsborn, Cagan, Boatwright, statistically derived vehicle shape grammar: https://scholarsmine.mst.edu/mec_aereng_facwork/1310
- Dornhof, procedural 3D vehicle generation with a form grammar (HAW Hamburg, 2024): https://reposit.haw-hamburg.de/handle/20.500.12738/16261
- Müller et al., Procedural Modeling of Buildings (CGA shape): https://course.ccs.neu.edu/cs5150f13/readings/muller_buildings.pdf
- DrivAerNet++: https://arxiv.org/abs/2406.09624 and https://github.com/Mohamedelrefaie/DrivAerNet
- VehicleSDF: https://www.arxiv.org/pdf/2410.18986
- Multi-agent AI car design with DeepSDF: https://arxiv.org/html/2503.23315v1

**Liveries, decals, text**
- Hutch, car decal painting technical test: https://www.hutch.io/blog/tech/car-decal-painting-technical-test/
- Unity 6 Decal Renderer Feature reference: https://docs.unity.com/en-us/engine/6000.6/manual/visual-effects/decals/renderer-feature-decal/reference.md
- Unity 6 Decal Projector reference: https://docs.unity.com/en-us/engine/6000.6/manual/visual-effects/decals/renderer-feature-decal/projector-reference
- Unity issue tracker, projected decals stretch on curves or edges: https://issuetracker.unity3d.com/issues/15719/the-projected-decals-contain-stretch-like-artifacts-when-used-on-curves-or-edges
- Unreal Engine, mesh decals: https://dev.epicgames.com/documentation/en-us/unreal-engine/using-mesh-decals-in-unreal-engine
- Forza Horizon 5 livery editor: https://www.gtplanet.net/forza-horizon-5s-new-livery-editor-revealed/
- Forza Horizon 3 painting and vinyls: https://support.forza.net/hc/en-us/articles/360005306774-FH3-Painting-Designs-and-Vinyls
- Forza Motorsport 3 livery editor impressions: https://gamespot.com/articles/forza-motorsport-3-updated-impressions-livery-editor-and-storefront/1100-6216859/
- Rocket League decal compatibility (Epic): https://www.epicgames.com/help/rocket-league-c-37599050/a11663275
- Roblox patent, local space texture mapping based on reverse projection: https://patents.google.com/patent/US12597197
- Valve, Improved Alpha-Tested Magnification (SDF text): https://cdn.akamai.steamstatic.com/apps/valve/2007/SIGGRAPH2007_AlphaTestedMagnification.pdf
- Chlumsky, msdfgen: https://github.com/Chlumsky/msdfgen and msdf-atlas-gen: https://github.com/Chlumsky/msdf-atlas-gen
- Chlumsky, Sloup, Šimeček, Improved Corners with Multi-Channel Signed Distance Fields: https://diglib7.eg.org/items/cdf15fe4-bfbc-4a8a-b70e-79b887b2dc64/full

**Look, budgets, QC**
- BeamNG vehicle creation workflow (geometry vs normal-map approaches): https://wiki.beamng.com/Vehicle_Creation_Workflow
- Unity forum, poly count for a mobile racing game: https://discussions.unity.com/t/what-would-be-an-ideal-poly-count-for-a-mobile-racing-game/673701
- Rhino, surface continuity: https://www.rhino3d.com/docs/guides/modeling/rhino-continuity-surfaces/
- Zebra analysis for continuity: https://novedge.com/blogs/design-news/rhino-3d-tip-using-zebra-analysis-to-evaluate-surface-continuity-in-rhino-3d
- Mesh parameterisation benchmark: https://arxiv.org/pdf/2208.01772
- Metric optimisation in Penner coordinates (area and shape distortion measures): https://arxiv.org/pdf/2206.11456
- 3D shape retrieval with silhouettes and Hu moments: https://arxiv.org/pdf/1111.1752 and https://pyimagesearch.com/2014/10/27/opencv-shape-descriptor-hu-moments-example/
- libigl self-intersection node (CGAL-backed detection, via a ComfyUI wrapper): https://www.runcomfy.com/comfyui-nodes/ComfyUI-GeometryPack/geom-pack-detect-self-intersections

**Project files read for context (not modified)**
- `docs/66-car-builder-v2.md` (why every car converged; the no-boolean cabin)
- `source-art/regale_livery.py` (current 3-view livery atlas, 0.20 UV per metre, graded colours)
- `source-art/build_family_cars.py`, `source-art/export_unity.py`
