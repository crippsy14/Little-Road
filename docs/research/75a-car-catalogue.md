# Little Road: reference car catalogue (classes C, B, A, S, E)

Research date: 13 Oct 2026. Goal: maximise DISTINCT SILHOUETTES, not famous models.

How to read this: web search was strong on principles, legal and some history (Hot Wheels design process, Tomica, Choro-Q, Kamm tail, wedges, shooting brakes, Pike cars, Group 5/B, Le Mans classes), and weak on per-car shape detail. Shape descriptions in the tables are therefore mostly from general automotive knowledge, with the bracketed source numbers `[n]` pointing to the source list where a fact was checked. Anything marked `(verify)` is lower confidence. Years are production/competition span, rounded.

Shorthand:
- Cabin: **CF** cab-forward, **CM** cab-mid, **CR** cab-rearward (long bonnet, cabin set back). **Mid-eng** = engine behind the driver.
- Overhangs: **sh** short, **lg** long. WB = wheelbase.
- Proportion words: "tall box" (height roughly 0.4 of length or more), "low wedge" (height about 0.25-0.28 of length), "wide" (width over 0.45 of length).
- Toy-read tag in the last column = the one feature that must survive stylisation.

---

## 1. Principles: making cars READ distinct at toy scale

| # | Principle | Evidence / source |
|---|---|---|
| 1 | **Caricature, do not scale down.** Shrinking a real car makes wheels look too small and the roof too big. Hot Wheels designers enlarge the wheels, crush the roof down, often widen the body, and "distil the design to its caricature". | Hot Wheels designers quoted in PopSci and The Drive [1][2] |
| 2 | **Pick the 2-3 character-defining features per car and emphasise them; sacrifice the rest.** Hot Wheels: identify what gives the car "visual personality", emphasise it, drop less important detail. Tomica: a "signature stylized redesign" so the miniature conveys the real car's shape. | [1][2][3] |
| 3 | **Rework window openings, door handles, wheel sizes rather than copying.** These details do not translate at scale; redraw them so they stay legible. | [2] |
| 4 | **Make function shape the form, invisibly.** Hot Wheels widened fenders to the track width and lowered the roof for centre of gravity, without losing the silhouette. In Little Road terms: a shared wheel-track and ride-height rule can still allow very different bodies. | Hot Wheels Beach Bomb history [1][4] |
| 5 | **Normalise footprint, vary proportion.** Choro-Q renders very different cars at the same toy size (a Subaru 360 equals a Hummer H2 in size), with shortened bodies and bigger rear wheels. Tomica sizes each model to the box, not to a true scale. Implication: let class set a footprint budget but let each body differ in height, nose length, cabin position and wheel size. | [3][5][6] |
| 6 | **Side profile carries identity; add one profile "kink".** A 2022 car-styling paper treats the side view as holding silhouette, proportion and the whole image of the car, and cites single profile features (BMW Hofmeister kink) as brand signatures. For a chase-camera game, also test the REAR silhouette (our inference). | [7] |
| 7 | **Silhouette can encode performance.** PixelRacer's side-profile-only design found a sharp, low-area triangle became the default "fast" shape. Use: low wedge = fast, tall box = slow/handling-heavy; it makes class reading automatic. | [8] |
| 8 | **Shape-to-collision honesty.** Rocket League uses six standard hitbox shapes (Octane, Dominus, Breakout, Plank, Hybrid, Merc); players favour cars whose visible edges match the hitbox. Keep the visible body edges close to the physics shape so the car reads "fair". | [9] |
| 9 | **Colour-block early, separate body / trim / glass.** Hot Wheels plans base, interior and trim colours at design stage and checks the scheme on the final form. Two-tone blocking helps a silhouette read at 40 px. | [2] |
| 10 | **Scale the "hero" exaggeration by role.** Nintendo made the Master Cycle "a little bigger" than other bikes so it leaves an impression on track. Rule for us: halo cars (class A/E) get the loudest silhouette feature (wing, canopy, tail), bangers get the quietest. | [10] |
| 11 | **Unlicensed games use homage cars with invented names.** Burnout Paradise gave cars fictional makers/models "loosely based" on real ones (e.g. a DeLorean homage); Wreckfest has "literally no official licensed content", with a Skyline-R33 tribute (Razor) and an Opel-Manta-like car (Stellar), plus joke vehicles (couch, mower, outhouse on a kart). | [11][12] |
| 12 | **Mix real archetypes and gag vehicles.** Wreckfest's bangers sit next to hot rods, tailfin cars, Euro coupes and rear-engine 80s coupes; variety of era is what makes a grid readable. | [12] |

Practical silhouette test for the pipeline (our suggestion): render each body as a flat black side view AND rear view at 64 px; if two bodies in the same class are confusable, change one of: nose height, cabin position, roofline type (fastback / notchback / hatch / bubble / targa), tail type (Kamm / duck / wing / fin), or wheel-arch treatment.

---

## 2. Class C: family / everyday (30)

