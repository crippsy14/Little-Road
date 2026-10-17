# Sub-D kernel techniques for the next Forge engine

Date: 13 Oct 2026. Companion to `docs/75-car-forge-spec.md` (§4) and `docs/research/75b-car-modelling-methods.md`. Those two already decided: one quad patch per panel, patches welded on shared seam curves, edge tags (smooth / character / hard / gap), no booleans, separate shells for aero parts, LODs by coarsen-and-project. This note does not revisit them. It answers the next level down: how to build the patches, join them, place poles, set sharpness, cut gaps and arches, and measure the result.

**How to read the evidence marks.**
- **[B5]** means I measured it myself in Blender 5.2.2 LTS (headless, the copy installed on this PC) on 13 Oct. The method is in Appendix A. Re-run it on any Blender upgrade.
- **[src]** means I read it in a page or source file this session. The URL is in the reference list.
- **[mem]** means it is from my background knowledge of the literature and was not re-fetched. Check it before relying on it.
- **[inference]** is my own reasoning or extrapolation.
- **[verify]** is a detail I could not confirm, such as a package API or licence.

---

## 0. Executive summary

1. **Patch interior = Coons with ribbons, plus a shape term, then a light bi-Laplacian fair.** Build each panel as a Boolean sum (Coons) of its four seam curves with cross-boundary tangent ribbons. Add the interior shape (crown, tumblehome, power bulge) as a displacement along the patch normal, scaled by a window function. Then solve a small sparse bi-Laplacian system, with boundary row and ribbon row fixed. Solve it with `scipy.sparse`. A 30 x 30 patch is 900 unknowns, so it costs milliseconds.
2. **Parametrise by stations, not by arc length.** Give every longitudinal seam the same station table (axles, pillars, door cuts). Isoparms then stay in vertical planes and the grids stay square.
3. **Smooth seams are a cage rule, not a continuity trick.** A Catmull-Clark surface is C2 across any seam made of valence-4 vertices. What makes a seam look smooth is that the cage rows either side are collinear, evenly spaced (grade ratio 0.7 to 1.4) and share one curvature. A cage kink of 90 degrees is not a seam defect. It is a radius: **a kink with holding rows at distance d each side gives an apparent radius of 0.57 d** [B5]. Build the ribbon so that the kink is zero for `smooth` and deliberate for `character`.
4. **Compensate the cage for the limit surface.** Catmull-Clark pulls vertices inwards. Fit the cage so its limit positions hit the seam samples. Use the limit stencils (4/9, 1/9, 1/36 in the interior; 1/6, 4/6, 1/6 along a seam). Two or three Jacobi sweeps are enough.
5. **One bevel family, driven by the seam's first-row width.** Radius R at a seam with mean first-row width w is: crease only if R/w is at most 0.45 (use table 4.2); a plain holding row at d = R/0.57 above that. Use `use_limit_surface = True` in Blender. It makes the crease radius independent of subdivision level [B5]. Hard means crease exactly 1.0. Values from 0.75 to 0.95 are not sharp in limit mode [B5].
6. **Gaps are built from the same seam samples.** Each `gap` seam gives two offset curves (one per panel), a floor curve, and a three-quad strip (wall, floor, wall). Defaults for the toy scale: width 10 mm, depth 1.5 times the width, lip radius at most 0.3 times the width, held by a plain row at d = R/0.57. Terminate channels by tapering the depth to zero over three samples, or end them on a boundary.
7. **Poles go where nobody looks.** Allowed sites are panel corners, hard or crease corners, the middle of a flat area, and anything hidden behind a lens, grille or liner. Forbidden sites are belt and shoulder lines, the arch lip, and any ridge a highlight runs along.
8. **Arches are O-grids.** A wheel arch is three annular patches (front, top, rear) around a half circle. The rim is a real edge loop shared with an arch-liner shell. Flares are extra rings on the rim loop, or a separate bolt-on shell.
9. **Recesses are knot lines.** Insert the rectangle's four edges as grid lines of the host patch (non-uniform grid), displace the interior along the normal, and crease the rim. Round pockets use a square core with four valence-3 poles hidden behind the lamp.
10. **Own Catmull-Clark in numpy, cross-checked against Blender.** About 200 lines. It gives the QC loop a limit surface without Blender, and a differential test against Blender catches rule differences (creasing method, boundary mode).
11. **QC uses limit-surface curvature on the patch grid.** Mean and Gaussian curvature per vertex, curvature along section lines, and seam-crossing normal and curvature jumps. Proposed thresholds are in §10. They are [inference] until calibrated on the Tuner family.
12. **Test with properties, not pictures.** Watertight, manifold, symmetric, deterministic, seam arrays bit-identical, Euler characteristic, and a numpy-versus-Blender limit comparison. Golden meshes are hashes of rounded arrays plus a Chamfer tolerance.

---

## 1. Building a quad patch from boundary curves

### 1.1 Methods

**Discrete Coons / transfinite interpolation (TFI).**
Given four boundary curves sampled on an (m+1) x (n+1) grid, the Boolean-sum patch is:

```
S(u,v) = (1-v) C0(u) + v C1(u)            # ruled between bottom/top
       + (1-u) D0(v) + u D1(v)            # ruled between left/right
       - bilinear(corners)                # remove the double count
```

This is the classic Coons patch [mem: Coons 1967; Farin and Hansford, "Discrete Coons patches", CAGD 1999]. In grid generation it is called transfinite interpolation and is the standard starting grid for elliptic smoothing [mem: Thompson, Warsi, Mastin; Knupp and Steinberg].

- **Strength.** It interpolates the boundary exactly. It is a one-line numpy expression over the grid.
- **Pitfall 1.** It knows nothing about the cross-boundary tangents. Two neighbours built this way meet with an arbitrary crease. This is fixed by ribbons (§1.3).
- **Pitfall 2.** The interior is a blend of the boundaries. If the belt line is concave and the shoulder line convex, the interior wobbles or goes flat in the wrong place.
- **Pitfall 3.** If opposite boundaries are parametrised by arc length, features do not line up. A wheel centre at 30 % along the belt line is at 34 % along the shoulder line. Isoparms then shear.

**Gordon surfaces.**
A Gordon surface interpolates a whole network: m cross-sections and n rails [mem: Gordon 1969, "Spline-blended surface interpolation through curve networks"; CAESES and OpenCASCADE `GeomFill_Gordon` in refs]. Discretely:

```
G = Lu(sections) + Lv(rails) - Tensor(intersection points)
```

with Lu, Lv blended by cardinal functions (Hermite or B-spline).

- **Strength.** The interior follows the section curves, so cross-section families (round, wedge, coke-bottle) are honoured everywhere, not only at the edges.
- **Pitfall.** It needs compatibility: every section must pass exactly through its rail points. Free sections never do. [inference] Make the sections compatible by construction (§1.3), not by fitting.

**Laplacian and bi-Laplacian fairing.**
Minimise a discrete thin-plate energy over interior grid points with boundary rows fixed [mem: Kobbelt, "Discrete fairing" 1997; Botsch and Kobbelt, "An intuitive framework for real-time freeform modeling", SIGGRAPH 2004]:

```
minimise  || L (L x) ||_W^2  + lambda || S (x - x0) ||^2
          x = boundary rows and ribbon rows fixed
```

- L is the grid Laplacian with parameter-space spacing normalised to chord length (not the cotan Laplacian, which is for triangles).
- The k=2 (bi-Laplacian) case gives tangent-continuous blending with the fixed boundary rows. A plain Laplacian (k=1) gives a minimal-surface soap film and sags.
- libigl has `igl.harmonic(V, F, b, bc, k)` and `igl.min_quad_with_fixed` [verify they are exposed in the pip wheel]. For a structured grid it is simpler and faster to build L with `scipy.sparse` and solve with `splu`.
- **Pitfall.** The pure fair surface forgets the intended shape. The term lambda ties it to x0 (Coons plus shape). Typical lambda is 0.02 to 0.1 [inference].

**Isoparm flow: tangential relaxation.**
After the above, a few sweeps of tangential Laplacian smoothing of interior vertices (move to the neighbour centroid, remove the normal component, re-project onto the shape function) spread the cells evenly without changing the surface [inference; same idea as Winslow / elliptic smoothing in grid generation]. Check with aspect ratio (§10).

