# 66 · The car builder, v2: room for unique cars (6 October)

Richard (6 Oct), after four Tuners at 8/10: "I worry all these tuner cars are going to look very similar... an enhanced
version of the builder... keeps the overall toy aesthetic, but allows more freedom to create different car types with each
car feeling unique."

## Why every car converged (review of `build_family_cars.py`)
1. **One cross-section for the whole car.**
   - The body is a lofted superellipse: each station gives width, sill and top, but one pair of exponents shapes every
     section from nose to tail.
   - A car can't be square at the waist and round at the nose.
   - Nothing can rise above the bonnet line, so every bonnet-to-wing shoulder is the same.
2. **The same automatic wheel arch** on every car (`fender()` pushes the shell out to the tyre plus 13 cm).
3. **A cabin of straight quads.**
   - The windscreen, rear screen and side windows are four-corner frames, with a flat crowned roof.
   - That's the identical "box on top" in every render.
4. **One tyre** (the chunky treaded one) on everything, and five spoke designs in one finish.
5. **A few lamp, grille and tail styles**, all placed at an (x, z) on the end face, so the faces converge.

## The approach
Keep **one builder and its toy language** (the lofted shell, real boolean arches, pressed frames, modelled parts). Give
recipes **opt-in parameters** that break each convergence point. Recipes that don't use them build exactly as before, and
a rebuilt Comet confirms it.

### Phase 1 (built today)

| Option | Recipe key | What it allows |
|---|---|---|
| Section shape along the car | stations may carry `px, pz` as 5th/6th values | Square waist, round nose; a tall square tail on a round car |
| Fender pods | `fenders=dict(front=(rise,reach,length),rear=…)` | Wings standing above a low bonnet (RX-7), big rear haunches (Z), boxed pods (GT-R) |
| Roof arc and double bubble | `glass` keys `arc`, `bubble` | A roof that arcs front to back; the RX-7's twin humps |
| Road tyres | `wheel` key `tyre='road'` | Low profile, square shoulders, grooved crown, a 13% bigger rim |
| Wheel design | `wheel` keys `style` (`six`, `mesh`, `split`, and the old ones), `finish` (alloy, gunmetal, bronze, white, black), `dish`, `lip='polished'` | Every car its own period wheel |
| Signature lamps | extras `popup_lids`, `swept_lamps`, `hoop_wing`, `tail_panel`, `twin_round` and `boomerang` tails | The details that name a car |

**The four Tuners re-tuned** (`review/cars/tuner-v2`):
- **Hayate:** square everywhere, boxed pods, gunmetal six-spokes.
- **Kaze:** round, with haunches, swept lamps and bronze split-spokes.
- **Raijin:** the roundest, with raised wings, a double bubble, pop-up lids and silver five-spokes.
- **Hoshi:** long and smooth, squaring at the tail, with gunmetal mesh wheels.

### Phase 2 (built 6 Oct): the cabin and the face

| Option | Recipe key | What it allows |
|---|---|---|
| Lofted cabin | `cabin=dict(roofline, plan, section, edge, screen, apillar, rear_screen, cpillar, side, pillars, blackout, bubble, corner)` | The greenhouse is its own curved surface: a side-view roofline (a monotone cubic, so a crest stays flat and nothing overshoots), belt and roof widths along the car (a teardrop plan for fastbacks), its own superellipse section (boxy or bubble) |
| Windows by outline | the same keys | Windscreen between A-pillars that run from the roof edge down to any point (low = wrap-around); rear glass the same between C-pillars (high = narrow glass and broad sail panels); side glass along the car with its own lower and upper edges (a kick-up, a rising point); B-pillars, satin black if `blackout` |
| Face outlines | `face=[dict(kind='lamp'/'grille'/'intake'/'panel', pts=[(x,z)…] or oval=(cx,cz,rx,rz,p), bars, projectors, surround, mat)]`, `face_plate` | Each car draws its own face in front view; parts are ray-laid onto the real nose, with a dark rim, blades and projector lamps |
| Integrated nose | bumper style `lip` (and `none`) | No wrap-round bumper bar: the body is the bumper, with a dark lip under it, so mouths and corner intakes show |
| Real lamp details | extras `swept_lamps` (a long tapering lens in a surround), `popup_lids` (raised lids inside a shut line) | Both now sit on the real wing surface (a ray cast; the fenders push the wing above the formula's surface) |

How the cabin works: the surface is parametrised by arc fraction round the section (t) and position along the car (y),
so every window is a band in (t, y) with a lower and upper edge per y, rounded at the corners. The glass is meshed in that
band. The cabin skin is a fine grid with the openings left out, and the hole's edge vertices snap onto the window
outline under a rubber seal. There are no booleans, so the thin cabin can't trip the exact solver. The passenger well and
the door cards follow the cabin's plan and belt; the seats shrink to fit under a low roof.

Recipes without `cabin` or `face` build exactly as before: the Comet render is identical, and every C car was rebuilt.

**The four Tuners with Phase 2** (`review/cars/tuner-v3`):
- **Hayate:** upright boxy notchback, flat roof, thick C-pillar, kick-up in the rear side glass, black B-pillars; slim
  twin-projector lamps, a slim upper grille, a big bumper mouth round the plate.
- **Kaze:** one arc from screen to hatch, side glass ending in a rising point, long lamps swept up the wings, a wide low
  mouth.
- **Raijin:** teardrop bubble cabin with a double-hump roof and wrap-round hatch glass; pop-up lids, an oval mouth, slim
  amber corner signals, a ducktail in place of the roof spoiler.
- **Hoshi:** long set-back canopy with broad sail panels; triple-lens lamp clusters wrapping the corners, a big oval mouth,
  side vents.

**Round 3b (Richard, 6 Oct): "a cartoonish bump on its head" and "a black gap beneath the windscreen"**
(`review/cars/tuner-v3b`, with a new `cowl` close-up).
- **The Raijin's roof.** It was a short, steep crest between two big dark glass areas, so it read as a lump. Now it has
  a lower, longer crest, a wider roof, a gentler bubble, broader painted pillars and a slim roof rail.
- **The gap.** The passenger well was cut right up to the windscreen's base, but the glass only rises out of the bonnet
  further back. In between you looked down into the cut: a dark band, plus flicker where the cabin skin and bonnet
  nearly coincide.
- **The fix:**
  - the well now opens only where the cabin stands clear of the shell (`cab_emerge`), front and rear;
  - the glass always runs down to that line;
  - a satin-black cowl panel with vent slots lies over the joint, as on the real cars.

Fixed along the way: the Kaze's swept lamps (now long, on the real wing) and its split spokes (thicker).

### Next
- **Kits** for A/S/E: a cage, box flares, a race wing, roof vents, number boards; later a livery painter. See doc 65.
- Tails by outline: `face()` already takes `front=False`, so tail lamps and diffusers can be outlines too.

A new car is still 1–2 hours, but now the cabin and the face are as free as the body.