| Car | Years / ctry | Defining shape features (survive stylisation) | Proportions | Archetype |
|---|---|---|---|---|
| Fiat Panda Mk1 | 1980-2003 IT | Flat panels, flat glass, "tool-box" slab sides, square wheel arches, upright tailgate | Tall box, sh/sh overhang, CF | Supermini / box |
| Fiat 500 Nuova | 1957-75 IT | Egg-shaped, rear engine, roll-back fabric roof, tiny, bulb headlights, bonnet that slopes to the ground | Tiny, rounded, tall-ish, WB short | City car |
| Classic Mini | 1959-2000 UK | Wheel at each corner, 10 in wheels, exposed welded seams, upright greenhouse, boot lid hinge-down | Very short, boxy, near-zero overhang | City car |
| Citroen Ami 6 | 1961-69 FR | Reverse-slope (inverted) rear window, oval headlight bulges, odd double-bubble roof | Short, notchback with tail inverted | 2-door oddball |
| Volkswagen Beetle | 1938-2003 DE | Domed teardrop, separate swooping fenders and running boards, round headlights on fender tops | Short, rounded, CM, rear-eng | Classic 2-door |
| Volkswagen Golf Mk1 | 1974-83 DE | Crisp Giugiaro box hatch, thick C-pillar, rectangular lamps, flat bonnet | Compact, CM, sh overhang | Supermini / hatch |
| Fiat Multipla | 1998-2010 IT | Double "bubble" greenhouse, lower lamp cluster, 6-seat 2x3 monobox | Short but very wide and tall | Quirky MPV |
| Renault Espace Mk1 | 1984-91 FR | Monobox, huge glass area, steep windscreen, flush glass | Tall, CF, long cabin | MPV / people carrier |
| Volkswagen Type 2 (T1 Splitscreen) | 1950-67 DE | V-shaped nose with big badge, split windscreen, huge glass, rear-engine | Bread-loaf box, near-vertical front | Van-based / MPV |
| Citroen HY | 1947-81 FR | Corrugated steel panels, nose with vertical grille bars, cab-forward van | Tall box, CF | Van-based |
| Smart fortwo | 1998- DE | Tridion safety cell in contrasting colour, chopped tail, two seats | Almost as wide as long/2; tall | City car |
| Honda N-Box | 2011- JP | Tall slab-sided kei box, near-vertical front and rear | Tallest-per-length, boxiest | Kei tall-wagon |
| Suzuki Carry / Subaru Sambar | 1960s- JP | Cab-over, tiny flat load bed (truck) or box (van), tall cab | CF, tiny | Kei truck/van |
| Toyota Prius (Gen 2) | 2003-09 JP | Triangular profile, high Kamm rump, short nose | CF, tall tail | Hybrid hatch |
| Honda Fit / Jazz | 2001-2008 JP | One-box mini-MPV, short nose, high floor | CF, tall | Supermini MPV |
| Nissan Cube (Z12) | 2008-14 JP | Asymmetric wraparound rear window, rounded box, ripple-effect detail | Tall rounded box | Quirky kei-ish |
| Tata Nano | 2008-18 IN | Tall egg with tiny wheels at corners, single wiper | Tall, very short | City car |
| Maruti 800 (Suzuki Alto SS80) | 1983-2014 IN | Boxy two-box, large rear glass hatch, flat panels | Small box | City car |
| Mercedes-Benz W123 saloon | 1976-85 DE | Upright three-box, vertical chrome grille, big flat roofline, wraparound tail lamps | Roomy, balanced | 3-box saloon |
| Ford Cortina Mk2 | 1966-70 UK | Flat-sided box saloon, narrow pillars, Coke-bottle hint | Long box | 3-box saloon |
| Ford Anglia 105E | 1959-67 UK | Reverse-slope rear window with chrome swept fins (small tail fins), bug-eye lamps | Small notchback | 2-door classic |
| Chevrolet Bel Air | 1955-57 US | Chrome bumpers, tailfins, two-tone side sweep, wide grille smile | Long, low-ish, wide | 2-door classic |
| Ford Country Squire | 1950s-70s US | Fake wood side panels, long roof, huge chrome | Very long, wagon | Estate |
| Volvo Amazon estate / 940 wagon | 1962-70 / 1990-98 SE | Square tail, vertical rear window, boxy hip line | Long, boxy | Estate |
| Dodge Caravan / Plymouth Voyager | 1984- US | Minivan wedge-box, steep screen, sliding door, fake wood option | Tall, long wheelbase | MPV |
| Mitsubishi Delica Star Wagon | 1982-94 JP | Cab-over van-based 4x4 with flat nose, high ride | CF, tall | Van-based MPV |
| Renault Twizy | 2012- FR | Open-sided pod, tandem seats, scissor doors | Tiny, tall | Microcar |
| Isetta (BMW) | 1955-62 IT/DE | Front-opening bubble door, rounded egg, narrow rear track | Short, tall bubble | Microcar |
| Land Rover Series / Defender | 1948-2016 UK | Slab sides, flat panels, high ride, square arches, upright windscreen | Boxy, short overhangs | Utility 4x4 |
| Toyota Hilux (4th gen) | 1983-88 JP | Simple pickup: cab + short bed, high ride, rectangular lamps | Long WB, CM | Pickup |

Fast notes: kei trucks and the Fiat Multipla/Renault Twizy give silhouettes nothing else in the catalogue has; keep at least one of each. Ford Anglia and Citroen Ami 6 both use reversed rear glass but differ in tail fins vs bubble roof.

---

## 3. Class C "bangers" (cheap-looking, old, slow) (26)

| Car | Years / ctry | Defining shape features | Proportions | Why it reads "slow/cheap" |
|---|---|---|---|---|
| Trabant 601 | 1963-90 DD | Boxy slab body (Duroplast), tiny two-stroke, flat panels, slab roof; looks like a "shrunk Peugeot 404" [13] | Short, tall box | Flat slabs and tiny wheels |
| Reliant Robin | 1973-2002 UK | Three wheels, single front wheel, wedge-ish fibreglass body, rear-opening glass [14] | Tall, short, narrow front | Wobbly stance |
| Lada Riva (VAZ-2105/07) | 1980-2012 SU | Squared-off Fiat 124 derivative, chrome bumpers, flat bonnet, flat grille with big badge | Long box | Upright boxy |
| Austin Allegro | 1973-82 UK | Bulbous bodywork, round hood, "Quartic" square wheel option, heavy rear overhang | Short bonnet, fat rear | Lumpy |
| Citroen 2CV | 1948-90 FR | Corrugated bonnet, big exposed fender wheels, bulb headlamps on fenders, rolled canvas roof | Tall, soft, high suspension travel | Tall sway |
| Yugo 45/GV | 1980-92 YU | Boxy hatch, Giugiaro Fiat 127 shape, flat nose | Tiny box | Cheap flat |
| Morris Minor 1000 | 1948-71 UK | Rounded, split screen, small oval grille, separate wings, tall rump | Short, bulbous | Chubby |
| Hindustan Ambassador | 1958-2014 IN | Bulbous Morris Oxford III shape, chrome bumpers and grille, domed roof | Long, upright, tall | Retro chrome |
| Volvo 240 | 1974-93 SE | Brick: flat panels, squarely styled grille, estate version a vertical rear | Long, tall box | Brick |
| Renault 4 | 1961-92 FR | Boxy van-like hatch, long travel suspension, slightly leaning, thin tall wheels | Tall, high, short | Utilitarian |
| Skoda Estelle (120) | 1976-90 CS | Rear-engined 3-box, squared nose, black plastic bumpers | Short box | Flat rear-engine |
| Fiat 126p | 1972-2000 PL | Squared 500 evolution, tiny, rear engine | Tiny, square | Tiny squashed |
| Daewoo Tico | 1991-2001 KR | Tall narrow hatchback, Suzuki Alto lineage | Tiny, tall | Doll-like |
| Moskvitch 412 | 1967-97 SU | Narrow three-box with hockey-stick chrome side line, flat front | Long, narrow | Plain |
| Wartburg 353 | 1966-89 DD | Boxy 1960s saloon, flat sides, estate version [13] | Long, flat | Smoky flat |
| Dacia 1300 | 1969-2004 RO | Renault 12-derived; plain squared saloon | Medium box | Blandness |
| ZAZ-968 Zaporozhets | 1971-94 SU | Rear-engine, side air intakes ("ears"), fake grille [13] | Short, rounded box | Ears |
| Hillman Imp | 1963-76 UK | Rear-engine, hinged hatch window, Mini-sized | Short | Fussy |
| Austin Maxi | 1969-81 UK | Five-door hatch, Pininfarina-ish slab; flat roof | Long, tall | Dumpy |
| Triumph Herald | 1959-71 UK | Michelotti, forward-hinged bonnet, fins at rear | Short, with large bonnet | Cute 60s |
| AMC Pacer | 1975-80 US | Fishbowl glass, asymmetric doors, short rounded nose | Wide, tall, short | Bubble |
| AMC Gremlin | 1970-78 US | Chopped Kamm tail, long nose | Long hood, stubby tail | Awkward |
| Ford Pinto | 1971-80 US | Humpbacked small saloon/hatch, small round lamps | Small, rounded | Plain |
| Checker Marathon | 1960-82 US | Taxi, tall boxy 3-box with huge doors | Tall, long | Taxi |
| FSO Polonez | 1978-2002 PL | Boxy hatch with hatch glass, rectangular lamps | Medium box | Flat |
| Nissan Pao / Figaro (as retro-cheap) | 1989-91 JP | Pike Factory retro: round lamps, exposed hinges, Mini/Fiat 500 cues [15] | Small, rounded | Retro-cute |