### 1.2 Making the interior follow a shape

The aim is that the interior comes from a design (crown, tumblehome, bulge), not from the boundary alone. Use a **Hermite section with ribbons**, which is a Coons patch with cross-boundary derivatives [mem: Coons-Gregory / bicubic Boolean sum in Farin, *Curves and Surfaces for CAGD*]:

```
for each station u:
    P0 = bottom_seam(u)        P1 = top_seam(u)
    T0 = ribbon_bottom(u)      T1 = ribbon_top(u)      # unit tangent into the patch, scaled by |P1-P0|*k
    base(v) = H00(v) P0 + H10(v) T0 + H01(v) P1 + H11(v) T1     # cubic Hermite, v in [0,1]
    n(u,v)  = normalised base'(v) x seam_tangent(u)
    bump(u,v) = crown(u) * 4v(1-v)                              # symmetric hump
              + bulge_amp * W(u; u0,u1,s) * W(v; v0,v1,s)       # a power bulge, windowed
    S(u,v) = base(v) + bump(u,v) * n(u,v)
```

- **Crown.** `crown(u)` is a signed height in metres, tied to the car width. A side panel with 0.02 m crown over a 0.6 m span gives the sheet-metal sag of a real door. [inference: values to tune]
- **Tumblehome.** It is not a bump. It is the ribbon at the shoulder seam: T1 points inwards and up. The Hermite section then curls over without extra parameters.
- **Power bulge.** `W` is a smooth window (smootherstep, `6t^5 - 15t^4 + 10t^3`) over a station range and a v range. Its flanks must be at least three cage cells wide, or the Hermite shape cannot hold it and it turns into a pimple.
- **Section families.** The six families in 75 §4.4 (round, square, wedge, teardrop, flat-bottom, coke-bottle) are just choices of (T0, T1, crown profile). For the nose and tail, where the section is closed, use a superellipse with a per-height exponent n(z), clamped to [2.0, 5.0], and round n to 0.25 steps.
- **Compatibility.** Because P0 and P1 are the rails, and the section is defined relative to them, the section passes through the rails by construction. That is a discrete Gordon patch with no fitting step.

### 1.3 Parametrisation

- Sample each seam once. Use a global **station table** (x positions of axles, pillars, door cuts, nose, tail). Parametrise every longitudinal seam piecewise-linearly between stations (equal cell count per interval), not by total arc length.
- Within an interval, use chord-length parameters. Make the cell count even, so a coarsen step (LOD) can halve it.
- Cell size target: `N = clamp(2 * round(L / (2 e_t)), 4, 64)`, where `e_t` is the cage edge length (8 cm for LOD0 with Catmull-Clark level 2 [inference]; revisit against the budget in 75 §6).

### Recommendation for our engine

```
class Seam:   name, N, s[0..N]  (station-aware params), P[N+1,3], T[N+1,3], Nrm[N+1,3], kind, h_ribbon[N+1]
class Patch:  corners, 4 seams (with orientation flags), nu, nv, shape_params, X[nu+1,nv+1,3]

build_patch(patch):
    X0 = hermite_section(seams, ribbons)          # §1.2, includes crown and bulge
    X  = solve_biharmonic(X0, fixed = boundary + first interior row, lambda = 0.05)
    X  = tangential_relax(X, sweeps = 3)          # keeps boundary + ribbon rows fixed
    return X
```

- Order of preference: Hermite section (default), then bi-Laplacian fair, then tangential relax. Use plain Coons only for tiny patches (under 4 x 4).
- Use a Gordon-style sum (two section families) for the sides and greenhouse where cross-sections matter. It is the same code with `sections` supplied by the section-family function.
- `scipy.sparse.linalg.splu` with the matrix pre-factored per (nu, nv, lambda) and cached. Solve three right-hand sides.
- Keep the first two rows from every seam fixed in the solve: boundary row (position) and ribbon row (tangent). This is what makes §2 work.

---

## 2. Joining patches

### 2.1 Matching seam sampling

Done already in 75 §4.2: one sample array per seam, shared. Extra rules:

- **Seam equivalence classes.** A structured patch needs equal counts on opposite sides. Build a union-find over seam names (`bottom == top`, `left == right` for each patch). Each class takes `N = max(requested)`, rounded up to even.
- **Orientation flags.** A patch stores for each side the seam name plus a `reversed` flag. Its boundary is read through the flag, not copied. Unit test: concatenated boundaries are bit-equal (`np.array_equal`).
- **Corner points.** A corner is one vertex shared by every patch that touches it, from a corner table, not recomputed per seam.

### 2.2 Tangent continuity under Catmull-Clark

Catmull-Clark gives a bicubic B-spline surface around any vertex that is regular (valence 4, and all faces quads). The limit is C2 there, wherever the vertex sits [mem: Catmull and Clark 1978; Peters and Reif 2008; Stam 1998]. So across a seam of regular vertices **tangent continuity is automatic**. The cage decides how *good* it looks.

What a "ribbon row" must therefore achieve:

1. **Collinearity** of `A_row[i]`, `S[i]`, `B_row[i]` (the "mirrored tangent" rule). Otherwise the cage bends at the seam, and the limit is a tight round, not a smooth join.
2. **Even spacing**, `|A_row - S| ≈ |B_row - S|`. Different spacing gives a curvature jump of order `(hB - hA)/(hA + hB)` [inference, from the uniform B-spline second-difference]. Keep the grade ratio in [0.7, 1.4].
3. **Shared curvature** (optional). Set the second difference across the seam, `A_row - 2 S + B_row`, to a common value `h^2 c_i n_i` where c_i is the average of the two patches' normal curvatures [inference]. This gives a G2-like cage.

```
for each smooth seam sample i:
    a = normalise(A_prelim[i] - S[i]);  b = normalise(B_prelim[i] - S[i])
    m = normalise(a - b)                     # bisector direction; points into A, mirrored into B
    m -= dot(m, T[i]) * T[i];  m = normalise(m)       # keep it perpendicular to the seam
    h = min(hA[i], hB[i]) * 1.0              # equal spacing; or h_i from the grade rule
    A_row[i] = S[i] + h*m + 0.5*h*h*c[i]*Nrm[i]
    B_row[i] = S[i] - h*m + 0.5*h*h*c[i]*Nrm[i]
```

**Cage kink as design tool.** A kink of angle theta with holding rows at distance d gives, for a uniform cubic B-spline curve, a peak curvature `kappa = 2 sin(theta/2) / (d cos^2(theta/2))`. For 90 degrees that is `2.83/d`, minimum radius `0.35 d`. For small theta it is about `theta/d` [inference: derived from S'' = P-1 - 2P0 + P1]. The measured diagonal-fit radius of a 90-degree kink is `0.57 d` [B5] (§4). Use `kappa_peak` for curvature budgets and `0.57 d` for sizing a visible radius.

**Cage shrink.** Catmull-Clark vertices move inwards on convex surfaces. For a seam of radius R and cell e, the limit sits about `e^2/(6R)` inside the cage point [inference]. At e = 8 cm and R = 1 m that is about 1 mm and does not matter. At a bumper corner (R = 15 cm) it is 7 mm and does. Apply the limit-fit of §2.4.

### 2.3 Boundary and creased seams

- `character` and `hard` seams: skip the ribbon and tag the edges (§4). Two patches meeting at a crease need no tangent matching.
- An open boundary (the cage edge of a patch at the underbody, arch liner end, or cockpit lip) uses OpenSubdiv's boundary rule. In Blender, `boundary_smooth = 'PRESERVE_CORNERS'` should correspond to OpenSubdiv's "edge and corner" mode (the boundary curve interpolates the end vertices) and `'ALL'` to "edge only" (corners round off) [inference from the names; the enum is B5, the OpenSubdiv modes are src]. Confirm with a one-quad test. Use `PRESERVE_CORNERS` for open lips and panel ends.

### 2.4 Limit-fit (pre-compensation)

Catmull-Clark limit position of a vertex (regular, valence 4):

```
p_lim = (16 v + 4 * sum(edge_nbrs) + 1 * sum(diag_nbrs)) / 36        # weights 4/9, 1/9, 1/36   [mem]
crease/boundary vertex:  (e1 + 4 v + e2) / 6
corner vertex (>= 3 creases, or hard corner):  v                       # stays put
```

