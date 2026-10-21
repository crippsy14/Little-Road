# 90. Vinelands, Zone 5: the Belvedere, the Mirante and the Seis Voltas (brief, 22 October 2026)

The fifth zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, sections 1–6).
Frame as in docs/86. Built behind `--vinelands`.

## What this place is, and why it's here
The crest road south-west from the Largo is the old ridge track. After phylloxera (the 1880s), men came home from Brazil
with coffee money and built villas along it, high enough to be seen from the whole town and to see both valleys. The
villas are tiled fronts, turrets, urns on the cornice, palms in walled gardens. Emigrants from France added their own in
the 1970s, with big balconies over a garage. Past the villas the road runs on along the open crest:
- the **cemetery**, walled and white on stony ground;
- the **Moinhos do Alto**, three windmills, two of them ruined;
- the **Belvedere**, a knoll at 230 m with its bell tower and chapel, the whole amphitheatre on one side and the Vale on
  the other.

Below it, in the empty south bowl, the 1930s road from the river climbs to the knoll in six bends: the **Subida das Seis
Voltas**. It is the hill climb (its event opens to the faster classes; the road is built now). Spectators stand on the
terrace walls at the voltas, among olives along the edges and hay bales.

## Shape
- **Villas:** both sides of the Mirante road from s 175 to 640 (past the bus terminus to the mills). Each is set back 7 m
  behind a garden wall with a gate.
- **The cemetery:** 84 × 54 m by the road at about (−130, −310), its gate to the road.
- **The mills** on the crest at about (−445, −480).
- **The Belvedere:** the roads' end at (−590, −614), with a paved terrace and car park of 36 × 36 m, the tower and the
  chapel.
- **The Seis Voltas:** see the track below. The paddock is on the Veiga do Fojo at (1040, −1010), by the river road.
- **The bowl's land:** two new parcels.
  - *Vinhas das Voltas*: old vines inside the voltas.
  - *Mortório das Voltas*: the upper loops, abandoned.

## The track (track-design skill)
The piece list is `Vinelands.VoltasSpec` (draft `review/tmp/sv3.txt`, evaluated with `tools/track-eval.py --p2p`):
- **Course:** start (1115, −1000) heading west. From the turtle's end, a straight finishes at the Belvedere.
- **Profile:** 15 % maximum grade, with level 20 m landings at both ends.
- **Corners, in order:** Volta do Rio (−82°, R30), Volta das Lajes (+88°, R26), a kink, Volta do Fojo (−86°, R22), a
  sweeper, Volta da Oliveira (+92°, R20), Volta da Capela (−95°, R22), Volta do Moinho (+90°, R22), and the Gancho do
  Alto (−80°, R40). Then the finish ramp's sweeper and kink.
- **Metrics:** 2.06 km of pieces plus about 170 m of finish, a 10.8 % average, 4.4 corners/km, 331°/km turning. The
  longest straight is 210 m, 0.10 of the length.
- **Self-critique:** the skill's hill-climb bands (5–10 corners/km, 450–900°/km) are estimates, and this sits under them.
  That's deliberate: the plan's Seis Voltas are six 90° voltas on 200–280 m legs, the road's identity. Variety comes
  from the radii (20–40), the sweepers before two voltas, a kink on the Lajes leg, and the steepening finish. If the
  event needs more, add esses on the Fojo leg.

## Architecture (Car Forge kit `mirante`)
- **Villas:** the returnee's villa (tiled front, turret, urns); the 1920s chalet; the emigrant's house (balcony, garage).
- **Pieces:**
  - the gate piers with urns;
  - the ruined and the restored windmill;
  - the family tomb chapel;
  - the grave;
  - the iron cross.
- **Reused:**
  - `torre_belvedere`;
  - `cais_capela` (the ermida and the mortuary chapel);
  - `quinta_portao` (the cemetery's gate and the voltas' name stones);
  - `vila_cipreste`, `palm`, `rua_camelia`, `olive_tree`, `hay_bale`, `telescope`.

## Life
- **Named residents:**
  - Dona Ermelinda at her villa;
  - Sr. Belmiro at the tower;
  - Sr. Fausto at the paddock, who explains the climb.
- Cars at the villa gates, on the Belvedere's terrace and in the paddock.

## Done when
- The crest reads as villas, then open crest with the cemetery and mills, then the knoll.
- The Seis Voltas read from the Belvedere and from the river as a stack of bends on the bowl.
- The climb's road has no seams or lips, and the area audit has no new defects.

## Build rounds (22 October)
Code: `VinelandsMirante.cs`. Kit: car-forge `mirante` v1 (0d8314b).
1. **First build.**
   - 37 villas went up first and took the cemetery's ground.
   - The climb's start and its paddock sat on the district's rim hill: the plan's south bowl reaches into the ring where
     the land rises as a rim and then falls to the sea. Olives floated there.
2. **Order.** The cemetery, mills and knoll are now placed first. The villas stop at s 470, so the crest is open from
   there to the knoll.
3. **The rim.** There's no rim hill over the south bowl now. The climb's 80 lips were the corridor failing on that slope.
4. **The bowl's land.**
   - The land runs 220 m further before it falls to the sea, and the terraces and the Vale blend follow it.
   - The climb's finish straight to the knoll is evenly spaced: one long last segment made the road's spline loop back
     on itself (a 1.9 m seam).
   - The climb now has no lips, seams or floating pieces: 2.25 km, 216 m of rise, 15 % at its steepest.