Also usable as "bangers" at lower toy-prestige: Subaru 360 ("ladybug" shape [16]), Honda N360, Mazda Carol, Suzuki Fronte, Reliant Regal/Rialto, Bond Bug (wedge three-wheeler, see B).

---

## 4. Class C top-end: "warm" cars for the fastest C races (10)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Ford Fiesta XR2 Mk1 | 1981-83 UK/DE | Squared supermini, black grille slats, clip-on spoiler, driving lamps | Short, tall-ish, CF |
| Volkswagen Golf GTI Mk1 | 1976-83 DE | Red-trimmed grille, black wheel-arch trims, wider tyres | Compact, CM |
| Fiat Uno Turbo i.e. | 1985-89 IT | Giugiaro box, tall rear, black body stripe, bonnet bulge | Short, tall, CF |
| Honda CR-X (EF/ED) | 1984-91 JP | Kamm-cut short tail, rear glass wraparound; low roofline | Short, low wedge-ish |
| Suzuki Swift GTi | 1989-2003 JP | Small hatch with rear spoiler, tidy | Short, light |
| Mini Cooper S | 1963-71 UK | Roof-rack-less box with contrasting roof, 2 headlamp driving lights | Tiny box |
| Citroen AX GTi | 1987-96 FR | Very light, flat sides, small rear glass, plastic bumper | Short, lightweight |
| Peugeot 205 GTI (also fits B) | 1984-94 FR | Clean Pininfarina hatch, black wheel arches, fat sills | Short, thin-pillared |
| Autobianchi A112 Abarth | 1971-85 IT | Square hatch, black bonnet scoop | Small box |
| Ford Escort XR3i (Mk3) | 1980-86 UK | Wedge nose and notched grille, "bumper-spoiler" and bonnet vents | Short, wedge |

---

## 5. Class B: tuner / sports cars by archetype (58)

Aim: no two B cars look alike. Japanese 90s tuners are capped at about 10 here; everything else widens the field.

### 5.1 Hot hatches (box and round) (9)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Peugeot 205 GTI | 1984-94 FR | Clean Pininfarina hatch, thin pillars, red grille trim | Short, tall greenhouse |
| Renault 5 Turbo | 1980-84 FR | Mid-engine, giant wide rear arches with air intakes behind the doors, plastic wheel flares | Short, very wide rear |
| Renault Clio V6 | 2001-05 FR | Mid-engine, huge rear arches, minuscule cabin and nose | Wide, low, CM |
| Lancia Delta HF Integrale Evo | 1991-94 IT | Giugiaro hatch + bulging fender flares, bonnet bulge, big wheels | Boxy, wide |
| Ford Escort RS Cosworth | 1992-96 UK | Huge "whale-tail" rear wing, bonnet intake, sleek nose | Hatch + big wing |
| Volkswagen Corrado VR6 | 1988-95 DE | Fastback hatch with pop-up spoiler, pop-up wing | Low, 2+2 |
| Nissan Pulsar GTi-R | 1990-94 JP | Bonnet scoop + enormous roof spoiler, boxy hatch | Short, big wing |
| Honda Civic Type R EK9 / FK2 | 1997 / 2015- JP | EK9 plain hatch; FK2 massive winglet + side vents | Compact |
| Opel Corsa OPC / Vauxhall Nova GTE | 1980s DE | Box with black bumper-spoiler | Small box |

### 5.2 Homologation / Group-B-style road cars (7)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Lancia 037 Stradale | 1982-84 IT | Pininfarina mid-engine; long fastback tail, rear vent grille; very low | Low, long tail |
| Ford RS200 | 1984-86 UK | Mid-engine; boxy, front-engined gearbox, flat tail; huge side intakes | Squat, wide |
| Peugeot 205 T16 | 1984-86 FR | Mid-engine 205 with wide arches and cut-out tail; boxy wing-less | Short, wide |
| MG Metro 6R4 | 1985-86 UK | Metro hatchback with wide arches, huge intakes (V6 mid) | Squat |
| Audi Quattro | 1980-91 DE | Boxy coupe with flared arches and chiselled lines; Sport version has short wheelbase | Short WB, square |
| Mitsubishi Pajero Evolution | 1997-99 JP | SUV with flared arches and bonnet scoop (Dakar homologation) | Tall, wide |
| Skoda 130 RS | 1975-80 CS | Rear-engine coupe "Porsche of the East" with rear spoiler | Low, short |

### 5.3 Muscle / pony cars (8)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Ford Mustang (fastback) | 1964-68 US | Long hood, short deck, "sharkmouth" grille, side scoop on the flank | CR, long nose |
| Chevrolet Camaro | 1967-69 US | Coke-bottle waist, bulged flanks, stripes | Wide, mid |
| Dodge Charger | 1968-70 US | Hidden headlights, tunnel-back roof, coke-bottle | Long, wide |
| Plymouth Superbird | 1970 US | Rounded nose cone, tall wing on stalks | Long wing |
| Pontiac Firebird Trans Am | 1977-81 US | Shaker hood scoop, black-wing front, bird-decal bonnet | Long hood |
| Ford Capri Mk1 | 1969-74 UK/DE | European Mustang, long bonnet, fastback, wide side flare | Low, long nose |
| Opel Manta A / Kadett GT/E | 1970-75 DE | Coupe, rounded, short rear, bonnet stripes | Short |
| Holden Monaro / Ford Falcon GT-HO / Valiant Charger E49 | 1968-73 AU | Australian muscle: Monaro (coupe), Falcon (saloon), Charger (short-wheelbase coupe) [17] | Medium |

### 5.4 Utes, pickups and vans with performance (7)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Holden HSV Maloo / Ford Falcon XR8 ute | 1990s-2017 AU | Coupe-utility: car front, open load bed behind cab, sports body kit [17] | Long, low |
| Chevrolet El Camino SS | 1964-87 US | Car-based pickup, long hood, sloping bed | Long, low |
| Ford F-150 SVT Lightning | 1993-95 / 1999-2004 US | Lowered pickup, deep front air dam, body-colour trim | Truck, low |
| GMC Syclone | 1991 US | Pickup with body kit, deep skirts, AWD | Short truck |
| Dodge Ram SRT-10 | 2004-06 US | Pickup with big bonnet scoop and V10, tight dress | Truck |
| Subaru Brat | 1978-87 JP | Car-based ute with rear-facing jump seats and T-tops | Small ute |
| Ford Transit Supervan | 1971 / 1994 UK | Race-car mid-engine under a van body | Van + wide arches |

