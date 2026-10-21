# 91. Vinelands, Zone 6: the Alto da Portela and the Entroncamento (brief, 22 October 2026)

The sixth zone of the rebuild (docs/85), from the town plan (`review/street-review/vinelands-town-plan.md`, sections 1–4
and 7). Frame as in docs/86. Built behind `--vinelands`.

## What these places are, and why they're here
- **The Alto da Portela** is the town's newest edge, built after 1980 on the saddle where the entry road crosses the
  ridge. It's the only flat ground near the town, so the things that need flat ground went there:
  - the football ground and its courts;
  - the school;
  - the fire station, right on the junction so the engines are out in seconds.

  The health centre stands on Rua Direita, where the ambulance reaches the entry road. Along a contour street on the
  west flank (Rua das Amendoeiras) stand the Portela's three-storey flats and the emigrants' semi-detached houses, with
  drives and gardens. The old olive press is at the street's end. The lay-by at the saddle has the double view: the Vale
  one side, the river the other.
- **The Entroncamento** is where the tractor road from the quintas (the Estrada do Meio) meets the entry road, near the
  river. The co-op Adega was built there in 1956, so four hundred growers could bring their grapes downhill to one press:
  - the Adega's long white hall, its steel tanks behind it and the weighbridge at its yard;
  - across the road, the bottling plant, the cooperage and the warehouses;
  - the farm supply shop and a row of co-op houses.

  In September the tractors queue here from dawn.

## Shape
- **The fire station** at the junction's south-east corner, (−15, 690), its doors to the road.
- **The school** (110, 690), south of the entry road, with parking at its gate.
- **The football ground** north of the road at (0, 795), on a level pad 68–92 m across. Its stand faces the road side;
  the courts are beside it at (88, 800).
- **The health centre** on Rua Direita's east side at n 600.
- **Rua das Amendoeiras**, a new 6 m street off Rua Direita at n 560, west to (−335, 690). Flats and semis line both
  sides; the olive press is at its end.
- **The Entroncamento:**
  - the Adega 62 m east of the junction on the south side, the tanks behind it, the weighbridge at its gate, tractors and
    vans in the yard;
  - across the road: the bottling plant, the cooperage and a warehouse;
  - another warehouse on the south side;
  - west of the junction: the farm shop (south) and the co-op houses (north).

## Architecture (Car Forge kit `portela`)
- **The Portela:** the school; the football pitch (lines, goals, floodlights, a low wall); the stand; the court; the
  health centre; the fire station with its training tower; the flats; the semi-detached pair; the olive press.
- **The Entroncamento:** the steel tanks, the weighbridge, the bottling plant, the cooperage, the warehouse and the farm
  shop.
- **Reused:** `adega_coop` (douro), `quinta_caseiros` (the co-op houses), `tractor`.

## Life
- **Named residents:**
  - Comandante Nunes at the fire station;
  - Professora Luísa at the school;
  - Engenheira Sofia at the Adega.
- Cars at the school, the ground and the drives; tractors at the Adega; a car on Rua das Amendoeiras.

## Done when
- From the saddle the Portela reads as the modern edge: civic buildings, the pitch, the flats.
- The Entroncamento reads as a working co-op quarter.
- The new street and its junction are clean in the audit.

## Build rounds (22 October)
Code: `VinelandsPortela.cs`. Kit: car-forge `portela` v1 (3d7ea63).
1. **First build.** The saddle and the west flank aren't flat. The civic buildings and every house on Rua das Amendoeiras
   rose 6–9 m into the slope and were refused. The pitch was lifted by the entry road's embankment fill (it reaches up
   to 54 m from the road).
2. **Sites.** `Vinelands.Sites` and `Vinelands.AmendoeirasPlots` put each building on its own level platform at its
   road's height. Both the ground (`Pads`) and the buildings use the list. The pitch moved 35 m further from the road,
   clear of the fill, onto its own level ground. The Portela now stands: fire station, school, pitch and stand, courts,
   health centre, 18 houses and flats.
3. **The rim.** The Entroncamento sat in a cutting through the district's rim hill, nearly 100 m deep. No rim there now.
4. **The vale blend.** It was still 100 m: at 1375 m from the centre the land blends into the neighbouring vale's hills,
   so the blend leaves the co-op quarter alone too. The Adega, the tanks, the bottling plant, the cooperage, the farm
   shop and the co-op houses now stand. Two warehouses by the river bank stay refused (a 6 m rise). The yard's tractors
   are now set on their own ground.
