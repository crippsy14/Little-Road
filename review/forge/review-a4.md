# Review of stage A (a4, with a4z zebra), blunt

## 1. Scores, strict basis
| Car | Now | r20 on the same basis (r20 was scored on an older, easier basis) | Distance from "playdough" |
|---|---|---|---|
| banger | 7.0 | ~6.0 | Mostly out of it. Flat sides and a crisp shoulder read as sheet metal. Still doughy at the nose and the A-pillar base. |
| hatch | 6.0 | ~5.0 | Panels are out, but the glasshouse is a lumpy slab. Windows, pillars and doors don't agree, and the zebra is still dough. |
| wedge | 6.5 | ~5.5 | Profile and bonnet are now wedge-like. The nose is still a rounded-over pillow and the bonnet and wings are blobby. |

## 2. Per car

### Banger (Wartburg 353, Lada 2101)
**Proportion and surfacing.** The best of the three.
- The side profile is right: upright tail, long flat bonnet, A-pillar raked about 60 degrees. Flat vertical sides with a clean waist line hold up in the zebra.
- The sides are now a true panel: sky above the waist, ground below, no wobble.

**Biggest gaps:**
1. The A-pillar is a blank body-coloured wedge. The windscreen foot is at `cowl` (.52) but the side glass starts at `a_top-.02` (.16). That leaves a 0.35 m solid triangle ahead of the door glass. The Wartburg's glass runs right up to the pillar.
2. The doors and glass disagree. Door seams are at .48 and -.48, and `b` is -.42. There is no rear-door seam or handle, so the rear window sits over an undefined panel.
3. The nose face has large plan-view corner radii (`nose_len=.15, nose_square=5`). The real cars have near-square corners. The underside also rolls under like a bathtub at the front (`r_rocker=.5`).
4. The cabin is a little narrow and short. Roof width is .66 to .69 against a body half-width of .765.

**Defects.**
- The A-pillar flares outward at its base into a "horn", visible in the top and nose views.
- The roof slab is a flat white rectangle with sharp inner corners.

**Zebra.**
- Flat sides: clean.
- The bonnet/shoulder band wobbles near the A-pillar foot, with black "drips" down the front wing and arch lip.
- The boot lid has the same band wobble.
- The A and C pillars have healthy curved stripes.

**Top changes:**
- `side[0]` starts at `a_top-.02`. Change it to start at `cowl-.12`.
- Seams at `-.42` and `-1.02`, plus a rear handle at `(-.88,.72)`.
- `nose_square` 5 to 8 and `nose_len` .15 to .09. Same for the tail.
- `r_rocker` .5 to .3 and `r_floor` .1 to .06.
- `roof_w` `('cowl',.70)`, `('b',.72)`, `('boot',.70)`. This stops the base flare and fattens the cabin.

### Hatch (Golf Mk1, 205 GTI)
**Proportion and surfacing.** Overall stance, bonnet, flat tail and roofline are credible. The glasshouse is the problem.

**Biggest gaps:**
1. The B-pillar sits mid-door. `b=-.32`, but the door runs .44 to -.62 and the handle is at -.52. The front glass is only .3 m wide and the rear glass straddles the door seam. A two-door should have its pillar at about -.60, with one long door glass from about .38 to -.55.
2. The rear quarter is a solid 0.6 m wall. `side` ends at `c+.06` (-.92) and the hatch point is at -1.50. The Golf and 205 both have a rear quarter window. The 34-rear view reads as a box with a screen punched in.
3. As on the banger, there is a blank A-pillar wedge. `a_top=.02` against `cowl=.52`.
4. The front plan corners (`nose_square=4.5`) show a visible chamfer facet.

**Zebra.**
- The side panel has a black blob with a wavy lower edge at the arches. The waist/shoulder is not planar (`sh_w` and `sh_z` are keyframed ra/fa/nose).
- The front wing corner shows V zigzags.
- The rear quarter has broad wobbling stripes. `side_round=.08` plus `tumble=.07` make a bulging slab.
- The roof has a bulge patch at the back.