### 5.5 European sports coupes and fastbacks (8)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Alfa Romeo Giulia Sprint GT / GTV | 1963-77 IT | Bertone, "scalino" step in the nose, fastback | Low, long nose |
| Alfa Romeo Montreal | 1970-77 IT | Louvred "eyelid" headlamp bezels, NACA bonnet scoop, Gandini | Wide, long |
| Alfa Romeo SZ "Il Mostro" | 1989-91 IT | Angular, almost no rear screen; wedge | Wide, low |
| Lancia Fulvia Coupe | 1965-76 IT | Small, curvy, crisp shoulder line | Small |
| Fiat Coupe 20v Turbo | 1993-2000 IT | Curved arches with crease, exposed rear lamp bar (verify) | Compact |
| Volvo P1800 / P1800ES | 1961-73 SE | Frua fins (Coupe), glass hatch ES (shooting brake) [18] | Medium |
| Saab 900 Turbo (hatch) | 1978-93 SE | Aero-hatch with huge glass and sloping nose | Hatch |
| Matra Bagheera | 1973-80 FR | Three-abreast seating (wide), mid-engine, fastback | Wide, low |

### 5.6 Mid-engine small sports and wedges (7)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Fiat X1/9 | 1972-89 IT | Wedge, targa roof, pop-up lamps, sail panels [19] | Low wedge |
| Lotus Europa | 1966-75 UK | Fastback "breadvan" with flying-buttress C-pillars | Very low |
| Pontiac Fiero | 1984-88 US | Mid-engine wedge, black plastic panels | Short wedge |
| Toyota MR2 (AW11) | 1984-89 JP | Wedge, sharp creases, pop-up lamps | Low wedge |
| Lancia Beta Montecarlo / Scorpion | 1975-81 IT | Targa with buttressed rear window | Small coupe |
| Triumph TR7 | 1975-81 UK | Wedge profile, wedge nose pop-ups, no fins [19] | Wedge |
| Porsche 914 | 1969-76 DE | Boxy targa, mid-engine, pop-ups | Flat, square |

### 5.7 Roadsters, lightweights and kei sports (9)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Caterham Seven | 1957- UK | Exposed cycle wings, open wheels, nose cone, no doors | Narrow, open |
| Morgan Plus 4 | 1950- UK | Separate flat-faced rad, louvred long bonnet, leather strap | Long hood |
| Lotus Elan | 1962-75 UK | Small, simple, pop-up lamps, taut pods | Small roadster |
| Fiat 124 Spider | 1966-85 IT | Pininfarina, scalloped sides, roadster | Medium |
| Mazda MX-5 NA | 1989-97 JP | Pop-up lamps, round teardrop nose, retro-Elan | Small |
| Honda S800 | 1966-70 JP | Tiny roadster, chain drive, round lamps | Tiny |
| Honda Beat | 1991-96 JP | Kei mid-engine roadster [16] | Tiny |
| Autozam AZ-1 | 1992-95 JP | Kei mid-engine gullwing coupe [16] | Tiny, wide |
| Suzuki Cappuccino | 1991-98 JP | Kei, removable roof panels [16] | Tiny |

### 5.8 Shooting brakes, hatches and wagons, sports estates (6)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Reliant Scimitar GTE | 1968-90 UK | Wedge-shaped shooting brake with high shoulder line [18] | Long |
| Lancia Beta HPE | 1975-84 IT | Coupe front, estate rear [18] | Medium |
| Volvo 850 T5-R wagon | 1995 SE | Long wagon, squared tail | Long |
| Ogle GTS / Ferrari Breadvan style | 1960s UK | Chopped Kamm tail estate [18] | Long |
| Kia Proceed GT / Mercedes CLA shooting brake | 2010s | Sloping roof, wide stance | Medium |
| Mini Clubman (Mk1) | 1969-80 UK | Longer nose, estate rear, barn doors (Traveller) | Small estate |

### 5.9 Quirky / retro-futurist / non-Japanese outliers (10)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Nissan Figaro | 1991 JP | Retro Pike car: round lamps, canvas roof [15] | Small |
| DeLorean DMC-12 | 1981-83 US/UK | Brushed stainless panels, gullwing doors, louvre rear [19] | Wedge, wide |
| BMW Z1 | 1989-91 DE | Drop-down doors that slide into sills | Small roadster |
| BMW 3.0 CSL "Batmobile" | 1972-75 DE | Fins, rear wing, roof spoiler | Long coupe |
| Bond Bug | 1970-74 UK | Wedge three-wheeler with lift-up canopy, orange [20] | Tiny wedge |
| Citroen SM | 1970-75 FR | Fish-nose with glass-covered lamps, Kamm tail [21] | Long, wide |
| Renault Alpine A110 | 1961-77 FR | Rear-engine fastback with four lamps | Low |
| Hyundai Veloster | 2011- KR | Asymmetric doors (one driver, two passenger) | Short hatch |
| Hyundai N Vision 74 | 2022 KR | Retro-future pixel lamps, pony-coupe wedge [22] | Wide, boxy |
| Peugeot RCZ | 2010-15 FR | "Double-bubble" roof, fastback | Coupe |

### 5.10 Japanese coupes / tuners (cap, 10)

| Car | Years | Defining features |
|---|---|---|
| Toyota AE86 Trueno/Levin | 1983-87 | Boxy, pop-ups (Trueno), flat panels |
| Nissan Fairlady Z (S30) | 1969-78 | Long nose, fastback, rounded |
| Mazda RX-7 (FD) | 1992-2002 | Curvy, low, pop-down (no) lamps |
| Nissan Silvia S13 | 1988-93 | Pop-up (180SX), small coupe |
| Toyota Celica GT-Four ST205 | 1994-99 | Bug-eye lamps, big wing |
| Mitsubishi Lancer Evo VI | 1999 | Tall wing, bonnet vents, boxy |
| Mitsubishi GTO / 3000GT VR-4 | 1990-2000 | Active aero, wide haunches |
| Subaru Impreza 22B | 1998 | Blister fenders, big wing |
| Subaru SVX | 1991-97 | Window-within-window |
| Mazda RX-8 | 2003-12 | Rear-hinged "freestyle" doors |

B count: 9 + 7 + 8 + 7 + 8 + 7 + 9 + 6 + 10 + 10 = 81 rows. Pick about 45 with the largest silhouette spread (see Section 10 for archetype counts).

---

## 6. Class A: fast sports cars and supercars by archetype (40)