To make the limit pass through target points `t`:

```
p = t.copy()
repeat 3 times:   p += (t - A p)         # A = sparse limit-stencil matrix over the cage
```

For general valence n the interior weights are `n^2 v + 4 sum(e) + sum(f)` over `n(n+5)` [mem: Halstead, Kass, DeRose 1993, "Efficient, fair interpolation using Catmull-Clark surfaces"]. Apply to: gap seams, the belt and sill lines, and any seam where the car's width or length must be exact (track width, overall length).

### 2.5 T-junctions

- **Rule 1: avoid by propagation.** Use the seam equivalence classes of §2.1. Densities of neighbours come from a shared per-seam count, so no T exists.
- **Rule 2: when two panels need different densities along a shared seam** (a fine wing, a coarse door), set `N_coarse = N_fine / 2` and use a **2:1 transition row** in the fine panel:

```
fine side:   |  |  |  |  |  |  |  |       8 edges
transition:   \/   \/   \/   \/            each coarse edge = 2 fine; two quads merge to one
coarse side: |     |     |     |          4 edges
```

  The standard quad template for 2:1 is a row with a valence-3 and valence-5 vertex pair per transition [mem: Schneiders, quadrilateral mesh transitions; Polycount advice to "use 5-poles to merge or split loops"]. In a structured grid it is cheaper to split the coarse side (Rule 1) and live with the extra density.
- **Rule 3: never leave a hanging vertex in a patch that goes to Catmull-Clark.** The `Mesh` validator rejects any edge used by 3 faces or any vertex with a free half-edge (§12).
- **Density grade.** Keep neighbouring cell edge lengths within a ratio of 1.5 : 1 across a smooth seam, or the curvature jump in §2.2 shows in reflections.

### Recommendation for our engine

- One `SeamTable` (dict of `Seam`), built before any patch. Patches read, never write.
- Ribbon pass between "provisional patch" and "final patch": build every patch once with free interior rows; compute bisectors on all `smooth` seams from the two provisional rows; refix and re-solve each patch with the ribbon rows fixed. Two passes are enough for a car [inference].
- Ribbon spacing `h = 0.9 * min(cell_A, cell_B)` perpendicular to the seam; grade ratio check in QC (fail above 1.6).
- Run the limit-fit (§2.4) on flagged seams once, at the cage stage, before tagging.
- Seam equivalence classes in the recipe compiler. Transition rows only as a manual override.

---

## 3. n-sided corners and poles

### 3.1 What practitioners say

- Poles are not bad in themselves. Their *placement* matters. Common advice: keep 3-poles and 5-poles off areas of tight, directional curvature, put them on flat or hidden geometry, and judge them on the subdivided mesh, not the cage [src: Shoot First topology guide; Polycount threads on automotive and hard-surface sub-D; Tripo3D practitioner guide].
- A 5-pole is the usual way to merge or split edge loops. A 3-pole is the usual rounded-box corner.
- Many car modellers build the whole body as one continuous surface first, with support loops, and cut the panel lines afterwards, so reflections flow from panel to panel [src: Polycount comment 2739867 summary]. We do the opposite (patches first), so we must reproduce that flow with ribbons.

### 3.2 Where the poles fall on a car

| Junction | Panels meeting | Valence at the vertex | Where to put it |
|---|---|---|---|
| Scuttle corner (A-pillar base, bonnet rear corner, wing top) | bonnet, wing, scuttle / A-pillar base | 3 or 4 | On the crease or gap end. Hidden by the shut line. |
| Bumper corner | nose face, side/wing front, valance or bonnet | 3 | The rounded corner of a box. Equal-length incident edges. |
| Wheel arch lip, where wing meets nose or door | wing, arch tunnel, neighbour | 3 | End of the arch lip crease. |
| Roof corner (C-pillar top) | roof, side glass, C-pillar | 3 | On the roof edge crease. |
| Lamp pocket | host panel, lamp recess | 3 or 5 (core) | Inside the pocket, behind the lens. |
| Loop merging in a large flat | door, side | 5 + 3 pair | Door centre, away from the belt and the scoop. |

### 3.3 Corner templates for a generator

**Template T3: tri-corner (rounded box corner).**
Three patches share one cage corner. Valence 3. Use it where a convex corner is meant to read as a rounded block, such as a bumper corner or roof corner.

```
        edge A (hold d)
          \
           o  <- valence-3 vertex, pushed out by 0.08 * mean edge length
         /   \
 edge B       edge C       all three incident edges the same length +-10 %
```

- Cage: pole vertex plus one hold row on each of its three incident edges, at the same distance `d`. The result is a spherical-octant corner of radius about `0.57 d`.
- If the three edges are hard creases (infinite), the vertex is a corner and stays put: a sharp box corner. If they are semi-sharp, the corner rounds with radius per §4.
- Pole push-out is to offset Catmull-Clark's pull-in at valence 3 [inference]. Tune by looking at a reflection sheet.

**Template T5: five-valence loop merge (flat area only).**
Where two edge loops need to become one. Put it on a flat area with the 3-pole of the pair within 2 cells. Do not put it on a ridge or the lip of an arch.

**Template T4C: four panels on a creased corner.**
Valence-4 vertex where two crease lines cross. No pole, no problem.

**Template TG: gap end.**
Gap channel ends on a vertex where the depth tapers to zero over three samples. The pole (if any) sits at the zero-depth end.

### 3.4 Avoiding highlight pinches

[inference unless noted]

1. **Equal incident edge lengths** at every pole, within 10 %. Unequal lengths make the surface lopsided.
2. **Hold row near the pole**, at about 1.0 cell. A pole with no hold row on a gentle curve makes a visible dimple or star.
3. **Keep Gaussian curvature sign consistent at the pole.** A 3-pole on a saddle, or a 5-pole on a dome, is the usual cause of pinch. Catmull-Clark is C1 but not C2 at an extraordinary vertex, so curvature can jump there [mem: Reif 1995; Peters and Reif 1998].
4. **Move the pole to flat areas by moving the cell boundaries**, e.g. shifting the cell where the loop merges 1 or 2 cells along the seam.
5. **Use QC (§10.4) to find them.** Flag any extraordinary vertex whose neighbourhood has `|H|` more than 2.5 times the ring median or where `K` changes sign across the one-ring.

### Recommendation for our engine

- The recipe compiler marks every cage vertex with valence != 4 as a **pole** with a `site` class: `corner`, `crease_corner`, `flat`, `hidden`. It refuses any pole with site `ridge`.
- Corner templates T3, T4C, TG in code. T5 only from named layout presets (door merge, bonnet vent).
- QC reports poles per square metre by region. Budget: at most 2 visible poles on the bonnet or roof, none on belt or shoulder lines.

---

## 4. Sharpness control

### 4.1 How OpenSubdiv treats sharpness

- Semi-sharp creases are values from 0 to 10. Zero has no effect. Ten (or more) is infinitely sharp [src: OpenSubdiv documentation of semi-sharp creases].
- With the **uniform** rule, a semi-sharp edge or vertex is decremented by 1.0 per subdivision level, and an edge or vertex sharp from the start stays sharp. The fractional part at the last step is a blend weight between the sharp and smooth rules [src: `sdc/crease.h`: `decrementSharpness`, `ComputeFractionalWeightAtVertex`; mem: DeRose, Kass and Truong 1998].
- The **Chaikin** rule computes each child edge's sharpness from all sharpnesses around the shared vertex. OpenSubdiv's own documentation calls it little known and of limited effectiveness, to be used with caution [src]. We use the **uniform** rule, and test that Blender does too (§12).
- Vertex rule by creased edges at the vertex: 0 or 1 creased edges: smooth (dart). 2: crease. 3 or more: corner [mem: Sdc `Rule`].
- Boundary handling: boundary edges are infinitely sharp; whether boundary corners are also fixed depends on the boundary interpolation option [src].

### 4.2 How Blender exposes it

