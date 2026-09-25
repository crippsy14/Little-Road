# Bayview map sweep (2026-09-25)

Goal: lean Bayview into a Road Trip Adventure 2 toy world without touching the race corridor.
Captures come from `.\tools\capture-map.ps1 -Name <run>` (world review plus `--map-views`); run folders are never overwritten.

| Run | What changed | Clearance |
|---|---|---|
| before | Baseline (flagged: Rowan's Otter inside the shoulder at offset 11) | fail (pre-existing) |
| a1, a2 | Toy terrain shader (meadow patches, wildflowers, soft sandstone bands), toy sea, Neutral grading | fail (pre-existing) |
| b1–b2 | Kit high street; fountain/Rowan moved out of the shoulder | pass |
| b3–b5 | Harbour (sea wall, rail, stairs, pier, boats), kit lighthouse, windmill; palette fix | pass |
| b6 | All island trees and scrub as kit models | pass |
| c1–c2 | Farm, fields, campsite, lookout, village green, car park, chevrons, telegraph poles | pass |
| d1-traffic | `--ambient-traffic` (4 cruising family cars, free roam only, off by default) | pass |

## Checks

- Adventure smoke (`capture-adventure -Name v5-map-b`): all passed. This was after phase B and was not rerun after C/D.
- Frame-cost study 122, `bayview-s1709-map-cd`, rendered, compared with 120 `bayview-s1709-off`:

| | Finished | Collective (s) | Resets | Contacts | Mean frame | p95 frame |
|---|---|---|---|---|---|---|
| 120 (before map) | 24/24 | 803.352 | 0 | 6 | 12.05 ms | 26.73 ms |
| 122 (after C/D) | 24/24 | 803.352 | 0 | 6 | 10.55 ms | 20.88 ms |

Racing is identical; the toy trees render cheaper than the old realistic foliage.

## Rules kept

- Nothing new is solid within 12 m of the road centreline. `VillageReview` checks road support and obstructions every run.
- The sea-wall railing's colliders stop cars going over the promenade edge.
- Chevrons are non-solid.
- Free-roam traffic is behind `--ambient-traffic`.

## Still open

- Harbour Garage is still the older cube build.
- No lay-by viewpoint (it would need a physical road change, which needs the AI protocol).
- Ambient traffic cars never recover if stuck, because finished-car planning skips recovery.

## Fixes and paint shop round (same day)

- Fixes: window/shutter depths (no z-fighting), coral kerbs 6 mm prouder (curve overlap flicker), promenade kept flat to the sea wall, free-roam car pitches/rolls with the ground (`--free-roam-level` restores; target tilt capped at 30 degrees, hard righting past ~40; hill review: no tipping), Puddle roof spare seated on the roof.
- Paint shop: Dot at BAYVIEW PAINTS, 16 colours (first four = old save colours), 150–200 credits, `AdventureStore.Repaint` plus 4 editor checks (877 total). Mara's free repaint removed. `--paint-shop-review` captures in `paint-1`/`paint-2`.
- NPC speech bubbles on bumps: residents (own lines), parked, campsite and traffic cars.
- Adventure smoke v6-paint was an outlier: 6 AI resets, 26 contacts, collective 2852.7 s, with no scenery impacts. The repeat on the same build (v7-paint-repeat) matched earlier runs exactly (0 resets, 2068.7 s). The smoke race isn't fully deterministic, and that run exposed AI physical-recovery "Wait for clearance" loops; an AI robustness item, not a map one.