### 6.1 Wedge supercars (70s-80s) (6)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Lamborghini Countach LP400 | 1974-90 IT | Scissor doors, NACA ducts in shoulders, periscope roof; later huge wing; sharp wedge [23] | Low, wide, CM |
| Lancia Stratos HF | 1973-75 IT | Bertone, bubble windscreen, extremely short WB, concave rear [23] | Tiny, wide |
| Lotus Esprit S1 | 1976-81 UK | Giugiaro "origami" folded wedge, flat sides [23] | Low, long cabin |
| BMW M1 | 1978-81 DE | Giugiaro, kidney grille, fastback, louvred rear glass | Low |
| De Tomaso Pantera | 1971-92 IT/US | Ford V8 wedge with deep rear arches | Low |
| Ferrari Testarossa | 1984-91 IT | Huge side strakes ("cheese grater"), very wide rear | Wide, low |

### 6.2 Classic 60s-70s curvy exotics (7)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Lamborghini Miura | 1966-73 IT | "Eyelashes" around lamps, louvred rear glass, transverse mid V12 | Low, long |
| Ferrari 250 GTO | 1962-64 IT | Long nose, fastback, side vents, Kamm-ish tail | Low, long |
| Jaguar E-Type | 1961-75 UK | Extremely long hood, tiny cabin, oval mouth | Long, CR |
| Mercedes 300 SL Gullwing | 1954-57 DE | Gullwing doors, hood eyebrows over wheel arches | Medium |
| Shelby Cobra 427 | 1965-67 US | Massively flared arches, tiny body, side pipes | Short, wide |
| Ford GT40 | 1964-69 UK/US | Only 40 in high, doors cut into the roof | Very low |
| Ferrari Dino 246 GT | 1969-74 IT | Rounded, small, flowing arches | Small |

### 6.3 Front-engine GT and muscle exotics (6)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Aston Martin DB5 / Vantage | 1963- UK | Long hood, tidy tail, wire wheels | CR |
| Dodge Viper | 1992-2017 US | Huge hood, side pipes, double-bubble roof | Very long hood |
| Chevrolet Corvette C3 Stingray | 1968-82 US | Coke-bottle fenders, pop-up lamps, T-tops | Long hood |
| Mercedes-AMG GT | 2014- DE | Very long nose, cabin pushed to rear | CR |
| Mercedes SLR McLaren | 2003-09 DE | Long nose with side-exit exhausts, gullwing, rising air-brake | CR |
| Ferrari 812 Superfast | 2017- IT | Duct-through-bonnet "aero bridge", fastback | CR |

### 6.4 Mid-engine modern (7)

| Car | Years / ctry | Defining features | Proportions |
|---|---|---|---|
| Honda NSX | 1990-2005 JP | F-16 inspired canopy, flat rear lamp bar | Mid |
| McLaren F1 | 1992-98 UK | Central driver seat, butterfly doors, scoop above cabin | Low |
| Ferrari F40 | 1987-92 IT | Kamm tail, huge fixed wing, engine louvres [21] | Low, wide |
| Lamborghini Diablo / Aventador | 1990 / 2011 IT | Diablo wedge; Aventador Y-lights, hex-cut surfaces | Very wide, low |
| McLaren 720S | 2017- UK | Eye-socket headlamps, double skin doors, dihedral doors | Wide, rounded |
| Chevrolet Corvette C8 | 2020- US | Mid-engine, side-intake blade, aggressive nose | Low wedge |
| Ford GT (2017) | 2017-22 US | Flying buttresses with open tunnels, teardrop cabin | Narrow cabin, wide hips |

### 6.5 Rear-engine 911-style (3)

| Car | Years / ctry | Defining features |
|---|---|---|
| Porsche 911 (993) | 1993-98 DE | Bug-eye lamps, sloped nose, curved rump |
| Porsche 959 | 1986-89 DE | 911 shape with wide arches, big wing |
| Porsche 718 Cayman GT4 RS (as a 911 alternative) | 2022- DE | Mid-engine, fixed wing, large intakes |

### 6.6 Hypercars with active aero and layered surfaces (9)

| Car | Years / ctry | Defining features |
|---|---|---|
| Bugatti Veyron / Chiron | 2005 / 2016 FR | Horseshoe grille, C-line side arc, huge rear wing [24] |
| Pagani Zonda / Huayra | 1999 / 2011 IT | Quad central exhausts, active flaps, rounded cab |
| Koenigsegg Jesko / Regera | 2017- SE | Dihedral synchro-helix doors, giant fixed wing |
| Ferrari Enzo / LaFerrari | 2002 / 2013 IT | F1-style nose, scissor doors / flowing vents |
| McLaren P1 | 2013-15 UK | Large roof scoop, active rear wing |
| Aston Martin Valkyrie | 2022 UK | Enclosed wheels, Venturi tunnels, extremely narrow cabin |
| Lotus Evija | 2020 UK | Through-body venturi tunnels at rear, ribbon lights |
| Rimac Nevera | 2021 HR | Closed front, active flaps, rounded EV shape |
| Gordon Murray T.50 | 2022 UK | Rear fan, three-seat central driver |

### 6.7 Track-focused road cars and radicals (4)

| Car | Defining features |
|---|---|
| BAC Mono | Single-seater road car, cycle wings, exposed wheels |
| Ariel Atom | Exoskeleton, no body panels |
| Dallara Stradale | Open cockpit, no roof, big roll hoop |
| Spyker C8 | Propeller-style grille, jet-fighter dash |

### 6.8 Electric / shooting-brake GTs (2)

| Car | Defining features |
|---|---|
| Ferrari FF / GTC4Lusso | Shooting brake, four seats, glass hatch [18] |
| Lamborghini Urus (as GT SUV) | SUV, scissor-edge lamps |

A count: 6 + 7 + 6 + 7 + 3 + 9 + 4 + 2 = 44 rows. Pick about 30 for variety. Complex surface features to model deliberately: scoops (McLaren F1), intakes through doors (Countach NACA ducts, Testarossa strakes), flying buttresses (Ford GT, Lancia 037), floating roofs (Aston Valkyrie, Evija tunnels), dihedral doors, big splitters (Valkyrie), diffusers (Chiron).

---

## 7. Class S: race cars by archetype (41)

