# Little Road

Working codename for an original car adventure inspired by Road Trip Adventure / Choro Q HG 2.

**24 September handoff:** start with the root [Racing AI guide](RACING_AI_GUIDE.md) for the current code map, best established test results, known failures, and exact build/watch commands. Latest compiled experiment: 111; retained exact performance baseline: 109; development paused. Historical checkpoint notes and launchers below are preserved and are not selectors for the newest experiment. Bulky generated telemetry and unreferenced capture sequences remain local; source, assets, summaries and review-linked screenshots are versioned.

21 September traffic checkpoint: [local following and passing](review/iteration-40-local-traffic-planning.md) records 239 editor checks and 105 runtime checks across eight controlled scenarios, including the first reverse-speed three-car queue. `Watch Traffic Passing.cmd` opens that frozen development test. Broader corner/abort cases, varied gaps, performance and player acceptance remain outstanding; the desktop adventure remains checkpoint 20.

21 September AI checkpoint: [blocked recovery and anticipation](review/iteration-39-blocked-recovery-and-preview.md) records 18 passing stock/club scenarios, including safe assisted fallback for permanent traps, reversing, missed-gate return and waiting for traffic. `Watch Physical Recovery.cmd` opens the frozen stock-car wall replay. This is an opt-in development build; the normal desktop adventure remains checkpoint 20. Broader traffic/corner recovery and moving player acceptance are still outstanding. The review also records an early-close shutdown issue seen during a slow-motion visual inspection.

Created: 16 September 2026. Status: first playable prototype in development. Godot 4.7.2 and Blender 5.2.2 are verified. The reference is Road Trip Adventure on PS2. The name is provisional.

19 September planning update: [current scope and priorities](docs/17-scope-and-priorities.md) now target at least 12 hours of substantial content, retaining the previous mechanics and fidelity goals. New specifications cover [endurance, fuel, tanks, tyre service and changing fuel mass](docs/18-endurance-fuel-and-mass.md) and the [original-inspired 3D parts carousel](docs/19-parts-browser-reference-and-spec.md), visually checked in the user's emulator. This is a design update; no new gameplay build or bug fixes are claimed.

19 September implementation: checkpoint 11 includes the [checkpoint/recovery corrections](review/iteration-18-checkpoint-recovery.md) and [course-defect/water pass](review/iteration-19-course-defects.md). The stretched grass is absent from inspected driving/racing views, the lighthouse lane ends outside the carriageway, planting accounts for root footprints and distant water is calmer. The candidate passes 129 editor checks, world/garage checks and a continuous free lap followed by all 24 career racers finishing with the actual result menu and saved payout. Manual finish and physical controller acceptance remain open. The world and cars still need substantial fidelity work.

17 September: Unity migration is underway in the separate `unity` project, using Unity 6000.6.1f1, URP and C#. The current source connects free driving, three residents, a delivery, five finds, the seven-slot garage, a 24-car race and separate versioned saving. The village has an open workshop, shaped tiled roofs, shop frontages and forecourt props. Sculpted landforms, coastal walls and an arched inlet bridge now replace the flat island. It remains an unfinished prototype: scenery fidelity, car art, collision/recovery behaviour, balance, audio and broader progression still need work. The Godot checkpoint below remains available. See [migration status](docs/16-unity-migration.md), [the adventure integration review](review/iteration-11-unity-adventure.md), [the village review](review/iteration-12-unity-village.md) and [the coastal review](review/iteration-13-unity-coast.md).

## Run the Unity prototype

Latest scenery work: [coastal relief](review/iteration-26-coastal-relief.md) and [masonry](review/iteration-25-coastal-masonry.md) are included in checkpoint 14. Outcrops have finer fractures and ledges, banks vary in shape, and fallen rocks sit on shallow slopes. Individual wall stones, coping and recessed joints replace flat parapets; roadside sections and colliders follow bends without the repeated gaps. World/garage, 24-car and connected free-drive/race/payout checks pass. Bridge structure, overall scene composition and rendering-performance acceptance still need work.

Earlier [village planting and lighter foliage](review/iteration-24-village-planting.md) brings fuller coastal canopies, branched village olives in soil beds and detailed flower boxes from checkpoint 13. It halves the near-leaf geometry and reduces distant vertices by 65–71% from the [initial foliage study](review/iteration-23-foliage.md). LOD transitions and actual frame-time acceptance remain open. The [scanned cliff comparison](review/iteration-21-cliff-study.md) remains opt-in and disabled in normal play; its rectangular silhouette and terrain joins are still experimental.