**Top changes:**
- `b` to -.60, with `side=[('cowl-.10','b+.04'),('b-.04','c-.05')]`.
- Extend the rear quarter glass to `c-.30`.
- `sh_w` to 2 keyframes only (`ra` .82, `nose` .80) so the side plane is planar. Same for `crown_w`.
- `side_round` .08 to .03 and `tumble` .07 to .04.
- `nose_square` 4.5 to 7.

### Wedge (Countach, Esprit S1)
**Proportion and surfacing.** The profile is a genuine wedge now: straight bonnet line, short cabin, windscreen at about 24 degrees. That matches the Countach. The side glass reaches the pillar, which is the one car where the glazing is right.

**Biggest gaps:**
1. The nose is too tall and blunt. Tip height is about .43 against 1.05 overall, so about 41%. The Countach's tip is about a third of overall height. In plan it is a rounded lozenge (`nose_len=.40, nose_square=2.6`) where the Countach is a trapezoid with small corner radii. The result is a "chin box".
2. The bonnet and wing-top read as one rounded lump. There is no crisp wing-to-bonnet fold in the paint, and the 34-front view is pillowy.
3. The rear quarter and tail are boxy and tall, with an oversized louvre panel (stage D).
4. The wing end-plates are billboard-sized, and the cabin is somewhat small.

**Zebra.**
- The bonnet shows island blobs, so the surface is not a plane (kinked at `cowl+.25`).
- The side plane's black boundary is curved. `waist_w` and `sill_w` vary along length, so it is twisted rather than two flat planes.
- The nose face has wavy stripes.
- The rear quarter swirls.
- The roof and windscreen are clean.

**Top changes:**
- Lower the nose: `nose` `waist_z` .30 to .26, `sh_z` .45 to .38, `crown_z` .46 to .38, `deck_z` .43 to .36, `sill_z` .22 to .19.
- `nose_square` 2.6 to 4.2, `nose_len` .40 to .30.
- `waist_w` and `sill_w` as two keyframes each (`ra` to `nose`). Drop `c` and `fa`.
- `sh_z` and `crown_z`: remove the `cowl+.25` kink. Run `cowl` to `nose` as a straight line (.70 to .45).
- `edges.shoulder` .012 to .006, and `r_sh` .12 to .06 for the wing/bonnet fold.

## 3. Did stage A meet its aims?
- **Edge radii and flat panels: mostly.** The banger's flat sides are proven by the zebra. The hatch and wedge are still not planar: their variable-width keyframes twist the side planes.
- **Side swages: weak.** Only the banger has a visible line, and that is a decal trim bar.
- **Roofline as straight runs: yes** for the banger and wedge. The hatch roof is fine but its pillars are not.
- **End faces: partial.** The corner radii in plan are still too big on all three.

## 4. Ranked top-6, next changes overall
1. **Hatch and banger: fix glass, pillars and doors together.** Put the B-pillar at the door's rear edge, extend the quarter glass, and add the missing banger rear-door seam and handle. This is the owner's "weird windows" complaint, and the zebra and paint views show it plainly.
2. **Remove the blank A-pillar wedges** (banger, hatch). Start `side[0]` near the windscreen foot, as the wedge already does, and narrow the A-pillar flare at `roof_w['cowl']`.
3. **Wedge nose: lower, sharper, trapezoid in plan.** Lower the nose keyframes and raise `nose_square` to 4+.
4. **Make side and bonnet planes truly planar.** Use two-keyframe `waist_w`, `sill_w`, `sh_w` and `crown_w`, and remove the `cowl+.25` kink. The zebra shows blobs and curved boundaries on the hatch and wedge sides and on all three bonnets.
5. **Tighten plan-view end corners on all three.** Raise `nose_square` and `tail_square` to 6 to 8 and shorten `nose_len`. This removes the pillow chamfer facets.
6. **Reduce the bathtub roll-under** at nose and sill. Lower `r_rocker` to about .3 and `r_floor` to about .06, which also helps the underside stripes in the zebra.