| Archetype | Car | Years / ctry | Defining features |
|---|---|---|---|
| Touring (Group A / BTCC / DTM) | Ford Sierra RS500 Cosworth | 1987-90 UK/DE | Roadcar shape + whale-tail wing |
| | BMW M3 E30 DTM | 1987-92 DE | Wide arch flares, small rear wing, deep sills |
| | Alfa Romeo 155 V6 Ti | 1993-96 IT | Wide arches, large rear spoiler, DTM-like |
| | Mercedes 190E Evo II | 1990-92 DE | Huge double wing, sill extensions |
| | Volvo 850 estate (BTCC) | 1994 SE | Racing estate with roof spoiler |
| | Audi RS5 DTM (Class 1) | 2019 DE | Carbon body, big wing, carbon intakes [25] |
| | NGTC BTCC saloon/hatch | 2011- UK | Racing hatch/saloon with large splitter, spec wing [25] |
| Group 5 silhouette | Porsche 935 K3 | 1976-81 DE | Flat-nose, huge arch flares, big rear wing [26] |
| | Porsche 935/78 "Moby Dick" | 1978 DE | Long streamlined tail, low nose [26] |
| | BMW 320 Turbo | 1977-80 DE | Wide arches on a boxy coupe, airdam [26] |
| | Ford Capri Zakspeed Turbo | 1980-81 DE | Wide flared Capri, big wing [26] |
| | Lancia Beta Montecarlo Turbo | 1979-81 IT | Mid-engine, wide flares [26] |
| GT3 / GT4 | Porsche 911 GT3 R | 2019- DE | Wide arches, swan-neck wing, flat floor |
| | Mercedes-AMG GT3 | 2015- DE | Long hood, huge splitter, rear wing |
| | Lamborghini Huracan GT3 | 2015- IT | Wedge, wide wing |
| | McLaren 720S GT3 | 2019 UK | Rounded, wing |
| | Toyota GR Supra GT4 | 2019- JP | Double-bubble roof, GT4 wing |
| Rally (Group B/WRC/Rally1) | Lancia Delta S4 | 1985-86 IT | Boxy flared body, big wings, roof vent [27] |
| | Audi Sport Quattro S1 E2 | 1985-86 DE | Short WB, huge multi-element wings [27] |
| | Peugeot 205 T16 E2 | 1985-86 FR | Wide, with rear cut-out and wing [27] |
| | Lancia 037 Rally | 1982-85 IT | Low, long tail, rear scoop [27] |
| | Subaru Impreza WRC97 | 1997-98 JP | Wide arches, huge wing |
| | Toyota GR Yaris Rally1 | 2022- JP | Wide body, big wing, hybrid tube-frame |
| Rallycross | Audi S1 EKS RX | 2014-18 DE | Hatch with huge rear wing and big front splitter [28] |
| | Peugeot 208 WRX | 2018 FR | Short hatch with big wing [28] |
| | Volkswagen Polo RX Supercar | 2015-18 DE | Short, extremely wide arches [28] |
| Trans-Am / IMSA / stock | Ford Mustang Boss 302 Trans-Am | 1970 US | Flared fenders, front air dam, rear spoiler |
| | Dodge Charger Daytona | 1969 US | Pointed nose cone, tall wing |
| | NASCAR Next Gen Camaro ZL1 | 2022- US | Symmetrical body, centre-lock wheels, decal lamps |
| Open wheel | Lotus 49 | 1967 UK | Cigar body, engine as stress member, spoilers |
| | Tyrrell P34 | 1976 UK | Four small front wheels |
| | Brabham BT46B "Fan car" | 1978 UK | Flat, fan at rear, skirts |
| | McLaren MP4/4 | 1988 UK | Low nose, sidepods |
| | Current F1 (ground effect) | 2022- | Wide front wing, tall airbox, halo |
| Hillclimb / Pikes Peak | Peugeot 208 T16 Pikes Peak | 2013 FR | Huge wing and splitter, tiny roof |
| | Volkswagen I.D. R | 2018 DE | Open canopy, giant wing, fins |
| Drift | Hoonicorn (1965 Mustang) | 2014- US | Fastback with wide flares and big wing |
| | Nissan S15 Silvia (widebody) | 1999- JP | Wide arches, big wing, stance |
| Trophy / desert / Dakar | Ford Raptor Trophy Truck | 2010s US | Huge wheelbase, long-travel suspension, canopy [29] |
| | Baja Bug | 1960s- US | Cut fenders, fat tyres, exposed engine |
| | Toyota Hilux Dakar | 2010s JP | Wide arch pickup |
| | Audi RS Q e-tron | 2022 DE | Tank-like body |

S count: 7 + 5 + 5 + 4 + 6 + 3 + 3 + 5 + 2 + 2 + 2 + 3 = 47 rows. Choose around 28-30 for distinct silhouettes. Class S shape features available: wings and endplates, splitters, dive planes, louvres, box flares (Group 5, DTM), visible roll cages through windows, exposed wheels (open-wheel), center-lock wheels, roof scoops and snorkels, tow hooks.

---

## 8. Class E: Le Mans endurance by category / era (41)

Main classes: Hypercar (LMH and LMDh), LMP1 (2014-2020), LMP2, LMP3, GTE (2011-2023), LMGT3 (2024-), plus historic GT1, Group C, Group 6. Modern class list confirmed from official Le Mans and WEC sources [30][31].

### 8.1 Hypercar (LMH / LMDh), 2021-

| Car | Defining features |
|---|---|
| Toyota GR010 Hybrid | LMH, wide nose, long cabin tail fin |
| Ferrari 499P | LMH, red, short-tail fin, wide hips [32] |
| Porsche 963 | LMDh, "light pods" behind wheels, vertical fin |
| Peugeot 9X8 | LMH, NO rear wing, large shark fin [33] |
| Cadillac V-Series.R | LMDh, upright headlights, flat-top fin |
| Alpine A424 | LMDh, LED lamps, sculpted flanks |
| BMW M Hybrid V8 | LMDh, kidney-shaped intake |
| Lamborghini SC63 | LMDh, Y-lamps |
| Glickenhaus SCG 007 | LMH, squared "bubble" cockpit [32] |
| Isotta Fraschini Tipo6-C | LMH, tall canopy |

### 8.2 LMP1 (closed/open cockpit, 2000s-2010s)

| Car | Defining features |
|---|---|
| Audi R18 e-tron quattro | Closed cockpit with shark-fin |
| Audi R10 TDI | Closed; curved sports prototype with bulging flanks |
| Peugeot 908 HDi FAP | Long snout, pointed nose |
| Toyota TS050 Hybrid | Wide with bulbous side-pods |
| Porsche 919 Hybrid | Open top air cutout, vertical fin |
| Nissan GT-R LM Nismo | Front-engine with a pencil nose |

### 8.3 LMP2

| Car | Defining features |
|---|---|
| Oreca 07 | Open-top, high airbox, spec Gibson V8 |
| Ligier JS P2 | Open cockpit, small pod |
| Dallara P217 | Roofless, rear fin |

### 8.4 LMP3

| Car | Defining features |
|---|---|
| Ligier JS P320 | Open cockpit, simple wing |
| Duqueine M30 D08 | Open cockpit, tall fin |
| Ginetta G61-LT-P3 | Open cockpit |

### 8.5 GTE / GTLM, 2011-2023

| Car | Defining features |
|---|---|
| Porsche 911 RSR (2017) | Mid-engine 911, giant rear diffuser |
| Chevrolet Corvette C8.R | Mid-engine, big wing |
| Ferrari 488 GTE | Wide rear, swan-neck wing |
| Ford GT GTE | Flying buttresses and huge wing |
| Aston Martin Vantage GTE | Long nose, cut-back front |
| BMW M8 GTE | Squared, tall wing |

