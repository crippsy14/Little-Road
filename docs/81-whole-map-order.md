# 81. The whole map: the order we build it in (20 October 2026)

Richard, 19-20 Oct: once the three towns are done, "plan the whole map (all towns, parts, races) and start working
through those towns one by one".

This doc is the order of work. The design stays where it is:
- [doc 58](58-world-and-campaign-design.md): places, races, story, principles, build order W0-W4;
- `docs/data/world-plan.json`: the blueprint's 33 places, 27 tracks and 2 Grand Tours;
- [doc 44](44-parts-tiers-and-progression.md): the parts ladder;
- [doc 70](70-race-fields-across-the-map.md): race fields and stars;
- [doc 67](67-class-b-races-and-unlocks.md): class B races and unlocks;
- [doc 57](57-new-area-standards.md): the standards a new area is held to.

`tools/world-plan.py` prints any of it.

## Where we stand
- **Built and remodelled:** Bayview (the new island), Hillcrest and Quarry Hills (docs/79-80). Prop Forge kits, the
  area audit and the street audit are all default.
- **Races before the credits:** C 21, B 23, A 29, S 26, E 10, plus 12 post-game E.
  - The three built towns hold C's main-town races today.
  - Every other race needs its place built first.
- **Parts:** only five places sell local parts (doc 58, decision 13): Bramblewick (C), Highmoor (B), Copper Gulch (S),
  Icefall Hut (E-ice) and Lowtown (secret). The rest come from the Pit Stops on the doc 44 ladder.

## How each place is built (the loop since 20 Oct)
1. **Brief** (doc 57, step 0):
   - the vision, the district plan, races and activities;
   - which kits it needs, and which of them are new;
   - the questions only Richard can answer, asked at the start in one go.
2. **Kits first:** any new Prop Forge or Car Forge pieces, each judged on its contact sheet before it goes in a town.
3. **Build** behind the place's flag. Roads and rail join it to its neighbours.
4. **Check:**
   - `--area-audit` with the street audit at zero defects;
   - the old world's fingerprint unchanged;
   - its races 24/24 on seeds 1709-1711;
   - targeted photos of what the audit flags.
5. **Targeted review:** one Sonnet agent on one contact sheet of about 12 chosen views. Its agreed suggestions go into
   the next place's round.
6. **Default:** once it passes, the flag comes off. Commit, push, merge to `main`, publish the play copy.

## The order
Nearest first within each wave, but each main town comes before the villages that hang off it. A village needs its
main town's roads and railway.

### W2 · the class C ring

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 1 | **Bramblewick** | C village | 2.9 | local parts, a recruit | - | C 1 |
| 2 | **Vinelands** | C main | 4.6 | - | Vineyard Loop (club, 1.6 km); Vinelands Hill Climb (pro, 3.8 km) | C 2 |
| 3 | **Orchard End** | C village | 3.8 | a recruit, a track | - | C 1 |
| 4 | **Kettle Falls** | C village | 5.2 | a track, rewards | - | C 1 |
| 5 | **Cobble Cove** | C village | 5.8 | a track | - | C 1 |
| 6 | **Saltmarsh** | C village | 6.6 | a track, rewards | - | C 1 |

### W3 · a vertical slice of class B

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 7 | **Harbour Point** | B main | 8.6 | - | Quayside Sprint (club, 1.4 km); Coast Road GP (pro, 5.2 km) | B 2, S 1 |
| 8 | **Coral Key** | B island | 4.3 | - | Reef Street Circuit (club, 2.0 km); Reef GP (pro, 4.0 km) | B 2 |

### W4 · class B

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 9 | **Tollgate** | B village | 5.5 | a track | - | B 1 |
| 10 | **Highmoor** | B village | 5.8 | local parts | - | B 1 |
| 11 | **Millbrook** | B village | 6.3 | a recruit, a track | - | B 1 |
| 12 | **Summit Pass** | B main | 8.2 | - | Summit Climb (club, 3.0 km); Summit Pass Downhill (pro, 7.0 km) | B 1, S 1 |
| 13 | **Dunmore Junction** | B village | 8.4 | a recruit, a track | - | B 1 |
| 14 | **Ridgeway Circuit** | B village | 12.0 | a track, rewards | - | B 2 |
| 15 | **Lowtown** | B secret | 11.8 | local parts, a recruit, rewards | - | B 1 |

