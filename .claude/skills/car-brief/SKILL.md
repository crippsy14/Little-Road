---
name: car-brief
description: How to write a car brief for Little Road, a brave and exhaustive description of one toy car for a given class and star band. It covers the body shape, every component, lamp shapes and their inner patterns, wheels, small parts, kit, paint and the toy idioms. Every element is written as a numbered pass/fail requirement, and the model counts as created only when all of them pass. Use it before modelling or remodelling any car, in Car Forge or any other way, for any class (C, B, A, S, E), and when a car "isn't as good as it should be". Judge a model against its brief with the car-brief-check skill.
---

# Writing a car brief

Richard, 24 Oct: the Forge's class C cars still aren't as good as the legacy cars from before the Forge. He asked for a
skill that writes "an incredibly detailed idea for a car of a specific tier", with specific elements that must all pass
before a model counts as created. It should be really brave and really descriptive of every element of the car: the
body shape, each component part, light shapes and patterns, and the toy elements. It should lean heavily into the toy,
**without a specific brand reference**, because brand references pigeonholed the cars. Also: **the brief is not limited
by Car Forge. If the Forge can't build the brief, the Forge gets updated.**

A brief is the car's specification and its acceptance test in one document. `car-brief-check` judges a model against
it, requirement by requirement.

## 0. Why the Forge class C cars fell short (read this first)

These are measured, not guessed (24 Oct, `car-forge/tools/proportions.py` on the roster):
- **The proportions are already in band.** Bramble: L/W 1.75, H/L 0.44, wheel r/L 0.117, the same as the legacy cars.
  More toy factors won't close the gap.
- **The gap is form and identity.** Most of the 55 cars are one archetype shell per body type (hatch, van, 4x4,
  saloon), with flat slab sides, the same shoulder and the same wheel. The face and tail come from a shared variety
  library, and the character sits in props stuck on top (a cone, a bale, a ladder). Take the props off and half the
  sheet is the same car. The legacy cars each had their own volumes: an upright classic grille, a wedge coupe, a car with
  a wing. Their soft but purposeful shapes and oversized lamps and grilles made each one its own toy.
- **Earlier misses to avoid:**
  - soft everywhere reads as "playdoughy";
  - made too straight, the cars lost their character;
  - window outlines were not true to life;
  - door lines did not meet the window frame;
  - lamps and grilles were painted on instead of built.

So a brief describes **shapes**: the body's own volumes, sections and lines, and each part's form and pattern. Ratios
only frame them. The signature lives in the body, not in a prop.

## 1. The rules every brief keeps

1. **No brands, anywhere.** Name no real car maker, model or badge, no toy line (no Tomica, Hot Wheels, Matchbox,
   Majorette, Corgi, Siku or Micro Machines), and no "in the style of" a studio or film. Don't use another Little Road
   car as the template either (not "like the Kestrel"). Describe with:
   - an era ("late 70s");
   - a job ("a seaside chip van");
   - a personality;
   - shape words ("a bar of soap", "a loaf", "a toaster", "a pebble", "a shoe", "a bread bin");
   - geometry: counts, fractions of the car's length, angles, positions.
2. **Not limited by the Forge.** Write the car that should exist. Never soften a requirement because the engine can't
   build it yet. `car-brief-check` records those requirements as ENGINE GAP, and they become Forge work.
3. **Brave.** Every brief must have:
   - one exaggeration of at least 25 per cent over a real car;
   - one body shape that no other car in the roster has;
   - a face whose expression can be named in one word.
   If the idea could be any car of its type, it isn't finished. The test: would a child pick it out of a tipped-out
   toy box, and draw it from memory after one look?
4. **A toy, through and through.** A brief describes a diecast toy of a car, not a car (§4). Proportions, edges,
   materials and finish all say "toy".
5. **The world of cars.** No people and no animals anywhere: no drivers, figures, pets or mascots of living things
   (`docs/design.md` §1 and §12). Cars are the characters, and the car's face is a face. Empty seats, a helmet on a
   seat and a hat on the parcel shelf are fine.
6. **Every element is a requirement, and every requirement is checkable.** Each one names:
   - what must be true: the shape, count, position, and size relative to something on the car;
   - where to see it: which view, or which measurement.
   Two reviewers reading it should reach the same verdict. A requirement nobody could fail ("nicely shaped") is
   rewritten.
7. **All must pass.** There is no "nice to have" tier. If something is optional, leave it out of the brief.
8. **The signature survives a repaint.** Rivals' colours come from each race's paint list, so identity lives in shape
   and kit, and must show in a black silhouette.

## 2. The tier: class and stars

