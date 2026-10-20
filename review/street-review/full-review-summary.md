# Street review, full run: summary (5 Oct)

This run covered the 154 photos left over from the paused Opus review on 4 Oct. Quarry Road 005–012 was already covered by the effort trial.
- **Reviewers:** six `street-reviewer-high` (Sonnet) batches ran in parallel. Each prompt warned about the trial's three false alarms: kerbside lamps that only look as if they pass through cars, chimney smoke puffs, and buildings at different depths.
- **Result:** all 154 photos were read, with 73 findings. None of the reviewers repeated the three known false alarms.
- **Checking:** the main session checked 13 claims against the photos (marked ✔ below). All 13 were real.

| Batch | Photos | Findings | Tokens | Time | Report |
|---|---|---|---|---|---|
| Bayview mainline + Hillcrest Quay Road 007–010 | 28 | 18 | 108k | 171 s | `full-bayview-quayroad.md` |
| Via Crest 005–012 | 23 | 9 | 87k | 113 s | `full-via-crest-a.md` |
| Via Crest 013–020 | 24 | 9 | 92k | 128 s | `full-via-crest-b.md` |
| High Street 007 + Terrace Row | 24 | 12 | 89k | 100 s | `full-highstreet-terrace.md` |
| Quay Lane | 23 | 8 | 84k | 82 s | `full-quay-lane.md` |
| Haul Road 002–007 + Chapel Street | 32 | 17 | 121k | 191 s | `full-haul-chapel.md` |
| **Total** | **154** | **73** | **581k** | **~3 min (parallel)** | |

## Town-wide patterns (one fix each in the builders)
Most findings repeat on every street, so each pattern below is one generator fix rather than many local ones.

1. **Shop-name boards behind the balcony railings.**
   - The bars and flower boxes cross the letters, and the end letters are cropped: "RDWARE", "KERY", "HARDV".
   - Seen on Quarry Road, Quay Lane, Terrace Row, Haul Road, Via Crest and Hillcrest Quay Road. "SIOP" also reads as "STOP".
   - Fix: move the board onto a fascia in front of the railing (QR-7).
2. **Top-floor loggias are black voids.** The coloured "flower" balls on the sill look like gems floating in a hole. Seen in every Quarry Hills and Hillcrest batch. Fix: give the loggias a back wall and floor, or fill them in.
3. **Arch shopfronts.**
   - The voussoir blocks are jagged and offset at the springing.
   - The flat blue panel fills only part of the opening, leaving a black slot beside it.
   - Seen at: TR-3, QL2-3, VCB-2, HR2-7/8, CS2-4/5, Quarry Road 012-right ✔.
   - This is probably one arch-builder bug (the arch is too wide for its facade bay).
4. **Benches and slatted boxes jammed under window boxes**, merging with the box or wall. Seen at: HS-2, TR-5, QL2-4, VCA-9, HR2-9, CS2-7 and the Quarry Road trial. Pull them forward about 0.5 m, or drop the ones placed under windows.
5. **Doorstep wedges lying tilted on lawns, away from doors.** Seen at: TR-8, VCA-7 ✔ (Via Crest 007-right), HR2-10, CS2-6.
6. **Buildings on slopes.**
   - Terrain runs diagonally up the side walls, so houses look half-buried: VCA-1 ✔, VCB-4 ✔ (Via Crest 007-right and 018-right).
   - Grey plinth wedges hang over the drops (VCA-4).
   - Retaining-wall slabs jut out into the air and cast hard shadows (CS2-2 ✔, Chapel 009-left).
   - Zig-zag lawn walls enclose nothing (TR-4).
   - Fix: a foundation or retaining-wall pass for plots whose ground isn't level.
7. **Parked cars on pavements and lawns, across doors, clashing with props.**
   - Seen on Quarry Road, Hillcrest Quay Road (QY-2) and Via Crest (VCA-3).
   - Fix: the parking clearance test from QR-1, QR-2 and QR-6.

## Specific defects (worst first)

| Id | Where | What | Checked |
|---|---|---|---|
| QY-5 | Hillcrest Quay Road 010-left | A water wheel is wedged between two houses, its rim cutting into both walls, over a dry paved alley. | ✔ |
| QY-3 | Hillcrest Quay Road 009-right | A pink car on the side road is tilted on top of a pink diagonal slab. | ✔ |
| QY-4 | Hillcrest Quay Road 009-right | The pavement's square corner pokes past the rounded kerb into the junction. | ✔ |
| VCB-3 | Via Crest 018-right | The Town Hall arcade brackets are twisted and broken, and the blue panels run past the arches. | ✔ |
| CS2-1 | Chapel Street 006-left | A flat sand "path" is pasted up the bank; it stops halfway and starts again higher up. | ✔ |
| HR2-6 | Haul Road 006/007-along | The far quarry wall has pale zig-zag triangles and stair-step artefacts. | ✔ |
| BH-2 | Bayview mainline 001–005-left | Pale translucent streaks show under the viaduct piers, in the water. | ✔ |
| (trial) | Quarry Road 010-right | The blue car floats tilted above the lawn. | ✔ |
| QR-2, OPUS-14 | Quarry Road 010-right, 011-right | Topiary planters inside the cream 4x4 and the white car. | ✔ |
| QR-5 | Quarry Road 009-along | Doubled lamp posts. | ✔ |
| VCB-7 | Via Crest 017-left, 020-left | Steep drop beside the pavement with no wall. | – |
| HR2-2 | Haul Road 003-along | A hard straight seam where the sandy road meets the asphalt. | – |
| VCA-5 | Via Crest 012-left | Moiré swirls on one terracotta roof. | – |
| CS2-3 | Chapel Street banks | Grey, smeared slope texture. It's the rock layer on steep terrain; it looked plausible in 006-left and 009-left. | partly |
| HR2-5 | Haul Road 007 | A sign lies flat on the bank with no post. | partly (it's visible in 007-along) |

The remaining low-severity and low-confidence items are in the batch reports, along with the improvement lists (empty lots, blank gables, bare lawns, flat shop windows).

## Next
- Fix the town-wide patterns first; patterns 1–5 are each one change in the builder.
- Close-ups are worth taking for the water wheel, the pink car and slab at Quay Road 009, the Town Hall arcade, and the Chapel Street sand path.
- The earlier planned close-ups are still listed in each street file's "Continue from here" section.
