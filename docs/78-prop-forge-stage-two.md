# 78. Prop Forge, stage two: remodel the world and bring it to life

Richard, 18 Oct: "Go crazy, no holds barred. Everything in the game can be replaced if it would benefit from a remodel,
and we can go crazy with new assets to bring it all to life, small or large. Be ambitious!"

This document is the plan for that work. Stage one (doc 77) built the engine: runs, objects, the library and the
proving ground. Stage two puts the engine to work across the whole world. Everything stays behind `--prop-forge` until
Richard has driven it, so the game without the flag stays as it is.

## How it works
- **Stand-ins.** A Forge kit can declare `REPLACES = {game kit id: [variant pieces]}`. With `--prop-forge`,
  `BayviewKit.Spawn` takes the Forge model, a seeded variant per placement, in place of the Blender kit's single model
  (`ForgeStandIns`).
  - The stand-in is coloured by the game's palette names, so tints, patterns, night windows, colliders, footprints,
    culling and far stand-ins all apply unchanged.
  - Every existing placement in every town takes the new model, with no layout code touched.
- **New ids.** A Forge kit can also bring ids the Blender kit never had (a post box, a lobster pot). `BayviewKit.Has`
  and `Spawn` take them like any other, so new dressing code can place them by name.
- **Runs** replace the cube-built walls, rails and kerbs: the dressing code asks `Runs` for a run along a line instead
  of stamping boxes.
- **Judging.**
  - Every family is judged on its contact sheet first (`tools/prop_sheet.py --grid`).
  - Then it's judged in the world against the same views before and after (`tools/capture-map.ps1`, flag off and on).
  - It must beat the old model outright to stay: variety alone is not enough. The first stone pines lost to the kit's,
    and were redone.

## What's worth remodelling (the inventory, 18 Oct)
The Blender kit's buildings and big landmarks are already good toy models; they come last.

The gains are, in order:
1. **One model repeated where a family belongs.** 5,400 mainland trees and 640 on the island were one model per
   species; rocks were a squashed sphere and three boxes.
2. **Structures stamped from plain cubes:**
   - Hillcrest's Via Crest parapets, the square's rim wall, terrace walls and plinths;
   - the valley guardrails;
   - the causeway, bridges and viaduct;
   - the raceway kerbs;
   - Quarry Hills' 1.9 km of dry-stone walls and its field gates;
   - the sheep;
   - the phone and post boxes, headstones, fingerposts and speed cameras.
3. **Small kit props of a few lines each:** the fence, bench, flag pole, telescope, grass tuft and campfire.
4. **Life the world doesn't have yet:**
   - harbour clutter (lobster pots, crates, nets, buoys, rowing boats);
   - street clutter (bins, bollards, planters, post boxes, bus stops, signs);
   - garden and farm detail (washing lines, wheelbarrows, troughs, gates);
   - the beach (deckchairs, windbreaks, a lifeguard hut).

## The order of work
1. **Nature (done, 18 Oct):**
   - **Trees:** eleven species, 49 seeded trees, each by its own habit.
   - **Rocks:** faceted boulders with moss on the stone, slab stacks and clusters.
2. **Runs over cubes:**
   - Quarry Hills' walls become the dry-stone kit, and its gates a five-bar gate object;
   - the valley guardrails become an Armco kit;
   - the Via Crest parapets and square walls become a capped stone parapet kit;
   - the raceway kerbs become a kerb profile;
   - hedgerows become a deforming hedge kit.
3. **Street and harbour life:**
   - furniture families: lamp posts by town, benches, bins, bollards, planters;
   - new clutter;
   - dressing rules that place them along streets, quays and gardens by what's near (a bin by each bench, pots by the
     quay edge).
4. **Animals and craft:** a sheep family (fleece puffs, a face, legs) and a boat family (dinghies, fishing boats).
5. **Buildings:** a plot-fitted generator, after the rest.

## Bars (doc 73)
- Every family is checked on its contact sheet, then in the world against before-and-after views.
- Budgets:
  - an object stays under 1.5k triangles at LOD0 where it's dense (trees under 3.5k, bushes under 2.5k);
  - LOD1 is a quarter of that;
  - far trees keep KitLod's stand-ins.
- With the flag off, the world's fingerprint is unchanged.
- The full regression runs at each landmark.