The tier sets the cast, the finish and the detail density. Higher tiers look visibly more expensive. Read
`docs/design.md` §2 and §4, `car-forge/docs/roster-c-design.md` for the class C cast, and the world-plan skill for which
races a band covers.

| Tier | What the cars are | Finish and density | Proportion bands |
|---|---|---|---|
| C 1–3★ | bangers, city cars, delivery vans, rogue toys (ice cream van, milk float, rust buckets) | simple and chunky: few, big parts; worn or plain paint; steel wheels and hubcaps | L/W 1.55–1.8, H/L 0.42–0.60 (vans higher), wheel r/L 0.11–0.13 |
| C 4–7★ | family and working cars, quirky toys, the first rally-kit and warm-trim cars | cleaner: more trim, alloys on the warm cars, kit that tells a story | as above |
| C 8–10★ | quick-looking everyday cars, rally saloons, the flagship; still not sports cars | polished: stripes, spot lamps, wide wheels, lower stance | L/W 1.6–1.85, H/L 0.38–0.48 |
| B | as many unique forms as possible; every C car also gets a B kit | dense detail, aero parts, sporty wheels | set by the first B brief, then recorded here |
| A | road sports cars and supercars | jewellery-level lamps, sculpted intakes | set by the first A brief |
| S | race cars | liveries, roll cages, slicks, wings | set by the first S brief |
| E | Le Mans cars across the real Le Mans grades | prototype and GT forms, dense aero | set by the first E brief |

Class C bands come from the legacy cars (`docs/design.md` §4). The first brief of a new class measures its band and
writes it into this table.

## 3. Coming up with the car (before writing a word of the brief)

1. **Read the slot:**
   - the class, star band, surface and role (starter, rival, quick-looking, rogue, working, everyday, backmarker, flagship);
   - the existing concept line, if the car is in the roster;
   - the nearest roster neighbours, so this car looks unlike them.
2. **Three directions, from three sources:**
   - **its job or life:** what it does all day, and what that leaves on it;
   - **an object it resembles:** a toaster, a bathtub, a bulldog's head, a shoe, a bread bin;
   - **a personality:** shy, pompous, over-eager, grumpy, sleepy, smug.
   Write each in two lines. **Throw away the first idea that came to mind**, because anyone would have had it.
3. **Pick the direction with the strongest silhouette.** Sketch it in words as a black shape from the side: "a loaf
   with a dent in the middle and two big round eyes".
4. **Find the one exaggeration** (at least 25 per cent) that tells the story, and **the signature**: a body form, not a
   prop.
5. **Then fill in every element from the whole car down.** Never detail a part before the level above it is fixed.

## 4. The toy vocabulary

These are the idioms that make a model read as a toy, not a scaled-down car. Every brief uses most of them, and says
which ones and where.

- **Caricature:**
  - big wheels (+10 to 25 per cent);
  - oversized lamps (+10 to 20 per cent);
  - a tall glasshouse and thick pillars;
  - a short, chunky body;
  - wheels at the corners;
  - a wide track, tyres flush with or just proud of the body.
- **Edges:**
  - every outer edge is rounded, about 1 per cent of length on the big edges and never knife-sharp;
  - crisp lines only where a character line or shut line must catch a highlight.
  The rule is **big soft forms crossed by a few crisp lines**.
- **Castings:**
  - one body casting, with engraved shut lines that read dark;
  - a separate dark base plate with cast detail (exhaust, sump, suspension arms, axle ends, rivet heads) and the car's
    name embossed on it;
  - one clear or tinted glass unit, and an interior moulded in one colour.
- **Printing and plating:**
  - liveries as flat pad-printed graphics (tampo) that stop at shut lines;
  - lamp reflectors and bumpers in bright plated plastic;
  - lenses as clear jewels over the plated reflectors;
  - tyres in chunky soft rubber, with printed or raised sidewall lettering.
- **Chunkiness:** no part thinner than about 1 per cent of length. Thin things (aerials, wipers, ladder rails) are
  thickened to toy strength and get rounded ends.
- **Play value:** at least one element a hand wants to touch: a part that looks like it opens, swivels or clips off
  (a serving hatch, a tilting crane, a roof box lid, a spare wheel on a carrier). In the game it's looks only.
- **Comic wear** (rogue cars): rust as printed patches, a mismatched panel, a dent, a missing hubcap, tape, a bungee.
  Funny, never grim: a well-loved toy.

## 5. The brief template

Save the brief as `car-forge/briefs/<class>/<id>.md` (for example `briefs/class_c/puddle.md`). Requirement IDs are the
section code plus a number (`FA3`) and never get reused, because checks refer to them. Aim for **60 to 100
requirements**: enough to fix every element, and each one a real choice.