- **Edge crease** is a float edge attribute named `crease_edge`, 0 to 1. Vertex crease is `crease_vert`. The old `MeshEdge.crease` property was removed in 4.0 [src: Blender patch 104709; bug 101401]. Python helpers: `mesh.edge_creases_ensure()`, `mesh.edge_creases`, `mesh.vertex_creases_ensure()`, `mesh.vertex_creases` [B5 lists all six names in 5.2.2].
- In `bmesh`, `bm.edges.layers.float.new("crease_edge")` creates the layer, and `e[layer] = value` sets it. This works in 5.2.2 and the crease is honoured by the modifier after `bm.to_mesh()` [B5].
- Modifier properties in 5.2.2 [B5]: `levels`, `render_levels`, `quality` (int), `boundary_smooth` (`'PRESERVE_CORNERS'` or `'ALL'`), `uv_smooth` (six enum values), `use_creases`, `use_custom_normals`, `use_limit_surface`, `subdivision_type` (`'CATMULL_CLARK'` or `'SIMPLE'`), `use_adaptive_subdivision`.
- **`use_limit_surface = True` places output vertices on the limit surface.** This is the setting that makes the result level-independent (below).

### 4.3 What radius a crease or holding row gives

All figures are for a 90-degree convex edge on a closed box, measured as the radius of the circle through the point where the 45-degree diagonal meets the evaluated slice [B5]. `w` is the width of the first cell row either side of the edge. `d` is the distance of a holding row from the edge.

**Holding rows (no crease).** `r = 0.569 d` at the limit, at every `d` I tried (0.02 to 0.5 on a half-width-1 box). Mesh output without limit placement gives: level 1, 0.427 d; level 2, 0.533 d; level 3, 0.560 d; level 6, 0.569 d.

**Crease value to radius (limit surface, w = 0.5, mesh at level 6).** [B5]

| crease c | r / w | | crease c | r / w |
|---|---|---|---|---|
| 0.0 | 0.569 | | 0.5 | 0.106 |
| 0.1 | 0.540 | | 0.6 | 0.048 |
| 0.2 | 0.455 | | 0.7 | 0.015 |
| 0.25 | 0.391 | | 0.8 and above | 0 in mesh mode |
| 0.3 | 0.313 | | 1.0 | 0 (infinite) |
| 0.4 | 0.199 | | | |

**Level dependence without limit placement.** At c = 0.5, r/w is 0 at level 2, 0.053 at level 3, 0.093 at level 4, 0.103 at level 5 and 0.106 at level 6. At c = 0.3 it is 0.245, 0.296, 0.309, 0.312, 0.313. So a semi-sharp crease changes character with level. This confirms the 75b reason for not simply lowering the level for LODs.

**With `use_limit_surface = True`** the radius is the same at levels 1, 2 and 3 for every c tried (e.g. 0.107 w at c = 0.5) [B5]. This is why we use it.

**Caution: the 0.75 to 0.95 band.** In limit mode, c = 0.75, 0.9 each gave a residual radius of 0.071 w, not zero. Only c = 1.0 gave exactly zero [B5]. [inference] Blender seems to treat 1.0 as infinite and anything below as a finite sharpness. So: `hard` is exactly 1.0, `character` is 0.2 to 0.5, and avoid 0.6 to 0.95.

**Mixed face sizes.** In a 6 mm gap with an 8 mm wall and a 27 mm top face, c = 0.5 gave a lip radius of 1.5 mm. The table predicts 0.106 w, so w is about 14 mm. That is between the harmonic mean (12.3 mm) and the arithmetic mean (17.5 mm) of the two face widths [B5]. Use the arithmetic mean and accept an error of about 20 % [inference].

**Cage-kink peak curvature.** From the B-spline second difference (§2.2), the minimum radius at a 90-degree kink is `0.35 d`. The apparent radius `0.57 d` is the diagonal-fit one.

### 4.4 One consistent bevel family

The 75 spec has three sizes: about 2 cm, 5 cm and 12 cm. Define a function that turns a target radius `R` and a seam into cage data:

```
def bevel(R, w):                       # w = mean first-row width either side (m)
    if R <= 0.45 * w:                  # small radius: crease only
        c = invert_table(R / w)        # table 4.3, monotone; clamp to [0.05, 0.5]
        return crease=c, hold_rows=None
    elif R <= 0.57 * w * 1.1:          # medium: plain edge, no crease
        return crease=0.0, hold_rows=None   # the cell itself is the radius
    else:                              # large: insert holding rows
        d = R / 0.57
        return crease=0.0, hold_rows=d
```

- For the 2 cm bevel on an 8 cm cage, R/w = 0.25, so c is about 0.35.
- The 5 cm bevel on 8 cm cells is R/w = 0.63. Hold rows are needed unless the local cell is 8.8 cm. Use `d = 0.088`.
- The 12 cm bevel needs `d = 0.21`. This is a wide strip, so it is a design feature (the shoulder roll), not a detail.
- Keep **character lines** at c from 0.2 to 0.5, with the same function. They are then radius-labelled, e.g. `character:2cm`.
- Keep `hard` exactly 1.0, and only on edges that are really sharp (shut lines, the grille opening corners, vent slots).
- Never vary c along a seam by more than 0.1 per cell. A ramp in c along an edge changes the radius smoothly. A step gives a visible bead.

### Recommendation for our engine

- Tag values: `smooth=0`, `character=bevel(R, w)` per seam sample, `hard=1.0`, `gap` handled by §5.
- Always set `use_limit_surface=True`, `use_creases=True`, `levels=render_levels=2` for LOD0, and `boundary_smooth='PRESERVE_CORNERS'`.
- Keep a calibration test: rebuild the box of Appendix A on each Blender update and assert the table within 5 %.
- Write the numpy Catmull-Clark (§11) with the uniform rule and match Blender's by the differential test.

---

## 5. Panel gaps in sub-D

### 5.1 What modellers do

- **Build the body whole, then cut.** Most tutorials keep the body as one smooth shell with support loops, then cut the panel lines and add support loops round them, which keeps the reflections flowing from panel to panel [src: Polycount thread summaries].
- **Recipes that appear:** split the panel; thicken or extrude the cut edge; bevel; inset the edge loop and turn the inset inwards to form the wall; fill the bottom so light does not leak; give the gap faces their own UV island [src: Polycount].
- **Non-destructive tools** (Panel Cutter for Blender) add support loops by raising bevel segments to 4 [src].
- **NURBS practice.** Cut with curves projected from several views and set the splitting surfaces so the gap direction runs from the car's centre outwards [src: McNeel thread, Audi Avus concept].

### 5.2 Size relationships

Measured in Blender 5.2.2 [B5]: a 6 mm wide, 8 mm deep gap in a slab. Lip radius:

| Lip treatment | Lip radius (mm) |
|---|---|
| no support, c = 0 | 7.7 (far too round; the gap nearly vanishes) |
| no support, c = 0.4 / 0.5 / 0.6 | 2.8 / 1.5 / 1.0 |
| support row 1.5 mm from lip, c = 0 | 0.85 |
| support row 3.0 mm from lip, c = 0 | 1.71 |
| support row 1.5 mm, c = 0.5 | 0.16 |
| any, c = 1.0 | 0 |

The plain support row matches the holding law, `r = 0.57 d` (0.855 and 1.71). A creased floor corner made no measurable difference to the depth at the centre, which stayed at 8.0 mm.

Practical rules [inference, with sources for the general idea]:

- **Width.** Real gaps are 3 to 8 mm. A toy car seen at 8 m (75 §4.4: minimum feature 3 cm) would lose them. Use **10 mm** as the LOD0 default, range 6 to 14 mm.
- **Depth.** `1.5 to 2.0 x width`. The wall must be deep enough that the floor reads as a dark line.
- **Lip radius.** `R <= 0.3 x width`. A radius of half the width closes the gap visually.
- **Support row.** `d = R / 0.57`. For a 10 mm gap and R = 3 mm that is d = 5.3 mm.
- **Floor corners.** Hard (1.0). They are never visible; the sharp corner keeps the floor flat.

### 5.3 Procedural gaps on welded patches, no booleans

A `gap` seam is a seam curve S (shared samples), flagged with `(width g, depth h, lip R)`.

