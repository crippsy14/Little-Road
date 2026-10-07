# 65 · Class B, the Tuner series: the cars (plan, 6 October)

Richard (6 Oct): "plan out tier B cars, the Tuner series... a classic Japanese 1990s / early 2000s feel, with Skyline and
350Z kind of feel". Doc 44 sets class B as aftermarket parts and weekend track days ("hot hatches, tuner coupés,
rally-bred saloons"), with anodised purple and blue, polished alloy, braided hoses, 5-spoke and split-spoke alloys.

## The idea
Class B is where your car stops being a runabout and becomes *a car people know*. Every Tuner body is a fictional car
with the unmistakable silhouette of one late-90s/early-2000s Japanese icon: real enough that you recognise it across a
car park, never a copy of a badge or a name.

The class is built so that **drivetrain is the choice**, not just a number. C cars are all much of a muchness. In B you
pick a character:
- AWD grip and launch;
- RWD balance and slide;
- FWD high-revving precision;
- the rotary's lightness.

Each has a clear strength and a weakness on the B tracks (the reversed and extended C tracks, and the new region).

## The roster (8 new bodies + 3 kits)

| Body | Spirit of | Drive and engine | Strength | Weakness | Status |
|---|---|---|---|---|---|
| **Hayate GT-4** | R34 Skyline GT-R | AWD, twin-turbo straight-six | Launch, wet and gravel grip, all-rounder | Heavy: tight hairpins, brakes | modelled (round 3: builder v2) |
| **Kaze Z** | Z33 350Z | RWD, big V6 | Balance and torque out of corners | Mid top speed | modelled (round 3) |
| Raijin RX | FD RX-7 | RWD, twin-rotor | Lightest; agility, revs | Fragile torque low down | modelled (round 3) |
| Hoshi 80 | A80 Supra | RWD, big single-turbo six | Top speed king | Lag; a handful in the wet | modelled (round 3) |
| Kumo Evo | Lancer Evo VI | AWD, turbo four, rally saloon | Gravel and mixed surfaces (Quarry Hills' B events) | Plain on tarmac | modelled (field round) |
| Tsubasa R | Integra / Civic Type R | FWD, high-rev four | Cheapest; late braking, stability | Understeer, low torque | modelled (field round) |
| Sakura S | S15 Silvia | RWD, turbo four | Light and playful: the drift car | Mid everything | modelled (field round) |
| Shiro GT | NSX (NA1) | RWD mid-engine V6 | The halo: sharpest handling | Priciest | modelled (field round; the B "dream car") |
| Sprint Tuner, Comet Tuner, Rally Tuner | kits of the C cars | as their C car | Keep a favourite viable (doc 44) | Lower ceiling than new bodies | modelled (field round): flares, Tuner wheels, aero |

The cars come in **three B platforms**, matching the size bands doc 44 asks for:
- **Compact** (3.6–3.8 m): Tsubasa, Sakura, the kits.
- **Coupé** (3.8–4.0 m): Kaze, Raijin, Hoshi, Shiro.
- **Saloon** (4.0–4.2 m): Hayate, Kumo.

## How they look (the shared Tuner look)
- **The class C style.** Richard (6 Oct): the sculpted version (`build_tuner.py`, round 1) looked "too play-dough". So the
  Tuners are recipes in `family_recipes.py`, built by `build_family_cars.py` like every C car: a lofted superellipse shell,
  real boolean arches, pressed window frames, modelled lamps, bumpers and extras. They're a step bigger than the C coupés
  (3.8–4.05 m against 3.5), with bigger wheels (0.43–0.44 m) and wider tracks.
- New builder options: the `twin_round` tail (the Hayate's four round lamps with bright inner rings), the `boomerang` tail
  (the Kaze's), and the `two_seater` extra (a parcel shelf instead of a rear bench).
- **Real panel gaps** are cut into the body (doors, bonnet, boot), not painted lines (Richard, 28 Sep: real parts).
- **Signature details carry the identity:**
  - the Hayate's four round tail lamps, boxed arches, bonnet duct and big wing;
  - the Kaze's headlamps swept up the fenders, boomerang tail lamps, vertical door handles and fat rear haunches.
- **Period wheels per car:**
  - deep-dish twin five-spoke in gunmetal (Hayate);
  - split seven-spoke in bronze (Kaze);
  - later: TE37-style six-spoke, mesh, and RS-Watanabe-style eight-spoke.
- **Factory colours with a JDM feel**: Bayside Blue, Midnight Purple, Championship White, Sunset Orange, Lightning Yellow.
  Saturated, mid-value paint with a deep clear coat, not pastel (Richard, 28 Sep).
- **Aero kits as visible upgrades.** Class B parts show on the car: the wing grows with the aero part, and the lip and
  canards come with the front kit. This follows the gadget and "rally kit you can see" idea (doc 60).

## Round 1: what's there and what isn't yet (`review/cars/hayate-round1`, `kaze-round1`)

**Hayate GT-4.**
- Done: the long bonnet, squared shoulders, notchback cabin, flared arches, the four round lamps, and a wing on uprights.
  Panel gaps; gold calipers.
- Still to do:
  - the nose is too tall and flat: the R34's slim headlamps over a deep mouth want a lower, sharper bonnet edge;
  - the front bumper corners are boxy;
  - the C-pillar should kick up into the boot more;
  - window outlines are still a little ragged.

**Kaze Z.**
- Done: the side profile reads as a Z already: one roof arc, a short tail and big haunches. Lamps swept up the fenders,
  boomerang tails, bronze split-spokes.
- Still to do:
  - the headlamps want to be longer and higher up the fender;
  - the mouth wants to be wider and lower;
  - the rear-screen line should be crisper;
  - the paint is a bit flat orange (try a deeper Sunset Orange metallic).

Following the model loop (memory): renders each round, scored by Richard against his reference photos; export to the game
only once he's happy. The cars then need a `CarBodies` entry (a Tuner list), a collision envelope per platform band, LODs,
and B-class tuning sheets.

## Questions for Richard
1. **Roster:** keep all eight, or trim to six for the first B release (my pick: Hayate, Kaze, Raijin, Hoshi, Kumo, Tsubasa)?
2. **Names:** Japanese-word names (Hayate = gale, Kaze = wind, Raijin = thunder god, Hoshi = star, Kumo = cloud, Tsubasa =
   wing). OK, or would you rather nod to the originals more (e.g. "GT-4R", "Z-Kaze")?
3. **Reference photos:** send one or two shots of each that you love (angle and colour), and I'll measure the proportions off
   them, as with the Regale.

## Round 2 (6 Oct): the C-car style
Renders: `review/cars/tuner-family-1` (both cars) and `tuner-family-3` (the Kaze after its fastback fix). Build with:
`blender --background --python source-art/build_family_cars.py -- hayate kaze --no-export --render DIR --closeups`.
The sculpted round 1 (`build_tuner.py`, `review/cars/*-round1`) is kept for reference only.

## Round 2b (6 Oct): the Raijin RX and the Hoshi 80
Renders in `review/cars/tuner-raijin-2` and `tuner-hoshi-2`.
- **Raijin RX** (FD RX-7 spirit): low and curvy, a bubble cabin over a fastback hatch, slim lamps in a low nose, a roof
  spoiler, round tail lamps, Competition Yellow.
- **Hoshi 80** (A80 Supra spirit): a long smooth nose, the cabin set well back, a hoop wing close over the boot (new
  `hoop_wing` extra), four big round lamps in a dark full-width panel (new `tail_panel` extra), deep red.

The Regale study (`regale_toy`, `review/cars/regale-toy-3`) showed the C builder makes closed cars only. Richard (6 Oct):
keep the sculpted Regale. Builder fixes found along the way: the rear badge, boot lock, boot shut line and hatch release
used fixed heights and crashed on low tails; they now skip or lower themselves.

## How a car is built, and the effort for later classes
**The pipeline.**
1. A recipe in `source-art/family_recipes.py`, about 8 lines: the shell's stations, the glasshouse, axles and wheels, lamp
   and tail styles, bumpers, extras.
2. `build_family_cars.py` in Blender turns it into the car:
   - a lofted superellipse shell, with real boolean arches and lamp apertures and a cut cabin;
   - framed glass, an interior, bumpers, lamps and wheels (six styles);
   - about 40 optional extras.
3. It writes `.blend`, an FBX and two LODs to `Resources/Cars`.
4. In the game: a `CarBodies` entry, the shared collision envelope, and an offer in the showroom. Performance comes from
   the parts, not the body.

**Per new car of an existing kind:** about 1–2 hours with 2–3 render rounds. Add 30–60 min when it needs a new signature
part (as the hoop wing and twin round lamps did).

**Kit versions** of earlier cars (Hayate → Hayate Touring): 30–60 min each once the kit extras exist.

**Class B in the game** needs the platform system (doc 45, F7: platform purchase, class gating, the B parts ladder). That's
the bigger job, about 1–2 days, separate from modelling.

**Class A Touring** (BTCC/JGTC-style touring cars and GTs). This fits the recipe style well.
- **Builder work, about a day:** a roll cage, roof vents, number boards, wider box arches, slicks, a race splitter and wing.
- **Livery system, about 1–2 days:** the one real gap. It means adapting the Regale's livery painter to the lofted shell
  (decals per panel, numbers, sponsors).
- **Then per car:** 1–2 h, and kits of the B cars are cheap.

**Class S Racing** (open-wheelers and prototypes). The closed-shell builder can't make these, as the Regale study showed.
- **Option 1:** a new "racing" body option in the same chunky style (canopy or open cockpit, nose cone, sidepods, fender
  pods, open wheels). About 2–3 days of builder work, then about 2 h per car.
- **Option 2:** the sculpted pipeline per car, as the Regale is. Half a day to a day per car, and a different look.

**Class E Endurance.** Mostly reuse: S's prototype bodies plus A's GTs with endurance kits.
- Night lights (emissive headlamps, lit number panels, light pods), fuel flaps, endurance livery variants.
- About a day of extras, then kits.

## The full field (6 October)
Richard: "lets generate the full field and have a look at all of the models in one go". All 11 are built with builder v2
(doc 66): renders per car, plus the line-up `review/cars/tuner-field/field-front.png` and `field-rear.png`. Build:
`blender --background --python source-art/build_family_cars.py -- <ids> --no-export --render DIR --lineup DIR`.

New this round:
- **Kumo Evo** (Evo VI): a tall boxy four-door with flared arches; a huge mouth with round fog lamps, white six-spokes,
  a big wing, a bonnet scoop, a roof vent and mudflaps. Racing green.
- **Tsubasa R** (EK9 Civic Type R): short and upright, with a flat roof to a near-vertical tailgate and white five-spokes.
  Milano red.
- **Sakura S** (S15 Silvia): a sleek low notchback with slim cat-eye twin-projector lamps and polished mesh wheels.
  Deep cherry.
- **Shiro GT** (NA1 NSX): very low, the cabin pushed forward under a black canopy (`roof_mat='trim'`); pop-ups, a long
  engine deck, side air intakes ahead of the rear wheels (new `side_scoop` extra), and a full-width tail panel.
  Pearl white.
- **Sprint, Comet and Rally Tuner kits** (`_kit()` in `family_recipes.py`): the C body plus fender flares, Tuner wheels
  (white six-spokes, bronze split-spokes, bronze mesh) and aero. The Rally Tuner keeps treaded tyres for gravel.

Builder fixes found on the way:
- **Roofline ends.** A roofline's ends never sit above the bonnet or deck they meet. The Raijin's ended 11 cm above its
  low bonnet, so a black cowl bridged the drop, and its glass's bottom edge showed as a second line.
- **Cowl width.** The cowl stops at the windscreen's width and always reaches past the glass's lower seal.
- **Tail panel.** `tail_panel` works with any tail style.

## Getting into class B: a free B car, paid kits for your C cars (Richard, 6 October)
This sharpens doc 44 §2 and doc 45 F7 (platforms and body kits):
1. **Unlocking B gives the player a brand-new B car free** (one of the Tuners), so there's something to drive the moment
   the class opens.
2. **Owned C cars can be converted to their B kit** (the Sprint Tuner and friends) at a cost: the body kit plus fitting
   it to the B chassis. Only a converted car can take B parts. The conversion is the money sink, and a visible one (flares,
   Tuner wheels, aero), so an old favourite comes back looking the part.
3. So every C body wants a B kit. They're cheap to make: a kit is the C recipe plus fenders, Tuner wheels and aero, via
   `_kit()` in `family_recipes.py` (about four lines each).

## The 24-car grid (6 October)
Richard: "lets generate a full field of 24 cars first". The thirteen added to the eleven above:

| Body | Spirit of | Silhouette |
|---|---|---|
| Fubuki 22B | Impreza 22B | Compact rally coupe with box flares, gold six-spokes, scoop, wing, round fog lamps |
| Hachi | AE86 Trueno | Small boxy hatch: pop-ups, a black bumper, a full-width tail panel, bronze eight-spokes |
| Kogarashi | MX-5 NA | Tiny roadster with a black hardtop and pop-ups |
| Arashi Z | 300ZX Z32 | Wide and low, long raked lamps, glass T-tops (new `roof_glass` cabin key) |
| Taiyo GT-4 | Celica GT-Four ST205 | Round rally coupe with four round lamps, scoop and tall wing |
| Midori MR | MR2 SW20 | Small mid-engine car with pop-ups and side intakes |
| Hibari R | Integra DC2 Type R | Low coupe with four round "eyes" and a tall wing |
| Tsuki | Eclipse 2G | Curvy coupe with almond lamps and a hoop wing; lime green |
| Yoru SX | 180SX | Crisp pop-up fastback with a full-width tail; midnight purple |
| Nagare GT-B | Legacy GT-B wagon | The grid's estate: long flat roof, roof rails, vertical tail lamps |
| Kage | Chaser JZX100 | Long low four-door drift saloon; black |
| Chibi | Cappuccino | Kei roadster, the smallest car, with round lamps and a black hardtop |
| Kitsune GTI-R | Pulsar GTI-R | Rally hot hatch with a bonnet vent, roof vent, fog lamps and mudflaps |

The line-up is now a grid of up to six a row: `review/cars/tuner-field/field-front.png`, `field-rear.png`.

**Next:** the C car kits (below), then reviews of colour and finish, including Richard's idea of some matte paints among
the glossy ones.

## Every C car's B kit (6 October)
Richard: "lets do the tier C car kits next up". Every one of the 30 class C cars that players own now has a B kit,
`<id>_tuner`. The three pit-stop service vehicles are left out. They're defined in `_KITS` in `family_recipes.py`
(blurb, wheels, front and rear flares, parts added, parts dropped), with `KIT_ORDER` listing them. A kit wears its C
car's paint (`paint_of()`). Renders and the line-up are in `review/cars/kits/`.

Each kit suits its car:
- **Road cars:** Tuner wheels on road tyres, flares and aero. Examples: the Marlow as a sleeper (gunmetal split-spokes,
  ducktail, integrated nose); the Ritz VIP-style (deep-dish polished mesh); the Beetle Cal-look (bronze mesh, ducktail);
  the Puddle Tuner from doc 44 (white five-spokes, spoiler, fog lamps).
- **Vans and kei cars keep their signature parts:** the Taxi its sign, the Gelato its cone, the Post Van its horn, the
  Camper its pop-top, the Heron its woodie panels.
- **Dropped where they don't suit a tuned car:** aerials, rubbing strips, the Kipper's ladder, roof-top spares and
  luggage.
- **The six off-roaders get rally-raid kits on their knobbly tyres** (light bars, spot lamps, scoops, bigger flares).
  This is the class B rallycross field for Quarry Hills.

**Kit arches (round 2).** The first kits were too subtle: on screen they changed little but the wheels. Two reasons:
- **The flares were small:** 2–3 cm. They now bulge nearly twice as far.
- **The arch lips never drew:** they're traced off the body's side, and on these chunky bodies the lip's outer edge sits
  above the wing at the top of the arch. The code then drops the lip altogether. That's true of the C cars' `flare` and
  `paint` arches too. They're unchanged, and still have no lips.

The kits now wear bolt-on arches built round the wheel as their own part (`kit_arch()`):
- **`overfender`:** satin black, riveted (most kits and all the rally-raid kits).
- **`kitflare`:** body colour (the classics: Dover, Ritz, Beetle, Doodle, Heron, Camper).

Each arch leans out over the tyre and, over the top, reaches down onto the wing, so it reads as a widened wing rather than
a mudguard.

## Kit experiments (6 October, round 3)
Richard: the bolt-on arches "look a bit bolted on... floating", and didn't suit the wheel sizes, so they're gone. Instead,
try spoilers, bonnet air parts, a go-faster stripe, a sportier body, a lowered screen. New builder options:
- **`race_stripes`:** twin centre stripes over bonnet, roof and boot, colour `stripe_colour`.
- **`bonnet_vents`:** two louvred vents.
- **`lower`:** the body sits down over the same wheels.
- **`chop`:** a `_kit()` argument that brings the roof and screen down.

Four C cars try different mixes (`_EXPERIMENTS` in `family_recipes.py`; renders in `review/cars/kit-experiments/`):

| Car | Treatment |
|---|---|
| Puddle | White stripes, bonnet vents, spoiler, lowered |
| Bramble | Black stripes, vents, wing, lowered |
| Marlow | Chopped 10 cm, lowered, scoop, ducktail |
| Beetle | Cal-look: chopped, lowered, bronze mesh wheels |

**The big box cars.** Richard: be playful and brave, picturing a box car one day in class S, lapping the Nurburgring.
Only the B Tuner is built for now, but every part is a first step later tiers can grow:
- **Picnic Tuner:** stripes, a roof wing, bonnet vents, splitter and skirts, lowered.
- **Kipper Tuner:** SuperVan flavour. White stripes, a roof wing, canards, side exhausts, lowered; it keeps its fish
  signwriting.

Renders are in `review/cars/box-tuners/`. Parts ready for the higher tiers, built and tested but not yet on any car:
- `gt_wing` (a towering swan-neck roof wing);
- `roof_scoop`, `diffuser`, `canards`, `side_exhaust`;
- `roundel` (a race number in seven-segment digits);
- slick tyres;
- `splitter_d` (splitter depth).

## Quarry Hills' class B off-roaders (7 October)
Richard: "Lets sort the Tier B for the mining town now". The six off-road bodies get B kits in the experiments' style.
Off-roaders are lifted, not lowered (a negative `lower`), on bigger knobblies, and each is a first step a later tier can
grow. Renders are in `review/cars/quarry-b/`.

| Car | Kit |
|---|---|
| Cob (No. 4) | Dakar-lite: lifted 5 cm, 0.49 m wheels, stripes, bonnet vents, light bar, skid plate, door number |
| Drover (No. 11) | Baja trophy truck: lifted 7 cm, 0.50 m wheels on a wider track, white stripes, skid plate; no bull bar |
| Scamp (No. 27) | Rallycross hatch: black stripes, roof scoop, bonnet vents, rally spot lamps, spoiler |
| Fell (No. 8) | Expedition wagon: lifted 5 cm, spare tyres on the roof rack, light bar, skid plate |
| Tumbler | Rock crawler: lifted 9 cm, 0.52 m wheels on a wider track, light bar, stripes, skid plate |
| Hauler | The kei truck with a roof wing, side exhausts and canards, on knobblies |

New part: `skid_plate`, an alloy plate tucked under the bumper and sloping back beneath the car. The first version stuck
out in front like a ramp. The Hauler's cab door is too small for a number roundel, so it has none.

## New class B off-road bodies for Quarry Hills (7 October)
Richard: "can we add some new ones for Tier B as well". Six new bodies join the six kits, so B rallycross gets new faces
too. Renders are in `review/cars/quarry-b-new/`.

| Body | Spirit of | Look |
|---|---|---|
| Sabaku Evo (No. 1) | Pajero Evolution (Dakar) | Wide three-door on big box flares; roof scoop, white six-spokes, red stripes on white |
| Yama 80 (No. 80) | Land Cruiser 80 | Long boxy expedition wagon: chrome grille, jerry cans on the rack, snorkel, bull bar, light bar |
| Tanuki | Jimny | Tiny, tall and square; round lamps, black four-spokes, spare on the back; bright yellow |
| Tora Runner (No. 5) | Hilux pre-runner | Wide single-cab pickup on fibreglass flares; slim lamps, light bar, black mesh wheels |
| Kaminari (No. 3) | Escudo hill-climb special | Short three-door with huge flares, roof wing, canards, deep splitter, side pipes |
| Kiri | Subaru BRAT | Low car-based ute with quad lamps, chrome bumpers and two rear-facing jump seats in the bed |

New builder part: `jump_seats` (two rear-facing seats with grab handles in a pickup bed). Go-faster stripes now skip the
deck on pickups. Known gap: the Kiri's bed has no wheel tubs, so the rear tyres show inside it.
