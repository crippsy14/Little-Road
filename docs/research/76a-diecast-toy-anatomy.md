# Little Road: diecast toy anatomy (what a Tomica-style car is made of)

Research date: 13 Oct 2026. Purpose: tell the new car-generation engine exactly which components a real 1:64-ish diecast has, what each looks like, and how toy makers abstract real cars, so the engine can reproduce "all the same components".

How to read this. Sources are brand history pages, Wikipedia (English and Japanese), designer interviews, collector blogs and retailer copy. Detailed construction facts are thin in the written sources, so where a point comes from my own reading of the Wikimedia Commons photos, it is marked **[photo]**, and where it is my own reasoning it is marked **[inference]**. Photo filenames refer to `review/forge/refs/<folder>/` (see section 7).

---

## 0. Executive summary

1. A mainline diecast is a **small kit of 5 to 8 separate parts**, not one casting: metal body, plastic base, glazing, interior tub, four wheels on two steel axles, and fixings (one or two rivets, or screws). Designers split a car into "undercarriage, interior and outer shell" from the first sketch ([Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/); [The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)).
2. The **base is a separate part** and carries the bumpers, sills, lower valances and underside. Often it is black plastic. On older or premium castings it is metal. **[photo]** On the Tomica Hiace the black front bumper and chin read as one piece with the underside.
3. **Glazing is one clear or tinted unit** (windscreen, side lights and rear light together), sitting over a separate interior tub. Lamps are a mix of tampo dots, painted chrome and small clear or coloured inserts, depending on price tier.
4. **Scale is not fixed.** Tomica sizes each car to fit one box (78 mm wide for the basic line), so passenger cars sit near 1:60 and Premium castings range 1:58 to 1:62. Hot Wheels is nominally 1:64 but measured at about 1:65 to 1:80. Package size, not ratio, decides length ([Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/); [Nostalgic Car](https://japanesenostalgiccar.com/50-year-club-tomica/); [Farquhar](https://dfarq.homeip.net/what-scale-are-hot-wheels-cars)).
5. **Caricature, not shrinking.** Hot Wheels designers say the wheels are made "a lot bigger", the roof is crushed down and the body is sometimes widened, then the design is distilled to its caricature (second-hand quote via [RK Motors](https://www.rkmotors.com/blog/Hot-Wheels-Designs), attributed to GM designer Adam Barry; I could not find the original interview).
6. **Safety shapes Tomica.** Door mirrors are left off passenger cars, tips are rounded, and parts are thickened, because the line is for children from age three ([Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/); [Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ)).
7. **Wheels are the loudest tier signal.** Basic wheels are generic plastic (5-spoke, button, 10-spoke). Premium and collector lines have car-specific rims and rubber tyres ([Wikipedia: Tomica](https://en.wikipedia.org/wiki/Tomica_(toy_line)); [Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/)).
8. **Opening parts and suspension are a Tomica and Matchbox tradition** (doors, bonnet, sprung axles), but they cost stance. Premium Tomica mostly drops suspension for a better stance ([Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/)).
9. **Decoration stops at the part boundary.** Paint is on the metal body, tampo prints on top of it, and glazing, base and interior carry their own colours. There is no wrap-around livery across parts.
10. The checklist in section 5 and the "must reproduce" list in section 6 are written to be tested one line at a time.

---

## 1. Physical construction

### 1.1 The parts, in assembly order

| # | Part | Material | What it looks like | Sources |
|---|---|---|---|---|
| 1 | **Body casting** | Zamak (zinc, aluminium, magnesium, copper), pressure die-cast; flash trimmed, tumbled to smooth sharp edges | One solid shell: roof, pillars, bonnet, boot, wings and door skins. Glossy paint. Cast-in detail: door handles, vents, grille bars, badges. | [Wikipedia: die-cast toy](https://en.wikipedia.org/wiki/Die-cast_toy); [Škoda](https://www.skoda-storyboard.com/en/lifestyle/a-unique-look-at-how-skoda-model-cars-are-made/) |
| 2 | **Base (chassis)** | Plastic on modern mainline; metal on older lines, Tomica Dandy, Hot Wheels Car Culture and other premium lines | A thin floor plate with the underside moulded (engine, exhaust, axle seats), plus the lower bumpers, sills and valance. Black, grey or silver. Carries the maker's marks. | [Wikipedia: Tomica](https://en.wikipedia.org/wiki/Tomica_(toy_line)); [Wikipedia: Majorette](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer)); [Farquhar](https://dfarq.homeip.net/take-apart-a-hot-wheels-car-without-a-drill); [Walmart listing via search](https://www.walmart.com/ip/seort/113817509) |
| 3 | **Window unit** | Clear polystyrene or polycarbonate, often tinted (smoked, blue-green, amber) | One piece: windscreen, side lights, rear light. Sits inside the body from below. Pillars are the body's own metal. Some Matchbox and Tomica vans use dark glazing with mesh printed on. | [Muncle Mikes](https://munclemikes.com/pages/hot-wheels-identification-guide); **[photo]** `toys-tomica/02`, `toys-matchbox/05` |
| 4 | **Interior tub** | Injection-moulded plastic | Seats, dashboard and floor as a single part, usually a contrasting colour (black, tan, red, yellow). Almost no leg room. Snaps in with the glazing. | [The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy); [Farquhar](https://dfarq.homeip.net/what-scale-are-hot-wheels-cars) |
| 5 | **Wheels and tyres** | One-piece plastic (hub and tyre together) on basic lines; two-piece rim plus rubber tyre on premium lines | See section 1.3. | [Wikipedia: Hot Wheels](https://en.wikipedia.org/wiki/Hot_Wheels); [Wikipedia: Tomica](https://en.wikipedia.org/wiki/Tomica_(toy_line)) |
| 6 | **Axles** | Steel pins | Two axles through the base, held in tabs or seats. Wheels are pressed on to the ends. | [Historic Recreations teardown via search](https://historicrecreations.yolasite.com/inspiration.php) |
| 7 | **Fixings** | Rivets (one or two) or small self-tapping screws | Visible as a round or domed head on the underside, one near the nose and one near the tail. Older Tomica Dandy used two screws. | [Farquhar](https://dfarq.homeip.net/take-apart-a-hot-wheels-car-without-a-drill); [Toys NZ Dandy J04](https://toysnz.com/Tomica-Dandy_J04_ToyotaCelica-XX-2800GT-1978) |
| 8 | **Add-on parts** | Plastic, sometimes metal | Wings, bonnet scoops, exposed engines, roof beacons, light bars, number plates, lamp lenses. See 1.5. | **[photo]** `toys-matchbox/06`, `toys-tomica/02`, `07` |

Hot Wheels design sheets colour-block the "interior, base, windows, wheels and bodyshell" as separate pieces before tooling ([The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)). That is the strongest sourced statement that the part split is a design decision, not an accident of manufacture.

### 1.2 What the base carries

[inference, from the sources above and the photos] The base is not just a floor. It is how the toy gets its "lower body" in a second colour:

- **Front and rear bumpers or valances**, particularly on Tomica vans, 4x4s and saloons. **[photo]** `toys-tomica/02` (Hiace: black chin and bumper) and `toys-tomica/01` (Safari: black bumper, white plate block).
- **Sills and side skirts**, as a dark strip under the doors.
- **Grille and lamp surround** on cheaper toys (Majorette Renault Express has a silver-grey bumper and grille unit in the base colour). **[photo]** `toys-majorette/01`.
- **Axle seats, underside engine and exhaust moulding**, and maker marks.
- **Fixing posts** where the rivets pass up into the body.

On Majorette the base was silver metal at first and was changed to black plastic during the 1990s to cut cost ([Wikipedia](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer))). So the "dark lower strip" is a cost-driven choice that became part of the look.

### 1.3 Wheels, tyres and axles by brand

| Brand | Basic wheel | Premium wheel | Notes |
|---|---|---|---|
| **Tomica** | Chrome-look hub on black plastic tyre; commonly 5 spokes with studs (4 before 2007), also "button" and "racing" types. Chrome is hot-stamped aluminium foil. | Tomica Limited Vintage (TLV) has car-specific wheels with rubber tyres; Premium has more realistic plastic wheels. | Early cars had two-piece wheels (1971), one-piece since 1976. Dandy used hard black plastic tyres. ([Wikipedia](https://en.wikipedia.org/wiki/Tomica_(toy_line)); [Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ); [Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/)) |
| **Hot Wheels** | 3-spoke (1995-2008), 5-spoke (1995-), 10-spoke (2003-), open-hole 5-spoke (2005-), muscle-car 5-spoke (2010-). All one-piece plastic. | Real Riders: two-piece rim with rubber tyre; "deep dish" and "modern concave" variants. Redline era had wide plastic tyres with a red pin-stripe. | ([Wikipedia](https://en.wikipedia.org/wiki/Hot_Wheels); [Muncle Mikes](https://munclemikes.com/pages/hot-wheels-wheel-types); [Fandom wheel list via search](https://hotwheels.fandom.com/wiki/Wheel_types_(by_type))) |
| **Matchbox** | Early: metal, then plastic (silver, grey, black) wheels. Superfast (1969-70): narrow low-friction wheels, then wider tyres. | Collectors' line: two-piece wheels with rubber tyres. | ([Wikipedia: Matchbox](https://en.wikipedia.org/wiki/Matchbox_(brand))) **[photo]** `toys-matchbox/06`: small black tyre with a 5-dot silver hub. |
| **Majorette** | Black plastic tyre with silver 5-hole disc hub. | Rubber tyres on "Showroom Deluxe" type lines (retailer claim). | **[photo]** `toys-majorette/01`. Suspension: sprung in mid-1970s ([Wikipedia](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer))). |
| **Corgi / Dinky / Siku** | Rubber tyres on cast or plastic hubs; Siku tyres are separate rubber over sculpted wheels. | Corgi "jewelled" lamps and Take-Off wheels (1968). | ([Wikipedia: Corgi](https://en.wikipedia.org/wiki/Corgi_Toys); [Dinky](https://en.wikipedia.org/wiki/Dinky_Toys); [Siku](https://en.wikipedia.org/wiki/Siku_Toys)) |

Two visual rules fall out of the photos **[photo]**:

- **Basic tier:** a fat black tyre with a small bright hub, so the wheel reads as a black disc with a bright centre. `toys-tomica/02`, `toys-matchbox/06`.
- **Premium tier:** a dark or silver multi-spoke rim with a thin tyre and a visible tread or sidewall edge. `toys-tomica/07` (R34 black rims), `09` (S2000 with tread on the sidewall).

### 1.4 Suspension and opening parts

- **Tomica suspension** is pseudo-suspension: the axles are piano wire that flexes, giving the car a small bounce ([Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ)). The 1970 originals were described as leaf-sprung ([Nostalgic Car](https://japanesenostalgiccar.com/50-year-club-tomica/)). Regular Tomica usually has suspension. Premium mostly drops it for a better stance, with a few exceptions ([Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/)).
- **Opening parts**: Tomica copied the idea from Matchbox. Passenger cars typically open the side or back doors; work vehicles have their own function ([Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ)). Opening is model and year dependent: one 2019 GT-R had opening doors, a 2019 Civic Type R had none ([eBay review summary via search](https://www.ebay.com/usr/mmahmood2003)).
- **Matchbox** by the late 1960s had windows, interiors, spring suspension and some opening parts; "Moving Parts" (2019) is a dedicated opening-parts line ([Wikipedia](https://en.wikipedia.org/wiki/Matchbox_(brand))). Corgi pioneered the opening bonnet (1960), steerable wheels and jewelled lamps ([Wikipedia](https://en.wikipedia.org/wiki/Corgi_Toys)).
- **Effect on shut lines** [inference from photos]. A part that opens needs a gap wide enough to swing and a hinge, so its line is **wider and deeper** (about 0.5 to 1 mm at toy size) and usually runs all the way round the door, with the door cut out of the body's side and lower sill. A part that does not open gets only a **fine engraved line**. **[photo]** `toys-tomica/07` (R34 door cut with a visible gap along the front edge) against `toys-tomica/02` (Hiace doors: shallow engraved lines).
- **Hot Wheels mainline** does not open and does not have suspension. Track function (rolling, loops, boosters) sets ride height and clearance ([Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/)).

### 1.5 Lights and small parts

There are three tiers of lamp, often within one car **[photo]**:

| Tier | How it is done | Where seen |
|---|---|---|
| **Cast detail only** | Recessed or raised lamp shape cast in the body, painted silver or left body colour. | Cheapest basics, Majorette Renault (`toys-majorette/01`: grey lamps in the bumper unit). |
| **Tampo** | Flat printed dots or shapes on the body: red, amber and clear tail lamp groups, silver headlamps. | Tomica Premium S2000 tail lamps (`toys-tomica/09`); Hot Wheels body graphics. |
| **Separate insert** | A small clear or coloured plastic piece sitting flush in a recess: headlamp lens, tail lamp lens, indicators, roof beacons. | Tomica Hiace beacons (`02`), TLV-N Safari lamps and orange indicators (`01`), Premium R34 round tail lamps (`07`), Corgi "jewels". |

Majorette moved from clear plastic lamps to tampo-printed lamps later in its history ([Wikipedia](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer))). TLV-N lists headlamps, tail lamps, mirrors, number plate and exhaust as separate pieces ([Nostalgic Car](https://japanesenostalgiccar.com/minicars-tomica-limited-vintage-neo-honda-cr-x/)). A Premium review notes clear plastic lights on some models only ([Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/)).

Other small parts seen in the photos **[photo]**: separate wings with end plates (`toys-tomica/07`, `09`), a black bonnet scoop insert (`toys-matchbox/06`, `08`), chrome exposed engine (`toys-matchbox/04`, `07`), small metal exhaust tips (`toys-tomica/09`), number plates as printed black or white rectangles (`toys-tomica/01`, `07`, `09`).

**Mirrors:** Tomica omits door mirrors on passenger cars as a deliberate safety rule. Early Tomica had them, and they were removed because children could be hurt ([Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ); [Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/)). TLV-N, which is a display line, has mirrors **[photo]** (`toys-tomica/01`). Hot Wheels designers also change mirrors for safety ([RK Motors](https://www.rkmotors.com/blog/Hot-Wheels-Designs), secondary).

### 1.6 Assembly and rivets

The sequence [inference, matches the teardown sources]: the interior and glazing drop into the body; the base is aligned underneath with axles and wheels; one or two rivets (or screws) go through the base posts and are peened over. Factory rivets are smooth and consistent; drilled or distorted rivets mark a rebuilt car ([Muncle Mikes search summary](https://munclemikes.com/pages/hot-wheels-identification-guide)). Heating the rivet with a soldering iron softens the plastic and releases the base, which confirms the base is plastic on newer cars and metal (not openable that way) on older ones ([Farquhar](https://dfarq.homeip.net/take-apart-a-hot-wheels-car-without-a-drill)).

---

## 2. Surface language

Written sources say very little about panel-line depth or edge radius, so most of this section is **[inference]** from the photos, plus the few sourced statements.

### 2.1 Panel lines

| Feature | Observed in photos **[photo]** | Rule of thumb [inference] |
|---|---|---|
| Shut lines (door, bonnet, boot) | Engraved grooves, dark in the gloss paint. Continuous round the part. Door lines cut right down to the sill. | Width about 0.3 to 0.5 mm on a 70 mm car, which is about 0.4 to 0.7 percent of body length. Depth about 0.15 to 0.3 mm. Wider if the part opens. |
| Sliding-door rails, bumper breaks | Shallow engraved lines on vans. | Same width, shallower. |
| Roof, rain gutter, pillar edge | Small raised ridge or a step. | One step only; never a double line. |
| Fuel flap, small panels | Often dropped on basics. Present on Premium. | Optional. |
| Door handles | Small cast bumps or a recessed oval. | A single bump about 1.5 mm long. |

**[photo]** The Tomica Premium R34 shows a long front-edge door gap, a cast door handle and a fuel-flap bump (`toys-tomica/07`). The Premium S2000 shows a cast handle and a fuel flap (`09`).

### 2.2 Edge radii

- Safety-driven rounding is explicit for Tomica: rounded tips on pointed parts, thickened thin areas ([Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/)). Hot Wheels rounded corners and flattened rear profiles after track testing ([The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)).
- **[inference]** Radii are small but visible. Roof and sill edges about 0.5 to 1 mm at toy size (roughly 1 percent of body length). Wheel-arch lips are softly rounded. Cast panel edges read crisp, not sharp: a thin highlight line along every edge in photos. Tumbling after casting rounds all edges a little ([Škoda](https://www.skoda-storyboard.com/en/lifestyle/a-unique-look-at-how-skoda-model-cars-are-made/)).
- Thin or protruding parts (wings, aerials, mirrors, wipers) are either omitted, thickened or made as separate plastic parts.

### 2.3 Grilles, vents and lamps

- **Grilles**: cast recessed with raised bars, then painted silver or black, or flat tampo. **[photo]** `toys-tomica/01` (Safari: horizontal bars with silver paint on raised ribs), `toys-tomica/02` (Hiace: dark moulded grille).
- **Vents and scoops**: either cast recessed and painted black (NSX side intake, `toys-tomica/08`), or a separate black plastic insert (Matchbox bonnet scoop, `toys-matchbox/06`).
- **Lamps**: sit in a recess with a clean border. Separate inserts finish flush with the body, not proud of it.
- **Roof ribs and panels**: raised and engraved cast detail (Majorette van roof, `toys-majorette/01`).

### 2.4 What gets simplified or dropped

[inference, confirmed in part by the sources]

- Dropped: wipers (sometimes cast as a single ridge), door mirrors on Tomica, aerials, badges smaller than about 1 mm, door-sill trim, rear-view mirror inside, steering columns.
- Merged: bumper and grille and number plate into one base or body feature; windscreen frame into body pillar; side-window rubber into the pillar.
- Redrawn: window openings, door handles and wheel size ([The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)).
- 1970s British Dinky and others show cost-cut versions: chunky door frames, imprecise grilles, separately painted parts ([Wikipedia: Dinky](https://en.wikipedia.org/wiki/Dinky_Toys)).

### 2.5 Paint, tampo and livery

- Body paint is applied to the metal casting. Tomica uses electrostatic coating like a real car ([Nostalgic Car](https://japanesenostalgiccar.com/50-year-club-tomica/)). Hot Wheels uses enamel or Spectraflame, ([Wikipedia](https://en.wikipedia.org/wiki/Hot_Wheels)).
- Details and liveries are **tampo** (pad printing): a flat, thin print, one colour per pad, sitting on the paint. Genuine tampo is printed, not stickered ([Muncle Mikes summary](https://munclemikes.com/pages/hot-wheels-identification-guide)).
- **Where prints stop** **[photo]**: on a body panel they follow the surface and stop at the shut line or panel edge. They do not wrap onto the glazing, the base or the tyres. `toys-hotwheels/03` shows a race-stripe that runs along a wing and stops at the lower body; the circular event logo sits wholly inside the door panel.
- Parts moulded in colour (interior, base, wings, wheels) carry their own colours; contrast colours are chosen at design stage ([The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)).

---

## 3. Proportions and caricature by brand

### 3.1 Scale

| Brand | Nominal | Reality | Sources |
|---|---|---|---|
| **Tomica regular** | None; "sized to fit the box" | Box width 78 mm since 1970. Passenger cars about 1:60. Typical range 1:55 to 1:72. Long vehicles are a separate "Long" category. | [Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/); [Nostalgic Car](https://japanesenostalgiccar.com/50-year-club-tomica/); [Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ); [Lazada brand wiki](https://brandwiki.lazada.com.ph/tomica/) |
| **Tomica Premium** | "Various, to fit the regular large box" | Fairlady Z 1:58, Skyline and Gallardo 1:62. | [Lamley](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/) |
| **Tomica TLV / TLV-N** | True 1:64 | Rubber tyres and car-specific wheels allow the true scale. | [Nostalgic Car](https://japanesenostalgiccar.com/minicars-tomica-limited-vintage-neo-honda-cr-x/) |
| **Hot Wheels** | 1:64 | Measured wheelbase ratios 1:64.7, 1:66.2, 1:69.5, 1:79.6 on four cars; 2.5 to 3 in long; "size is driven by packaging, not scale". | [Farquhar](https://dfarq.homeip.net/what-scale-are-hot-wheels-cars) |
| **Matchbox 1-75** | "About 1:64" | Under 1:100 to over 1:64; 6.5 to 7.5 cm. | [Wikipedia](https://en.wikipedia.org/wiki/Matchbox_(brand)) |
| **Majorette** | About 1:64 | 1:55 to 1:76; 2.5 to 3 in. | [Wikipedia](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer)) |
| **Siku** | V-series 1:60; Super 1:55 | Larger vehicles in small packaging vary. | [Wikipedia](https://en.wikipedia.org/wiki/Siku_Toys) |

**Consequence for the engine** [inference]. Treat the footprint as a **box**, not a ratio: each car fits a fixed length envelope (roughly 70 mm on the toy, or an agreed in-game length), and the proportions inside it are adjusted. Short cars are scaled up to fill the box; long cars and buses are shortened (Tomica motorised buses and lorries look "disproportionately short", [Wikipedia](https://en.wikipedia.org/wiki/Tomica_(toy_line))).

### 3.2 Brand attitudes

| | Tomica | Hot Wheels | Matchbox | Majorette |
|---|---|---|---|---|
| Philosophy | A "signature stylised redesign", safe and rounded, play-first ([Mojo Nation](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/)) | Hot-rod and concept-car energy; exaggerated stance ([Wikipedia](https://en.wikipedia.org/wiki/Hot_Wheels)) | Realistic, modelled on real vehicles; fantasy lines failed ([Wikipedia](https://en.wikipedia.org/wiki/Matchbox_(brand))) | Practical play toy: sprung suspension, sprung doors ([Wikipedia](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer))) |
| Wheels vs body | Moderately large; generic hubs | Much larger; fill the arches | True to slightly large | Large, generic |
| Stance | Natural, slightly tall because of suspension | Low and raked, but with enough clearance for orange track ([Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/)) | Natural | Natural to raised (sprung) |
| Cabin | Real size, slightly chunky pillars | Roof crushed down | Real | Real |
| Detail | Medium; clean | High on body; fantasy additions | High; thin | Medium |

### 3.3 Measured exaggeration (one sample)

**[photo] [inference]** From the side-on photo of a Hot Wheels Camaro (`toys-hotwheels/03`), I measured pixel ratios:

| Ratio | Toy | Real Camaro (about 4.84 m long, 20 in wheels, 1.35 m tall) | Exaggeration |
|---|---|---|---|
| Wheel diameter / body length | about 0.19 | about 0.145 | wheels about 30 percent too big |
| Roof height / body length | about 0.32 | about 0.28 | body about 14 percent taller than a straight shrink |

This is a single photo of a toy on rough ground, so treat it as indicative, not a spec. It matches the designers' own account of "wheels a lot bigger" and the observation that Hot Wheels wheel arches may be enlarged to suit ([RK Motors](https://www.rkmotors.com/blog/Hot-Wheels-Designs)). The Tomica Hiace (`toys-tomica/02`) and Safari (`01`) show smaller exaggeration: wheels about 10 to 15 percent oversize by eye, and chunky pillars and sills.

### 3.4 Track width, clearance, and long/tall cars

- **Track width**: Hot Wheels axle width is fixed to fit track slots and packaging; the body is slightly wider than a true scale to hide this ([Farquhar](https://dfarq.homeip.net/what-scale-are-hot-wheels-cars) for packaging; track fit from [Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/)). [inference] A toy body is therefore about 5 to 15 percent wider relative to its length than the real car, and wheels sit flush or slightly proud of the arch.
- **Ground clearance**: more than a real low-slung car. Hot Wheels needs approach and departure angles for the orange track ([Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/)). Tomica carries suspension travel. [inference] Typical gap under the sill is 1 to 2 mm at toy size.
- **Long vehicles are shortened** (buses, vans, trucks), **tall ones are squashed** or kept chunky so they stay stable, and **short ones are enlarged** to fill the box. Designers say wheel sizes, window openings and handles "would not translate well at perfect scale" ([The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy)).

---

## 4. Per category: how toys abstract each kind of car

The written sources do not break this down by category, so the tables below come from the photos (Tomica, Matchbox, Majorette, Corgi, Norev, Siku) and general knowledge. **All are [inference]** unless a photo is cited.

### 4.1 Road cars

| Category | Components present | Signature features always kept | Typical abstraction |
|---|---|---|---|
| **Small city car** (kei, Mini, 2CV) | Body, base, glazing, interior, 4 wheels; lamps as tampo or inserts | Tall upright greenhouse, round or boxy lamps, big window area, wheels at the corners | Scaled up to fill the box, so it looks chunky; wheels large; very short overhangs. **[photo]** `toys-other/02` (Corgi Mini), `17` (Norev 2CV) |
| **Family hatch** | As above; opening doors on Tomica; tailgate line | Sloping hatch rear, rear wiper bump, side-window shape, roof line | Mild stretch of cabin; pillars thickened |
| **Saloon** | As above; boot line | Three-box profile, boot lid line, grille and lamp pair, long rear window | Bonnet and boot kept proportionate; cabin slightly oversize. **[photo]** `toys-other/12` (Dinky Opel Rekord), `toys-matchbox/14` |
| **Estate** | As above plus roof rails or rack | Long roof, tailgate, D-pillar glass | Roof height kept; rails sometimes cast low. **[photo]** `toys-matchbox/13` (Studebaker Wagonaire) |
| **Van** | Body, base, glazing (often dark or none behind cab), interior in cab only, roof detail | Cab-forward face, sliding door line, roof ribs, side-window panels printed or mesh, beacons on service vans | Cargo area cast solid; long vans shortened. **[photo]** `toys-tomica/02`, `03` (Hiace police), `toys-majorette/01` (Renault Express), `toys-other/15` (Norev Master) |
| **Pickup** | Body, base, glazing for cab, open bed | Cab shape, open load bed with raised sides, tailgate line, ride height | Bed shortened. **[photo]** `toys-other/01` (Corgi Land Rover pickup), `toys-matchbox/03` (pickup in the group) |
| **4x4** | As above plus roof rack, spare wheel, bumpers | Upright slab sides, flat bonnet, square arches, thick tyres, raised stance, black bumpers | Tyres deep and chunky with tread; sills raised. **[photo]** `toys-tomica/01` (TLV-N Safari: black bumper, deep tread tyres, steel hubs) |

### 4.2 Sports and race cars

| Category | Components present | Signature features always kept | Typical abstraction |
|---|---|---|---|
| **Sports coupe** | Body, base, glazing, interior, wing or spoiler (separate or cast), exhausts | Long bonnet or wide stance, low roof, rear lamps, side intakes | Roof crushed; wheels big; wing sometimes oversize. **[photo]** `toys-tomica/07` (R34), `08` (NSX), `toys-hotwheels/03` |
| **70s wedge supercar** | Body, base, glazing with large rear window or louvres, interior, intake, wheels | Low pointed nose, flat or hooked wedge profile, cab-forward glazing, engine cover or louvres, chunky rear arches | Louvres and intakes as cast or inserted black plastic. **[photo]** `toys-matchbox/06`, `08` (AMX Javelin: amber glass, black scoop insert), `toys-matchbox/07` (Pi-Eyed Piper), `toys-other/09` (Siku Gallardo) |
| **Roadster** | Body, base, interior with seats, thin windscreen frame, soft top or hardtop as a separate part, small wing or roll hoops | Cut-away cabin, windscreen frame, tonneau or soft-top line, rear lamps | No glazing except a short windscreen; the interior tub is the main visible detail. **[photo]** `toys-tomica/09`, `10` (S2000 open), `toys-matchbox/04` (open Jeep) |
| **Rally car** | Body, base, glazing, interior, roof scoop, big wing, auxiliary lamp bar, mud flaps | Livery, big rear wing, wide arches, roof vent, bonnet vents | Mostly tampo livery; lamps as dots. **[photo]** `toys-matchbox/02` (Impreza rally livery, small photo) |
| **Touring or GT race car** | Body, base, glazing (often blacked out), interior, wing, splitter, numbers | Wide arches, big wing, splitter, number panel, sponsor livery | Aero parts separate or thick; windows dark. **[photo]** `toys-other/34` (Porsche GT3 RSR in livery) |
| **Le Mans prototype, closed** | Body, base, canopy glazing, small interior, rear wing, fin, wheel covers | Long tail, central fin, covered wheels, sponsored livery | Wheel covers cast in body; canopy tinted. [inference, no photo] |
| **Le Mans prototype, open** | As above but open cockpit; driver hump | Open cockpit with headrest or roll hoop, long tail fin | Cockpit as recessed interior tub with a driver hump. [inference, no photo] |
| **Single-seater (F1 or open-wheel)** | Body (nose, sidepods), base, separate wheels outside body, halo or roll hoop, front and rear wing | Exposed wheels, wide nose, wings with end plates, tall rear wing, driver helmet | Wings thickened; wheels on pods; halo as separate plastic or omitted. Premium Hot Wheels F1 has a metal chassis, Real Riders tyres with Pirelli branding ([Motor1 Spain / Visor search](https://visor.ph/culture/hot-wheels-unveils-formula-1-toy-cars/)) |

For the three motorsport categories without photos (closed and open Le Mans, single-seater), I found no reliable review of a 1:64 casting that describes the build, so the descriptions are general knowledge about how open-wheel and prototype toys are made (separate wings, thickened end plates, wheels outside the body). Treat them as hypotheses to check against a handful of toys when the engine is built.

---

## 5. Component checklist (acceptance list)

R = required for all cars. C = required for the category named. O = optional (appears on some toys).

| ID | Component | Status | Test |
|---|---|---|---|
| K1 | Metal-look body shell (glossy, painted) | R | One shell mesh with a single body colour |
| K2 | Separate base part, dark or neutral colour | R | Distinct material and mesh; visible in underside view |
| K3 | Lower bumpers, sills and valance on the base | R (cars, vans, 4x4); O (open-wheel) | Dark strip along the lower body edge |
| K4 | Underside detail (axle seats, exhaust, engine suggestion) | R | Shapes visible from below |
| K5 | Window unit (single glazing piece) | R (closed cars); C (roadster: windscreen only; open-wheel: none) | One transparent mesh covering all glass |
| K6 | Glazing tint | O (smoked, blue-green, amber) | Colour is a parameter |
| K7 | Interior tub (seats, dash, floor) | R (not for single-seater without cockpit detail) | Visible through glass |
| K8 | Four wheels as hub + tyre | R | Two materials, tyre black or dark |
| K9 | Two axles, one per pair | R | Wheel centres on a straight line |
| K10 | Fixing points (rivets or screws) | R | Two small round marks underneath |
| K11 | Front lamps | R | Recessed; tampo, painted chrome or insert |
| K12 | Rear lamps | R | Recessed; red, amber and clear zones |
| K13 | Grille (cast recess with bars or flat print) | R (front-engined); O (mid or rear-engined) | A distinct dark or silver panel |
| K14 | Door shut lines | R | Engraved groove, continuous |
| K15 | Bonnet and boot shut lines | R | Engraved groove |
| K16 | Door handles | R (basic); O (hidden) | A tiny bump or recess per door |
| K17 | Wing or spoiler (separate part) | C (sports, GT, rally, F1) | Distinct plastic-like part |
| K18 | Bonnet scoop, vents, louvres | C (wedge, muscle, rally, supercar) | Insert or cast recess in black |
| K19 | Exhaust tips | O | Small round tips |
| K20 | Number plate (printed rectangle) | O | Flat print, white or black |
| K21 | Door mirrors | O (omitted on Tomica passenger cars; present on collector lines) | Switchable; default off for the Tomica look |
| K22 | Wipers | O | Usually a ridge or omitted |
| K23 | Roof accessories (beacon, light bar, rack) | C (service vans, 4x4, rally) | Separate coloured parts |
| K24 | Tampo graphics (livery, stripes, logos) | O (R for race cars) | Flat prints that stop at part edges |
| K25 | Opening parts (doors, bonnet, boot) | O | If present, wider shut lines |
| K26 | Suspension (Tomica style) | O | Small travel; raise ride height |
| K27 | Driver figure or helmet | O (open cockpit, F1) | A head or helmet bump in the tub |
| K28 | Tyre tread on sidewall | O (premium); R for 4x4 | Visible edge on tyre |
| K29 | Exposed engine or chrome detail | O (hot-rod and wedge toys) | Separate bright part |
| K30 | Cabin soft top or hardtop | C (convertible) | Separate part |

---

## 6. What the engine must reproduce

U = universal. C = per category. Each line is testable by inspecting a built car.

1. **U.** The car is built from separate parts: body, base, glazing, interior, wheels. Each has its own material. (Sections 1.1, 5.)
2. **U.** The base is a separate dark part carrying the lower bumpers, sills and underside. Its colour is not the body colour. [Tomica Hiace and Safari photos; Majorette base notes.]
3. **U.** The body colour stops at the base line; there is no body colour on the underside.
4. **U.** The glazing is a single piece that fills every window opening, and it is slightly recessed behind the pillars.
5. **U.** An interior tub is visible through the glass, in a contrasting colour, with almost no depth.
6. **U.** Each wheel has a hub and a tyre as two materials. The tyre is dark; the hub is bright or contrasting.
7. **U.** Wheels are exaggerated: about 10 to 30 percent bigger than true scale, filling the arch. The Hot Wheels sample measured about 30 percent. Tomica is nearer 10 to 15 percent. [One photo sample; verify.]
8. **U.** Lamps are recessed and flush with the body, never proud. They are tampo, painted or insert; none are modelled as thin protruding bulbs.
9. **U.** Shut lines are engraved grooves roughly 0.4 to 0.7 percent of body length wide, continuous round each door and panel, deepest where a part opens. [inference]
10. **U.** All edges carry a small radius (about 1 percent of body length); no knife edges, spikes or thin protrusions.
11. **U.** Small details are dropped or merged: wipers, aerials, tiny badges, side-window rubbers. No part is thinner than about 0.5 mm at toy size. [inference]
12. **U.** Tampo and livery sit on the body panel and stop at shut lines and panel edges. They never wrap onto glass, base or tyres.
13. **U.** The footprint fits a fixed box. Length is set by the box, not by the real car's length. Long vehicles are shortened and short ones enlarged.
14. **U.** Two visible fixing marks (rivets or screws) on the underside, one near each end.
15. **U.** Ride height leaves a small, even gap under the sill, larger than a real low car.
16. **C (Tomica look).** Door mirrors are omitted on passenger cars; all tips are rounded.
17. **C (Tomica look).** Suspension travel is allowed; wheels sit slightly proud of the arch with some gap.
18. **C (Hot Wheels look).** Roof is lowered about 10 to 15 percent, wheels are enlarged by about 30 percent, and stance is lower and wider.
19. **C (Matchbox look).** Proportions stay close to real; windows tinted amber or blue; exposed engine or scoop in black or chrome on hot-rod or wedge toys.
20. **C (vans).** Cab glazing is clear; cargo-area side glass is dark or printed with mesh; roof ribs or beacons are cast or separate. [Hiace, Renault Express photos.]
21. **C (4x4).** Slab sides, upright windscreen, deep-tread tyres, raised stance, a black bumper from the base.
22. **C (wedge supercar).** Pointed nose, cab-forward cabin, a rear glass that is large or louvred, a black scoop or louvre insert, chunky rear arches.
23. **C (roadster).** The interior tub is the main visible detail; only a short windscreen is glazed; the hood line is a separate or cast panel.
24. **C (rally and GT race car).** A livery is mandatory; the wing is a separate thick part with end plates; windows may be dark.
25. **C (Le Mans prototype).** Closed type: tinted canopy, central fin, covered wheels, long tail. Open type: a recessed cockpit with a driver hump. [no photo evidence; verify]
26. **C (single-seater).** Wheels outside the body, thickened wings with end plates, a roll hoop or halo as a thick part, a helmet bump; premium tier uses rubber-look tyres with brand text.
27. **C (premium tier).** Rubber-look tyres with a visible sidewall edge and car-specific multi-spoke rims; lamps as separate inserts; metal-look base.

---

## 7. Reference photos (Wikimedia Commons only)

Fetched with `tools/forge-refs.py`, which writes licences to `sources.txt` in each folder. Non-toy images (real cars in toy liveries, crowds, boots, instruments) were deleted along with their source lines.

| Folder | Images | Best for |
|---|---|---|
| `review/forge/refs/toys-tomica/` | 9 | Hiace van (02, 03), TLV-N Safari 4x4 (01), Premium Evo VI (05), R34 (07), NSX (06, 08), S2000 (09, 10) |
| `review/forge/refs/toys-matchbox/` | 13 | Superfast wedge and hot-rod castings (04 to 08), older estates and saloons (13, 14), buses (10, 12), wreck pile showing bases and wheels (11) |
| `review/forge/refs/toys-hotwheels/` | 2 | Camaro side view with tampo and wheels (03), Impala (04). Commons has few real Hot Wheels toy photos; most results were full-size show cars. |
| `review/forge/refs/toys-majorette/` | 3 | Renault Express vans with base-colour bumper unit (01), dump truck (02), space-shuttle transporter (06) |
| `review/forge/refs/toys-other/` | 19 | Corgi, Dinky, Siku and Norev road cars, vans and trucks; Maisto bus and Viper; a livery GT3 |

Total: **46 images**. Gaps: no Commons toy photos of Hot Wheels F1 or Le Mans prototype toys, no Tomica 70s wedge. Evidence for those categories should be gathered from shop and collector pages (cited as URLs, not downloaded) before the engine's category rules are frozen.

---

## 8. Sources

Tomica: [Wikipedia](https://en.wikipedia.org/wiki/Tomica_(toy_line)), [Japanese Wikipedia](https://ja.wikipedia.org/wiki/トミカ), [Mojo Nation interview with Shunsuke Takeuchi](https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/), [50 Year Club (Nostalgic Car)](https://japanesenostalgiccar.com/50-year-club-tomica/), [TLV-N CR-X review](https://japanesenostalgiccar.com/minicars-tomica-limited-vintage-neo-honda-cr-x/), [Lamley Premium](https://lamleygroup.com/2020/02/11/why-you-should-collect-tomica-premium-including-new-release-for-2020/), [Lamley Premium showcase](https://lamleygroup.com/2018/05/12/showcase-jumping-into-tomica-premium/), [Toys NZ Dandy J04](https://toysnz.com/Tomica-Dandy_J04_ToyotaCelica-XX-2800GT-1978).
Hot Wheels: [Wikipedia](https://en.wikipedia.org/wiki/Hot_Wheels), [Develop3D](https://develop3d.com/3d-printing/hot-wheels-from-custom-car-to-die-cast-collectible/), [The Drive](https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy), [RK Motors](https://www.rkmotors.com/blog/Hot-Wheels-Designs), [Farquhar on scale](https://dfarq.homeip.net/what-scale-are-hot-wheels-cars), [Farquhar teardown](https://dfarq.homeip.net/take-apart-a-hot-wheels-car-without-a-drill), [Muncle Mikes wheels](https://munclemikes.com/pages/hot-wheels-wheel-types), [CAR magazine test](https://products.carmagazine.co.uk/car-accessories/gifts/hot-wheels-test/).
Others: [Wikipedia: Matchbox](https://en.wikipedia.org/wiki/Matchbox_(brand)), [Majorette](https://en.wikipedia.org/wiki/Majorette_(toy_manufacturer)), [Corgi](https://en.wikipedia.org/wiki/Corgi_Toys), [Dinky](https://en.wikipedia.org/wiki/Dinky_Toys), [Siku](https://en.wikipedia.org/wiki/Siku_Toys), [die-cast toy](https://en.wikipedia.org/wiki/Die-cast_toy), [Škoda factory feature](https://www.skoda-storyboard.com/en/lifestyle/a-unique-look-at-how-skoda-model-cars-are-made/).

Fetch limits: Hot Wheels and Matchbox fandom wikis returned HTTP 402 and could not be read, and the Hagerty and Motor1 articles returned errors. Fandom facts above come only from search-result summaries and are marked "via search".