```
for each sample i on S:
    m_i = cross-seam tangent in the panel plane   (bisector of the two panels, as §2.2)
    n_i = shared surface normal at S[i]
    SA[i] = S[i] + (g/2) * m_i         # A-panel's boundary (A is on the +m side)
    SB[i] = S[i] - (g/2) * m_i
    FA[i] = SA[i] - h * n_i  - (taper) * m_i      # floor corner at A, taper 0..0.1 g inwards
    FB[i] = SB[i] - h * n_i  + (taper) * m_i
panel A boundary  := SA          panel B boundary := SB      (not S)
strip patches:    wallA = quad(SA, FA)   floor = quad(FA, FB)   wallB = quad(FB, SB)
tags:   SA, SB  -> hold rows in the panels at d = R/0.57   (plain, no crease)
        FA, FB  -> hard (1.0)
        wall-to-floor edges along the strip -> smooth
```

- Every gap vertex is computed from the same sample arrays, so the channel is watertight by construction.
- The panels are *generated against SA and SB*, not against S. That means the seam table stores both curves and the panel generator reads whichever applies.
- **Gap ends.** Taper `h` and `g` to zero over three samples at an open end, or end the channel in the tail of another gap (T junction): the arriving channel's floor meets the through-channel's wall. For a cross, use a central floor quad with four arms. Prefer to design layouts so gaps run in closed loops or end on the underbody boundary.
- **Strip density.** Three quads across (wall, floor, wall) and one quad per seam sample along. Each wall is one quad of depth h, held by the hard floor corner and the plain lip row.
- **LOD1.** No channel. A shader line from a baked per-vertex distance to the nearest gap edge (75b §4.3).

### Recommendation for our engine

- Gap defaults: `g = 0.010`, `h = 0.016`, `R = 0.003`, `d = 0.0053`.
- Gap cost: four extra rows per gap seam. Budget about 4 to 6 % of LOD0 faces on a typical car [inference].
- QC: assert every gap floor point is at least `h * 0.9` below the surface and the channel's width is within 10 % of `g` at level 2.

---

## 6. Wheel arches

### 6.1 Topology

A wheel arch is a half circle open at the bottom (the sill side). Build it as an **O-grid** (annular sector), three patches in a U around the arch:

```
          shoulder seam / bonnet edge   (top boundary of the wing)
        +--------------------------------------+
        |   [top patch: annular sector 80..100 deg]      |
   nose |  [front patch]        arch        [rear patch]  | door
   seam |   45..80 deg        (half circle)    100..135   | seam
        +------+                                   +------+
             sill / valance                   sill
```

- Rings: rim ring (radius `r_a`), then rows at `r_a + d`, `r_a + d + e1`, ... graded by 1.35 outwards. The outer ring of the three patches is the wing's U-shaped outline, which coincides with the wing's boundary seams.
- Angular count: 12 to 16 segments per 180 degrees for LOD0. Make the count divisible by 4 so the three patches split cleanly at 45 and 135 degrees [inference].
- Corner valence: the patch corners at the outer U boundary are panel corners, so valence 3 sits where three panels meet (§3.2), at a gap end. The interior radial seams are regular.
- The circle-to-U mapping is a blend between a circle and the outer polyline using a squircle-style weight, `lerp(circle(theta), outline(theta), t^1.5)`, where t is the radial row fraction. This keeps cells nearly square.
- Wing front and rear rim points on the sill side end on the sill boundary. The rim loop is a boundary of the wing patches and the arch liner.

### 6.2 Flares and lips

| Style | Cage construction | Cost |
|---|---|---|
| Rolled lip | Add three rings near the rim: lip crest (offset outwards by t), lip front (rim), lip back (inside). A small tube section, thickness t about 15 mm. Tag lip crest `character:3cm`, rim 0. | +3 rows |
| Box flare | Raise the outer rings by `rise` in a stepped profile; step rows hard (1.0); a plain row each side. | +4 rows |
| Bolt-on flare | A separate closed shell: the arch rim loop extruded outwards, with a rounded rectangular section (`a x b` cm), overlapping the host 5 to 10 mm (75 §4.2 rule 6). Optional bolts are a decal detail. | one shell |

- Flare is a **displacement on the rings**: `p' = p + n * rise * smoothstep(row)`, where the window starts from the outer ring and reaches full at the rim.
- Rolled lip radius is `t/2`. Use `bevel(t/2, w)` with the lip rows as hold rows.

### 6.3 Arch liner and inner well

- A short **sleeve** shell: a half-cylinder of radius `r_a - 0.01`, width `tyre_width + 2 * clearance` (clearance 3 to 5 cm), built from the rim loop (shared samples), plus a domed top cap. It is a separate shell tagged `unpainted`. It shares the rim loop with the wing so the join is exact.
- The sleeve needs its own few rings. Do not subdivide it. Put it in the cage as a ring of 4 x 16 quads with the outer end creased so it stays sharp where it reaches the underbody.

### Recommendation for our engine

```
def arch(wing, cx, cz, r_a, flare, lip, n_theta=16, rows=5, grade=1.35):
    rim = circle(cx, cz, r_a, n_theta)          # shared with liner
    rings = graded_rings(rim, wing.outline_U, rows, grade, squircle_t=1.5)
    patches = [annular_sector(rings, theta0, theta1) for (theta0, theta1) in splits]
    apply_flare(rings, rise=flare.rise, reach=flare.reach)
    if lip: add_lip_rows(rim, t=lip.t, tag='character')
    liner = sleeve(rim, width=tyre_w + 2*clear, depth=r_a*0.25)
    return patches, liner
```

- Defaults (toy scale, tyre radius R_t): `r_a = 1.08 R_t`, `rise = 0.012 m`, `reach = 0.08 m`, lip thickness 0.015 m, rows 5, grade 1.35, n_theta 16.
- Never delete faces and snap. The previous prototype did, and the result had visible flats.

---

## 7. Recesses and openings without booleans

### 7.1 Region as knot lines

For a rectangular or rounded-rectangle recess (lamp pocket, grille opening, intake mouth, scoop, side vent):

1. Add the region's boundary as **grid lines** of the host patch: extra rows and columns at `u0, u1, v0, v1`, plus one hold line outside. This makes the grid non-uniform, but still structured.
2. Move the interior vertices along the patch normal by `-depth * W(u,v)`, where `W` is a smooth window (1 in the middle, ramping to 0 over the wall width).
3. Tag the rim `character:R_rim`, the floor corner `hard`.
4. Floor: flat, or domed for a lamp pocket.

Window openings work the same way: the **daylight opening** is an inset loop in the greenhouse patch. The glass is a separate surface copied from the DLO loop, pushed inwards by 15 to 25 mm. The frame is the ring between the outer silhouette and the DLO. This is the existing cabin cutout (doc 66), generalised.

### 7.2 Round pockets

A circular or oval pocket in a quad grid is the classic "circle in a square":

```
   outer rectangle (host grid lines)
   +-------------------+
   |   .-----------.   |
   |  /   core 3x3  \  |     core square corners: 4 valence-3 vertices
   | |   (circle)    | |     outer corners:        4 valence-5 vertices
   |  \             /  |
   |   '-----------'   |
   +-------------------+
```

- The poles (four 3-valent core corners, four 5-valent outer corners) sit at 45 degrees. Put them behind the lens (lamp) or in the grille cavity (hidden). They must not sit on the rim lip.

### 7.3 When to use something else

| Feature | Method |
|---|---|
| Lamp pocket (round or oval) | Round pocket (§7.2), lens is a separate shell sitting 3 mm inside the rim. |
| Grille opening | Rectangular recess; the grille texture or slats are a separate inserted part. |
| Intake mouth | Recess whose floor is open: the floor becomes an `unpainted` tunnel shell; end it 10 cm inside. |
| Hood scoop | Raised region: same window but with positive displacement and a crease on the rim. |
| Side vent / strake | Recess with a long thin window; keep d >= 2 cells wide. |
| Large cut (open-air scoop) | Two shells meeting at a seam (75b §3.4). |
| Window | DLO loop + separate glass shell. |
| Tiny details (badge, mirror base, filler cap) | Separate inserted parts, ray-laid (75 §4.5). |

### Recommendation for our engine

- A `Region` object: `{kind, u_range, v_range, corner_radius, depth, wall_width, rim_tag, floor_tag}`. The patch builder adds the knot lines, then applies displacement.
- Refuse a region closer than 3 cells to a patch boundary or to another region.
- Minimum wall width: 2 cells. Minimum depth: 5 mm (it must read at LOD0 only).
- Pole placement rule (§3) applies; the `Region` templates keep their poles internal.

