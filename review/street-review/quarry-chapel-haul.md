# Quarry Hills street review: Chapel Street and the Haul road (partial)

Source: review/quarry-hills/streets-1/streets/quarry-hills/{chapel-street,haul-road}/. Coordinates are world x,z, taken from streets.json (the camera position or look-at point, or an estimate near it). No close-ups were taken.

## Findings

**CH-1 | Chapel St | 000-left, 000-right | ~(-2582,-2497), the corner where Chapel St meets the main road | bug | medium**
The slate retaining wall around the corner plot comes in 3–4 separate pieces that don't join. In 000-right there is a straight stub, a gap, a second straight run, a gap, and then a curved section. The pieces have different top heights, and one end stops in the open grass. In 000-left the curved piece kinks and steps where it meets the straight piece in front of the gable.
Fix: build the corner wall as one polyline that follows the kerb radius, with a constant top height (or a stepped coping that follows the slope) and end piers or caps.

**CH-2 | Chapel St | 000-left, 000-right | gable ends at ~(-2578,-2505) and ~(-2588,-2491) | improvement | medium**
Two large blank end-of-terrace gables face the junction. All they have is closed grey shutters. This is the entrance view of the street.
Fix: make one of them slate-hung, which is classic on the weather side in north Wales. Add a painted ghost sign, a drainpipe, and a bilingual street sign "Stryd y Capel / Chapel Street".

**CH-3 | Chapel St | 001-right, 004-right (also 002-right "CHIPS", 000-right "STOP") | 001-right look (-2624,-2501); 004-right look (-2740,-2521) | bug | medium**
The shop name boards sit on the first-floor balconies, behind the railings and flower boxes, so the letters are cut off and hard to read. "POST" and "SLATE CRAFTS" are half hidden. One board on the far left of 001-right can't be read at all ("?VER..").
Fix: put the name on a proper fascia board above each awning at ground-floor level, clear of the railings.

**CH-4 | Chapel St | 001-left, 003-left, 004-left | e.g. 003-left look (-2697,-2540) | bug (visual) | low–medium**
Each top-floor arched loggia shows a pure black void with 5 coloured gem-like spheres on the sill. The spheres look like they float, and the opening reads as a hole in the model.
Fix: give the loggia a back wall or interior colour, and swap the spheres for real planters or flower pots.

**CH-5 | Chapel St | 000-along to 004-along (the whole street) | (-2579,-2500) to (-2775,-2538) | improvement | medium**
Both sides are one long run of the same 3-storey rendered terrace, with cream quoins, coloured shutters, balconies and loggias. It feels repetitive and Mediterranean rather than like a Welsh quarry street, and the street has no chapel in the reviewed photos.
Fix: break it up with runs of plain 2-storey slate-stone quarrymen's cottages (slate roofs, sash windows, no shutters, doors close to the pavement). Add a gable-fronted nonconformist chapel with a date plaque (for example "Capel Bethel 1868") as the street's landmark, ideally where it ends the view at the bend (see CH-7).

**CH-6 | Chapel St | 001-left, 002-left, 003-left, 004-left | whole street | improvement | low–medium**
Front doors open straight onto the grass verge. There is no path from the pavement, and every house repeats the same small bench under its window.
Fix: lay a slate-flag path to each door. Add front-yard boundaries of upright slate slabs wired together (crawiau). These are unique to the slate valleys and would give the street instant Welsh character. Use the under-window benches more sparingly.

**CH-7 | Chapel St | 005-along | road end ~(-2800,-2537) | improvement | medium (also check a possible bug, low)**
The terrace stops suddenly and the street ends on a bare grass bank, with a sandy path running straight up it. A lone bench stands on the steep bank at about (-2800,-2537). It may be tilted or half-sunk on the slope; this is unconfirmed and needs a close-up.
Fix: end the view with something. A slate retaining wall with steps up the bank, the chapel, or a slate tip would all work. Put the bench on a level slate pad, or move it.

**HR-1 | Haul road | 000-right | ~(-2600,-2362), by the square's plaza | bug | medium**
A short slate wall stub (about 4–5 m) stands on its own at the grass/plaza edge, with a bench just behind it. It doesn't connect to anything at either end.
Fix: if the grass really is higher than the plaza, run the retaining wall along the whole edge. If not, remove the stub and grade the grass to meet the paving.

**HR-2 | Haul road | 001-right | ~(-2603,-2402) | bug | low**
A street bench stands right in front of the purple-door house, between its door and window, with the house's under-window bench just behind it. The entrance looks blocked and cluttered.
Fix: move the street bench a few metres along, clear of the door.

**HR-3 | Haul road | 008-along/left/right, 009-left/right | (-2491,-2656) to (-2473,-2692) | improvement | high**
The approach to the quarry is a deep cutting with bare grass on both sides, broken only by scattered grass tufts. It has no quarry or industrial detail at all, so it feels like an empty, generic hill road.
Fix: cut exposed slate rock faces into the banks. Add waste-slate spoil tips (the defining Welsh quarry landmark), an old incline with rails and a winding drum, a few slate wagons (drams), fence posts, telegraph poles, and a quarry gate with a bilingual sign ("Chwarel ... / ... Quarry").

**HR-4 | Haul road | 009-right, 010-along, 010-left, 010-right | (-2455,-2727) and around | improvement | medium**
The road comes out onto a vast flat uniform grey plane, the quarry floor or race area. It meets the grass with a hard, painted-looking edge and no transition. There are a few tiny buildings and a lone lattice tower out in the middle, and the space reads as unfinished.
Fix: put a transition band (gravel, slate scree, rubble) along the edge. Break up the plane with slate stacks, spoil piles, sheds, a weighbridge and puddles, and vary the colour.

**HR-5 | Haul road | 008-along, 010-along | the quarry pit wall in the distance, around (-2440,-2780) | bug (visual) | low**
On the curved end of the quarry pit's terraced wall, the texture is visibly stretched into vertical streaks, which gives it a "cylinder" look. The pale zig-zag triangles (the inclines) read as flat polygons. This is geometry and texturing, not distance blur.
Fix: correct the UVs (triplanar or world-space mapping on the pit walls), and give the inclines some thickness.

## Overall impression (so far)
Chapel Street works as a street. It is cheerful, well lit and tidy, with lamps, planters and benches, and no floating or sunk props were found. Its weaknesses are a broken corner wall at the entrance, shop signs hidden behind balconies, and heavy repetition of one Mediterranean-looking house type, so it doesn't yet say "Welsh slate town" or "chapel". The Haul road is lively in town but becomes empty grass for its middle and upper stretches. Then it opens onto an unfinished-looking flat quarry floor. Its biggest opportunity is quarry and industrial dressing along 250–410 m.

## Continue from here
- Reviewed: Haul road 000, 001, 008, 009, 010 (along/left/right); Chapel St 000–004 (along/left/right) and 005-along.
- Not reviewed: Haul road 002–007, all three views each (18 photos; they failed to load because of an image request limit). Chapel St 005-left, 005-right and 006–009, all views (14 photos).
- Close-ups planned but not taken: (1) the CH-1 corner wall at about (-2582,-2497), from g+2 and from above; (2) the CH-7 bench on the bank at about (-2800,-2537), to see whether it is tilted or sunk; (3) the HR-1 wall stub at about (-2600,-2362).
