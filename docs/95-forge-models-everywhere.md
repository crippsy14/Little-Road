# 95 · Car Forge models everywhere, a smooth shop preview, and the garage screen (23 Oct)

Richard played the 23 Oct play copy and reported four problems with the car models:

1. Many cars in the world still used the older models instead of the Car Forge ones.
2. The player's own choices were all old models, in the body shop and the garage bay.
3. The body shop's car preview ran at a very low frame rate.
4. There was no proper screen for browsing the models you already own.

This note records what changed, how it was checked, and what is still open.

## 1. One rule for every car in the world: `CarBodies.Look`

Every old class C body already has a Car Forge namesake in the roster: `pebble` is `forge_pebble`, `gelato` is
`forge_gelato`, and so on. The compact (`compact_v1`) maps to `forge_pocket`, the Pocket compact. Rather than editing
each of the twenty-odd places that pick a body, the mapping sits where every car model is made:

- `TrialWorld.Car` shows any old id as its namesake. It names the car after the model it shows, so the collision box
  is that model's own (`RoadCar.CollisionBox` reads the car's name).
- `RoadCar.ReplaceBody` does the same for garage swaps.
- Bodies with no namesake stay as they are: the class B Tuners (their Forge roster is still to be redone), the Pit
  Stop's own vehicles, and the Regale.

That covers race fields (the recipe fields were already Forge since 20 Oct; the older field paths now show Forge
models too), valley and town traffic, parked cars, residents and townsfolk, the Quarry festival and Bramblewick's
spectators. It reaches the files the start-up session was editing without touching them.

The roster also has 25 cars with no old model. These are the characters: the Chippy, the Gold Top milk float, the
Ladders, the Hookline tow truck, the Pronto, the Wren, the L-Plate, the Rattler, the Patchwork, the Sunday Best, the
Woody, the Shedbuilt, the Stock Banger, the Haybale, the Coastguard and the Sandhopper. They now appear in the world:

| Where | What changed |
|---|---|
| Hillcrest's ten valley traffic cars | Five are new characters |
| Bramblewick's lane traffic | Gains Peg's milk float (the chatter already mentioned it) |
| Vinelands traffic | Gains the Pronto, the Wren three-wheeler and the Sunday Best |
| Parked and resident pools | Gain the rust buckets and working vans (`CarBodies.WithForge`) |
| Quarry Hills' working vehicles | Gain the Haybale, the Coastguard and the Sandhopper (`CarBodies.OffRoadFolk`) |
| Bayview's harbour parking | Has the Chippy and the Gelato van |

## 2. The player's cars

- **New saves** start in the Forge Puddle (`CarBodies.StarterBody`).
- **The body shops** sell the Forge range (`CarBodies.ForgeRange`):
  - Hillcrest has 43 tarmac and rally cars, from the Pocket at 400 cr to the Zenith GT at 4,800 cr.
  - Quarry Hills has 11 gravel cars.
  - Every old body's namesake keeps its old price. New cars are priced by star band: the Kestrel is 2,400 cr, the
    Otter GT 3,400 cr.
  - The Tuner Garage's class B range is unchanged.
- **Names and descriptions:** each car has its own name, a line on what it is, and its signature (`CarBodies.About`,
  `CarBodies.Signature`). These come from the first lines of its car-forge concept.
- **Old saves still load.** On loading, the driven body and every owned body become their namesakes (`puddle` becomes
  `forge_puddle`, the compact becomes the Pocket, a Tuner stays, duplicates merge). The save is written back the next
  time it changes. `Ownable` accepts both ranges, so a save loads either way.
- **Repainting a Forge car** now composes its livery again in the new paint (`ForgeLivery.Repaint`). Tinting the old
  livery, which is what happened before, only darkened it. This was a new bug, because the player's car is now a Forge
  car. The old livery texture is freed.

