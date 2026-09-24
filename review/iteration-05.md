# Village, bridge and race-corridor review

16 September 2026. Development milestone; the goal remains active.

## Changes and visual evidence

Replaced the garage block with an open workshop containing benches, shelving, tools, tyres and a service bay. Added side-wall windows and masonry, planted borders, varied tree canopies, cafe furniture, a tidal inlet and a three-arch bridge. Road and masonry now have original procedural surface detail. Common static meshes are batched while their collision bodies are retained.

Initial captures exposed a reversed garage gable and vegetation floating above the triangulated riverbank. Corrected the gable winding, matched vegetation placement to the rendered terrain triangles, added bridge foundations/abutments and reduced fog's effect on the sky. The automatic capture sequence now includes the bridge and workshop frontage.

Final standalone evidence at 1920×1080: [village](captures/standalone-04/01-village.png), [coast](captures/standalone-04/02-coast.png), [car](captures/standalone-04/03-car.png), [24-car grid](captures/standalone-04/04-grid.png), [racing](captures/standalone-04/05-racing.png), [parts menu](captures/standalone-04/06-garage.png), [bridge](captures/standalone-04/07-bridge.png), [workshop](captures/standalone-04/08-workshop.png).

## Race regression found and corrected

The first scenery trial put the developed car 18th in 125.12 seconds, including a recovery. Added stall/contact and recovery telemetry to the race harness. It showed the player AI leaving the road near distance 37 m and hitting the workshop wall; the bridge was not that stall's location.

Rival steering now checks the current and predicted road offset. A car heading outside the driving corridor brakes and aims toward the road centre before returning to its chosen passing lane. This applies equally to rivals and the scripted test player. Equipment values, rewards and rival setups were unchanged.

| Setup | Place / 24 | Time | Recoveries before player finish |
| --- | ---: | ---: | ---: |
| Stock | 24 | 122.18 s | 0 across the field |
| Developed road setup | 6 | 97.82 s | 0 across the field |

Evidence: [stock](race-trial-stock.json), [developed](race-trial-developed.json). All 24 cars finished the stock trial without recovery or a recorded one-second low-progress episode. The developed trial ended at the player's sixth-place finish, so only six cars had finished; it recorded 18 low-progress episodes among opponents around the bridge approach, despite zero recoveries. That traffic behaviour still needs work. Earlier reports are retained as `*-before-scenery.json`, and the developed-car regression is retained in `race-trial-developed-scenery-stalls.json`. These are single scripted comparisons, not human difficulty measurements or distributions. The developed car has not yet met the winning-setup target. Repeated opening-bend, blocked-lane and varied-grid tests remain required.

[Functional checks](test-results.json): 32/32 passed after the final AI change. The check script now detects script errors during import and bounds test execution so a compilation failure cannot leave an endless failing run. The exported executable produced all eight captures and exited with code zero; the final bridge, parts menu and race captures were visually inspected. Successful captures are not a frame-rate benchmark.

## Visual assessment against the unchanged concepts

| Dimension | Assessment | Remaining gap |
| --- | --- | --- |
| Car model | 2/5 | Cabin/body still too boxy; arches and material detail need work |
| Architecture | 3/5 | Workshop and facades are more legible; repeated roof geometry and blank expanses remain |
| Road and edges | 2/5 | Texture and paving improved; abrupt verges and grounding need further review |
| Lighting and atmosphere | 2/5 | Better shadows/colour, but sky, water and distant scenery remain simple |
| Composition and exploration | 2/5 | Bridge/inlet add landmarks; starting vista still lacks the reference's coastal composition |
| Prop density | 2/5 | Village is fuller; broad grassy areas remain sparse and planting repetitive |
| HUD | 3/5 | Readable; controller and smaller-screen review remain open |

These are qualitative observations from runtime captures. No dimension is claimed to satisfy the final 4/5 target. All remaining gameplay, ownership, controller, audio, performance and additional-track requirements in the backlog still apply.