Latest contact work: [car contact and wheel coverage](review/iteration-22-car-contact.md) changes the default to speculative collision detection after matched solo, controlled passing and 24-car comparisons. Two candidate fields measure less than 1 mm maximum box overlap, versus the previous 21.7 mm case. Sampled animated body/wheel vertices remain inside the existing boxes. World/garage and connected free-drive/race/results/saved-payout checks pass; this is not universal no-clipping or performance acceptance.

Latest checkpoint: `little-road-unity-adventure-20.zip` adds a physical hill, combined course and gravity controls, improves the measurement driver and fixes sustained downhill overspeed above the gear ceiling. All 183 final equipment runs pass their analysis checks, alongside a complete 24-car race and passing vertical audit. See [checkpoint 20 evidence](review/iteration-33-physical-grade-fixtures.md). The desktop launcher uses this verified export; checkpoint 19 is preserved in `little-road-unity-windows-before-20`. The engine browser and existing car/world art are retained. Physical-controller/manual, visible performance and the wider campaign remain unfinished. Next is the user-requested [technical AI circuit](docs/25-technical-ai-test-circuit.md), following the exact external AI implementation plan linked there.

The [evening route investigation](review/evening-01/notes.md) preserves the original reported failures. Checkpoint 11 corrects the reproduced sideways-crossing and busy-recovery defects, but a manual completion/rejoin playtest is still required to close the original finish report. Traffic/contact regression and the Bayview/three-master-car visual benchmark are next; a passing automated race does not establish final balance or no-clipping.

Run `Launch Little Road Unity.cmd` or `Little Road Unity.exe` in the neighbouring `little-road-unity-windows` folder. The normal launch starts beside Harbour Garage. Drive with WASD/arrows or left stick and triggers; stop near a resident and press E/Cross. Escape/Circle opens or closes town menus. Buy and fit equipment separately at Mara's garage; Rowan starts the race. Escape/Options retires from an unfinished race without payout.

The Unity save is `little-road-unity-save-v1.json` in the app's persistent-data directory. Each transaction saves automatically. The Godot save remains separate; there is no save import yet. Physical DualSense testing remains outstanding despite generic gamepad support and synthetic input checks.

For the standalone race-only developer mode, launch the executable with `--trial-auto` (24 automatic entrants) or `--trial-race` (manual player). These modes do not award adventure currency.

`Watch 24-Car Race.cmd` opens a windowed three-lap demonstration with all 24 cars driving automatically. Tab toggles control of the stock car; Escape closes the test. The window closes once the field finishes or the four-minute test limit is reached.

The preceding landscape checkpoint is `little-road-unity-adventure-09.zip` in the neighbouring outputs folder, with [cliffs and ground review](review/iteration-16-cliffs-and-ground.md). It adds original outcrops, credited photographic ground materials and grass clumps; the reviewed scenery remains below the visual target. The preceding `little-road-unity-adventure-08.zip` preserves the [foliage and lighting pass](review/iteration-15-foliage-and-light.md), and `little-road-unity-adventure-07.zip` preserves the [functional traffic checkpoint](review/iteration-14-unity-traffic.md). The mutable export is also used for ongoing visual experiments.

## Preserved Godot checkpoint

For the standalone Windows version, run `Little Road.exe` in the neighbouring `little-road-windows` folder. Godot is not required to play that build. The Windows ZIP contains the executable, control guide and engine licence notices.

Open `game/project.godot` in Godot 4.7.2 and press F6 for the main scene or F5 to run the project. On this PC, `Launch Little Road.cmd` uses the engine in `C:\Users\cripp\Documents\Godot`.

Controls: WASD or arrows to drive, S/down to brake and then reverse, Space for handbrake, E to talk when stopped near a resident, R to recover to the road, Escape for pause, F12 to save a photo. Gamepad mappings exist but have not been tested on a physical controller.

