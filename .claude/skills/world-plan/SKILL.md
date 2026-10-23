---
name: world-plan
description: Breakdown of the planned Little Road world (docs/world.md, layout B "Glasswater") - every planned town and village, its class, distance from Bayview, features, tracks and races, the race catalogue per class with planned stars, and the world blueprint map. Use when planning races, unlocks, parts, star difficulty, new areas or anything that depends on where places are or what races the full map will have.
---

# The planned world

The full map plan lives in three places that agree:
- **docs/world.md**: the design. §1 the continent, places, story, principles and build order; §2 standards for places and tracks; §3 the built places.
- **The world blueprint artifact**: the interactive map, pinned in Richard's claude.ai sidebar. It has the generated map,
  a route planner with drive times per class, race heat maps and the race catalogue.
  https://claude.ai/artifact/L9zDqLxe29UbkXcLHmfPrb
- **docs/data/world-plan.json**: the repo's copy of the blueprint's data (33 places, 27 tracks, 2 Grand Tours). It's
  extracted by `tools/world-plan-extract.py`.
  - **Planned stars** per event (classes C and B) are in **docs/data/race-stars.json**.

## Get a breakdown
Run from the project root, with the Blender Python (there's no system Python or Node on Richard's PC):

```
"C:/Program Files/Blender Foundation/Blender 5.2/5.2/python/bin/python.exe" tools/world-plan.py
```

| Arguments | What it shows |
|---|---|
| (none) | Races per class and each class's home places (109 races, plus 12 post-game) |
| `class B` | Every place of a class: kind, km from Bayview, features, tracks, races, description |
| `races B` | Every race the class runs anywhere, nearest first, with planned stars and the extra events |
| `place harbour` | One place in full (by id or name) |
| `places` | Every place, nearest Bayview first |

## Rules the plan follows
- **Main towns** run one race per class on each track, plus one more in the town's own class:
  - the club layout serves C and B;
  - the pro layout serves A, S and E (post-game E on every pro layout).
- **Villages, Lowtown and the Grand Tours** list their own races. Villages are built from a recipe: recruit, parts,
  track, rewards.
- **Only five places sell local parts** (docs/world.md §1, decision 13): Bramblewick (C), Highmoor (B), Copper Gulch (S),
  Icefall Hut (E-ice) and Lowtown (secret).
- **What's built so far:** Bayview, Hillcrest, Quarry Hills, Bramblewick and Vinelands. Everything else is planned only. `--scale-test` builds
  the whole continent as graybox (`ContinentPlan.cs` has the same places in game coordinates).
- **Class B stars** run 1–8 across the 26 planned events, 120★ in all. The Tuner Championship is the 10★
  finale: three races, at three towns with class B races, to be chosen later.

## When the blueprint changes
1. Save the page: Artifact `read` with `path: "index.html"`.
2. Re-run `tools/world-plan-extract.py <saved index.html>`.
3. Check `tools/world-plan.py` still shows docs/world.md's totals.