---

## 8. Ends: front clip and rear clip that are not flat caps

### 8.1 What went wrong in the prototype

A tube lofted nose to tail with a cap at each end gives a box front. The side view and plan view are tied together by one section.

### 8.2 The end as a wrap-around patch

Build each end as its own **wrap-around patch group**, whose boundaries are the same seams the side panels use.

```
Nose patch (u = left to right across the front, v = top edge to bottom edge):
    top edge     = bonnet front edge (curve, with crown and plan taper)
    bottom edge  = valance lower edge (curve at splitter height)
    left edge    = left nose-fender corner line   (shared with the left wing)
    right edge   = mirror
```

- **Side profile.** The centre-line (x vs z) of the nose is a curve with parameters: `rake` (angle of the leading edge, 20 to 70 degrees), `nose_height` (z of the leading edge), `undercut` (how far the valance sits behind the bumper: 0 to 0.15 m), `bumper_bulge` (outward bump of 0.01 to 0.04 m at bumper height), `lip` (splitter lip height).
- **Plan shape.** The outline in plan is a superellipse-ish curve with `corner_radius(z)`: tighter at the bumper, wider at the belt. Parameters `taper` (0 to 0.25 of width), `chin` (centre bulge ahead of the corners, 0 to 0.05 m), `corner_radius_top` and `corner_radius_bottom`.
- **Corner line.** The vertical seam between the nose face and the side wing is a smooth seam if the plan radius is large (> 0.12 m, ribbon rows), or a character crease (2 to 5 cm) if smaller. It is a shared seam, built once.
- **Valance.** A separate patch below the bumper bulge, joined by a character line, gives the undercut look.
- **Rear clip.**
  - **Notch / three-box:** the deck is a patch whose front edge is the rear window base seam and rear edge is the boot lip (crease). The tail face is a second patch below the lip. A step of 0.03 to 0.08 m at the lip gives the boot step.
  - **Kamm tail:** the tail face is near-vertical, joined to the roof/deck by a hard crease, with the tail edge radius from `bevel(0.02, w)`.
  - **Fastback / hatch:** the roof patch flows to the tail edge with a smooth seam and a small spoiler lip (character crease).
  - **Duck tail:** the deck rises in the last 15 % of its length: a Hermite section with the rear ribbon pointing up.
  - **Longtail:** the deck and quarter panels extend to a tapered tail, with pods as separate shells.
- **Plan sections.** Use the station-based parametrisation: the end patches use the last two stations, so the plan curves of the side seams are tangent to the end patch.

### 8.3 Why this fixes the flat cap

Each end has its own side profile *and* plan profile, set separately. The cap is a curved patch with corner lines and a bumper bulge, not a planar closure.

### Recommendation for our engine

- Parameter schema `EndClip`: `{kind: nose|tail, rake, height, undercut, bulge, lip, taper, chin, r_top, r_bottom, step, kamm: bool}` with defaults per section family, snapped to 5 mm and 2.5 degrees (75 §4.4).
- Rows across a nose: 12 cells; rows top to bottom: 8; valence: all 4, with poles only at the four corners where wing, bonnet, nose face and valance meet.
- Test: silhouette of the nose from the side must have at least two inflection points for any `bulge > 0` (§10.5).

---

## 9. Other topologies

### 9.1 Open roadster cockpit (tub lip)

- The cockpit opening is a **closed loop** in the body shell (an inset loop of the deck and the door tops), like a window opening.
- The **lip** is a rolled edge: three rings around the opening, thickness 15 to 25 mm, tagged `character:2cm`. This is the "tub lip".
- The **tub** is a separate inner shell welded to the opening's inner loop: a bowl with sides and a floor. Cage: 4 rings down, then a floor patch. Tagged `interior`.
- Windscreen frame and roll hoop: separate thin shells (75 §4.3).
- Practice: car modellers model the roadster as a closed body with a cockpit cut, because the sill and door tops give a continuous line [mem].

### 9.2 Canopy prototype

- The body tub is one shell with a cockpit opening (as 9.1).
- The **canopy** is a separate shell: a bubble with a cage of 6 x 10 cells, overlapping the tub's edge by 5 to 10 mm. Its lower edge follows the tub's belt line, shared samples.
- Fins and longtails: separate shells with overlaps.
- The canopy-to-tub join has a real `gap` seam (§5).

### 9.3 Open-wheel monocoque with sidepods

- Monocoque: a tube patch group built from stations (nose cone, cockpit, engine cover), not a loft. Cockpit opening as in 9.1.
- **Sidepods:** separate closed shells with a rounded-rectangle section family, tapering in plan. They overlap the monocoque by 5 to 10 mm.
- Suspension arms: simple rods. Wings: airfoil extrusions plus end plates (75 §4.2).
- Wheels are outside the body, so no arch is needed.

### 9.4 Van or pickup bed

- **Van:** a box body. Panels: roof, sides, front (windscreen and bonnet), rear doors. Sub-D on a box needs hold rows at all edges: use `bevel(R, w)`. The rear doors are two panels with a `gap` seam.
- **Pickup bed:** five panels (floor, two sides, tailgate, bulkhead) forming an open box, with a **rolled rim** (the top edges) of 3 rings, thickness 20 to 30 mm. A real gap separates bed from cab (a `gap` seam at 20 to 40 mm, larger than a door gap).
- Wheel arches inside the bed sides are bumps in the side panel (a protruding arch, using the O-grid of §6 with positive displacement) and a floor cut.

### Recommendation for our engine

- Each topology is a **template**: a list of panels, a seam table template, and a gap list. A recipe fills in numbers.
- Separate-shell rules: every extra shell must name its host, its overlap (5 to 10 mm) and its allowed contacts (the table in 75 §4.2 rule 6).
- Build order: tub, then openings, then lips, then inner shells, then extra shells.

---

## 10. Quality measurement

### 10.1 Which surface to measure

Measure the **limit surface** (or level 2 with limit placement), never the cage. Evaluate on the structured patch grid when you need section lines; evaluate on the whole mesh for global checks.

### 10.2 Curvature

[inference for thresholds; formulas are standard]

On the subdivided mesh, per vertex:

```
H  = 0.5 * |Delta x|  (signed by normal)      # mean curvature, cotan Laplacian (igl.cotmatrix + massmatrix)
K  = (2π - sum(theta_i)) / A_mixed            # Gaussian curvature by angle defect
k1,k2 = H ± sqrt(max(H^2 - K, 0))
```

libigl offers `igl.principal_curvature` (quadric fitting) [verify in wheel]. The cotan version is a dozen lines of scipy. On the patch grid, finite differences in the parameter domain are cleaner, and give the same curvature along isoparm lines.

**Checks:**

| Check | Method | Proposed fail (calibrate on the Tuner family) |
|---|---|---|
| Curvature comb on section lines | Plot `kappa(s)` along each u-line and v-line of each patch. Count local extrema and sign changes. | A panel meant to be convex has `> 2` sign changes along any line. Total variation of kappa > 3 x its mean. |
| Pinch | Vertex with `|H|` > 2.5 x the median in its 2-ring, or K sign differs from all ring neighbours | Any, away from declared poles; at poles, report with location |
| Wobble | Second difference of normal angle along an isoline: `|d^2 theta/ds^2|` above 1.5 x the median of the panel | More than 3 % of vertices of a panel |
| Seam normal jump (smooth seam) | Evaluated vertex normals either side of seam | > 2 degrees [from 75b §5.3] |
| Seam curvature jump (smooth seam) | `|H_A - H_B| / max(|H_A|,|H_B|,eps)` | > 0.35 |
| Minimum radius | `1 / max(|k1|,|k2|)` per region | Below the declared bevel R times 0.6, or below the minimum radius of the surface type (75 §4.4) |
| Curvature budget | Mean `|H|` per section family | Outside the family's band |

### 10.3 Reflection and zebra lines