The source prototype contains a coastal route with a village workshop and bridge, original Blender car bodies, three residents, delivery progression, seven equipment slots with fourteen purchasable parts, recolouring, collectibles, a 24-car race and local save/load. The latest handling adds power, drag, turbo upgrades and shared cornering traction. It is not yet at the visual or gameplay completion target. Read [the evening handoff](docs/14-power-grip-and-evening-handoff.md), [the latest visual review](review/iteration-07-evening.md), and [the previous packaged-build review](review/iteration-05.md) for evidence and limitations.

## Start here

- [Game research](docs/01-game-research.md): what the reference game does, evidence, design lessons and research gaps.
- [Project brief](docs/02-project-brief.md): our proposed game, first playable slice, milestones and success criteria.
- [Software and setup](docs/03-software-and-setup.md): what to download, why Godot, engine alternatives and the Blender workflow.
- [Development backlog](docs/04-development-backlog.md): ordered implementation tasks and acceptance checks.
- [Mechanics and system connections](docs/05-mechanics-and-system-connections.md): how to preserve depth while developing in small steps.
- [Visual and video references](docs/06-visual-and-video-references.md): original concept sheets, original-game screenshot sources and selected YouTube videos.
- [Active goal and quality loop](docs/07-goal-and-quality-loop.md): screenshot comparison, improvement cycles and completion gates.
- [Tools and optional Blender MCP](docs/08-tools-and-optional-blender-mcp.md): verified local tools, testing workflow and live-connection instructions.
- [Racing and earning progression](docs/09-racing-and-parts-progression.md): reference research, 24-car requirement, economy, tracks/classes and AI failure cases.
- [Expanded car ownership](docs/10-expanded-ownership-and-parts.md): every original equipment category, our expanded families and staged requirements.
- [PS5 DualSense specification](docs/11-dualsense-controller-spec.md): Windows USB/Bluetooth controls and hardware acceptance tests.
- [Driver skill and balance testing](docs/12-driver-skill-and-balance-testing.md): natural variation, matched solo runs, equipment comparisons and two-/three-car overtaking tests.
- [Future driver and teammate progression](docs/20-driver-personality-and-teammate-progression.md): distinct abilities/behaviours, racing experience and separate track familiarity; full specification and implementation deferred.
- [Future performance rating and race preview](docs/21-performance-rating-and-race-preview.md): equipment-based car ratings and player/teammate/opponent comparison before entering a race; implementation deferred.
- [Car roster and collisions](docs/13-car-roster-and-collisions.md): road cars through F1-style bodies, class-specific fields, visual collision checks and recoverable crash response.
- [Car fidelity and 100-body plan](docs/15-car-fidelity-assets-and-100-body-plan.md): master bodies, reusable modules, asset review and separate decal production.
- [Unity migration](docs/16-unity-migration.md): installed toolchain, build entry point, evidence and feature-parity gates.

**Toolchain:** Unity 6000.6.1f1 / C# / URP for the approved migration; Godot 4.7.2 / GDScript for the preserved checkpoint; Blender 5.2.2 for editable original art. Optional art and audio applications can wait.

## First playable target

A small coastal town connected to a scenic road loop. Drive a toy-like car, meet three car residents, complete one delivery, race in a field of at least 24 cars, earn and fit multiple upgrades, discover a hidden collectible, and save your progress. Aim for a satisfying 10–15 minute session before adding more regions.

This remains the target experience. The first implementation exists, but its pacing, depth, visual polish and full end-to-end playability still need review.

## Project layout

```text
little-road/
  README.md
  docs/                 Research, design and backlog
  game/                 Godot project, scripts, tests and runtime assets
  source-art/           Editable Blender and painting source files
  tools/                Reproducible checks
  review/               Test evidence, runtime captures and visual assessments
```

Keep editable source art separate from assets exported into the game. Keep build exports outside the source tree. The car, village, landscape and vegetation meshes are authored for this project. CC0 sky photography and rock/grass surface maps from Poly Haven are documented in [the asset credits](THIRD_PARTY_ASSETS.md); there are no paid art dependencies at this stage.

The source ZIP includes research, concepts, Blender source, the Godot project and review evidence. Generated Godot caches and tool downloads are omitted. To export on another machine, install matching Godot 4.7.2 export templates and clear the custom-template paths in the Windows export preset; those paths point to tooling in this workspace.

This is the local project repository. It has not been registered as a saved project in the Codex sidebar. Third-party art provenance is recorded in `THIRD_PARTY_ASSETS.md`; no paid dependencies have been introduced.