### 8.6 LMGT3, 2024-

| Car | Defining features |
|---|---|
| Porsche 911 GT3 R LMGT3 | Swan-neck wing, wide flares |
| Ferrari 296 LMGT3 | Short, fat |
| Ford Mustang GT3 | Wide, long hood |
| Chevrolet Corvette Z06 GT3.R | Mid-engine |
| McLaren 720S GT3 Evo | Rounded wedge |

### 8.7 GT1 (1990s, road-derived)

| Car | Defining features |
|---|---|
| Porsche 911 GT1 | 993 nose, mid-engine, long tail [34] |
| Mercedes-Benz CLK GTR | Long hood, small cabin, big wing (verify) |
| Toyota GT-One TS020 | Purpose-built prototype, long tail, very low [34] |
| McLaren F1 GTR | Road car with huge wing; "long tail" 1997 [34] |
| Nissan R390 GT1 | Closed prototype with big wing (verify) |
| Panoz Esperante GTR-1 | Front-engine, bulbous nose [34] |

### 8.8 Group C / Group 6 / sports prototypes (60s-90s)

| Car | Years | Defining features |
|---|---|---|
| Porsche 956 / 962 | 1982-91 | Ground-effect, skirts, tall tail wing |
| Jaguar XJR-9 LM | 1988 | Smooth, rounded, double tail fins |
| Sauber-Mercedes C9 | 1989 | Silver, sloped, long tail |
| Mazda 787B | 1991 | Rotary, closed coupe |
| Porsche 917 K | 1969-71 | Short tail with fins |
| Porsche 917 LH (Langheck) | 1969-71 | Long tail |
| Ferrari 512 S Coda Lunga | 1970 | Long-tail |
| Ford GT40 Mk IV | 1967 | Roofline bump, sleek |
| Ferrari 330 P4 | 1967 | Voluptuous body |
| Ferrari 250 TR | 1957-61 | Pontoon fenders, open cockpit |
| Jaguar D-Type | 1954-57 | Tail fin on headrest |
| Matra MS670 | 1972-74 | Rounded |
| Mirage M12 | 1973 | Flat nose |
| Chaparral 2J | 1970 | Rear fans |
| Alpine A442B | 1978 | Bubble canopy |

E count: 10 + 6 + 3 + 3 + 6 + 5 + 6 + 15 = 54 rows. Select 30-35 with distinct silhouettes: longtails, closed fins, open pontoons, bubbles, GT1 specials.

---

## 9. Legal notes (short; not legal advice)

Games typically use "inspired by" cars: Burnout Paradise uses invented makers/models loosely based on real vehicles [11]; Wreckfest has "literally no official licensed content", only tributes and gag vehicles [12]. Burnout and Wreckfest also tend to avoid licensing when cars get damaged or used in illegal activity (our reading of [11]).

What the law says:
- Real vehicles in games can be defended as expressive content (AM General v. Activision: judge dismissed the Humvee suit, applying the Rogers test) [35], but Jack Daniel's v. VIP (2023) says Rogers does not apply when the mark is used as a source identifier for your own product [36]. Practical split: scenery in a game vs branded merchandise or a product name.
- Car shape: US trade dress for a car shape needs secondary meaning and non-functionality; Ferrari v. Roberts upheld protection against near-identical replica kits [37]. Jeep won an ITC trade dress claim against Mahindra Roxor (overall appearance) even though the registered seven-slot grille trademark was not infringed [38].
- EU/Japan: shape marks are inconsistent. EUIPO cancelled Ferrari's 250 GTO shape mark for non-use [39]; Porsche lost EU registered designs for the 911 for lack of individual character [40] but the German Federal Supreme Court allowed 3D marks for 911/996/Boxster [40]; JLR's Evoque shape was refused [41]; Japan refused the VW Beetle 3D mark [41]; EU examiners did register shapes of Beetle, new Mini, 911/Boxster, Smart and Lotus [41].

Generic design language (safe) vs trade-dress sensitive (avoid copying exactly):

| Generic / safe | Sensitive (vary or avoid) |
|---|---|
| Wedge, fastback, notchback, Kamm tail, shooting brake, targa, hatchback | Exact 911 profile, bug-eye lamps plus sloping rump (Porsche; registered shapes) [40] |
| Scissor, gullwing, butterfly doors | Exact Beetle / new Mini / Smart outlines (EU registered shapes) [41] |
| Pop-up lamps, round lamps, flared arches, side strakes | BMW kidney grille (a brand symbol, not tested in court in the sources we found) [42] |
| Ladder-frame boxy 4x4 proportions | Jeep: seven-slot grille + round lamps + fender flares on the ensemble [38] |
| Whale-tail / swan-neck wings, splitters, diffusers | Rolls-Royce Pantheon grille + Spirit of Ecstasy (named brand elements) [43] |
| Cab-forward vans, kei boxes, microcar bubble shapes | Ferrari 250 GTO / Testarossa / Daytona profile (replica case) [37][39] |
| Retro-futurist pixel lamps (Hyundai) as concept | Brand badges, wordmarks, wheels with brand logos, "911", "GT-R" names |

Rules of thumb (ours): change at least three of nose, greenhouse, tail, arches and lamp shape from any one donor; never use grilles or badges that function as the brand; do not name cars after real model names or numbers; blend two or three donors; keep fictional makers.

---

## 10. Counts

| Class | Archetype groups | Distinct cars listed |
|---|---|---|
| C family | city, kei, supermini, 3-box, estate, MPV, van-based, microcar, 2-door classic, utility 4x4, pickup (11) | 30 |
| C bangers | Eastern Bloc, UK, French, Japanese kei, US, Indian, Korean (7) | 26 |
| C top-end warm | 1 | 10 |
| B | hot hatch, homologation, muscle, ute/van, EU coupe, mid-engine, roadster/kei sport, shooting brake, quirky, Japanese (10) | 81 (select about 45) |
| A | wedge, classic curvy, front-engine GT, mid-engine modern, rear-engine, hypercar active aero, radical track, shooting-brake GT (8) | 44 (select about 30) |
| S | touring, Group 5, GT3/GT4, rally, rallycross, Trans-Am/stock, open-wheel, hillclimb, drift, trophy/Dakar (10) | 47 |
| E | Hypercar, LMP1, LMP2, LMP3, GTE, LMGT3, GT1, Group C/6 (8) | 54 |

---

## 11. Source list