Each section opens with a short paragraph of vivid prose: what it is and how it feels, written so an artist could draw
from it. A table of requirements follows:

```
| ID | Requirement (what must be true) | Seen in |
|---|---|---|
| FA1 | Two round headlamps, each about 0.55 x the wheel's diameter, set wide so their outer edges are within 5 cm of the body's corners, their centres level with the top of the grille | front, 34front |
```

"Seen in" names one or more of these: `M` (measured by the tool), `side`, `front`, `rear`, `top`, `34front`, `34rear`,
`face` (front close-up), `tail` (rear close-up), `wheel` (wheel close-up), `base` (underside), `clay` (form with no
paint), `zebra` (surface flow), `chase` (game distance), `silhouette` (black on white), `roster` (beside the named
neighbours, plus look-alike scores) and `blind` (the blind read in car-brief-check).

### Header
`id`, name, class, stars, surface, role, the roster slot it fills or replaces, the date, a status line (`draft` →
`building` → `created (check N, date)`), and the **concept line**: one sentence anyone could draw from.

### CH: Character
- the personality sentence;
- the face's expression in one word;
- the stance;
- **the one exaggeration**, with its size;
- **the signature**, and why it is in the body;
- the era and the life it has led.
Requirements cover what the blind read must name (the type of car, the expression and the signature) and what the
silhouette must show.

### SP: Silhouette and proportions
Describe three silhouettes, as shapes traced with a finger:
- the side, from nose to tail: where the bonnet rises, where the screen starts, where the roof peaks, how the tail drops;
- the front, as a cross-section: tumblehome, shoulder width against the wheels;
- the plan (top) view.
Include a measures block, which `tools/proportions.py --brief` reads:

````
```yaml
measures:            # [low, high]; names from tools/proportions.py
  L_over_W: [1.60, 1.75]
  H_over_L: [0.48, 0.54]
  wheel_r_over_L: [0.12, 0.135]
  wheelbase_over_L: [0.58, 0.66]
  glasshouse_over_H: [0.40, 0.48]
  tyre_proud_m: [0.0, 0.03]
```
````

Add a requirement that the silhouette alone, filled black, reads as this car from the side, the front and the
three-quarter view.

### BV: Body volumes and surfaces
Cover every volume with its crown and section:
- the bonnet (crown, bulge or creases, and where they start and end);
- the roof (crown, gutter, rails, any step);
- the sides: the section described top to bottom (tumblehome, shoulder, waist, door surface, sill tuck);
- the main character line: where it starts and ends, and whether it is crisp;
- a second, softer line;
- the arches: flare, lip or blister, how far they stand out, their shape (round, flat-topped, squared);
- each end as a volume of its own;
- the bumpers as separate volumes, with their own section, ends and valance.
Name the **edge plan**: which edges are crisp, which medium, which soft. No panel is a flat slab.

### GL: Glasshouse
- the side window outline (DLO) as a shape: the count of side windows, every corner (radius, kink, the C-pillar
  shape), the beltline (level, rising or kicked up);
- the windscreen: rake, curvature, pillar thickness;
- the rear screen: rake, its curve in plan and in side view;
- the colour of the window surrounds (black rubber, plated, or body colour);
- B-pillar treatment, seals, glass tint, the glass's depth behind the surround.
Window outlines must be true to the kind of car, never bands of a grid.

### PN: Panels and shut lines
- doors (count, where each line runs, how it meets the window frame);
- bonnet, boot or tailgate lines, fuel flap, any sliding door or serving hatch;
- lines meet the window frames and each other the way a real car's do;
- the shut-line width and depth that reads dark at arm's length.

### FA: The face
- **Headlamps:**
  - shape and count;
  - size against the wheel;
  - placement and angle;
  - the bezel or trim ring;
  - **the inner pattern:** reflector type (faceted, smooth bowl, projector), bulb or lens, a daytime strip or any
    eyebrow, the lens's dome;
  - indicators: shape, position, amber.