### W4 · class A

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 16 | **My City** | A hub | 8.0 | - | Waterfront Circuit (club, 2.2 km); GP Park (pro, 5.5 km) | A 3, S 1 |
| 17 | **Fernhollow** | A village | 8.1 | a recruit, a track | - | A 1 |
| 18 | **Pinewood Coast** | A main | 10.8 | - | Forest Sprint (club, 2.4 km); Pinewood Rally Stages (pro, 8.0 km) | A 2 |
| 19 | **Cinder Flats** | A village | 12.8 | a track, rewards | - | A 1 |
| 20 | **Larchwood** | A village | 12.8 | a track, a recruit | - | A 1 |
| 21 | **Lowmere** | A village | 14.3 | a track, rewards | - | A 1 |
| 22 | **Port City** | A main | 16.8 | - | Docks Street Race (club, 2.0 km); Port City Circuit (pro, 5.0 km) | A 2 |
| 23 | **Cape Lantern** | A village | 18.9 | a track, rewards | - | A 1 |

### W4 · class S

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 24 | **Mirage Wells** | S village | 13.1 | a track, a recruit | - | S 1 |
| 25 | **Redrock Mesa** | S main | 13.4 | - | Canyon Sprint (club, 2.6 km); Redrock Desert Rally (pro, 9.0 km) | S 2 |
| 26 | **Copper Gulch** | S village | 13.4 | a recruit, local parts | - | S 1 |
| 27 | **Icefall Hut** | S village | 14.6 | local parts | - | S 1 |
| 28 | **Frostcrown** | S main | 15.9 | - | Frozen Lake Oval (club, 1.5 km); Glacier Ice Circuit (pro, 4.5 km) | S 2 |

### W4 · class E

| # | Place | Class, kind | km | What it has | Tracks | Races |
|---|---|---|---|---|---|---|
| 29 | **Polar Post** | E village | 18.8 | a recruit, rewards | - | - |
| 30 | **Aurora Raceway** | E main | 19.3 | - | Aurora Club Circuit (club, 2.4 km); Aurora GP (pro, 6.0 km); Aurora Endurance (endurance, 9.0 km) | E 10 |

Lowtown is the secret town under My City (doc 58 §7). It is built with My City and opened after the credits.

## Work that runs alongside the towns
These aren't places, but the places need them. Each goes into the round of the place that first needs it.

| Work | First needed by | Notes |
|---|---|---|
| **UI art pass** (bug #18: "AI-ish UI elements") | Now | Its own brief for Richard first: type, colour, the pill and banner shapes, the HUD, against `docs/art-direction.md` |
| **Fields redo** (bug #20: cars clip through crop rows) | Bramblewick (farm village) | Crops that part and flatten where a car drives through, with tracks left behind; fields Prop Forge kits |
| **Roads beyond the three towns** | Bramblewick | Valley Road South from Hillcrest to Vinelands via the Bramblewick turn (docs/68 §roads); the Kettle Falls gate opens in step 4 |
| **Railway branches** | Bramblewick (Coast Branch), Orchard End (Glasswater & Southern) | Doc 58 §8; trains to ride between towns |
| **Parts** | Bramblewick (C), Highmoor (B) | Bramblewick's backyard tuner: class C parts not sold in Bayview (doc 70 row 4) |
| **Teams and recruits** | Class A (teams open) | Recruits are found earlier: Bramblewick's milk-float champion, Orchard End's apple-cart racer |
| **Class B roster redo** | W3 | Withdrawn 18 Oct, to be redone with Richard's direction from the start |
| **Railway wars, frenzy layer, Endurance, credits, post-game** | After W4 | Doc 58 §7-8, principle 23: the graybox path to the credits exists before polish |

## Leftovers carried from docs/80
- Audit defects:
  - Hillcrest's floating vine row and one hidden sign;
  - two waterline rocks at the new Bayview;
  - the road-edge lips in Quarry Hills and Hillcrest Valley.
- From the Quarry Hills review, QH2-09 to QH2-22: pavement ends, blank gables, slope textures.
- Quarry floor dressing (review R7-03).
- Fields (above).

## Next
**Bramblewick:** its brief is [doc 82](82-bramblewick.md).