1. PopSci, inside the Hot Wheels design centre: https://www.popsci.com/technology/how-hot-wheels-makes-its-cars/
2. The Drive, Hot Wheels design studio (real car to 1:64 toy): https://www.thedrive.com/news/42183/inside-the-hot-wheels-design-studio-how-a-real-car-gets-turned-into-a-164-toy
3. TOMY's Shunsuke Takeuchi on Tomica: https://www.mojo-nation.com/tomys-shunsuke-takeuchi-on-the-legacy-and-enduring-appeal-of-tomica/
4. Hot Wheels (Wikipedia, Beach Bomb history): https://en.wikipedia.org/wiki/Hot_Wheels
5. Choro-Q (Wikipedia): https://en.wikipedia.org/wiki/Choro-Q
6. Car Throttle on Choro-Q: https://www.carthrottle.com/posts/choro-q-toy-car-you-always-wanted-never-knew-about-it
7. GP22: A Car Styling Dataset for Automotive Designers (arXiv): https://ar5iv.labs.arxiv.org/html/2207.01760
8. PixelRacer write-up: https://engineering.trustfull.com/pixelracer-when-pixel-art-meets-automotive-engineering-ee19a52ea3c3
9. Rocket League hitboxes: https://www.ggrecon.com/guides/rocket-league-hitboxes/
10. Mario Kart 8 Master Cycle (Aonuma): https://www.siliconera.com/?p=440004
11. Burnout Paradise (Wikipedia) and Jalopnik on the Legendary Cars homages: https://en.wikipedia.org/wiki/Burnout_Paradise , https://www.jalopnik.com/burnout-paradise-gets-flying-delorean-calls-it-jansen-5082044
12. Wreckfest car lists: https://traxion.gg/which-are-the-best-vehicles-to-drive-in-wreckfest/ , https://onlineracedriver.com/2019/12/18/complete-official-wreckfest-car-list , https://godisageek.com/2019/09/wreckfest-dlc-retro-rammers-out-today/
13. Autocar and Emerging Europe on Eastern Bloc cars: https://www.autocar.co.uk/node/363947 , https://emerging-europe.com/unreliable-uncomfortable-legends-the-iconic-motors-of-emerging-europe/
14. Reliant Robin: https://www.3-wheelers.com/robin.html
15. Nissan Pike cars: https://petrolblog.com/articles/bubble-cars-brief-history-nissan-pike-factory , https://en.wikipedia.org/wiki/Nissan_Be-1
16. Kei sports cars and Subaru 360: https://www.wapcar.my/news/4559/amp , https://www.goodwood.com/grr/road/news/the-10-best-kei-cars
17. Australian muscle: https://www.whichcar.com.au/features/top-10-classic-australian-muscle-cars
18. Shooting brakes: https://www.slashgear.com/1400511/best-shooting-brake-cars-ever-made/ , https://www.caradvice.com.au/901407/the-greatest-shooting-brakes-ever/ , https://en.wikipedia.org/wiki/Lancia_Beta
19. Wedge cars: https://www.hotcars.com/wedge-era-car-designs/ , https://jalopnik.com/the-best-looking-car-shape-is-the-wedge-1841391941 , https://www.jalopnik.com/2244032/cars-defined-wedge-shape-era/
20. Bond Bug: https://en.wikipedia.org/wiki/Bond_Bug
21. Kamm tail: https://en.wikipedia.org/wiki/Alfa_Romeo_Giulia_TZ
22. Hyundai Pony Coupe / N Vision 74: https://www.hyundai.news/eu/articles/press-releases/hyundai-and-giorgetto-giugiaro-collaborate-to-rebuild-original-1974-pony-coupe-concept.html
23. Countach/Stratos/Esprit via wedge articles (see 19).
24. Bugatti horseshoe grille: general knowledge (no source consulted).
25. BTCC NGTC / DTM Class 1: https://en.wikipedia.com/wiki/NGTC , https://pmw-magazine.com/news/race-series-news/dtm-super-gt.html
26. Group 5: https://www.goodwood.com/grrc/event-coverage/members-meeting/2018/2/are-these-the-five-coolest-group-5-sportscars/ , https://www.zwischengas.com/en/articles/books-and-movies/Gruppe-5-Die-Silhouetteformel-1976-1981-Buchbesprechung-.html
27. Group B: https://www.goodwood.com/grr/road/news/2020/8/the-nine-best-homologation-specials-ever/ , https://pistonheads.com/news/general/homolorated--forgotten-group-b/42154
28. Rallycross supercars: https://www.motor.es/noticias/supercar-world-rallycross-2018-201845584.html
29. Trophy trucks: https://www.hotcars.com/ford-raptor-trophy-truck-fastball/
30. 24h Le Mans classes: https://www.24h-lemans.com/en/lemans/classes
31. WEC class guide: https://racer.com/racerpedia/series-guides/sportscars/fia-wec-class-guide
32. 2024 Le Mans Hypercar grid: https://www.24h-lemans.com/en/news/2024-24-hours-of-le-mans-everything-you-need-to-know-about-the-hypercar-category-58693
33. Peugeot 9X8 no rear wing: https://motorsportmagazine.com/articles/sports-cars/le-mans/the-peugeot-breakthrough-that-led-to-new-9x8-hypercar-having-no-rear-wing
34. GT1 cars: https://www.supercars.net/blog/1998-porsche-911-gt1-98/ , https://www.supercars.net/blog/1998%e2%86%921999-toyota-gt-one/
35. AM General v. Activision: https://www.thedrive.com/tech/32872/you-have-a-first-amendment-right-to-humvees-in-call-of-duty-games-judge-rules
36. Jack Daniel's v. VIP Products: https://en.wikipedia.org/wiki/Jack_Daniel%27s_Properties,_Inc._v._VIP_Products_LLC , https://dwt.com/insights/2023/06/scotus-jack-daniels-trademark-rogers-test
37. Ferrari v. Roberts: https://cyber.harvard.edu/IPCoop/91ferr1.html
38. Mahindra Roxor v. Jeep: https://www.theippress.com/2020/08/22/mahindras-roxor-versus-fiat-chryslers-jeep-us-trade-dress-infringement-case
39. EUIPO Ferrari 250 GTO: https://www.mlex.com/mlex/amp/articles/2241776
40. Porsche 911 designs and 3D marks: https://www.worldtrademarkreview.com/article/porsche-3-d-marks-steered-successfully-the-register , https://hlc.com/en/publications/eu-general-court-on-industrial-designs-porsche-911-car-model-lacks-individual-character
41. Car shape marks: https://trademarkblog.kluweriplaw.com/2015/12/17/general-court-car-shape-found-not-registrable-decision-of-25-november-2015-case-t-62914/ , https://blog.marks-iplaw.jp/?p=1755
42. BMW kidney grille: https://www.caradvice.com.au/732441/the-mysterious-origins-of-bmws-kidney-grille/
43. Rolls-Royce Pantheon grille and Spirit of Ecstasy: https://www.slashgear.com/1770680/rolls-royce-pantheon-grille-history/
44. Car-shape protection overview: https://www.murgitroyd.com/blog/how-to-protect-the-shape-of-a-car-lessons-from-jaguar-land-rover-and-ineos/ , https://www.dlapiper.com/insights/publications/2022/07/ipt-news-july-2022-trade-dress-protects-auto-design