- **Grille:**
  - outline;
  - infill pattern (bar count and direction, mesh, egg-crate, slots), its depth and backing;
  - surround;
  - the badge (an invented emblem's shape, never a real one).
- **The bumper and valance:**
  - intake shape;
  - fog or spot lamps (count, shape, their own reflectors);
  - plate recess;
  - tow eye.
- The expression these make together, and how it is made: the eyes (lamps), the brow (bonnet edge or eyebrows), the
  mouth (grille or bumper).

### TA: The tail
- **Tail lamps:**
  - outline;
  - how they wrap round the corner;
  - **the chamber pattern**, drawn as a small grid. For example:
    ```
    [ red stop/tail (ribbed) | amber | clear reverse ]
    ```
  - each chamber's inner texture (ribs, honeycomb, prisms, plated pillows);
  - the dark surround.
- **The rest:**
  - the plate and its lamp;
  - exhaust: count, shape, bore;
  - the rear bumper;
  - the rear screen's wiper, badges and script;
  - any ladder, hatch or spare.

### WH: Wheels and tyres
- rim design: spoke count and style (steel with holes, hubcap, multi-spoke, turbine, dish, split-rim), its colour and
  finish;
- the centre cap and lug nuts;
- depth: spokes proud of a dark recess;
- brake disc and caliper (where they suit the car);
- the tyre: profile height, sidewall bulge, rim protector, tread pattern and how it wraps the shoulder, any lettering;
- size, fill of the arch (the gap above the tyre) and proudness.
Front and rear may differ, and a rogue car's wheels may not match.

### SM: Small parts
Each part with its position, its shape and how it's made (cast, separate or printed):
- mirrors (and their sail panel);
- door handles and the key lock;
- wipers and washer jets;
- fuel cap;
- side repeaters;
- badges and script (invented);
- the number plates (border, raised characters);
- aerial, roof rails, gutters, sunroof seams;
- mud flaps, tow ball;
- anything the car's life adds.

### KT: Kit and props
Every prop that tells the story (roof sign, crates, ladder, beacon, spare wheel, roof box), each as a **built object
with its own detail**: frame, straps, fixings, lettering. Say how it is fixed to the car. Props support the signature;
they never replace it.

### TY: Toy elements
Which idioms from §4 this car uses, and where:
- the exaggerations, with sizes;
- the edge softness;
- the base plate and its embossed name;
- the plated parts;
- the tampo graphics;
- the play-value element;
- the chunkiness minimums for its thin parts.

### PM: Paint and materials
- the base colour as a named toy colour (post-box red, harvest gold, mint);
- the second tone and where it goes;
- the livery: tampo stripes, numbers, signwriting (invented words and businesses only);
- **a material map**: paint, satin black, plated, rubber, glass tint, each lens's colour;
- the wear, if any, panel by panel.
The car must still read in a rival's paint from the race's list.

### GD: Game distance
The three things that must read from the chase camera at race distance, where lamp detail is a few pixels: for
example "the tall white roof sign, the yellow-and-black stripe, the fat rear wheels".

### RO: Against the roster
- the nearest two or three cars in the roster;
- what makes this one different from them, in silhouette and in kit.
The game-distance sheet must show no near twin.

### NT: Not this
The anti-goals: what would make this car a failure, each one written as a requirement that can fail. For example:
- "The sides are not flat slabs: the side section has a visible shoulder and a sill tuck in the front view."
- "It is not a stock hatch with a prop on top: with all props removed, the body alone is still recognisably this car
  in clay."

### Lessons from the first brief (the Puddle, 24 Oct)
- **Bands must leave room for the signature.**
  - The Puddle's signature (the sit) had to lead the blind read, but its own bands capped it: rake at most 4°,
    haunches at most 8 cm, the rear wheel at most 1.18 x the front.
  - In eight rounds every reader found the face unique instead.
  - Set the signature's bands from the size it needs to read, or make the signature the boldest thing in the brief.
- **Check requirements against each other:**
  - a raked car's two bumper tops are at different heights, so one rubbing strip can't meet both;
  - a "nothing else is plated" list must name every part other requirements ask to be plated.
- **Kind, not menacing:**
  - Richard saw "a murderer" in huge staring eyes over a five-bar grin;
  - bars across a smile read as teeth;
  - big eyes stay friendly when they nestle into the body and have soft brows, not when they bulge above it;
  - add an NT requirement against menace whenever a car has a face.

## 6. Before calling the brief done

- [ ] Every section is present, and every requirement has an ID and a "Seen in".
- [ ] 60–100 requirements. Fewer means elements are missing; more means requirements need merging.
- [ ] No brand names, no toy-line names, no "like <roster car>". Search the text for them.
- [ ] Every requirement could fail: reread each and ask "what would a FAIL look like?"
- [ ] There is a measures block, and its bands fit the tier (§2).
- [ ] The exaggeration is at least 25 per cent; the signature is a body form and shows in a black silhouette.
- [ ] The NT section names the failures this car is most likely to have.
- [ ] Nothing was softened because "the Forge can't do that".
- [ ] For the first brief of each class, or a hero car: send it to Richard before building. For others, record the
      brief and go on (design freedom, 20 Oct).

Then build the car in Car Forge (`car-forge/docs/authoring-guide.md`), and judge it with **car-brief-check**. When the
Forge can't build an element, fix the Forge rather than the brief.
