# Evening route review — 17 September 2026

Investigation only, following the user's reports after checkpoint 10. Development remains paused until the user asks to continue. The visual target and broader game goal remain unfinished. No reported bug was fixed tonight.

## Scope and evidence

A separate diagnostic export ran a continuous physical AI-driven free-roam lap, then a connected three-lap career race with 24 cars. These were not manual keyboard/controller playthroughs. Captures were taken approximately every 55 metres; all 13 free-roam and 13 first-race-lap views, plus the result screen, were inspected.

The free-roam test made one initial placement on the road and reused the gentle 18 m/s AI planning mode; it did not teleport around the route. It completed 683.60 m in 42.62 s, with zero recorded contacts or recoveries. Maximum centreline offset was 8.67 m, briefly exceeding the road's 8 m half-width. This is route coverage, not acceptance of manual handling or road containment. See [free-lap data](captures/free-lap.json).

The career test entered through the existing race-start callback rather than physically returning to Rowan. All 24 cars finished. The stock player car finished 24th in 109.99 s; the real result menu appeared and a fresh disk read confirmed 100 credits, raceSerial 1 and settledSerial 1. See [result data](captures/race-result.json), [result screenshot](captures/race-result.png) and [run log](route-run.log). One race does not establish balance or finish robustness.

The diagnostic used an isolated save. The user's existing save was read only and still has 0 credits, raceSerial 1 and settledSerial 0. The original session log is preserved as [user-run-through-preserved.log](user-run-through-preserved.log).

## Findings and next-session priorities

1. **Race completion: unresolved, highest gameplay priority.** The user's log contains finishes for all 23 opponents but no player finish event and no reward-saving error. The successful AI control run demonstrates that the result/payout chain can work; it does not resolve the user's failure. Source inspection suggests a missed checkpoint can leave a manually driven car permanently waiting for that gate: crossing requires a narrow progress window, lateral bounds and forward heading, while only AI cars automatically recover after missing a gate. The original log lacks the trajectory needed to prove this explanation. Next time, record player gate/progress state and reproduce wide crossings, spins, reversing, missed-gate rejoining and recovery. Make valid finishes reliable and communicate invalid laps/rejoining clearly; verify the menu, exactly-once payout and saved balance together.

2. **Stretched grass: reproduced in both modes.** Long green/black triangles and their shadows cross the carriageway and obscure the camera around the village/start and first bends. Strong evidence: [free-roam at 110 m](captures/free-110.png), [race at 110 m](captures/race-110.png) and [race at 165 m](captures/race-165.png). This is a rendering/geometry failure, not intended roadside vegetation. The large ground-cover mesh, static batching and index handling are candidates, not a confirmed cause. Next time isolate the renderer, compare batched/unbatched behaviour, inspect source and post-batch indices/triangle spans, and consider spatial chunks if warranted. Verify the whole loop and shadows in both modes after correction.

3. **Driveway crossing the road: reproduced in free roam.** The beige lighthouse lane beside Pip extends well into the carriageway, beyond its centre: [free-roam at 275 m](captures/free-275.png). It is absent in [the race view](captures/race-275.png), since this lane belongs to adventure scenery. `AdventureGame.CreatePlaces()` builds the spur to the road centreline and adds five metres to its length, explaining the protrusion. Next time terminate and blend the lane at the shoulder/road edge. Existing collider clearance checks miss this noncolliding visual intrusion; add visual geometry coverage.

4. **Distant water needs another pass.** From road cameras the sea has dense, high-contrast ripple patterns resembling noise, especially around 385–550 m: [race at 550 m](captures/race-550.png). The close-water review was insufficient. Review distance filtering, ripple attenuation and the horizon from the actual driving camera.

5. **Lower-priority presentation observations.** Some tree roots visibly hang above steep slopes; long banks remain bare and smooth; rocks and foliage repeat prominently. Resident labels, floating collectible placeholders and large HUD panels need polish. These remain below the reference target. No new art work is authorized by this evening review alone.

## Review process correction

Previous fixed-camera checks missed the stretched grass. Future visual acceptance needs continuous route coverage in both adventure and race modes, with captures from the player's camera, alongside the existing static compositions. Automated race success must not substitute for manual completion and recovery tests.

## Reproducibility and stopping point

The opt-in `EveningRouteReview` harness, its adventure/race hooks and a separate build-output option were added to source solely for this investigation. It requires explicit isolated-save and capture paths. The diagnostic build passed the existing 112 editor checks; see [editor-checks.json](editor-checks.json). No scenery, driving, checkpoint or reward fixes were made.

Diagnostic export: `outputs/little-road-evening-review/Little Road Unity.exe`. Assembly SHA-256: `1BB06623F2AF187159D8F86AE655B6824FC12F61EDC1DAA9348D3122062A2467`.

The normal playable export and checkpoint-10 archive were not replaced. Normal export assembly SHA-256 remains `947F8D82BFD46F0F7F495CB89A6D3E1401BF0CC7F7AD25E6833A82B2A268477D`. The diagnostic process exited after capturing the result. Stop here for the evening; resume the priorities above only when the user asks.