**Opt-out:** `--no-forge-roster` (the Forge roster's flag since 20 Oct) brings back the old models, pools and range
everywhere. The old-world fingerprint runs can still use it. The roster is on in player builds and in the editor's
checks, as it has been since 20 Oct.

## 3. The body shop preview

Measured with `tools/review-body-shop.ps1` (`BODY_SHOP_FRAMES`: 150 frames, vsync and the frame cap off, same build;
`--old-shop-preview` is the old behaviour):

| | Shop frame | Preview renders in 150 frames | Town on its own (same run) |
|---|---|---|---|
| Old (`--old-shop-preview`) | 24.3 ms avg (p95 28.3) | 37 | 26.2 ms |
| New | 7.3 ms avg (p95 8.2) | 150 | 17.0 ms |

There were three causes:

- **The turntable stepped.** It re-rendered only every 2 degrees, about 10 to 12 times a second whatever the frame
  rate, so the car moved like a slide show. This is what read as a very low frame rate. It now renders every frame.
- **The whole town drew behind the opaque shop.** The world camera now draws nothing (no renderers, no
  post-processing) while a full-screen shop is open (`Toy.HideWorld`). A full-screen fill in the shop's colour covers
  screens wider than 16:9.
- **Every focus change built a new preview stage.** That meant three 4x MSAA render textures (two of them unused) and a
  livery texture that was never freed. One stage now serves every car (`PartPreviewStage.SetCar`). A car-only stage
  makes no part textures, renderers are found once, and livery textures are freed.

The board now scrolls with the focus (and the mouse wheel), because 43 cars no longer fit. It shows "N of 43 models ·
K yours", and the tag shows what each car is.

## 4. The garage screen (`GarageScreen`)

At any Pit Stop garage bay, "My garage · N models" opens the new screen. It replaced a text list of names.

- **On the left,** the focused car turns on the reveal studio's turntable (the dark set with the mint rim) in your
  paint, with a DRIVING NOW or IN THE GARAGE ribbon.
- **On the right,** its card shows:
  - the name;
  - what it is;
  - its signature;
  - chips for class, surface (tarmac, gravel, or rally on any surface) and "quick-looking";
  - your paint;
  - the reminder that looks never change pace.
- **Below,** a strip of stills of every car you own: the one you drive is badged, and the strip is centred when it is
  short and scrolls when it is long. The stills are rendered once each when the screen opens, one a frame, nearest the
  focus first.
- **Controls:** Cross drives the focused car (free; a swap down a class refits parts as before, and the tuning now
  follows: `ApplyEquipment`). Circle goes back to the bay.
- **Cost:** `GARAGE_FRAMES` averages 7.0 ms a frame (p95 8.4).

## Checks and tests

- **Editor checks:**
  - `PitStopChecks` covers the Forge range: every roster car but the starter is sold, only roster cars are sold, old
    namesakes keep old prices, and every car has a name, description and signature.
  - It also covers an old save migrating, writing back and reloading.
  - `ForgeLiveryChecks` now allows owning roster cars, while the engine's test designs still need `--forge-drive`.
  - `WheelChecks` accepts kei-car wheels (0.27 to 0.30 m) on the Forge Puddle and Pocket.
- **The build** passes all 4,539 checks.
- **Body shop review** (`tools/review-body-shop.ps1`): exit 0. It buys the Kestrel, opens the garage and drives the
  Kestrel from it (`swapped=True`). Frame times were measured three times as the machine got busier: shop 7.3, 9.5 and
  9.4 ms; garage 7.0, 9.0 and 9.2 ms; the town behind them 17, 34 and 45 ms.
- **Adventure smoke** (`tools/capture-adventure.ps1`): `LITTLE_ROAD_ADVENTURE_SMOKE_PASSED`, with the pause, result,
  retry and input checks passed too. The fixture save was on the old `puddle`.
- **A full race** (`tools/event-race-review.ps1 -Event hillcrest`, the Valley Cup):
  - 24/24 finished, no resets, 6 contacts, 16 passes.
  - All 24 cars were Forge models, the player's among them. The fixture's old `puddle` migrated to `forge_puddle` and
    was written back.

## Open

- **Class B:** the Tuners keep the old models until class B's Forge roster is redone with Richard's direction.
- **Pit Stop vehicles:** the Pit Stop's own vehicles and the Regale stay as they are; neither has a Forge design.
