# Car Forge r19 review (banger, hatch, wedge)

Viewed all 21 images (18 car views, 2 line-ups, the in-game class C line-up). Compared with review-r17.md.

## 1. Scores

| Car | r12 | r17 | r19 |
|---|---|---|---|
| test_banger | 4.5 | 5.5 | **6.5** |
| test_hatch | 4 | 5.5 | **6** |
| test_wedge | 5 | 6 | **6.5** |

The studio sky and exposure make the renders much more readable. Wheel faces are now bright, and the arch feet are gone on the banger and hatch. Most r17 faults are fixed. The remaining gap is surface richness: panels are still mostly smooth blobs with hairline decals.

## 2. Per car

### test_banger (6.5)
**Improved:** the cream roof is the best single change this round. It gives a period two-tone and a strong silhouette, and it reads in every view, including the game. The cream steel wheels with bolt holes and a chrome cap are period-correct and separate cleanly from the tyre. The grille speckle is gone and the bars sit clear of the grille. The boot handle and shut line are present. The bumpers and mirrors now read as chrome. The paint is deeper, though still a mid teal. The arch liners no longer poke below the sill.

**Defects:**
- The boot shut line in 34rear sags and wobbles in the middle, so the lid looks dented. The `seam` (rear) uses a flat rectangle and the surface under it is uneven.
- The exhaust shows in the side view as a grey block under the rear bumper (`exhaust at=(-.42,.24)`).
- The side glass is a flat grey with no depth in the side and flank views.
- The bonnet is still a smooth slab. The `ridges` feature (h=.018) does not show anywhere.
- Panels are hairlines only. The roof is a flat cream rectangle from above, with no roof crown or drip rail.

**Top improvements:**
1. Make the bonnet ridges read. Raise `ridges h` to about .03 and widen `w` to .20, or add a centre crease. Today the bonnet is the biggest blank area.
2. Add a drip rail or gutter in `ChromeAlloy` along the cream roof edge, plus a small C-pillar vent. Both would sell the two-tone.
3. Add a second wear tone: a darker lower body band below `Side trim`, or a different tone on the sills.
4. Fix the boot lid surface. Smooth the deck curve (`deck_z` between 'c' and 'boot') and make the shut line a fixed offset from the surface.
5. Add front over-riders to the `Front bumper` bar and a bumper-top lip. Tuck the exhaust higher or shorten it.
6. Give the glass some interior depth in side view (visible seat backs).

### test_hatch (6)
**Improved:** the tailgate shut line and handle exist. The bumper is clean. The five-spoke wheels now have bright spoke faces. The grille bars are clear. The colour holds up in the game (deeper red).

**Defects:**
- A black tag still sticks out forward of the front arch at the end of the rubbing strip (visible in the 34front, nose and side views). It is the strip end at `pts (1.62,.50)`.
- The bumper ends are black blocks that stick out past the body at both ends (side view).
- A faint pink speckle surrounds the grille in the nose view (surround z-fighting).
- The plate overlaps the grille and bumper edge at (0,.53).
- The tailgate has a pinch or fold at the left shoulder in 34rear. The shut line is barely visible.
- The pastel salmon in the Blender renders is washed out. The game colour is better.

**Top improvements:**
1. Shorten the `Rubbing strip` so it ends about 0.1 short of each arch. Round the ends. This removes the black tag.
2. Move the plate fully into the bumper recess (`plate at=(0,.53)` to about .46) and give the grille a clear gap above it.
3. Tailgate: make the shut line a visible 2 mm groove and add a roof lip spoiler above the glass. The rear is still the weakest face.
4. Add a shoulder crease. Raise `creases shoulder` to 0.5+ so the flank has a highlight line, as the wedge has.
5. Hot-hatch identity: add a split tail-lamp lens, fog lamps in the bumper, or a body-colour grille surround. At present the car only reads as "red hatch".
6. Make the wheels differ from the wedge's (see section 3).

### test_wedge (6.5)
**Improved:** the front wheels are smaller and the stagger is clear. The wheel faces are bright with a polished lip. The side intake strakes behind the door are a good change. The straight side profile from the nose to the arch is strong. The rear (wing, red lamps, tail grille) is a good A-class read.

**Defects:**
- The bonnet saddle is not fixed. In 34front, nose and flank the front fenders are still two humps with a dip between them (`crown_z`/`deck_z` at 'fa' vs 'cowl'). The nose is still a bulbous frog shape from the front.
- The nose is still a wide spade in plan (top view). `crown_w`/`sh_w` at nose (.58 / .66) is not narrow enough.
- The pop-up lamp lids are still outlined squares (the `seam` top at (.30,1.30)-(.56,1.50)), and they look skewed and flat. They read as decals.
- The wing endplates are flat rectangular blocks, and the "rounded wing plates" change is not visible. A dark post floats under the endplate in the side view.
- The splitter shows white speckle at its lower edge (nose view). The front arch flange is jagged.
- Small feet are visible below the sill at the rear arch (side view).
- The windscreen cowl bar is still thick.
- Blender paint is washed peach, while the game shows saturated yellow. That is a studio tone-mapping fault.

**Top improvements:**
1. Flatten the bonnet. Set `crown_z` and `deck_z` at 'fa' to about the average of nose and cowl (.46 or so) so the line rises straight. Narrow `crown_w` at the nose to about .50.
2. Make the lamp lids a raised, hinged hardware piece (a pop-up lid with a dark gap), or show one lamp raised.
3. Rebuild the wing endplates as rounded, thicker plates with a foot on the tail, and a single central pylon or thicker posts.
4. Brighten the paint in the Blender studio so the wedge saturates the way it does in game.
5. Fix the splitter speckle and the front flare edge.
6. Make the rear fenders higher than the tyre top, or lower the rear tyre, so the tail does not look buried.

## 3. Cross-car notes

**Tier read:** the wedge is clearly a different class within one second (stance, wing, low nose). Banger to hatch reads as old to newer (steel to alloy, chrome to black plastic, two-tone to one-tone). Hatch to wedge works by shape. Detail count and finish barely differ across tiers: all three use the same hairline panel language.

**Variety:** the colours and silhouettes differ, and the banger's cream roof gives it unique identity. The hatch and wedge now share the same five-spoke wheel, so they lose variety. All three share the same square-tag arch trim.

**Toy-likeness:** the banger is the most toy-like (round lamps, big grille). The wedge's lamp decals and boxy wing weaken it. The hatch is too neutral.

**Materials and lighting:** the sky and exposure are good. Chrome now reads as chrome on the banger. Paint depth is better but still low in contrast on grey. The grey studio floor still lets pale cars blend. Glass is flat grey in side views. The Blender and game colours differ, so judge paint in the game.

## 4. Top-8 next changes

1. Wedge: flatten the bonnet saddle (`crown_z`/`deck_z` at 'fa') and narrow the nose plan.
2. Hatch: shorten and round the `Rubbing strip` so the black tag goes.
3. Wedge: raised, hinged pop-up lamp lids.
4. Banger and hatch: bonnet ridges and a tailgate or boot groove with real depth.
5. Differentiate wheel designs by tier (banger steel, hatch a different alloy such as a mesh or a 6-spoke, wedge a deep-dish five-spoke or a gold finish).
6. Wedge: rounded, thicker wing endplates with a pylon.
7. Add a shoulder crease and a two-tone or accent band to the hatch, and a stripe to the wedge.
8. Fix the surface speckle (grille surround, splitter) and the studio tone mapping, and darken the floor so pale cars separate.