- **Render route.** Blender Workbench with a stripe matcap, or a custom shader; or in numpy: for each vertex, reflect the view ray, compute `s = fract(dot(r, axis) * f)`. The stripe edges' direction should change smoothly across seams [src: Rhino and NovEdge tips for zebra analysis, via 75b].
- **Automatic check.** On the quad grid, take the isolines of `dot(r, axis)` (marching squares per patch). At each smooth seam, compare the direction of the isoline on each side. Pass: angle < 5 degrees for G1, and relative curvature of the isoline within 40 % for a G2-like pass. [inference]
- **Caution.** A zebra pass is not a Class-A pass [src: forum caution in 75b]. We want "clean and stylised". Use the stripe sheet mainly as a human review image, and let the automated checks above do the gating.

### 10.4 Mesh quality metrics

For the cage and for the level-2 mesh:

| Metric | Method | Threshold |
|---|---|---|
| Valence histogram | count per vertex | Cage: at least 94 % valence 4, rest 3 or 5. No vertex above 6. Level 2: at least 97 % valence 4. [inference] |
| Pole sites | each non-4 vertex vs its declared `site` | Any undeclared pole fails |
| Quad aspect ratio | longest / shortest edge (or Verdict) | Cage: under 4 (flat areas under 6). Level 2: under 3 |
| Quad angles | min and max interior angle | 30 to 150 degrees |
| Scaled Jacobian | Verdict metric [mem] | > 0.4 |
| Row grade | cell size ratio across a smooth seam | < 1.6 |
| Planarity | max distance of 4th vertex from the plane | Informational |
| Degenerate | area < 1e-8, edge < 1e-5 | none |
| Dihedral histogram | every edge > 30 degrees not tagged `hard` or `character` | none (75b §5.3) |

PyVista exposes the Verdict metrics through `cell_quality` on a quad mesh [verify].

### 10.5 Silhouette and inflection checks

- Side-view silhouette curvature: count of inflections on the nose and tail. A flat cap has none.
- Plan-view silhouette: same.

### Recommendation for our engine

- A `qc.geometry` module that takes `(verts, quads, edge_tags, part_ids, grid_info)` and returns a report. No Blender import.
- Two curvature estimators: cotan (any mesh) and grid finite-difference (patch lines). Cache cotan matrices.
- Calibrate thresholds on the Tuner and Regale meshes in a single script, write them to `qc_thresholds.json`, and review with Richard.
- The reflection sheet is a review artefact; the gating checks are the numeric ones.

---

## 11. Libraries (offline, Windows Python 3.11 or Blender's Python)

| Library | Use | Licence | Notes |
|---|---|---|---|
| **Blender 5.2 bpy / bmesh / mathutils** | Subdivision (OpenSubdiv), FBX export, BVH overlap | GPL-3.0-or-later (the application) | Already used. Installed here as 5.2.2. `pip install bpy` also exists as a wheel [verify the 5.x wheel and its Python version]. |
| **OpenSubdiv** (C++) | The engine behind Blender's Subsurf | Modified Apache 2.0 [mem] | **No official Python bindings found.** Searches turned up Blender's internal C API and a Rust crate only. Use Blender, or write our own CC. |
| **numpy / scipy** | Own Catmull-Clark; sparse fairing; KD-trees | BSD-3 | Core dependency. |
| **libigl (Python)** | cotan Laplacian, harmonic/biharmonic, principal curvature, AABB | MPL-2.0 for the core; the `copyleft` tree (e.g. CGAL self-intersection) is GPL [src: PyPI page and licence note] | `pip install libigl`; check `dir(igl)` for the functions you need. Docs said to be out of date. |
| **trimesh** | Watertight/winding checks, volume, sampling, simple booleans via backends | MIT (some files BSD-3) [src] | Good for QC. |
| **pymeshlab** | Filters including Catmull-Clark and quality | GPL-3 [src] | Do not link it into anything we ship. Fine as an offline experiment tool. |
| **PyGEL3D** | Half-edge mesh, simplification, distance fields | not confirmed [verify] | No Catmull-Clark found. |
| **manifold3d** | Robust boolean / solid checks | Apache-2.0 [src, via distro packages] | Use for an "is this shell a valid solid" test, not for building. |
| **PyVista / VTK** | Verdict mesh-quality metrics, plotting | MIT / BSD | [verify `cell_quality`]. |
| **scikit-sparse (CHOLMOD)** | Faster sparse solves | BSD, but needs SuiteSparse (GPL parts) | Not needed at our sizes. |
| **Hypothesis** | Property-based tests | MPL-2.0 | Dev only. |

**Blender operators worth using** (signatures read from 5.2.2 [B5]):

- `bmesh.ops.inset_region(bm, faces, use_boundary, use_even_offset, use_interpolate, use_relative_offset, use_edge_rail, thickness, depth, use_outset)`. Good for reference/visual checks and for authoring-style insets. We build our own so the shared-seam rule holds.
- `bmesh.ops.bevel(bm, geom, offset, offset_type, profile_type, segments, profile, affect, clamp_overlap, loop_slide, ...)`. Not for the engine. Useful for building reference meshes.
- `bmesh.ops.bridge_loops(bm, edges, use_pairs, use_cyclic, use_merge, merge_factor, twist_offset)`. Good for the arch sleeve and tub inner shell.
- `bmesh.ops.subdivide_edges(bm, edges, cuts, smooth, use_grid_fill, quad_corner_type, ...)`, and `subdivide_edgering(...)`. Good for adding hold rows.
- `bmesh.ops.grid_fill(bm, edges, ...)`, `create_grid(...)`. Quick patches in tests.
- `bmesh.ops.weld_verts(bm, targetmap)`, `remove_doubles(bm, verts, dist)`. Weld shared seam vertices when importing a cage (exact match, so `dist` can be 1e-9).
- `mathutils.bvhtree.BVHTree` and `.overlap()` for triangle pair candidates in the self-intersection test [verify whether `overlap` is exact or box-only; plan an exact triangle test on the candidates].
- Note: `bmesh.ops.face_split_edgenet` is absent in 5.2.2.

**Blender build step** (minimal, no operators needed):

```python
me = bpy.data.meshes.new("body")
me.from_pydata(verts, [], quads)
me.update()
cr = me.attributes.new("crease_edge", 'FLOAT', 'EDGE')      # or me.edge_creases_ensure()
cr.data.foreach_set("value", crease_per_edge)                # order by me.edges
ob = bpy.data.objects.new("body", me)
m = ob.modifiers.new("sd", 'SUBSURF')
m.levels = m.render_levels = 2
m.use_limit_surface = True; m.use_creases = True
m.boundary_smooth = 'PRESERVE_CORNERS'
```

Mapping from your edge list to `me.edges` indices needs the `(min(a,b), max(a,b))` key. [inference] The bmesh route with `bm.edges.layers.float.new("crease_edge")` is shorter and was tested [B5].

**Own Catmull-Clark in numpy (about 200 lines).** Rules from Pixar [src, mem]:

```
face point  F = mean(face verts)
edge point  E:  smooth = (v1 + v2 + F1 + F2)/4;  sharp = (v1 + v2)/2
                s = sharpness; E = lerp(smooth, sharp, clamp(s, 0, 1))      # s >= 1 -> sharp
                boundary edge: sharp
vertex point V:  creased incident edges k (s > 0):
                k <= 1 : smooth   (F_avg + 2 E_avg + (n-3) v)/n
                k == 2 : crease   (e1' + 6 v + e2')/8      # e1', e2' = other ends of the creased edges
                k >= 3 : corner   v
                fractional blend by min sharpness when 0 < s < 1
child sharpness s' = max(s - 1, 0)       # uniform rule
```

Differential test: subdivide the same cage with creases in numpy and in Blender, compare vertex positions (`use_limit_surface=False`, same level). A mismatch above 1e-6 tells which rule differs.

### Recommendation for our engine

- Dependencies: numpy, scipy, trimesh (MIT), Hypothesis (dev). Optional: libigl for a curvature cross-check. Keep GPL tools (pymeshlab, Blender) out of the engine library; run Blender as a process boundary.
- Blender is used only through `blender --background --python`, as now.

---

## 12. Testing a geometry kernel

### 12.1 Fixtures

