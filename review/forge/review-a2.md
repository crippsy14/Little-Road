# Forge stage A2 review (caricature layer)

Judged from review/forge/b1, compare-a2, a5, refs and measure-a2-2.json. Report only; nothing was edited.

## Verdict
Character is partly back, not fully. The banger and hatch now read as toys, mainly through the larger wheels, lamps and cabin. Next to the legacy cars they are still a "real car with soft corners". Bramble, Sprint and Dover are inflated: the whole body is one pillowy mass with big glass and thin pillars. Ours are still flat-sided slabs. The sides are vertical (banger waist_w .772 against shoulder sh_w .765, so there is no bulge or tumblehome) and the bonnets are flat planes. The toy layer scales the proportions but does not add volume. The wedge got almost no toy treatment (no lamps or cabin factor) and its crispness has gone soft.

## Scores (stage A in brackets)
| Car | Now | Stage A |
|---|---|---|
| Banger | **7.5** | 7 |
| Hatch | **6.5** | 6 |
| Wedge | **6.0** | 6.5 |

## Banger
Character: the cream roof cap, chrome bumpers, hubcaps and big round lamps give it a clear personality. It is the closest to the legacy cars, though still a little stiff.
Fidelity: Lada 2101 / Fiat 124 three-box, with a Wartburg-style upright glasshouse. Recognisable. The Wartburg's long boot and low bonnet are only partly there.

Changes:
1. **Wheels are the smallest of the set.** wheelD/H is 0.46; the legacy cars are 0.49-0.53. Set wheels 1.15 -> 1.26 (wheel_r about .41) and widen the arch_r to match. This is the cheapest character gain.
2. **Slab sides.** Add a bulge: waist_w .772 -> .80, sh_w .765 -> .74 (tumblehome), r_sh .55 -> .65, edges 2.0 -> 2.6. This gives the inflated-pillow section of the legacy cars.
3. **Lamps read as blank eyes.** Cream discs with a grey ring and no depth. Raise lamps 1.15 -> 1.35, add a domed lens (lift .02) and a darker inner reflector.
4. **Defects.** In the side view the rear bumper shows as a stub below the body line (about x=235, y=493), and the front bumper ends are small cubes. Extend the bumper bars across the full width, or tuck them in. The tyre outer face sits slightly proud of the body: track .68 x 1.16 plus half the tyre width is about .91, against a waist of .895. Pull tyres back to 1.08 or flare the arch.

## Hatch
Character: friendly, with big googly lamps, but the lamps and wheels are the only toy features. It is less chunky than Bramble or Sprint.
Fidelity: the Golf Mk1 face (grille band, round lamps, black bumper) is good. The rear slope and bulging quarter are less like the Golf's upright square tail.

Changes:
1. **Wheels are too busy.** Twenty-plus thin spokes read as a turbine against the chunky tyre. Use a 6-hole steel or 4-spoke BBS style with a fat rim lip, and set wheels 1.08 -> 1.16 (wheelD/H .49 -> about .52, matching Sprint).
2. **Tail and C-pillar.** In 34rear the quarter swells into a pod with a flat sloped hatch. Make the hatch near-vertical: hatch mark -1.50 -> -1.58, roofline point ('hatch', 1.08) -> ('hatch', 1.00), and a squarer rear window (rear_screen top at c-.02). Make the tail lamps wide and low (.14 x .07) as on the Golf.
3. **More volume.** The same slab problem as the banger. Use r_up .16 -> .22, edges 2.0 -> 2.5, and raise cabin 1.14 -> 1.2 for bigger glass, as on the Sprint. Add a thin pillar-and-roof gutter line.
4. **Lamps.** The white discs lack depth. Add a domed lens with an inner ring, and keep the diamond surround for the Golf character.

## Wedge
Character: low, long and wide, with a wing, but it is a faithful car, not a toy. There is no cartoon in the stance or glass.
Fidelity: the Countach/Esprit silhouette is there, but the Countach's key signature has gone. The bonnet is pillowy and bulbous where the Countach has a flat plane with hard edges. The side intake, sail panel and engine louvres are missing. Stage A's crisper nose was better.

Changes:
1. **Too tall.** H 1.18 (H/W .59) against the Countach's real .535. The height factor 1.12 makes it chunky in the wrong direction. Set height 1.12 -> 1.02, width 1.06 -> 1.12 (a Tomica wedge is low and wide), and edges 1.6 -> 1.1 to restore the folded-paper nose.
2. **Flatten the bonnet.** crown_z should sit about .01 below deck_z across the nose, and r_top lower, so the planes meet at an edge. Restore the chamfer facets (nose_square 4.2 -> 5.5).
3. **Add the Countach details.** A black triangular side intake behind the door (about .35 x .22, at y -.55, z .62), louvres on the engine cover, a bigger wing (span +15%) with slimmer end plates, and move the pop-up lamps to the nose corners (x about .62).
4. **Defects.** The low-poly faceted lump at the nose corner, where the lamp is (0.6 u from the centre at z .45). The wobbly black arch crescent on the front wheel. A dark outline halo around the tail lamps and vent. The exhaust pipes dangling below the tail and the dark undertray wedge. The pop-up lids have visible box outlines and read as hatches.

## Ranked top-6 overall
1. Add body volume to all cars: bulge and tumblehome, with edges 2.5+ on banger and hatch. This is the biggest gap against the legacy cars.
2. Wedge: lower and widen it (height 1.02, width 1.12) and sharpen the bonnet planes (edges 1.1). It is the only score that dropped.
3. Banger wheels 1.26, hatch wheels 1.16, and a simpler hatch wheel style.
4. Hatch tail: near-vertical hatch, squarer rear window, wide low tail lamps, no swollen quarter.
5. Wedge identity: side intake, engine louvres, corner pop-ups, larger wing.
6. Clean up defects: the banger rear bumper stub, the wedge nose-corner facet, the arch crescent, the tail halos and dangling exhausts, and the tyres sitting proud of the body. Also give the lamps domed lenses on all three.