- **Primitive cages:** unit cube, cube with hold rows, cylinder, torus, a two-panel plane with a smooth seam, a gap strip, a wheel-arch O-grid, a round pocket. Each has an analytic expected result (Euler characteristic, limit radius from table 4.3, volume).
- **Reference cars:** three small recipes (a box car, a rounded car, a roadster) that are the regression set. Their cage JSON is checked in.
- **Calibration box** of Appendix A: asserts the crease table within 5 %.

### 12.2 Property tests (Hypothesis)

For random recipes within bounds (seeded):

| Property | Check |
|---|---|
| Watertight | each edge in a closed shell belongs to exactly 2 faces; no boundary edges except declared lips |
| Manifold | each vertex's faces form one fan; no edge with 3+ faces |
| Orientation | winding consistent; signed volume > 0 |
| Euler | `V - E + F = 2 - 2g` per shell with the declared genus (e.g. torus 0, sphere 2) |
| No degenerates | no zero-area face; no coincident vertices (except seams, which are welded) |
| Seams exact | for each seam, patches' boundary arrays are `np.array_equal` to the seam samples |
| Symmetry | mirror in x maps vertex set to itself: sort by rounded coordinates, compare to 1e-6. Mirror-symmetric recipes only. |
| No self-intersection | BVH overlap candidates, then exact triangle test; allow-list from 75 §4.2 rule 6 |
| Overlap table | each listed shell pair penetrates 5 to 10 mm; unlisted pairs do not touch |
| Poles | every non-4 vertex has a declared `site`; none on ridges |
| Metamorphic | uniform scale of a recipe scales all vertices by the same factor; translation along x does not change shapes; mirrored recipe mirrors the result |
| Bounds | dimensions equal to recipe stance within 1 mm after limit-fit |

### 12.3 Golden meshes

- Store the **cage** (rounded to 1e-6, JSON) and its SHA-256, per reference recipe.
- Compare by hash first. On hash change, compare by **Chamfer and Hausdorff distance** on a sampled limit surface; the test fails above 0.1 mm (so numpy-version noise does not fail the build) and reports the distance.
- Update goldens only through an explicit `--update-golden` run, with the diff image from the review sheet.

### 12.4 Determinism

- No unordered sets or dicts in geometry loops (or sort the keys). Python's hash randomisation affects set iteration; test with `PYTHONHASHSEED` set to two values.
- Seed any noise from the recipe id (`np.random.default_rng(seed)`), never the clock.
- Run the build twice in two processes and compare hashes. Run once with 1 thread and once with several for the sparse solver.
- Round to 1e-7 before hashing. numpy BLAS can differ in the last bit across CPUs [inference].
- Differential test: numpy CC versus Blender subsurf (§11).

### 12.5 Performance and regression

- A budget test: each reference car builds in under a time limit (set after first measure) and under the face counts in 75 §6.
- The QC report is itself golden: per-check numbers are stored, and a drift of more than 10 % must be explained.

### Recommendation for our engine

- `tests/forge/` with fixtures, `test_properties.py` (Hypothesis, 200 examples per property in CI), `test_golden.py`, `test_blender_diff.py` (needs Blender; marked and run on demand).
- Run all properties on every shell, not only the body.
- Fail the build on any Euler or manifold failure before running the heavier tests.

---

## Appendix A. Experiments run for this note (Blender 5.2.2 LTS, 13 Oct 2026)

**Crease and holding-row radius.**
- Closed box, cross-section square with half-width 1, extruded along y in [-4, 4]. Ring points on each side at -1, -1+w, 0, 1-w, 1. Corner long edges set to the crease under test.
- Evaluated with the Subsurf modifier at levels 1 to 6, with and without `use_limit_surface`.
- Slice at y = 0. Intersect the 45-degree diagonal with the slice polyline. Distance from the sharp corner `d_diag`; `r_eff = d_diag / (sqrt(2) - 1)`.
- Holding-row case: crease 0, first-row width `w = d`.
- Results are in §4.3. Selected raw values: hold d = 0.1 gives r = 0.0569 (limit) and 0.0533 (level 2); crease 0.5 at w = 0.5 gives 0.0, 0.053, 0.093, 0.103, 0.106 at levels 2 to 6.

**Gap lip.**
- Slab 120 mm wide, 20 mm thick, with a 6 mm wide, 8 mm deep groove. Lip crease, floor crease and support rows varied. Level 5, limit surface.
- Lip radius from the nearest slice point to the sharp lip corner, divided by `sqrt(2) - 1`.
- Results in §5.2.

**Known limits.** One edge angle (90 degrees), straight edges, symmetric first-row widths for the radius table. Curved edges and mixed angles are not covered. A valence-3 corner was not measured.

---

## References

**Read this session**
- OpenSubdiv `Sdc::Crease` source (decrement, Chaikin, fractional weights): https://raw.githubusercontent.com/PixarAnimationStudios/OpenSubdiv/release/opensubdiv/sdc/crease.h
- OpenSubdiv subdivision-surfaces documentation (semi-sharp 0 to 10, Chaikin, boundary modes): https://raw.githubusercontent.com/PixarAnimationStudios/OpenSubdiv/release/documentation/subdivision_surfaces.rst
- Blender crease attribute change: https://projects.blender.org/blender/blender-addons/pulls/104709 and https://projects.blender.org/blender/blender/issues/101401
- DeRose, Kass, Truong, "Subdivision surfaces in character animation", SIGGRAPH 1998: https://graphics.pixar.com/library/Geri/paper.pdf
- Topology guide (3-pole, 5-pole): https://www.shootfirst.art/toplogy
- Polycount, car panel gaps and support loops: https://polycount.com/discussion/comment/2739867/ and https://polycount.com/discussion/comment/2739865/
- Polycount and forum threads on automotive topology: https://blenderartists.org/t/hard-surface-automotive-modelling-just-stuck-here-with-awkward-topology/1296583 and https://www.tripo3d.ai/blog/explore/smart-mesh-topology-for-vehicles-and-mechanical-parts
- Panel Cutter (Blender add-on, support loops): https://blendermarket.com/products/panel-cutter---non-destructive-panel-cuts
- McNeel forum, Audi Avus build, splitting surfaces: https://discourse.mcneel.com/t/audi-avus-zwei/120836
- libigl Python bindings (PyPI, licence): https://pypi.org/project/libigl/
- PyGEL3D: https://pypi.org/project/PyGEL3D
- PyMeshLab licence: https://pymeshlab.readthedocs.io/en/latest/about.html
- trimesh licence (Debian copyright): https://metadata.ftp-master.debian.org/changelogs/main/t/trimesh/stable_copyright
- Blender 5.2.2 Python API, by introspection in the installed copy (`bpy.types.SubsurfModifier`, `bmesh.ops`, `Mesh.edge_creases*`).

**From memory, not re-fetched (verify before citing)**
- Catmull and Clark, "Recursively generated B-spline surfaces on arbitrary topological meshes", 1978.
- Coons, "Surfaces for computer-aided design of space forms", MIT 1967. Gordon, "Spline-blended surface interpolation through curve networks", J. Math. Mech. 1969.
- Farin and Hansford, "Discrete Coons patches", CAGD 16, 1999. Farin, *Curves and Surfaces for CAGD*.
- Halstead, Kass, DeRose, "Efficient, fair interpolation using Catmull-Clark surfaces", SIGGRAPH 1993.
- Stam, "Exact evaluation of Catmull-Clark subdivision surfaces at arbitrary parameter values", SIGGRAPH 1998.
- Reif, "A unified approach to subdivision algorithms near extraordinary vertices", 1995. Peters and Reif, *Subdivision Surfaces*, 2008.
- Kobbelt, "Discrete fairing", 1997. Botsch and Kobbelt, "An intuitive framework for real-time freeform modeling", SIGGRAPH 2004.
- Loop and Schaefer, "Approximating Catmull-Clark subdivision surfaces with bicubic patches", 2008. Niessner et al., "Feature-adaptive GPU rendering of Catmull-Clark subdivision surfaces", 2012.
- Thompson, Warsi, Mastin, *Numerical Grid Generation*; Knupp and Steinberg, *Fundamentals of Grid Generation* (TFI and elliptic smoothing).
- Sandia Verdict mesh-quality library (quad metrics).
- Carried from 75b: Gordon fill pages (CAESES, OpenCASCADE `GeomFill_Gordon`), Rhino continuity and zebra pages, BeamNG vehicle workflow page.
